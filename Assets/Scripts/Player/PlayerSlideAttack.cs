using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox sem dano do Slide. Inimigos atingidos recebem somente o Launch Up
/// global do PlayerCombat, no maximo uma vez por slide.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSlideAttack : MonoBehaviour
{
    [Header("Slide Launch Attack")]
    [Tooltip("Velocidade horizontal minima para a hitbox do Slide ficar ativa.")]
    [Min(0f)] public float minimumSlideSpeed = 12f;
    [Tooltip("Largura da hitbox em relacao a largura atual do player.")]
    [Min(1f)] public float hitboxWidthMultiplier = 1.75f;
    [Tooltip("Profundidade da hitbox em relacao a profundidade atual do player.")]
    [Min(0f)] public float hitboxDepthMultiplier = 1.75f;
    [Tooltip("Tempo em que o player atravessa apenas o inimigo que o Slide acabou de lancar.")]
    [Min(0f)] public float launchedEnemyCollisionIgnoreDuration = .15f;
    [Tooltip("Converte a velocidade atual do Slide em impulso horizontal para o inimigo lancado.")]
    [Min(0f)] public float launchForwardConversion = 0f;

    private readonly HashSet<EnemyBase> enemiesLaunchedThisSlide = new();
    private readonly List<IgnoredEnemyCollision> ignoredEnemyCollisions = new();

    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerCombat playerCombat;
    private CharacterController controller;
    private bool wasSliding;

    private sealed class IgnoredEnemyCollision
    {
        public float restoreTime;
        public readonly List<Collider> colliders = new();
    }

    public bool IsActive => playerMovement != null && playerState != null &&
        playerState.IsSliding() &&
        playerMovement.HorizontalSpeed >= minimumSlideSpeed;

    public static PlayerSlideAttack GetOrAdd(GameObject target)
    {
        PlayerSlideAttack slideAttack = target.GetComponent<PlayerSlideAttack>();
        if (slideAttack == null)
            slideAttack = target.AddComponent<PlayerSlideAttack>();

        slideAttack.enabled = true;
        return slideAttack;
    }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;

        RestoreExpiredEnemyCollisions();

        bool isSliding = playerState != null && playerState.IsSliding();
        if (!isSliding && wasSliding)
            enemiesLaunchedThisSlide.Clear();

        wasSliding = isSliding;

        if (!IsActive || playerCombat == null || controller == null)
            return;

        Collider[] hits = Physics.OverlapBox(
            GetHitboxCenter(),
            GetHitboxHalfExtents(),
            transform.rotation,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || !enemy.CanBeLaunched ||
                enemiesLaunchedThisSlide.Contains(enemy))
            {
                continue;
            }

            IgnoreEnemyCollisionTemporarily(enemy);
            playerCombat.ApplyNonDamagingLaunchUp(
                enemy,
                transform.forward,
                playerMovement.HorizontalSpeed * launchForwardConversion
            );
            enemiesLaunchedThisSlide.Add(enemy);
        }
    }

    private void IgnoreEnemyCollisionTemporarily(EnemyBase enemy)
    {
        if (enemy == null || controller == null ||
            launchedEnemyCollisionIgnoreDuration <= 0f)
        {
            return;
        }

        IgnoredEnemyCollision ignored = new()
        {
            restoreTime = Time.time + launchedEnemyCollisionIgnoreDuration
        };

        foreach (Collider enemyCollider in enemy.GetComponentsInChildren<Collider>())
        {
            if (enemyCollider == null || enemyCollider == controller)
                continue;

            Physics.IgnoreCollision(controller, enemyCollider, true);
            ignored.colliders.Add(enemyCollider);
        }

        if (ignored.colliders.Count > 0)
            ignoredEnemyCollisions.Add(ignored);
    }

    private void RestoreExpiredEnemyCollisions()
    {
        for (int index = ignoredEnemyCollisions.Count - 1; index >= 0; index--)
        {
            IgnoredEnemyCollision ignored = ignoredEnemyCollisions[index];
            if (Time.time < ignored.restoreTime)
                continue;

            RestoreEnemyCollision(ignored);
            ignoredEnemyCollisions.RemoveAt(index);
        }
    }

    private void RestoreAllEnemyCollisions()
    {
        foreach (IgnoredEnemyCollision ignored in ignoredEnemyCollisions)
            RestoreEnemyCollision(ignored);

        ignoredEnemyCollisions.Clear();
    }

    private void RestoreEnemyCollision(IgnoredEnemyCollision ignored)
    {
        if (controller == null || ignored == null)
            return;

        foreach (Collider enemyCollider in ignored.colliders)
        {
            if (enemyCollider != null)
                Physics.IgnoreCollision(controller, enemyCollider, false);
        }
    }

    private void OnDisable() => RestoreAllEnemyCollisions();
    private void OnDestroy() => RestoreAllEnemyCollisions();

    private Vector3 GetHitboxCenter()
    {
        return controller != null
            ? controller.bounds.center
            : transform.position;
    }

    private Vector3 GetHitboxHalfExtents()
    {
        if (controller == null)
            return Vector3.one * .5f;

        // A altura acompanha o player; largura e profundidade sao
        // configuraveis em relacao ao tamanho atual da capsula.
        float radius = controller.radius;
        return new Vector3(
            radius * hitboxWidthMultiplier,
            controller.height * .5f,
            radius * hitboxDepthMultiplier
        );
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !PlayerDebugToolkitHUD.IsVisible)
            return;

        CharacterController characterController = controller != null
            ? controller
            : GetComponent<CharacterController>();
        if (characterController == null)
            return;

        float radius = characterController.radius;
        Vector3 halfExtents = new(
            radius * hitboxWidthMultiplier,
            characterController.height * .5f,
            radius * hitboxDepthMultiplier
        );

        Gizmos.matrix = Matrix4x4.TRS(
            characterController.bounds.center,
            transform.rotation,
            Vector3.one
        );

        // O preenchimento torna a area legivel mesmo quando ela se sobrepoe
        // ao CharacterController e aos gizmos do cenario.
        Gizmos.color = IsActive
            ? new Color(1f, .38f, 0f, .22f)
            : new Color(1f, .38f, 0f, .08f);
        Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);

        Gizmos.color = IsActive ? Color.yellow : new Color(1f, .65f, 0f, .6f);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        Gizmos.matrix = Matrix4x4.identity;
    }
}

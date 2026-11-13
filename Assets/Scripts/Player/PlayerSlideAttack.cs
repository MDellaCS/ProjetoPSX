using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox sem dano do Slide. Causa Poise Damage e solicita Launch Up no maximo
/// uma vez por inimigo a cada slide.
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
    [Tooltip("Camadas consultadas pelo hitbox do Slide para encontrar inimigos atingidos.")]
    [SerializeField] private LayerMask slideHitboxLayers = Physics.DefaultRaycastLayers;
    [Tooltip("Converte a velocidade atual do Slide em impulso horizontal para o inimigo lancado.")]
    [Min(0f)] public float launchForwardConversion = 0f;
    [Tooltip("Dano de Poise causado pelo impacto do Slide. O Launch Up acontece somente quando este golpe deixa o Poise do inimigo em zero ou menos.")]
    [Min(0)] public int poiseDamage = 60;

    private readonly HashSet<EnemyBase> enemiesHitThisSlide = new();
    private readonly List<IgnoredEnemyCollision> ignoredEnemyCollisions = new();
    private Collider[] hitBuffer = new Collider[32];

    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerAttackResolver attackResolver;
    private PlayerCharacterController controller;
    private bool wasSliding;

    private sealed class IgnoredEnemyCollision
    {
        public EnemyBase enemy;
        public float restoreTime;
    }

    internal bool IsIgnoringMovementCollision(EnemyBase enemy)
    {
        for (int index = 0; index < ignoredEnemyCollisions.Count; index++)
        {
            IgnoredEnemyCollision ignored = ignoredEnemyCollisions[index];
            if (ignored.enemy == enemy && Time.time < ignored.restoreTime)
                return true;
        }
        return false;
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
        attackResolver = GetComponent<PlayerAttackResolver>();
        controller = GetComponent<PlayerCharacterController>();
    }

    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        RestoreExpiredEnemyCollisions();

        bool isSliding = playerState != null && playerState.IsSliding();
        if (!isSliding && wasSliding)
            enemiesHitThisSlide.Clear();

        wasSliding = isSliding;

        if (!IsActive || attackResolver == null || controller == null)
            return;

        Vector3 hitboxHalfExtents = GetHitboxHalfExtents();
        float horizontalAttackRadius = new Vector2(
            hitboxHalfExtents.x, hitboxHalfExtents.z).magnitude;
        int hitCount;
        while (true)
        {
            hitCount = Physics.OverlapBoxNonAlloc(GetHitboxCenter(),
                hitboxHalfExtents, hitBuffer, transform.rotation,
                slideHitboxLayers, QueryTriggerInteraction.Ignore);
            if (hitCount < hitBuffer.Length)
                break;
            System.Array.Resize(ref hitBuffer, hitBuffer.Length * 2);
        }

        for (int index = 0; index < hitCount; index++)
        {
            Collider hit = hitBuffer[index];
            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemiesHitThisSlide.Contains(enemy))
            {
                continue;
            }

            if (enemy.TryAvoidPlayerAttack(horizontalAttackRadius))
            {
                enemiesHitThisSlide.Add(enemy);
                continue;
            }

            bool launched = attackResolver.ApplyNonDamagingLaunchUp(
                enemy,
                poiseDamage,
                transform.forward,
                playerMovement.HorizontalSpeed * launchForwardConversion
            );
            if (launched)
                IgnoreEnemyCollisionTemporarily(enemy);
            enemiesHitThisSlide.Add(enemy);
        }
    }

    private void IgnoreEnemyCollisionTemporarily(EnemyBase enemy)
    {
        if (enemy == null || controller == null ||
            launchedEnemyCollisionIgnoreDuration <= 0f)
        {
            return;
        }

        for (int index = 0; index < ignoredEnemyCollisions.Count; index++)
        {
            IgnoredEnemyCollision existing = ignoredEnemyCollisions[index];
            if (existing.enemy != enemy)
                continue;
            existing.restoreTime = Time.time +
                launchedEnemyCollisionIgnoreDuration;
            return;
        }

        ignoredEnemyCollisions.Add(new IgnoredEnemyCollision
        {
            enemy = enemy,
            restoreTime = Time.time + launchedEnemyCollisionIgnoreDuration
        });
    }

    private void RestoreExpiredEnemyCollisions()
    {
        for (int index = ignoredEnemyCollisions.Count - 1; index >= 0; index--)
        {
            IgnoredEnemyCollision ignored = ignoredEnemyCollisions[index];
            if (Time.time < ignored.restoreTime)
                continue;

            ignoredEnemyCollisions.RemoveAt(index);
        }
    }

    private void OnDisable() => ignoredEnemyCollisions.Clear();

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
        if (!Application.isPlaying ||
            !DebugGizmoSettings.Enabled(DebugGizmoGroup.PlayerCombat) ||
            !IsActive)
            return;

        PlayerCharacterController characterController = controller != null
            ? controller
            : GetComponent<PlayerCharacterController>();
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
        Gizmos.color = new Color(1f, .38f, 0f, .22f);
        Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        Gizmos.matrix = Matrix4x4.identity;
    }
}

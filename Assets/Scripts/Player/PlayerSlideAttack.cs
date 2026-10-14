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
    [Min(0f)] public float minimumSlideSpeed = 15f;
    [Tooltip("Largura da hitbox em relacao a largura atual do player.")]
    [Min(1f)] public float hitboxWidthMultiplier = 2f;
    [Tooltip("Profundidade da hitbox em relacao a profundidade atual do player.")]
    [Min(0f)] public float hitboxDepthMultiplier = 3f;

    private readonly HashSet<BaseEnemy> enemiesLaunchedThisSlide = new();

    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerCombat playerCombat;
    private CharacterController controller;
    private bool wasSliding;

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
            BaseEnemy enemy = hit.GetComponentInParent<BaseEnemy>();
            if (enemy == null || !enemy.CanBeLaunched ||
                enemiesLaunchedThisSlide.Contains(enemy))
            {
                continue;
            }

            playerCombat.ApplyNonDamagingLaunchUp(enemy);
            enemiesLaunchedThisSlide.Add(enemy);
        }
    }

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

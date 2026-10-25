using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Observa exclusivamente o gameplay do player e resolve a intencao visual
/// atual. A ausencia deste componente, de playback, Animator ou VisualRoot
/// nunca interfere na simulacao.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimationPresenter : MonoBehaviour
{
    [Header("Optional References")]
    [Tooltip("Deixe vazio para usar este GameObject. Pode apontar para o VisualRoot futuro.")]
    [SerializeField] private Transform playbackRoot;

    [Header("State Resolution")]
    [Min(0f)] [SerializeField] private float moveSpeedThreshold = .1f;

    private readonly List<IPlayerAnimationPlayback> playbackTargets = new();

    private PlayerState playerState;
    private PlayerMovement playerMovement;
    private PlayerAttackController attackController;
    private bool hasResolvedState;

    public PlayerVisualState CurrentVisualState { get; private set; } =
        PlayerVisualState.Idle;

    /// <summary>
    /// Permite observar trocas visuais sem acoplar gameplay ao presenter.
    /// </summary>
    public event Action<PlayerVisualState> VisualStateChanged;

    private void Awake()
    {
        ResolveGameplaySources();
        RefreshPlaybackTargets();
    }

    private void OnEnable()
    {
        // O estado e resolvido novamente ao religar apenas para sincronizar o
        // visual; nenhuma regra de gameplay e executada aqui.
        hasResolvedState = false;
    }

    private void Update()
    {
        if (CutsceneController.IsPlayerControlLocked)
            return;

        ResolveGameplaySources();
        SetVisualState(ResolveVisualState());
        PresentCurrentMovement();
        PresentCurrentAttack();
    }

    /// <summary>
    /// Rele os adaptadores opcionais. Use apos trocar/adicionar o VisualRoot
    /// em runtime. Um array vazio e um resultado valido.
    /// </summary>
    public void RefreshPlaybackTargets()
    {
        playbackTargets.Clear();
        Transform root = playbackRoot != null ? playbackRoot : transform;
        MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour component in components)
        {
            if (component is IPlayerAnimationPlayback playback)
                playbackTargets.Add(playback);
        }

        if (hasResolvedState)
            PresentCurrentState();
    }

    private void ResolveGameplaySources()
    {
        if (playerState == null)
            playerState = GetComponent<PlayerState>();
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (attackController == null)
            attackController = GetComponent<PlayerAttackController>();
    }

    private PlayerVisualState ResolveVisualState()
    {
        // Prioridade: interrupcoes definitivas primeiro; acoes que dominam a
        // pose em seguida; locomocao por ultimo. Ataque vence Jump/Fall para
        // que ataques aereos continuem visualmente como ataques.
        if (playerState != null && playerState.IsDead())
            return PlayerVisualState.Dead;

        if (playerState != null && playerState.IsStaggered())
            return PlayerVisualState.Stunned;

        if (playerState != null && playerState.IsDashing())
            return PlayerVisualState.Dash;

        if (attackController != null && attackController.IsAttacking)
            return PlayerVisualState.Attack;

        if (playerState != null && playerState.IsSliding())
            return PlayerVisualState.Slide;

        if (playerMovement != null && !playerMovement.IsGrounded)
        {
            return playerMovement.VerticalSpeed > 0f
                ? PlayerVisualState.Jump
                : PlayerVisualState.Fall;
        }

        if (playerMovement != null && playerMovement.IsCrouching)
            return PlayerVisualState.Crouch;

        if (playerMovement != null &&
            playerMovement.HorizontalSpeed > moveSpeedThreshold)
        {
            return PlayerVisualState.Move;
        }

        return PlayerVisualState.Idle;
    }

    private void SetVisualState(PlayerVisualState nextState)
    {
        if (hasResolvedState && CurrentVisualState == nextState)
            return;

        hasResolvedState = true;
        CurrentVisualState = nextState;
        VisualStateChanged?.Invoke(nextState);

        PresentCurrentState();
    }

    private void PresentCurrentState()
    {
        foreach (IPlayerAnimationPlayback playback in playbackTargets)
            playback.Present(CurrentVisualState);
    }

    private void PresentCurrentAttack()
    {
        if (attackController == null || !attackController.IsAttacking ||
            string.IsNullOrEmpty(attackController.CurrentAttackId))
        {
            return;
        }

        foreach (IPlayerAnimationPlayback playback in playbackTargets)
        {
            if (playback is IPlayerAttackAnimationPlayback attackPlayback)
            {
                attackPlayback.PresentAttack(
                    attackController.CurrentAttackId,
                    attackController.CurrentPhase,
                    attackController.CurrentPhaseProgress
                );
            }
        }
    }

    private void PresentCurrentMovement()
    {
        float horizontalSpeed = playerMovement != null
            ? playerMovement.HorizontalSpeed
            : 0f;
        bool isMoving = CurrentVisualState == PlayerVisualState.Move;

        foreach (IPlayerAnimationPlayback playback in playbackTargets)
        {
            if (playback is IPlayerMovementAnimationPlayback movementPlayback)
                movementPlayback.PresentMovement(horizontalSpeed, isMoving);
        }
    }
}

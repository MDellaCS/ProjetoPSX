using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttackMovement), typeof(PlayerHitboxController), typeof(PlayerAttackResolver))]
public sealed class PlayerAttackController : MonoBehaviour
{
    private PlayerState playerState;
    private PlayerHeat heat;
    private PlayerAttackMovement attackMovement;
    private PlayerHitboxController hitboxes;
    private PlayerAttackResolver resolver;
    private PlayerMovesetController movesetController;
    private float phaseTimer;
    private bool finishAfterActive;

    public PlayerAttackRuntime CurrentAttack { get; private set; }
    public PlayerAttackPhase CurrentPhase { get; private set; }
    public bool IsAttacking => CurrentAttack != null;
    public string CurrentAttackId => CurrentAttack?.Definition.attackId ?? string.Empty;
    public bool IsCurrentAttackHeavy => IsAttacking && CurrentAttack.Definition.kind == PlayerAttackKind.Heavy;
    public float CurrentPhaseProgress
    {
        get
        {
            float duration = GetPhaseDuration(CurrentPhase);
            return duration <= 0f ? 1f : Mathf.Clamp01(1f - phaseTimer / duration);
        }
    }

    public event Action<PlayerAttackRuntime> AttackStarted;
    public event Action<PlayerAttackRuntime> AttackFinished;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>(); heat = GetComponent<PlayerHeat>();
        attackMovement = GetComponent<PlayerAttackMovement>(); hitboxes = GetComponent<PlayerHitboxController>();
        resolver = GetComponent<PlayerAttackResolver>();
        movesetController = GetComponent<PlayerMovesetController>();
    }

    private void Update()
    {
        if (!IsAttacking || CombatImpactSystem.IsGameplayFrozen(this)) return;
        attackMovement.Tick(CurrentPhase, Time.deltaTime);
        if (CurrentPhase == PlayerAttackPhase.Active)
        {
            hitboxes.Tick(CurrentPhaseProgress);
        }
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f) return;
        if (CurrentPhase == PlayerAttackPhase.Windup) ChangePhase(PlayerAttackPhase.Active);
        else if (CurrentPhase == PlayerAttackPhase.Active)
        {
            if (finishAfterActive) FinishAttack();
            else ChangePhase(PlayerAttackPhase.Recovery);
        }
        else FinishAttack();
    }

    public bool TryStartAttack(PlayerAttackDefinition definition)
    {
        if (definition == null || IsAttacking || playerState == null || !playerState.IsNormal()) return false;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (definition.context == PlayerAttackContext.Ground && movement != null && !movement.IsGrounded) return false;
        PlayerMovesetDefinition moveset = movesetController != null
            ? movesetController.Moveset : null;
        float rotationSpeed = moveset != null
            ? moveset.GetWindupActiveRotationSpeed(definition.kind)
            : 0f;
        CurrentAttack = new PlayerAttackRuntime(definition, heat,
            rotationSpeed);
        CurrentPhase = PlayerAttackPhase.Windup; phaseTimer = CurrentAttack.WindupDuration;
        finishAfterActive = false;
        playerState.ChangeState(PlayerState.State.Attacking);
        attackMovement.Begin(CurrentAttack); hitboxes.Begin(CurrentAttack); resolver.BeginAttack();
        AttackStarted?.Invoke(CurrentAttack);
        if (phaseTimer <= 0f) ChangePhase(PlayerAttackPhase.Active);
        return true;
    }

    public bool RequestCancel(PlayerAttackCancelAction action)
    {
        if (!IsAttacking) return true;
        PlayerMovesetDefinition moveset = movesetController != null
            ? movesetController.Moveset : null;
        System.Collections.Generic.List<PlayerAttackCancelRule> rules =
            moveset != null ? moveset.GetCancelRules(CurrentAttack.Definition.kind) : null;
        PlayerAttackCancelRule rule = rules?.Find(candidate =>
            candidate != null && candidate.action == action);
        if (rule == null || (rule.allowedPhases & ToFlag(CurrentPhase)) == 0) return false;
        if (CurrentPhase == PlayerAttackPhase.Active && rule.finishActiveBeforeCancelling)
        {
            finishAfterActive = true;
            return true;
        }
        FinishAttack(); return true;
    }

    public void CancelAttack()
    {
        if (IsAttacking) FinishAttack(false);
    }

    private void ChangePhase(PlayerAttackPhase phase)
    {
        CurrentPhase = phase; phaseTimer = GetPhaseDuration(phase);
        if (phase == PlayerAttackPhase.Recovery) hitboxes.End();
        if (phaseTimer <= 0f)
        {
            if (phase == PlayerAttackPhase.Active) ChangePhase(PlayerAttackPhase.Recovery);
            else if (phase == PlayerAttackPhase.Recovery) FinishAttack();
        }
    }

    private float GetPhaseDuration(PlayerAttackPhase phase) => CurrentAttack == null ? 0f : phase switch
    {
        PlayerAttackPhase.Windup => CurrentAttack.WindupDuration,
        PlayerAttackPhase.Active => CurrentAttack.ActiveDuration,
        PlayerAttackPhase.Recovery => CurrentAttack.RecoveryDuration,
        _ => 0f
    };

    private void FinishAttack(bool notify = true)
    {
        PlayerAttackRuntime finished = CurrentAttack;
        CurrentAttack = null; CurrentPhase = PlayerAttackPhase.None; phaseTimer = 0f; finishAfterActive = false;
        hitboxes.End(); attackMovement.End();
        if (playerState != null && playerState.IsAttacking()) playerState.ChangeState(PlayerState.State.Normal);
        if (notify && finished != null) AttackFinished?.Invoke(finished);
    }

    private static PlayerAttackCancelPhases ToFlag(PlayerAttackPhase phase) => phase switch
    {
        PlayerAttackPhase.Windup => PlayerAttackCancelPhases.Windup,
        PlayerAttackPhase.Active => PlayerAttackCancelPhases.Active,
        PlayerAttackPhase.Recovery => PlayerAttackCancelPhases.Recovery,
        _ => PlayerAttackCancelPhases.None
    };
}

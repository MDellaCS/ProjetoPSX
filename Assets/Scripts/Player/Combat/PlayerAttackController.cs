using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttackMovement), typeof(PlayerHitboxController), typeof(PlayerAttackResolver))]
[RequireComponent(typeof(PlayerAttackAudioController))]
public sealed class PlayerAttackController : MonoBehaviour
{
    private PlayerState playerState;
    private PlayerHeat heat;
    private PlayerAttackMovement attackMovement;
    private PlayerHitboxController hitboxes;
    private PlayerAttackResolver resolver;
    private PlayerAttackAudioController attackAudio;
    private PlayerMovesetController movesetController;
    private PlayerMovement movement;
    private PlayerTargetAssist targetAssist;
    private readonly Dictionary<PlayerAttackDefinition, float> attackReadyAt = new();
    private float phaseTimer;
    private bool finishAfterActive;
    private bool isExternalAttack;

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
        movement = GetComponent<PlayerMovement>();
        targetAssist = GetComponent<PlayerTargetAssist>();
        if (targetAssist == null)
        {
            targetAssist = gameObject.AddComponent<PlayerTargetAssist>();
            GetComponent<PlayerGrappleAttackController>()?.CopyLegacyTargetAssistSettingsTo(
                targetAssist);
        }
        attackAudio = GetComponent<PlayerAttackAudioController>();
        if (attackAudio == null)
            attackAudio = gameObject.AddComponent<PlayerAttackAudioController>();
        attackAudio.Initialize(this);
    }

    private void Update()
    {
        if (playerState != null && playerState.IsDead())
        {
            CancelAttack();
            return;
        }
        if (!IsAttacking || GamePauseController.IsPaused ||
            CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this)) return;
        PlayerAttackRuntime tickedAttack = CurrentAttack;
        attackMovement.Tick(CurrentPhase, Time.deltaTime);
        if (CurrentPhase == PlayerAttackPhase.Active)
        {
            hitboxes.Tick(CurrentPhaseProgress);
            // Um acerto (grapple, alvo interativo) pode encerrar o ataque, ou
            // iniciar outro via AttackFinished, durante o Tick. Nao avance as
            // fases de um ataque que ja nao e o deste frame.
            if (CurrentAttack != tickedAttack)
                return;
        }
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f) return;
        if (CurrentPhase == PlayerAttackPhase.Active &&
            ShouldHoldActiveUntilGrounded())
        {
            return;
        }
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
        return TryStartAttackInternal(definition, false);
    }

    /// <summary>Inicia a definicao especial de grapple sem limitar o contexto ao chao/ar.</summary>
    public bool TryStartGrappleAttack(PlayerAttackDefinition definition)
    {
        return TryStartAttackInternal(definition, true);
    }

    private bool TryStartAttackInternal(PlayerAttackDefinition definition,
        bool isGrappleAttack)
    {
        if (definition == null || IsAttacking || playerState == null ||
            CutsceneController.IsPlayerControlLocked || !playerState.IsNormal() ||
            !CanStartAttack(definition)) return false;
        // Agua funda: sem ataques (configuravel no WaterProfile).
        if (movement != null && !movement.AllowsAttacksInWater) return false;
        if (!isGrappleAttack && definition.context == PlayerAttackContext.Ground &&
            movement != null && !movement.IsGrounded) return false;
        PlayerMovesetDefinition moveset = movesetController != null
            ? movesetController.Moveset : null;
        float rotationSpeed = moveset != null
            ? moveset.GetWindupActiveRotationSpeed(definition.kind)
            : 0f;
        float extendedArmRadius = moveset != null
            ? moveset.extendedArmHitboxRadius
            : .25f;
        float extendedArmHeightOffset = moveset != null
            ? moveset.extendedArmHitboxHeightOffset
            : 0f;
        EnemyBase assistedTarget = null;
        Vector3 assistedTargetPosition = default;
        float targetAssistRange = definition.UsesTargetAssistTrajectory &&
            targetAssist != null
            ? targetAssist.MaximumRange
            : definition.GetTargetAssistRange(moveset != null
                ? moveset.targetAssistRangePadding : 0f);
        // O gancho de combate tem alcance e mira proprios (GrappleSettings.Combat).
        bool hasAssistedTarget = targetAssist != null && (isGrappleAttack
            ? targetAssist.TryCaptureCombatGrappleTarget(out assistedTarget,
                out assistedTargetPosition)
            : targetAssist.TryCaptureTarget(targetAssistRange, out assistedTarget,
                out assistedTargetPosition));
        if (hasAssistedTarget)
        {
            if (isGrappleAttack)
            {
                Vector3 direction = Vector3.ProjectOnPlane(
                    assistedTargetPosition - GetPlayerCenter(), Vector3.up);
                if (direction.sqrMagnitude > .0001f)
                    transform.rotation =
                        Quaternion.LookRotation(direction.normalized);
            }
        }

        CurrentAttack = new PlayerAttackRuntime(definition, heat,
            rotationSpeed, extendedArmRadius, extendedArmHeightOffset,
            isGrappleAttack, assistedTarget, assistedTargetPosition);
        RecordAttackStart(definition);
        CurrentPhase = PlayerAttackPhase.Windup; phaseTimer = CurrentAttack.WindupDuration;
        finishAfterActive = false; isExternalAttack = false;
        playerState.ChangeState(PlayerState.State.Attacking);
        attackMovement.Begin(CurrentAttack); hitboxes.Begin(CurrentAttack); resolver.BeginAttack();
        AttackStarted?.Invoke(CurrentAttack);
        if (phaseTimer <= 0f) ChangePhase(PlayerAttackPhase.Active);
        return true;
    }

    /// <summary>Encerra a fase de ataque apos o primeiro acerto que inicia um puxao.</summary>
    public void CompleteGrappleAttack(PlayerAttackRuntime runtime)
    {
        if (runtime == null || runtime != CurrentAttack ||
            !runtime.IsGrappleAttack)
        {
            return;
        }

        FinishAttack();
    }

    /// <summary>
    /// Executa uma definicao de ataque por um evento de gameplay, sem input,
    /// sem checar contexto/chao e sem trocar o estado atual do player.
    /// Burn, hazards e futuras habilidades usam a mesma timeline, Hitboxes e
    /// resolver dos ataques normais sem interromper seus estados proprios.
    /// </summary>
    public bool TryStartExternalAttack(PlayerAttackDefinition definition)
    {
        if (definition == null || IsAttacking ||
            CutsceneController.IsPlayerControlLocked ||
            !CanStartAttack(definition))
        {
            return false;
        }

        PlayerMovesetDefinition moveset = movesetController != null
            ? movesetController.Moveset : null;
        float rotationSpeed = moveset != null
            ? moveset.GetWindupActiveRotationSpeed(definition.kind)
            : 0f;
        float extendedArmRadius = moveset != null
            ? moveset.extendedArmHitboxRadius : .25f;
        float extendedArmHeightOffset = moveset != null
            ? moveset.extendedArmHitboxHeightOffset : 0f;

        EnemyBase assistedTarget = null;
        Vector3 assistedTargetPosition = default;
        targetAssist?.TryCaptureTarget(out assistedTarget,
            out assistedTargetPosition);

        // Somente a explosao de Burn recebe escala Heavy; sua carga especial
        // e capturada sem consumo. Outros eventos externos nao usam Heat.
        PlayerHeat attackHeat = definition.attackId == "BurnExplosion"
            ? heat : null;
        CurrentAttack = new PlayerAttackRuntime(definition, attackHeat,
            rotationSpeed, extendedArmRadius, extendedArmHeightOffset,
            false, assistedTarget, assistedTargetPosition);
        RecordAttackStart(definition);
        CurrentPhase = PlayerAttackPhase.Windup;
        phaseTimer = CurrentAttack.WindupDuration;
        finishAfterActive = false;
        isExternalAttack = true;
        attackMovement.Begin(CurrentAttack);
        hitboxes.Begin(CurrentAttack);
        resolver.BeginAttack();
        AttackStarted?.Invoke(CurrentAttack);
        if (phaseTimer <= 0f)
            ChangePhase(PlayerAttackPhase.Active);
        return true;
    }

    /// <summary>Indica se a definicao terminou seu cooldown individual.</summary>
    public bool CanStartAttack(PlayerAttackDefinition definition)
    {
        if (definition == null)
            return false;
        if (definition.cooldownDuration <= 0f)
            return true;
        return !attackReadyAt.TryGetValue(definition, out float readyAt) ||
            Time.time >= readyAt;
    }

    private void RecordAttackStart(PlayerAttackDefinition definition)
    {
        if (definition.cooldownDuration <= 0f)
        {
            attackReadyAt.Remove(definition);
            return;
        }

        // O cronometro comeca no inicio do ataque, junto ao primeiro frame do Windup.
        attackReadyAt[definition] = Time.time + definition.cooldownDuration;
    }

    public bool RequestCancel(PlayerAttackCancelAction action)
    {
        if (!IsAttacking) return true;
        if (!CanRequestCancel(action)) return false;

        PlayerMovesetDefinition moveset = movesetController != null
            ? movesetController.Moveset : null;
        System.Collections.Generic.List<PlayerAttackCancelRule> rules =
            moveset != null ? moveset.GetCancelRules(CurrentAttack.Definition.kind) : null;
        PlayerAttackCancelRule rule = rules?.Find(candidate =>
            candidate != null && candidate.action == action);
        if (rule == null) return false;
        if (CurrentPhase == PlayerAttackPhase.Active && rule.finishActiveBeforeCancelling)
        {
            finishAfterActive = true;
            return true;
        }
        FinishAttack(); return true;
    }

    /// <summary>Informa se a acao pode cancelar o ataque atual na fase presente.</summary>
    public bool CanRequestCancel(PlayerAttackCancelAction action)
    {
        if (!IsAttacking)
            return false;

        PlayerMovesetDefinition moveset = movesetController != null
            ? movesetController.Moveset : null;
        System.Collections.Generic.List<PlayerAttackCancelRule> rules =
            moveset != null
                ? moveset.GetCancelRules(CurrentAttack.Definition.kind)
                : null;
        PlayerAttackCancelRule rule = rules?.Find(candidate =>
            candidate != null && candidate.action == action);
        return rule != null &&
            (rule.allowedPhases & ToFlag(CurrentPhase)) != 0;
    }

    public void CancelAttack()
    {
        if (IsAttacking) FinishAttack(false);
    }

    private void ChangePhase(PlayerAttackPhase phase)
    {
        CurrentPhase = phase; phaseTimer = GetPhaseDuration(phase);
        if (phase == PlayerAttackPhase.Recovery) hitboxes.End();
        if (phaseTimer <= 0f && !(phase == PlayerAttackPhase.Active &&
            ShouldHoldActiveUntilGrounded()))
        {
            if (phase == PlayerAttackPhase.Active) ChangePhase(PlayerAttackPhase.Recovery);
            else if (phase == PlayerAttackPhase.Recovery) FinishAttack();
        }
    }

    private bool ShouldHoldActiveUntilGrounded() => CurrentAttack != null &&
        CurrentAttack.Definition.holdActiveUntilGrounded &&
        movement != null && !movement.IsGrounded;

    private Vector3 GetPlayerCenter()
    {
        PlayerCharacterController controller = movement != null
            ? movement.EntityCharacterController
            : GetComponent<PlayerCharacterController>();
        return transform.position + transform.rotation *
            (controller != null ? controller.center : Vector3.zero);
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
        bool finishedExternalAttack = isExternalAttack;
        if (!notify)
            attackAudio?.CancelAttackSounds(finished);
        CurrentAttack = null; CurrentPhase = PlayerAttackPhase.None; phaseTimer = 0f; finishAfterActive = false; isExternalAttack = false;
        hitboxes.End(); attackMovement.End();
        if (!finishedExternalAttack && playerState != null && playerState.IsAttacking()) playerState.ChangeState(PlayerState.State.Normal);
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

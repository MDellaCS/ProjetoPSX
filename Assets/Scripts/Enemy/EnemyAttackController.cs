using System;
using UnityEngine;

/// <summary>Orquestra o grafo, as fases e os sistemas desacoplados do ataque.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyAttackMovement))]
[RequireComponent(typeof(EnemyAttackHitboxController))]
[RequireComponent(typeof(EnemyAttackPreview))]
public sealed class EnemyAttackController : MonoBehaviour
{
    [Header("Moveset")]
    [Tooltip("Asset com aberturas, nodes, combos, mixups e configuracoes globais deste inimigo.")]
    [SerializeField] private EnemyMovesetDefinition moveset;

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyMovement locomotion;
    private EnemyAttackSelector selector;
    private EnemyAttackMovement authoredMovement;
    private EnemyAttackHitboxController hitboxes;
    private float phaseTimer;
    private float recoveryElapsed;
    private int attacksInSequence;
    private EnemyMoveNode pendingContinuation;
    private float pendingContinuationDelay;
    private string queuedSpecialAttackId;
    private bool approachHasStarted;
    private bool approachReachedPreferredRange;
    private float approachElapsed;

    public EnemyMovesetDefinition Moveset => moveset;
    public EnemyAttackRuntime CurrentAttack { get; private set; }
    public EnemyAttackPhase CurrentPhase { get; private set; }
    public event Action<EnemyAttackRuntime> AttackStarted;
    public event Action<EnemyAttackRuntime, EnemyAttackPhase> PhaseChanged;
    public event Action<EnemyAttackRuntime> AttackEnded;
    public float PhaseProgress => CurrentAttack == null ? 0f :
        Mathf.Clamp01(1f - phaseTimer /
            Mathf.Max(.0001f, GetPhaseDuration(CurrentPhase)));

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyMovement enemyMovement)
    {
        owner = enemy;
        brain = enemyBrain;
        locomotion = enemyMovement;
        if (!TryGetComponent(out authoredMovement))
            authoredMovement = gameObject.AddComponent<EnemyAttackMovement>();
        if (!TryGetComponent(out hitboxes))
            hitboxes = gameObject.AddComponent<EnemyAttackHitboxController>();
        authoredMovement.Initialize(owner, brain, locomotion);
        hitboxes.Initialize(owner);
        selector = new EnemyAttackSelector(owner, brain, locomotion, moveset);
    }

    public bool HasAvailableOpening() => selector != null && selector.HasOpening();

    public bool HasAvailableAttackWithTag(string tag) => selector != null &&
        selector.HasValidAttackWithTag(tag);

    public bool TryBeginAttackWithTag(string tag, float windupDuration)
    {
        EnemyMoveNode node = selector?.FindValidAttackWithTag(tag);
        if (node == null || !EnemyDirector.RequestAttackTurn(owner))
            return false;

        attacksInSequence = 0;
        if (BeginOrQueueAttack(node, windupDuration))
            return true;
        // O turno do Director foi reservado acima; sem ataque iniciado ele
        // ficaria preso ocupando o orcamento de Lead.
        EnemyDirector.ReleaseAttackTurn(owner);
        return false;
    }

    public bool TryBeginSelectedOpening()
    {
        if (selector == null || !selector.TrySelectOpening(out EnemyMoveNode node,
            out bool needsDirector))
            return false;
        if (needsDirector && !EnemyDirector.RequestAttackTurn(owner))
            return false;
        attacksInSequence = 0;
        if (BeginOrQueueAttack(node, null))
            return true;
        if (needsDirector)
            EnemyDirector.ReleaseAttackTurn(owner);
        return false;
    }

    private bool BeginOrQueueAttack(EnemyMoveNode node,
        float? windupDurationOverride)
    {
        string queueId = node.attack.specialQueueId;
        if (string.IsNullOrWhiteSpace(queueId))
        {
            BeginAttack(node, windupDurationOverride);
            return CurrentAttack != null;
        }

        queuedSpecialAttackId = queueId;
        bool queued = EnemySpecialAttackQueue.TryQueue(queueId, owner,
            EnemyDirector.SpecialAttackQueueDelayFrames,
            () => StartQueuedAttack(node, windupDurationOverride));
        if (!queued)
            queuedSpecialAttackId = null;
        return queued;
    }

    private bool StartQueuedAttack(EnemyMoveNode node,
        float? windupDurationOverride)
    {
        if (owner == null || owner.IsDead || node?.attack == null ||
            !selector.IsValid(node.attack))
            return false;

        BeginAttack(node, windupDurationOverride);
        return CurrentAttack != null;
    }

    private void BeginAttack(EnemyMoveNode node,
        float? windupDurationOverride = null)
    {
        if (node?.attack == null || moveset == null)
            return;
        CurrentAttack = new EnemyAttackRuntime(node, moveset,
            windupDurationOverride);
        CurrentPhase = EnemyAttackPhase.Windup;
        phaseTimer = Mathf.Max(0f, CurrentAttack.WindupDuration);
        recoveryElapsed = 0f;
        pendingContinuation = null;
        pendingContinuationDelay = 0f;
        approachHasStarted = false;
        approachReachedPreferredRange = false;
        approachElapsed = 0f;
        attacksInSequence++;
        authoredMovement.Begin(CurrentAttack);
        hitboxes.Begin(CurrentAttack);
        brain.OnAttackStarted();
        owner.NotifyAttackStarted(node.attack);
        AttackStarted?.Invoke(CurrentAttack);
        PhaseChanged?.Invoke(CurrentAttack, CurrentPhase);
        owner.CurrentState = EnemyBase.State.Windup;
        SkipEmptyPhases();
    }

    public void TickWindup()
    {
        if (CurrentAttack == null) return;
        if (CurrentAttack.Definition.movement.approachTargetUntilPreferredRange)
        {
            if (!approachHasStarted)
            {
                locomotion.Move(Vector3.zero);
                phaseTimer = Mathf.Max(0f, phaseTimer - Time.deltaTime);
                if (phaseTimer <= 0f)
                    approachHasStarted = true;
                return;
            }

            if (approachReachedPreferredRange)
            {
                locomotion.Move(Vector3.zero);
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                    EnterActive();
                return;
            }

            approachElapsed += Time.deltaTime;
            if (approachElapsed >= Mathf.Max(.1f,
                CurrentAttack.Definition.movement.approachTimeout))
            {
                CancelAttack();
                owner.CurrentState = EnemyBase.State.Combat;
                brain.ScheduleNextDecision();
                return;
            }

            if (brain.Target == null)
            {
                CancelAttack();
                owner.CurrentState = EnemyBase.State.Combat;
                brain.ScheduleNextDecision();
                return;
            }
            if (authoredMovement.TickApproachToPreferredRange())
            {
                approachReachedPreferredRange = true;
                phaseTimer = Mathf.Max(0f, CurrentAttack.WindupDuration);
            }
            return;
        }

        TickMovement();
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
        {
            TickMovementAtPhaseEnd();
            EnterActive();
        }
    }

    public void TickActive()
    {
        if (CurrentAttack == null) return;
        TickMovement();
        hitboxes.Tick(PhaseProgress);
        phaseTimer -= Time.deltaTime;
        bool endEarly = owner.ShouldEndAttackActivePhaseEarly(CurrentAttack);
        if (endEarly || phaseTimer <= 0f)
        {
            if (!endEarly && owner.ShouldHoldAttackActivePhase(CurrentAttack))
            {
                phaseTimer = 0f;
                return;
            }
            if (!endEarly)
                TickMovementAtPhaseEnd();
            hitboxes.Tick(1f);
            EnterRecovery();
        }
    }

    public void TickRecovery()
    {
        if (CurrentAttack == null) return;
        authoredMovement.TickRecovery();
        recoveryElapsed += Time.deltaTime;
        phaseTimer -= Time.deltaTime;
        if (pendingContinuation != null &&
            recoveryElapsed >= pendingContinuationDelay)
        {
            BeginContinuation();
            return;
        }
        if (phaseTimer <= 0f) FinishOrContinue();
    }

    private void TickMovement()
    {
        float windup = CurrentAttack.WindupDuration;
        float active = CurrentAttack.ActiveDuration;
        float elapsed = CurrentPhase == EnemyAttackPhase.Windup
            ? windup * PhaseProgress : windup + active * PhaseProgress;
        authoredMovement.Tick(CurrentPhase, PhaseProgress,
            elapsed / Mathf.Max(.0001f, windup + active));
    }

    private void TickMovementAtPhaseEnd()
    {
        float windup = CurrentAttack.WindupDuration;
        float active = CurrentAttack.ActiveDuration;
        float elapsed = CurrentPhase == EnemyAttackPhase.Windup
            ? windup : windup + active;
        authoredMovement.Tick(CurrentPhase, 1f,
            elapsed / Mathf.Max(.0001f, windup + active));
    }

    private void EnterActive()
    {
        CurrentPhase = EnemyAttackPhase.Active;
        phaseTimer = Mathf.Max(0f, CurrentAttack.ActiveDuration);
        owner.CurrentState = EnemyBase.State.Active;
        owner.NotifyAttackActive(CurrentAttack.Definition);
        PhaseChanged?.Invoke(CurrentAttack, CurrentPhase);
        SkipEmptyPhases();
    }

    private void EnterRecovery()
    {
        CurrentPhase = EnemyAttackPhase.Recovery;
        phaseTimer = Mathf.Max(0f, CurrentAttack.RecoveryDuration);
        recoveryElapsed = 0f;
        owner.CurrentState = EnemyBase.State.Recovery;
        owner.NotifyAttackRecoveryStarted(CurrentAttack.Definition);
        PhaseChanged?.Invoke(CurrentAttack, CurrentPhase);
        pendingContinuation = null;
        pendingContinuationDelay = 0f;
        if (attacksInSequence < Mathf.Max(1,
            moveset.maximumAttacksPerSequence))
            selector.TrySelectContinuation(CurrentAttack,
                out pendingContinuation, out pendingContinuationDelay);
        SkipEmptyPhases();
    }

    private void SkipEmptyPhases()
    {
        if (CurrentAttack == null || phaseTimer > 0f) return;
        if (CurrentPhase == EnemyAttackPhase.Windup &&
            CurrentAttack.Definition.movement.approachTargetUntilPreferredRange)
            return;
        if (CurrentPhase == EnemyAttackPhase.Windup) EnterActive();
        else if (CurrentPhase == EnemyAttackPhase.Active) EnterRecovery();
        else FinishOrContinue();
    }

    private void FinishOrContinue()
    {
        if (pendingContinuation != null &&
            recoveryElapsed >= pendingContinuationDelay)
        {
            BeginContinuation();
            return;
        }
        CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        brain.ScheduleNextDecision();
    }

    private void BeginContinuation()
    {
        EnemyMoveNode continuation = pendingContinuation;
        EnemyAttackRuntime ended = CurrentAttack;
        if (continuation?.attack == null || ended == null)
        {
            CancelAttack();
            owner.CurrentState = EnemyBase.State.Combat;
            brain.ScheduleNextDecision();
            return;
        }

        // Libera os recursos do ataque atual sem liberar o turno reservado;
        // a continuação continua pertencendo à mesma sequência e atacante.
        CurrentAttack = null;
        CurrentPhase = EnemyAttackPhase.None;
        phaseTimer = 0f;
        recoveryElapsed = 0f;
        if (!string.IsNullOrWhiteSpace(queuedSpecialAttackId))
        {
            EnemySpecialAttackQueue.Release(owner, queuedSpecialAttackId);
            queuedSpecialAttackId = null;
        }
        authoredMovement?.End();
        hitboxes?.End();
        owner.NotifyAttackEnded(ended.Definition);
        AttackEnded?.Invoke(ended);
        if (!BeginOrQueueAttack(continuation, null))
        {
            CancelAttack();
            owner.CurrentState = EnemyBase.State.Combat;
            brain.ScheduleNextDecision();
        }
    }

    public void CancelAttack()
    {
        EnemyAttackRuntime ended = CurrentAttack;
        if (!string.IsNullOrWhiteSpace(queuedSpecialAttackId))
        {
            EnemySpecialAttackQueue.Release(owner, queuedSpecialAttackId);
            queuedSpecialAttackId = null;
        }
        CurrentAttack = null;
        CurrentPhase = EnemyAttackPhase.None;
        phaseTimer = 0f;
        recoveryElapsed = 0f;
        attacksInSequence = 0;
        pendingContinuation = null;
        pendingContinuationDelay = 0f;
        approachHasStarted = false;
        approachReachedPreferredRange = false;
        approachElapsed = 0f;
        authoredMovement?.End();
        hitboxes?.End();
        if (ended != null)
        {
            owner.NotifyAttackEnded(ended.Definition);
            AttackEnded?.Invoke(ended);
        }
        EnemyDirector.ReleaseAttackTurn(owner);
    }

    internal void OnSpecialAttackQueueFailed(string queueId)
    {
        if (queuedSpecialAttackId == queueId)
            queuedSpecialAttackId = null;
        // A reserva falhou antes de o ataque comecar: devolve o turno do Director.
        if (CurrentAttack == null)
            EnemyDirector.ReleaseAttackTurn(owner);
    }

    private float GetPhaseDuration(EnemyAttackPhase phase) =>
        CurrentAttack == null ? 0f : phase switch
        {
            EnemyAttackPhase.Windup => CurrentAttack.WindupDuration,
            EnemyAttackPhase.Active => CurrentAttack.ActiveDuration,
            EnemyAttackPhase.Recovery => CurrentAttack.RecoveryDuration,
            _ => 0f
        };

#if UNITY_EDITOR
    public void EditorAssignMoveset(EnemyMovesetDefinition value) => moveset = value;
#endif
}

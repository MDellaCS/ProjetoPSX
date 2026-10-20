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

    public EnemyMovesetDefinition Moveset => moveset;
    public EnemyAttackRuntime CurrentAttack { get; private set; }
    public EnemyAttackPhase CurrentPhase { get; private set; }
    public event Action<EnemyAttackRuntime> AttackStarted;
    public event Action<EnemyAttackRuntime, EnemyAttackPhase> PhaseChanged;
    public event Action<EnemyAttackRuntime> AttackEnded;
    public float PhaseProgress => CurrentAttack == null ? 0f :
        1f - phaseTimer / Mathf.Max(.0001f, GetPhaseDuration(CurrentPhase));

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyMovement enemyMovement)
    {
        owner = enemy;
        brain = enemyBrain;
        locomotion = enemyMovement;
        authoredMovement = GetComponent<EnemyAttackMovement>() ??
            gameObject.AddComponent<EnemyAttackMovement>();
        hitboxes = GetComponent<EnemyAttackHitboxController>() ??
            gameObject.AddComponent<EnemyAttackHitboxController>();
        authoredMovement.Initialize(owner, brain, locomotion);
        hitboxes.Initialize(owner);
        selector = new EnemyAttackSelector(owner, brain, locomotion, moveset);
    }

    public bool HasAvailableOpening() => selector != null && selector.HasOpening();

    public bool TryBeginSelectedOpening()
    {
        if (selector == null || !selector.TrySelectOpening(out EnemyMoveNode node,
            out bool needsDirector))
            return false;
        if (needsDirector && !EnemyDirector.RequestAttackTurn(owner))
            return false;
        attacksInSequence = 0;
        BeginAttack(node);
        return true;
    }

    private void BeginAttack(EnemyMoveNode node)
    {
        if (node?.attack == null || moveset == null)
            return;
        CurrentAttack = new EnemyAttackRuntime(node, moveset);
        CurrentPhase = EnemyAttackPhase.Windup;
        phaseTimer = Mathf.Max(0f, CurrentAttack.WindupDuration);
        recoveryElapsed = 0f;
        pendingContinuation = null;
        pendingContinuationDelay = 0f;
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
        TickMovement();
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f) EnterActive();
    }

    public void TickActive()
    {
        if (CurrentAttack == null) return;
        TickMovement();
        hitboxes.Tick(PhaseProgress);
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
        {
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
            AttackEnded?.Invoke(CurrentAttack);
            BeginAttack(pendingContinuation);
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
        if (CurrentPhase == EnemyAttackPhase.Windup) EnterActive();
        else if (CurrentPhase == EnemyAttackPhase.Active) EnterRecovery();
        else FinishOrContinue();
    }

    private void FinishOrContinue()
    {
        if (pendingContinuation != null &&
            recoveryElapsed >= pendingContinuationDelay)
        {
            AttackEnded?.Invoke(CurrentAttack);
            BeginAttack(pendingContinuation);
            return;
        }
        CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        brain.ScheduleNextDecision();
    }

    public void CancelAttack()
    {
        EnemyAttackRuntime ended = CurrentAttack;
        CurrentAttack = null;
        CurrentPhase = EnemyAttackPhase.None;
        phaseTimer = 0f;
        recoveryElapsed = 0f;
        attacksInSequence = 0;
        pendingContinuation = null;
        pendingContinuationDelay = 0f;
        authoredMovement?.End();
        hitboxes?.End();
        if (ended != null)
            AttackEnded?.Invoke(ended);
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

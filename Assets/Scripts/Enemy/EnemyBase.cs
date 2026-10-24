using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Identidade e ciclo de vida do inimigo. Os sistemas de jogo ficam nos
/// componentes do mesmo GameObject; inimigos especificos sobrescrevem ganchos.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(EnemyBrain))]
[RequireComponent(typeof(EnemyAttackController))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyDebug))]
[RequireComponent(typeof(EnemyReactionController))]
public class EnemyBase : MonoBehaviour
{
    public enum State { Idle, Combat, Windup, Active, Recovery, Guarding, Dodging, Taunting, Staggered }

    private static readonly HashSet<EnemyBase> activeEnemies = new();
    public static event Action<EnemyBase> EnemyDied;
    public event Action<EnemyBase, int, int> HealthChanged;
    public event Action<EnemyBase> Died;

    private EnemyBrain brain;
    private EnemyAttackController attacks;
    private EnemyMovement movement;
    private EnemyHealth health;
    private EnemyDebug debugView;
    private EnemyGuard guard;
    private EnemyDodge dodge;
    private EnemyTaunt taunt;
    private EnemyReactionController reactions;
    private CharacterController characterController;
    private bool initialized;
    private bool isSpawning;

    public State CurrentState { get; internal set; } = State.Idle;
    public bool IsDead => health != null && health.IsDead;
    public bool IsInAttackSequence => CurrentState == State.Windup ||
        CurrentState == State.Active || CurrentState == State.Recovery;
    public bool IsLaunched => movement != null && movement.IsLaunched;
    public bool IsLaunchedForwards => movement != null && movement.IsLaunchedForwards;
    public bool CanBeLockOnTarget => isActiveAndEnabled && !IsDead &&
        brain != null && !brain.IsWaitingForEncounter;
    public bool IsSpawning => isSpawning;
    public int CurrentHealth => health != null ? health.CurrentHealth : 0;
    public int MaxHealth => health != null ? health.MaxHealth : 0;
    public float ImpactWeight => movement != null
        ? movement.ImpactWeight : 1f;
    public Vector3 LaunchForwardsVelocity => movement != null
        ? movement.LaunchForwardsVelocity : Vector3.zero;
    public float CurrentAggressionPercent => brain != null ? brain.CurrentAggressionPercent : 0f;
    public float DirectorWeight => brain != null ? brain.DirectorWeight : 100f;
    public Transform Target => brain != null ? brain.Target : null;
    public bool IsSearchingLastKnownPosition => brain != null && brain.IsSearchingLastKnownPosition;
    public bool IsGuarding => guard != null && guard.IsGuarding;
    /// <summary>
    /// Centro de combate do inimigo. Hitboxes e ataques sem um ponto visual
    /// explicito devem partir daqui, nao dos pes do Transform raiz.
    /// </summary>
    public Vector3 AttackOrigin
    {
        get
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            return characterController != null
                ? transform.TransformPoint(characterController.center)
                : transform.position;
        }
    }
    protected EnemyAttackDefinition CurrentAttack =>
        attacks != null ? attacks.CurrentAttack?.Definition : null;
    protected EnemyBrain Brain => brain;
    protected EnemyAttackController AttackController => attacks;
    protected EnemyMovement Movement => movement;
    protected EnemyHealth Health => health;

    private void Awake() => EnsureInitialized();

    private void EnsureInitialized()
    {
        if (initialized)
            return;
        brain = GetOrAdd<EnemyBrain>();
        attacks = GetOrAdd<EnemyAttackController>();
        movement = GetOrAdd<EnemyMovement>();
        health = GetOrAdd<EnemyHealth>();
        debugView = GetOrAdd<EnemyDebug>();
        characterController = GetOrAdd<CharacterController>();
        guard = GetComponent<EnemyGuard>();
        dodge = GetComponent<EnemyDodge>();
        taunt = GetComponent<EnemyTaunt>();
        reactions = GetOrAdd<EnemyReactionController>();

        movement.Initialize(this, health);
        attacks.Initialize(this, brain, movement);
        health.Initialize(this, brain, movement, attacks, guard, dodge);
        reactions.Initialize(this, health, movement);
        brain.Initialize(this, movement, attacks, health, guard);
        guard?.Initialize(this, brain, health, attacks, movement);
        dodge?.Initialize(this, brain, movement, attacks);
        taunt?.Initialize(this, brain, movement);
        debugView.Initialize(this, brain, attacks, movement, health);
        initialized = true;
    }

    private T GetOrAdd<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private void OnEnable() => activeEnemies.Add(this);
    private void OnDisable()
    {
        activeEnemies.Remove(this);
        EnemyDirector.ReleaseEnemy(this);
    }

    internal static IEnumerable<EnemyBase> ActiveEnemies => activeEnemies;
    internal bool HasAvailableOpening => attacks != null &&
        attacks.HasAvailableOpening();
    internal void CancelGuardForAirborne() => guard?.CancelForAirborne();
    internal void CancelGuardForDodge() => guard?.CancelForDodge();
    internal void EndStaggerForDodge() => health?.EndStaggerForDodge();
    internal void CancelDodge() => dodge?.CancelDodge();
    internal void CancelTaunt() => taunt?.CancelTaunt();
    internal bool TryAvoidPlayerAttack(float attackReach,
        int incomingDamage = 0) => dodge != null &&
        dodge.TryAvoidPlayerAttack(attackReach, incomingDamage);

    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.AreEnemiesFrozen ||
            IsDead || isSpawning ||
            brain.IsWaitingForEncounter ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        brain.ResolvePlayer();
        brain.TickAggression();
        guard?.TickCooldown();
        dodge?.TickTimers();
        taunt?.TickCooldown();
        health.TickHealth();
        health.TickPoise();
        reactions.Tick();
        UpdateVerticalMovement(movement);
        brain.TickCommitment();

        if ((movement.IsLaunched || movement.IsLaunchedForwards) &&
            CurrentState != State.Dodging)
        {
            movement.Move(Vector3.zero);
            return;
        }

        switch (CurrentState)
        {
            case State.Idle:
                brain.TickIdle();
                movement.Move(Vector3.zero);
                break;
            case State.Combat:
                brain.TickCombat();
                break;
            case State.Windup:
                attacks.TickWindup();
                break;
            case State.Active:
                attacks.TickActive();
                break;
            case State.Recovery:
                attacks.TickRecovery();
                if (CurrentState == State.Recovery)
                    movement.Move(Vector3.zero);
                break;
            case State.Guarding:
                guard?.TickGuard();
                movement.Move(guard != null
                    ? guard.GetGuardRetreatVelocity() : Vector3.zero);
                break;
            case State.Dodging:
                movement.Move(dodge != null
                    ? dodge.TickDodge() : Vector3.zero);
                break;
            case State.Taunting:
                taunt?.TickTaunt();
                if (CurrentState == State.Taunting)
                    movement.SetVerticalVelocity(0f);
                movement.Move(Vector3.zero);
                break;
            case State.Staggered:
                guard?.TickStaggerGuard();
                if (CurrentState == State.Staggered)
                {
                    health.TickStagger();
                    movement.Move(Vector3.zero);
                }
                break;
        }
    }

    // Ganchos especificos: a base conserva o comportamento generico.
    protected virtual void UpdateVerticalMovement(EnemyMovement locomotion) =>
        locomotion.ApplyGravity();
    protected virtual bool CanUseAttack(EnemyAttackDefinition attack) => true;
    protected virtual bool TryChooseSpecialCombatAction() => false;
    protected virtual void OnAttackStarted(EnemyAttackDefinition attack) { }
    protected virtual void OnAttackActiveStarted(EnemyAttackDefinition attack) { }

    internal bool CanSelectAttack(EnemyAttackDefinition attack) =>
        CanUseAttack(attack);
    internal bool ChooseSpecialCombatAction() => TryChooseSpecialCombatAction();
    internal void NotifyAttackStarted(EnemyAttackDefinition attack) =>
        OnAttackStarted(attack);
    internal void NotifyAttackActive(EnemyAttackDefinition attack) =>
        OnAttackActiveStarted(attack);

    public bool TakeDamage(int damage, Vector3 attackerPosition, int poiseDamage,
        out int damageDealt, out CombatPoiseHitResult poiseResult,
        bool fromPlayerAttack = false,
        PlayerAttackType playerAttackType = PlayerAttackType.None,
        float playerAttackRadius = 0f,
        Vector3 impactDirection = default,
        Vector3 impactPosition = default,
        bool hasImpactPosition = false) =>
        health.TakeDamage(damage, attackerPosition, poiseDamage, out damageDealt,
            out poiseResult,
            fromPlayerAttack, playerAttackType, playerAttackRadius,
            impactDirection, impactPosition, hasImpactPosition);
    public bool ApplyPoiseReaction(CombatPoiseHitResult poiseHit,
        CombatReactionRequest request) => reactions.Apply(poiseHit, request);
    public void ApplyLaunchUpHorizontalImpulse(Vector3 direction, float speed) =>
        movement.ApplyLaunchUpHorizontalImpulse(direction, speed);
    public bool ApplyBodyImpactLaunch(Vector3 direction, float speed,
        float deceleration) => movement != null &&
        movement.ApplyBodyImpactLaunch(direction, speed, deceleration);
    public void CancelSlideLaunchHorizontalImpulse() =>
        movement.CancelSlideLaunchHorizontalImpulse();
    public void SetEncounterEngaged(Transform player)
    {
        EnsureInitialized();
        brain.SetEncounterEngaged(player);
    }
    public void SetWaitingForEncounter()
    {
        EnsureInitialized();
        brain.SetWaitingForEncounter();
    }
    /// <summary>Impede IA, colisao e lock-on enquanto o efeito de spawn toca.</summary>
    public void SetSpawning(bool value)
    {
        EnsureInitialized();
        isSpawning = value;
    }
    public void SetUnkillable(bool value)
    {
        EnsureInitialized();
        health.SetUnkillable(value);
    }
    public string GetDebugStatusText() => debugView.GetStatusText();

    internal void RaiseHealthChanged() =>
        HealthChanged?.Invoke(this, CurrentHealth, MaxHealth);

    internal void RaiseDied()
    {
        Died?.Invoke(this);
        EnemyDied?.Invoke(this);
    }

    public static bool IsAnyEnemyAggroedOn(Transform player)
    {
        foreach (EnemyBase enemy in activeEnemies)
        {
            if (enemy != null && enemy.Target == player &&
                enemy.CurrentState != State.Idle && !enemy.IsDead)
                return true;
        }
        return false;
    }

    public static void NotifyPlayerDamagedByEnemy()
    {
        foreach (EnemyBase enemy in activeEnemies)
        {
            if (enemy != null)
                enemy.brain.ReactToPlayerDamagedByEnemy();
        }
    }

}

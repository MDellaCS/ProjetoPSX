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
    private CharacterController characterController;
    private bool initialized;

    public State CurrentState { get; internal set; } = State.Idle;
    public bool IsDead => health != null && health.IsDead;
    public bool IsInAttackSequence => CurrentState == State.Windup ||
        CurrentState == State.Active || CurrentState == State.Recovery;
    public bool CanBeLaunched => movement != null && movement.CanBeLaunched && !IsDead;
    public bool IsLaunched => movement != null && movement.IsLaunched;
    public bool IsPushed => movement != null && movement.IsPushed;
    public bool CanBeLockOnTarget => isActiveAndEnabled && !IsDead &&
        brain != null && !brain.IsWaitingForEncounter;
    public int CurrentHealth => health != null ? health.CurrentHealth : 0;
    public int MaxHealth => health != null ? health.MaxHealth : 0;
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
    protected EnemyAttackData CurrentAttack => attacks != null ? attacks.CurrentAttack : null;
    protected EnemyBrain Brain => brain;
    protected EnemyAttackController AttackController => attacks;
    protected EnemyMovement Movement => movement;
    protected EnemyHealth Health => health;

    protected virtual EnemyAttackData[] CreateDefaultAttacks() =>
        Array.Empty<EnemyAttackData>();

    protected virtual void Reset()
    {
        PopulateDefaultAttacks();
    }

    [ContextMenu("Preencher ataques padrao")]
    private void PopulateDefaultAttacks()
    {
        GetOrAdd<EnemyAttackController>().SetDefaultsIfEmpty(
            CreateDefaultAttacks());
    }

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

        movement.Initialize(this, health);
        attacks.Initialize(this, brain, CreateDefaultAttacks());
        health.Initialize(this, brain, movement, attacks, guard, dodge);
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
    internal bool TryAvoidPlayerAttack(float attackRadius,
        int incomingDamage = 0) => dodge != null &&
        dodge.TryAvoidPlayerAttack(attackRadius, incomingDamage);

    private void Update()
    {
        if (IsDead || brain.IsWaitingForEncounter ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        brain.ResolvePlayer();
        brain.TickAggression();
        guard?.TickCooldown();
        dodge?.TickTimers();
        taunt?.TickCooldown();
        health.TickPoise();
        UpdateVerticalMovement(movement);
        brain.TickCommitment();

        if ((movement.IsLaunched || movement.IsPushed) &&
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
                movement.FaceTarget();
                attacks.TickWindup();
                movement.Move(Vector3.zero);
                break;
            case State.Active:
                EnemyAttackData activeAttack = attacks.CurrentAttack;
                if (FaceTargetDuringActive(activeAttack))
                    movement.FaceTarget();
                movement.Move(GetActiveMovementVelocity(activeAttack));
                if (CurrentState == State.Active)
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
                    ? guard.GetGuardRetreatVelocity() : Vector3.zero,
                    rotateTankControlsTowardMovement: false);
                break;
            case State.Dodging:
                movement.Move(dodge != null
                    ? dodge.TickDodge() : Vector3.zero,
                    rotateTankControlsTowardMovement: false);
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
    protected virtual bool CanUseAttack(EnemyAttackData attack, int index) => true;
    protected virtual bool TryChooseSpecialCombatAction() => false;
    protected virtual void OnAttackStarted(EnemyAttackData attack) { }
    protected virtual void OnAttackActiveStarted(EnemyAttackData attack) { }
    protected virtual bool FaceTargetDuringActive(EnemyAttackData attack) => true;
    protected virtual Vector3 GetActiveMovementVelocity(EnemyAttackData attack) =>
        Vector3.zero;
    protected virtual bool ExecuteAttack(EnemyAttackData attack) =>
        attacks.ExecuteMeleeAttack(attack);

    internal bool CanSelectAttack(EnemyAttackData attack, int index) =>
        CanUseAttack(attack, index);
    internal bool ChooseSpecialCombatAction() => TryChooseSpecialCombatAction();
    internal void NotifyAttackStarted(EnemyAttackData attack) =>
        OnAttackStarted(attack);
    internal void NotifyAttackActive(EnemyAttackData attack) =>
        OnAttackActiveStarted(attack);
    internal bool PerformAttack(EnemyAttackData attack) => ExecuteAttack(attack);

    public bool TakeDamage(int damage, Vector3 attackerPosition, int poiseDamage,
        out int damageDealt,
        bool fromPlayerAttack = false,
        PlayerAttackType playerAttackType = PlayerAttackType.None,
        float playerAttackRadius = 0f) =>
        health.TakeDamage(damage, attackerPosition, poiseDamage, out damageDealt,
            fromPlayerAttack, playerAttackType, playerAttackRadius);
    public void ApplyStagger(Vector3 attackerPosition) =>
        health.ApplyStagger(attackerPosition);
    public void ApplyLaunchUp(float initialSpeed, float gravity) =>
        movement.ApplyLaunchUp(initialSpeed, gravity);
    public void ApplyLaunchUpHorizontalImpulse(Vector3 direction, float speed) =>
        movement.ApplyLaunchUpHorizontalImpulse(direction, speed);
    public void CancelSlideLaunchHorizontalImpulse() =>
        movement.CancelSlideLaunchHorizontalImpulse();
    public void ApplyPush(Vector3 attackerPosition, float force, float duration) =>
        movement.ApplyPush(attackerPosition, force, duration);
    public void ApplyAerialJuggleLight(float liftSpeed, float gravity,
        float gravityMultiplier, float duration) =>
        movement.ApplyAerialJuggleLight(liftSpeed, gravity, gravityMultiplier, duration);
    public void ApplyAerialDive(float fallSpeed) => movement.ApplyAerialDive(fallSpeed);
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

    protected virtual void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible)
            return;
        EnemyDebug view = debugView != null ? debugView : GetComponent<EnemyDebug>();
        view?.DrawGizmos();
    }
}

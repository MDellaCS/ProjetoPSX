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
    public enum State { Idle, Combat, Windup, Active, Recovery, Staggered }

    private static readonly HashSet<EnemyBase> activeEnemies = new();
    public static event Action<EnemyBase> EnemyDied;
    public event Action<EnemyBase, int, int> HealthChanged;
    public event Action<EnemyBase> Died;

    private EnemyBrain brain;
    private EnemyAttackController attacks;
    private EnemyMovement movement;
    private EnemyHealth health;
    private EnemyDebug debugView;
    private bool initialized;

    public State CurrentState { get; internal set; } = State.Idle;
    public bool IsDead => health != null && health.IsDead;
    public bool IsInAttackSequence => CurrentState == State.Windup ||
        CurrentState == State.Active || CurrentState == State.Recovery;
    public bool CanBeLaunched => movement != null && movement.CanBeLaunched && !IsDead;
    public bool IsLaunched => movement != null && movement.IsLaunched;
    public bool CanBeLockOnTarget => isActiveAndEnabled && !IsDead &&
        brain != null && !brain.IsWaitingForEncounter;
    public int CurrentHealth => health != null ? health.CurrentHealth : 0;
    public int MaxHealth => health != null ? health.MaxHealth : 0;
    public float CurrentAggressionPercent => brain != null ? brain.CurrentAggressionPercent : 0f;
    public Transform Target => brain != null ? brain.Target : null;
    public bool IsSearchingLastKnownPosition => brain != null && brain.IsSearchingLastKnownPosition;
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

        movement.Initialize(this, health);
        attacks.Initialize(this, brain, CreateDefaultAttacks());
        health.Initialize(this, brain, movement, attacks);
        brain.Initialize(this, movement, attacks, health);
        debugView.Initialize(this, brain, attacks, movement, health);
        initialized = true;
    }

    private T GetOrAdd<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private void OnEnable() => activeEnemies.Add(this);
    private void OnDisable() => activeEnemies.Remove(this);

    private void Update()
    {
        if (IsDead || brain.IsWaitingForEncounter ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        brain.ResolvePlayer();
        brain.TickAggression();
        health.TickPoise();
        UpdateVerticalMovement(movement);
        brain.TickCommitment();

        if (movement.IsLaunched || movement.IsPushed)
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
                movement.FaceTarget();
                attacks.TickActive();
                movement.Move(Vector3.zero);
                break;
            case State.Recovery:
                attacks.TickRecovery();
                if (CurrentState == State.Recovery)
                    movement.Move(Vector3.zero);
                break;
            case State.Staggered:
                health.TickStagger();
                movement.Move(Vector3.zero);
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

    public void TakeDamage(int damage, Vector3 attackerPosition, int poiseDamage,
        bool fromPlayerAttack = false) =>
        health.TakeDamage(damage, attackerPosition, poiseDamage, fromPlayerAttack);
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

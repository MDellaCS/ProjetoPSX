using UnityEngine;

/// <summary>Vida, Poise, Stagger, morte e feedback de dano do inimigo.</summary>
[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour
{
    private const float UnkillableHealthRegenPerSecond = 10000f;

    [Header("Health")]
    [Tooltip("Vida maxima do inimigo.")]
    [Min(1)] [SerializeField] private int maxHealth = 100;
    [Tooltip("Impede que a vida chegue a zero; util para bonecos de teste.")]
    [SerializeField] private bool isUnkillable;

    [Header("Poise")]
    [Tooltip("Poise necessario para causar Stagger.")]
    [Min(1)] [SerializeField] private int maxPoise = 60;

    [Header("Stagger")]
    [Tooltip("Tempo em que Stagger impede o inimigo de agir.")]
    [Min(0f)] [SerializeField] private float staggerDuration = .8f;
    [Tooltip("Forca horizontal aplicada ao receber Stagger.")]
    [Min(0f)] [SerializeField] private float staggerKnockbackForce = 8f;
    [Tooltip("Duracao do deslocamento causado por Stagger.")]
    [Min(0f)] [SerializeField] private float staggerKnockbackDuration = .2f;

    [Header("Visual Feedback")]
    [Tooltip("Tempo que o material visual pisca vermelho apos receber dano.")]
    [Min(0f)] [SerializeField] private float damageFlashDuration = .15f;

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyMovement movement;
    private EnemyAttackController attacks;
    private EnemyGuard guard;
    private EnemyDodge dodge;
    private EnemyGore gore;
    private Renderer enemyRenderer;
    private DamageNumberSpawner damageNumbers;
    private Color normalColor;
    private static readonly Color guardingColor = new(.55f, .2f, .8f, 1f);
    private readonly CombatPoiseState poise = new();
    private float staggerTimer;
    private bool isGuardBreakStagger;
    private bool isShowingGuardVisual;
    private float unkillableHealthRegenRemainder;

    public bool IsDead { get; private set; }
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public float CurrentPoise => poise.Current;
    public int MaxPoise => maxPoise;
    public float HealthPercent => maxHealth > 0
        ? Mathf.Clamp01((float)CurrentHealth / maxHealth) : 0f;

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyMovement locomotion, EnemyAttackController attackController,
        EnemyGuard enemyGuard, EnemyDodge enemyDodge)
    {
        owner = enemy;
        brain = enemyBrain;
        movement = locomotion;
        attacks = attackController;
        guard = enemyGuard;
        dodge = enemyDodge;
        gore = GetComponent<EnemyGore>();
        CurrentHealth = maxHealth;
        poise.Initialize(maxPoise);
        enemyRenderer = GetComponentInChildren<Renderer>();
        damageNumbers = GetComponent<DamageNumberSpawner>();
        if (damageNumbers == null)
            damageNumbers = gameObject.AddComponent<DamageNumberSpawner>();
        if (enemyRenderer != null)
            normalColor = enemyRenderer.material.color;
    }

    public void TickPoise()
    {
        bool reacting = owner.CurrentState == EnemyBase.State.Staggered ||
            movement.IsLaunched || movement.IsLaunchedForwards;
        poise.Tick(maxPoise, !reacting);
    }

    /// <summary>
    /// Mantem bonecos marcados como invenciveis prontos para teste. A taxa e
    /// fixa de 10.000 de vida por segundo e nunca ultrapassa a vida maxima.
    /// </summary>
    public void TickHealth()
    {
        if (!isUnkillable || IsDead || CurrentHealth >= maxHealth)
        {
            unkillableHealthRegenRemainder = 0f;
            return;
        }

        float healingWithRemainder = UnkillableHealthRegenPerSecond *
            Time.deltaTime + unkillableHealthRegenRemainder;
        int healing = Mathf.FloorToInt(healingWithRemainder);
        unkillableHealthRegenRemainder = healingWithRemainder - healing;
        if (healing <= 0)
            return;

        int healthBeforeHealing = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healing);
        if (CurrentHealth != healthBeforeHealing)
            owner.RaiseHealthChanged();
    }

    public bool TakeDamage(int damage, Vector3 attackerPosition, int poiseDamage,
        out int damageDealt, out CombatPoiseHitResult poiseResult,
        bool fromPlayerAttack = false,
        PlayerAttackType playerAttackType = PlayerAttackType.None,
        float playerAttackReach = 0f,
        Vector3 impactDirection = default,
        Vector3 impactPosition = default,
        bool hasImpactPosition = false,
        bool particlesUseRadialImpactDirection = false)
    {
        damageDealt = 0;
        poiseResult = default;
        if (IsDead) return false;
        if (fromPlayerAttack && dodge != null &&
            dodge.TryAvoidPlayerAttack(playerAttackReach, damage))
            return false;
        if (fromPlayerAttack)
            movement.CancelSlideLaunchHorizontalImpulse();
        guard?.TryBlock(playerAttackType, attackerPosition,
            ref damage, ref poiseDamage);
        int healthBeforeDamage = CurrentHealth;
        CurrentHealth -= damage;
        if (damage > 0)
            damageNumbers?.ShowDamage(damage);
        FlashDamage();
        poiseResult = poise.ReceiveHit(poiseDamage, maxPoise);
        if (isGuardBreakStagger)
            poiseResult = default;

        if (CurrentHealth <= 0)
        {
            if (isUnkillable)
                CurrentHealth = 1;
            else
            {
                CurrentHealth = 0;
                damageDealt = Mathf.Max(0, healthBeforeDamage - CurrentHealth);
                brain.OnDamaged(damageDealt);
                Vector3 resolvedDirection = ResolveImpactDirection(attackerPosition,
                    impactDirection);
                Vector3 particleDirection = ResolveParticleImpactDirection(
                    resolvedDirection, impactPosition, hasImpactPosition,
                    particlesUseRadialImpactDirection);
                owner.RaiseHealthChanged();
                CombatParticleSystem.ReportEnemyDamage(owner, damageDealt,
                    true, particleDirection, impactPosition, hasImpactPosition, gore);
                Die(damageDealt, resolvedDirection);
                return true;
            }
        }
        damageDealt = Mathf.Max(0, healthBeforeDamage - CurrentHealth);
        brain.OnDamaged(damageDealt);
        owner.RaiseHealthChanged();
        Vector3 resolvedHitDirection = ResolveImpactDirection(attackerPosition,
            impactDirection);
        CombatParticleSystem.ReportEnemyDamage(owner, damageDealt, false,
            ResolveParticleImpactDirection(resolvedHitDirection, impactPosition,
                hasImpactPosition, particlesUseRadialImpactDirection),
            impactPosition, hasImpactPosition, gore);
        return true;
    }

    public void ApplyPoiseStagger(Vector3 attackerPosition, bool superStagger)
    {
        if (IsDead) return;
        dodge?.CancelDodge();
        owner.CancelTaunt();
        bool wasAlreadyStaggered = owner.CurrentState == EnemyBase.State.Staggered;
        isGuardBreakStagger = false;
        movement.ApplyKnockback(attackerPosition, staggerKnockbackForce,
            staggerKnockbackDuration);
        attacks.CancelAttack();
        brain.CancelReposition();
        staggerTimer = staggerDuration * (superStagger ? 2f : 1f);
        owner.CurrentState = EnemyBase.State.Staggered;
        if (!wasAlreadyStaggered)
            guard?.NotifyStaggerStarted();
    }

    internal void EndStaggerForGuard()
    {
        staggerTimer = 0f;
        isGuardBreakStagger = false;
    }

    internal void EndStaggerForDodge()
    {
        staggerTimer = 0f;
        isGuardBreakStagger = false;
    }

    internal void ApplyGuardBreakStagger(Vector3 attackerPosition, float duration)
    {
        if (IsDead || movement.IsLaunched)
            return;

        movement.ApplyKnockback(attackerPosition, staggerKnockbackForce,
            staggerKnockbackDuration);
        attacks.CancelAttack();
        brain.CancelReposition();
        staggerTimer = Mathf.Max(0f, duration);
        isGuardBreakStagger = staggerTimer > 0f;
        owner.CurrentState = staggerTimer > 0f
            ? EnemyBase.State.Staggered : EnemyBase.State.Combat;
    }

    /// <summary>Aplica o Stagger fixo de uma colisao contra superficie.</summary>
    internal void ApplyStaticImpactStagger(float duration)
    {
        if (IsDead) return;
        dodge?.CancelDodge();
        owner.CancelTaunt();
        attacks.CancelAttack();
        brain.CancelReposition();
        staggerTimer = duration;
        owner.CurrentState = duration > 0f
            ? EnemyBase.State.Staggered : EnemyBase.State.Combat;
    }

    internal void TickStagger()
    {
        staggerTimer -= Time.deltaTime;
        if (staggerTimer > 0f) return;
        isGuardBreakStagger = false;
        owner.CurrentState = EnemyBase.State.Combat;
    }

    public void SetUnkillable(bool value) => isUnkillable = value;

    internal void SetGuardVisual(bool isGuarding)
    {
        isShowingGuardVisual = isGuarding;
        ResetVisualColor();
    }

    private Vector3 ResolveImpactDirection(Vector3 attackerPosition,
        Vector3 impactDirection)
    {
        impactDirection.y = 0f;
        if (impactDirection.sqrMagnitude > .0001f)
            return impactDirection.normalized;

        Vector3 fallbackDirection = transform.position - attackerPosition;
        fallbackDirection.y = 0f;
        return fallbackDirection.sqrMagnitude > .0001f
            ? fallbackDirection.normalized : transform.forward;
    }

    private Vector3 ResolveParticleImpactDirection(Vector3 fallbackDirection,
        Vector3 impactPosition, bool hasImpactPosition, bool useRadialDirection)
    {
        if (!useRadialDirection || !hasImpactPosition)
            return fallbackDirection;

        Vector3 radialDirection = transform.position - impactPosition;
        radialDirection.y = 0f;
        return radialDirection.sqrMagnitude > .0001f
            ? radialDirection.normalized
            : fallbackDirection;
    }

    private void Die(int lethalDamage, Vector3 impactDirection)
    {
        if (IsDead) return;
        IsDead = true;
        owner.RaiseDied();
        Destroy(gameObject);
    }

    private void FlashDamage()
    {
        if (enemyRenderer == null) return;
        enemyRenderer.material.color = Color.red;
        CancelInvoke(nameof(ResetVisualColor));
        Invoke(nameof(ResetVisualColor), damageFlashDuration);
    }

    private void ResetVisualColor()
    {
        if (enemyRenderer != null)
            enemyRenderer.material.color = isShowingGuardVisual
                ? guardingColor : normalColor;
    }
}

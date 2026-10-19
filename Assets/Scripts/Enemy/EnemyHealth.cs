using UnityEngine;

/// <summary>Vida, Poise, Stagger, morte e feedback de dano do inimigo.</summary>
[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Vida maxima do inimigo.")]
    [Min(1)] [SerializeField] private int maxHealth = 100;
    [Tooltip("Impede que a vida chegue a zero; util para bonecos de teste.")]
    [SerializeField] private bool isUnkillable;

    [Header("Poise")]
    [Tooltip("Poise necessario para causar Stagger.")]
    [Min(1)] [SerializeField] private int maxPoise = 60;
    [Tooltip("Tempo sem receber dano de Poise antes de iniciar a recuperacao.")]
    [Min(0f)] [SerializeField] private float poiseRecoveryDelay = 3f;
    [Tooltip("Poise recuperado por segundo apos o atraso.")]
    [Min(0f)] [SerializeField] private float poiseRecoveryPerSecond = 20f;

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
    private Renderer enemyRenderer;
    private DamageNumberSpawner damageNumbers;
    private Color normalColor;
    private static readonly Color guardingColor = new(.55f, .2f, .8f, 1f);
    private float currentPoise;
    private float timeSinceLastPoiseDamage;
    private float staggerTimer;
    private bool isGuardBreakStagger;
    private bool isShowingGuardVisual;

    public bool IsDead { get; private set; }
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public float CurrentPoise => currentPoise;
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
        CurrentHealth = maxHealth;
        currentPoise = maxPoise;
        enemyRenderer = GetComponentInChildren<Renderer>();
        damageNumbers = GetComponent<DamageNumberSpawner>();
        if (damageNumbers == null)
            damageNumbers = gameObject.AddComponent<DamageNumberSpawner>();
        if (enemyRenderer != null)
            normalColor = enemyRenderer.material.color;
    }

    public void TickPoise()
    {
        if (currentPoise >= maxPoise) return;
        timeSinceLastPoiseDamage += Time.deltaTime;
        if (timeSinceLastPoiseDamage >= poiseRecoveryDelay)
            currentPoise = Mathf.Min(maxPoise,
                currentPoise + poiseRecoveryPerSecond * Time.deltaTime);
    }

    public bool TakeDamage(int damage, Vector3 attackerPosition, int poiseDamage,
        out int damageDealt,
        bool fromPlayerAttack = false,
        PlayerAttackType playerAttackType = PlayerAttackType.None,
        float playerAttackRadius = 0f)
    {
        damageDealt = 0;
        if (IsDead) return false;
        if (fromPlayerAttack && dodge != null &&
            dodge.TryAvoidPlayerAttack(playerAttackRadius, damage))
            return false;
        if (fromPlayerAttack)
            movement.CancelSlideLaunchHorizontalImpulse();
        guard?.TryBlock(playerAttackType, attackerPosition,
            ref damage, ref poiseDamage);
        damageDealt = Mathf.Max(0, damage);
        CurrentHealth -= damage;
        damageNumbers?.ShowDamage(damage);
        brain.OnDamaged();
        FlashDamage();
        ApplyPoiseDamage(poiseDamage, attackerPosition);

        if (CurrentHealth <= 0)
        {
            if (isUnkillable)
                CurrentHealth = 1;
            else
            {
                CurrentHealth = 0;
                owner.RaiseHealthChanged();
                Die();
                return true;
            }
        }
        owner.RaiseHealthChanged();
        return true;
    }

    private void ApplyPoiseDamage(int amount, Vector3 attackerPosition)
    {
        if (amount <= 0) return;
        currentPoise -= amount;
        timeSinceLastPoiseDamage = 0f;
        if (currentPoise > 0f || isGuardBreakStagger) return;
        ApplyStagger(attackerPosition);
        currentPoise = maxPoise;
    }

    public void ApplyStagger(Vector3 attackerPosition)
    {
        if (IsDead || movement.IsLaunched) return;
        dodge?.CancelDodge();
        owner.CancelTaunt();
        bool wasAlreadyStaggered = owner.CurrentState == EnemyBase.State.Staggered;
        isGuardBreakStagger = false;
        movement.ApplyKnockback(attackerPosition, staggerKnockbackForce,
            staggerKnockbackDuration);
        attacks.CancelAttack();
        brain.CancelReposition();
        staggerTimer = staggerDuration;
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

    internal void ApplyWallSplatStagger(float duration)
    {
        if (IsDead) return;
        dodge?.CancelDodge();
        owner.CancelTaunt();
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

    private void Die()
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

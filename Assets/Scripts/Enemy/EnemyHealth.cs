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
    private Renderer enemyRenderer;
    private DamageNumberSpawner damageNumbers;
    private Color normalColor;
    private float currentPoise;
    private float timeSinceLastPoiseDamage;
    private float staggerTimer;

    public bool IsDead { get; private set; }
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public float CurrentPoise => currentPoise;
    public int MaxPoise => maxPoise;
    public float HealthPercent => maxHealth > 0
        ? Mathf.Clamp01((float)CurrentHealth / maxHealth) : 0f;

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyMovement locomotion, EnemyAttackController attackController)
    {
        owner = enemy;
        brain = enemyBrain;
        movement = locomotion;
        attacks = attackController;
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

    public void TakeDamage(int damage, Vector3 attackerPosition, int poiseDamage,
        bool fromPlayerAttack = false)
    {
        if (IsDead) return;
        if (fromPlayerAttack)
            movement.CancelSlideLaunchHorizontalImpulse();
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
                return;
            }
        }
        owner.RaiseHealthChanged();
    }

    private void ApplyPoiseDamage(int amount, Vector3 attackerPosition)
    {
        if (amount <= 0) return;
        currentPoise -= amount;
        timeSinceLastPoiseDamage = 0f;
        if (currentPoise > 0f) return;
        ApplyStagger(attackerPosition);
        currentPoise = maxPoise;
    }

    public void ApplyStagger(Vector3 attackerPosition)
    {
        if (IsDead || movement.IsLaunched) return;
        movement.ApplyKnockback(attackerPosition, staggerKnockbackForce,
            staggerKnockbackDuration);
        attacks.CancelAttack();
        brain.CancelReposition();
        staggerTimer = staggerDuration;
        owner.CurrentState = EnemyBase.State.Staggered;
    }

    internal void ApplyWallSplatStagger(float duration)
    {
        if (IsDead) return;
        staggerTimer = duration;
        owner.CurrentState = duration > 0f
            ? EnemyBase.State.Staggered : EnemyBase.State.Combat;
    }

    internal void TickStagger()
    {
        staggerTimer -= Time.deltaTime;
        if (staggerTimer > 0f) return;
        owner.CurrentState = EnemyBase.State.Combat;
    }

    public void SetUnkillable(bool value) => isUnkillable = value;

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
            enemyRenderer.material.color = normalColor;
    }
}

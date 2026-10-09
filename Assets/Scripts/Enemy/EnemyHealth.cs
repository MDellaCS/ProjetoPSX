using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public static event Action<EnemyHealth> EnemyDied;
    public event Action<EnemyHealth, int, int> HealthChanged;
    public event Action<EnemyHealth> Died;

    public int maxHealth = 100;

    [HideInInspector]
    public bool isUnkillable;

    [Header("Launch Up")]
    [Tooltip("Desmarque para inimigos que nao podem ser lancados para cima.")]
    public bool canBeLaunched = true;

    [Header("Enemy Juggle")]
    [Tooltip("Desmarque para inimigos que podem receber Launch Up, mas nao acompanham os ataques aereos do jogador.")]
    public bool canBeJuggled = true;

    private int currentHealth;
    private DamageNumberSpawner damageNumberSpawner;

    private Renderer enemyRenderer;
    private Color normalColor;

    public float damageFlashDuration = 0.15f;

    private EnemyMovement enemyMovement;
    private EnemyCombat enemyCombat;

    [Header("Poise")]
    [Min(1)]
    public int maxPoise = 60;

    [Min(0f)]
    public float poiseRecoveryDelay = 3f;

    [Min(0f)]
    public float poiseRecoveryPerSecond = 20f;

    private float currentPoise;
    private float timeSinceLastPoiseDamage;
    private bool hasDied;

    public bool IsDead => hasDied;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        currentPoise = maxPoise;

        enemyRenderer = GetComponent<Renderer>();
        normalColor = enemyRenderer.material.color;

        enemyMovement = GetComponent<EnemyMovement>();
        enemyCombat = GetComponent<EnemyCombat>();

        damageNumberSpawner = GetComponent<DamageNumberSpawner>();
    }

    void Update()
    {
        if (currentPoise >= maxPoise)
            return;

        timeSinceLastPoiseDamage += Time.deltaTime;

        if (timeSinceLastPoiseDamage < poiseRecoveryDelay)
            return;

        currentPoise = Mathf.Min(
            maxPoise,
            currentPoise + poiseRecoveryPerSecond * Time.deltaTime
        );
    }

    public void TakeDamage(
        int damage,
        Vector3 attackerPosition,
        int staggerDamage
    )
    {
        currentHealth -= damage;

        damageNumberSpawner?.ShowDamage(damage);

        FlashDamage();

        ApplyPoiseDamage(staggerDamage, attackerPosition);

        if (currentHealth <= 0)
        {
            if (isUnkillable)
            {
                currentHealth = 1;
                HealthChanged?.Invoke(this, currentHealth, maxHealth);
                return;
            }

            HealthChanged?.Invoke(this, 0, maxHealth);
            Die();
            return;
        }

        HealthChanged?.Invoke(this, currentHealth, maxHealth);
    }

    private void ApplyPoiseDamage(int amount, Vector3 attackerPosition)
    {
        if (amount <= 0)
            return;

        currentPoise -= amount;
        timeSinceLastPoiseDamage = 0f;

        if (currentPoise > 0f)
            return;

        enemyCombat.ApplyStagger(attackerPosition);

        currentPoise = maxPoise;
    }

    void FlashDamage()
    {
        enemyRenderer.material.color = Color.red;

        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), damageFlashDuration);
    }

    void ResetColor()
    {
        enemyRenderer.material.color = normalColor;
    }

    void Die()
    {
        if (hasDied)
            return;

        hasDied = true;
        Died?.Invoke(this);
        EnemyDied?.Invoke(this);
        Debug.Log("Inimigo morreu!");
        Destroy(gameObject);
    }

    public void SetUnkillable(bool value)
    {
        isUnkillable = value;
    }

    public void ApplyLaunchUp(float initialSpeed, float gravityMagnitude)
    {
        if (!canBeLaunched)
            return;

        enemyCombat?.InterruptForLaunch();
        enemyMovement?.ApplyLaunchUp(initialSpeed, gravityMagnitude);
    }

    public void ApplyAerialJuggleLight(
        float liftSpeed,
        float gravityMagnitude,
        float gravityMultiplier,
        float duration)
    {
        if (!canBeJuggled ||
            enemyMovement == null ||
            !enemyMovement.IsLaunched)
            return;

        enemyCombat?.InterruptForLaunch();
        enemyMovement?.BeginAerialJuggleLight(
            liftSpeed,
            gravityMagnitude,
            gravityMultiplier,
            duration
        );
    }

    public void ApplyAerialJuggleDive(float fallSpeed)
    {
        if (!canBeJuggled ||
            enemyMovement == null ||
            !enemyMovement.IsLaunched)
            return;

        enemyCombat?.InterruptForLaunch();
        enemyMovement?.BeginAerialJuggleDive(fallSpeed);
    }
}

using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;

    [HideInInspector]
    public bool isUnkillable;

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

    void Start()
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

        Debug.Log("Inimigo tomou " + damage + " de dano!");

        FlashDamage();

        enemyCombat.RegisterDamageDuringStun(damage);

        // CameraFollow.Instance.ImpactShake(knockbackDirection);
        // HitStopManager.Instance.HitStop();
        ApplyPoiseDamage(staggerDamage, attackerPosition);

        if (currentHealth <= 0)
        {
            if (isUnkillable)
            {
                currentHealth = 1;
                return;
            }

            Die();
        }
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
        Debug.Log("Inimigo morreu!");
        Destroy(gameObject);
    }

    public void SetUnkillable(bool value)
    {
        isUnkillable = value;
    }
}

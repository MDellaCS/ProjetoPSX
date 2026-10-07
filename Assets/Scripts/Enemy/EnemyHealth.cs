using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;

    public float knockbackForce = 8f;
    public float knockbackDuration = 0.2f;

    private int currentHealth;
    private DamageNumberSpawner damageNumberSpawner;

    private Renderer enemyRenderer;
    private Color normalColor;

    public float damageFlashDuration = 0.15f;

    private EnemyMovement enemyMovement;
    private EnemyCombat enemyCombat;

    void Start()
    {
        currentHealth = maxHealth;

        enemyRenderer = GetComponent<Renderer>();
        normalColor = enemyRenderer.material.color;

        enemyMovement = GetComponent<EnemyMovement>();
        enemyCombat = GetComponent<EnemyCombat>();

        damageNumberSpawner = GetComponent<DamageNumberSpawner>();
    }

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        currentHealth -= damage;

        damageNumberSpawner?.ShowDamage(damage);

        Debug.Log("Inimigo tomou " + damage + " de dano!");

        FlashDamage();

        Vector3 knockbackDirection = transform.position - attackerPosition;

        knockbackDirection.y = 0f;
        knockbackDirection.Normalize();

        enemyMovement.ApplyKnockback(
            knockbackDirection,
            knockbackForce,
            knockbackDuration
        );

        // CameraFollow.Instance.ImpactShake(knockbackDirection);
        // HitStopManager.Instance.HitStop();
        enemyCombat.ApplyStagger();

        if (currentHealth <= 0)
        {
            Die();
        }
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
}
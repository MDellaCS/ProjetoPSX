using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    public float damageFlashDuration = 0.15f;

    private Renderer enemyRenderer;
    private Color normalColor;

    void Start()
    {
        currentHealth = maxHealth;
        enemyRenderer = GetComponent<Renderer>();
        normalColor = enemyRenderer.material.color;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        FlashDamage();

        Debug.Log("Inimigo tomou " + damage + " de dano!");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Inimigo morreu!");

        Destroy(gameObject);
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
}
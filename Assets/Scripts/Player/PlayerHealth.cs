using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public float successfulDodgeFlashDuration = 0.15f;

    private int currentHealth;
    private bool isInvulnerable;

    private Renderer playerRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        playerRenderer = GetComponent<Renderer>();
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable)
        {
            playerRenderer.material.color = Color.magenta;

            CancelInvoke(nameof(ReturnToDodgeColor));
            Invoke(nameof(ReturnToDodgeColor), successfulDodgeFlashDuration);

            return;
        }

        currentHealth -= damage;

        Debug.Log("Player tomou " + damage + " de dano!");
        Debug.Log("Vida: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ReturnToDodgeColor()
    {
        if (isInvulnerable)
        {
            playerRenderer.material.color = Color.green;
        }
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    void Die()
    {
        Debug.Log("Player morreu!");
    }
}
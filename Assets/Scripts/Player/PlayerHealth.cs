using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public float successfulDodgeFlashDuration = 0.15f;

    private int currentHealth;
    private bool isInvulnerable;

    private Renderer playerRenderer;
    public float staggerDuration = 0.8f;

    private bool isStaggered;
    private float staggerTimer;
    private Color normalColor;
    private PlayerCombat playerCombat;

    void Start()
    {
        currentHealth = maxHealth;
        playerRenderer = GetComponent<Renderer>();
        normalColor = playerRenderer.material.color;
        playerCombat = GetComponent<PlayerCombat>();
    }

    void Update()
    {
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;

            if (staggerTimer <= 0f)
            {
                EndStagger();
            }
        }
    }

    void StartStagger()
    {
        isStaggered = true;
        staggerTimer = staggerDuration;

        playerRenderer.material.color = Color.yellow;

        playerCombat.CancelAttack();
    }

    void EndStagger()
    {
        isStaggered = false;
        playerRenderer.material.color = normalColor;
    }

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (isInvulnerable)
        {
            playerRenderer.material.color = Color.magenta;

            CancelInvoke(nameof(ReturnToDodgeColor));
            Invoke(nameof(ReturnToDodgeColor), successfulDodgeFlashDuration);

            return;
        }

        currentHealth -= damage;

        Vector3 knockbackDirection = transform.position - attackerPosition;

        knockbackDirection.y = 0f;
        knockbackDirection.Normalize();

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();

        playerMovement.ApplyKnockback(
            knockbackDirection,
            8f,
            0.2f
        );

        Debug.Log("Player tomou " + damage + " de dano!");
        Debug.Log("Vida: " + currentHealth + "/" + maxHealth);

        CameraFollow.Instance.ImpactShake(knockbackDirection);
        HitStopManager.Instance.HitStop();
        StartStagger();

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

    public bool IsStaggered
    {
        get { return isStaggered; }
    }
}
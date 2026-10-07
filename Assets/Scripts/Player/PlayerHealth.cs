using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public float successfulDodgeFlashDuration = 0.15f;

    private int currentHealth;
    private bool isInvulnerable;

    private Renderer playerRenderer;
    private PlayerHeavyCombo playerHeavyCombo;
    public float staggerDuration = 0.8f;

    private PlayerState playerState;
    private PlayerLightCombo playerLightCombo;
    private float staggerTimer;
    private Color normalColor;
    private PlayerCombat playerCombat;

    void Start()
    {
        currentHealth = maxHealth;
        playerRenderer = GetComponent<Renderer>();
        normalColor = playerRenderer.material.color;
        playerCombat = GetComponent<PlayerCombat>();
        playerState = GetComponent<PlayerState>();
        playerLightCombo = GetComponent<PlayerLightCombo>();
        playerHeavyCombo = GetComponent<PlayerHeavyCombo>();
    }

    void Update()
    {
        if (playerState.IsStaggered())
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
        playerState.ChangeState(PlayerState.State.Stagger);

        staggerTimer = staggerDuration;

        playerRenderer.material.color = Color.yellow;

        playerCombat.CancelAttack();
        playerLightCombo.CancelCombo();
        playerHeavyCombo.CancelCombo();
    }

    void EndStagger()
    {
        playerRenderer.material.color = normalColor;

        playerState.ChangeState(PlayerState.State.Normal);
    }

    public void TakeDamage(
        int damage,
        Vector3 attackerPosition,
        float knockbackForce,
        float knockbackDuration
    )
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
            knockbackForce,
            knockbackDuration
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

        playerState.ChangeState(PlayerState.State.Dead);
    }
}
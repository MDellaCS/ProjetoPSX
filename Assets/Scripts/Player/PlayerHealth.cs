using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float successfulDodgeFlashDuration = 0.15f;
    public float staggerDuration = 0.8f;

    private bool isInvulnerable;

    private Renderer playerRenderer;

    private PlayerState playerState;
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;

    private PlayerLightCombo playerLightCombo;
    private PlayerHeavyCombo playerHeavyCombo;

    private PlayerHeat playerHeat;

    private float staggerTimer;
    private Color normalColor;

    void Start()
    {
        playerRenderer = GetComponent<Renderer>();
        normalColor = playerRenderer.material.color;

        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();

        playerLightCombo = GetComponent<PlayerLightCombo>();
        playerHeavyCombo = GetComponent<PlayerHeavyCombo>();

        playerHeat = GetComponent<PlayerHeat>();
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
        playerState.ChangeState(
            PlayerState.State.Stagger
        );

        staggerTimer = staggerDuration;

        playerRenderer.material.color =
            Color.yellow;

        playerCombat.CancelAttack();
        playerLightCombo.CancelCombo();
        playerHeavyCombo.CancelCombo();
    }

    void EndStagger()
    {
        // Se o dano levou o jogador à morte,
        // não podemos voltar para Normal.
        if (playerState.IsDead())
            return;

        playerRenderer.material.color =
            normalColor;

        playerState.ChangeState(
            PlayerState.State.Normal
        );
    }

    public void TakeDamage(
        int damage,
        Vector3 attackerPosition,
        float knockbackForce,
        float knockbackDuration
    )
    {
        if (playerState.IsDead())
            return;

        // =========================
        // DODGE / INVULNERABILIDADE
        // =========================

        if (isInvulnerable)
        {
            playerRenderer.material.color =
                Color.magenta;

            CancelInvoke(
                nameof(ReturnToDodgeColor)
            );

            Invoke(
                nameof(ReturnToDodgeColor),
                successfulDodgeFlashDuration
            );

            return;
        }

        // =========================
        // DANO = PERDA DE HEAT
        // =========================

        playerHeat.RemoveHeat(damage);

        // Se chegamos a -1000,
        // PlayerHeat já colocou o jogador
        // no estado Dead.
        if (playerState.IsDead())
        {
            Debug.Log(
                "Dano recebido: " +
                damage +
                " Heat"
            );

            return;
        }

        // =========================
        // KNOCKBACK
        // =========================

        Vector3 knockbackDirection =
            transform.position -
            attackerPosition;

        knockbackDirection.y = 0f;

        knockbackDirection.Normalize();

        playerMovement.ApplyKnockback(
            knockbackDirection,
            knockbackForce,
            knockbackDuration
        );

        Debug.Log(
            "Player perdeu " +
            damage +
            " Heat!"
        );

        Debug.Log(
            "Heat atual: " +
            playerHeat.CurrentHeat
        );

        // =========================
        // IMPACTO
        // =========================

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ImpactShake(
                knockbackDirection
            );
        }

        if (HitStopManager.Instance != null)
        {
            HitStopManager.Instance.HitStop();
        }

        // =========================
        // STAGGER
        // =========================

        StartStagger();
    }

    void ReturnToDodgeColor()
    {
        if (isInvulnerable)
        {
            playerRenderer.material.color =
                Color.green;
        }
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }
}
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float staggerDuration = 0.8f;

    private Renderer playerRenderer;

    private PlayerState playerState;
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private PlayerComboController comboController;

    private PlayerHeat playerHeat;

    private float staggerTimer;
    private Color normalColor;

    void Start()
    {
        playerRenderer = GetComponent<Renderer>();
        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        comboController = PlayerComboController.GetOrAdd(gameObject);
        playerHeat = GetComponent<PlayerHeat>();

        normalColor = playerRenderer.material.color;
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
        comboController.CancelCombo();
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
        float knockbackDuration,
        bool causedByEnemy = false
    )
    {
        if (playerState.IsDead())
            return;

        // Um ataque inimigo conectado mantém o jogador em combate.
        if (causedByEnemy)
        {
            playerCombat.RegisterDamageTakenFromEnemy();
        }

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

}

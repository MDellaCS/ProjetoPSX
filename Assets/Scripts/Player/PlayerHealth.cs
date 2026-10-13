using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float staggerDuration = 0.8f;

    [Header("Damage Numbers")]
    [SerializeField] private Vector3 damageNumberOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private Color incomingDamageNumberColor = new Color(1f, .3f, .3f);

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
        // O renderer pertence ao VisualRoot/filho, nao a raiz que concentra
        // a simulacao. Esta referencia e apenas feedback visual opcional.
        playerRenderer = GetComponentInChildren<Renderer>(true);
        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        comboController = PlayerComboController.GetOrAdd(gameObject);
        playerHeat = GetComponent<PlayerHeat>();

        if (playerRenderer != null)
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

        if (playerRenderer != null)
            playerRenderer.material.color = Color.yellow;

        playerCombat.CancelAttack();
        comboController.CancelCombo();
    }

    void EndStagger()
    {
        // Se o dano levou o jogador à morte,
        // não podemos voltar para Normal.
        if (playerState.IsDead())
            return;

        if (playerRenderer != null)
            playerRenderer.material.color = normalColor;

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

        DamageNumber.Show(
            transform,
            damage,
            damageNumberOffset,
            incomingDamageNumberColor
        );

        playerHeat.RemoveHeat(damage);

        // Se chegamos a -1000,
        // PlayerHeat já colocou o jogador
        // no estado Dead.
        if (playerState.IsDead())
            return;

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

        // =========================
        // IMPACTO
        // =========================

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ImpactShake(
                knockbackDirection
            );
        }

        // =========================
        // STAGGER
        // =========================

        StartStagger();
    }

}

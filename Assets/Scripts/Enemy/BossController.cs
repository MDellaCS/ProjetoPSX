using UnityEngine;

/// <summary>
/// Adiciona identidade de chefe a um inimigo normal: repertorio proprio e HUD.
/// A maquina de estados continua pertencendo ao EnemyCombat.
/// </summary>
[RequireComponent(typeof(EnemyCombat), typeof(EnemyHealth))]
public class BossController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string bossName = "The Warden";

    [Header("HUD")]
    [Tooltip("Opcional. Se ficar vazio, o boss encontra automaticamente o BossHealthHUD da cena.")]
    [SerializeField] private BossHealthHUD bossHealthHUD;

    [Header("Boss Attacks")]
    [Tooltip("Cada ataque usa pesos para perto, medio e longe. Os ataques invalidos para a distancia sao ignorados.")]
    [SerializeField] private EnemyAttackOption[] bossAttacks =
    {
        new EnemyAttackOption
        {
            attack = new EnemyAttackData
            {
                attackName = "Boss Quick Slash", damage = 20, maximumRange = 2.6f,
                windupDuration = .18f, activeDuration = .16f, recoveryDuration = .3f,
                hitboxDistance = 1.35f, hitboxRadius = .9f,
                knockbackForce = 4f, knockbackDuration = .12f
            },
            closeWeight = 45f, mediumWeight = 12f, farWeight = 0f
        },
        new EnemyAttackOption
        {
            attack = new EnemyAttackData
            {
                attackName = "Boss Heavy Strike", damage = 52, maximumRange = 3.1f,
                windupDuration = .72f, activeDuration = .24f, recoveryDuration = .85f,
                hitboxDistance = 1.8f, hitboxRadius = 1.15f,
                knockbackForce = 10f, knockbackDuration = .25f
            },
            closeWeight = 25f, mediumWeight = 28f, farWeight = 0f
        },
        new EnemyAttackOption
        {
            attack = new EnemyAttackData
            {
                attackName = "Boss Charge", damage = 36, minimumRange = 2f, maximumRange = 9f,
                windupDuration = .65f, activeDuration = .5f, recoveryDuration = 1.05f,
                hitboxDistance = 1.35f, hitboxRadius = 1f,
                knockbackForce = 11f, knockbackDuration = .28f,
                movesForward = true, movementSpeed = 11f
            },
            closeWeight = 5f, mediumWeight = 35f, farWeight = 65f
        },
        new EnemyAttackOption
        {
            attack = new EnemyAttackData
            {
                attackName = "Boss Wide Sweep", damage = 32, maximumRange = 3.6f,
                windupDuration = .5f, activeDuration = .28f, recoveryDuration = .55f,
                hitboxDistance = 1.7f, hitboxRadius = 1.8f,
                knockbackForce = 7f, knockbackDuration = .18f
            },
            closeWeight = 22f, mediumWeight = 35f, farWeight = 0f
        },
        new EnemyAttackOption
        {
            attack = new EnemyAttackData
            {
                attackName = "Boss Slam", damage = 65, maximumRange = 3.8f,
                windupDuration = 1f, activeDuration = .32f, recoveryDuration = 1.2f,
                hitboxDistance = 1.9f, hitboxRadius = 1.45f,
                knockbackForce = 13f, knockbackDuration = .35f
            },
            closeWeight = 8f, mediumWeight = 20f, farWeight = 0f
        }
    };

    private EnemyCombat enemyCombat;
    private EnemyHealth enemyHealth;

    public string BossName => bossName;

    private void Awake()
    {
        enemyCombat = GetComponent<EnemyCombat>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCombat.SetCustomAttackOptions(bossAttacks);
    }

    private void OnEnable()
    {
        ResolveBossHealthHUD();
        bossHealthHUD?.Show(this);
    }

    private void OnDisable()
    {
        bossHealthHUD?.Hide(this);
    }

    private void Start()
    {
        if (enemyHealth != null)
            enemyHealth.Died += HandleDeath;
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
            enemyHealth.Died -= HandleDeath;
    }

    private void HandleDeath(EnemyHealth _)
    {
        bossHealthHUD?.Hide(this);
    }

    private void ResolveBossHealthHUD()
    {
        if (bossHealthHUD != null)
            return;

        bossHealthHUD = FindAnyObjectByType<BossHealthHUD>();
    }
}

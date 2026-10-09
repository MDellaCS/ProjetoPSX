using UnityEngine;

/// <summary>
/// Transforma um inimigo existente em um alvo de teste: ele nao persegue nem
/// gira para o jogador, mas ainda recebe dano, poise e stagger.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyHealth))]
public class TrainingDummyEnemy : MonoBehaviour
{
    [Header("Survival")]
    [Tooltip("Impede que o boneco seja destruido ao chegar a zero de vida.")]
    public bool isUnkillable = true;

    [Header("Optional Attack")]
    [Tooltip("Quando desligado, o boneco apenas fica parado e recebe golpes.")]
    public bool attackRegularly;

    [Min(0f)]
    [Tooltip("Tempo entre o fim de um ataque e o inicio do proximo.")]
    public float attackInterval = 2f;

    [Tooltip("O golpe e sempre executado para a frente atual do boneco.")]
    public EnemyAttackData attack = new EnemyAttackData
    {
        attackName = "Training Strike",
        damage = 15,
        minimumRange = 0f,
        maximumRange = 99f,
        windupDuration = .45f,
        activeDuration = .15f,
        recoveryDuration = .55f,
        hitboxDistance = 1.2f,
        hitboxRadius = .8f,
        knockbackForce = 4f,
        knockbackDuration = .12f
    };

    private EnemyCombat enemyCombat;
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyCombat = GetComponent<EnemyCombat>();
        enemyHealth = GetComponent<EnemyHealth>();
        ApplyConfiguration();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && enemyCombat != null)
            ApplyConfiguration();
    }

    private void ApplyConfiguration()
    {
        enemyCombat.ConfigureAsTrainingDummy(
            attackRegularly,
            attackInterval,
            attack
        );
        enemyHealth.SetUnkillable(isUnkillable);
    }
}

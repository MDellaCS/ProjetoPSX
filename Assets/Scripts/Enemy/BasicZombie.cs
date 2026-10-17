using UnityEngine;

/// <summary>
/// Zumbi basico corpo a corpo: um unico golpe repetido em um combo de ate
/// tres ataques, progressivamente mais rapido.
/// </summary>
public class BasicZombie : BaseEnemy
{
    [Header("Basic Zombie Combo")]
    [SerializeField] private BaseEnemyAttack[] attackCombo =
    {
        new BaseEnemyAttack
        {
            attackName = "Zombie Strike 1",
            damage = 20,
            maximumRange = 1.5f,
            windupDuration = 1.5f,
            activeDuration = .1f,
            recoveryDuration = 1f,
            hitboxDistance = .5f,
            hitboxRadius = 1f,
            knockbackForce = 3f,
            knockbackDuration = .12f
        },
        new BaseEnemyAttack
        {
            attackName = "Zombie Strike 2",
            damage = 20,
            maximumRange = 1.5f,
            windupDuration = 1f,
            activeDuration = .1f,
            recoveryDuration = 1f,
            hitboxDistance = .5f,
            hitboxRadius = 1f,
            knockbackForce = 3f,
            knockbackDuration = .12f
        },
        new BaseEnemyAttack
        {
            attackName = "Zombie Strike 3",
            damage = 20,
            maximumRange = 1.5f,
            windupDuration = .5f,
            activeDuration = .1f,
            recoveryDuration = 1f,
            hitboxDistance = .5f,
            hitboxRadius = 1f,
            knockbackForce = 3f,
            knockbackDuration = .12f
        }
    };

    protected override BaseEnemyAttack[] AttackCombo => attackCombo;
}

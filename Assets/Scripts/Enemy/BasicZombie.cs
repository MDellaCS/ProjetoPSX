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
            damage = 18,
            maximumRange = 1.7f,
            windupDuration = .65f,
            activeDuration = .1f,
            recoveryDuration = .65f,
            hitboxDistance = 1f,
            hitboxRadius = .75f,
            knockbackForce = 3f,
            knockbackDuration = .12f
        },
        new BaseEnemyAttack
        {
            attackName = "Zombie Strike 2",
            damage = 18,
            maximumRange = 1.7f,
            windupDuration = .45f,
            activeDuration = .1f,
            recoveryDuration = .55f,
            hitboxDistance = 1f,
            hitboxRadius = .75f,
            knockbackForce = 3f,
            knockbackDuration = .12f
        },
        new BaseEnemyAttack
        {
            attackName = "Zombie Strike 3",
            damage = 18,
            maximumRange = 1.7f,
            windupDuration = .28f,
            activeDuration = .1f,
            recoveryDuration = .8f,
            hitboxDistance = 1f,
            hitboxRadius = .75f,
            knockbackForce = 3f,
            knockbackDuration = .12f
        }
    };

    protected override BaseEnemyAttack[] AttackCombo => attackCombo;
}

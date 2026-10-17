using UnityEngine;

/// <summary>Zumbi lento com tres golpes progressivamente mais rapidos.</summary>
public class BasicZombie : EnemyBase
{
    protected override EnemyAttackData[] CreateDefaultAttacks() => new[]
    {
        new EnemyAttackData
        {
            attackName = "Zombie Strike 1", openingWeight = 100f,
            continuationChance = 65f,
            continuations = new() {
                new EnemyAttackContinuation { nextAttackIndex = 1 }
            },
            damage = 20, maximumRange = 1.5f, windupDuration = 1.5f,
            activeDuration = .1f, recoveryDuration = 1f,
            hitboxDistance = .5f, hitboxRadius = 1f,
            knockbackForce = 3f, knockbackDuration = .12f
        },
        new EnemyAttackData
        {
            attackName = "Zombie Strike 2", canStartAttack = false,
            continuationChance = 65f,
            continuations = new() {
                new EnemyAttackContinuation { nextAttackIndex = 2 }
            },
            damage = 20, maximumRange = 1.5f, windupDuration = 1f,
            activeDuration = .1f, recoveryDuration = 1f,
            hitboxDistance = .5f, hitboxRadius = 1f,
            knockbackForce = 3f, knockbackDuration = .12f
        },
        new EnemyAttackData
        {
            attackName = "Zombie Strike 3", canStartAttack = false,
            damage = 20, maximumRange = 1.5f, windupDuration = .5f,
            activeDuration = .1f, recoveryDuration = 1f,
            hitboxDistance = .5f, hitboxRadius = 1f,
            knockbackForce = 3f, knockbackDuration = .12f
        }
    };
}

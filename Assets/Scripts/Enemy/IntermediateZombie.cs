using UnityEngine;

/// <summary>Zumbi intermediario: combo basico de tres ou pesado de dois.</summary>
public class IntermediateZombie : EnemyBase
{
    protected override EnemyAttackData[] CreateDefaultAttacks() => new[]
    {
        new EnemyAttackData
        {
            attackName = "Zombie Strike 1", openingWeight = 65f,
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
        },
        new EnemyAttackData
        {
            attackName = "Zombie Heavy 1", openingWeight = 35f,
            continuationChance = 65f,
            continuations = new() {
                new EnemyAttackContinuation { nextAttackIndex = 4 }
            },
            damage = 32, maximumRange = 1.8f, windupDuration = 1.15f,
            activeDuration = .16f, recoveryDuration = .85f,
            hitboxDistance = .7f, hitboxRadius = 1.15f,
            knockbackForce = 5f, knockbackDuration = .2f
        },
        new EnemyAttackData
        {
            attackName = "Zombie Heavy 2", canStartAttack = false,
            damage = 38, maximumRange = 1.9f, windupDuration = .75f,
            activeDuration = .18f, recoveryDuration = 1.25f,
            hitboxDistance = .8f, hitboxRadius = 1.25f,
            knockbackForce = 7f, knockbackDuration = .28f
        }
    };
}

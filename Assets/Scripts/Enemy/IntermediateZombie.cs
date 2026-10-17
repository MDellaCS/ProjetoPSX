using UnityEngine;

/// <summary>
/// Zumbi intermediario com dois repertorios separados: o combo basico de
/// tres golpes e um combo pesado de dois golpes. As sequencias nunca se
/// misturam entre si.
/// </summary>
public class IntermediateZombie : BaseEnemy
{
    private const int BasicComboStartIndex = 0;
    private const int BasicComboEndIndex = 2;
    private const int HeavyComboStartIndex = 3;
    private const int HeavyComboEndIndex = 4;

    [Header("Intermediate Zombie Combo Selection")]
    [Tooltip("Chance de iniciar o combo pesado de dois golpes em vez do combo basico de tres golpes.")]
    [Range(0f, 100f)] [SerializeField] private float heavyComboSelectionChance = 35f;

    [Header("Intermediate Zombie Combos")]
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
        },
        new BaseEnemyAttack
        {
            attackName = "Zombie Heavy 1",
            damage = 32,
            maximumRange = 1.8f,
            windupDuration = 1.15f,
            activeDuration = .16f,
            recoveryDuration = .85f,
            hitboxDistance = .7f,
            hitboxRadius = 1.15f,
            knockbackForce = 5f,
            knockbackDuration = .2f
        },
        new BaseEnemyAttack
        {
            attackName = "Zombie Heavy 2",
            damage = 38,
            maximumRange = 1.9f,
            windupDuration = .75f,
            activeDuration = .18f,
            recoveryDuration = 1.25f,
            hitboxDistance = .8f,
            hitboxRadius = 1.25f,
            knockbackForce = 7f,
            knockbackDuration = .28f
        }
    };

    protected override BaseEnemyAttack[] AttackCombo => attackCombo;

    protected override int SelectAttackIndex()
    {
        return Random.value <= heavyComboSelectionChance / 100f
            ? HeavyComboStartIndex
            : BasicComboStartIndex;
    }

    protected override bool CanContinueAttackCombo(
        int currentIndex,
        int nextIndex)
    {
        bool isBasicContinuation = currentIndex >= BasicComboStartIndex &&
            currentIndex < BasicComboEndIndex &&
            nextIndex == currentIndex + 1;
        bool isHeavyContinuation = currentIndex == HeavyComboStartIndex &&
            nextIndex == HeavyComboEndIndex;

        return isBasicContinuation || isHeavyContinuation;
    }
}

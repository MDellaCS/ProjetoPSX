using UnityEngine;

public enum PlayerAttackPhase { None, Windup, Active, Recovery }

public sealed class PlayerAttackRuntime
{
    public readonly PlayerAttackDefinition Definition;
    public readonly int Damage;
    public readonly int PoiseDamage;
    public readonly float WindupDuration;
    public readonly float ActiveDuration;
    public readonly float RecoveryDuration;
    public readonly float HeatConsumed;
    public readonly float WindupActiveRotationSpeed;
    public readonly float ExtendedArmHitboxRadius;
    public readonly float ExtendedArmHitboxHeightOffset;
    public readonly bool IsGrappleAttack;
    public bool HitConfirmed;

    public PlayerAttackRuntime(PlayerAttackDefinition definition, PlayerHeat heat,
        float windupActiveRotationSpeed, float extendedArmHitboxRadius,
        float extendedArmHitboxHeightOffset, bool isGrappleAttack = false)
    {
        Definition = definition;
        int damage = CombatUnitSettings.PlayerDamage(definition.baseDamage);
        int poise = definition.basePoiseDamage;
        float timing = 1f;
        float consumed = 0f;
        if (definition.kind == PlayerAttackKind.Heavy && heat != null)
        {
            bool isBurnExplosion = definition.attackId == "BurnExplosion";
            PlayerHeat.HeavyAttackCharge charge = isBurnExplosion
                ? heat.CaptureBurnExplosionCharge()
                : heat.ConsumeHeatForHeavyAttack();
            damage = heat.CalculateHeavyDamage(damage, charge.DamageMultiplier);
            poise = heat.CalculateHeavyPoiseDamage(poise, charge.PoiseMultiplier);
            timing = 1f / Mathf.Max(.01f, Mathf.Lerp(1f, charge.DamageMultiplier, .5f));
            consumed = charge.HeatConsumed;
        }
        Damage = damage;
        PoiseDamage = poise;
        WindupDuration = definition.windupDuration * timing;
        ActiveDuration = definition.activeDuration;
        RecoveryDuration = definition.recoveryDuration * timing;
        HeatConsumed = consumed;
        WindupActiveRotationSpeed = Mathf.Max(0f,
            windupActiveRotationSpeed);
        ExtendedArmHitboxRadius = Mathf.Max(.01f, extendedArmHitboxRadius);
        ExtendedArmHitboxHeightOffset = extendedArmHitboxHeightOffset;
        IsGrappleAttack = isGrappleAttack;
    }
}

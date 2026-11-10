using UnityEngine;

public enum EnemyAttackPhase { None, Windup, Active, Recovery }

public sealed class EnemyAttackRuntime
{
    public readonly EnemyMoveNode Node;
    public readonly EnemyAttackDefinition Definition;
    public readonly int Damage;
    public readonly int PoiseDamage;
    public readonly float WindupDuration;
    public readonly float ActiveDuration;
    public readonly float RecoveryDuration;
    public readonly float RotationSpeed;
    public readonly float ExtendedArmHitboxRadius;
    public bool HitConfirmed;
    public Vector3 HitTargetPosition;

    public EnemyAttackRuntime(EnemyMoveNode node, EnemyMovesetDefinition moveset,
        float? windupDurationOverride = null, bool useGlobalTiming = false)
    {
        CorpseRunSettings corpseSettings = useGlobalTiming
            ? CorpseRunSettings.Load() : null;
        float windupMultiplier = corpseSettings != null
            ? Mathf.Max(0f, corpseSettings.windupMultiplier) : 1f;
        float activeMultiplier = corpseSettings != null
            ? Mathf.Max(0f, corpseSettings.activeMultiplier) : 1f;
        float recoveryMultiplier = corpseSettings != null
            ? Mathf.Max(0f, corpseSettings.recoveryMultiplier) : 1f;
        Node = node;
        Definition = node.attack;
        Damage = Definition.baseDamage;
        PoiseDamage = Definition.basePoiseDamage;
        WindupDuration = Mathf.Max(0f,
            (windupDurationOverride ?? Definition.windupDuration) * windupMultiplier);
        ActiveDuration = Definition.activeDuration * activeMultiplier;
        RecoveryDuration = Definition.recoveryDuration * recoveryMultiplier;
        RotationSpeed = moveset.GetRotationSpeed(Definition.kind);
        ExtendedArmHitboxRadius = Definition.usesExtendedArmHitbox
            ? moveset.extendedArmHitboxRadius : 0f;
    }
}

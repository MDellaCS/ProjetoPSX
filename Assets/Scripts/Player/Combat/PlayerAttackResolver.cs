using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAttackResolver : MonoBehaviour
{
    private PlayerHeat heat;
    private PlayerMovement movement;
    private PlayerCombatStatus combatStatus;
    private PlayerDpsMeter dpsMeter;
    private bool cameraShakeReported;

    private void Awake()
    {
        heat = GetComponent<PlayerHeat>();
        movement = GetComponent<PlayerMovement>();
        combatStatus = GetComponent<PlayerCombatStatus>();
        dpsMeter = GetComponent<PlayerDpsMeter>();
        if (dpsMeter == null)
            dpsMeter = gameObject.AddComponent<PlayerDpsMeter>();
    }

    public void BeginAttack() => cameraShakeReported = false;

    public bool Resolve(PlayerAttackRuntime runtime, PlayerAttackStrikeDefinition strike,
        EnemyBase enemy, float effectiveRadius, Vector3 impactPosition,
        Vector3 impactDirection)
    {
        int damage = Mathf.RoundToInt(runtime.Damage * strike.damageMultiplier);
        int poise = Mathf.RoundToInt(runtime.PoiseDamage * strike.poiseMultiplier);
        PlayerAttackType type = runtime.Definition.context switch
        {
            PlayerAttackContext.Dash => PlayerAttackType.DashLight,
            PlayerAttackContext.Air => runtime.Definition.kind == PlayerAttackKind.Heavy
                ? PlayerAttackType.AerialHeavy
                : PlayerAttackType.AerialLight,
            _ => runtime.Definition.kind == PlayerAttackKind.Heavy
                ? PlayerAttackType.Heavy
                : PlayerAttackType.Light
        };
        bool connected = enemy.TakeDamage(damage, transform.position, poise,
            out int dealt, out CombatPoiseHitResult poiseHit,
            true, type, effectiveRadius, impactDirection, impactPosition, true);
        if (!connected) return false;

        runtime.HitConfirmed = true;
        dpsMeter?.ReportDamage(dealt);
        CombatImpactSystem.ReportPlayerAttackImpact(transform, enemy.transform,
            runtime.Definition.kind == PlayerAttackKind.Heavy, dealt, !cameraShakeReported);
        cameraShakeReported = true;
        PlayerAttackEffectsDefinition effects = strike.effects;
        CombatReactionType reaction = GetRequestedReaction(effects);
        CombatPoiseSystem rules = CombatPoiseSystem.Instance;
        enemy.ApplyPoiseReaction(poiseHit, new CombatReactionRequest(
            reaction,
            transform.position,
            GetLaunchSpeed(),
            GetLaunchGravity(),
            rules.CalculateLaunchForce(dealt),
            rules.CalculateLaunchForce(dealt),
            rules.LaunchForwardsDeceleration,
            rules.JuggleLiftSpeed,
            rules.JuggleGravityMultiplier,
            rules.JuggleDuration
        ));
        if (runtime.Definition.kind == PlayerAttackKind.Light &&
            strike.generatesHeatOnHit && heat != null)
            heat.AddHeat(dealt * heat.heatGeneratedPerDamageDealt);
        combatStatus?.RegisterDamageDealtToEnemy();
        return true;
    }

    public void ApplySelfLaunchUp() => movement?.ApplySelfLaunchUp(
        movement.GetMaximumJumpLaunchSpeed(), movement.gravity);

    public void ApplySelfLaunchDown() =>
        movement?.BeginAerialDive(CombatPoiseSystem.Instance.MinimumLaunchForce);

    public void ApplyJuggleToPlayer()
    {
        if (movement == null)
            return;

        CombatPoiseSystem rules = CombatPoiseSystem.Instance;
        movement.BeginAerialLight(rules.JuggleLiftSpeed,
            rules.JuggleGravityMultiplier, rules.JuggleDuration);
    }

    public bool ApplyNonDamagingLaunchUp(EnemyBase enemy, int poiseDamage,
        Vector3 horizontalDirection, float horizontalImpulse)
    {
        if (enemy == null) return false;
        bool connected = enemy.TakeDamage(0, transform.position, poiseDamage,
            out _, out CombatPoiseHitResult poiseHit, true,
            PlayerAttackType.Light);
        if (!connected || !poiseHit.CanReact)
            return false;
        CombatPoiseSystem rules = CombatPoiseSystem.Instance;
        enemy.ApplyPoiseReaction(poiseHit, new CombatReactionRequest(
            CombatReactionType.LaunchUp, transform.position,
            GetLaunchSpeed(), GetLaunchGravity(), rules.MinimumLaunchForce,
            0f, rules.LaunchForwardsDeceleration,
            rules.JuggleLiftSpeed, rules.JuggleGravityMultiplier,
            rules.JuggleDuration));
        if (enemy.IsLaunched)
            enemy.ApplyLaunchUpHorizontalImpulse(horizontalDirection, horizontalImpulse);
        combatStatus?.RegisterDamageDealtToEnemy();
        return enemy.IsLaunched;
    }

    private static CombatReactionType GetRequestedReaction(
        PlayerAttackEffectsDefinition effects)
    {
        if (effects.launchUp && effects.launchForwards)
            return CombatReactionType.LaunchUpForwards;
        if (effects.launchDown) return CombatReactionType.LaunchDown;
        if (effects.launchUp) return CombatReactionType.LaunchUp;
        if (effects.launchForwards) return CombatReactionType.LaunchForwards;
        if (effects.juggle) return CombatReactionType.Juggle;
        return CombatReactionType.None;
    }

    private float GetLaunchSpeed() => movement != null
        ? movement.GetMaximumJumpLaunchSpeed() : 18f;
    private float GetLaunchGravity() => movement != null ? movement.gravity : -60f;
}

using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAttackResolver : MonoBehaviour
{
    private PlayerHeat heat;
    private PlayerMovement movement;
    private PlayerCombatStatus combatStatus;
    private PlayerDpsMeter dpsMeter;
    private bool cameraShakeReported;
    private int lastSelfJuggleFrame = -1;

    private void Awake()
    {
        heat = GetComponent<PlayerHeat>();
        movement = GetComponent<PlayerMovement>();
        combatStatus = GetComponent<PlayerCombatStatus>();
        dpsMeter = GetComponent<PlayerDpsMeter>();
        if (dpsMeter == null)
            dpsMeter = gameObject.AddComponent<PlayerDpsMeter>();
    }

    public void BeginAttack()
    {
        cameraShakeReported = false;
        lastSelfJuggleFrame = -1;
    }

    public bool Resolve(PlayerAttackRuntime runtime, PlayerAttackStrikeDefinition strike,
        EnemyBase enemy, float attackReach, Vector3 impactPosition,
        Vector3 impactDirection)
    {
        float distanceFromPlayer = Vector3.Distance(transform.position,
            impactPosition);
        float falloffMultiplier = strike.damageFalloff != null
            ? strike.damageFalloff.Evaluate(distanceFromPlayer)
            : 1f;
        int damage = Mathf.RoundToInt(runtime.Damage * strike.damageMultiplier *
            falloffMultiplier);
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
            true, type, attackReach, impactDirection, impactPosition, true);
        if (!connected) return false;

        runtime.HitConfirmed = true;
        if (strike.effects.juggle)
            ApplyJuggleToPlayer();
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

    /// <summary>Aplica o dano final do golpe a objetos interativos do mundo.</summary>
    public bool ResolveUtilityTarget(PlayerAttackRuntime runtime,
        PlayerAttackStrikeDefinition strike, IPlayerAttackTarget target,
        Vector3 impactPosition, Vector3 impactDirection)
    {
        if (target == null)
            return false;

        float distanceFromPlayer = Vector3.Distance(transform.position,
            impactPosition);
        float falloffMultiplier = strike.damageFalloff != null
            ? strike.damageFalloff.Evaluate(distanceFromPlayer)
            : 1f;
        int damage = Mathf.RoundToInt(runtime.Damage * strike.damageMultiplier *
            falloffMultiplier);
        bool connected = target.ReceivePlayerAttack(damage, impactPosition,
            impactDirection);
        if (!connected)
            return false;

        runtime.HitConfirmed = true;

        // Alvos interativos, como StrengthLevers, devem responder ao golpe
        // com o mesmo impacto basico de acertar uma entidade: hitstop local e
        // tremor unico por ataque. O alvo e necessariamente um Component na
        // implementacao Unity atual, mas a interface mantem a hitbox livre de
        // depender de um tipo concreto.
        if (target is Component targetComponent)
        {
            CombatImpactSystem.ReportPlayerAttackImpact(transform,
                targetComponent.transform,
                runtime.Definition.kind == PlayerAttackKind.Heavy,
                damage,
                !cameraShakeReported);
            cameraShakeReported = true;
        }

        if (runtime.Definition.kind == PlayerAttackKind.Light &&
            strike.generatesHeatOnHit && heat != null)
        {
            heat.AddHeat(damage * heat.heatGeneratedPerDamageDealt);
        }

        return true;
    }

    public void ApplySelfLaunchUp() => movement?.ApplySelfLaunchUp(
        movement.GetMaximumJumpLaunchSpeed(), movement.gravity);

    public void ApplySelfLaunchDown() =>
        movement?.BeginAerialDive(CombatPoiseSystem.Instance.MinimumLaunchForce);

    /// <summary>
    /// Aplica o impulso do player para cada impacto de Juggle confirmado,
    /// mantendo-o sincronizado com o inimigo. Consultas duplicadas no mesmo
    /// frame (por exemplo, volumes sobrepostos) contam apenas uma vez.
    /// </summary>
    public void ApplyJuggleToPlayer()
    {
        if (movement == null || lastSelfJuggleFrame == Time.frameCount)
            return;

        lastSelfJuggleFrame = Time.frameCount;

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

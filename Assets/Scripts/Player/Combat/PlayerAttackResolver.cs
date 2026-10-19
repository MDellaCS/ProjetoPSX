using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAttackResolver : MonoBehaviour
{
    [Header("Launch Up e Juggle")]
    [Tooltip("Impulso vertical aplicado pelo Juggle em inimigos que ja estao no ar.")]
    [Min(0f)] [SerializeField] private float juggleLiftSpeed = 2f;
    [Tooltip("Multiplicador temporario de gravidade usado pelo Juggle.")]
    [Range(0f, 1f)] [SerializeField] private float juggleGravityMultiplier = .3f;
    [Tooltip("Duracao do efeito de gravidade do Juggle.")]
    [Min(0f)] [SerializeField] private float juggleDuration = .2f;

    [Header("Push")]
    [Tooltip("Velocidade horizontal de Push gerada por cada ponto de Poise Damage final.")]
    [Min(0f)] [SerializeField] private float pushForcePerPoiseDamage = .42f;
    [Tooltip("Duracao maxima do Push caso o inimigo nao toque uma parede ou o chao.")]
    [Min(0f)] [SerializeField] private float pushDuration = .65f;

    private PlayerHeat heat;
    private PlayerMovement movement;
    private PlayerCombatStatus combatStatus;
    private bool cameraShakeReported;

    private void Awake()
    {
        heat = GetComponent<PlayerHeat>();
        movement = GetComponent<PlayerMovement>();
        combatStatus = GetComponent<PlayerCombatStatus>();
    }

    public void BeginAttack() => cameraShakeReported = false;

    public bool Resolve(PlayerAttackRuntime runtime, PlayerAttackStrikeDefinition strike,
        EnemyBase enemy, float effectiveRadius)
    {
        int damage = Mathf.RoundToInt(runtime.Damage * strike.damageMultiplier);
        int poise = Mathf.RoundToInt(runtime.PoiseDamage * strike.poiseMultiplier);
        PlayerAttackType type = runtime.Definition.kind == PlayerAttackKind.Heavy
            ? PlayerAttackType.Heavy : PlayerAttackType.Light;
        bool connected = enemy.TakeDamage(damage, transform.position, poise,
            out int dealt, true, type, effectiveRadius);
        if (!connected) return false;

        runtime.HitConfirmed = true;
        CombatImpactSystem.ReportPlayerAttackImpact(transform, enemy.transform,
            runtime.Definition.kind == PlayerAttackKind.Heavy, dealt, !cameraShakeReported);
        cameraShakeReported = true;
        PlayerAttackEffectsDefinition effects = strike.effects;
        if (effects.launchUp) enemy.ApplyLaunchUp(GetLaunchSpeed(), GetLaunchGravity());
        if (effects.push) enemy.ApplyPush(transform.position,
            poise * pushForcePerPoiseDamage, pushDuration);
        if (effects.juggle) enemy.ApplyAerialJuggleLight(juggleLiftSpeed,
            GetLaunchGravity(), juggleGravityMultiplier, juggleDuration);
        if (runtime.Definition.kind == PlayerAttackKind.Light &&
            strike.generatesHeatOnHit && heat != null)
            heat.AddHeat(heat.lightAttackHeatGeneratedOnHit);
        combatStatus?.RegisterDamageDealtToEnemy();
        return true;
    }

    public void ApplySelfLaunchUp() => movement?.ApplySelfLaunchUp(
        movement.GetMaximumJumpLaunchSpeed(), movement.gravity);

    public void ApplyNonDamagingLaunchUp(EnemyBase enemy, Vector3 horizontalDirection, float horizontalImpulse)
    {
        if (enemy == null || !enemy.CanBeLaunched) return;
        enemy.ApplyLaunchUp(GetLaunchSpeed(), GetLaunchGravity());
        enemy.ApplyLaunchUpHorizontalImpulse(horizontalDirection, horizontalImpulse);
        combatStatus?.RegisterDamageDealtToEnemy();
    }

    private float GetLaunchSpeed() => movement != null
        ? movement.GetMaximumJumpLaunchSpeed() : 18f;
    private float GetLaunchGravity() => movement != null ? movement.gravity : -60f;
}

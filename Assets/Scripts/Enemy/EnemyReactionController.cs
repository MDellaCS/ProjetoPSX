using UnityEngine;

/// <summary>Escolhe e executa a reacao de Poise valida para um inimigo.</summary>
[DisallowMultipleComponent]
public sealed class EnemyReactionController : MonoBehaviour
{
    private EnemyBase owner;
    private EnemyHealth health;
    private EnemyMovement movement;
    private bool pendingStagger;
    private bool pendingSuperStagger;
    private Vector3 pendingStaggerSource;
    private bool wasInForcedMovement;

    internal void Initialize(EnemyBase enemy, EnemyHealth enemyHealth,
        EnemyMovement enemyMovement)
    {
        owner = enemy;
        health = enemyHealth;
        movement = enemyMovement;
    }

    internal void Tick()
    {
        bool forced = movement.IsLaunched || movement.IsLaunchedForwards;
        if (pendingStagger && wasInForcedMovement && !forced)
        {
            pendingStagger = false;
            bool superStagger = pendingSuperStagger;
            pendingSuperStagger = false;
            health.ApplyPoiseStagger(pendingStaggerSource, superStagger);
        }
        wasInForcedMovement = forced;
    }

    public bool Apply(CombatPoiseHitResult poiseHit,
        CombatReactionRequest request)
    {
        if (!poiseHit.CanReact || owner.IsDead)
            return false;

        CombatReactionType reaction = ResolveAllowedReaction(request.Type);
        bool appliedForcedMovement = ApplyReaction(reaction, request,
            poiseHit.IsSuperStagger);

        if (poiseHit.IsSuperStagger && appliedForcedMovement)
        {
            pendingStagger = true;
            pendingSuperStagger = true;
            pendingStaggerSource = request.SourcePosition;
            wasInForcedMovement = true;
        }
        return true;
    }

    private CombatReactionType ResolveAllowedReaction(
        CombatReactionType requested)
    {
        bool airborne = !movement.IsGrounded || movement.IsLaunched;
        return requested switch
        {
            CombatReactionType.LaunchUpForwards when movement.CanBeLaunchedUp &&
                movement.CanBeLaunchedForwards => CombatReactionType.LaunchUpForwards,
            CombatReactionType.LaunchUpForwards when movement.CanBeLaunchedUp =>
                CombatReactionType.LaunchUp,
            CombatReactionType.LaunchUpForwards when movement.CanBeLaunchedForwards =>
                CombatReactionType.LaunchForwards,
            CombatReactionType.LaunchUp when movement.CanBeLaunchedUp =>
                CombatReactionType.LaunchUp,
            CombatReactionType.LaunchDown when airborne && movement.CanBeLaunchedDown =>
                CombatReactionType.LaunchDown,
            CombatReactionType.LaunchForwards when movement.CanBeLaunchedForwards =>
                CombatReactionType.LaunchForwards,
            CombatReactionType.Juggle when airborne && movement.CanBeJuggled =>
                CombatReactionType.Juggle,
            CombatReactionType.Stagger => CombatReactionType.Stagger,
            CombatReactionType.None when airborne && movement.CanBeJuggled =>
                CombatReactionType.Juggle,
            _ => CombatReactionType.Stagger
        };
    }

    private bool ApplyReaction(CombatReactionType reaction,
        CombatReactionRequest request, bool superStagger)
    {
        switch (reaction)
        {
            case CombatReactionType.LaunchUp:
                movement.ApplyLaunchUp(request.LaunchSpeed, request.Gravity);
                return true;
            case CombatReactionType.LaunchUpForwards:
                movement.ApplyLaunchUpAndForwards(request.SourcePosition,
                    request.LaunchSpeed, request.Gravity,
                    request.LaunchForwardsForce,
                    request.LaunchForwardsDeceleration);
                return true;
            case CombatReactionType.LaunchDown:
                movement.ApplyAerialDive(request.LaunchDownSpeed);
                return true;
            case CombatReactionType.LaunchForwards:
                if (request.LaunchForwardsForce <= 0f)
                {
                    health.ApplyPoiseStagger(request.SourcePosition,
                        superStagger);
                    return false;
                }
                movement.ApplyLaunchForwards(request.SourcePosition,
                    request.LaunchForwardsForce,
                    request.LaunchForwardsDeceleration);
                return true;
            case CombatReactionType.Juggle:
                movement.ApplyAerialJuggleLight(request.JuggleLiftSpeed,
                    request.Gravity, request.JuggleGravityMultiplier,
                    request.JuggleDuration);
                return true;
            default:
                if (movement.IsLaunched || movement.IsLaunchedForwards)
                {
                    pendingStagger = true;
                    pendingSuperStagger = superStagger;
                    pendingStaggerSource = request.SourcePosition;
                    wasInForcedMovement = true;
                    return false;
                }
                health.ApplyPoiseStagger(request.SourcePosition, superStagger);
                return false;
        }
    }
}

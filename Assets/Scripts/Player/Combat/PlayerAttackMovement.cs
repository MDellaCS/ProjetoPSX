using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAttackMovement : MonoBehaviour
{
    private PlayerMovement movement;
    private PlayerLockOn lockOn;
    private PlayerAttackRuntime runtime;
    private float elapsed;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        lockOn = PlayerLockOn.GetOrAdd(gameObject);
    }

    public void Begin(PlayerAttackRuntime attack) { runtime = attack; elapsed = 0f; }
    public void End() { runtime = null; elapsed = 0f; }

    public void Tick(PlayerAttackPhase phase, float deltaTime)
    {
        if (runtime == null) return;
        PlayerAttackMovementDefinition data = runtime.Definition.movement;
        if (!runtime.IsGrappleAttack &&
            (phase == PlayerAttackPhase.Windup ||
             phase == PlayerAttackPhase.Active))
        {
            Vector3 direction;
            // Lock-on e uma intencao explicita do jogador e sempre tem
            // prioridade sobre qualquer alvo escolhido automaticamente.
            if (lockOn != null && lockOn.IsLockedOn)
                direction = lockOn.GetDirectionToLockedTarget();
            else if (movement != null &&
                movement.DesiredDirection.sqrMagnitude > .001f)
                direction = movement.DesiredDirection;
            else
                direction = transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude > .001f &&
                runtime.WindupActiveRotationSpeed > 0f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction.normalized),
                    runtime.WindupActiveRotationSpeed * deltaTime);
        }
        if (phase != PlayerAttackPhase.Windup && phase != PlayerAttackPhase.Active) return;
        float duration = runtime.WindupDuration + runtime.ActiveDuration;
        float previous = duration > 0f ? elapsed / duration : 1f;
        elapsed = Mathf.Min(duration, elapsed + deltaTime);
        float current = duration > 0f ? elapsed / duration : 1f;
        float start = Mathf.Min(data.startNormalized, data.endNormalized);
        float end = Mathf.Max(data.startNormalized, data.endNormalized);
        if (end <= start || data.forwardDistance <= 0f) return;
        float a = Evaluate(data, Mathf.InverseLerp(start, end, previous));
        float b = Evaluate(data, Mathf.InverseLerp(start, end, current));
        movement?.ApplyAttackMovement(transform.forward * (data.forwardDistance * (b - a)));
    }

    private static float Evaluate(PlayerAttackMovementDefinition data, float value) =>
        data.movementCurve != null ? Mathf.Clamp01(data.movementCurve.Evaluate(value)) : value;

}

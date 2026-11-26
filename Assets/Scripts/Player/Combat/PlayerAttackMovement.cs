using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAttackMovement : MonoBehaviour
{
    [Tooltip("Espaco minimo entre o jogador e o inimigo travado ao avancar durante um ataque, em metros.")]
    [Min(0f)] [SerializeField] private float lockOnAttackStopDistance = .15f;

    private PlayerMovement movement;
    private PlayerCharacterController playerController;
    private EnemyBase cachedTarget;
    private CharacterController targetController;
    private PlayerAttackRuntime runtime;
    private float elapsed;
    private float attackDistance;
    private PlayerLockOn lockOn;
    private bool magnetism;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        playerController = GetComponent<PlayerCharacterController>();
        lockOn = GetComponent<PlayerLockOn>();
    }

    public void Begin(PlayerAttackRuntime attack, bool allowMagnetism = true,
        float distanceMultiplier = 1f)
    {
        runtime = attack; elapsed = 0f;
        attackDistance = attack.Definition.Movement.forwardDistance * Mathf.Max(0f, distanceMultiplier);
        if (lockOn == null) lockOn = GetComponent<PlayerLockOn>();
        magnetism = allowMagnetism;
    }
    public void End() { runtime = null; elapsed = 0f; }

    public void Tick(PlayerAttackPhase phase, float deltaTime, bool applyRotation = true)
    {
        if (runtime == null) return;
        PlayerAttackMovementDefinition data = runtime.Definition.Movement;
        if (applyRotation && !(magnetism && TickLockOnRotation(phase, deltaTime)) && !runtime.IsGrappleAttack &&
            (phase == PlayerAttackPhase.Windup ||
             phase == PlayerAttackPhase.Active))
        {
            Vector3 direction;
            if (movement != null &&
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
        if (runtime.Definition.allowMovementWhileAttacking) return;
        float start = Mathf.Min(data.startNormalized, data.endNormalized);
        float end = Mathf.Max(data.startNormalized, data.endNormalized);
        if (end <= start || Mathf.Approximately(attackDistance, 0f)) return;
        float a = Evaluate(data, Mathf.InverseLerp(start, end, previous));
        float b = Evaluate(data, Mathf.InverseLerp(start, end, current));
        float distance = attackDistance * (b - a);
        if (distance > 0f && magnetism && lockOn != null && lockOn.Target != null)
            distance = LimitForwardMovement(distance, lockOn.Target);
        movement?.ApplyAttackMovement(transform.forward * distance);
    }

    private float LimitForwardMovement(float distance, EnemyBase target)
    {
        if (cachedTarget != target)
        {
            cachedTarget = target;
            targetController = target.GetComponent<CharacterController>();
        }

        Bounds playerBounds = playerController != null ? playerController.bounds : default;
        Bounds targetBounds = targetController != null ? targetController.bounds : default;
        Vector3 playerCenter = playerController != null ? playerBounds.center : transform.position;
        Vector3 targetCenter = targetController != null ? targetBounds.center : target.transform.position;
        float playerRadius = playerController != null ? Mathf.Max(playerBounds.extents.x, playerBounds.extents.z) : 0f;
        float targetRadius = targetController != null ? Mathf.Max(targetBounds.extents.x, targetBounds.extents.z) : 0f;
        float stopDistance = playerRadius + targetRadius + Mathf.Max(0f, lockOnAttackStopDistance);
        Vector3 offset = targetCenter - playerCenter;
        offset.y = 0f;
        float offsetSquared = offset.sqrMagnitude;
        float stopSquared = stopDistance * stopDistance;
        if (offsetSquared <= stopSquared) return 0f;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= .0001f) return distance;
        forward.Normalize();
        float projection = Vector3.Dot(offset, forward);
        if (projection <= 0f) return distance;
        float discriminant = projection * projection - (offsetSquared - stopSquared);
        if (discriminant <= 0f) return distance;
        return Mathf.Min(distance, Mathf.Max(0f, projection - Mathf.Sqrt(discriminant)));
    }

    // Tambem usado pelo ataque de slide, que tem movimentacao propria.
    public bool TickLockOnRotation(PlayerAttackPhase phase, float deltaTime)
    {
        if (phase != PlayerAttackPhase.Windup && phase != PlayerAttackPhase.Active)
            return false;
        if (lockOn == null || lockOn.Target == null)
            return false;

        Vector3 aim = Vector3.ProjectOnPlane(
            lockOn.Target.LockOnAimPoint - transform.position, Vector3.up);
        if (aim.sqrMagnitude > .0001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(aim), lockOn.MagnetismSpeed * deltaTime);
        return true;
    }

    private static float Evaluate(PlayerAttackMovementDefinition data, float value) =>
        data.movementCurve != null ? Mathf.Clamp01(data.movementCurve.Evaluate(value)) : value;

}

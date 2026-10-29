using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAttackMovement : MonoBehaviour
{
    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyMovement movement;
    private EnemyAttackRuntime runtime;
    private float previousCurveValue;
    private Transform attackTarget;
    private Vector3 attackDirection;

    public void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyMovement locomotion)
    {
        owner = enemy;
        brain = enemyBrain;
        movement = locomotion;
    }

    public void Begin(EnemyAttackRuntime attack)
    {
        runtime = attack;
        previousCurveValue = EvaluateAt(0f);
        attackTarget = brain != null ? brain.Target : null;
        attackDirection = attackTarget != null
            ? attackTarget.position - transform.position
            : transform.forward;
        attackDirection.y = 0f;
        attackDirection = attackDirection.sqrMagnitude > .001f
            ? attackDirection.normalized : transform.forward;
        attackDirection.y = 0f;
        attackDirection = attackDirection.sqrMagnitude > .001f
            ? attackDirection.normalized : Vector3.forward;
    }

    public void End()
    {
        runtime = null;
        previousCurveValue = 0f;
        attackTarget = null;
        attackDirection = Vector3.zero;
    }

    public void Tick(EnemyAttackPhase phase, float phaseProgress,
        float windupActiveProgress)
    {
        if (runtime == null)
            return;
        EnemyAttackMovementDefinition authored = runtime.Definition.movement;
        if (authored.approachTargetUntilPreferredRange)
        {
            movement.Move(Vector3.zero);
            return;
        }

        bool face = phase == EnemyAttackPhase.Windup
            ? authored.faceTargetDuringWindup
            : phase == EnemyAttackPhase.Active && authored.faceTargetDuringActive;
        if (face && brain.Target != null && runtime.RotationSpeed > 0f)
        {
            Vector3 direction = brain.Target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > .001f)
            {
                Quaternion target = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    target, runtime.RotationSpeed * Time.deltaTime);
            }
        }

        float current = EvaluateAt(windupActiveProgress);
        float delta = current - previousCurveValue;
        previousCurveValue = current;
        Vector3 displacement = transform.forward * authored.forwardDistance * delta +
            transform.right * authored.lateralDistance * delta;
        if (authored.stopAtDistancePastTargetOnHit && runtime.HitConfirmed)
        {
            Vector3 targetPosition = runtime.HitTargetPosition;
            targetPosition.y = owner.transform.position.y;
            float currentPastTarget = Vector3.Dot(
                owner.transform.position - targetPosition, attackDirection);
            float desiredPastTarget = Mathf.Max(0f,
                authored.distancePastTargetOnHit);
            float remaining = desiredPastTarget - currentPastTarget;
            float forwardDisplacement = Vector3.Dot(displacement,
                attackDirection);

            if (remaining <= 0f)
                displacement = Vector3.zero;
            else if (forwardDisplacement > remaining)
                displacement -= attackDirection *
                    (forwardDisplacement - remaining);
        }
        Vector3 velocity = Time.deltaTime > 0f
            ? displacement / Time.deltaTime : Vector3.zero;
        movement.Move(velocity);
    }

    public bool TickApproachToPreferredRange()
    {
        if (runtime == null || brain.Target == null)
        {
            movement.Move(Vector3.zero);
            return false;
        }

        float preferredRange = Mathf.Max(0f,
            runtime.Definition.requirements.preferredRange);
        Vector3 direction = brain.Target.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance <= preferredRange)
        {
            movement.Move(Vector3.zero);
            return true;
        }

        if (direction.sqrMagnitude <= .001f)
        {
            movement.Move(Vector3.zero);
            return false;
        }

        float speed = runtime.Definition.movement.approachSpeed > 0f
            ? runtime.Definition.movement.approachSpeed
            : movement.FastSpeed;
        float distanceToRange = distance - preferredRange;
        if (Time.deltaTime > 0f)
            speed = Mathf.Min(speed, distanceToRange / Time.deltaTime);
        movement.MoveTowardDirection(direction, speed);
        return false;
    }

    public void TickRecovery() => movement.Move(Vector3.zero);

    private float EvaluateAt(float overallProgress)
    {
        if (runtime == null)
            return 0f;
        EnemyAttackMovementDefinition authored = runtime.Definition.movement;
        if (overallProgress <= authored.startNormalized)
            return 0f;
        if (overallProgress >= authored.endNormalized)
            return 1f;
        float normalized = Mathf.InverseLerp(authored.startNormalized,
            Mathf.Max(authored.startNormalized + .0001f, authored.endNormalized),
            overallProgress);
        return authored.movementCurve == null || authored.movementCurve.length == 0
            ? normalized : authored.movementCurve.Evaluate(normalized);
    }
}

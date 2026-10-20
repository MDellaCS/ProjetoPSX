using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAttackMovement : MonoBehaviour
{
    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyMovement movement;
    private EnemyAttackRuntime runtime;
    private float previousCurveValue;

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
    }

    public void End()
    {
        runtime = null;
        previousCurveValue = 0f;
    }

    public void Tick(EnemyAttackPhase phase, float phaseProgress,
        float windupActiveProgress)
    {
        if (runtime == null)
            return;
        EnemyAttackMovementDefinition authored = runtime.Definition.movement;
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
        Vector3 velocity = Time.deltaTime > 0f
            ? displacement / Time.deltaTime : Vector3.zero;
        movement.Move(velocity);
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

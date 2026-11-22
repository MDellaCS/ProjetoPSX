using UnityEngine;

/// <summary>Seleciona o alvo do grapple de combate e do seu indicador.</summary>
[DisallowMultipleComponent]
public sealed class PlayerTargetAssist : MonoBehaviour, IGrappleTargetProvider
{
    // Preserva os valores de cenas e prefabs que usavam a assistencia global.
    [HideInInspector, SerializeField] private bool targetAssistEnabled = true;
    [HideInInspector, SerializeField] private float targetAssistRange = 16f;
    [HideInInspector, SerializeField] private float targetAssistAngle = 55f;
    [HideInInspector, SerializeField] private float targetAssistNearAngle = 90f;
    [HideInInspector, SerializeField] private AnimationCurve targetAssistNearAngleDistanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Camadas que bloqueiam a visibilidade entre o jogador e o inimigo para o grapple de combate.")]
    [SerializeField] private LayerMask targetObstructionMask = Physics.DefaultRaycastLayers;

    private PlayerCharacterController characterController;
    private PlayerState playerState;

    public int GrappleTargetPriority => 200;

    private void Awake()
    {
        characterController = GetComponent<PlayerCharacterController>();
        playerState = GetComponent<PlayerState>();
    }

    private void OnEnable() => GrappleTargetSystem.Register(this);
    private void OnDisable() => GrappleTargetSystem.Unregister(this);

    // Usado ao criar o componente em jogadores com a configuracao antiga.
    public void ConfigureFromLegacy(float range, float farAngle,
        float nearAngle, LayerMask obstructionMask, bool enabled)
    {
        targetAssistRange = range;
        targetAssistAngle = farAngle;
        targetAssistNearAngle = nearAngle;
        targetObstructionMask = obstructionMask;
        targetAssistEnabled = enabled;
    }

    public bool TryGetGrappleTarget(out GrappleTarget target)
    {
        target = default;
        if (playerState == null) playerState = GetComponent<PlayerState>();
        if (playerState != null && !playerState.IsNormal()) return false;
        EnemyBase enemy = FindCombatGrappleTarget();
        if (enemy == null) return false;
        Collider body = enemy.GetComponentInChildren<Collider>();
        target = new GrappleTarget
        {
            kind = GrappleTargetKind.Enemy,
            point = body != null ? body.bounds.center : enemy.transform.position + Vector3.up,
            source = enemy
        };
        return true;
    }

    /// <summary>Seleciona o melhor inimigo visivel na area do grapple.</summary>
    public EnemyBase FindCombatGrappleTarget()
    {
        CombatGrappleSettings settings = GrappleSettings.Combat;
        Vector3 origin = GetPlayerCenter();
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .0001f) return null;

        EnemyBase best = null;
        float bestScore = float.PositiveInfinity;
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (!IsEligibleTarget(enemy, origin, forward, settings)) continue;
            Vector3 offset = enemy.AttackOrigin - origin;
            Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
            float score = offset.magnitude + Vector3.Angle(forward, horizontal) * .025f;
            if (score >= bestScore) continue;
            best = enemy;
            bestScore = score;
        }
        return best;
    }

    public bool TryCaptureCombatGrappleTarget(out EnemyBase target, out Vector3 lockedPosition)
    {
        target = FindCombatGrappleTarget();
        lockedPosition = target != null ? target.AttackOrigin : default;
        return target != null;
    }

    private bool IsEligibleTarget(EnemyBase enemy, Vector3 origin,
        Vector3 forward, CombatGrappleSettings settings)
    {
        if (enemy == null || !enemy.CanBeCombatTarget) return false;
        Vector3 offset = enemy.AttackOrigin - origin;
        if (!IsWithinArea(offset, forward, settings.assistCircleRadius,
            settings.range, settings.assistAngle, settings.assistHeight)) return false;
        float distance = offset.magnitude;
        if (distance <= .0001f) return true;
        return !Physics.Raycast(origin, offset / distance, out RaycastHit hit,
            distance, targetObstructionMask, QueryTriggerInteraction.Ignore) ||
            (hit.collider != null && hit.collider.GetComponentInParent<EnemyBase>() == enemy);
    }

    internal static bool IsWithinArea(Vector3 offset, Vector3 forward,
        float circleRadius, float coneRange, float coneHalfAngle, float height)
    {
        if (Mathf.Abs(offset.y) > Mathf.Max(0f, height) * .5f) return false;
        Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
        float distanceSqr = horizontal.sqrMagnitude;
        float circle = Mathf.Max(0f, circleRadius);
        if (distanceSqr <= circle * circle) return true;
        float range = Mathf.Max(0f, coneRange);
        return distanceSqr <= range * range &&
            Vector3.Angle(forward, horizontal) <= Mathf.Clamp(coneHalfAngle, 0f, 180f);
    }

    private Vector3 GetPlayerCenter()
    {
        if (characterController == null) characterController = GetComponent<PlayerCharacterController>();
        return transform.position + transform.rotation *
            (characterController != null ? characterController.center : Vector3.zero);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !DebugGizmoSettings.Enabled(DebugGizmoGroup.PlayerCombat)) return;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .0001f) return;
        CombatGrappleSettings settings = GrappleSettings.Combat;
        Vector3 center = GetPlayerCenter();
        float halfHeight = Mathf.Max(0f, settings.assistHeight) * .5f;
        Color previous = Gizmos.color;
        Gizmos.color = new Color(.82f, .42f, 1f, .98f);
        DrawArea(center + Vector3.up * halfHeight, forward, settings);
        DrawArea(center - Vector3.up * halfHeight, forward, settings);
        float circle = Mathf.Max(0f, settings.assistCircleRadius);
        float range = Mathf.Max(0f, settings.range);
        float angle = Mathf.Clamp(settings.assistAngle, 0f, 180f);
        DrawVertical(center + forward * circle, halfHeight);
        DrawVertical(center - forward * circle, halfHeight);
        DrawVertical(center + Quaternion.AngleAxis(-angle, Vector3.up) * forward * range, halfHeight);
        DrawVertical(center + Quaternion.AngleAxis(angle, Vector3.up) * forward * range, halfHeight);
        Gizmos.color = previous;
    }

    private static void DrawArea(Vector3 origin, Vector3 forward, CombatGrappleSettings settings)
    {
        float circle = Mathf.Max(0f, settings.assistCircleRadius);
        float range = Mathf.Max(0f, settings.range);
        float angle = Mathf.Clamp(settings.assistAngle, 0f, 180f);
        DrawArc(origin, forward, circle, 180f);
        DrawArc(origin, forward, range, angle);
        Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(-angle, Vector3.up) * forward * range);
        Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(angle, Vector3.up) * forward * range);
    }

    private static void DrawArc(Vector3 origin, Vector3 forward, float radius, float halfAngle)
    {
        const int segments = 48;
        Vector3 previous = origin + Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
            Vector3 point = origin + Quaternion.AngleAxis(angle, Vector3.up) * forward * radius;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }

    private static void DrawVertical(Vector3 point, float halfHeight) =>
        Gizmos.DrawLine(point - Vector3.up * halfHeight, point + Vector3.up * halfHeight);
}

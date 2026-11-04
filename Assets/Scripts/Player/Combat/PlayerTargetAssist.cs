using UnityEngine;

/// <summary>
/// Seleciona um inimigo para auxiliar a mira dos ataques do jogador e captura
/// a posicao desse alvo no instante em que o ataque comeca.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerTargetAssist : MonoBehaviour
{
    [Header("Global Target Assist")]
    [Tooltip("Ativa a selecao de inimigos e o giro assistido no inicio dos ataques do jogador.")]
    [SerializeField] private bool targetAssistEnabled = true;
    [Tooltip("Distancia maxima para selecionar um inimigo como alvo assistido.")]
    [Min(.1f)] [SerializeField] private float targetAssistRange = 16f;
    [Tooltip("Abertura horizontal da assistencia em cada lado quando o alvo esta no limite do alcance.")]
    [Range(0f, 180f)] [SerializeField] private float targetAssistAngle = 55f;
    [Tooltip("Abertura horizontal maxima em cada lado quando o alvo esta perto. Interpola suavemente ate o angulo de longo alcance.")]
    [Range(0f, 180f)] [SerializeField] private float targetAssistNearAngle = 90f;
    [Tooltip("Controla como o angulo de assistencia passa de Target Assist Near Angle (distancia 0) para Target Assist Angle (distancia maxima). X representa a distancia normalizada; Y representa o progresso entre os dois angulos. Uma curva reta faz uma interpolacao uniforme.")]
    [SerializeField] private AnimationCurve targetAssistNearAngleDistanceCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Camadas que podem bloquear a visibilidade entre o jogador e o inimigo assistido.")]
    [SerializeField] private LayerMask targetObstructionMask = Physics.DefaultRaycastLayers;

    private PlayerCharacterController characterController;
    private PlayerLockOn lockOn;
    private Mesh assistAreaMesh;
    private float assistAreaMeshAngle = -1f;
    private float assistAreaMeshNearAngle = -1f;
    private int assistAreaMeshCurveHash;

    public bool IsEnabled => targetAssistEnabled;
    public float MaximumRange => targetAssistRange;
    public EnemyBase CurrentBestTarget => FindBestTarget(targetAssistRange);

    private void Awake()
    {
        characterController = GetComponent<PlayerCharacterController>();
        lockOn = GetComponent<PlayerLockOn>();
    }

    /// <summary>
    /// Copia as configuracoes serializadas no antigo componente de grapple,
    /// usado apenas quando este componente e criado automaticamente.
    /// </summary>
    public void ConfigureFromLegacy(float range, float farAngle,
        float nearAngle, LayerMask obstructionMask, bool enabled)
    {
        targetAssistRange = Mathf.Max(.1f, range);
        targetAssistAngle = Mathf.Clamp(farAngle, 0f, 180f);
        targetAssistNearAngle = Mathf.Clamp(nearAngle, 0f, 180f);
        targetObstructionMask = obstructionMask;
        targetAssistEnabled = enabled;
    }

    /// <summary>Captura o melhor alvo valido e sua posicao mundial atual.</summary>
    public bool TryCaptureTarget(out EnemyBase target,
        out Vector3 lockedPosition)
    {
        return TryCaptureTarget(targetAssistRange, out target,
            out lockedPosition);
    }

    /// <summary>
    /// Captura o alvo travado, quando houver, ou o melhor alvo automatico
    /// dentro do alcance informado pelo ataque.
    /// </summary>
    public bool TryCaptureTarget(float attackRange, out EnemyBase target,
        out Vector3 lockedPosition)
    {
        if (lockOn == null)
            lockOn = GetComponent<PlayerLockOn>();
        EnemyBase lockedEnemy = lockOn != null ? lockOn.LockedEnemy : null;
        // Lock-on e uma escolha explicita do jogador: ele sempre vence a
        // selecao automatica, inclusive fora do alcance da hitbox.
        target = lockedEnemy != null
            ? lockedEnemy
            : targetAssistEnabled
                ? FindBestTarget(Mathf.Min(targetAssistRange,
                    Mathf.Max(0f, attackRange)))
                : null;
        lockedPosition = target != null ? target.AttackOrigin : default;
        return target != null;
    }

    public EnemyBase FindBestTarget(float maximumRange)
    {
        if (!targetAssistEnabled)
            return null;

        Vector3 origin = GetPlayerCenter();
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward,
            Vector3.up).normalized;
        if (forward.sqrMagnitude < .0001f)
            return null;

        EnemyBase best = null;
        float bestScore = float.PositiveInfinity;
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy == null || !IsEligibleTarget(enemy, origin, forward,
                maximumRange))
                continue;

            Vector3 offset = enemy.AttackOrigin - origin;
            float distance = offset.magnitude;
            Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
            float angle = Vector3.Angle(forward, horizontal);
            float score = distance + angle * .025f;
            if (score >= bestScore)
                continue;

            best = enemy;
            bestScore = score;
        }

        return best;
    }

    private bool IsEligibleTarget(EnemyBase enemy, Vector3 origin,
        Vector3 forward, float maximumRange)
    {
        if (enemy == null || !enemy.CanBeLockOnTarget)
            return false;

        Vector3 offset = enemy.AttackOrigin - origin;
        float distance = offset.magnitude;
        Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
        if (distance > maximumRange || horizontal.sqrMagnitude < .0001f)
            return false;

        float angle = Vector3.Angle(forward, horizontal);
        return angle <= GetAssistAngleAtDistance(distance) &&
            !IsTargetOccluded(origin, enemy);
    }

    private bool IsTargetOccluded(Vector3 origin, EnemyBase enemy)
    {
        Vector3 toTarget = enemy.AttackOrigin - origin;
        float distance = toTarget.magnitude;
        if (!Physics.Raycast(origin, toTarget.normalized,
            out RaycastHit hit, distance, targetObstructionMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.collider != null &&
            hit.collider.GetComponentInParent<EnemyBase>() != enemy;
    }

    private float GetAssistAngleAtDistance(float distance)
    {
        float normalizedDistance = Mathf.Clamp01(
            distance / Mathf.Max(.01f, targetAssistRange));
        float angleProgress = targetAssistNearAngleDistanceCurve == null ||
            targetAssistNearAngleDistanceCurve.length == 0
            ? normalizedDistance
            : targetAssistNearAngleDistanceCurve.Evaluate(normalizedDistance);
        return Mathf.LerpUnclamped(targetAssistNearAngle, targetAssistAngle,
            angleProgress);
    }

    private Vector3 GetPlayerCenter()
    {
        Vector3 centerOffset = characterController != null
            ? characterController.center
            : Vector3.zero;
        return transform.position + transform.rotation * centerOffset;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible ||
            !targetAssistEnabled)
        {
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < .0001f)
            return;
        forward.Normalize();

        Vector3 assistOrigin = GetPlayerCenter();
        float groundY = characterController != null
            ? characterController.bounds.min.y
            : transform.position.y;
        assistOrigin.y = groundY + .035f;
        Quaternion areaRotation = Quaternion.LookRotation(forward, Vector3.up);
        float range = Mathf.Max(.1f, targetAssistRange);
        EnsureAssistAreaMesh();

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = Matrix4x4.TRS(
            assistOrigin, areaRotation, new Vector3(range, 1f, range));
        Gizmos.color = new Color(.62f, .08f, .95f, .34f);
        if (assistAreaMesh != null &&
            (targetAssistAngle > 0f || targetAssistNearAngle > 0f))
        {
            Gizmos.DrawMesh(assistAreaMesh);
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = new Color(.82f, .42f, 1f, .98f);
        DrawAssistBoundaries(assistOrigin, forward, range);
        Gizmos.color = previousColor;
    }

    private void EnsureAssistAreaMesh()
    {
        if (assistAreaMesh != null &&
            Mathf.Approximately(assistAreaMeshAngle, targetAssistAngle) &&
            Mathf.Approximately(assistAreaMeshNearAngle, targetAssistNearAngle) &&
            assistAreaMeshCurveHash == GetAngleCurveHash())
        {
            return;
        }

        if (assistAreaMesh == null)
        {
            assistAreaMesh = new Mesh
            {
                name = "Player Target Assist Ground Sector",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        const int radialSegments = 24;
        const int angularSegments = 32;
        Vector3[] vertices = new Vector3[
            1 + radialSegments * (angularSegments + 1)];
        int[] triangles = new int[
            angularSegments * 3 +
            (radialSegments - 1) * angularSegments * 6];
        vertices[0] = Vector3.zero;

        int VertexIndex(int radialIndex, int angleIndex) =>
            1 + (radialIndex - 1) * (angularSegments + 1) + angleIndex;

        for (int radialIndex = 1; radialIndex <= radialSegments; radialIndex++)
        {
            float radius = radialIndex / (float)radialSegments;
            float halfAngle = GetAssistAngleAtDistance(
                radius * targetAssistRange);
            for (int angleIndex = 0; angleIndex <= angularSegments; angleIndex++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle,
                    angleIndex / (float)angularSegments) * Mathf.Deg2Rad;
                vertices[VertexIndex(radialIndex, angleIndex)] =
                    new Vector3(Mathf.Sin(angle) * radius, 0f,
                        Mathf.Cos(angle) * radius);
            }
        }

        int triangleIndex = 0;
        for (int angleIndex = 0; angleIndex < angularSegments; angleIndex++)
        {
            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = VertexIndex(1, angleIndex);
            triangles[triangleIndex++] = VertexIndex(1, angleIndex + 1);
        }

        for (int radialIndex = 2; radialIndex <= radialSegments; radialIndex++)
        {
            for (int angleIndex = 0; angleIndex < angularSegments; angleIndex++)
            {
                int innerStart = VertexIndex(radialIndex - 1, angleIndex);
                int innerNext = VertexIndex(radialIndex - 1, angleIndex + 1);
                int outerStart = VertexIndex(radialIndex, angleIndex);
                int outerNext = VertexIndex(radialIndex, angleIndex + 1);

                triangles[triangleIndex++] = innerStart;
                triangles[triangleIndex++] = outerStart;
                triangles[triangleIndex++] = innerNext;
                triangles[triangleIndex++] = innerNext;
                triangles[triangleIndex++] = outerStart;
                triangles[triangleIndex++] = outerNext;
            }
        }

        assistAreaMesh.Clear();
        assistAreaMesh.vertices = vertices;
        assistAreaMesh.triangles = triangles;
        assistAreaMesh.RecalculateNormals();
        assistAreaMesh.RecalculateBounds();
        assistAreaMeshAngle = targetAssistAngle;
        assistAreaMeshNearAngle = targetAssistNearAngle;
        assistAreaMeshCurveHash = GetAngleCurveHash();
    }

    private int GetAngleCurveHash()
    {
        if (targetAssistNearAngleDistanceCurve == null)
            return 0;

        unchecked
        {
            int hash = 17;
            foreach (Keyframe key in targetAssistNearAngleDistanceCurve.keys)
            {
                hash = hash * 31 + key.time.GetHashCode();
                hash = hash * 31 + key.value.GetHashCode();
                hash = hash * 31 + key.inTangent.GetHashCode();
                hash = hash * 31 + key.outTangent.GetHashCode();
            }
            return hash;
        }
    }

    private void DrawAssistBoundaries(Vector3 origin, Vector3 forward,
        float range)
    {
        const int radialSegments = 48;
        Vector3 previousLeft = origin;
        Vector3 previousRight = origin;
        for (int index = 1; index <= radialSegments; index++)
        {
            float distance = range * index / radialSegments;
            float angle = GetAssistAngleAtDistance(distance);
            Vector3 left = origin +
                Quaternion.AngleAxis(-angle, Vector3.up) * forward * distance;
            Vector3 right = origin +
                Quaternion.AngleAxis(angle, Vector3.up) * forward * distance;
            Gizmos.DrawLine(previousLeft, left);
            Gizmos.DrawLine(previousRight, right);
            previousLeft = left;
            previousRight = right;
        }

        const int arcSegments = 48;
        Vector3 previousArcPoint = origin +
            Quaternion.AngleAxis(-targetAssistAngle, Vector3.up) * forward * range;
        for (int index = 1; index <= arcSegments; index++)
        {
            float angle = Mathf.Lerp(-targetAssistAngle,
                targetAssistAngle, index / (float)arcSegments);
            Vector3 point = origin +
                Quaternion.AngleAxis(angle, Vector3.up) * forward * range;
            Gizmos.DrawLine(previousArcPoint, point);
            previousArcPoint = point;
        }
    }

    private void OnDestroy()
    {
        if (assistAreaMesh == null)
            return;

        Destroy(assistAreaMesh);
        assistAreaMesh = null;
    }
}

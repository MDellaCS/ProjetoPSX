using UnityEngine;

/// <summary>Ferramenta opcional de autoria; nao participa da simulacao.</summary>
[ExecuteAlways]
public sealed class PlayerAttackPreview : MonoBehaviour
{
    private static PlayerAttackPreview editorTarget;
    private static PlayerAttackDefinition editorAttack;
    private static float editorTime;

    [Tooltip("Ataque cujas Hitbox Tracks serao visualizadas na Scene View.")]
    [SerializeField] private PlayerAttackDefinition attack;
    [Tooltip("Progresso normalizado usado para visualizar a fase Active sem entrar em Play Mode.")]
    [Range(0f, 1f)] [SerializeField] private float activePreviewProgress;
    [Tooltip("Cor dos volumes exibidos durante a autoria.")]
    [SerializeField] private Color previewColor = new(1f, .65f, 0f, .8f);

    public static void SetEditorPreview(PlayerAttackPreview target,
        PlayerAttackDefinition definition, float time)
    {
        editorTarget = target;
        editorAttack = definition;
        editorTime = Mathf.Max(0f, time);
    }

    public static void ClearEditorPreview(PlayerAttackDefinition definition)
    {
        if (editorAttack != definition) return;
        editorTarget = null;
        editorAttack = null;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying || editorTarget != this || editorAttack == null)
            return;
        AttackPreviewGrid.Draw(transform, GetComponent<PlayerCharacterController>());
        DrawAttack(editorAttack, editorTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying || editorTarget == this) return;
        float activeTime = attack == null ? 0f : attack.windupDuration +
            activePreviewProgress * attack.activeDuration;
        DrawAttack(attack, activeTime);
    }

    private void DrawAttack(PlayerAttackDefinition definition, float previewTime)
    {
        if (definition == null) return;
        PlayerCharacterController controller = GetComponent<PlayerCharacterController>();
        CharacterController legacyController = controller == null
            ? GetComponent<CharacterController>() : null;
        Vector3 capsuleCenter = controller != null
            ? controller.center
            : legacyController != null ? legacyController.center : Vector3.zero;
        float capsuleRadius = controller != null
            ? controller.radius
            : legacyController != null ? legacyController.radius : .5f;
        Vector3 origin = transform.position + transform.rotation * capsuleCenter;
        Vector3 extendedArmOrigin = origin + transform.up * GetExtendedArmHeightOffset();
        float windupEnd = definition.windupDuration;
        float activeEnd = windupEnd + definition.activeDuration;
        DrawMovementTrajectory(definition, origin, previewTime, activeEnd);
        if (definition.hitboxes == null) return;
        if (previewTime < windupEnd)
        {
            DrawPhaseRing(origin, capsuleRadius, Color.green);
            return;
        }
        if (previewTime >= activeEnd)
        {
            DrawPhaseRing(origin, capsuleRadius, new Color(1f, .45f, 0f));
            return;
        }

        float progress = Mathf.InverseLerp(windupEnd, activeEnd, previewTime);
        Gizmos.color = previewColor;
        float extendedArmRadius = GetExtendedArmRadius();
        {
            float strikeProgress = progress;
            foreach (PlayerHitboxTrackDefinition track in definition.hitboxes)
            {
                if (track == null || strikeProgress < track.activeStartNormalized || strikeProgress > track.activeEndNormalized)
                    continue;
                float t = Mathf.InverseLerp(track.activeStartNormalized, track.activeEndNormalized, strikeProgress);
                t = PlayerHitboxRepeat.GetProgress(track, t, out _, out bool mirror);
                GetPose(track, origin, t, out Vector3 center, out Quaternion rotation,
                    out Vector3 capsuleA, out Vector3 capsuleB,
                    out float effectiveRadius);
                if (mirror)
                    PlayerHitboxRepeat.MirrorPose(transform, origin, ref center,
                        ref rotation, ref capsuleA, ref capsuleB);
            if (track.shape == PlayerHitboxShape.Sphere) Gizmos.DrawWireSphere(center, effectiveRadius);
            else if (track.shape == PlayerHitboxShape.Box)
            { Matrix4x4 old = Gizmos.matrix; Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one); Gizmos.DrawWireCube(Vector3.zero, track.boxSize); Gizmos.matrix = old; }
            else
            {
                DrawWireCapsule(capsuleA, capsuleB, effectiveRadius);
            }
            if (definition.usesExtendedArmHitbox && extendedArmRadius > 0f)
                DrawWireCapsule(extendedArmOrigin, center, extendedArmRadius);
            }
        }
    }

    /// <summary>Desenha a trajetoria de autoria; nunca aparece durante o jogo.</summary>
    private void DrawMovementTrajectory(PlayerAttackDefinition definition,
        Vector3 origin, float previewTime, float windupActiveDuration)
    {
        if (definition.allowMovementWhileAttacking) return;
        PlayerAttackMovementDefinition movement = definition.Movement;
        if (movement == null || Mathf.Approximately(movement.forwardDistance, 0f) ||
            movement.endNormalized <= movement.startNormalized)
        {
            return;
        }

        const int segments = 24;
        Color previousColor = Gizmos.color;
        Gizmos.color = new Color(.1f, .85f, 1f, .85f);
        Vector3 previous = origin;
        for (int index = 1; index <= segments; index++)
        {
            float progress = index / (float)segments;
            Vector3 point = origin + transform.forward *
                (movement.forwardDistance * EvaluateMovement(movement,
                    progress));
            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.DrawWireSphere(origin, .06f);
        Gizmos.DrawWireSphere(previous, .09f);
        float timelineProgress = windupActiveDuration <= 0f ? 1f :
            Mathf.Clamp01(previewTime / windupActiveDuration);
        Vector3 current = origin + transform.forward *
            (movement.forwardDistance * EvaluateMovement(movement,
                timelineProgress));
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(current, .07f);
        Gizmos.color = previousColor;
    }

    private static float EvaluateMovement(PlayerAttackMovementDefinition movement,
        float overallProgress)
    {
        if (overallProgress <= movement.startNormalized) return 0f;
        if (overallProgress >= movement.endNormalized) return 1f;
        float normalized = Mathf.InverseLerp(movement.startNormalized,
            movement.endNormalized, overallProgress);
        return movement.movementCurve == null || movement.movementCurve.length == 0
            ? normalized
            : Mathf.Clamp01(movement.movementCurve.Evaluate(normalized));
    }

    private static void DrawPhaseRing(Vector3 origin, float controllerRadius,
        Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(origin, Mathf.Max(.3f, controllerRadius * 1.15f));
    }

    private float GetExtendedArmRadius()
    {
        PlayerMovesetController controller =
            GetComponent<PlayerMovesetController>();
        return controller != null && controller.Moveset != null
            ? Mathf.Max(.01f,
                controller.Moveset.extendedArmHitboxRadius)
            : .25f;
    }

    private float GetExtendedArmHeightOffset()
    {
        PlayerMovesetController controller =
            GetComponent<PlayerMovesetController>();
        return controller != null && controller.Moveset != null
            ? controller.Moveset.extendedArmHitboxHeightOffset
            : 0f;
    }

    private void GetPose(PlayerHitboxTrackDefinition track, Vector3 origin, float progress,
        out Vector3 center, out Quaternion rotation, out Vector3 capsuleA,
        out Vector3 capsuleB, out float effectiveRadius)
    {
        if (track.anchorMode == PlayerHitboxAnchorMode.FollowAnchors)
        {
            PlayerHitboxAnchor primary = FindAnchor(track.primaryAnchorId);
            PlayerHitboxAnchor secondary = FindAnchor(track.secondaryAnchorId);
            if (primary != null)
            {
                center = primary.transform.position;
                rotation = primary.transform.rotation;
                effectiveRadius = track.radius;
                if (secondary != null)
                {
                    capsuleA = primary.transform.position;
                    capsuleB = secondary.transform.position;
                    center = (capsuleA + capsuleB) * .5f;
                    return;
                }
                GetCapsuleEndpoints(track, center, rotation,
                    effectiveRadius, out capsuleA, out capsuleB);
                return;
            }
        }
        Vector3 local;
        if (track.trajectoryMode == PlayerHitboxTrajectoryMode.Orbit)
        {
            float angleProgress = Evaluate(track.orbitAngleCurve, progress);
            float distanceProgress = Evaluate(track.orbitDistanceCurve, progress);
            float radiusProgress = Evaluate(track.orbitRadiusCurve, progress);
            float angle = Mathf.LerpUnclamped(track.orbitStartAngleDegrees,
                track.orbitEndAngleDegrees, angleProgress) * Mathf.Deg2Rad;
            float distance = Mathf.LerpUnclamped(track.orbitStartDistance,
                track.orbitEndDistance, distanceProgress);
            local = new Vector3(Mathf.Sin(angle) * distance,
                track.orbitHeight, Mathf.Cos(angle) * distance);
            effectiveRadius = Mathf.Max(.01f, Mathf.LerpUnclamped(
                track.orbitStartRadius, track.orbitEndRadius, radiusProgress));
        }
        else
        {
            local = Vector3.Lerp(track.startLocalPosition,
                track.endLocalPosition, progress);
            effectiveRadius = track.radius;
        }
        center = origin + transform.rotation * local;
        rotation = transform.rotation * Quaternion.Euler(Vector3.Lerp(track.startLocalEuler, track.endLocalEuler, progress));
        GetCapsuleEndpoints(track, center, rotation, effectiveRadius,
            out capsuleA, out capsuleB);
    }

    private PlayerHitboxAnchor FindAnchor(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (PlayerHitboxAnchor anchor in GetComponentsInChildren<PlayerHitboxAnchor>(true))
            if (anchor != null && anchor.AnchorId == id) return anchor;
        return null;
    }

    private static void GetCapsuleEndpoints(PlayerHitboxTrackDefinition track,
        Vector3 center, Quaternion rotation, float effectiveRadius,
        out Vector3 a, out Vector3 b)
    {
        Vector3 axis = track.capsuleAxis == PlayerHitboxAxis.X ? Vector3.right :
            track.capsuleAxis == PlayerHitboxAxis.Y ? Vector3.up : Vector3.forward;
        Vector3 offset = rotation * axis * Mathf.Max(0f,
            track.capsuleHeight * .5f - effectiveRadius);
        a = center - offset;
        b = center + offset;
    }

    private static float Evaluate(AnimationCurve curve, float progress) =>
        curve == null || curve.length == 0
            ? progress
            : curve.Evaluate(progress);

    private static void DrawWireCapsule(Vector3 a, Vector3 b, float radius)
    {
        Gizmos.DrawWireSphere(a, radius);
        Gizmos.DrawWireSphere(b, radius);
        Vector3 axis = (b - a).normalized;
        Vector3 side = Vector3.Cross(axis, Vector3.up);
        if (side.sqrMagnitude < .001f)
            side = Vector3.Cross(axis, Vector3.right);
        side.Normalize();
        Vector3 secondSide = Vector3.Cross(axis, side).normalized;
        Gizmos.DrawLine(a + side * radius, b + side * radius);
        Gizmos.DrawLine(a - side * radius, b - side * radius);
        Gizmos.DrawLine(a + secondSide * radius, b + secondSide * radius);
        Gizmos.DrawLine(a - secondSide * radius, b - secondSide * radius);
    }
}

/// <summary>Grade de referencia para os previews de ataques no editor.</summary>
internal static class AttackPreviewGrid
{
    public static void Draw(Transform origin, PlayerCharacterController player)
    {
        const int extent = 16;
        const int spacing = 2;
        float groundY = origin.position.y;
        if (player != null)
        {
            Transform playerTransform = player.transform;
            Vector3 scale = playerTransform.lossyScale;
            float radius = player.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float height = Mathf.Max(player.height * Mathf.Abs(scale.y), radius * 2f);
            Vector3 center = playerTransform.TransformPoint(player.center);
            groundY = center.y - Mathf.Abs(playerTransform.up.y) *
                (height * .5f - radius) - radius;
        }

        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(.65f, .65f, .65f, .5f);
        Vector3 position = origin.position;
        for (int offset = -extent; offset <= extent; offset += spacing)
        {
            Gizmos.DrawLine(new Vector3(position.x + offset, groundY, position.z - extent),
                new Vector3(position.x + offset, groundY, position.z + extent));
            Gizmos.DrawLine(new Vector3(position.x - extent, groundY, position.z + offset),
                new Vector3(position.x + extent, groundY, position.z + offset));
        }
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}

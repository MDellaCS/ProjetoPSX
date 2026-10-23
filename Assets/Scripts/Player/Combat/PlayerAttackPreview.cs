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
        if (definition == null || definition.strikes == null) return;
        CharacterController controller = GetComponent<CharacterController>();
        Vector3 origin = transform.position + transform.rotation * (controller != null ? controller.center : Vector3.zero);
        float windupEnd = definition.windupDuration;
        float activeEnd = windupEnd + definition.activeDuration;
        if (previewTime < windupEnd)
        {
            DrawPhaseRing(origin, controller, Color.green);
            return;
        }
        if (previewTime >= activeEnd)
        {
            DrawPhaseRing(origin, controller, new Color(1f, .45f, 0f));
            return;
        }

        float progress = Mathf.InverseLerp(windupEnd, activeEnd, previewTime);
        Gizmos.color = previewColor;
        float extendedArmRadius = definition.usesExtendedArmHitbox
            ? GetExtendedArmRadius()
            : 0f;
        foreach (PlayerAttackStrikeDefinition strike in definition.strikes)
        {
            if (strike == null || strike.hitboxes == null ||
                progress < strike.activeStartNormalized || progress > strike.activeEndNormalized)
                continue;
            float strikeProgress = Mathf.InverseLerp(strike.activeStartNormalized, strike.activeEndNormalized, progress);
            foreach (PlayerHitboxTrackDefinition track in strike.hitboxes)
            {
                if (track == null || strikeProgress < track.activeStartNormalized || strikeProgress > track.activeEndNormalized)
                    continue;
                float t = Mathf.InverseLerp(track.activeStartNormalized, track.activeEndNormalized, strikeProgress);
                GetPose(track, origin, t, out Vector3 center, out Quaternion rotation,
                    out Vector3 capsuleA, out Vector3 capsuleB,
                    out float effectiveRadius);
            if (track.shape == PlayerHitboxShape.Sphere) Gizmos.DrawWireSphere(center, effectiveRadius);
            else if (track.shape == PlayerHitboxShape.Box)
            { Matrix4x4 old = Gizmos.matrix; Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one); Gizmos.DrawWireCube(Vector3.zero, track.boxSize); Gizmos.matrix = old; }
            else
            {
                DrawWireCapsule(capsuleA, capsuleB, effectiveRadius);
            }
            if (extendedArmRadius > 0f)
                DrawWireCapsule(origin, center, extendedArmRadius);
            }
        }
    }

    private static void DrawPhaseRing(Vector3 origin, CharacterController controller,
        Color color)
    {
        Gizmos.color = color;
        float radius = controller != null ? Mathf.Max(.3f, controller.radius * 1.15f) : .65f;
        Gizmos.DrawWireSphere(origin, radius);
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

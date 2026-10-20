using UnityEngine;

/// <summary>Preview de autoria; nunca controla a simulacao do ataque.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class EnemyAttackPreview : MonoBehaviour
{
    private static EnemyAttackPreview editorTarget;
    private static EnemyAttackDefinition editorAttack;
    private static float editorProgress;

    [Tooltip("Ataque exibido ao selecionar este inimigo diretamente.")]
    [SerializeField] private EnemyAttackDefinition attack;
    [Tooltip("Progresso normalizado da fase Active usado fora do Play Mode.")]
    [Range(0f, 1f)] [SerializeField] private float activePreviewProgress;
    [Tooltip("Cor das hitboxes no preview de autoria.")]
    [SerializeField] private Color previewColor = new(1f, .25f, .1f, .85f);

    public static void SetEditorPreview(EnemyAttackPreview target,
        EnemyAttackDefinition definition, float progress)
    {
        editorTarget = target;
        editorAttack = definition;
        editorProgress = Mathf.Clamp01(progress);
    }

    public static void ClearEditorPreview(EnemyAttackDefinition definition)
    {
        if (editorAttack != definition) return;
        editorTarget = null;
        editorAttack = null;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && editorTarget == this && editorAttack != null)
            DrawAttack(editorAttack, editorProgress);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying && editorTarget != this)
            DrawAttack(attack, activePreviewProgress);
    }

    private void DrawAttack(EnemyAttackDefinition definition, float progress)
    {
        if (definition == null || definition.strikes == null) return;
        Gizmos.color = previewColor;
        foreach (EnemyAttackStrikeDefinition strike in definition.strikes)
        {
            if (strike == null || progress < strike.activeStartNormalized ||
                progress > strike.activeEndNormalized) continue;
            float strikeProgress = Mathf.InverseLerp(strike.activeStartNormalized,
                strike.activeEndNormalized, progress);
            foreach (EnemyHitboxTrackDefinition track in strike.hitboxes)
            {
                if (track == null || strikeProgress < track.activeStartNormalized ||
                    strikeProgress > track.activeEndNormalized) continue;
                float t = Mathf.InverseLerp(track.activeStartNormalized,
                    track.activeEndNormalized, strikeProgress);
                GetPose(track, t, out Vector3 center, out Quaternion rotation,
                    out Vector3 a, out Vector3 b, out float radius);
                if (track.shape == EnemyHitboxShape.Sphere)
                    Gizmos.DrawWireSphere(center, radius);
                else if (track.shape == EnemyHitboxShape.Box)
                {
                    Matrix4x4 old = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, track.boxSize);
                    Gizmos.matrix = old;
                }
                else DrawCapsule(a, b, radius);
                if (definition.usesExtendedArmHitbox)
                    DrawCapsule(GetCenter(), center, GetExtendedRadius());
            }
        }
    }

    private void GetPose(EnemyHitboxTrackDefinition track, float progress,
        out Vector3 center, out Quaternion rotation, out Vector3 a,
        out Vector3 b, out float radius)
    {
        if (track.anchorMode == EnemyHitboxAnchorMode.FollowAnchors)
        {
            EnemyHitboxAnchor primary = FindAnchor(track.primaryAnchorId);
            EnemyHitboxAnchor secondary = FindAnchor(track.secondaryAnchorId);
            if (primary != null)
            {
                center = primary.transform.position;
                rotation = primary.transform.rotation;
                radius = track.radius;
                if (secondary != null)
                {
                    a = primary.transform.position;
                    b = secondary.transform.position;
                    center = (a + b) * .5f;
                }
                else GetCapsule(track, center, rotation, radius, out a, out b);
                return;
            }
        }
        Vector3 local;
        if (track.trajectoryMode == EnemyHitboxTrajectoryMode.Orbit)
        {
            float angle = Mathf.LerpUnclamped(track.orbitStartAngleDegrees,
                track.orbitEndAngleDegrees, Evaluate(track.orbitAngleCurve,
                progress)) * Mathf.Deg2Rad;
            float distance = Mathf.LerpUnclamped(track.orbitStartDistance,
                track.orbitEndDistance, Evaluate(track.orbitDistanceCurve,
                progress));
            local = new Vector3(Mathf.Sin(angle) * distance, track.orbitHeight,
                Mathf.Cos(angle) * distance);
            radius = Mathf.LerpUnclamped(track.orbitStartRadius,
                track.orbitEndRadius, Evaluate(track.orbitRadiusCurve, progress));
        }
        else
        {
            local = Vector3.Lerp(track.startLocalPosition,
                track.endLocalPosition, progress);
            radius = track.radius;
        }
        center = GetCenter() + transform.rotation * local;
        rotation = transform.rotation * Quaternion.Euler(Vector3.Lerp(
            track.startLocalEuler, track.endLocalEuler, progress));
        GetCapsule(track, center, rotation, radius, out a, out b);
    }

    private Vector3 GetCenter()
    {
        CharacterController controller = GetComponent<CharacterController>();
        return transform.position + transform.rotation *
            (controller != null ? controller.center : Vector3.zero);
    }

    private float GetExtendedRadius()
    {
        EnemyAttackController controller = GetComponent<EnemyAttackController>();
        return controller != null && controller.Moveset != null
            ? controller.Moveset.extendedArmHitboxRadius : .25f;
    }

    private EnemyHitboxAnchor FindAnchor(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (EnemyHitboxAnchor anchor in
            GetComponentsInChildren<EnemyHitboxAnchor>(true))
            if (anchor.AnchorId == id) return anchor;
        return null;
    }

    private static void GetCapsule(EnemyHitboxTrackDefinition track,
        Vector3 center, Quaternion rotation, float radius, out Vector3 a,
        out Vector3 b)
    {
        Vector3 axis = track.capsuleAxis == EnemyHitboxAxis.X ? Vector3.right :
            track.capsuleAxis == EnemyHitboxAxis.Y ? Vector3.up : Vector3.forward;
        Vector3 offset = rotation * axis * Mathf.Max(0f,
            track.capsuleHeight * .5f - radius);
        a = center - offset;
        b = center + offset;
    }

    private static float Evaluate(AnimationCurve curve, float progress) =>
        curve == null || curve.length == 0 ? progress : curve.Evaluate(progress);

    private static void DrawCapsule(Vector3 a, Vector3 b, float radius)
    {
        Gizmos.DrawWireSphere(a, radius);
        Gizmos.DrawWireSphere(b, radius);
        Gizmos.DrawLine(a, b);
    }
}

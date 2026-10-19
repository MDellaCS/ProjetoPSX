using UnityEngine;

/// <summary>Ferramenta opcional de autoria; nao participa da simulacao.</summary>
[ExecuteAlways]
public sealed class PlayerAttackPreview : MonoBehaviour
{
    private static PlayerAttackPreview editorTarget;
    private static PlayerAttackDefinition editorAttack;
    private static float editorProgress;

    [Tooltip("Ataque cujas Hitbox Tracks serao visualizadas na Scene View.")]
    [SerializeField] private PlayerAttackDefinition attack;
    [Tooltip("Progresso normalizado usado para visualizar a fase Active sem entrar em Play Mode.")]
    [Range(0f, 1f)] [SerializeField] private float activePreviewProgress;
    [Tooltip("Cor dos volumes exibidos durante a autoria.")]
    [SerializeField] private Color previewColor = new(1f, .65f, 0f, .8f);

    public static void SetEditorPreview(PlayerAttackPreview target,
        PlayerAttackDefinition definition, float progress)
    {
        editorTarget = target;
        editorAttack = definition;
        editorProgress = Mathf.Clamp01(progress);
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
        DrawAttack(editorAttack, editorProgress);
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying || editorTarget == this) return;
        DrawAttack(attack, activePreviewProgress);
    }

    private void DrawAttack(PlayerAttackDefinition definition, float progress)
    {
        if (definition == null || definition.strikes == null) return;
        CharacterController controller = GetComponent<CharacterController>();
        Vector3 origin = transform.position + transform.rotation * (controller != null ? controller.center : Vector3.zero);
        Gizmos.color = previewColor;
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
                    out Vector3 capsuleA, out Vector3 capsuleB);
            if (track.shape == PlayerHitboxShape.Sphere) Gizmos.DrawWireSphere(center, track.radius);
            else if (track.shape == PlayerHitboxShape.Box)
            { Matrix4x4 old = Gizmos.matrix; Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one); Gizmos.DrawWireCube(Vector3.zero, track.boxSize); Gizmos.matrix = old; }
            else
            {
                Gizmos.DrawWireSphere(capsuleA, track.radius); Gizmos.DrawWireSphere(capsuleB, track.radius);
                Gizmos.DrawLine(capsuleA, capsuleB);
            }
            }
        }
    }

    private void GetPose(PlayerHitboxTrackDefinition track, Vector3 origin, float progress,
        out Vector3 center, out Quaternion rotation, out Vector3 capsuleA, out Vector3 capsuleB)
    {
        if (track.anchorMode == PlayerHitboxAnchorMode.FollowAnchors)
        {
            PlayerHitboxAnchor primary = FindAnchor(track.primaryAnchorId);
            PlayerHitboxAnchor secondary = FindAnchor(track.secondaryAnchorId);
            if (primary != null)
            {
                center = primary.transform.position;
                rotation = primary.transform.rotation;
                if (secondary != null)
                {
                    capsuleA = primary.transform.position;
                    capsuleB = secondary.transform.position;
                    center = (capsuleA + capsuleB) * .5f;
                    return;
                }
                GetCapsuleEndpoints(track, center, rotation, out capsuleA, out capsuleB);
                return;
            }
        }
        center = origin + transform.rotation * Vector3.Lerp(track.startLocalPosition, track.endLocalPosition, progress);
        rotation = transform.rotation * Quaternion.Euler(Vector3.Lerp(track.startLocalEuler, track.endLocalEuler, progress));
        GetCapsuleEndpoints(track, center, rotation, out capsuleA, out capsuleB);
    }

    private PlayerHitboxAnchor FindAnchor(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (PlayerHitboxAnchor anchor in GetComponentsInChildren<PlayerHitboxAnchor>(true))
            if (anchor != null && anchor.AnchorId == id) return anchor;
        return null;
    }

    private static void GetCapsuleEndpoints(PlayerHitboxTrackDefinition track,
        Vector3 center, Quaternion rotation, out Vector3 a, out Vector3 b)
    {
        Vector3 axis = track.capsuleAxis == PlayerHitboxAxis.X ? Vector3.right :
            track.capsuleAxis == PlayerHitboxAxis.Y ? Vector3.up : Vector3.forward;
        Vector3 offset = rotation * axis * Mathf.Max(0f, track.capsuleHeight * .5f - track.radius);
        a = center - offset;
        b = center + offset;
    }
}

using UnityEngine;

/// <summary>Ferramenta opcional de autoria; nao participa da simulacao.</summary>
[ExecuteAlways]
public sealed class PlayerAttackPreview : MonoBehaviour
{
    [Tooltip("Ataque cujas Hitbox Tracks serao visualizadas na Scene View.")]
    [SerializeField] private PlayerAttackDefinition attack;
    [Tooltip("Progresso normalizado usado para visualizar a fase Active sem entrar em Play Mode.")]
    [Range(0f, 1f)] [SerializeField] private float activePreviewProgress;
    [Tooltip("Cor dos volumes exibidos durante a autoria.")]
    [SerializeField] private Color previewColor = new(1f, .65f, 0f, .8f);

    private void OnDrawGizmosSelected()
    {
        if (attack == null) return;
        CharacterController controller = GetComponent<CharacterController>();
        Vector3 origin = transform.position + transform.rotation * (controller != null ? controller.center : Vector3.zero);
        Gizmos.color = previewColor;
        foreach (PlayerAttackStrikeDefinition strike in attack.strikes)
        foreach (PlayerHitboxTrackDefinition track in strike.hitboxes)
        {
            float t = Mathf.InverseLerp(track.activeStartNormalized, track.activeEndNormalized, activePreviewProgress);
            Vector3 center = origin + transform.rotation * Vector3.Lerp(track.startLocalPosition, track.endLocalPosition, t);
            Quaternion rotation = transform.rotation * Quaternion.Euler(Vector3.Lerp(track.startLocalEuler, track.endLocalEuler, t));
            if (track.shape == PlayerHitboxShape.Sphere) Gizmos.DrawWireSphere(center, track.radius);
            else if (track.shape == PlayerHitboxShape.Box)
            { Matrix4x4 old = Gizmos.matrix; Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one); Gizmos.DrawWireCube(Vector3.zero, track.boxSize); Gizmos.matrix = old; }
            else
            {
                Vector3 axis = track.capsuleAxis == PlayerHitboxAxis.X ? Vector3.right : track.capsuleAxis == PlayerHitboxAxis.Y ? Vector3.up : Vector3.forward;
                float half = Mathf.Max(0f, track.capsuleHeight * .5f - track.radius);
                Vector3 offset = rotation * axis * half;
                Gizmos.DrawWireSphere(center - offset, track.radius); Gizmos.DrawWireSphere(center + offset, track.radius);
                Gizmos.DrawLine(center - offset, center + offset);
            }
        }
    }
}

using UnityEngine;

/// <summary>
/// Gizmos padrao das areas de interacao. Aparecem no Editor fora do Play e,
/// durante o jogo, so com o F3 (debug) aberto.
/// </summary>
public static class InteractionGizmos
{
    public static readonly Color ZoneFill = new(.1f, .75f, 1f, .12f);
    public static readonly Color ZoneLine = new(.1f, .75f, 1f, .9f);

    public static bool Visible => !Application.isPlaying || PlayerDebugToolkitHUD.IsVisible;

    /// <summary>Desenha o collider como area (caixa, esfera, capsula ou limites).</summary>
    public static void DrawCollider(Collider collider, Color fill, Color line)
    {
        if (collider == null || !collider.enabled)
            return;

        Matrix4x4 previous = Gizmos.matrix;
        switch (collider)
        {
            case BoxCollider box:
                Gizmos.matrix = box.transform.localToWorldMatrix;
                DrawBox(box.center, box.size, fill, line);
                break;
            case SphereCollider sphere:
                Gizmos.matrix = Matrix4x4.TRS(sphere.transform.TransformPoint(sphere.center),
                    Quaternion.identity, Vector3.one);
                float scale = Mathf.Max(Mathf.Abs(sphere.transform.lossyScale.x),
                    Mathf.Abs(sphere.transform.lossyScale.y),
                    Mathf.Abs(sphere.transform.lossyScale.z));
                Gizmos.color = fill;
                Gizmos.DrawSphere(Vector3.zero, sphere.radius * scale);
                Gizmos.color = line;
                Gizmos.DrawWireSphere(Vector3.zero, sphere.radius * scale);
                break;
            default:
                Gizmos.matrix = Matrix4x4.identity;
                DrawBox(collider.bounds.center, collider.bounds.size, fill, line);
                break;
        }

        Gizmos.matrix = previous;
    }

    /// <summary>Caixa preenchida com contorno, no espaco atual de Gizmos.matrix.</summary>
    public static void DrawBox(Vector3 center, Vector3 size, Color fill, Color line)
    {
        Gizmos.color = fill;
        Gizmos.DrawCube(center, size);
        Gizmos.color = line;
        Gizmos.DrawWireCube(center, size);
    }
}

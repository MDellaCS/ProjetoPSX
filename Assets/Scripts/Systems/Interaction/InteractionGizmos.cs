using UnityEngine;

/// <summary>
/// Gizmos padrao das areas de interacao. Aparecem no Editor fora do Play e,
/// durante o jogo, so com o F3 (debug) aberto.
/// </summary>
public static class InteractionGizmos
{
    public static readonly Color ZoneFill = new(.1f, .75f, 1f, .12f);
    public static readonly Color ZoneLine = new(.1f, .75f, 1f, .9f);

    public static bool Visible => DebugGizmoSettings.Enabled(DebugGizmoGroup.Interaction);

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

public enum DebugGizmoGroup { Interaction, Lights, Camera, Water, World, Encounters, Spawns, PlayerCombat, EnemyCombat, Swing, FlyingPuppet,
    PlayerHitboxes, PlayerLockOn, EnemyHitboxes, EnemyDistance,
    WorldObjects, WorldLevers, WorldCheckpoints, WorldAreaTitles, WorldPrompts,
    EncounterTriggers, EncounterSpawnAreas, SwingAnchors, SwingRope,
    FlyingPuppetBones, FlyingPuppetLanding, DamageNumbers, PlayerDps,
    DebugHud, EnemyAggression, PlayerCurrentAttack, PlayerAttackBpm }

public static class DebugGizmoSettings
{
    private static readonly System.Collections.Generic.Dictionary<DebugGizmoGroup, bool> values = new();

    public static bool Enabled(DebugGizmoGroup group) => !Application.isPlaying ||
        (PlayerDebugToolkitHUD.IsVisible && Selected(group));

    public static bool Selected(DebugGizmoGroup group)
    {
        if (!values.TryGetValue(group, out bool value))
        {
            value = PlayerPrefs.GetInt("Debug.Gizmo." + group,
                PlayerPrefs.GetInt("Debug.Gizmo." + PreviousGroup(group), 1)) != 0;
            values[group] = value;
        }
        return value;
    }

    private static DebugGizmoGroup PreviousGroup(DebugGizmoGroup group)
    {
        switch (group)
        {
            case DebugGizmoGroup.PlayerHitboxes:
            case DebugGizmoGroup.PlayerLockOn: return DebugGizmoGroup.PlayerCombat;
            case DebugGizmoGroup.EnemyHitboxes:
            case DebugGizmoGroup.EnemyDistance: return DebugGizmoGroup.EnemyCombat;
            case DebugGizmoGroup.WorldObjects:
            case DebugGizmoGroup.WorldLevers:
            case DebugGizmoGroup.WorldCheckpoints:
            case DebugGizmoGroup.WorldAreaTitles:
            case DebugGizmoGroup.WorldPrompts: return DebugGizmoGroup.World;
            case DebugGizmoGroup.EncounterTriggers:
            case DebugGizmoGroup.EncounterSpawnAreas: return DebugGizmoGroup.Encounters;
            case DebugGizmoGroup.SwingAnchors:
            case DebugGizmoGroup.SwingRope: return DebugGizmoGroup.Swing;
            case DebugGizmoGroup.FlyingPuppetBones:
            case DebugGizmoGroup.FlyingPuppetLanding: return DebugGizmoGroup.FlyingPuppet;
            default: return group;
        }
    }

    public static void Set(DebugGizmoGroup group, bool value)
    {
        values[group] = value;
        PlayerPrefs.SetInt("Debug.Gizmo." + group, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PlayerStarterMovesetCreator
{
    private const string Folder = "Assets/GameData/Player/Moveset";

    [MenuItem("Tools/Projeto PSX/Player Combat/Create Starter Moveset")]
    public static void CreateStarterMoveset()
    {
        if (AssetDatabase.LoadAssetAtPath<PlayerMovesetDefinition>($"{Folder}/StarterMoveset.asset") != null)
        {
            EditorUtility.DisplayDialog("Starter Moveset", "Os assets iniciais ja existem. Apague-os manualmente se realmente quiser recria-los.", "OK");
            return;
        }
        EnsureFolder("Assets", "GameData");
        EnsureFolder("Assets/GameData", "Player");
        EnsureFolder("Assets/GameData/Player", "Moveset");
        PlayerAttackDefinition light = CreateAttack("L1", PlayerAttackKind.Light, 10, 50, .05f, .08f, .05f, .2f);
        light.hitboxes.Add(new PlayerHitboxTrackDefinition
        {
            shape = PlayerHitboxShape.Capsule,
            startLocalPosition = new Vector3(0f, 0f, .7f), endLocalPosition = new Vector3(0f, 0f, 1.7f),
            radius = .4f, capsuleHeight = 2.6f, capsuleAxis = PlayerHitboxAxis.Z,
            hitGroup = 0
        });
        AssetDatabase.CreateAsset(light, $"{Folder}/L1.asset");

        PlayerAttackDefinition heavy = CreateAttack("H1", PlayerAttackKind.Heavy, 120, 60, .5f, .03f, .35f, 1f);
        heavy.generatesHeatOnHit = false;
        heavy.hitboxes.Add(new PlayerHitboxTrackDefinition
        {
            shape = PlayerHitboxShape.Sphere,
            startLocalPosition = new Vector3(0f, 0f, 2f), endLocalPosition = new Vector3(0f, 0f, 2f),
            radius = 2f, hitGroup = 0
        });
        AssetDatabase.CreateAsset(heavy, $"{Folder}/H1.asset");

        PlayerMovesetDefinition moveset = ScriptableObject.CreateInstance<PlayerMovesetDefinition>();
        moveset.entries = new List<PlayerMovesetEntry>
        {
            new() { input = PlayerAttackInput.Light, context = PlayerAttackContext.Ground, targetNodeId = "L1" },
            new() { input = PlayerAttackInput.Heavy, context = PlayerAttackContext.Ground, targetNodeId = "H1" }
        };
        moveset.nodes = new List<PlayerMoveNode>
        {
            new() { nodeId = "L1", attack = light }, new() { nodeId = "H1", attack = heavy }
        };
        AssetDatabase.CreateAsset(moveset, $"{Folder}/StarterMoveset.asset");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Selection.activeObject = moveset;
    }

    [MenuItem("Tools/Projeto PSX/Player Combat/Configure Selected Player")]
    public static void ConfigureSelectedPlayer()
    {
        GameObject player = Selection.activeGameObject;
        PlayerMovesetDefinition moveset = AssetDatabase.LoadAssetAtPath<PlayerMovesetDefinition>($"{Folder}/StarterMoveset.asset");
        if (player == null || moveset == null)
        {
            EditorUtility.DisplayDialog("Configure Player", "Selecione o Player na Hierarchy e crie o Starter Moveset primeiro.", "OK");
            return;
        }
        Undo.RegisterFullObjectHierarchyUndo(player, "Configure Player Combat");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);
        GetOrAdd<PlayerCombatStatus>(player);
        GetOrAdd<PlayerAttackMovement>(player);
        GetOrAdd<PlayerAttackResolver>(player);
        GetOrAdd<PlayerHitboxController>(player);
        GetOrAdd<PlayerAttackController>(player);
        PlayerMovesetController controller = GetOrAdd<PlayerMovesetController>(player);
        GetOrAdd<PlayerAttackDebug>(player);
        PlayerAttackPreview preview = GetOrAdd<PlayerAttackPreview>(player);
        SerializedObject serialized = new(controller);
        serialized.FindProperty("moveset").objectReferenceValue = moveset;
        serialized.ApplyModifiedProperties();
        SerializedObject previewSerialized = new(preview);
        previewSerialized.FindProperty("attack").objectReferenceValue = moveset.FindNode("L1")?.attack;
        previewSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(player);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }

    private static PlayerAttackDefinition CreateAttack(string id, PlayerAttackKind kind,
        int damage, int poise, float windup, float active, float recovery, float movement)
    {
        PlayerAttackDefinition attack = ScriptableObject.CreateInstance<PlayerAttackDefinition>();
        attack.attackId = id; attack.kind = kind; attack.context = PlayerAttackContext.Ground;
        attack.baseDamage = damage; attack.basePoiseDamage = poise;
        attack.windupDuration = windup; attack.activeDuration = active; attack.recoveryDuration = recovery;
        PlayerAttackMovementProfile profile = ScriptableObject.CreateInstance<PlayerAttackMovementProfile>();
        profile.movement.forwardDistance = movement;
        profile.movement.startNormalized = 0f;
        profile.movement.endNormalized = kind == PlayerAttackKind.Light ? .5f : 1f;
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath($"{Folder}/{id}_Movement.asset"));
        attack.movementProfile = profile;
        return attack;
    }
}

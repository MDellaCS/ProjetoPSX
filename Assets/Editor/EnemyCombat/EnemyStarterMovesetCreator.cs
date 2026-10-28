using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Cria e conecta os movesets dos inimigos iniciais.</summary>
public static class EnemyStarterMovesetCreator
{
    private const string Root = "Assets/GameData/Enemy/Movesets";

    [MenuItem("Tools/Projeto PSX/Enemy Combat/Ensure Starter Movesets")]
    public static void EnsureStarterMovesets()
    {
        EnsureFolder("Assets/GameData");
        EnsureFolder("Assets/GameData/Enemy");
        EnsureFolder(Root);

        EnemyMovesetDefinition limpRagDoll = CreateLimpRagDoll();
        EnemyMovesetDefinition ragDoll = CreateRagDoll();
        EnemyMovesetDefinition flyer = CreateFlyingPuppet();
        AssignToPrefab("Assets/Prefabs/LimpRagDoll.prefab", limpRagDoll);
        AssignToPrefab("Assets/Prefabs/RagDoll.prefab", ragDoll);
        AssignToPrefab("Assets/Prefabs/FlyingPuppet.prefab", flyer);
        AssetDatabase.SaveAssets();
    }

    private static EnemyMovesetDefinition CreateLimpRagDoll()
    {
        const string folder = Root + "/LimpRagDoll";
        EnsureFolder(folder);
        EnemyAttackDefinition z1 = EnsureAttack(folder, "Z1", 25, 20,
            1.5f, .1f, 1f, 1.5f, .5f, 1.25f);
        EnemyAttackDefinition z2 = EnsureAttack(folder, "Z2", 20, 20,
            1f, .1f, .75f, 1.5f, .5f, 1f);
        return EnsureMoveset(folder, "LimpRagDollMoveset", 2,
            new[] { z1, z2 },
            new[] { Opening("Z1", 50f), Opening("Z2", 50f) },
            new Dictionary<string, EnemyMoveTransition[]>
            {
                ["Z1"] = new[] { Transition("Z2", 50f, 1f) },
                ["Z2"] = new[] { Transition("Z1", 50f, .75f) }
            });
    }

    private static EnemyMovesetDefinition CreateRagDoll()
    {
        const string folder = Root + "/RagDoll";
        EnsureFolder(folder);
        EnemyAttackDefinition z1 = EnsureAttack(folder, "Z1", 35, 20,
            1f, .1f, .4f, 1.5f, .5f, 1.3f);
        EnemyAttackDefinition z2 = EnsureAttack(folder, "Z2", 30, 20,
            .75f, .1f, .4f, 1.5f, .5f, 1.1f);
        return EnsureMoveset(folder, "RagDollMoveset", 5,
            new[] { z1, z2 },
            new[] { Opening("Z1", 50f), Opening("Z2", 50f) },
            new Dictionary<string, EnemyMoveTransition[]>
            {
                ["Z1"] = new[] { Transition("Z2", 50f, .4f) },
                ["Z2"] = new[] { Transition("Z1", 50f, .4f) }
            });
    }

    private static EnemyMovesetDefinition CreateFlyingPuppet()
    {
        const string folder = Root + "/FlyingPuppet";
        EnsureFolder(folder);
        bool createdSweep = AssetDatabase.LoadAssetAtPath<EnemyAttackDefinition>(
            folder + "/FlyerSweep.asset") == null;
        EnemyAttackDefinition sweep = EnsureAttack(folder, "FlyerSweep", 25,
            20, 1.5f, .25f, 1.4f, 1f, .5f, 1.2f,
            EnemyAttackKind.Special, EnemyAttackContext.Any);
        if (createdSweep)
        {
            sweep.tags = new List<string> { "Sweep" };
            sweep.requirements.targetContext = EnemyAttackContext.Ground;
            EditorUtility.SetDirty(sweep);
        }
        bool createdSpin = AssetDatabase.LoadAssetAtPath<EnemyAttackDefinition>(
            folder + "/FlyerAirSpin.asset") == null;
        EnemyAttackDefinition spin = EnsureAttack(folder, "FlyerAirSpin", 30,
            25, 1.8f, .4f, 3f, 2.5f, 0f, 2.5f,
            EnemyAttackKind.Special, EnemyAttackContext.Any);
        if (createdSpin)
        {
            spin.displayName = "Flyer Air Spin";
            spin.tags = new List<string> { "AirSpin" };
            spin.requirements.targetContext = EnemyAttackContext.Air;
            EditorUtility.SetDirty(spin);
        }
        return EnsureMoveset(folder, "FlyingPuppetMoveset", 5,
            new[] { sweep, spin },
            new[] { Opening(sweep.attackId, 100f),
                Opening(spin.attackId, 100f) }, null);
    }

    private static EnemyAttackDefinition EnsureAttack(string folder, string id,
        int damage, int poise, float windup, float active, float recovery,
        float maxRange, float hitboxDistance, float hitboxRadius,
        EnemyAttackKind kind = EnemyAttackKind.Light,
        EnemyAttackContext context = EnemyAttackContext.Ground)
    {
        string path = folder + "/" + id + ".asset";
        EnemyAttackDefinition attack =
            AssetDatabase.LoadAssetAtPath<EnemyAttackDefinition>(path);
        if (attack != null) return attack;
        attack = ScriptableObject.CreateInstance<EnemyAttackDefinition>();
        attack.attackId = id;
        attack.displayName = id;
        attack.kind = kind;
        attack.context = context;
        attack.windupDuration = windup;
        attack.activeDuration = active;
        attack.recoveryDuration = recovery;
        attack.baseDamage = damage;
        attack.basePoiseDamage = poise;
        attack.requirements.maximumRange = maxRange;
        attack.requirements.preferredRange = Mathf.Clamp(hitboxDistance,
            0f, maxRange);
        attack.strikes.Add(new EnemyAttackStrikeDefinition
        {
            strikeName = id,
            reaction = CombatReactionType.LaunchForwards,
            hitboxes = new List<EnemyHitboxTrackDefinition>
            {
                new()
                {
                    trackName = id + " Hitbox",
                    startLocalPosition = Vector3.forward * hitboxDistance,
                    endLocalPosition = Vector3.forward * hitboxDistance,
                    radius = hitboxRadius
                }
            }
        });
        AssetDatabase.CreateAsset(attack, path);
        return attack;
    }

    private static EnemyMovesetDefinition EnsureMoveset(string folder,
        string name, int maximumSequence, EnemyAttackDefinition[] attacks,
        EnemyMovesetOpening[] openings,
        Dictionary<string, EnemyMoveTransition[]> transitions)
    {
        string path = folder + "/" + name + ".asset";
        EnemyMovesetDefinition moveset =
            AssetDatabase.LoadAssetAtPath<EnemyMovesetDefinition>(path);
        if (moveset != null) return moveset;
        moveset = ScriptableObject.CreateInstance<EnemyMovesetDefinition>();
        moveset.maximumAttacksPerSequence = maximumSequence;
        moveset.openings.AddRange(openings);
        foreach (EnemyAttackDefinition attack in attacks)
        {
            EnemyMoveNode node = new() { nodeId = attack.attackId,
                attack = attack };
            if (transitions != null && transitions.TryGetValue(attack.attackId,
                out EnemyMoveTransition[] paths))
                node.transitions.AddRange(paths);
            moveset.nodes.Add(node);
        }
        AssetDatabase.CreateAsset(moveset, path);
        return moveset;
    }

    private static EnemyMovesetOpening Opening(string id, float weight) => new()
    {
        targetNodeId = id,
        selectionWeight = weight,
        requiresDirectorAuthorization = true
    };

    private static EnemyMoveTransition Transition(string id, float chance,
        float delay) => new()
    {
        targetNodeId = id,
        continuationChance = chance,
        selectionWeight = 100f,
        minimumDelayAfterActive = delay
    };

    private static void AssignToPrefab(string path,
        EnemyMovesetDefinition moveset)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            EnemyAttackController controller =
                root.GetComponentInChildren<EnemyAttackController>(true);
            if (controller == null) return;
            controller.EditorAssignMoveset(moveset);
            if (controller.GetComponent<EnemyAttackMovement>() == null)
                controller.gameObject.AddComponent<EnemyAttackMovement>();
            if (controller.GetComponent<EnemyAttackHitboxController>() == null)
                controller.gameObject.AddComponent<EnemyAttackHitboxController>();
            if (controller.GetComponent<EnemyAttackPreview>() == null)
                controller.gameObject.AddComponent<EnemyAttackPreview>();
            EditorUtility.SetDirty(controller);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
    }
}

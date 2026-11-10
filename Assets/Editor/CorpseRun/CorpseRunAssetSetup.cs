using UnityEditor;
using UnityEngine;

/// <summary>Gera os assets base da feature sem alterar cenas ou assets existentes.</summary>
[InitializeOnLoad]
public static class CorpseRunAssetSetup
{
    private const string CorpseFolder = "Assets/Resources/CorpseRun";

    static CorpseRunAssetSetup()
    {
        EditorApplication.delayCall += EnsureWhenReady;
    }

    private static void EnsureWhenReady()
    {
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += EnsureWhenReady;
            return;
        }
        EnsureAssets();
    }

    [MenuItem("Tools/Projeto PSX/Corpse Run/Ensure Assets")]
    public static void EnsureAssets()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureFolder("Assets/Resources");
        EnsureFolder(CorpseFolder);

        const string prefabPath = CorpseFolder + "/CorpseRunEnemy.prefab";
        GameObject corpsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (corpsePrefab == null)
            corpsePrefab = CreateCorpsePrefab(prefabPath);

        const string corpseSettingsPath = CorpseFolder + "/CorpseRunSettings.asset";
        CorpseRunSettings corpseSettings = AssetDatabase.LoadAssetAtPath<CorpseRunSettings>(corpseSettingsPath);
        if (corpseSettings == null)
        {
            corpseSettings = ScriptableObject.CreateInstance<CorpseRunSettings>();
            AssetDatabase.CreateAsset(corpseSettings, corpseSettingsPath);
        }
        if (corpseSettings.corpseEnemyPrefab != corpsePrefab)
        {
            corpseSettings.corpseEnemyPrefab = corpsePrefab;
            EditorUtility.SetDirty(corpseSettings);
        }

        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateCorpsePrefab(string destination)
    {
        const string sourcePath = "Assets/Prefabs/RagDoll.prefab";
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogError($"CorpseRun: prefab base nao encontrado em {sourcePath}.");
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(sourcePath);
        try
        {
            root.name = "CorpseRunEnemy";
            if (root.GetComponent<CorpseRunEnemy>() == null)
                root.AddComponent<CorpseRunEnemy>();
            return PrefabUtility.SaveAsPrefabAsset(root, destination);
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
        EnsureFolder(path[..slash]);
        AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
    }
}

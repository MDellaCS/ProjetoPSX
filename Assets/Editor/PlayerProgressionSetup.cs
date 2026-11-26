using System;
using UnityEditor;
using UnityEngine;

/// <summary>Cria apenas assets e prefab; nunca altera cenas.</summary>
[InitializeOnLoad]
public static class PlayerProgressionSetup
{
    private const string DataFolder = "Assets/GameData/Player/Progression";
    private const string CatalogPath = "Assets/Resources/Player/PlayerUpgradeCatalog.asset";
    private const string PrefabPath = "Assets/Prefabs/World/UpgradeShop.prefab";
    private const string RagePath = DataFolder + "/Rage.asset";

    static PlayerProgressionSetup() => EditorApplication.delayCall += EnsureDefaults;

    [MenuItem("Tools/Projeto PSX/Progression/Create Shop Assets and Prefab")]
    public static void EnsureDefaults()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder(DataFolder);
        EnsureFolder("Assets/Resources/Player");
        EnsureFolder("Assets/Prefabs/World");
        PlayerRageUpgradeEffect rage = AssetDatabase.LoadAssetAtPath<PlayerRageUpgradeEffect>(RagePath);
        if (rage == null)
        {
            rage = ScriptableObject.CreateInstance<PlayerRageUpgradeEffect>();
            AssetDatabase.CreateAsset(rage, RagePath);
        }
        PlayerUpgradeCatalog catalog = AssetDatabase.LoadAssetAtPath<PlayerUpgradeCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PlayerUpgradeCatalog>();
            for (int i = 0; i < 4; i++)
            {
                string path = DataFolder + (i == 0 ? "/Charge.asset" : "/TBD" + i + ".asset");
                PlayerUpgradeDefinition attribute = AssetDatabase.LoadAssetAtPath<PlayerUpgradeDefinition>(path);
                if (attribute == null)
                {
                    attribute = ScriptableObject.CreateInstance<PlayerUpgradeDefinition>();
                    attribute.id = i == 0 ? "charge" : "tbd-" + i;
                    attribute.displayName = i == 0 ? "Carga" : "TBD " + i;
                    attribute.purchasable = i == 0;
                    attribute.statId = i == 0 ? "max-heat" : "tbd-" + i;
                    attribute.description = i == 0 ? "Mais capacidade de Heat e poder para os ataques pesados." : "Atributo ainda nao definido.";
                    attribute.levels.Clear();
                    for (int level = 1; level <= 6; level++)
                        attribute.levels.Add(new PlayerUpgradeLevel
                        {
                            price = level == 1 ? 0 : (level - 1) * 20,
                            gain = i == 0 && level > 1 ? level == 6 ? 10 : 5 : 0,
                            retainedGain = 1f
                        });
                    if (i == 0)
                        attribute.levels[5].unlocks.Add(new PlayerUpgradeUnlock
                        {
                            id = "rage", displayName = "Rage",
                            description = "Movimentos normais e Heat maximo sem consumo apos a explosao de Burn.",
                            effects = new System.Collections.Generic.List<PlayerUpgradeEffect> { rage }
                        });
                    AssetDatabase.CreateAsset(attribute, path);
                }
                catalog.attributes.Add(attribute);
            }
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            GameObject root = new("UpgradeShop");
            try
            {
                BoxCollider body = root.AddComponent<BoxCollider>();
                body.center = Vector3.up; body.size = new Vector3(1.3f, 2, 1.3f);
                root.AddComponent<EnemyGore>();
                CurrencyShopTerminal terminal = root.AddComponent<CurrencyShopTerminal>();
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "VisualRoot";
                UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.up;
                visual.transform.localScale = new Vector3(1.1f, 2, 1.1f);
                SerializedObject settings = new(terminal);
                settings.FindProperty("visualRoot").objectReferenceValue = visual;
                settings.ApplyModifiedPropertiesWithoutUndo();
                GameObject area = new("InteractionArea");
                area.transform.SetParent(root.transform, false);
                SphereCollider trigger = area.AddComponent<SphereCollider>();
                trigger.isTrigger = true; trigger.center = Vector3.up; trigger.radius = 3;
                area.AddComponent<CurrencyShopInteractionArea>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Projeto PSX/Progression/Validate Global Catalog")]
    public static void ValidateCatalog()
    {
        PlayerUpgradeCatalog catalog = AssetDatabase.LoadAssetAtPath<PlayerUpgradeCatalog>(CatalogPath);
        if (catalog == null) throw new InvalidOperationException("Catalogo global ausente.");
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (PlayerUpgradeDefinition attribute in catalog.attributes)
        {
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.id) || !ids.Add(attribute.id))
                throw new InvalidOperationException("Cada atributo deve ter um ID unico e nao vazio.");
            if (!Mathf.Approximately(attribute.Bonus(1), 0))
                throw new InvalidOperationException("Nivel 1 precisa ser neutro.");
            if (attribute.levels == null || attribute.levels.Count == 0)
                throw new InvalidOperationException("Configure ao menos o nivel inicial: " + attribute.name);
            for (int level = 1; level <= attribute.MaximumLevel; level++)
                if (attribute.GetLevel(level) == null || attribute.Price(level) < 0 ||
                    level > 1 && attribute.Bonus(level) < attribute.Bonus(level - 1))
                    throw new InvalidOperationException("Precos e ganhos invalidos: " + attribute.name);
        }
        EditorUtility.DisplayDialog("Progression", "Catalogo validado: IDs, precos, ganhos e nivel inicial.", "OK");
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/');
        string parent = path.Substring(0, split);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
    }
}

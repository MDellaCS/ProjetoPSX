using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Converte materiais URP Lit/Simple Lit/Standard/ProBuilder para "Projeto PSX/Retro Lit".
/// So altera cenas quando o usuario executa o comando de cena pelo menu.
/// </summary>
public static class RetroMaterialConverter
{
    public const string ShaderName = RetroMaterialConversion.ShaderName;
    public const string RetroMaterialFolder = "Assets/Materials/Retro";
    private const string MenuRoot = "Tools/Projeto PSX/Iluminacao/";

    public static Shader RetroShader => RetroMaterialConversion.RetroShader;

    public static bool CanConvert(Material material) => RetroMaterialConversion.CanConvert(material);

    /// <summary>Troca o shader preservando cor, textura, recorte, emissao e cull.</summary>
    public static bool Convert(Material material) => RetroMaterialConversion.Convert(material);

    [MenuItem(MenuRoot + "Converter materiais do projeto para Retro Lit")]
    public static void ConvertProjectMaterials()
    {
        int converted = 0;
        List<string> skipped = new();
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("TextMesh Pro"))
                continue;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null || material.shader.name == ShaderName)
                continue;
            if (!CanConvert(material) || material.isVariant)
            {
                skipped.Add(path + " (" + material.shader.name + ")");
                continue;
            }
            Undo.RecordObject(material, "Converter para Retro Lit");
            if (Convert(material))
            {
                EditorUtility.SetDirty(material);
                converted++;
            }
        }

        // Materiais embutidos em modelos sao convertidos pelo RetroModelMaterialPostprocessor.
        int models = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
        {
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            models++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Retro Lit: {converted} materiais convertidos, {models} modelos reimportados. " +
            $"Ignorados (shader nao suportado, transparente ou variante): {skipped.Count}\n" + string.Join("\n", skipped));
    }

    [MenuItem(MenuRoot + "Substituir materiais padrao nas cenas abertas")]
    public static void ReplaceBuiltInMaterialsInOpenScenes()
    {
        if (!EditorUtility.DisplayDialog("Retro Lit",
                "Substitui, nas cenas abertas, materiais de pacotes (Lit padrao da URP, material padrao do ProBuilder) " +
                "por copias Retro Lit em " + RetroMaterialFolder + ". As cenas ficam marcadas como alteradas para voce revisar e salvar.",
                "Substituir", "Cancelar"))
            return;

        Dictionary<Material, Material> replacements = new();
        int renderers = 0;
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
                continue;
            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool rendererChanged = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || AssetDatabase.GetAssetPath(source).StartsWith("Assets/") || !CanConvert(source))
                        continue;
                    if (!replacements.TryGetValue(source, out Material retro))
                    {
                        retro = CreateRetroCopy(source);
                        replacements.Add(source, retro);
                    }
                    if (retro == null)
                        continue;
                    materials[i] = retro;
                    rendererChanged = true;
                }
                if (!rendererChanged)
                    continue;
                Undo.RecordObject(renderer, "Substituir materiais por Retro Lit");
                renderer.sharedMaterials = materials;
                renderers++;
                changed = true;
            }
            if (changed)
                EditorSceneManager.MarkSceneDirty(scene);
        }
        Debug.Log($"Retro Lit: {renderers} renderers atualizados com {replacements.Count} materiais Retro Lit.");
    }

    private static Material CreateRetroCopy(Material source)
    {
        if (!AssetDatabase.IsValidFolder(RetroMaterialFolder))
        {
            Directory.CreateDirectory(RetroMaterialFolder);
            AssetDatabase.Refresh();
        }
        string safeName = string.Join("_", source.name.Split(Path.GetInvalidFileNameChars()));
        string path = RetroMaterialFolder + "/" + safeName + "_Retro.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Material copy = RetroMaterialConversion.CreateCopy(source);
        if (!Convert(copy))
            return null;
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }
}

/// <summary>Materiais de modelos importados nascem com Retro Lit (ver RetroLightingSettings).</summary>
public sealed class RetroModelMaterialPostprocessor : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    private void OnPostprocessMaterial(Material material)
    {
        if (!assetPath.StartsWith("Assets/"))
            return;
        RetroLightingSettings settings = AssetDatabase.LoadAssetAtPath<RetroLightingSettings>(
            "Assets/Resources/" + RetroLightingSettings.ResourcePath + ".asset");
        if (settings != null && !settings.convertImportedModelMaterials)
            return;
        RetroMaterialConverter.Convert(material);
    }
}

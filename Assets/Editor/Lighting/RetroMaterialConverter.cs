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
    public const string ShaderName = "Projeto PSX/Retro Lit";
    public const string RetroMaterialFolder = "Assets/Materials/Retro";
    private const string MenuRoot = "Tools/Projeto PSX/Iluminacao/";

    public static Shader RetroShader => Shader.Find(ShaderName);

    public static bool CanConvert(Material material)
    {
        if (material == null || material.shader == null)
            return false;
        string name = material.shader.name;
        if (name == ShaderName)
            return false;
        bool lit = name == "Universal Render Pipeline/Lit" ||
            name == "Universal Render Pipeline/Simple Lit" ||
            name == "Universal Render Pipeline/Complex Lit" ||
            name == "Universal Render Pipeline/Baked Lit" ||
            name == "Standard" ||
            name.StartsWith("ProBuilder/");
        // Transparencias continuam na URP: o Retro Lit e opaco/recortado.
        bool transparent = material.HasProperty("_Surface") && material.GetFloat("_Surface") > .5f;
        return lit && !transparent;
    }

    /// <summary>Troca o shader preservando cor, textura, recorte, emissao e cull.</summary>
    public static bool Convert(Material material)
    {
        Shader shader = RetroShader;
        if (shader == null || !CanConvert(material))
            return false;

        string sourceName = material.shader.name;
        Texture baseMap = GetTexture(material, "_BaseMap", "_MainTex");
        Vector2 scale = material.HasProperty("_BaseMap") ? material.GetTextureScale("_BaseMap")
            : material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
        Vector2 offset = material.HasProperty("_BaseMap") ? material.GetTextureOffset("_BaseMap")
            : material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
        Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor")
            : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        bool alphaClip = material.IsKeywordEnabled("_ALPHATEST_ON") ||
            (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > .5f);
        float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : .5f;
        bool emission = material.IsKeywordEnabled("_EMISSION");
        Color emissionColor = emission && material.HasProperty("_EmissionColor")
            ? material.GetColor("_EmissionColor") : Color.black;
        Texture emissionMap = emission ? GetTexture(material, "_EmissionMap") : null;
        float cull = material.HasProperty("_Cull") ? material.GetFloat("_Cull") : 2f;

        material.shader = shader;
        material.shaderKeywords = alphaClip ? new[] { "_ALPHATEST_ON" } : new string[0];
        material.SetTexture("_BaseMap", baseMap);
        material.SetTextureScale("_BaseMap", scale);
        material.SetTextureOffset("_BaseMap", offset);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
        material.SetFloat("_Cutoff", cutoff);
        material.SetColor("_EmissionColor", emissionColor);
        material.SetTexture("_EmissionMap", emissionMap);
        material.SetFloat("_Cull", cull);
        material.SetFloat("_UseVertexColor", sourceName.Contains("Vertex Color") ? 1f : 0f);
        material.renderQueue = -1;
        return true;
    }

    private static Texture GetTexture(Material material, params string[] names)
    {
        foreach (string name in names)
            if (material.HasProperty(name))
                return material.GetTexture(name);
        return null;
    }

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
            if (!CanConvert(material))
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
            $"Ignorados (shader nao suportado ou transparente): {skipped.Count}\n" + string.Join("\n", skipped));
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

        Material copy = new(source);
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

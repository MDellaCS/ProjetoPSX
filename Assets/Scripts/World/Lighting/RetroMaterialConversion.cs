using UnityEngine;

/// <summary>
/// Converte materiais URP Lit / Simple Lit / Standard / ProBuilder para "Projeto PSX/Retro Lit".
/// Usado pelo conversor de Editor e pela troca automatica em runtime (RetroMaterialSwapper).
/// </summary>
public static class RetroMaterialConversion
{
    public const string ShaderName = "Projeto PSX/Retro Lit";

    public static Shader RetroShader
    {
        get
        {
            RetroLightingSettings settings = RetroLightingSettings.Instance;
            if (settings != null && settings.retroLitShader != null)
                return settings.retroLitShader;
            return Shader.Find(ShaderName);
        }
    }

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
            name.StartsWith("ProBuilder");
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
#if UNITY_EDITOR
        // Variantes herdam o shader do material pai e nao podem troca-lo diretamente.
        if (material.isVariant)
            return false;
#endif

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
        // Pixel funciona em qualquer malha; Vertex so ilumina bem malhas subdivididas.
        material.SetFloat("_LightingMode", 2f);
        material.SetFloat("_UseVertexColor", sourceName.Contains("Vertex Color") ? 1f : 0f);
        material.renderQueue = -1;
        return true;
    }

    /// <summary>Cria um material independente, preservando os valores efetivos da origem.</summary>
    public static Material CreateCopy(Material source)
    {
        if (!CanConvert(source))
            return null;

        Material copy = new Material(source.shader) { name = source.name + " (Retro)" };
        copy.CopyPropertiesFromMaterial(source);
        return copy;
    }

    private static Texture GetTexture(Material material, params string[] names)
    {
        foreach (string name in names)
            if (material.HasProperty(name))
                return material.GetTexture(name);
        return null;
    }
}

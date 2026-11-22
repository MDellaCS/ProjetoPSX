using UnityEditor;
using UnityEngine;

/// <summary>Inspector do shader "Projeto PSX/Retro Lit", com explicacoes em portugues.</summary>
public sealed class RetroLitShaderGUI : ShaderGUI
{
    private static readonly GUIContent BaseMap = new("Base Map", "Textura de cor da superficie.");
    private static readonly GUIContent UseVertexColor = new("Use Vertex Color",
        "Multiplica a cor pela cor de vertice da malha. Permite iluminacao pintada a mao nos vertices, tecnica comum no PS1/PS2.");
    private static readonly GUIContent Emission = new("Emission",
        "Cor (e mapa) que brilha sem depender de luz, como telas, olhos ou lampadas. Preto desliga.");
    private static readonly GUIContent LightingMode = new("Lighting Mode",
        "Vertex: luz calculada so nos vertices e interpolada (Gouraud, PS1/PS2). ATENCAO: um chao ou parede grande com poucos vertices nao recebe luz no meio, pois os cantos ficam fora do alcance. Use apenas em malhas subdivididas.\n" +
        "Grid: luz amostrada em celulas de mundo, com aspecto de texel grande, independente da malha.\n" +
        "Pixel: luz suave por pixel (early PS3). Usado quando o ajuste global esta em Per Material e nao ha override.");
    private static readonly GUIContent LightingOverride = new("Lighting Mode Override",
        "Global usa o modo de Retro Lighting Settings. Vertex, Grid e Pixel substituem o modo global neste material.");
    private static readonly GUIContent GridSize = new("Grid Size",
        "Tamanho, em metros, de cada celula de luz no modo Grid. Valores maiores deixam a luz mais blocada.");
    private static readonly GUIContent FlatShading = new("Flat Shading",
        "Cada triangulo recebe um tom unico de luz, revelando as faces da malha (sombreamento facetado).");
    private static readonly GUIContent MetallicShine = new("Brilho metalico",
        "Intensidade do reflexo das luzes locais retro, semelhante a ferro polido. Zero desativa o brilho.");
    private static readonly GUIContent ShineSharpness = new("Nitidez do brilho",
        "Controla a concentracao do reflexo: valores maiores produzem pontos de luz menores e mais definidos.");
    private static readonly GUIContent ShineLimit = new("Limite do brilho metalico",
        "Limita a intensidade maxima do reflexo das luzes, mesmo quando elas sao muito fortes.");
    private static readonly GUIContent ReactiveGlowMode = new("Emissao reativa",
        "Off desliga. Light aumenta a emissao conforme a superficie recebe luz; Dark faz a emissao desaparecer conforme recebe luz. Considera apenas luzes locais do sistema retro, incluindo a do jogador.");
    private static readonly GUIContent ReactiveGlowColor = new("Cor da emissao reativa",
        "Cor do brilho reativo. O mapa de emissao tambem controla onde ele aparece.");
    private static readonly GUIContent ReactiveGlowMax = new("Intensidade maxima da emissao",
        "Brilho maximo da emissao reativa. Valores acima de 1 podem produzir halo quando HDR e Bloom estiverem ativos.");
    private static readonly GUIContent ReactiveGlowThreshold = new("Luz para resposta completa",
        "Quantidade de iluminacao necessaria para atingir o maximo em Light ou apagar em Dark. Valores maiores exigem mais luz.");
    private static readonly GUIContent AlphaClip = new("Alpha Clip",
        "Descarta pixels com alpha abaixo do limite (folhagem, grades). Nao ha transparencia parcial.");
    private static readonly GUIContent Cutoff = new("Alpha Cutoff", "Limite de alpha do recorte.");
    private static readonly GUIContent Cull = new("Cull",
        "Faces descartadas: Back (padrao) esconde o verso; Off desenha os dois lados.");

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        MaterialProperty baseMap = FindProperty("_BaseMap", properties);
        MaterialProperty baseColor = FindProperty("_BaseColor", properties);
        MaterialProperty emissionMap = FindProperty("_EmissionMap", properties);
        MaterialProperty emissionColor = FindProperty("_EmissionColor", properties);
        MaterialProperty lightingMode = FindProperty("_LightingMode", properties);
        MaterialProperty alphaClip = FindProperty("_AlphaClip", properties);

        EditorGUILayout.LabelField("Superficie", EditorStyles.boldLabel);
        editor.TexturePropertySingleLine(BaseMap, baseMap, baseColor);
        editor.TextureScaleOffsetProperty(baseMap);
        editor.ShaderProperty(FindProperty("_UseVertexColor", properties), UseVertexColor);
        editor.TexturePropertyWithHDRColor(Emission, emissionMap, emissionColor, false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Iluminacao retro", EditorStyles.boldLabel);
        MaterialProperty lightingOverride = FindProperty("_LightingModeOverride", properties);
        editor.ShaderProperty(lightingOverride, LightingOverride);
        editor.ShaderProperty(lightingMode, LightingMode);
        editor.ShaderProperty(FindProperty("_GridSize", properties), GridSize);
        editor.ShaderProperty(FindProperty("_FlatShading", properties), FlatShading);
        MaterialProperty metallicShine = FindProperty("_MetallicShine", properties);
        editor.ShaderProperty(metallicShine, MetallicShine);
        if (metallicShine.floatValue > 0f || metallicShine.hasMixedValue)
        {
            editor.ShaderProperty(FindProperty("_ShineSharpness", properties), ShineSharpness);
            editor.ShaderProperty(FindProperty("_ShineLimit", properties), ShineLimit);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Emissao reativa", EditorStyles.boldLabel);
        MaterialProperty glowMode = FindProperty("_ReactiveGlowMode", properties);
        editor.ShaderProperty(glowMode, ReactiveGlowMode);
        if (glowMode.floatValue > 0.5f || glowMode.hasMixedValue)
        {
            editor.ShaderProperty(FindProperty("_ReactiveGlowColor", properties), ReactiveGlowColor);
            editor.ShaderProperty(FindProperty("_ReactiveGlowMax", properties), ReactiveGlowMax);
            editor.ShaderProperty(FindProperty("_ReactiveGlowThreshold", properties), ReactiveGlowThreshold);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Renderizacao", EditorStyles.boldLabel);
        editor.ShaderProperty(alphaClip, AlphaClip);
        if (alphaClip.floatValue > .5f)
            editor.ShaderProperty(FindProperty("_Cutoff", properties), Cutoff);
        editor.ShaderProperty(FindProperty("_Cull", properties), Cull);
        editor.EnableInstancingField();
    }
}

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
        "Pixel: luz suave por pixel (early PS3). O RetroLightingSettings pode forcar um modo para todos os materiais.");
    private static readonly GUIContent GridSize = new("Grid Size",
        "Tamanho, em metros, de cada celula de luz no modo Grid. Valores maiores deixam a luz mais blocada.");
    private static readonly GUIContent FlatShading = new("Flat Shading",
        "Cada triangulo recebe um tom unico de luz, revelando as faces da malha (sombreamento facetado).");
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
        editor.ShaderProperty(lightingMode, LightingMode);
        if (Mathf.RoundToInt(lightingMode.floatValue) == 1)
            editor.ShaderProperty(FindProperty("_GridSize", properties), GridSize);
        editor.ShaderProperty(FindProperty("_FlatShading", properties), FlatShading);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Renderizacao", EditorStyles.boldLabel);
        editor.ShaderProperty(alphaClip, AlphaClip);
        if (alphaClip.floatValue > .5f)
            editor.ShaderProperty(FindProperty("_Cutoff", properties), Cutoff);
        editor.ShaderProperty(FindProperty("_Cull", properties), Cull);
        editor.EnableInstancingField();
    }
}

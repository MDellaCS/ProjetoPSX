using UnityEngine;

public enum RetroLightingModeOverride
{
    [Tooltip("Cada material usa o proprio Lighting Mode.")]
    PerMaterial,
    [Tooltip("Usa iluminacao por vertice (Gouraud) como padrao dos materiais Retro Lit.")]
    Vertex,
    [Tooltip("Usa iluminacao em grade de mundo como padrao dos materiais Retro Lit.")]
    Grid,
    [Tooltip("Usa iluminacao por pixel como padrao dos materiais Retro Lit.")]
    Pixel
}

public enum RetroLightingRendererOverride
{
    Global,
    Vertex,
    Grid,
    Pixel
}

/// <summary>
/// Configuracao GLOBAL da iluminacao retro (Resources/Lighting/RetroLightingSettings).
/// Ambiente e nevoa ficam em RetroEnvironmentProfile; luzes em LocalLightProfile.
/// </summary>
[CreateAssetMenu(menuName = "Projeto PSX/Lighting/Retro Lighting Settings", fileName = "RetroLightingSettings")]
public sealed class RetroLightingSettings : ScriptableObject
{
    public const string ResourcePath = "Lighting/RetroLightingSettings";

    [Header("Orcamento")]
    [Tooltip("Maximo de luzes locais enviadas a GPU por camera. As mais proximas da camera e visiveis vencem. Cada luz custa um passo do loop por vertice (ou pixel); PS2 trabalhava com cerca de 4 a 8 por objeto.")]
    [Range(1, RetroLightSystem.MaxLights)] public int maxVisibleLights = 16;
    [Tooltip("Multiplicador global aplicado a todas as luzes locais. Ajusta o brilho geral sem editar cada perfil.")]
    [Min(0f)] public float localLightIntensityMultiplier = 1f;

    [Header("Estilo")]
    [Tooltip("Modo padrao dos materiais Retro Lit. Cada material ou objeto com override pode escolher outro modo. Per Material usa o Lighting Mode de cada material.")]
    public RetroLightingModeOverride lightingModeOverride = RetroLightingModeOverride.PerMaterial;
    [Tooltip("Tamanho da celula da grade, em metros, usado quando o modo Grid e forcado. Zero usa o valor de cada material.")]
    [Min(0f)] public float gridCellSizeOverride;
    [Tooltip("Niveis por canal de cor no resultado final. 32 imita a cor de 15 bits do PS1/PS2; 0 desliga a reducao de cor.")]
    [Range(0, 256)] public int colorLevels = 32;
    [Tooltip("Forca do dither ordenado 4x4 usado para esconder as faixas da reducao de cor, como no PS1/PS2. Zero mostra as faixas puras.")]
    [Range(0f, 1f)] public float ditherStrength = 1f;

    [Header("Ambiente")]
    [Tooltip("Ambiente usado quando a cena nao tem um Retro Lighting Environment ativo.")]
    public RetroEnvironmentProfile defaultEnvironment;
    [Tooltip("Perfis disponiveis para alternar durante o Play Mode com [ e ]. O primeiro perfil valido e usado ao iniciar, salvo quando a cena tem um Retro Lighting Environment ativo.")]
    public RetroEnvironmentProfile[] environmentProfiles = System.Array.Empty<RetroEnvironmentProfile>();
    [Tooltip("Em Play, aplica a cor ambiente e a nevoa tambem nos materiais que nao usam Retro Lit (URP Lit), substituindo a sonda ambiente salva na cena. Evita o mundo azulado do skybox padrao.")]
    public bool applyToUnityRenderSettings = true;

    [Header("Materiais")]
    [Tooltip("Shader 'Projeto PSX/Retro Lit'. Referenciado aqui para entrar no build.")]
    public Shader retroLitShader;
    [Tooltip("Em Play, troca em memoria os materiais URP Lit e ProBuilder dos renderers de cada cena carregada por copias Retro Lit, para que recebam as luzes retro sem editar as cenas. Desligue depois de converter os materiais no projeto.")]
    public bool autoConvertSceneMaterials = true;

    [Header("Importacao")]
    [Tooltip("Modelos importados (FBX/OBJ) recebem materiais Retro Lit em vez de URP Lit ao serem (re)importados.")]
    public bool convertImportedModelMaterials = true;

    private static RetroLightingSettings cached;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cached = null;

    public static RetroLightingSettings Instance
    {
        get
        {
            if (cached == null)
                cached = Resources.Load<RetroLightingSettings>(ResourcePath);
            return cached;
        }
    }

    private void OnValidate() => RetroLightSystem.MarkEnvironmentDirty();
}

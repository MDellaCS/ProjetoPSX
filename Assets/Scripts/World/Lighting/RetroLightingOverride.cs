using UnityEngine;

/// <summary>Substitui o modo de iluminacao dos renderizadores deste objeto.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Lighting/Retro Lighting Override")]
public sealed class RetroLightingOverride : MonoBehaviour
{
    [Tooltip("Escolhe o modo de iluminação deste objeto e dos seus filhos. Global respeita o material e as configurações globais.")]
    [SerializeField] private RetroLightingRendererOverride lightingMode = RetroLightingRendererOverride.Global;

    private static readonly int RendererMode = Shader.PropertyToID("_RendererLightingModeOverride");
    private Renderer[] renderers;
    private MaterialPropertyBlock block;

    private void OnEnable()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        Apply(lightingMode);
    }

    private void OnDisable() => Apply(RetroLightingRendererOverride.Global);

    private void Apply(RetroLightingRendererOverride mode)
    {
        if (renderers == null) return;
        block ??= new MaterialPropertyBlock();
        foreach (Renderer target in renderers)
            ApplyToRenderer(target, mode, block);
    }

    public static void ApplyToRenderer(Renderer target, RetroLightingRendererOverride mode,
        MaterialPropertyBlock propertyBlock)
    {
        if (target == null || propertyBlock == null) return;
        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(RendererMode, (float)mode);
        target.SetPropertyBlock(propertyBlock);
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class GlobalRetroController : MonoBehaviour
{
    private static readonly int ParamsId = Shader.PropertyToID("_GlobalRetroParams");
    private static readonly int StyleId = Shader.PropertyToID("_GlobalRetroStyle");
    private GlobalRetroSettings settings;
    private UniversalRenderPipelineAsset modifiedPipeline;
    private float originalScale;
    private UpscalingFilterSelection originalUpscaler;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var settings = Resources.Load<GlobalRetroSettings>("GlobalRetroSettings");
        if (settings == null) return;
        var controller = new GameObject("Global Retro Controller").AddComponent<GlobalRetroController>();
        controller.settings = settings;
        DontDestroyOnLoad(controller.gameObject);
        controller.Apply();
    }

    private void LateUpdate() => Apply();

    private void Apply()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        bool enabled = settings != null && settings.ativo;
        bool internalMode = enabled && settings.resolucao == GlobalRetroResolutionMode.Interna;
        if (modifiedPipeline != null && (!internalMode || modifiedPipeline != pipeline)) Restore();
        if (internalMode && pipeline != null)
        {
            if (modifiedPipeline == null)
            {
                modifiedPipeline = pipeline;
                originalScale = pipeline.renderScale;
                originalUpscaler = pipeline.upscalingFilter;
            }
            pipeline.renderScale = Mathf.Clamp((float)settings.altura / Mathf.Max(1, Screen.height), 0.1f, 1f);
            pipeline.upscalingFilter = UpscalingFilterSelection.Point;
        }

        Shader.SetGlobalVector(ParamsId, enabled
            ? new Vector4(1f, internalMode ? 0f : 1f, Mathf.Max(1, settings.altura), Mathf.Clamp(settings.niveisCor, 2, 32)) : Vector4.zero);
        Shader.SetGlobalVector(StyleId, enabled
            ? new Vector4(Mathf.Clamp01(settings.dithering), Mathf.Clamp01(settings.intensidadeCor), Mathf.Clamp01(settings.intensidadePixel), 0f) : Vector4.zero);
    }

    private void OnDestroy()
    {
        Restore();
        Shader.SetGlobalVector(ParamsId, Vector4.zero);
    }

    private void Restore()
    {
        if (modifiedPipeline == null) return;
        modifiedPipeline.renderScale = originalScale;
        modifiedPipeline.upscalingFilter = originalUpscaler;
        modifiedPipeline = null;
    }
}

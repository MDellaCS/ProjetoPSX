using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Sincroniza a fog com o ambiente ativo antes da renderizacao.</summary>
public static class FogRuntime
{
    private const int CurveSamples = 128;
    private static readonly int OriginId = Shader.PropertyToID("_PSXFogOrigin");
    public static Transform Player { get; set; }
    private static readonly int ColorId = Shader.PropertyToID("_PSXFogColor");
    private static readonly int ColorRangeId = Shader.PropertyToID("_PSXFogColorRange");
    private static readonly int BlurRangeId = Shader.PropertyToID("_PSXFogBlurRange");
    private static readonly int VolumeRangeId = Shader.PropertyToID("_PSXFogVolumeRange");
    private static readonly int VolumeHeightId = Shader.PropertyToID("_PSXFogVolumeHeight");
    private static readonly int VolumeColorId = Shader.PropertyToID("_PSXFogVolumeColor");
    private static readonly int VolumeParamsId = Shader.PropertyToID("_PSXFogVolumeParams");
    private static readonly int NoiseId = Shader.PropertyToID("_PSXFogNoise");
    private static readonly int AnimationTimeId = Shader.PropertyToID("_PSXFogAnimationTime");
    private static readonly int WindId = Shader.PropertyToID("_PSXFogWind");
    private static readonly int CurveId = Shader.PropertyToID("_PSXFogCurves");
    private static readonly Color[] CurvePixels = new Color[CurveSamples];
    private static Texture2D curves;
    private static FogProfile current;
    private static FogProfile previous;
    private static float transitionStart;
    private static bool dirty = true;
    private static float animationTime;
    private static int animationFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        RenderPipelineManager.beginCameraRendering -= UpdateCamera;
        current = previous = null;
        animationTime = 0f;
        animationFrame = -1;
        Player = null;
        dirty = true;
        if (curves != null) Object.Destroy(curves);
        curves = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        RenderPipelineManager.beginCameraRendering -= UpdateCamera;
        RenderPipelineManager.beginCameraRendering += UpdateCamera;
    }

    public static void Invalidate() => dirty = true;

    private static float Curve(AnimationCurve curve, float t) => curve == null || curve.length == 0 ? t : Mathf.Clamp01(curve.Evaluate(t));
    private static float Value(float a, float b, float t) => Mathf.Lerp(a, b, t);
    private static void UpdateCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera.cameraType != CameraType.Game) return;
        if (animationFrame != Time.frameCount)
        {
            animationFrame = Time.frameCount;
            animationTime += GamePauseController.IsPaused ? Time.unscaledDeltaTime : Time.deltaTime;
        }
        Shader.SetGlobalFloat(AnimationTimeId, animationTime);
        FogProfile target = RetroLightSystem.ActiveEnvironment != null ? RetroLightSystem.ActiveEnvironment.fogProfile : null;
        if (target != current)
        {
            previous = current;
            current = target;
            transitionStart = Time.unscaledTime;
            dirty = true;
        }
        // On first activation, use the selected environment immediately.
        float duration = current != null ? current.transitionTime : previous != null ? previous.transitionTime : 0f;
        float t = previous == null || duration <= 0 ? 1f : Mathf.Clamp01((Time.unscaledTime - transitionStart) / duration);
        FogProfile a = previous != null && t < 1f ? previous : current;
        FogProfile b = current != null ? current : (t < 1f ? previous : null);
        float fade = current == null ? 1f - t : 1f;
        if (b == null)
        {
            Shader.SetGlobalVector(ColorRangeId, Vector4.zero);
            Shader.SetGlobalVector(BlurRangeId, Vector4.zero);
            Shader.SetGlobalVector(VolumeParamsId, Vector4.zero);
            return;
        }
        if (a == null) a = b;
        if (curves == null)
        {
            curves = new Texture2D(CurveSamples, 1, TextureFormat.RGBA32, false, true) { name = "Fog Curves", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            dirty = true;
        }
        if (dirty || t < 1f)
        {
            for (int i = 0; i < CurveSamples; i++)
            {
                float x = (float)i / (CurveSamples - 1);
                CurvePixels[i] = new Color(Value(Curve(a.colorCurve, x), Curve(b.colorCurve, x), t), Value(Curve(a.blurCurve, x), Curve(b.blurCurve, x), t), Value(Curve(a.volumeCurve, x), Curve(b.volumeCurve, x), t), 1);
            }
            curves.SetPixels(CurvePixels);
            curves.Apply(false, false);
            dirty = t < 1f;
        }
        Vector3 cameraPosition = camera.transform.position;
        Vector3 playerPosition = Player != null ? Player.position : cameraPosition;
        Vector3 originA = a.distanceOrigin == FogDistanceOrigin.Player ? playerPosition : cameraPosition;
        Vector3 originB = b.distanceOrigin == FogDistanceOrigin.Player ? playerPosition : cameraPosition;
        Vector3 origin = Vector3.Lerp(originA, originB, t);
        Shader.SetGlobalVector(OriginId, new Vector4(origin.x, origin.y, origin.z, 0));
        Shader.SetGlobalTexture(CurveId, curves);
        Shader.SetGlobalColor(ColorId, Color.Lerp(a.color.linear, b.color.linear, t));
        Shader.SetGlobalVector(ColorRangeId, new Vector4(Value(a.colorStart,b.colorStart,t), Value(a.colorEnd,b.colorEnd,t), Value(a.colorEnabled ? a.colorMin : 0,b.colorEnabled ? b.colorMin : 0,t) * fade, Value(a.colorEnabled ? a.colorMax : 0,b.colorEnabled ? b.colorMax : 0,t) * fade));
        Shader.SetGlobalVector(BlurRangeId, new Vector4(Value(a.blurStart,b.blurStart,t), Value(a.blurEnd,b.blurEnd,t), Value(a.blurEnabled ? a.blurMin*a.blurRadius : 0,b.blurEnabled ? b.blurMin*b.blurRadius : 0,t) * fade, Value(a.blurEnabled ? a.blurMax*a.blurRadius : 0,b.blurEnabled ? b.blurMax*b.blurRadius : 0,t) * fade));
        Shader.SetGlobalVector(VolumeRangeId, new Vector4(Value(a.volumeStart,b.volumeStart,t), Value(a.volumeEnd,b.volumeEnd,t), Value(a.volumeMin,b.volumeMin,t), Value(a.volumeMax,b.volumeMax,t)));
        Shader.SetGlobalVector(VolumeHeightId, new Vector4(a.limitVolumeHeight ? 1f : 0f, b.limitVolumeHeight ? 1f : 0f, t, 0f));
        Shader.SetGlobalColor(VolumeColorId, Color.Lerp(a.volumetricColor.linear,b.volumetricColor.linear,t));
        Shader.SetGlobalVector(VolumeParamsId, new Vector4(Value(a.volumetricEnabled ? a.density : 0,b.volumetricEnabled ? b.density : 0,t) * fade, Value(a.volumetricEnabled ? a.volumetricOpacity : 0,b.volumetricEnabled ? b.volumetricOpacity : 0,t) * fade, Value(a.baseHeight,b.baseHeight,t), Value(a.height,b.height,t)));
        Shader.SetGlobalVector(NoiseId, new Vector4(Value(a.noiseScale,b.noiseScale,t),Value(a.noiseStrength,b.noiseStrength,t),Value(a.maxDistance,b.maxDistance,t),Value(a.steps,b.steps,t)));
        Vector3 wind = Vector3.Lerp(a.wind,b.wind,t);
        Shader.SetGlobalVector(WindId, new Vector4(wind.x,wind.y,wind.z,0));
    }
}

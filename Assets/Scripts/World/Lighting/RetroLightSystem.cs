using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Uma luz local registrada no RetroLightSystem. Pertence ao emissor que a criou.</summary>
public sealed class RetroLight
{
    private Transform ownerTransform;
    internal bool hadOwner;

    /// <summary>Transform que move a luz. Sem dono, localPosition e uma posicao de mundo.</summary>
    public Transform owner
    {
        get => ownerTransform;
        set
        {
            ownerTransform = value;
            hadOwner |= value != null;
        }
    }

    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
    public bool enabled = true;
    public bool spot;
    public Color color = Color.white;
    public float intensity = 1f;
    public float range = 8f;
    public float coreRadius;
    public float falloffExponent = 1f;
    public int bands;
    public int bandSegments = 8;
    public float wrap;
    public float spotAngle = 45f;
    public float innerSpotAngle = 30f;

    internal bool registered;

    public Vector3 WorldPosition => owner != null ? owner.TransformPoint(localPosition) : localPosition;
    public Vector3 WorldForward => owner != null
        ? owner.rotation * (localRotation * Vector3.forward)
        : localRotation * Vector3.forward;
}

/// <summary>
/// Iluminacao retro do Projeto PSX. Nao usa componentes Light da Unity nem altera a URP:
/// as luzes locais viram arrays globais lidos pelo shader "Projeto PSX/Retro Lit".
/// Por camera escolhe ate maxVisibleLights luzes visiveis (as mais proximas vencem).
/// </summary>
public static class RetroLightSystem
{
    public const int MaxLights = 32;

    private static readonly List<RetroLight> Lights = new();
    private static readonly List<RetroLightingEnvironment> Environments = new();

    private static readonly Vector4[] PositionData = new Vector4[MaxLights];
    private static readonly Vector4[] ColorData = new Vector4[MaxLights];
    private static readonly Vector4[] DirectionData = new Vector4[MaxLights];
    private static readonly Vector4[] ShapeData = new Vector4[MaxLights];
    private static readonly Vector4[] FacetData = new Vector4[MaxLights];
    private static readonly RetroLight[] Selected = new RetroLight[MaxLights];
    private static readonly float[] SelectedScore = new float[MaxLights];
    private static readonly Plane[] FrustumPlanes = new Plane[6];

    private static readonly int LightPositionId = Shader.PropertyToID("_RetroLightPosition");
    private static readonly int LightColorId = Shader.PropertyToID("_RetroLightColor");
    private static readonly int LightDirectionId = Shader.PropertyToID("_RetroLightDirection");
    private static readonly int LightShapeId = Shader.PropertyToID("_RetroLightShape");
    private static readonly int LightFacetId = Shader.PropertyToID("_RetroLightFacet");
    private static readonly int LightCountId = Shader.PropertyToID("_RetroLightCount");
    private static readonly int AmbientSkyId = Shader.PropertyToID("_RetroAmbientSky");
    private static readonly int AmbientGroundId = Shader.PropertyToID("_RetroAmbientGround");
    private static readonly int FogColorId = Shader.PropertyToID("_RetroFogColor");
    private static readonly int FogParamsId = Shader.PropertyToID("_RetroFogParams");
    private static readonly int FogCenterId = Shader.PropertyToID("_RetroFogCenter");
    private static readonly int FogHeightId = Shader.PropertyToID("_RetroFogHeight");
    private static readonly int GlobalParamsId = Shader.PropertyToID("_RetroLightingGlobal");

    private static bool hooked;
    private static bool environmentDirty = true;
    private static int selectedEnvironmentIndex = -1;

    /// <summary>Alvo usado como origem da nevoa no modo Player (definido pela camera de gameplay).</summary>
    public static Transform FogFocus { get; set; }
    public static int RegisteredLightCount => Lights.Count;
    /// <summary>Luzes enviadas a GPU na ultima camera renderizada.</summary>
    public static int VisibleLightCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        for (int i = 0; i < Lights.Count; i++)
            Lights[i].registered = false;
        Lights.Clear();
        Environments.Clear();
        System.Array.Clear(Selected, 0, Selected.Length);
        FogFocus = null;
        VisibleLightCount = 0;
        selectedEnvironmentIndex = -1;
        environmentDirty = true;
        hooked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RebuildRegistriesAfterReset()
    {
        // Com Scene Reload desligado, objetos ja habilitados nao repetem OnEnable:
        // uma unica busca no inicio do Play reconstroi os registros.
        Hook();
        foreach (LocalLightEmitter emitter in Object.FindObjectsByType<LocalLightEmitter>(FindObjectsInactive.Exclude))
            emitter.RegisterLights();
        foreach (RetroLightingEnvironment environment in Object.FindObjectsByType<RetroLightingEnvironment>(FindObjectsInactive.Exclude))
            if (environment.isActiveAndEnabled)
                PushEnvironment(environment);
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void EditorHook() => Hook();
#endif

    public static void Hook()
    {
        if (hooked)
            return;
        hooked = true;
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void Register(RetroLight light)
    {
        if (light == null || light.registered)
            return;
        Hook();
        light.registered = true;
        Lights.Add(light);
    }

    public static void Unregister(RetroLight light)
    {
        if (light == null || !light.registered)
            return;
        light.registered = false;
        Lights.Remove(light);
    }

    public static void PushEnvironment(RetroLightingEnvironment environment)
    {
        if (environment == null)
            return;
        Hook();
        Environments.Remove(environment);
        Environments.Add(environment);
        environmentDirty = true;
    }

    public static void RemoveEnvironment(RetroLightingEnvironment environment)
    {
        if (Environments.Remove(environment))
            environmentDirty = true;
    }

    public static void MarkEnvironmentDirty() => environmentDirty = true;

    public static RetroEnvironmentProfile ActiveEnvironment
    {
        get
        {
            RetroLightingSettings settings = RetroLightingSettings.Instance;
            if (selectedEnvironmentIndex >= 0 && settings != null && settings.environmentProfiles != null &&
                selectedEnvironmentIndex < settings.environmentProfiles.Length &&
                settings.environmentProfiles[selectedEnvironmentIndex] != null)
                return settings.environmentProfiles[selectedEnvironmentIndex];

            for (int i = Environments.Count - 1; i >= 0; i--)
            {
                RetroLightingEnvironment environment = Environments[i];
                if (environment == null)
                {
                    Environments.RemoveAt(i);
                    continue;
                }
                if (environment.Profile != null)
                    return environment.Profile;
            }
            if (settings != null && settings.environmentProfiles != null && settings.environmentProfiles.Length > 0)
            {
                int count = settings.environmentProfiles.Length;
                int start = selectedEnvironmentIndex < 0 ? 0 : selectedEnvironmentIndex % count;
                for (int offset = 0; offset < count; offset++)
                {
                    RetroEnvironmentProfile profile = settings.environmentProfiles[(start + offset) % count];
                    if (profile != null)
                        return profile;
                }
            }
            return settings != null ? settings.defaultEnvironment : null;
        }
    }

    public static int EnvironmentProfileIndex => selectedEnvironmentIndex;
    public static bool CycleEnvironmentProfile(int direction)
    {
        RetroLightingSettings settings = RetroLightingSettings.Instance;
        if (settings == null || settings.environmentProfiles == null || settings.environmentProfiles.Length == 0)
            return false;

        int count = settings.environmentProfiles.Length;
        int index = selectedEnvironmentIndex;
        if (index < 0)
        {
            RetroEnvironmentProfile current = ActiveEnvironment;
            index = System.Array.IndexOf(settings.environmentProfiles, current);
            if (index < 0)
                index = direction < 0 ? 0 : count - 1;
        }
        for (int step = 0; step < count; step++)
        {
            index = (index + (direction < 0 ? count - 1 : 1)) % count;
            if (settings.environmentProfiles[index] == null)
                continue;
            selectedEnvironmentIndex = index;
            environmentDirty = true;
            return true;
        }
        return false;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // A cena carregada traz a propria sonda ambiente; reaplica o ambiente retro.
        environmentDirty = true;
    }

    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) =>
        RefreshForCamera(camera);

    /// <summary>Envia ambiente e luzes visiveis para a camera. Chamado antes de cada camera renderizar.</summary>
    public static void RefreshForCamera(Camera camera)
    {
        if (camera == null)
            return;
        RetroLightingSettings settings = RetroLightingSettings.Instance;
        RetroEnvironmentProfile environment = ActiveEnvironment;
        UploadEnvironment(settings, environment, camera);
        UploadLights(settings, camera);
        if (environmentDirty && Application.isPlaying)
            ApplyUnityRenderSettings(settings, environment);
    }

    private static void UploadEnvironment(RetroLightingSettings settings, RetroEnvironmentProfile environment, Camera camera)
    {
        Color sky = environment != null ? environment.AmbientSky : new Color(.16f, .16f, .18f);
        Color ground = environment != null ? environment.AmbientGround : new Color(.07f, .06f, .06f);
        Shader.SetGlobalVector(AmbientSkyId, (Vector4)sky.linear);
        Shader.SetGlobalVector(AmbientGroundId, (Vector4)ground.linear);

        bool fog = environment != null && environment.fogEnabled;
        if (fog)
        {
            Color fogColor = environment.fogColor.linear;
            fogColor.a = 1f;
            Shader.SetGlobalVector(FogColorId, (Vector4)fogColor);
            Shader.SetGlobalVector(FogParamsId, new Vector4(environment.fogStart,
                1f / Mathf.Max(.01f, environment.fogEnd - environment.fogStart),
                environment.fogMaxOpacity, environment.fogLightPierce));

            bool usePlayer = environment.fogCenter == RetroFogCenter.Player && FogFocus != null &&
                camera.cameraType == CameraType.Game;
            Vector3 center = usePlayer ? FogFocus.position : camera.transform.position;
            Shader.SetGlobalVector(FogCenterId, new Vector4(center.x, center.y, center.z, 1f));
            Shader.SetGlobalVector(FogHeightId, new Vector4(center.y + environment.heightFogBaseOffset,
                1f / Mathf.Max(.01f, environment.heightFogThickness), environment.heightFog ? 1f : 0f, 0f));
        }
        else
            Shader.SetGlobalVector(FogColorId, Vector4.zero);

        // x: modo forcado + 1 (0 = por material); y: grade forcada; z: niveis de cor; w: dither.
        float mode = settings != null ? (float)settings.lightingModeOverride : 0f;
        float grid = settings != null ? settings.gridCellSizeOverride : 0f;
        float levels = settings != null ? settings.colorLevels : 0f;
        float dither = settings != null ? settings.ditherStrength : 0f;
        Shader.SetGlobalVector(GlobalParamsId, new Vector4(mode, grid, levels, dither));
    }

    private static void UploadLights(RetroLightingSettings settings, Camera camera)
    {
        int budget = settings != null ? Mathf.Clamp(settings.maxVisibleLights, 1, MaxLights) : 16;
        float multiplier = settings != null ? settings.localLightIntensityMultiplier : 1f;
        GeometryUtility.CalculateFrustumPlanes(camera, FrustumPlanes);
        Vector3 cameraPosition = camera.transform.position;
        int count = 0;

        for (int i = Lights.Count - 1; i >= 0; i--)
        {
            RetroLight light = Lights[i];
            if (light.hadOwner && light.owner == null && light.registered)
            {
                // Dono destruido sem OnDisable (ex.: recompilacao).
                light.registered = false;
                Lights.RemoveAt(i);
                continue;
            }
            if (!light.enabled || light.intensity <= 0f || light.range <= 0f)
                continue;

            Vector3 position = light.WorldPosition;
            if (!GeometryUtility.TestPlanesAABB(FrustumPlanes,
                    new Bounds(position, Vector3.one * (light.range * 2f))))
                continue;

            float score = Vector3.Distance(cameraPosition, position) - light.range;
            count = InsertByScore(light, score, count, budget);
        }

        for (int i = 0; i < count; i++)
        {
            RetroLight light = Selected[i];
            Vector3 position = light.WorldPosition;
            PositionData[i] = new Vector4(position.x, position.y, position.z, 1f / light.range);
            Color color = light.color.linear * (light.intensity * multiplier);
            ColorData[i] = new Vector4(color.r, color.g, color.b, Mathf.Clamp(light.coreRadius, 0f, .95f));

            Vector3 forward = light.WorldForward;
            float cosOuter = -2f;
            float spotScale = 0f;
            if (light.spot)
            {
                float outer = Mathf.Clamp(light.spotAngle, 1f, 179f) * .5f * Mathf.Deg2Rad;
                float inner = Mathf.Clamp(light.innerSpotAngle, 0f, light.spotAngle) * .5f * Mathf.Deg2Rad;
                cosOuter = Mathf.Cos(outer);
                spotScale = 1f / Mathf.Max(1e-4f, Mathf.Cos(inner) - cosOuter);
            }
            DirectionData[i] = new Vector4(forward.x, forward.y, forward.z, cosOuter);
            ShapeData[i] = new Vector4(spotScale, Mathf.Max(.01f, light.falloffExponent),
                Mathf.Clamp(light.bands, 0, 64), Mathf.Clamp01(light.wrap));
            FacetData[i] = new Vector4(Mathf.Clamp(light.bandSegments, 3, 32), 0f, 0f, 0f);
            Selected[i] = null;
        }

        Shader.SetGlobalVectorArray(LightPositionId, PositionData);
        Shader.SetGlobalVectorArray(LightColorId, ColorData);
        Shader.SetGlobalVectorArray(LightDirectionId, DirectionData);
        Shader.SetGlobalVectorArray(LightShapeId, ShapeData);
        Shader.SetGlobalVectorArray(LightFacetId, FacetData);
        Shader.SetGlobalFloat(LightCountId, count);
        VisibleLightCount = count;
    }

    private static int InsertByScore(RetroLight light, float score, int count, int budget)
    {
        if (count == budget && score >= SelectedScore[count - 1])
            return count;
        int index = count < budget ? count++ : count - 1;
        while (index > 0 && SelectedScore[index - 1] > score)
        {
            Selected[index] = Selected[index - 1];
            SelectedScore[index] = SelectedScore[index - 1];
            index--;
        }
        Selected[index] = light;
        SelectedScore[index] = score;
        return count;
    }

    /// <summary>
    /// Materiais fora do Retro Lit usam a sonda ambiente da cena. Em Play ela e trocada
    /// pela cor ambiente do perfil, eliminando o azul do skybox padrao.
    /// </summary>
    private static void ApplyUnityRenderSettings(RetroLightingSettings settings, RetroEnvironmentProfile environment)
    {
        environmentDirty = false;
        if (settings == null || !settings.applyToUnityRenderSettings || environment == null)
            return;

        Color sky = environment.AmbientSky;
        Color ground = environment.AmbientGround;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = sky;
        RenderSettings.ambientEquatorColor = Color.Lerp(sky, ground, .5f);
        RenderSettings.ambientGroundColor = ground;
        SphericalHarmonicsL2 probe = new();
        probe.AddAmbientLight(Color.Lerp(sky, ground, .5f).linear);
        RenderSettings.ambientProbe = probe;
        RenderSettings.reflectionIntensity = environment.unityReflectionIntensity;

        RenderSettings.fog = environment.fogEnabled;
        if (environment.fogEnabled)
        {
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = environment.fogColor;
            RenderSettings.fogStartDistance = environment.fogStart;
            RenderSettings.fogEndDistance = environment.fogEnd;
        }
    }
}

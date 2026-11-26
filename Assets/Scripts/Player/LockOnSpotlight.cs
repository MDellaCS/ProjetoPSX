using UnityEngine;

/// <summary>Apresentacao do foco, separada da selecao de alvo.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerLockOn))]
public sealed class LockOnSpotlight : MonoBehaviour
{
    [Header("Transicao")]
    [Tooltip("Tempo total, em segundos, para a luz chegar ao novo alvo. Zero troca imediatamente.")]
    [Min(0f)] [SerializeField] private float transitionDuration = .3f;
    [Tooltip("Curva de progresso da transicao entre o alvo anterior e o novo. Os eixos vao de zero a um.")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Tempo de entrada e saida da luz e do escurecimento, em segundos.")]
    [Min(0f)] [SerializeField] private float fadeDuration = .2f;
    [Header("Luz")]
    [Tooltip("Cor da luz e do cone visivel sobre o alvo.")]
    [SerializeField] private Color lightColor = Color.red;
    [Tooltip("Intensidade da luz projetada sobre os objetos.")]
    [Min(0f)] [SerializeField] private float lightIntensity = 3f;
    [Tooltip("Altura da origem da luz acima do ponto de mira do inimigo, em metros.")]
    [Min(.1f)] [SerializeField] private float lightHeight = 6f;
    [Tooltip("Alcance da luz a partir de sua origem, em metros.")]
    [Min(.1f)] [SerializeField] private float lightRange = 12f;
    [Tooltip("Abertura total do cone de luz, em graus.")]
    [Range(1f, 170f)] [SerializeField] private float coneAngle = 40f;
    [Tooltip("Opacidade do feixe de luz visivel no ar. Zero deixa o feixe invisivel.")]
    [Range(0f, 1f)] [SerializeField] private float beamOpacity = .12f;
    [Header("Escurecimento")]
    [Tooltip("Quanto o restante da cena escurece durante o foco. Zero desativa o escurecimento.")]
    [Range(0f, 1f)] [SerializeField] private float darkness = .55f;
    [Tooltip("Largura da transicao suave entre a area iluminada e o restante da cena, em metros.")]
    [Min(.01f)] [SerializeField] private float edgeSoftness = .8f;

    private static readonly int SpotlightPosition = Shader.PropertyToID("_LockSpotPosition");
    private static readonly int SpotlightParameters = Shader.PropertyToID("_LockSpotParameters");
    private static readonly int SpotlightColor = Shader.PropertyToID("_LockSpotColor");
    private static readonly int BeamColor = Shader.PropertyToID("_BeamColor");
    private static readonly int ProtectedBoundsMin = Shader.PropertyToID("_LockSpotProtectedMin");
    private static readonly int ProtectedBoundsMax = Shader.PropertyToID("_LockSpotProtectedMax");
    private PlayerLockOn lockOn;
    private EnemyBase currentTarget;
    private Renderer[] targetRenderers;
    private GameObject lightObject, beamObject;
    private Light spot;
    private RetroLight retroSpot;
    private Material beamMaterial;
    private Mesh beamMesh;
    private Vector3 displayedPoint, transitionStart;
    private float transitionElapsed, visibility;
    private bool hasPoint, overrideActive;
    private Vector3 overridePoint;
    private float intensityMultiplier = 1f, darknessMultiplier = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetShader()
    {
        Shader.SetGlobalVector(SpotlightParameters, Vector4.zero);
        Shader.SetGlobalVector(ProtectedBoundsMin, Vector4.zero);
        Shader.SetGlobalColor(SpotlightColor, Color.clear);
    }

    private void Awake()
    {
        lockOn = GetComponent<PlayerLockOn>();
        CreateVisuals();
    }

    private void OnEnable()
    {
        if (retroSpot != null) RetroLightSystem.Register(retroSpot);
    }

    private void OnDisable()
    {
        if (retroSpot != null) RetroLightSystem.Unregister(retroSpot);
        if (spot != null) spot.enabled = false;
        if (beamObject != null) beamObject.SetActive(false);
        Shader.SetGlobalVector(SpotlightParameters, Vector4.zero);
        Shader.SetGlobalVector(ProtectedBoundsMin, Vector4.zero);
        Shader.SetGlobalColor(SpotlightColor, Color.clear);
        currentTarget = null; visibility = 0f; hasPoint = false;
        targetRenderers = null;
    }

    private void OnDestroy()
    {
        if (beamMaterial != null) Destroy(beamMaterial);
        if (beamMesh != null) Destroy(beamMesh);
        if (beamObject != null) Destroy(beamObject);
        if (lightObject != null) Destroy(lightObject);
    }

    /// <summary>Permite que outro mecanismo substitua temporariamente a posicao do foco.</summary>
    public void SetFocusOverride(Vector3 worldPoint)
    {
        if (!overrideActive) { transitionStart = displayedPoint; transitionElapsed = 0f; }
        overridePoint = worldPoint; overrideActive = true;
    }
    /// <summary>Devolve o controle do foco ao lock-on.</summary>
    public void ClearFocusOverride()
    {
        if (overrideActive) { transitionStart = displayedPoint; transitionElapsed = 0f; }
        overrideActive = false;
    }
    /// <summary>Multiplica a luz e o escurecimento sem alterar as configuracoes do Inspector.</summary>
    public void SetEffectMultipliers(float lightMultiplier, float sceneDarknessMultiplier)
    {
        intensityMultiplier = Mathf.Max(0f, lightMultiplier);
        darknessMultiplier = Mathf.Max(0f, sceneDarknessMultiplier);
    }

    /// <summary>Ajusta a cor, a abertura e a intensidade base do foco em execucao.</summary>
    public void SetLightAppearance(Color color, float intensity, float angle)
    {
        lightColor = color;
        lightIntensity = Mathf.Max(0f, intensity);
        coneAngle = Mathf.Clamp(angle, 1f, 170f);
    }

    private void LateUpdate()
    {
        EnemyBase target = lockOn != null ? lockOn.Target : null;
        bool active = overrideActive || target != null;
        if (target != currentTarget)
        {
            targetRenderers = target != null
                ? target.GetComponentsInChildren<Renderer>() : null;
            if (!overrideActive)
            {
                transitionStart = displayedPoint;
                transitionElapsed = 0f;
            }
        }
        currentTarget = target;
        if (active)
        {
            Vector3 destination = overrideActive ? overridePoint : target.LockOnAimPoint;
            if (!hasPoint) { displayedPoint = destination; hasPoint = true; transitionElapsed = transitionDuration; }
            else if (transitionElapsed < transitionDuration)
            {
                transitionElapsed = Mathf.Min(transitionDuration, transitionElapsed + Time.deltaTime);
                float t = transitionDuration > 0f ? transitionElapsed / transitionDuration : 1f;
                float eased = transitionCurve != null && transitionCurve.length > 0 ? transitionCurve.Evaluate(t) : t;
                displayedPoint = Vector3.LerpUnclamped(transitionStart, destination, Mathf.Clamp01(eased));
            }
            else displayedPoint = destination;
        }
        float step = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;
        visibility = Mathf.MoveTowards(visibility, active ? 1f : 0f, step);
        if (visibility <= 0f)
        {
            if (spot != null) spot.enabled = false;
            if (retroSpot != null) retroSpot.enabled = false;
            if (beamObject != null) beamObject.SetActive(false);
            Shader.SetGlobalVector(SpotlightParameters, Vector4.zero);
            Shader.SetGlobalColor(SpotlightColor, Color.clear);
            hasPoint = false;
            return;
        }
        ApplyVisuals();
    }

    private void CreateVisuals()
    {
        lightObject = new GameObject("Lock-on Spotlight (Runtime)");
        lightObject.transform.SetParent(transform, false);
        spot = lightObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.shadows = LightShadows.None;
        spot.enabled = false;
        retroSpot = new RetroLight { owner = lightObject.transform, spot = true, enabled = false };
        RetroLightSystem.Register(retroSpot);

        Material source = Resources.Load<Material>("LockOnSpotlight/Beam");
        if (source == null) { Debug.LogError("Lock-on: material do cone nao encontrado.", this); return; }
        beamMaterial = new Material(source);
        beamMesh = BuildCone();
        beamObject = new GameObject("Lock-on Beam (Runtime)");
        beamObject.transform.SetParent(transform, false);
        beamObject.AddComponent<MeshFilter>().sharedMesh = beamMesh;
        MeshRenderer renderer = beamObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = beamMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        beamObject.SetActive(false);
    }

    private void ApplyVisuals()
    {
        float height = Mathf.Max(.1f, lightHeight);
        float range = Mathf.Max(.1f, lightRange);
        float angle = Mathf.Clamp(coneAngle, 1f, 170f);
        Vector3 origin = displayedPoint + Vector3.up * height;
        Quaternion down = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        lightObject.transform.SetPositionAndRotation(origin, down);
        spot.enabled = true; spot.color = lightColor; spot.intensity = lightIntensity * intensityMultiplier * visibility;
        spot.range = range; spot.spotAngle = angle; spot.innerSpotAngle = angle * .8f;
        retroSpot.enabled = true; retroSpot.color = lightColor; retroSpot.intensity = spot.intensity;
        retroSpot.range = range; retroSpot.spotAngle = angle; retroSpot.innerSpotAngle = angle * .8f;
        if (beamObject != null)
        {
            beamObject.SetActive(beamOpacity > 0f);
            beamObject.transform.SetPositionAndRotation(origin, down);
            float radius = Mathf.Tan(angle * .5f * Mathf.Deg2Rad) * range;
            beamObject.transform.localScale = new Vector3(radius, radius, range);
            Color color = lightColor; color.a = beamOpacity * visibility;
            beamMaterial.SetColor(BeamColor, color);
        }
        Shader.SetGlobalVector(SpotlightPosition, new Vector4(origin.x, origin.y, origin.z, range));
        Shader.SetGlobalVector(SpotlightParameters, new Vector4(Mathf.Clamp01(darkness * darknessMultiplier) * visibility,
            Mathf.Tan(angle * .5f * Mathf.Deg2Rad), Mathf.Max(.01f, edgeSoftness),
            Mathf.Clamp01(beamOpacity) * visibility));
        Color edgeColor = lightColor;
        edgeColor.a = visibility;
        Shader.SetGlobalColor(SpotlightColor, edgeColor);
        UpdateProtectedBounds();
    }

    private void UpdateProtectedBounds()
    {
        Bounds bounds = default;
        bool hasBounds = false;
        if (targetRenderers != null)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy ||
                    (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer))
                    continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
        }
        if (!hasBounds)
        {
            Shader.SetGlobalVector(ProtectedBoundsMin, Vector4.zero);
            return;
        }

        bounds.Expand(.1f);
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Shader.SetGlobalVector(ProtectedBoundsMin, new Vector4(min.x, min.y, min.z, 1f));
        Shader.SetGlobalVector(ProtectedBoundsMax, new Vector4(max.x, max.y, max.z, 0f));
    }

    private static Mesh BuildCone()
    {
        const int sides = 32;
        Vector3[] vertices = new Vector3[sides + 1];
        int[] indices = new int[sides * 3];
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 1f);
            // A Unity considera a face frontal no sentido horario quando vista de fora.
            indices[i * 3] = 0; indices[i * 3 + 1] = i + 1; indices[i * 3 + 2] = (i + 1) % sides + 1;
        }
        Mesh mesh = new() { name = "Lock-on Beam" };
        mesh.vertices = vertices; mesh.triangles = indices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
}

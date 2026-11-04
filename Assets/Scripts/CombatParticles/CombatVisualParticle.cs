using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Particula visual com fisica limitada ao mundo configurado.</summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public sealed class CombatVisualParticle : MonoBehaviour
{
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorProperty = Shader.PropertyToID("_Color");

    private Rigidbody body;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private BoxCollider boxCollider;
    private Mesh defaultMesh;
    private Mesh generatedMesh;
    private Material[] defaultMaterials;
    private Material[] fadeSourceMaterials;
    private Material[] fadeMaterials;
    private MaterialPropertyBlock alphaBlock;
    private Vector3 defaultColliderSize;
    private Vector3 defaultColliderCenter;
    private float expiresAt;
    private float lifetimeDuration;
    private float fadeStartRemainingPercent;
    private float currencyBlinkStartPercent;
    private float currencyFastBlinkStartPercent;
    private float currencyBlinkFrequency;
    private float currencyFastBlinkFrequency;
    private bool isCurrency;
    private CombatParticleProfile profile;
    private EnemyBodyPartProfile bodyPartProfile;
    private float noiseSeed;

    public bool HasExpired => Time.time >= expiresAt;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        boxCollider = GetComponent<BoxCollider>();
        if (meshFilter != null)
            defaultMesh = meshFilter.sharedMesh;
        if (meshRenderer != null)
            defaultMaterials = meshRenderer.sharedMaterials;
        if (boxCollider != null)
        {
            defaultColliderSize = boxCollider.size;
            defaultColliderCenter = boxCollider.center;
        }
        alphaBlock = new MaterialPropertyBlock();
    }

    /// <summary>Encerra a particula para que o pool a recicle no proximo frame.</summary>
    public void ExpireNow()
    {
        expiresAt = 0f;
    }

    public void Initialize(CombatParticleProfile newProfile, Vector3 position,
        Vector3 initialVelocity, float size, Material material, int particleLayer,
        int currencyValue = 0, Material currencyMaterial = null,
        float currencyLifetimeMultiplier = 1f, float minimumCurrencyLifetime = 0f,
        float fadeStartPercent = 25f, float firstCurrencyBlinkStartPercent = 30f,
        float secondCurrencyBlinkStartPercent = 10f,
        float firstCurrencyBlinkFrequency = 3f,
        float secondCurrencyBlinkFrequency = 8f)
    {
        RestoreDefaultVisual();
        profile = newProfile;
        bodyPartProfile = null;
        gameObject.layer = particleLayer;
        transform.SetPositionAndRotation(position, Random.rotation);
        transform.localScale = Vector3.one * size;
        Material selectedMaterial = currencyValue > 0 && currencyMaterial != null
            ? currencyMaterial : material;
        ConfigureFadeMaterials(new[] { selectedMaterial });
        if (body == null)
            body = GetComponent<Rigidbody>();
        body.mass = profile.mass;
        body.linearDamping = profile.linearDamping;
        body.angularDamping = profile.angularDamping;
        body.useGravity = profile.kind != CombatParticleKind.Cotton;
        // A ativacao de um Rigidbody reutilizado pode limpar sua velocidade.
        // Ativa primeiro e aplica o impulso baseado no dano em seguida.
        gameObject.SetActive(true);
        body.linearVelocity = initialVelocity;
        body.angularVelocity = Random.insideUnitSphere * 8f;
        body.WakeUp();
        float lifetime = currencyValue > 0
            ? Mathf.Max(minimumCurrencyLifetime,
                profile.lifetime * currencyLifetimeMultiplier)
            : profile.lifetime;
        ConfigureLifetime(lifetime, currencyValue > 0, fadeStartPercent,
            firstCurrencyBlinkStartPercent,
            secondCurrencyBlinkStartPercent,
            firstCurrencyBlinkFrequency, secondCurrencyBlinkFrequency);
        noiseSeed = Random.value * 1000f;
        if (TryGetComponent(out CurrencyGorePickup pickup))
            pickup.Initialize(currencyValue);
    }

    /// <summary>Inicializa uma peca real do modelo como particula fisica.</summary>
    public void InitializeMeshPart(EnemyBodyPartProfile newProfile, Mesh mesh,
        bool ownsMesh, Vector3 position, Quaternion rotation, Vector3 worldScale,
        Vector3 initialVelocity, Material[] materials, int particleLayer,
        float fadeStartPercent = 25f)
    {
        RestoreDefaultVisual();
        profile = null;
        bodyPartProfile = newProfile;
        gameObject.layer = particleLayer;
        transform.SetPositionAndRotation(position, rotation);
        Vector3 parentScale = transform.parent != null
            ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            worldScale.x * InverseScale(parentScale.x),
            worldScale.y * InverseScale(parentScale.y),
            worldScale.z * InverseScale(parentScale.z));

        if (meshFilter != null)
        {
            meshFilter.sharedMesh = mesh;
            if (ownsMesh)
                generatedMesh = mesh;
        }
        ConfigureFadeMaterials(materials);
        if (boxCollider != null && mesh != null)
        {
            Vector3 size = mesh.bounds.size;
            boxCollider.size = new Vector3(Mathf.Max(.02f, size.x),
                Mathf.Max(.02f, size.y), Mathf.Max(.02f, size.z));
            boxCollider.center = mesh.bounds.center;
        }

        if (body == null)
            body = GetComponent<Rigidbody>();
        body.mass = bodyPartProfile.mass;
        body.linearDamping = bodyPartProfile.linearDamping;
        body.angularDamping = bodyPartProfile.angularDamping;
        body.useGravity = false;
        gameObject.SetActive(true);
        body.linearVelocity = initialVelocity;
        body.angularVelocity = Random.insideUnitSphere *
            bodyPartProfile.randomAngularSpeed;
        body.WakeUp();
        ConfigureLifetime(bodyPartProfile.lifetime, false, fadeStartPercent,
            0f, 0f, 0f, 0f);
        if (TryGetComponent(out CurrencyGorePickup meshPartPickup))
            meshPartPickup.Initialize(0);
    }

    private void ConfigureLifetime(float lifetime, bool currency,
        float fadeStartPercent, float firstBlinkPercent,
        float secondBlinkPercent, float firstBlinkFrequency,
        float secondBlinkFrequency)
    {
        lifetimeDuration = Mathf.Max(.001f, lifetime);
        expiresAt = Time.time + lifetimeDuration;
        isCurrency = currency;
        fadeStartRemainingPercent = Mathf.Clamp(fadeStartPercent, 0f, 100f);
        currencyBlinkStartPercent = Mathf.Clamp(firstBlinkPercent, 0f, 100f);
        currencyFastBlinkStartPercent = Mathf.Clamp(secondBlinkPercent, 0f,
            currencyBlinkStartPercent);
        currencyBlinkFrequency = Mathf.Max(0f, firstBlinkFrequency);
        currencyFastBlinkFrequency = Mathf.Max(0f, secondBlinkFrequency);
        ApplyFadeOrCurrencyBlink(1f);
    }

    private void ConfigureFadeMaterials(Material[] sourceMaterials)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null || sourceMaterials == null ||
            sourceMaterials.Length == 0)
            return;

        bool canReuse = fadeMaterials != null &&
            fadeSourceMaterials != null &&
            fadeSourceMaterials.Length == sourceMaterials.Length;
        if (canReuse)
        {
            for (int index = 0; index < sourceMaterials.Length; index++)
            {
                if (fadeSourceMaterials[index] == sourceMaterials[index])
                    continue;
                canReuse = false;
                break;
            }
        }

        if (!canReuse)
        {
            DestroyFadeMaterials();
            fadeSourceMaterials = (Material[])sourceMaterials.Clone();
            fadeMaterials = new Material[sourceMaterials.Length];
            for (int index = 0; index < sourceMaterials.Length; index++)
            {
                Material source = sourceMaterials[index];
                if (source == null)
                    continue;
                Material fadeMaterial = new(source)
                {
                    name = $"{source.name} (Particle Fade)"
                };
                ConfigureTransparentMaterial(fadeMaterial);
                fadeMaterials[index] = fadeMaterial;
            }
        }

        meshRenderer.sharedMaterials = fadeMaterials;
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_SrcBlendAlpha"))
                material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            if (material.HasProperty("_DstBlendAlpha"))
                material.SetInt("_DstBlendAlpha",
                    (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private void DestroyFadeMaterials()
    {
        if (fadeMaterials != null)
        {
            foreach (Material fadeMaterial in fadeMaterials)
            {
                if (fadeMaterial != null)
                    Destroy(fadeMaterial);
            }
        }

        fadeMaterials = null;
        fadeSourceMaterials = null;
    }

    private void ApplyFadeOrCurrencyBlink(float alpha)
    {
        if (meshRenderer == null || fadeSourceMaterials == null ||
            alphaBlock == null)
            return;

        for (int index = 0; index < fadeSourceMaterials.Length; index++)
        {
            Material source = fadeSourceMaterials[index];
            if (source == null)
                continue;

            Color color = source.HasProperty(BaseColorProperty)
                ? source.GetColor(BaseColorProperty)
                : source.HasProperty(LegacyColorProperty)
                    ? source.GetColor(LegacyColorProperty) : Color.white;
            color.a *= alpha;
            alphaBlock.Clear();
            if (source.HasProperty(BaseColorProperty))
                alphaBlock.SetColor(BaseColorProperty, color);
            if (source.HasProperty(LegacyColorProperty))
                alphaBlock.SetColor(LegacyColorProperty, color);
            meshRenderer.SetPropertyBlock(alphaBlock, index);
        }
    }

    private static float InverseScale(float value) =>
        Mathf.Abs(value) > .0001f ? 1f / value : 1f;

    private void OnDisable() => RestoreDefaultVisual();

    private void RestoreDefaultVisual()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        if (generatedMesh != null)
        {
            if (meshFilter != null && meshFilter.sharedMesh == generatedMesh)
                meshFilter.sharedMesh = defaultMesh;
            Destroy(generatedMesh);
            generatedMesh = null;
        }
        if (meshFilter != null && defaultMesh != null)
            meshFilter.sharedMesh = defaultMesh;
        if (boxCollider != null)
        {
            boxCollider.size = defaultColliderSize;
            boxCollider.center = defaultColliderCenter;
        }
        if (meshRenderer != null)
        {
            meshRenderer.SetPropertyBlock(null);
            if (fadeMaterials != null)
                for (int index = 0; index < fadeMaterials.Length; index++)
                    meshRenderer.SetPropertyBlock(null, index);
        }
        if (meshRenderer != null && defaultMaterials != null)
            meshRenderer.sharedMaterials = defaultMaterials;
    }

    private void OnDestroy() => DestroyFadeMaterials();

    private void Update()
    {
        if (lifetimeDuration <= 0f || expiresAt <= 0f)
            return;

        float remainingPercent = Mathf.Clamp01(
            (expiresAt - Time.time) / lifetimeDuration) * 100f;
        if (isCurrency)
        {
            float frequency = remainingPercent <= currencyFastBlinkStartPercent
                ? currencyFastBlinkFrequency
                : remainingPercent <= currencyBlinkStartPercent
                    ? currencyBlinkFrequency : 0f;
            float alpha = frequency <= 0f ||
                Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) >= 0f
                    ? 1f : .12f;
            ApplyFadeOrCurrencyBlink(alpha);
            return;
        }

        float alphaFade = fadeStartRemainingPercent <= 0f ||
            remainingPercent > fadeStartRemainingPercent
                ? 1f
                : remainingPercent / fadeStartRemainingPercent;
        ApplyFadeOrCurrencyBlink(alphaFade);
    }

    private void FixedUpdate()
    {
        if (bodyPartProfile != null)
        {
            body.AddForce(Physics.gravity * bodyPartProfile.gravityMultiplier,
                ForceMode.Acceleration);
            return;
        }

        if (profile == null || profile.kind != CombatParticleKind.Cotton) return;
        float time = Time.time;
        Vector3 wind = new(Mathf.PerlinNoise(noiseSeed, time * .35f) * 2f - 1f,
            0f, Mathf.PerlinNoise(noiseSeed + 41f, time * .35f) * 2f - 1f);
        body.AddForce(Physics.gravity * profile.cottonGravityMultiplier +
            wind * profile.cottonWindForce, ForceMode.Acceleration);
    }
}

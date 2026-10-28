using UnityEngine;

/// <summary>Particula visual com fisica limitada ao mundo configurado.</summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public sealed class CombatVisualParticle : MonoBehaviour
{
    private Rigidbody body;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private BoxCollider boxCollider;
    private Mesh defaultMesh;
    private Mesh generatedMesh;
    private Material[] defaultMaterials;
    private Vector3 defaultColliderSize;
    private Vector3 defaultColliderCenter;
    private float expiresAt;
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
    }

    /// <summary>Encerra a particula para que o pool a recicle no proximo frame.</summary>
    public void ExpireNow()
    {
        expiresAt = 0f;
    }

    public void Initialize(CombatParticleProfile newProfile, Vector3 position,
        Vector3 initialVelocity, float size, Material material, int particleLayer,
        int currencyValue = 0, Material currencyMaterial = null,
        float currencyLifetimeMultiplier = 1f, float minimumCurrencyLifetime = 0f)
    {
        RestoreDefaultVisual();
        profile = newProfile;
        bodyPartProfile = null;
        gameObject.layer = particleLayer;
        transform.SetPositionAndRotation(position, Random.rotation);
        transform.localScale = Vector3.one * size;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = currencyValue > 0 && currencyMaterial != null
                ? currencyMaterial : material;
        body ??= GetComponent<Rigidbody>();
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
        expiresAt = Time.time + lifetime;
        noiseSeed = Random.value * 1000f;
        GetComponent<CurrencyGorePickup>()?.Initialize(currencyValue);
    }

    /// <summary>Inicializa uma peca real do modelo como particula fisica.</summary>
    public void InitializeMeshPart(EnemyBodyPartProfile newProfile, Mesh mesh,
        bool ownsMesh, Vector3 position, Quaternion rotation, Vector3 worldScale,
        Vector3 initialVelocity, Material[] materials, int particleLayer)
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
        if (meshRenderer != null && materials != null && materials.Length > 0)
            meshRenderer.sharedMaterials = materials;
        if (boxCollider != null && mesh != null)
        {
            Vector3 size = mesh.bounds.size;
            boxCollider.size = new Vector3(Mathf.Max(.02f, size.x),
                Mathf.Max(.02f, size.y), Mathf.Max(.02f, size.z));
            boxCollider.center = mesh.bounds.center;
        }

        body ??= GetComponent<Rigidbody>();
        body.mass = bodyPartProfile.mass;
        body.linearDamping = bodyPartProfile.linearDamping;
        body.angularDamping = bodyPartProfile.angularDamping;
        body.useGravity = false;
        gameObject.SetActive(true);
        body.linearVelocity = initialVelocity;
        body.angularVelocity = Random.insideUnitSphere *
            bodyPartProfile.randomAngularSpeed;
        body.WakeUp();
        expiresAt = Time.time + bodyPartProfile.lifetime;
        GetComponent<CurrencyGorePickup>()?.Initialize(0);
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
        if (meshRenderer != null && defaultMaterials != null)
            meshRenderer.sharedMaterials = defaultMaterials;
        if (boxCollider != null)
        {
            boxCollider.size = defaultColliderSize;
            boxCollider.center = defaultColliderCenter;
        }
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

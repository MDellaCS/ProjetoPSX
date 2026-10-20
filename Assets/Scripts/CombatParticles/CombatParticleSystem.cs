using System.Collections.Generic;
using UnityEngine;

/// <summary>Servico central, pooled e independente para particulas de combate.</summary>
[DisallowMultipleComponent]
public sealed class CombatParticleSystem : MonoBehaviour
{
    private static CombatParticleSystem instance;
    private readonly List<CombatVisualParticle> activeVisual = new();
    private readonly Queue<CombatVisualParticle> freeVisual = new();
    private readonly Dictionary<CombatParticleProfile, Material> profileMaterials = new();
    private Transform poolRoot;
    private Material hitMaterial;
    private Material goreMaterial;
    private CombatParticleProfile defaultHit;
    private CombatParticleProfile defaultGore;
    private int particleLayer;

    [Tooltip("Numero maximo de particulas visuais simultaneas. As mais antigas sao reutilizadas ao atingir o limite.")]
    [Min(1)] [SerializeField] private int maximumVisualParticles = 192;
    [Tooltip("Layers que representam chao e paredes. Particulas so colidem com Colliders nestas layers.")]
    [SerializeField] private LayerMask worldCollisionLayers = ~0;

    public static CombatParticleSystem Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindAnyObjectByType<CombatParticleSystem>();
            if (instance != null) return instance;
            GameObject root = new("CombatParticleSystem");
            instance = root.AddComponent<CombatParticleSystem>();
            DontDestroyOnLoad(root);
            return instance;
        }
    }

    public static void ReportEnemyDamage(EnemyBase enemy, int damageDealt,
        bool lethal, Vector3 direction, Vector3 impactPosition,
        bool hasImpactPosition, EnemyGore gore)
    {
        if (enemy == null || damageDealt <= 0) return;
        CombatParticleSystem system = Instance;
        Renderer renderer = enemy.GetComponentInChildren<Renderer>(true);
        Vector3 position = hasImpactPosition
            ? impactPosition
            : renderer != null ? renderer.bounds.center : enemy.AttackOrigin;
        system.Emit(gore != null ? gore.DamageProfile : null, system.defaultHit,
            position, direction, damageDealt, renderer);
        if (lethal)
            system.Emit(gore != null ? gore.DeathProfile : null, system.defaultGore,
                position, direction, damageDealt, renderer);
    }

    public void EmitTestDeath(CombatParticleProfile overrideProfile,
        Vector3 position, Vector3 direction, int damageDealt, Renderer source)
    {
        EnsureDefaults();
        Emit(overrideProfile, defaultGore, position, direction, damageDealt, source);
    }

    public void Emit(CombatParticleProfile overrideProfile,
        CombatParticleProfile fallbackProfile, Vector3 position, Vector3 direction,
        int damageDealt, Renderer sourceRenderer)
    {
        EnsureDefaults();
        CombatParticleProfile profile = overrideProfile != null ? overrideProfile : fallbackProfile;
        int count = Mathf.Clamp(Mathf.RoundToInt(profile.countByDamage.Evaluate(damageDealt)),
            profile.minimumCount, profile.maximumCount);
        Material material = ResolveMaterial(profile, sourceRenderer);
        Vector3 baseDirection = direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.forward;
        for (int index = 0; index < count; index++)
        {
            Vector3 velocity = baseDirection * (profile.baseSpeed + damageDealt * profile.speedPerDamage) +
                Vector3.up * profile.upwardSpeed + Random.insideUnitSphere * profile.randomSpeed;
            float size = Random.Range(profile.minimumSize, profile.maximumSize);
            Vector3 scatter = profile.useScatter
                ? Random.insideUnitSphere * profile.scatterRadius : Vector3.zero;
            Vector3 spawnPosition = position + scatter + Random.insideUnitSphere * size;
            if (scatter.sqrMagnitude > .0001f)
                velocity += scatter.normalized * profile.randomSpeed;
            SpawnVisual(profile, spawnPosition, velocity, size, material);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        poolRoot = new GameObject("Combat Particle Pool").transform;
        poolRoot.SetParent(transform);
        particleLayer = LayerMask.NameToLayer("CombatParticle");
        if (particleLayer >= 0)
            Physics.IgnoreLayerCollision(particleLayer, particleLayer, true);
        else
            particleLayer = gameObject.layer;
        EnsureDefaults();
    }

    private void Update()
    {
        Reclaim(activeVisual, freeVisual, particle => particle.HasExpired);
    }

    /// <summary>Retorna se o Collider pertence ao chao ou parede permitido.</summary>
    public bool IsWorldCollision(Collider collider)
    {
        if (collider == null || collider.GetComponent<CombatVisualParticle>() != null)
            return false;
        if (collider.GetComponentInParent<PlayerHealth>() != null ||
            collider.GetComponentInParent<EnemyBase>() != null)
            return false;
        return (worldCollisionLayers.value & (1 << collider.gameObject.layer)) != 0;
    }

    private void SpawnVisual(CombatParticleProfile profile, Vector3 position,
        Vector3 velocity, float size, Material material)
    {
        CombatVisualParticle particle = freeVisual.Count > 0 ? freeVisual.Dequeue() :
            activeVisual.Count < maximumVisualParticles ? CreateVisual() : ReuseOldest(activeVisual, freeVisual);
        particle.Initialize(profile, position, velocity, size, material, particleLayer);
        activeVisual.Add(particle);
    }

    private CombatVisualParticle CreateVisual()
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = "Combat Visual Particle";
        piece.transform.SetParent(poolRoot);
        piece.AddComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        piece.AddComponent<CombatParticleCollisionFilter>();
        CombatVisualParticle particle = piece.AddComponent<CombatVisualParticle>();
        piece.SetActive(false);
        return particle;
    }

    private static T ReuseOldest<T>(List<T> active, Queue<T> free) where T : Component
    {
        T particle = active[0]; active.RemoveAt(0); particle.gameObject.SetActive(false); free.Enqueue(particle);
        return free.Dequeue();
    }

    private static void Reclaim<T>(List<T> active, Queue<T> free, System.Func<T, bool> expired) where T : Component
    {
        for (int index = active.Count - 1; index >= 0; index--)
        {
            T particle = active[index];
            if (particle != null && !expired(particle)) continue;
            active.RemoveAt(index);
            if (particle != null) { particle.gameObject.SetActive(false); free.Enqueue(particle); }
        }
    }

    private void EnsureDefaults()
    {
        if (defaultHit != null) return;
        hitMaterial = CreateMaterial(new Color(1f, .15f, .1f));
        goreMaterial = CreateMaterial(new Color(.75f, .05f, .05f));
        defaultHit = ScriptableObject.CreateInstance<CombatParticleProfile>();
        defaultHit.displayName = "Default Damage Hit"; defaultHit.kind = CombatParticleKind.Mechanical;
        defaultHit.countByDamage = AnimationCurve.Linear(0, 1, 100, 12); defaultHit.maximumCount = 16;
        defaultHit.minimumSize = .04f; defaultHit.maximumSize = .11f; defaultHit.lifetime = .45f;
        defaultHit.baseSpeed = 1.5f; defaultHit.speedPerDamage = .025f; defaultHit.material = hitMaterial;
        defaultGore = ScriptableObject.CreateInstance<CombatParticleProfile>();
        defaultGore.displayName = "Default Death Gore"; defaultGore.kind = CombatParticleKind.Mechanical;
        defaultGore.countByDamage = AnimationCurve.Linear(0, 5, 100, 20); defaultGore.maximumCount = 32;
        defaultGore.minimumSize = .1f; defaultGore.maximumSize = .28f; defaultGore.lifetime = 4f;
        defaultGore.baseSpeed = 3f; defaultGore.speedPerDamage = .07f; defaultGore.upwardSpeed = 2f; defaultGore.material = goreMaterial;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new(shader) { color = color }; return material;
    }

    private Material ResolveMaterial(CombatParticleProfile profile, Renderer source)
    {
        if (profile.useSourceMaterial && source != null && source.sharedMaterial != null) return source.sharedMaterial;
        if (profile.material != null) return profile.material;
        if (!profileMaterials.TryGetValue(profile, out Material material) || material == null)
        {
            material = CreateMaterial(profile.testColor);
            profileMaterials[profile] = material;
        }
        return material;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>Servico central, pooled e independente para particulas de combate.</summary>
[DisallowMultipleComponent]
public sealed class CombatParticleSystem : MonoBehaviour
{
    private static CombatParticleSystem instance;
    private readonly List<CombatVisualParticle> activeVisual = new();
    private readonly Queue<CombatVisualParticle> freeVisual = new();
    private readonly List<CombatVisualParticle> activeBodyParts = new();
    private readonly Queue<CombatVisualParticle> freeBodyParts = new();
    private readonly Dictionary<CombatParticleProfile, Material> profileMaterials = new();
    private Transform poolRoot;
    private Material hitMaterial;
    private Material goreMaterial;
    private Material currencyGoreMaterial;
    private CombatParticleProfile defaultHit;
    private CombatParticleProfile defaultGore;
    private int particleLayer;

    [Tooltip("Numero maximo de particulas visuais simultaneas. As mais antigas sao reutilizadas ao atingir o limite.")]
    [Min(1)] [SerializeField] private int maximumVisualParticles = 192;
    [Tooltip("Layers que representam chao e paredes. Particulas so colidem com Colliders nestas layers.")]
    [SerializeField] private LayerMask worldCollisionLayers = ~0;
    [Header("Currency Gore")]
    [Tooltip("Material opcional dos fragmentos de gore que carregam currency. Vazio usa um material dourado gerado pelo sistema.")]
    [SerializeField] private Material currencyGoreVisualMaterial;
    [Tooltip("Multiplica o tempo de vida normal dos fragmentos que carregam currency.")]
    [Min(1f)] [SerializeField] private float currencyGoreLifetimeMultiplier = 5f;
    [Tooltip("Tempo de vida minimo, em segundos, de um fragmento que carrega currency.")]
    [Min(0f)] [SerializeField] private float minimumCurrencyGoreLifetime = 12f;
    [Header("Desaparecimento dos Fragmentos")]
    [Tooltip("Porcentagem final do tempo de vida em que particulas comuns e partes do corpo comecam a ficar transparentes ate desaparecer.")]
    [Range(0f, 100f)] [SerializeField] private float particleFadeStartRemainingPercent = 25f;
    [Tooltip("Porcentagem restante do tempo de vida da currency em que ela comeca a piscar na primeira velocidade.")]
    [Range(0f, 100f)] [SerializeField] private float currencyBlinkStartRemainingPercent = 30f;
    [Tooltip("Porcentagem restante do tempo de vida da currency em que ela troca para a piscada mais rapida.")]
    [Range(0f, 100f)] [SerializeField] private float currencyFastBlinkStartRemainingPercent = 10f;
    [Tooltip("Quantidade de piscadas por segundo da currency no primeiro estagio de aviso.")]
    [Min(0f)] [SerializeField] private float currencyBlinkFrequency = 3f;
    [Tooltip("Quantidade de piscadas por segundo da currency no estagio final antes de desaparecer.")]
    [Min(0f)] [SerializeField] private float currencyFastBlinkFrequency = 9f;

    private void OnValidate()
    {
        currencyGoreLifetimeMultiplier = Mathf.Max(1f,
            currencyGoreLifetimeMultiplier);
        minimumCurrencyGoreLifetime = Mathf.Max(0f,
            minimumCurrencyGoreLifetime);
        particleFadeStartRemainingPercent = Mathf.Clamp(
            particleFadeStartRemainingPercent, 0f, 100f);
        currencyBlinkStartRemainingPercent = Mathf.Clamp(
            currencyBlinkStartRemainingPercent, 0f, 100f);
        currencyFastBlinkStartRemainingPercent = Mathf.Clamp(
            currencyFastBlinkStartRemainingPercent, 0f,
            currencyBlinkStartRemainingPercent);
        currencyBlinkFrequency = Mathf.Max(0f, currencyBlinkFrequency);
        currencyFastBlinkFrequency = Mathf.Max(0f,
            currencyFastBlinkFrequency);
    }

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
        bool hasImpactPosition, EnemyGore gore, int finalHitDamage = 0,
        int enemyMaxHealth = 0)
    {
        if (enemy == null || damageDealt <= 0) return;
        CombatParticleSystem system = Instance;
        Renderer renderer = enemy.GetComponentInChildren<Renderer>(true);
        Vector3 position = hasImpactPosition
            ? impactPosition
            : renderer != null ? renderer.bounds.center : enemy.AttackOrigin;
        system.Emit(gore != null ? gore.DamageProfile : null, system.defaultHit,
            position, direction, damageDealt, renderer,
            gore != null ? gore.ParticleCountMultiplier : 1f);
        if (lethal)
        {
            int currencyGorePieces = gore != null
                ? gore.GetCurrencyGorePieceCount(finalHitDamage, enemyMaxHealth) : 0;
            system.Emit(gore != null ? gore.DeathProfile : null,
                system.defaultGore, position, direction, damageDealt,
                renderer, gore != null ? gore.ParticleCountMultiplier : 1f,
                gore != null ? gore.CurrencyValueRange : default,
                currencyGorePieces);
            if (gore != null && gore.IsMechanical)
                system.Emit(gore.DeathProfile, system.defaultGore, position,
                    direction, damageDealt, renderer, gore.ParticleCountMultiplier,
                    gore.VitalOilValue, Mathf.Max(1, gore.CurrencyGorePieces), true);
        }
    }

    public void EmitTestDeath(CombatParticleProfile overrideProfile,
        Vector3 position, Vector3 direction, int damageDealt, Renderer source,
        float particleCountMultiplier = 1f)
    {
        EnsureDefaults();
        Emit(overrideProfile, defaultGore, position, direction, damageDealt,
            source, particleCountMultiplier);
    }

    /// <summary>
    /// Emissao para objetos de mundo. Sem perfil, usa o padrao de golpe ou o de
    /// morte do sistema. Com currency, cada moeda vira um fragmento coletavel.
    /// </summary>
    public void EmitWorldObjectParticles(CombatParticleProfile profile,
        bool useDeathDefault, Vector3 position, Vector3 direction,
        int damageDealt, Renderer source, float countMultiplier,
        Vector2Int currencyValueRange = default, int currencyPieces = 0)
    {
        EnsureDefaults();
        Emit(profile, useDeathDefault ? defaultGore : defaultHit, position,
            direction, damageDealt, source, countMultiplier, currencyValueRange,
            currencyPieces);
    }

    public void EmitCurrencyDeath(CombatParticleProfile profile, Vector3 position,
        Vector3 direction, int damage, Renderer source, float countMultiplier,
        Vector2Int valueRange, int currencyPieces)
    {
        EnsureDefaults();
        Emit(profile, defaultGore, position, direction, damage, source,
            countMultiplier, valueRange, currencyPieces);
    }

    public void Emit(CombatParticleProfile overrideProfile,
        CombatParticleProfile fallbackProfile, Vector3 position, Vector3 direction,
        int damageDealt, Renderer sourceRenderer,
        float particleCountMultiplier = 1f, Vector2Int currencyValueRange = default,
        int currencyGorePieces = 0, bool vitalOil = false)
    {
        EnsureDefaults();
        CombatParticleProfile profile = overrideProfile != null ? overrideProfile : fallbackProfile;
        int profileCount = Mathf.Clamp(
            Mathf.RoundToInt(profile.countByDamage.Evaluate(damageDealt)),
            profile.minimumCount, profile.maximumCount);
        // O multiplicador do EnemyGore e propositalmente posterior ao clamp
        // do profile: ele personaliza a densidade deste inimigo sem mudar os
        // limites compartilhados pelo perfil.
        int count = Mathf.Max(0, Mathf.RoundToInt(profileCount *
            Mathf.Max(0f, particleCountMultiplier)));
        bool emitCurrency = currencyGorePieces > 0 &&
            Mathf.Max(currencyValueRange.x, currencyValueRange.y) > 0;
        // Currency tem prioridade: cada pickup precisa de sua propria
        // particula e currencyGorePieces tambem define o minimo visual.
        if (emitCurrency)
            count = Mathf.Max(count, currencyGorePieces);
        Material material = ResolveMaterial(profile, sourceRenderer);
        Vector3 baseDirection = direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.forward;
        int collectibleCount = emitCurrency ? currencyGorePieces : 0;
        int minimumValue = Mathf.Min(currencyValueRange.x, currencyValueRange.y);
        int maximumValue = Mathf.Max(currencyValueRange.x, currencyValueRange.y);
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
            int value = index < collectibleCount
                ? Random.Range(minimumValue, maximumValue + 1) : 0;
            SpawnVisual(profile, spawnPosition, velocity, size, material, value, vitalOil);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        // Quando e filho do GameRoot, a raiz ja e persistente. Unity so
        // aceita DontDestroyOnLoad em objetos raiz; chamar aqui em um filho
        // geraria o aviso e nao acrescentaria persistencia alguma.
        if (GetComponentInParent<PersistentGameRoot>() == null)
            DontDestroyOnLoad(gameObject);
        poolRoot = new GameObject("Combat Particle Pool").transform;
        poolRoot.SetParent(transform);
        particleLayer = LayerMask.NameToLayer("CombatParticle");
        if (particleLayer >= 0)
            Physics.IgnoreLayerCollision(particleLayer, particleLayer, false);
        else
            particleLayer = gameObject.layer;
        EnsureDefaults();
    }

    private void Update()
    {
        Reclaim(activeVisual, freeVisual, particle => particle.HasExpired);
        Reclaim(activeBodyParts, freeBodyParts, particle => particle.HasExpired);
    }

    /// <summary>Retorna se o collider deve participar da fisica das particulas.</summary>
    public bool CanParticleCollideWith(Collider collider)
    {
        if (collider == null)
            return false;

        // As particulas interagem umas com as outras, mas nunca empurram
        // entidades com controllers proprios ou corpos dinamicos.
        if (collider.GetComponentInParent<CombatVisualParticle>() != null)
            return true;
        if (collider.attachedRigidbody != null ||
            collider.GetComponentInParent<PlayerMovement>() != null ||
            collider.GetComponentInParent<PlayerHealth>() != null ||
            collider.GetComponentInParent<PlayerCharacterController>() != null ||
            collider.GetComponentInParent<EnemyBase>() != null ||
            collider.GetComponentInParent<EnemyMovement>() != null)
            return false;

        return (worldCollisionLayers.value & (1 << collider.gameObject.layer)) != 0;
    }

    private void SpawnVisual(CombatParticleProfile profile, Vector3 position,
        Vector3 velocity, float size, Material material, int currencyValue = 0, bool vitalOil = false)
    {
        CombatVisualParticle particle = freeVisual.Count > 0
            ? freeVisual.Dequeue()
            : activeVisual.Count < maximumVisualParticles
                ? CreateVisual()
                : ReuseOldestNonCurrencyVisual();
        particle.Initialize(profile, position, velocity, size, material, particleLayer,
            currencyValue, CurrencyGoreMaterial, currencyGoreLifetimeMultiplier,
            minimumCurrencyGoreLifetime, particleFadeStartRemainingPercent,
            currencyBlinkStartRemainingPercent,
            currencyFastBlinkStartRemainingPercent,
            currencyBlinkFrequency, currencyFastBlinkFrequency, vitalOil);
        activeVisual.Add(particle);
    }

    private CombatVisualParticle ReuseOldestNonCurrencyVisual()
    {
        for (int i = 0; i < activeVisual.Count; i++)
        {
            CombatVisualParticle candidate = activeVisual[i];
            if (candidate == null)
            {
                // Particula destruida externamente: descarta o registro e tenta o proximo.
                activeVisual.RemoveAt(i--);
                continue;
            }
            if (candidate.TryGetComponent(out CurrencyGorePickup pickup) &&
                pickup.HasCurrency)
                continue;

            activeVisual.RemoveAt(i);
            candidate.gameObject.SetActive(false);
            return candidate;
        }

        // Currency ja conquistada nao pode sumir para respeitar o limite visual.
        return CreateVisual();
    }

    public void EmitBodyPart(Mesh mesh, bool ownsMesh, Vector3 position,
        Quaternion rotation, Vector3 worldScale, Vector3 velocity,
        Material[] materials, EnemyBodyPartProfile profile)
    {
        if (mesh == null || profile == null)
        {
            if (ownsMesh && mesh != null)
                Destroy(mesh);
            return;
        }

        SpawnMeshVisual(profile, mesh, ownsMesh, position, rotation,
            worldScale, velocity, materials);
    }

    private void SpawnMeshVisual(EnemyBodyPartProfile profile, Mesh mesh,
        bool ownsMesh, Vector3 position, Quaternion rotation,
        Vector3 worldScale, Vector3 velocity, Material[] materials)
    {
        // O pool de partes cresce quando necessario e nao toma slots do pool
        // limitado de gore/currency. Assim, nenhuma parte e substituida por
        // uma particula comum durante uma morte com muitos segmentos.
        CombatVisualParticle particle = freeBodyParts.Count > 0
            ? freeBodyParts.Dequeue() : CreateVisual(true);
        particle.InitializeMeshPart(profile, mesh, ownsMesh, position,
            rotation, worldScale, velocity, materials, particleLayer,
            particleFadeStartRemainingPercent);
        activeBodyParts.Add(particle);
    }

    private CombatVisualParticle CreateVisual(bool isBodyPart = false)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = isBodyPart ? "Combat Body Part Particle" :
            "Combat Visual Particle";
        piece.transform.SetParent(poolRoot);
        piece.AddComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        piece.AddComponent<CombatParticleCollisionFilter>();
        piece.AddComponent<CurrencyGorePickup>();
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
        currencyGoreMaterial = CreateMaterial(new Color(1f, .72f, .08f));
        if (currencyGoreMaterial.HasProperty("_EmissionColor"))
        {
            currencyGoreMaterial.EnableKeyword("_EMISSION");
            currencyGoreMaterial.SetColor("_EmissionColor", new Color(1f, .4f, .01f));
        }
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
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material material = new(shader) { color = color }; return material;
    }

    private Material CurrencyGoreMaterial => currencyGoreVisualMaterial != null
        ? currencyGoreVisualMaterial : currencyGoreMaterial;

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

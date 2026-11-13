using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Resultado de consultar a agua em uma posicao.</summary>
public struct WaterSample
{
    public WaterVolume volume;
    public float surfaceY;
    /// <summary>Altura do chao abaixo da superficie (ou um valor bem profundo se nao houver chao).</summary>
    public float floorY;

    public float ColumnDepth => surfaceY - floorY;
    public WaterProfile Profile => volume.Profile;
}

/// <summary>
/// Volume de agua. Defina a area com o Collider (o TOPO do collider e a
/// superficie). Use um BoxCollider (ou outro collider convexo); o collider e
/// forcado a ser trigger. O jogador e os inimigos reagem por meio do
/// WaterSensor; este componente so responde "onde esta a superficie aqui?" e
/// dispara eventos para efeitos (respingos, sons, shader).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Projeto PSX/World/Water Volume")]
public sealed class WaterVolume : MonoBehaviour
{
    private const int FloorHitBufferSize = 16;
    // Entidade com os pes mais de X abaixo do chao medido esta em outro nivel
    // (corredor sob um lago): nao pertence a este volume.
    private const float BelowFloorTolerance = .5f;
    private static readonly List<WaterVolume> activeVolumes = new();
    private static readonly RaycastHit[] floorHits = new RaycastHit[FloorHitBufferSize];
    private static readonly int SurfaceHeightId = Shader.PropertyToID("_WaterSurfaceY");

    [Header("Regras")]
    [Tooltip("Regras de gameplay desta agua (rasa/funda, multiplicadores, Heat). Vazio usa os valores padrao.")]
    [SerializeField] private WaterProfile profile;
    [Tooltip("Em sobreposicao de volumes, vence o de maior prioridade; em empate, o de superficie mais alta.")]
    [SerializeField] private int priority;

    [Header("Superficie")]
    [Tooltip("Desloca a superficie, em metros, em relacao ao topo do collider. Util para ajustar sem mexer no tamanho da caixa.")]
    [SerializeField] private float surfaceOffset;
    [Tooltip("Camadas consideradas chao ao medir a profundidade da agua. Personagens e particulas sao sempre ignorados. Prefira so as camadas do cenario.")]
    [SerializeField] private LayerMask floorLayers = ~0;
    [Tooltip("Distancia maxima, em metros, abaixo da superficie para procurar o chao. Sem chao dentro dessa distancia a agua conta como bem funda.")]
    [Min(1f)] [SerializeField] private float floorProbeDistance = 30f;

    [Header("Visual (preparado)")]
    [Tooltip("Material aplicado ao plano gerado. Use um material transparente de água ou atribua uma textura no material.")]
    [SerializeField] private Material surfaceMaterial;
    [Tooltip("Repetições da textura por metro no plano gerado. A textura deve usar Wrap Mode = Repeat.")]
    [Min(0.01f)] [SerializeField] private float textureRepeatsPerMeter = .2f;
    [Tooltip("Renderers da agua. Recebem _WaterSurfaceY (altura da superficie) para o shader futuro, sem precisar de codigo.")]
    [SerializeField] private Renderer[] visualRenderers;

    [Header("Eventos")]
    [Tooltip("Chamado quando uma entidade (jogador ou inimigo) entra na agua. Recebe o GameObject. Use para respingo e som.")]
    [SerializeField] private UnityEvent<GameObject> onEntityEntered;
    [Tooltip("Chamado quando uma entidade sai da agua.")]
    [SerializeField] private UnityEvent<GameObject> onEntityExited;

    private Collider volumeCollider;
    private MaterialPropertyBlock propertyBlock;
    private bool warnedCollider;

    /// <summary>Volumes ativos. Vazio quando a cena nao tem agua (custo zero).</summary>
    public static IReadOnlyList<WaterVolume> Active => activeVolumes;

    public WaterProfile Profile => profile != null ? profile : WaterProfile.Fallback;
    public int Priority => priority;
    public Material SurfaceMaterial => surfaceMaterial;
    public float TextureRepeatsPerMeter => textureRepeatsPerMeter;
    public Collider VolumeCollider => volumeCollider != null ? volumeCollider : GetComponent<Collider>();

    public void SetSurfaceVisual(Material material, Renderer renderer)
    {
        surfaceMaterial = material;
        visualRenderers = renderer != null ? new[] { renderer } : System.Array.Empty<Renderer>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => activeVolumes.Clear();

    private void Reset()
    {
        if (TryGetComponent(out Collider collider))
            collider.isTrigger = true;
    }

    private void Awake()
    {
        volumeCollider = GetComponent<Collider>();
        ValidateCollider();
    }

    private void OnEnable()
    {
        if (volumeCollider == null)
            volumeCollider = GetComponent<Collider>();
        if (!activeVolumes.Contains(this))
            activeVolumes.Add(this);
    }

    private void OnDisable() => activeVolumes.Remove(this);

    // Sem isTrigger a agua viraria chao solido; ClosestPoint (usado para a area)
    // so funciona em colliders convexos.
    private void ValidateCollider()
    {
        if (volumeCollider == null)
            return;

        volumeCollider.isTrigger = true;
        bool supported = volumeCollider is BoxCollider ||
            volumeCollider is SphereCollider || volumeCollider is CapsuleCollider ||
            (volumeCollider is MeshCollider mesh && mesh.convex);
        if (!supported && !warnedCollider)
        {
            warnedCollider = true;
            Debug.LogWarning($"WaterVolume '{name}': use BoxCollider (ou outro collider convexo). Colliders nao convexos nao funcionam como area de agua.", this);
        }
    }

    private void LateUpdate()
    {
        if (visualRenderers == null || visualRenderers.Length == 0)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        float height = SurfaceHeightAt(transform.position.x, transform.position.z);
        foreach (Renderer renderer in visualRenderers)
        {
            if (renderer == null)
                continue;
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(SurfaceHeightId, height);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    /// <summary>
    /// Altura da superficie em (x, z). Ponto unico de verdade para gameplay e
    /// shader: hoje e plana; ondas futuras entram aqui.
    /// </summary>
    public float SurfaceHeightAt(float x, float z)
    {
        Collider collider = VolumeCollider;
        Bounds bounds = collider != null
            ? collider.bounds : new Bounds(transform.position, Vector3.zero);
        return bounds.max.y + surfaceOffset;
    }

    /// <summary>Verdadeiro se (x, z) esta dentro da area horizontal do volume.</summary>
    public bool ContainsHorizontally(float x, float z)
    {
        if (volumeCollider == null || !volumeCollider.enabled)
            return false;

        Bounds bounds = volumeCollider.bounds;
        if (x < bounds.min.x || x > bounds.max.x ||
            z < bounds.min.z || z > bounds.max.z)
            return false;

        // Ponto logo abaixo da superficie: funciona para caixas giradas em Y.
        Vector3 probe = new(x, bounds.max.y - .01f, z);
        return (volumeCollider.ClosestPoint(probe) - probe).sqrMagnitude < 1e-6f;
    }

    /// <summary>
    /// Superficie mais alta de agua em (x, z) que nao esteja acima de
    /// <paramref name="maxSurfaceY"/>, sem exigir contato. Usado por voadores,
    /// que passam o proprio nivel para ignorar lagos acima deles.
    /// </summary>
    public static bool TryGetSurfaceAt(float x, float z, float maxSurfaceY,
        out float surfaceY)
    {
        surfaceY = float.MinValue;
        bool found = false;
        for (int i = 0; i < activeVolumes.Count; i++)
        {
            WaterVolume volume = activeVolumes[i];
            if (volume == null || !volume.ContainsHorizontally(x, z))
                continue;
            float height = volume.SurfaceHeightAt(x, z);
            if (height > maxSurfaceY)
                continue;
            if (!found || height > surfaceY)
                surfaceY = height;
            found = true;
        }

        return found;
    }

    /// <summary>
    /// Agua em (x, z) para uma entidade cujos pes estao em <paramref name="feetY"/>.
    /// Retorna falso se nao houver agua, se os pes estiverem acima da superficie
    /// ou se a entidade estiver abaixo do fundo (outro nivel).
    /// <paramref name="currentVolume"/> e o volume em que a entidade ja estava:
    /// ele recebe uma folga (histerese) para nao alternar na borda.
    /// </summary>
    public static bool TryGetSample(float x, float z, float feetY,
        Transform exclude, WaterVolume currentVolume, out WaterSample sample)
    {
        sample = default;
        WaterVolume best = null;
        float bestSurface = 0f;
        for (int i = 0; i < activeVolumes.Count; i++)
        {
            WaterVolume volume = activeVolumes[i];
            if (volume == null || !volume.ContainsHorizontally(x, z))
                continue;

            float surface = volume.SurfaceHeightAt(x, z);
            WaterProfile volumeProfile = volume.Profile;
            float required = Mathf.Max(0f, volumeProfile.minimumContactDepth -
                (volume == currentVolume ? volumeProfile.contactHysteresis : 0f));
            if (surface - feetY < required)
                continue;
            if (feetY < volume.volumeCollider.bounds.min.y - BelowFloorTolerance)
                continue;

            bool better = best == null || volume.priority > best.priority ||
                (volume.priority == best.priority && surface > bestSurface);
            if (better)
            {
                best = volume;
                bestSurface = surface;
            }
        }

        if (best == null)
            return false;

        float floorY = best.FindFloorY(x, z, bestSurface, exclude);
        if (feetY < floorY - BelowFloorTolerance)
            return false;

        sample = new WaterSample
        {
            volume = best,
            surfaceY = bestSurface,
            floorY = floorY
        };
        return true;
    }

    private float FindFloorY(float x, float z, float surface, Transform exclude)
    {
        Vector3 origin = new(x, surface, z);
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, floorHits,
            floorProbeDistance, floorLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        float floor = surface - floorProbeDistance;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = floorHits[i];
            Collider collider = hit.collider;
            if (collider == null || hit.distance >= nearest)
                continue;
            if (exclude != null && collider.transform.IsChildOf(exclude))
                continue;
            // Personagens e particulas nao sao chao.
            if (collider.GetComponentInParent<EnemyBase>() != null ||
                collider.GetComponentInParent<PlayerMovement>() != null ||
                collider.GetComponentInParent<CombatVisualParticle>() != null)
                continue;
            nearest = hit.distance;
            floor = hit.point.y;
        }

        return floor;
    }

    internal void NotifyEntered(GameObject entity) => onEntityEntered?.Invoke(entity);
    internal void NotifyExited(GameObject entity) => onEntityExited?.Invoke(entity);

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !DebugGizmoSettings.Enabled(DebugGizmoGroup.Water))
            return;

        Collider collider = volumeCollider != null
            ? volumeCollider : GetComponent<Collider>();
        if (collider == null)
            return;

        Bounds bounds = collider.bounds;
        float surface = bounds.max.y + surfaceOffset;
        Gizmos.color = new Color(.035f, .12f, .28f, .42f);
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(.025f, .09f, .23f, .9f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        // Plano da superficie.
        Gizmos.color = new Color(.08f, .25f, .48f, .4f);
        Gizmos.DrawCube(new Vector3(bounds.center.x, surface, bounds.center.z),
            new Vector3(bounds.size.x, .035f, bounds.size.z));
        Gizmos.color = new Color(.12f, .34f, .58f, .95f);
        Gizmos.DrawWireCube(new Vector3(bounds.center.x, surface, bounds.center.z),
            new Vector3(bounds.size.x, 0f, bounds.size.z));
        // Limite entre agua rasa e funda.
        float depthLimitY = surface - Profile.deepWaterMinDepth;
        Gizmos.color = new Color(.055f, .2f, .4f, .28f);
        Gizmos.DrawCube(new Vector3(bounds.center.x, depthLimitY, bounds.center.z),
            new Vector3(bounds.size.x, .025f, bounds.size.z));
        Gizmos.color = new Color(.16f, .4f, .66f, .88f);
        Gizmos.DrawWireCube(
            new Vector3(bounds.center.x, depthLimitY, bounds.center.z),
            new Vector3(bounds.size.x, 0f, bounds.size.z));
    }
}

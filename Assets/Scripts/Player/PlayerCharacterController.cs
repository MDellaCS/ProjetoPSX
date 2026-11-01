using UnityEngine;

/// <summary>
/// Motor cinemático próprio do jogador. Move uma cápsula com consultas
/// explícitas, sem depender de CharacterController.Move ou isGrounded.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider))]
public sealed class PlayerCharacterController : MonoBehaviour
{
    [Header("Collision Shape")]
    [Tooltip("Altura inicial da cápsula de colisão do jogador, em unidades locais.")]
    [Min(.1f)] [SerializeField] private float capsuleHeight = 2.5f;
    [Tooltip("Raio inicial da cápsula de colisão do jogador, em unidades locais.")]
    [Min(.05f)] [SerializeField] private float capsuleRadius = .55f;
    [Tooltip("Centro local da cápsula. Ajuste para alinhar sua base com o chão e o visual do jogador.")]
    [SerializeField] private Vector3 capsuleCenter = new(0f, 1.25f, 0f);

    [Header("Collision Queries")]
    [Tooltip("Camadas sólidas que bloqueiam o movimento do jogador. Triggers são ignorados.")]
    [SerializeField] private LayerMask collisionLayers = Physics.DefaultRaycastLayers;
    [Tooltip("Distância mínima mantida entre a cápsula e uma superfície após uma colisão. Evita tremor e encaixe em quinas.")]
    [Min(.001f)] [SerializeField] private float skinWidth = .06f;
    [Tooltip("Número máximo de colisões resolvidas em uma chamada de movimento. Mais iterações permitem deslizar por cantos complexos.")]
    [Range(1, 12)] [SerializeField] private int maxSweepIterations = 5;
    [Tooltip("Número máximo de correções para retirar a cápsula de sobreposições antes de movê-la.")]
    [Range(0, 12)] [SerializeField] private int maxDepenetrationIterations = 4;
    [Tooltip("Tamanho dos buffers reutilizados pelas consultas de colisão. Aumente em cenas com muitos colliders próximos.")]
    [Range(8, 128)] [SerializeField] private int queryBufferSize = 32;

    [Header("Grounding and Slopes")]
    [Tooltip("Inclinação máxima, em graus, que o jogador pode subir caminhando. Acima dela, o movimento usa a descida da superfície.")]
    [Range(0f, 89f)] [SerializeField] private float maximumWalkableSlope = 45f;
    [Tooltip("Distância abaixo da cápsula usada para confirmar contato com o chão após o movimento.")]
    [Min(.001f)] [SerializeField] private float groundProbeDistance = .12f;
    [Tooltip("Altura máxima de degraus que o jogador pode subir automaticamente ao caminhar no chão.")]
    [Min(0f)] [SerializeField] private float stepHeight = .3f;
    [Tooltip("A distância máxima em que o motor tenta manter a cápsula apoiada no chão ao descer pequenos desníveis.")]
    [Min(0f)] [SerializeField] private float groundSnapDistance = .18f;

    [Header("Legacy Migration")]
    [Tooltip("Ao iniciar, copia altura, raio, centro e parâmetros de colisão do CharacterController nativo existente e o desativa. Desligue depois de remover o componente antigo.")]
    [SerializeField] private bool copyLegacyControllerSettings = true;

    private CapsuleCollider capsule;
    private CharacterController legacyController;
    private RaycastHit[] hitBuffer;
    private Collider[] overlapBuffer;
    private bool grounded;
    private bool hasGroundContact;
    private Vector3 groundNormal = Vector3.up;
    private CollisionFlags lastCollisionFlags;

    public float height
    {
        get => capsule != null ? capsule.height : capsuleHeight;
        set { capsuleHeight = Mathf.Max(capsuleRadius * 2f, value); ApplyShape(); }
    }

    public float radius
    {
        get => capsule != null ? capsule.radius : capsuleRadius;
        set { capsuleRadius = Mathf.Max(.01f, value); ApplyShape(); }
    }

    public Vector3 center
    {
        get => capsule != null ? capsule.center : capsuleCenter;
        set { capsuleCenter = value; ApplyShape(); }
    }

    public Bounds bounds => capsule != null ? capsule.bounds : new Bounds(transform.position, Vector3.zero);
    public float slopeLimit => maximumWalkableSlope;
    public bool isGrounded => grounded;
    public bool IsColliderEnabled => capsule != null && capsule.enabled;
    public CapsuleCollider Collider => capsule;
    public Vector3 GroundNormal => hasGroundContact ? groundNormal : Vector3.up;
    public bool HasGroundContact => hasGroundContact;
    public CollisionFlags LastCollisionFlags => lastCollisionFlags;

    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
        legacyController = GetComponent<CharacterController>();
        if (copyLegacyControllerSettings && legacyController != null)
            CopyLegacySettings();

        capsule.direction = 1;
        // Este collider fornece bounds para outros sistemas; o motor resolve
        // contato manualmente por casts e não deve receber resposta física nativa.
        capsule.isTrigger = true;
        ApplyShape();

        if (legacyController != null)
            legacyController.enabled = false;

        int bufferSize = Mathf.Max(8, queryBufferSize);
        hitBuffer = new RaycastHit[bufferSize];
        overlapBuffer = new Collider[bufferSize];
        ProbeGround();
    }

    private void OnValidate()
    {
        capsuleHeight = Mathf.Max(.1f, capsuleHeight);
        capsuleRadius = Mathf.Max(.01f, capsuleRadius);
        capsuleHeight = Mathf.Max(capsuleHeight, capsuleRadius * 2f);
        skinWidth = Mathf.Max(.001f, skinWidth);
        maxSweepIterations = Mathf.Clamp(maxSweepIterations, 1, 12);
        maxDepenetrationIterations = Mathf.Clamp(maxDepenetrationIterations, 0, 12);
        queryBufferSize = Mathf.Clamp(queryBufferSize, 8, 128);
        maximumWalkableSlope = Mathf.Clamp(maximumWalkableSlope, 0f, 89f);
        groundProbeDistance = Mathf.Max(.001f, groundProbeDistance);
        if (capsule == null)
            capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
            ApplyShape();
    }

    public static PlayerCharacterController GetOrAdd(GameObject player)
    {
        PlayerCharacterController motor = player.GetComponent<PlayerCharacterController>();
        return motor != null ? motor : player.AddComponent<PlayerCharacterController>();
    }

    public CollisionFlags Move(Vector3 displacement)
    {
        if (!enabled || capsule == null || !capsule.enabled || !gameObject.activeInHierarchy)
            return CollisionFlags.None;

        lastCollisionFlags = CollisionFlags.None;
        Vector3 initialPosition = transform.position;
        ResolveOverlaps();

        Vector3 remaining = displacement;
        bool wasGrounded = grounded;
        grounded = false;
        hasGroundContact = false;
        groundNormal = Vector3.up;
        for (int iteration = 0; iteration < maxSweepIterations; iteration++)
        {
            float distance = remaining.magnitude;
            if (distance <= .0001f)
                break;

            Vector3 direction = remaining / distance;
            if (!TryFindNearestHit(direction, distance + skinWidth, out RaycastHit hit))
            {
                transform.position += remaining;
                remaining = Vector3.zero;
                break;
            }

            float travel = Mathf.Clamp(hit.distance - skinWidth, 0f, distance);
            if (travel > 0f)
                transform.position += direction * travel;

            remaining -= direction * travel;
            CollisionFlags hitFlag = GetCollisionFlag(hit.normal);
            lastCollisionFlags |= hitFlag;
            if (hit.normal.y > .0001f)
            {
                groundNormal = hit.normal.normalized;
                hasGroundContact = true;
                grounded = Vector3.Angle(groundNormal, Vector3.up) <= slopeLimit;
            }
            // Step-up e permitido apenas em obstaculos baixos. Uma parede
            // vertical pode produzir uma normal levemente inclinada por
            // causa da capsula/esfera do cast; validamos a altura real do
            // ponto de contato antes de tentar subir.
            if (wasGrounded && (hitFlag & CollisionFlags.Sides) != 0 &&
                hit.collider.GetComponentInParent<EnemyBase>() == null &&
                stepHeight > 0f && IsLowStepContact(hit) &&
                TryStep(ref remaining))
            {
                remaining = Vector3.zero;
                break;
            }

            Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
            if (Vector3.Dot(slide, remaining) <= .00001f)
            {
                remaining = Vector3.zero;
                break;
            }

            remaining = slide;
        }

        ProbeGround(allowSnap: displacement.y <= 0f);
        if ((transform.position - initialPosition).sqrMagnitude > .0000001f)
            Physics.SyncTransforms();
        return lastCollisionFlags;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        bool wasEnabled = capsule != null && capsule.enabled;
        if (wasEnabled)
            capsule.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (wasEnabled)
            capsule.enabled = true;
        grounded = false;
        hasGroundContact = false;
        groundNormal = Vector3.up;
        Physics.SyncTransforms();
        ProbeGround();
    }

    private void CopyLegacySettings()
    {
        capsuleHeight = legacyController.height;
        capsuleRadius = legacyController.radius;
        capsuleCenter = legacyController.center;
        maximumWalkableSlope = legacyController.slopeLimit;
        stepHeight = legacyController.stepOffset;
        skinWidth = Mathf.Max(.001f, legacyController.skinWidth);
    }

    private void ApplyShape()
    {
        if (capsule == null)
            return;
        capsule.height = Mathf.Max(capsuleHeight, capsuleRadius * 2f);
        capsule.radius = capsuleRadius;
        capsule.center = capsuleCenter;
        capsule.direction = 1;
    }

    private bool TryFindNearestHit(Vector3 direction, float distance, out RaycastHit nearest)
    {
        GetWorldCapsule(out Vector3 bottom, out Vector3 top, out float worldRadius);
        int count = Physics.CapsuleCastNonAlloc(bottom, top, worldRadius,
            direction, hitBuffer, distance, collisionLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        nearest = default;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = hitBuffer[i];
            if (candidate.collider == null || IsIgnoredForMovement(candidate.collider) ||
                candidate.distance >= nearestDistance)
                continue;

            nearestDistance = candidate.distance;
            nearest = candidate;
            found = true;
        }

        return found;
    }

    private void ResolveOverlaps()
    {
        for (int iteration = 0; iteration < maxDepenetrationIterations; iteration++)
        {
            GetWorldCapsule(out Vector3 bottom, out Vector3 top, out float worldRadius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, worldRadius,
                overlapBuffer, collisionLayers, QueryTriggerInteraction.Ignore);
            bool moved = false;

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null || IsIgnoredForMovement(other))
                    continue;

                if (!Physics.ComputePenetration(capsule, transform.position,
                    transform.rotation, other, other.transform.position,
                    other.transform.rotation, out Vector3 direction,
                    out float distance))
                    continue;

                transform.position += direction * (distance + skinWidth * .25f);
                moved = true;
            }

            if (!moved)
                break;
        }
    }

    private bool TryStep(ref Vector3 remaining)
    {
        Vector3 originalPosition = transform.position;
        if (TryFindNearestHit(Vector3.up, stepHeight, out RaycastHit ceiling) &&
            ceiling.distance < stepHeight - skinWidth)
            return false;

        transform.position += Vector3.up * stepHeight;
        Vector3 horizontal = Vector3.ProjectOnPlane(remaining, Vector3.up);
        if (horizontal.sqrMagnitude > .0001f &&
            TryFindNearestHit(horizontal.normalized,
                horizontal.magnitude + skinWidth, out RaycastHit obstacle) &&
            obstacle.distance < horizontal.magnitude - skinWidth)
        {
            transform.position = originalPosition;
            return false;
        }

        transform.position += horizontal;
        float downDistance = stepHeight + groundProbeDistance;
        if (!TryFindNearestHit(Vector3.down, downDistance, out RaycastHit floor) ||
            floor.normal.y <= .0001f ||
            Vector3.Angle(floor.normal, Vector3.up) > slopeLimit)
        {
            transform.position = originalPosition;
            return false;
        }

        transform.position += Vector3.down *
            Mathf.Max(0f, floor.distance - skinWidth);
        groundNormal = floor.normal.normalized;
        grounded = true;
        hasGroundContact = true;
        lastCollisionFlags |= CollisionFlags.Below;
        remaining = Vector3.zero;
        return true;
    }

    private bool IsLowStepContact(RaycastHit hit)
    {
        GetWorldCapsule(out Vector3 bottom, out _, out _);
        float contactHeight = hit.point.y - bottom.y;
        return contactHeight <= stepHeight + skinWidth;
    }

    private void ProbeGround(bool allowSnap = false)
    {
        bool hadSweepContact = hasGroundContact;
        Vector3 sweepNormal = groundNormal;
        GetWorldCapsule(out Vector3 bottom, out Vector3 top, out float worldRadius);
        float probeDistance = groundProbeDistance + groundSnapDistance;
        int count = Physics.CapsuleCastNonAlloc(bottom, top, worldRadius,
            Vector3.down, hitBuffer, probeDistance, collisionLayers,
            QueryTriggerInteraction.Ignore);
        if (count == 0)
        {
            hasGroundContact = hadSweepContact;
            groundNormal = hadSweepContact ? sweepNormal : Vector3.up;
            grounded = hadSweepContact &&
                Vector3.Angle(groundNormal, Vector3.up) <= slopeLimit;
            return;
        }

        float nearestDistance = float.MaxValue;
        Vector3 nearestNormal = Vector3.up;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (hit.collider == null || IsIgnoredForMovement(hit.collider) ||
                hit.normal.y <= .0001f || hit.distance >= nearestDistance)
                continue;
            nearestDistance = hit.distance;
            nearestNormal = hit.normal.normalized;
        }

        if (hadSweepContact &&
            Vector3.Angle(sweepNormal, Vector3.up) > slopeLimit)
        {
            hasGroundContact = true;
            grounded = false;
            groundNormal = sweepNormal;
            return;
        }

        hasGroundContact = nearestDistance < float.MaxValue;
        groundNormal = hasGroundContact ? nearestNormal : Vector3.up;
        grounded = hasGroundContact &&
            Vector3.Angle(groundNormal, Vector3.up) <= slopeLimit;
        float snapAmount = nearestDistance - skinWidth;
        if (grounded && allowSnap && groundSnapDistance > 0f &&
            snapAmount > .002f &&
            nearestDistance <= groundSnapDistance + skinWidth)
        {
            transform.position += Vector3.down * snapAmount;
            lastCollisionFlags |= CollisionFlags.Below;
        }
    }

    private void GetWorldCapsule(out Vector3 bottom, out Vector3 top, out float worldRadius)
    {
        Vector3 scale = transform.lossyScale;
        float verticalScale = Mathf.Abs(scale.y);
        float horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        worldRadius = Mathf.Max(.001f, capsuleRadius * horizontalScale);
        float worldHeight = Mathf.Max(capsuleHeight * verticalScale,
            worldRadius * 2f);
        Vector3 worldCenter = transform.TransformPoint(capsuleCenter);
        float halfSegment = Mathf.Max(0f, worldHeight * .5f - worldRadius);
        Vector3 up = transform.up.sqrMagnitude > .0001f
            ? transform.up.normalized : Vector3.up;
        bottom = worldCenter - up * halfSegment;
        top = worldCenter + up * halfSegment;
    }

    private bool IsIgnoredForMovement(Collider other)
    {
        if (other == capsule || other.transform == transform ||
            other.transform.IsChildOf(transform) ||
            other.GetComponentInParent<CombatVisualParticle>() != null)
        {
            return true;
        }

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy == null)
            return false;

        // Inimigos terrestres continuam sendo separados pelo sistema
        // centralizado. O FlyingPuppet usa a colisao do proprio motor do
        // jogador enquanto voa, exceto durante o ataque de teleporte.
        FlyingPuppet flyingPuppet = enemy.GetComponent<FlyingPuppet>();
        return flyingPuppet == null || flyingPuppet.IgnoresPlayerCollision;
    }

    private static CollisionFlags GetCollisionFlag(Vector3 normal)
    {
        if (normal.y > .001f)
            return CollisionFlags.Below;
        if (normal.y < -.5f)
            return CollisionFlags.Above;
        return CollisionFlags.Sides;
    }
}

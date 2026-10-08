using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    [Header("Target")]
    public Transform target;
    [Tooltip("Posicao da camera em relacao ao ponto que ela acompanha.")]
    public Vector3 offset = new Vector3(0f, 7f, -8f);
    [Tooltip("Desloca o ponto para o qual a camera olha. Um pouco acima do player mostra mais do chao.")]
    public Vector3 lookAtOffset = new Vector3(0f, 0.35f, 0f);

    [Header("Follow Smoothing")]
    [Min(0.01f)] public float followSmoothTime = 0.07f;
    [Tooltip("Area horizontal em que o player pode se mover antes de arrastar a camera.")]
    [Min(0f)] public float deadZoneHorizontal = 0f;
    [Min(0f)] public float deadZoneForward = 0f;

    [Header("Look Ahead")]
    [Min(0f)] public float lookAheadDistance = 2f;
    [Min(0.01f)] public float lookAheadSmoothTime = 0.5f;

    [Header("Combat Framing")]
    [Tooltip("Multiplica a distancia da camera durante combate. 1.2 equivale a 20% mais distante.")]
    [Min(1f)] public float combatDistanceMultiplier = 1.5f;
    [Min(0.01f)] public float combatFramingSmoothTime = 1.5f;

    [Header("Scene Collision")]
    public LayerMask collisionLayers = ~0;
    [Min(0.01f)] public float collisionRadius = 0.2f;
    [Min(0f)] public float collisionPadding = 0.1f;
    [Min(0f)] public float minimumDistanceFromFocus = 0.8f;

    [Header("Impact Shake")]
    public float impactShakeDistance = 0.3f;
    public float impactShakeDuration = 0.08f;

    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private Vector3 deadZoneAnchor;
    private Vector3 smoothedFocus;
    private Vector3 followVelocity;
    private Vector3 lookAheadOffset;
    private Vector3 lookAheadVelocity;
    private float combatFraming;
    private float combatFramingVelocity;
    private Vector3 shakeDirection;
    private float shakeTimer;
    private bool isInitialized;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (target == null)
            return;

        playerMovement = target.GetComponent<PlayerMovement>();
        playerCombat = target.GetComponent<PlayerCombat>();
        deadZoneAnchor = target.position;
        smoothedFocus = target.position + lookAtOffset;
        isInitialized = true;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        EnsureInitialized();
        UpdateDeadZoneAnchor();
        UpdateLookAhead();
        UpdateCombatFraming();

        Vector3 desiredFocus = deadZoneAnchor + lookAtOffset + lookAheadOffset;
        smoothedFocus = Vector3.SmoothDamp(
            smoothedFocus,
            desiredFocus,
            ref followVelocity,
            followSmoothTime
        );

        Vector3 desiredPosition = ResolveCameraCollision(
            smoothedFocus,
            smoothedFocus + offset * Mathf.Lerp(1f, combatDistanceMultiplier, combatFraming)
        );

        transform.position = desiredPosition + GetShakeOffset();
        transform.LookAt(smoothedFocus);
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

        playerMovement = target.GetComponent<PlayerMovement>();
        playerCombat = target.GetComponent<PlayerCombat>();
        deadZoneAnchor = target.position;
        smoothedFocus = target.position + lookAtOffset;
        isInitialized = true;
    }

    private void UpdateDeadZoneAnchor()
    {
        Vector3 cameraRight = transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 cameraForward = transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 difference = target.position - deadZoneAnchor;
        float horizontalDifference = Vector3.Dot(difference, cameraRight);
        float forwardDifference = Vector3.Dot(difference, cameraForward);

        if (Mathf.Abs(horizontalDifference) > deadZoneHorizontal)
        {
            float excess = horizontalDifference - Mathf.Sign(horizontalDifference) * deadZoneHorizontal;
            deadZoneAnchor += cameraRight * excess;
        }

        if (Mathf.Abs(forwardDifference) > deadZoneForward)
        {
            float excess = forwardDifference - Mathf.Sign(forwardDifference) * deadZoneForward;
            deadZoneAnchor += cameraForward * excess;
        }

        // Saltos e quedas devem continuar no enquadramento, sem zona morta vertical.
        deadZoneAnchor.y = target.position.y;
    }

    private void UpdateLookAhead()
    {
        bool isOutOfCombat = playerCombat == null || playerCombat.IsOutOfCombat;
        Vector3 movementDirection = isOutOfCombat && playerMovement != null
            ? playerMovement.DesiredDirection
            : Vector3.zero;

        Vector3 targetLookAhead = movementDirection.sqrMagnitude > 0.0001f
            ? movementDirection.normalized * lookAheadDistance
            : Vector3.zero;

        lookAheadOffset = Vector3.SmoothDamp(
            lookAheadOffset,
            targetLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );
    }

    private void UpdateCombatFraming()
    {
        bool isInCombat = playerCombat != null && !playerCombat.IsOutOfCombat;
        float targetFraming = isInCombat ? 1f : 0f;
        combatFraming = Mathf.SmoothDamp(
            combatFraming,
            targetFraming,
            ref combatFramingVelocity,
            combatFramingSmoothTime
        );

    }

    private Vector3 ResolveCameraCollision(Vector3 focus, Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - focus;
        float distance = direction.magnitude;
        if (distance <= 0.0001f)
            return desiredPosition;

        direction /= distance;
        RaycastHit[] hits = Physics.SphereCastAll(
            focus,
            collisionRadius,
            direction,
            distance,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = distance;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(target) ||
                target.IsChildOf(hit.collider.transform) ||
                hit.collider.GetComponentInParent<EnemyHealth>() != null)
            {
                continue;
            }

            closestDistance = Mathf.Min(closestDistance, hit.distance);
        }

        if (closestDistance >= distance)
            return desiredPosition;

        float safeDistance = Mathf.Max(
            minimumDistanceFromFocus,
            closestDistance - collisionPadding
        );

        return focus + direction * safeDistance;
    }

    private Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0f || impactShakeDuration <= 0f)
            return Vector3.zero;

        float shakeStrength = shakeTimer / impactShakeDuration;
        shakeTimer = Mathf.Max(0f, shakeTimer - Time.unscaledDeltaTime);
        return shakeDirection * impactShakeDistance * shakeStrength;
    }

    public void ImpactShake(Vector3 attackDirection)
    {
        attackDirection.y = 0f;
        if (attackDirection.sqrMagnitude <= 0.0001f)
            return;

        shakeDirection = -attackDirection.normalized;
        shakeTimer = impactShakeDuration;
    }
}

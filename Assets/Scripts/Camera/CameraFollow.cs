using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    [Header("Target")]
    [Tooltip("Transform do player que a camera acompanha.")]
    public Transform target;
    [Tooltip("Posicao da camera em relacao ao ponto que ela acompanha.")]
    public Vector3 offset = new Vector3(0f, 7f, -8f);
    [Tooltip("Desloca o ponto para o qual a camera olha. Um pouco acima do player mostra mais do chao.")]
    public Vector3 lookAtOffset = new Vector3(0f, 0.35f, 0f);

    [Header("Mouse Rotation")]
    [Tooltip("Graus de rotacao horizontal por pixel de movimento do mouse.")]
    [Min(0f)] public float horizontalMouseSensitivity = 0.05f;
    [Tooltip("Escape solta o cursor; clique esquerdo o prende novamente.")]
    [SerializeField] private bool lockCursorOnStart = true;

    [Header("Follow Smoothing")]
    [Tooltip("Tempo de suavizacao ao seguir o foco. Menor responde mais rapido.")]
    [Min(0.01f)] public float followSmoothTime = 0.1f;
    [Tooltip("Area horizontal em que o player pode se mover antes de arrastar a camera.")]
    [Min(0f)] public float deadZoneHorizontal = 0f;
    [Tooltip("Folga para frente/tras antes de o foco da camera acompanhar o player.")]
    [Min(0f)] public float deadZoneForward = 0f;

    [Header("Look Ahead")]
    [Tooltip("Antecipacao maxima na direcao de movimento, usada apenas fora de combate.")]
    [Min(0f)] public float lookAheadDistance = 0f;
    [Tooltip("Tempo de suavizacao para entrar e sair do Look Ahead.")]
    [Min(0.01f)] public float lookAheadSmoothTime = 0.01f;

    [Header("Combat Framing")]
    [Tooltip("Multiplica a distancia da camera durante combate. 1.2 equivale a 20% mais distante.")]
    [Min(1f)] public float combatDistanceMultiplier = 1.2f;
    [Tooltip("Altura efetiva da camera enquanto o player esta fora de combate.")]
    public float outOfCombatOffsetY = 1f;
    [Tooltip("Altura efetiva da camera enquanto o player esta em combate.")]
    public float combatOffsetY = 3.5f;
    [Tooltip("Tempo de transicao entre o enquadramento de exploracao e combate.")]
    [Min(0.01f)] public float combatFramingSmoothTime = 0.75f;

    [Header("Lock On Framing")]
    [Tooltip("Quanto o foco da mesma camera se desloca do player em direcao ao inimigo travado.")]
    [Range(0f, 1f)] public float lockOnFocusWeight = .22f;

    [Header("Scene Collision")]
    [Tooltip("Camadas do cenario que bloqueiam a camera.")]
    public LayerMask collisionLayers = ~0;
    [Tooltip("Raio da esfera usada para detectar colisao da camera.")]
    [Min(0.01f)] public float collisionRadius = 0.2f;
    [Tooltip("Folga deixada antes do obstaculo detectado.")]
    [Min(0f)] public float collisionPadding = 0.1f;
    [Tooltip("Distancia minima entre o foco e a camera ao colidir com o cenario.")]
    [Min(0f)] public float minimumDistanceFromFocus = 0.8f;

    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private PlayerLockOn playerLockOn;
    private Vector3 deadZoneAnchor;
    private Vector3 smoothedFocus;
    private Vector3 followVelocity;
    private Vector3 lookAheadOffset;
    private Vector3 lookAheadVelocity;
    private float combatFraming;
    private float combatFramingVelocity;
    private float horizontalYaw;
    private Vector3 shakeDirection;
    private float shakeTimer;
    private float shakeDistance;
    private float shakeDuration;
    private AnimationCurve shakeIntensityCurve;
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
        playerLockOn = target.GetComponent<PlayerLockOn>();
        deadZoneAnchor = target.position;
        smoothedFocus = target.position + lookAtOffset;
        isInitialized = true;

        if (lockCursorOnStart)
            LockCursor();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        EnsureInitialized();
        UpdateMouseRotation();
        UpdateDeadZoneAnchor();
        UpdateLookAhead();
        UpdateCombatFraming();

        Vector3 playerFocus = deadZoneAnchor + lookAtOffset +
            lookAheadOffset;
        Vector3 desiredFocus = GetLockOnFocus(playerFocus);
        smoothedFocus = Vector3.SmoothDamp(
            smoothedFocus,
            desiredFocus,
            ref followVelocity,
            followSmoothTime
        );

        Vector3 desiredPosition = ResolveCameraCollision(
            smoothedFocus,
            smoothedFocus + GetRotatedOffset() * Mathf.Lerp(
                1f,
                combatDistanceMultiplier,
                combatFraming
            )
        );

        // Shake e um desvio temporario do Look At. A posicao calculada por
        // Follow, colisao e Offset permanece intacta.
        Vector3 shakenLookAt = smoothedFocus + GetShakeOffset();
        transform.position = desiredPosition;
        transform.LookAt(shakenLookAt);
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

        playerMovement = target.GetComponent<PlayerMovement>();
        playerCombat = target.GetComponent<PlayerCombat>();
        playerLockOn = target.GetComponent<PlayerLockOn>();
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
        bool isOutOfCombat = (playerCombat == null || playerCombat.IsOutOfCombat) &&
            (playerLockOn == null || !playerLockOn.IsLockedOn);
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

    private void UpdateMouseRotation()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (lockCursorOnStart)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    LockCursor();

                return;
            }
        }

        horizontalYaw += mouse.delta.ReadValue().x *
            horizontalMouseSensitivity;
    }

    private Vector3 GetRotatedOffset()
    {
        Vector3 effectiveOffset = offset;
        effectiveOffset.y = Mathf.Lerp(
            outOfCombatOffsetY,
            combatOffsetY,
            combatFraming
        );
        return Quaternion.Euler(0f, horizontalYaw, 0f) * effectiveOffset;
    }

    private Vector3 GetLockOnFocus(Vector3 playerFocus)
    {
        if (playerLockOn == null)
            playerLockOn = target.GetComponent<PlayerLockOn>();

        return playerLockOn != null && playerLockOn.IsLockedOn
            ? Vector3.Lerp(playerFocus, playerLockOn.LockPoint, lockOnFocusWeight)
            : playerFocus;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
                hit.collider.GetComponentInParent<EnemyBase>() != null ||
                hit.collider.GetComponentInParent<EnemyProjectile>() != null)
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
        if (shakeTimer <= 0f || shakeDuration <= 0f)
            return Vector3.zero;

        float elapsedNormalized = 1f - shakeTimer / shakeDuration;
        float shakeStrength = shakeIntensityCurve != null
            ? Mathf.Max(0f, shakeIntensityCurve.Evaluate(elapsedNormalized))
            : 1f - elapsedNormalized;
        shakeTimer = Mathf.Max(0f, shakeTimer - Time.unscaledDeltaTime);
        return shakeDirection * shakeDistance * shakeStrength;
    }

    /// <summary>Recebe impulsos visuais do CombatImpactSystem.</summary>
    public void RequestImpactShake(
        Vector3 attackDirection,
        float distance,
        float duration,
        AnimationCurve intensityCurve)
    {
        attackDirection.y = 0f;
        if (attackDirection.sqrMagnitude <= 0.0001f ||
            distance <= 0f || duration <= 0f)
            return;

        // O impulso acompanha a direcao fisica do golpe: atacante -> alvo.
        shakeDirection = attackDirection.normalized;
        if (shakeTimer <= 0f)
        {
            shakeDistance = distance;
            shakeDuration = duration;
            shakeTimer = duration;
            shakeIntensityCurve = intensityCurve;
            return;
        }

        shakeDistance = Mathf.Max(shakeDistance, distance);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimer = Mathf.Max(shakeTimer, duration);
        shakeIntensityCurve = intensityCurve;
    }
}

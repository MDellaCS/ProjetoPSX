using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    private enum RecoveryCancelAction
    {
        None,
        Jump,
        Dash
    }

    private const int MaximumJumpCount = 2;

    [Header("Walking")]
    [Min(0f)] public float speed = 7.5f;
    [Min(0f)] public float acceleration = 120f;
    [Min(0f)] public float deceleration = 220f;
    [Min(0f)] public float rotationSpeed = 15f;

    [Header("Attack Momentum")]
    [Tooltip("Quao rapido a velocidade de corrida restante desaparece ao iniciar um ataque.")]
    [Min(0f)] public float attackMomentumDeceleration = 70f;

    [Header("Crouch")]
    [Tooltip("Botao lateral voltar do mouse. Deve permanecer pressionado para agachar.")]
    [Range(0f, 1f)] public float crouchSpeedMultiplier = 0.45f;

    [Header("Slide")]
    [Tooltip("Multiplicador da Speed necessario para iniciar um slide. 1.1 significa 110% da velocidade base.")]
    [Min(0f)] public float slideStartSpeedPercent = 1.1f;
    [Tooltip("Velocidade abaixo da qual o slide termina.")]
    [Min(0f)] public float slideEndSpeed = 0.2f;
    [Tooltip("Quanto o slide perde de velocidade no chao plano.")]
    [Min(0f)] public float slideFriction = 16f;
    [Tooltip("Aceleracao adicional ao deslizar em uma descida.")]
    [Min(0f)] public float slideSlopeAcceleration = 24f;
    [Tooltip("Velocidade, em graus por segundo, para curvar levemente o slide.")]
    [Min(0f)] public float slideSteeringSpeed = 50f;

    [Header("Gravity")]
    public float gravity = -60f;

    [Header("Jump")]
    [Min(0f)] public float jumpInitialSpeed = 13f;
    [Min(0f)] public float jumpHoldAcceleration = 40f;
    [Min(0f)] public float maxJumpHoldDuration = 0.15f;

    [Header("Air Movement")]
    [FormerlySerializedAs("airControlMultiplier")]
    [Tooltip("Aceleracao usada para mudar direcao e velocidade enquanto existe input no ar.")]
    [Min(0f)] public float airControl = 30f;
    [Tooltip("Multiplicador da Speed no chao que limita a velocidade no ar.")]
    [Min(0f)] public float airMaxSpeedMultiplier = 1f;
    [Tooltip("Desaceleracao passiva do momentum no ar quando nao existe input.")]
    [Min(0f)] public float airDrag = 3f;

    [Header("Dash")]
    [FormerlySerializedAs("dashSpeedMultiplier")]
    [Tooltip("Impulso unico do Dash, calculado a partir da Speed base do jogador.")]
    [Min(0f)] public float dashImpulseMultiplier = 1.5f;
    [Min(0f)] public float dashDuration = 0.2f;
    [Min(0f)] public float dashCooldown = 1f;

    [Header("References")]
    public Transform cameraTransform;

    public Vector3 DesiredDirection { get; private set; }
    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public float VerticalSpeed => verticalVelocity;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public float DashCooldownRemaining => dashCooldownTimer;
    public bool IsDashReady =>
        dashCooldownTimer <= 0f &&
        playerState != null &&
        playerState.IsNormal();
    public bool IsCrouching => isCrouching;
    public bool IsAerialDiving => isAerialDiving;
    public bool IsLaunched => isLaunched;
    public bool IsGroundDash =>
        playerState != null &&
        playerState.IsDashing() &&
        dashStartedGrounded;

    private CharacterController controller;
    private PlayerState playerState;
    private PlayerCombat playerCombat;
    private PlayerComboController comboController;
    private PlayerInputBuffer inputBuffer;

    private Vector3 horizontalVelocity;
    private Vector3 knockbackVelocity;
    private Vector3 slideDirection;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    private float verticalVelocity;
    private float jumpHoldTimer;
    private float knockbackTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float aerialLightTimer;
    private float aerialGravityMultiplier = 1f;
    private float aerialDiveFallSpeed;
    private float launchGravityMagnitude;
    private int jumpsUsed;
    private bool hasUsedAirDash;
    private bool dashStartedGrounded;
    private bool isCrouching;
    private bool isAerialDiving;
    private bool isLaunched;
    private RecoveryCancelAction queuedRecoveryCancelAction;

    [SerializeField, HideInInspector]
    private int airMovementSettingsVersion;

    void Start()
    {
        EnsureAirMovementDefaults();
        controller = GetComponent<CharacterController>();
        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
        comboController = PlayerComboController.GetOrAdd(gameObject);
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);

    }

    void OnValidate()
    {
        EnsureAirMovementDefaults();
    }

    private void EnsureAirMovementDefaults()
    {
        if (airMovementSettingsVersion >= 1)
            return;

        // Os valores anteriores usavam Air Drag como aceleracao. Esta
        // migracao aplica valores equivalentes para a nova semantica.
        airControl = 16f;
        airMaxSpeedMultiplier = 0.75f;
        airDrag = 3f;
        airMovementSettingsVersion = 1;
    }

    void Update()
    {
        DesiredDirection = ReadMovementDirection();

        UpdateCrouchState();
        ResetAirActionsWhenGrounded();
        UpdateDashCooldown();
        TryStartSlide();
        TryStartDash();
        TryStartJump();
        UpdateVerticalVelocity();

        horizontalVelocity = CalculateHorizontalVelocity();
        RotateTowardsMovement();

        MoveCharacter();
    }

    private Vector3 ReadMovementDirection()
    {
        if (cameraTransform == null)
            return Vector3.zero;

        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        return (cameraForward * input.y + cameraRight * input.x).normalized;
    }

    private void TryStartDash()
    {
        Keyboard keyboard = Keyboard.current;

        if (playerState.IsAttacking())
        {
            if (dashCooldownTimer <= 0f &&
                (IsGrounded || !hasUsedAirDash) &&
                DesiredDirection.sqrMagnitude > 0.0001f)
            {
                TryQueueRecoveryCancel(
                    PlayerBufferedInput.Dash,
                    RecoveryCancelAction.Dash
                );
            }

            return;
        }

        bool isQueuedDash = queuedRecoveryCancelAction ==
            RecoveryCancelAction.Dash;
        bool canStartDash = playerState.IsNormal() ||
            playerState.IsSliding();

        if ((keyboard == null && !isQueuedDash) ||
            !canStartDash ||
            dashCooldownTimer > 0f ||
            (!IsGrounded && hasUsedAirDash) ||
            DesiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        if (!isQueuedDash &&
            !inputBuffer.TryConsume(PlayerBufferedInput.Dash))
            return;

        queuedRecoveryCancelAction = RecoveryCancelAction.None;
        SetCrouching(false);
        dashStartedGrounded = IsGrounded;
        playerState.ChangeState(PlayerState.State.Dashing);
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (!dashStartedGrounded)
            hasUsedAirDash = true;

        // O Dash soma um impulso ao momentum atual. Consequentemente, dar
        // Dash na mesma direcao acelera o player; no sentido oposto, freia.
        horizontalVelocity += DesiredDirection * GetDashImpulse();
    }

    private void UpdateDashCooldown()
    {
        if (dashCooldownTimer <= 0f)
            return;

        dashCooldownTimer = Mathf.Max(
            0f,
            dashCooldownTimer - Time.deltaTime
        );
    }

    private void TryStartJump()
    {
        Keyboard keyboard = Keyboard.current;

        if (playerState.IsAttacking())
        {
            if (jumpsUsed < MaximumJumpCount)
            {
                TryQueueRecoveryCancel(
                    PlayerBufferedInput.Jump,
                    RecoveryCancelAction.Jump
                );
            }

            return;
        }

        bool isQueuedJump = queuedRecoveryCancelAction ==
            RecoveryCancelAction.Jump;

        bool canStartJump = playerState.IsNormal() || playerState.IsSliding();

        if ((keyboard == null && !isQueuedJump) || !canStartJump ||
            jumpsUsed >= MaximumJumpCount)
        {
            return;
        }

        if (!isQueuedJump &&
            !inputBuffer.TryConsume(PlayerBufferedInput.Jump))
            return;

        queuedRecoveryCancelAction = RecoveryCancelAction.None;
        // Pular e deslizar sao acoes mutuamente exclusivas. O pulo encerra o slide antes de aplicar o impulso vertical.
        EndSlide();
        SetCrouching(false);

        verticalVelocity = jumpInitialSpeed;
        jumpHoldTimer = maxJumpHoldDuration;
        jumpsUsed++;
    }

    private void TryQueueRecoveryCancel(
        PlayerBufferedInput input,
        RecoveryCancelAction action)
    {
        if (!inputBuffer.HasBuffered(input) ||
            playerCombat == null ||
            !playerCombat.QueueRecoveryCancel())
        {
            return;
        }

        inputBuffer.TryConsume(input);
        queuedRecoveryCancelAction = action;
        comboController?.CancelCombo();
    }

    private void ResetAirActionsWhenGrounded()
    {
        if (!IsGrounded)
        {
            // Sair de uma borda nao concede dois pulos extras. Se o player
            // ja esta no ar sem ter pulado, consideramos o primeiro pulo
            // gasto e mantemos somente o pulo aereo disponivel.
            if (jumpsUsed == 0)
                jumpsUsed = 1;

            return;
        }

        // Enquanto o player sobe, o CharacterController pode ainda reportar
        // grounded por um frame. A velocidade vertical evita resetar o pulo
        // que acabou de começar.
        if (verticalVelocity > 0f)
            return;

        jumpsUsed = 0;
        hasUsedAirDash = false;
    }

    private void UpdateCrouchState()
    {
        Mouse mouse = Mouse.current;
        bool wantsToCrouch = mouse != null && mouse.backButton.isPressed;
        bool canCrouch = IsGrounded &&
            (playerState.IsNormal() || playerState.IsSliding());

        SetCrouching(wantsToCrouch && canCrouch);
    }

    private void TryStartSlide()
    {
        bool hasEnoughSpeed = horizontalVelocity.magnitude >= speed * slideStartSpeedPercent;
        bool isMovingDownhill = IsMovingDownhill(horizontalVelocity);

        if (!playerState.IsNormal() ||
            !IsGrounded ||
            !isCrouching ||
            (!hasEnoughSpeed && !isMovingDownhill))
        {
            return;
        }

        slideDirection = Vector3.ProjectOnPlane(
            horizontalVelocity,
            Vector3.up
        ).normalized;
        playerState.ChangeState(PlayerState.State.Sliding);
    }

    private void SetCrouching(bool value)
    {
        if (isCrouching == value)
            return;

        isCrouching = value;
    }

    private void UpdateVerticalVelocity()
    {
        if (isLaunched && controller.isGrounded && verticalVelocity < 0f)
        {
            isLaunched = false;
            verticalVelocity = -2f;
            jumpHoldTimer = 0f;
        }

        if (playerState.IsDashing())
        {
            // Dash iniciado no chao mantem uma pequena pressao para baixo,
            // preservando o contato com o solo. O dash aereo continua sem
            // gravidade, portanto mantem a altura durante toda a duracao.
            verticalVelocity = dashStartedGrounded ? -2f : 0f;
            jumpHoldTimer = 0f;
            return;
        }

        if (isAerialDiving)
        {
            verticalVelocity = -aerialDiveFallSpeed;
            jumpHoldTimer = 0f;
            return;
        }

        if (isLaunched)
        {
            verticalVelocity += -launchGravityMagnitude * Time.deltaTime;
            return;
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            jumpHoldTimer = 0f;
            isLaunched = false;
        }

        ApplyJumpHold();

        UpdateAerialLightTimer();
        verticalVelocity += gravity * aerialGravityMultiplier *
            Time.deltaTime;
    }

    private void UpdateAerialLightTimer()
    {
        if (aerialLightTimer <= 0f)
        {
            aerialGravityMultiplier = 1f;
            return;
        }

        aerialLightTimer = Mathf.Max(
            0f,
            aerialLightTimer - Time.deltaTime
        );

        if (aerialLightTimer <= 0f)
            aerialGravityMultiplier = 1f;
    }

    private void ApplyJumpHold()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            !keyboard.spaceKey.isPressed ||
            jumpHoldTimer <= 0f ||
            verticalVelocity <= 0f)
        {
            return;
        }

        verticalVelocity += jumpHoldAcceleration * Time.deltaTime;
        jumpHoldTimer -= Time.deltaTime;
    }

    private Vector3 CalculateHorizontalVelocity()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            return knockbackVelocity;
        }

        if (playerState.IsStaggered())
        {
            return Vector3.zero;
        }

        if (playerState.IsAttacking())
        {
            // O avanco proprio do ataque e aplicado por PlayerCombat. Aqui desaceleramos o movimento que ja existia para evitar um corte brusco de velocidade ao atacar enquanto corre.
            return Vector3.MoveTowards(
                horizontalVelocity,
                Vector3.zero,
                attackMomentumDeceleration * Time.deltaTime
            );
        }

        if (playerState.IsDashing())
        {
            if (UpdateDash())
            {
                // A duracao controla somente o state/altura do Dash. O
                // impulso foi aplicado uma vez ao iniciar e agora preserva
                // o momentum acumulado.
                return horizontalVelocity;
            }
        }

        if (playerState.IsSliding())
            return CalculateSlideVelocity();

        if (!playerState.IsNormal())
        {
            return Vector3.zero;
        }

        return CalculateWalkVelocity();
    }

    private Vector3 CalculateSlideVelocity()
    {
        if (!IsGrounded || !IsCrouchHeld())
        {
            EndSlide();
            return horizontalVelocity;
        }

        if (DesiredDirection.sqrMagnitude > 0.0001f)
        {
            slideDirection = Vector3.RotateTowards(
                slideDirection,
                DesiredDirection,
                slideSteeringSpeed * Mathf.Deg2Rad * Time.deltaTime,
                0f
            ).normalized;
        }

        float slideSpeed = horizontalVelocity.magnitude;
        slideSpeed = Mathf.Max(
            0f,
            slideSpeed - slideFriction * Time.deltaTime
        );
        slideSpeed += GetDownhillSlideAcceleration(slideDirection) *
            Time.deltaTime;

        if (slideSpeed <= slideEndSpeed)
        {
            EndSlide();
            return horizontalVelocity;
        }

        return GetGroundAlignedVelocity(slideDirection * slideSpeed);
    }

    private float GetDownhillSlideAcceleration(Vector3 direction)
    {
        if (!TryGetGroundNormal(out Vector3 groundNormal))
            return 0f;

        Vector3 downhillDirection = Vector3.ProjectOnPlane(
            Vector3.down,
            groundNormal
        );

        if (downhillDirection.sqrMagnitude <= 0.0001f)
            return 0f;

        return Mathf.Max(
            0f,
            Vector3.Dot(direction, downhillDirection.normalized)
        ) * slideSlopeAcceleration;
    }

    private bool IsMovingDownhill(Vector3 velocity)
    {
        if (velocity.sqrMagnitude <= 0.0001f ||
            !TryGetGroundNormal(out Vector3 groundNormal))
        {
            return false;
        }

        Vector3 downhillDirection = Vector3.ProjectOnPlane(
            Vector3.down,
            groundNormal
        );

        return downhillDirection.sqrMagnitude > 0.0001f &&
            Vector3.Dot(
                velocity.normalized,
                downhillDirection.normalized
            ) > 0.1f;
    }

    private Vector3 GetGroundAlignedVelocity(Vector3 velocity)
    {
        if (velocity.sqrMagnitude <= 0.0001f ||
            !IsGrounded ||
            !TryGetGroundNormal(out Vector3 groundNormal))
        {
            return velocity;
        }

        Vector3 alignedVelocity = Vector3.ProjectOnPlane(
            velocity,
            groundNormal
        );

        if (alignedVelocity.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        return alignedVelocity.normalized * velocity.magnitude;
    }

    private bool TryGetGroundNormal(out Vector3 groundNormal)
    {
        groundNormal = Vector3.up;

        if (controller == null)
            return false;

        int hitCount = Physics.RaycastNonAlloc(
            controller.bounds.center,
            Vector3.down,
            groundHits,
            controller.bounds.extents.y + 0.2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHits[i];

            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            groundNormal = hit.normal;
        }

        return closestDistance < float.MaxValue;
    }

    private bool IsCrouchHeld()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.backButton.isPressed;
    }

    private void EndSlide()
    {
        if (!playerState.IsSliding())
            return;

        playerState.ChangeState(PlayerState.State.Normal);
        SetCrouching(IsGrounded && IsCrouchHeld());
    }

    private Vector3 CalculateWalkVelocity()
    {
        if (!IsGrounded || verticalVelocity > 0f)
        {
            return CalculateAirVelocity();
        }

        float currentSpeed = isCrouching
            ? speed * crouchSpeedMultiplier
            : speed;
        Vector3 targetVelocity = DesiredDirection * currentSpeed;
        float rate = DesiredDirection.sqrMagnitude > 0.0001f
            ? acceleration
            : deceleration;

        Vector3 walkVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            rate * Time.deltaTime
        );

        return GetGroundAlignedVelocity(walkVelocity);
    }

    private Vector3 CalculateAirVelocity()
    {
        if (DesiredDirection.sqrMagnitude > 0.0001f)
        {
            float maximumAirSpeed = speed * airMaxSpeedMultiplier;
            Vector3 targetVelocity =
                DesiredDirection * maximumAirSpeed;

            return Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                airControl * Time.deltaTime
            );
        }

        return Vector3.MoveTowards(
            horizontalVelocity,
            Vector3.zero,
            airDrag * Time.deltaTime
        );
    }

    private bool UpdateDash()
    {
        dashTimer -= Time.deltaTime;

        if (dashTimer > 0f)
            return true;

        playerState.ChangeState(PlayerState.State.Normal);
        dashStartedGrounded = false;

        return false;
    }

    private float GetDashImpulse()
    {
        return speed * dashImpulseMultiplier;
    }

    private void RotateTowardsMovement()
    {
        if (!playerState.IsNormal())
        {
            return;
        }

        if (DesiredDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(DesiredDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void MoveCharacter()
    {
        Vector3 velocity = horizontalVelocity;
        velocity.y += verticalVelocity;

        CollisionFlags collisionFlags = controller.Move(
            velocity * Time.deltaTime
        );

        if (playerState.IsSliding() &&
            (collisionFlags & CollisionFlags.Sides) != 0)
        {
            EndSlide();
        }

        if (isAerialDiving &&
            (collisionFlags & CollisionFlags.Below) != 0)
        {
            StopAerialAttackMovement();
        }
    }

    public void ApplyKnockback(
        Vector3 direction,
        float force,
        float duration
    )
    {
        knockbackVelocity = direction * force;
        knockbackTimer = duration;
    }

    public void ApplyAttackMovement(Vector3 displacement)
    {
        if (controller == null || displacement.sqrMagnitude <= 0.000001f)
            return;

        // PlayerCombat planeja o ataque; PlayerMovement continua sendo o unico
        // responsavel por mover o CharacterController e respeitar colisoes.
        controller.Move(displacement);
    }

    public void BeginAerialLight(
        float liftSpeed,
        float gravityMultiplier,
        float duration)
    {
        if (IsGrounded)
            return;

        isAerialDiving = false;
        isLaunched = false;
        // O leve aereo substitui o momentum vertical do pulo. Primeiro ele
        // interrompe a subida/queda atual e o hold do pulo; depois aplica
        // somente o lift configurado para o ataque.
        verticalVelocity = 0f;
        jumpHoldTimer = 0f;
        verticalVelocity = liftSpeed;
        aerialGravityMultiplier = Mathf.Clamp01(gravityMultiplier);
        aerialLightTimer = Mathf.Max(aerialLightTimer, duration);
    }

    public void BeginAerialDive(float fallSpeed)
    {
        if (IsGrounded)
            return;

        aerialLightTimer = 0f;
        aerialGravityMultiplier = 1f;
        isLaunched = false;
        aerialDiveFallSpeed = fallSpeed;
        isAerialDiving = true;
    }

    public void StopAerialAttackMovement()
    {
        isAerialDiving = false;
        aerialDiveFallSpeed = 0f;
        aerialLightTimer = 0f;
        aerialGravityMultiplier = 1f;
    }

    public void ApplySelfLaunchUp(float initialSpeed, float gravityMagnitude)
    {
        // Uma fonte externa pode colocar o player em uma trajetoria identica a de um inimigo lancado, sem depender de um ataque especifico.
        // Launch Up e uma trajetoria vertical intencional: ele limpa qualquer momentum horizontal anterior antes de aplicar o impulso para cima.
        horizontalVelocity = Vector3.zero;
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        isAerialDiving = false;
        aerialLightTimer = 0f;
        aerialGravityMultiplier = 1f;
        aerialDiveFallSpeed = 0f;
        jumpHoldTimer = 0f;
        verticalVelocity = initialSpeed;
        launchGravityMagnitude = Mathf.Max(0f, gravityMagnitude);
        isLaunched = true;
    }
}

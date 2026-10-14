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
    [Min(0f)] public float speed = 8f;
    [Min(0f)] public float acceleration = 120f;
    [Tooltip("Desaceleracao ao soltar completamente o movimento.")]
    [Min(0f)] public float releaseDeceleration = 90f;
    [Tooltip("Desaceleracao ao segurar a direcao oposta ao movimento atual.")]
    [Min(0f)] public float reverseDeceleration = 220f;
    [Tooltip("Desaceleracao do momentum extra ao voltar para a Speed normal.")]
    [Min(0f)] public float momentumDeceleration = 33f;
    [Min(0f)] public float rotationSpeed = 17f;

    [Header("Attack Momentum")]
    [Tooltip("Quao rapido a velocidade de corrida restante desaparece ao iniciar um ataque.")]
    [Min(0f)] public float attackMomentumDeceleration = 80f;

    [Header("Crouch")]
    [Tooltip("Botao lateral voltar do mouse. Deve permanecer pressionado para agachar.")]
    [Range(0f, 1f)] public float crouchSpeedMultiplier = 0.3f;
    [Tooltip("Multiplicador da altura do CharacterController ao agachar ou deslizar.")]
    [Range(0.2f, 1f)] public float crouchingHeightMultiplier = 0.6f;
    [Tooltip("Largura da capsula agachada como porcentagem do maximo fisicamente possivel para sua altura. 1 = o mais largo permitido.")]
    [Range(0.1f, 1f)] public float crouchingRadiusPercentOfHeight = 0.815f;
    [Tooltip("Velocidade de transicao da colisao entre em pe e agachado.")]
    [Min(0f)] public float colliderResizeSpeed = 15f;
    [Tooltip("Camadas que bloqueiam o retorno para a altura em pe.")]
    public LayerMask standingClearanceMask = Physics.DefaultRaycastLayers;
    [Tooltip("Folga extra acima da cabeca exigida antes de voltar a ficar em pe, em unidades do mundo.")]
    [Min(0f)] public float standingClearancePadding = 0.05f;

    [Header("Slide")]
    [Tooltip("Multiplicador da Speed necessario para iniciar um slide. 1.1 significa 110% da velocidade base.")]
    [Min(0f)] public float slideStartSpeedPercent = 1.1f;
    [Tooltip("Velocidade abaixo da qual o slide termina.")]
    [Min(0f)] public float slideEndSpeed = 0.1f;
    [Tooltip("Quanto o slide perde de velocidade no chao plano.")]
    [Min(0f)] public float slideFriction = 14f;
    [Tooltip("Aceleracao adicional ao deslizar em uma descida.")]
    [Min(0f)] public float slideSlopeAcceleration = 24f;

    [Header("Steering")]
    [FormerlySerializedAs("slideSteeringSpeed")]
    [Tooltip("Velocidade, em graus por segundo, para curvar Slide, Dash e Dash Jump.")]
    [Min(0f)] public float steeringStrength = 40f;

    [Header("Steep Slopes")]
    [Tooltip("Aceleracao que puxa o player para baixo em inclinacoes acima do Slope Limit do CharacterController.")]
    [Min(0f)] public float steepSlopeSlideAcceleration = 30f;
    [Tooltip("Velocidade maxima ao escorregar automaticamente por uma inclinacao muito alta.")]
    [Min(0f)] public float steepSlopeMaximumSlideSpeed = 10f;

    [Header("Gravity")]
    public float gravity = -60f;
    [Tooltip("Velocidade maxima de queda, aplicada a gravidade, Launch Up e Dive.")]
    [Min(2f)] public float terminalFallSpeed = 30f;

    [Header("Jump")]
    [Min(0f)] public float jumpInitialSpeed = 14f;
    [Min(0f)] public float jumpHoldAcceleration = 40f;
    [Min(0f)] public float maxJumpHoldDuration = 0.25f;

    [Header("Air Movement")]
    [FormerlySerializedAs("airControlMultiplier")]
    [Tooltip("Aceleracao usada para mudar direcao e velocidade enquanto existe input no ar.")]
    [Min(0f)] public float airControl = 40f;
    [Tooltip("Multiplicador da Speed no chao que limita a velocidade no ar.")]
    [Min(0f)] public float airMaxSpeedMultiplier = 1f;
    [Tooltip("Desaceleracao passiva do momentum no ar quando nao existe input.")]
    [Min(0f)] public float airDrag = 8f;

    [Header("Dash")]
    [FormerlySerializedAs("dashSpeedMultiplier")]
    [Tooltip("Impulso unico do Dash, calculado a partir da Speed base do jogador.")]
    [Min(0f)] public float dashImpulseMultiplier = 1.6f;
    [Min(0f)] public float dashDuration = 0.33f;
    [Min(0f)] public float dashCooldown = 1.3f;

    [Header("Enemy Body Interaction")]
    [Tooltip("Impulso horizontal que tira o player de cima de um inimigo.")]
    [Min(0f)] public float enemyHeadPushSpeed = 10f;

    [Header("References")]
    public Transform cameraTransform;

    public Vector3 DesiredDirection { get; private set; }
    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public float VerticalSpeed => verticalVelocity;
    public bool IsGrounded => controller != null && controller.isGrounded &&
        !IsStandingOnEnemy;
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
    private float launchGravity;
    private float launchGravityMultiplier = 1f;
    private float launchLowGravityMultiplier = 1f;
    private float launchLowGravityTimer;
    private float launchLowGravityDuration;
    private float enemyGroundContactTimer;
    private bool launchLowGravityPending;
    private int jumpsUsed;
    private bool hasUsedAirDash;
    private bool dashStartedGrounded;
    private bool isCrouching;
    private bool isAerialDiving;
    private bool isLaunched;
    private bool isDashJump;
    private RecoveryCancelAction queuedRecoveryCancelAction;

    private float standingControllerHeight;
    private float standingControllerRadius;
    private Vector3 standingControllerCenter;
    private bool hasCachedStandingControllerShape;

    private bool IsStandingOnEnemy => enemyGroundContactTimer > 0f;

    [SerializeField, HideInInspector]
    private int airMovementSettingsVersion;

    void Start()
    {
        EnsureAirMovementDefaults();
        controller = GetComponent<CharacterController>();
        CacheStandingControllerShape();
        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
        comboController = PlayerComboController.GetOrAdd(gameObject);
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
        PlayerSlideAttack.GetOrAdd(gameObject);

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
        enemyGroundContactTimer = Mathf.Max(
            0f,
            enemyGroundContactTimer - Time.deltaTime
        );

        DesiredDirection = ReadMovementDirection();

        TryCancelGroundDashWithCrouch();
        UpdateCrouchState();
        ResetAirActionsWhenGrounded();
        UpdateDashCooldown();
        TryStartSlide();
        TryStartDash();
        TryStartJump();
        UpdateCharacterControllerShape();
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

        isDashJump = false;
        BreakMomentumIfDashingOpposite();

        // O Dash soma um impulso ao momentum atual. Consequentemente, dar
        // Dash na mesma direcao acelera o player; no sentido oposto, elimina
        // o momentum anterior antes de aplicar o novo impulso.
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

        bool wasDashing = playerState.IsDashing();
        bool canStartJump = (playerState.IsNormal() || playerState.IsSliding() ||
            wasDashing) &&
            !IsStandingOnEnemy;

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

        if (wasDashing)
        {
            // Dash Jump encerra o Dash mais cedo, mas conserva seu impulso
            // inteiro ate o player realizar outra acao relevante.
            playerState.ChangeState(PlayerState.State.Normal);
            dashTimer = 0f;
            dashStartedGrounded = false;
            isDashJump = true;
        }
        else if (!IsGrounded)
        {
            // O pulo aereo comum e uma ferramenta precisa: ele zera a
            // velocidade horizontal, ao contrario do Dash Jump.
            horizontalVelocity = Vector3.zero;
            isDashJump = false;
        }

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
        isDashJump = false;
    }

    private void UpdateCrouchState()
    {
        Mouse mouse = Mouse.current;
        bool wantsToCrouch = mouse != null && mouse.backButton.isPressed;

        bool canCrouch = IsGrounded &&
            (playerState.IsNormal() || playerState.IsSliding());

        SetCrouching(wantsToCrouch && canCrouch);
    }

    private void TryCancelGroundDashWithCrouch()
    {
        if (!IsGroundDash || !IsCrouchHeld())
            return;

        // O cancelamento acontece antes de atualizar crouch/slide no mesmo
        // frame, permitindo converter o impulso do Dash em um Slide.
        playerState.ChangeState(PlayerState.State.Normal);
        dashTimer = 0f;
        dashStartedGrounded = false;
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
        if (!value && !CanStandUp())
            return;

        if (isCrouching == value)
            return;

        isCrouching = value;
    }

    private void CacheStandingControllerShape()
    {
        if (controller == null || hasCachedStandingControllerShape)
            return;

        standingControllerHeight = controller.height;
        standingControllerRadius = controller.radius;
        standingControllerCenter = controller.center;
        hasCachedStandingControllerShape = true;
    }

    private void UpdateCharacterControllerShape()
    {
        CacheStandingControllerShape();
        if (!hasCachedStandingControllerShape)
            return;

        bool lowProfile = isCrouching || playerState.IsSliding();
        float targetHeight = lowProfile
            ? standingControllerHeight * crouchingHeightMultiplier
            : standingControllerHeight;
        // CharacterController exige raio <= metade da altura. Em vez de
        // multiplicar o raio original e bater nesse limite silenciosamente,
        // configuramos diretamente uma porcentagem do maximo permitido.
        float maximumRadius = targetHeight * .5f;
        float targetRadius = lowProfile
            ? maximumRadius * crouchingRadiusPercentOfHeight
            : standingControllerRadius;

        // Mantem a base da capsula no mesmo Y local. Com o setup atual
        // (Height 2.5, Center Y 1.25), ela continua exatamente no chao.
        // O Center nao pode ter uma interpolacao independente: para cada
        // mudanca de Height ele precisa acompanhar exatamente metade dela,
        // ou a capsula flutua/perde o chao durante a transicao.
        float standingBottom = standingControllerCenter.y -
            standingControllerHeight * .5f;

        float resizeDelta = colliderResizeSpeed * Time.deltaTime;
        float resizedHeight = Mathf.MoveTowards(
            controller.height,
            targetHeight,
            resizeDelta
        );
        controller.height = resizedHeight;
        controller.radius = Mathf.MoveTowards(
            controller.radius,
            targetRadius,
            resizeDelta
        );
        controller.center = new Vector3(
            standingControllerCenter.x,
            standingBottom + resizedHeight * .5f,
            standingControllerCenter.z
        );
    }

    private bool CanStandUp()
    {
        CacheStandingControllerShape();
        if (!hasCachedStandingControllerShape || controller == null)
            return true;

        float radius = Mathf.Min(
            standingControllerRadius,
            standingControllerHeight * .5f
        );
        float halfStraightSection = Mathf.Max(
            0f,
            standingControllerHeight * .5f - radius
        );
        Vector3 worldCenter = transform.TransformPoint(standingControllerCenter);
        Vector3 worldUp = transform.up;
        Vector3 bottom = worldCenter - worldUp * halfStraightSection;
        // A margem e aplicada somente acima da cabeca. Isso exige alguns
        // centimetros extras livres sem tornar paredes laterais mais largas.
        Vector3 top = worldCenter + worldUp *
            (halfStraightSection + standingClearancePadding);

        Collider[] overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            standingClearanceMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider overlap in overlaps)
        {
            if (overlap == null || overlap == controller ||
                overlap.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void UpdateVerticalVelocity()
    {
        if (isLaunched && controller.isGrounded && verticalVelocity < 0f)
        {
            isLaunched = false;
            verticalVelocity = -2f;
            jumpHoldTimer = 0f;
            ResetLaunchLowGravity();
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
            verticalVelocity = Mathf.Max(
                -aerialDiveFallSpeed,
                -terminalFallSpeed
            );
            jumpHoldTimer = 0f;
            return;
        }

        if (isLaunched)
        {
            UpdateLaunchLowGravity();
            verticalVelocity += launchGravity * launchGravityMultiplier *
                Time.deltaTime;
            ClampTerminalFallSpeed();
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
        ClampTerminalFallSpeed();
    }

    private void ClampTerminalFallSpeed()
    {
        verticalVelocity = Mathf.Max(
            verticalVelocity,
            -terminalFallSpeed
        );
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

    private void UpdateLaunchLowGravity()
    {
        if (launchLowGravityPending)
        {
            // A sustentacao comeca somente quando a subida terminou.
            if (verticalVelocity > 0f)
                return;

            launchLowGravityPending = false;
            launchLowGravityTimer = launchLowGravityDuration;
            launchGravityMultiplier = launchLowGravityMultiplier;
        }

        if (launchLowGravityTimer <= 0f)
        {
            launchGravityMultiplier = 1f;
            return;
        }

        launchLowGravityTimer = Mathf.Max(
            0f,
            launchLowGravityTimer - Time.deltaTime
        );

        if (launchLowGravityTimer <= 0f)
            launchGravityMultiplier = 1f;
    }

    private void ResetLaunchLowGravity()
    {
        launchLowGravityTimer = 0f;
        launchLowGravityDuration = 0f;
        launchLowGravityPending = false;
        launchGravityMultiplier = 1f;
        launchLowGravityMultiplier = 1f;
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
            isDashJump = false;
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
                return SteerHorizontalVelocity(horizontalVelocity);
            }
        }

        if (isDashJump)
            return SteerHorizontalVelocity(horizontalVelocity);

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
                steeringStrength * Mathf.Deg2Rad * Time.deltaTime,
                0f
            ).normalized;
        }

        // Em uma inclinacao acima do limite, o Slide manual continua
        // permitido, mas a direcao e obrigatoriamente a da descida. Isso
        // impede subir a rampa usando momentum e garante a descida.
        if (TryGetSteepSlopeDownhillDirection(
            out Vector3 steepDownhillDirection))
        {
            slideDirection = steepDownhillDirection;
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

    private Vector3 SteerHorizontalVelocity(Vector3 velocity)
    {
        if (velocity.sqrMagnitude <= 0.0001f ||
            DesiredDirection.sqrMagnitude <= 0.0001f)
        {
            return velocity;
        }

        float speedMagnitude = velocity.magnitude;
        Vector3 steeredDirection = Vector3.RotateTowards(
            velocity.normalized,
            DesiredDirection.normalized,
            steeringStrength * Mathf.Deg2Rad * Time.deltaTime,
            0f
        );

        return steeredDirection.normalized * speedMagnitude;
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

        // O Slide manual tem prioridade em CalculateHorizontalVelocity. Aqui
        // tratamos somente o movimento normal: acima do Slope Limit, o player
        // nao consegue caminhar/subir e escorrega na direcao da descida.
        if (TryGetSteepSlopeDownhillDirection(out Vector3 downhillDirection))
            return CalculateSteepSlopeVelocity(downhillDirection);

        float currentSpeed = isCrouching
            ? speed * crouchSpeedMultiplier
            : speed;
        Vector3 targetVelocity = DesiredDirection * currentSpeed;
        float rate = GetGroundMovementRate(targetVelocity);

        Vector3 walkVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            rate * Time.deltaTime
        );

        return GetGroundAlignedVelocity(walkVelocity);
    }

    private float GetGroundMovementRate(Vector3 targetVelocity)
    {
        if (targetVelocity.sqrMagnitude <= 0.0001f)
            return releaseDeceleration;

        if (horizontalVelocity.sqrMagnitude <= 0.0001f)
            return acceleration;

        float directionDot = Vector3.Dot(
            horizontalVelocity.normalized,
            targetVelocity.normalized
        );

        if (directionDot < 0f)
            return reverseDeceleration;

        // Continua na mesma direcao, mas acima da Speed normal: perde apenas
        // o momentum excedente, sem parecer uma freada brusca.
        if (horizontalVelocity.magnitude > targetVelocity.magnitude)
            return momentumDeceleration;

        return acceleration;
    }

    private void BreakMomentumIfDashingOpposite()
    {
        if (horizontalVelocity.sqrMagnitude <= 0.0001f ||
            DesiredDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        if (Vector3.Dot(horizontalVelocity.normalized,
            DesiredDirection.normalized) < 0f)
        {
            horizontalVelocity = Vector3.zero;
        }
    }

    private Vector3 CalculateSteepSlopeVelocity(Vector3 downhillDirection)
    {
        Vector3 targetVelocity = downhillDirection *
            steepSlopeMaximumSlideSpeed;

        return Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            steepSlopeSlideAcceleration * Time.deltaTime
        );
    }

    private bool TryGetSteepSlopeDownhillDirection(
        out Vector3 downhillDirection)
    {
        downhillDirection = Vector3.zero;

        if (controller == null || !TryGetGroundNormal(out Vector3 groundNormal))
            return false;

        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        if (slopeAngle <= controller.slopeLimit)
            return false;

        downhillDirection = Vector3.ProjectOnPlane(
            Vector3.down,
            groundNormal
        );

        if (downhillDirection.sqrMagnitude <= 0.0001f)
            return false;

        downhillDirection.Normalize();
        return true;
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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null)
            return;

        BaseEnemy enemy = hit.collider.GetComponentInParent<BaseEnemy>();
        if (enemy == null || enemy.IsDead || !IsLandingOnEnemyTop(enemy))
            return;

        // Um inimigo nunca conta como chao: nao reseta pulos e nao permite
        // iniciar um novo pulo. O impulso lateral tambem tira o player do
        // topo, em vez de deixa-lo equilibrado na cabeca do inimigo.
        enemyGroundContactTimer = .1f;
        verticalVelocity = Mathf.Min(verticalVelocity, -2f);

        Vector3 pushDirection = transform.position - enemy.transform.position;
        pushDirection.y = 0f;

        if (pushDirection.sqrMagnitude <= .0001f)
            pushDirection = transform.right;

        horizontalVelocity = pushDirection.normalized * enemyHeadPushSpeed;
    }

    private bool IsLandingOnEnemyTop(BaseEnemy enemy)
    {
        // Colisoes laterais e por baixo tambem chamam
        // OnControllerColliderHit. So tratamos o inimigo como "chao invalido"
        // quando a base do player chegou ao topo dele enquanto estava caindo.
        if (controller == null || verticalVelocity > 0f)
            return false;

        CharacterController enemyController =
            enemy.GetComponent<CharacterController>();
        if (enemyController == null)
            return false;

        const float topContactTolerance = .15f;
        float playerBottom = controller.bounds.min.y;
        float enemyTop = enemyController.bounds.max.y;

        return playerBottom >= enemyTop - topContactTolerance;
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
        ResetLaunchLowGravity();
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
        ResetLaunchLowGravity();
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

    public float GetMaximumJumpLaunchSpeed()
    {
        float targetHeight = GetMaximumJumpHeight();
        if (targetHeight <= 0f)
            return 0f;

        float gravityMagnitude = Mathf.Max(.001f, Mathf.Abs(gravity));
        return Mathf.Sqrt(2f * gravityMagnitude * targetHeight);
    }

    private float GetMaximumJumpHeight()
    {
        if (jumpInitialSpeed <= 0f)
            return 0f;

        float holdDuration = Mathf.Max(0f, maxJumpHoldDuration);
        float accelerationDuringHold = gravity + jumpHoldAcceleration;

        // O hold so fornece impulso enquanto o player ainda sobe. Se a
        // gravidade vencer antes do limite, usamos apenas esse trecho real.
        if (accelerationDuringHold < 0f && jumpInitialSpeed > 0f)
        {
            holdDuration = Mathf.Min(
                holdDuration,
                jumpInitialSpeed / -accelerationDuringHold
            );
        }

        float speedAfterHold = jumpInitialSpeed +
            accelerationDuringHold * holdDuration;
        float heightDuringHold = jumpInitialSpeed * holdDuration +
            .5f * accelerationDuringHold * holdDuration * holdDuration;
        float gravityMagnitude = Mathf.Max(.001f, Mathf.Abs(gravity));

        float heightAfterHold = speedAfterHold > 0f
            ? speedAfterHold * speedAfterHold / (2f * gravityMagnitude)
            : 0f;
        return Mathf.Max(0f, heightDuringHold + heightAfterHold);
    }

    public void ApplySelfLaunchUp(float initialSpeed, float gravity,
        float lowGravityMultiplier, float lowGravityDuration)
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
        launchGravity = gravity;
        launchGravityMultiplier = 1f;
        launchLowGravityMultiplier = Mathf.Clamp01(lowGravityMultiplier);
        launchLowGravityTimer = 0f;
        launchLowGravityDuration = Mathf.Max(0f, lowGravityDuration);
        launchLowGravityPending = launchLowGravityDuration > 0f &&
            launchLowGravityMultiplier < 1f;
        isLaunched = true;
    }
}

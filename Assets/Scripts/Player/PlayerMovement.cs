using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1000)]
public class PlayerMovement : MonoBehaviour
{
    private enum RecoveryCancelAction
    {
        None,
        Jump,
        Dash
    }

    [Header("Walking")]
    [Tooltip("Velocidade base maxima ao andar no chao.")]
    [Min(0f)] public float speed = 8f;
    [Tooltip("Rapidez para atingir a velocidade desejada ao comecar a andar.")]
    [Min(0f)] public float acceleration = 120f;
    [Tooltip("Desaceleracao ao soltar completamente o movimento.")]
    [Min(0f)] public float releaseDeceleration = 90f;
    [Tooltip("Desaceleracao ao segurar a direcao oposta ao movimento atual.")]
    [Min(0f)] public float reverseDeceleration = 220f;
    [Tooltip("Desaceleracao do momentum extra ao voltar para a Speed normal.")]
    [Min(0f)] public float momentumDeceleration = 61f;
    [Tooltip("Rapidez com que o visual gira para a direcao de movimento normal.")]
    [Min(0f)] public float rotationSpeed = 17f;

    [Header("Attack Momentum")]
    [Tooltip("Quao rapido a velocidade de corrida restante desaparece ao iniciar um ataque.")]
    [Min(0f)] public float attackMomentumDeceleration = 80f;

    [Header("Crouch")]
    [Tooltip("Botao lateral voltar do mouse. Deve permanecer pressionado para agachar.")]
    [Range(0f, 1f)] public float crouchSpeedMultiplier = 0.5f;
    [Tooltip("Multiplicador da altura da capsula de colisao ao agachar ou deslizar.")]
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
    [Min(0f)] public float slideEndSpeed = 0f;
    [Tooltip("Quanto o slide perde de velocidade no chao plano.")]
    [Min(0f)] public float slideFriction = 16f;
    [Tooltip("Aceleracao adicional ao deslizar em uma descida.")]
    [Min(0f)] public float slideSlopeAcceleration = 24f;

    [Header("Steering")]
    [Tooltip("Velocidade, em graus por segundo, para curvar Slide, Dash e Dash Jump.")]
    [Min(0f)] public float steeringStrength = 40f;

    [Header("Steep Slopes")]
    [Tooltip("Aceleracao que puxa o player para baixo em inclinacoes acima do limite configurado no PlayerCharacterController.")]
    [Min(0f)] public float steepSlopeSlideAcceleration = 30f;
    [Tooltip("Velocidade maxima ao escorregar automaticamente por uma inclinacao muito alta.")]
    [Min(0f)] public float steepSlopeMaximumSlideSpeed = 10f;

    [Header("Gravity")]
    [Tooltip("Aceleracao vertical aplicada ao player. Deve ser negativa para puxar para baixo.")]
    public float gravity = -60f;
    [Tooltip("Velocidade maxima de queda, aplicada a gravidade e Launch Up.")]
    [Min(2f)] public float terminalFallSpeed = 30f;

    [Header("Jump")]
    [Tooltip("Velocidade vertical adicional por segundo enquanto o botao de pulo permanece pressionado.")]
    [Min(0f)] public float jumpHoldAcceleration = 40f;
    [Tooltip("Tempo maximo em que segurar pulo aumenta sua altura.")]
    [Min(0f)] public float maxJumpHoldDuration = 0.25f;

    [Header("Dash")]
    [Tooltip("Duração global do estado de dash. Controla steering, dash-jump, cancelamentos e a troca para a animação seguinte; não altera o impulso inicial.")]
    [Min(0f)] public float dashDuration = .3f;

    [Header("Air Movement")]
    [Tooltip("Aceleracao usada para mudar direcao e velocidade enquanto existe input no ar.")]
    [Min(0f)] public float airControl = 40f;
    [Tooltip("Multiplicador da Speed no chao que limita a velocidade no ar.")]
    [Min(0f)] public float airMaxSpeedMultiplier = 1f;
    [Tooltip("Desaceleracao passiva do momentum no ar quando nao existe input.")]
    [Min(0f)] public float airDrag = 8f;

    [Header("Enemy Body Interaction")]
    [Tooltip("Impulso horizontal que tira o player de cima de um inimigo.")]
    [Min(0f)] public float enemyHeadSeparationSpeed = 10f;

    [Header("References")]
    [Tooltip("Camera usada para converter o input de movimento em direcoes do mundo.")]
    public Transform cameraTransform;

    public Vector3 DesiredDirection { get; private set; }
    public float HorizontalSpeed => horizontalVelocity.magnitude;
    public float VerticalSpeed => verticalVelocity;
    public bool IsGrounded => controller != null && controller.isGrounded &&
        !IsStandingOnEnemy;
    public float DashCooldownRemaining => dashCooldownTimer;
    public bool IsDashReady =>
        CanDash &&
        dashCooldownTimer <= 0f &&
        playerState != null &&
        playerState.IsNormal() &&
        (IsGrounded || (CanAirDash && !hasUsedAirDash));
    public bool IsCrouching => isCrouching;
    public bool IsLaunched => isLaunched;
    public bool IsLaunchedForwards => launchForwardsVelocity.sqrMagnitude > .0001f;
    public bool IsAerialDiving => isAerialDiving;
    public bool IsGroundDash =>
        playerState != null &&
        playerState.IsDashing() &&
        dashStartedGrounded;
    /// <summary>Motor de colisao exposto para a separacao central de entidades terrestres.</summary>
    public PlayerCharacterController EntityCharacterController => controller;

    private PlayerCharacterController controller;
    private PlayerState playerState;
    private PlayerAttackController attackController;
    private PlayerHeat playerHeat;
    private PlayerMovesetController movesetController;
    private PlayerInputBuffer inputBuffer;

    private Vector3 horizontalVelocity;
    private Vector3 launchForwardsVelocity;
    private Vector3 slideDirection;

    private float verticalVelocity;
    private float jumpHoldTimer;
    private float launchForwardsDeceleration;
    private float dashTimer;
    private float dashCooldownTimer;
    private float aerialLightTimer;
    private float aerialGravityMultiplier = 1f;
    private float aerialDiveFallSpeed;
    private float launchGravity;
    private float enemyGroundContactTimer;
    private int jumpsUsed;
    private bool hasUsedAirDash;
    private bool dashStartedGrounded;
    private bool isCrouching;
    private bool isLaunched;
    private bool isAerialDiving;
    private bool isDashJump;
    private bool wasAirborne;
    private RecoveryCancelAction queuedRecoveryCancelAction;

    private float standingControllerHeight;
    private float standingControllerRadius;
    private Vector3 standingControllerCenter;
    private bool hasCachedStandingControllerShape;

    private bool IsStandingOnEnemy => enemyGroundContactTimer > 0f;
    private bool CanAirJump => movesetController == null ||
        movesetController.Moveset == null ||
        movesetController.Moveset.allowAirJump;
    private bool CanAirDash => movesetController == null ||
        movesetController.Moveset == null ||
        movesetController.Moveset.allowAirDash;
    private bool CanDash => movesetController == null ||
        movesetController.Moveset == null ||
        movesetController.Moveset.allowDash;

    private void Awake()
    {
        controller = PlayerCharacterController.GetOrAdd(gameObject);
        controller.CollisionHit += HandleControllerHit;
    }

    void Start()
    {
        CacheStandingControllerShape();
        playerState = GetComponent<PlayerState>();
        attackController = GetComponent<PlayerAttackController>();
        playerHeat = GetComponent<PlayerHeat>();
        movesetController = GetComponent<PlayerMovesetController>();
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
        PlayerSlideAttack.GetOrAdd(gameObject);
        wasAirborne = !IsGrounded;

    }

    private void OnDestroy()
    {
        if (controller != null)
            controller.CollisionHit -= HandleControllerHit;
    }

    void Update()
    {
        // Este temporizador precisa continuar avancando durante stagger e
        // hitstop. Caso contrario o player pode ficar marcado como apoiado em
        // um inimigo para sempre quando a simulacao for interrompida.
        enemyGroundContactTimer = Mathf.Max(
            0f,
            enemyGroundContactTimer - Time.deltaTime
        );

        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        if (!CanDash && playerState != null && playerState.IsDashing())
        {
            dashTimer = 0f;
            dashStartedGrounded = false;
            playerState.ChangeState(PlayerState.State.Normal);
        }

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

        Vector2 input = GameInput.Move;

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
        if (!CanDash)
        {
            inputBuffer?.TryConsume(PlayerBufferedInput.Dash);
            if (queuedRecoveryCancelAction == RecoveryCancelAction.Dash)
                queuedRecoveryCancelAction = RecoveryCancelAction.None;
            return;
        }

        if (playerState.IsAttacking())
        {
            if (dashCooldownTimer <= 0f &&
                (IsGrounded || (CanAirDash && !hasUsedAirDash)) &&
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

        if (!canStartDash ||
            dashCooldownTimer > 0f ||
            (!IsGrounded && (!CanAirDash || hasUsedAirDash)) ||
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
        dashCooldownTimer = GetDashCooldown();

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
        // Contato com uma rampa acima do limite nao conta como chão nem como
        // oportunidade para consumir o pulo aéreo enquanto o jogador ainda
        // está apoiado nela.
        if (controller != null && controller.HasGroundContact && !IsGrounded)
            return;

        if (playerState.IsAttacking())
        {
            if (jumpsUsed < GetMaximumJumpCount())
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

        if (!canStartJump ||
            jumpsUsed >= GetMaximumJumpCount())
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

        verticalVelocity = GetJumpInitialSpeed();
        jumpHoldTimer = maxJumpHoldDuration;
        jumpsUsed++;
    }

    private void TryQueueRecoveryCancel(
        PlayerBufferedInput input,
        RecoveryCancelAction action)
    {
        if (!inputBuffer.HasBuffered(input) ||
            attackController == null ||
            !attackController.RequestCancel(action == RecoveryCancelAction.Jump
                ? PlayerAttackCancelAction.Jump : PlayerAttackCancelAction.Dash))
        {
            return;
        }

        inputBuffer.TryConsume(input);
        queuedRecoveryCancelAction = action;
        movesetController?.ResetSequence();
    }

    private void ResetAirActionsWhenGrounded()
    {
        if (!IsGrounded)
        {
            wasAirborne = true;
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
        {
            wasAirborne = true;
            return;
        }

        if (wasAirborne)
            movesetController?.ResetAerialSequenceOnLanding();
        wasAirborne = false;

        jumpsUsed = 0;
        hasUsedAirDash = false;
        isDashJump = false;
    }

    private void UpdateCrouchState()
    {
        bool wantsToCrouch = GameInput.CrouchHeld;

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
        bool hasEnoughSpeed = horizontalVelocity.magnitude >=
            GetMovementSpeed() * slideStartSpeedPercent;
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

        if (isLaunched)
        {
            verticalVelocity += launchGravity * Time.deltaTime;
            ClampTerminalFallSpeed();
            return;
        }

        if (isAerialDiving)
        {
            // O Dive controla a queda diretamente ate o CharacterController
            // confirmar o contato com o chao.
            if (controller.isGrounded)
            {
                isAerialDiving = false;
                aerialDiveFallSpeed = 0f;
                verticalVelocity = -2f;
                return;
            }

            verticalVelocity = -aerialDiveFallSpeed;
            jumpHoldTimer = 0f;
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

    private void ApplyJumpHold()
    {
        if (!GameInput.JumpHeld ||
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
        if (launchForwardsVelocity.sqrMagnitude > .0001f)
        {
            Vector3 forcedVelocity = launchForwardsVelocity;
            launchForwardsVelocity = Vector3.MoveTowards(
                launchForwardsVelocity, Vector3.zero,
                launchForwardsDeceleration * Time.deltaTime);
            return forcedVelocity;
        }

        if (playerState.IsStaggered())
        {
            return Vector3.zero;
        }

        if (playerState.IsBurnStunned())
            return Vector3.zero;

        if (playerState.IsBurnWalking())
            return CalculateBurnWalkVelocity();

        if (playerState.IsAttacking())
        {
            isDashJump = false;
            // O avanco proprio do ataque e aplicado por PlayerAttackMovement. Aqui desaceleramos o movimento que ja existia para evitar um corte brusco de velocidade ao atacar enquanto corre.
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

    private Vector3 CalculateBurnWalkVelocity()
    {
        float multiplier = playerHeat != null
            ? playerHeat.BurnMovementSpeedMultiplier
            : 0f;
        Vector3 targetVelocity = DesiredDirection * GetMovementSpeed() * multiplier;

        return Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.deltaTime
        );
    }

    private Vector3 CalculateSlideVelocity()
    {
        bool onSteepSlope = TryGetSteepSlopeDownhillDirection(
            out Vector3 steepDownhillDirection);
        if ((!IsGrounded && !onSteepSlope) || !IsCrouchHeld())
        {
            EndSlide();
            return horizontalVelocity;
        }

        if (onSteepSlope)
        {
            slideDirection = steepDownhillDirection;
        }
        else if (DesiredDirection.sqrMagnitude > 0.0001f)
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
            controller == null || !controller.HasGroundContact ||
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
        groundNormal = controller != null
            ? controller.GroundNormal
            : Vector3.up;
        return controller != null && controller.HasGroundContact;
    }

    private bool IsCrouchHeld()
    {
        return GameInput.CrouchHeld;
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
        if (verticalVelocity > 0f)
        {
            return CalculateAirVelocity();
        }

        // O Slide manual tem prioridade em CalculateHorizontalVelocity. Aqui
        // tratamos somente o movimento normal: acima do Slope Limit, o player
        // nao consegue caminhar/subir e escorrega na direcao da descida.
        if (TryGetSteepSlopeDownhillDirection(out Vector3 downhillDirection))
            return CalculateSteepSlopeVelocity(downhillDirection);

        if (!IsGrounded)
            return CalculateAirVelocity();

        float movementSpeed = GetMovementSpeed();
        float currentSpeed = isCrouching
            ? movementSpeed * crouchSpeedMultiplier
            : movementSpeed;
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
            float maximumAirSpeed = GetMovementSpeed() * airMaxSpeedMultiplier;
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
        return movesetController != null &&
            movesetController.Moveset != null
            ? movesetController.Moveset.dashImpulse
            : 12.8f;
    }

    private float GetMovementSpeed() => speed *
        (movesetController != null && movesetController.Moveset != null
            ? movesetController.Moveset.movementSpeedMultiplier
            : 1f);

    private float GetJumpInitialSpeed() => movesetController != null &&
        movesetController.Moveset != null
        ? movesetController.Moveset.jumpInitialSpeed
        : 14f;

    private float GetDashCooldown() => movesetController != null &&
        movesetController.Moveset != null
        ? movesetController.Moveset.dashCooldown
        : 1.3f;

    private int GetMaximumJumpCount() => CanAirJump ? 2 : 1;

    private void RotateTowardsMovement()
    {
        // Pre-Explosion e Burn liberam somente caminhada lenta, mas o corpo
        // ainda precisa acompanhar a direcao desse movimento.
        if (!playerState.IsNormal() && !playerState.IsBurnWalking())
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

        // CharacterController so confirma o chao depois deste Move. Encerrar
        // aqui evita que um clique capturado no mesmo frame do pouso seja
        // usado para continuar uma rota aerea.
        if (wasAirborne && verticalVelocity <= 0f &&
            (collisionFlags & CollisionFlags.Below) != 0)
        {
            movesetController?.ResetAerialSequenceOnLanding();
            wasAirborne = false;
        }

        if (playerState.IsSliding() &&
            (collisionFlags & CollisionFlags.Sides) != 0)
        {
            EndSlide();
        }

    }

    /// <summary>
    /// Chamado pelo sistema global para remover sobreposicao horizontal com
    /// entidades terrestres. Nao altera input, estado de ataque ou velocidade.
    /// </summary>
    public void ApplyGroundEntitySeparation(Vector3 displacement)
    {
        if (controller == null || !controller.enabled ||
            !gameObject.activeInHierarchy)
        {
            return;
        }

        displacement.y = 0f;
        if (displacement.sqrMagnitude > .0000001f)
            controller.Move(displacement);
    }

    /// <summary>Impede que um inimigo seja tratado como chao valido pelo player.</summary>
    public void MarkEntityAsInvalidGround()
    {
        enemyGroundContactTimer = Mathf.Max(enemyGroundContactTimer, .12f);
        verticalVelocity = Mathf.Min(verticalVelocity, -2f);
    }

    private void HandleControllerHit(Collider hitCollider, Vector3 hitPoint,
        Vector3 hitNormal)
    {
        EnemyBase enemy = hitCollider != null
            ? hitCollider.GetComponentInParent<EnemyBase>()
            : null;
        if (enemy == null || enemy.IsDead || !IsLandingOnEnemyTop(enemy))
            return;

        // Um inimigo nunca conta como chao: nao reseta pulos e nao permite
        // iniciar um novo pulo. O impulso lateral tambem tira o player do
        // topo, em vez de deixa-lo equilibrado na cabeca do inimigo.
        enemyGroundContactTimer = .1f;
        verticalVelocity = Mathf.Min(verticalVelocity, -2f);

        Vector3 separationDirection = transform.position - enemy.transform.position;
        separationDirection.y = 0f;

        if (separationDirection.sqrMagnitude <= .0001f)
            separationDirection = transform.right;

        horizontalVelocity = separationDirection.normalized * enemyHeadSeparationSpeed;
    }

    private bool IsLandingOnEnemyTop(EnemyBase enemy)
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

    public void ApplyLaunchForwards(
        Vector3 direction,
        float force,
        float deceleration
    )
    {
        launchForwardsVelocity = direction * Mathf.Max(0f, force);
        launchForwardsDeceleration = Mathf.Max(.01f, deceleration);
    }

    /// <summary>Interrompe traversal ao iniciar Burn, sem alterar o estado.</summary>
    public void InterruptForBurn()
    {
        dashTimer = 0f;
        dashStartedGrounded = false;
        isDashJump = false;
        isCrouching = false;
        aerialLightTimer = 0f;
        aerialGravityMultiplier = 1f;
        jumpHoldTimer = 0f;
        horizontalVelocity = Vector3.zero;
        launchForwardsVelocity = Vector3.zero;
        launchForwardsDeceleration = 0f;
    }

    /// <summary>
    /// Encerra traversal que pode ser substituida por um ataque pesado. Nao
    /// interfere em stun, Burn ou morte, que continuam bloqueando ataques.
    /// </summary>
    public void InterruptForHeavyAttack()
    {
        if (playerState == null)
            return;

        if (playerState.IsDashing())
        {
            dashTimer = 0f;
            dashStartedGrounded = false;
            isDashJump = false;
            playerState.ChangeState(PlayerState.State.Normal);
            return;
        }

        if (playerState.IsSliding())
            EndSlide();
    }

    /// <summary>
    /// Encerra exclusivamente um Dash terrestre para que um Dash Attack possa
    /// assumir o controle no mesmo frame. Nao inicia nenhum ataque por conta
    /// propria e nao interfere em Dash aereo.
    /// </summary>
    public bool InterruptGroundDashForAttack()
    {
        if (!IsGroundDash || playerState == null)
            return false;

        dashTimer = 0f;
        dashStartedGrounded = false;
        isDashJump = false;
        playerState.ChangeState(PlayerState.State.Normal);
        return true;
    }

    public void ApplyAttackMovement(Vector3 displacement)
    {
        if (controller == null || displacement.sqrMagnitude <= 0.000001f)
            return;

        // PlayerAttackMovement planeja o ataque; PlayerMovement continua sendo o unico
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

    /// <summary>
    /// Inicia uma queda controlada para um ataque Dive. O movimento termina
    /// somente ao tocar o chao ou quando o ataque for cancelado.
    /// </summary>
    public void BeginAerialDive(float fallSpeed)
    {
        if (IsGrounded)
            return;

        isLaunched = false;
        aerialLightTimer = 0f;
        aerialGravityMultiplier = 1f;
        aerialDiveFallSpeed = Mathf.Max(0f, fallSpeed);
        verticalVelocity = -aerialDiveFallSpeed;
        jumpHoldTimer = 0f;
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
        float initialJumpSpeed = GetJumpInitialSpeed();
        if (initialJumpSpeed <= 0f)
            return 0f;

        float holdDuration = Mathf.Max(0f, maxJumpHoldDuration);
        float accelerationDuringHold = gravity + jumpHoldAcceleration;

        // O hold so fornece impulso enquanto o player ainda sobe. Se a
        // gravidade vencer antes do limite, usamos apenas esse trecho real.
        if (accelerationDuringHold < 0f && initialJumpSpeed > 0f)
        {
            holdDuration = Mathf.Min(
                holdDuration,
                initialJumpSpeed / -accelerationDuringHold
            );
        }

        float speedAfterHold = initialJumpSpeed +
            accelerationDuringHold * holdDuration;
        float heightDuringHold = initialJumpSpeed * holdDuration +
            .5f * accelerationDuringHold * holdDuration * holdDuration;
        float gravityMagnitude = Mathf.Max(.001f, Mathf.Abs(gravity));

        float heightAfterHold = speedAfterHold > 0f
            ? speedAfterHold * speedAfterHold / (2f * gravityMagnitude)
            : 0f;
        return Mathf.Max(0f, heightDuringHold + heightAfterHold);
    }

    public void ApplySelfLaunchUp(float initialSpeed, float gravity)
    {
        // Uma fonte externa pode colocar o player em uma trajetoria identica a de um inimigo lancado, sem depender de um ataque especifico.
        // Launch Up e uma trajetoria vertical intencional: ele limpa qualquer momentum horizontal anterior antes de aplicar o impulso para cima.
        horizontalVelocity = Vector3.zero;
        launchForwardsVelocity = Vector3.zero;
        launchForwardsDeceleration = 0f;
        aerialLightTimer = 0f;
        aerialGravityMultiplier = 1f;
        jumpHoldTimer = 0f;
        verticalVelocity = initialSpeed;
        launchGravity = gravity;
        isLaunched = true;
    }
}

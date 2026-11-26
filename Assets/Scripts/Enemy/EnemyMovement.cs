using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Locomocao, gravidade, Launch Up, Juggle, Launch Forwards e colisao.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Velocidade usada ao bloquear e recuar. Deve ser menor ou igual a Standard Speed.")]
    [Min(0f)] [SerializeField] private float slowSpeed = 1.1f;
    [Tooltip("Velocidade constante usada em strafe, orbit, recuo, reposicionamento e outras locomocoes comuns.")]
    [Min(0f)] [SerializeField] private float standardSpeed = 2.2f;
    [Tooltip("Velocidade constante usada para perseguir o jogador e buscar sua ultima posicao conhecida.")]
    [Min(0f)] [SerializeField] private float fastSpeed = 3.3f;
    [Tooltip("Aceleracao vertical somada a velocidade. Use um valor negativo para queda.")]
    [SerializeField] private float gravity = -20f;
    [Tooltip("Velocidade maxima de queda vertical.")]
    [Min(2f)] [SerializeField] private float terminalFallSpeed = 30f;
    [Tooltip("Velocidade de giro ao mirar ou andar.")]
    [Min(0f)] [SerializeField] private float rotationSpeed = 240f;
    [Tooltip("Ao perseguir, gira antes de avancar; ainda permite strafe e recuo no reposicionamento.")]
    [SerializeField] private bool useTankControls;
    [Tooltip("Alinhamento minimo entre a frente do inimigo e o alvo antes de Tank Controls permitir avanco. 1 exige alinhamento perfeito.")]
    [Range(0f, 1f)] [SerializeField] private float tankControlsAdvanceAlignment = .85f;

    [Header("Launch Up")]
    [Tooltip("Permite que ataques com Launch Up levantem este inimigo.")]
    [SerializeField] private bool canBeLaunchedUp = true;
    [Tooltip("Permite que ataques com Launch Down lancem este inimigo para o chao enquanto ele estiver no ar.")]
    [SerializeField] private bool canBeLaunchedDown = true;
    [Tooltip("Permite que ataques com Launch Forwards ou Launch Backwards lancem este inimigo horizontalmente.")]
    [SerializeField] private bool canBeLaunchedForwards = true;
    [Tooltip("Permite que ataques de Juggle alterem o movimento vertical depois de Launch Up.")]
    [SerializeField] private bool canBeJuggled = true;

    [Header("Impacto Fisico")]
    [Tooltip("Massa relativa deste inimigo em impactos. Mais massa reduz dano, Poise e deslocamento recebidos; quando arremessado, transfere mais forca para outros inimigos.")]
    [FormerlySerializedAs("bodyImpactWeight")]
    [Min(.01f)] [SerializeField] private float impactWeight = 1f;
    [Tooltip("Aumento percentual do raio e da altura da capsula invisivel que detecta inimigos durante Launch Forwards. Nao altera o CharacterController normal.")]
    [Range(0f, 300f)] [SerializeField] private float launchedBodyImpactHitboxSizeIncreasePercent = 50f;
    [Tooltip("Camadas consultadas pela hitbox de impacto corporal durante Launch Forwards.")]
    [SerializeField] private LayerMask bodyImpactQueryMask = Physics.DefaultRaycastLayers;

    [Header("Separacao de Entidades")]
    [Tooltip("Ignora a separacao horizontal entre entidades terrestres. Use para inimigos rastejadores ou outras criaturas que devem poder passar por baixo/de dentro de outras entidades. Flyers ja sao ignorados automaticamente.")]
    [SerializeField] private bool ignoreGroundEntitySeparation;

    private EnemyBase owner;
    private EnemyDecisions brain;
    private EnemyHealth health;
    private EnemyAttackController attacks;
    private CharacterController controller;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private Vector3 slideLaunchHorizontalVelocity;
    private bool hasSlideLaunchHorizontalImpulse;
    private float slideLaunchHorizontalDeceleration;
    private Vector3 playerHeadSeparationVelocity;
    private float playerHeadSeparationTimer;
    private float verticalVelocity;
    private float verticalDisplacementThisFrame;
    private float launchGravity;
    private float launchHoldElapsed;
    private float launchHoldDuration;
    private float launchHoldGravityMultiplier = 1f;
    private bool hasLaunchHoldProfile;
    private float juggleTimer;
    private float juggleGravityMultiplier = 1f;
    private Vector3 launchForwardsVelocity;
    private float launchForwardsDeceleration;
    private Vector3 lastMoveVelocity;
    private bool isLaunched;
    private bool hasLaunchProfileForFrame;
    private bool isLaunchedForwards;
    private bool hasGroundContact;
    private int lastStaticImpactFrame = -1;
    private bool temporarilyIgnoreGroundEntitySeparation;
    private bool grapplePullActive;
    private bool suppressAuthoredMovement;
    private Vector3 grapplePullVelocity;
    private readonly HashSet<EnemyBase> bodyImpactTargets = new();
    private Collider[] bodyImpactOverlapBuffer = new Collider[64];

    public float SlowSpeed => slowSpeed;
    public float StandardSpeed => standardSpeed;
    public float FastSpeed => fastSpeed;
    public float LastHorizontalSpeed =>
        new Vector3(lastMoveVelocity.x, 0f, lastMoveVelocity.z).magnitude;
    public float ImpactWeight => Mathf.Max(.01f, impactWeight);
    public bool CanBeLaunchedUp => canBeLaunchedUp;
    public bool CanBeLaunchedDown => canBeLaunchedDown;
    public bool CanBeLaunchedForwards => canBeLaunchedForwards;
    public bool CanBeJuggled => canBeJuggled;
    public bool IsLaunched => isLaunched;
    public bool IsLaunchedForwards => isLaunchedForwards;
    public Vector3 LaunchForwardsVelocity => launchForwardsVelocity;
    /// <summary>CharacterController exposto apenas para a separacao central de entidades terrestres.</summary>
    public CharacterController EntityCharacterController => controller;
    /// <summary>Indica se este inimigo participa da separacao entre entidades terrestres.</summary>
    public bool ParticipatesInGroundEntitySeparation =>
        !ignoreGroundEntitySeparation &&
        !temporarilyIgnoreGroundEntitySeparation;
    /// <summary>Indica se o Character Controller esta apoiado no chao.</summary>
    public bool IsGrounded
    {
        get
        {
            if (!IsControllerUsable)
                return false;
            // Flutuando, o bobbing gera velocidade vertical positiva sem que
            // o inimigo esteja no ar.
            if (IsFloatingOnWater)
                return true;
            if (verticalVelocity > .05f)
                return false;
            if (isLaunched && !hasGroundContact)
                return false;
            return hasGroundContact || controller.isGrounded;
        }
    }
    public float VerticalVelocity => verticalVelocity;

    private WaterSensor Water => owner != null ? owner.Water : null;
    private float WaterSpeedMultiplier => Water != null ? Water.SpeedMultiplier : 1f;
    private Vector3 waterInertiaVelocity;

    // Na agua o inimigo ganha e perde velocidade gradualmente (WaterProfile).
    // Fora da agua, sem limite configurado ou durante ataque/esquiva/guarda, a
    // velocidade passa direto (os avancos autorados nao sao suavizados).
    private bool WaterInertiaHandledThisFrame;

    // Lentidao base e inercia da agua valem so para o movimento de andar; os
    // avancos autorados (ataque, esquiva, guarda) tem os proprios
    // multiplicadores e nao sao suavizados.
    private Vector3 ApplyWaterWalking(Vector3 desired)
    {
        WaterSensor water = Water;
        bool walking = owner != null && (owner.CurrentState == EnemyBase.State.Idle ||
            owner.CurrentState == EnemyBase.State.Combat ||
            owner.CurrentState == EnemyBase.State.TacticalIdle);
        WaterInertiaHandledThisFrame = true;
        if (water == null || !water.IsInWater || !walking)
        {
            waterInertiaVelocity = desired;
            return desired;
        }

        return ApplyWaterInertia(desired * water.SpeedMultiplier, water);
    }

    private Vector3 ApplyWaterInertia(Vector3 desired, WaterSensor water)
    {
        float rate = desired.sqrMagnitude < waterInertiaVelocity.sqrMagnitude
            ? water.DecelerationLimit : water.AccelerationLimit;
        if (rate <= 0f)
        {
            waterInertiaVelocity = desired;
            return desired;
        }

        waterInertiaVelocity = Vector3.MoveTowards(waterInertiaVelocity, desired,
            rate * Time.deltaTime);
        return waterInertiaVelocity;
    }
    // Flutuando na agua funda: sem lancamento e com os pes na altura de
    // flutuacao. Conta como chao para a IA e para os efeitos.
    private bool IsFloatingOnWater
    {
        get
        {
            WaterSensor water = Water;
            return water != null && water.IsSwimming && controller != null &&
                owner != null && owner.FloatsInWater &&
                !isLaunched && Mathf.Abs(water.FloatTargetFeetY -
                    controller.bounds.min.y) < .4f;
        }
    }

    /// <summary>Suspende temporariamente a separacao global entre inimigos durante movimentos especiais.</summary>
    public void SetTemporaryGroundEntitySeparationIgnored(bool ignored) =>
        temporarilyIgnoreGroundEntitySeparation = ignored;

    internal void Initialize(EnemyBase enemy, EnemyHealth enemyHealth)
    {
        owner = enemy;
        health = enemyHealth;
        brain = GetComponent<EnemyDecisions>();
        attacks = GetComponent<EnemyAttackController>();
        controller = GetComponent<CharacterController>();
    }

    private void OnValidate()
    {
        if (gravity > 0f)
            gravity = -gravity;
        slowSpeed = Mathf.Max(0f, slowSpeed);
        standardSpeed = Mathf.Max(slowSpeed, standardSpeed);
        fastSpeed = Mathf.Max(standardSpeed, fastSpeed);
    }

    /// <summary>
    /// Move por uma velocidade em coordenadas de mundo. Sistemas especiais
    /// (ataques, guarda, esquiva e forças) usam esta camada sem decidir giro.
    /// </summary>
    public void Move(Vector3 horizontalVelocity)
    {
        // A apresentacao de spawn desativa temporariamente o controller para
        // impedir colisao antes de o inimigo nascer. Nunca chame Move nesse
        // intervalo: o Unity registra um erro mesmo que o GameObject siga
        // ativo e o Brain ainda tenha um Tick pendente naquele frame.
        if (!IsControllerUsable)
            return;
        Vector3 velocity = grapplePullActive ? grapplePullVelocity :
            isLaunchedForwards ? launchForwardsVelocity :
            playerHeadSeparationTimer > 0f ? playerHeadSeparationVelocity :
            hasSlideLaunchHorizontalImpulse ? slideLaunchHorizontalVelocity :
            knockbackTimer > 0f ? knockbackVelocity :
            suppressAuthoredMovement ? Vector3.zero : ApplyWaterWalking(horizontalVelocity);

        playerHeadSeparationTimer = Mathf.Max(0f, playerHeadSeparationTimer - Time.deltaTime);
        knockbackTimer = Mathf.Max(0f, knockbackTimer - Time.deltaTime);
        if (grapplePullActive)
            verticalVelocity = grapplePullVelocity.y;
        else if (hasLaunchProfileForFrame)
        {
            velocity.y = 0f;
        }
        else
            velocity.y = verticalVelocity;
        lastMoveVelocity = velocity;
        // Knockback, launch, grapple etc. nao passam pela inercia: acompanha a
        // velocidade real para nao "voltar" deslizando na direcao antiga.
        if (!WaterInertiaHandledThisFrame)
            waterInertiaVelocity = new Vector3(velocity.x, 0f, velocity.z);
        WaterInertiaHandledThisFrame = false;
        Vector3 displacement = velocity * Time.deltaTime;
        if (hasLaunchProfileForFrame)
            displacement.y = verticalDisplacementThisFrame;
        hasLaunchProfileForFrame = false;
        CollisionFlags flags = controller.Move(displacement);
        hasGroundContact = (flags & CollisionFlags.Below) != 0 ||
            controller.isGrounded;

        if (isLaunchedForwards)
        {
            ScanThrownBodyImpactHitbox();
            launchForwardsVelocity = Vector3.MoveTowards(
                launchForwardsVelocity,
                Vector3.zero,
                launchForwardsDeceleration * Time.deltaTime
            );
            if (launchForwardsVelocity.sqrMagnitude <= .0001f)
                ResetLaunchForwards();
        }
        if (hasSlideLaunchHorizontalImpulse && slideLaunchHorizontalDeceleration > 0f)
        {
            slideLaunchHorizontalVelocity = Vector3.MoveTowards(
                slideLaunchHorizontalVelocity, Vector3.zero,
                slideLaunchHorizontalDeceleration * Time.deltaTime);
            if (slideLaunchHorizontalVelocity.sqrMagnitude <= .0001f)
                CancelSlideLaunchHorizontalImpulse();
        }
        if (isLaunched && verticalVelocity <= 0f &&
            (flags & CollisionFlags.Below) != 0)
        {
            float landingSpeed = Mathf.Max(0f, -verticalVelocity);
            bool floorSplatted = TryApplyStaticSplat(landingSpeed, Vector3.down);
            isLaunched = false;
            launchHoldElapsed = launchHoldDuration;
            verticalVelocity = -2f;
            ResetJuggleMovement();
            CancelSlideLaunchHorizontalImpulse();
            ResetLaunchForwards();
            if (!floorSplatted && !owner.IsDead)
            {
                owner.CurrentState = EnemyBase.State.Combat;
                brain.DecideImmediately();
            }
        }
    }

    public void SetAuthoredMovementSuppressed(bool suppressed) =>
        suppressAuthoredMovement = suppressed;

    public void ResetForPracticeDummy(Vector3 position, Quaternion rotation)
    {
        waterInertiaVelocity = Vector3.zero;
        if (controller != null)
            controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (controller != null)
            controller.enabled = true;
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        playerHeadSeparationVelocity = Vector3.zero;
        playerHeadSeparationTimer = 0f;
        CancelSlideLaunchHorizontalImpulse();
        ResetLaunchForwards();
        ResetJuggleMovement();
        grapplePullActive = false;
        grapplePullVelocity = Vector3.zero;
        isLaunched = false;
        verticalVelocity = 0f;
        hasGroundContact = controller != null && controller.isGrounded;
        lastMoveVelocity = Vector3.zero;
    }

    /// <summary>
    /// Chamado pelo sistema global para desfazer sobreposicao horizontal sem
    /// depender da IA, do ataque atual ou do estado de stagger.
    /// </summary>
    public void ApplyGroundEntitySeparation(Vector3 displacement)
    {
        if (!IsControllerUsable)
            return;

        displacement.y = 0f;
        if (displacement.sqrMagnitude > .0000001f)
            controller.Move(displacement);
    }

    /// <summary>
    /// Correcao de contato do sistema central quando este inimigo cai sobre a
    /// cabeca de outra entidade: desliza pelo domo em vez de se apoiar nela.
    /// </summary>
    public void ApplyEntityDomeContact(Vector3 correction, float maxFallSpeed)
    {
        if (!IsControllerUsable)
            return;

        if (correction.sqrMagnitude > .0000001f)
            controller.Move(correction);
        if (!hasLaunchProfileForFrame && verticalVelocity < -maxFallSpeed)
            verticalVelocity = -maxFallSpeed;
    }

    public void ApplyGravity()
    {
        if (!IsControllerUsable) return;

        // Agua funda: sem gravidade; os pes seguem a altura de flutuacao.
        WaterSensor water = Water;
        if (water != null && water.IsSwimming && !grapplePullActive)
        {
            // Lancado e caindo dentro da agua: a trajetoria termina aqui.
            if (isLaunched && verticalVelocity <= 0f)
            {
                // Mesma limpeza do pouso (sem splat): impulsos horizontais do
                // launch nao podem continuar derivando pela agua.
                isLaunched = false;
                launchGravity = gravity;
                launchHoldElapsed = launchHoldDuration;
                ResetJuggleMovement();
                CancelSlideLaunchHorizontalImpulse();
                ResetLaunchForwards();
                if (owner != null && !owner.IsDead)
                {
                    owner.CurrentState = EnemyBase.State.Combat;
                    brain.DecideImmediately();
                }
            }

            if (!isLaunched)
            {
                WaterProfile profile = water.Profile;
                float error = water.FloatTargetFeetY - controller.bounds.min.y;
                verticalVelocity = Mathf.Clamp(error * profile.floatSpringFrequency,
                    -profile.floatMaxVerticalSpeed, profile.floatMaxVerticalSpeed) +
                    water.FloatBobSpeedBoost;
                verticalDisplacementThisFrame = verticalVelocity * Time.deltaTime;
                hasLaunchProfileForFrame = false;
                return;
            }
        }

        if (!isLaunched && IsGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        if (juggleTimer > 0f)
        {
            juggleTimer = Mathf.Max(0f, juggleTimer - Time.deltaTime);
            if (juggleTimer <= 0f)
                juggleGravityMultiplier = 1f;
        }
        float activeGravity = isLaunched ? launchGravity : gravity;
        float dt = Time.deltaTime;
        if (isLaunched && verticalVelocity > 0f &&
            launchHoldElapsed < launchHoldDuration)
        {
            float heldTime = Mathf.Min(dt,
                launchHoldDuration - launchHoldElapsed);
            IntegrateVerticalAcceleration(activeGravity *
                launchHoldGravityMultiplier * juggleGravityMultiplier,
                heldTime);
            float displacement = verticalDisplacementThisFrame;
            launchHoldElapsed += heldTime;
            dt -= heldTime;
            if (dt > 0f)
            {
                IntegrateVerticalAcceleration(activeGravity *
                    juggleGravityMultiplier, dt);
                displacement += verticalDisplacementThisFrame;
            }
            verticalDisplacementThisFrame = displacement;
        }
        else
            IntegrateVerticalAcceleration(activeGravity *
                juggleGravityMultiplier, dt);
        hasLaunchProfileForFrame = isLaunched;
    }

    private void IntegrateVerticalAcceleration(float acceleration,
        float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            verticalDisplacementThisFrame = 0f;
            return;
        }
        float initialVelocity = verticalVelocity;
        float finalVelocity = initialVelocity + acceleration * deltaTime;
        if (acceleration < 0f && finalVelocity < -terminalFallSpeed)
        {
            float untilTerminal = Mathf.Clamp(
                (-terminalFallSpeed - initialVelocity) / acceleration,
                0f, deltaTime);
            verticalDisplacementThisFrame = initialVelocity * untilTerminal +
                .5f * acceleration * untilTerminal * untilTerminal -
                terminalFallSpeed * (deltaTime - untilTerminal);
            verticalVelocity = -terminalFallSpeed;
            return;
        }
        verticalDisplacementThisFrame = initialVelocity * deltaTime +
            .5f * acceleration * deltaTime * deltaTime;
        verticalVelocity = finalVelocity;
    }

    public void SetVerticalVelocity(float value)
    {
        verticalVelocity = value;
        if (value > .05f)
            hasGroundContact = false;
    }

    /// <summary>Ativa o controle de movimento do grapple sem alterar a gravidade padrao.</summary>
    public void BeginGrapplePull()
    {
        grapplePullActive = true;
        grapplePullVelocity = Vector3.zero;
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        playerHeadSeparationVelocity = Vector3.zero;
        playerHeadSeparationTimer = 0f;
        CancelSlideLaunchHorizontalImpulse();
        ResetLaunchForwards();
        isLaunched = false;
        verticalVelocity = 0f;
        hasGroundContact = controller != null && controller.isGrounded;
    }

    /// <summary>Define a velocidade vetorial de puxao usada no proximo passo do motor.</summary>
    public void SetGrapplePullVelocity(Vector3 velocity)
    {
        if (!grapplePullActive)
            return;

        grapplePullVelocity = velocity;
    }

    /// <summary>Devolve o controle do deslocamento ao Brain e encerra o puxao.</summary>
    public void EndGrapplePull()
    {
        grapplePullActive = false;
        grapplePullVelocity = Vector3.zero;
        verticalVelocity = IsGrounded ? -2f : 0f;
    }

    private bool IsControllerUsable => controller != null &&
        controller.enabled && controller.gameObject.activeInHierarchy;

    internal void StopForTaunt()
    {
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        playerHeadSeparationVelocity = Vector3.zero;
        playerHeadSeparationTimer = 0f;
        CancelSlideLaunchHorizontalImpulse();
    }

    /// <summary>Move em direcao ao alvo. Tank Controls gira antes de avancar.</summary>
    public void MoveTowardTarget(float speed) =>
        MoveTowardDirection(GetTargetDirection(), speed);

    /// <summary>
    /// Desloca-se na direcao tática calculada pelo Brain sem virar as costas
    /// ao jogador. Permite perseguir e orbitar mantendo o alvo a frente.
    /// </summary>
    public void MoveWithTargetFacing(Vector3 direction, float speed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f || speed <= 0f)
        {
            Move(Vector3.zero);
            return;
        }

        FaceTarget();
        Move(direction.normalized * speed);
    }

    /// <summary>
    /// Move em uma direcao de mundo. Com Tank Controls, a translacao acontece
    /// apenas pela frente local depois que o corpo esta alinhado.
    /// </summary>
    public void MoveTowardDirection(Vector3 direction, float speed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f || speed <= 0f)
        {
            Move(Vector3.zero);
            return;
        }

        direction.Normalize();
        FaceDirection(direction);
        if (!useTankControls)
        {
            Move(direction * speed);
            return;
        }

        float alignment = Vector3.Dot(transform.forward, direction);
        if (alignment < tankControlsAdvanceAlignment)
        {
            Move(Vector3.zero);
            return;
        }

        float advanceFactor = Mathf.InverseLerp(tankControlsAdvanceAlignment,
            1f, alignment);
        Move(transform.forward * speed * advanceFactor);
    }

    /// <summary>Orbita o alvo pelos eixos locais, mantendo-o a frente.</summary>
    public void StrafeTarget(float side, float speed)
    {
        FaceTarget();
        Move(transform.right * Mathf.Sign(side) * Mathf.Max(0f, speed));
    }

    /// <summary>Recua pelos eixos locais, mantendo o corpo voltado ao alvo.</summary>
    public void RetreatFromTarget(float speed)
    {
        FaceTarget();
        Move(-transform.forward * Mathf.Max(0f, speed));
    }

    /// <summary>Gira o corpo para o alvo atual, independente de Tank Controls.</summary>
    public void FaceTarget()
    {
        FaceDirection(GetTargetDirection());
    }

    public void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= .001f) return;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
    }

    private Vector3 GetTargetDirection()
    {
        if (brain == null || brain.Target == null)
            return Vector3.zero;
        Vector3 direction = brain.Target.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > .001f ? direction.normalized : Vector3.zero;
    }

    public void ApplyKnockback(Vector3 attackerPosition, float force, float duration)
    {
        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;
        knockbackVelocity = direction.sqrMagnitude > .001f
            ? direction.normalized * force : Vector3.zero;
        knockbackTimer = duration;
        FaceForceOrigin(attackerPosition);
    }

    public void ApplyLaunchUp(float initialSpeed, float newGravity,
        float holdDuration = 0f, float holdGravityMultiplier = 1f)
    {
        if (!canBeLaunchedUp || owner.IsDead) return;
        attacks.CancelAttack();
        owner.CancelDodge();
        owner.CancelTaunt();
        owner.CancelGuardForAirborne();
        owner.CurrentState = EnemyBase.State.Combat;
        brain.CancelReposition();
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        ResetLaunchForwards();
        verticalVelocity = initialSpeed;
        launchGravity = newGravity;
        launchHoldElapsed = 0f;
        launchHoldDuration = Mathf.Max(0f, holdDuration);
        launchHoldGravityMultiplier = Mathf.Clamp(holdGravityMultiplier,
            .05f, 1f);
        hasLaunchProfileForFrame = false;
        isLaunched = true;
        hasGroundContact = false;
        ResetJuggleMovement();
        CancelSlideLaunchHorizontalImpulse();
    }

    public void ApplyLaunchUpHorizontalImpulse(Vector3 direction, float speed)
    {
        ApplyLaunchUpHorizontalImpulse(direction, speed, 0f);
    }

    /// <summary>Aplica Launch Up e Launch Forwards como uma unica reacao diagonal.</summary>
    public void ApplyLaunchUpAndForwards(Vector3 attackerPosition,
        float initialSpeed, float newGravity, float forwardsForce,
        float forwardsDeceleration, float holdDuration = 0f,
        float holdGravityMultiplier = 1f)
    {
        if (!canBeLaunchedUp || !canBeLaunchedForwards || owner.IsDead)
            return;

        ApplyLaunchUp(initialSpeed, newGravity, holdDuration,
            holdGravityMultiplier);
        if (forwardsForce <= 0f || forwardsDeceleration <= 0f)
            return;

        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f)
            direction = transform.forward;
        ApplyLaunchUpHorizontalImpulse(direction, forwardsForce,
            forwardsDeceleration);
        FaceForceOrigin(attackerPosition);
    }

    public void CancelSlideLaunchHorizontalImpulse()
    {
        slideLaunchHorizontalVelocity = Vector3.zero;
        hasSlideLaunchHorizontalImpulse = false;
        slideLaunchHorizontalDeceleration = 0f;
    }

    internal void PrepareForDodge()
    {
        // A retirada precisa vencer deslocamentos horizontais antigos, mas a
        // velocidade vertical e o Launch Up continuam existindo no ar.
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        playerHeadSeparationVelocity = Vector3.zero;
        playerHeadSeparationTimer = 0f;
        CancelSlideLaunchHorizontalImpulse();
    }

    public void ApplyLaunchForwards(Vector3 attackerPosition, float force, float deceleration)
    {
        if (!canBeLaunchedForwards || owner.IsDead || force <= 0f || deceleration <= 0f)
            return;
        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f)
            direction = transform.forward;
        BeginLaunchForwards(direction, force, deceleration);
        FaceForceOrigin(attackerPosition);
    }

    public void ApplyLaunchBackwards(Vector3 attackerPosition, float force,
        float deceleration)
    {
        if (!canBeLaunchedForwards || owner.IsDead || force <= 0f ||
            deceleration <= 0f)
            return;

        Vector3 direction = attackerPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f)
            direction = transform.forward;
        BeginLaunchForwards(direction, force, deceleration);
        FaceForceOrigin(attackerPosition);
    }

    /// <summary>
    /// Recebe a velocidade transferida por outro inimigo arremessado. Nao e
    /// um ataque do player: guarda e esquiva nao participam deste impacto.
    /// </summary>
    public bool ApplyBodyImpactLaunch(Vector3 direction, float speed,
        float deceleration)
    {
        if (!canBeLaunchedForwards || owner == null || owner.IsDead ||
            speed <= 0f || deceleration <= 0f)
        {
            return false;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f)
            return false;

        BeginLaunchForwards(direction, speed, deceleration);
        return true;
    }

    private void BeginLaunchForwards(Vector3 direction, float force,
        float deceleration)
    {
        attacks.CancelAttack();
        owner.CancelDodge();
        owner.CancelTaunt();
        owner.CurrentState = EnemyBase.State.Combat;
        brain.CancelReposition();
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        launchForwardsVelocity = direction.normalized * force;
        launchForwardsDeceleration = deceleration;
        isLaunchedForwards = true;
        bodyImpactTargets.Clear();
        CancelSlideLaunchHorizontalImpulse();
        FaceAgainstHorizontalForce(direction);
    }

    public void ApplyAerialJuggleLight(float liftSpeed, float newGravity,
        float gravityMultiplier, float duration)
    {
        if (!canBeJuggled || IsGrounded || owner.IsDead) return;
        attacks.CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        isLaunched = true;
        verticalVelocity = liftSpeed;
        hasGroundContact = false;
        launchGravity = newGravity;
        juggleGravityMultiplier = Mathf.Clamp01(gravityMultiplier);
        juggleTimer = Mathf.Max(juggleTimer, duration);
    }

    public void ApplyAerialDive(float fallSpeed)
    {
        if (!canBeLaunchedDown || IsGrounded || owner.IsDead) return;
        attacks.CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        isLaunched = true;
        verticalVelocity = -Mathf.Max(0f, fallSpeed);
        hasGroundContact = false;
        launchGravity = gravity;
        ResetJuggleMovement();
    }

    private struct HitClassification
    {
        public PlayerMovement player;
        public EnemyBase enemy;
    }

    // OnControllerColliderHit dispara quase todo frame (inclusive no chao).
    // Classificar o collider uma unica vez evita GetComponentInParent repetido.
    private const int HitClassificationCacheLimit = 64;
    private readonly Dictionary<Collider, HitClassification>
        hitClassificationCache = new();

    private HitClassification ClassifyHitCollider(Collider collider)
    {
        if (hitClassificationCache.TryGetValue(collider,
            out HitClassification cached) &&
            (cached.player == null || cached.player) &&
            (cached.enemy == null || cached.enemy))
        {
            return cached;
        }

        if (hitClassificationCache.Count >= HitClassificationCacheLimit)
            hitClassificationCache.Clear();
        HitClassification result = new()
        {
            player = collider.GetComponentInParent<PlayerMovement>(),
            enemy = collider.GetComponentInParent<EnemyBase>()
        };
        hitClassificationCache[collider] = result;
        return result;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (owner == null || hit == null || hit.collider == null) return;
        Collider obstacle = hit.collider;
        HitClassification classification = ClassifyHitCollider(obstacle);
        PlayerMovement player = classification.player;
        if (player != null)
        {
            if (IsLandingOnPlayerTop(player))
                SeparateFromPlayerHead(player);
            return;
        }
        EnemyBase impactedEnemy = classification.enemy;
        if (impactedEnemy != null)
        {
            TryApplyThrownBodyImpact(impactedEnemy);
            return;
        }
        if (Mathf.Abs(hit.normal.y) > .5f) return;
        CombatPoiseSystem rules = CombatPoiseSystem.Instance;
        if ((rules.StaticImpactLayers.value &
            (1 << obstacle.gameObject.layer)) == 0)
            return;
        Vector3 horizontalVelocity = lastMoveVelocity;
        horizontalVelocity.y = 0f;
        Vector3 horizontalNormal = hit.normal;
        horizontalNormal.y = 0f;
        if (horizontalNormal.sqrMagnitude <= .0001f)
            return;

        horizontalNormal.Normalize();
        float impactSpeed = Mathf.Max(0f,
            -Vector3.Dot(horizontalVelocity, horizontalNormal));
        if (impactSpeed < rules.MinimumImpactSpeed)
            return;
        TryApplyStaticSplat(impactSpeed, horizontalVelocity);
    }

    private void TryApplyThrownBodyImpact(EnemyBase impactedEnemy)
    {
        if (!isLaunchedForwards || owner == null || impactedEnemy == null ||
            impactedEnemy == owner || impactedEnemy.IsDead || health == null)
        {
            return;
        }

        Vector3 impactVelocity = launchForwardsVelocity;
        impactVelocity.y = 0f;
        float impactSpeed = impactVelocity.magnitude;
        if (impactSpeed <= .0001f)
            return;

        Vector3 impactDirection = impactVelocity.normalized;
        CombatPoiseSystem rules = CombatPoiseSystem.Instance;
        float targetSpeed = Vector3.Dot(impactedEnemy.LaunchForwardsVelocity,
            impactDirection);
        float relativeSpeed = Mathf.Max(0f, impactSpeed - targetSpeed);
        if (relativeSpeed < rules.MinimumImpactSpeed ||
            !bodyImpactTargets.Add(impactedEnemy))
            return;

        CombatMassImpact massImpact = rules.ResolveMassImpact(ImpactWeight,
            impactedEnemy.ImpactWeight, impactSpeed, targetSpeed);
        rules.CalculateImpactDamage(massImpact.TargetImpactIntensity,
            out int damage, out int poiseDamage);
        CombatPoiseHitResult poiseHit = default;
        if (impactedEnemy.TryConsumeCollisionDamageProtection(
            rules.EnemyCollisionDamageProtectionDuration))
        {
            bool connected = impactedEnemy.TakeDamage(damage,
                owner.AttackOrigin, poiseDamage, out _, out poiseHit, false,
                PlayerAttackType.None, 0f, impactDirection,
                impactedEnemy.AttackOrigin, true);
            if (!connected)
                return;
        }

        // Colisao 1D elástica simplificada contra um alvo inicialmente
        // parado. Assim, somente as massas relativas e a velocidade atual
        // definem o quanto cada corpo continua (ou ricocheteia) apos bater.
        bool chainLaunched = impactedEnemy.ApplyBodyImpactLaunch(
            impactDirection, massImpact.TargetPostImpactSpeed,
            launchForwardsDeceleration);
        if (!chainLaunched)
        {
            impactedEnemy.ApplyPoiseReaction(poiseHit,
                new CombatReactionRequest(CombatReactionType.DefaultStagger,
                    owner.AttackOrigin, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f));
        }

        // Um alvo que nao aceita Launch Forwards e tratado como obstaculo
        // imovel: o arremesso nao atravessa nem continua por ele.
        launchForwardsVelocity = chainLaunched
            ? impactDirection * massImpact.SourcePostImpactSpeed : Vector3.zero;
        if (launchForwardsVelocity.sqrMagnitude <= .0001f)
            ResetLaunchForwards();
    }

    private void ScanThrownBodyImpactHitbox()
    {
        if (!isLaunchedForwards || controller == null)
            return;

        GetBodyImpactCapsule(out Vector3 first, out Vector3 second,
            out float radius);
        int count;
        while (true)
        {
            count = Physics.OverlapCapsuleNonAlloc(first, second, radius,
                bodyImpactOverlapBuffer, bodyImpactQueryMask,
                QueryTriggerInteraction.Collide);
            if (count < bodyImpactOverlapBuffer.Length)
                break;
            System.Array.Resize(ref bodyImpactOverlapBuffer,
                bodyImpactOverlapBuffer.Length * 2);
        }
        for (int index = 0; index < count; index++)
        {
            Collider hit = bodyImpactOverlapBuffer[index];
            EnemyBase impactedEnemy = hit != null
                ? hit.GetComponentInParent<EnemyBase>() : null;
            TryApplyThrownBodyImpact(impactedEnemy);
        }
    }

    private Vector3 GetBodyImpactOrigin() => controller != null
        ? transform.TransformPoint(controller.center) : transform.position;

    private void GetBodyImpactCapsule(out Vector3 first, out Vector3 second,
        out float radius)
    {
        float sizeMultiplier = 1f + Mathf.Max(0f,
            launchedBodyImpactHitboxSizeIncreasePercent) / 100f;
        radius = Mathf.Max(.01f, controller.radius * sizeMultiplier);
        float halfLine = Mathf.Max(0f,
            controller.height * sizeMultiplier * .5f - radius);
        Vector3 center = GetBodyImpactOrigin();
        first = center + Vector3.up * halfLine;
        second = center - Vector3.up * halfLine;
    }

    private bool IsLandingOnPlayerTop(PlayerMovement player)
    {
        if (controller == null || verticalVelocity > 0f) return false;
        PlayerCharacterController playerController =
            player.EntityCharacterController;
        if (playerController == null) return false;
        return controller.bounds.min.y >= playerController.bounds.max.y - .15f;
    }

    private void SeparateFromPlayerHead(PlayerMovement player)
    {
        Vector3 direction = transform.position - player.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .0001f)
            direction = transform.right;
        playerHeadSeparationVelocity = direction.normalized *
            player.EnemyHeadSeparationSpeed;
        playerHeadSeparationTimer = .1f;
        verticalVelocity = Mathf.Min(verticalVelocity, -2f);
        FaceForceOrigin(player.transform.position);
    }

    /// <summary>
    /// Parede e chao sao o mesmo tipo de alvo: uma massa estatica virtual.
    /// A massa enorme faz o inimigo perder o movimento, mas a intensidade
    /// recebida continua proporcional a sua velocidade de impacto.
    /// </summary>
    private bool TryApplyStaticSplat(float impactSpeed, Vector3 impactDirection)
    {
        CombatPoiseSystem rules = CombatPoiseSystem.Instance;
        if (impactSpeed < rules.MinimumImpactSpeed || health == null ||
            owner == null || owner.IsDead ||
            lastStaticImpactFrame == Time.frameCount)
        {
            return false;
        }

        // Um CharacterController pode reportar varias faces da mesma quina
        // durante um unico Move. Isso representa um impacto, nao um por face.
        lastStaticImpactFrame = Time.frameCount;

        CombatMassImpact impact = rules.ResolveMassImpact(ImpactWeight,
            CombatPoiseSystem.StaticSurfaceMass, impactSpeed);
        rules.CalculateImpactDamage(impact.SourceImpactIntensity,
            out int damage, out int poiseDamage);
        bool applyCollisionDamage = health.TryConsumeCollisionDamageProtection(
            rules.EnemyCollisionDamageProtectionDuration);

        impactDirection.y = 0f;
        if (impactDirection.sqrMagnitude <= .0001f)
            impactDirection = transform.forward;
        impactDirection.Normalize();

        ResetLaunchForwards();
        CancelSlideLaunchHorizontalImpulse();
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        playerHeadSeparationVelocity = Vector3.zero;
        playerHeadSeparationTimer = 0f;
        attacks.CancelAttack();
        brain.CancelReposition();

        int damageDealt = 0;
        if (applyCollisionDamage)
        {
            bool connected = owner.TakeDamage(damage,
                owner.AttackOrigin - impactDirection, poiseDamage,
                out damageDealt, out _, false, PlayerAttackType.None, 0f,
                impactDirection, owner.AttackOrigin, true, null,
                GetStaticImpactParticleDirection(impactDirection));
            if (!connected || owner.IsDead)
                return true;
        }

        // Superficies estaticas sempre encerram o arremesso em Stagger;
        // o Poise recebido continua contabilizado para recuperacao futura.
        health.ApplyStaticImpactStagger(rules.StaticImpactStunDuration);
        CombatImpactSystem.ReportHeavyEnvironmentalImpact(transform,
            impactDirection, damageDealt);
        return true;
    }

    private Vector3 GetStaticImpactParticleDirection(Vector3 impactDirection)
    {
        Transform playerTarget = owner != null ? owner.Target : null;
        if (playerTarget != null)
        {
            Vector3 awayFromPlayer = transform.position - playerTarget.position;
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude > .0001f)
                return awayFromPlayer.normalized;
        }

        impactDirection.y = 0f;
        if (impactDirection.sqrMagnitude > .0001f)
            return -impactDirection.normalized;
        return transform.forward;
    }

    private void ResetJuggleMovement()
    {
        juggleTimer = 0f;
        juggleGravityMultiplier = 1f;
    }

    private void ApplyLaunchUpHorizontalImpulse(Vector3 direction, float speed,
        float deceleration)
    {
        if (!isLaunched || owner.IsDead || speed <= 0f) return;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .0001f) return;
        slideLaunchHorizontalVelocity = direction.normalized * speed;
        slideLaunchHorizontalDeceleration = Mathf.Max(0f, deceleration);
        hasSlideLaunchHorizontalImpulse = true;
        FaceAgainstHorizontalForce(direction);
    }

    /// <summary>
    /// Forcas externas empurram o inimigo para longe da origem. O corpo olha
    /// imediatamente para essa origem, deixando a reacao legivel mesmo antes
    /// de a IA voltar a tomar decisao.
    /// </summary>
    private void FaceForceOrigin(Vector3 origin)
    {
        Vector3 directionToOrigin = origin - transform.position;
        directionToOrigin.y = 0f;
        FaceDirectionImmediately(directionToOrigin);
    }

    private void FaceAgainstHorizontalForce(Vector3 forceDirection)
    {
        forceDirection.y = 0f;
        FaceDirectionImmediately(-forceDirection);
    }

    private void FaceDirectionImmediately(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized,
            Vector3.up);
    }

    private void ResetLaunchForwards()
    {
        launchForwardsVelocity = Vector3.zero;
        launchForwardsDeceleration = 0f;
        isLaunchedForwards = false;
        bodyImpactTargets.Clear();
    }

}

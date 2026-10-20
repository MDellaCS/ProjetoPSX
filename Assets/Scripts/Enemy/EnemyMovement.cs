using UnityEngine;

/// <summary>Locomocao, gravidade, Launch Up, Juggle, Launch Forwards e colisao.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Velocidade de deslocamento normal do inimigo.")]
    [Min(0f)] [SerializeField] private float movementSpeed = 2.2f;
    [Tooltip("Aceleracao vertical somada a velocidade. Use um valor negativo para queda.")]
    [SerializeField] private float gravity = -20f;
    [Tooltip("Velocidade maxima de queda vertical.")]
    [Min(2f)] [SerializeField] private float terminalFallSpeed = 30f;
    [Tooltip("Velocidade de giro ao mirar ou andar.")]
    [Min(0f)] [SerializeField] private float rotationSpeed = 240f;
    [Tooltip("Ao perseguir, gira antes de avancar; ainda permite strafe e recuo no reposicionamento.")]
    [SerializeField] private bool useTankControls;

    [Header("Launch Up")]
    [Tooltip("Permite que ataques com Launch Up levantem este inimigo.")]
    [SerializeField] private bool canBeLaunchedUp = true;
    [Tooltip("Permite que ataques com Launch Down lancem este inimigo para o chao enquanto ele estiver no ar.")]
    [SerializeField] private bool canBeLaunchedDown = true;
    [Tooltip("Permite que ataques com Launch Forwards lancem este inimigo horizontalmente.")]
    [SerializeField] private bool canBeLaunchedForwards = true;
    [Tooltip("Permite que ataques de Juggle alterem o movimento vertical depois de Launch Up.")]
    [SerializeField] private bool canBeJuggled = true;

    [Header("Wall Splat")]
    [Tooltip("Velocidade horizontal minima ao colidir com parede para ativar Wall Splat.")]
    [Min(0f)] [SerializeField] private float wallSplatMinimumSpeed = 10f;
    [Tooltip("Dano de Wall Splat causado para cada unidade de velocidade de impacto contra a parede.")]
    [Min(0f)] [SerializeField] private float wallSplatDamagePerImpactSpeed = 2f;
    [Tooltip("Tempo de Stagger aplicado no Wall Splat.")]
    [Min(0f)] [SerializeField] private float wallSplatStunDuration = 1f;
    [Tooltip("Camadas do cenario que contam como parede para Wall Splat.")]
    [SerializeField] private LayerMask wallSplatLayers = ~0;

    [Header("Floor Splat")]
    [Tooltip("Velocidade vertical minima de queda para ativar Floor Splat ao atingir o chao durante um Launch ou Dive.")]
    [Min(0f)] [SerializeField] private float floorSplatMinimumSpeed = 12f;
    [Tooltip("Dano de Floor Splat causado para cada unidade de velocidade vertical de impacto no chao.")]
    [Min(0f)] [SerializeField] private float floorSplatDamagePerImpactSpeed = 2f;
    [Tooltip("Tempo de Stagger aplicado no Floor Splat.")]
    [Min(0f)] [SerializeField] private float floorSplatStunDuration = 1f;

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyHealth health;
    private EnemyAttackController attacks;
    private CharacterController controller;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private Vector3 slideLaunchHorizontalVelocity;
    private bool hasSlideLaunchHorizontalImpulse;
    private Vector3 playerHeadSeparationVelocity;
    private float playerHeadSeparationTimer;
    private float verticalVelocity;
    private float launchGravity;
    private float juggleTimer;
    private float juggleGravityMultiplier = 1f;
    private Vector3 launchForwardsVelocity;
    private float launchForwardsDeceleration;
    private Vector3 lastMoveVelocity;
    private bool isLaunched;
    private bool isLaunchedForwards;

    public float MovementSpeed => movementSpeed;
    public bool CanBeLaunchedUp => canBeLaunchedUp;
    public bool CanBeLaunchedDown => canBeLaunchedDown;
    public bool CanBeLaunchedForwards => canBeLaunchedForwards;
    public bool CanBeJuggled => canBeJuggled;
    public bool IsLaunched => isLaunched;
    public bool IsLaunchedForwards => isLaunchedForwards;
    /// <summary>Indica se o Character Controller esta apoiado no chao.</summary>
    public bool IsGrounded => controller != null && controller.isGrounded;
    public float VerticalVelocity => verticalVelocity;

    internal void Initialize(EnemyBase enemy, EnemyHealth enemyHealth)
    {
        owner = enemy;
        health = enemyHealth;
        brain = GetComponent<EnemyBrain>();
        attacks = GetComponent<EnemyAttackController>();
        controller = GetComponent<CharacterController>();
    }

    private void OnValidate()
    {
        if (gravity > 0f)
            gravity = -gravity;
    }

    public void Move(Vector3 horizontalVelocity,
        bool rotateTankControlsTowardMovement = true)
    {
        if (controller == null)
            return;
        bool usesRequestedMovement = !isLaunchedForwards &&
            playerHeadSeparationTimer <= 0f && !hasSlideLaunchHorizontalImpulse &&
            knockbackTimer <= 0f;
        if (usesRequestedMovement && horizontalVelocity.sqrMagnitude > .001f &&
            useTankControls && rotateTankControlsTowardMovement)
            FaceDirection(horizontalVelocity);

        Vector3 velocity = isLaunchedForwards ? launchForwardsVelocity :
            playerHeadSeparationTimer > 0f ? playerHeadSeparationVelocity :
            hasSlideLaunchHorizontalImpulse ? slideLaunchHorizontalVelocity :
            knockbackTimer > 0f ? knockbackVelocity : horizontalVelocity;

        playerHeadSeparationTimer = Mathf.Max(0f, playerHeadSeparationTimer - Time.deltaTime);
        knockbackTimer = Mathf.Max(0f, knockbackTimer - Time.deltaTime);
        velocity.y = verticalVelocity;
        lastMoveVelocity = velocity;
        CollisionFlags flags = controller.Move(velocity * Time.deltaTime);

        if (isLaunchedForwards)
        {
            launchForwardsVelocity = Vector3.MoveTowards(
                launchForwardsVelocity,
                Vector3.zero,
                launchForwardsDeceleration * Time.deltaTime
            );
            if (launchForwardsVelocity.sqrMagnitude <= .0001f)
                ResetLaunchForwards();
        }
        if (isLaunched && verticalVelocity <= 0f &&
            (flags & CollisionFlags.Below) != 0)
        {
            float landingSpeed = Mathf.Max(0f, -verticalVelocity);
            bool floorSplatted = TryApplyFloorSplat(landingSpeed);
            isLaunched = false;
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

    public void ApplyGravity()
    {
        if (controller == null) return;
        if (!isLaunched && controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        if (juggleTimer > 0f)
        {
            juggleTimer = Mathf.Max(0f, juggleTimer - Time.deltaTime);
            if (juggleTimer <= 0f)
                juggleGravityMultiplier = 1f;
        }
        float activeGravity = isLaunched ? launchGravity : gravity;
        verticalVelocity = Mathf.Max(verticalVelocity +
            activeGravity * juggleGravityMultiplier * Time.deltaTime,
            -terminalFallSpeed);
    }

    public void SetVerticalVelocity(float value) => verticalVelocity = value;

    internal void StopForTaunt()
    {
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        playerHeadSeparationVelocity = Vector3.zero;
        playerHeadSeparationTimer = 0f;
        CancelSlideLaunchHorizontalImpulse();
    }

    public void FaceTarget()
    {
        if (useTankControls || brain.Target == null) return;
        Vector3 direction = brain.Target.position - transform.position;
        direction.y = 0f;
        FaceDirection(direction);
    }

    public void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= .001f) return;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
    }

    public void ApplyKnockback(Vector3 attackerPosition, float force, float duration)
    {
        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;
        knockbackVelocity = direction.sqrMagnitude > .001f
            ? direction.normalized * force : Vector3.zero;
        knockbackTimer = duration;
    }

    public void ApplyLaunchUp(float initialSpeed, float newGravity)
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
        isLaunched = true;
        ResetJuggleMovement();
        CancelSlideLaunchHorizontalImpulse();
    }

    public void ApplyLaunchUpHorizontalImpulse(Vector3 direction, float speed)
    {
        if (!isLaunched || owner.IsDead || speed <= 0f) return;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .0001f) return;
        slideLaunchHorizontalVelocity = direction.normalized * speed;
        hasSlideLaunchHorizontalImpulse = true;
    }

    public void CancelSlideLaunchHorizontalImpulse()
    {
        slideLaunchHorizontalVelocity = Vector3.zero;
        hasSlideLaunchHorizontalImpulse = false;
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
        CancelSlideLaunchHorizontalImpulse();
    }

    public void ApplyAerialJuggleLight(float liftSpeed, float newGravity,
        float gravityMultiplier, float duration)
    {
        if (!canBeJuggled || IsGrounded || owner.IsDead) return;
        attacks.CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        isLaunched = true;
        verticalVelocity = liftSpeed;
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
        ResetJuggleMovement();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (owner == null || hit == null || hit.collider == null) return;
        PlayerMovement player = hit.collider.GetComponentInParent<PlayerMovement>();
        if (player != null && IsLandingOnPlayerTop(player))
        {
            SeparateFromPlayerHead(player);
            return;
        }
        if (Mathf.Abs(hit.normal.y) > .5f) return;
        Collider obstacle = hit.collider;
        if (obstacle.GetComponentInParent<EnemyBase>() != null ||
            obstacle.GetComponentInParent<PlayerMovement>() != null)
            return;
        if ((wallSplatLayers.value & (1 << obstacle.gameObject.layer)) == 0)
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
        if (impactSpeed < wallSplatMinimumSpeed)
            return;
        ApplyWallSplat(impactSpeed, horizontalVelocity);
    }

    private bool IsLandingOnPlayerTop(PlayerMovement player)
    {
        if (controller == null || verticalVelocity > 0f) return false;
        CharacterController playerController =
            player.GetComponent<CharacterController>();
        if (playerController == null) return false;
        return controller.bounds.min.y >= playerController.bounds.max.y - .15f;
    }

    private void SeparateFromPlayerHead(PlayerMovement player)
    {
        Vector3 direction = transform.position - player.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .0001f)
            direction = transform.right;
        playerHeadSeparationVelocity = direction.normalized * player.enemyHeadSeparationSpeed;
        playerHeadSeparationTimer = .1f;
        verticalVelocity = Mathf.Min(verticalVelocity, -2f);
    }

    private bool TryApplyFloorSplat(float impactSpeed)
    {
        if (impactSpeed < floorSplatMinimumSpeed || health == null || owner.IsDead)
            return false;

        // A queda e vertical, portanto usamos a frente atual para dar uma
        // direcao horizontal legivel ao shake de camera.
        ApplySplat(impactSpeed, floorSplatDamagePerImpactSpeed,
            floorSplatStunDuration, transform.forward);
        return true;
    }

    private void ApplyWallSplat(float impactSpeed, Vector3 impactVelocity)
    {
        ApplySplat(impactSpeed, wallSplatDamagePerImpactSpeed,
            wallSplatStunDuration, impactVelocity);
    }

    private void ApplySplat(float impactSpeed, float damagePerImpactSpeed,
        float stunDuration, Vector3 impactDirection)
    {
        ResetLaunchForwards();
        CancelSlideLaunchHorizontalImpulse();
        attacks.CancelAttack();
        brain.CancelReposition();
        int damage = Mathf.Max(0, Mathf.RoundToInt(impactSpeed *
            Mathf.Max(0f, damagePerImpactSpeed)));
        int damageDealt = health.ApplySplatDamage(damage, impactDirection);
        if (owner.IsDead)
            return;

        health.ApplySplatStagger(stunDuration);
        CombatImpactSystem.ReportHeavyEnvironmentalImpact(transform,
            impactDirection, damageDealt);
    }

    private void ResetJuggleMovement()
    {
        juggleTimer = 0f;
        juggleGravityMultiplier = 1f;
    }

    private void ResetLaunchForwards()
    {
        launchForwardsVelocity = Vector3.zero;
        launchForwardsDeceleration = 0f;
        isLaunchedForwards = false;
    }
}

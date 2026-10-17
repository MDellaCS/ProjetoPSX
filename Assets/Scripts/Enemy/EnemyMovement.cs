using UnityEngine;

/// <summary>Locomocao, gravidade, Launch Up, Juggle, Push e colisao.</summary>
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
    [SerializeField] private bool canBeLaunched = true;
    [Tooltip("Permite que ataques de Juggle alterem o movimento vertical depois de Launch Up.")]
    [SerializeField] private bool canBeJuggled = true;

    [Header("Wall Splat")]
    [Tooltip("Velocidade horizontal minima ao colidir com parede para ativar Wall Splat.")]
    [Min(0f)] [SerializeField] private float wallSplatMinimumSpeed = 10f;
    [Tooltip("Tempo de Stagger aplicado no Wall Splat.")]
    [Min(0f)] [SerializeField] private float wallSplatStunDuration = 1f;
    [Tooltip("Camadas do cenario que contam como parede para Wall Splat.")]
    [SerializeField] private LayerMask wallSplatLayers = ~0;

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyHealth health;
    private EnemyAttackController attacks;
    private CharacterController controller;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private Vector3 slideLaunchHorizontalVelocity;
    private bool hasSlideLaunchHorizontalImpulse;
    private Vector3 playerHeadPushVelocity;
    private float playerHeadPushTimer;
    private float verticalVelocity;
    private float launchGravity;
    private float juggleTimer;
    private float juggleGravityMultiplier = 1f;
    private Vector3 horizontalPushVelocity;
    private float horizontalPushTimer;
    private Vector3 lastMoveVelocity;
    private bool isLaunched;
    private bool isPushed;

    public float MovementSpeed => movementSpeed;
    public bool CanBeLaunched => canBeLaunched;
    public bool IsLaunched => isLaunched;
    public bool IsPushed => isPushed;
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
        bool usesRequestedMovement = !isPushed &&
            playerHeadPushTimer <= 0f && !hasSlideLaunchHorizontalImpulse &&
            knockbackTimer <= 0f;
        if (usesRequestedMovement && horizontalVelocity.sqrMagnitude > .001f &&
            useTankControls && rotateTankControlsTowardMovement)
            FaceDirection(horizontalVelocity);

        Vector3 velocity = isPushed ? horizontalPushVelocity :
            playerHeadPushTimer > 0f ? playerHeadPushVelocity :
            hasSlideLaunchHorizontalImpulse ? slideLaunchHorizontalVelocity :
            knockbackTimer > 0f ? knockbackVelocity : horizontalVelocity;

        playerHeadPushTimer = Mathf.Max(0f, playerHeadPushTimer - Time.deltaTime);
        knockbackTimer = Mathf.Max(0f, knockbackTimer - Time.deltaTime);
        velocity.y = verticalVelocity;
        lastMoveVelocity = velocity;
        CollisionFlags flags = controller.Move(velocity * Time.deltaTime);

        if (isPushed)
        {
            horizontalPushTimer -= Time.deltaTime;
            if (horizontalPushTimer <= 0f)
                ResetHorizontalPush();
        }
        if (isLaunched && verticalVelocity <= 0f &&
            (flags & CollisionFlags.Below) != 0)
        {
            isLaunched = false;
            verticalVelocity = -2f;
            ResetJuggleMovement();
            CancelSlideLaunchHorizontalImpulse();
            owner.CurrentState = EnemyBase.State.Combat;
            brain.DecideImmediately();
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
        if (!canBeLaunched || owner.IsDead) return;
        attacks.CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        brain.CancelReposition();
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        ResetHorizontalPush();
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

    public void ApplyPush(Vector3 attackerPosition, float force, float duration)
    {
        if (!canBeLaunched || owner.IsDead || force <= 0f || duration <= 0f)
            return;
        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f)
            direction = transform.forward;
        attacks.CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        brain.CancelReposition();
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        horizontalPushVelocity = direction.normalized * force;
        horizontalPushTimer = duration;
        isPushed = true;
        CancelSlideLaunchHorizontalImpulse();
    }

    public void ApplyAerialJuggleLight(float liftSpeed, float newGravity,
        float gravityMultiplier, float duration)
    {
        if (!canBeJuggled || !isLaunched || owner.IsDead) return;
        attacks.CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        verticalVelocity = liftSpeed;
        launchGravity = newGravity;
        juggleGravityMultiplier = Mathf.Clamp01(gravityMultiplier);
        juggleTimer = Mathf.Max(juggleTimer, duration);
    }

    public void ApplyAerialDive(float fallSpeed)
    {
        if (!canBeJuggled || !isLaunched || owner.IsDead) return;
        attacks.CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        verticalVelocity = -Mathf.Max(0f, fallSpeed);
        ResetJuggleMovement();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (owner == null || hit == null || hit.collider == null) return;
        PlayerMovement player = hit.collider.GetComponentInParent<PlayerMovement>();
        if (player != null && IsLandingOnPlayerTop(player))
        {
            PushOffPlayerHead(player);
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
        if (horizontalVelocity.magnitude < wallSplatMinimumSpeed)
            return;
        ApplyWallSplat();
    }

    private bool IsLandingOnPlayerTop(PlayerMovement player)
    {
        if (controller == null || verticalVelocity > 0f) return false;
        CharacterController playerController =
            player.GetComponent<CharacterController>();
        if (playerController == null) return false;
        return controller.bounds.min.y >= playerController.bounds.max.y - .15f;
    }

    private void PushOffPlayerHead(PlayerMovement player)
    {
        Vector3 direction = transform.position - player.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .0001f)
            direction = transform.right;
        playerHeadPushVelocity = direction.normalized * player.enemyHeadPushSpeed;
        playerHeadPushTimer = .1f;
        verticalVelocity = Mathf.Min(verticalVelocity, -2f);
    }

    private void ApplyWallSplat()
    {
        ResetHorizontalPush();
        CancelSlideLaunchHorizontalImpulse();
        attacks.CancelAttack();
        brain.CancelReposition();
        health.ApplyWallSplatStagger(wallSplatStunDuration);
    }

    private void ResetJuggleMovement()
    {
        juggleTimer = 0f;
        juggleGravityMultiplier = 1f;
    }

    private void ResetHorizontalPush()
    {
        horizontalPushVelocity = Vector3.zero;
        horizontalPushTimer = 0f;
        isPushed = false;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Walking")]
    [Min(0f)] public float speed = 5f;
    [Min(0f)] public float acceleration = 50f;
    [Min(0f)] public float deceleration = 65f;
    [Min(0f)] public float rotationSpeed = 10f;

    [Header("Gravity")]
    public float gravity = -40f;

    [Header("Jump")]
    [Min(0f)] public float jumpInitialSpeed = 8f;
    [Min(0f)] public float jumpHoldAcceleration = 25f;
    [Min(0f)] public float maxJumpHoldDuration = 0.15f;

    [Header("Air Movement")]
    [Range(0f, 1f)] public float airControlMultiplier = 0.75f;
    [Min(0f)] public float airDrag = 10f;

    [Header("Dash")]
    [Min(0f)] public float dashSpeedMultiplier = 2f;
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

    private CharacterController controller;
    private PlayerState playerState;

    private Vector3 horizontalVelocity;
    private Vector3 knockbackVelocity;
    private Vector3 dashDirection;

    private float verticalVelocity;
    private float jumpHoldTimer;
    private float knockbackTimer;
    private float dashTimer;
    private float dashCooldownTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerState = GetComponent<PlayerState>();
    }

    void Update()
    {
        DesiredDirection = ReadMovementDirection();

        UpdateDashCooldown();
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

        if (keyboard == null ||
            !keyboard.leftShiftKey.wasPressedThisFrame ||
            !playerState.IsNormal() ||
            dashCooldownTimer > 0f ||
            DesiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        playerState.ChangeState(PlayerState.State.Dashing);
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = DesiredDirection;

        // O dash substitui a velocidade anterior imediatamente.
        horizontalVelocity = dashDirection * GetDashSpeed();
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

        if (keyboard == null ||
            !keyboard.spaceKey.wasPressedThisFrame ||
            !controller.isGrounded ||
            !playerState.IsNormal())
        {
            return;
        }

        verticalVelocity = jumpInitialSpeed;
        jumpHoldTimer = maxJumpHoldDuration;
    }

    private void UpdateVerticalVelocity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            jumpHoldTimer = 0f;
        }

        ApplyJumpHold();

        verticalVelocity += gravity * Time.deltaTime;
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
        if (playerState.IsBlocking() || playerState.IsGuardBroken())
        {
            knockbackTimer = 0f;
            return Vector3.zero;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            return knockbackVelocity;
        }

        if (playerState.IsStaggered())
        {
            return Vector3.zero;
        }

        if (playerState.IsDashing())
        {
            if (UpdateDash())
            {
                return dashDirection * GetDashSpeed();
            }
        }

        if (!playerState.IsNormal())
        {
            return Vector3.zero;
        }

        return CalculateWalkVelocity();
    }

    private Vector3 CalculateWalkVelocity()
    {
        if (!IsGrounded || verticalVelocity > 0f)
        {
            return CalculateAirVelocity();
        }

        Vector3 targetVelocity = DesiredDirection * speed;
        float rate = DesiredDirection.sqrMagnitude > 0.0001f
            ? acceleration
            : deceleration;

        return Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            rate * Time.deltaTime
        );
    }

    private Vector3 CalculateAirVelocity()
    {
        Vector3 targetVelocity =
            DesiredDirection * speed * airControlMultiplier;

        return Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            airDrag * Time.deltaTime
        );
    }

    private bool UpdateDash()
    {
        dashTimer -= Time.deltaTime;

        if (dashTimer > 0f)
            return true;

        playerState.ChangeState(PlayerState.State.Normal);

        return false;
    }

    private float GetDashSpeed()
    {
        return speed * dashSpeedMultiplier;
    }

    private void RotateTowardsMovement()
    {
        if (!playerState.IsNormal() ||
            DesiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

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
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
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
}

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Walking")]
    [Min(0f)] public float speed = 5f;
    [Min(0f)] public float acceleration = 30f;
    [Min(0f)] public float deceleration = 40f;
    [Min(0f)] public float rotationSpeed = 10f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Dash")]
    [Min(0f)] public float dashSpeedMultiplier = 3f;
    [Min(0f)] public float dashDuration = 0.5f;

    [Header("References")]
    public Transform cameraTransform;

    public Vector3 DesiredDirection { get; private set; }

    private CharacterController controller;
    private PlayerState playerState;

    private Vector3 horizontalVelocity;
    private Vector3 knockbackVelocity;
    private Vector3 dashDirection;

    private float verticalVelocity;
    private float knockbackTimer;
    private float dashTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerState = GetComponent<PlayerState>();
    }

    void Update()
    {
        DesiredDirection = ReadMovementDirection();

        TryStartDash();
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
            DesiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        playerState.ChangeState(PlayerState.State.Dashing);
        dashTimer = dashDuration;
        dashDirection = DesiredDirection;

        // O dash substitui a velocidade anterior imediatamente.
        horizontalVelocity = dashDirection * GetDashSpeed();
    }

    private void UpdateVerticalVelocity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
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

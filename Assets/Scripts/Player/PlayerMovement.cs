using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float gravity = -20f;
    public float rotationSpeed = 10f;
    public float dodgeSpeed = 15f;
    public float dodgeDuration = 0.5f;

    private bool isDodging;
    private float dodgeTimer;
    private Vector3 dodgeDirection;

    public Transform cameraTransform;

    private CharacterController controller;
    private PlayerHealth playerHealth;
    private float verticalVelocity;
    private Renderer playerRenderer;
    private Color normalColor;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        playerRenderer = GetComponent<Renderer>();
        normalColor = playerRenderer.material.color;
    }

    void Update()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 direction = (cameraForward * input.y + cameraRight * input.x).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isDodging)
        {
            isDodging = true;
            dodgeTimer = dodgeDuration;

            playerHealth.SetInvulnerable(true);

            playerRenderer.material.color = Color.green;

            if (direction != Vector3.zero)
            {
                dodgeDirection = direction;
            }
            else
            {
                dodgeDirection = transform.forward;
            }
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity;

        if (isDodging)
        {
            velocity = dodgeDirection * dodgeSpeed;

            dodgeTimer -= Time.deltaTime;

            if (dodgeTimer <= 0f)
            {
                isDodging = false;
                playerHealth.SetInvulnerable(false);
                playerRenderer.material.color = normalColor;
            }
        }
        else
        {
            velocity = direction * speed;
        }
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}
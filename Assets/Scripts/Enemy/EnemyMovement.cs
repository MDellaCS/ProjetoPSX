using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 3f;
    public float stoppingDistance = 1.5f;
    public float gravity = -20f;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    private float verticalVelocity;
    private float launchGravityMagnitude;
    private bool isLaunched;

    public Transform target;
    private CharacterController controller;
    private EnemyCombat enemyCombat;

    public bool IsLaunched => isLaunched;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        enemyCombat = GetComponent<EnemyCombat>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target = player.transform;
        }
    }

    void Update()
    {
        // Gravidade
        if (!isLaunched && controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        float currentGravity = isLaunched
            ? -launchGravityMagnitude
            : gravity;
        verticalVelocity += currentGravity * Time.deltaTime;

        // Movimento horizontal
        Vector3 horizontalVelocity = Vector3.zero;

        if (!isLaunched && target != null &&
            enemyCombat.currentState == EnemyCombat.EnemyState.Chasing)
        {
            Vector3 direction = target.position - transform.position;

            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance > 0.01f)
            {
                direction.Normalize();

                // Continua olhando para o jogador, mesmo quando está parado
                transform.rotation = Quaternion.LookRotation(direction);

                // Só se movimenta se estiver longe
                if (distance > stoppingDistance)
                {
                    horizontalVelocity = direction * speed;
                }
            }
        }

        if (!isLaunched && enemyCombat.IsCharging)
        {
            horizontalVelocity = transform.forward * enemyCombat.ChargeSpeed;
        }

        Vector3 velocity;

        if (!isLaunched && knockbackTimer > 0f)
        {
            velocity = knockbackVelocity;
            knockbackTimer -= Time.deltaTime;
        }
        else
        {
            velocity = horizontalVelocity;
        }

        velocity.y = verticalVelocity;

        CollisionFlags collisionFlags = controller.Move(
            velocity * Time.deltaTime
        );

        if (isLaunched && verticalVelocity <= 0f &&
            (collisionFlags & CollisionFlags.Below) != 0)
        {
            isLaunched = false;
            verticalVelocity = -2f;
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

    public void ApplyLaunchUp(float initialSpeed, float gravityMagnitude)
    {
        knockbackTimer = 0f;
        verticalVelocity = initialSpeed;
        launchGravityMagnitude = gravityMagnitude;
        isLaunched = true;
    }
}

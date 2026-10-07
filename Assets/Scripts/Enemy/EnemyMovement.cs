using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 3f;
    public float stoppingDistance = 1.5f;
    public float gravity = -20f;

    private float verticalVelocity;

    public Transform target;
    private CharacterController controller;
    private EnemyCombat enemyCombat;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        enemyCombat = GetComponent<EnemyCombat>();
    }

    void Update()
    {
        // Gravidade
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        // Movimento horizontal
        Vector3 horizontalVelocity = Vector3.zero;

        if (target != null && enemyCombat.currentState == EnemyCombat.EnemyState.Chasing)
        {
            Vector3 direction = target.position - transform.position;

            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance > stoppingDistance)
            {
                direction.Normalize();

                horizontalVelocity = direction * speed;

                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Junta movimento horizontal + vertical
        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}
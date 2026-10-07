using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chasing,
        Windup,
        Attacking,
        Recovery,
        Staggered
    }

    public EnemyState currentState = EnemyState.Idle;

    public Transform target;

    public int attackDamage = 20;
    public float attackKnockbackForce = 8f;
    public float attackKnockbackDuration = 0.2f;

    // Distâncias da IA
    public float attackRange = 2f;
    public float detectionRange = 8f;

    // Hitbox do ataque
    public float hitboxDistance = 1f;
    public float hitboxRadius = 1f;

    // Tempos dos estados
    public float windupDuration = 0.5f;
    public float recoveryDuration = 0.8f;
    public float attackDuration = 0.2f;
    public float staggerDuration = 0.8f;

    private float stateTimer;
    private bool hasDealtDamage;

    private Renderer enemyRenderer;
    private Color normalColor;

    void Start()
    {
        enemyRenderer = GetComponent<Renderer>();
        normalColor = enemyRenderer.material.color;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target = player.transform;
        }
    }

    void Update()
    {
        if (target == null)
            return;

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;

            case EnemyState.Chasing:
                UpdateChasing();
                break;

            case EnemyState.Windup:
                UpdateWindup();
                break;

            case EnemyState.Attacking:
                UpdateAttacking();
                break;

            case EnemyState.Recovery:
                UpdateRecovery();
                break;

            case EnemyState.Staggered:
                UpdateStaggered();
                break;
        }
    }

    void UpdateIdle()
    {
        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance <= detectionRange)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    void UpdateChasing()
    {
        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance <= attackRange)
        {
            ChangeState(EnemyState.Windup);
        }
        else if (distance > detectionRange)
        {
            ChangeState(EnemyState.Idle);
        }
    }

    void UpdateWindup()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ChangeState(EnemyState.Attacking);
        }
    }

    void UpdateAttacking()
    {
        Attack();

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ChangeState(EnemyState.Recovery);
        }
    }

    void UpdateRecovery()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    void UpdateStaggered()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            enemyRenderer.material.color = normalColor;
            ChangeState(EnemyState.Chasing);
        }
    }

    void ChangeState(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Windup)
        {
            stateTimer = windupDuration;
            enemyRenderer.material.color = Color.magenta;
        }

        if (newState == EnemyState.Attacking)
        {
            stateTimer = attackDuration;
            hasDealtDamage = false;
        }

        if (newState == EnemyState.Recovery)
        {
            stateTimer = recoveryDuration;
            enemyRenderer.material.color = normalColor;
        }

        if (newState == EnemyState.Staggered)
        {
            stateTimer = staggerDuration;
            enemyRenderer.material.color = Color.yellow;
        }
    }

    void Attack()
    {
        if (hasDealtDamage)
            return;

        Vector3 hitboxPosition =
            transform.position + transform.forward * hitboxDistance;

        Collider[] hits = Physics.OverlapSphere(
            hitboxPosition,
            hitboxRadius
        );

        foreach (Collider hit in hits)
        {
            PlayerHealth player = hit.GetComponent<PlayerHealth>();

            if (player != null)
            {
                player.TakeDamage(
                    attackDamage,
                    transform.position,
                    attackKnockbackForce,
                    attackKnockbackDuration
                );
                hasDealtDamage = true;
                break;
            }
        }
    }

    void OnDrawGizmos()
    {
        // Área de detecção
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        // Área que inicia o ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        // Hitbox real do golpe
        if (currentState == EnemyState.Attacking)
        {
            Vector3 hitboxPosition =
                transform.position + transform.forward * hitboxDistance;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(
                hitboxPosition,
                hitboxRadius
            );
        }
    }

    public void ApplyStagger()
    {
        ChangeState(EnemyState.Staggered);
    }
}
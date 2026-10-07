using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chasing,
        Windup,
        Attacking,
        Recovery
    }

    public EnemyState currentState = EnemyState.Idle;

    public Transform target;

    public int attackDamage = 20;
    public float attackRange = 2f;
    public float detectionRange = 8f;
    public float windupDuration = 0.5f;
    public float recoveryDuration = 0.8f;

    private float stateTimer;

    private Renderer enemyRenderer;
    private Color normalColor;

    void Start()
    {
        enemyRenderer = GetComponent<Renderer>();
        normalColor = enemyRenderer.material.color;
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
        }
    }

    void UpdateIdle()
    {
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= detectionRange)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    void UpdateChasing()
    {
        float distance = Vector3.Distance(transform.position, target.position);

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

        ChangeState(EnemyState.Recovery);
    }

    void UpdateRecovery()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
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

        if (newState == EnemyState.Recovery)
        {
            stateTimer = recoveryDuration;

            enemyRenderer.material.color = normalColor;
        }
    }

    void Attack()
    {
        PlayerHealth player = target.GetComponent<PlayerHealth>();

        if (player != null)
        {
            player.TakeDamage(attackDamage);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
using UnityEngine;
using System.Collections.Generic;

public class EnemyCombat : MonoBehaviour
{
    private static readonly HashSet<EnemyCombat> activeEnemies =
        new HashSet<EnemyCombat>();

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

    [Header("Stagger Knockback")]
    [Min(0f)]
    public float staggerKnockbackForce = 8f;

    [Min(0f)]
    public float staggerKnockbackDuration = 0.2f;

    private EnemyMovement enemyMovement;

    [Header("Attack Tracking")]
    [Min(0f)]
    public float windupRotationSpeed = 180f;

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

    void OnEnable()
    {
        activeEnemies.Add(this);
    }

    void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    public static bool IsAnyEnemyAggroedOn(Transform player)
    {
        foreach (EnemyCombat enemy in activeEnemies)
        {
            if (enemy != null &&
                enemy.target == player &&
                enemy.currentState != EnemyState.Idle)
            {
                return true;
            }
        }

        return false;
    }

    void Start()
    {
        enemyRenderer = GetComponent<Renderer>();
        enemyMovement = GetComponent<EnemyMovement>();
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
        // Acompanha o jogador durante a preparação
        RotateTowardsTarget();

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

    void RotateTowardsTarget()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;

        // Ignora diferenças de altura
        direction.y = 0f;

        // Evita tentar girar com uma direção nula
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            windupRotationSpeed * Time.deltaTime
        );
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
                    attackKnockbackDuration,
                    causedByEnemy: true
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

    public void ApplyStagger(Vector3 attackerPosition)
    {
        ChangeState(EnemyState.Staggered);

        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        enemyMovement.ApplyKnockback(
            direction,
            staggerKnockbackForce,
            staggerKnockbackDuration
        );
    }
}

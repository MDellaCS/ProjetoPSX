using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;


public class PlayerCombat : MonoBehaviour
{
    public int attackDamage = 25;

    public float attackDistance = 1.5f;
    public float attackRadius = 1f;
    public float attackDuration = 0.2f;

    private bool isAttacking;
    private HashSet<EnemyHealth> enemiesHit = new HashSet<EnemyHealth>();
    private float attackTimer;
    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isAttacking && !playerHealth.IsStaggered)
        {
            StartAttack();
        }

        if (isAttacking)
        {
            UpdateAttack();
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        enemiesHit.Clear();
        attackTimer = attackDuration;
    }

    void UpdateAttack()
    {
        Attack();

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            isAttacking = false;
        }
    }

    public void CancelAttack()
    {
        isAttacking = false;
        enemiesHit.Clear();
    }

    void Attack()
    {
        Vector3 attackPosition =
            transform.position + transform.forward * attackDistance;

        Collider[] hits = Physics.OverlapSphere(
            attackPosition,
            attackRadius
        );

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

            if (enemy != null && !enemiesHit.Contains(enemy))
            {
                enemy.TakeDamage(
                    attackDamage,
                    transform.position
                );

                enemiesHit.Add(enemy);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!isAttacking)
            return;

        Vector3 attackPosition = transform.position + transform.forward * attackDistance;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            attackPosition,
            attackRadius
        );
    }
}
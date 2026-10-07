using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    public int attackDamage = 25;
    public float attackRange = 1.5f;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Attack();
        }
    }

    void Attack()
    {
        Vector3 attackPosition = transform.position + transform.forward * attackRange;

        Collider[] hits = Physics.OverlapSphere(
            attackPosition,
            attackRange
        );

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

            enemy?.TakeDamage(attackDamage);
        }
    }
}
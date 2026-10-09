using UnityEngine;

/// <summary>
/// Inicia um EnemyEncounter uma unica vez quando o Player entra no trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EncounterTrigger : MonoBehaviour
{
    [SerializeField] private EnemyEncounter encounter;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        encounter = GetComponentInParent<EnemyEncounter>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform player = FindPlayerTransform(other);
        if (player != null)
            encounter?.TryStartEncounter(player);
    }

    private static Transform FindPlayerTransform(Collider other)
    {
        PlayerMovement playerMovement =
            other.GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
            return playerMovement.transform;

        return other.CompareTag("Player")
            ? other.transform
            : null;
    }

    private void OnDrawGizmos()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger == null)
            return;

        Gizmos.color = GetGizmoColor();

        switch (trigger)
        {
            case BoxCollider box:
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
                break;

            case SphereCollider sphere:
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                Gizmos.matrix = Matrix4x4.identity;
                break;

            default:
                Gizmos.DrawWireCube(
                    trigger.bounds.center,
                    trigger.bounds.size
                );
                break;
        }
    }

    private Color GetGizmoColor()
    {
        if (encounter == null)
            return Color.yellow;

        switch (encounter.CurrentState)
        {
            case EnemyEncounter.EncounterState.Active:
                return Color.red;

            case EnemyEncounter.EncounterState.Completed:
                return Color.green;

            default:
                return Color.cyan;
        }
    }
}

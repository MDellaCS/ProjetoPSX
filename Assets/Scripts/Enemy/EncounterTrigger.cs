using UnityEngine;

/// <summary>
/// Inicia um EnemyEncounter uma unica vez quando o Player entra no trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EncounterTrigger : MonoBehaviour
{
    [Tooltip("Encontro iniciado quando o jogador atravessa este volume. Se vazio, usa o componente EnemyEncounter pai.")]
    [SerializeField] private EnemyEncounter encounter;
    private PlayerMovement playerMovement;
    private Collider triggerCollider;
    private bool playerInside;
    private float nextPlayerLookupTime;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (encounter == null)
            encounter = GetComponentInParent<EnemyEncounter>();
    }

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        encounter = GetComponentInParent<EnemyEncounter>();
    }

    private void Update()
    {
        if (encounter == null || encounter.CurrentState !=
            EnemyEncounter.EncounterState.Inactive)
        {
            enabled = false;
            return;
        }

        if (playerMovement == null && Time.unscaledTime >= nextPlayerLookupTime)
        {
            nextPlayerLookupTime = Time.unscaledTime + .5f;
            playerMovement = FindAnyObjectByType<PlayerMovement>();
        }

        bool insideNow = PlayerAreaOverlap.ContainsPlayer(
            triggerCollider, playerMovement);
        if (!insideNow)
        {
            playerInside = false;
            return;
        }

        if (playerInside)
            return;

        playerInside = true;
        if (playerMovement != null)
            encounter?.TryStartEncounter(playerMovement.transform);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !PlayerDebugToolkitHUD.IsVisible)
            return;

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

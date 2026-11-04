using UnityEngine;

/// <summary>
/// Zona de entrada EXTRA de uma escada: o Collider deste objeto tambem permite
/// agarrar a escada (alem da zona automatica). Use para entradas em lugares
/// incomuns, como uma plataforma ao lado ou um vao no chao. Opcional: a escada
/// funciona sem este componente.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Projeto PSX/Level Design/Hand Ladder Extra Entry Zone")]
public sealed class HandLadderInteractionArea : MonoBehaviour
{
    [Tooltip("Escada que esta zona libera. Vazio usa a HandLadder em um objeto pai.")]
    [SerializeField] private HandLadder ladder;

    private Collider zone;

    private void Reset()
    {
        if (TryGetComponent(out Collider trigger))
            trigger.isTrigger = true;
    }

    private void OnEnable()
    {
        if (ladder == null)
            ladder = GetComponentInParent<HandLadder>();
        zone = GetComponent<Collider>();
        if (zone != null)
            zone.isTrigger = true;
        if (ladder != null)
            ladder.RegisterEntryZone(zone);
    }

    private void OnDisable()
    {
        if (ladder != null)
            ladder.UnregisterEntryZone(zone);
    }

    private void OnDrawGizmos()
    {
        if (InteractionGizmos.Visible)
            InteractionGizmos.DrawCollider(GetComponent<Collider>(),
                InteractionGizmos.ZoneFill, InteractionGizmos.ZoneLine);
    }
}

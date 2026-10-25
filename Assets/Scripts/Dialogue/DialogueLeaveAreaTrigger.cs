using System.Collections.Generic;
using UnityEngine;

/// <summary>Encaminha a saida do jogador da area ampliada ao DialogueInteraction.</summary>
[DisallowMultipleComponent]
public sealed class DialogueLeaveAreaTrigger : MonoBehaviour
{
    private readonly HashSet<Collider> playerColliders = new();
    private DialogueInteraction owner;

    internal void Initialize(DialogueInteraction interaction) => owner = interaction;

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            playerColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerColliders.Remove(other) || playerColliders.Count > 0)
            return;

        owner?.OnLeaveAreaExited();
    }

    private void OnDisable() => playerColliders.Clear();

    private static bool IsPlayer(Collider other) => other != null &&
        (other.GetComponentInParent<PlayerMovement>() != null ||
         other.CompareTag("Player"));
}

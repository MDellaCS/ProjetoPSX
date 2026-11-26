using UnityEngine;

/// <summary>Salva automaticamente no slot de checkpoint isolado ao entrar no trigger.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Projeto PSX/Save/Checkpoint Automatico")]
public sealed class GameSaveCheckpoint : MonoBehaviour
{
    [Tooltip("Nome da area exibido no resumo do checkpoint automatico.")]
    [SerializeField] private string locationName = "Checkpoint";
    [Tooltip("ID do ScenePlayerSpawnPoint usado ao carregar o checkpoint. Vazio usa o spawn padrao da cena.")]
    [SerializeField] private string spawnId;

    private Collider trigger;

    private void Awake()
    {
        trigger = GetComponent<Collider>();
        if (trigger != null) trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;
        GameSaveService.SaveCheckpoint(locationName, spawnId);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !DebugGizmoSettings.Enabled(DebugGizmoGroup.WorldCheckpoints)) return;
        Collider collider = trigger != null ? trigger : GetComponent<Collider>();
        if (collider == null) return;
        Gizmos.color = new Color(.3f, .9f, .95f, .8f);
        InteractionGizmos.DrawCollider(collider,
            new Color(.2f, .8f, .9f, .08f), Gizmos.color);
    }
}

using UnityEngine;

/// <summary>Concede item-chave quando a sequencia associada termina completamente.</summary>
[AddComponentMenu("Projeto PSX/Itens/Recompensa por dialogo")]
public sealed class KeyItemDialogueReward : MonoBehaviour
{
    [Tooltip("Sequencia de dialogo que concede o item quando todas as falas terminam.")]
    [SerializeField] private DialogueSequence sequence;
    [Tooltip("Item-chave adicionado ao inventario ao concluir o dialogo.")]
    [SerializeField] private KeyItemDefinition item;
    [Tooltip("Mostra a modal de obtencao quando o dialogo concede o item.")]
    [SerializeField] private bool showModal = true;

    private void OnEnable() => DialogueController.SequenceCompleted += HandleSequenceCompleted;
    private void OnDisable() => DialogueController.SequenceCompleted -= HandleSequenceCompleted;

    private void HandleSequenceCompleted(DialogueSequence completed)
    {
        if (sequence != null && completed == sequence && item != null)
            KeyItemInventory.Grant(item, showModal);
    }
}

using UnityEngine;

/// <summary>
/// Ponte simples para UnityEvents. Permite que alavancas, encounters e outros
/// sistemas mostrem uma sequencia sem depender do HUD concreto.
/// </summary>
public sealed class DialogueEvent : MonoBehaviour
{
    [Tooltip("Sequencia exibida pelo metodo Show configuravel em UnityEvents.")]
    [SerializeField] private DialogueSequence sequence;

    public void Show()
    {
        DialogueController controller = DialogueController.Instance;
        if (controller != null)
            controller.TryShow(sequence);
    }
}

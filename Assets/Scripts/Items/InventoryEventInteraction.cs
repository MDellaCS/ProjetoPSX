using UnityEngine;
using UnityEngine.Events;

/// <summary>Executa eventos diferentes ao interagir, conforme os itens do inventario.</summary>
[AddComponentMenu("Projeto PSX/Items/Inventory Event Interaction")]
[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionArea))]
public sealed class InventoryEventInteraction : InteractableBase
{
    [Header("Condicao")]
    [Tooltip("Itens que precisam estar no inventario para executar o evento de sucesso.")]
    [SerializeField] private InventoryItemRequirement requirement = new();
    [Tooltip("Remove os itens que satisfizeram a condicao antes de executar o evento de sucesso.")]
    [SerializeField] private bool consumeRequiredItems;

    [Header("Eventos")]
    [Tooltip("Chamado quando a condicao e atendida. Conecte dialogos, estados, portas ou cutscenes.")]
    [SerializeField] private UnityEvent onRequirementMet = new();
    [Tooltip("Chamado ao interagir sem satisfazer a condicao. Use para mostrar uma fala ou mensagem de bloqueio.")]
    [SerializeField] private UnityEvent onRequirementFailed = new();

    public override string InteractionMessage => "Interagir";

    protected override void OnEnable()
    {
        RegisterSaveTargets();
        base.OnEnable();
    }

    internal void RegisterSaveTargets() => GameWorldSaveState.Register(onRequirementMet);

    public override bool Interact(PlayerMovement player)
    {
        RegisterSaveTargets();
        if (requirement == null || !requirement.IsSatisfied ||
            (consumeRequiredItems && !requirement.TryConsumeRequiredItems()))
        {
            onRequirementFailed?.Invoke();
            return false;
        }

        onRequirementMet?.Invoke();
        return true;
    }
}

using UnityEngine;
using UnityEngine.Events;

/// <summary>Reage a mudancas no inventario sem verificacao por frame.</summary>
[AddComponentMenu("Projeto PSX/Items/Inventory Condition Listener")]
[DisallowMultipleComponent]
public sealed class InventoryConditionListener : MonoBehaviour
{
    [Header("Condicao")]
    [Tooltip("Itens que precisam estar no inventario para disparar o evento de condicao atendida.")]
    [SerializeField] private InventoryItemRequirement requirement = new();
    [Tooltip("Remove os itens que satisfizeram a condicao antes de disparar o evento.")]
    [SerializeField] private bool consumeRequiredItems;

    [Header("Disparo")]
    [Tooltip("Executa o evento imediatamente ao habilitar se a condicao ja estiver atendida.")]
    [SerializeField] private bool triggerWhenEnabled = true;
    [Tooltip("Executa o evento de condicao atendida somente uma vez durante a vida deste objeto.")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Eventos")]
    [Tooltip("Chamado quando a condicao passa de falsa para verdadeira ou ja esta verdadeira ao habilitar.")]
    [SerializeField] private UnityEvent onConditionMet = new();
    [Tooltip("Chamado quando a condicao passa de verdadeira para falsa, por exemplo ao consumir a chave.")]
    [SerializeField] private UnityEvent onConditionLost = new();

    private bool conditionWasMet;
    private bool hasInitialState;
    private bool hasTriggered;
    private bool isExecuting;

    private void OnEnable()
    {
        KeyItemInventory.Changed += HandleInventoryChanged;
        conditionWasMet = requirement != null && requirement.IsSatisfied;
        hasInitialState = true;
        if (conditionWasMet && triggerWhenEnabled)
            TriggerConditionMet();
    }

    private void OnDisable()
    {
        KeyItemInventory.Changed -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (isExecuting || requirement == null)
            return;

        bool isMet = requirement.IsSatisfied;
        if (!hasInitialState)
        {
            hasInitialState = true;
            conditionWasMet = isMet;
            if (isMet)
                TriggerConditionMet();
            return;
        }

        if (isMet == conditionWasMet)
            return;

        conditionWasMet = isMet;
        if (isMet)
            TriggerConditionMet();
        else
            onConditionLost?.Invoke();
    }

    private void TriggerConditionMet()
    {
        if (isExecuting || (triggerOnlyOnce && hasTriggered))
            return;

        isExecuting = true;
        if (triggerOnlyOnce)
            hasTriggered = true;

        if (!consumeRequiredItems || requirement.TryConsumeRequiredItems())
            onConditionMet?.Invoke();
        else if (triggerOnlyOnce)
            hasTriggered = false;

        conditionWasMet = requirement.IsSatisfied;
        isExecuting = false;
    }
}

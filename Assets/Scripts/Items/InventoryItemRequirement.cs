using System;
using System.Collections.Generic;
using UnityEngine;

public enum InventoryItemMatchMode
{
    [InspectorName("Todos os itens")]
    All,
    [InspectorName("Qualquer item")]
    Any
}

/// <summary>Regra reutilizavel que compara itens-chave presentes e ausentes.</summary>
[Serializable]
public sealed class InventoryItemRequirement
{
    [Tooltip("Itens exigidos para satisfazer a regra. No modo Qualquer item, basta possuir um deles.")]
    [SerializeField] private List<KeyItemDefinition> requiredItems = new();
    [Tooltip("Define se todos os itens exigidos ou apenas um deles precisam estar no inventario.")]
    [SerializeField] private InventoryItemMatchMode matchMode = InventoryItemMatchMode.All;
    [Tooltip("A regra falha se qualquer um destes itens estiver no inventario.")]
    [SerializeField] private List<KeyItemDefinition> excludedItems = new();

    public bool IsSatisfied
    {
        get
        {
            if (requiredItems == null || requiredItems.Count == 0)
                return false;

            bool foundRequired = false;
            for (int i = 0; i < requiredItems.Count; i++)
            {
                KeyItemDefinition item = requiredItems[i];
                if (item == null)
                {
                    if (matchMode == InventoryItemMatchMode.All)
                        return false;
                    continue;
                }

                bool hasItem = KeyItemInventory.Contains(item);
                foundRequired |= hasItem;
                if (matchMode == InventoryItemMatchMode.All && !hasItem)
                    return false;
            }

            if (matchMode == InventoryItemMatchMode.Any && !foundRequired)
                return false;

            if (excludedItems != null)
            {
                for (int i = 0; i < excludedItems.Count; i++)
                    if (excludedItems[i] != null &&
                        KeyItemInventory.Contains(excludedItems[i]))
                        return false;
            }

            return true;
        }
    }

    /// <summary>Consome os itens que satisfizeram a regra, sem alteracao parcial.</summary>
    public bool TryConsumeRequiredItems()
    {
        if (!IsSatisfied || requiredItems == null)
            return false;

        List<KeyItemDefinition> itemsToConsume = new();
        for (int i = 0; i < requiredItems.Count; i++)
        {
            KeyItemDefinition item = requiredItems[i];
            if (item == null || !KeyItemInventory.Contains(item))
                continue;

            itemsToConsume.Add(item);
            if (matchMode == InventoryItemMatchMode.Any)
                break;
        }

        return KeyItemInventory.TryConsume(itemsToConsume);
    }
}

using System;
using UnityEngine;

/// <summary>Definicao persistente de um item-chave do inventario.</summary>
[CreateAssetMenu(fileName = "KeyItem", menuName = "Projeto PSX/Itens/Item-chave")]
public sealed class KeyItemDefinition : ScriptableObject
{
    [Tooltip("Identificador persistente e unico. Gere uma vez e mantenha igual para preservar saves existentes.")]
    [SerializeField] private string id;
    [Tooltip("Imagem exibida no inventario e na modal de obtencao.")]
    [SerializeField] private Sprite image;
    [Tooltip("Nome mostrado no inventario e na modal de obtencao.")]
    [SerializeField] private string itemName;
    [Tooltip("Descricao narrativa ou instrucao exibida ao inspecionar o item.")]
    [TextArea(3, 8)] [SerializeField] private string description;

    public string Id => id;
    public Sprite Image => image;
    public string ItemName => itemName;
    public string Description => description;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = Guid.NewGuid().ToString("N");
        id = id.Trim();
    }
}

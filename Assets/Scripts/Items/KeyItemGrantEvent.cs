using UnityEngine;

/// <summary>Permite conceder um item por UnityEvent, Timeline ou inicio de cena.</summary>
[AddComponentMenu("Projeto PSX/Itens/Conceder item-chave")]
public sealed class KeyItemGrantEvent : MonoBehaviour
{
    [Tooltip("Item-chave concedido ao chamar Grant. O inventario ignora duplicatas.")]
    [SerializeField] private KeyItemDefinition item;
    [Tooltip("Concede o item automaticamente no Start, util para recompensas no inicio de uma cena.")]
    [SerializeField] private bool grantOnStart;
    [Tooltip("Mostra a modal de obtencao ao conceder o item por evento ou no inicio da cena.")]
    [SerializeField] private bool showModal = true;

    private void Start()
    {
        if (grantOnStart)
            Grant();
    }

    public void Grant()
    {
        if (item != null)
            KeyItemInventory.Grant(item, showModal);
    }
}

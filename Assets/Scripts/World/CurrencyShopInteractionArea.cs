using UnityEngine;

/// <summary>Area separada do collider fisico; abre a loja do terminal. O sistema central de interacao escolhe o terminal mais proximo.</summary>
public sealed class CurrencyShopInteractionArea : InteractionArea
{
    [Tooltip("Terminal fisico que esta area abre. Vazio usa o terminal nos pais.")]
    [SerializeField] private CurrencyShopTerminal terminal;

    public override string InteractionMessage => "Abrir loja";

    public override bool CanInteract(PlayerMovement player)
    {
        ResolveTerminal();
        return terminal != null && !terminal.IsDestroyed && base.CanInteract(player);
    }

    public override bool Interact(PlayerMovement player) =>
        CanInteract(player) && terminal != null && terminal.OpenShop(player.transform);

    private void ResolveTerminal()
    {
        if (terminal == null)
            terminal = GetComponentInParent<CurrencyShopTerminal>();
    }
}

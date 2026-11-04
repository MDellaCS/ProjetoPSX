using UnityEngine;

/// <summary>Area separada do collider fisico; abre a loja do terminal. O sistema central de interacao escolhe o terminal mais proximo.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CurrencyShopInteractionArea : InteractionArea
{
    [Tooltip("Terminal fisico que esta area abre. Vazio usa o terminal nos pais.")]
    [SerializeField] private CurrencyShopTerminal terminal;

    protected override void Awake()
    {
        base.Awake();
        ResolveTerminal();
    }

    public override bool CanInteract(PlayerMovement player)
    {
        ResolveTerminal();
        return terminal != null && !terminal.IsDestroyed && base.CanInteract(player);
    }

    protected override bool OnInteracted(PlayerMovement player) =>
        terminal != null && terminal.OpenShop(player.transform);

    private void ResolveTerminal()
    {
        if (terminal == null)
            terminal = GetComponentInParent<CurrencyShopTerminal>();
    }
}

using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Painel modal simples para gastar currency em Carga.</summary>
[RequireComponent(typeof(PanelRenderer))]
[DisallowMultipleComponent]
public sealed class CurrencyShopHUD : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    [Tooltip("Panel Renderer usado para desenhar a loja acima da interface existente.")]
    [SerializeField] private PanelRenderer panelRenderer;
    private VisualElement overlay;
    private Label title, balance, chargeInfo, feedback;
    private Button purchaseButton;
    private CurrencyShopTerminal terminal;
    private PlayerCurrencyWallet wallet;
    private PlayerChargeUpgrade charge;

    private void Awake()
    {
        panelRenderer ??= GetComponent<PanelRenderer>();
        panelRenderer?.RegisterUIReloadCallback(HandleUIReload);
    }
    private void OnDestroy() { panelRenderer?.UnregisterUIReloadCallback(HandleUIReload); Close(); }

    public static bool Open(CurrencyShopTerminal source, Transform player)
    {
        CurrencyShopHUD hud = FindAnyObjectByType<CurrencyShopHUD>();
        return hud != null && source != null && player != null && hud.OpenInternal(source, player);
    }
    private bool OpenInternal(CurrencyShopTerminal source, Transform player)
    {
        terminal = source; wallet = player.GetComponent<PlayerCurrencyWallet>() ??
            PlayerCurrencyWallet.FindOrCreate();
        charge = PlayerChargeUpgrade.FindOrCreate(player);
        if (wallet == null || charge == null) return false;
        terminal.ConfigureCharge(charge); wallet.Changed += OnCurrencyChanged;
        IsOpen = true; GamePauseController.RequestPause(this); Refresh();
        if (overlay != null) overlay.style.display = DisplayStyle.Flex;
        return true;
    }
    public void Close()
    {
        if (!IsOpen) return;
        if (wallet != null) wallet.Changed -= OnCurrencyChanged;
        IsOpen = false; GamePauseController.ReleasePause(this);
        if (overlay != null) overlay.style.display = DisplayStyle.None;
        terminal = null; wallet = null; charge = null;
    }
    private void HandleUIReload(PanelRenderer _, VisualElement root, int __)
    {
        if (root == null) return;
        overlay?.RemoveFromHierarchy(); overlay = new VisualElement();
        overlay.style.position = Position.Absolute; overlay.style.left = 0; overlay.style.right = 0;
        overlay.style.top = 0; overlay.style.bottom = 0; overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center; overlay.style.backgroundColor = new Color(0, 0, 0, .76f);
        VisualElement panel = new VisualElement(); panel.style.width = 480; panel.style.paddingLeft = 28; panel.style.paddingRight = 28; panel.style.paddingTop = 24; panel.style.paddingBottom = 24;
        panel.style.backgroundColor = new Color(.055f, .065f, .09f, .98f);
        panel.style.borderLeftWidth = panel.style.borderRightWidth = panel.style.borderTopWidth = panel.style.borderBottomWidth = 2;
        panel.style.borderLeftColor = panel.style.borderRightColor = panel.style.borderTopColor = panel.style.borderBottomColor = new Color(1f, .72f, .15f);
        title = Label(24, new Color(1f, .78f, .25f)); balance = Label(16, Color.white); chargeInfo = Label(18, Color.white); feedback = Label(14, new Color(.8f, .85f, .9f));
        purchaseButton = new Button(BuyCharge) { text = "COMPRAR CARGA" }; purchaseButton.style.marginTop = 18; purchaseButton.style.height = 40;
        Button close = new Button(Close) { text = "FECHAR" }; close.style.marginTop = 8; close.style.height = 32;
        panel.Add(title); panel.Add(balance); panel.Add(chargeInfo); panel.Add(feedback); panel.Add(purchaseButton); panel.Add(close); overlay.Add(panel); root.Add(overlay);
        overlay.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None; Refresh();
    }
    private static Label Label(int size, Color color) { Label label = new(); label.style.fontSize = size; label.style.color = color; label.style.marginBottom = 8; label.style.whiteSpace = WhiteSpace.Normal; return label; }
    private void BuyCharge()
    {
        if (terminal == null || wallet == null || charge == null) return;
        int cost = terminal.GetCostForNextLevel(charge.CurrentLevel);
        feedback.text = charge.IsMaxLevel ? "CARGA NO NIVEL MAXIMO." : charge.TryBuy(wallet, cost) ? "CARGA APRIMORADA." : "CURRENCY INSUFICIENTE.";
        Refresh();
    }
    private void OnCurrencyChanged(int _) => Refresh();
    private void Refresh()
    {
        if (title == null || terminal == null || wallet == null || charge == null) return;
        title.text = terminal.ShopTitle; balance.text = "CURRENCY: " + wallet.Current;
        if (charge.IsMaxLevel) { chargeInfo.text = "CARGA " + charge.CurrentLevel + "/" + charge.MaximumLevel + "\nMAX HEAT: +" + charge.CurrentMaxHeatBonus.ToString("0") + "\nNIVEL MAXIMO"; purchaseButton?.SetEnabled(false); return; }
        int next = charge.CurrentLevel + 1, cost = terminal.GetCostForNextLevel(charge.CurrentLevel);
        chargeInfo.text = terminal.ShopDescription + "\n\nCARGA " + charge.CurrentLevel + " -> " + next + "\nMAX HEAT: +" + charge.CurrentMaxHeatBonus.ToString("0") + " -> +" + terminal.GetMaxHeatBonusForLevel(next).ToString("0") + "\nPRECO: " + cost;
        purchaseButton?.SetEnabled(wallet.Current >= cost);
    }
}

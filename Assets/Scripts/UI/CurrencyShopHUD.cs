using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>Uma unica tela modal, compartilhada por todos os terminais do mapa.</summary>
[RequireComponent(typeof(PanelRenderer))]
[DisallowMultipleComponent]
public sealed class CurrencyShopHUD : MonoBehaviour
{
    private sealed class AttributeRow
    {
        public PlayerUpgradeDefinition Definition;
        public Label Description;
        public Button Buy;
    }
    private static CurrencyShopHUD instance;
    private bool isOpen;
    private static int lastCloseFrame = -1;
    public static bool IsOpen => instance != null && instance.isOpen;
    public static bool BlocksPauseInput => IsOpen || Time.frameCount == lastCloseFrame;

    [Tooltip("Painel existente usado para apresentar a loja acima dos outros elementos da interface.")]
    [SerializeField] private PanelRenderer panelRenderer;
    private readonly List<AttributeRow> rows = new();
    private readonly List<Button> buttons = new();
    private VisualElement overlay;
    private ScrollView list;
    private Label title, balance, feedback;
    private CurrencyShopTerminal terminal;
    private PlayerCurrencyWallet wallet;
    private PlayerProgression progression;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private int lastClickFrame = -1;
    private Button hoveredButton;
    private int selectedButtonIndex;
    private float nextNavigationAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => lastCloseFrame = -1;

    private void Awake()
    {
        if (instance != null && instance != this) { enabled = false; return; }
        instance = this;
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        EnsureUI(GetComponent<PauseMenuHUD>()?.UIRoot);
    }
    private void OnDisable() => Close();
    private void OnDestroy()
    {
        panelRenderer?.UnregisterUIReloadCallback(HandleUIReload);
        Close();
        if (instance == this) instance = null;
    }
    public static bool Open(CurrencyShopTerminal source, Transform player)
    {
        CurrencyShopHUD hud = instance != null ? instance : FindAnyObjectByType<CurrencyShopHUD>();
        if (source == null || player == null) return false;
        if (IsOpen)
        {
            MenuUIAudio.Play(MenuAudioCue.Unavailable);
            return false;
        }
        if (hud == null)
            return OpenFailed(source, "CurrencyShopHUD nao foi encontrado. Mantenha PauseMenuHUD ativo no GameRoot.");
        hud.EnsureUI(hud.GetComponent<PauseMenuHUD>()?.UIRoot);
        if (hud.overlay == null)
            return OpenFailed(source, "O painel da loja ainda nao foi inicializado. Confira o PanelRenderer do menu de pausa.");
        PlayerState state = player.GetComponent<PlayerState>();
        if (state != null && state.IsDead()) return false;
        hud.progression = PlayerProgression.GetOrAdd(player.gameObject);
        hud.progression.Initialize();
        if (hud.progression.Catalog == null)
            return OpenFailed(source, "Catalogo ausente: crie Assets/Resources/PlayerUpgradeCatalog.asset pelo menu Progression.");
        hud.wallet = player.GetComponent<PlayerCurrencyWallet>();
        if (hud.wallet == null)
            hud.wallet = player.gameObject.AddComponent<PlayerCurrencyWallet>();
        hud.terminal = source;
        hud.isOpen = true;
        hud.wallet.Changed += hud.OnCurrencyChanged;
        hud.progression.Changed += hud.Refresh;
        hud.previousCursorLock = UnityEngine.Cursor.lockState;
        hud.previousCursorVisible = UnityEngine.Cursor.visible;
        GamePauseController.RequestPause(hud);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        hud.BuildRows();
        hud.SelectButton(0, false);
        hud.overlay.style.display = DisplayStyle.Flex;
        hud.overlay.BringToFront();
        hud.feedback.text = "";
        hud.Refresh();
        MenuUIAudio.Play(MenuAudioCue.MenuOpen);
        return true;
    }
    private static bool OpenFailed(CurrencyShopTerminal source, string message)
    {
        MenuUIAudio.Play(MenuAudioCue.Unavailable);
        Debug.LogWarning("[Loja] " + message, source);
        return false;
    }
    internal void EnsureUI(VisualElement root)
    {
        if (root == null || overlay != null && overlay.parent == root) return;
        HandleUIReload(panelRenderer, root, 0);
    }
    public static void CloseFor(CurrencyShopTerminal source)
    {
        if (instance != null && instance.terminal == source) instance.Close();
    }
    public void Close()
    {
        if (!isOpen) return;
        // Cada tela libera somente seu proprio pedido de pausa.
        GamePauseController.ReleasePause(this);
        if (wallet != null) wallet.Changed -= OnCurrencyChanged;
        if (progression != null) progression.Changed -= Refresh;
        bool wasOpen = isOpen;
        isOpen = false;
        terminal = null; wallet = null; progression = null;
        hoveredButton = null;
        if (overlay != null) overlay.style.display = DisplayStyle.None;
        if (wasOpen)
        {
            MenuUIAudio.Play(MenuAudioCue.Back);
            lastCloseFrame = Time.frameCount;
            UnityEngine.Cursor.lockState = previousCursorLock;
            UnityEngine.Cursor.visible = previousCursorVisible;
        }
    }
    private void Update()
    {
        if (!isOpen) return;
        if (terminal == null || !terminal.isActiveAndEnabled) { Close(); return; }
        if (GameInput.UICancelPressed || GameInput.PausePressed) { Close(); return; }
        if (overlay == null) return;

        float vertical = GameInput.UINavigate.y;
        if (Mathf.Abs(vertical) > .5f && Time.unscaledTime >= nextNavigationAt &&
            buttons.Count > 0)
        {
            SelectButton((selectedButtonIndex + (vertical < 0f ? 1 : -1) + buttons.Count)
                % buttons.Count);
            nextNavigationAt = Time.unscaledTime + .17f;
        }

        if (GameInput.UISubmitPressed && buttons.Count > 0)
        {
            ActivateButton(buttons[selectedButtonIndex]);
            return;
        }

        Vector2 mouse = GameInput.UIPointer;
        Rect viewport = overlay.worldBound;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Vector2 point = new(viewport.xMin + mouse.x / Screen.width * viewport.width,
            viewport.yMin + (1f - mouse.y / Screen.height) * viewport.height);
        Button hovered = null;
        foreach (Button button in buttons)
        {
            if (!button.worldBound.Contains(point)) continue;
            if (button != buttons[buttons.Count - 1] && !list.worldBound.Contains(point)) continue;
            hovered = button;
            break;
        }

        if (Mouse.current != null && hovered != hoveredButton)
        {
            hoveredButton = hovered;
            if (hoveredButton != null)
                MenuUIAudio.Play(MenuAudioCue.Focus);
        }

        // PanelRenderer deste projeto usa entrada manual por coordenadas.
        if (!GameInput.UIClickPressed || hovered == null) return;
        int hoveredIndex = buttons.IndexOf(hovered);
        if (hoveredIndex >= 0)
            SelectButton(hoveredIndex, false);
        ActivateButton(hovered);
    }
    private void LateUpdate()
    {
        if (terminal != null) overlay?.BringToFront();
    }
    private void HandleUIReload(PanelRenderer _, VisualElement root, int __)
    {
        if (root == null) return;
        overlay?.RemoveFromHierarchy();
        overlay = new VisualElement { name = "currency-shop-overlay" };
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0; overlay.style.right = 0;
        overlay.style.top = 0; overlay.style.bottom = 0;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        overlay.style.backgroundColor = new Color(0, 0, 0, .86f);
        VisualElement panel = new();
        panel.style.width = Length.Percent(80);
        panel.style.maxWidth = 850;
        panel.style.maxHeight = Length.Percent(90);
        panel.style.paddingLeft = 24; panel.style.paddingRight = 24;
        panel.style.paddingTop = 20; panel.style.paddingBottom = 20;
        panel.style.backgroundColor = new Color(.055f, .065f, .09f);
        title = NewLabel(24, new Color(1, .78f, .25f));
        balance = NewLabel(18, Color.white);
        list = new ScrollView();
        list.style.flexShrink = 1;
        feedback = NewLabel(15, new Color(.85f, .85f, .85f));
        panel.Add(title); panel.Add(balance); panel.Add(list); panel.Add(feedback);
        Button close = new(Close) { text = "FECHAR" };
        close.style.height = 36;
        panel.Add(close);
        overlay.Add(panel); root.Add(overlay);
        overlay.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (IsOpen) { BuildRows(); Refresh(); }
    }
    private void BuildRows()
    {
        list.Clear(); rows.Clear(); buttons.Clear();
        var ids = new HashSet<string>();
        foreach (PlayerUpgradeDefinition definition in progression.Catalog.attributes)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id) || !ids.Add(definition.id)) continue;
            VisualElement row = new();
            row.style.marginBottom = 16;
            Label description = NewLabel(16, Color.white);
            Button buy = new(() => Buy(definition));
            buy.userData = definition;
            buy.style.height = 34;
            row.Add(description); row.Add(buy); list.Add(row);
            rows.Add(new AttributeRow { Definition = definition, Description = description, Buy = buy });
            buttons.Add(buy);
        }
        // O botao Fechar e o ultimo filho do painel.
        VisualElement panel = overlay[0];
        Button close = panel[panel.childCount - 1] as Button;
        if (close != null) buttons.Add(close);
    }

    private void SelectButton(int index, bool playFocusSound = true)
    {
        if (buttons.Count == 0)
            return;

        Button previous = selectedButtonIndex >= 0 && selectedButtonIndex < buttons.Count
            ? buttons[selectedButtonIndex] : null;
        previous?.RemoveFromClassList("currency-shop-selected");
        selectedButtonIndex = Mathf.Clamp(index, 0, buttons.Count - 1);
        Button selected = buttons[selectedButtonIndex];
        selected.AddToClassList("currency-shop-selected");
        selected.Focus();
        if (playFocusSound && previous != selected)
            MenuUIAudio.Play(MenuAudioCue.Focus);
        if (list != null && list.Contains(selected))
            list.ScrollTo(selected);
    }

    private void ActivateButton(Button button)
    {
        if (button == null)
            return;
        if (!button.enabledInHierarchy)
        {
            MenuUIAudio.Play(MenuAudioCue.Unavailable);
            return;
        }

        if (button.userData is PlayerUpgradeDefinition definition)
            Buy(definition);
        else
            Close();
    }
    private void Buy(PlayerUpgradeDefinition definition)
    {
        if (lastClickFrame == Time.frameCount || progression == null) return;
        lastClickFrame = Time.frameCount;
        bool purchased = progression.TryPurchase(definition, out string message);
        MenuUIAudio.Play(purchased
            ? MenuAudioCue.Success
            : MenuAudioCue.Unavailable);
        feedback.text = message;
        Refresh();
    }
    private void OnCurrencyChanged(int _) => Refresh();
    private void Refresh()
    {
        if (progression == null || title == null || wallet == null) return;
        title.text = progression.Catalog.shopTitle;
        balance.text = "CURRENCY: " + wallet.Current;
        foreach (AttributeRow row in rows)
        {
            PlayerUpgradeDefinition definition = row.Definition;
            int level = progression.Level(definition);
            int maximumLevel = definition.MaximumLevel;
            string next = level < maximumLevel ? " -> " + definition.Bonus(level + 1).ToString("0.##") : "";
            var details = new StringBuilder();
            details.Append(definition.displayName).Append("  ").Append(level).Append("/").Append(maximumLevel)
                .Append("\n").Append(definition.description).Append("\nBonus total: ")
                .Append(definition.Bonus(level).ToString("0.##")).Append(next);
            for (int index = 1; index <= maximumLevel; index++)
            {
                PlayerUpgradeLevel entry = definition.GetLevel(index);
                if (entry?.unlocks == null) continue;
                foreach (PlayerUpgradeUnlock unlock in entry.unlocks)
                {
                    if (unlock == null) continue;
                    details.Append("\nNivel ").Append(index).Append(": ")
                        .Append(string.IsNullOrWhiteSpace(unlock.displayName) ? unlock.id : unlock.displayName)
                        .Append(index <= level ? " [DESBLOQUEADO]" : " [BLOQUEADO]");
                    if (!string.IsNullOrWhiteSpace(unlock.description))
                        details.Append(" — ").Append(unlock.description);
                }
            }
            row.Description.text = details.ToString();
            row.Buy.text = !definition.purchasable ? "TBD" : level >= maximumLevel ? "NIVEL MAXIMO" :
                "SUBIR PARA " + (level + 1) + " — " + definition.Price(level + 1) + " CURRENCY";
            row.Buy.SetEnabled(definition.purchasable && level < maximumLevel &&
                definition.GetLevel(level + 1) != null && wallet.Current >= definition.Price(level + 1));
        }
    }
    private static Label NewLabel(int size, Color color)
    {
        Label label = new();
        label.style.fontSize = size; label.style.color = color;
        label.style.marginBottom = 8; label.style.whiteSpace = WhiteSpace.Normal;
        return label;
    }
}

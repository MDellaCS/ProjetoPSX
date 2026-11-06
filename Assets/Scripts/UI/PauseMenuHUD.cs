using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>Menu de pausa e opções em UI Toolkit, com aplicação e cancelamento.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PanelRenderer))]
public sealed class PauseMenuHUD : MonoBehaviour
{
    private enum SettingsPage { Audio, Video, Camera, Controls, Accessibility }
    private enum PendingConfirm { None, NewGame, RestoreDefaults }

    private const int SliderLevels = 10;

    private sealed class SliderBinding
    {
        public VisualElement bars;
        public int level;
        public Action<int> apply;
    }

    public static bool IsOpen { get; private set; }
    internal VisualElement UIRoot => root;

    [Header("References")]
    [Tooltip("Panel Renderer que apresenta o UXML do menu de pausa e configurações.")]
    [SerializeField] private PanelRenderer panelRenderer;

    private readonly List<Button> buttons = new();
    private readonly Dictionary<Button, Action> commands = new();
    private readonly Dictionary<Button, MenuAudioCue> commandCues = new();
    private VisualElement root;
    private VisualElement overlay;
    private VisualElement content;
    private VisualElement mainView;
    private VisualElement settingsView;
    private ScrollView options;
    private Label settingsTitle;
    private bool nativeWheelWorks;
    private Label mainMenuStatus;
    private Label status;
    private GameSettingsData editingSettings;
    private readonly Dictionary<Button, SliderBinding> sliders = new();
    private readonly HashSet<Button> confirmButtons = new();
    private PendingConfirm pending;
    private bool selectConfirmNo;
    private int confirmNoIndex;
    private float nextHorizontalAt;
    private float appliedScale;
    private SettingsPage page;
    private CursorLockMode cursorLockBeforePause;
    private bool cursorVisibleBeforePause;
    private bool settingsOpen;
    private bool mainMenuMode;
    private Action startGameAction;
    private string mainMenuMessage;
    private int selectedIndex;
    private float nextNavigationAt;
    private int hoveredIndex = -1;

    private void Awake()
    {
        IsOpen = false;
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        SetVisible(false);
        if (GetComponent<CurrencyShopHUD>() == null)
            gameObject.AddComponent<CurrencyShopHUD>();
    }

    private void OnEnable()
    {
        GameSettingsService.Changed += RefreshScale;
    }

    private void OnDisable()
    {
        GameSettingsService.Changed -= RefreshScale;
        Resume();
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        Resume();
    }

    private void Update()
    {
        if (CurrencyShopHUD.BlocksPauseInput) return;
        if (GameInputBindings.IsRebinding ||
            Time.frameCount <= GameInputBindings.LastRebindFrame + 1)
            return;

        if (GameInput.PausePressed && !mainMenuMode)
        {
            if (pending != PendingConfirm.None)
                ClearConfirm();
            else if (settingsOpen)
                CloseSettings();
            else if (IsOpen)
                Resume();
            else
                Pause();
            return;
        }

        if (!IsOpen)
            return;

        UpdatePointerHover();
        UpdateOptionsWheel();

        if (GameInput.UICancelPressed)
        {
            if (pending != PendingConfirm.None) ClearConfirm();
            else if (settingsOpen) CloseSettings();
            else if (!mainMenuMode) Resume();
            return;
        }

        if (GameInput.UIClickPressed)
        {
            Vector2 pointer = GameInput.UIPointer;
            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                Button button = buttons[i];
                if (button.resolvedStyle.display == DisplayStyle.None ||
                    !IsPointerInside(button, pointer))
                    continue;
                if (button.enabledInHierarchy)
                    Select(i, false);
                if (sliders.TryGetValue(button, out SliderBinding slider))
                    ClickSlider(slider, pointer);
                else
                    ActivateButton(button);
                return;
            }
        }

        float vertical = GameInput.UINavigate.y;
        if (Mathf.Abs(vertical) > .5f && Time.unscaledTime >= nextNavigationAt)
        {
            int direction = vertical < 0f ? 1 : -1;
            int next = selectedIndex;
            for (int step = 0; step < buttons.Count; step++)
            {
                next = (next + direction + buttons.Count) % buttons.Count;
                if (buttons[next].enabledInHierarchy)
                {
                    Select(next);
                    break;
                }
            }
            nextNavigationAt = Time.unscaledTime + .17f;
        }

        float horizontal = GameInput.UINavigate.x;
        if (Mathf.Abs(horizontal) > .5f && Time.unscaledTime >= nextHorizontalAt &&
            selectedIndex >= 0 && selectedIndex < buttons.Count &&
            sliders.TryGetValue(buttons[selectedIndex], out SliderBinding selected))
        {
            SetSliderLevel(selected, selected.level + (horizontal > 0f ? 1 : -1));
            nextHorizontalAt = Time.unscaledTime + .14f;
        }

        if (GameInput.UISubmitPressed && buttons.Count > 0)
            ActivateButton(buttons[selectedIndex]);
    }

    private void HandleUIReload(PanelRenderer _, VisualElement visualRoot, int __)
    {
        root = visualRoot;
        if (root == null)
            return;
        GetComponent<CurrencyShopHUD>()?.EnsureUI(root);
        overlay = root.Q<VisualElement>("pause-menu-overlay");
        content = root.Q<VisualElement>("pause-menu-content");
        appliedScale = 0f;
        mainView = root.Q<VisualElement>("pause-main-view");
        settingsView = root.Q<VisualElement>("pause-settings-view");
        options = root.Q<ScrollView>("settings-options");
        settingsTitle = root.Q<Label>("pause-settings-title");
        if (options != null)
            options.RegisterCallback<WheelEvent>(_ => nativeWheelWorks = true,
                TrickleDown.TrickleDown);
        mainMenuStatus = root.Q<Label>("main-menu-status");
        status = root.Q<Label>("settings-status");
        RebuildButtons();
        ApplyMenuModePresentation();
        SetVisible(IsOpen);
        RefreshScale();
    }

    private void RebuildButtons(bool keepSelection = false)
    {
        foreach (Button old in buttons)
            old.RemoveFromClassList("is-selected");
        int previousSelection = selectedIndex;
        buttons.Clear();
        commands.Clear();
        commandCues.Clear();
        sliders.Clear();
        confirmButtons.Clear();
        if (!keepSelection)
            hoveredIndex = -1;
        if (root == null)
            return;
        if (!settingsOpen)
        {
            Button primaryButton = root.Q<Button>("menu-primary-button");
            Button saveButton = root.Q<Button>("menu-save-button");
            Button optionsButton = root.Q<Button>("menu-options-button");
            Button exitButton = root.Q<Button>("menu-exit-button");
            // Mesmo menu nos dois modos; so mudam os textos e comandos.
            SetButtonText(primaryButton, mainMenuMode ? "Novo Jogo" : "Continuar");
            SetButtonText(saveButton, mainMenuMode ? "Continuar" : "Carregar Save");
            SetButtonText(optionsButton, "Opções");
            SetButtonText(exitButton, mainMenuMode ? "Sair" : "Voltar ao Menu Inicial");

            bool confirming = mainMenuMode && pending == PendingConfirm.NewGame;
            BuildConfirmSlot(primaryButton, "menu-primary-confirm", "menu-primary-yes",
                "menu-primary-no", confirming, StartNewGame, MenuAudioCue.Confirm);
            if (!confirming)
                AddButton(primaryButton, mainMenuMode
                    ? new Action(AskNewGame) : new Action(Resume),
                    mainMenuMode ? MenuAudioCue.Confirm : MenuAudioCue.None);
            // Ainda nao existe sistema de save: fica cinza e inativo.
            AddButton(saveButton, LoadSave);
            if (saveButton != null)
                saveButton.SetEnabled(HasSaveGame);
            AddButton(optionsButton, OpenSettings);
            AddButton(exitButton, mainMenuMode
                ? new Action(QuitPlaySession) : new Action(ReturnToMainMenu),
                MenuAudioCue.Warning);
        }
        else
        {
            (string name, SettingsPage target)[] tabs =
            {
                ("settings-audio-tab", SettingsPage.Audio),
                ("settings-video-tab", SettingsPage.Video),
                ("settings-camera-tab", SettingsPage.Camera),
                ("settings-controls-tab", SettingsPage.Controls),
                ("settings-accessibility-tab", SettingsPage.Accessibility)
            };
            foreach ((string name, SettingsPage target) tab in tabs)
            {
                Button tabButton = root.Q<Button>(tab.name);
                SettingsPage targetPage = tab.target;
                AddButton(tabButton, () => ShowPage(targetPage), MenuAudioCue.TabChange);
                if (tabButton != null)
                    tabButton.EnableInClassList("is-active", page == targetPage);
            }
            BuildOptions();

            Button defaultsButton = root.Q<Button>("settings-defaults-button");
            SetButtonText(defaultsButton, "Restaurar Padrões");
            bool restoring = pending == PendingConfirm.RestoreDefaults;
            BuildConfirmSlot(defaultsButton, "settings-defaults-confirm", "settings-defaults-yes",
                "settings-defaults-no", restoring, ConfirmRestoreDefaults, MenuAudioCue.Warning);
            if (!restoring)
                AddButton(defaultsButton, AskRestoreDefaults, MenuAudioCue.None);
            AddButton(root.Q<Button>("settings-back-button"), CloseSettings, MenuAudioCue.None);
        }

        int targetIndex = selectConfirmNo ? confirmNoIndex : keepSelection ? previousSelection : 0;
        selectConfirmNo = false;
        Select(targetIndex, false);
        ApplyMenuModePresentation();
    }

    // Sim/Nao ocupam o lugar exato do botao original (sobrepostos no mesmo "slot"),
    // entao a confirmacao nao desloca nada do layout.
    private void BuildConfirmSlot(Button original, string rowName, string yesName,
        string noName, bool confirming, Action onYes, MenuAudioCue yesCue)
    {
        VisualElement row = root.Q<VisualElement>(rowName);
        Button yes = root.Q<Button>(yesName);
        Button no = root.Q<Button>(noName);
        SetButtonText(yes, "Sim");
        SetButtonText(no, "Não");
        if (original != null)
            original.style.visibility = confirming ? Visibility.Hidden : Visibility.Visible;
        if (row != null)
            row.style.display = confirming ? DisplayStyle.Flex : DisplayStyle.None;
        if (!confirming)
            return;
        AddButton(yes, onYes, yesCue);
        confirmButtons.Add(yes);
        confirmNoIndex = buttons.Count;
        AddButton(no, ClearConfirm, MenuAudioCue.Back);
        confirmButtons.Add(no);
    }

    private void AskNewGame() => AskConfirm(PendingConfirm.NewGame);
    private void AskRestoreDefaults() => AskConfirm(PendingConfirm.RestoreDefaults);

    private void AskConfirm(PendingConfirm kind)
    {
        pending = kind;
        selectConfirmNo = true;
        hoveredIndex = -1;
        RebuildButtons();
    }

    private void ClearConfirm()
    {
        if (pending == PendingConfirm.None)
            return;
        pending = PendingConfirm.None;
        hoveredIndex = -1;
        RebuildButtons(true);
    }

    private void AddButton(Button button, Action command,
        MenuAudioCue cue = MenuAudioCue.Confirm)
    {
        if (button == null)
            return;
        EnsureLabel(button);
        buttons.Add(button);
        commands[button] = command;
        commandCues[button] = cue;
    }

    // O texto fica num Label filho: o Button e a caixa de interacao e o
    // sublinhado do menu inicial acompanha so a largura do texto.
    private static Label EnsureLabel(Button button)
    {
        Label label = button.Q<Label>(className: "menu-button-label");
        if (label != null)
            return label;
        button.AddToClassList("menu-button");
        label = new Label(button.text);
        label.AddToClassList("menu-button-label");
        label.pickingMode = PickingMode.Ignore;
        button.text = string.Empty;
        button.Add(label);
        return label;
    }

    private static void SetButtonText(Button button, string text)
    {
        if (button != null)
            EnsureLabel(button).text = text;
    }

    // Nao ha sistema de save ainda; quando existir, Continuar passa a ativo.
    private static bool HasSaveGame => false;

    private void LoadSave() { }

    private void UpdateOptionsWheel()
    {
        // Rolagem com scrollbar invisivel: so assume a roda se o UI Toolkit nao entregar o evento.
        if (!settingsOpen || nativeWheelWorks || options == null || Mouse.current == null)
            return;
        float wheel = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(wheel) > .01f)
            options.verticalScroller.value -= Mathf.Sign(wheel) * 48f;
    }

    private void Select(int index, bool playFocusSound = true)
    {
        if (buttons.Count == 0)
            return;
        int previousIndex = selectedIndex;
        if (selectedIndex >= 0 && selectedIndex < buttons.Count)
            buttons[selectedIndex].RemoveFromClassList("is-selected");
        selectedIndex = Mathf.Clamp(index, 0, buttons.Count - 1);
        buttons[selectedIndex].AddToClassList("is-selected");
        buttons[selectedIndex].Focus();
        if (playFocusSound && previousIndex != selectedIndex)
            MenuUIAudio.Play(MenuAudioCue.Focus);
        if (options != null && options.Contains(buttons[selectedIndex]))
            options.ScrollTo(buttons[selectedIndex]);
    }

    private void ActivateButton(Button button)
    {
        if (button == null || !commands.TryGetValue(button, out Action command))
            return;
        if (!button.enabledInHierarchy)
        {
            MenuUIAudio.Play(MenuAudioCue.Unavailable);
            return;
        }

        // Qualquer outra opcao cancela uma confirmacao pendente.
        bool clearedPending = pending != PendingConfirm.None &&
            !confirmButtons.Contains(button);
        bool settingsBefore = settingsOpen;
        if (clearedPending)
            pending = PendingConfirm.None;

        commandCues.TryGetValue(button, out MenuAudioCue cue);
        command?.Invoke();
        MenuUIAudio.Play(cue);
        if (clearedPending && settingsOpen == settingsBefore && IsOpen)
            RebuildButtons(true);
    }

    private void UpdatePointerHover()
    {
        if (Mouse.current == null)
            return;

        Vector2 pointer = GameInput.UIPointer;
        int hovered = -1;
        for (int i = buttons.Count - 1; i >= 0; i--)
        {
            Button candidate = buttons[i];
            if (candidate.resolvedStyle.display == DisplayStyle.None ||
                !candidate.enabledInHierarchy ||
                !IsPointerInside(candidate, pointer))
                continue;
            hovered = i;
            break;
        }

        if (hovered == hoveredIndex)
            return;

        hoveredIndex = hovered;
        if (hovered >= 0)
        {
            // Hover vira selecao: uma unica linha de destaque por vez.
            Select(hovered, false);
            MenuUIAudio.Play(MenuAudioCue.Focus);
        }
    }

    private void ClickSlider(SliderBinding slider, Vector2 pointer)
    {
        if (!TryGetPanelPoint(pointer, out Vector2 point))
            return;
        Rect area = slider.bars.worldBound;
        if (!area.Contains(point) || area.width <= 0f)
            return;
        int level = Mathf.FloorToInt((point.x - area.xMin) / area.width * SliderLevels);
        SetSliderLevel(slider, level);
    }

    private void SetSliderLevel(SliderBinding slider, int level)
    {
        level = Mathf.Clamp(level, 0, SliderLevels - 1);
        if (level == slider.level)
            return;
        MenuUIAudio.Play(MenuAudioCue.Adjust);
        slider.apply(level);
    }

    public void Pause()
    {
        if (IsOpen)
            return;
        MenuUIAudio.Play(MenuAudioCue.MenuOpen);
        cursorLockBeforePause = UnityEngine.Cursor.lockState;
        cursorVisibleBeforePause = UnityEngine.Cursor.visible;
        IsOpen = true;
        GamePauseController.RequestPause(this);
        SetVisible(true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        RebuildButtons();
    }

    public void ShowMainMenu(Action onStartGame)
    {
        mainMenuMode = true;
        startGameAction = onStartGame;
        mainMenuMessage = string.Empty;
        settingsOpen = false;
        pending = PendingConfirm.None;
        if (!IsOpen)
            Pause();
        else
        {
            SetVisible(true);
            RebuildButtons();
        }
        ApplyMenuModePresentation();
    }

    public void CloseMainMenu()
    {
        if (mainMenuMode)
            Resume();
    }

    private void StartNewGame() => startGameAction?.Invoke();

    public void SetMainMenuMessage(string message)
    {
        mainMenuMessage = message ?? string.Empty;
        if (mainMenuStatus == null)
            return;

        mainMenuStatus.text = mainMenuMessage;
        mainMenuStatus.style.display = mainMenuMode &&
            !string.IsNullOrEmpty(mainMenuMessage)
            ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Resume()
    {
        if (!IsOpen)
            return;
        if (settingsOpen)
            CloseSettings();
        MenuUIAudio.Play(MenuAudioCue.MenuClose);
        IsOpen = false;
        hoveredIndex = -1;
        pending = PendingConfirm.None;
        mainMenuMode = false;
        startGameAction = null;
        GamePauseController.ReleasePause(this);
        SetVisible(false);
        UnityEngine.Cursor.lockState = cursorLockBeforePause;
        UnityEngine.Cursor.visible = cursorVisibleBeforePause;
        ApplyMenuModePresentation();
    }

    private void OpenSettings()
    {
        editingSettings = GameSettingsService.Snapshot();
        settingsOpen = true;
        pending = PendingConfirm.None;
        page = SettingsPage.Audio;
        if (status != null) status.text = string.Empty;
        SetVisible(true);
        RebuildButtons();
    }

    // As alteracoes sao salvas a cada mudanca; Voltar so fecha a tela.
    private void CloseSettings()
    {
        MenuUIAudio.Play(MenuAudioCue.Back);
        GameInputBindings.CancelRebind();
        pending = PendingConfirm.None;
        settingsOpen = false;
        SetVisible(true);
        RebuildButtons();
    }

    private void ShowPage(SettingsPage next)
    {
        page = next;
        pending = PendingConfirm.None;
        RebuildButtons(true);
    }

    private void BuildOptions()
    {
        if (options == null || editingSettings == null)
            return;
        options.Clear();
        switch (page)
        {
            case SettingsPage.Audio:
                AddStep("Volume geral", editingSettings.masterVolume, 0f, 1f,
                    value => editingSettings.masterVolume = value);
                AddStep("Efeitos sonoros", editingSettings.effectsVolume, 0f, 1f,
                    value => editingSettings.effectsVolume = value);
                AddStep("Sons de interface", editingSettings.uiVolume, 0f, 1f,
                    value => editingSettings.uiVolume = value);
                break;
            case SettingsPage.Video:
                AddToggle("Tela cheia", editingSettings.fullScreen,
                    value => editingSettings.fullScreen = value);
                AddToggle("Sincronização vertical", editingSettings.verticalSync,
                    value => editingSettings.verticalSync = value);
                int[] caps = { 0, 30, 60, 90, 120, 144, 240 };
                AddOption("Limite de FPS: " + (editingSettings.frameRateCap == 0
                    ? "Ilimitado" : editingSettings.frameRateCap.ToString()), () =>
                {
                    int index = Array.IndexOf(caps, editingSettings.frameRateCap);
                    editingSettings.frameRateCap = caps[(index + 1) % caps.Length];
                    PreviewAndRebuild();
                });
                string[] levels = QualitySettings.names;
                if (levels.Length > 0)
                {
                    AddOption("Qualidade: " + levels[editingSettings.qualityLevel], () =>
                    {
                        editingSettings.qualityLevel = (editingSettings.qualityLevel + 1) % levels.Length;
                        PreviewAndRebuild();
                    });
                }
                break;
            case SettingsPage.Camera:
                AddStep("Sensibilidade", editingSettings.cameraSensitivity, .2f, 3f,
                    value => editingSettings.cameraSensitivity = value);
                AddToggle("Inverter eixo vertical", editingSettings.invertCameraY,
                    value => editingSettings.invertCameraY = value);
                break;
            case SettingsPage.Controls:
                BuildControls();
                break;
            case SettingsPage.Accessibility:
                AddStep("Tremor da câmera", editingSettings.cameraShake, 0f, 1f,
                    value => editingSettings.cameraShake = value);
                AddStep("Escala dos menus", editingSettings.menuScale, .8f, 1.4f,
                    value => editingSettings.menuScale = value);
                break;
        }
    }

    // Valor mostrado de 0 a 9 (0 a 100% do intervalo) em 10 barras clicaveis;
    // a primeira barra sempre acesa representa o 0.
    private void AddStep(string label, float current, float minimum, float maximum,
        Action<float> setter)
    {
        int level = Mathf.Clamp(Mathf.RoundToInt(
            Mathf.InverseLerp(minimum, maximum, current) * (SliderLevels - 1)),
            0, SliderLevels - 1);
        Button row = new() { text = label };
        row.AddToClassList("menu-slider");
        options.Add(row);
        AddButton(row, () => { }, MenuAudioCue.None);

        VisualElement bars = new();
        bars.AddToClassList("menu-bars");
        bars.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < SliderLevels; i++)
        {
            VisualElement bar = new();
            bar.AddToClassList("menu-bar");
            if (i == 0)
                bar.AddToClassList("menu-bar-first");
            bar.EnableInClassList("is-filled", i <= level);
            bar.pickingMode = PickingMode.Ignore;
            bars.Add(bar);
        }
        row.Add(bars);

        sliders[row] = new SliderBinding
        {
            bars = bars,
            level = level,
            apply = newLevel =>
            {
                float value = Mathf.Lerp(minimum, maximum, newLevel / (SliderLevels - 1f));
                setter(Mathf.Round(value * 100f) / 100f);
                PreviewAndRebuild();
            }
        };
    }

    private void AddToggle(string label, bool current, Action<bool> setter) =>
        AddOption(label + ": " + (current ? "SIM" : "NÃO"), () =>
        { setter(!current); PreviewAndRebuild(); }, MenuAudioCue.Toggle);

    private void AddOption(string text, Action command,
        MenuAudioCue cue = MenuAudioCue.Confirm)
    {
        Button button = new() { text = text };
        options.Add(button);
        AddButton(button, command, cue);
    }

    // Salva a cada mudanca e reconstroi mantendo selecao e rolagem.
    private void PreviewAndRebuild()
    {
        GameSettingsService.Save(editingSettings);
        if (page == SettingsPage.Controls)
            GameInputBindings.Save();
        float scroll = options != null ? options.verticalScroller.value : 0f;
        RebuildButtons(true);
        if (options != null)
            options.schedule.Execute(() => options.verticalScroller.value = scroll);
    }

    private void BuildControls()
    {
        InputActionMap gameplay = GameInput.Asset?.FindActionMap("Gameplay");
        if (gameplay == null)
            return;
        foreach (InputAction action in gameplay.actions)
        {
            for (int index = 0; index < action.bindings.Count; index++)
            {
                InputBinding binding = action.bindings[index];
                if (binding.isComposite || !GameInputBindings.IsKeyboardOrMouse(binding))
                    continue;
                int capturedIndex = index;
                string name = action.name;
                if (binding.isPartOfComposite)
                    name += " " + binding.name;
                AddOption(name + ": " + action.GetBindingDisplayString(index),
                    () => StartRebind(action, capturedIndex));
            }
        }
    }

    private void StartRebind(InputAction action, int index)
    {
        if (status != null) status.text = "Pressione a nova tecla ou botão. Esc cancela.";
        GameInputBindings.BeginRebind(action, index, (_, message) =>
        {
            if (status != null) status.text = message;
            GameInputBindings.Save();
            RebuildButtons(true);
        });
    }

    private void ConfirmRestoreDefaults()
    {
        pending = PendingConfirm.None;
        ResetCurrentSection();
    }

    private void ResetCurrentSection()
    {
        GameSettingsData defaults = GameSettingsService.CreateDefaults();
        switch (page)
        {
            case SettingsPage.Audio:
                editingSettings.masterVolume = defaults.masterVolume;
                editingSettings.effectsVolume = defaults.effectsVolume;
                editingSettings.uiVolume = defaults.uiVolume;
                break;
            case SettingsPage.Video:
                editingSettings.fullScreen = defaults.fullScreen;
                editingSettings.verticalSync = defaults.verticalSync;
                editingSettings.frameRateCap = defaults.frameRateCap;
                editingSettings.qualityLevel = defaults.qualityLevel;
                break;
            case SettingsPage.Camera:
                editingSettings.cameraSensitivity = defaults.cameraSensitivity;
                editingSettings.invertCameraY = defaults.invertCameraY;
                break;
            case SettingsPage.Controls:
                GameInputBindings.ResetToDefaults();
                break;
            case SettingsPage.Accessibility:
                editingSettings.cameraShake = defaults.cameraShake;
                editingSettings.menuScale = defaults.menuScale;
                break;
        }
        PreviewAndRebuild();
    }

    private void SetVisible(bool visible)
    {
        if (overlay != null) overlay.style.display = visible
            ? DisplayStyle.Flex : DisplayStyle.None;
        if (mainView != null) mainView.style.display = settingsOpen
            ? DisplayStyle.None : DisplayStyle.Flex;
        if (settingsView != null) settingsView.style.display = settingsOpen
            ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ApplyMenuModePresentation()
    {
        if (settingsTitle != null)
            settingsTitle.text = "OPÇÕES";
        if (options != null)
        {
            options.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            options.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }
        SetMainMenuMessage(mainMenuMessage);
    }

    private void RefreshScale()
    {
        if (content != null)
        {
            float scale = Mathf.Clamp(GameSettingsService.Applied.menuScale, .5f, 2f);
            if (Mathf.Approximately(scale, appliedScale))
                return;
            appliedScale = scale;
            // Espacamentos (padding, margens) encolhem conforme a escala; ver .menu-scale-N no USS.
            int level = Mathf.Clamp(Mathf.RoundToInt(
                Mathf.InverseLerp(.8f, 1.4f, scale) * (SliderLevels - 1)), 0, SliderLevels - 1);
            for (int i = 0; i < SliderLevels; i++)
                overlay?.EnableInClassList("menu-scale-" + i, i == level);
            // A escala parte do canto superior direito; encolher a caixa por 1/escala
            // faz o resultado visual caber exatamente na tela (sem cortar nem vazar).
            float inverse = 100f / scale;
            content.style.width = Length.Percent(inverse);
            content.style.height = Length.Percent(inverse);
            content.style.maxHeight = Length.Percent(inverse);
            content.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }
    }

    private bool TryGetPanelPoint(Vector2 screenPointer, out Vector2 panelPoint)
    {
        panelPoint = default;
        if (overlay == null || Screen.width <= 0 || Screen.height <= 0)
            return false;
        // Usa o retangulo real do painel como referencia: Game View e
        // PanelRenderer podem ter escalas diferentes. Mouse usa origem embaixo;
        // worldBound do UI Toolkit usa origem em cima.
        Rect viewport = overlay.worldBound;
        panelPoint = new Vector2(
            viewport.xMin + screenPointer.x / Screen.width * viewport.width,
            viewport.yMin + (1f - screenPointer.y / Screen.height) * viewport.height);
        return true;
    }

    private bool IsPointerInside(VisualElement element, Vector2 screenPointer)
    {
        if (element == null || !TryGetPanelPoint(screenPointer, out Vector2 panelPoint))
            return false;
        if (options != null && options.Contains(element) &&
            !options.worldBound.Contains(panelPoint))
            return false;
        return element.worldBound.Contains(panelPoint);
    }

    private void ReturnToMainMenu()
    {
        Resume();
        GameSceneLoader loader = FindAnyObjectByType<GameSceneLoader>();
        if (loader != null)
            loader.LoadMainMenu();
    }

    private void QuitPlaySession()
    {
        Resume();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

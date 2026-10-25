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

    public static bool IsOpen { get; private set; }
    internal VisualElement UIRoot => root;

    [Header("References")]
    [Tooltip("Panel Renderer que apresenta o UXML do menu de pausa e configurações.")]
    [SerializeField] private PanelRenderer panelRenderer;

    private readonly List<Button> buttons = new();
    private readonly Dictionary<Button, Action> commands = new();
    private VisualElement root;
    private VisualElement overlay;
    private VisualElement content;
    private VisualElement mainView;
    private VisualElement settingsView;
    private ScrollView options;
    private Label menuTitle;
    private Label mainMenuStatus;
    private Label status;
    private Label keyHint;
    private GameSettingsData originalSettings;
    private GameSettingsData editingSettings;
    private string originalBindings;
    private SettingsPage page;
    private CursorLockMode cursorLockBeforePause;
    private bool cursorVisibleBeforePause;
    private bool settingsOpen;
    private bool mainMenuMode;
    private Action startGameAction;
    private string mainMenuMessage;
    private int selectedIndex;
    private float nextNavigationAt;

    private void Awake()
    {
        IsOpen = false;
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer?.RegisterUIReloadCallback(HandleUIReload);
        SetVisible(false);
        if (GetComponent<CurrencyShopHUD>() == null)
            gameObject.AddComponent<CurrencyShopHUD>();
    }

    private void OnEnable()
    {
        GameSettingsService.Changed += RefreshScale;
        GameInputBindings.BindingsChanged += RefreshHint;
    }

    private void OnDisable()
    {
        GameSettingsService.Changed -= RefreshScale;
        GameInputBindings.BindingsChanged -= RefreshHint;
        Resume();
    }

    private void OnDestroy()
    {
        panelRenderer?.UnregisterUIReloadCallback(HandleUIReload);
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
            if (settingsOpen)
                CancelSettings();
            else if (IsOpen)
                Resume();
            else
                Pause();
        }

        if (!IsOpen)
            return;

        if (GameInput.UICancelPressed)
        {
            if (settingsOpen) CancelSettings();
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
                Select(i);
                commands[button]?.Invoke();
                return;
            }
        }

        float vertical = GameInput.UINavigate.y;
        if (Mathf.Abs(vertical) > .5f && Time.unscaledTime >= nextNavigationAt)
        {
            Select((selectedIndex + (vertical < 0f ? 1 : -1) + buttons.Count)
                % Mathf.Max(1, buttons.Count));
            nextNavigationAt = Time.unscaledTime + .17f;
        }
        if (GameInput.UISubmitPressed && buttons.Count > 0)
            commands[buttons[selectedIndex]]?.Invoke();
    }

    private void HandleUIReload(PanelRenderer _, VisualElement visualRoot, int __)
    {
        root = visualRoot;
        if (root == null)
            return;
        GetComponent<CurrencyShopHUD>()?.EnsureUI(root);
        overlay = root.Q<VisualElement>("pause-menu-overlay");
        content = root.Q<VisualElement>("pause-menu-content");
        mainView = root.Q<VisualElement>("pause-main-view");
        settingsView = root.Q<VisualElement>("pause-settings-view");
        options = root.Q<ScrollView>("settings-options");
        menuTitle = root.Q<Label>("pause-menu-title");
        mainMenuStatus = root.Q<Label>("main-menu-status");
        status = root.Q<Label>("settings-status");
        keyHint = root.Q<Label>("pause-key-hint");
        RebuildButtons();
        ApplyMenuModePresentation();
        SetVisible(IsOpen);
        RefreshScale();
        RefreshHint();
    }

    private void RebuildButtons()
    {
        buttons.Clear();
        commands.Clear();
        if (root == null)
            return;
        if (!settingsOpen)
        {
            Button primaryButton = root.Q<Button>("pause-resume-button");
            Button settingsButton = root.Q<Button>("pause-settings-button");
            Button quitButton = root.Q<Button>("pause-quit-button");
            if (primaryButton != null)
                primaryButton.text = mainMenuMode ? "NOVO JOGO" : "CONTINUAR";
            if (settingsButton != null)
                settingsButton.text = "CONFIGURAÇÕES";
            if (quitButton != null)
                quitButton.text = "SAIR";
            AddButton(primaryButton, mainMenuMode
                ? new Action(StartNewGame) : new Action(Resume));
            AddButton(settingsButton, OpenSettings);
            AddButton(quitButton, QuitPlaySession);
        }
        else
        {
            AddButton(root.Q<Button>("settings-audio-tab"), () => ShowPage(SettingsPage.Audio));
            AddButton(root.Q<Button>("settings-video-tab"), () => ShowPage(SettingsPage.Video));
            AddButton(root.Q<Button>("settings-camera-tab"), () => ShowPage(SettingsPage.Camera));
            AddButton(root.Q<Button>("settings-controls-tab"), () => ShowPage(SettingsPage.Controls));
            AddButton(root.Q<Button>("settings-accessibility-tab"), () => ShowPage(SettingsPage.Accessibility));
            BuildOptions();
            AddButton(root.Q<Button>("settings-defaults-button"), ResetCurrentSection);
            AddButton(root.Q<Button>("settings-apply-button"), ApplySettings);
            AddButton(root.Q<Button>("settings-cancel-button"), CancelSettings);
        }
        Select(0);
        ApplyMenuModePresentation();
    }

    private void AddButton(Button button, Action command)
    {
        if (button == null)
            return;
        buttons.Add(button);
        commands[button] = command;
    }

    private void Select(int index)
    {
        if (buttons.Count == 0)
            return;
        if (selectedIndex >= 0 && selectedIndex < buttons.Count)
            buttons[selectedIndex].RemoveFromClassList("settings-selected");
        selectedIndex = Mathf.Clamp(index, 0, buttons.Count - 1);
        buttons[selectedIndex].AddToClassList("settings-selected");
        buttons[selectedIndex].Focus();
        if (options != null && options.Contains(buttons[selectedIndex]))
            options.ScrollTo(buttons[selectedIndex]);
    }

    public void Pause()
    {
        if (IsOpen)
            return;
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
            CancelSettings();
        IsOpen = false;
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
        originalSettings = GameSettingsService.Snapshot();
        editingSettings = originalSettings.Copy();
        originalBindings = GameInputBindings.Snapshot();
        settingsOpen = true;
        page = SettingsPage.Audio;
        if (status != null) status.text = string.Empty;
        SetVisible(true);
        RebuildButtons();
    }

    private void ApplySettings()
    {
        GameSettingsService.Save(editingSettings);
        GameInputBindings.Save();
        settingsOpen = false;
        SetVisible(true);
        RebuildButtons();
    }

    private void CancelSettings()
    {
        GameInputBindings.CancelRebind();
        if (originalSettings != null)
            GameSettingsService.Preview(originalSettings);
        GameInputBindings.Restore(originalBindings);
        settingsOpen = false;
        SetVisible(true);
        RebuildButtons();
    }

    private void ShowPage(SettingsPage next)
    {
        page = next;
        RebuildButtons();
    }

    private void BuildOptions()
    {
        if (options == null || editingSettings == null)
            return;
        options.Clear();
        switch (page)
        {
            case SettingsPage.Audio:
                AddStep("Volume geral", editingSettings.masterVolume, 0f, 1f, .1f,
                    value => editingSettings.masterVolume = value);
                AddStep("Efeitos sonoros", editingSettings.effectsVolume, 0f, 1f, .1f,
                    value => editingSettings.effectsVolume = value);
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
                AddStep("Sensibilidade", editingSettings.cameraSensitivity, .2f, 3f, .1f,
                    value => editingSettings.cameraSensitivity = value, "0.0x");
                AddToggle("Inverter eixo vertical", editingSettings.invertCameraY,
                    value => editingSettings.invertCameraY = value);
                break;
            case SettingsPage.Controls:
                BuildControls();
                break;
            case SettingsPage.Accessibility:
                AddStep("Intensidade do tremor", editingSettings.cameraShake, 0f, 1f, .1f,
                    value => editingSettings.cameraShake = value, "0.0x");
                AddStep("Escala do menu", editingSettings.menuScale, .8f, 1.4f, .1f,
                    value => editingSettings.menuScale = value, "0.0x");
                break;
        }
    }

    private void AddStep(string label, float current, float minimum, float maximum,
        float step, Action<float> setter, string format = "P0")
    {
        Label title = new() { text = label + ": " + current.ToString(format) };
        title.AddToClassList("settings-option-label");
        options.Add(title);
        VisualElement row = new();
        row.style.flexDirection = FlexDirection.Row;
        options.Add(row);
        Button minus = new() { text = "−" };
        Button plus = new() { text = "+" };
        minus.style.width = 80;
        plus.style.width = 80;
        row.Add(minus);
        row.Add(plus);
        AddButton(minus, () => { setter(Mathf.Max(minimum,
            Mathf.Round((current - step) * 10f) / 10f)); PreviewAndRebuild(); });
        AddButton(plus, () => { setter(Mathf.Min(maximum,
            Mathf.Round((current + step) * 10f) / 10f)); PreviewAndRebuild(); });
    }

    private void AddToggle(string label, bool current, Action<bool> setter) =>
        AddOption(label + ": " + (current ? "SIM" : "NÃO"), () =>
        { setter(!current); PreviewAndRebuild(); });

    private void AddOption(string text, Action command)
    {
        Button button = new() { text = text };
        options.Add(button);
        AddButton(button, command);
    }

    private void PreviewAndRebuild()
    {
        GameSettingsService.Preview(editingSettings);
        RebuildButtons();
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
            RebuildButtons();
        });
    }

    private void ResetCurrentSection()
    {
        GameSettingsData defaults = GameSettingsService.CreateDefaults();
        switch (page)
        {
            case SettingsPage.Audio:
                editingSettings.masterVolume = defaults.masterVolume;
                editingSettings.effectsVolume = defaults.effectsVolume;
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
        if (menuTitle != null)
            menuTitle.text = mainMenuMode ? "PROJETO PSX" : "PAUSADO";
        if (overlay != null)
            overlay.EnableInClassList("main-menu-mode", mainMenuMode);
        if (keyHint != null)
        {
            keyHint.style.display = mainMenuMode
                ? DisplayStyle.None : DisplayStyle.Flex;
            if (!mainMenuMode)
                RefreshHint();
        }
        SetMainMenuMessage(mainMenuMessage);
    }

    private void RefreshScale()
    {
        if (content != null)
        {
            float scale = GameSettingsService.Applied.menuScale;
            content.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }
    }

    private void RefreshHint()
    {
        if (keyHint != null)
            keyHint.text = GameInputBindings.GetDisplayString("Pause") +
                ": PAUSAR / VOLTAR";
    }

    private bool IsPointerInside(VisualElement element, Vector2 screenPointer)
    {
        if (element == null || overlay == null ||
            Screen.width <= 0 || Screen.height <= 0)
            return false;
        // Usa o retângulo real do painel como referência: Game View e
        // PanelRenderer podem ter escalas diferentes. Mouse usa origem embaixo;
        // worldBound do UI Toolkit usa origem em cima.
        Rect viewport = overlay.worldBound;
        Vector2 panelPoint = new(
            viewport.xMin + screenPointer.x / Screen.width * viewport.width,
            viewport.yMin + (1f - screenPointer.y / Screen.height) * viewport.height);
        if (options != null && options.Contains(element) &&
            !options.worldBound.Contains(panelPoint))
            return false;
        return element.worldBound.Contains(panelPoint);
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

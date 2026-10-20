using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>Menu de pausa global em UI Toolkit, aberto por ESC.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PanelRenderer))]
public sealed class PauseMenuHUD : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("References")]
    [Tooltip("Panel Renderer que apresenta o UXML do menu de pausa.")]
    [SerializeField] private PanelRenderer panelRenderer;

    private VisualElement overlay;
    private Button resumeButton;
    private Button settingsButton;
    private Button quitButton;
    private float timeScaleBeforePause = 1f;
    private bool ownsPause;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        ResumeIfOwned();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    private void HandleUIReload(PanelRenderer _, VisualElement root, int __)
    {
        if (root == null)
            return;

        overlay = root.Q<VisualElement>("pause-menu-overlay");
        resumeButton = root.Q<Button>("pause-resume-button");
        settingsButton = root.Q<Button>("pause-settings-button");
        quitButton = root.Q<Button>("pause-quit-button");

        if (resumeButton != null)
            resumeButton.clicked += Resume;
        // Reservado para a futura tela de configuracoes.
        if (settingsButton != null)
            settingsButton.clicked += OpenSettingsPlaceholder;
        if (quitButton != null)
            quitButton.clicked += QuitPlaySession;

        SetVisible(IsPaused);
    }

    private void TogglePause()
    {
        if (IsPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        ownsPause = true;
        IsPaused = true;
        SetVisible(true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    public void Resume()
    {
        ResumeIfOwned();
        SetVisible(false);
    }

    private void ResumeIfOwned()
    {
        if (!ownsPause)
            return;

        Time.timeScale = timeScaleBeforePause;
        ownsPause = false;
        IsPaused = false;
    }

    private void OpenSettingsPlaceholder()
    {
        // Intencionalmente vazio: o botao ja faz parte do fluxo visual,
        // enquanto Configuracoes ainda nao tem tela definida.
    }

    private static void QuitPlaySession()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetVisible(bool visible)
    {
        if (overlay != null)
            overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

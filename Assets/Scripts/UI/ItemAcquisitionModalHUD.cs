using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Modal reutilizavel para destacar a obtencao de um item e pausar o jogo.</summary>
[DisallowMultipleComponent]
public sealed class ItemAcquisitionModalHUD : MonoBehaviour
{
    private static ItemAcquisitionModalHUD instance;
    private PanelRenderer panelRenderer;
    private VisualElement root;
    private VisualElement overlay;
    private Image image;
    private Label itemName;
    private Label closeHint;
    private bool isOpen;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private static int lastCloseFrame = -1;

    public static bool IsOpen => instance != null && instance.isOpen;
    public static bool BlocksMenuInput => IsOpen || Time.frameCount == lastCloseFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        instance = null;
        lastCloseFrame = -1;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }
        instance = this;
        panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        EnsureUI(GetPauseMenuRoot());
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        Close(false);
        if (instance == this)
            instance = null;
    }

    public static bool Show(KeyItemDefinition item)
    {
        if (item == null)
            return false;
        return Show(item.Image, item.ItemName, "[E / Clique] Fechar");
    }

    /// <summary>Exibe a mesma modal para uma imagem e titulo de qualquer sistema.</summary>
    public static bool Show(Sprite sprite, string title, string closeText = "[E] Fechar")
    {
        ItemAcquisitionModalHUD hud = instance != null
            ? instance : FindAnyObjectByType<ItemAcquisitionModalHUD>();
        if (hud == null)
        {
            Debug.LogWarning("[Inventario] Modal de item nao encontrada. Mantenha PauseMenuHUD ativo no GameRoot.");
            return false;
        }
        if (!hud.isOpen)
            hud.EnsureUI(hud.GetPauseMenuRoot());
        if (hud.overlay == null)
        {
            Debug.LogWarning("[Inventario] Elementos da modal nao encontrados no UXML do PauseMenuHUD.", hud);
            return false;
        }

        hud.image.sprite = sprite;
        hud.itemName.text = title ?? string.Empty;
        hud.closeHint.text = string.IsNullOrWhiteSpace(closeText) ? "[E] Fechar" : closeText;
        if (!hud.isOpen)
        {
            hud.previousCursorLock = UnityEngine.Cursor.lockState;
            hud.previousCursorVisible = UnityEngine.Cursor.visible;
            hud.isOpen = true;
            GamePauseController.RequestPause(hud);
        }
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        hud.overlay.style.display = DisplayStyle.Flex;
        hud.overlay.BringToFront();
        MenuUIAudio.Play(MenuAudioCue.Confirm);
        return true;
    }

    private void Update()
    {
        if (isOpen && (GameInput.UICloseModalPressed || GameInput.UICancelPressed ||
            GameInput.PausePressed || GameInput.UIClickPressed))
            Close();
    }

    public void Close() => Close(true);

    private void Close(bool playSound)
    {
        if (!isOpen)
            return;
        isOpen = false;
        GamePauseController.ReleasePause(this);
        if (overlay != null)
            overlay.style.display = DisplayStyle.None;
        UnityEngine.Cursor.lockState = previousCursorLock;
        UnityEngine.Cursor.visible = previousCursorVisible;
        lastCloseFrame = Time.frameCount;
        if (playSound)
            MenuUIAudio.Play(MenuAudioCue.Back);
    }

    private void EnsureUI(VisualElement visualRoot)
    {
        if (visualRoot == null || overlay != null && overlay.parent == visualRoot)
            return;
        HandleUIReload(panelRenderer, visualRoot, 0);
    }

    private VisualElement GetPauseMenuRoot()
    {
        PauseMenuHUD menu = GetComponent<PauseMenuHUD>();
        return menu != null ? menu.UIRoot : null;
    }

    private void HandleUIReload(PanelRenderer _, VisualElement visualRoot, int __)
    {
        root = visualRoot;
        if (root == null)
            return;
        overlay = root.Q<VisualElement>("item-acquisition-modal");
        image = root.Q<Image>("item-modal-image");
        itemName = root.Q<Label>("item-modal-name");
        closeHint = root.Q<Label>("item-modal-close-hint");
        if (overlay != null)
        {
            overlay.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (isOpen)
                overlay.BringToFront();
        }
    }
}

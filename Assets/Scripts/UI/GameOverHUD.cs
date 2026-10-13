using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Tela global de Game Over em UI Toolkit.
/// PlayerGameOver a encontra na cena quando o player morre.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class GameOverHUD : MonoBehaviour
{
    [SerializeField] private PanelRenderer panelRenderer;

    private VisualElement overlay;
    private Label deathMessageText;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
    }

    public void Show(string deathMessage)
    {
        if (deathMessageText != null)
            deathMessageText.text = deathMessage;

        SetVisible(true);
    }

    private void HandleUIReload(
        PanelRenderer _,
        VisualElement rootVisualElement,
        int __)
    {
        if (rootVisualElement == null)
            return;

        overlay = rootVisualElement.Q<VisualElement>("game-over-overlay");
        deathMessageText = rootVisualElement.Q<Label>("game-over-message");

        SetVisible(false);
    }

    private void SetVisible(bool value)
    {
        if (overlay != null)
        {
            overlay.style.display = value
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}

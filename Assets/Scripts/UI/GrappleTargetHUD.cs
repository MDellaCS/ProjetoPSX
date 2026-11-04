using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Indicador do alvo do gancho: um disco preenchido, grande e pulsante, com
/// tamanho constante na tela (visivel de longe) e desenhado por cima da cena.
/// Mostra o alvo do <see cref="GrappleTargetSystem"/>, entao funciona para
/// qualquer feature que registre um provedor. Estrutura em UXML/USS em
/// Resources/GrappleTargetHUD; criado sozinho no primeiro registro.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PanelRenderer))]
public sealed class GrappleTargetHUD : MonoBehaviour
{
    private const string LayoutResource = "GrappleTargetHUD/GrappleTargetHUD";
    private const string PanelSettingsResource = "GrappleTargetHUD/PanelSettings";
    private const float PulseSpeed = 2.6f;
    private const float PulseAmount = .16f;
    private const float EdgeMargin = 48f;

    private static GrappleTargetHUD instance;

    private PanelRenderer panelRenderer;
    private VisualElement root;
    private VisualElement marker;
    private VisualElement glow;
    private Camera worldCamera;
    private GrappleTargetKind shownKind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    /// <summary>Cria o HUD global na primeira vez que um provedor existe.</summary>
    public static void EnsureExists()
    {
        if (instance != null || !Application.isPlaying)
            return;

        VisualTreeAsset layout = Resources.Load<VisualTreeAsset>(LayoutResource);
        PanelSettings settings = Resources.Load<PanelSettings>(PanelSettingsResource);
        if (layout == null || settings == null)
        {
            Debug.LogError("GrappleTargetHUD nao encontrou seus assets em Resources/GrappleTargetHUD.");
            return;
        }

        GameObject host = new("Grapple Target HUD (Persistent)");
        host.SetActive(false);
        PanelRenderer renderer = host.AddComponent<PanelRenderer>();
        renderer.panelSettings = settings;
        renderer.visualTreeAsset = layout;
        GrappleTargetHUD hud = host.AddComponent<GrappleTargetHUD>();
        hud.panelRenderer = renderer;
        instance = hud;
        DontDestroyOnLoad(host);
        host.SetActive(true);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        if (instance == this)
            instance = null;
    }

    private void HandleUIReload(PanelRenderer _, VisualElement rootVisualElement, int __)
    {
        // O PanelRenderer pode notificar com a arvore ainda vazia; os elementos
        // sao resolvidos sob demanda em EnsureElements.
        root = rootVisualElement;
        marker = null;
        glow = null;
        shownKind = GrappleTargetKind.None;
    }

    private void EnsureElements()
    {
        if (marker != null || root == null)
            return;

        marker = root.Q<VisualElement>("grapple-marker");
        if (marker == null && root.childCount == 0 && panelRenderer != null &&
            panelRenderer.visualTreeAsset != null)
        {
            // Arvore vazia: instancia o UXML manualmente.
            panelRenderer.visualTreeAsset.CloneTree(root);
            marker = root.Q<VisualElement>("grapple-marker");
        }

        glow = root.Q<VisualElement>("grapple-glow");
    }

    private void LateUpdate()
    {
        EnsureElements();
        if (marker == null || root == null)
            return;

        GrappleTarget target = GamePauseController.IsPaused ||
            CutsceneController.IsPlayerControlLocked
            ? default : GrappleTargetSystem.Current;
        if (!target.IsValid)
        {
            Hide();
            return;
        }

        if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            worldCamera = Camera.main;
        float scaleX = root.resolvedStyle.width / Mathf.Max(1, Screen.width);
        float scaleY = root.resolvedStyle.height / Mathf.Max(1, Screen.height);
        if (worldCamera == null || scaleX <= 0f || scaleY <= 0f)
        {
            Hide();
            return;
        }

        Vector3 screen = worldCamera.WorldToScreenPoint(target.point);
        if (screen.z <= 0f)
        {
            Hide();
            return;
        }

        // Fora da tela, o marcador gruda na borda para continuar visivel.
        float x = Mathf.Clamp(screen.x, EdgeMargin, Screen.width - EdgeMargin);
        float y = Mathf.Clamp(screen.y, EdgeMargin, Screen.height - EdgeMargin);
        marker.style.left = x * scaleX;
        marker.style.top = (Screen.height - y) * scaleY;
        SetKind(target.kind);
        marker.style.display = DisplayStyle.Flex;

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * PulseSpeed * Mathf.PI * 2f) * PulseAmount;
        if (glow != null)
            glow.style.scale = new Scale(new Vector3(pulse * 1.1f, pulse * 1.1f, 1f));
        marker.style.scale = new Scale(new Vector3(1f + (pulse - 1f) * .35f,
            1f + (pulse - 1f) * .35f, 1f));
    }

    private void Hide()
    {
        if (marker != null)
            marker.style.display = DisplayStyle.None;
    }

    private void SetKind(GrappleTargetKind kind)
    {
        if (kind == shownKind)
            return;

        marker.RemoveFromClassList("grapple-marker--swing");
        marker.RemoveFromClassList("grapple-marker--enemy");
        marker.RemoveFromClassList("grapple-marker--ladder");
        marker.RemoveFromClassList("grapple-marker--custom");
        marker.AddToClassList(kind switch
        {
            GrappleTargetKind.SwingAnchor => "grapple-marker--swing",
            GrappleTargetKind.Enemy => "grapple-marker--enemy",
            GrappleTargetKind.Ladder => "grapple-marker--ladder",
            _ => "grapple-marker--custom"
        });
        shownKind = kind;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Barras de vida flutuantes dos WorldObjects, em UI Toolkit. Um unico
/// PanelRenderer (estrutura em UXML/USS em Resources) e reutilizado por todos
/// os objetos; as barras sao criadas sob demanda a partir de um template.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PanelRenderer))]
public sealed class WorldObjectHealthBarHUD : MonoBehaviour
{
    private const string LayoutResource = "WorldObjectHealthBarHUD/WorldObjectHealthBarHUD";
    private const string BarTemplateResource = "WorldObjectHealthBarHUD/WorldObjectHealthBar";
    private const string PanelSettingsResource = "WorldObjectHealthBarHUD/PanelSettings";
    private const float BarHeightPixels = 26f;
    private const float PlayerLookupInterval = 1f;

    private static WorldObjectHealthBarHUD instance;

    private readonly Dictionary<WorldObject, VisualElement> bars = new();
    private readonly List<WorldObject> removalBuffer = new();
    private readonly HashSet<WorldObject> usedThisFrame = new();
    private PanelRenderer panelRenderer;
    private VisualElement root;
    private VisualElement layer;
    private VisualTreeAsset barTemplate;
    private Camera worldCamera;
    private PlayerLockOn playerLockOn;
    private float nextPlayerLookup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    /// <summary>Cria o HUD na primeira vez que um WorldObject existe.</summary>
    public static void EnsureExists()
    {
        if (instance != null || !Application.isPlaying)
            return;

        VisualTreeAsset layout = Resources.Load<VisualTreeAsset>(LayoutResource);
        PanelSettings settings = Resources.Load<PanelSettings>(PanelSettingsResource);
        VisualTreeAsset template = Resources.Load<VisualTreeAsset>(BarTemplateResource);
        if (layout == null || settings == null || template == null)
        {
            Debug.LogError("WorldObjectHealthBarHUD nao encontrou seus assets em Resources/WorldObjectHealthBarHUD.");
            return;
        }

        GameObject rootObject = new("World Object Health Bars (Persistent)");
        rootObject.SetActive(false);
        PanelRenderer renderer = rootObject.AddComponent<PanelRenderer>();
        renderer.panelSettings = settings;
        renderer.visualTreeAsset = layout;
        WorldObjectHealthBarHUD hud = rootObject.AddComponent<WorldObjectHealthBarHUD>();
        hud.panelRenderer = renderer;
        hud.barTemplate = template;
        instance = hud;
        DontDestroyOnLoad(rootObject);
        rootObject.SetActive(true);
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

    private void HandleUIReload(PanelRenderer _, VisualElement rootVisualElement,
        int __)
    {
        if (rootVisualElement == null)
            return;

        root = rootVisualElement;
        layer = null;
        bars.Clear();
    }

    // O PanelRenderer pode notificar com a arvore vazia: resolve sob demanda e,
    // se continuar vazia, instancia o UXML manualmente.
    private void EnsureLayer()
    {
        if (layer != null || root == null)
            return;

        layer = root.Q<VisualElement>("world-bar-layer");
        if (layer == null && root.childCount == 0 && panelRenderer != null &&
            panelRenderer.visualTreeAsset != null)
        {
            panelRenderer.visualTreeAsset.CloneTree(root);
            layer = root.Q<VisualElement>("world-bar-layer");
        }
    }

    private void LateUpdate()
    {
        EnsureLayer();
        if (layer == null || root == null || barTemplate == null)
            return;

        float scaleX = root.resolvedStyle.width / Mathf.Max(1, Screen.width);
        float scaleY = root.resolvedStyle.height / Mathf.Max(1, Screen.height);
        if (scaleX <= 0f || scaleY <= 0f)
            return;

        if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            worldCamera = Camera.main;
        ResolvePlayerLockOn();

        usedThisFrame.Clear();
        IReadOnlyList<WorldObject> objects = WorldObject.Active;
        if (worldCamera != null)
        {
            for (int i = 0; i < objects.Count; i++)
                UpdateBar(objects[i], scaleX, scaleY);
        }

        HideUnusedAndRemoveDestroyed();
    }

    private void UpdateBar(WorldObject worldObject, float scaleX, float scaleY)
    {
        if (worldObject == null)
            return;

        bool lockedOn = playerLockOn != null &&
            playerLockOn.LockedTarget == worldObject.transform;
        if (!worldObject.ShouldShowHealthBar(lockedOn))
            return;

        Vector3 screen = worldCamera.WorldToScreenPoint(
            worldObject.HealthBarAnchor);
        if (screen.z <= 0f)
            return;

        if (!bars.TryGetValue(worldObject, out VisualElement bar))
        {
            TemplateContainer container = barTemplate.CloneTree();
            bar = container.Q<VisualElement>("world-bar");
            if (bar == null)
                return;
            layer.Add(container);
            container.pickingMode = PickingMode.Ignore;
            container.style.position = Position.Absolute;
            bars[worldObject] = bar;
        }

        VisualElement fill = bar.Q<VisualElement>("world-bar-fill");
        Label text = bar.Q<Label>("world-bar-text");
        if (fill != null)
            fill.style.width = Length.Percent(
                Mathf.Clamp01(worldObject.HealthBarFill) * 100f);
        if (text != null)
        {
            string value = worldObject.HealthBarText;
            text.text = value;
            text.style.display = string.IsNullOrEmpty(value)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }

        bar.style.left = screen.x * scaleX;
        bar.style.top = (Screen.height - screen.y) * scaleY - BarHeightPixels;
        bar.style.display = DisplayStyle.Flex;
        usedThisFrame.Add(worldObject);
    }

    private void HideUnusedAndRemoveDestroyed()
    {
        removalBuffer.Clear();
        foreach (KeyValuePair<WorldObject, VisualElement> pair in bars)
        {
            if (pair.Key == null)
            {
                removalBuffer.Add(pair.Key);
                continue;
            }

            if (!usedThisFrame.Contains(pair.Key))
                pair.Value.style.display = DisplayStyle.None;
        }

        // Objetos destruidos: remove o elemento do painel.
        foreach (WorldObject destroyedObject in removalBuffer)
        {
            if (bars.TryGetValue(destroyedObject, out VisualElement bar))
            {
                bar.parent?.RemoveFromHierarchy();
                bars.Remove(destroyedObject);
            }
        }
    }

    private void ResolvePlayerLockOn()
    {
        if (playerLockOn != null || Time.unscaledTime < nextPlayerLookup)
            return;

        nextPlayerLookup = Time.unscaledTime + PlayerLookupInterval;
        playerLockOn = FindAnyObjectByType<PlayerLockOn>();
    }
}

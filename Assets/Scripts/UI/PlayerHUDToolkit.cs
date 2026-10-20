using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class PlayerHUDToolkit : MonoBehaviour
{
    [SerializeField] private PanelRenderer panelRenderer;
    [SerializeField] private Transform playerTransform;

    private readonly List<VisualElement> coldNotches = new();
    private readonly List<VisualElement> hotNotches = new();
    private readonly List<VisualElement> superheatNotches = new();
    private PlayerHeat playerHeat;
    private PlayerMovement playerMovement;
    private Label heatValueText;
    private Label heatPowerText;
    private Label superheatValueText;
    private VisualElement dashIndicator;
    private Label dashText;
    private Label controlsHint;

    private void Awake()
    {
        if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null) panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        ResolvePlayerComponents();
    }

    private void OnDestroy()
    {
        if (panelRenderer != null) panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        GameInputBindings.BindingsChanged -= UpdateControlsHint;
    }

    private void OnEnable()
    {
        GameInputBindings.BindingsChanged += UpdateControlsHint;
    }

    private void OnDisable()
    {
        GameInputBindings.BindingsChanged -= UpdateControlsHint;
    }

    private void Update()
    {
        if (playerHeat == null || playerMovement == null) ResolvePlayerComponents();
        if (playerHeat == null || playerMovement == null) return;
        UpdateHeatNotches();
        UpdateDash();
    }

    private void HandleUIReload(PanelRenderer _, VisualElement root, int __)
    {
        if (root == null) return;
        heatValueText = root.Q<Label>("heat-value");
        heatPowerText = root.Q<Label>("heat-power-value");
        superheatValueText = root.Q<Label>("superheat-value");
        dashIndicator = root.Q<VisualElement>("dash-indicator");
        dashText = root.Q<Label>("dash-text");
        controlsHint = root.Q<Label>("combat-controls-hint");
        UpdateControlsHint();
        BuildNotches(root.Q<VisualElement>("heat-cold-notches"), coldNotches, 10, "heat-notch");
        BuildNotches(root.Q<VisualElement>("heat-hot-notches"), hotNotches, 10, "heat-notch");
        BuildNotches(root.Q<VisualElement>("superheat-notches"), superheatNotches, 5, "superheat-notch");
    }

    private static void BuildNotches(VisualElement parent, List<VisualElement> output, int count, string className)
    {
        output.Clear();
        if (parent == null) return;
        parent.Clear();
        for (int index = 0; index < count; index++)
        {
            VisualElement notch = new VisualElement();
            notch.AddToClassList(className);
            parent.Add(notch);
            output.Add(notch);
        }
    }

    private void ResolvePlayerComponents()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        if (playerTransform == null) return;
        playerHeat = playerTransform.GetComponent<PlayerHeat>();
        playerMovement = playerTransform.GetComponent<PlayerMovement>();
    }

    private void UpdateHeatNotches()
    {
        int heatNotch = playerHeat.CurrentHeatNotch;
        int coldCount = Mathf.Clamp(-heatNotch, 0, 10);
        int hotCount = Mathf.Clamp(heatNotch, 0, 10);
        int superheatCount = playerHeat.CurrentSuperheatNotch;

        for (int index = 0; index < coldNotches.Count; index++)
            SetNotchColor(coldNotches[index], ColdColor(index / 9f), index >= coldNotches.Count - coldCount);
        for (int index = 0; index < hotNotches.Count; index++)
            SetNotchColor(hotNotches[index], HotColor(index / 9f), index < hotCount);
        for (int index = 0; index < superheatNotches.Count; index++)
            SetNotchColor(superheatNotches[index], SuperheatColor(index / 4f), index < superheatCount);

        if (heatValueText != null) heatValueText.text = "HEAT " + FormatSigned(playerHeat.CurrentHeat) + " / " + playerHeat.MaxHeat.ToString("0");
        if (heatPowerText != null) heatPowerText.text = playerHeat.HeavyDamageMultiplier.ToString("0.#") + "x";
        if (superheatValueText != null) superheatValueText.text = "SUPERHEAT " + playerHeat.CurrentSuperheat.ToString("0.0") + " / " + playerHeat.MaxSuperheat.ToString("0");
    }

    private void UpdateDash()
    {
        bool ready = playerMovement.IsDashReady;
        if (dashIndicator != null) dashIndicator.style.backgroundColor = ready ? new Color(.9f, .96f, 1f) : new Color(.2f, .26f, .32f);
        if (dashText != null) dashText.text = ready ? "DASH READY" : "DASH " + playerMovement.DashCooldownRemaining.ToString("F1") + "s";
    }

    private void UpdateControlsHint()
    {
        if (controlsHint == null) return;
        controlsHint.text = "LEVE " + GameInputBindings.GetDisplayString("LightAttack") +
            "  |  PESADO " + GameInputBindings.GetDisplayString("HeavyAttack") +
            "  |  DASH " + GameInputBindings.GetDisplayString("Dash");
    }

    private static void SetNotchColor(VisualElement notch, Color color, bool active)
    {
        if (notch != null) notch.style.backgroundColor = active ? color : new Color(.12f, .14f, .17f, 1f);
    }

    private static Color ColdColor(float amount) => Color.Lerp(new Color(.04f, .13f, .35f), new Color(.2f, .85f, 1f), amount);
    private static Color HotColor(float amount) => Color.Lerp(new Color(1f, .75f, .08f), new Color(1f, .18f, .02f), amount);
    private static Color SuperheatColor(float amount) => Color.Lerp(new Color(.45f, 0f, .02f), new Color(1f, .08f, .02f), amount);
    private static string FormatSigned(float value) => (value > 0f ? "+" : string.Empty) + value.ToString("0.0");
}

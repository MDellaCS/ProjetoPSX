using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD jogavel em UI Toolkit. Representa os recursos atuais do player:
/// Heat, Superheat e disponibilidade de Dash.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class PlayerHUDToolkit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PanelRenderer panelRenderer;
    [SerializeField] private Transform playerTransform;

    private PlayerHeat playerHeat;
    private PlayerMovement playerMovement;

    private VisualElement heatColdFill;
    private VisualElement heatHotFill;
    private Label heatValueText;
    private VisualElement superheatFill;
    private Label superheatValueText;
    private VisualElement dashIndicator;
    private Label dashText;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);

        ResolvePlayerComponents();
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
    }

    private void Update()
    {
        if (playerHeat == null || playerMovement == null)
            ResolvePlayerComponents();

        if (playerHeat == null || playerMovement == null)
            return;

        UpdateHeat();
        UpdateSuperheat();
        UpdateDash();
    }

    private void HandleUIReload(
        PanelRenderer _,
        VisualElement rootVisualElement,
        int __)
    {
        if (rootVisualElement == null)
            return;

        heatColdFill = rootVisualElement.Q<VisualElement>("heat-cold-fill");
        heatHotFill = rootVisualElement.Q<VisualElement>("heat-hot-fill");
        heatValueText = rootVisualElement.Q<Label>("heat-value");
        superheatFill = rootVisualElement.Q<VisualElement>("superheat-fill");
        superheatValueText = rootVisualElement.Q<Label>("superheat-value");
        dashIndicator = rootVisualElement.Q<VisualElement>("dash-indicator");
        dashText = rootVisualElement.Q<Label>("dash-text");

        ValidateElement(heatColdFill, "heat-cold-fill");
        ValidateElement(heatHotFill, "heat-hot-fill");
        ValidateElement(heatValueText, "heat-value");
        ValidateElement(superheatFill, "superheat-fill");
        ValidateElement(superheatValueText, "superheat-value");
        ValidateElement(dashIndicator, "dash-indicator");
        ValidateElement(dashText, "dash-text");
    }

    private void ResolvePlayerComponents()
    {
        if (playerTransform == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        if (playerTransform == null)
            return;

        playerHeat = playerTransform.GetComponent<PlayerHeat>();
        playerMovement = playerTransform.GetComponent<PlayerMovement>();
    }

    private void UpdateHeat()
    {
        int heat = playerHeat.CurrentHeat;
        float coldAmount = heat < 0 && playerHeat.MinHeat < 0
            ? Mathf.Clamp01((float)heat / playerHeat.MinHeat)
            : 0f;
        float hotAmount = heat > 0 && playerHeat.MaxHeat > 0
            ? Mathf.Clamp01((float)heat / playerHeat.MaxHeat)
            : 0f;

        // Cada lado ocupa no maximo metade da barra, a partir do zero central.
        SetPercentWidth(heatColdFill, coldAmount * 50f);
        SetPercentWidth(heatHotFill, hotAmount * 50f);

        if (heatValueText != null)
        {
            string sign = heat > 0 ? "+" : string.Empty;
            heatValueText.text = "HEAT  " + sign + heat + " / " +
                playerHeat.MaxHeat;
        }
    }

    private void UpdateSuperheat()
    {
        float amount = playerHeat.MaxSuperheat > 0
            ? Mathf.Clamp01((float)playerHeat.CurrentSuperheat /
                playerHeat.MaxSuperheat)
            : 0f;

        SetPercentWidth(superheatFill, amount * 100f);

        if (superheatValueText != null)
        {
            superheatValueText.text = "SUPERHEAT  " +
                playerHeat.CurrentSuperheat + " / " +
                playerHeat.MaxSuperheat;
        }
    }

    private void UpdateDash()
    {
        bool ready = playerMovement.IsDashReady;

        if (dashIndicator != null)
        {
            dashIndicator.style.backgroundColor = ready
                ? new Color(0.9f, 0.96f, 1f, 1f)
                : new Color(0.2f, 0.26f, 0.32f, 1f);
        }

        if (dashText != null)
        {
            dashText.text = ready
                ? "DASH READY"
                : "DASH " + playerMovement.DashCooldownRemaining.ToString("F1") + "s";
        }
    }

    private static void SetPercentWidth(VisualElement element, float percent)
    {
        if (element != null)
            element.style.width = new Length(
                Mathf.Clamp(percent, 0f, 100f),
                LengthUnit.Percent
            );
    }

    private void ValidateElement(VisualElement element, string elementName)
    {
        if (element == null)
        {
            Debug.LogWarning(
                "PlayerHUDToolkit: nao encontrei o elemento '" +
                elementName + "' no UXML.",
                this
            );
        }
    }
}

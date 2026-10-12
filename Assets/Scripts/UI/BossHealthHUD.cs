using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD de chefe em UI Toolkit. Um unico PanelRenderer e reutilizado pelo
/// chefe atualmente ativo.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class BossHealthHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PanelRenderer panelRenderer;

    private BaseEnemy currentBoss;
    private BaseEnemy currentHealth;
    private string currentBossName;

    private VisualElement bossPanel;
    private Label bossNameText;
    private VisualElement healthFill;
    private Label healthValueText;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
    }

    private void OnDestroy()
    {
        UnsubscribeFromHealth();

        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
    }

    public void Show(BaseEnemy boss, string bossName)
    {
        if (boss == null)
            return;

        if (currentBoss != boss)
        {
            UnsubscribeFromHealth();
            currentBoss = boss;
            currentHealth = boss;

            if (currentHealth != null)
                currentHealth.HealthChanged += UpdateHealth;
        }

        currentBossName = string.IsNullOrWhiteSpace(bossName)
            ? boss.name
            : bossName;

        RefreshVisuals();
        SetVisible(true);
    }

    public void Hide(BaseEnemy boss)
    {
        if (boss != null && boss != currentBoss)
            return;

        UnsubscribeFromHealth();
        currentBoss = null;
        currentHealth = null;
        currentBossName = null;
        SetVisible(false);
    }

    private void HandleUIReload(
        PanelRenderer _,
        VisualElement rootVisualElement,
        int __)
    {
        if (rootVisualElement == null)
            return;

        bossPanel = rootVisualElement.Q<VisualElement>("boss-hud");
        bossNameText = rootVisualElement.Q<Label>("boss-name");
        healthFill = rootVisualElement.Q<VisualElement>("boss-health-fill");
        healthValueText = rootVisualElement.Q<Label>("boss-health-value");

        ValidateElement(bossPanel, "boss-hud");
        ValidateElement(bossNameText, "boss-name");
        ValidateElement(healthFill, "boss-health-fill");
        ValidateElement(healthValueText, "boss-health-value");

        RefreshVisuals();
        SetVisible(currentBoss != null);
    }

    private void UpdateHealth(BaseEnemy _, int current, int maximum)
    {
        UpdateHealthVisuals(current, maximum);
    }

    private void RefreshVisuals()
    {
        if (currentBoss != null && bossNameText != null)
            bossNameText.text = currentBossName;

        if (currentHealth != null)
        {
            UpdateHealthVisuals(
                currentHealth.CurrentHealth,
                currentHealth.MaxHealth
            );
        }
    }

    private void UpdateHealthVisuals(int current, int maximum)
    {
        float amount = maximum > 0
            ? Mathf.Clamp01((float)current / maximum)
            : 0f;

        if (healthFill != null)
        {
            healthFill.style.width = new Length(
                amount * 100f,
                LengthUnit.Percent
            );
        }

        if (healthValueText != null)
            healthValueText.text = current + " / " + maximum;
    }

    private void UnsubscribeFromHealth()
    {
        if (currentHealth != null)
            currentHealth.HealthChanged -= UpdateHealth;
    }

    private void SetVisible(bool value)
    {
        if (bossPanel != null)
        {
            bossPanel.style.display = value
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }

    private void ValidateElement(VisualElement element, string elementName)
    {
        if (element == null)
        {
            Debug.LogWarning(
                "BossHealthHUD: nao encontrei o elemento '" +
                elementName + "' no UXML.",
                this
            );
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class PlayerHUDToolkit : MonoBehaviour
{
    [Tooltip("PanelRenderer que desenha o PlayerHUD e seu filho visual PlayerHeatHUD.")]
    [SerializeField] private PanelRenderer panelRenderer;
    [Tooltip("Transform do jogador acompanhado por este HUD. Se vazio, procura o objeto com a tag Player.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Atraso em segundos antes de atualizar o numero do multiplicador depois que o poder do Heat muda.")]
    [Min(0f)] [SerializeField] private float multiplierChangeDelay = .2f;
    [Header("Indicacao de dano")]
    [Tooltip("Tempo em segundos que o trecho perdido permanece visivel antes de desaparecer.")]
    [Min(0f)] [SerializeField] private float damageLossHoldSeconds = 1.2f;
    [Tooltip("Tempo em segundos para o trecho perdido desaparecer gradualmente.")]
    [Min(0f)] [SerializeField] private float damageLossFadeSeconds = .5f;
    [Header("Posicao do Termometro")]
    [Tooltip("Posicao vertical do centro do termometro como porcentagem da tela. 50 e o centro; valores maiores movem para baixo.")]
    [Range(0f, 100f)] [SerializeField] private float thermometerVerticalPositionPercent = 57f;
    [Tooltip("Largura total do painel do termometro em pixels da interface.")]
    [Min(1f)] [SerializeField] private float thermometerPanelWidth = 520f;
    [Tooltip("Altura minima reservada ao painel do termometro em pixels. O painel cresce se o multiplicador precisar de mais espaco.")]
    [Min(1f)] [SerializeField] private float thermometerMinimumPanelHeight = 64f;
    [Header("Visual do Multiplicador")]
    [Tooltip("Escala visual do numero do multiplicador na HUD.")]
    [Min(1f)] [SerializeField] private float multiplierVisualScale = 1.7f;
    [Tooltip("Tamanho inicial do multiplicador exibido ao lado do termometro, em pontos de fonte.")]
    [Min(1f)] [SerializeField] private float multiplierBaseFontSize = 26f;
    [Tooltip("Aumento do tamanho da fonte por cada ponto de multiplicador acima de 1x.")]
    [Min(0f)] [SerializeField] private float multiplierFontSizePerStep = 3.5f;
    [Tooltip("Limite maximo do tamanho do numero do multiplicador para evitar que cubra o termometro.")]
    [Min(1f)] [SerializeField] private float multiplierMaximumFontSize = 64f;
    [Tooltip("Graus de rotacao aplicados por ponto de multiplicador em relacao a 1x..")]
    [Min(0f)] [SerializeField] private float multiplierRotationPerStep = 3f;
    [Tooltip("Inclinacao maxima em graus para multiplicadores acima de 1x.")]
    [Min(0f)] [SerializeField] private float multiplierMaximumHotRotation = 35f;
    [Tooltip("Gradiente de cor do multiplicador. A posicao 0 representa poder frio minimo e 1 representa poder quente maximo.")]
    [SerializeField] private Gradient multiplierColors = CreateDefaultMultiplierGradient();

    [Header("Visual do Termometro")]
    [Tooltip("Multiplica o tamanho dos quadrados de Heat para torna-los mais visiveis.")]
    [Min(1f)] [SerializeField] private float notchVisualScale = 2f;
    [Tooltip("Quantidade de notches entre separadores maiores no termometro.")]
    [Min(1)] [SerializeField] private int notchesPerMajorDivider = 5;
    [Tooltip("Largura de cada notch em pixels da interface.")]
    [Min(1f)] [SerializeField] private float thermometerNotchWidth = 12f;
    [Tooltip("Altura da barra principal de Heat em pixels da interface.")]
    [Min(1f)] [SerializeField] private float thermometerBarHeight = 9f;
    [Tooltip("Cor do trecho vazio das barras de Heat.")]
    [SerializeField] private Color emptyNotchColor = new(.11f, .13f, .16f, 1f);
    [Tooltip("Cor das divisoes pequenas entre notches.")]
    [SerializeField] private Color minorDividerColor = new(.02f, .03f, .05f, .9f);
    [Tooltip("Cor das divisoes maiores entre grupos de notches.")]
    [SerializeField] private Color majorDividerColor = new(.75f, .78f, .82f, .9f);
    [Tooltip("Espessura das divisoes maiores entre grupos de notches, em pixels.")]
    [Min(1f)] [SerializeField] private float majorDividerWidth = 3f;
    [Tooltip("Gradiente de preenchimento dos notches positivos de Heat, do primeiro notch ao limite visual.")]
    [SerializeField] private Gradient heatBarColors = CreateDefaultHeatGradient();

    private readonly List<VisualElement> hotNotches = new();
    private PlayerHeat playerHeat;
    private PlayerHealth playerHealth;
    private PlayerMovesetController movesetController;
    private VisualElement playerHudRoot;
    private VisualElement heatThermometer;
    private VisualElement heatMultiplier;
    private VisualElement healthBar;
    private VisualElement vitalOilBar;
    private VisualElement vitalOilFill;
    private VisualElement vitalOilLoss;
    private VisualElement healthFill;
    private VisualElement healthLoss;
    private Label healthValue;
    private Label heatPowerText;
    private VisualElement hotNotchParent;
    private VisualElement thermometerMainRow;
    private float displayedMultiplier;
    private float pendingMultiplier;
    private float pendingMultiplierElapsed;
    private float animatedFontSize;
    private float animatedRotation;
    private Color animatedMultiplierColor;
    private bool multiplierInitialized;
    private float heatLossValue, healthLossValue, vitalOilLossValue;
    private float heatLossAt, healthLossAt, vitalOilLossAt;
    private PlayerHealth subscribedHealth;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        ResolvePlayerComponents();
    }

    private void OnValidate()
    {
        notchesPerMajorDivider = Mathf.Max(1, notchesPerMajorDivider);
        multiplierMaximumFontSize = Mathf.Max(multiplierBaseFontSize,
            multiplierMaximumFontSize);
        if (multiplierColors == null)
            multiplierColors = CreateDefaultMultiplierGradient();
        if (heatBarColors == null)
            heatBarColors = CreateDefaultHeatGradient();
    }

    private void OnDestroy()
    {
        UnsubscribeHealth();
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
    }

    private void OnDisable() => UnsubscribeHealth();
    private void OnEnable() => ResolvePlayerComponents();

    private void Update()
    {
        if (playerHeat == null || playerHealth == null || movesetController == null)
            ResolvePlayerComponents();
        bool usesHealth = movesetController != null && movesetController.UsesHealth;
        if (heatThermometer != null)
            heatThermometer.style.display = usesHealth || PlayerDebugToolkitHUD.IsDebugMenuVisible
                ? DisplayStyle.None : DisplayStyle.Flex;
        if (heatMultiplier != null)
            heatMultiplier.style.display = usesHealth || PlayerDebugToolkitHUD.IsDebugMenuVisible
                ? DisplayStyle.None : DisplayStyle.Flex;
        if (playerHeat == null || playerHealth == null)
            return;

        if (healthBar != null)
            healthBar.style.display = usesHealth ? DisplayStyle.Flex : DisplayStyle.None;
        if (vitalOilBar != null)
            vitalOilBar.style.display = usesHealth ? DisplayStyle.None : DisplayStyle.Flex;
        if (vitalOilFill != null)
            vitalOilFill.style.width = Length.Percent(playerHealth.VitalOilPercent * 100f);
        UpdateLossBar(vitalOilLoss, vitalOilLossValue / playerHealth.MaxVitalOil, vitalOilLossAt);
        UpdateHealthBar();
        if (usesHealth)
            return;

        UpdateThermometer();
        UpdateMultiplierPresentation();
    }

    private void HandleUIReload(PanelRenderer _, VisualElement root, int __)
    {
        if (root == null)
            return;
        lastHealthTextCurrent = int.MinValue;

        playerHudRoot = root.Q<VisualElement>("player-hud") ?? root;
        playerHudRoot.style.position = Position.Absolute;
        playerHudRoot.style.left = 0f;
        playerHudRoot.style.top = 0f;
        playerHudRoot.style.right = 0f;
        playerHudRoot.style.bottom = 0f;
        playerHudRoot.style.width = Length.Percent(100f);
        playerHudRoot.style.height = Length.Percent(100f);
        playerHudRoot.style.overflow = Overflow.Visible;

        heatPowerText = root.Q<Label>("heat-power-value");
        heatThermometer = root.Q<VisualElement>("heat-thermometer");
        heatMultiplier = root.Q<VisualElement>("heat-multiplier");
        vitalOilBar = root.Q<VisualElement>("vital-oil-bar");
        vitalOilFill = root.Q<VisualElement>("vital-oil-fill");
        vitalOilLoss = root.Q<VisualElement>("vital-oil-loss");
        healthBar = root.Q<VisualElement>("human-health-bar");
        healthFill = root.Q<VisualElement>("human-health-fill");
        healthLoss = root.Q<VisualElement>("human-health-loss");
        healthValue = root.Q<Label>("human-health-value");
        VisualElement thermometer = root.Q<VisualElement>("heat-thermometer");
        if (thermometer != null)
        {
            thermometer.style.position = Position.Absolute;
            thermometer.style.left = Length.Percent(50f);
            thermometer.style.top = Length.Percent(thermometerVerticalPositionPercent);
            thermometer.style.width = thermometerPanelWidth;
            thermometer.style.marginLeft = -thermometerPanelWidth * .5f;
            thermometer.style.minHeight = thermometerMinimumPanelHeight;
            thermometer.style.flexShrink = 0f;
            thermometer.style.overflow = Overflow.Visible;
            thermometer.UnregisterCallback<GeometryChangedEvent>(CenterThermometerVertically);
            thermometer.RegisterCallback<GeometryChangedEvent>(CenterThermometerVertically);
        }
        if (heatPowerText != null)
            heatPowerText.style.overflow = Overflow.Visible;
        hotNotchParent = root.Q<VisualElement>("thermometer-hot-notches");
        thermometerMainRow = root.Q<VisualElement>("thermometer-main-row");
        BuildNotches(hotNotchParent, hotNotches, 10, "thermometer-notch");

        multiplierInitialized = false;
        ResolvePlayerComponents();
        if (playerHeat != null)
        {
            UpdateThermometer();
            UpdateMultiplierPresentation();
        }
    }

    private void CenterThermometerVertically(GeometryChangedEvent evt)
    {
        VisualElement thermometer = (VisualElement)evt.currentTarget;
        if (thermometer != null && evt.newRect.height > 0f)
            thermometer.style.marginTop = -evt.newRect.height * .5f;
    }

    private static void BuildNotches(VisualElement parent,
        List<VisualElement> output, int count, string className)
    {
        output.Clear();
        if (parent == null)
            return;

        parent.Clear();
        for (int index = 0; index < count; index++)
        {
            VisualElement notch = new();
            notch.AddToClassList(className);
            VisualElement loss = new();
            loss.AddToClassList("thermometer-notch-loss");
            notch.Add(loss);
            VisualElement fill = new();
            fill.AddToClassList("thermometer-notch-fill");
            notch.Add(fill);
            parent.Add(notch);
            output.Add(notch);
        }
    }

    private void ResolvePlayerComponents()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            playerHeat = playerTransform.GetComponent<PlayerHeat>();
            PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
            if (subscribedHealth != health)
            {
                UnsubscribeHealth();
                subscribedHealth = health;
                if (subscribedHealth != null)
                    subscribedHealth.ResourcesLostToDamage += HandleResourceDamage;
                heatLossValue = healthLossValue = vitalOilLossValue = 0f;
            }
            playerHealth = health;
            movesetController = playerTransform.GetComponent<PlayerMovesetController>();
        }
    }

    private void UnsubscribeHealth()
    {
        if (subscribedHealth != null)
            subscribedHealth.ResourcesLostToDamage -= HandleResourceDamage;
        subscribedHealth = null;
    }

    private void HandleResourceDamage(float heatLost, int healthLost, int vitalOilLost)
    {
        float now = Time.time;
        if (heatLost > 0f && playerHeat != null)
        {
            heatLossValue = Mathf.Max(LossOpacity(heatLossAt) > 0f ? heatLossValue : 0f,
                playerHeat.CurrentHeat + heatLost);
            heatLossAt = now;
        }
        if (healthLost > 0 && playerHealth != null)
        {
            healthLossValue = Mathf.Max(LossOpacity(healthLossAt) > 0f ? healthLossValue : 0f,
                playerHealth.CurrentHealth + healthLost);
            healthLossAt = now;
        }
        if (vitalOilLost > 0 && playerHealth != null)
        {
            vitalOilLossValue = Mathf.Max(LossOpacity(vitalOilLossAt) > 0f ? vitalOilLossValue : 0f,
                playerHealth.CurrentVitalOil + vitalOilLost);
            vitalOilLossAt = now;
        }
    }

    private float LossOpacity(float occurredAt)
    {
        float elapsed = Time.time - occurredAt;
        if (elapsed <= damageLossHoldSeconds) return 1f;
        return damageLossFadeSeconds > 0f
            ? 1f - Mathf.Clamp01((elapsed - damageLossHoldSeconds) / damageLossFadeSeconds)
            : 0f;
    }

    private void UpdateLossBar(VisualElement bar, float fraction, float occurredAt)
    {
        if (bar == null) return;
        bar.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
        bar.style.opacity = LossOpacity(occurredAt);
    }

    private void UpdateHealthBar()
    {
        if (playerHealth == null)
            return;

        float percent = playerHealth.HealthPercent;
        if (healthFill != null)
            healthFill.style.width = Length.Percent(percent * 100f);
        UpdateLossBar(healthLoss, healthLossValue / playerHealth.MaxHealth, healthLossAt);
        if (healthValue != null)
        {
            int currentHp = playerHealth.CurrentHealth;
            int maxHp = playerHealth.MaxHealth;
            if (currentHp != lastHealthTextCurrent || maxHp != lastHealthTextMax)
            {
                lastHealthTextCurrent = currentHp;
                lastHealthTextMax = maxHp;
                healthValue.text = $"{currentHp} / {maxHp}";
            }
        }
    }

    // Cache do texto de vida para evitar alocar string e invalidar layout a cada frame.
    private int lastHealthTextCurrent = int.MinValue;
    private int lastHealthTextMax = int.MinValue;

    private void UpdateThermometer()
    {
        float notchSize = Mathf.Max(thermometerNotchWidth, thermometerBarHeight) *
            Mathf.Max(1f, notchVisualScale);
        if (thermometerMainRow != null)
            thermometerMainRow.style.height = notchSize;

        int requiredHotNotches = Mathf.Max(1,
            Mathf.CeilToInt(playerHeat.MaxHeat / 10f));
        if (requiredHotNotches != hotNotches.Count)
            BuildNotches(hotNotchParent, hotNotches, requiredHotNotches,
                "thermometer-notch");

        bool heavyConsumesHeat = !playerHeat.IsBurning && !playerHeat.IsRaging;

        for (int index = 0; index < hotNotches.Count; index++)
        {
            float capacity = Mathf.Clamp01(playerHeat.MaxHeat / 10f - index);
            float filled = capacity > 0f
                ? Mathf.Clamp01((playerHeat.CurrentHeat / 10f - index) / capacity)
                : 0f;
            float lostFill = capacity > 0f
                ? Mathf.Clamp01((heatLossValue / 10f - index) / capacity) : 0f;
            UpdateNotchVisual(hotNotches[index],
                EvaluateGradient(heatBarColors, Mathf.Clamp01(index / 9f)),
                filled, lostFill, notchSize * capacity, notchSize,
                heavyConsumesHeat && filled > 0f);
        }


    }

    private void UpdateMultiplierPresentation()
    {
        if (heatPowerText == null)
            return;

        float targetMultiplier = playerHeat.HeavyDamageMultiplier;
        if (!multiplierInitialized)
        {
            displayedMultiplier = targetMultiplier;
            pendingMultiplier = targetMultiplier;
            pendingMultiplierElapsed = 0f;
            animatedFontSize = GetMultiplierFontSize(displayedMultiplier);
            animatedRotation = GetMultiplierRotation(displayedMultiplier);
            animatedMultiplierColor = GetMultiplierColor(displayedMultiplier);
            multiplierInitialized = true;
        }
        else if (!Mathf.Approximately(targetMultiplier, displayedMultiplier))
        {
            if (!Mathf.Approximately(targetMultiplier, pendingMultiplier))
            {
                pendingMultiplier = targetMultiplier;
                pendingMultiplierElapsed = 0f;
            }

            pendingMultiplierElapsed += Time.deltaTime;
            if (multiplierChangeDelay <= 0f ||
                pendingMultiplierElapsed >= multiplierChangeDelay)
            {
                displayedMultiplier = pendingMultiplier;
                pendingMultiplierElapsed = 0f;
            }
        }
        else
        {
            pendingMultiplier = targetMultiplier;
            pendingMultiplierElapsed = 0f;
        }

        float blend = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
        animatedFontSize = Mathf.Lerp(animatedFontSize,
            GetMultiplierFontSize(displayedMultiplier), blend);
        animatedRotation = Mathf.Lerp(animatedRotation,
            GetMultiplierRotation(displayedMultiplier), blend);
        animatedMultiplierColor = Color.Lerp(animatedMultiplierColor,
            GetMultiplierColor(displayedMultiplier), blend);

        heatPowerText.text = displayedMultiplier.ToString("0.#") + "x";
        heatPowerText.style.fontSize = animatedFontSize;
        heatPowerText.style.rotate = new Rotate(new Angle(
            animatedRotation, AngleUnit.Degree));
        heatPowerText.style.color = animatedMultiplierColor;
    }

    private void UpdateNotchVisual(VisualElement notch, Color color,
        float fillAmount, float lossAmount, float width, float height, bool heavyCost)
    {
        if (notch == null)
            return;

        bool major = notchesPerMajorDivider > 0 &&
            (notch.parent.IndexOf(notch) + 1) % notchesPerMajorDivider == 0;
        notch.style.width = width;
        notch.style.height = height;
        notch.style.marginLeft = 0f;
        notch.style.backgroundColor = emptyNotchColor;
        notch.EnableInClassList("thermometer-notch-heavy-cost", heavyCost);
        notch.style.borderRightWidth = heavyCost ? 3f : major ? majorDividerWidth : 1f;
        notch.style.borderRightColor = heavyCost
            ? Color.black : major ? majorDividerColor : minorDividerColor;

        VisualElement loss = notch.Q<VisualElement>(className: "thermometer-notch-loss");
        if (loss != null)
        {
            Color lossColor = color;
            lossColor.a *= .45f * LossOpacity(heatLossAt);
            loss.style.backgroundColor = lossColor;
            loss.style.width = Length.Percent(Mathf.Clamp01(lossAmount) * 100f);
        }

        VisualElement fill = notch.Q<VisualElement>(
            className: "thermometer-notch-fill");
        if (fill == null)
            return;

        fill.style.backgroundColor = color;
        fill.style.width = Length.Percent(Mathf.Clamp01(fillAmount) * 100f);
    }

    private float GetMultiplierFontSize(float multiplier)
    {
        float size = multiplierBaseFontSize +
            (Mathf.Max(1f, multiplier) - 1f) * multiplierFontSizePerStep;
        return Mathf.Clamp(size, multiplierBaseFontSize,
            multiplierMaximumFontSize) * Mathf.Max(1f, multiplierVisualScale);
    }

    private float GetMultiplierRotation(float multiplier)
    {
        float rotation = -(multiplier - 1f) * multiplierRotationPerStep;
        return Mathf.Clamp(rotation, -multiplierMaximumHotRotation,
            0f);
    }

    private Color GetMultiplierColor(float multiplier)
    {
        return EvaluateGradient(multiplierColors,
            Mathf.Clamp01(multiplier / 10f));
    }

    private static Color EvaluateGradient(Gradient gradient, float time) =>
        gradient == null ? Color.white : gradient.Evaluate(Mathf.Clamp01(time));


    private static Gradient CreateDefaultHeatGradient() => CreateGradient(
        new GradientColorKey(new Color(1f, .75f, .08f), 0f),
        new GradientColorKey(new Color(1f, .18f, .02f), 1f));


    private static Gradient CreateDefaultMultiplierGradient()
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(.3f, .75f, 1f), 0f),
                new GradientColorKey(new Color(1f, .9f, .58f), .1f),
                new GradientColorKey(new Color(1f, .48f, .12f), .4f),
                new GradientColorKey(new Color(1f, .12f, .06f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
        return gradient;
    }

    private static Gradient CreateGradient(GradientColorKey start,
        GradientColorKey end)
    {
        Gradient gradient = new();
        gradient.SetKeys(new[] { start, end }, new[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        });
        return gradient;
    }
}

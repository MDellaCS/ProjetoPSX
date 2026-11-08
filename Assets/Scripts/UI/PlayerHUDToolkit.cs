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
    [Header("Posicao do Termometro")]
    [Tooltip("Posicao vertical do centro do termometro como porcentagem da tela. 50 e o centro; valores maiores movem para baixo.")]
    [Range(0f, 100f)] [SerializeField] private float thermometerVerticalPositionPercent = 57f;
    [Tooltip("Largura total do painel do termometro em pixels da interface.")]
    [Min(1f)] [SerializeField] private float thermometerPanelWidth = 520f;
    [Tooltip("Altura minima reservada ao painel do termometro em pixels. O painel cresce se o multiplicador precisar de mais espaco.")]
    [Min(1f)] [SerializeField] private float thermometerMinimumPanelHeight = 64f;
    [Header("Visual do Multiplicador")]
    [Tooltip("Tamanho inicial do multiplicador exibido ao lado do termometro, em pontos de fonte.")]
    [Min(1f)] [SerializeField] private float multiplierBaseFontSize = 26f;
    [Tooltip("Aumento do tamanho da fonte por cada ponto de multiplicador acima de 1x. Valores de Cold mantem o tamanho-base.")]
    [Min(0f)] [SerializeField] private float multiplierFontSizePerStep = 3.5f;
    [Tooltip("Limite maximo do tamanho do numero do multiplicador para evitar que cubra o termometro.")]
    [Min(1f)] [SerializeField] private float multiplierMaximumFontSize = 64f;
    [Tooltip("Graus de rotacao aplicados por ponto de multiplicador em relacao a 1x. Valores positivos inclinam para um lado e Cold para o outro.")]
    [Min(0f)] [SerializeField] private float multiplierRotationPerStep = 3f;
    [Tooltip("Inclinacao maxima em graus para multiplicadores acima de 1x.")]
    [Min(0f)] [SerializeField] private float multiplierMaximumHotRotation = 35f;
    [Tooltip("Inclinacao maxima em graus para multiplicadores abaixo de 1x, incluindo Cold.")]
    [Min(0f)] [SerializeField] private float multiplierMaximumColdRotation = 25f;
    [Tooltip("Gradiente de cor do multiplicador. A posicao 0 representa poder frio minimo e 1 representa poder quente maximo.")]
    [SerializeField] private Gradient multiplierColors = CreateDefaultMultiplierGradient();

    [Header("Visual do Termometro")]
    [Tooltip("Quantidade de notches entre separadores maiores no termometro e na barra de Superheat.")]
    [Min(1)] [SerializeField] private int notchesPerMajorDivider = 5;
    [Tooltip("Largura de cada notch em pixels da interface.")]
    [Min(1f)] [SerializeField] private float thermometerNotchWidth = 12f;
    [Tooltip("Altura da barra principal de Heat e Cold em pixels da interface.")]
    [Min(1f)] [SerializeField] private float thermometerBarHeight = 9f;
    [Tooltip("Altura da barra de Superheat em pixels da interface.")]
    [Min(1f)] [SerializeField] private float superheatBarHeight = 3f;
    [Tooltip("Cor do trecho vazio das barras de Heat, Cold e Superheat.")]
    [SerializeField] private Color emptyNotchColor = new(.11f, .13f, .16f, 1f);
    [Tooltip("Cor das divisoes pequenas entre notches.")]
    [SerializeField] private Color minorDividerColor = new(.02f, .03f, .05f, .9f);
    [Tooltip("Cor das divisoes maiores entre grupos de notches.")]
    [SerializeField] private Color majorDividerColor = new(.75f, .78f, .82f, .9f);
    [Tooltip("Espessura das divisoes maiores entre grupos de notches, em pixels.")]
    [Min(1f)] [SerializeField] private float majorDividerWidth = 3f;
    [Tooltip("Cor do marcador que separa Cold e Heat no zero.")]
    [SerializeField] private Color zeroMarkerColor = new(.92f, .94f, .97f, 1f);
    [Tooltip("Gradiente de preenchimento dos notches negativos de Cold, do valor mais baixo ate o zero.")]
    [SerializeField] private Gradient coldBarColors = CreateDefaultColdGradient();
    [Tooltip("Gradiente de preenchimento dos notches positivos de Heat, do primeiro notch ao limite visual.")]
    [SerializeField] private Gradient heatBarColors = CreateDefaultHeatGradient();
    [Tooltip("Gradiente de preenchimento dos notches de Superheat, do primeiro ao ultimo.")]
    [SerializeField] private Gradient superheatBarColors = CreateDefaultSuperheatGradient();

    private readonly List<VisualElement> coldNotches = new();
    private readonly List<VisualElement> hotNotches = new();
    private readonly List<VisualElement> superheatNotches = new();
    private PlayerHeat playerHeat;
    private PlayerHealth playerHealth;
    private PlayerMovesetController movesetController;
    private PlayerLockOn playerLockOn;
    private Camera lockOnCamera;
    private VisualElement playerHudRoot;
    private VisualElement lockOnMarker;
    private VisualElement heatThermometer;
    private VisualElement healthBar;
    private VisualElement healthFill;
    private Label healthValue;
    private Label heatPowerText;
    private VisualElement hotNotchParent;
    private VisualElement zeroNotch;
    private VisualElement thermometerMainRow;
    private VisualElement superheatNotchParent;
    private VisualElement thermometerHeading;
    private VisualElement comboMeter;
    private VisualElement comboTimerFill;
    private Label comboTitle;
    private Label comboCount;
    private Label comboReward;
    private PlayerComboSystem playerComboSystem;
    private int lastDisplayedComboCount = int.MinValue;
    private float comboBreakVisibleUntil;
    private float comboRewardVisibleUntil;
    private float displayedMultiplier;
    private float pendingMultiplier;
    private float pendingMultiplierElapsed;
    private float animatedFontSize;
    private float animatedRotation;
    private Color animatedMultiplierColor;
    private bool multiplierInitialized;

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
        if (coldBarColors == null)
            coldBarColors = CreateDefaultColdGradient();
        if (heatBarColors == null)
            heatBarColors = CreateDefaultHeatGradient();
        if (superheatBarColors == null)
            superheatBarColors = CreateDefaultSuperheatGradient();
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        UnsubscribeComboSystem();
    }

    private void OnDisable() => UnsubscribeComboSystem();

    private void OnEnable() => ResolvePlayerComponents();

    private void Update()
    {
        if (playerHeat == null || playerHealth == null || movesetController == null ||
            playerComboSystem == null)
            ResolvePlayerComponents();
        if (playerLockOn == null && playerTransform != null)
            playerLockOn = playerTransform.GetComponent<PlayerLockOn>();
        UpdateLockOnMarker();
        UpdateComboMeter();
        if (playerHeat == null || playerHealth == null)
            return;

        bool usesHealth = movesetController != null && movesetController.UsesHealth;
        if (heatThermometer != null)
            heatThermometer.style.display = usesHealth ? DisplayStyle.None : DisplayStyle.Flex;
        if (healthBar != null)
            healthBar.style.display = usesHealth ? DisplayStyle.Flex : DisplayStyle.None;
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
        lockOnMarker = root.Q<VisualElement>("lock-on-marker");
        comboMeter = root.Q<VisualElement>("player-combo-meter");
        comboTitle = root.Q<Label>("player-combo-title");
        comboCount = root.Q<Label>("player-combo-count");
        comboTimerFill = root.Q<VisualElement>("player-combo-timer-fill");
        comboReward = root.Q<Label>("player-combo-reward");

        heatPowerText = root.Q<Label>("heat-power-value");
        heatThermometer = root.Q<VisualElement>("heat-thermometer");
        healthBar = root.Q<VisualElement>("human-health-bar");
        healthFill = root.Q<VisualElement>("human-health-fill");
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
        thermometerHeading = root.Q<VisualElement>("thermometer-heading");
        if (thermometerHeading != null)
        {
            thermometerHeading.style.height = StyleKeyword.Auto;
            thermometerHeading.style.minHeight = multiplierMaximumFontSize + 8f;
            thermometerHeading.style.overflow = Overflow.Visible;
        }
        if (heatPowerText != null)
            heatPowerText.style.overflow = Overflow.Visible;
        hotNotchParent = root.Q<VisualElement>("thermometer-hot-notches");
        zeroNotch = root.Q<VisualElement>("thermometer-zero-notch");
        thermometerMainRow = root.Q<VisualElement>("thermometer-main-row");
        superheatNotchParent = root.Q<VisualElement>("thermometer-superheat-notches");
        BuildNotches(root.Q<VisualElement>("thermometer-cold-notches"),
            coldNotches, 10, "thermometer-notch");
        BuildNotches(hotNotchParent, hotNotches, 10, "thermometer-notch");
        BuildNotches(superheatNotchParent,
            superheatNotches, 5, "thermometer-superheat-notch");

        multiplierInitialized = false;
        ResolvePlayerComponents();
        if (playerComboSystem != null)
            HandleComboChanged(playerComboSystem.CurrentHits,
                playerComboSystem.SecondsRemaining, false);
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

    private void UpdateLockOnMarker()
    {
        if (lockOnMarker == null)
            return;

        Transform target = playerLockOn != null
            ? playerLockOn.LockedTarget : null;
        if (target == null)
        {
            lockOnMarker.style.display = DisplayStyle.None;
            return;
        }

        if (lockOnCamera == null || !lockOnCamera.isActiveAndEnabled)
            lockOnCamera = Camera.main;
        if (lockOnCamera == null)
        {
            lockOnMarker.style.display = DisplayStyle.None;
            return;
        }

        Vector3 screen = lockOnCamera.WorldToScreenPoint(
            playerLockOn.LockPoint);
        if (screen.z <= 0f || playerHudRoot == null ||
            playerHudRoot.resolvedStyle.width <= 0f ||
            playerHudRoot.resolvedStyle.height <= 0f)
        {
            lockOnMarker.style.display = DisplayStyle.None;
            return;
        }

        float scaleX = playerHudRoot.resolvedStyle.width / Screen.width;
        float scaleY = playerHudRoot.resolvedStyle.height / Screen.height;
        lockOnMarker.style.left = screen.x * scaleX - 8f;
        lockOnMarker.style.top = (Screen.height - screen.y) * scaleY - 8f;
        lockOnMarker.style.display = DisplayStyle.Flex;
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
            playerHealth = playerTransform.GetComponent<PlayerHealth>();
            movesetController = playerTransform.GetComponent<PlayerMovesetController>();
            playerLockOn = playerTransform.GetComponent<PlayerLockOn>();
            PlayerComboSystem combo = playerTransform.GetComponent<PlayerComboSystem>();
            if (combo != playerComboSystem)
            {
                UnsubscribeComboSystem();
                playerComboSystem = combo;
                if (playerComboSystem != null)
                {
                    playerComboSystem.ComboChanged += HandleComboChanged;
                    playerComboSystem.RewardGiven += HandleComboReward;
                }
            }
        }
    }

    private void UnsubscribeComboSystem()
    {
        if (playerComboSystem == null)
            return;
        playerComboSystem.ComboChanged -= HandleComboChanged;
        playerComboSystem.RewardGiven -= HandleComboReward;
        playerComboSystem = null;
    }

    private void HandleComboChanged(int hits, float remaining, bool brokenByDamage)
    {
        if (comboMeter == null)
            return;
        if (hits > 0)
        {
            comboMeter.style.display = DisplayStyle.Flex;
            if (comboTitle != null)
                comboTitle.text = "COMBO";
            if (comboCount != null && lastDisplayedComboCount != hits)
            {
                comboCount.text = hits.ToString();
                lastDisplayedComboCount = hits;
            }
            if (comboTimerFill != null)
            {
                float duration = playerComboSystem != null
                    ? playerComboSystem.TimeoutSeconds : 4f;
                comboTimerFill.style.width = Length.Percent(
                    Mathf.Clamp01(remaining / Mathf.Max(.01f, duration)) * 100f);
            }
            comboBreakVisibleUntil = 0f;
            return;
        }

        lastDisplayedComboCount = int.MinValue;
        if (brokenByDamage)
        {
            comboMeter.style.display = DisplayStyle.Flex;
            if (comboTitle != null)
                comboTitle.text = "COMBO BROKEN";
            if (comboCount != null)
                comboCount.text = string.Empty;
            if (comboTimerFill != null)
                comboTimerFill.style.width = Length.Percent(0f);
            comboBreakVisibleUntil = Time.unscaledTime + 1.2f;
        }
        else
        {
            bool rewardStillVisible = comboRewardVisibleUntil > Time.unscaledTime;
            comboBreakVisibleUntil = 0f;
            comboMeter.style.display = rewardStillVisible
                ? DisplayStyle.Flex : DisplayStyle.None;
            if (rewardStillVisible && comboTitle != null)
                comboTitle.text = "REWARD";
            if (rewardStillVisible && comboCount != null)
                comboCount.text = string.Empty;
        }
    }

    private void HandleComboReward(string rewardText)
    {
        if (comboReward == null || string.IsNullOrWhiteSpace(rewardText))
            return;
        comboReward.text = rewardText;
        comboReward.style.display = DisplayStyle.Flex;
        comboRewardVisibleUntil = Time.unscaledTime + 2.2f;
        if (playerComboSystem == null || playerComboSystem.CurrentHits <= 0)
        {
            if (comboMeter != null)
                comboMeter.style.display = DisplayStyle.Flex;
            if (comboTitle != null)
                comboTitle.text = "REWARD";
            if (comboCount != null)
                comboCount.text = string.Empty;
        }
    }

    private void UpdateComboMeter()
    {
        if (playerComboSystem != null && playerComboSystem.CurrentHits > 0)
        {
            float remaining = playerComboSystem.SecondsRemaining;
            float duration = playerComboSystem.TimeoutSeconds;
            if (comboTimerFill != null)
                comboTimerFill.style.width = Length.Percent(
                    Mathf.Clamp01(remaining / Mathf.Max(.01f, duration)) * 100f);
        }
        else if (comboBreakVisibleUntil > 0f &&
                 Time.unscaledTime >= comboBreakVisibleUntil)
        {
            comboBreakVisibleUntil = 0f;
            if (comboMeter != null && comboRewardVisibleUntil <= 0f)
                comboMeter.style.display = DisplayStyle.None;
        }

        if (comboRewardVisibleUntil > 0f &&
            Time.unscaledTime >= comboRewardVisibleUntil)
        {
            comboRewardVisibleUntil = 0f;
            if (comboReward != null)
                comboReward.style.display = DisplayStyle.None;
        }

        bool comboActive = playerComboSystem != null &&
            playerComboSystem.CurrentHits > 0;
        if (!comboActive && comboBreakVisibleUntil <= 0f &&
            comboRewardVisibleUntil <= 0f && comboMeter != null)
            comboMeter.style.display = DisplayStyle.None;
    }

    private void UpdateHealthBar()
    {
        if (playerHealth == null)
            return;

        float percent = playerHealth.HealthPercent;
        if (healthFill != null)
            healthFill.style.width = Length.Percent(percent * 100f);
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
        if (thermometerMainRow != null)
            thermometerMainRow.style.height = thermometerBarHeight;
        if (superheatNotchParent != null)
            superheatNotchParent.style.height = superheatBarHeight;

        int requiredHotNotches = Mathf.Max(1,
            Mathf.CeilToInt(playerHeat.MaxHeat / 10f));
        if (requiredHotNotches != hotNotches.Count)
            BuildNotches(hotNotchParent, hotNotches, requiredHotNotches,
                "thermometer-notch");

        for (int index = 0; index < coldNotches.Count; index++)
        {
            float filled = GetColdNotchFill(index, coldNotches.Count);
            UpdateNotchVisual(coldNotches[index],
                EvaluateGradient(coldBarColors, index / 9f), filled,
                thermometerNotchWidth, thermometerBarHeight);
        }

        for (int index = 0; index < hotNotches.Count; index++)
        {
            float capacity = Mathf.Clamp01(playerHeat.MaxHeat / 10f - index);
            float filled = capacity > 0f
                ? Mathf.Clamp01((playerHeat.CurrentHeat / 10f - index) / capacity)
                : 0f;
            UpdateNotchVisual(hotNotches[index],
                EvaluateGradient(heatBarColors, Mathf.Clamp01(index / 9f)),
                filled, thermometerNotchWidth * capacity,
                thermometerBarHeight);
        }

        for (int index = 0; index < superheatNotches.Count; index++)
        {
            float filled = GetPositiveNotchFill(playerHeat.CurrentSuperheat,
                playerHeat.MaxSuperheat, index, superheatNotches.Count);
            UpdateNotchVisual(superheatNotches[index],
                EvaluateGradient(superheatBarColors, index / 4f), filled,
                thermometerNotchWidth, superheatBarHeight);
        }

        if (zeroNotch != null)
        {
            zeroNotch.style.backgroundColor = zeroMarkerColor;
            zeroNotch.style.height = thermometerBarHeight + 2f;
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

    private float GetColdNotchFill(int index, int count)
    {
        if (count <= 0 || playerHeat == null)
            return 0f;

        float coldUnits = Mathf.Clamp01(-playerHeat.CurrentHeat /
            Mathf.Max(.001f, -playerHeat.MinHeat)) * count;
        // O notch junto ao zero acende primeiro; o preenchimento avanca da direita para a esquerda.
        return Mathf.Clamp01(coldUnits - (count - 1 - index));
    }

    private static float GetPositiveNotchFill(float value, float maximum,
        int index, int count)
    {
        if (count <= 0 || maximum <= 0f)
            return 0f;

        float units = Mathf.Clamp01(value / maximum) * count;
        return Mathf.Clamp01(units - index);
    }

    private void UpdateNotchVisual(VisualElement notch, Color color,
        float fillAmount, float width, float height)
    {
        if (notch == null)
            return;

        bool major = notchesPerMajorDivider > 0 &&
            (notch.parent.IndexOf(notch) + 1) % notchesPerMajorDivider == 0;
        notch.style.width = width;
        notch.style.height = height;
        notch.style.marginLeft = 0f;
        notch.style.backgroundColor = emptyNotchColor;
        notch.style.borderRightWidth = major ? majorDividerWidth : 1f;
        notch.style.borderRightColor = major ? majorDividerColor : minorDividerColor;

        VisualElement fill = notch.Q<VisualElement>(
            className: "thermometer-notch-fill");
        if (fill == null)
            return;

        fill.style.backgroundColor = color;
        fill.style.width = Length.Percent(Mathf.Clamp01(fillAmount) * 100f);
    }

    private float GetMultiplierFontSize(float multiplier)
    {
        // Cold mantem o tamanho-base; multiplicadores positivos ampliam o numero.
        float size = multiplierBaseFontSize +
            (Mathf.Max(1f, multiplier) - 1f) * multiplierFontSizePerStep;
        return Mathf.Clamp(size, multiplierBaseFontSize,
            multiplierMaximumFontSize);
    }

    private float GetMultiplierRotation(float multiplier)
    {
        float rotation = -(multiplier - 1f) * multiplierRotationPerStep;
        return Mathf.Clamp(rotation, -multiplierMaximumHotRotation,
            multiplierMaximumColdRotation);
    }

    private Color GetMultiplierColor(float multiplier)
    {
        return EvaluateGradient(multiplierColors,
            Mathf.Clamp01(multiplier / 10f));
    }

    private static Color EvaluateGradient(Gradient gradient, float time) =>
        gradient == null ? Color.white : gradient.Evaluate(Mathf.Clamp01(time));

    private static Gradient CreateDefaultColdGradient() => CreateGradient(
        new GradientColorKey(new Color(.04f, .13f, .35f), 0f),
        new GradientColorKey(new Color(.2f, .85f, 1f), 1f));

    private static Gradient CreateDefaultHeatGradient() => CreateGradient(
        new GradientColorKey(new Color(1f, .75f, .08f), 0f),
        new GradientColorKey(new Color(1f, .18f, .02f), 1f));

    private static Gradient CreateDefaultSuperheatGradient() => CreateGradient(
        new GradientColorKey(new Color(.45f, 0f, .02f), 0f),
        new GradientColorKey(new Color(1f, .08f, .02f), 1f));

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

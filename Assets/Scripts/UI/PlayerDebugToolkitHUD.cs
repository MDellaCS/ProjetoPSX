using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD de inspecao F3 e menu de comandos F4.
/// O UXML precisa ter os elementos "debug-panel" e "debug-text".
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class PlayerDebugToolkitHUD : MonoBehaviour
{
    private static PlayerDebugToolkitHUD activeDpsHud;
    public static bool IsVisible { get; private set; }
    public static bool IsDebugMenuVisible { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDebugVisibility()
    {
        IsVisible = false;
        IsDebugMenuVisible = false;
        activeDpsHud = null;
    }

    [Header("References")]
    [SerializeField] private PanelRenderer panelRenderer;
    [SerializeField] private Transform playerTransform;

    [Header("DPS Debug")]
    [Tooltip("Periodo em segundos usado para calcular a media de dano por segundo.")]
    [Min(.1f)] [SerializeField] private float dpsWindowSeconds = 1f;
    [Tooltip("Intervalo em segundos entre pontos sucessivos do grafico de DPS.")]
    [Min(.05f)] [SerializeField] private float dpsSampleInterval = .25f;
    [Tooltip("Duracao em segundos do historico exibido no grafico de DPS.")]
    [Min(1f)] [SerializeField] private float dpsHistorySeconds = 15f;

    [Header("BPM Debug")]
    [Tooltip("Velocidade inicial das batidas de referencia em tempo de jogo; tambem pode ser alterada no F4.")]
    [Range(20, 400)] [SerializeField] private int bpm = 120;
    [Tooltip("Quantidade de batidas mostradas no historico de golpes do jogador.")]
    [Range(2, 32)] [SerializeField] private int bpmHistoryBeats = 8;

    private readonly Queue<float> bpmAttacks = new();
    private PlayerAttackController bpmAttackSource;
    private VisualElement bpmPanel;
    private VisualElement bpmTimeline;
    private VisualElement bpmIntervals;
    private readonly List<Label> bpmIntervalLabels = new();
    private IntegerField bpmInput;
    private float bpmOrigin;
    private bool bpmVisible;

    private struct DpsHit { public float time; public int damage; }
    private readonly Queue<DpsHit> dpsHits = new();
    private readonly List<float> dpsSamples = new();
    private long dpsDamage;
    private float nextDpsSample;
    private bool dpsVisible;
    private VisualElement dpsPanel;
    private VisualElement dpsGraph;
    private Label dpsText;
    private Label dpsRange;

    private readonly StringBuilder textBuilder = new StringBuilder();
    private readonly StringBuilder cutsceneTextBuilder = new StringBuilder();

    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerHeat playerHeat;
    private PlayerHealth playerHealth;
    private PlayerCombatStatus combatStatus;
    private PlayerAttackController attackController;
    private PlayerMovesetController movesetController;
    private PlayerCurrencyWallet playerCurrency;
    private PlayerCharacterController playerCharacterController;
    private WaterSensor waterSensor;
    private PlayerLockOn playerLockOn;

    private VisualElement rootVisualElement;
    private VisualElement debugPanel;
    private Label debugText;
    private Label waterStateLabel;
    private VisualElement lockOnStickPanel;
    private VisualElement lockOnMousePanel;
    private VisualElement lockOnStickDeadZone;
    private VisualElement lockOnStickDot;
    private VisualElement lockOnMouseDot;
    private Label lockOnStickText;
    private Label lockOnMouseText;
    private VisualElement cutsceneDebugPanel;
    private Label cutsceneDebugText;
    private VisualElement cutsceneTimelineFill;
    private VisualElement debugMenuPanel;
    private VisualElement heatControls;
    private VisualElement boundButtonRoot;
    private VisualElement boundGizmoRoot;
    private IntegerField currencyInput;
    private FloatField debugTimeScaleInput;
    private Label debugActionStatus;
    private Button godModeButton;
    private Button combatOverrideButton;
    private Button slowMotionButton;
    private Button resetSaveButton;
    private float resetConfirmationExpiresAt;
    private VisualElement debugSubmenu;
    private Label debugSubmenuTitle;
    private ScrollView debugSubmenuList;
    private ScrollView debugGizmoList;
    private UnityEngine.CursorLockMode cursorLockBeforeDebug;
    private bool cursorVisibleBeforeDebug;
    private bool isVisible;
    private bool lockOnDebugVisible;
    private bool debugPanelVisible;

    private void Awake()
    {
        activeDpsHud = this;
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);

        ResolvePlayerComponents();
        SetHudVisible(false);
        SetDebugMenuVisible(false);
    }

    private void OnDestroy()
    {
        if (activeDpsHud == this) activeDpsHud = null;
        if (dpsGraph != null) dpsGraph.generateVisualContent -= DrawDpsGraph;
        if (bpmTimeline != null) bpmTimeline.generateVisualContent -= DrawBpmTimeline;
        if (bpmAttackSource != null) bpmAttackSource.AttackActiveStarted -= OnBpmAttackActiveStarted;
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);

        IsVisible = false;
        IsDebugMenuVisible = false;
    }

    private void OnDisable()
    {
        SetHudVisible(false);
        SetDebugMenuVisible(false);
    }

    private void Update()
    {
        if (GameInput.DebugHUDPressed)
        {
            SetHudVisible(!isVisible);
        }

        if (GameInput.DebugMenuPressed)
            SetDebugMenuVisible(!IsDebugMenuVisible);

        UpdateDps();
        UpdateBpm();
        UpdateDebugPanelVisibility();

        if ((!isVisible && !IsDebugMenuVisible) || debugText == null)
            return;

        if (!HasPlayerComponents() || playerCurrency == null ||
            movesetController == null)
            ResolvePlayerComponents();

        if (debugPanelVisible) UpdateDebugText();
        UpdateDebugActionLabels();
        UpdateCutsceneDebug();
    }

    private void HandleUIReload(
        PanelRenderer _,
        VisualElement rootVisualElement,
        int __)
    {
        if (rootVisualElement == null)
            return;

        if (dpsGraph != null) dpsGraph.generateVisualContent -= DrawDpsGraph;
        if (bpmTimeline != null) bpmTimeline.generateVisualContent -= DrawBpmTimeline;
        this.rootVisualElement = rootVisualElement;
        bpmPanel = rootVisualElement.Q<VisualElement>("debug-bpm-panel");
        bpmTimeline = rootVisualElement.Q<VisualElement>("debug-bpm-timeline");
        bpmIntervals = rootVisualElement.Q<VisualElement>("debug-bpm-intervals");
        bpmIntervalLabels.Clear();
        bpmIntervals?.Clear();
        bpmInput = rootVisualElement.Q<IntegerField>("debug-bpm-input");
        if (bpmInput != null) bpmInput.SetValueWithoutNotify(Mathf.Clamp(bpm, 20, 400));
        if (bpmTimeline != null) bpmTimeline.generateVisualContent += DrawBpmTimeline;
        bpmVisible = false;
        dpsPanel = rootVisualElement.Q<VisualElement>("debug-dps-panel");
        dpsGraph = rootVisualElement.Q<VisualElement>("debug-dps-graph");
        dpsText = rootVisualElement.Q<Label>("debug-dps-text");
        dpsRange = rootVisualElement.Q<Label>("debug-dps-range");
        if (dpsGraph != null) dpsGraph.generateVisualContent += DrawDpsGraph;
        dpsVisible = false;
        debugPanel = rootVisualElement.Q<VisualElement>("debug-panel");
        debugPanelVisible = false;
        if (debugPanel != null) debugPanel.style.display = DisplayStyle.None;
        debugText = rootVisualElement.Q<Label>("debug-text");
        waterStateLabel = rootVisualElement.Q<Label>("water-state-label");
        lockOnStickPanel = rootVisualElement.Q<VisualElement>("lockon-debug-stick");
        lockOnMousePanel = rootVisualElement.Q<VisualElement>("lockon-debug-mouse");
        lockOnStickDeadZone = rootVisualElement.Q<VisualElement>("lockon-stick-deadzone");
        lockOnStickDot = rootVisualElement.Q<VisualElement>("lockon-stick-dot");
        lockOnMouseDot = rootVisualElement.Q<VisualElement>("lockon-mouse-dot");
        lockOnStickText = rootVisualElement.Q<Label>("lockon-stick-text");
        lockOnMouseText = rootVisualElement.Q<Label>("lockon-mouse-text");
        lockOnDebugVisible = false;
        cutsceneDebugPanel = rootVisualElement.Q<VisualElement>("cutscene-debug-panel");
        cutsceneDebugText = rootVisualElement.Q<Label>("cutscene-debug-text");
        cutsceneTimelineFill = rootVisualElement.Q<VisualElement>("cutscene-timeline-fill");
        debugMenuPanel = rootVisualElement.Q<VisualElement>("debug-menu-panel");
        heatControls = rootVisualElement.Q<VisualElement>("heat-controls");
        currencyInput = rootVisualElement.Q<IntegerField>("currency-input");
        debugTimeScaleInput = rootVisualElement.Q<FloatField>("debug-time-scale-input");
        debugActionStatus = rootVisualElement.Q<Label>("debug-action-status");
        godModeButton = rootVisualElement.Q<Button>("god-mode-button");
        combatOverrideButton = rootVisualElement.Q<Button>("combat-override-button");
        slowMotionButton = rootVisualElement.Q<Button>("slow-motion-button");
        resetSaveButton = rootVisualElement.Q<Button>("reset-save-button");
        debugSubmenu = rootVisualElement.Q<VisualElement>("debug-submenu");
        debugSubmenuTitle = rootVisualElement.Q<Label>("debug-submenu-title");
        debugSubmenuList = rootVisualElement.Q<ScrollView>("debug-submenu-list");
        debugGizmoList = rootVisualElement.Q<ScrollView>("debug-gizmo-list");
        BindDebugButtons(rootVisualElement);
        BindGizmoCallbacks();


        SetHudVisible(isVisible);
        SetDebugMenuVisible(IsDebugMenuVisible);
        if (waterStateLabel != null)
            waterStateLabel.style.display = DisplayStyle.None;
    }

    private void LateUpdate()
    {
        UpdateLockOnDebug();
        if (waterStateLabel == null)
            return;

        if (!isVisible || waterSensor == null || !waterSensor.IsInWater ||
            playerTransform == null)
        {
            waterStateLabel.style.display = DisplayStyle.None;
            return;
        }

        Camera worldCamera = Camera.main;
        if (worldCamera == null || rootVisualElement == null)
        {
            waterStateLabel.style.display = DisplayStyle.None;
            return;
        }

        Bounds playerBounds = playerCharacterController != null
            ? playerCharacterController.bounds
            : new Bounds(playerTransform.position, Vector3.up * 2f);
        Vector3 screen = worldCamera.WorldToScreenPoint(new Vector3(
            playerBounds.center.x, playerBounds.max.y + .2f,
            playerBounds.center.z));
        if (screen.z <= 0f)
        {
            waterStateLabel.style.display = DisplayStyle.None;
            return;
        }

        float scaleX = rootVisualElement.resolvedStyle.width /
            Mathf.Max(1f, Screen.width);
        float scaleY = rootVisualElement.resolvedStyle.height /
            Mathf.Max(1f, Screen.height);
        if (scaleX <= 0f || scaleY <= 0f)
        {
            waterStateLabel.style.display = DisplayStyle.None;
            return;
        }

        const float labelWidth = 150f;
        const float labelHeight = 42f;
        waterStateLabel.text = waterSensor.Zone == WaterZone.Deep
            ? "FUNDA" : "RASA";
        waterStateLabel.style.left = screen.x * scaleX - labelWidth * .5f;
        waterStateLabel.style.top = (Screen.height - screen.y) * scaleY -
            labelHeight;
        waterStateLabel.style.display = DisplayStyle.Flex;
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

        playerMovement = playerTransform.GetComponent<PlayerMovement>();
        playerState = playerTransform.GetComponent<PlayerState>();
        playerHeat = playerTransform.GetComponent<PlayerHeat>();
        playerHealth = playerTransform.GetComponent<PlayerHealth>();
        combatStatus = playerTransform.GetComponent<PlayerCombatStatus>();
        attackController = playerTransform.GetComponent<PlayerAttackController>();
        if (bpmAttackSource != attackController)
        {
            if (bpmAttackSource != null) bpmAttackSource.AttackActiveStarted -= OnBpmAttackActiveStarted;
            bpmAttackSource = attackController;
            if (bpmAttackSource != null) bpmAttackSource.AttackActiveStarted += OnBpmAttackActiveStarted;
        }
        movesetController = playerTransform.GetComponent<PlayerMovesetController>();
        playerCurrency = playerTransform.GetComponent<PlayerCurrencyWallet>();
        playerCharacterController = playerTransform.GetComponent<PlayerCharacterController>();
        waterSensor = playerTransform.GetComponent<WaterSensor>();
        playerLockOn = playerTransform.GetComponent<PlayerLockOn>();
    }

    private bool HasPlayerComponents()
    {
        return playerMovement != null &&
               playerState != null &&
               playerHeat != null &&
               playerHealth != null &&
               combatStatus != null &&
               attackController != null;
    }

    private void SetHudVisible(bool value)
    {
        isVisible = value;
        IsVisible = value;

        UpdateDebugPanelVisibility();
        if (cutsceneDebugPanel != null)
            cutsceneDebugPanel.style.display = DisplayStyle.None;
        if (!value) SetLockOnDebugVisible(false);
        UpdateDps();
        UpdateBpm();
    }

    // Ferramenta temporaria de debug: registra apenas dano efetivo causado pelo jogador.
    public static void RecordPlayerDamage(int damage)
    {
        if (!Debug.isDebugBuild || !IsVisible || damage <= 0 || activeDpsHud == null ||
            !DebugGizmoSettings.Selected(DebugGizmoGroup.PlayerDps)) return;
        activeDpsHud.dpsHits.Enqueue(new DpsHit { time = Time.time, damage = damage });
        activeDpsHud.dpsDamage += damage;
    }

    private void UpdateDps()
    {
        bool visible = Debug.isDebugBuild && isVisible &&
            DebugGizmoSettings.Selected(DebugGizmoGroup.PlayerDps);
        if (dpsPanel != null && visible != dpsVisible)
            dpsPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (!visible)
        {
            if (dpsVisible || dpsHits.Count != 0 || dpsSamples.Count != 0)
            {
                dpsHits.Clear();
                dpsSamples.Clear();
                dpsDamage = 0;
                nextDpsSample = 0f;
            }
            dpsVisible = false;
            return;
        }
        dpsVisible = true;
        float now = Time.time;
        float window = Mathf.Max(.1f, dpsWindowSeconds);
        while (dpsHits.Count > 0 && dpsHits.Peek().time <= now - window)
            dpsDamage -= dpsHits.Dequeue().damage;
        if (now < nextDpsSample) return;
        float interval = Mathf.Max(.05f, dpsSampleInterval);
        float history = Mathf.Max(1f, dpsHistorySeconds);
        nextDpsSample = now + interval;
        float dps = dpsDamage / window;
        dpsSamples.Add(dps);
        int capacity = Mathf.Clamp(Mathf.CeilToInt(history / interval) + 1, 2, 1201);
        if (dpsSamples.Count > capacity) dpsSamples.RemoveRange(0, dpsSamples.Count - capacity);
        if (dpsText != null) dpsText.text = $"DPS: {dps:0.0}  |  janela: {window:0.0}s";
        float peak = 1f;
        for (int i = 0; i < dpsSamples.Count; i++) peak = Mathf.Max(peak, dpsSamples[i]);
        if (dpsRange != null) dpsRange.text = $"Escala: 0 - {peak:0.0} DPS  |  {Mathf.Min(history, (capacity - 1) * interval):0.0}s (tempo de jogo)";
        dpsGraph?.MarkDirtyRepaint();
    }

    private void DrawDpsGraph(MeshGenerationContext context)
    {
        if (dpsSamples.Count < 2 || dpsGraph == null) return;
        Rect rect = dpsGraph.contentRect;
        if (rect.width <= 0f || rect.height <= 0f) return;
        float peak = 1f;
        for (int i = 0; i < dpsSamples.Count; i++) peak = Mathf.Max(peak, dpsSamples[i]);
        float interval = Mathf.Max(.05f, dpsSampleInterval);
        int capacity = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1f, dpsHistorySeconds) / interval) + 1, 2, 1201);
        var painter = context.painter2D;
        painter.strokeColor = new Color(.4f, 1f, .65f);
        painter.lineWidth = 2f;
        painter.BeginPath();
        for (int i = 0; i < dpsSamples.Count; i++)
        {
            float x = rect.xMax - (dpsSamples.Count - 1 - i) * rect.width / (capacity - 1);
            float y = rect.yMax - Mathf.Clamp01(dpsSamples[i] / peak) * (rect.height - 3f);
            if (i == 0) painter.MoveTo(new Vector2(x, y));
            else painter.LineTo(new Vector2(x, y));
        }
        painter.Stroke();
    }

    private void OnBpmAttackActiveStarted(PlayerAttackRuntime attack)
    {
        if (!Debug.isDebugBuild || !isVisible ||
            !DebugGizmoSettings.Selected(DebugGizmoGroup.PlayerAttackBpm)) return;
        bpmAttacks.Enqueue(Time.time);
    }

    private void UpdateBpm()
    {
        bool visible = Debug.isDebugBuild && isVisible &&
            DebugGizmoSettings.Selected(DebugGizmoGroup.PlayerAttackBpm);
        if (bpmPanel != null && visible != bpmVisible)
            bpmPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (!visible)
        {
            if (bpmVisible || bpmAttacks.Count > 0) bpmAttacks.Clear();
            foreach (Label label in bpmIntervalLabels)
                label.style.display = DisplayStyle.None;
            bpmVisible = false;
            return;
        }
        if (!bpmVisible) bpmOrigin = Time.time;
        bpmVisible = true;
        int beats = Mathf.Clamp(bpmHistoryBeats, 2, 32);
        float interval = 60f / Mathf.Clamp(bpm, 20, 400);
        float now = Time.time;
        while (bpmAttacks.Count > 0 && bpmAttacks.Peek() < now - beats * interval)
            bpmAttacks.Dequeue();
        UpdateBpmIntervalLabels(now, beats * interval);
        bpmTimeline?.MarkDirtyRepaint();
    }

    private void UpdateBpmIntervalLabels(float now, float duration)
    {
        if (bpmIntervals == null || bpmTimeline == null) return;
        float width = bpmTimeline.contentRect.width;
        if (width <= 0f) return;
        float previous = 0f;
        bool hasPrevious = false;
        int count = 0;
        foreach (float timestamp in bpmAttacks)
        {
            if (hasPrevious)
            {
                if (count == bpmIntervalLabels.Count)
                {
                    Label label = new();
                    label.AddToClassList("debug-bpm-interval");
                    label.pickingMode = PickingMode.Ignore;
                    bpmIntervals.Add(label);
                    bpmIntervalLabels.Add(label);
                }
                Label intervalLabel = bpmIntervalLabels[count];
                int milliseconds = Mathf.RoundToInt((timestamp - previous) * 1000f);
                if (!(intervalLabel.userData is int oldValue) || oldValue != milliseconds)
                {
                    intervalLabel.text = $"{milliseconds} ms";
                    intervalLabel.userData = milliseconds;
                }
                float midpoint = (previous + timestamp) * .5f;
                intervalLabel.style.left = width * (1f - (now - midpoint) / duration);
                intervalLabel.style.top = 8f + (count % 3) * 27f;
                intervalLabel.style.display = DisplayStyle.Flex;
                count++;
            }
            previous = timestamp;
            hasPrevious = true;
        }
        for (int i = count; i < bpmIntervalLabels.Count; i++)
            bpmIntervalLabels[i].style.display = DisplayStyle.None;
    }

    private void DrawBpmTimeline(MeshGenerationContext context)
    {
        if (!bpmVisible || bpmTimeline == null) return;
        Rect rect = bpmTimeline.contentRect;
        if (rect.width <= 0f || rect.height <= 0f) return;
        float now = Time.time;
        float interval = 60f / Mathf.Clamp(bpm, 20, 400);
        float duration = Mathf.Clamp(bpmHistoryBeats, 2, 32) * interval;
        float elapsed = Mathf.Max(0f, now - bpmOrigin);
        float sinceBeat = elapsed % interval;
        var painter = context.painter2D;
        painter.lineWidth = 1f;
        painter.strokeColor = new Color(.35f, .7f, 1f, .8f);
        painter.BeginPath();
        for (float age = sinceBeat; age <= duration && age <= elapsed; age += interval)
        {
            float x = rect.xMax - age / duration * rect.width;
            painter.MoveTo(new Vector2(x, rect.yMin + 4f));
            painter.LineTo(new Vector2(x, rect.yMax - 4f));
        }
        painter.Stroke();
        painter.lineWidth = 3f;
        painter.strokeColor = new Color(1f, .8f, .2f);
        painter.BeginPath();
        foreach (float timestamp in bpmAttacks)
        {
            float age = now - timestamp;
            if (age < 0f || age > duration) continue;
            float x = rect.xMax - age / duration * rect.width;
            painter.MoveTo(new Vector2(x, rect.yMin + 8f));
            painter.LineTo(new Vector2(x, rect.yMax - 8f));
        }
        painter.Stroke();
    }

    private void ApplyDebugBpm()
    {
        if (!Debug.isDebugBuild || bpmInput == null) return;
        int value = bpmInput.value;
        if (value < 20 || value > 400)
        {
            SetActionStatus("BPM must be between 20 and 400.");
            return;
        }
        bpm = value;
        bpmOrigin = Time.time;
        SetActionStatus($"BPM set to {bpm}.");
    }

    private void UpdateDebugPanelVisibility()
    {
        bool visible = isVisible && DebugGizmoSettings.Selected(DebugGizmoGroup.DebugHud);
        if (debugPanelVisible == visible) return;
        debugPanelVisible = visible;
        if (debugPanel != null)
            debugPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetLockOnDebugVisible(bool visible)
    {
        if (lockOnDebugVisible == visible) return;
        lockOnDebugVisible = visible;
        if (lockOnStickPanel != null)
            lockOnStickPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (lockOnMousePanel != null)
            lockOnMousePanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        debugPanel?.EnableInClassList("lockon-debug-shifted", visible);
    }

    private void UpdateLockOnDebug()
    {
        if (!isVisible) return;
        if (!DebugGizmoSettings.Enabled(DebugGizmoGroup.PlayerLockOn))
        {
            SetLockOnDebugVisible(false);
            return;
        }
        if (playerLockOn == null && playerMovement != null)
            playerLockOn = playerMovement.LockOn;
        if (playerLockOn == null || playerLockOn.Target == null ||
            lockOnStickDot == null || lockOnMouseDot == null ||
            lockOnStickDeadZone == null || lockOnStickText == null || lockOnMouseText == null)
        {
            SetLockOnDebugVisible(false);
            return;
        }

        SetLockOnDebugVisible(true);
        const float radius = 46f;
        const float dotRadius = 4f;
        float deadZone = playerLockOn.StickSwitchDeadZone;
        float diameter = deadZone * radius * 2f;
        lockOnStickDeadZone.style.width = diameter;
        lockOnStickDeadZone.style.height = diameter;
        lockOnStickDeadZone.style.left = radius - diameter * .5f;
        lockOnStickDeadZone.style.top = radius - diameter * .5f;

        Vector2 stick = Vector2.ClampMagnitude(playerLockOn.StickSwitchInput, 1f);
        Vector2 mouse = playerLockOn.MouseSwitchProgress;
        Vector2 mousePosition = Vector2.ClampMagnitude(mouse / playerLockOn.MouseSwitchThreshold, 1f);
        lockOnStickDot.style.left = radius + stick.x * radius - dotRadius;
        lockOnStickDot.style.top = radius - stick.y * radius - dotRadius;
        lockOnMouseDot.style.left = radius + mousePosition.x * radius - dotRadius;
        lockOnMouseDot.style.top = radius - mousePosition.y * radius - dotRadius;
        lockOnStickText.text = $"Entrada: {stick.magnitude:0.00} / Zona: {deadZone:0.00}";
        lockOnMouseText.text = $"{mouse.magnitude:0.0} / {playerLockOn.MouseSwitchThreshold:0.0} px\n" +
            (playerLockOn.SwitchOnCooldown ? "Intervalo de troca" :
                $"Prazo: {playerLockOn.MouseProgressTimeRemaining:0.00} s");
    }

    private void SetDebugMenuVisible(bool value)
    {
        value &= Debug.isDebugBuild;
        if (value && !IsDebugMenuVisible)
        {
            cursorLockBeforeDebug = UnityEngine.Cursor.lockState;
            cursorVisibleBeforeDebug = UnityEngine.Cursor.visible;
            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else if (!value && IsDebugMenuVisible)
        {
            UnityEngine.Cursor.lockState = cursorLockBeforeDebug;
            UnityEngine.Cursor.visible = cursorVisibleBeforeDebug;
        }

        IsDebugMenuVisible = value;
        if (debugSubmenu != null) debugSubmenu.style.display = DisplayStyle.None;
        if (debugMenuPanel != null)
            debugMenuPanel.style.display = value
                ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindDebugButtons(VisualElement root)
    {
        if (boundButtonRoot == root)
            return;
        boundButtonRoot = root;
        BindButton(root, "debug-submenu-back", CloseSubmenu);
        BindButton(root, "edit-levels-button", OpenLevelEditor);
        BindButton(root, "heat-minus-button", () => AdjustPlayerHeat(-10f));
        BindButton(root, "heat-plus-button", () => AdjustPlayerHeat(10f));
        BindButton(root, "currency-take-button", () => AdjustCurrency(false));
        BindButton(root, "currency-give-button", () => AdjustCurrency(true));
        BindButton(root, "god-mode-button", ToggleGodMode);
        BindButton(root, "combat-override-button", CycleCombatOverride);
        BindButton(root, "kill-player-button", KillPlayer);
        BindButton(root, "slow-motion-button", ToggleSlowMotion);
        BindButton(root, "debug-time-scale-apply", ApplyDebugTimeScale);
        BindButton(root, "debug-bpm-apply", ApplyDebugBpm);
        BindButton(root, "next-moveset-button", CycleMoveset);
        BindButton(root, "select-scene-button", OpenSceneSelector);
        BindButton(root, "select-gizmos-button", OpenGizmoSelector);
        BindButton(root, "select-camera-profile-button", OpenCameraProfileSelector);
        BindButton(root, "environment-previous-button", () => CycleEnvironment(-1));
        BindButton(root, "environment-next-button", () => CycleEnvironment(1));
        BindButton(root, "reload-scene-button", ReloadScene);
        BindButton(root, "reset-save-button", RequestSaveReset);
        BindButton(root, "clear-key-items-button", ClearKeyItems);
    }

    private static void BindButton(VisualElement root, string name,
        System.Action action)
    {
        Button button = root.Q<Button>(name);
        if (button == null) return;
        button.clicked -= action;
        button.clicked += action;
    }

    private void CloseSubmenu()
    {
        if (debugSubmenu != null) debugSubmenu.style.display = DisplayStyle.None;
        if (debugMenuPanel != null && IsDebugMenuVisible) debugMenuPanel.style.display = DisplayStyle.Flex;
    }

    private bool OpenSubmenu(string title)
    {
        if (!Debug.isDebugBuild || debugSubmenu == null || debugSubmenuList == null) return false;
        debugMenuPanel.style.display = DisplayStyle.None;
        debugSubmenu.style.display = DisplayStyle.Flex;
        debugSubmenuTitle.text = title;
        debugSubmenuList.Clear();
        debugSubmenuList.style.display = DisplayStyle.Flex;
        if (debugGizmoList != null) debugGizmoList.style.display = DisplayStyle.None;
        debugSubmenu.RemoveFromClassList("debug-gizmo-submenu");
        return true;
    }

    private void OpenSceneSelector()
    {
        if (!OpenSubmenu("SELECT SCENE")) return;
        foreach (string path in GameSceneLoader.FindSceneCyclePaths())
        {
            string scenePath = path;
            var button = new Button(() => {
                GameSceneLoader loader = FindAnyObjectByType<GameSceneLoader>();
                if (loader == null) return;
                SetDebugMenuVisible(false);
                loader.DebugSelectScene(scenePath);
            }) { text = System.IO.Path.GetFileNameWithoutExtension(path) };
            debugSubmenuList.Add(button);
        }
    }

    private void OpenCameraProfileSelector()
    {
        if (!OpenSubmenu("CAMERA PROFILE")) return;
        CameraFollow cameraFollow = CameraFollow.Instance;
        if (cameraFollow == null)
        {
            debugSubmenuList.Add(new Label("Camera indisponivel."));
            return;
        }
        if (cameraFollow.IsCameraZoneActive)
            debugSubmenuList.Add(new Label("Uma zona de camera esta ativa e pode substituir o perfil selecionado."));
        var sceneButton = new Button(() =>
        {
            cameraFollow.DebugSetProfile(null, false);
            SetDebugMenuVisible(false);
        }) { text = cameraFollow.IsDebugProfileOverride ? "SCENE DEFAULT" : "SCENE DEFAULT [ACTIVE]" };
        debugSubmenuList.Add(sceneButton);
        CameraFollowProfile[] profiles = Resources.LoadAll<CameraFollowProfile>("CameraFollowProfiles");
        System.Array.Sort(profiles, (a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        foreach (CameraFollowProfile profile in profiles)
        {
            CameraFollowProfile selected = profile;
            var button = new Button(() =>
            {
                cameraFollow.DebugSetProfile(selected, true);
                SetDebugMenuVisible(false);
            }) { text = profile.name + (cameraFollow.IsDebugProfileOverride && cameraFollow.ActiveProfile == profile ? " [ACTIVE]" : "") };
            debugSubmenuList.Add(button);
        }
    }

    private void OpenGizmoSelector()
    {
        if (debugGizmoList == null || !OpenSubmenu("F3 GIZMOS")) return;
        debugSubmenu.AddToClassList("debug-gizmo-submenu");
        debugSubmenuList.style.display = DisplayStyle.None;
        debugGizmoList.style.display = DisplayStyle.Flex;
        BindGizmoToggle("gizmo-debug-hud", DebugGizmoGroup.DebugHud);
        BindGizmoToggle("gizmo-enemy-aggression", DebugGizmoGroup.EnemyAggression);
        BindGizmoToggle("gizmo-player-current-attack", DebugGizmoGroup.PlayerCurrentAttack);
        BindGizmoToggle("gizmo-player-bpm", DebugGizmoGroup.PlayerAttackBpm);
        BindGizmoToggle("gizmo-player-dps", DebugGizmoGroup.PlayerDps);
        BindGizmoToggle("gizmo-damage-numbers", DebugGizmoGroup.DamageNumbers);
        BindGizmoToggle("gizmo-player-hitboxes", DebugGizmoGroup.PlayerHitboxes);
        BindGizmoToggle("gizmo-enemy-hitboxes", DebugGizmoGroup.EnemyHitboxes);
        BindGizmoToggle("gizmo-player-lockon", DebugGizmoGroup.PlayerLockOn);
        BindGizmoToggle("gizmo-enemy-distance", DebugGizmoGroup.EnemyDistance);
        BindGizmoToggle("gizmo-puppet-bones", DebugGizmoGroup.FlyingPuppetBones);
        BindGizmoToggle("gizmo-puppet-landing", DebugGizmoGroup.FlyingPuppetLanding);
        BindGizmoToggle("gizmo-interaction", DebugGizmoGroup.Interaction);
        BindGizmoToggle("gizmo-swing-anchors", DebugGizmoGroup.SwingAnchors);
        BindGizmoToggle("gizmo-swing-rope", DebugGizmoGroup.SwingRope);
        BindGizmoToggle("gizmo-encounter-triggers", DebugGizmoGroup.EncounterTriggers);
        BindGizmoToggle("gizmo-encounter-spawns", DebugGizmoGroup.EncounterSpawnAreas);
        BindGizmoToggle("gizmo-player-spawns", DebugGizmoGroup.Spawns);
        BindGizmoToggle("gizmo-world-objects", DebugGizmoGroup.WorldObjects);
        BindGizmoToggle("gizmo-world-levers", DebugGizmoGroup.WorldLevers);
        BindGizmoToggle("gizmo-world-checkpoints", DebugGizmoGroup.WorldCheckpoints);
        BindGizmoToggle("gizmo-world-titles", DebugGizmoGroup.WorldAreaTitles);
        BindGizmoToggle("gizmo-world-prompts", DebugGizmoGroup.WorldPrompts);
        BindGizmoToggle("gizmo-camera-zones", DebugGizmoGroup.Camera);
        BindGizmoToggle("gizmo-lights", DebugGizmoGroup.Lights);
        BindGizmoToggle("gizmo-water", DebugGizmoGroup.Water);
    }

    private void BindGizmoCallbacks()
    {
        if (debugGizmoList == null || boundGizmoRoot == debugGizmoList) return;
        boundGizmoRoot = debugGizmoList;
        RegisterGizmoToggle("gizmo-debug-hud", DebugGizmoGroup.DebugHud);
        RegisterGizmoToggle("gizmo-enemy-aggression", DebugGizmoGroup.EnemyAggression);
        RegisterGizmoToggle("gizmo-player-current-attack", DebugGizmoGroup.PlayerCurrentAttack);
        RegisterGizmoToggle("gizmo-player-bpm", DebugGizmoGroup.PlayerAttackBpm);
        RegisterGizmoToggle("gizmo-player-dps", DebugGizmoGroup.PlayerDps);
        RegisterGizmoToggle("gizmo-damage-numbers", DebugGizmoGroup.DamageNumbers);
        RegisterGizmoToggle("gizmo-player-hitboxes", DebugGizmoGroup.PlayerHitboxes);
        RegisterGizmoToggle("gizmo-enemy-hitboxes", DebugGizmoGroup.EnemyHitboxes);
        RegisterGizmoToggle("gizmo-player-lockon", DebugGizmoGroup.PlayerLockOn);
        RegisterGizmoToggle("gizmo-enemy-distance", DebugGizmoGroup.EnemyDistance);
        RegisterGizmoToggle("gizmo-puppet-bones", DebugGizmoGroup.FlyingPuppetBones);
        RegisterGizmoToggle("gizmo-puppet-landing", DebugGizmoGroup.FlyingPuppetLanding);
        RegisterGizmoToggle("gizmo-interaction", DebugGizmoGroup.Interaction);
        RegisterGizmoToggle("gizmo-swing-anchors", DebugGizmoGroup.SwingAnchors);
        RegisterGizmoToggle("gizmo-swing-rope", DebugGizmoGroup.SwingRope);
        RegisterGizmoToggle("gizmo-encounter-triggers", DebugGizmoGroup.EncounterTriggers);
        RegisterGizmoToggle("gizmo-encounter-spawns", DebugGizmoGroup.EncounterSpawnAreas);
        RegisterGizmoToggle("gizmo-player-spawns", DebugGizmoGroup.Spawns);
        RegisterGizmoToggle("gizmo-world-objects", DebugGizmoGroup.WorldObjects);
        RegisterGizmoToggle("gizmo-world-levers", DebugGizmoGroup.WorldLevers);
        RegisterGizmoToggle("gizmo-world-checkpoints", DebugGizmoGroup.WorldCheckpoints);
        RegisterGizmoToggle("gizmo-world-titles", DebugGizmoGroup.WorldAreaTitles);
        RegisterGizmoToggle("gizmo-world-prompts", DebugGizmoGroup.WorldPrompts);
        RegisterGizmoToggle("gizmo-camera-zones", DebugGizmoGroup.Camera);
        RegisterGizmoToggle("gizmo-lights", DebugGizmoGroup.Lights);
        RegisterGizmoToggle("gizmo-water", DebugGizmoGroup.Water);
    }

    private void RegisterGizmoToggle(string name, DebugGizmoGroup group)
    {
        Toggle toggle = debugGizmoList.Q<Toggle>(name);
        if (toggle != null)
            toggle.RegisterValueChangedCallback(evt => DebugGizmoSettings.Set(group, evt.newValue));
    }

    private void BindGizmoToggle(string name, DebugGizmoGroup group)
    {
        Toggle toggle = debugGizmoList.Q<Toggle>(name);
        if (toggle == null) return;
        toggle.SetValueWithoutNotify(DebugGizmoSettings.Selected(group));
    }

    private void OpenLevelEditor()
    {
        if (!OpenSubmenu("LEVEL UP STATS")) return;
        if (playerTransform == null) ResolvePlayerComponents();
        if (playerTransform == null) return;
        PlayerProgression progression = PlayerProgression.GetOrAdd(playerTransform.gameObject);
        progression.Initialize();
        VisualTreeAsset rowTemplate = Resources.Load<VisualTreeAsset>("Debug/DebugLevelRow");
        if (progression.Catalog == null || progression.Catalog.attributes == null || rowTemplate == null)
        {
            SetActionStatus("Level editor unavailable.");
            CloseSubmenu();
            return;
        }

        foreach (PlayerUpgradeDefinition attribute in progression.Catalog.attributes)
        {
            if (attribute == null || !attribute.purchasable || string.IsNullOrWhiteSpace(attribute.id))
                continue;
            TemplateContainer row = rowTemplate.Instantiate();
            Label name = row.Q<Label>("level-name");
            Label value = row.Q<Label>("level-value");
            Button minus = row.Q<Button>("level-minus");
            Button plus = row.Q<Button>("level-plus");
            if (name == null || value == null || minus == null || plus == null) continue;
            name.text = attribute.displayName;
            void Refresh()
            {
                int level = progression.Level(attribute);
                value.text = $"{level} / {attribute.MaximumLevel}";
                minus.SetEnabled(level > 1);
                plus.SetEnabled(level < attribute.MaximumLevel && attribute.GetLevel(level + 1) != null);
            }
            minus.clicked += () => { progression.DebugSetLevel(attribute, progression.Level(attribute) - 1); Refresh(); };
            plus.clicked += () => { progression.DebugSetLevel(attribute, progression.Level(attribute) + 1); Refresh(); };
            Refresh();
            debugSubmenuList.Add(row);
        }
    }

    private void AdjustPlayerHeat(float amount)
    {
        if (!Debug.isDebugBuild || playerHeat == null) return;
        playerHeat.DebugAdjustHeat(amount);
        SetActionStatus("Heat: " + playerHeat.CurrentHeat.ToString("0.0"));
    }

    private void AdjustCurrency(bool give)
    {
        if (!Debug.isDebugBuild || currencyInput == null) return;
        if (playerCurrency == null && playerTransform != null)
            playerCurrency = playerTransform.GetComponent<PlayerCurrencyWallet>();
        if (playerCurrency == null) return;
        int amount = Mathf.Max(0, currencyInput.value);
        if (give)
            playerCurrency.DebugSetCurrency((int)System.Math.Min(int.MaxValue, (long)playerCurrency.Current + amount));
        else
            playerCurrency.DebugSetCurrency(Mathf.Max(0, playerCurrency.Current - amount));
        SetActionStatus("Currency: " + playerCurrency.Current + ".");
    }

    private void ToggleGodMode()
    {
        if (!Debug.isDebugBuild || playerHealth == null) return;
        playerHealth.DebugToggleGodMode();
        UpdateDebugActionLabels();
    }

    private void CycleCombatOverride()
    {
        if (!Debug.isDebugBuild || combatStatus == null) return;
        combatStatus.DebugCycleCombatOverride();
        UpdateDebugActionLabels();
    }

    private void KillPlayer()
    {
        if (!Debug.isDebugBuild || playerHealth == null) return;
        playerHealth.DebugKillPlayer();
        SetActionStatus("Player killed.");
    }

    private void ToggleSlowMotion()
    {
        if (!Debug.isDebugBuild) return;
        GameDebugTimeScale.Toggle();
        UpdateDebugActionLabels();
    }

    private void ApplyDebugTimeScale()
    {
        if (!Debug.isDebugBuild || debugTimeScaleInput == null) return;
        float value = debugTimeScaleInput.value;
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
        {
            SetActionStatus("Game time must be a finite value of zero or greater.");
            return;
        }
        if (!GameDebugTimeScale.SetScale(value))
        {
            SetActionStatus("Resume the game before changing game time.");
            return;
        }
        SetActionStatus($"Game time set to {value:0.###}x.");
        UpdateDebugActionLabels();
    }

    private void CycleMoveset()
    {
        if (!Debug.isDebugBuild || movesetController == null) return;
        movesetController.DebugCycleMoveset();
        SetActionStatus("Moveset: " + movesetController.CurrentMovesetName);
    }

    private void CycleEnvironment(int direction)
    {
        if (!Debug.isDebugBuild) return;
        RetroLightSystem.CycleEnvironmentProfile(direction);
        RetroEnvironmentProfile environment = RetroLightSystem.ActiveEnvironment;
        SetActionStatus("Environment: " + (environment != null ? environment.name : "Default"));
    }

    private void ReloadScene()
    {
        if (!Debug.isDebugBuild) return;
        GameSceneLoader loader = FindAnyObjectByType<GameSceneLoader>();
        if (loader != null) loader.ReloadCurrentScene();
    }

    private void RequestSaveReset()
    {
        if (!Debug.isDebugBuild || resetSaveButton == null) return;
        if (Time.unscaledTime > resetConfirmationExpiresAt)
        {
            resetConfirmationExpiresAt = Time.unscaledTime + 5f;
            resetSaveButton.text = "CONFIRM RESET SAVE";
            SetActionStatus("Click again within 5 seconds to erase this game's save.");
            return;
        }

        GameSaveResetService.ResetAllSavesAndReload(
            playerTransform != null ? playerTransform.gameObject : null);
    }

    private void ClearKeyItems()
    {
        if (!Debug.isDebugBuild) return;
        KeyItemInventory.ResetForNewGame();
        SetActionStatus("Key item inventory cleared.");
    }

    private void SetActionStatus(string message)
    {
        if (debugActionStatus != null) debugActionStatus.text = message;
    }

    private void UpdateDebugActionLabels()
    {
        if (heatControls != null)
            heatControls.style.display = movesetController != null &&
                movesetController.UsesHeat ? DisplayStyle.Flex : DisplayStyle.None;
        if (godModeButton != null)
            godModeButton.text = "GOD MODE: " +
                (playerHealth != null && playerHealth.IsGodMode ? "ON" : "OFF");
        if (combatOverrideButton != null)
            combatOverrideButton.text = "COMBAT OVERRIDE: " +
                (combatStatus == null || !combatStatus.IsCombatStateForced
                    ? "NEUTRAL" : combatStatus.IsForcedInCombat ? "FORCE IN" : "FORCE OUT");
        if (slowMotionButton != null)
            slowMotionButton.text = GameDebugTimeScale.IsSlowMotion
                ? $"RESET GAME TIME ({GameDebugTimeScale.CurrentScale:0.###}x)"
                : "SLOW MOTION: OFF";
        if (resetSaveButton != null && Time.unscaledTime > resetConfirmationExpiresAt)
            resetSaveButton.text = "RESET GAME SAVE";
    }

    private void UpdateDebugText()
    {
        if (!HasPlayerComponents())
        {
            debugText.text = "DEBUG HUD\nPlayer components not found.";
            return;
        }

        textBuilder.Clear();
        textBuilder.AppendLine("DEBUG HUD");
        textBuilder.Append("STATE: ")
            .AppendLine(playerState.CurrentState.ToString());
        if (playerHealth.IsHeavyStaggered)
        {
            textBuilder.Append("HEAVY STAGGER: ")
                .Append(playerHealth.CurrentStaggerRemaining.ToString("0.00"))
                .Append(" s | IMMUNE: ")
                .Append(playerHealth.IsDamageImmune ? "YES" : "NO")
                .Append(" | EARLY EXIT: ")
                .AppendLine(playerHealth.CanExitHeavyStaggerEarly
                    ? "READY" : "LOCKED");
        }
        textBuilder.Append("MOVESET: ")
            .AppendLine(movesetController != null
                ? movesetController.CurrentMovesetName
                : "NONE");
        textBuilder.Append("GOD MODE: ")
            .AppendLine(playerHealth.IsGodMode ? "ON" : "OFF");
        textBuilder.Append("COMBAT: ")
            .AppendLine(combatStatus.IsOutOfCombat
                ? "OUT OF COMBAT"
                : "IN COMBAT");
        if (combatStatus.IsCombatStateForced)
        {
            textBuilder.Append("COMBAT OVERRIDE: ")
                .AppendLine(combatStatus.IsForcedInCombat
                    ? "FORCED IN"
                    : "FORCED OUT");
        }
        else
        {
            textBuilder.AppendLine("COMBAT OVERRIDE: NEUTRAL");
        }
        textBuilder.Append("ENEMY AGGRO: ")
            .AppendLine(combatStatus.HasAggroedEnemies ? "YES" : "NO");
        textBuilder.Append("COMBAT TIMER: ")
            .AppendLine(combatStatus.CombatTimer.ToString("F2") + " s");
        textBuilder.Append("GROUNDED: ")
            .AppendLine(playerMovement.IsGrounded ? "YES" : "NO");
        textBuilder.Append("MOVES LAST FRAME: ")
            .AppendLine(playerMovement.MovementCommitsInLastFrame.ToString());
        textBuilder.Append("POISE: ")
            .Append(Mathf.CeilToInt(playerHealth.CurrentPoise))
            .Append(" / ")
            .AppendLine(playerHealth.maxPoise.ToString());
        textBuilder.Append("CROUCHING: ")
            .AppendLine(playerMovement.IsCrouching ? "YES" : "NO");
        textBuilder.Append("SLIDING: ")
            .AppendLine(playerState.IsSliding() ? "YES" : "NO");
        textBuilder.Append("MOVE SPEED: ")
            .Append(playerMovement.HorizontalSpeed.ToString("F2"))
            .Append(" / ")
            .AppendLine(playerMovement.Speed.ToString("F2"));
        textBuilder.Append("VERTICAL SPEED: ")
            .AppendLine(playerMovement.VerticalSpeed.ToString("F2"));
        textBuilder.Append("DASH: ")
            .AppendLine(playerMovement.DashCooldownRemaining.ToString("F2") + " s");
        textBuilder.Append("TIME SCALE: ")
            .AppendLine(Time.timeScale.ToString("0.###"));
        textBuilder.Append("ENCOUNTER SLOWMO: ")
            .AppendLine(GameTimeScaleEffects.IsCompletionSlowMotionActive
                ? "YES" : "NO");
        bool usesHealth = movesetController != null && movesetController.UsesHealth;
        if (usesHealth)
        {
            textBuilder.Append("HEALTH: ")
                .Append(playerHealth.CurrentHealth)
                .Append(" / ")
                .AppendLine(playerHealth.MaxHealth.ToString());
        }
        else
        {
            textBuilder.Append("VITAL OIL: ")
                .Append(playerHealth.CurrentVitalOil)
                .Append(" / ")
                .AppendLine(playerHealth.MaxVitalOil.ToString());
            textBuilder.Append("HEAT: ")
                .Append(FormatSigned(playerHeat.CurrentHeat))
                .Append(" / ")
                .AppendLine(playerHeat.MaxHeat.ToString("0"));
            textBuilder.Append("HEAT NOTCH: ")
                .AppendLine(playerHeat.CurrentHeatNotch.ToString());
            textBuilder.Append("HEAVY POWER: ")
                .AppendLine(playerHeat.HeavyDamageMultiplier.ToString("0.#") + "x");
            textBuilder.Append("HEAVY POISE: ")
                .AppendLine(playerHeat.HeavyPoiseMultiplier.ToString("0.##") + "x");
            textBuilder.Append("BURN: ")
                .AppendLine(playerHeat.IsBurning ? "YES" : "NO");
            textBuilder.Append("RAGE: ")
                .AppendLine(playerHeat.IsRaging
                    ? playerHeat.RageTimeRemaining.ToString("0.0") + " s"
                    : playerHeat.HasRageUnlocked ? "UNLOCKED" : "LOCKED");
        }
        textBuilder.Append("CURRENCY: ")
            .AppendLine((playerCurrency != null ? playerCurrency.Current : 0).ToString());
        textBuilder.Append("ATTACKING: ")
            .AppendLine(attackController.IsAttacking ? "YES" : "NO");
        debugText.text = textBuilder.ToString();
    }

    private void UpdateCutsceneDebug()
    {
        if (cutsceneDebugPanel == null || cutsceneDebugText == null)
            return;

        CutsceneController cutscene = CutsceneController.Active;
        bool show = cutscene != null;
        cutsceneDebugPanel.style.display = show
            ? DisplayStyle.Flex
            : DisplayStyle.None;
        if (!show)
            return;

        cutsceneTextBuilder.Clear();
        cutsceneTextBuilder.AppendLine("CUTSCENE DEBUG");
        cutsceneTextBuilder.Append("OBJECT: ")
            .AppendLine(cutscene.name);
        cutsceneTextBuilder.Append("TIMELINE: ")
            .AppendLine(cutscene.TimelineName);
        cutsceneTextBuilder.Append("STATE: ")
            .AppendLine(cutscene.IsRunning ? "RUNNING" : "STOPPED");
        cutsceneTextBuilder.Append("TIME: ")
            .Append(cutscene.TimelineTime.ToString("0.00"))
            .Append(" / ")
            .Append(cutscene.TimelineDuration.ToString("0.00"))
            .Append(" s (")
            .Append((cutscene.TimelineProgress * 100f).ToString("0.0"))
            .AppendLine("%)");
        cutsceneTextBuilder.Append("UPDATE: ")
            .AppendLine(cutscene.TimelineUpdateMode.ToString());
        cutsceneTextBuilder.Append("PLAYER LOCK: ")
            .AppendLine(CutsceneController.IsPlayerControlLocked ? "YES" : "NO");
        cutsceneTextBuilder.Append("ENEMIES FROZEN: ")
            .AppendLine(CutsceneController.AreEnemiesFrozen ? "YES" : "NO");
        cutsceneTextBuilder.Append("SKIP: ")
            .AppendLine(cutscene.CanSkip ? cutscene.SkipKeyName : "DISABLED");
        cutsceneDebugText.text = cutsceneTextBuilder.ToString();
        if (cutsceneTimelineFill != null)
            cutsceneTimelineFill.style.width = Length.Percent(
                cutscene.TimelineProgress * 100f);
    }

    private static string FormatSigned(float value)
    {
        return (value > 0f ? "+" : string.Empty) + value.ToString("0.0");
    }
}

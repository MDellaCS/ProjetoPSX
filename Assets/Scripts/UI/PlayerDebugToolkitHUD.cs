using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD de inspecao F3 e menu de comandos F4.
/// O UXML precisa ter os elementos "debug-panel" e "debug-text".
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class PlayerDebugToolkitHUD : MonoBehaviour
{
    public static bool IsVisible { get; private set; }
    public static bool IsDebugMenuVisible { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDebugVisibility()
    {
        IsVisible = false;
        IsDebugMenuVisible = false;
    }

    [Header("References")]
    [SerializeField] private PanelRenderer panelRenderer;
    [SerializeField] private Transform playerTransform;

    private readonly StringBuilder textBuilder = new StringBuilder();
    private readonly StringBuilder cutsceneTextBuilder = new StringBuilder();

    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerHeat playerHeat;
    private PlayerHealth playerHealth;
    private PlayerCombatStatus combatStatus;
    private PlayerAttackController attackController;
    private PlayerMovesetController movesetController;
    private PlayerLockOn playerLockOn;
    private PlayerCurrencyWallet playerCurrency;
    private PlayerCharacterController playerCharacterController;
    private WaterSensor waterSensor;

    private VisualElement rootVisualElement;
    private VisualElement debugPanel;
    private Label debugText;
    private Label waterStateLabel;
    private VisualElement enemyDebugPanel;
    private Label enemyDebugText;
    private VisualElement cutsceneDebugPanel;
    private Label cutsceneDebugText;
    private VisualElement cutsceneTimelineFill;
    private VisualElement debugMenuPanel;
    private VisualElement heatControls;
    private VisualElement boundButtonRoot;
    private IntegerField currencyInput;
    private Label debugActionStatus;
    private Button godModeButton;
    private Button combatOverrideButton;
    private Button slowMotionButton;
    private Button resetSaveButton;
    private float resetConfirmationExpiresAt;
    private bool currencyInputEdited;
    private UnityEngine.CursorLockMode cursorLockBeforeDebug;
    private bool cursorVisibleBeforeDebug;
    private bool isVisible;

    private void Awake()
    {
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

        if ((!isVisible && !IsDebugMenuVisible) || debugText == null)
            return;

        // PlayerLockOn pode ser adicionado pelo sistema de ataques durante Awake.
        // A ordem de Awake entre objetos nao e garantida, entao tentamos
        // encontra-lo novamente enquanto o F3 estiver aberto.
        if (!HasPlayerComponents() || playerLockOn == null || playerCurrency == null ||
            movesetController == null)
            ResolvePlayerComponents();

        UpdateDebugText();
        UpdateDebugActionLabels();
        UpdateLockedEnemyDebug();
        UpdateCutsceneDebug();
    }

    private void HandleUIReload(
        PanelRenderer _,
        VisualElement rootVisualElement,
        int __)
    {
        if (rootVisualElement == null)
            return;

        this.rootVisualElement = rootVisualElement;
        debugPanel = rootVisualElement.Q<VisualElement>("debug-panel");
        debugText = rootVisualElement.Q<Label>("debug-text");
        waterStateLabel = rootVisualElement.Q<Label>("water-state-label");
        enemyDebugPanel = rootVisualElement.Q<VisualElement>("enemy-debug-panel");
        enemyDebugText = rootVisualElement.Q<Label>("enemy-debug-text");
        cutsceneDebugPanel = rootVisualElement.Q<VisualElement>("cutscene-debug-panel");
        cutsceneDebugText = rootVisualElement.Q<Label>("cutscene-debug-text");
        cutsceneTimelineFill = rootVisualElement.Q<VisualElement>("cutscene-timeline-fill");
        debugMenuPanel = rootVisualElement.Q<VisualElement>("debug-menu-panel");
        heatControls = rootVisualElement.Q<VisualElement>("heat-controls");
        currencyInput = rootVisualElement.Q<IntegerField>("currency-input");
        debugActionStatus = rootVisualElement.Q<Label>("debug-action-status");
        godModeButton = rootVisualElement.Q<Button>("god-mode-button");
        combatOverrideButton = rootVisualElement.Q<Button>("combat-override-button");
        slowMotionButton = rootVisualElement.Q<Button>("slow-motion-button");
        resetSaveButton = rootVisualElement.Q<Button>("reset-save-button");
        BindDebugButtons(rootVisualElement);
        if (currencyInput != null)
        {
            currencyInput.RegisterValueChangedCallback(_ => currencyInputEdited = true);
            if (playerCurrency != null)
                currencyInput.SetValueWithoutNotify(playerCurrency.Current);
        }

        SetHudVisible(isVisible);
        SetDebugMenuVisible(IsDebugMenuVisible);
        if (waterStateLabel != null)
            waterStateLabel.style.display = DisplayStyle.None;
    }

    private void LateUpdate()
    {
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
        movesetController = playerTransform.GetComponent<PlayerMovesetController>();
        playerLockOn = playerTransform.GetComponent<PlayerLockOn>();
        playerCurrency = playerTransform.GetComponent<PlayerCurrencyWallet>();
        playerCharacterController = playerTransform.GetComponent<PlayerCharacterController>();
        waterSensor = playerTransform.GetComponent<WaterSensor>();
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

        if (debugPanel != null)
            debugPanel.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        if (enemyDebugPanel != null)
            enemyDebugPanel.style.display = DisplayStyle.None;
        if (cutsceneDebugPanel != null)
            cutsceneDebugPanel.style.display = DisplayStyle.None;
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
        if (debugMenuPanel != null)
            debugMenuPanel.style.display = value
                ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindDebugButtons(VisualElement root)
    {
        if (root.Q<Button>("debug-close-button") == null ||
            boundButtonRoot == root)
            return;
        boundButtonRoot = root;
        BindButton(root, "debug-close-button", CloseDebugMenu);
        BindButton(root, "status-max-button", () => SetPlayerStatus(true));
        BindButton(root, "status-zero-button", () => SetPlayerStatus(false));
        BindButton(root, "heat-minus-button", () => AdjustPlayerHeat(-10f));
        BindButton(root, "heat-plus-button", () => AdjustPlayerHeat(10f));
        BindButton(root, "currency-set-button", SetCurrency);
        BindButton(root, "god-mode-button", ToggleGodMode);
        BindButton(root, "combat-override-button", CycleCombatOverride);
        BindButton(root, "kill-player-button", KillPlayer);
        BindButton(root, "slow-motion-button", ToggleSlowMotion);
        BindButton(root, "next-moveset-button", CycleMoveset);
        BindButton(root, "next-scene-button", LoadNextScene);
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

    private void CloseDebugMenu()
    {
        if (Debug.isDebugBuild) SetDebugMenuVisible(false);
    }

    private void SetPlayerStatus(bool maximize)
    {
        if (!Debug.isDebugBuild) return;
        if (playerHealth != null) playerHealth.DebugSetStatus(maximize);
        if (playerHeat != null) playerHeat.DebugSetStatus(maximize);
        SetActionStatus(maximize ? "Player status maximized." : "Player status zeroed.");
    }

    private void AdjustPlayerHeat(float amount)
    {
        if (!Debug.isDebugBuild || playerHeat == null) return;
        playerHeat.DebugAdjustHeat(amount);
        SetActionStatus("Heat: " + playerHeat.CurrentHeat.ToString("0.0"));
    }

    private void SetCurrency()
    {
        if (!Debug.isDebugBuild || currencyInput == null) return;
        if (playerCurrency == null && playerTransform != null)
            playerCurrency = playerTransform.GetComponent<PlayerCurrencyWallet>();
        if (playerCurrency == null && playerTransform != null)
            playerCurrency = playerTransform.gameObject.AddComponent<PlayerCurrencyWallet>();
        if (playerCurrency == null) return;
        playerCurrency.DebugSetCurrency(currencyInput.value);
        currencyInputEdited = false;
        currencyInput.SetValueWithoutNotify(playerCurrency.Current);
        SetActionStatus("Money set to " + playerCurrency.Current + ".");
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

    private void CycleMoveset()
    {
        if (!Debug.isDebugBuild || movesetController == null) return;
        movesetController.DebugCycleMoveset();
        SetActionStatus("Moveset: " + movesetController.CurrentMovesetName);
    }

    private void LoadNextScene()
    {
        if (!Debug.isDebugBuild) return;
        GameSceneLoader loader = FindAnyObjectByType<GameSceneLoader>();
        if (loader != null) loader.DebugLoadNextScene();
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
            slowMotionButton.text = "SLOW MOTION: " +
                (GameDebugTimeScale.IsSlowMotion ? "ON" : "OFF");
        if (currencyInput != null && playerCurrency != null && !currencyInputEdited)
            currencyInput.SetValueWithoutNotify(playerCurrency.Current);
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
            textBuilder.Append("SUPERHEAT: ")
                .Append(playerHeat.CurrentSuperheat.ToString("0.0"))
                .Append(" / ")
                .AppendLine(playerHeat.MaxSuperheat.ToString("0"));
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

    private void UpdateLockedEnemyDebug()
    {
        if (enemyDebugPanel == null || enemyDebugText == null)
            return;

        EnemyBase enemy = playerLockOn != null
            ? playerLockOn.LockedEnemy
            : null;
        bool show = enemy != null;
        enemyDebugPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

        if (show)
            enemyDebugText.text = enemy.GetDebugStatusText();
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

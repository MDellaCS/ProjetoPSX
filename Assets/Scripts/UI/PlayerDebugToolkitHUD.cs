using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Versao UI Toolkit do HUD de debug aberto por F3.
/// O UXML precisa ter os elementos "debug-panel" e "debug-text".
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class PlayerDebugToolkitHUD : MonoBehaviour
{
    public static bool IsVisible { get; private set; }

    [Header("References")]
    [SerializeField] private PanelRenderer panelRenderer;
    [SerializeField] private Transform playerTransform;

    [Header("Display")]
    [SerializeField] private bool visibleOnStart;

    private readonly StringBuilder textBuilder = new StringBuilder();

    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerHeat playerHeat;
    private PlayerHealth playerHealth;
    private PlayerCombatStatus combatStatus;
    private PlayerAttackController attackController;
    private PlayerLockOn playerLockOn;

    private VisualElement debugPanel;
    private Label debugText;
    private VisualElement enemyDebugPanel;
    private Label enemyDebugText;
    private bool isVisible;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);

        ResolvePlayerComponents();
        SetVisible(visibleOnStart);
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);

        IsVisible = false;
    }

    private void OnDisable()
    {
        IsVisible = false;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.f3Key.wasPressedThisFrame)
        {
            SetVisible(!isVisible);
        }

        if (!isVisible || debugText == null)
            return;

        // PlayerLockOn pode ser adicionado pelo sistema de ataques durante Awake.
        // A ordem de Awake entre objetos nao e garantida, entao tentamos
        // encontra-lo novamente enquanto o F3 estiver aberto.
        if (!HasPlayerComponents() || playerLockOn == null)
            ResolvePlayerComponents();

        UpdateDebugText();
        UpdateLockedEnemyDebug();
    }

    private void HandleUIReload(
        PanelRenderer _,
        VisualElement rootVisualElement,
        int __)
    {
        if (rootVisualElement == null)
            return;

        debugPanel = rootVisualElement.Q<VisualElement>("debug-panel");
        debugText = rootVisualElement.Q<Label>("debug-text");
        enemyDebugPanel = rootVisualElement.Q<VisualElement>("enemy-debug-panel");
        enemyDebugText = rootVisualElement.Q<Label>("enemy-debug-text");

        SetVisible(isVisible);
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

    private void SetVisible(bool value)
    {
        isVisible = value;
        IsVisible = value;

        if (debugPanel != null)
        {
            debugPanel.style.display = value
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        if (enemyDebugPanel != null)
            enemyDebugPanel.style.display = DisplayStyle.None;
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
        textBuilder.Append("CROUCHING: ")
            .AppendLine(playerMovement.IsCrouching ? "YES" : "NO");
        textBuilder.Append("SLIDING: ")
            .AppendLine(playerState.IsSliding() ? "YES" : "NO");
        textBuilder.Append("MOVE SPEED: ")
            .Append(playerMovement.HorizontalSpeed.ToString("F2"))
            .Append(" / ")
            .AppendLine(playerMovement.speed.ToString("F2"));
        textBuilder.Append("VERTICAL SPEED: ")
            .AppendLine(playerMovement.VerticalSpeed.ToString("F2"));
        textBuilder.Append("DASH: ")
            .AppendLine(playerMovement.DashCooldownRemaining.ToString("F2") + " s");
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

    private static string FormatSigned(float value)
    {
        return (value > 0f ? "+" : string.Empty) + value.ToString("0.0");
    }
}

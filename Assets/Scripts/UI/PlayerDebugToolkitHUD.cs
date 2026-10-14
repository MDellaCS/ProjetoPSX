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
    private PlayerCombat playerCombat;
    private PlayerInputBuffer playerInputBuffer;

    private VisualElement debugPanel;
    private Label debugText;
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

        if (!HasPlayerComponents())
            ResolvePlayerComponents();

        UpdateDebugText();
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
        playerCombat = playerTransform.GetComponent<PlayerCombat>();
        playerInputBuffer = playerTransform.GetComponent<PlayerInputBuffer>();
    }

    private bool HasPlayerComponents()
    {
        return playerMovement != null &&
               playerState != null &&
               playerHeat != null &&
               playerCombat != null &&
               playerInputBuffer != null;
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
        textBuilder.Append("COMBAT: ")
            .AppendLine(playerCombat.IsOutOfCombat
                ? "OUT OF COMBAT"
                : "IN COMBAT");
        if (playerCombat.IsCombatStateForced)
        {
            textBuilder.Append("COMBAT OVERRIDE: ")
                .AppendLine(playerCombat.IsForcedInCombat
                    ? "FORCED IN"
                    : "FORCED OUT");
        }
        else
        {
            textBuilder.AppendLine("COMBAT OVERRIDE: NEUTRAL");
        }
        textBuilder.Append("ENEMY AGGRO: ")
            .AppendLine(playerCombat.HasAggroedEnemies ? "YES" : "NO");
        textBuilder.Append("COMBAT TIMER: ")
            .AppendLine(playerCombat.CombatTimer.ToString("F2") + " s");
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
            .AppendLine(playerHeat.MaxHeat.ToString());
        textBuilder.Append("SUPERHEAT: ")
            .Append(playerHeat.CurrentSuperheat)
            .Append(" / ")
            .AppendLine(playerHeat.MaxSuperheat.ToString());
        textBuilder.Append("ATTACKING: ")
            .AppendLine(playerCombat.IsAttacking ? "YES" : "NO");
        textBuilder.Append("MOUSE: ")
            .AppendLine(playerInputBuffer.HasMouseDevice ? "READY" : "MISSING");
        textBuilder.Append("CLICKS DETECTED: L ")
            .Append(playerInputBuffer.LightPressesDetected)
            .Append(" / H ")
            .AppendLine(playerInputBuffer.HeavyPressesDetected.ToString());

        debugText.text = textBuilder.ToString();
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }
}

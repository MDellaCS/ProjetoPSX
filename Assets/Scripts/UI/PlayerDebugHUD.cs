using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDebugHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private Transform playerTransform;

    [Header("Display")]
    [SerializeField] private bool visibleOnStart;

    private readonly StringBuilder textBuilder = new StringBuilder();

    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerHeat playerHeat;
    private PlayerCombat playerCombat;

    private bool isVisible;

    void Awake()
    {
        if (debugText == null)
        {
            debugText = GetComponent<TMP_Text>();
        }

        ResolvePlayerComponents();
        SetVisible(visibleOnStart);
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.f3Key.wasPressedThisFrame)
        {
            SetVisible(!isVisible);
        }

        if (!isVisible || debugText == null)
            return;

        if (!HasPlayerComponents())
        {
            ResolvePlayerComponents();
        }

        UpdateDebugText();
    }

    private void ResolvePlayerComponents()
    {
        if (playerTransform == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (playerTransform == null)
            return;

        playerMovement = playerTransform.GetComponent<PlayerMovement>();
        playerState = playerTransform.GetComponent<PlayerState>();
        playerHeat = playerTransform.GetComponent<PlayerHeat>();
        playerCombat = playerTransform.GetComponent<PlayerCombat>();
    }

    private bool HasPlayerComponents()
    {
        return playerMovement != null &&
               playerState != null &&
               playerHeat != null &&
               playerCombat != null;
    }

    private void SetVisible(bool value)
    {
        isVisible = value;

        if (debugText != null)
        {
            debugText.enabled = isVisible;
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
            .AppendLine(
                playerCombat.IsOutOfCombat
                    ? "OUT OF COMBAT"
                    : "IN COMBAT"
            );
        textBuilder.Append("ENEMY AGGRO: ").AppendLine(playerCombat.HasAggroedEnemies ? "YES" : "NO");
        textBuilder.Append("COMBAT TIMER: ").AppendLine(FormatCombatTimer());
        textBuilder.Append("GROUNDED: ").AppendLine(playerMovement.IsGrounded ? "YES" : "NO");
        textBuilder.Append("CROUCHING: ").AppendLine(playerMovement.IsCrouching ? "YES" : "NO");
        textBuilder.Append("SLIDING: ").AppendLine(playerState.IsSliding() ? "YES" : "NO");
        textBuilder.Append("MOVE SPEED: ").Append(playerMovement.HorizontalSpeed.ToString("F2")).Append(" / ").AppendLine(playerMovement.speed.ToString("F2"));
        textBuilder.Append("VERTICAL SPEED: ").AppendLine(playerMovement.VerticalSpeed.ToString("F2"));

        textBuilder.Append("DASH: ").AppendLine(playerMovement.DashCooldownRemaining.ToString("F2") + " s");

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

        debugText.text = textBuilder.ToString();
    }

    private string FormatSigned(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }

    private string FormatCombatTimer()
    {
        return playerCombat.CombatTimer.ToString("F2") + " s";
    }
}

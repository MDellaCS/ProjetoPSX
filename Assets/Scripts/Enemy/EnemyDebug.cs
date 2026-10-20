using UnityEngine;

/// <summary>Dados do inimigo para F3 e visualizacao das hitboxes.</summary>
[DisallowMultipleComponent]
public sealed class EnemyDebug : MonoBehaviour
{
    private static GUIStyle aggressionStyle;
    private static GUIStyle aggressionShadowStyle;
    private static GUIStyle leadArrowStyle;

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyAttackController attacks;
    private EnemyMovement movement;
    private EnemyHealth health;
    private CharacterController characterController;

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyAttackController attackController, EnemyMovement locomotion,
        EnemyHealth enemyHealth)
    {
        owner = enemy;
        brain = enemyBrain;
        attacks = attackController;
        movement = locomotion;
        health = enemyHealth;
        characterController = GetComponent<CharacterController>();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible ||
            owner == null || owner.IsDead || brain == null)
        {
            return;
        }

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
            return;

        Vector3 headPosition = characterController != null
            ? new Vector3(characterController.bounds.center.x,
                characterController.bounds.max.y + .35f,
                characterController.bounds.center.z)
            : transform.position + Vector3.up * 2f;
        Vector3 screenPosition = gameCamera.WorldToScreenPoint(headPosition);
        if (screenPosition.z <= 0f)
            return;

        EnsureWorldDebugStyles();
        const float width = 180f;
        float x = screenPosition.x - width * .5f;
        float y = Screen.height - screenPosition.y;
        string realAggressionText =
            brain.CurrentAggressionPercent.ToString("0.0") + "%";
        string effectiveAggressionText =
            brain.EffectiveAggressionPercent.ToString("0.0") + "%";

        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        GUI.depth = -110;

        Rect realAggressionRect = new Rect(x, y, width, 38f);
        Rect effectiveAggressionRect = new Rect(x, y + 31f, width, 38f);
        DrawWorldDebugText(realAggressionRect, realAggressionText,
            Color.white);
        DrawWorldDebugText(effectiveAggressionRect, effectiveAggressionText,
            new Color(.3f, .9f, 1f, 1f));

        if (EnemyDirector.IsLeadEnemy(owner))
        {
            GUI.color = new Color(1f, .8f, .1f, 1f);
            GUI.Label(new Rect(x, y - 46f, width, 48f), "▼",
                leadArrowStyle);
        }

        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private static void DrawWorldDebugText(Rect rect, string text, Color color)
    {
        GUI.color = Color.black;
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f,
            rect.width, rect.height), text, aggressionShadowStyle);
        GUI.color = color;
        GUI.Label(rect, text, aggressionStyle);
    }

    private static void EnsureWorldDebugStyles()
    {
        if (aggressionStyle != null)
            return;

        aggressionStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 28
        };
        aggressionShadowStyle = new GUIStyle(aggressionStyle);
        leadArrowStyle = new GUIStyle(aggressionStyle)
        {
            fontSize = 38
        };
    }

    public void DrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible)
            return;
        if (brain == null || attacks == null)
            return;

        Gizmos.color = new Color(.2f, .85f, 1f, .7f);
        Gizmos.DrawWireSphere(transform.position, brain.DetectionRange);

        // As formas ativas sao desenhadas pelo EnemyAttackHitboxController.
    }

    public string GetStatusText()
    {
        if (brain == null || attacks == null || movement == null || health == null)
            return "Enemy: aguardando inicializacao";

        EnemyAttackDefinition attack = attacks.CurrentAttack?.Definition;
        string attackName = attack != null ? attack.displayName : "-";
        string timings = attack != null
            ? attack.windupDuration.ToString("0.00") + " / " +
                attack.activeDuration.ToString("0.00") + " / " +
                attack.recoveryDuration.ToString("0.00")
            : "-";
        string status = movement.IsLaunchedForwards ? "LAUNCHED FORWARDS" :
            movement.IsLaunched ? "LAUNCHED" :
            brain.IsSearchingLastKnownPosition ? "SEARCHING" : "GROUND";
        string cameraStatus = brain.IsCameraVisible ? "VISIBLE" : "HIDDEN";
        return owner.GetType().Name + "\n" +
            "HP: " + health.CurrentHealth + " / " + health.MaxHealth +
            "   POISE: " + Mathf.CeilToInt(health.CurrentPoise) +
                " / " + health.MaxPoise + "\n" +
            "STATE: " + owner.CurrentState + "   " + status + "\n" +
            "AGGRO: " + brain.CurrentAggressionPercent.ToString("0.0") +
                "% (" + brain.CurrentAggressionVelocity.ToString("+0.0;-0.0;0.0") +
                "/s)\n" +
            "DIRECTOR: " + EnemyDirector.GetRoleLabel(owner) +
                "   WEIGHT: " +
                EnemyDirector.GetAssignedWeight(owner).ToString("0.#") +
                "   BUDGET: " +
                EnemyDirector.GetCurrentLeadWeight().ToString("0.#") +
                " / " + EnemyDirector.GetMaximumLeadWeight().ToString("0.#") +
                "\n" +
                "   EFFECTIVE AGGRO: " +
                brain.EffectiveAggressionPercent.ToString("0.0") + "%\n" +
            "MOOD: " + brain.AggressionChangeTimer.ToString("0.00") +
                "s   BACK: " + (brain.IsPlayerBackTurned ? "YES" : "NO") + "\n" +
            "CAMERA: " + cameraStatus + "\n" +
            "COMMIT: " + brain.CommitmentTimer.ToString("0.00") +
                "s   CD: " + brain.CommitmentCooldownTimer.ToString("0.00") + "s\n" +
            "MOVE: " + movement.MovementSpeed.ToString("0.00") +
                "   DETECT: " + brain.DetectionRange.ToString("0.00") +
                " / " + brain.CombatDetectionRange.ToString("0.00") + "\n" +
            "ATTACK: " + attackName + "\n" +
            "W/A/R: " + timings + "\n" +
            "TARGET: " + (brain.Target != null ? brain.Target.name : "None");
    }
}

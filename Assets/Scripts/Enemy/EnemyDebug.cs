using UnityEngine;

/// <summary>Dados do inimigo para F3 e visualizacao das hitboxes.</summary>
[DisallowMultipleComponent]
public sealed class EnemyDebug : MonoBehaviour
{
    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyAttackController attacks;
    private EnemyMovement movement;
    private EnemyHealth health;

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyAttackController attackController, EnemyMovement locomotion,
        EnemyHealth enemyHealth)
    {
        owner = enemy;
        brain = enemyBrain;
        attacks = attackController;
        movement = locomotion;
        health = enemyHealth;
    }

    public void DrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible)
            return;
        if (brain == null || attacks == null)
            return;

        Gizmos.color = new Color(.2f, .85f, 1f, .7f);
        Gizmos.DrawWireSphere(transform.position, brain.DetectionRange);

        EnemyAttackData attack = attacks.CurrentAttack;
        if (owner.CurrentState != EnemyBase.State.Active || attack == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position +
            transform.forward * attack.hitboxDistance, attack.hitboxRadius);
    }

    public string GetStatusText()
    {
        if (brain == null || attacks == null || movement == null || health == null)
            return "Enemy: aguardando inicializacao";

        EnemyAttackData attack = attacks.CurrentAttack;
        string attackName = attack != null ? attack.attackName : "-";
        string timings = attack != null
            ? attack.windupDuration.ToString("0.00") + " / " +
                attack.activeDuration.ToString("0.00") + " / " +
                attack.recoveryDuration.ToString("0.00")
            : "-";
        string status = movement.IsPushed ? "PUSHED" :
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

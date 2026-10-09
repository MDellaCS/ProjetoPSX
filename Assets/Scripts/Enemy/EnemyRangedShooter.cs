using UnityEngine;

/// <summary>
/// Configura um EnemyCombat como inimigo de longa distancia de um unico tiro.
/// Mantem EnemyCombat/EnemyMovement para encounters, poise, stun e morte.
/// </summary>
[RequireComponent(typeof(EnemyCombat), typeof(EnemyMovement))]
public class EnemyRangedShooter : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [Min(0.01f)] [SerializeField] private float projectileSpeed = 14f;
    [Min(0.01f)] [SerializeField] private float projectileHitRadius = 0.3f;
    [Min(0.1f)] [SerializeField] private float projectileLifetime = 2f;

    [Header("Distance")]
    [Min(0f)] [SerializeField] private float minimumAttackRange = 4f;
    [Min(0.1f)] [SerializeField] private float maximumAttackRange = 12f;
    [Min(0f)] [SerializeField] private float preferredDistance = 7f;

    [Header("Attack")]
    [Min(0)] [SerializeField] private int damage = 18;
    [Min(0f)] [SerializeField] private float windupDuration = 0.55f;
    [Min(0f)] [SerializeField] private float activeDuration = 0.1f;
    [Min(0f)] [SerializeField] private float recoveryDuration = 0.65f;
    [Min(0f)] [SerializeField] private float knockbackForce = 5f;
    [Min(0f)] [SerializeField] private float knockbackDuration = 0.15f;

    private void Awake()
    {
        EnemyCombat enemyCombat = GetComponent<EnemyCombat>();
        EnemyMovement enemyMovement = GetComponent<EnemyMovement>();

        enemyCombat.SetCustomAttackOptions(new[]
        {
            new EnemyAttackOption
            {
                attack = new EnemyAttackData
                {
                    attackName = "Projectile Shot",
                    damage = damage,
                    minimumRange = minimumAttackRange,
                    maximumRange = maximumAttackRange,
                    windupDuration = windupDuration,
                    activeDuration = activeDuration,
                    recoveryDuration = recoveryDuration,
                    knockbackForce = knockbackForce,
                    knockbackDuration = knockbackDuration,
                    firesProjectile = true,
                    projectilePrefab = projectilePrefab,
                    projectileSpawnPoint = projectileSpawnPoint,
                    projectileSpeed = projectileSpeed,
                    projectileHitRadius = projectileHitRadius,
                    projectileLifetime = projectileLifetime
                },
                closeWeight = 1f,
                mediumWeight = 1f,
                farWeight = 1f
            }
        });

        enemyMovement.stoppingDistance = Mathf.Clamp(
            preferredDistance,
            minimumAttackRange,
            maximumAttackRange
        );
        enemyCombat.detectionRange = Mathf.Max(
            enemyCombat.detectionRange,
            maximumAttackRange + 1f
        );
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && PlayerDebugToolkitHUD.IsVisible)
            DrawDebugGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && PlayerDebugToolkitHUD.IsVisible)
            return;

        DrawDebugGizmos();
    }

    private void DrawDebugGizmos()
    {
        Gizmos.color = new Color(1f, 0.78f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, minimumAttackRange);

        Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, maximumAttackRange);

        Gizmos.color = new Color(0.45f, 1f, 0.45f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, preferredDistance);

        Vector3 origin = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position + Vector3.up + transform.forward * 0.8f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, projectileHitRadius);
        Gizmos.DrawLine(origin, origin + transform.forward * maximumAttackRange);
    }
}

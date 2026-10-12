using UnityEngine;

/// <summary>
/// Arqueiro basico: inimigo lento e passivo que executa um unico disparo
/// retilineo, rapido e com precisao media.
/// </summary>
public class BasicArcher : BaseEnemy
{
    [Header("Basic Archer Attack")]
    [SerializeField] private BaseEnemyAttack[] attackCombo =
    {
        new BaseEnemyAttack
        {
            attackName = "Arrow Shot",
            damage = 14,
            maximumRange = 12f,
            windupDuration = .7f,
            activeDuration = .05f,
            recoveryDuration = .9f,
            knockbackForce = 2f,
            knockbackDuration = .08f
        }
    };

    [Header("Projectile")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [Min(0f)] [SerializeField] private float projectileSpeed = 22f;
    [Min(.01f)] [SerializeField] private float projectileHitRadius = .18f;
    [Min(.01f)] [SerializeField] private float projectileLifetime = 3f;
    [Range(0f, 1f)] [SerializeField] private float accuracy = .65f;
    [Min(0f)] [SerializeField] private float maximumInaccuracyDegrees = 10f;

    protected override BaseEnemyAttack[] AttackCombo => attackCombo;

    protected override bool ExecuteAttack(BaseEnemyAttack attack)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                "BasicArcher: atribua um EnemyProjectile em Projectile Prefab.",
                this
            );
            return true;
        }

        Vector3 spawnPosition = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position + Vector3.up + transform.forward * .7f;
        Vector3 targetPosition = Target != null
            ? Target.position + Vector3.up
            : spawnPosition + transform.forward;
        Vector3 direction = targetPosition - spawnPosition;
        direction = ApplyInaccuracy(direction);

        EnemyProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(direction.normalized)
        );
        projectile.Launch(
            transform,
            direction,
            attack.damage,
            attack.knockbackForce,
            attack.knockbackDuration,
            projectileSpeed,
            projectileHitRadius,
            projectileLifetime
        );
        return true;
    }

    private Vector3 ApplyInaccuracy(Vector3 direction)
    {
        if (direction.sqrMagnitude <= .001f)
            direction = transform.forward;

        float spread = Mathf.Lerp(
            maximumInaccuracyDegrees,
            0f,
            accuracy
        );
        if (spread <= 0f)
            return direction.normalized;

        Quaternion yaw = Quaternion.AngleAxis(
            Random.Range(-spread, spread),
            Vector3.up
        );
        Quaternion pitch = Quaternion.AngleAxis(
            Random.Range(-spread, spread),
            transform.right
        );
        return (yaw * pitch * direction).normalized;
    }
}

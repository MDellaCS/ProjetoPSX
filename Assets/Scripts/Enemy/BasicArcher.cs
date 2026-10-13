using UnityEngine;

/// <summary>
/// Arqueiro basico: inimigo lento e passivo que atira a media distancia e
/// usa um empurrao defensivo se o jogador entrar muito perto.
/// </summary>
public class BasicArcher : BaseEnemy
{
    [Header("Basic Archer Attack")]
    [SerializeField] private BaseEnemyAttack[] attackCombo =
    {
        new BaseEnemyAttack
        {
            attackName = "Arrow Shot",
            isProjectileAttack = true,
            damage = 14,
            maximumRange = 12f,
            windupDuration = .7f,
            activeDuration = .05f,
            recoveryDuration = .9f,
            knockbackForce = 2f,
            knockbackDuration = .08f
        },
        new BaseEnemyAttack
        {
            attackName = "Close Push",
            damage = 5,
            maximumRange = 1.6f,
            windupDuration = .3f,
            activeDuration = .08f,
            recoveryDuration = .75f,
            hitboxDistance = .9f,
            hitboxRadius = .8f,
            knockbackForce = 11f,
            knockbackDuration = .25f
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

    private void Start()
    {
        EnsureAttackDefaults();
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        // Prefabs criados antes do empurrao possuem somente o tiro no array.
        // Preservamos seus valores e acrescentamos o novo ataque.
        EnsureAttackDefaults();
    }

    protected override int SelectAttackIndex()
    {
        // O segundo ataque e exclusivamente defensivo: impede que o arqueiro
        // continue atirando quando o jogador ja esta sobre ele.
        return attackCombo != null && attackCombo.Length > 1 &&
            attackCombo[1] != null && Target != null &&
            Vector3.Distance(transform.position, Target.position) <=
                attackCombo[1].maximumRange
            ? 1
            : 0;
    }

    protected override bool CanContinueAttackCombo(int currentIndex, int nextIndex)
    {
        // Tiro e empurrao sao escolhas separadas, nao um combo.
        return false;
    }

    private void EnsureAttackDefaults()
    {
        if (attackCombo == null || attackCombo.Length == 0)
        {
            attackCombo = new[]
            {
                CreateArrowShot(),
                CreateClosePush()
            };
            return;
        }

        if (attackCombo[0] == null)
            attackCombo[0] = CreateArrowShot();

        // Um BasicArcher sempre usa seu primeiro ataque como tiro.
        attackCombo[0].isProjectileAttack = true;

        if (attackCombo.Length > 1)
        {
            if (attackCombo[1] == null)
                attackCombo[1] = CreateClosePush();
            return;
        }

        System.Array.Resize(ref attackCombo, 2);
        attackCombo[1] = CreateClosePush();
    }

    private static BaseEnemyAttack CreateArrowShot() => new()
    {
        attackName = "Arrow Shot",
        isProjectileAttack = true,
        damage = 14,
        maximumRange = 12f,
        windupDuration = .7f,
        activeDuration = .05f,
        recoveryDuration = .9f,
        knockbackForce = 2f,
        knockbackDuration = .08f
    };

    private static BaseEnemyAttack CreateClosePush() => new()
    {
        attackName = "Close Push",
        damage = 5,
        maximumRange = 1.6f,
        windupDuration = .3f,
        activeDuration = .08f,
        recoveryDuration = .75f,
        hitboxDistance = .9f,
        hitboxRadius = .8f,
        knockbackForce = 11f,
        knockbackDuration = .25f
    };

    protected override bool ExecuteAttack(BaseEnemyAttack attack)
    {
        if (!attack.isProjectileAttack)
            return base.ExecuteAttack(attack);

        if (projectilePrefab == null)
        {
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

using UnityEngine;

/// <summary>Arqueiro lento que atira e empurra jogadores muito proximos.</summary>
public class BasicArcher : EnemyBase
{
    private EnemyAttackController attackController;

    [Header("Projectile")]
    [Tooltip("Prefab do projetil disparado no ataque de tiro.")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [Tooltip("Ponto de onde o tiro sai. Se vazio, usa uma posicao relativa ao arqueiro.")]
    [SerializeField] private Transform projectileSpawnPoint;
    [Tooltip("Velocidade do projetil em linha reta.")]
    [Min(0f)] [SerializeField] private float projectileSpeed = 22f;
    [Tooltip("Raio da colisao do projetil.")]
    [Min(.01f)] [SerializeField] private float projectileHitRadius = .18f;
    [Tooltip("Tempo ate o projetil desaparecer se nao atingir nada.")]
    [Min(.01f)] [SerializeField] private float projectileLifetime = 3f;
    [Tooltip("Precisao do tiro: 1 aponta exatamente para o alvo.")]
    [Range(0f, 1f)] [SerializeField] private float accuracy = .65f;
    [Tooltip("Desvio maximo em graus quando a precisao esta em zero.")]
    [Min(0f)] [SerializeField] private float maximumInaccuracyDegrees = 10f;

    protected override EnemyAttackData[] CreateDefaultAttacks() => new[]
    {
        new EnemyAttackData
        {
            attackName = "Arrow Shot", isProjectileAttack = true,
            damage = 14, maximumRange = 12f, windupDuration = .7f,
            activeDuration = .05f, recoveryDuration = .9f,
            knockbackForce = 2f, knockbackDuration = .08f
        },
        new EnemyAttackData
        {
            attackName = "Close Push", attackId = "ArcherClosePush",
            damage = 5, maximumRange = 1.6f,
            windupDuration = .3f, activeDuration = .08f,
            recoveryDuration = .75f, hitboxDistance = .9f,
            hitboxRadius = .8f, knockbackForce = 11f,
            knockbackDuration = .25f
        }
    };

    protected override bool CanUseAttack(EnemyAttackData attack, int index)
    {
        if (!attack.isProjectileAttack || Target == null)
            return true;
        attackController ??= GetComponent<EnemyAttackController>();
        EnemyAttackData push = null;
        foreach (EnemyAttackData candidate in attackController.Attacks)
        {
            if (candidate != null && candidate.attackId == "ArcherClosePush")
            {
                push = candidate;
                break;
            }
        }
        Vector3 offset = Target.position - transform.position;
        offset.y = 0f;
        return push == null ||
            offset.sqrMagnitude > push.maximumRange * push.maximumRange;
    }

    protected override bool ExecuteAttack(EnemyAttackData attack)
    {
        if (!attack.isProjectileAttack)
            return base.ExecuteAttack(attack);
        if (projectilePrefab == null)
            return true;

        Vector3 spawn = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position + Vector3.up + transform.forward * .7f;
        Vector3 aim = Target != null
            ? Target.position + Vector3.up - spawn
            : transform.forward;
        Vector3 direction = ApplyInaccuracy(aim);
        EnemyProjectile projectile = Instantiate(projectilePrefab, spawn,
            Quaternion.LookRotation(direction));
        projectile.Launch(transform, direction, attack.damage,
            attack.knockbackForce, attack.knockbackDuration,
            projectileSpeed, projectileHitRadius, projectileLifetime);
        return true;
    }

    private Vector3 ApplyInaccuracy(Vector3 direction)
    {
        if (direction.sqrMagnitude <= .001f)
            direction = transform.forward;
        float spread = Mathf.Lerp(maximumInaccuracyDegrees, 0f, accuracy);
        if (spread <= 0f)
            return direction.normalized;
        Quaternion yaw = Quaternion.AngleAxis(Random.Range(-spread, spread),
            Vector3.up);
        Quaternion pitch = Quaternion.AngleAxis(Random.Range(-spread, spread),
            transform.right);
        return (yaw * pitch * direction).normalized;
    }
}

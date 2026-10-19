using UnityEngine;

/// <summary>
/// Zumbi avancado: conserva os golpes do IntermediateZombie e acrescenta uma
/// investida com hitbox durante todo o deslocamento.
/// </summary>
public class AdvancedZombie : IntermediateZombie
{
    private const string ChargeAttackId = "AdvancedZombieCharge";

    [Header("Charge Attack")]
    [Tooltip("Velocidade horizontal da investida durante o tempo Active do ataque de corrida.")]
    [Min(0f)] [SerializeField] private float chargeSpeed = 9f;
    [Tooltip("Distancia minima do jogador para escolher a investida. Evita iniciar a corrida colado nele.")]
    [Min(0f)] [SerializeField] private float minimumChargeRange = 2.5f;

    private readonly Collider[] chargeHits = new Collider[32];
    private Vector3 chargeDirection;
    private Vector3 previousHitboxCenter;

    protected override EnemyAttackData[] CreateDefaultAttacks()
    {
        EnemyAttackData[] inheritedAttacks = base.CreateDefaultAttacks();
        EnemyAttackData[] result = new EnemyAttackData[inheritedAttacks.Length + 1];
        inheritedAttacks.CopyTo(result, 0);
        result[inheritedAttacks.Length] = new EnemyAttackData
        {
            attackName = "Zombie Charge",
            attackId = ChargeAttackId,
            openingWeight = 45f,
            continuationChance = 0f,
            damage = 30,
            maximumRange = 5.5f,
            windupDuration = .8f,
            activeDuration = .5f,
            recoveryDuration = 1.2f,
            hitboxDistance = .8f,
            hitboxRadius = 1f,
            knockbackForce = 5f,
            knockbackDuration = .2f
        };
        return result;
    }

    protected override bool CanUseAttack(EnemyAttackData attack, int index)
    {
        return base.CanUseAttack(attack, index) &&
            (!IsCharge(attack) || Brain.DistanceToTarget() >= minimumChargeRange);
    }

    protected override void OnAttackActiveStarted(EnemyAttackData attack)
    {
        base.OnAttackActiveStarted(attack);
        if (!IsCharge(attack))
            return;

        chargeDirection = Target != null
            ? Target.position - transform.position : transform.forward;
        chargeDirection.y = 0f;
        chargeDirection = chargeDirection.sqrMagnitude > .0001f
            ? chargeDirection.normalized : transform.forward;
        transform.rotation = Quaternion.LookRotation(chargeDirection);
        previousHitboxCenter = ChargeHitboxCenter(attack);
    }

    protected override bool FaceTargetDuringActive(EnemyAttackData attack) =>
        !IsCharge(attack) && base.FaceTargetDuringActive(attack);

    protected override Vector3 GetActiveMovementVelocity(EnemyAttackData attack) =>
        IsCharge(attack) ? chargeDirection * chargeSpeed
            : base.GetActiveMovementVelocity(attack);

    protected override bool ExecuteAttack(EnemyAttackData attack)
    {
        if (!IsCharge(attack))
            return base.ExecuteAttack(attack);

        Vector3 currentCenter = ChargeHitboxCenter(attack);
        int hitCount = Physics.OverlapCapsuleNonAlloc(previousHitboxCenter,
            currentCenter, attack.hitboxRadius, chargeHits);
        previousHitboxCenter = currentCenter;

        for (int i = 0; i < hitCount; i++)
        {
            PlayerHealth player = chargeHits[i].GetComponentInParent<PlayerHealth>();
            if (player == null)
                continue;

            player.TakeDamage(attack.damage, AttackOrigin,
                attack.knockbackForce, attack.knockbackDuration, true, transform);
            return true;
        }

        return false;
    }

    private Vector3 ChargeHitboxCenter(EnemyAttackData attack) =>
        AttackOrigin + chargeDirection * attack.hitboxDistance;

    private static bool IsCharge(EnemyAttackData attack) =>
        attack != null && attack.attackId == ChargeAttackId;
}

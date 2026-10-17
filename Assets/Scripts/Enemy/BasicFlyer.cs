using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inimigo aereo basico. Mantem uma altura sobre o player com bobbing suave
/// e executa um sweep descendente cuja hitbox permanece ativa no Active todo.
/// </summary>
public class BasicFlyer : BaseEnemy
{
    [Header("Flight")]
    [Tooltip("Altura que o inimigo busca manter acima do alvo enquanto nao esta em sweep.")]
    [Min(0f)] [SerializeField] private float targetFlightHeight = 2.5f;
    [Tooltip("Velocidade maxima usada para corrigir a altura de voo.")]
    [Min(0f)] [SerializeField] private float heightCorrectionSpeed = 5f;
    [Tooltip("Intensidade do bobbing vertical ao redor da altura alvo.")]
    [Min(0f)] [SerializeField] private float bobbingAmplitude = .15f;
    [Tooltip("Ciclos por segundo do bobbing enquanto o inimigo voa.")]
    [Min(0f)] [SerializeField] private float bobbingFrequency = 1.2f;

    [Header("Sweep")]
    [Tooltip("Velocidade vertical para baixo durante a fase Active do sweep.")]
    [Min(0f)] [SerializeField] private float sweepDescentSpeed = 5.5f;

    [Header("Basic Flyer Attack")]
    [SerializeField] private BaseEnemyAttack[] attackCombo =
    {
        new BaseEnemyAttack
        {
            attackName = "Flyer Sweep",
            damage = 14,
            maximumRange = 1.5f,
            windupDuration = .45f,
            activeDuration = .55f,
            recoveryDuration = .9f,
            hitboxDistance = .65f,
            hitboxRadius = .85f,
            knockbackForce = 4f,
            knockbackDuration = .18f
        }
    };

    private readonly HashSet<PlayerHealth> sweepHitPlayers = new();
    private float idleFlightHeight;

    protected override BaseEnemyAttack[] AttackCombo => attackCombo;

    private void Start()
    {
        idleFlightHeight = transform.position.y;
    }

    protected override void UpdateVerticalMovement()
    {
        if (IsLaunched)
        {
            base.UpdateVerticalMovement();
            return;
        }

        if (CurrentState == State.Active)
        {
            SetVerticalVelocity(-sweepDescentSpeed);
            return;
        }

        float targetBaseHeight = Target != null
            ? Target.position.y + targetFlightHeight
            : idleFlightHeight;
        float bobbing = Mathf.Sin(Time.time * bobbingFrequency * Mathf.PI * 2f) *
            bobbingAmplitude;
        float targetHeight = targetBaseHeight + bobbing;
        float heightDifference = targetHeight - transform.position.y;
        SetVerticalVelocity(Mathf.Clamp(
            heightDifference * heightCorrectionSpeed,
            -heightCorrectionSpeed,
            heightCorrectionSpeed
        ));
    }

    protected override void OnAttackActiveStarted(BaseEnemyAttack attack)
    {
        sweepHitPlayers.Clear();
    }

    protected override bool ExecuteAttack(BaseEnemyAttack attack)
    {
        Vector3 hitPosition = transform.position +
            transform.forward * attack.hitboxDistance;

        foreach (Collider hit in Physics.OverlapSphere(hitPosition, attack.hitboxRadius))
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null || !sweepHitPlayers.Add(playerHealth))
                continue;

            playerHealth.TakeDamage(
                attack.damage,
                transform.position,
                attack.knockbackForce,
                attack.knockbackDuration,
                true,
                transform
            );
        }

        // Retorna false para BaseEnemy consultar a hitbox durante todo Active.
        return false;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inimigo aereo basico. Mantem altura sobre o chao e oferece Sweep e Air Spin.
/// </summary>
public class BasicFlyer : EnemyBase
{
    private const string SweepAttackId = "FlyerSweep";
    private const string AirSpinAttackId = "FlyerAirSpin";

    [Header("Flight")]
    [Tooltip("Altura que o inimigo busca manter acima do chao enquanto nao esta em sweep.")]
    [Min(0f)] [SerializeField] private float groundFlightHeight = 2.5f;
    [Tooltip("Camadas que o raycast de voo considera como chao. Player e inimigos sao ignorados mesmo se estiverem nessas camadas.")]
    [SerializeField] private LayerMask flightGroundLayers = ~0;
    [Tooltip("Distancia maxima abaixo do Flyer usada para encontrar o chao de referencia.")]
    [Min(.01f)] [SerializeField] private float groundProbeDistance = 20f;
    [Tooltip("Altura adicional da origem do raycast acima do Flyer para encontrar chao em descidas e plataformas.")]
    [Min(0f)] [SerializeField] private float groundProbeStartOffset = 2f;
    [Tooltip("Velocidade maxima usada para corrigir a altura de voo.")]
    [Min(0f)] [SerializeField] private float heightCorrectionSpeed = 5f;
    [Tooltip("Intensidade do bobbing vertical ao redor da altura alvo.")]
    [Min(0f)] [SerializeField] private float bobbingAmplitude = .15f;
    [Tooltip("Ciclos por segundo do bobbing enquanto o inimigo voa.")]
    [Min(0f)] [SerializeField] private float bobbingFrequency = 1.2f;

    [Header("Sweep")]
    [Tooltip("Velocidade vertical para baixo durante a fase Active do sweep.")]
    [Min(0f)] [SerializeField] private float sweepDescentSpeed = 5.5f;

    [Header("Air Spin")]
    [Tooltip("Objeto visual que gira durante Air Spin. O Collider e a IA permanecem no objeto raiz.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Numero de voltas completas do visual durante a janela Active de Air Spin.")]
    [Min(0f)] [SerializeField] private float airSpinVisualRotations = 2f;

    protected override EnemyAttackData[] CreateDefaultAttacks() => new[]
    {
        new EnemyAttackData
        {
            attackName = "Flyer Sweep",
            attackId = SweepAttackId,
            damage = 14,
            maximumRange = 1.5f,
            windupDuration = .45f,
            activeDuration = .55f,
            recoveryDuration = .9f,
            hitboxDistance = .65f,
            hitboxRadius = .85f,
            knockbackForce = 4f,
            knockbackDuration = .18f
        },
        new EnemyAttackData
        {
            attackName = "Flyer Air Spin",
            attackId = AirSpinAttackId,
            damage = 16,
            maximumRange = 2.4f,
            windupDuration = .35f,
            activeDuration = .65f,
            recoveryDuration = 1f,
            hitboxDistance = 0f,
            hitboxRadius = 2.4f,
            knockbackForce = 5f,
            knockbackDuration = .2f
        }
    };

    private readonly HashSet<PlayerHealth> sweepHitPlayers = new();
    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private float fallbackFlightHeight;
    private bool isAirSpinActive;
    private float airSpinElapsed;
    private Quaternion airSpinBaseLocalRotation;

    private void Start()
    {
        fallbackFlightHeight = transform.position.y;
        ResolveVisualRoot();
    }

    private void LateUpdate()
    {
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;

        if (isAirSpinActive && CurrentState != State.Active)
            StopAirSpinVisual();
    }

    protected override void UpdateVerticalMovement(EnemyMovement movement)
    {
        if (IsLaunched)
        {
            base.UpdateVerticalMovement(movement);
            return;
        }

        if (CurrentState == State.Active && !IsAirSpinAttack(CurrentAttack))
        {
            movement.SetVerticalVelocity(-sweepDescentSpeed);
            return;
        }

        float targetBaseHeight = TryGetGroundHeight(out float groundHeight)
            ? groundHeight + groundFlightHeight
            : fallbackFlightHeight;
        float bobbing = Mathf.Sin(Time.time * bobbingFrequency * Mathf.PI * 2f) *
            bobbingAmplitude;
        float targetHeight = targetBaseHeight + bobbing;
        float heightDifference = targetHeight - transform.position.y;
        movement.SetVerticalVelocity(Mathf.Clamp(
            heightDifference * heightCorrectionSpeed,
            -heightCorrectionSpeed,
            heightCorrectionSpeed
        ));
    }

    private bool TryGetGroundHeight(out float groundHeight)
    {
        Vector3 rayOrigin = transform.position +
            Vector3.up * groundProbeStartOffset;
        int hitCount = Physics.RaycastNonAlloc(
            rayOrigin,
            Vector3.down,
            groundHits,
            groundProbeDistance + groundProbeStartOffset,
            flightGroundLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.MaxValue;
        groundHeight = 0f;
        bool foundGround = false;

        for (int index = 0; index < hitCount; index++)
        {
            Collider hitCollider = groundHits[index].collider;
            if (hitCollider == null ||
                hitCollider.GetComponentInParent<EnemyBase>() != null ||
                hitCollider.GetComponentInParent<PlayerHealth>() != null)
            {
                continue;
            }

            if (groundHits[index].distance >= closestDistance)
                continue;

            closestDistance = groundHits[index].distance;
            groundHeight = groundHits[index].point.y;
            foundGround = true;
        }

        return foundGround;
    }

    protected override void OnAttackActiveStarted(EnemyAttackData attack)
    {
        sweepHitPlayers.Clear();

        isAirSpinActive = IsAirSpinAttack(attack);
        airSpinElapsed = 0f;
        if (isAirSpinActive && visualRoot != null)
            airSpinBaseLocalRotation = visualRoot.localRotation;
    }

    protected override bool ExecuteAttack(EnemyAttackData attack)
    {
        if (IsAirSpinAttack(attack))
            UpdateAirSpinVisual(attack.activeDuration);

        Vector3 hitPosition = IsAirSpinAttack(attack)
            ? transform.position
            : transform.position + transform.forward * attack.hitboxDistance;

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

        // Retorna false para consultar a hitbox durante todo Active.
        return false;
    }

    protected override bool CanUseAttack(EnemyAttackData attack, int attackIndex)
    {
        // Air Spin e a resposta especifica do Flyer ao jogador no ar; o
        // Sweep ocupa o papel equivalente contra o jogador no chao.
        return attack.attackId == AirSpinAttackId
            ? IsTargetAirborne()
            : attack.attackId != SweepAttackId || !IsTargetAirborne();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible ||
            CurrentState != State.Active ||
            !IsAirSpinAttack(CurrentAttack))
        {
            return;
        }

        Gizmos.color = new Color(1f, .35f, .1f, .9f);
        Gizmos.DrawWireSphere(transform.position, CurrentAttack.hitboxRadius);
    }

    private bool IsTargetAirborne()
    {
        PlayerMovement targetMovement = Target != null
            ? Target.GetComponent<PlayerMovement>()
            : null;
        return targetMovement != null && !targetMovement.IsGrounded;
    }

    private bool IsAirSpinAttack(EnemyAttackData attack)
    {
        return attack != null && attack.attackId == AirSpinAttackId;
    }

    private void UpdateAirSpinVisual(float activeDuration)
    {
        if (visualRoot == null)
            return;

        airSpinElapsed += Time.deltaTime;
        float progress = activeDuration <= 0f
            ? 1f
            : Mathf.Clamp01(airSpinElapsed / activeDuration);
        float degrees = 360f * airSpinVisualRotations * progress;
        visualRoot.localRotation = airSpinBaseLocalRotation *
            Quaternion.Euler(0f, degrees, 0f);
    }

    private void StopAirSpinVisual()
    {
        if (visualRoot != null)
            visualRoot.localRotation = airSpinBaseLocalRotation;

        isAirSpinActive = false;
    }

    private void ResolveVisualRoot()
    {
        if (visualRoot != null)
            return;

        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.transform != transform)
        {
            visualRoot = animator.transform;
            return;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.transform == transform)
                continue;
            Transform childRoot = renderer.transform;
            while (childRoot.parent != null && childRoot.parent != transform)
                childRoot = childRoot.parent;
            visualRoot = childRoot;
            return;
        }
    }
}

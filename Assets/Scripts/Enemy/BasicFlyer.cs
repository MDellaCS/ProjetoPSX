using UnityEngine;

/// <summary>
/// Inimigo aereo basico. Mantem altura sobre o chao e oferece Sweep e Air Spin.
/// </summary>
public class BasicFlyer : EnemyBase
{
    private const string SweepTag = "Sweep";
    private const string AirSpinTag = "AirSpin";

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
    [Tooltip("Numero de voltas completas do visual durante Windup + Active de Air Spin.")]
    [Min(0f)] [SerializeField] private float airSpinVisualRotations = 2f;

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

        if (isAirSpinActive &&
            (CurrentState == State.Windup || CurrentState == State.Active))
        {
            UpdateAirSpinVisual(CurrentAttack);
        }
        else if (isAirSpinActive)
        {
            StopAirSpinVisual();
        }
    }

    protected override void UpdateVerticalMovement(EnemyMovement movement)
    {
        if (IsLaunched)
        {
            base.UpdateVerticalMovement(movement);
            return;
        }

        if (CurrentState == State.Active &&
            CurrentAttack != null && CurrentAttack.HasTag(SweepTag))
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

    protected override void OnAttackStarted(EnemyAttackDefinition attack)
    {
        if (!IsAirSpinAttack(attack))
            return;

        isAirSpinActive = true;
        airSpinElapsed = 0f;
        if (visualRoot != null)
            airSpinBaseLocalRotation = visualRoot.localRotation;
    }

    private bool IsAirSpinAttack(EnemyAttackDefinition attack)
    {
        return attack != null && attack.HasTag(AirSpinTag);
    }

    private void UpdateAirSpinVisual(EnemyAttackDefinition attack)
    {
        if (visualRoot == null || attack == null)
            return;

        airSpinElapsed += Time.deltaTime;
        float spinDuration = attack.windupDuration + attack.activeDuration;
        float progress = spinDuration <= 0f
            ? 1f
            : Mathf.Clamp01(airSpinElapsed / spinDuration);
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

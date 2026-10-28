using UnityEngine;

/// <summary>
/// Puppet voador. Mantem altura sobre o chao e executa ataques aereos.
/// </summary>
[RequireComponent(typeof(EnemyBobbing))]
public class FlyingPuppet : EnemyBase
{
    private const string SweepTag = "Sweep";
    private const string AirSpinTag = "AirSpin";
    private const string TeleportDiveTag = "TeleportDive";

    private enum TeleportDivePhase { None, Rising, HiddenWindup, Descending }

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
    [Header("Sweep")]
    [Tooltip("Velocidade vertical para baixo durante a fase Active do sweep.")]
    [Min(0f)] [SerializeField] private float sweepDescentSpeed = 5.5f;

    [Header("Air Spin")]
    [Tooltip("Objeto visual que gira durante Air Spin. O Collider e a IA permanecem no objeto raiz.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Numero de voltas completas do visual durante Windup + Active de Air Spin.")]
    [Min(0f)] [SerializeField] private float airSpinVisualRotations = 2f;

    [Header("Teleport Dive")]
    [Tooltip("Intervalo minimo entre tentativas do ataque de desaparecer e cair sobre o jogador.")]
    [Min(0f)] [SerializeField] private float teleportDiveCooldown = 8f;
    [Tooltip("Chance de escolher o ataque de mergulho quando ele estiver disponivel.")]
    [Range(0f, 100f)] [SerializeField] private float teleportDiveDecisionChance = 35f;
    [Tooltip("Duracao da subida antes do Flyer desaparecer e travar a posicao do alvo.")]
    [Min(0f)] [SerializeField] private float teleportDiveRiseDuration = .45f;
    [Tooltip("Velocidade vertical usada para subir antes do desaparecimento.")]
    [Min(0f)] [SerializeField] private float teleportDiveRiseSpeed = 9f;
    [Tooltip("Altura acima do ponto marcado para onde o Flyer aparece durante o aviso.")]
    [Min(.1f)] [SerializeField] private float teleportDiveHeight = 6f;
    [Tooltip("Intervalo aleatorio do tempo de aviso enquanto o Flyer esta invisivel acima do alvo.")]
    [SerializeField] private EnemyFloatRange teleportDiveWindup = new() { min = 1f, max = 1.5f };
    [Tooltip("Velocidade vertical da queda durante o ataque ativo. O hitbox do asset acompanha a descida.")]
    [Min(.1f)] [SerializeField] private float teleportDiveDescentSpeed = 14f;
    [Tooltip("Tamanho do cubo vermelho de aviso desenhado sobre o ponto de impacto quando F3 esta ativo.")]
    [SerializeField] private Vector3 teleportDiveWarningSize = new(2.5f, .25f, 2.5f);

    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private float fallbackFlightHeight;
    private bool isAirSpinActive;
    private float airSpinElapsed;
    private Quaternion airSpinBaseLocalRotation;
    private EnemyBobbing bobbing;
    private TeleportDivePhase teleportDivePhase;
    private float teleportDivePhaseTimer;
    private float teleportDiveCooldownTimer;
    private float preparedTeleportDiveWindup;
    private Vector3 teleportDiveLandingPoint;
    private Renderer[] teleportDiveRenderers;
    private bool[] teleportDiveRendererStates;
    private Collider[] teleportDiveColliders;
    private bool[] teleportDiveColliderStates;

    private void Start()
    {
        fallbackFlightHeight = transform.position.y;
        bobbing = GetComponent<EnemyBobbing>();
        if (bobbing == null)
            bobbing = gameObject.AddComponent<EnemyBobbing>();
        ResolveVisualRoot();
    }

    private void OnValidate()
    {
        teleportDiveWindup.min = Mathf.Max(0f, teleportDiveWindup.min);
        teleportDiveWindup.max = Mathf.Max(teleportDiveWindup.min,
            teleportDiveWindup.max);
        teleportDiveWarningSize = new Vector3(
            Mathf.Max(.05f, teleportDiveWarningSize.x),
            Mathf.Max(.05f, teleportDiveWarningSize.y),
            Mathf.Max(.05f, teleportDiveWarningSize.z));
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

        if (teleportDivePhase != TeleportDivePhase.None &&
            (CurrentAttack == null || !CurrentAttack.HasTag(TeleportDiveTag)))
            RestoreTeleportDivePresentation();
    }

    protected override void TickEnemySpecificSystems()
    {
        if (teleportDivePhase != TeleportDivePhase.None &&
            (CurrentAttack == null || !CurrentAttack.HasTag(TeleportDiveTag)))
        {
            RestoreTeleportDivePresentation();
            return;
        }

        if (teleportDiveCooldownTimer > 0f && !GamePauseController.IsPaused &&
            !CombatImpactSystem.IsGameplayFrozen(this))
            teleportDiveCooldownTimer = Mathf.Max(0f,
                teleportDiveCooldownTimer - Time.deltaTime);

        if (teleportDivePhase == TeleportDivePhase.Rising)
        {
            teleportDivePhaseTimer -= Time.deltaTime;
            if (teleportDivePhaseTimer <= 0f)
                BeginHiddenTeleportDive();
        }
        else if (teleportDivePhase == TeleportDivePhase.HiddenWindup)
        {
            teleportDivePhaseTimer -= Time.deltaTime;
        }
    }

    protected override bool TryChooseSpecialCombatAction()
    {
        if (teleportDiveCooldownTimer > 0f || Brain == null ||
            Brain.Target == null || AttackController == null ||
            !AttackController.HasAvailableAttackWithTag(TeleportDiveTag) ||
            teleportDiveDecisionChance <= 0f ||
            (teleportDiveDecisionChance < 100f &&
                Random.value * 100f >= teleportDiveDecisionChance))
            return false;

        preparedTeleportDiveWindup = Mathf.Max(0f,
            teleportDiveWindup.RandomValue());
        float totalWindup = teleportDiveRiseDuration +
            preparedTeleportDiveWindup;
        if (!AttackController.TryBeginAttackWithTag(TeleportDiveTag,
            totalWindup))
            return false;

        teleportDiveCooldownTimer = teleportDiveCooldown;
        return true;
    }

    protected override void UpdateVerticalMovement(EnemyMovement movement)
    {
        if (teleportDivePhase == TeleportDivePhase.Descending &&
            CurrentState == State.Recovery)
            teleportDivePhase = TeleportDivePhase.None;

        if (teleportDivePhase == TeleportDivePhase.Rising)
        {
            movement.SetVerticalVelocity(teleportDiveRiseSpeed);
            return;
        }
        if (teleportDivePhase == TeleportDivePhase.HiddenWindup)
        {
            movement.SetVerticalVelocity(0f);
            return;
        }
        if (teleportDivePhase == TeleportDivePhase.Descending)
        {
            movement.SetVerticalVelocity(-teleportDiveDescentSpeed);
            return;
        }

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
        bobbing?.Tick(movement.LastHorizontalSpeed, Time.deltaTime);
        float targetHeight = targetBaseHeight +
            (bobbing != null ? bobbing.CurrentHeightOffset : 0f);
        float heightDifference = targetHeight - transform.position.y;
        movement.SetVerticalVelocity(Mathf.Clamp(
            heightDifference * heightCorrectionSpeed,
            -heightCorrectionSpeed,
            heightCorrectionSpeed
        ) + (bobbing != null ? bobbing.CurrentVerticalSpeedBoost : 0f));
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
        if (attack != null && attack.HasTag(TeleportDiveTag))
        {
            teleportDivePhase = TeleportDivePhase.Rising;
            teleportDivePhaseTimer = teleportDiveRiseDuration;
            return;
        }

        if (!IsAirSpinAttack(attack))
            return;

        isAirSpinActive = true;
        airSpinElapsed = 0f;
        if (visualRoot != null)
            airSpinBaseLocalRotation = visualRoot.localRotation;
    }

    protected override void OnAttackActiveStarted(EnemyAttackDefinition attack)
    {
        if (attack == null || !attack.HasTag(TeleportDiveTag))
            return;

        RestoreTeleportDivePresentation();
        teleportDivePhase = TeleportDivePhase.Descending;
        Movement?.SetVerticalVelocity(-teleportDiveDescentSpeed);
    }

    private void BeginHiddenTeleportDive()
    {
        if (Brain == null || Brain.Target == null)
        {
            AttackController?.CancelAttack();
            return;
        }

        Vector3 targetPosition = Brain.Target.position;
        teleportDiveLandingPoint = targetPosition;
        if (TryGetGroundHeightAt(targetPosition, out float groundHeight))
            teleportDiveLandingPoint.y = groundHeight;

        CacheTeleportDivePresentation();
        SetTeleportDivePresentation(false);

        Vector3 spawnPosition = teleportDiveLandingPoint +
            Vector3.up * teleportDiveHeight;
        CharacterController characterController = GetComponent<CharacterController>();
        bool controllerWasEnabled = characterController != null &&
            characterController.enabled;
        if (characterController != null)
            characterController.enabled = false;
        transform.position = spawnPosition;
        if (characterController != null)
            characterController.enabled = controllerWasEnabled;

        teleportDivePhase = TeleportDivePhase.HiddenWindup;
        teleportDivePhaseTimer = preparedTeleportDiveWindup;
        SetTemporarilyUntargetable(true);
        if (Movement != null)
            Movement.SetVerticalVelocity(0f);
    }

    private bool TryGetGroundHeightAt(Vector3 position, out float groundHeight)
    {
        Vector3 origin = position + Vector3.up * groundProbeStartOffset;
        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,
            groundProbeDistance + groundProbeStartOffset, flightGroundLayers,
            QueryTriggerInteraction.Ignore);
        float closestDistance = float.MaxValue;
        groundHeight = position.y;
        bool foundGround = false;
        for (int index = 0; index < hitCount; index++)
        {
            Collider hitCollider = groundHits[index].collider;
            if (hitCollider == null ||
                hitCollider.GetComponentInParent<EnemyBase>() != null ||
                hitCollider.GetComponentInParent<PlayerHealth>() != null)
                continue;
            if (groundHits[index].distance >= closestDistance)
                continue;
            closestDistance = groundHits[index].distance;
            groundHeight = groundHits[index].point.y;
            foundGround = true;
        }
        return foundGround;
    }

    private void CacheTeleportDivePresentation()
    {
        if (teleportDiveRenderers == null)
            teleportDiveRenderers = GetComponentsInChildren<Renderer>(true);
        if (teleportDiveRendererStates == null ||
            teleportDiveRendererStates.Length != teleportDiveRenderers.Length)
            teleportDiveRendererStates = new bool[teleportDiveRenderers.Length];
        for (int i = 0; i < teleportDiveRenderers.Length; i++)
            teleportDiveRendererStates[i] = teleportDiveRenderers[i] != null &&
                teleportDiveRenderers[i].enabled;

        if (teleportDiveColliders == null)
            teleportDiveColliders = GetComponentsInChildren<Collider>(true);
        if (teleportDiveColliderStates == null ||
            teleportDiveColliderStates.Length != teleportDiveColliders.Length)
            teleportDiveColliderStates = new bool[teleportDiveColliders.Length];
        for (int i = 0; i < teleportDiveColliders.Length; i++)
            teleportDiveColliderStates[i] = teleportDiveColliders[i] != null &&
                teleportDiveColliders[i].enabled;
    }

    private void SetTeleportDivePresentation(bool visible)
    {
        if (teleportDiveRenderers != null)
            for (int i = 0; i < teleportDiveRenderers.Length; i++)
                if (teleportDiveRenderers[i] != null)
                    teleportDiveRenderers[i].enabled = visible &&
                        teleportDiveRendererStates[i];
        if (teleportDiveColliders != null)
            for (int i = 0; i < teleportDiveColliders.Length; i++)
                if (teleportDiveColliders[i] != null)
                    teleportDiveColliders[i].enabled = visible &&
                        teleportDiveColliderStates[i];
    }

    private void RestoreTeleportDivePresentation()
    {
        if (teleportDiveRenderers != null)
            for (int i = 0; i < teleportDiveRenderers.Length; i++)
                if (teleportDiveRenderers[i] != null)
                    teleportDiveRenderers[i].enabled = teleportDiveRendererStates[i];
        if (teleportDiveColliders != null)
            for (int i = 0; i < teleportDiveColliders.Length; i++)
                if (teleportDiveColliders[i] != null)
                    teleportDiveColliders[i].enabled = teleportDiveColliderStates[i];
        SetTemporarilyUntargetable(false);
        teleportDivePhase = TeleportDivePhase.None;
    }

    private void OnDisable() => RestoreTeleportDivePresentation();

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying ||
            !PlayerDebugToolkitHUD.IsVisible ||
            teleportDivePhase != TeleportDivePhase.HiddenWindup)
            return;

        Vector3 size = new(
            Mathf.Max(.05f, teleportDiveWarningSize.x),
            Mathf.Max(.05f, teleportDiveWarningSize.y),
            Mathf.Max(.05f, teleportDiveWarningSize.z));
        Gizmos.color = new Color(1f, .05f, .025f, .28f);
        Gizmos.DrawCube(teleportDiveLandingPoint + Vector3.up * (size.y * .5f), size);
        Gizmos.color = new Color(1f, .08f, .04f, .95f);
        Gizmos.DrawWireCube(teleportDiveLandingPoint + Vector3.up * (size.y * .5f), size);
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

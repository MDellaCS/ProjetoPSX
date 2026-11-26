using UnityEngine;

/// <summary>Taticas e posicionamento de grupo do inimigo.</summary>
[DisallowMultipleComponent]
public sealed class EnemyDecisions : MonoBehaviour
{
    private enum RepositionMode
    {
        None,
        Retreat,
        StrafeLeft,
        StrafeRight,
        Orbit
    }

    private enum OrbitDirectionMode
    {
        Random,
        Clockwise,
        CounterClockwise
    }

    private static int nextGroupSeparationOrder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticCaches()
    {
        nextGroupSeparationOrder = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RebuildGroupSeparationOrder()
    {
        foreach (EnemyDecisions brain in Object.FindObjectsByType<EnemyDecisions>(
            FindObjectsInactive.Exclude))
            brain.groupSeparationOrder = ++nextGroupSeparationOrder;
    }

    [Header("Decisions")]
    [Tooltip("Tempo entre decisoes de perseguir, atacar ou se reposicionar.")]
    [SerializeField] private EnemyFloatRange decisionInterval =
        new() { min = .3f, max = .7f };
    [Tooltip("Duracao de um reposicionamento escolhido.")]
    [SerializeField] private EnemyFloatRange repositionDuration =
        new() { min = .3f, max = .8f };
    [Tooltip("Tempo em que o inimigo permanece comprometido a perseguir ou atacar.")]
    [Min(0f)] [SerializeField] private float chaseCommitmentDuration = 5f;
    [Tooltip("Espera apos um compromisso de perseguir ou atacar terminar.")]
    [Min(0f)] [SerializeField] private float chaseCommitmentCooldown = 2f;
    [Header("Combat Flow")]
    [Tooltip("Permite que este inimigo pare brevemente entre decisoes livres de combate.")]
    [SerializeField] private bool canChooseTacticalIdle = true;
    [Tooltip("Chance percentual sorteada para esta instancia de escolher uma pausa tatica quando nao ha compromisso ativo.")]
    [SerializeField] private EnemyFloatRange tacticalIdleChancePercent =
        new() { min = 10f, max = 25f };
    [Tooltip("Duracao sorteada para cada pausa tatica. O inimigo para, mas continua olhando para o jogador.")]
    [SerializeField] private EnemyFloatRange tacticalIdleDuration =
        new() { min = .5f, max = 1.25f };

    [Header("Combat Group Positioning")]
    [Tooltip("Ativa direcao de movimento que combina chase, orbit e separacao de aliados. Desative para usar apenas reposicionamento classico.")]
    [SerializeField] private bool useCohesiveGroupPositioning = true;
    [Tooltip("Raio em que outros inimigos comecam a influenciar o espacamento deste inimigo. Um valor e sorteado ao nascer.")]
    [SerializeField] private EnemyFloatRange groupSeparationRadius =
        new() { min = 2.25f, max = 3.25f };
    [Tooltip("Peso da forca que afasta este inimigo de aliados proximos. Um valor e sorteado ao nascer; valores maiores priorizam espaco pessoal.")]
    [SerializeField] private EnemyFloatRange groupSeparationStrength =
        new() { min = .65f, max = 1.15f };
    [Tooltip("Distancia preferida do jogador quando este inimigo nao esta perseguindo nem atacando. Um valor e sorteado ao nascer.")]
    [SerializeField] private EnemyFloatRange preferredOrbitDistance =
        new() { min = 2.1f, max = 3.2f };
    [Tooltip("Peso do movimento lateral ao redor do jogador. Um valor e sorteado ao nascer; valores maiores favorecem cercar em vez de avancar diretamente.")]
    [SerializeField] private EnemyFloatRange orbitStrength =
        new() { min = .35f, max = .8f };
    [Tooltip("Intervalo entre mudancas de sentido orbital. Um tempo e sorteado ao nascer e outro a cada mudanca.")]
    [SerializeField] private EnemyFloatRange orbitChangeInterval =
        new() { min = 3f, max = 6f };
    [Tooltip("Chance percentual minima, sorteada por inimigo, de preferir orbitar. A agressividade decide as demais escolhas entre perseguir e orbitar.")]
    [SerializeField] private EnemyFloatRange repositionChancePercent =
        new() { min = 25f, max = 50f };
    [Tooltip("Sentido orbital inicial. Random sorteia horario ou anti-horario para cada instancia.")]
    [SerializeField] private OrbitDirectionMode orbitDirectionMode =
        OrbitDirectionMode.Random;

    private EnemyAggression aggression;
    private EnemyBase owner;
    private EnemyMovement movement;
    private EnemyAttackController attacks;
    private EnemyGuard guard;
    private EnemyTaunt taunt;
    private Transform target;
    private PlayerHeat playerHeat;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private PlayerState playerState;
    internal bool HoldBossTarget { get; set; }
    internal bool HasLivingPlayerTarget => target != null && target.gameObject.activeInHierarchy &&
        playerState != null && !playerState.IsDead();
    private PlayerCharacterController playerCharacterController;
    private Transform cachedComponentsTarget;
    private float nextPlayerLookupTime;
    private float decisionTimer;
    private float repositionTimer;
    private float actionMovementSpeed;
    private RepositionMode repositionMode;
    private bool isChasing;
    private float commitmentTimer;
    private float commitmentCooldownTimer;
    private bool isEncounterEngaged;
    private bool isWaitingForEncounter;
    private float instanceGroupSeparationRadius;
    private float instanceGroupSeparationStrength;
    private float instancePreferredOrbitDistance;
    private float instanceOrbitStrength;
    private float instanceOrbitChangeInterval;
    private float instanceRepositionChancePercent;
    private float orbitDirection;
    private float orbitDirectionTimer;
    private int groupSeparationOrder;
    private float instanceTacticalIdleChancePercent;
    private float tacticalIdleTimer;
    private bool skipTacticalIdleOnNextDecision;

    public Transform Target => target;
    internal PlayerMovement TargetMovement => playerMovement;
    internal PlayerHeat PlayerHeat => playerHeat;
    internal PlayerHealth PlayerHealth => playerHealth;
    internal PlayerCharacterController TargetCharacterController =>
        playerCharacterController;
    public bool IsWaitingForEncounter => isWaitingForEncounter;
    public bool IsEncounterEngaged => isEncounterEngaged;
    public float CommitmentTimer => commitmentTimer;
    public float CommitmentCooldownTimer => commitmentCooldownTimer;
    public float CurrentAggressionPercent => aggression.CurrentAggressionPercent;
    public float EffectiveAggressionPercent => aggression.EffectiveAggressionPercent;
    internal float DirectorSelectionAggressionPercent => aggression.DirectorSelectionAggressionPercent;
    public float AggressionChangeTimer => aggression.AggressionChangeTimer;
    public float CurrentAggressionVelocity => aggression.GetCurrentAggressionVelocity();
    public bool IsPlayerBackTurned => aggression.IsPlayerBackTurnedToEnemy();
    public bool IsCameraVisible => aggression.IsInsideCameraVisibilityPadding();
    public float PlayerFarAggressionDistance => aggression.PlayerFarAggressionDistance;
    public float GroupSeparationRadius => instanceGroupSeparationRadius;
    public float GroupSeparationStrength => instanceGroupSeparationStrength;
    public float PreferredOrbitDistance => instancePreferredOrbitDistance;
    public float OrbitStrength => instanceOrbitStrength;
    public float OrbitChangeInterval => instanceOrbitChangeInterval;
    public float RepositionChancePercent => instanceRepositionChancePercent;
    public bool UsesCohesiveGroupPositioning => useCohesiveGroupPositioning;
    public float OrbitDirection => orbitDirection;
    public bool IsTacticalIdle => owner != null &&
        owner.CurrentState == EnemyBase.State.TacticalIdle;
    public float TacticalIdleRemaining => IsTacticalIdle
        ? Mathf.Max(0f, tacticalIdleTimer) : 0f;

    internal void Initialize(EnemyBase enemy, EnemyMovement locomotion,
        EnemyAttackController attackController, EnemyHealth enemyHealth,
        EnemyGuard enemyGuard)
    {
        owner = enemy;
        unchecked
        {
            groupSeparationOrder = ++nextGroupSeparationOrder;
        }
        movement = locomotion;
        attacks = attackController;
        guard = enemyGuard;
        taunt = GetComponent<EnemyTaunt>();
        ValidateBounds();
        aggression = GetComponent<EnemyAggression>();
        if (aggression == null) aggression = gameObject.AddComponent<EnemyAggression>();
        aggression.Initialize(enemy, this, enemyHealth);
        RandomizeCombatGroupProfile();
        RandomizeTacticalIdleProfile();
        ResolvePlayer();
    }

    private void OnValidate()
    {
        ValidateBounds();
    }

    private void ValidateBounds()
    {
        ValidateRange(ref groupSeparationRadius, 0f);
        ValidateRange(ref groupSeparationStrength, 0f);
        ValidateRange(ref preferredOrbitDistance, .1f);
        ValidateRange(ref orbitStrength, 0f);
        ValidateRange(ref orbitChangeInterval, .1f);
        ValidateRange(ref repositionChancePercent, 0f, 100f);
        ValidateRange(ref tacticalIdleChancePercent, 0f, 100f);
        ValidateRange(ref tacticalIdleDuration, .01f);
    }

    private static void ValidateRange(ref EnemyFloatRange range,
        float minimumValue, float maximumValue = float.MaxValue)
    {
        range.min = Mathf.Clamp(range.min, minimumValue, maximumValue);
        range.max = Mathf.Clamp(range.max, minimumValue, maximumValue);
        if (range.min > range.max)
            (range.min, range.max) = (range.max, range.min);
    }

    private void RandomizeCombatGroupProfile()
    {
        instanceGroupSeparationRadius = groupSeparationRadius.RandomValue();
        instanceGroupSeparationStrength = groupSeparationStrength.RandomValue();
        instancePreferredOrbitDistance = preferredOrbitDistance.RandomValue();
        instanceOrbitStrength = orbitStrength.RandomValue();
        instanceOrbitChangeInterval = orbitChangeInterval.RandomValue();
        instanceRepositionChancePercent = repositionChancePercent.RandomValue();
        orbitDirection = orbitDirectionMode switch
        {
            OrbitDirectionMode.Clockwise => 1f,
            OrbitDirectionMode.CounterClockwise => -1f,
            _ => Random.value < .5f ? -1f : 1f
        };
        orbitDirectionTimer = instanceOrbitChangeInterval;
    }

    private void RandomizeTacticalIdleProfile()
    {
        instanceTacticalIdleChancePercent =
            tacticalIdleChancePercent.RandomValue();
    }

    internal void ResolvePlayer()
    {
        if (owner.IsBoss && target != null && !target.gameObject.activeInHierarchy)
            target = null;
        if (target == null)
        {
            // Sem jogador na cena, evita uma busca por tag em todo frame.
            if (Time.unscaledTime < nextPlayerLookupTime)
                return;
            nextPlayerLookupTime = Time.unscaledTime + .5f;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        if (target != null && cachedComponentsTarget != target)
            CacheTargetComponents();
    }

    private void CacheTargetComponents()
    {
        cachedComponentsTarget = target;
        playerHeat = target.GetComponent<PlayerHeat>();
        playerMovement = target.GetComponent<PlayerMovement>();
        playerHealth = target.GetComponent<PlayerHealth>();
        playerState = target.GetComponent<PlayerState>();
        playerCharacterController = target.GetComponent<PlayerCharacterController>();
    }

    internal void TickIdle()
    {
        if (target != null && (!owner.IsBoss || HasLivingPlayerTarget))
        {
            owner.CurrentState = EnemyBase.State.Combat;
            decisionTimer = 0f;
        }
    }

    internal void TickCombat()
    {
        if (target == null)
            return;
        tacticalIdleTimer = 0f;
        TickOrbitDirection();
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
            ChooseCombatAction();
        if (owner.CurrentState != EnemyBase.State.Combat)
            return;

        if (repositionTimer > 0f)
        {
            repositionTimer -= Time.deltaTime;
            TickReposition();
        }
        else if (commitmentTimer > 0f || isChasing)
            TickChaseMovement();
        else
            movement.Move(Vector3.zero);
    }

    private void ChooseCombatAction()
    {
        bool skipIdle = skipTacticalIdleOnNextDecision;
        skipTacticalIdleOnNextDecision = false;
        decisionTimer = Mathf.Max(.01f, decisionInterval.RandomValue());
        if (owner.ChooseSpecialCombatAction())
            return;
        if (guard != null && guard.TryStartGuardFromDecision())
            return;
        if (taunt != null && taunt.TryStartFromDecision())
            return;
        bool hasOpening = attacks.HasAvailableOpening();
        float aggression = EffectiveAggressionPercent / 100f;
        if (commitmentTimer > 0f)
        {
            if (hasOpening)
            {
                if (!attacks.TryBeginSelectedOpening())
                    BeginSupportReposition();
            }
            else
                BeginChase();
            return;
        }

        if (canChooseTacticalIdle && !skipIdle &&
            Random.value * 100f < instanceTacticalIdleChancePercent)
        {
            BeginTacticalIdle();
            return;
        }

        if (hasOpening)
        {
            if (attacks.TryBeginSelectedOpening())
                TryStartCommitment();
            else
                BeginSupportReposition();
            return;
        }

        if (useCohesiveGroupPositioning)
        {
            float repositionChance = instanceRepositionChancePercent / 100f;
            float chaseChance = (1f - repositionChance) * aggression;
            if (DirectionToTarget().sqrMagnitude > .001f &&
                Random.value < chaseChance)
            {
                TryStartCommitment();
                BeginChase();
                return;
            }

            BeginReposition(RepositionMode.Orbit);
            return;
        }

        if (DirectionToTarget().sqrMagnitude > .001f &&
            Random.value <= aggression)
        {
            TryStartCommitment();
            BeginChase();
            return;
        }

        BeginReposition(Random.value < .35f
            ? RepositionMode.Retreat
            : Random.value < .5f
                ? RepositionMode.StrafeLeft
                : RepositionMode.StrafeRight);
    }

    private void BeginTacticalIdle()
    {
        isChasing = false;
        CancelReposition();
        tacticalIdleTimer = Mathf.Max(.01f,
            tacticalIdleDuration.RandomValue());
        skipTacticalIdleOnNextDecision = true;
        owner.CurrentState = EnemyBase.State.TacticalIdle;
    }

    internal void TickTacticalIdle()
    {
        if (owner == null || owner.CurrentState != EnemyBase.State.TacticalIdle)
            return;

        if (target == null || (owner.IsBoss && !HasLivingPlayerTarget))
        {
            tacticalIdleTimer = 0f;
            owner.CurrentState = EnemyBase.State.Combat;
            decisionTimer = 0f;
            return;
        }

        movement.FaceTarget();
        tacticalIdleTimer = Mathf.Max(0f, tacticalIdleTimer - Time.deltaTime);
        if (tacticalIdleTimer > 0f)
            return;

        owner.CurrentState = EnemyBase.State.Combat;
        decisionTimer = 0f;
    }

    private void BeginSupportReposition()
    {
        if (useCohesiveGroupPositioning)
        {
            BeginReposition(RepositionMode.Orbit);
            return;
        }

        BeginReposition(Random.value < .25f
            ? RepositionMode.Retreat
            : Random.value < .5f
                ? RepositionMode.StrafeLeft
                : RepositionMode.StrafeRight);
    }

    private void BeginReposition(RepositionMode mode)
    {
        isChasing = false;
        repositionMode = mode;
        actionMovementSpeed = movement.StandardSpeed;
        repositionTimer = Mathf.Max(0f, repositionDuration.RandomValue());
    }

    private void BeginChase()
    {
        isChasing = true;
        repositionMode = RepositionMode.None;
        actionMovementSpeed = movement.FastSpeed;
        repositionTimer = 0f;
    }

    /// <summary>
    /// Converte uma decisao tática em locomocao. EnemyMovement e a unica
    /// camada que traduz essas intencoes para Tank Controls ou movimento livre.
    /// </summary>
    private void TickReposition()
    {
        switch (repositionMode)
        {
            case RepositionMode.Orbit:
                TickCohesiveMovement(false);
                break;
            case RepositionMode.Retreat:
                movement.RetreatFromTarget(actionMovementSpeed);
                break;
            case RepositionMode.StrafeLeft:
                movement.StrafeTarget(-1f, actionMovementSpeed);
                break;
            case RepositionMode.StrafeRight:
                movement.StrafeTarget(1f, actionMovementSpeed);
                break;
            default:
                movement.Move(Vector3.zero);
                break;
        }
    }

    private void TickChaseMovement()
    {
        if (!useCohesiveGroupPositioning)
        {
            movement.MoveTowardTarget(actionMovementSpeed);
            return;
        }

        TickCohesiveMovement(true);
    }

    /// <summary>
    /// Combina aproximacao, movimento orbital e separacao de aliados numa
    /// unica direcao. Os valores usados aqui sao sorteados uma vez por
    /// instancia em Initialize, para manter o comportamento estavel durante
    /// cada encontro.
    /// </summary>
    private void TickCohesiveMovement(bool chase)
    {
        Vector3 towardPlayer = DirectionToTarget();
        if (towardPlayer.sqrMagnitude <= .001f)
        {
            movement.Move(Vector3.zero);
            return;
        }

        float distance = DistanceToTarget();
        float preferredDistance = GetPreferredOrbitDistance();
        Vector3 desiredDirection = Vector3.zero;

        if (chase)
        {
            desiredDirection += towardPlayer;
        }
        else if (distance < preferredDistance * .8f)
        {
            desiredDirection -= towardPlayer * Mathf.Clamp01(
                (preferredDistance * .8f - distance) /
                Mathf.Max(.1f, preferredDistance * .8f));
        }

        Vector3 radialFromPlayer = -towardPlayer;
        Vector3 tangent = new(radialFromPlayer.z, 0f, -radialFromPlayer.x);
        float orbitBlend = chase
            ? Mathf.Clamp01(distance / Mathf.Max(.1f, preferredDistance))
            : 1f;
        float leadOrbitScale = EnemyDirector.IsLeadEnemy(owner) ? .65f : 1f;
        desiredDirection += tangent * orbitDirection * instanceOrbitStrength *
            orbitBlend * leadOrbitScale;
        desiredDirection += GetGroupSeparationDirection() *
            instanceGroupSeparationStrength;

        if (desiredDirection.sqrMagnitude <= .001f)
        {
            movement.Move(Vector3.zero);
            return;
        }

        movement.MoveWithTargetFacing(desiredDirection.normalized,
            actionMovementSpeed);
    }

    private float GetPreferredOrbitDistance()
    {
        float leadScale = EnemyDirector.IsLeadEnemy(owner) ? .8f : 1f;
        return Mathf.Max(.1f, instancePreferredOrbitDistance * leadScale);
    }

    private Vector3 GetGroupSeparationDirection()
    {
        float radius = instanceGroupSeparationRadius;
        if (radius <= .001f)
            return Vector3.zero;

        Vector3 separation = Vector3.zero;
        foreach (EnemyBase other in EnemyBase.ActiveEnemies)
        {
            if (other == null || other == owner || other.IsDead ||
                !other.isActiveAndEnabled || other.IsSpawning ||
                other.Target != target)
            {
                continue;
            }

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance >= radius)
                continue;

            if (distance <= .001f)
            {
                EnemyDecisions otherBrain = other.GetComponent<EnemyDecisions>();
                away = otherBrain != null &&
                    groupSeparationOrder < otherBrain.groupSeparationOrder
                    ? Vector3.right : Vector3.left;
                distance = 0f;
            }

            float influence = 1f - distance / radius;
            separation += away.normalized * influence;
        }

        return separation.sqrMagnitude > 1f
            ? separation.normalized : separation;
    }

    private void TickOrbitDirection()
    {
        if (!useCohesiveGroupPositioning || orbitDirectionMode !=
            OrbitDirectionMode.Random)
        {
            return;
        }

        orbitDirectionTimer -= Time.deltaTime;
        if (orbitDirectionTimer > 0f)
            return;

        if (Random.value < .5f)
            orbitDirection *= -1f;
        instanceOrbitChangeInterval = Mathf.Max(.1f,
            orbitChangeInterval.RandomValue());
        orbitDirectionTimer = instanceOrbitChangeInterval;
    }

    private bool TryStartCommitment()
    {
        if (commitmentCooldownTimer > 0f || chaseCommitmentDuration <= 0f)
            return false;
        commitmentTimer = chaseCommitmentDuration;
        return true;
    }

    internal void TickCommitment()
    {
        commitmentCooldownTimer = Mathf.Max(0f,
            commitmentCooldownTimer - Time.deltaTime);
        if (commitmentTimer <= 0f)
            return;
        commitmentTimer = Mathf.Max(0f, commitmentTimer - Time.deltaTime);
        if (commitmentTimer <= 0f)
            commitmentCooldownTimer = Mathf.Max(commitmentCooldownTimer,
                chaseCommitmentCooldown);
    }

    internal void OnAttackStarted()
    {
        isChasing = false;
        repositionTimer = 0f;
        repositionMode = RepositionMode.None;
    }

    internal void ScheduleNextDecision() =>
        decisionTimer = Mathf.Max(.01f, decisionInterval.RandomValue());

    internal void DecideImmediately() => decisionTimer = 0f;

    internal void CancelReposition()
    {
        repositionTimer = 0f;
        repositionMode = RepositionMode.None;
    }

    internal void ResetForPracticeDummy()
    {
        isChasing = false;
        CancelReposition();
        tacticalIdleTimer = 0f;
        commitmentTimer = 0f;
        commitmentCooldownTimer = 0f;
        decisionTimer = 0f;
    }

    public void SetEncounterEngaged(Transform encounterPlayer)
    {
        if (encounterPlayer == null || owner.IsDead)
            return;
        target = encounterPlayer;
        if (cachedComponentsTarget != target)
            CacheTargetComponents();
        if (owner.IsBoss && !HasLivingPlayerTarget) return;
        isWaitingForEncounter = false;
        isEncounterEngaged = true;
        if (owner.CurrentState == EnemyBase.State.Idle)
            owner.CurrentState = EnemyBase.State.Combat;
    }

    public void SetWaitingForEncounter()
    {
        owner.CancelTaunt();
        isWaitingForEncounter = true;
        isEncounterEngaged = false;
        attacks.CancelAttack();
        owner.CurrentState = EnemyBase.State.Idle;
        isChasing = false;
        CancelReposition();
        tacticalIdleTimer = 0f;
        skipTacticalIdleOnNextDecision = false;
        commitmentTimer = 0f;
        commitmentCooldownTimer = 0f;
        aggression.ResetAggression();
    }

    public float DistanceToTarget()
    {
        if (target == null) return float.PositiveInfinity;
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private Vector3 DirectionToTarget()
    {
        if (target == null) return Vector3.zero;
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > .001f ? direction.normalized : Vector3.zero;
    }

    internal void TickAggression() => aggression.TickAggression();
    internal void ApplyDodgeAggressionChange(float change) => aggression.ApplyDodgeAggressionChange(change);
    internal void OnDamaged(int damage) => aggression.OnDamaged(damage);
    internal void ReactToPlayerDamagedByEnemy() => aggression.ReactToPlayerDamagedByEnemy();
}

using UnityEngine;

/// <summary>Deteccao, decisoes, agressividade e memoria de alvo.</summary>
[DisallowMultipleComponent]
public sealed class EnemyBrain : MonoBehaviour
{
    private enum RepositionMode
    {
        None,
        Retreat,
        StrafeLeft,
        StrafeRight
    }

    private static int cachedVisibilityFrame = -1;
    private static Camera cachedVisibilityCamera;
    private static Plane[] cachedVisibilityPlanes;

    [Header("Detection")]
    [Tooltip("Alcance para encontrar o jogador antes de entrar em combate.")]
    [Min(0f)] [SerializeField] private float detectionRange = 8f;
    [Tooltip("Alcance para manter contato depois de entrar em combate.")]
    [Min(0f)] [SerializeField] private float combatDetectionRange = 14f;
    [Tooltip("Tempo que busca a ultima posicao conhecida ao perder o jogador.")]
    [Min(0f)] [SerializeField] private float lastKnownSearchDuration = 2.5f;
    [Tooltip("Distancia suficiente para considerar que chegou a ultima posicao conhecida.")]
    [Min(0f)] [SerializeField] private float lastKnownArrivalDistance = .35f;

    [Header("Aggression")]
    [Tooltip("Agressividade inicial sorteada ao nascer. Valores iguais eliminam o sorteio.")]
    [SerializeField] private EnemyFloatRange initialAggressionPercent =
        new() { min = 20f, max = 30f };
    [Tooltip("Limite minimo da agressividade atual, em porcentagem.")]
    [Range(0f, 100f)] [SerializeField] private float minimumAggressionPercent = 10f;
    [Tooltip("Limite maximo da agressividade atual, em porcentagem.")]
    [Range(0f, 100f)] [SerializeField] private float maximumAggressionPercent = 100f;
    [Tooltip("Velocidade sorteada para subir ou descer a agressividade por segundo.")]
    [SerializeField] private EnemyFloatRange aggressionChangeRatePerSecond =
        new() { min = 6f, max = 10f };
    [Tooltip("Tempo antes de sortear uma nova direcao para a agressividade.")]
    [SerializeField] private EnemyFloatRange aggressionChangeInterval =
        new() { min = 1.5f, max = 3f };
    [Tooltip("Agressividade adicional por segundo quando o jogador esta de costas.")]
    [Min(0f)] [SerializeField] private float playerBackTurnedAggressionGain = 12f;
    [Tooltip("Variacao por segundo fora da camera. Valor negativo aumenta agressividade.")]
    [SerializeField] private float outOfCameraAggressionLoss = 16f;
    [Tooltip("Margem ao redor da camera antes de considerar o inimigo fora da visao.")]
    [Min(0f)] [SerializeField] private float cameraVisibilityPadding = 3f;
    [Tooltip("Distancia horizontal a partir da qual o jogador e considerado longe para este modificador. Use 0 para desativar.")]
    [Min(0f)] [SerializeField] private float playerFarAggressionDistance;
    [Tooltip("Variacao de agressividade por segundo enquanto o jogador estiver longe. Valor positivo incentiva perseguicao; negativo torna o inimigo mais passivo a distancia.")]
    [SerializeField] private float playerFarAggressionChangePerSecond;

    [Header("Aggression Combat Reactions")]
    [Tooltip("Variacao de agressividade para cada 1 por cento da vida maxima realmente perdida ao receber dano. Valor negativo reduz agressividade; positivo a aumenta. Ex.: -1 reduz 50 pontos ao perder 50 por cento da vida maxima.")]
    [SerializeField] private float damageTakenAggressionChangePerMaxHealthPercent = -1f;
    [Tooltip("Porcentagem de vida abaixo da qual o humor tende a diminuir agressividade.")]
    [Range(0f, 1f)] [SerializeField] private float lowEnemyHealthPercent = .35f;
    [Tooltip("Chance de reduzir agressividade na proxima mudanca de humor com pouca vida.")]
    [Range(0f, 1f)] [SerializeField] private float lowEnemyHealthDecreaseChance = .75f;
    [Tooltip("Abaixo desta porcentagem de vida do jogador, a agressividade sobe.")]
    [Range(0f, 1f)] [SerializeField] private float lowPlayerHealthPercent = .3f;
    [Tooltip("Agressividade ganha por segundo com o jogador em pouca vida.")]
    [Min(0f)] [SerializeField] private float lowPlayerHealthAggressionGainPerSecond = 14f;
    [Tooltip("Acima desta porcentagem de Heat do jogador, a agressividade cai.")]
    [Range(0f, 1f)] [SerializeField] private float highPlayerHealthPercent = .75f;
    [Tooltip("Agressividade perdida por segundo com o jogador em Heat alto.")]
    [Min(0f)] [SerializeField] private float highPlayerHealthAggressionLossPerSecond = 10f;
    [Tooltip("Variacao de agressividade por segundo enquanto o jogador esta no ar. Use valor positivo para o inimigo pressionar o combate aereo ou negativo para ele recuar.")]
    [SerializeField] private float playerAirborneAggressionChangePerSecond;
    [Tooltip("Reduz agressividade quando qualquer inimigo acerta o jogador. Tem prioridade sobre a reacao invertida.")]
    [SerializeField] private bool retreatsWhenPlayerTakesEnemyDamage = true;
    [Tooltip("Aumenta agressividade quando outro inimigo acerta o jogador, se a regra de recuar estiver desligada.")]
    [SerializeField] private bool gainsAggressionWhenPlayerTakesEnemyDamage;
    [Tooltip("Fracao da agressividade atual alterada quando um inimigo acerta o jogador.")]
    [Range(0f, 1f)] [SerializeField] private float playerDamagedAggressionPercent = .2f;
    [Tooltip("Pontos percentuais adicionais alterados quando um inimigo acerta o jogador.")]
    [Min(0f)] [SerializeField] private float playerDamagedAggressionFlatChange = 5f;

    [Header("Decisions")]
    [Tooltip("Tempo entre decisoes de perseguir, atacar ou se reposicionar.")]
    [SerializeField] private EnemyFloatRange decisionInterval =
        new() { min = .3f, max = .7f };
    [Tooltip("Duracao de um reposicionamento escolhido.")]
    [SerializeField] private EnemyFloatRange repositionDuration =
        new() { min = .3f, max = .8f };
    [Tooltip("Fator da velocidade de movimento usado ao recuar ou orbitar.")]
    [SerializeField] private EnemyFloatRange repositionSpeedMultiplier =
        new() { min = .25f, max = .55f };
    [Tooltip("Tempo em que o inimigo permanece comprometido a perseguir ou atacar.")]
    [Min(0f)] [SerializeField] private float chaseCommitmentDuration = 5f;
    [Tooltip("Espera apos um compromisso de perseguir ou atacar terminar.")]
    [Min(0f)] [SerializeField] private float chaseCommitmentCooldown = 2f;

    [Header("Enemy Director")]
    [Tooltip("Quantidade do orcamento de 100 pontos ocupada por este inimigo quando ele se torna Lead. Pesos menores permitem mais inimigos atacando ao mesmo tempo.")]
    [Range(1f, 100f)] [SerializeField] private float directorWeight = 30f;

    private EnemyBase owner;
    private EnemyMovement movement;
    private EnemyAttackController attacks;
    private EnemyHealth health;
    private EnemyGuard guard;
    private EnemyTaunt taunt;
    private Transform target;
    private PlayerHeat playerHeat;
    private PlayerMovement playerMovement;
    private float decisionTimer;
    private float repositionTimer;
    private float repositionSpeed;
    private RepositionMode repositionMode;
    private bool isChasing;
    private float commitmentTimer;
    private float commitmentCooldownTimer;
    private Vector3 lastKnownPlayerPosition;
    private float lastKnownSearchTimer;
    private bool hasLastKnownPlayerPosition;
    private bool isEncounterEngaged;
    private bool isWaitingForEncounter;
    private float initialAggression;
    private float aggressionChangeRate;
    private float aggressionChangeDirection;
    private float aggressionChangeTimer;
    private float currentAggressionPercent;

    public Transform Target => target;
    public bool IsWaitingForEncounter => isWaitingForEncounter;
    public bool IsEncounterEngaged => isEncounterEngaged;
    public float CurrentAggressionPercent => currentAggressionPercent;
    public float EffectiveAggressionPercent => Mathf.Clamp(
        currentAggressionPercent + EnemyDirector.GetAggressionModifier(owner),
        minimumAggressionPercent, maximumAggressionPercent);
    public float AggressionChangeTimer => aggressionChangeTimer;
    public float CommitmentTimer => commitmentTimer;
    public float CommitmentCooldownTimer => commitmentCooldownTimer;
    public float DetectionRange => detectionRange;
    public float CombatDetectionRange => combatDetectionRange;
    public bool IsSearchingLastKnownPosition =>
        !HasVisualContact() && lastKnownSearchTimer > 0f;
    public float CurrentAggressionVelocity => GetCurrentAggressionVelocity();
    public bool IsPlayerBackTurned => IsPlayerBackTurnedToEnemy();
    public bool IsCameraVisible => IsInsideCameraVisibilityPadding();
    public float PlayerFarAggressionDistance => playerFarAggressionDistance;
    public float DirectorWeight => Mathf.Clamp(directorWeight, 1f, 100f);

    internal void Initialize(EnemyBase enemy, EnemyMovement locomotion,
        EnemyAttackController attackController, EnemyHealth enemyHealth,
        EnemyGuard enemyGuard)
    {
        owner = enemy;
        movement = locomotion;
        attacks = attackController;
        health = enemyHealth;
        guard = enemyGuard;
        taunt = GetComponent<EnemyTaunt>();
        ValidateBounds();
        initialAggression = Mathf.Clamp(initialAggressionPercent.RandomValue(),
            minimumAggressionPercent, maximumAggressionPercent);
        currentAggressionPercent = initialAggression;
        SelectAggressionChange();
        ResolvePlayer();
        RememberPlayerPosition();
    }

    private void OnValidate()
    {
        combatDetectionRange = Mathf.Max(detectionRange, combatDetectionRange);
        ValidateBounds();
    }

    private void ValidateBounds()
    {
        minimumAggressionPercent = Mathf.Clamp(minimumAggressionPercent, 0f, 100f);
        maximumAggressionPercent = Mathf.Clamp(maximumAggressionPercent,
            minimumAggressionPercent, 100f);
        lowEnemyHealthPercent = Mathf.Clamp01(lowEnemyHealthPercent);
        lowEnemyHealthDecreaseChance = Mathf.Clamp01(lowEnemyHealthDecreaseChance);
        lowPlayerHealthPercent = Mathf.Clamp01(lowPlayerHealthPercent);
        highPlayerHealthPercent = Mathf.Clamp(highPlayerHealthPercent,
            lowPlayerHealthPercent, 1f);
        playerFarAggressionDistance = Mathf.Max(0f,
            playerFarAggressionDistance);
    }

    internal void ResolvePlayer()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        if (target != null && playerHeat == null)
            playerHeat = target.GetComponent<PlayerHeat>();
        if (target != null && playerMovement == null)
            playerMovement = target.GetComponent<PlayerMovement>();
    }

    internal void TickIdle()
    {
        if (HasVisualContact())
        {
            owner.CurrentState = EnemyBase.State.Combat;
            decisionTimer = 0f;
        }
    }

    internal void TickCombat()
    {
        if (target == null)
            return;
        if (!HasVisualContact())
        {
            TickLastKnownSearch();
            return;
        }

        RememberPlayerPosition();
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
            movement.MoveTowardTarget(repositionSpeed);
        else
            movement.Move(Vector3.zero);
    }

    private void ChooseCombatAction()
    {
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
                BeginChase(aggression);
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

        if (DirectionToTarget().sqrMagnitude > .001f && Random.value <= aggression)
        {
            TryStartCommitment();
            BeginChase(aggression);
            return;
        }
        BeginReposition(Random.value < .35f
            ? RepositionMode.Retreat
            : Random.value < .5f
                ? RepositionMode.StrafeLeft
                : RepositionMode.StrafeRight);
    }

    private void BeginSupportReposition()
    {
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
        repositionSpeed = movement.MovementSpeed *
            Mathf.Max(0f, repositionSpeedMultiplier.RandomValue());
        repositionTimer = Mathf.Max(0f, repositionDuration.RandomValue());
    }

    private void BeginChase(float aggression)
    {
        isChasing = true;
        repositionMode = RepositionMode.None;
        repositionSpeed = movement.MovementSpeed * Mathf.Lerp(.2f, 1f, aggression);
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
            case RepositionMode.Retreat:
                movement.RetreatFromTarget(repositionSpeed);
                break;
            case RepositionMode.StrafeLeft:
                movement.StrafeTarget(-1f, repositionSpeed);
                break;
            case RepositionMode.StrafeRight:
                movement.StrafeTarget(1f, repositionSpeed);
                break;
            default:
                movement.Move(Vector3.zero);
                break;
        }
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

    private void TickLastKnownSearch()
    {
        if (!hasLastKnownPlayerPosition || lastKnownSearchTimer <= 0f)
        {
            owner.CurrentState = EnemyBase.State.Idle;
            ResetAggression();
            return;
        }
        commitmentTimer = 0f;
        isChasing = false;
        lastKnownSearchTimer -= Time.deltaTime;
        Vector3 direction = lastKnownPlayerPosition - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance > lastKnownArrivalDistance)
        {
            direction /= distance;
            movement.MoveTowardDirection(direction, movement.MovementSpeed * .5f);
        }
        else
            movement.Move(Vector3.zero);
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

    public void SetEncounterEngaged(Transform encounterPlayer)
    {
        if (encounterPlayer == null || owner.IsDead)
            return;
        target = encounterPlayer;
        playerHeat = target.GetComponent<PlayerHeat>();
        playerMovement = target.GetComponent<PlayerMovement>();
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
        commitmentTimer = 0f;
        commitmentCooldownTimer = 0f;
        lastKnownSearchTimer = 0f;
        ResetAggression();
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

    private bool HasVisualContact() => target != null &&
        (isEncounterEngaged || DistanceToTarget() <=
            (owner.CurrentState == EnemyBase.State.Idle
                ? detectionRange : combatDetectionRange));

    private void RememberPlayerPosition()
    {
        if (!HasVisualContact()) return;
        lastKnownPlayerPosition = target.position;
        hasLastKnownPlayerPosition = true;
        lastKnownSearchTimer = lastKnownSearchDuration;
    }

    internal void TickAggression()
    {
        if (owner.CurrentState == EnemyBase.State.Idle && !isEncounterEngaged)
            return;
        aggressionChangeTimer -= Time.deltaTime;
        if (aggressionChangeTimer <= 0f)
            SelectAggressionChange();
        currentAggressionPercent = Mathf.Clamp(
            currentAggressionPercent + GetCurrentAggressionVelocity() * Time.deltaTime,
            minimumAggressionPercent, maximumAggressionPercent);
    }

    private void ResetAggression()
    {
        currentAggressionPercent = initialAggression;
        SelectAggressionChange();
    }

    private void SelectAggressionChange()
    {
        float decreaseChance = health != null &&
            health.HealthPercent <= lowEnemyHealthPercent
            ? lowEnemyHealthDecreaseChance : .5f;
        aggressionChangeDirection = Random.value < decreaseChance ? -1f : 1f;
        aggressionChangeRate = Mathf.Max(0f,
            aggressionChangeRatePerSecond.RandomValue());
        aggressionChangeTimer = Mathf.Max(.01f,
            aggressionChangeInterval.RandomValue());
    }

    private float GetCurrentAggressionVelocity()
    {
        float velocity = aggressionChangeDirection * aggressionChangeRate;
        if (IsPlayerBackTurnedToEnemy())
            velocity += playerBackTurnedAggressionGain;
        if (!IsInsideCameraVisibilityPadding())
            velocity -= outOfCameraAggressionLoss;
        float playerHealthPercent = playerHeat == null ? .5f :
            Mathf.InverseLerp(playerHeat.MinHeat, playerHeat.MaxHeat,
                playerHeat.CurrentHeat);
        if (playerHealthPercent <= lowPlayerHealthPercent)
            velocity += lowPlayerHealthAggressionGainPerSecond;
        else if (playerHealthPercent >= highPlayerHealthPercent)
            velocity -= highPlayerHealthAggressionLossPerSecond;
        if (playerMovement != null && !playerMovement.IsGrounded)
            velocity += playerAirborneAggressionChangePerSecond;
        if (IsPlayerFarAway())
            velocity += playerFarAggressionChangePerSecond;
        return velocity;
    }

    private bool IsPlayerFarAway() => playerFarAggressionDistance > 0f &&
        DistanceToTarget() >= playerFarAggressionDistance;

    private bool IsPlayerBackTurnedToEnemy()
    {
        if (target == null) return false;
        Vector3 playerForward = target.forward;
        playerForward.y = 0f;
        Vector3 direction = transform.position - target.position;
        direction.y = 0f;
        return playerForward.sqrMagnitude > .001f &&
            direction.sqrMagnitude > .001f &&
            Vector3.Dot(playerForward.normalized, direction.normalized) < 0f;
    }

    private bool IsInsideCameraVisibilityPadding()
    {
        Camera camera = Camera.main;
        if (camera == null) return true;
        if (cachedVisibilityFrame != Time.frameCount ||
            cachedVisibilityCamera != camera)
        {
            cachedVisibilityFrame = Time.frameCount;
            cachedVisibilityCamera = camera;
            cachedVisibilityPlanes =
                GeometryUtility.CalculateFrustumPlanes(camera);
        }
        Bounds bounds = new(transform.position,
            Vector3.one * cameraVisibilityPadding * 2f);
        return GeometryUtility.TestPlanesAABB(cachedVisibilityPlanes, bounds);
    }

    internal void OnDamaged(int damageDealt)
    {
        if (damageDealt <= 0)
            return;

        float maxHealth = Mathf.Max(1f, owner != null ? owner.MaxHealth : 1f);
        float lostMaxHealthPercent = damageDealt / maxHealth * 100f;
        float change = lostMaxHealthPercent *
            damageTakenAggressionChangePerMaxHealthPercent;
        currentAggressionPercent = Mathf.Clamp(currentAggressionPercent + change,
            minimumAggressionPercent, maximumAggressionPercent);
    }

    internal void ReactToPlayerDamagedByEnemy()
    {
        if (owner.IsDead || isWaitingForEncounter) return;
        float change = currentAggressionPercent * playerDamagedAggressionPercent +
            playerDamagedAggressionFlatChange;
        if (retreatsWhenPlayerTakesEnemyDamage)
            currentAggressionPercent = Mathf.Max(minimumAggressionPercent,
                currentAggressionPercent - change);
        else if (gainsAggressionWhenPlayerTakesEnemyDamage)
            currentAggressionPercent = Mathf.Min(maximumAggressionPercent,
                currentAggressionPercent + change);
    }
}

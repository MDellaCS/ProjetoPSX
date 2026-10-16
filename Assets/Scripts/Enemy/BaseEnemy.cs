using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class BaseEnemy : MonoBehaviour
{
    public enum State
    {
        Idle,
        Combat,
        Windup,
        Active,
        Recovery,
        Staggered
    }

    private static readonly HashSet<BaseEnemy> activeEnemies =
        new HashSet<BaseEnemy>();

    public static event Action<BaseEnemy> EnemyDied;
    public event Action<BaseEnemy, int, int> HealthChanged;
    public event Action<BaseEnemy> Died;

    [Header("Health")]
    [Min(1)] [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool isUnkillable;

    [Header("Poise")]
    [Min(1)] [SerializeField] private int maxPoise = 60;
    [Min(0f)] [SerializeField] private float poiseRecoveryDelay = 3f;
    [Min(0f)] [SerializeField] private float poiseRecoveryPerSecond = 20f;

    [Header("Movement")]
    [Min(0f)] [SerializeField] private float movementSpeed = 2.2f;
    [Tooltip("Aceleracao vertical adicionada a velocidade. Use valor negativo para queda, como no player.")]
    [SerializeField] private float gravity = -20f;
    [Min(2f)] [SerializeField] private float terminalFallSpeed = 30f;
    [Min(0f)] [SerializeField] private float rotationSpeed = 240f;

    [Header("Detection")]
    [Min(0f)] [SerializeField] private float detectionRange = 8f;
    [Tooltip("Alcance de deteccao usado depois que o inimigo ja entrou em combate.")]
    [Min(0f)] [SerializeField] private float combatDetectionRange = 14f;
    [Tooltip("Por quanto tempo procura a ultima posicao conhecida depois de perder o player.")]
    [Min(0f)] [SerializeField] private float lastKnownSearchDuration = 2.5f;
    [Min(0f)] [SerializeField] private float lastKnownArrivalDistance = .35f;

    [Header("Aggression")]
    [Tooltip("Agressividade inicial sorteada uma vez ao nascer. Ex.: 20-20 sempre resulta em 20%.")]
    [FormerlySerializedAs("aggressionPercent")]
    [SerializeField] private EnemyFloatRange initialAggressionPercent = new() { min = 20f, max = 30f };
    [Range(0f, 100f)] [SerializeField] private float minimumAggressionPercent = 10f;
    [Range(0f, 100f)] [SerializeField] private float maximumAggressionPercent = 100f;
    [Tooltip("Velocidade com que a agressividade sobe ou desce durante cada fase aleatoria.")]
    [FormerlySerializedAs("aggressionGrowthPerSecond")]
    [SerializeField] private EnemyFloatRange aggressionChangeRatePerSecond = new() { min = 6f, max = 10f };
    [Tooltip("Tempo antes de sortear se a agressividade sobe ou desce novamente.")]
    [SerializeField] private EnemyFloatRange aggressionChangeInterval = new() { min = 1.5f, max = 3f };
    [Tooltip("Ganho adicional de agressividade por segundo quando o player esta de costas para este inimigo.")]
    [Min(0f)] [SerializeField] private float playerBackTurnedAggressionGain = 12f;
    [Tooltip("Perda de agressividade por segundo quando o inimigo esta fora da camera, incluindo a margem extra configurada.")]
    [Min(0f)] [SerializeField] private float outOfCameraAggressionLoss = 16f;
    [Tooltip("Margem, em unidades de mundo, adicionada ao enquadramento da camera antes de o inimigo ser considerado fora de vista.")]
    [Min(0f)] [SerializeField] private float cameraVisibilityPadding = 3f;
    [Header("Aggression Combat Reactions")]
    [Tooltip("Reducao imediata de agressividade quando este inimigo recebe dano.")]
    [Min(0f)] [SerializeField] private float damageTakenAggressionReduction = 12f;
    [Tooltip("Abaixo desta porcentagem de vida, o proximo Mood Change tende a reduzir agressividade.")]
    [Range(0f, 1f)] [SerializeField] private float lowEnemyHealthPercent = .35f;
    [Range(0f, 1f)] [SerializeField] private float lowEnemyHealthDecreaseChance = .75f;
    [Tooltip("Abaixo desta porcentagem de vida do player, todos os inimigos ganham agressividade continuamente.")]
    [Range(0f, 1f)] [SerializeField] private float lowPlayerHealthPercent = .3f;
    [Min(0f)] [SerializeField] private float lowPlayerHealthAggressionGainPerSecond = 14f;
    [Tooltip("Acima desta porcentagem de Heat/vida do player, os inimigos perdem agressividade continuamente.")]
    [Range(0f, 1f)] [SerializeField] private float highPlayerHealthPercent = .75f;
    [Min(0f)] [SerializeField] private float highPlayerHealthAggressionLossPerSecond = 10f;
    [Tooltip("Tempo entre decisoes de perseguir, atacar ou se reposicionar.")]
    [SerializeField] private EnemyFloatRange decisionInterval = new() { min = .3f, max = .7f };
    [Tooltip("Tempo que um reposicionamento aleatorio dura.")]
    [SerializeField] private EnemyFloatRange repositionDuration = new() { min = .3f, max = .8f };
    [Tooltip("Multiplicador da velocidade ao se reposicionar lateralmente, para frente ou para tras.")]
    [SerializeField] private EnemyFloatRange repositionSpeedMultiplier = new() { min = .25f, max = .55f };

    [Header("Chase Commitment")]
    [Tooltip("Tempo que uma decisao de perseguir e mantida antes da proxima decisao de IA.")]
    [Min(0f)] [SerializeField] private float chaseCommitmentDuration = 1.2f;

    [Header("Attack Combo")]
    [Range(0f, 100f)] [SerializeField] private float comboContinuationChance = 65f;

    [Header("Stagger")]
    [Min(0f)] [SerializeField] private float staggerDuration = .8f;
    [Min(0f)] [SerializeField] private float staggerKnockbackForce = 8f;
    [Min(0f)] [SerializeField] private float staggerKnockbackDuration = .2f;

    [Header("Launch Up")]
    [SerializeField] private bool canBeLaunched = true;
    [SerializeField] private bool canBeJuggled = true;

    [Header("Wall Splat")]
    [Tooltip("Velocidade horizontal minima ao colidir com uma parede para aplicar Wall Splat.")]
    [Min(0f)] [SerializeField] private float wallSplatMinimumSpeed = 10f;
    [Min(0f)] [SerializeField] private float wallSplatStunDuration = 1f;
    [Tooltip("Camadas do cenario que contam como parede para Wall Splat.")]
    [SerializeField] private LayerMask wallSplatLayers = ~0;

    [Header("Visual Feedback")]
    [Min(0f)] [SerializeField] private float damageFlashDuration = .15f;

    [Header("Debug")]
    [Min(0f)] [SerializeField] private float debugLabelHeight = 2.4f;

    private CharacterController controller;
    private Renderer enemyRenderer;
    private DamageNumberSpawner damageNumberSpawner;
    private Transform target;
    private PlayerHeat playerHeat;
    private State currentState;
    private BaseEnemyAttack currentAttack;
    private int comboIndex;
    private float stateTimer;
    private float decisionTimer;
    private float repositionTimer;
    private Vector3 repositionDirection;
    private float repositionSpeed;
    private float chaseTimer;
    private Vector3 lastKnownPlayerPosition;
    private float lastKnownSearchTimer;
    private bool hasLastKnownPlayerPosition;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private Vector3 slideLaunchHorizontalVelocity;
    private bool hasSlideLaunchHorizontalImpulse;
    private Vector3 playerHeadPushVelocity;
    private float playerHeadPushTimer;
    private float verticalVelocity;
    private float launchGravity;
    private float launchGravityMultiplier = 1f;
    private float launchLowGravityMultiplier = 1f;
    private float launchLowGravityTimer;
    private float launchLowGravityDuration;
    private bool launchLowGravityPending;
    private float juggleTimer;
    private float juggleGravityMultiplier = 1f;
    private Vector3 horizontalLaunchVelocity;
    private float horizontalLaunchTimer;
    private Vector3 lastMoveVelocity;
    private bool isLaunched;
    private bool isHorizontallyLaunched;
    private bool hasDamagedDuringActive;
    private bool isEncounterEngaged;
    private bool isWaitingForEncounter;
    private bool hasDied;
    private float currentPoise;
    private float timeSinceLastPoiseDamage;
    private float initialAggression;
    private float aggressionChangeRate;
    private float aggressionChangeDirection;
    private float aggressionChangeTimer;
    private float currentAggressionPercent;
    private Color normalColor;

    private static GUIStyle debugTextStyle;

    public bool IsDead => hasDied;
    public bool CanBeLaunched => canBeLaunched && !hasDied;
    public bool IsLaunched => isLaunched;
    public bool CanBeLockOnTarget => isActiveAndEnabled && !hasDied && !isWaitingForEncounter;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public State CurrentState => currentState;
    public float CurrentAggressionPercent => currentAggressionPercent;
    public Transform Target => target;
    public bool IsSearchingLastKnownPosition =>
        !HasVisualContact() && lastKnownSearchTimer > 0f;

    protected virtual BaseEnemyAttack[] AttackCombo => Array.Empty<BaseEnemyAttack>();
    protected BaseEnemyAttack CurrentAttack => currentAttack;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        enemyRenderer = GetComponentInChildren<Renderer>();
        damageNumberSpawner = GetComponent<DamageNumberSpawner>();
        if (damageNumberSpawner == null)
            damageNumberSpawner = gameObject.AddComponent<DamageNumberSpawner>();
        CurrentHealth = maxHealth;
        currentPoise = maxPoise;
        ValidateAggressionBounds();
        ValidateAggressionReactionThresholds();
        initialAggression = Mathf.Clamp(
            initialAggressionPercent.RandomValue(),
            minimumAggressionPercent,
            maximumAggressionPercent
        );
        currentAggressionPercent = initialAggression;
        SelectAggressionChange();

        if (enemyRenderer != null)
            normalColor = enemyRenderer.material.color;

        ResolvePlayer();
        RememberPlayerPosition();
    }

    private void OnEnable()
    {
        activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    protected virtual void OnValidate()
    {
        // A convencao da base e igual a do player: gravidade e somada a
        // velocidade vertical, portanto queda precisa de valor negativo.
        if (gravity > 0f)
            gravity = -gravity;

        combatDetectionRange = Mathf.Max(
            detectionRange,
            combatDetectionRange
        );
        maximumAggressionPercent = Mathf.Clamp(
            maximumAggressionPercent,
            0f,
            100f
        );
        ValidateAggressionBounds();
        ValidateAggressionReactionThresholds();
    }

    private void Update()
    {
        if (hasDied || isWaitingForEncounter)
            return;

        ResolvePlayer();
        UpdateAggression();
        UpdatePoise();
        UpdateVerticalMovement();

        if (isLaunched || isHorizontallyLaunched)
        {
            Move(Vector3.zero);
            return;
        }

        switch (currentState)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.Combat:
                UpdateCombat();
                break;
            case State.Windup:
                FaceTarget();
                UpdateTimedState(State.Active);
                break;
            case State.Active:
                FaceTarget();
                DealAttackDamage();
                UpdateTimedState(State.Recovery);
                break;
            case State.Recovery:
                UpdateRecovery();
                break;
            case State.Staggered:
                UpdateTimedState(State.Combat);
                break;
        }
    }

    private void UpdateIdle()
    {
        if (HasVisualContact())
        {
            currentState = State.Combat;
            decisionTimer = 0f;
        }
    }

    private void UpdateCombat()
    {
        if (target == null)
            return;

        if (!HasVisualContact())
        {
            UpdateLastKnownSearch();
            return;
        }

        RememberPlayerPosition();
        FaceTarget();

        if (chaseTimer > 0f)
        {
            chaseTimer -= Time.deltaTime;
            Move(DirectionToTarget() * repositionSpeed);
            return;
        }

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
            ChooseCombatAction();

        if (repositionTimer > 0f)
        {
            repositionTimer -= Time.deltaTime;
            Move(repositionDirection * repositionSpeed);
        }
        else
        {
            Move(Vector3.zero);
        }
    }

    private void ChooseCombatAction()
    {
        decisionTimer = Mathf.Max(0.01f, decisionInterval.RandomValue());
        BaseEnemyAttack[] combo = AttackCombo;
        int attackIndex = SelectAttackIndex();
        BaseEnemyAttack selectedAttack = combo != null &&
            attackIndex >= 0 && attackIndex < combo.Length
            ? combo[attackIndex]
            : null;
        float aggression = currentAggressionPercent / 100f;

        // Fora do alcance, o inimigo sempre fecha a distancia. A agressividade
        // determina quao rapido ele faz isso; ela nao pode deixa-lo preso em
        // um reposicionamento lateral para sempre.
        if (selectedAttack != null &&
            DistanceToTarget() > selectedAttack.maximumRange)
        {
            BeginChase(aggression);
            return;
        }

        if (selectedAttack != null &&
            DistanceToTarget() <= selectedAttack.maximumRange &&
            UnityEngine.Random.value <= aggression)
        {
            BeginAttack(attackIndex);
            return;
        }

        Vector3 toTarget = DirectionToTarget();
        if (toTarget.sqrMagnitude > 0.001f &&
            UnityEngine.Random.value <= aggression)
        {
            BeginChase(aggression);
            return;
        }

        Vector3 side = UnityEngine.Random.value < .5f
            ? Vector3.Cross(Vector3.up, toTarget)
            : Vector3.Cross(toTarget, Vector3.up);
        Vector3 randomDirection = UnityEngine.Random.value < .35f
            ? -toTarget
            : side;
        BeginReposition(randomDirection);
    }

    /// <summary>
    /// Escolhe qual ataque abre a proxima acao. Inimigos simples usam o
    /// primeiro; tipos derivados podem escolher conforme a distancia.
    /// </summary>
    protected virtual int SelectAttackIndex() => 0;

    /// <summary>
    /// Determina se um ataque pode seguir para o proximo item do array como
    /// combo. Tipos com ataques situacionais podem desativar isso.
    /// </summary>
    protected virtual bool CanContinueAttackCombo(int currentIndex, int nextIndex) =>
        true;

    private void BeginReposition(Vector3 direction)
    {
        repositionDirection = direction.sqrMagnitude > .001f
            ? direction.normalized
            : Vector3.zero;
        repositionSpeed = movementSpeed * Mathf.Max(0f, repositionSpeedMultiplier.RandomValue());
        repositionTimer = Mathf.Max(0f, repositionDuration.RandomValue());
    }

    private void BeginChase(float aggression)
    {
        repositionDirection = DirectionToTarget();
        repositionSpeed = movementSpeed * Mathf.Lerp(.2f, 1f, aggression);
        repositionTimer = 0f;
        chaseTimer = Mathf.Max(.01f, chaseCommitmentDuration);
    }

    private void UpdateLastKnownSearch()
    {
        if (!hasLastKnownPlayerPosition || lastKnownSearchTimer <= 0f)
        {
            currentState = State.Idle;
            ResetAggression();
            return;
        }

        chaseTimer = 0f;
        lastKnownSearchTimer -= Time.deltaTime;
        Vector3 searchDirection = DirectionToLastKnownPlayerPosition();
        float distance = HorizontalDistanceTo(lastKnownPlayerPosition);

        if (distance > lastKnownArrivalDistance)
        {
            FaceDirection(searchDirection);
            Move(searchDirection * movementSpeed * .5f);
        }
        else
        {
            Move(Vector3.zero);
        }
    }

    private void BeginAttack(int index)
    {
        BaseEnemyAttack[] combo = AttackCombo;
        if (combo == null || index < 0 || index >= combo.Length || combo[index] == null)
            return;

        comboIndex = index;
        currentAttack = combo[index];
        repositionTimer = 0f;
        stateTimer = currentAttack.windupDuration;
        currentState = State.Windup;
    }

    private void UpdateRecovery()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
            return;

        BaseEnemyAttack[] combo = AttackCombo;
        int nextIndex = comboIndex + 1;
        bool canContinue = combo != null && nextIndex < combo.Length &&
            combo[nextIndex] != null && target != null &&
            DistanceToTarget() <= combo[nextIndex].maximumRange &&
            CanContinueAttackCombo(comboIndex, nextIndex) &&
            UnityEngine.Random.value <= comboContinuationChance / 100f;

        if (canContinue)
        {
            BeginAttack(nextIndex);
            return;
        }

        currentAttack = null;
        currentState = State.Combat;
        decisionTimer = Mathf.Max(0.01f, decisionInterval.RandomValue());
    }

    private void UpdateTimedState(State nextState)
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            if (nextState == State.Active)
            {
                stateTimer = currentAttack != null ? currentAttack.activeDuration : 0f;
                hasDamagedDuringActive = false;
            }
            else if (nextState == State.Recovery)
            {
                stateTimer = currentAttack != null ? currentAttack.recoveryDuration : 0f;
            }

            currentState = nextState;
        }
    }

    private void DealAttackDamage()
    {
        if (hasDamagedDuringActive || currentAttack == null)
            return;

        if (ExecuteAttack(currentAttack))
            hasDamagedDuringActive = true;
    }

    /// <summary>
    /// Ponto de extensao para cada tipo de inimigo executar seu ataque.
    /// O BaseEnemy usa uma hitbox corpo a corpo; arqueiros podem criar um
    /// projetil sem duplicar a maquina de estados ou a IA.
    /// </summary>
    protected virtual bool ExecuteAttack(BaseEnemyAttack attack)
    {
        Vector3 hitPosition = transform.position +
            transform.forward * attack.hitboxDistance;
        foreach (Collider hit in Physics.OverlapSphere(hitPosition, attack.hitboxRadius))
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
                continue;

            playerHealth.TakeDamage(
                attack.damage,
                transform.position,
                attack.knockbackForce,
                attack.knockbackDuration,
                true,
                transform
            );
            return true;
        }

        return false;
    }

    private void Move(Vector3 horizontalVelocity)
    {
        Vector3 velocity = isHorizontallyLaunched
            ? horizontalLaunchVelocity
            : playerHeadPushTimer > 0f
                ? playerHeadPushVelocity
            : hasSlideLaunchHorizontalImpulse
                ? slideLaunchHorizontalVelocity
            : knockbackTimer > 0f
                ? knockbackVelocity
                : horizontalVelocity;

        if (playerHeadPushTimer > 0f)
            playerHeadPushTimer -= Time.deltaTime;

        if (knockbackTimer > 0f)
            knockbackTimer -= Time.deltaTime;

        velocity.y = verticalVelocity;
        lastMoveVelocity = velocity;
        CollisionFlags flags = controller.Move(velocity * Time.deltaTime);

        if (isHorizontallyLaunched)
        {
            horizontalLaunchTimer -= Time.deltaTime;
            if (horizontalLaunchTimer <= 0f)
                ResetHorizontalLaunch();
        }

        if (isLaunched && verticalVelocity <= 0f &&
            (flags & CollisionFlags.Below) != 0)
        {
            isLaunched = false;
            verticalVelocity = -2f;
            ResetLaunchLowGravity();
            ResetJuggleMovement();
            CancelSlideLaunchHorizontalImpulse();
            currentState = State.Combat;
            decisionTimer = 0f;
        }
    }

    private void UpdateVerticalMovement()
    {
        if (!isLaunched && controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (juggleTimer > 0f)
        {
            juggleTimer = Mathf.Max(0f, juggleTimer - Time.deltaTime);
            if (juggleTimer <= 0f)
                juggleGravityMultiplier = 1f;
        }

        UpdateLaunchLowGravity();
        float activeGravity = isLaunched ? launchGravity : gravity;
        float activeGravityMultiplier = isLaunched
            ? launchGravityMultiplier
            : 1f;
        verticalVelocity = Mathf.Max(
            verticalVelocity + activeGravity * activeGravityMultiplier *
            juggleGravityMultiplier * Time.deltaTime,
            -terminalFallSpeed
        );
    }

    private void FaceTarget()
    {
        Vector3 direction = DirectionToTarget();
        FaceDirection(direction);
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= .001f)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(direction),
            rotationSpeed * Time.deltaTime
        );
    }

    private Vector3 DirectionToTarget()
    {
        if (target == null)
            return Vector3.zero;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > .001f ? direction.normalized : Vector3.zero;
    }

    private float DistanceToTarget()
    {
        if (target == null)
            return float.PositiveInfinity;

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private float HorizontalDistanceTo(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool HasVisualContact()
    {
        if (target == null)
            return false;

        return isEncounterEngaged ||
            DistanceToTarget() <= (currentState == State.Idle
                ? detectionRange
                : combatDetectionRange);
    }

    private void RememberPlayerPosition()
    {
        if (!HasVisualContact() || target == null)
            return;

        lastKnownPlayerPosition = target.position;
        hasLastKnownPlayerPosition = true;
        lastKnownSearchTimer = lastKnownSearchDuration;
    }

    private Vector3 DirectionToLastKnownPlayerPosition()
    {
        Vector3 direction = lastKnownPlayerPosition - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > .001f ? direction.normalized : Vector3.zero;
    }

    private void UpdatePoise()
    {
        if (currentPoise >= maxPoise)
            return;

        timeSinceLastPoiseDamage += Time.deltaTime;
        if (timeSinceLastPoiseDamage >= poiseRecoveryDelay)
        {
            currentPoise = Mathf.Min(maxPoise,
                currentPoise + poiseRecoveryPerSecond * Time.deltaTime);
        }
    }

    private void UpdateAggression()
    {
        if (currentState == State.Idle && !isEncounterEngaged)
            return;

        aggressionChangeTimer -= Time.deltaTime;
        if (aggressionChangeTimer <= 0f)
            SelectAggressionChange();

        float aggressionVelocity = GetCurrentAggressionVelocity();

        currentAggressionPercent = Mathf.Min(
            maximumAggressionPercent,
            Mathf.Max(
                minimumAggressionPercent,
                currentAggressionPercent + aggressionVelocity * Time.deltaTime
            )
        );
    }

    private void ResetAggression()
    {
        currentAggressionPercent = initialAggression;
        SelectAggressionChange();
    }

    private void SelectAggressionChange()
    {
        float decreaseChance = CurrentHealthPercent <= lowEnemyHealthPercent
            ? lowEnemyHealthDecreaseChance
            : .5f;
        aggressionChangeDirection = UnityEngine.Random.value < decreaseChance
            ? -1f
            : 1f;
        aggressionChangeRate = Mathf.Max(
            0f,
            aggressionChangeRatePerSecond.RandomValue()
        );
        aggressionChangeTimer = Mathf.Max(
            .01f,
            aggressionChangeInterval.RandomValue()
        );
    }

    private float GetCurrentAggressionVelocity()
    {
        float aggressionVelocity = aggressionChangeDirection * aggressionChangeRate;

        if (IsPlayerBackTurnedToEnemy())
            aggressionVelocity += playerBackTurnedAggressionGain;

        if (!IsInsideCameraVisibilityPadding())
            aggressionVelocity -= outOfCameraAggressionLoss;

        float playerHealthPercent = GetPlayerHealthPercent();
        if (playerHealthPercent <= lowPlayerHealthPercent)
            aggressionVelocity += lowPlayerHealthAggressionGainPerSecond;
        else if (playerHealthPercent >= highPlayerHealthPercent)
            aggressionVelocity -= highPlayerHealthAggressionLossPerSecond;

        return aggressionVelocity;
    }

    private float CurrentHealthPercent => maxHealth > 0
        ? Mathf.Clamp01((float)CurrentHealth / maxHealth)
        : 0f;

    private float GetPlayerHealthPercent()
    {
        if (playerHeat == null)
            return .5f;

        return Mathf.InverseLerp(
            playerHeat.MinHeat,
            playerHeat.MaxHeat,
            playerHeat.CurrentHeat
        );
    }

    private bool IsPlayerBackTurnedToEnemy()
    {
        if (target == null)
            return false;

        Vector3 playerForward = target.forward;
        playerForward.y = 0f;
        Vector3 directionToEnemy = transform.position - target.position;
        directionToEnemy.y = 0f;

        if (playerForward.sqrMagnitude <= .001f ||
            directionToEnemy.sqrMagnitude <= .001f)
        {
            return false;
        }

        return Vector3.Dot(
            playerForward.normalized,
            directionToEnemy.normalized
        ) < 0f;
    }

    private bool IsInsideCameraVisibilityPadding()
    {
        Camera gameCamera = Camera.main;
        if (gameCamera == null)
            return true;

        Bounds expandedEnemyBounds = new Bounds(
            transform.position,
            Vector3.one * cameraVisibilityPadding * 2f
        );
        Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(gameCamera);
        return GeometryUtility.TestPlanesAABB(
            cameraPlanes,
            expandedEnemyBounds
        );
    }

    private void ValidateAggressionBounds()
    {
        minimumAggressionPercent = Mathf.Clamp(
            minimumAggressionPercent,
            0f,
            100f
        );
        maximumAggressionPercent = Mathf.Clamp(
            maximumAggressionPercent,
            minimumAggressionPercent,
            100f
        );
    }

    private void ValidateAggressionReactionThresholds()
    {
        lowEnemyHealthPercent = Mathf.Clamp01(lowEnemyHealthPercent);
        lowEnemyHealthDecreaseChance = Mathf.Clamp01(lowEnemyHealthDecreaseChance);
        lowPlayerHealthPercent = Mathf.Clamp01(lowPlayerHealthPercent);
        highPlayerHealthPercent = Mathf.Clamp(
            highPlayerHealthPercent,
            lowPlayerHealthPercent,
            1f
        );
    }

    public void TakeDamage(
        int damage,
        Vector3 attackerPosition,
        int poiseDamage,
        bool fromPlayerAttack = false)
    {
        if (hasDied)
            return;

        if (fromPlayerAttack)
            CancelSlideLaunchHorizontalImpulse();

        CurrentHealth -= damage;
        damageNumberSpawner?.ShowDamage(damage);
        currentAggressionPercent = Mathf.Max(
            minimumAggressionPercent,
            currentAggressionPercent - damageTakenAggressionReduction
        );
        FlashDamage();
        ApplyPoiseDamage(poiseDamage, attackerPosition);

        if (CurrentHealth <= 0)
        {
            if (isUnkillable)
            {
                CurrentHealth = 1;
                HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
                return;
            }

            CurrentHealth = 0;
            HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
            Die();
            return;
        }

        HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
    }

    private void ApplyPoiseDamage(int amount, Vector3 attackerPosition)
    {
        if (amount <= 0)
            return;

        currentPoise -= amount;
        timeSinceLastPoiseDamage = 0f;
        if (currentPoise > 0f)
            return;

        ApplyStagger(attackerPosition);
        currentPoise = maxPoise;
    }

    public void ApplyStagger(Vector3 attackerPosition)
    {
        if (hasDied || isLaunched)
            return;

        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;
        knockbackVelocity = direction.sqrMagnitude > .001f
            ? direction.normalized * staggerKnockbackForce
            : Vector3.zero;
        knockbackTimer = staggerKnockbackDuration;
        currentAttack = null;
        repositionTimer = 0f;
        stateTimer = staggerDuration;
        currentState = State.Staggered;
    }

    public void ApplyLaunchUp(float initialSpeed, float gravity,
        float lowGravityMultiplier, float lowGravityDuration)
    {
        if (!canBeLaunched || hasDied)
            return;

        currentAttack = null;
        repositionTimer = 0f;
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        verticalVelocity = initialSpeed;
        launchGravity = gravity;
        launchGravityMultiplier = 1f;
        launchLowGravityMultiplier = Mathf.Clamp01(lowGravityMultiplier);
        launchLowGravityTimer = 0f;
        launchLowGravityDuration = Mathf.Max(0f, lowGravityDuration);
        launchLowGravityPending = launchLowGravityDuration > 0f &&
            launchLowGravityMultiplier < 1f;
        isLaunched = true;
        ResetJuggleMovement();
        CancelSlideLaunchHorizontalImpulse();
    }

    /// <summary>
    /// Complementa um Launch Up com um impulso horizontal que persiste ate o
    /// inimigo tocar o chao ou receber dano do player.
    /// </summary>
    public void ApplyLaunchUpHorizontalImpulse(
        Vector3 direction,
        float speed)
    {
        if (!isLaunched || hasDied || speed <= 0f)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude <= .0001f)
            return;

        slideLaunchHorizontalVelocity = direction.normalized * speed;
        hasSlideLaunchHorizontalImpulse = true;
    }

    public void CancelSlideLaunchHorizontalImpulse()
    {
        slideLaunchHorizontalVelocity = Vector3.zero;
        hasSlideLaunchHorizontalImpulse = false;
    }

    public void ApplyLaunch(Vector3 attackerPosition, float force, float duration)
    {
        if (!canBeLaunched || hasDied || force <= 0f || duration <= 0f)
            return;

        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= .001f)
            direction = transform.forward;

        currentAttack = null;
        repositionTimer = 0f;
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        horizontalLaunchVelocity = direction.normalized * force;
        horizontalLaunchTimer = duration;
        isHorizontallyLaunched = true;
        CancelSlideLaunchHorizontalImpulse();
    }

    public void ApplyAerialJuggleLight(float liftSpeed, float gravity,
        float gravityMultiplier, float duration)
    {
        if (!canBeJuggled || !isLaunched || hasDied)
            return;

        currentAttack = null;
        verticalVelocity = liftSpeed;
        launchGravity = gravity;
        ResetLaunchLowGravity();
        juggleGravityMultiplier = Mathf.Clamp01(gravityMultiplier);
        juggleTimer = Mathf.Max(juggleTimer, duration);
    }

    /// <summary>
    /// Faz um inimigo ja lancado acompanhar a queda de um Dive. Nao altera
    /// inimigos no chao: Dive nao e uma fonte de Launch Up.
    /// </summary>
    public void ApplyAerialDive(float fallSpeed)
    {
        if (!canBeJuggled || !isLaunched || hasDied)
            return;

        currentAttack = null;
        verticalVelocity = -Mathf.Max(0f, fallSpeed);
        ResetLaunchLowGravity();
        ResetJuggleMovement();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null || hit.collider == null)
            return;

        PlayerMovement hitPlayer =
            hit.collider.GetComponentInParent<PlayerMovement>();
        if (hitPlayer != null && IsLandingOnPlayerTop(hitPlayer))
        {
            PushOffPlayerHead(hitPlayer);
            return;
        }

        // Chao e teto nao contam como parede. Uma parede possui normal quase
        // horizontal; rampas muito inclinadas tambem ficam fora do Wall Splat.
        if (Mathf.Abs(hit.normal.y) > .5f)
            return;

        Collider obstacle = hit.collider;
        if (obstacle.GetComponentInParent<BaseEnemy>() != null ||
            obstacle.GetComponentInParent<PlayerMovement>() != null)
        {
            return;
        }

        int obstacleLayer = obstacle.gameObject.layer;
        if ((wallSplatLayers.value & (1 << obstacleLayer)) == 0)
            return;

        Vector3 horizontalVelocity = lastMoveVelocity;
        horizontalVelocity.y = 0f;
        if (horizontalVelocity.magnitude < wallSplatMinimumSpeed)
            return;

        ApplyWallSplat();
    }

    private bool IsLandingOnPlayerTop(PlayerMovement player)
    {
        if (controller == null || verticalVelocity > 0f)
            return false;

        CharacterController playerController =
            player.GetComponent<CharacterController>();
        if (playerController == null)
            return false;

        const float topContactTolerance = .15f;
        float enemyBottom = controller.bounds.min.y;
        float playerTop = playerController.bounds.max.y;
        return enemyBottom >= playerTop - topContactTolerance;
    }

    private void PushOffPlayerHead(PlayerMovement player)
    {
        // O mesmo valor usado pelo Player ao cair num inimigo tambem impede
        // que inimigos usem a cabeca do player como plataforma.
        float pushSpeed = player.enemyHeadPushSpeed;
        Vector3 pushDirection = transform.position - player.transform.position;
        pushDirection.y = 0f;

        if (pushDirection.sqrMagnitude <= .0001f)
            pushDirection = transform.right;

        playerHeadPushVelocity = pushDirection.normalized * pushSpeed;
        playerHeadPushTimer = .1f;
        verticalVelocity = Mathf.Min(verticalVelocity, -2f);
    }

    public void SetEncounterEngaged(Transform encounterPlayer)
    {
        if (encounterPlayer == null || hasDied)
            return;

        target = encounterPlayer;
        isWaitingForEncounter = false;
        isEncounterEngaged = true;
        if (currentState == State.Idle)
            currentState = State.Combat;
    }

    public void SetWaitingForEncounter()
    {
        isWaitingForEncounter = true;
        isEncounterEngaged = false;
        currentAttack = null;
        currentState = State.Idle;
        chaseTimer = 0f;
        lastKnownSearchTimer = 0f;
        ResetAggression();
    }

    public void SetUnkillable(bool value) => isUnkillable = value;

    public static bool IsAnyEnemyAggroedOn(Transform player)
    {
        foreach (BaseEnemy enemy in activeEnemies)
        {
            if (enemy != null && enemy.target == player &&
                enemy.currentState != State.Idle && !enemy.hasDied)
            {
                return true;
            }
        }

        return false;
    }

    private void Die()
    {
        if (hasDied)
            return;

        hasDied = true;
        Died?.Invoke(this);
        EnemyDied?.Invoke(this);
        Destroy(gameObject);
    }

    private void FlashDamage()
    {
        if (enemyRenderer == null)
            return;

        enemyRenderer.material.color = Color.red;
        CancelInvoke(nameof(ResetVisualColor));
        Invoke(nameof(ResetVisualColor), damageFlashDuration);
    }

    private void ResetVisualColor()
    {
        if (enemyRenderer != null)
            enemyRenderer.material.color = normalColor;
    }

    private void ResetJuggleMovement()
    {
        juggleTimer = 0f;
        juggleGravityMultiplier = 1f;
    }

    private void UpdateLaunchLowGravity()
    {
        if (!isLaunched)
        {
            launchGravityMultiplier = 1f;
            return;
        }

        if (launchLowGravityPending)
        {
            // A sustentacao comeca somente quando a subida terminou.
            if (verticalVelocity > 0f)
                return;

            launchLowGravityPending = false;
            launchLowGravityTimer = launchLowGravityDuration;
            launchGravityMultiplier = launchLowGravityMultiplier;
        }

        if (launchLowGravityTimer <= 0f)
        {
            launchGravityMultiplier = 1f;
            return;
        }

        launchLowGravityTimer = Mathf.Max(
            0f,
            launchLowGravityTimer - Time.deltaTime
        );

        if (launchLowGravityTimer <= 0f)
            launchGravityMultiplier = 1f;
    }

    private void ResetLaunchLowGravity()
    {
        launchLowGravityTimer = 0f;
        launchLowGravityDuration = 0f;
        launchLowGravityPending = false;
        launchGravityMultiplier = 1f;
        launchLowGravityMultiplier = 1f;
    }

    private void ResetHorizontalLaunch()
    {
        horizontalLaunchVelocity = Vector3.zero;
        horizontalLaunchTimer = 0f;
        isHorizontallyLaunched = false;
    }

    private void ApplyWallSplat()
    {
        ResetHorizontalLaunch();
        CancelSlideLaunchHorizontalImpulse();

        currentAttack = null;
        repositionTimer = 0f;
        stateTimer = wallSplatStunDuration;
        currentState = wallSplatStunDuration > 0f
            ? State.Staggered
            : State.Combat;
    }

    private void ResolvePlayer()
    {
        if (target != null)
        {
            if (playerHeat == null)
                playerHeat = target.GetComponent<PlayerHeat>();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            playerHeat = player.GetComponent<PlayerHeat>();
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible)
            return;

        Gizmos.color = new Color(.2f, .85f, 1f, .7f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (currentState == State.Active && currentAttack != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position +
                transform.forward * currentAttack.hitboxDistance,
                currentAttack.hitboxRadius);
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible || hasDied)
            return;

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
            return;

        Vector3 screenPosition = gameCamera.WorldToScreenPoint(
            transform.position + Vector3.up * debugLabelHeight
        );
        if (screenPosition.z <= 0f)
            return;

        EnsureDebugTextStyle();

        string attackName = currentAttack != null
            ? currentAttack.attackName
            : "-";
        string attackTimings = currentAttack != null
            ? "W/A/R: " + currentAttack.windupDuration.ToString("0.00") +
                " / " + currentAttack.activeDuration.ToString("0.00") +
                " / " + currentAttack.recoveryDuration.ToString("0.00")
            : "W/A/R: -";
        string targetName = target != null ? target.name : "None";
        string status = isHorizontallyLaunched ? "LAUNCH" :
            isLaunched ? "LAUNCHED" :
            IsSearchingLastKnownPosition ? "SEARCHING" : "GROUND";
        float aggressionVelocity = GetCurrentAggressionVelocity();
        string playerBackStatus = IsPlayerBackTurnedToEnemy() ? "YES" : "NO";
        string cameraStatus = IsInsideCameraVisibilityPadding() ? "VISIBLE" : "HIDDEN";
        string debugText = GetType().Name + "\n" +
            "HP: " + CurrentHealth + " / " + maxHealth +
            "   POISE: " + Mathf.CeilToInt(currentPoise) + " / " + maxPoise + "\n" +
            "STATE: " + currentState + "   " + status + "\n" +
            "AGGRO: " + currentAggressionPercent.ToString("0.0") + "%" +
            " ( " + aggressionVelocity.ToString("+0.0;-0.0;0.0") + "/s )\n" +
            "MOOD CHANGE: " + aggressionChangeTimer.ToString("0.00") + "s" +
            "   BACK: " + playerBackStatus + "\n" +
            "CAMERA: " + cameraStatus + "\n" +
            "MOVE: " + movementSpeed.ToString("0.00") +
            "   DETECT: " + detectionRange.ToString("0.00") +
            " / " + combatDetectionRange.ToString("0.00") + "\n" +
            "ATTACK: " + attackName + "\n" +
            attackTimings + "\n" +
            "TARGET: " + targetName;

        const float width = 190f;
        const float height = 165f;
        Rect panelRect = new Rect(
            screenPosition.x - width * .5f,
            Screen.height - screenPosition.y - height,
            width,
            height
        );

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, .72f);
        GUI.Box(panelRect, GUIContent.none);
        GUI.color = previousColor;
        GUI.Label(panelRect, debugText, debugTextStyle);
    }

    private static void EnsureDebugTextStyle()
    {
        if (debugTextStyle != null)
            return;

        debugTextStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 11,
            padding = new RectOffset(6, 6, 4, 4),
            normal = { textColor = Color.white }
        };
    }
}

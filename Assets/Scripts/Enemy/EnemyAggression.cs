using UnityEngine;

/// <summary>Humor e agressividade do inimigo, incluindo modificadores temporarios.</summary>
[DisallowMultipleComponent]
public sealed class EnemyAggression : MonoBehaviour
{
    private static int cachedVisibilityFrame = -1;
    private static Camera cachedVisibilityCamera;
    private static Plane[] cachedVisibilityPlanes;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticCaches()
    {
        cachedVisibilityFrame = -1;
        cachedVisibilityCamera = null;
        cachedVisibilityPlanes = null;
    }

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
    [Tooltip("Abaixo desta fração do recurso defensivo do jogador, a agressividade sobe. Usa Heat no moveset de Heat e vida restante no moveset Health.")]
    [Range(0f, 1f)] [SerializeField] private float lowPlayerHealthPercent = .3f;
    [Tooltip("Agressividade ganha por segundo com o jogador em pouca vida.")]
    [Min(0f)] [SerializeField] private float lowPlayerHealthAggressionGainPerSecond = 14f;
    [Tooltip("Acima desta fração do recurso defensivo do jogador, a agressividade cai. Usa Heat no moveset de Heat e vida restante no moveset Health.")]
    [Range(0f, 1f)] [SerializeField] private float highPlayerHealthPercent = .75f;
    [Tooltip("Agressividade perdida por segundo quando o recurso defensivo do jogador esta alto.")]
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

    [Header("Aggression Lock-on")]
    [Tooltip("Pontos de agressividade somados enquanto este inimigo estiver travado pelo jogador. Nao altera a agressividade real.")]
    [Min(0f)] [SerializeField] private float lockOnAggressionBonus = 20f;
    [Tooltip("Agressividade efetiva minima deste inimigo enquanto estiver travado. Pode superar o limite maximo normal, se necessario.")]
    [Range(0f, 100f)] [SerializeField] private float lockOnMinimumAggressionPercent = 50f;
    [Tooltip("Pontos de agressividade retirados enquanto outro inimigo estiver travado. Nao altera a agressividade real.")]
    [Min(0f)] [SerializeField] private float otherLockOnAggressionPenalty = 20f;
    [Tooltip("Agressividade efetiva maxima deste inimigo enquanto outro inimigo estiver travado. Nao aumenta seu limite maximo normal.")]
    [Range(0f, 100f)] [SerializeField] private float otherLockOnMaximumAggressionPercent = 50f;

    private EnemyBase owner;
    private EnemyDecisions decisions;
    private EnemyHealth health;
    private Transform target => decisions.Target;
    private PlayerHeat playerHeat => decisions.PlayerHeat;
    private PlayerMovement playerMovement => decisions.TargetMovement;
    private PlayerHealth playerHealth => decisions.PlayerHealth;
    private float initialAggression;
    private float aggressionChangeRate;
    private float aggressionChangeDirection;
    private float aggressionChangeTimer;
    private float currentAggressionPercent;
    private bool isAggressionSuppressedByBurn;
    private float aggressionBeforeBurn;
    public float CurrentAggressionPercent => currentAggressionPercent;
    public float EffectiveAggressionPercent => isAggressionSuppressedByBurn ? 0f :
        ApplyLockOnAggression(currentAggressionPercent + EnemyDirector.GetAggressionModifier(owner));
    internal float DirectorSelectionAggressionPercent => isAggressionSuppressedByBurn
        ? 0f : ApplyLockOnAggression(currentAggressionPercent);
    public float AggressionChangeTimer => aggressionChangeTimer;
    public float PlayerFarAggressionDistance => playerFarAggressionDistance;

    private float ApplyLockOnAggression(float aggression)
    {
        PlayerLockOn lockOn = playerMovement != null ? playerMovement.LockOn : null;
        EnemyBase lockedEnemy = lockOn != null && lockOn.isActiveAndEnabled
            ? lockOn.Target : null;
        if (lockedEnemy == null)
            return Mathf.Clamp(aggression, minimumAggressionPercent, maximumAggressionPercent);

        return EnemyLockOnAggression.Calculate(aggression, minimumAggressionPercent,
            maximumAggressionPercent, lockedEnemy == owner, lockOnAggressionBonus,
            lockOnMinimumAggressionPercent, otherLockOnAggressionPenalty,
            otherLockOnMaximumAggressionPercent);
    }
    internal void Initialize(EnemyBase enemy, EnemyDecisions enemyDecisions, EnemyHealth enemyHealth)
    {
        owner = enemy; decisions = enemyDecisions; health = enemyHealth;
        ValidateBounds();
        initialAggression = Mathf.Clamp(initialAggressionPercent.RandomValue(),
            minimumAggressionPercent, maximumAggressionPercent);
        currentAggressionPercent = initialAggression;
        SelectAggressionChange();
    }

    private void OnValidate() => ValidateBounds();
    private void ValidateBounds()
    {
        minimumAggressionPercent = Mathf.Clamp(minimumAggressionPercent, 0f, 100f);
        maximumAggressionPercent = Mathf.Clamp(maximumAggressionPercent, minimumAggressionPercent, 100f);
        lockOnAggressionBonus = Mathf.Max(0f, lockOnAggressionBonus);
        lockOnMinimumAggressionPercent = Mathf.Clamp(lockOnMinimumAggressionPercent, 0f, 100f);
        otherLockOnAggressionPenalty = Mathf.Max(0f, otherLockOnAggressionPenalty);
        otherLockOnMaximumAggressionPercent = Mathf.Clamp(otherLockOnMaximumAggressionPercent, 0f, 100f);
        lowEnemyHealthPercent = Mathf.Clamp01(lowEnemyHealthPercent);
        lowEnemyHealthDecreaseChance = Mathf.Clamp01(lowEnemyHealthDecreaseChance);
        lowPlayerHealthPercent = Mathf.Clamp01(lowPlayerHealthPercent);
        highPlayerHealthPercent = Mathf.Clamp(highPlayerHealthPercent, lowPlayerHealthPercent, 1f);
        playerFarAggressionDistance = Mathf.Max(0f, playerFarAggressionDistance);
    }

    internal void TickAggression()
    {
        UpdateBurnAggressionSuppression();
        if (isAggressionSuppressedByBurn)
            return;
        if (owner.CurrentState == EnemyBase.State.Idle && !decisions.IsEncounterEngaged)
            return;
        aggressionChangeTimer -= Time.deltaTime;
        if (aggressionChangeTimer <= 0f)
            SelectAggressionChange();
        currentAggressionPercent = Mathf.Clamp(
            currentAggressionPercent + GetCurrentAggressionVelocity() * Time.deltaTime,
            minimumAggressionPercent, maximumAggressionPercent);
    }

    internal void ApplyDodgeAggressionChange(float change)
    {
        if (Mathf.Approximately(change, 0f) || isAggressionSuppressedByBurn ||
            owner == null || owner.IsDead)
            return;

        currentAggressionPercent = Mathf.Clamp(currentAggressionPercent + change,
            minimumAggressionPercent, maximumAggressionPercent);
    }

    internal void ResetAggression()
    {
        if (isAggressionSuppressedByBurn)
            return;
        currentAggressionPercent = initialAggression;
        SelectAggressionChange();
    }

    /// <summary>
    /// Burn e uma supressao temporaria, nao uma alteracao de humor. Cada
    /// inimigo guarda seu valor individual e o restaura intacto ao termino.
    /// </summary>
    private void UpdateBurnAggressionSuppression()
    {
        bool playerIsBurning = playerHeat != null && playerHeat.IsBurning;
        if (playerIsBurning == isAggressionSuppressedByBurn)
            return;

        isAggressionSuppressedByBurn = playerIsBurning;
        if (playerIsBurning)
        {
            aggressionBeforeBurn = currentAggressionPercent;
            currentAggressionPercent = 0f;
            return;
        }

        currentAggressionPercent = Mathf.Clamp(aggressionBeforeBurn,
            minimumAggressionPercent, maximumAggressionPercent);
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

    internal float GetCurrentAggressionVelocity()
    {
        float velocity = aggressionChangeDirection * aggressionChangeRate;
        if (IsPlayerBackTurnedToEnemy())
            velocity += playerBackTurnedAggressionGain;
        if (!IsInsideCameraVisibilityPadding())
            velocity -= outOfCameraAggressionLoss;
        PlayerHealth targetHealth = target != null ? playerHealth : null;
        bool targetUsesHealth = playerHeat != null && !playerHeat.IsHeatModeActive;
        float playerHealthPercent = targetUsesHealth && targetHealth != null
            ? targetHealth.HealthPercent
            : playerHeat == null ? .5f : Mathf.InverseLerp(playerHeat.MinHeat,
                playerHeat.MaxHeat, playerHeat.CurrentHeat);
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
        decisions.DistanceToTarget() >= playerFarAggressionDistance;

    internal bool IsPlayerBackTurnedToEnemy()
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

    internal bool IsInsideCameraVisibilityPadding()
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
        if (damageDealt <= 0 || isAggressionSuppressedByBurn)
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
        if (owner.IsDead || decisions.IsWaitingForEncounter || isAggressionSuppressedByBurn) return;
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

// Calcula apenas a agressividade usada nas decisoes; nao altera o estado real.
internal static class EnemyLockOnAggression
{
    internal static float Calculate(float aggression, float normalMinimum,
        float normalMaximum, bool isLockedTarget, float targetBonus,
        float targetMinimum, float otherPenalty, float otherMaximum)
    {
        if (isLockedTarget)
        {
            float minimum = Mathf.Max(normalMinimum, Mathf.Clamp(targetMinimum, 0f, 100f));
            float maximum = Mathf.Max(normalMaximum, minimum);
            return Mathf.Clamp(aggression + Mathf.Max(0f, targetBonus), minimum, maximum);
        }

        float maximumOther = Mathf.Min(normalMaximum, Mathf.Clamp(otherMaximum, 0f, 100f));
        float minimumOther = Mathf.Min(normalMinimum, maximumOther);
        return Mathf.Clamp(aggression - Mathf.Max(0f, otherPenalty), minimumOther, maximumOther);
    }
}

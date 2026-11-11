using UnityEngine;
using System.Collections.Generic;

public class PlayerHeat : MonoBehaviour
{
    private const float HeatPerNotch = 10f;

    /// <summary>Snapshot da carga usada por um pesado especifico.</summary>
    public readonly struct HeavyAttackCharge
    {
        public readonly float HeatConsumed;
        public readonly float DamageMultiplier;
        public readonly float PoiseMultiplier;

        public HeavyAttackCharge(float heatConsumed, float damageMultiplier, float poiseMultiplier)
        {
            HeatConsumed = heatConsumed;
            DamageMultiplier = damageMultiplier;
            PoiseMultiplier = poiseMultiplier;
        }
    }

    private enum BurnPhase { None, PreExplosion, ExplosionStun, Rage, Burning, RecoveryStun }

    [Header("Heat Notches")]
    [Tooltip("Limite positivo inicial de Heat, antes dos upgrades. Cada 10 Heat equivale a um notch de poder; Carga adiciona capacidade acima deste limite.")]
    [Min(1f)] public float maxHeat = 100f;
    [Tooltip("Magnitude maxima de Cold. O limite real de Heat e o valor negativo deste campo; alcanca-lo mata o player.")]
    [Min(1f)] public float maxColdMagnitude = 100f;

    [Header("Heavy Attack Power")]
    [Tooltip("Bonus de dano base adicionado por cada ponto do multiplicador de poder antes de multiplicar o pesado. Ex.: dano 130, poder 10x e bonus 5 resulta em (130 + 10 x 5) x 10 = 1800.")]
    [Min(0f)] public float heavyDamageAdditivePowerBonus = 5f;

    [Header("Light Attack Heat")]
    [Tooltip("Heat gerado por cada ponto de dano realmente retirado da vida do inimigo por um ataque leve marcado para gerar Heat.")]
    [Min(0f)] public float heatGeneratedPerDamageDealt = .3f;

    [Header("Superheat Notches")]
    [Tooltip("Capacidade maxima de Superheat. Ao encher, inicia o estado Burn.")]
    [Min(1f)] public float maxSuperheat = 50f;
    [Tooltip("Parte do Heat que excede o Max Heat atual, incluindo Carga, convertida em Superheat.")]
    [Min(0f)] public float excessHeatToSuperheatMultiplier = .5f;

    [Header("Passive Heat Cooling")]
    [Tooltip("Tempo minimo desde o ultimo ganho de Heat, ou desde sair de combate, antes que o resfriamento passivo possa comecar. Enquanto em combate, este tempo fica travado e nao comeca a contar.")]
    [Min(0f)] public float passiveHeatCoolingAfterGainDelay = 3f;
    [Tooltip("Heat removido por segundo no inicio do resfriamento, apos o Start Delay.")]
    [Min(0f)] public float initialPassiveHeatCoolingPerSecond = .2f;
    [Tooltip("Heat removido por segundo quando a rampa de resfriamento chega ao fim.")]
    [Min(0f)] public float finalPassiveHeatCoolingPerSecond = 18f;
    [Tooltip("Tempo fora de combate para a taxa de resfriamento ir do valor Initial ate o Final.")]
    [Min(0f)] public float passiveHeatCoolingRampDuration = 15f;
    [Tooltip("Formato da aceleracao da rampa. Valores maiores mantem o resfriamento fraco por mais tempo e aceleram perto do final.")]
    [Min(.01f)] public float passiveHeatCoolingRampExponent = 1.4f;

    [Header("Passive Cold Recovery")]
    [Tooltip("Heat recuperado por segundo enquanto o player esta com Heat negativo e fora de combate.")]
    [Min(0f)] public float passiveColdRecoveryPerSecond = 1f;
    [Tooltip("Parte do Cold maximo ate onde a recuperacao passiva pode chegar. Ex.: 0.9 recupera somente ate -90 se Min Heat for -100.")]
    [Range(0f, 1f)] public float coldRecoveryLimitPercent = .7f;

    [Header("Passive Superheat Cooling")]
    [Tooltip("Superheat removido por segundo enquanto o player esta fora de combate.")]
    [Min(0f)] public float passiveSuperheatCoolingPerSecond = 1f;
    [Tooltip("Superheat extra removido por segundo enquanto o Heat atual esta negativo. Acumula com o resfriamento fora de combate.")]
    [Min(0f)] public float coldSuperheatCoolingPerSecond = 4f;

    [Header("Burn")]
    [Tooltip("Tempo de Pre-Explosion em que o player pode apenas andar lentamente antes de explodir.")]
    [Min(0f)] public float burnPreExplosionDuration = 1.5f;
    [Tooltip("Duracao do stun completo causado pela propria explosao de Burn.")]
    [Min(0f)] public float burnInitialStunDuration = 3f;
    [Tooltip("Duracao do Burn depois da explosao. Durante esse tempo o player anda lentamente e perde Heat ate o alvo final.")]
    [Min(0f)] public float burningDuration = 5f;
    [Tooltip("Controla como o Heat cai durante o tempo de Burning. O eixo X representa o tempo normalizado; Y representa a parte da queda de Heat ja aplicada. Uma curva linear distribui a queda uniformemente.")]
    public AnimationCurve burningHeatDescentCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Duracao do stun de Burn Recovery aplicado ao fim do Burn.")]
    [Min(0f)] public float burnFinalStunDuration = 1f;
    [Tooltip("Multiplicador da velocidade de movimento durante Pre-Explosion e Burn.")]
    [Range(0f, 1f)] public float burnMovementSpeedMultiplier = .3f;
    [Tooltip("Porcentagem do Cold maximo alcancada ao terminar Burn. Ex.: 0.7 leva o Heat a -70 quando o Cold maximo e 100.")]
    [Range(0f, 1f)] public float burnFinalColdPercent = .7f;

    [Header("Burn Blast")]
    [Tooltip("Asset de ataque executado automaticamente quando o Burn explode. Controla dano, Poise, Strikes, hitboxes, falloff e efeitos como qualquer outro ataque do player.")]
    [SerializeField] private PlayerAttackDefinition burnExplosionAttack;

    private float currentHeat;
    private float currentSuperheat;
    private float heatCoolingElapsed;
    private float heatGainCoolingDelayRemaining;
    private BurnPhase burnPhase;
    private float burnPhaseTimer;
    private float burnElapsed;
    private float burnStartHeat;

    private PlayerState playerState;
    private PlayerHealth playerHealth;
    private PlayerCombatStatus combatStatus;
    private WaterSensor waterSensor;

    // Na agua (rasa ou funda, conforme o WaterProfile) o jogador conta como
    // fora de combate para o resfriamento passivo do Heat.
    private bool IsOutOfCombatForCooling =>
        (combatStatus != null && combatStatus.IsOutOfCombat) ||
        (waterSensor != null && waterSensor.CoolsHeat);
    private PlayerAttackController attackController;
    private PlayerMovement playerMovement;
    private PlayerMovesetController movesetController;
    private float chargeMaxHeatBonus;
    private readonly Dictionary<Object, float> rageGrants = new();
    private float rageDuration;

    public float CurrentHeat => currentHeat;
    public float CurrentSuperheat => currentSuperheat;
    public float InitialMaxHeat => Mathf.Max(1f, maxHeat);
    public float MaxHeat => Mathf.Max(1f, maxHeat + chargeMaxHeatBonus);
    public float MinHeat => -maxColdMagnitude;
    public float MaxSuperheat => maxSuperheat;
    // A pequena tolerancia evita que 100 armazenado como 99.99999 por
    // operacoes de ponto flutuante deixe o ultimo quadrado apagado.
    public int CurrentHeatNotch => GetSignedNotch(currentHeat, MaxHeat, maxColdMagnitude);
    public int CurrentSuperheatNotch => GetPositiveNotch(currentSuperheat,
        maxSuperheat, Mathf.CeilToInt(maxSuperheat / HeatPerNotch));
    public bool IsBurning => burnPhase != BurnPhase.None;
    public bool IsRaging => burnPhase == BurnPhase.Rage;
    public float RageTimeRemaining => IsRaging ? Mathf.Max(0f, burnPhaseTimer) : 0f;
    public bool HasRageUnlocked => rageDuration > 0f;
    public float BurnMovementSpeedMultiplier => burnMovementSpeedMultiplier;
    public float HeavyDamageMultiplier => GetHeavyDamageMultiplier();
    public float HeavyPoiseMultiplier => GetHeavyPoiseMultiplier();
    private float BurnFinalHeatTarget => -maxColdMagnitude * burnFinalColdPercent;
    public bool IsHeatModeActive
    {
        get
        {
            if (movesetController == null)
                movesetController = GetComponent<PlayerMovesetController>();
            return movesetController == null || movesetController.UsesHeat;
        }
    }


    private void Start()
    {
        PlayerProgression.GetOrAdd(gameObject).Initialize();
        movesetController = GetComponent<PlayerMovesetController>();
        playerState = GetComponent<PlayerState>();
        playerHealth = GetComponent<PlayerHealth>();
        combatStatus = GetComponent<PlayerCombatStatus>();
        waterSensor = WaterSensor.GetOrAdd(gameObject);
        attackController = GetComponent<PlayerAttackController>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    /// <summary>Aplica o bonus persistente de Carga ao limite positivo de Heat.</summary>
    public void SetChargeMaxHeatBonus(float bonus)
    {
        chargeMaxHeatBonus = Mathf.Max(0f, bonus);
        currentHeat = Mathf.Min(currentHeat, MaxHeat);
    }

    /// <summary>Concede ou remove Rage sem guardar estado mutavel no asset de upgrade.</summary>
    public void SetRageGrant(Object source, float duration)
    {
        if (source == null) return;
        if (duration > 0f) rageGrants[source] = duration;
        else rageGrants.Remove(source);
        rageDuration = 0f;
        foreach (float grantedDuration in rageGrants.Values)
            rageDuration = Mathf.Max(rageDuration, grantedDuration);
    }

    /// <summary>Retorna Heat, Superheat e fases de Burn/Rage ao estado inicial de uma nova campanha.</summary>
    public void ResetForNewGame()
    {
        currentHeat = 0f;
        currentSuperheat = 0f;
        heatCoolingElapsed = 0f;
        heatGainCoolingDelayRemaining = 0f;
        burnPhase = BurnPhase.None;
        burnPhaseTimer = 0f;
        burnElapsed = 0f;
        burnStartHeat = 0f;
        InterruptActions();
        if (movesetController != null)
            movesetController.ResetSequence();
        if (playerState != null)
            playerState.ResetForNewGame();
    }

    /// <summary>Zera apenas o estado termico ao trocar para ou desde um moveset Health.</summary>
    public void ResetHeatForResourceMode()
    {
        bool wasThermalStateActive = burnPhase != BurnPhase.None;
        currentHeat = 0f;
        currentSuperheat = 0f;
        heatCoolingElapsed = 0f;
        heatGainCoolingDelayRemaining = 0f;
        burnPhase = BurnPhase.None;
        burnPhaseTimer = 0f;
        burnElapsed = 0f;
        burnStartHeat = 0f;
        InterruptActions();
        if (TryGetComponent(out PlayerMovesetController moveset))
            moveset.ResetSequence();
        if (wasThermalStateActive && playerState != null)
            playerState.ChangeState(PlayerState.State.Normal);
    }

    /// <summary>Restaura apenas os recursos termicos persistentes; estados de Burn sao sempre transitarios.</summary>
    public void RestoreSaveState(float heat, float superheat)
    {
        InterruptActions();
        burnPhase = BurnPhase.None;
        burnPhaseTimer = 0f;
        burnElapsed = 0f;
        burnStartHeat = 0f;
        currentHeat = float.IsNaN(heat) || float.IsInfinity(heat)
            ? 0f : Mathf.Clamp(heat, MinHeat, MaxHeat);
        currentSuperheat = float.IsNaN(superheat) || float.IsInfinity(superheat)
            ? 0f : Mathf.Clamp(superheat, 0f, maxSuperheat);
        heatCoolingElapsed = 0f;
        heatGainCoolingDelayRemaining = 0f;
        if (playerState != null && !playerState.IsDead())
            playerState.ChangeState(PlayerState.State.Normal);
    }


    private void Update()
    {
        if (!IsHeatModeActive)
            return;
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        if (playerState == null || playerState.IsDead()) return;
        if (!IsOutOfCombatForCooling)
        {
            // Sair de combate sempre inicia uma janela completa de retencao.
            // Enquanto houver combate, ela nao pode ser consumida.
            heatGainCoolingDelayRemaining = Mathf.Max(0f,
                passiveHeatCoolingAfterGainDelay);
        }
        else
        {
            heatGainCoolingDelayRemaining = Mathf.Max(0f,
                heatGainCoolingDelayRemaining - Time.deltaTime);
        }

        if (IsBurning)
        {
            UpdateBurn();
            CheckColdDeath();
            return;
        }

        UpdatePassiveHeat();
        UpdatePassiveSuperheat();
        CheckColdDeath();
    }

    private void UpdatePassiveHeat()
    {
        if (!IsOutOfCombatForCooling)
        {
            ResetHeatCoolingRamp();
            return;
        }
        if (currentHeat < 0f)
        {
            ResetHeatCoolingRamp();
            // A agua so resfria Heat positivo: nunca "aquece" o Cold.
            if (combatStatus != null && combatStatus.IsOutOfCombat)
                RecoverColdOutOfCombat();
            return;
        }
        if (currentHeat <= 0f)
        {
            ResetHeatCoolingRamp();
            return;
        }

        if (heatGainCoolingDelayRemaining > 0f)
            return;
        heatCoolingElapsed += Time.deltaTime;
        float previousHeat = currentHeat;
        currentHeat = Mathf.Max(0f, currentHeat - GetPassiveHeatCoolingPerSecond() * Time.deltaTime);
        PlayHeatNotchSounds(previousHeat, currentHeat);
    }

    private void ResetHeatCoolingRamp()
    {
        heatCoolingElapsed = 0f;
    }

    private float GetPassiveHeatCoolingPerSecond()
    {
        float initialRate = Mathf.Max(0f, initialPassiveHeatCoolingPerSecond);
        float finalRate = Mathf.Max(initialRate, finalPassiveHeatCoolingPerSecond);
        if (passiveHeatCoolingRampDuration <= 0f) return finalRate;
        float progress = Mathf.Clamp01(heatCoolingElapsed / passiveHeatCoolingRampDuration);
        float exponent = Mathf.Max(.01f, passiveHeatCoolingRampExponent);
        float exponentialProgress = (Mathf.Exp(exponent * progress) - 1f) / (Mathf.Exp(exponent) - 1f);
        return Mathf.Lerp(initialRate, finalRate, exponentialProgress);
    }

    private void RecoverColdOutOfCombat()
    {
        float recoveryLimit = Mathf.Clamp(MinHeat * coldRecoveryLimitPercent, MinHeat, 0f);
        if (currentHeat >= recoveryLimit)
            return;

        currentHeat = Mathf.Min(recoveryLimit,
            currentHeat + passiveColdRecoveryPerSecond * Time.deltaTime);
    }

    private void UpdatePassiveSuperheat()
    {
        // Superheat so nasce do Heat que excede o maximo. Permanecer com
        // Heat alto nao o gera passivamente.
        float cooling = 0f;
        if (IsOutOfCombatForCooling) cooling += passiveSuperheatCoolingPerSecond;
        if (currentHeat < 0f) cooling += coldSuperheatCoolingPerSecond;
        RemoveSuperheat(cooling * Time.deltaTime);
    }

    public void AddHeat(float amount)
    {
        if (!IsHeatModeActive) return;
        if (amount <= 0f || IsBurning) return;
        float previousHeat = currentHeat;
        float availableHeat = MaxHeat - currentHeat;
        if (amount <= availableHeat)
        {
            currentHeat += amount;
            RegisterHeatGain();
            PlayHeatNotchSounds(previousHeat, currentHeat);
            return;
        }
        currentHeat = MaxHeat;
        AddSuperheat((amount - availableHeat) * excessHeatToSuperheatMultiplier);
        RegisterHeatGain();
        PlayHeatNotchSounds(previousHeat, currentHeat);
    }

    public void RemoveHeat(float amount)
    {
        if (!IsHeatModeActive) return;
        if (amount <= 0f || IsRaging) return;
        float previousHeat = currentHeat;
        currentHeat = Mathf.Max(MinHeat, currentHeat - amount);
        PlayHeatNotchSounds(previousHeat, currentHeat);
        CheckColdDeath();
    }

    public void AddSuperheat(float amount)
    {
        if (!IsHeatModeActive) return;
        if (amount <= 0f || IsBurning) return;
        currentSuperheat = Mathf.Min(maxSuperheat, currentSuperheat + amount);
        if (currentSuperheat >= maxSuperheat) BeginBurn();
    }

    public void RemoveSuperheat(float amount)
    {
        if (!IsHeatModeActive) return;
        if (amount > 0f) currentSuperheat = Mathf.Max(0f, currentSuperheat - amount);
    }

    /// <summary>
    /// Captura a carga do pesado e a gasta imediatamente. O snapshot e usado
    /// pelo golpe mesmo se ele errar ou se o Heat mudar durante a animacao.
    /// </summary>
    public HeavyAttackCharge ConsumeHeatForHeavyAttack()
    {
        return CaptureHeavyAttackCharge(consumeHeat: true);
    }

    /// <summary>Captura a escala Heavy no limite de Heat sem gastar recurso durante a explosao de Burn.</summary>
    public HeavyAttackCharge CaptureBurnExplosionCharge()
    {
        float notches = GetHeavyPowerNotches(MaxHeat);
        return new HeavyAttackCharge(0f, GetHeavyDamageMultiplier(notches),
            GetHeavyPoiseMultiplier(notches));
    }

    private HeavyAttackCharge CaptureHeavyAttackCharge(bool consumeHeat)
    {
        if (!IsHeatModeActive)
            return new HeavyAttackCharge(0f, 1f, 1f);
        if (IsRaging)
        {
            currentHeat = MaxHeat;
            float notches = MaxHeat / HeatPerNotch;
            return new HeavyAttackCharge(0f, GetHeavyDamageMultiplier(notches),
                GetHeavyPoiseMultiplier(notches));
        }
        float availablePositiveHeat = Mathf.Max(0f, currentHeat);
        float consumedNotch = GetHeavyPowerNotches(availablePositiveHeat);
        float currentPowerNotch = GetHeavyPowerNotches(currentHeat);
        float damageMultiplier = GetHeavyDamageMultiplier(currentPowerNotch);
        float poiseMultiplier = GetHeavyPoiseMultiplier(currentPowerNotch);
        // O poder ainda e discreto por notch, mas o gasto tambem: um pesado
        // consome blocos completos de 10 Heat e a fracao extra de capacidade
        // quando a barra estiver cheia. O restante fica para o proximo golpe.
        float heatConsumed = Mathf.Max(0, consumedNotch) * HeatPerNotch;

        if (!consumeHeat || heatConsumed <= 0f)
            return new HeavyAttackCharge(0f, damageMultiplier, poiseMultiplier);

        float previousHeat = currentHeat;
        currentHeat = Mathf.Max(0f, currentHeat - heatConsumed);
        PlayHeatNotchSounds(previousHeat, currentHeat);
        RefillHeatFromSuperheat(heatConsumed);
        return new HeavyAttackCharge(heatConsumed, damageMultiplier, poiseMultiplier);
    }

    private void RefillHeatFromSuperheat(float maximumRefill)
    {
        // Superheat recompõe somente a carga que o pesado acabou de gastar.
        // Assim, tanto o resto fracionario quanto a reserva de Superheat
        // permanecem legiveis e o ataque nao "arredonda para cima" o Heat.
        float refill = Mathf.Min(currentSuperheat, MaxHeat - currentHeat,
            Mathf.Max(0f, maximumRefill));
        if (refill <= 0f)
            return;

        currentSuperheat -= refill;
        currentHeat += refill;
        RegisterHeatGain();
    }

    public int CalculateHeavyDamage(int baseDamage, float damageMultiplier)
    {
        if (baseDamage <= 0)
            return 0;
        if (!IsHeatModeActive)
            return Mathf.RoundToInt(baseDamage * Mathf.Max(0f, damageMultiplier));

        float powerMultiplier = Mathf.Max(0f, damageMultiplier);
        float additiveBonus = powerMultiplier *
            Mathf.Max(0f, heavyDamageAdditivePowerBonus);
        return Mathf.RoundToInt((baseDamage + additiveBonus) *
            powerMultiplier);
    }

    public int CalculateHeavyPoiseDamage(int basePoiseDamage, float poiseMultiplier)
    {
        return basePoiseDamage <= 0 ? 0 : Mathf.RoundToInt(basePoiseDamage * poiseMultiplier);
    }

    private float GetHeavyDamageMultiplier()
    {
        if (!IsHeatModeActive) return 1f;
        return GetHeavyDamageMultiplier(GetHeavyPowerNotches(currentHeat));
    }

    private static float GetHeavyDamageMultiplier(float notch)
    {
        return notch >= 0 ? 1f + notch : Mathf.Max(0f, 1f + notch * .1f);
    }

    private float GetHeavyPoiseMultiplier()
    {
        if (!IsHeatModeActive) return 1f;
        return GetHeavyPoiseMultiplier(GetHeavyPowerNotches(currentHeat));
    }

    private static float GetHeavyPoiseMultiplier(float notch)
    {
        return notch >= 0 ? 1f + notch * .5f : Mathf.Max(0f, 1f + notch * .05f);
    }

    private static int GetSignedNotch(float value, float positiveMaximum, float negativeMagnitude)
    {
        const float fullNotchTolerance = .01f;

        if (value >= positiveMaximum - fullNotchTolerance)
            return Mathf.FloorToInt(positiveMaximum / HeatPerNotch + fullNotchTolerance);
        if (value <= -negativeMagnitude + fullNotchTolerance)
            return -10;

        return value > 0f
            ? Mathf.FloorToInt(value / HeatPerNotch)
            : Mathf.CeilToInt(value / HeatPerNotch);
    }

    // Preserva os marcos completos de 10 Heat. Quando a capacidade maxima
    // termina em meio notch, essa parte tambem concede poder e pode ser gasta.
    private float GetHeavyPowerNotches(float value)
    {
        if (value > 0f && value >= MaxHeat - .01f)
            return MaxHeat / HeatPerNotch;
        return GetSignedNotch(value, MaxHeat, maxColdMagnitude);
    }

    private static int GetPositiveNotch(float value, float maximum, int notchCount)
    {
        const float fullNotchTolerance = .01f;
        if (value >= maximum - fullNotchTolerance)
            return notchCount;

        return Mathf.Clamp(Mathf.FloorToInt(value / HeatPerNotch), 0, notchCount);
    }

    private void BeginBurn()
    {
        burnPhase = BurnPhase.PreExplosion;
        burnPhaseTimer = burnPreExplosionDuration;
        burnElapsed = 0f;
        burnStartHeat = currentHeat;
        if (attackController != null)
            attackController.CancelAttack();
        if (playerMovement != null)
            playerMovement.InterruptForBurn();
        if (playerState != null)
            playerState.ChangeState(PlayerState.State.BurnPreExplosion);
    }

    private void UpdateBurn()
    {
        if (IsRaging) currentHeat = MaxHeat;
        if (burnPhase == BurnPhase.Burning)
        {
            burnElapsed += Time.deltaTime;
            float progress = burningDuration <= 0f
                ? 1f
                : Mathf.Clamp01(burnElapsed / burningDuration);
            float heatProgress = burningHeatDescentCurve != null && burningHeatDescentCurve.length > 0
                ? Mathf.Clamp01(burningHeatDescentCurve.Evaluate(progress))
                : progress;
            currentHeat = Mathf.Lerp(burnStartHeat, BurnFinalHeatTarget, heatProgress);
        }

        burnPhaseTimer -= Time.deltaTime;
        if (burnPhaseTimer > 0f) return;

        switch (burnPhase)
        {
            case BurnPhase.PreExplosion:
                currentSuperheat = 0f;
                burnPhase = BurnPhase.ExplosionStun;
                burnPhaseTimer = burnInitialStunDuration;
                playerState.ChangeState(PlayerState.State.BurnStunned);
                BeginBurnBlast();
                break;
            case BurnPhase.ExplosionStun:
                // O asset da explosao deve terminar antes de liberar ataques normais.
                if (attackController != null && attackController.IsAttacking &&
                    attackController.CurrentAttack?.Definition == burnExplosionAttack)
                    break;
                if (HasRageUnlocked)
                {
                    burnPhase = BurnPhase.Rage;
                    burnPhaseTimer = rageDuration;
                    currentHeat = MaxHeat;
                    currentSuperheat = 0f;
                    playerState.ChangeState(PlayerState.State.Rage);
                }
                else BeginBurning();
                break;
            case BurnPhase.Rage:
                BeginBurning();
                break;
            case BurnPhase.Burning:
                currentHeat = BurnFinalHeatTarget;
                burnPhase = BurnPhase.RecoveryStun;
                burnPhaseTimer = burnFinalStunDuration;
                playerState.ChangeState(PlayerState.State.BurnStunned);
                break;
            case BurnPhase.RecoveryStun:
                currentHeat = BurnFinalHeatTarget;
                currentSuperheat = 0f;
                burnPhase = BurnPhase.None;
                playerState.ChangeState(PlayerState.State.Normal);
                break;
        }
    }

    private void BeginBurning()
    {
        burnPhase = BurnPhase.Burning;
        burnPhaseTimer = burningDuration;
        burnElapsed = 0f;
        burnStartHeat = currentHeat;
        InterruptActions();
        if (TryGetComponent(out PlayerMovesetController moveset))
            moveset.ResetSequence();
        playerState.ChangeState(PlayerState.State.Burning);
    }

    /// <summary>Cancela ataque, buffer de input e movimento em andamento.</summary>
    private void InterruptActions()
    {
        if (attackController != null)
            attackController.CancelAttack();
        if (TryGetComponent(out PlayerInputBuffer inputBuffer))
            inputBuffer.ClearBufferedInputs(ignoreNextFrame: true);
        if (playerMovement != null)
            playerMovement.InterruptForBurn();
    }

    private void BeginBurnBlast()
    {
        if (burnExplosionAttack != null)
            if (attackController != null)
                attackController.TryStartExternalAttack(burnExplosionAttack);
    }

    public void DebugSetStatus(bool maximize)
    {
        if (!Debug.isDebugBuild || !IsHeatModeActive) return;
        InterruptActions();
        burnPhase = BurnPhase.None;
        burnPhaseTimer = 0f;
        burnElapsed = 0f;
        burnStartHeat = 0f;
        rageGrants.Clear();
        rageDuration = 0f;
        currentHeat = maximize ? MaxHeat : 0f;
        currentSuperheat = maximize ? MaxSuperheat : 0f;
        heatCoolingElapsed = 0f;
        heatGainCoolingDelayRemaining = 0f;
    }

    public void DebugAdjustHeat(float amount)
    {
        if (!Debug.isDebugBuild || !IsHeatModeActive || IsBurning) return;
        currentHeat = Mathf.Clamp(currentHeat + amount, MinHeat, MaxHeat);
        if (amount > 0f) RegisterHeatGain();
        ResetHeatCoolingRamp();
    }

    private void RegisterHeatGain()
    {
        heatGainCoolingDelayRemaining = Mathf.Max(0f,
            passiveHeatCoolingAfterGainDelay);
        // Uma nova carga deve recomecar somente a rampa de intensidade. O
        // tempo ja passado fora de combate continua valido: os dois atrasos
        // sao regras independentes.
        heatCoolingElapsed = 0f;
    }

    private void PlayHeatNotchSounds(float previousHeat, float nextHeat)
    {
        int previousNotch = Mathf.FloorToInt(Mathf.Abs(previousHeat) / HeatPerNotch + .0001f);
        int nextNotch = Mathf.FloorToInt(Mathf.Abs(nextHeat) / HeatPerNotch + .0001f);
        if (previousNotch == nextNotch)
            return;

        int direction = nextNotch > previousNotch ? 1 : -1;
        for (int notch = previousNotch + direction; direction > 0
            ? notch <= nextNotch : notch >= nextNotch; notch += direction)
        {
            // O notch zero e apenas a transicao entre Heat positivo e Cold.
            if (notch == 0)
                continue;
            // Ao perder um notch, a nota representa o nivel que acabou de ser
            // deixado. Assim a descida de 2x para 1x toca a nota de 2x.
            int soundedNotch = direction < 0 ? Mathf.Abs(notch) + 1 : Mathf.Abs(notch);
            SfxAudio.PlayHeatNotch(direction > 0,
                soundedNotch, transform.position);
        }
    }

    private void CheckColdDeath()
    {
        if (playerState != null && !playerState.IsDead() &&
            (playerHealth == null || !playerHealth.IsGodMode) &&
            currentHeat <= MinHeat)
            playerState.ChangeState(PlayerState.State.Dead);
    }
}

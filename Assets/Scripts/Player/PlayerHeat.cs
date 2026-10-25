using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
    [Header("Debug")]
    [Tooltip("Permite usar F4 para remover Heat e F5 para adicionar Heat durante testes. Esses atalhos alteram apenas o Heat e nao geram Superheat.")]
    [SerializeField] private bool debugHeatInputEnabled = true;
    [Tooltip("Quantidade de Heat adicionada ou removida por segundo pelos atalhos de debug.")]
    [Min(0f)] [SerializeField] private float debugHeatPerSecond = 35f;

    private float currentHeat;
    private float currentSuperheat;
    private float heatCoolingElapsed;
    private float heatGainCoolingDelayRemaining;
    private float debugHeatAccumulator;
    private BurnPhase burnPhase;
    private float burnPhaseTimer;
    private float burnElapsed;
    private float burnStartHeat;

    private PlayerState playerState;
    private PlayerCombatStatus combatStatus;
    private PlayerAttackController attackController;
    private PlayerMovement playerMovement;
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
    public int CurrentSuperheatNotch => GetPositiveNotch(currentSuperheat, maxSuperheat, 5);
    public bool IsBurning => burnPhase != BurnPhase.None;
    public bool IsRaging => burnPhase == BurnPhase.Rage;
    public float RageTimeRemaining => IsRaging ? Mathf.Max(0f, burnPhaseTimer) : 0f;
    public bool HasRageUnlocked => rageDuration > 0f;
    public float BurnMovementSpeedMultiplier => burnMovementSpeedMultiplier;
    public float HeavyDamageMultiplier => GetHeavyDamageMultiplier();
    public float HeavyPoiseMultiplier => GetHeavyPoiseMultiplier();
    private float BurnFinalHeatTarget => -maxColdMagnitude * burnFinalColdPercent;


    private void Start()
    {
        PlayerProgression.GetOrAdd(gameObject).Initialize();
        playerState = GetComponent<PlayerState>();
        combatStatus = GetComponent<PlayerCombatStatus>();
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
        debugHeatAccumulator = 0f;
        burnPhase = BurnPhase.None;
        burnPhaseTimer = 0f;
        burnElapsed = 0f;
        burnStartHeat = 0f;
        rageGrants.Clear();
        rageDuration = 0f;
        attackController?.CancelAttack();
        playerMovement?.InterruptForBurn();
        GetComponent<PlayerMovesetController>()?.ResetSequence();
        GetComponent<PlayerInputBuffer>()?.ClearBufferedInputs(ignoreNextFrame: true);
        playerState?.ResetForNewGame();
    }


    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        if (playerState == null || playerState.IsDead()) return;
        UpdateDebugHeatInput();
        if (combatStatus == null || !combatStatus.IsOutOfCombat)
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
        if (combatStatus == null || !combatStatus.IsOutOfCombat)
        {
            ResetHeatCoolingRamp();
            return;
        }
        if (currentHeat < 0f)
        {
            ResetHeatCoolingRamp();
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
        currentHeat = Mathf.Max(0f, currentHeat - GetPassiveHeatCoolingPerSecond() * Time.deltaTime);
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
        currentHeat = Mathf.Min(recoveryLimit, currentHeat + passiveColdRecoveryPerSecond * Time.deltaTime);
    }

    private void UpdatePassiveSuperheat()
    {
        // Superheat so nasce do Heat que excede o maximo. Permanecer com
        // Heat alto nao o gera passivamente.
        float cooling = 0f;
        if (combatStatus != null && combatStatus.IsOutOfCombat) cooling += passiveSuperheatCoolingPerSecond;
        if (currentHeat < 0f) cooling += coldSuperheatCoolingPerSecond;
        RemoveSuperheat(cooling * Time.deltaTime);
    }

    public void AddHeat(float amount)
    {
        if (amount <= 0f || IsBurning) return;
        float availableHeat = MaxHeat - currentHeat;
        if (amount <= availableHeat)
        {
            currentHeat += amount;
            RegisterHeatGain();
            return;
        }
        currentHeat = MaxHeat;
        AddSuperheat((amount - availableHeat) * excessHeatToSuperheatMultiplier);
        RegisterHeatGain();
    }

    public void RemoveHeat(float amount)
    {
        if (amount <= 0f || IsRaging) return;
        currentHeat = Mathf.Max(MinHeat, currentHeat - amount);
        CheckColdDeath();
    }

    public void AddSuperheat(float amount)
    {
        if (amount <= 0f || IsBurning) return;
        currentSuperheat = Mathf.Min(maxSuperheat, currentSuperheat + amount);
        if (currentSuperheat >= maxSuperheat) BeginBurn();
    }

    public void RemoveSuperheat(float amount)
    {
        if (amount > 0f) currentSuperheat = Mathf.Max(0f, currentSuperheat - amount);
    }

    /// <summary>
    /// Atalho de teste usado por F8. Encher Superheat dispara Burn da mesma
    /// forma que ocorreria no gameplay normal.
    /// </summary>
    public void FillHeatAndSuperheatForDebug()
    {
        if (IsBurning)
            return;

        currentHeat = MaxHeat;
        currentSuperheat = maxSuperheat;
        BeginBurn();
    }

    /// <summary>
    /// Captura a carga do pesado e a gasta imediatamente. O snapshot e usado
    /// pelo golpe mesmo se ele errar ou se o Heat mudar durante a animacao.
    /// </summary>
    public HeavyAttackCharge ConsumeHeatForHeavyAttack()
    {
        if (IsRaging)
        {
            currentHeat = MaxHeat;
            float notches = MaxHeat / HeatPerNotch;
            return new HeavyAttackCharge(0f, GetHeavyDamageMultiplier(notches),
                GetHeavyPoiseMultiplier(notches));
        }
        float availablePositiveHeat = Mathf.Max(0f, currentHeat);
        float consumedNotch = GetHeavyPowerNotches(availablePositiveHeat);
        float damageMultiplier = GetHeavyDamageMultiplier(consumedNotch);
        float poiseMultiplier = GetHeavyPoiseMultiplier(consumedNotch);
        // O poder ainda e discreto por notch, mas o gasto tambem: um pesado
        // consome blocos completos de 10 Heat e a fracao extra de capacidade
        // quando a barra estiver cheia. O restante fica para o proximo golpe.
        float heatConsumed = Mathf.Max(0, consumedNotch) * HeatPerNotch;

        if (heatConsumed <= 0f)
            return new HeavyAttackCharge(0f, damageMultiplier, poiseMultiplier);

        currentHeat = Mathf.Max(0f, currentHeat - heatConsumed);
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
        return GetHeavyDamageMultiplier(GetHeavyPowerNotches(currentHeat));
    }

    private static float GetHeavyDamageMultiplier(float notch)
    {
        return notch >= 0 ? 1f + notch : Mathf.Max(0f, 1f + notch * .1f);
    }

    private float GetHeavyPoiseMultiplier()
    {
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
        attackController?.CancelAttack();
        playerMovement?.InterruptForBurn();
        playerState?.ChangeState(PlayerState.State.BurnPreExplosion);
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
        attackController?.CancelAttack();
        GetComponent<PlayerMovesetController>()?.ResetSequence();
        GetComponent<PlayerInputBuffer>()?.ClearBufferedInputs(ignoreNextFrame: true);
        playerMovement?.InterruptForBurn();
        playerState.ChangeState(PlayerState.State.Burning);
    }

    private void BeginBurnBlast()
    {
        if (burnExplosionAttack != null)
            attackController?.TryStartExternalAttack(burnExplosionAttack);
    }

    private void UpdateDebugHeatInput()
    {
        if (!debugHeatInputEnabled || IsBurning) return;
        float direction = 0f;
        if (GameInput.DebugHeatDownHeld) direction -= 1f;
        if (GameInput.DebugHeatUpHeld) direction += 1f;
        if (Mathf.Approximately(direction, 0f))
        {
            debugHeatAccumulator = 0f;
            return;
        }
        debugHeatAccumulator += debugHeatPerSecond * Time.deltaTime;
        float amount = debugHeatAccumulator;
        debugHeatAccumulator = 0f;
        // O controle de Heat isolado nao usa AddHeat: ultrapassar Max Heat
        // por F5 nao deve gerar Superheat nem iniciar Burn. F8 testa esse
        // fluxo completo separadamente.
        currentHeat = Mathf.Clamp(
            currentHeat + amount * direction,
            MinHeat,
            MaxHeat
        );
        if (direction > 0f)
            RegisterHeatGain();
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

    private void CheckColdDeath()
    {
        if (playerState != null && !playerState.IsDead() && currentHeat <= MinHeat)
            playerState.ChangeState(PlayerState.State.Dead);
    }
}

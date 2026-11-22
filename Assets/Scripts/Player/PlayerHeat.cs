using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerHeat : MonoBehaviour
{
    public const float HeatPerNotch = 10f;

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

    [Header("Heavy Attack Power")]
    [Tooltip("Aumento do multiplicador de dano por notch de Heat. Afeta ataques pesados e o dano recebido; por exemplo, 0,5 resulta em 2x com dois notches.")]
    [Min(0f)] [SerializeField] private float damageMultiplierPerNotch = .5f;
    [Tooltip("Quantidade de notches completos exigidos e consumidos ao iniciar um ataque pesado.")]
    [Min(1)] [SerializeField] private int heavyAttackNotchCost = 3;
    public float HeavyAttackHeatCost => Mathf.Max(1, heavyAttackNotchCost) * HeatPerNotch;

    [Header("Light Attack Heat")]
    [Tooltip("Heat gerado por cada ponto de dano realmente retirado da vida do inimigo por um ataque leve marcado para gerar Heat.")]
    [Min(0f)] public float heatGeneratedPerDamageDealt = .3f;

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

    [Header("Burn Blast")]
    [Tooltip("Asset de ataque executado automaticamente quando o Burn explode. Controla dano, Poise, Strikes, hitboxes, falloff e efeitos como qualquer outro ataque do player.")]
    [SerializeField] private PlayerAttackDefinition burnExplosionAttack;

    private float currentHeat;
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
    public float InitialMaxHeat => Mathf.Max(1f, maxHeat);
    public float MaxHeat => Mathf.Max(1f, maxHeat + chargeMaxHeatBonus);
    public float MinHeat => 0f;
    // A pequena tolerancia evita que 100 armazenado como 99.99999 por
    // operacoes de ponto flutuante deixe o ultimo quadrado apagado.
    public int CurrentHeatNotch => GetHeatNotch(currentHeat, MaxHeat);
    public bool IsBurning => burnPhase != BurnPhase.None;
    public bool IsRaging => burnPhase == BurnPhase.Rage;
    public float RageTimeRemaining => IsRaging ? Mathf.Max(0f, burnPhaseTimer) : 0f;
    public bool HasRageUnlocked => rageDuration > 0f;
    public float BurnMovementSpeedMultiplier => burnMovementSpeedMultiplier;
    public float HeavyDamageMultiplier => GetHeavyDamageMultiplier();
    public float HeavyPoiseMultiplier => GetHeavyPoiseMultiplier();
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

    /// <summary>Retorna Heat e fases de Burn/Rage ao estado inicial de uma nova campanha.</summary>
    public void ResetForNewGame()
    {
        currentHeat = 0f;
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
    public void RestoreSaveState(float heat)
    {
        InterruptActions();
        burnPhase = BurnPhase.None;
        burnPhaseTimer = 0f;
        burnElapsed = 0f;
        burnStartHeat = 0f;
        currentHeat = float.IsNaN(heat) || float.IsInfinity(heat)
            ? 0f : Mathf.Clamp(heat, MinHeat, MaxHeat);
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
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame &&
            currentHeat >= MaxHeat && playerState.IsNormal() &&
            burnExplosionAttack != null && attackController != null &&
            attackController.CanStartAttack(burnExplosionAttack))
        {
            BeginBurn();
            return;
        }
        UpdatePassiveHeat();
    }

    private void UpdatePassiveHeat()
    {
        if (!IsOutOfCombatForCooling)
        {
            ResetHeatCoolingRamp();
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
        float heatConsumed = Mathf.Min(HeavyAttackHeatCost, availablePositiveHeat);

        if (!consumeHeat || heatConsumed <= 0f)
            return new HeavyAttackCharge(0f, damageMultiplier, poiseMultiplier);

        float previousHeat = currentHeat;
        currentHeat = Mathf.Max(0f, currentHeat - heatConsumed);
        PlayHeatNotchSounds(previousHeat, currentHeat);

        return new HeavyAttackCharge(heatConsumed, damageMultiplier, poiseMultiplier);
    }

    public int CalculateHeavyDamage(int baseDamage, float damageMultiplier)
    {
        if (baseDamage <= 0)
            return 0;
        if (!IsHeatModeActive)
            return Mathf.RoundToInt(baseDamage * Mathf.Max(0f, damageMultiplier));

        return Mathf.RoundToInt(baseDamage * Mathf.Max(0f, damageMultiplier));
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

    private float GetHeavyDamageMultiplier(float notch)
    {
        return 1f + Mathf.Max(0f, notch) * Mathf.Max(0f, damageMultiplierPerNotch);
    }

    private float GetHeavyPoiseMultiplier()
    {
        if (!IsHeatModeActive) return 1f;
        return GetHeavyPoiseMultiplier(GetHeavyPowerNotches(currentHeat));
    }

    private static float GetHeavyPoiseMultiplier(float notch)
    {
        return 1f + Mathf.Max(0f, notch) * .5f;
    }

    private static int GetHeatNotch(float value, float maximum)
    {
        const float tolerance = .01f;
        if (value >= maximum - tolerance)
            return Mathf.FloorToInt(maximum / HeatPerNotch + tolerance);
        return Mathf.FloorToInt(Mathf.Max(0f, value) / HeatPerNotch);
    }

    // Preserva os marcos completos de 10 Heat. Quando a capacidade maxima
    // termina em meio notch, essa parte tambem concede poder e pode ser gasta.
    private float GetHeavyPowerNotches(float value)
    {
        if (value > 0f && value >= MaxHeat - .01f)
            return MaxHeat / HeatPerNotch;
        return GetHeatNotch(value, MaxHeat);
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
        if (movesetController != null)
            movesetController.ResetSequence();
        InterruptActions();
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
            currentHeat = Mathf.Min(currentHeat,
                Mathf.Lerp(burnStartHeat, 0f, heatProgress));
        }

        burnPhaseTimer -= Time.deltaTime;
        if (burnPhaseTimer > 0f) return;

        switch (burnPhase)
        {
            case BurnPhase.PreExplosion:
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
                    playerState.ChangeState(PlayerState.State.Rage);
                }
                else BeginBurning();
                break;
            case BurnPhase.Rage:
                BeginBurning();
                break;
            case BurnPhase.Burning:
                currentHeat = 0f;
                burnPhase = BurnPhase.RecoveryStun;
                burnPhaseTimer = burnFinalStunDuration;
                playerState.ChangeState(PlayerState.State.BurnStunned);
                break;
            case BurnPhase.RecoveryStun:
                currentHeat = 0f;
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
        heatCoolingElapsed = 0f;
        heatGainCoolingDelayRemaining = 0f;
    }

    public void DebugAdjustHeat(float amount)
    {
        if (!Debug.isDebugBuild || !IsHeatModeActive || IsBurning) return;
        float previousHeat = currentHeat;
        currentHeat = Mathf.Clamp(currentHeat + amount, MinHeat, MaxHeat);
        PlayHeatNotchSounds(previousHeat, currentHeat);
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
        int previousNotch = Mathf.FloorToInt(Mathf.Max(0f, previousHeat) / HeatPerNotch + .0001f);
        int nextNotch = Mathf.FloorToInt(Mathf.Max(0f, nextHeat) / HeatPerNotch + .0001f);
        if (previousNotch == nextNotch)
            return;

        SfxAudio.PlayHeatNotch(previousNotch, nextNotch, transform.position);
    }

}

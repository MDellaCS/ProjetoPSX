using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

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

    private enum BurnPhase { None, InitialStun, Burning, FinalStun }

    [Header("Heat Notches")]
    [Tooltip("Limite positivo de Heat. De 0 ate este valor existem 10 notches; cada notch aumenta o poder dos pesados.")]
    [Min(1f)] public float maxHeat = 100f;
    [FormerlySerializedAs("minHeatMagnitude")]
    [Tooltip("Magnitude maxima de Cold. O limite real de Heat e o valor negativo deste campo; alcanca-lo mata o player.")]
    [Min(1f)] public float maxColdMagnitude = 100f;

    [Header("Superheat Notches")]
    [Tooltip("Capacidade maxima de Superheat. Ao encher, inicia o estado Burn.")]
    [Min(1f)] public float maxSuperheat = 50f;
    [Tooltip("Parte do Heat que excede 100 convertida em Superheat.")]
    [Min(0f)] public float excessHeatToSuperheatMultiplier = .5f;

    [Header("Passive Heat Cooling")]
    [Tooltip("Tempo fora de combate antes de o resfriamento passivo de Heat positivo comecar.")]
    [Min(0f)] public float passiveHeatCoolingStartDelay = 5f;
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
    [Tooltip("Duracao do stun completo ao iniciar Burn por Superheat cheio.")]
    [Min(0f)] public float burnInitialStunDuration = 1.25f;
    [Tooltip("Duracao do estado Burning, em que o player so pode andar lentamente.")]
    [Min(0f)] public float burningDuration = 4f;
    [Tooltip("Duracao do stun completo aplicado ao fim de Burning.")]
    [Min(0f)] public float burnFinalStunDuration = 1.25f;
    [Tooltip("Multiplicador da velocidade de movimento durante Burning.")]
    [Range(0f, 1f)] public float burnMovementSpeedMultiplier = .3f;
    [Tooltip("Heat final ao terminar Burn. O Superheat sempre termina em 0.")]
    [Range(-100f, 0f)] public float burnFinalHeat = -70f;

    [Header("Debug")]
    [Tooltip("Permite usar F4 para remover Heat e F5 para adicionar Heat durante testes.")]
    [SerializeField] private bool debugHeatInputEnabled = true;
    [Tooltip("Quantidade de Heat adicionada ou removida por segundo pelos atalhos de debug.")]
    [Min(0f)] [SerializeField] private float debugHeatPerSecond = 15f;
    [SerializeField, HideInInspector] private int heatReworkVersion;

    private float currentHeat;
    private float currentSuperheat;
    private float heatCoolingOutOfCombatElapsed;
    private float heatCoolingElapsed;
    private float debugHeatAccumulator;
    private BurnPhase burnPhase;
    private float burnPhaseTimer;
    private float burnElapsed;
    private float burnTotalDuration;
    private float burnStartHeat;
    private float burnStartSuperheat;

    private PlayerState playerState;
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;

    public float CurrentHeat => currentHeat;
    public float CurrentSuperheat => currentSuperheat;
    public float MaxHeat => maxHeat;
    public float MinHeat => -maxColdMagnitude;
    public float MaxSuperheat => maxSuperheat;
    // A pequena tolerancia evita que 100 armazenado como 99.99999 por
    // operacoes de ponto flutuante deixe o ultimo quadrado apagado.
    public int CurrentHeatNotch => GetSignedNotch(currentHeat, maxHeat, maxColdMagnitude);
    public int CurrentSuperheatNotch => GetPositiveNotch(currentSuperheat, maxSuperheat, 5);
    public bool IsBurning => burnPhase != BurnPhase.None;
    public float BurnMovementSpeedMultiplier => burnMovementSpeedMultiplier;
    public float HeavyDamageMultiplier => GetHeavyDamageMultiplier();
    public float HeavyPoiseMultiplier => GetHeavyPoiseMultiplier();

    private void Awake() => ApplyHeatReworkDefaults();
    private void OnValidate() => ApplyHeatReworkDefaults();

    private void Start()
    {
        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (playerState == null || playerState.IsDead()) return;
        UpdateDebugHeatInput();

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

    private void ApplyHeatReworkDefaults()
    {
        if (heatReworkVersion >= 1) return;
        maxHeat = 100f;
        maxColdMagnitude = 100f;
        maxSuperheat = 50f;
        excessHeatToSuperheatMultiplier = .5f;
        passiveHeatCoolingStartDelay = 5f;
        initialPassiveHeatCoolingPerSecond = .2f;
        finalPassiveHeatCoolingPerSecond = 15f;
        passiveHeatCoolingRampDuration = 16f;
        passiveHeatCoolingRampExponent = 1.4f;
        passiveColdRecoveryPerSecond = 2f;
        coldRecoveryLimitPercent = .9f;
        passiveSuperheatCoolingPerSecond = 1f;
        coldSuperheatCoolingPerSecond = 4f;
        debugHeatPerSecond = 30f;
        heatReworkVersion = 1;
    }

    private void UpdatePassiveHeat()
    {
        if (playerCombat == null || !playerCombat.IsOutOfCombat)
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

        heatCoolingOutOfCombatElapsed += Time.deltaTime;
        if (heatCoolingOutOfCombatElapsed < passiveHeatCoolingStartDelay) return;
        heatCoolingElapsed += Time.deltaTime;
        currentHeat = Mathf.Max(0f, currentHeat - GetPassiveHeatCoolingPerSecond() * Time.deltaTime);
    }

    private void ResetHeatCoolingRamp()
    {
        heatCoolingOutOfCombatElapsed = 0f;
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
        if (playerCombat != null && playerCombat.IsOutOfCombat) cooling += passiveSuperheatCoolingPerSecond;
        if (currentHeat < 0f) cooling += coldSuperheatCoolingPerSecond;
        RemoveSuperheat(cooling * Time.deltaTime);
    }

    public void AddHeat(float amount)
    {
        if (amount <= 0f || IsBurning) return;
        float availableHeat = maxHeat - currentHeat;
        if (amount <= availableHeat)
        {
            currentHeat += amount;
            return;
        }
        currentHeat = maxHeat;
        AddSuperheat((amount - availableHeat) * excessHeatToSuperheatMultiplier);
    }

    public void RemoveHeat(float amount)
    {
        if (amount <= 0f) return;
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
    /// Captura a carga do pesado e a gasta imediatamente. O snapshot e usado
    /// pelo golpe mesmo se ele errar ou se o Heat mudar durante a animacao.
    /// </summary>
    public HeavyAttackCharge ConsumeHeatForHeavyAttack()
    {
        float heatConsumed = Mathf.Max(0f, currentHeat);
        int consumedNotch = GetSignedNotch(heatConsumed, maxHeat, maxColdMagnitude);
        float damageMultiplier = GetHeavyDamageMultiplier(consumedNotch);
        float poiseMultiplier = GetHeavyPoiseMultiplier(consumedNotch);

        if (heatConsumed <= 0f)
            return new HeavyAttackCharge(0f, damageMultiplier, poiseMultiplier);

        currentHeat = 0f;
        RefillHeatFromSuperheat();
        return new HeavyAttackCharge(heatConsumed, damageMultiplier, poiseMultiplier);
    }

    private void RefillHeatFromSuperheat()
    {
        float refill = Mathf.Min(currentSuperheat, maxHeat - currentHeat);
        if (refill <= 0f)
            return;

        currentSuperheat -= refill;
        currentHeat += refill;
    }

    public int CalculateHeavyDamage(int baseDamage, float damageMultiplier)
    {
        return baseDamage <= 0 ? 0 : Mathf.RoundToInt(baseDamage * damageMultiplier);
    }

    public int CalculateHeavyPoiseDamage(int basePoiseDamage, float poiseMultiplier)
    {
        return basePoiseDamage <= 0 ? 0 : Mathf.RoundToInt(basePoiseDamage * poiseMultiplier);
    }

    private float GetHeavyDamageMultiplier()
    {
        return GetHeavyDamageMultiplier(CurrentHeatNotch);
    }

    private static float GetHeavyDamageMultiplier(int notch)
    {
        return notch >= 0 ? 1f + notch : Mathf.Max(0f, 1f + notch * .1f);
    }

    private float GetHeavyPoiseMultiplier()
    {
        return GetHeavyPoiseMultiplier(CurrentHeatNotch);
    }

    private static float GetHeavyPoiseMultiplier(int notch)
    {
        return notch >= 0 ? 1f + notch * .5f : Mathf.Max(0f, 1f + notch * .05f);
    }

    private static int GetSignedNotch(float value, float positiveMaximum, float negativeMagnitude)
    {
        const float fullNotchTolerance = .01f;

        if (value >= positiveMaximum - fullNotchTolerance)
            return 10;
        if (value <= -negativeMagnitude + fullNotchTolerance)
            return -10;

        return value > 0f
            ? Mathf.FloorToInt(value / HeatPerNotch)
            : Mathf.CeilToInt(value / HeatPerNotch);
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
        burnPhase = BurnPhase.InitialStun;
        burnPhaseTimer = burnInitialStunDuration;
        burnElapsed = 0f;
        burnTotalDuration = burnInitialStunDuration + burningDuration + burnFinalStunDuration;
        burnStartHeat = currentHeat;
        burnStartSuperheat = currentSuperheat;
        playerCombat?.CancelAttack();
        playerMovement?.InterruptForBurn();
        playerState?.ChangeState(PlayerState.State.BurnStunned);
    }

    private void UpdateBurn()
    {
        burnElapsed += Time.deltaTime;
        float progress = burnTotalDuration <= 0f ? 1f : Mathf.Clamp01(burnElapsed / burnTotalDuration);
        currentHeat = Mathf.Lerp(burnStartHeat, burnFinalHeat, progress);
        currentSuperheat = Mathf.Lerp(burnStartSuperheat, 0f, progress);
        burnPhaseTimer -= Time.deltaTime;
        if (burnPhaseTimer > 0f) return;

        switch (burnPhase)
        {
            case BurnPhase.InitialStun:
                burnPhase = BurnPhase.Burning;
                burnPhaseTimer = burningDuration;
                playerState.ChangeState(PlayerState.State.Burning);
                break;
            case BurnPhase.Burning:
                burnPhase = BurnPhase.FinalStun;
                burnPhaseTimer = burnFinalStunDuration;
                playerState.ChangeState(PlayerState.State.BurnStunned);
                break;
            case BurnPhase.FinalStun:
                currentHeat = burnFinalHeat;
                currentSuperheat = 0f;
                burnPhase = BurnPhase.None;
                playerState.ChangeState(PlayerState.State.Normal);
                break;
        }
    }

    private void UpdateDebugHeatInput()
    {
        if (!debugHeatInputEnabled || Keyboard.current == null || IsBurning) return;
        float direction = 0f;
        if (Keyboard.current.f4Key.isPressed) direction -= 1f;
        if (Keyboard.current.f5Key.isPressed) direction += 1f;
        if (Mathf.Approximately(direction, 0f))
        {
            debugHeatAccumulator = 0f;
            return;
        }
        debugHeatAccumulator += debugHeatPerSecond * Time.deltaTime;
        float amount = debugHeatAccumulator;
        debugHeatAccumulator = 0f;
        if (direction > 0f) AddHeat(amount); else RemoveHeat(amount);
    }

    private void CheckColdDeath()
    {
        if (playerState != null && !playerState.IsDead() && currentHeat <= MinHeat)
            playerState.ChangeState(PlayerState.State.Dead);
    }
}

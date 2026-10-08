using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeat : MonoBehaviour
{
    [Header("Heat")]
    public int maxHeat = 10000;
    public int minHeat = -10000;

    [Header("Superheat")]
    public int maxSuperheat = 10000;

    [Min(0f)]
    public float excessHeatToSuperheatMultiplier = 0.75f;

    // Valor fixo, independente de maxHeat.
    public int superheatThreshold = 8500;

    [Header("Passive Heat Cooling")]
    public int passiveHeatCoolingPerSecond = 200;

    [Header("Passive Cold Recovery")]
    [Min(0)] public int passiveColdRecoveryPerSecond = 200;
    [Range(0f, 1f)] public float coldRecoveryLimitPercent = 0.75f;

    [Header("Passive Superheat Generation")]

    // Taxa de referência em Heat = 10000. Acima de 10000, a geração pode ultrapassar este valor.
    public int maxPassiveSuperheatPerSecond = 300;

    [Header("Superheat Scaling Reference")]
    [Min(1f)]
    public float superheatFullRateReference = 10000f;

    [Header("Passive Superheat Cooling")]
    public int passiveSuperheatCoolingPerSecond = 100;
    public int coldSuperheatCoolingPerSecond = 400;

    [Header("Cold Heavy Penalty")]
    [Range(0f, 1f)]
    public float minimumHeavyHeatGeneration = 0.2f;

    [Header("Heavy Power Scaling")]
    [Range(0f, 1f)]
    public float minimumHeavyDamageMultiplier = 0.2f;

    public float maximumHeavyDamageMultiplier = 5f;

    [Header("Heavy Additive Damage")]
    [Min(0)]
    public int maximumHeavyBonusDamage = 150;

    [Header("Heavy Additive Poise")]
    [Min(0)]
    public int maximumHeavyBonusPoise = 60;

    [Header("Heat Power Curve")]

    // Referência de balanceamento, NÃO limite de Heat.
    [Min(1f)]
    public float heatPowerReference = 10000f;

    // Controla a agressividade da curva exponencial.
    [Min(0.01f)]
    public float heatPowerExponent = 1.5f;

    [Header("Heavy Heat Generation Limits")]
    [Range(0f, 1f)]
    [SerializeField]
    private float minimumHeatPerHitPercent = 0.02f;

    [Range(0f, 1f)]
    [SerializeField]
    private float maximumHeatPerHitPercent = 0.20f;

    [Header("Debug")]
    [SerializeField] private bool debugHeatInputEnabled = true;
    [Min(1)] [SerializeField] private int debugHeatPerSecond = 1000;

    private int currentHeat = 0;
    private int currentSuperheat = 0;

    // Guardam frações de tempo para trabalhar com inteiros.
    private float heatCoolingAccumulator;
    private float superheatAccumulator;
    private float debugHeatAccumulator;

    private PlayerState playerState;
    private PlayerCombat playerCombat;

    public int CurrentHeat => currentHeat;
    public int CurrentSuperheat => currentSuperheat;
    public int MaxHeat => maxHeat;
    public int MinHeat => minHeat;
    public int MaxSuperheat => maxSuperheat;

    void Start()
    {
        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    void Update()
    {
        if (playerState.IsDead())
            return;

        UpdateDebugHeatInput();
        UpdatePassiveHeat();
        UpdatePassiveSuperheat();

        CheckDeath();
    }

    void UpdatePassiveHeat()
    {
        if (playerCombat == null || !playerCombat.IsOutOfCombat)
        {
            heatCoolingAccumulator = 0f;
            return;
        }

        // Cold só se recupera fora de combate.
        if (currentHeat < 0)
        {
            RecoverColdOutOfCombat();
            return;
        }

        if (currentHeat == 0)
        {
            heatCoolingAccumulator = 0f;
            return;
        }

        heatCoolingAccumulator +=
            passiveHeatCoolingPerSecond * Time.deltaTime;

        int amount = Mathf.FloorToInt(heatCoolingAccumulator);

        if (amount <= 0)
            return;

        currentHeat -= amount;

        // Heat passivo nunca atravessa zero.
        if (currentHeat < 0)
            currentHeat = 0;

        heatCoolingAccumulator -= amount;
    }

    void RecoverColdOutOfCombat()
    {
        int recoveryLimit = Mathf.Clamp(
            Mathf.RoundToInt(minHeat * coldRecoveryLimitPercent),
            minHeat,
            0
        );

        if (currentHeat >= recoveryLimit)
        {
            heatCoolingAccumulator = 0f;
            return;
        }

        heatCoolingAccumulator +=
            passiveColdRecoveryPerSecond * Time.deltaTime;

        int amount = Mathf.FloorToInt(heatCoolingAccumulator);

        if (amount <= 0)
            return;

        currentHeat = Mathf.Min(
            recoveryLimit,
            currentHeat + amount
        );

        heatCoolingAccumulator -= amount;
    }

    void UpdateDebugHeatInput()
    {
        if (!debugHeatInputEnabled || Keyboard.current == null)
            return;

        int direction = 0;

        if (Keyboard.current.f4Key.isPressed)
            direction++;

        if (Keyboard.current.f5Key.isPressed)
            direction--;

        if (direction == 0)
        {
            debugHeatAccumulator = 0f;
            return;
        }

        debugHeatAccumulator +=
            debugHeatPerSecond * Time.deltaTime;

        int amount = Mathf.FloorToInt(debugHeatAccumulator);

        if (amount <= 0)
            return;

        if (direction > 0)
        {
            AddHeat(amount);
        }
        else
        {
            RemoveHeat(amount);
        }

        debugHeatAccumulator -= amount;
    }

    void UpdatePassiveSuperheat()
    {
        // ====================================
        // ZONA QUENTE
        // ====================================

        if (currentHeat > superheatThreshold)
        {
            // A referência é fixa e independente de maxHeat.
            // Em 10000 Heat, a taxa é a configurada.
            // Acima de 10000 Heat, a taxa será maior.
            float referenceRange = Mathf.Max(
                1f,
                superheatFullRateReference - superheatThreshold
            );

            float heatAboveThreshold =
                currentHeat - superheatThreshold;

            float heatIntensity =
                heatAboveThreshold / referenceRange;

            float generationPerSecond =
                maxPassiveSuperheatPerSecond * heatIntensity;

            superheatAccumulator +=
                generationPerSecond * Time.deltaTime;

            int amount = Mathf.FloorToInt(
                superheatAccumulator
            );

            if (amount > 0)
            {
                AddSuperheat(amount);
                superheatAccumulator -= amount;
            }

            return;
        }

        int coolingPerSecond = 0;

        if (playerCombat != null && playerCombat.IsOutOfCombat)
        {
            coolingPerSecond +=
                passiveSuperheatCoolingPerSecond;
        }

        if (currentHeat < 0)
        {
            coolingPerSecond +=
                coldSuperheatCoolingPerSecond;
        }

        if (coolingPerSecond <= 0)
        {
            superheatAccumulator = 0f;
            return;
        }

        superheatAccumulator +=
            coolingPerSecond * Time.deltaTime;

        int coolingAmount = Mathf.FloorToInt(
            superheatAccumulator
        );

        if (coolingAmount <= 0)
            return;

        RemoveSuperheat(coolingAmount);

        superheatAccumulator -= coolingAmount;
    }

    public void AddHeat(int amount)
    {
        if (amount <= 0)
            return;

        // Heat negativo também recebe Heat normalmente.
        int availableHeat = maxHeat - currentHeat;

        if (amount <= availableHeat)
        {
            currentHeat += amount;
            return;
        }

        // Preenche até o maxHeat atual.
        currentHeat = maxHeat;

        int excessHeat = amount - availableHeat;

        int convertedSuperheat = Mathf.FloorToInt(
            excessHeat * excessHeatToSuperheatMultiplier
        );

        AddSuperheat(convertedSuperheat);
    }

    public void RemoveHeat(int amount)
    {
        if (amount <= 0)
            return;

        currentHeat -= amount;

        if (currentHeat < minHeat)
            currentHeat = minHeat;

        CheckDeath();
    }

    public void AddSuperheat(int amount)
    {
        if (amount <= 0)
            return;

        currentSuperheat += amount;

        if (currentSuperheat > maxSuperheat)
            currentSuperheat = maxSuperheat;

        CheckDeath();
    }

    public void RemoveSuperheat(int amount)
    {
        if (amount <= 0)
            return;

        currentSuperheat -= amount;

        if (currentSuperheat < 0)
            currentSuperheat = 0;
    }

    public int CalculateHeavyHeatGeneration(
        int baseHeatGeneration
    )
    {
        if (baseHeatGeneration <= 0 || maxHeat <= 0)
            return 0;

        float generationMultiplier = 1f;

        // Penalidade linear de geração no frio.
        if (currentHeat < 0 && minHeat < 0)
        {
            float coldAmount = Mathf.Clamp01(
                (float)currentHeat / minHeat
            );

            generationMultiplier = Mathf.Lerp(
                1f,
                minimumHeavyHeatGeneration,
                coldAmount
            );
        }

        float generatedHeat =
            baseHeatGeneration * generationMultiplier;

        // Limites relativos ao maxHeat atual.
        int minimum = Mathf.CeilToInt(
            maxHeat * minimumHeatPerHitPercent
        );

        int maximum = Mathf.FloorToInt(
            maxHeat * maximumHeatPerHitPercent
        );

        maximum = Mathf.Max(minimum, maximum);

        return Mathf.Clamp(
            Mathf.RoundToInt(generatedHeat),
            minimum,
            maximum
        );
    }

    public int CalculateHeavyDamage(int baseDamage)
    {
        if (baseDamage <= 0)
            return 0;

        // Mantém a penalidade linear no frio.
        float multiplier = CalculateHeavyDamageMultiplier();

        // O bônus aditivo só existe com Heat positivo.
        float bonusDamage = 0f;

        if (currentHeat > 0)
        {
            bonusDamage = maximumHeavyBonusDamage * GetPositiveHeatPower();
        }

        float finalDamage = (baseDamage * multiplier) + bonusDamage;

        return Mathf.RoundToInt(finalDamage);
    }

    float CalculateHeavyDamageMultiplier()
    {
        // ====================================
        // HEAT NEGATIVO — LINEAR
        // ====================================

        if (currentHeat < 0)
        {
            float coldAmount =
                (float)currentHeat / minHeat;

            coldAmount = Mathf.Clamp01(coldAmount);

            return Mathf.Lerp(
                1f,
                minimumHeavyDamageMultiplier,
                coldAmount
            );
        }

        // ====================================
        // HEAT POSITIVO — EXPONENCIAL
        // ====================================

        return 1f +
            (maximumHeavyDamageMultiplier - 1f) *
            GetPositiveHeatPower();
    }

    public int CalculateHeavyPoiseDamage(int basePoiseDamage)
    {
        if (basePoiseDamage <= 0)
            return 0;

        if (currentHeat <= 0)
            return basePoiseDamage;

        float bonusPoise =
            maximumHeavyBonusPoise * GetPositiveHeatPower();

        return basePoiseDamage + Mathf.RoundToInt(bonusPoise);
    }

    public void CoolFromLight(int heatAmount)
    {
        // ====================================
        // HEAT
        // ====================================

        // Light só reduz Heat acima de zero.
        if (currentHeat > 0)
        {
            currentHeat -= heatAmount;

            // Light nunca empurra Heat para valores negativos.
            if (currentHeat < 0)
                currentHeat = 0;
        }

    }

    private float GetPositiveHeatPower()
    {
        if (currentHeat <= 0)
            return 0f;

        float reference = Mathf.Max(1f, heatPowerReference);
        float exponent = Mathf.Max(0.01f, heatPowerExponent);
        float t = currentHeat / reference;

        return (Mathf.Exp(exponent * t) - 1f) /
            (Mathf.Exp(exponent) - 1f);
    }

    void CheckDeath()
    {
        if (playerState.IsDead())
            return;

        if (currentHeat <= minHeat)
        {
            Die("Frio extremo");
            return;
        }

        if (currentSuperheat >= maxSuperheat)
        {
            Die("Superaquecimento");
        }
    }

    void Die(string cause)
    {
        Debug.Log(
            "Player morreu! Causa: " + cause
        );

        playerState.ChangeState(
            PlayerState.State.Dead
        );
    }
}

using UnityEngine;

public class PlayerHeat : MonoBehaviour
{
    [Header("Heat")]
    public int maxHeat = 1000;
    public int minHeat = -1000;

    [Header("Superheat")]
    public int maxSuperheat = 1000;

    // Valor fixo, independente de maxHeat.
    public int superheatThreshold = 850;

    [Header("Passive Heat Cooling")]
    public int passiveHeatCoolingPerSecond = 20;

    [Header("Passive Superheat Generation")]

    // Taxa de referência em Heat = 1000. Acima de 1000, a geração pode ultrapassar este valor.
    public int maxPassiveSuperheatPerSecond = 30;

    [Header("Superheat Scaling Reference")]
    [Min(1f)]
    public float superheatFullRateReference = 1000f;

    [Header("Passive Superheat Cooling")]
    public int passiveSuperheatCoolingPerSecond = 10;
    public int coldSuperheatCoolingPerSecond = 40;

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
    public int maximumHeavyBonusPoise = 75;

    [Header("Heat Power Curve")]

    // Referência de balanceamento, NÃO limite de Heat.
    [Min(1f)]
    public float heatPowerReference = 1000f;

    // Controla a agressividade da curva exponencial.
    [Min(0.01f)]
    public float heatPowerExponent = 1.5f;

    [Header("Cooling Delays")]
    public float heatCoolingDelay = 5f;
    public float superheatCoolingDelay = 10f;

    private float timeSinceHeatIncrease;
    private float timeSinceSuperheatIncrease;

    [Header("Cold Superheat Cooling")]
    public int coldLightSuperheatMultiplier = 4;

    [Header("Heavy Heat Generation Limits")]
    [Range(0f, 1f)]
    [SerializeField]
    private float minimumHeatPerHitPercent = 0.02f;

    [Range(0f, 1f)]
    [SerializeField]
    private float maximumHeatPerHitPercent = 0.20f;

    private int currentHeat = 0;
    private int currentSuperheat = 0;

    // Guardam frações de tempo para trabalhar com inteiros.
    private float heatCoolingAccumulator;
    private float superheatAccumulator;

    private PlayerState playerState;
    private PlayerGuard playerGuard;

    public int CurrentHeat => currentHeat;
    public int CurrentSuperheat => currentSuperheat;
    public int MaxHeat => maxHeat;
    public int MinHeat => minHeat;
    public int MaxSuperheat => maxSuperheat;

    void Start()
    {
        playerState = GetComponent<PlayerState>();
        playerGuard = GetComponent<PlayerGuard>();
    }

    void Update()
    {
        if (playerState.IsDead())
            return;

        timeSinceHeatIncrease += Time.deltaTime;
        timeSinceSuperheatIncrease += Time.deltaTime;

        UpdatePassiveHeat();
        UpdatePassiveSuperheat();

        CheckDeath();
    }

    void UpdatePassiveHeat()
    {
        if (timeSinceHeatIncrease < heatCoolingDelay)
        {
            heatCoolingAccumulator = 0f;
            return;
        }

        // Heat negativo não se recupera sozinho.
        if (currentHeat <= 0)
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

    void UpdatePassiveSuperheat()
    {
        // ====================================
        // ZONA QUENTE
        // ====================================

        if (currentHeat > superheatThreshold)
        {
            // A referência é fixa e independente de maxHeat.
            // Em 1000 Heat, a taxa é a configurada.
            // Em 1200 Heat, a taxa será maior.
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

        if (timeSinceSuperheatIncrease < superheatCoolingDelay)
        {
            superheatAccumulator = 0f;
            return;
        }

        // ====================================
        // ZONA NORMAL / FRIA
        // ====================================

        int coolingPerSecond;

        if (currentHeat < 0)
        {
            coolingPerSecond =
                coldSuperheatCoolingPerSecond;
        }
        else
        {
            coolingPerSecond =
                passiveSuperheatCoolingPerSecond;
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

        if (currentHeat < 0 && playerGuard != null && playerGuard.HasParryHeatBuff)
        {
            amount = Mathf.RoundToInt(amount * playerGuard.ParryHeatMultiplier);
        }

        timeSinceHeatIncrease = 0f;

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

        // 2 Heat excedente = 1 Superheat.
        int convertedSuperheat = excessHeat / 2;

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

        timeSinceSuperheatIncrease = 0f;

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

    public void CoolFromLight(
        int heatAmount,
        int superheatAmount
    )
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

        // ====================================
        // SUPERHEAT
        // ====================================

        int finalSuperheatCooling =
            superheatAmount;

        if (currentHeat < 0)
        {
            finalSuperheatCooling *=
                coldLightSuperheatMultiplier;
        }

        RemoveSuperheat(finalSuperheatCooling);
    }

    private float GetPositiveHeatPower()
    {
        if (currentHeat <= 0)
            return 0f;

        float heat = currentHeat;

        float t = heat / 1000f;

        const float exponent = 2f;

        return (Mathf.Exp(exponent * t) - 1f) / (Mathf.Exp(exponent) - 1f);
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

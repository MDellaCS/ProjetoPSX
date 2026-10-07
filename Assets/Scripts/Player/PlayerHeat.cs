using UnityEngine;

public class PlayerHeat : MonoBehaviour
{
    [Header("Heat")]
    public int maxHeat = 1000;
    public int minHeat = -1000;

    [Header("Superheat")]
    public int maxSuperheat = 1000;

    // Superheat começa a subir passivamente quando Heat ultrapassa este valor.
    public int superheatThreshold = 850;

    [Header("Passive Heat Cooling")]

    // Quantos pontos de Heat positivo são perdidos por segundo.
    public int passiveHeatCoolingPerSecond = 20;

    [Header("Passive Superheat Generation")]

    // Em Heat = 1000, esta é a quantidade máxima de Superheat gerada por segundo.
    public int maxPassiveSuperheatPerSecond = 30;

    [Header("Passive Superheat Cooling")]

    // Dissipação normal.
    public int passiveSuperheatCoolingPerSecond = 10;

    // Dissipação quando Heat está negativo.
    public int coldSuperheatCoolingPerSecond = 40;

    [Header("Cold Heavy Penalty")]

    [Range(0f, 1f)]
    public float minimumHeavyHeatGeneration = 0.2f;

    [Header("Heavy Power Scaling")]

    [Range(0f, 1f)]
    public float minimumHeavyDamageMultiplier = 0.2f;

    [Range(0f, 1f)]
    public float minimumHeavyAreaMultiplier = 0.5f;

    public float maximumHeavyDamageMultiplier = 1.5f;
    public float maximumHeavyAreaMultiplier = 1.5f;

    [Header("Cooling Delays")]
    public float heatCoolingDelay = 5f;
    public float superheatCoolingDelay = 10f;

    private float timeSinceHeatIncrease;
    private float timeSinceSuperheatIncrease;

    [Header("Cold Superheat Cooling")]
    public int coldLightSuperheatMultiplier = 4;

    private int currentHeat = 0;
    private int currentSuperheat = 0;

    // Como queremos trabalhar apenas com inteiros, estes acumuladores guardam as frações de tempo.
    private float heatCoolingAccumulator;
    private float superheatAccumulator;

    private PlayerState playerState;

    public int CurrentHeat
    {
        get
        {
            return currentHeat;
        }
    }

    public int CurrentSuperheat
    {
        get
        {
            return currentSuperheat;
        }
    }

    public int MaxHeat
    {
        get
        {
            return maxHeat;
        }
    }

    public int MinHeat
    {
        get
        {
            return minHeat;
        }
    }

    public int MaxSuperheat
    {
        get
        {
            return maxSuperheat;
        }
    }

    void Start()
    {
        playerState = GetComponent<PlayerState>();
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
            passiveHeatCoolingPerSecond *
            Time.deltaTime;

        int amount =
            Mathf.FloorToInt(
                heatCoolingAccumulator
            );

        if (amount <= 0)
            return;

        currentHeat -= amount;

        // Heat passivo nunca atravessa zero.
        if (currentHeat < 0)
        {
            currentHeat = 0;
        }

        heatCoolingAccumulator -= amount;
    }

    void UpdatePassiveSuperheat()
    {
        // ====================================
        // ZONA QUENTE
        // ====================================

        if (currentHeat > superheatThreshold)
        {
            float heatRange =
                maxHeat - superheatThreshold;

            float heatAboveThreshold =
                currentHeat - superheatThreshold;

            float heatPercent =
                heatAboveThreshold / heatRange;

            float generationPerSecond =
                maxPassiveSuperheatPerSecond *
                heatPercent;

            superheatAccumulator +=
                generationPerSecond *
                Time.deltaTime;

            int amount =
                Mathf.FloorToInt(
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
            coolingPerSecond *
            Time.deltaTime;

        int coolingAmount =
            Mathf.FloorToInt(
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

        timeSinceHeatIncrease = 0f;

        // Heat negativo também recebe Heat normalmente.
        // É assim que Heavy futuramente tirará
        // o jogador do frio.

        int availableHeat =
            maxHeat - currentHeat;

        // Ainda existe espaço suficiente.
        if (amount <= availableHeat)
        {
            currentHeat += amount;

            return;
        }

        // Preenche Heat até +1000.
        currentHeat = maxHeat;

        // Descobre quanto Heat sobrou.
        int excessHeat =
            amount - availableHeat;

        // Conversão:
        // 2 Heat excedente = 1 Superheat.
        int convertedSuperheat =
            excessHeat / 2;

        AddSuperheat(convertedSuperheat);
    }

    public void RemoveHeat(int amount)
    {
        if (amount <= 0)
            return;

        currentHeat -= amount;

        if (currentHeat < minHeat)
        {
            currentHeat = minHeat;
        }

        CheckDeath();
    }

    public void AddSuperheat(int amount)
    {
        if (amount <= 0)
            return;

        timeSinceSuperheatIncrease = 0f;

        currentSuperheat += amount;

        if (currentSuperheat > maxSuperheat)
        {
            currentSuperheat = maxSuperheat;
        }

        CheckDeath();
    }

    public void RemoveSuperheat(int amount)
    {
        if (amount <= 0)
            return;

        currentSuperheat -= amount;

        if (currentSuperheat < 0)
        {
            currentSuperheat = 0;
        }
    }

    public int CalculateHeavyHeatGeneration(int baseHeatGeneration)
    {
        if (baseHeatGeneration <= 0)
            return 0;

        // Heat neutro ou positivo: Heavy gera 100%.
        if (currentHeat >= 0)
            return baseHeatGeneration;

        float coldAmount = (float)currentHeat / minHeat;

        coldAmount = Mathf.Clamp01(coldAmount);

        // Quanto mais frio,  menor o multiplicador.
        float generationMultiplier =
            Mathf.Lerp(
                1f,
                minimumHeavyHeatGeneration,
                coldAmount
            );

        int finalHeatGeneration =
            Mathf.RoundToInt(
                baseHeatGeneration *
                generationMultiplier
            );

        return finalHeatGeneration;
    }

    public int CalculateHeavyDamage(int baseDamage)
    {
        if (baseDamage <= 0)
            return 0;

        float multiplier = CalculateHeavyDamageMultiplier();

        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        return finalDamage;
    }

    float CalculateHeavyDamageMultiplier()
    {
        // =========================
        // HEAT NEGATIVO
        // =========================

        if (currentHeat < 0)
        {
            float coldAmount = (float)currentHeat / minHeat;

            coldAmount = Mathf.Clamp01(coldAmount);

            return Mathf.Lerp(
                1f,
                minimumHeavyDamageMultiplier,
                coldAmount
            );
        }

        // =========================
        // HEAT POSITIVO
        // =========================

        float hotAmount = (float)currentHeat / maxHeat;

        hotAmount = Mathf.Clamp01(hotAmount);

        return Mathf.Lerp(
            1f,
            maximumHeavyDamageMultiplier,
            hotAmount
        );
    }

    public float CalculateHeavyArea(float baseArea)
    {
        if (baseArea <= 0f)
            return 0f;

        float multiplier = CalculateHeavyAreaMultiplier();

        return baseArea * multiplier;
    }

    float CalculateHeavyAreaMultiplier()
    {
        // =========================
        // HEAT NEGATIVO
        // =========================

        if (currentHeat < 0)
        {
            float coldAmount = (float)currentHeat / minHeat;

            coldAmount = Mathf.Clamp01(coldAmount);

            return Mathf.Lerp(
                1f,
                minimumHeavyAreaMultiplier,
                coldAmount
            );
        }

        // =========================
        // HEAT POSITIVO
        // =========================

        float hotAmount = (float)currentHeat / maxHeat;

        hotAmount = Mathf.Clamp01(hotAmount);

        return Mathf.Lerp(
            1f,
            maximumHeavyAreaMultiplier,
            hotAmount
        );
    }

    public void CoolFromLight(int heatAmount, int superheatAmount)
    {
        // ====================================
        // HEAT
        // ====================================

        // Light só reduz Heat se ele estiver acima de zero.
        if (currentHeat > 0)
        {
            currentHeat -= heatAmount;

            // Light nunca empurra Heat para valores negativos.
            if (currentHeat < 0)
            {
                currentHeat = 0;
            }
        }

        // ====================================
        // SUPERHEAT
        // ====================================

        int finalSuperheatCooling = superheatAmount;

        // Se estamos no frio, Light remove Superheat mais rápido.
        if (currentHeat < 0)
        {
            finalSuperheatCooling *= coldLightSuperheatMultiplier;
        }

        RemoveSuperheat(finalSuperheatCooling);
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
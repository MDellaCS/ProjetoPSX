using UnityEngine;

/// <summary>Progresso persistente do atributo Carga do jogador.</summary>
[DisallowMultipleComponent]
public sealed class PlayerChargeUpgrade : MonoBehaviour
{
    private const string SaveKey = "ProjetoPSX.PlayerChargeLevel.v1";
    private const string HeatBonusSaveKey = "ProjetoPSX.PlayerChargeHeatBonus.v1";
    private PlayerHeat heat;
    private int maximumLevel;
    private AnimationCurve heatBonusByLevel;

    public int CurrentLevel { get; private set; }
    public int MaximumLevel => maximumLevel;
    public bool IsMaxLevel => CurrentLevel >= maximumLevel;
    public float CurrentMaxHeatBonus => EvaluateBonus(CurrentLevel);

    private void Awake()
    {
        CurrentLevel = Mathf.Max(0, PlayerPrefs.GetInt(SaveKey, 0));
        heat = GetComponent<PlayerHeat>();
    }

    public static PlayerChargeUpgrade FindOrCreate(Transform player)
    {
        if (player == null) return null;
        PlayerChargeUpgrade upgrade = player.GetComponent<PlayerChargeUpgrade>();
        return upgrade != null ? upgrade : player.gameObject.AddComponent<PlayerChargeUpgrade>();
    }

    /// <summary>Aplica a configuracao global de Carga oferecida pela loja.</summary>
    public void Configure(int newMaximumLevel, AnimationCurve newHeatBonusByLevel)
    {
        maximumLevel = Mathf.Max(0, newMaximumLevel);
        heatBonusByLevel = newHeatBonusByLevel != null
            ? new AnimationCurve(newHeatBonusByLevel.keys)
            : AnimationCurve.Linear(0f, 0f, 1f, 0f);
        CurrentLevel = Mathf.Clamp(CurrentLevel, 0, maximumLevel);
        ApplyHeatBonus();
    }

    public bool TryBuy(PlayerCurrencyWallet wallet, int price)
    {
        if (wallet == null || IsMaxLevel || price < 0 || !wallet.TrySpend(price))
            return false;
        CurrentLevel++;
        PlayerPrefs.SetInt(SaveKey, CurrentLevel);
        PlayerPrefs.Save();
        ApplyHeatBonus();
        return true;
    }

    private float EvaluateBonus(int level) => heatBonusByLevel == null
        ? 0f : Mathf.Max(0f, heatBonusByLevel.Evaluate(level));

    private void ApplyHeatBonus()
    {
        heat ??= GetComponent<PlayerHeat>();
        float bonus = EvaluateBonus(CurrentLevel);
        PlayerPrefs.SetFloat(HeatBonusSaveKey, bonus);
        PlayerPrefs.Save();
        heat?.SetChargeMaxHeatBonus(bonus);
    }
}

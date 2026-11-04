using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Unica autoridade de niveis e compras; restaura os efeitos a partir do catalogo global.</summary>
[DisallowMultipleComponent]
public sealed class PlayerProgression : MonoBehaviour
{
    private const string SavePrefix = "ProjetoPSX.Progression.v2.";
    private readonly Dictionary<string, int> levels = new();
    private readonly Dictionary<string, float> stats = new();
    private readonly HashSet<string> specials = new();
    private readonly HashSet<PlayerUpgradeEffect> appliedEffects = new();
    public event Action Changed;
    public PlayerUpgradeCatalog Catalog { get; private set; }

    public static PlayerProgression GetOrAdd(GameObject player) =>
        GetOrAddComponent(player);

    private static PlayerProgression GetOrAddComponent(GameObject player)
    {
        PlayerProgression progression = player.GetComponent<PlayerProgression>();
        return progression != null ? progression : player.AddComponent<PlayerProgression>();
    }

    private void Start() => Initialize();
    public void Initialize()
    {
        if (Catalog != null) return;
        Catalog = PlayerUpgradeCatalog.Load();
        if (Catalog == null) return;
        foreach (PlayerUpgradeDefinition attribute in Catalog.attributes)
        {
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.id) || levels.ContainsKey(attribute.id)) continue;
            levels.Add(attribute.id, Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + attribute.id, 1)));
        }
        ApplyBonuses();
        Changed?.Invoke();
    }
    public int Level(PlayerUpgradeDefinition attribute) => attribute != null &&
        levels.TryGetValue(attribute.id, out int level) ? Mathf.Clamp(level, 1, attribute.MaximumLevel) : 1;
    public float StatBonus(string id) => stats.TryGetValue(id, out float value) ? value : 0;
    public bool HasSpecial(string id) => !string.IsNullOrEmpty(id) && specials.Contains(id);

    /// <summary>Retorna todos os atributos ao nivel inicial sem gravar novos saves.</summary>
    public void ResetProgress()
    {
        Initialize();
        if (Catalog == null)
            return;

        foreach (PlayerUpgradeDefinition attribute in Catalog.attributes)
        {
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.id))
                continue;
            levels[attribute.id] = 1;
        }

        ApplyBonuses();
        Changed?.Invoke();
    }

    public void DeleteStoredProgress()
    {
        Initialize();
        if (Catalog == null)
            return;
        foreach (PlayerUpgradeDefinition attribute in Catalog.attributes)
        {
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.id))
                PlayerPrefs.DeleteKey(SavePrefix + attribute.id);
        }
    }

    public void ReapplyActiveEffects()
    {
        Initialize();
        foreach (PlayerUpgradeEffect effect in appliedEffects)
            if (effect != null)
                effect.Apply(this);
    }

    public bool TryPurchase(PlayerUpgradeDefinition attribute, out string message)
    {
        Initialize();
        if (Catalog == null || attribute == null || string.IsNullOrWhiteSpace(attribute.id) ||
            !Catalog.attributes.Contains(attribute) || !attribute.purchasable)
        { message = "Atributo indisponivel."; return false; }
        int level = Level(attribute);
        if (level >= attribute.MaximumLevel) { message = "Nivel maximo."; return false; }
        if (attribute.GetLevel(level + 1) == null) { message = "Nivel nao configurado."; return false; }
        PlayerCurrencyWallet wallet = GetComponent<PlayerCurrencyWallet>();
        if (wallet == null)
            wallet = gameObject.AddComponent<PlayerCurrencyWallet>();
        if (!wallet.TrySpend(attribute.Price(level + 1))) { message = "Currency insuficiente."; return false; }
        levels[attribute.id] = level + 1;
        PlayerPrefs.SetInt(SavePrefix + attribute.id, level + 1);
        PlayerPrefs.Save();
        ApplyBonuses();
        Changed?.Invoke();
        message = attribute.displayName + " aprimorado para nivel " + (level + 1) + ".";
        return true;
    }
    private void ApplyBonuses()
    {
        stats.Clear(); specials.Clear();
        var desiredEffects = new HashSet<PlayerUpgradeEffect>();
        var visited = new HashSet<string>();
        foreach (PlayerUpgradeDefinition attribute in Catalog.attributes)
        {
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.id) || !visited.Add(attribute.id)) continue;
            int level = Level(attribute);
            if (!string.IsNullOrWhiteSpace(attribute.statId))
                stats[attribute.statId] = StatBonus(attribute.statId) + attribute.Bonus(level);
            for (int unlockedLevel = 1; unlockedLevel <= level; unlockedLevel++)
            {
                PlayerUpgradeLevel entry = attribute.GetLevel(unlockedLevel);
                if (entry?.unlocks == null) continue;
                foreach (PlayerUpgradeUnlock unlock in entry.unlocks)
                {
                    if (unlock == null) continue;
                    if (!string.IsNullOrWhiteSpace(unlock.id)) specials.Add(unlock.id);
                    if (unlock.effects != null)
                        foreach (PlayerUpgradeEffect effect in unlock.effects)
                            if (effect != null) desiredEffects.Add(effect);
                }
            }
        }
        foreach (PlayerUpgradeEffect effect in appliedEffects)
            if (!desiredEffects.Contains(effect)) effect.Remove(this);
        foreach (PlayerUpgradeEffect effect in desiredEffects)
            if (!appliedEffects.Contains(effect)) effect.Apply(this);
        appliedEffects.Clear(); appliedEffects.UnionWith(desiredEffects);
        GetComponent<PlayerHeat>()?.SetChargeMaxHeatBonus(StatBonus("max-heat"));
    }
    private void OnDestroy()
    {
        foreach (PlayerUpgradeEffect effect in appliedEffects)
            if (effect != null) effect.Remove(this);
    }
}

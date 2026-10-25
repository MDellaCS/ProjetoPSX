using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Um desbloqueio independente, pertencente a um nivel especifico.</summary>
[Serializable]
public sealed class PlayerUpgradeUnlock
{
    [Tooltip("Identificador do especial, consultavel pelo gameplay e por PlayerUpgradeUnlockActions.")]
    public string id;
    [Tooltip("Nome do especial apresentado na loja.")]
    public string displayName;
    [Tooltip("Descricao do que este desbloqueio concede.")]
    [TextArea] public string description;
    [Tooltip("Efeitos aplicados ao atingir este nivel, inclusive ao restaurar um save.")]
    public List<PlayerUpgradeEffect> effects = new();
}

/// <summary>Uma entrada corresponde a um nivel. A primeira entrada e sempre o nivel inicial.</summary>
[Serializable]
public sealed class PlayerUpgradeLevel
{
    [Tooltip("Preco inteiro para comprar este nivel. O nivel 1 e inicial e nao cobra currency.")]
    [Min(0)] public int price;
    [Tooltip("Incremento bruto de status concedido por este nivel. Ignorado no nivel 1.")]
    [Min(0f)] public float gain;
    [Tooltip("Fracao do incremento que sera mantida. Use valores menores nos niveis altos para diminishing returns.")]
    [Range(0f, 1f)] public float retainedGain = 1f;
    [Tooltip("Especiais desbloqueados neste nivel. Lista vazia significa nenhum especial; niveis anteriores continuam desbloqueados.")]
    public List<PlayerUpgradeUnlock> unlocks = new();
}

/// <summary>Quantidade de niveis e valores individuais de um atributo.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Progression/Attribute")]
public sealed class PlayerUpgradeDefinition : ScriptableObject
{
    [Tooltip("Identificador persistente e unico. Nao altere depois de criar saves.")]
    public string id = "attribute";
    [Tooltip("Nome apresentado na loja.")]
    public string displayName = "TBD";
    [Tooltip("Descricao do atributo e dos seus beneficios.")]
    [TextArea] public string description;
    [Tooltip("Permite comprar este atributo. Desative enquanto estiver TBD.")]
    public bool purchasable;
    [Tooltip("Chave do status modificado. max-heat adiciona Heat maximo; outros sistemas podem consultar novas chaves.")]
    public string statId = "max-heat";
    [Tooltip("Lista ordenada de niveis. Element 0 e nivel 1 (inicial). A quantidade de entradas define o nivel maximo; cada entrada configura preco, ganho, retorno e especiais.")]
    public List<PlayerUpgradeLevel> levels = new()
    {
        new(), new() { price = 20, gain = 5 }, new() { price = 40, gain = 5 },
        new() { price = 60, gain = 5 }, new() { price = 80, gain = 5 },
        new() { price = 100, gain = 5 }
    };

    public int MaximumLevel => Mathf.Max(1, levels?.Count ?? 0);
    public PlayerUpgradeLevel GetLevel(int level) => levels != null && level >= 1 &&
        level <= levels.Count ? levels[level - 1] : null;
    public int Price(int level) => GetLevel(level) is PlayerUpgradeLevel entry
        ? Mathf.Max(0, entry.price) : int.MaxValue;
    public float Bonus(int level)
    {
        float total = 0;
        for (int i = 2; i <= Mathf.Clamp(level, 1, MaximumLevel); i++)
        {
            PlayerUpgradeLevel entry = GetLevel(i);
            if (entry != null) total += Mathf.Max(0, entry.gain) * Mathf.Clamp01(entry.retainedGain);
        }
        return total;
    }
}

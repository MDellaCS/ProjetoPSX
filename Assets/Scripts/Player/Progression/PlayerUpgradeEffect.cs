using UnityEngine;

/// <summary>Extensao para efeitos de desbloqueio. Nunca guarde estado do Player neste asset compartilhado.</summary>
public abstract class PlayerUpgradeEffect : ScriptableObject
{
    public abstract void Apply(PlayerProgression player);
    public abstract void Remove(PlayerProgression player);
}

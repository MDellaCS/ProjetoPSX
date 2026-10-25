using UnityEngine;

/// <summary>Desbloqueia Rage apos a explosao de Burn, por uma duracao configuravel.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Progression/Effects/Rage")]
public sealed class PlayerRageUpgradeEffect : PlayerUpgradeEffect
{
    [Tooltip("Duracao de Rage em segundos de gameplay. Durante esse periodo o Player tem movimentos normais e Heat maximo sem consumo. Use 0 para desativar.")]
    [Min(0f)] public float duration = 5f;

    public override void Apply(PlayerProgression player) =>
        player.GetComponent<PlayerHeat>()?.SetRageGrant(this, duration);
    public override void Remove(PlayerProgression player) =>
        player.GetComponent<PlayerHeat>()?.SetRageGrant(this, 0f);
}

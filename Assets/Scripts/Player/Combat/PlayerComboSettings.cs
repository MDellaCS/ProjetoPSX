using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Tempos e recompensas recebidos ao manter uma sequencia de golpes.</summary>
[CreateAssetMenu(fileName = "PlayerComboSettings", menuName = "Projeto PSX/Combat/Combo Settings")]
public sealed class PlayerComboSettings : ScriptableObject
{
    [Tooltip("Segundos sem acertar um inimigo antes de encerrar o combo.")]
    [Min(.1f)] [SerializeField] private float idleTimeout = 4f;
    [Tooltip("Recompensas concedidas uma vez cada vez que o combo alcanca o numero de golpes indicado.")]
    [SerializeField] private List<PlayerComboBreakpoint> breakpoints = new();

    public float IdleTimeout => Mathf.Max(.1f, idleTimeout);
    public IReadOnlyList<PlayerComboBreakpoint> Breakpoints => breakpoints;

    private void OnValidate()
    {
        idleTimeout = Mathf.Max(.1f, idleTimeout);
        if (breakpoints == null)
            breakpoints = new List<PlayerComboBreakpoint>();
        foreach (PlayerComboBreakpoint breakpoint in breakpoints)
            if (breakpoint != null)
                breakpoint.requiredHits = Mathf.Max(1, breakpoint.requiredHits);
    }
}

[Serializable]
public sealed class PlayerComboBreakpoint
{
    [Tooltip("Quantidade de impactos confirmados necessaria para receber esta recompensa.")]
    [Min(1)] public int requiredHits = 10;
    [Tooltip("Heat concedido no breakpoint. So e aplicado se o moveset ativo usar Heat e o jogador nao estiver queimando.")]
    [Min(0f)] public float heatReward;
    [Tooltip("Currency concedida no breakpoint.")]
    [Min(0)] public int currencyReward;
}

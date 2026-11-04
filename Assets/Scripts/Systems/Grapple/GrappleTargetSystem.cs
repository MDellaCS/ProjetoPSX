using System.Collections.Generic;
using UnityEngine;

public enum GrappleTargetKind
{
    None,
    SwingAnchor,
    Enemy,
    Ladder,
    Custom
}

/// <summary>Alvo que o gancho atingiria agora, descrito sem depender da feature que o criou.</summary>
public struct GrappleTarget
{
    public GrappleTargetKind kind;
    /// <summary>Ponto do mundo em que o indicador aparece.</summary>
    public Vector3 point;
    /// <summary>Objeto dono do alvo (inimigo, ancora, escada...). Opcional.</summary>
    public Object source;

    public bool IsValid => kind != GrappleTargetKind.None;
}

/// <summary>
/// Qualquer feature que o gancho possa atingir implementa isto e se registra no
/// <see cref="GrappleTargetSystem"/>: o indicador na tela funciona sem conhecer
/// a feature. Registre em OnEnable e remova em OnDisable.
/// </summary>
public interface IGrappleTargetProvider
{
    /// <summary>Maior prioridade vence quando varios provedores tem alvo.</summary>
    int GrappleTargetPriority { get; }

    /// <summary>Alvo atual do provedor. Falso se nao ha alvo valido agora.</summary>
    bool TryGetGrappleTarget(out GrappleTarget target);
}

/// <summary>
/// Registro global dos provedores de alvo do gancho (pontos de balanco,
/// inimigos, escadas e o que mais usar o gancho). O melhor alvo e reavaliado
/// poucas vezes por segundo e exibido pelo <see cref="GrappleTargetHUD"/>.
/// </summary>
public static class GrappleTargetSystem
{
    private const float EvaluationInterval = .05f;

    private static readonly List<IGrappleTargetProvider> providers = new();
    private static GrappleTarget current;
    private static float nextEvaluationTime;
    private static int evaluatedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        providers.Clear();
        current = default;
        nextEvaluationTime = 0f;
        evaluatedFrame = -1;
    }

    /// <summary>Melhor alvo do gancho agora (prioridade, depois ordem de registro).</summary>
    public static GrappleTarget Current
    {
        get
        {
            Evaluate();
            return current;
        }
    }

    public static void Register(IGrappleTargetProvider provider)
    {
        if (provider == null || providers.Contains(provider))
            return;

        providers.Add(provider);
        providers.Sort((a, b) => b.GrappleTargetPriority.CompareTo(a.GrappleTargetPriority));
        GrappleTargetHUD.EnsureExists();
    }

    public static void Unregister(IGrappleTargetProvider provider)
    {
        providers.Remove(provider);
        current = default;
        nextEvaluationTime = 0f;
        evaluatedFrame = -1;
    }

    private static void Evaluate()
    {
        if (evaluatedFrame == Time.frameCount || Time.unscaledTime < nextEvaluationTime)
            return;

        evaluatedFrame = Time.frameCount;
        nextEvaluationTime = Time.unscaledTime + EvaluationInterval;
        current = default;
        for (int i = 0; i < providers.Count; i++)
            if (providers[i].TryGetGrappleTarget(out GrappleTarget target) && target.IsValid)
            {
                current = target;
                return;
            }
    }
}

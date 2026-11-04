using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Resultado de um golpe aplicado a um modelo de dano.</summary>
public readonly struct WorldDamageResult
{
    /// <summary>O golpe foi aceito (gera hitstop, tremor e reacoes de golpe).</summary>
    public readonly bool Accepted;
    /// <summary>O golpe esgotou o modelo e deve disparar as reacoes de "destruido".</summary>
    public readonly bool Depleted;

    public WorldDamageResult(bool accepted, bool depleted)
    {
        Accepted = accepted;
        Depleted = depleted;
    }

    public static readonly WorldDamageResult Ignored = new(false, false);
}

/// <summary>
/// Regra que decide quando um objeto de mundo e esgotado pelo dano recebido.
/// Para criar um novo tipo, herde desta classe: ele aparece sozinho no seletor
/// de tipo do Inspector. Cada instancia pertence a um estado de um objeto, por
/// isso pode guardar o proprio estado de execucao.
/// </summary>
[Serializable]
public abstract class WorldDamageModel
{
    /// <summary>Nome apresentado no Inspector e na documentacao.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Prepara o estado de execucao. Chamado ao entrar no estado.</summary>
    public abstract void Initialize();

    /// <summary>Aplica um golpe. <paramref name="time"/> e o tempo atual do jogo.</summary>
    public abstract WorldDamageResult ApplyDamage(int damage, float time);

    /// <summary>Quanto da barra ainda esta cheia, de 0 a 1.</summary>
    public abstract float RemainingNormalized(float time);

    /// <summary>Texto curto mostrado junto da barra.</summary>
    public abstract string GetDisplayText(float time);
}

/// <summary>Vida normal: cada golpe subtrai da vida e ela nao regenera.</summary>
[Serializable]
public sealed class HealthPoolDamageModel : WorldDamageModel
{
    [Tooltip("Vida total do objeto. Ela so volta ao maximo quando a cena e recarregada ou o jogo e reiniciado.")]
    [Min(1)] [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private bool initialized;

    public override string DisplayName => "Vida";
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public int CurrentHealth => initialized ? currentHealth : MaxHealth;

    public override void Initialize()
    {
        currentHealth = MaxHealth;
        initialized = true;
    }

    public override WorldDamageResult ApplyDamage(int damage, float time)
    {
        if (!initialized)
            Initialize();
        if (damage <= 0 || currentHealth <= 0)
            return WorldDamageResult.Ignored;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        return new WorldDamageResult(true, currentHealth <= 0);
    }

    public override float RemainingNormalized(float time) =>
        CurrentHealth / (float)MaxHealth;

    public override string GetDisplayText(float time) =>
        $"{CurrentHealth} / {MaxHealth}";
}

/// <summary>Como a janela de tempo do dano acumulado e contada.</summary>
public enum AccumulatedDamageWindowMode
{
    /// <summary>Soma somente o dano recebido nos ultimos X segundos.</summary>
    SlidingWindow,
    /// <summary>A soma zera quando passam X segundos sem receber dano (comportamento das alavancas).</summary>
    ResetAfterIdle
}

/// <summary>
/// Dano acumulado em um intervalo: precisa receber Y de dano total dentro de X
/// segundos. A barra mostra quanto falta e volta a encher se a janela expirar.
/// </summary>
[Serializable]
public sealed class AccumulatedDamageModel : WorldDamageModel
{
    private struct Hit
    {
        public float Time;
        public int Damage;
    }

    [Tooltip("Dano total (Y) que precisa ser causado dentro da janela de tempo para esgotar o objeto.")]
    [Min(1)] [SerializeField] private int requiredDamage = 100;
    [Tooltip("Janela de tempo (X), em segundos, em que o dano conta.")]
    [Min(.01f)] [SerializeField] private float windowSeconds = 1f;
    [Tooltip("Sliding Window: soma o dano dos ultimos X segundos. Reset After Idle: zera tudo se ficar X segundos sem apanhar.")]
    [SerializeField] private AccumulatedDamageWindowMode windowMode;

    private readonly List<Hit> hits = new();
    private bool depleted;

    public AccumulatedDamageModel() { }

    public AccumulatedDamageModel(int requiredDamage, float windowSeconds,
        AccumulatedDamageWindowMode windowMode)
    {
        this.requiredDamage = Mathf.Max(1, requiredDamage);
        this.windowSeconds = Mathf.Max(.01f, windowSeconds);
        this.windowMode = windowMode;
    }

    public override string DisplayName => "Dano acumulado em X tempo";
    public int RequiredDamage => Mathf.Max(1, requiredDamage);

    public override void Initialize()
    {
        hits.Clear();
        depleted = false;
    }

    public int GetAccumulatedDamage(float time)
    {
        Trim(time);
        int total = 0;
        for (int i = 0; i < hits.Count; i++)
            total += hits[i].Damage;
        return total;
    }

    public override WorldDamageResult ApplyDamage(int damage, float time)
    {
        if (damage <= 0 || depleted)
            return WorldDamageResult.Ignored;

        Trim(time);
        hits.Add(new Hit { Time = time, Damage = damage });
        if (GetAccumulatedDamage(time) >= RequiredDamage)
            depleted = true;
        return new WorldDamageResult(true, depleted);
    }

    public override float RemainingNormalized(float time)
    {
        if (depleted)
            return 0f;
        return 1f - Mathf.Clamp01(GetAccumulatedDamage(time) /
            (float)RequiredDamage);
    }

    public override string GetDisplayText(float time) =>
        $"{Mathf.Max(0, RequiredDamage - GetAccumulatedDamage(time))} / " +
        $"{RequiredDamage} ({windowSeconds:0.##}s)";

    private void Trim(float time)
    {
        if (hits.Count == 0 || depleted)
            return;

        if (windowMode == AccumulatedDamageWindowMode.ResetAfterIdle)
        {
            if (time - hits[hits.Count - 1].Time > windowSeconds)
                hits.Clear();
            return;
        }

        float cutoff = time - windowSeconds;
        int expired = 0;
        while (expired < hits.Count && hits[expired].Time < cutoff)
            expired++;
        if (expired > 0)
            hits.RemoveRange(0, expired);
    }
}

/// <summary>
/// Dano em um unico golpe: so esgota se um unico golpe causar o dano exigido.
/// Golpes mais fracos sao aceitos, mas nao tiram nada da barra.
/// </summary>
[Serializable]
public sealed class SingleHitDamageModel : WorldDamageModel
{
    [Tooltip("Dano minimo que UM unico golpe precisa causar para esgotar o objeto. Golpes mais fracos nao acumulam.")]
    [Min(1)] [SerializeField] private int requiredDamage = 100;

    private bool depleted;

    public SingleHitDamageModel() { }

    public SingleHitDamageModel(int requiredDamage)
    {
        this.requiredDamage = Mathf.Max(1, requiredDamage);
    }

    public override string DisplayName => "Dano em um unico golpe";
    public int RequiredDamage => Mathf.Max(1, requiredDamage);

    public override void Initialize() => depleted = false;

    public override WorldDamageResult ApplyDamage(int damage, float time)
    {
        if (damage <= 0 || depleted)
            return WorldDamageResult.Ignored;

        depleted = damage >= RequiredDamage;
        return new WorldDamageResult(true, depleted);
    }

    public override float RemainingNormalized(float time) => depleted ? 0f : 1f;

    public override string GetDisplayText(float time) =>
        depleted ? "0" : $"{RequiredDamage}";
}

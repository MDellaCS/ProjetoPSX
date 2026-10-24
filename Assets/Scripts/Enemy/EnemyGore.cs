using UnityEngine;

/// <summary>Configuracao opcional de particulas para um inimigo.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Enemy Gore")]
public sealed class EnemyGore : MonoBehaviour
{
    [Header("Particle Profiles")]
    [Tooltip("Perfil emitido quando este inimigo recebe dano. Vazio usa o perfil global de impacto.")]
    [SerializeField] private CombatParticleProfile damageProfile;
    [Tooltip("Perfil fisico emitido quando este inimigo morre. Vazio usa o perfil global de gore mecanico.")]
    [SerializeField] private CombatParticleProfile deathProfile;
    [Tooltip("Multiplica a quantidade final de particulas deste inimigo depois dos limites minimo e maximo do perfil. Use 0 para desativar as particulas dele.")]
    [Min(0f)] [SerializeField] private float particleCountMultiplier = 1f;

    [Header("Currency")]
    [Tooltip("Intervalo inclusivo de currency sorteada individualmente por cada fragmento coletavel deste inimigo. Defina ambos como 0 para nao liberar currency.")]
    [SerializeField] private Vector2Int baseCurrencyValue = new(2, 5);
    [Tooltip("Quantidade minima de fragmentos de gore gerados na morte e quantidade normal de fragmentos que carregam currency. Este minimo tem prioridade sobre a curva de particulas.")]
    [Min(1)] [SerializeField] private int currencyGorePieces = 3;
    [Tooltip("Percentual da vida maxima que o dano do golpe final precisa atingir para contar como Overkill.")]
    [Range(.01f, 1f)] [SerializeField] private float overkillThresholdPercent = .5f;
    [Tooltip("Multiplica a quantidade de fragmentos que carregam currency quando o golpe final atinge o limite de Overkill. O valor sorteado por fragmento nao muda e nao possui relacao com Heat.")]
    [Min(1f)] [SerializeField] private float overkillCurrencyMultiplier = 2f;

    public CombatParticleProfile DamageProfile => damageProfile;
    public CombatParticleProfile DeathProfile => deathProfile;
    public float ParticleCountMultiplier => particleCountMultiplier;

    /// <summary>Indica se este inimigo pode soltar currency.</summary>
    public bool HasCurrency => Mathf.Max(baseCurrencyValue.x, baseCurrencyValue.y) > 0;

    /// <summary>Calcula a quantidade de pickups pelo golpe que matou o inimigo.</summary>
    public int GetCurrencyGorePieceCount(int finalHitDamage, int enemyMaxHealth)
    {
        if (!HasCurrency)
            return 0;

        bool isOverkill = enemyMaxHealth > 0 && finalHitDamage >=
            enemyMaxHealth * overkillThresholdPercent;
        float multiplier = isOverkill ? overkillCurrencyMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(currencyGorePieces * multiplier));
    }

    public int CurrencyGorePieces => currencyGorePieces;
    public Vector2Int CurrencyValueRange => new(
        Mathf.Min(baseCurrencyValue.x, baseCurrencyValue.y),
        Mathf.Max(baseCurrencyValue.x, baseCurrencyValue.y));

    /// <summary>Usado apenas pelo Enemy Death Loop Test.</summary>
    public void SpawnTestGore(int lethalDamage, Vector3 impactDirection)
    {
        Renderer source = GetComponentInChildren<Renderer>(true);
        Vector3 position = source != null ? source.bounds.center : transform.position;
        CombatParticleSystem.Instance.EmitTestDeath(deathProfile, position,
            impactDirection, lethalDamage, source, particleCountMultiplier);
    }
}

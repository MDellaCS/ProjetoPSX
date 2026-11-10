using UnityEngine;

/// <summary>Guarda o saldo perdido e devolve-o quando este inimigo morre.</summary>
[DisallowMultipleComponent]
public sealed class CorpseRunEnemy : MonoBehaviour
{
    public int CarriedCurrency { get; private set; }
    public string RunId { get; private set; }
    private EnemyBase enemy;
    private CorpseRun owner;
    private bool initialized;
    private bool claimed;

    public void Initialize(int currency, string runId, CorpseRun corpseRun)
    {
        CarriedCurrency = Mathf.Max(0, currency);
        RunId = runId;
        owner = corpseRun;
        initialized = true;
    }

    private void OnEnable()
    {
        if (!TryGetComponent(out enemy)) return;
        enemy.Died += OnDied;
    }

    private void OnDisable()
    {
        if (enemy != null) enemy.Died -= OnDied;
    }

    private void OnDied(EnemyBase _)
    {
        if (!initialized || claimed || CarriedCurrency <= 0) return;
        claimed = true;
        if (owner == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            owner = player != null ? CorpseRun.EnsureOn(player) : null;
        }
        if (owner != null) owner.NotifyCorpseDefeated(this);
        CarriedCurrency = 0;
    }
}

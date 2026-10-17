using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordenador de ritmo de combate da cena. Limita quantos inimigos podem
/// iniciar sequencias de ataque simultaneamente sem substituir a IA local.
/// </summary>
[DisallowMultipleComponent]
public class EnemyDirector : MonoBehaviour
{
    private static EnemyDirector activeDirector;

    [Header("Attack Turn Control")]
    [Tooltip("Quando ligado, inimigos precisam receber uma vaga do Director antes de iniciar um ataque.")]
    [SerializeField] private bool controlAttackTurns = true;
    [Tooltip("Numero maximo de inimigos que podem executar uma sequencia de ataque ao mesmo tempo.")]
    [Min(1)] [SerializeField] private int maximumConcurrentAttackers = 1;
    [Tooltip("Tempo que um inimigo espera apos terminar ou interromper seu ataque antes de poder reservar outra vaga.")]
    [Min(0f)] [SerializeField] private float attackerReentryCooldown = 1.25f;

    private readonly HashSet<EnemyBase> activeAttackers = new();
    private readonly Dictionary<EnemyBase, float> reentryReadyAt = new();
    private readonly List<EnemyBase> releasedEnemies = new();
    private readonly List<EnemyBase> expiredCooldowns = new();

    public static bool IsControllingAttackTurns =>
        activeDirector != null && activeDirector.controlAttackTurns;

    private void Awake()
    {
        activeDirector = this;
    }

    private void OnDestroy()
    {
        if (activeDirector == this)
            activeDirector = null;
    }

    private void Update()
    {
        RefreshAttackTurns();
    }

    /// <summary>
    /// Chamado pelo EnemyBase imediatamente antes de abrir um novo combo.
    /// Sem EnemyDirector ativo, a IA mantem o comportamento normal.
    /// </summary>
    public static bool RequestAttackTurn(EnemyBase enemy)
    {
        return activeDirector == null ||
            activeDirector.TryReserveAttackTurn(enemy);
    }

    private bool TryReserveAttackTurn(EnemyBase enemy)
    {
        if (!controlAttackTurns || enemy == null)
            return true;

        RefreshAttackTurns();

        if (activeAttackers.Contains(enemy))
            return true;

        if (reentryReadyAt.TryGetValue(enemy, out float readyAt) &&
            Time.time < readyAt)
        {
            return false;
        }

        if (activeAttackers.Count >= maximumConcurrentAttackers)
            return false;

        activeAttackers.Add(enemy);
        return true;
    }

    private void RefreshAttackTurns()
    {
        if (activeAttackers.Count > 0)
        {
            releasedEnemies.Clear();
            foreach (EnemyBase enemy in activeAttackers)
            {
                if (enemy == null || enemy.IsDead || !enemy.IsInAttackSequence)
                    releasedEnemies.Add(enemy);
            }

            foreach (EnemyBase enemy in releasedEnemies)
            {
                activeAttackers.Remove(enemy);
                if (enemy != null && attackerReentryCooldown > 0f)
                    reentryReadyAt[enemy] = Time.time + attackerReentryCooldown;
            }
        }

        if (reentryReadyAt.Count == 0)
            return;

        expiredCooldowns.Clear();
        foreach (KeyValuePair<EnemyBase, float> pair in reentryReadyAt)
        {
            EnemyBase enemy = pair.Key;
            if (enemy == null || enemy.IsDead || Time.time >= pair.Value)
                expiredCooldowns.Add(enemy);
        }

        foreach (EnemyBase enemy in expiredCooldowns)
            reentryReadyAt.Remove(enemy);
    }
}

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

    private readonly HashSet<BaseEnemy> activeAttackers = new();
    private readonly Dictionary<BaseEnemy, float> reentryCooldowns = new();

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
    /// Chamado pelo BaseEnemy imediatamente antes de abrir um novo combo.
    /// Sem EnemyDirector ativo, a IA mantem o comportamento normal.
    /// </summary>
    public static bool RequestAttackTurn(BaseEnemy enemy)
    {
        return activeDirector == null ||
            activeDirector.TryReserveAttackTurn(enemy);
    }

    private bool TryReserveAttackTurn(BaseEnemy enemy)
    {
        if (!controlAttackTurns || enemy == null)
            return true;

        RefreshAttackTurns();

        if (activeAttackers.Contains(enemy))
            return true;

        if (reentryCooldowns.TryGetValue(enemy, out float cooldown) &&
            cooldown > 0f)
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
            List<BaseEnemy> releasedEnemies = new();
            foreach (BaseEnemy enemy in activeAttackers)
            {
                if (enemy == null || enemy.IsDead || !enemy.IsInAttackSequence)
                    releasedEnemies.Add(enemy);
            }

            foreach (BaseEnemy enemy in releasedEnemies)
            {
                activeAttackers.Remove(enemy);
                if (enemy != null && attackerReentryCooldown > 0f)
                    reentryCooldowns[enemy] = attackerReentryCooldown;
            }
        }

        if (reentryCooldowns.Count == 0)
            return;

        List<BaseEnemy> expiredCooldowns = new();
        List<KeyValuePair<BaseEnemy, float>> updatedCooldowns = new();
        foreach (KeyValuePair<BaseEnemy, float> pair in reentryCooldowns)
        {
            BaseEnemy enemy = pair.Key;
            float remaining = pair.Value - Time.deltaTime;
            if (enemy == null || enemy.IsDead || remaining <= 0f)
                expiredCooldowns.Add(enemy);
            else
                updatedCooldowns.Add(
                    new KeyValuePair<BaseEnemy, float>(enemy, remaining)
                );
        }

        foreach (KeyValuePair<BaseEnemy, float> pair in updatedCooldowns)
            reentryCooldowns[pair.Key] = pair.Value;

        foreach (BaseEnemy enemy in expiredCooldowns)
            reentryCooldowns.Remove(enemy);
    }
}

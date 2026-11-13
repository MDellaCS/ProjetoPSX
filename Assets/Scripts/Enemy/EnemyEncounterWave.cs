using System.Collections.Generic;
using UnityEngine;

public enum EnemyEncounterWaveSpawnMode { Sequential, Overlap, Sustain }
public enum EnemyEncounterWaveSource { SceneEnemies, SpawnPrefabs }

/// <summary>Uma onda do encontro. Este objeto e filho direto do EnemyEncounter.</summary>
[DisallowMultipleComponent]
public sealed class EnemyEncounterWave : MonoBehaviour
{
    [Header("Progressao")]
    [Tooltip("Sequential aguarda a derrota das ondas anteriores. Overlap comeca apos o atraso mesmo com inimigos vivos. Sustain mantem um limite de inimigos vivos e repoe os mortos.")]
    [SerializeField] private EnemyEncounterWaveSpawnMode spawnMode;
    [Tooltip("Atraso antes de iniciar esta onda. Em Sustain, tambem e o atraso apos uma morte antes de repor.")]
    [Min(0f)] [SerializeField] private float startDelay;
    [Tooltip("Teto de inimigos vivos desta onda. Ondas com Spawn Prefabs pausam novas criacoes enquanto este limite estiver cheio e retomam quando uma vaga surge.")]
    [Min(1)] [SerializeField] private int maxAlive = 4;

    [Header("Elenco")]
    [Tooltip("Scene Enemies ativa os inimigos filhos. Spawn Prefabs instancia os prefabs na area deste objeto.")]
    [SerializeField] private EnemyEncounterWaveSource source;
    [Tooltip("Prefabs sorteados quando Source esta em Spawn Prefabs.")]
    [SerializeField] private EnemyBase[] enemyPrefabs;
    [Tooltip("Total de inimigos criados nesta onda quando Source esta em Spawn Prefabs.")]
    [Min(1)] [SerializeField] private int spawnCount = 1;
    [Tooltip("Intervalo entre criacoes de inimigos da onda. Usado somente em Spawn Prefabs fora de Sustain.")]
    [Min(0f)] [SerializeField] private float delayBetweenSpawns;
    [Tooltip("Largura e profundidade da area de spawn centrada neste objeto.")]
    [SerializeField] private Vector2 areaSize = new(8f, 8f);
    [Tooltip("Limite maximo da ampliacao da area de spawn apos tentativas sem local valido.")]
    [Min(1f)] [SerializeField] private float maximumAreaMultiplier = 1.5f;

    private EnemyEncounter encounter;
    private readonly List<EnemyBase> enemies = new();
    private int nextSceneEnemy;
    private int spawnedCount;
    private float spawnTimer;
    private float retryTimer;
    private float areaMultiplier = 1f;
    private bool started;

    public EnemyEncounterWaveSpawnMode SpawnMode => spawnMode;
    public float StartDelay => Mathf.Max(0f, startDelay);
    public Vector2 AreaSize => areaSize;
    public bool HasStarted => started;
    public int LivingCount
    {
        get
        {
            int count = 0;
            foreach (EnemyBase enemy in enemies)
                if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
                    count++;
            return count;
        }
    }
    public bool IsComplete => started && !HasPendingEnemies && LivingCount == 0;
    private bool HasPendingEnemies => source == EnemyEncounterWaveSource.SceneEnemies
        ? nextSceneEnemy < enemies.Count : spawnedCount < TotalSpawnCount;
    private int TotalSpawnCount => HasValidPrefab() ? Mathf.Max(1, spawnCount) : 0;

    internal void Prepare(EnemyEncounter owner)
    {
        encounter = owner;
        enemies.Clear();
        nextSceneEnemy = 0;
        spawnedCount = 0;
        started = false;
        if (source != EnemyEncounterWaveSource.SceneEnemies) return;
        // Filhos diretos sao o elenco em ordem. Outros componentes filhos nao contam.
        for (int i = 0; i < transform.childCount; i++)
        {
            EnemyBase enemy = transform.GetChild(i).GetComponent<EnemyBase>();
            if (enemy == null) continue;
            enemies.Add(enemy);
            enemy.SetWaitingForEncounter();
            enemy.gameObject.SetActive(false);
        }
    }

    internal void Begin(Transform player)
    {
        started = true;
        spawnTimer = 0f;
        retryTimer = 0f;
        areaMultiplier = 1f;
        Tick(0f, player);
    }

    internal void Tick(float deltaTime, Transform player)
    {
        if (!started) return;
        foreach (EnemyBase enemy in enemies)
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead &&
                !enemy.IsSpawning && player != null)
                enemy.SetEncounterEngaged(player);

        spawnTimer = Mathf.Max(0f, spawnTimer - deltaTime);
        retryTimer = Mathf.Max(0f, retryTimer - deltaTime);
        if (spawnTimer > 0f || retryTimer > 0f || !HasPendingEnemies) return;

        // Prefabs sao um spawner de fato: Max Alive e sempre um teto rigido,
        // independentemente de a progressao da onda ser Sequential, Overlap
        // ou Sustain. Assim nunca criaremos inimigos extras enquanto todos os
        // slots estiverem ocupados; Tick volta a tentar automaticamente assim
        // que LivingCount cair. Sustain aplica a mesma regra aos inimigos de
        // cena para manter o comportamento ja configurado desse modo.
        bool enforceAliveCap = source == EnemyEncounterWaveSource.SpawnPrefabs ||
            spawnMode == EnemyEncounterWaveSpawnMode.Sustain;
        int available = enforceAliveCap
            ? Mathf.Max(0, maxAlive - LivingCount)
            : int.MaxValue;
        while (available-- > 0 && HasPendingEnemies)
        {
            if (!TrySpawn(player)) break;
            if (source == EnemyEncounterWaveSource.SpawnPrefabs &&
                spawnMode != EnemyEncounterWaveSpawnMode.Sustain &&
                delayBetweenSpawns > 0f)
            {
                spawnTimer = delayBetweenSpawns;
                break;
            }
        }
    }

    internal void NotifyEnemyDied(EnemyBase enemy)
    {
        if (spawnMode == EnemyEncounterWaveSpawnMode.Sustain &&
            enemies.Contains(enemy) && HasPendingEnemies)
            spawnTimer = Mathf.Max(spawnTimer, startDelay);
    }

    private bool TrySpawn(Transform player)
    {
        if (source == EnemyEncounterWaveSource.SceneEnemies)
        {
            EnemyBase enemy = enemies[nextSceneEnemy++];
            if (enemy != null && !enemy.IsDead)
                ActivateEnemy(enemy, player);
            return true;
        }

        if (!encounter.TryFindSpawnPosition(this, areaMultiplier, out Vector3 position))
        {
            areaMultiplier = Mathf.Min(maximumAreaMultiplier,
                areaMultiplier + encounter.AreaGrowthPerRetry);
            retryTimer = encounter.RetryDelay;
            return false;
        }

        EnemyBase prefab = ChoosePrefab();
        if (prefab == null) return false;
        EnemyBase spawned = Instantiate(prefab, position,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        enemies.Add(spawned);
        spawnedCount++;
        spawned.SetWaitingForEncounter();
        ActivateEnemy(spawned, player);
        areaMultiplier = 1f;
        return true;
    }

    private static void ActivateEnemy(EnemyBase enemy, Transform player)
    {
        enemy.gameObject.SetActive(true);
        // O alvo entra na lista de combate imediatamente para poder receber
        // lock-on durante a apresentacao, mas EnemyBase bloqueia IA enquanto
        // IsSpawning estiver ativo.
        if (player != null)
            enemy.SetEncounterEngaged(player);
        EnemySpawnPresentation spawn = enemy.GetComponent<EnemySpawnPresentation>();
        spawn?.PlayEncounterSpawn();
    }

    private bool HasValidPrefab()
    {
        if (enemyPrefabs == null) return false;
        foreach (EnemyBase prefab in enemyPrefabs)
            if (prefab != null) return true;
        return false;
    }

    private EnemyBase ChoosePrefab()
    {
        int count = 0;
        foreach (EnemyBase prefab in enemyPrefabs)
            if (prefab != null) count++;
        if (count == 0) return null;
        int index = Random.Range(0, count);
        foreach (EnemyBase prefab in enemyPrefabs)
            if (prefab != null && index-- == 0) return prefab;
        return null;
    }

    private void OnValidate()
    {
        areaSize.x = Mathf.Max(.1f, areaSize.x);
        areaSize.y = Mathf.Max(.1f, areaSize.y);
        maxAlive = Mathf.Max(1, maxAlive);
    }

    private void OnDrawGizmos()
    {
        if (source != EnemyEncounterWaveSource.SpawnPrefabs ||
            (Application.isPlaying && !DebugGizmoSettings.Enabled(DebugGizmoGroup.Encounters))) return;
        Gizmos.color = new Color(.2f, .9f, 1f, .8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, .1f, areaSize.y));
        Gizmos.matrix = Matrix4x4.identity;
    }
}

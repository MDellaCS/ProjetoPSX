using System.Collections.Generic;
using UnityEngine;

public enum EnemyEncounterWaveSpawnMode
{
    Sequential,
    Overlap,
    Sustain
}

[System.Serializable]
public class EnemyEncounterWave
{
    public string waveName = "Wave";

    [Tooltip("Pai cujos filhos diretos sao os inimigos desta onda. Tem prioridade sobre Enemies e respeita a ordem na Hierarchy.")]
    public Transform enemiesParent;

    [Tooltip(
        "Sequential: nasce somente quando todas as ondas anteriores forem derrotadas.\n" +
        "Overlap: nasce apos Spawn Delay mesmo com inimigos de ondas anteriores vivos.\n" +
        "Sustain: nasce ate Max Alive inimigos; cada morte repoe um apos Spawn Delay, ate acabar a lista."
    )]
    public EnemyEncounterWaveSpawnMode spawnMode =
        EnemyEncounterWaveSpawnMode.Sequential;

    [Min(0f)]
    [Tooltip("Espera antes desta onda nascer. Em Sustain, tambem e a espera para repor inimigos mortos.")]
    public float spawnDelay;

    [Min(1)]
    [Tooltip("Maximo de inimigos vivos simultaneos. Usado apenas por Sustain.")]
    public int maxAlive = 4;

    [Tooltip("Lista manual usada apenas quando Enemies Parent estiver vazio.")]
    public EnemyBase[] enemies;
}

/// <summary>
/// Controla um encontro configuravel por ondas: bloqueia a arena, ativa
/// inimigos por politicas de spawn e conclui ao derrotar todos os participantes.
/// </summary>
public class EnemyEncounter : MonoBehaviour
{
    private static readonly HashSet<EnemyEncounter> activeEncounters =
        new HashSet<EnemyEncounter>();

    public enum EncounterState
    {
        Inactive,
        Active,
        Completed
    }

    [Header("Waves")]
    [SerializeField] private EnemyEncounterWave[] waves;

    [Header("Barriers")]
    [SerializeField] private EncounterBarrier[] barriers;

    [Header("Player")]
    [SerializeField] private Transform player;

    [SerializeField] private EncounterState currentState =
        EncounterState.Inactive;

    private readonly HashSet<EnemyBase> participants =
        new HashSet<EnemyBase>();
    private readonly Dictionary<EnemyEncounterWave, EnemyBase[]> waveEnemyCache =
        new Dictionary<EnemyEncounterWave, EnemyBase[]>();

    private int currentWaveIndex = -1;
    private int nextWaveIndex;
    private float nextWaveSpawnTimer;
    private bool isWaitingToSpawnWave;

    private int maintainCapWaveIndex = -1;
    private int nextCapEnemyIndex;
    private float capRefillTimer;
    private bool isWaitingToRefillCap;

    public EncounterState CurrentState => currentState;
    public int CurrentWaveIndex => currentWaveIndex;
    public int WaveCount => waves != null ? waves.Length : 0;
    public int LivingEnemyCount => CountLivingEnemiesInStartedWaves();

    public static bool IsPlayerInActiveEncounter(Transform player)
    {
        if (player == null)
            return false;

        foreach (EnemyEncounter encounter in activeEncounters)
        {
            if (encounter != null &&
                encounter.currentState == EncounterState.Active &&
                encounter.player == player)
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        CacheWaveEnemies();

        for (int waveIndex = 0; waveIndex < WaveCount; waveIndex++)
        {
            foreach (EnemyBase enemy in GetWaveEnemies(waveIndex))
                PrepareEnemyForEncounter(enemy);
        }
    }

    private void OnEnable()
    {
        activeEncounters.Add(this);
        EnemyBase.EnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        activeEncounters.Remove(this);
        EnemyBase.EnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        ResolvePlayer();
    }

    private void Update()
    {
        if (currentState != EncounterState.Active)
            return;

        KeepEnemiesEngaged();
        UpdateWaveSpawning();
        UpdateCapRefill();
        TryScheduleEligibleNextWave();
        TryCompleteEncounter();
    }

    public void TryStartEncounter(Transform enteringPlayer)
    {
        if (currentState != EncounterState.Inactive)
            return;

        if (enteringPlayer != null)
            player = enteringPlayer;

        ResolvePlayer();
        currentState = EncounterState.Active;
        LockBarriers();
        currentWaveIndex = -1;
        nextWaveIndex = 0;
        isWaitingToSpawnWave = false;
        maintainCapWaveIndex = -1;
        isWaitingToRefillCap = false;

        TryScheduleEligibleNextWave();
    }

    private void HandleEnemyDied(EnemyBase enemy)
    {
        if (currentState != EncounterState.Active ||
            enemy == null ||
            !participants.Contains(enemy))
        {
            return;
        }

        if (IsEnemyInMaintainCapWave(enemy))
            RequestCapRefill();

        TryFinishMaintainCapWave();
        TryScheduleEligibleNextWave();
        TryCompleteEncounter();
    }

    private void CompleteEncounter()
    {
        if (currentState != EncounterState.Active)
            return;

        currentState = EncounterState.Completed;
        UnlockBarriers();
    }

    private void KeepEnemiesEngaged()
    {
        if (player == null)
            ResolvePlayer();

        if (player == null)
            return;

        foreach (EnemyBase enemy in participants)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            enemy.SetEncounterEngaged(player);
        }
    }

    private void TryScheduleEligibleNextWave()
    {
        if (isWaitingToSpawnWave || nextWaveIndex >= WaveCount)
            return;

        EnemyEncounterWave nextWave = waves[nextWaveIndex];
        bool isFirstWave = nextWaveIndex == 0;
        bool canOverlap = nextWave != null &&
            nextWave.spawnMode == EnemyEncounterWaveSpawnMode.Overlap &&
            maintainCapWaveIndex < 0;

        if (isFirstWave || canOverlap || CanAdvanceSequentially())
            ScheduleNextWave();
    }

    private bool CanAdvanceSequentially()
    {
        return !HasPendingMaintainCapEnemies() &&
            CountLivingEnemiesInStartedWaves() == 0;
    }

    private void ScheduleNextWave()
    {
        if (nextWaveIndex >= WaveCount)
            return;

        EnemyEncounterWave nextWave = waves[nextWaveIndex];
        nextWaveSpawnTimer = nextWave != null
            ? Mathf.Max(0f, nextWave.spawnDelay)
            : 0f;
        isWaitingToSpawnWave = true;

        if (nextWaveSpawnTimer <= 0f)
            SpawnNextWave();
    }

    private void UpdateWaveSpawning()
    {
        if (!isWaitingToSpawnWave)
            return;

        nextWaveSpawnTimer -= Time.deltaTime;
        if (nextWaveSpawnTimer <= 0f)
            SpawnNextWave();
    }

    private void SpawnNextWave()
    {
        isWaitingToSpawnWave = false;

        if (nextWaveIndex >= WaveCount)
            return;

        currentWaveIndex = nextWaveIndex;
        nextWaveIndex++;

        EnemyEncounterWave wave = waves[currentWaveIndex];
        if (wave == null)
            return;

        if (wave.spawnMode == EnemyEncounterWaveSpawnMode.Sustain)
        {
            BeginMaintainCapWave(currentWaveIndex);
        }
        else
        {
            ActivateAllEnemiesInWave(currentWaveIndex);
        }

        KeepEnemiesEngaged();
    }

    private void ActivateAllEnemiesInWave(int waveIndex)
    {
        foreach (EnemyBase enemy in GetWaveEnemies(waveIndex))
        {
            if (enemy != null && !enemy.IsDead)
                enemy.gameObject.SetActive(true);
        }
    }

    private void BeginMaintainCapWave(int waveIndex)
    {
        maintainCapWaveIndex = waveIndex;
        nextCapEnemyIndex = 0;
        isWaitingToRefillCap = false;
        ActivateMaintainCapSlots();
        TryFinishMaintainCapWave();
    }

    private void RequestCapRefill()
    {
        if (!HasPendingMaintainCapEnemies() || isWaitingToRefillCap)
            return;

        EnemyEncounterWave wave = waves[maintainCapWaveIndex];
        capRefillTimer = Mathf.Max(0f, wave.spawnDelay);
        isWaitingToRefillCap = true;

        if (capRefillTimer <= 0f)
            RefillMaintainCapSlots();
    }

    private void UpdateCapRefill()
    {
        if (!isWaitingToRefillCap)
            return;

        capRefillTimer -= Time.deltaTime;
        if (capRefillTimer <= 0f)
            RefillMaintainCapSlots();
    }

    private void RefillMaintainCapSlots()
    {
        isWaitingToRefillCap = false;
        ActivateMaintainCapSlots();
        KeepEnemiesEngaged();
        TryFinishMaintainCapWave();
    }

    private void ActivateMaintainCapSlots()
    {
        if (maintainCapWaveIndex < 0 || maintainCapWaveIndex >= WaveCount)
            return;

        EnemyEncounterWave wave = waves[maintainCapWaveIndex];
        EnemyBase[] waveEnemies = GetWaveEnemies(maintainCapWaveIndex);
        int maxAlive = Mathf.Max(1, wave.maxAlive);
        int livingEnemies = CountLivingActiveEnemiesInWave(maintainCapWaveIndex);

        while (livingEnemies < maxAlive && nextCapEnemyIndex < waveEnemies.Length)
        {
            EnemyBase enemy = waveEnemies[nextCapEnemyIndex++];
            if (enemy == null || enemy.IsDead)
                continue;

            enemy.gameObject.SetActive(true);
            livingEnemies++;
        }
    }

    private void TryFinishMaintainCapWave()
    {
        if (maintainCapWaveIndex < 0 ||
            HasPendingMaintainCapEnemies() ||
            isWaitingToRefillCap ||
            CountLivingActiveEnemiesInWave(maintainCapWaveIndex) > 0)
        {
            return;
        }

        maintainCapWaveIndex = -1;
    }

    private bool IsEnemyInMaintainCapWave(EnemyBase enemy)
    {
        if (maintainCapWaveIndex < 0)
            return false;

        foreach (EnemyBase waveEnemy in GetWaveEnemies(maintainCapWaveIndex))
        {
            if (waveEnemy == enemy)
                return true;
        }

        return false;
    }

    private bool HasPendingMaintainCapEnemies()
    {
        return maintainCapWaveIndex >= 0 &&
            nextCapEnemyIndex < GetWaveEnemies(maintainCapWaveIndex).Length;
    }

    private void PrepareEnemyForEncounter(EnemyBase enemy)
    {
        if (enemy == null || !participants.Add(enemy))
            return;

        enemy.SetWaitingForEncounter();
        enemy.gameObject.SetActive(false);
    }

    private int CountLivingEnemiesInStartedWaves()
    {
        int livingEnemies = 0;

        for (int waveIndex = 0; waveIndex < nextWaveIndex; waveIndex++)
            livingEnemies += CountLivingActiveEnemiesInWave(waveIndex);

        return livingEnemies;
    }

    private int CountLivingActiveEnemiesInWave(int waveIndex)
    {
        int livingEnemies = 0;

        foreach (EnemyBase enemy in GetWaveEnemies(waveIndex))
        {
            if (enemy != null &&
                enemy.gameObject.activeInHierarchy &&
                !enemy.IsDead)
            {
                livingEnemies++;
            }
        }

        return livingEnemies;
    }

    private void TryCompleteEncounter()
    {
        if (nextWaveIndex < WaveCount ||
            isWaitingToSpawnWave ||
            HasPendingMaintainCapEnemies() ||
            isWaitingToRefillCap ||
            CountLivingEnemiesInStartedWaves() > 0)
        {
            return;
        }

        CompleteEncounter();
    }

    private EnemyBase[] GetWaveEnemies(int waveIndex)
    {
        if (waves == null || waveIndex < 0 || waveIndex >= waves.Length)
            return System.Array.Empty<EnemyBase>();

        EnemyEncounterWave wave = waves[waveIndex];
        if (wave == null)
            return System.Array.Empty<EnemyBase>();

        if (waveEnemyCache.TryGetValue(wave, out EnemyBase[] cachedEnemies))
            return cachedEnemies;

        return GetConfiguredWaveEnemies(wave);
    }

    private void CacheWaveEnemies()
    {
        waveEnemyCache.Clear();

        if (waves == null)
            return;

        foreach (EnemyEncounterWave wave in waves)
        {
            if (wave != null)
                waveEnemyCache[wave] = GetConfiguredWaveEnemies(wave);
        }
    }

    private static EnemyBase[] GetConfiguredWaveEnemies(EnemyEncounterWave wave)
    {
        if (wave.enemiesParent == null)
            return wave.enemies ?? System.Array.Empty<EnemyBase>();

        // GetChild segue exatamente a ordem visivel da Hierarchy. Buscamos
        // apenas filhos diretos para um pai de onda nao absorver inimigos de
        // subgrupos/ondas vizinhas por acidente.
        List<EnemyBase> enemiesFromParent = new List<EnemyBase>();
        for (int childIndex = 0;
             childIndex < wave.enemiesParent.childCount;
             childIndex++)
        {
            Transform child = wave.enemiesParent.GetChild(childIndex);
            EnemyBase enemy = child.GetComponent<EnemyBase>();
            if (enemy != null)
                enemiesFromParent.Add(enemy);
        }

        return enemiesFromParent.ToArray();
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void LockBarriers()
    {
        foreach (EncounterBarrier barrier in barriers)
            barrier?.Lock();
    }

    private void UnlockBarriers()
    {
        foreach (EncounterBarrier barrier in barriers)
            barrier?.Unlock();
    }
}

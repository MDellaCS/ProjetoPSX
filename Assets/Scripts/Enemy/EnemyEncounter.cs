using System.Collections.Generic;
using UnityEngine;

/// <summary>Orquestra as ondas filhas, o player e as barreiras da arena.</summary>
[DisallowMultipleComponent]
public sealed class EnemyEncounter : MonoBehaviour
{
    private static readonly HashSet<EnemyEncounter> active = new();
    public enum EncounterState { Inactive, Active, Completed }

    [Header("Arena")]
    [Tooltip("Barreiras fechadas ao iniciar e abertas ao concluir o encontro.")]
    [SerializeField] private EncounterBarrier[] barriers;
    [Tooltip("Player do encontro. Se vazio, usa a tag Player.")]
    [SerializeField] private Transform player;

    [Header("Conclusao")]
    [Tooltip("Tempo em segundos entre a derrota dos ultimos inimigos e a conclusao do encontro. As barreiras permanecem fechadas durante esse intervalo.")]
    [Min(0f)] [SerializeField] private float completionDelayAfterLastEnemyDeath = 2f;

    [Header("Spawn Global - Chao")]
    [Tooltip("Altura acima da area para procurar o chao.")]
    [Min(0f)] [SerializeField] private float spawnRayHeight = 10f;
    [Tooltip("Distancia maxima do raycast ate o chao.")]
    [Min(.1f)] [SerializeField] private float groundRayDistance = 30f;
    [Tooltip("Altura adicional aplicada ao inimigo criado.")]
    [Min(0f)] [SerializeField] private float groundOffset = .05f;
    [Tooltip("Camadas aceitas como chao.")]
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

    [Header("Spawn Global - Seguranca")]
    [Tooltip("Distancia horizontal minima entre o spawn e o player.")]
    [Min(0f)] [SerializeField] private float playerExclusionRadius = 3f;
    [Tooltip("Raio da zona circular preferida para nascer ao redor do player. O encontro tenta esta zona antes da area total da onda, sempre respeitando Player Exclusion Radius. Use 0 para desativar a preferencia.")]
    [Min(0f)] [SerializeField] private float preferredSpawnRadius = 8f;
    [Tooltip("Distancia horizontal minima entre o spawn e qualquer inimigo ativo.")]
    [Min(0f)] [SerializeField] private float enemyExclusionRadius = 1.5f;
    [Tooltip("Raio da capsula de espaco livre para spawn.")]
    [Min(.01f)] [SerializeField] private float spawnClearanceRadius = .6f;
    [Tooltip("Altura da capsula de espaco livre para spawn.")]
    [Min(.02f)] [SerializeField] private float spawnClearanceHeight = 2f;
    [Tooltip("Camadas que bloqueiam um ponto de spawn.")]
    [SerializeField] private LayerMask spawnBlockingLayers = Physics.DefaultRaycastLayers;

    [Header("Spawn Global - Tentativas")]
    [Tooltip("Pontos testados antes de expandir a area.")]
    [Min(1)] [SerializeField] private int spawnAttemptsPerArea = 12;
    [Tooltip("Fracao do tamanho inicial somada a area a cada rodada sem sucesso.")]
    [Min(.01f)] [SerializeField] private float spawnAreaGrowthPerRetry = .5f;
    [Tooltip("Espera entre rodadas de tentativas.")]
    [Min(0f)] [SerializeField] private float spawnRetryDelay = .05f;

    private readonly List<EnemyEncounterWave> waves = new();
    private EncounterState state;
    private int currentWaveIndex = -1;
    private int nextWaveIndex;
    private float nextWaveTimer;
    private bool nextWaveScheduled;
    private bool completionPending;
    private float completionDelayRemaining;

    public EncounterState CurrentState => state;
    public int CurrentWaveIndex => currentWaveIndex;
    public int WaveCount => waves.Count;
    public int LivingEnemyCount
    {
        get
        {
            int count = 0;
            foreach (EnemyEncounterWave wave in waves)
                if (wave.HasStarted) count += wave.LivingCount;
            return count;
        }
    }
    internal float AreaGrowthPerRetry => Mathf.Max(.01f, spawnAreaGrowthPerRetry);
    internal float RetryDelay => Mathf.Max(0f, spawnRetryDelay);

    public static bool IsPlayerInActiveEncounter(Transform target)
    {
        if (target == null) return false;
        foreach (EnemyEncounter encounter in active)
            if (encounter != null && encounter.state == EncounterState.Active &&
                encounter.player == target) return true;
        return false;
    }

    private void Awake()
    {
        // Filhos diretos, exatamente na ordem da Hierarchy.
        for (int i = 0; i < transform.childCount; i++)
        {
            EnemyEncounterWave wave = transform.GetChild(i).GetComponent<EnemyEncounterWave>();
            if (wave == null) continue;
            waves.Add(wave);
            wave.Prepare(this);
        }
    }

    private void OnEnable()
    {
        active.Add(this);
        EnemyBase.EnemyDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        active.Remove(this);
        EnemyBase.EnemyDied -= OnEnemyDied;
    }

    private void Start() => ResolvePlayer();

    private void Update()
    {
        if (state != EncounterState.Active || GamePauseController.IsPaused) return;
        ResolvePlayer();
        foreach (EnemyEncounterWave wave in waves)
            if (wave.HasStarted) wave.Tick(Time.deltaTime, player);
        if (nextWaveScheduled)
        {
            nextWaveTimer -= Time.deltaTime;
            if (nextWaveTimer <= 0f) StartNextWave();
        }
        if (completionPending)
            completionDelayRemaining -= Time.unscaledDeltaTime;
        ScheduleNextWave();
        TryComplete();
    }

    public void TryStartEncounter(Transform enteringPlayer)
    {
        if (state != EncounterState.Inactive) return;
        if (enteringPlayer != null) player = enteringPlayer;
        ResolvePlayer();
        state = EncounterState.Active;
        foreach (EncounterBarrier barrier in barriers) barrier?.Lock();
        ScheduleNextWave();
        TryComplete();
    }

    private void OnEnemyDied(EnemyBase enemy)
    {
        if (state != EncounterState.Active || enemy == null) return;
        foreach (EnemyEncounterWave wave in waves)
            if (wave.HasStarted) wave.NotifyEnemyDied(enemy);
        ScheduleNextWave();
        TryComplete(true);
    }

    private void ScheduleNextWave()
    {
        if (nextWaveScheduled || nextWaveIndex >= waves.Count) return;
        EnemyEncounterWave next = waves[nextWaveIndex];
        if (nextWaveIndex > 0 && next.SpawnMode != EnemyEncounterWaveSpawnMode.Overlap &&
            !AllStartedWavesComplete()) return;
        if (nextWaveIndex > 0 && next.SpawnMode == EnemyEncounterWaveSpawnMode.Overlap &&
            HasUnfinishedSustainWave()) return;
        nextWaveTimer = next.StartDelay;
        nextWaveScheduled = true;
        if (nextWaveTimer <= 0f) StartNextWave();
    }

    private void StartNextWave()
    {
        nextWaveScheduled = false;
        if (nextWaveIndex >= waves.Count) return;
        currentWaveIndex = nextWaveIndex++;
        waves[currentWaveIndex].Begin(player);
        ScheduleNextWave();
        TryComplete();
    }

    private bool AllStartedWavesComplete()
    {
        for (int i = 0; i < nextWaveIndex; i++)
            if (!waves[i].IsComplete) return false;
        return true;
    }

    private bool HasUnfinishedSustainWave()
    {
        for (int i = 0; i < nextWaveIndex; i++)
            if (waves[i].SpawnMode == EnemyEncounterWaveSpawnMode.Sustain &&
                !waves[i].IsComplete) return true;
        return false;
    }

    private void TryComplete(bool completedByEnemyDeath = false)
    {
        if (state != EncounterState.Active || nextWaveScheduled ||
            nextWaveIndex < waves.Count || !AllStartedWavesComplete()) return;

        if (!completionPending)
        {
            completionPending = true;
            completionDelayRemaining = Mathf.Max(0f,
                completionDelayAfterLastEnemyDeath);
            if (completedByEnemyDeath)
                GameTimeScaleEffects.PlayDefaultSlowMotion();
        }

        if (completionDelayRemaining > 0f)
            return;

        state = EncounterState.Completed;
        foreach (EncounterBarrier barrier in barriers) barrier?.Unlock();
    }

    internal bool TryFindSpawnPosition(EnemyEncounterWave wave, float multiplier,
        out Vector3 position)
    {
        // Primeiro procura perto do player, mas nunca dentro da zona segura e
        // nunca fora da area configurada da wave. A preferencia nao bloqueia
        // a onda: sem ponto valido, o metodo tenta a area completa abaixo.
        if (preferredSpawnRadius > 0f && player != null)
        {
            for (int i = 0; i < Mathf.Max(1, spawnAttemptsPerArea); i++)
            {
                Vector2 offset = Random.insideUnitCircle * preferredSpawnRadius;
                Vector3 preferred = player.position + new Vector3(offset.x,
                    spawnRayHeight, offset.y);
                if (!IsInsideWaveArea(wave, preferred, multiplier))
                    continue;
                if (TryUseSpawnCandidate(preferred, out position))
                    return true;
            }
        }

        for (int i = 0; i < Mathf.Max(1, spawnAttemptsPerArea); i++)
        {
            Vector2 half = wave.AreaSize * (.5f * multiplier);
            Vector3 local = new(Random.Range(-half.x, half.x), spawnRayHeight,
                Random.Range(-half.y, half.y));
            Vector3 origin = wave.transform.TransformPoint(local);
            if (TryUseSpawnCandidate(origin, out position))
                return true;
        }
        position = default;
        return false;
    }

    private bool TryUseSpawnCandidate(Vector3 rayOrigin, out Vector3 position)
    {
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
            groundRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            position = default;
            return false;
        }

        Vector3 candidate = hit.point + Vector3.up * groundOffset;
        if (TooCloseToPlayer(candidate) || TooCloseToEnemy(candidate) ||
            IsBlocked(candidate))
        {
            position = default;
            return false;
        }

        position = candidate;
        return true;
    }

    private static bool IsInsideWaveArea(EnemyEncounterWave wave,
        Vector3 worldPosition, float multiplier)
    {
        if (wave == null)
            return false;

        Vector3 local = wave.transform.InverseTransformPoint(worldPosition);
        Vector2 half = wave.AreaSize * (.5f * Mathf.Max(1f, multiplier));
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.y;
    }

    private bool TooCloseToPlayer(Vector3 position)
    {
        if (player == null || playerExclusionRadius <= 0f) return false;
        Vector3 offset = player.position - position;
        offset.y = 0f;
        return offset.sqrMagnitude < playerExclusionRadius * playerExclusionRadius;
    }

    private bool TooCloseToEnemy(Vector3 position)
    {
        if (enemyExclusionRadius <= 0f) return false;
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;
            Vector3 offset = enemy.AttackOrigin - position;
            offset.y = 0f;
            if (offset.sqrMagnitude < enemyExclusionRadius * enemyExclusionRadius)
                return true;
        }
        return false;
    }

    private bool IsBlocked(Vector3 position)
    {
        float radius = Mathf.Max(.01f, spawnClearanceRadius);
        Vector3 bottom = position + Vector3.up * radius;
        Vector3 top = position + Vector3.up * Mathf.Max(radius, spawnClearanceHeight - radius);
        return Physics.CheckCapsule(bottom, top, radius, spawnBlockingLayers,
            QueryTriggerInteraction.Ignore);
    }

    private void ResolvePlayer()
    {
        if (player != null) return;
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null) player = found.transform;
    }

    private void OnValidate()
    {
        spawnClearanceRadius = Mathf.Max(.01f, spawnClearanceRadius);
        spawnClearanceHeight = Mathf.Max(spawnClearanceHeight, spawnClearanceRadius * 2f);
    }

}

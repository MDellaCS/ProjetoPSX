using System.Collections;
using UnityEngine;

/// <summary>
/// Cria inimigos a partir de prefabs dentro de uma area configuravel. Cada
/// inimigo so nasce fora da zona proibida do player e em uma posicao livre.
/// Quando uma area nao possui espaco valido, ela cresce e tenta novamente.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField] private EnemyBase[] enemyPrefabs;
    [Min(1)] [SerializeField] private int spawnCount = 1;
    [SerializeField] private bool spawnOnStart = true;
    [Min(0f)] [SerializeField] private float delayBetweenSpawns;
    [SerializeField] private Transform spawnedEnemyParent;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 localAreaCenter;
    [SerializeField] private Vector2 areaSize = new Vector2(8f, 8f);
    [Min(0f)] [SerializeField] private float spawnRayHeight = 10f;
    [Min(0.1f)] [SerializeField] private float groundRayDistance = 30f;
    [Min(0f)] [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

    [Header("Player Exclusion")]
    [Min(0f)] [SerializeField] private float playerExclusionRadius = 3f;

    [Header("Spawn Clearance")]
    [Min(0.01f)] [SerializeField] private float clearanceRadius = 0.6f;
    [Min(0.02f)] [SerializeField] private float clearanceHeight = 2f;
    [SerializeField] private LayerMask blockingLayers = Physics.DefaultRaycastLayers;

    [Header("Expansion Retry")]
    [Min(1)] [SerializeField] private int attemptsPerArea = 12;
    [Tooltip("Multiplicador adicional da area depois de falhar todas as tentativas.")]
    [Min(0.01f)] [SerializeField] private float areaGrowthPerRetry = 0.5f;
    [Min(0f)] [SerializeField] private float retryDelay = 0.05f;

    private Transform player;
    private Coroutine spawnRoutine;
    private float currentAreaMultiplier = 1f;
    private int pendingSpawnCount;

    private void Start()
    {
        if (spawnOnStart)
            SpawnConfiguredEnemies();
    }

    /// <summary>
    /// Adiciona a quantidade configurada a fila de spawn.
    /// </summary>
    public void SpawnConfiguredEnemies()
    {
        QueueSpawns(spawnCount);
    }

    /// <summary>
    /// Adiciona um unico inimigo a fila de spawn. O prefab e escolhido
    /// aleatoriamente entre Enemy Prefabs.
    /// </summary>
    public void SpawnOneEnemy()
    {
        QueueSpawns(1);
    }

    private void QueueSpawns(int amount)
    {
        if (amount <= 0)
            return;

        pendingSpawnCount += amount;

        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnQueuedEnemiesRoutine());
    }

    private IEnumerator SpawnQueuedEnemiesRoutine()
    {
        if (!HasValidPrefab())
        {
            spawnRoutine = null;
            yield break;
        }

        while (pendingSpawnCount > 0)
        {
            pendingSpawnCount--;

            EnemyBase prefab = GetRandomPrefab();
            if (prefab != null)
                yield return SpawnEnemyUntilSuccessful(prefab);

            if (delayBetweenSpawns > 0f && pendingSpawnCount > 0)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }

        currentAreaMultiplier = 1f;
        spawnRoutine = null;
    }

    private IEnumerator SpawnEnemyUntilSuccessful(EnemyBase prefab)
    {
        currentAreaMultiplier = 1f;

        while (true)
        {
            ResolvePlayer();

            for (int attempt = 0; attempt < attemptsPerArea; attempt++)
            {
                if (!TryFindSpawnPosition(
                    currentAreaMultiplier,
                    out Vector3 spawnPosition))
                {
                    continue;
                }

                EnemyBase spawnedEnemy = Instantiate(
                    prefab,
                    spawnPosition,
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    spawnedEnemyParent
                );
                spawnedEnemy.gameObject.SetActive(true);
                yield break;
            }

            currentAreaMultiplier += areaGrowthPerRetry;
            yield return retryDelay > 0f
                ? new WaitForSeconds(retryDelay)
                : null;
        }
    }

    private bool TryFindSpawnPosition(
        float areaMultiplier,
        out Vector3 spawnPosition)
    {
        Vector2 halfSize = areaSize * 0.5f * areaMultiplier;
        Vector3 localCandidate = localAreaCenter + new Vector3(
            Random.Range(-halfSize.x, halfSize.x),
            spawnRayHeight,
            Random.Range(-halfSize.y, halfSize.y)
        );
        Vector3 rayOrigin = transform.TransformPoint(localCandidate);

        if (!Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit groundHit,
            groundRayDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            spawnPosition = default;
            return false;
        }

        spawnPosition = groundHit.point + Vector3.up * groundOffset;

        if (IsTooCloseToPlayer(spawnPosition) ||
            IsSpawnPositionBlocked(spawnPosition))
        {
            return false;
        }

        return true;
    }

    private bool IsTooCloseToPlayer(Vector3 position)
    {
        if (player == null)
            return false;

        Vector3 playerOffset = player.position - position;
        playerOffset.y = 0f;
        return playerOffset.sqrMagnitude <
            playerExclusionRadius * playerExclusionRadius;
    }

    private bool IsSpawnPositionBlocked(Vector3 position)
    {
        float bottomOffset = clearanceRadius;
        float topOffset = Mathf.Max(
            bottomOffset,
            clearanceHeight - clearanceRadius
        );
        Vector3 bottom = position + Vector3.up * bottomOffset;
        Vector3 top = position + Vector3.up * topOffset;

        return Physics.CheckCapsule(
            bottom,
            top,
            clearanceRadius,
            blockingLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private bool HasValidPrefab()
    {
        if (enemyPrefabs == null)
            return false;

        foreach (EnemyBase prefab in enemyPrefabs)
        {
            if (prefab != null)
                return true;
        }

        return false;
    }

    private EnemyBase GetRandomPrefab()
    {
        int validPrefabCount = 0;
        foreach (EnemyBase prefab in enemyPrefabs)
        {
            if (prefab != null)
                validPrefabCount++;
        }

        if (validPrefabCount == 0)
            return null;

        int selectedIndex = Random.Range(0, validPrefabCount);
        foreach (EnemyBase prefab in enemyPrefabs)
        {
            if (prefab == null)
                continue;

            if (selectedIndex-- == 0)
                return prefab;
        }

        return null;
    }

    private void OnValidate()
    {
        areaSize.x = Mathf.Max(0.1f, areaSize.x);
        areaSize.y = Mathf.Max(0.1f, areaSize.y);
        clearanceHeight = Mathf.Max(
            clearanceHeight,
            clearanceRadius * 2f
        );
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !PlayerDebugToolkitHUD.IsVisible)
            return;

        DrawSpawnAreaGizmo(1f, new Color(0.2f, 0.9f, 1f, 0.8f));

        if (Application.isPlaying && currentAreaMultiplier > 1f)
        {
            DrawSpawnAreaGizmo(
                currentAreaMultiplier,
                new Color(1f, 0.75f, 0.15f, 0.9f)
            );
        }

        ResolvePlayer();
        if (player != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(player.position, playerExclusionRadius);
        }
    }

    private void DrawSpawnAreaGizmo(float multiplier, Color color)
    {
        Gizmos.color = color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(
            localAreaCenter,
            new Vector3(
                areaSize.x * multiplier,
                0.1f,
                areaSize.y * multiplier
            )
        );
        Gizmos.matrix = Matrix4x4.identity;
    }
}

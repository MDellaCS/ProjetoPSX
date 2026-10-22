using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla um unico alvo travado pelo player. O alvo e escolhido entre os
/// EnemyBase ativos, pode ser alternado pelo scroll e recebe um marcador verde.
/// </summary>
public class PlayerLockOn : MonoBehaviour
{
    [Header("Targeting")]
    [Min(0f)] [SerializeField] private float maximumLockDistance = 14f;

    [Header("Marker")]
    [Min(1f)] [SerializeField] private float markerScreenSize = 16f;
    [SerializeField] private Color markerColor = new(.15f, 1f, .15f, 1f);

    private readonly List<EnemyBase> candidates = new();
    private EnemyBase lockedEnemy;
    private static Texture2D markerTexture;

    public bool IsLockedOn => lockedEnemy != null && lockedEnemy.CanBeLockOnTarget;
    public EnemyBase LockedEnemy => IsLockedOn ? lockedEnemy : null;
    public Transform LockedTarget => IsLockedOn ? lockedEnemy.transform : null;
    public Vector3 LockPoint => IsLockedOn
        ? GetEnemyCenter(lockedEnemy)
        : transform.position;

    private void Update()
    {
        ValidateLockedTarget();

        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked)
            return;

        if (GameInput.LockOnPressed)
        {
            if (IsLockedOn)
                ClearLock();
            else
                LockBestAvailableTarget();
        }

        if (IsLockedOn)
        {
            float scroll = GameInput.CycleTarget.y;
            if (scroll > .01f)
                CycleTarget(1);
            else if (scroll < -.01f)
                CycleTarget(-1);
        }

    }

    private void OnGUI()
    {
        if (!IsLockedOn)
            return;

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
            return;

        Vector3 screenPosition = gameCamera.WorldToScreenPoint(LockPoint);
        if (screenPosition.z <= 0f)
            return;

        float halfSize = markerScreenSize * .5f;
        Rect markerRect = new(
            screenPosition.x - halfSize,
            Screen.height - screenPosition.y - halfSize,
            markerScreenSize,
            markerScreenSize
        );

        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -100;
        GUI.color = markerColor;
        GUI.DrawTexture(markerRect, GetMarkerTexture());
        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    public Vector3 GetDirectionToLockedTarget()
    {
        if (!IsLockedOn)
            return Vector3.zero;

        Vector3 direction = LockPoint - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > .0001f
            ? direction.normalized
            : Vector3.zero;
    }

    public static PlayerLockOn GetOrAdd(GameObject player)
    {
        PlayerLockOn lockOn = player.GetComponent<PlayerLockOn>();
        return lockOn != null ? lockOn : player.AddComponent<PlayerLockOn>();
    }

    private void LockBestAvailableTarget()
    {
        RefreshCandidates();
        if (candidates.Count == 0)
            return;

        Camera gameCamera = Camera.main;
        EnemyBase bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (EnemyBase candidate in candidates)
        {
            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            float score = distance;

            if (gameCamera != null)
            {
                Vector3 viewport = gameCamera.WorldToViewportPoint(
                    GetEnemyCenter(candidate)
                );
                if (viewport.z <= 0f)
                    continue;

                Vector2 viewportOffset = new(
                    viewport.x - .5f,
                    viewport.y - .5f
                );
                score += viewportOffset.sqrMagnitude * maximumLockDistance;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        if (bestTarget != null)
            SetLock(bestTarget);
    }

    private void CycleTarget(int direction)
    {
        RefreshCandidates();
        if (candidates.Count == 0)
        {
            ClearLock();
            return;
        }

        int currentIndex = candidates.IndexOf(lockedEnemy);
        if (currentIndex < 0)
        {
            SetLock(candidates[0]);
            return;
        }

        int nextIndex = (currentIndex + direction) % candidates.Count;
        if (nextIndex < 0)
            nextIndex += candidates.Count;

        SetLock(candidates[nextIndex]);
    }

    private void RefreshCandidates()
    {
        candidates.Clear();
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>();

        foreach (EnemyBase enemy in enemies)
        {
            if (enemy == null || !enemy.CanBeLockOnTarget ||
                Vector3.Distance(transform.position, enemy.transform.position) >
                    maximumLockDistance)
            {
                continue;
            }

            candidates.Add(enemy);
        }

        candidates.Sort((first, second) =>
            Vector3.SqrMagnitude(first.transform.position - transform.position)
                .CompareTo(Vector3.SqrMagnitude(
                    second.transform.position - transform.position
                ))
        );
    }

    private void SetLock(EnemyBase enemy)
    {
        lockedEnemy = enemy;
    }

    private void ClearLock()
    {
        lockedEnemy = null;
    }

    private void ValidateLockedTarget()
    {
        if (lockedEnemy == null)
            return;

        if (!lockedEnemy.CanBeLockOnTarget ||
            Vector3.Distance(transform.position, lockedEnemy.transform.position) >
                maximumLockDistance)
        {
            ClearLock();
            LockBestAvailableTarget();
        }
    }

    private static Vector3 GetEnemyCenter(EnemyBase enemy)
    {
        Renderer enemyRenderer = enemy.GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
            return enemyRenderer.bounds.center;

        CharacterController controller = enemy.GetComponent<CharacterController>();
        return controller != null
            ? controller.bounds.center
            : enemy.transform.position + Vector3.up;
    }

    private static Texture2D GetMarkerTexture()
    {
        if (markerTexture != null)
            return markerTexture;

        const int textureSize = 16;
        markerTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false
        );
        markerTexture.filterMode = FilterMode.Point;

        Vector2 center = Vector2.one * (textureSize - 1) * .5f;
        float radius = textureSize * .5f;
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance <= radius ? 1f : 0f;
                markerTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        markerTexture.Apply();
        return markerTexture;
    }
}

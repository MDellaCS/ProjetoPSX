using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla um unico alvo travado pelo player. O alvo e escolhido entre os
/// inimigos e objetos WorldLockOnTarget ativos, com scroll e marcador verde.
/// </summary>
public class PlayerLockOn : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Distancia maxima, em unidades do mundo, para selecionar e manter um alvo travado.")]
    [Min(0f)] [SerializeField] private float maximumLockDistance = 14f;

    private readonly List<Component> candidates = new();
    private Component lockedEnemy;
    private int lastCycleDirection;

    public bool IsLockedOn => lockedEnemy != null && CanTarget(lockedEnemy);
    public EnemyBase LockedEnemy => IsLockedOn ? lockedEnemy as EnemyBase : null;
    public Transform LockedTarget => IsLockedOn ? lockedEnemy.transform : null;
    public Vector3 LockPoint => IsLockedOn
        ? GetEnemyCenter(lockedEnemy)
        : transform.position;

    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked)
            return;

        ValidateLockedTarget();

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
            int cycleDirection = scroll > .01f ? 1 : scroll < -.01f ? -1 : 0;
            // O D-pad mantem o valor enquanto pressionado; so troca na borda.
            bool isNewPress = cycleDirection != 0 &&
                (cycleDirection != lastCycleDirection ||
                 !GameInput.CycleTargetIsHeldInput);
            if (isNewPress)
                CycleTarget(cycleDirection);
            lastCycleDirection = cycleDirection;
        }
        else
        {
            lastCycleDirection = 0;
        }

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
        Component bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Component candidate in candidates)
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
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy == null || !enemy.CanBeLockOnTarget ||
                Vector3.Distance(transform.position, enemy.transform.position) >
                    maximumLockDistance)
            {
                continue;
            }

            InsertCandidateByDistance(enemy);
        }

        foreach (WorldLockOnTarget target in WorldLockOnTarget.ActiveTargets)
            if (target.IsAvailable && Vector3.Distance(transform.position, target.Center) <= maximumLockDistance)
                InsertCandidateByDistance(target);
    }

    private void InsertCandidateByDistance(Component candidate)
    {
        float candidateDistance = (candidate.transform.position -
            transform.position).sqrMagnitude;
        int index = candidates.Count;
        while (index > 0 &&
            (candidates[index - 1].transform.position - transform.position)
                .sqrMagnitude > candidateDistance)
            index--;
        candidates.Insert(index, candidate);
    }

    private void SetLock(Component enemy)
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

        if (!CanTarget(lockedEnemy) ||
            Vector3.Distance(transform.position, lockedEnemy.transform.position) >
                maximumLockDistance)
        {
            ClearLock();
            LockBestAvailableTarget();
        }
    }

    private static Vector3 GetEnemyCenter(Component enemy)
    {
        if (enemy is WorldLockOnTarget worldTarget)
            return worldTarget.Center;
        return enemy is EnemyBase enemyBase
            ? enemyBase.AttackOrigin
            : enemy.transform.position + Vector3.up;
    }

    private static bool CanTarget(Component target) => target is EnemyBase enemy
        ? enemy.CanBeLockOnTarget : target is WorldLockOnTarget world && world.IsAvailable;

}

using System.Collections.Generic;
using UnityEngine;

/// <summary>Permite que objetos interativos participem da mira sem possuir IA de inimigo.</summary>
[DisallowMultipleComponent]
public sealed class WorldLockOnTarget : MonoBehaviour
{
    private static readonly HashSet<WorldLockOnTarget> activeTargets = new();
    internal static HashSet<WorldLockOnTarget> ActiveTargets => activeTargets;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTargets() => activeTargets.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RebuildTargets()
    {
        foreach (WorldLockOnTarget target in Object.FindObjectsByType<
            WorldLockOnTarget>(FindObjectsInactive.Exclude))
            activeTargets.Add(target);
    }

    public bool IsAvailable => isActiveAndEnabled && gameObject.activeInHierarchy;
    public Vector3 Center => TryGetComponent(out Collider collider)
        ? collider.bounds.center : transform.position;

    private void OnEnable() => activeTargets.Add(this);
    private void OnDisable() => activeTargets.Remove(this);
}

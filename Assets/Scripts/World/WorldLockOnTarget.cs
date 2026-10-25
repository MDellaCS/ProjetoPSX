using UnityEngine;

/// <summary>Permite que objetos interativos participem da mira sem possuir IA de inimigo.</summary>
[DisallowMultipleComponent]
public sealed class WorldLockOnTarget : MonoBehaviour
{
    public bool IsAvailable => isActiveAndEnabled && gameObject.activeInHierarchy;
    public Vector3 Center => TryGetComponent(out Collider collider)
        ? collider.bounds.center : transform.position;
}

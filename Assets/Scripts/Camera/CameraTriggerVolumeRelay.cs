using System.Collections.Generic;
using UnityEngine;

/// <summary>Encaminha entradas de colliders adicionais ao trigger de camera dono.</summary>
[DisallowMultipleComponent]
public sealed class CameraTriggerVolumeRelay : MonoBehaviour
{
    private readonly List<FixedCameraZone> owners = new();

    internal void Register(FixedCameraZone owner)
    {
        if (owner != null && !owners.Contains(owner))
            owners.Add(owner);
        enabled = owners.Count > 0;
    }

    internal void Unregister(FixedCameraZone owner)
    {
        owners.Remove(owner);
        for (int i = owners.Count - 1; i >= 0; i--)
        {
            if (owners[i] == null)
                owners.RemoveAt(i);
        }

        enabled = owners.Count > 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        for (int i = owners.Count - 1; i >= 0; i--)
        {
            FixedCameraZone owner = owners[i];
            if (owner == null)
            {
                owners.RemoveAt(i);
                continue;
            }
            owner.HandleTriggerEnter(other);
        }
    }
}

using UnityEngine;

/// <summary>
/// Controla apenas o bloqueio fisico e visual de uma passagem.
/// </summary>
public class EncounterBarrier : MonoBehaviour
{
    [Header("Barrier Parts")]
    [SerializeField] private Collider[] blockingColliders;
    [SerializeField] private GameObject[] barrierVisuals;

    [Header("Initial State")]
    [SerializeField] private bool startsLocked;

    public bool IsLocked { get; private set; }

    private void Awake()
    {
        SetLocked(startsLocked);
    }

    public void Lock()
    {
        SetLocked(true);
    }

    public void Unlock()
    {
        SetLocked(false);
    }

    private void SetLocked(bool locked)
    {
        IsLocked = locked;

        foreach (Collider blockingCollider in blockingColliders)
        {
            if (blockingCollider != null)
                blockingCollider.enabled = locked;
        }

        foreach (GameObject barrierVisual in barrierVisuals)
        {
            if (barrierVisual != null)
                barrierVisual.SetActive(locked);
        }
    }
}

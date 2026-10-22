using UnityEngine;

/// <summary>
/// Ação simples e reutilizável para ser chamada por Signals ou UnityEvents de
/// uma Timeline. Métodos públicos aparecem no Signal Receiver.
/// </summary>
public sealed class CutsceneObjectAction : MonoBehaviour
{
    [Tooltip("Objeto do cenário afetado pelos métodos desta ação.")]
    [SerializeField] private GameObject target;

    public void HideTarget()
    {
        if (target != null)
            target.SetActive(false);
    }

    public void ShowTarget()
    {
        if (target != null)
            target.SetActive(true);
    }

    public void ToggleTarget()
    {
        if (target != null)
            target.SetActive(!target.activeSelf);
    }
}

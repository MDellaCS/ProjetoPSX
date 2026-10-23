using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Timeline;

/// <summary>
/// Ação simples e reutilizável para ser chamada por Signals ou UnityEvents de
/// uma Timeline. Métodos públicos aparecem no Signal Receiver.
/// </summary>
public sealed class CutsceneObjectAction : MonoBehaviour
{
    [Tooltip("Objeto do cenário afetado pelos métodos desta ação.")]
    [SerializeField] private GameObject target;

    [Header("Signal Persistente")]
    [Tooltip("Signal opcional que chama Hide Target mesmo quando o Signal Receiver esta na Main Camera persistente de outra cena.")]
    [SerializeField] private SignalAsset hideTargetOnSignal;

    private UnityEvent registeredReaction;

    private void OnEnable()
    {
        if (hideTargetOnSignal != null)
            StartCoroutine(RegisterPersistentSignalNextFrame());
    }

    private void OnDisable()
    {
        if (registeredReaction != null)
            registeredReaction.RemoveListener(HideTarget);
        registeredReaction = null;
    }

    private IEnumerator RegisterPersistentSignalNextFrame()
    {
        // O Receiver pertence a Main Camera no GameRoot. Aguardar um frame
        // tambem cobre o carregamento de fase logo apos o Bootstrap.
        yield return null;
        if (!isActiveAndEnabled || hideTargetOnSignal == null)
            yield break;

        SignalReceiver receiver = FindAnyObjectByType<SignalReceiver>();
        if (receiver == null)
            yield break;

        registeredReaction = receiver.GetReaction(hideTargetOnSignal);
        if (registeredReaction == null)
        {
            registeredReaction = new UnityEvent();
            receiver.AddReaction(hideTargetOnSignal, registeredReaction);
        }
        registeredReaction.AddListener(HideTarget);
    }

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

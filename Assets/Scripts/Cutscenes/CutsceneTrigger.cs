using UnityEngine;

/// <summary>Inicia uma CutsceneController quando o player entra nesta área.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class CutsceneTrigger : MonoBehaviour
{
    [Tooltip("Controlador da cutscene iniciada por esta área.")]
    [SerializeField] private CutsceneController cutscene;
    [Tooltip("Impede que esta área inicie a cutscene mais de uma vez.")]
    [SerializeField] private bool playOnce = true;
    [Tooltip("Desativa o trigger após iniciar a cutscene com sucesso.")]
    [SerializeField] private bool disableAfterStart = true;

    private bool hasPlayed;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed && playOnce)
            return;
        if (other.GetComponentInParent<PlayerMovement>() == null &&
            !other.CompareTag("Player"))
            return;
        if (cutscene == null || !cutscene.PlayCutscene())
            return;

        hasPlayed = true;
        if (disableAfterStart)
            gameObject.SetActive(false);
    }
}

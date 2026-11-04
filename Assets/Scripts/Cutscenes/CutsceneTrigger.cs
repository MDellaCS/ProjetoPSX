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
    private bool playerInside;
    private PlayerMovement playerMovement;
    private Collider triggerCollider;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void Update()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();
        bool insideNow = PlayerAreaOverlap.ContainsPlayer(
            triggerCollider, playerMovement);
        if (!insideNow)
        {
            playerInside = false;
            return;
        }

        if (playerInside)
            return;

        playerInside = true;
        TryStartCutscene();
    }

    private void TryStartCutscene()
    {
        if (hasPlayed && playOnce)
            return;
        if (cutscene == null || !cutscene.PlayCutscene())
            return;

        hasPlayed = true;
        if (disableAfterStart)
            gameObject.SetActive(false);
    }
}

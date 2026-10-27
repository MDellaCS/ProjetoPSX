using UnityEngine;

/// <summary>Exibe uma sequencia de dialogo ao player entrar nesta area.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DialogueTrigger : MonoBehaviour
{
    [Tooltip("Sequencia de dialogo exibida quando o player entra nesta area.")]
    [SerializeField] private DialogueSequence sequence;
    [Tooltip("Impede que a area tente exibir esta sequencia mais de uma vez.")]
    [SerializeField] private bool playOnce = true;
    [Tooltip("Desativa o GameObject do trigger depois de enfileirar a sequencia com sucesso.")]
    [SerializeField] private bool disableAfterPlay = true;

    private bool hasPlayed;
    private bool playerInside;
    private PlayerMovement playerMovement;

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

        bool insideNow = PlayerAreaOverlap.ContainsPlayer(
            GetComponent<Collider>(), playerMovement);
        if (!insideNow)
        {
            playerInside = false;
            return;
        }

        if (playerInside)
            return;

        playerInside = true;
        TryStartDialogue();
    }

    private void TryStartDialogue()
    {
        if (hasPlayed && playOnce)
            return;
        if (DialogueController.Instance == null ||
            !DialogueController.Instance.TryShow(sequence))
            return;

        hasPlayed = true;
        if (disableAfterPlay)
            gameObject.SetActive(false);
    }
}

using UnityEngine;

/// <summary>
/// Exibe um prompt contextual e inicia uma cutscene quando o jogador pressiona
/// a acao Interact dentro da area.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class CutsceneInteraction : MonoBehaviour
{
    [Header("Cutscene")]
    [Tooltip("CutsceneController cuja Timeline sera iniciada ao interagir.")]
    [SerializeField] private CutsceneController cutscene;
    [Tooltip("Impede iniciar esta cutscene novamente depois de uma interacao bem-sucedida.")]
    [SerializeField] private bool interactOnce = true;
    [Tooltip("Desativa este objeto depois de iniciar a cutscene.")]
    [SerializeField] private bool disableAfterStart;

    [Header("Prompt")]
    [Tooltip("Mensagem exibida junto ao botao de interacao dentro da area.")]
    [SerializeField] private string promptMessage = "Iniciar cutscene";
    [Tooltip("Icone opcional exibido no lugar do nome do botao de interacao.")]
    [SerializeField] private Sprite promptIcon;

    private Collider interactionCollider;
    private PlayerMovement playerMovement;
    private TemporaryWorldPromptHUD promptHUD;
    private bool hasInteracted;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;
    }

    private void OnDisable()
    {
        promptHUD?.HideContextualPrompt(this);
    }

    private void Update()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        bool playerInside = PlayerAreaOverlap.ContainsPlayer(
            interactionCollider, playerMovement);
        if (!playerInside || GamePauseController.IsPaused ||
            CutsceneController.IsPlaying || DialogueController.IsDisplaying ||
            DialogueController.IsInteractionCooldownActive || cutscene == null ||
            (interactOnce && hasInteracted))
        {
            promptHUD?.HideContextualPrompt(this);
            return;
        }

        if (promptHUD == null)
            promptHUD = FindAnyObjectByType<TemporaryWorldPromptHUD>();

        promptHUD?.ShowContextualPrompt(
            this,
            GameInputBindings.GetDisplayString("Interact"),
            promptMessage,
            promptIcon);

        if (!GameInput.InteractPressed || !cutscene.PlayCutscene())
            return;

        hasInteracted = true;
        promptHUD?.HideContextualPrompt(this);

        if (disableAfterStart)
            gameObject.SetActive(false);
    }
}

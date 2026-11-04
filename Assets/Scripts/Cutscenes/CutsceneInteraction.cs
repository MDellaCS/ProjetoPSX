using UnityEngine;

/// <summary>
/// Inicia uma cutscene quando o jogador aperta Interagir dentro da area. O
/// prompt e o input vem do sistema central de interacao.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class CutsceneInteraction : InteractableBase
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
    private bool hasInteracted;

    public override string InteractionMessage => promptMessage;
    public override Sprite InteractionIcon => promptIcon;

    private void Reset()
    {
        if (TryGetComponent(out Collider trigger))
            trigger.isTrigger = true;
    }

    private void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;
    }

    public override bool ContainsPlayer(PlayerMovement player)
    {
        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider>();
        return PlayerAreaOverlap.ContainsPlayer(interactionCollider, player);
    }

    public override bool CanInteract(PlayerMovement player) =>
        cutscene != null && !CutsceneController.IsPlaying &&
        !DialogueController.IsInteractionCooldownActive &&
        !(interactOnce && hasInteracted);

    public override bool Interact(PlayerMovement player)
    {
        if (cutscene == null || !cutscene.PlayCutscene())
            return false;

        hasInteracted = true;
        if (disableAfterStart)
            gameObject.SetActive(false);
        return true;
    }

    private void OnDrawGizmos()
    {
        if (InteractionGizmos.Visible)
            InteractionGizmos.DrawCollider(GetComponent<Collider>(),
                InteractionGizmos.ZoneFill, InteractionGizmos.ZoneLine);
    }
}

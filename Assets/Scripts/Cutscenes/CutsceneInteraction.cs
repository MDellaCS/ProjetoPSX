using UnityEngine;

/// <summary>
/// Inicia uma cutscene quando o jogador aperta Interagir dentro da area. O
/// prompt e o input vem do sistema central de interacao.
/// </summary>
[DisallowMultipleComponent]
public sealed class CutsceneInteraction : InteractableBase
{
    [Header("Cutscene")]
    [Tooltip("CutsceneController cuja Timeline sera iniciada ao interagir.")]
    [SerializeField] private CutsceneController cutscene;
    [Tooltip("Desativa este objeto depois de iniciar a cutscene.")]
    [SerializeField] private bool disableAfterStart;

    public override string InteractionMessage => "Iniciar cutscene";

    public override bool CanInteract(PlayerMovement player) =>
        cutscene != null && !CutsceneController.IsPlaying &&
        !DialogueController.IsInteractionCooldownActive && base.CanInteract(player);

    public override bool Interact(PlayerMovement player)
    {
        if (cutscene == null || !cutscene.PlayCutscene())
            return false;

        if (disableAfterStart)
            gameObject.SetActive(false);
        return true;
    }

}

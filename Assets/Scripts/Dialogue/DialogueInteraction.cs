using UnityEngine;

/// <summary>Inicia dialogos pelo arbitrador global de interacoes.</summary>
[DisallowMultipleComponent]
public sealed class DialogueInteraction : InteractableBase
{
    [Header("Dialogue")]
    [Tooltip("Sequencia de falas iniciada ao interagir com este NPC.")]
    [SerializeField] private DialogueSequence sequence;

    private PlayerMovement playerMovement;
    private bool dialogueStartedByInteraction;

    public override string InteractionMessage => "Falar";

    protected override void OnDisable()
    {
        dialogueStartedByInteraction = false;
        base.OnDisable();
    }

    private void Update()
    {
        if (!dialogueStartedByInteraction)
            return;

        DialogueController controller = DialogueController.Instance;
        if (controller == null || !controller.IsShowing(sequence))
        {
            dialogueStartedByInteraction = false;
            return;
        }

        if (sequence == null ||
            (!EffectiveInteractionProfile.DialogueLeaveArea &&
             !sequence.HasLeaveEarlyLines))
            return;

        if (playerMovement != null && !InteractionArea.ContainsPlayer(
                InteractionVolumeTransform(),
                EffectiveInteractionProfile, playerMovement,
                EffectiveInteractionProfile.DialogueLeaveAreaScale))
            OnLeaveAreaExited();
    }

    public override bool CanInteract(PlayerMovement player) =>
        sequence != null && !DialogueController.IsInteractionCooldownActive &&
        DialogueController.Instance != null && base.CanInteract(player);

    public override bool Interact(PlayerMovement player)
    {
        DialogueController controller = DialogueController.Instance;
        if (!CanInteract(player) || controller == null || !controller.TryShow(sequence))
            return false;

        dialogueStartedByInteraction = true;
        playerMovement = player;
        return true;
    }

    private void OnLeaveAreaExited()
    {
        if (!dialogueStartedByInteraction)
            return;

        dialogueStartedByInteraction = false;
        DialogueController controller = DialogueController.Instance;
        if (controller != null && controller.IsShowing(sequence))
            controller.HandleLeaveArea(sequence);
    }

    private void OnDrawGizmosSelected()
    {
        if (sequence == null ||
            (!EffectiveInteractionProfile.DialogueLeaveArea &&
             !sequence.HasLeaveEarlyLines))
            return;
        InteractionArea.DrawProfileGizmo(InteractionVolumeTransform(),
            EffectiveInteractionProfile,
            EffectiveInteractionProfile.DialogueLeaveAreaScale);
    }
}

using UnityEngine;

/// <summary>
/// Executa a arbitragem das interacoes (criado sozinho pelo InteractionSystem).
/// Roda antes do PlayerMovement para que uma interacao que muda o estado do
/// jogador, como agarrar uma escada, ja valha neste frame.
/// </summary>
[DefaultExecutionOrder(-200)]
internal sealed class InteractionDriver : MonoBehaviour
{
    private PlayerMovement player;
    private TemporaryWorldPromptHUD promptHUD;
    private float nextPlayerSearchTime;
    private float nextPromptSearchTime;
    private InteractableBase holdTarget;
    private float heldSeconds;
    private bool holdAttempted;

    public InteractableBase Current { get; private set; }

    public static InteractionDriver Create()
    {
        GameObject host = new("Interaction Driver")
        {
            hideFlags = HideFlags.HideInHierarchy
        };
        DontDestroyOnLoad(host);
        return host.AddComponent<InteractionDriver>();
    }

    public void Forget(InteractableBase item)
    {
        if (Current == item)
            ClearCurrent();
    }

    private void OnDisable() => ClearCurrent();

    private void Update()
    {
        ResolvePlayer();
        InteractableBase best = FindBest();
        if (best != Current)
        {
            ResetHold();
            ClearCurrent();
        }
        Current = best;
        if (best == null)
        {
            ResetHold();
            return;
        }

        ResolvePrompt();
        if (promptHUD != null)
            promptHUD.ShowContextualPrompt(best,
                GameInputBindings.GetDisplayString("Interact"),
                best.InteractionMessage, best.InteractionIcon,
                best.ActivationMode == InteractionActivationMode.Hold,
                best.ActivationMode == InteractionActivationMode.Hold && holdTarget == best
                    ? heldSeconds / best.HoldDuration : 0f);

        if (best.ActivationMode == InteractionActivationMode.Hold)
        {
            UpdateHoldInteraction(best);
            return;
        }

        ResetHold();
        if (GameInput.InteractPressed && best.Interact(player))
            HidePrompt(best);
    }

    private void UpdateHoldInteraction(InteractableBase target)
    {
        if (!GameInput.InteractHeld)
        {
            ResetHold();
            if (promptHUD != null)
                promptHUD.UpdateContextualProgress(target, 0f);
            return;
        }

        if (holdTarget != target)
        {
            ResetHold();
            if (!GameInput.InteractPressed)
                return;
            holdTarget = target;
        }

        if (holdAttempted)
            return;

        heldSeconds += Time.deltaTime;
        float progress = Mathf.Clamp01(heldSeconds / target.HoldDuration);
        target.SetHoldProgress(progress);
        if (promptHUD != null)
            promptHUD.UpdateContextualProgress(target, progress);
        if (progress < 1f)
            return;

        holdAttempted = true;
        if (target.Interact(player))
            HidePrompt(target);
    }

    private void ResetHold()
    {
        if (holdTarget != null)
            holdTarget.SetHoldProgress(0f);
        if (holdTarget != null && promptHUD != null)
            promptHUD.UpdateContextualProgress(holdTarget, 0f);
        holdTarget = null;
        heldSeconds = 0f;
        holdAttempted = false;
    }

    private InteractableBase FindBest()
    {
        if (player == null || !player.isActiveAndEnabled ||
            GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            DialogueController.IsDisplaying)
            return null;

        System.Collections.Generic.IReadOnlyList<InteractableBase> items =
            InteractionSystem.Items;
        Vector3 position = player.transform.position;
        InteractableBase best = null;
        float bestDistance = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            InteractableBase item = items[i];
            if (item == null || !item.isActiveAndEnabled ||
                !item.ContainsPlayer(player) || !item.CanInteract(player))
                continue;

            float distance = item.DistanceSqrTo(position);
            bool better = best == null ||
                item.InteractionPriority > best.InteractionPriority ||
                (item.InteractionPriority == best.InteractionPriority &&
                 distance < bestDistance);
            if (!better)
                continue;
            best = item;
            bestDistance = distance;
        }

        return best;
    }

    private void ClearCurrent()
    {
        ResetHold();
        if (Current != null)
            HidePrompt(Current);
        Current = null;
    }

    private void HidePrompt(Object owner)
    {
        if (promptHUD != null)
            promptHUD.HideContextualPrompt(owner);
    }

    private void ResolvePlayer()
    {
        if (player != null && player.isActiveAndEnabled)
            return;
        if (Time.unscaledTime < nextPlayerSearchTime)
            return;
        nextPlayerSearchTime = Time.unscaledTime + .5f;
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        player = found != null ? found.GetComponent<PlayerMovement>() : null;
    }

    private void ResolvePrompt()
    {
        if (promptHUD != null || Time.unscaledTime < nextPromptSearchTime)
            return;
        nextPromptSearchTime = Time.unscaledTime + .5f;
        promptHUD = FindAnyObjectByType<TemporaryWorldPromptHUD>();
    }
}

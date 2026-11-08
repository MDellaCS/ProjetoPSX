using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD global para dicas contextuais disparadas por areas do mundo.
/// Controla duracoes globais e garante que somente uma dica seja exibida.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class TemporaryWorldPromptHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PanelRenderer panelRenderer;

    [Header("Global Timing")]
    [Tooltip("Tempo maximo que qualquer dica fica visivel antes de ser consumida automaticamente.")]
    [Min(0f)] [SerializeField] private float autoConsumeAfterVisibleSeconds = 6f;
    [Tooltip("Espera entre apertar o input exibido e consumir uma dica do tipo Input.")]
    [Min(0f)] [SerializeField] private float inputCompletionDelay = .25f;
    [Tooltip("Espera entre completar uma interacao e consumir uma dica do tipo Interaction.")]
    [Min(0f)] [SerializeField] private float interactionCompletionDelay = .5f;
    [Tooltip("Tempo que um prompt que pausa o jogo deve permanecer na tela antes de aceitar sua sequencia de input.")]
    [Min(0f)] [SerializeField] private float minimumPausedPromptDisplaySeconds = .75f;

    [Header("Global Trigger Behavior")]
    [Tooltip("Quanto o collider do trigger cresce depois da primeira entrada do player.")]
    [Min(0f)] [SerializeField] private float triggerExpansionPercent = 25f;

    private VisualElement promptPanel;
    private VisualElement promptKeys;
    private Label promptBody;
    private VisualElement contextualProgressTrack;
    private VisualElement contextualProgressFill;
    private TemporaryWorldPromptTrigger currentPrompt;
    private float visibleTimer;
    private float completionTimer = -1f;
    private bool isPausedByPrompt;
    private Object contextualOwner;
    private string contextualKey;
    private string contextualMessage;
    private Sprite contextualIcon;
    private bool contextualHold;
    private float contextualProgress;

    public void ShowContextualPrompt(Object owner, string key, string message,
        Sprite icon, bool hold = false, float progress = 0f)
    {
        if (contextualOwner == owner && contextualKey == key &&
            contextualMessage == message && contextualIcon == icon &&
            contextualHold == hold)
        {
            UpdateContextualProgress(owner, progress);
            SetVisible(!GamePauseController.IsPaused);
            return;
        }

        contextualOwner = owner;
        contextualKey = key;
        contextualMessage = message;
        contextualIcon = icon;
        contextualHold = hold;
        contextualProgress = Mathf.Clamp01(progress);
        DrawContextualPrompt();
    }

    public void UpdateContextualProgress(Object owner, float progress)
    {
        if (contextualOwner != owner)
            return;
        contextualProgress = Mathf.Clamp01(progress);
        if (contextualProgressFill != null)
            contextualProgressFill.style.width = Length.Percent(contextualProgress * 100f);
    }

    public void HideContextualPrompt(Object owner)
    {
        if (contextualOwner != owner) return;
        contextualOwner = null;
        contextualHold = false;
        contextualProgress = 0f;
        if (contextualProgressTrack != null)
            contextualProgressTrack.style.display = DisplayStyle.None;
        if (currentPrompt != null) SetPromptContent(currentPrompt);
        SetVisible(currentPrompt != null);
    }

    private void DrawContextualPrompt()
    {
        if (promptKeys == null || promptBody == null) return;
        promptKeys.Clear();
        if (contextualIcon != null)
        {
            Image icon = new() { sprite = contextualIcon };
            icon.AddToClassList("temporary-world-prompt__key-icon");
            promptKeys.Add(icon);
            if (contextualHold)
            {
                Label hold = new() { text = "SEGURE" };
                hold.AddToClassList("temporary-world-prompt__key-fallback");
                promptKeys.Add(hold);
            }
        }
        else
        {
            Label key = new()
            {
                text = contextualHold ? "SEGURE " + contextualKey : contextualKey
            };
            key.AddToClassList("temporary-world-prompt__key-fallback");
            promptKeys.Add(key);
        }
        promptBody.text = contextualMessage;
        if (contextualProgressTrack != null)
            contextualProgressTrack.style.display = contextualHold
                ? DisplayStyle.Flex : DisplayStyle.None;
        UpdateContextualProgress(contextualOwner, contextualProgress);
        SetVisible(true);
    }

    public float TriggerExpansionPercent => triggerExpansionPercent;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);

        ResumeGameIfPausedByPrompt();
    }

    private void Update()
    {
        if (contextualOwner != null)
        {
            SetVisible(!GamePauseController.IsPaused);
            return;
        }
        if (PauseMenuHUD.IsOpen)
            return;
        if (currentPrompt == null)
            return;

        if (completionTimer >= 0f)
        {
            completionTimer -= Time.unscaledDeltaTime;
            if (completionTimer <= 0f)
                ConsumeCurrentPrompt();
            return;
        }

        visibleTimer += Time.unscaledDeltaTime;
        if (!currentPrompt.PausesGame && visibleTimer >= autoConsumeAfterVisibleSeconds)
        {
            ConsumeCurrentPrompt();
            return;
        }

        if (currentPrompt.PromptType == TemporaryWorldPromptType.Input &&
            CanAcceptCurrentInput() &&
            currentPrompt.WasConfiguredInputPressedThisFrame())
        {
            completionTimer = inputCompletionDelay;
            ResumeGameIfPausedByPrompt();
        }
    }

    public bool TryShow(TemporaryWorldPromptTrigger prompt)
    {
        if (prompt == null || prompt.IsConsumed)
            return false;

        if (currentPrompt == prompt)
            return true;

        if (currentPrompt != null)
            return false;

        currentPrompt = prompt;
        visibleTimer = 0f;
        completionTimer = -1f;

        SetPromptContent(prompt);

        SetVisible(!GamePauseController.IsPaused);
        PauseGameIfRequested(prompt);
        return true;
    }

    public void NotifyInteractionCompleted(TemporaryWorldPromptTrigger prompt)
    {
        if (prompt == null || prompt != currentPrompt || completionTimer >= 0f)
            return;

        completionTimer = interactionCompletionDelay;
    }

    public void HidePrompt(TemporaryWorldPromptTrigger prompt)
    {
        if (prompt == null || prompt != currentPrompt)
            return;

        ResumeGameIfPausedByPrompt();
        currentPrompt = null;
        completionTimer = -1f;
        visibleTimer = 0f;
        if (contextualOwner != null)
            DrawContextualPrompt();
        else
            SetVisible(false);
    }

    private void ConsumeCurrentPrompt()
    {
        if (currentPrompt != null)
            currentPrompt.Consume();

        ResumeGameIfPausedByPrompt();
        currentPrompt = null;
        completionTimer = -1f;
        visibleTimer = 0f;
        SetVisible(false);
    }

    private void HandleUIReload(
        PanelRenderer _,
        VisualElement rootVisualElement,
        int __)
    {
        if (rootVisualElement == null)
            return;

        promptPanel = rootVisualElement.Q<VisualElement>("temporary-world-prompt");
        promptKeys = rootVisualElement.Q<VisualElement>("temporary-world-prompt-keys");
        promptBody = rootVisualElement.Q<Label>("temporary-world-prompt-body");
        contextualProgressTrack = rootVisualElement.Q<VisualElement>(
            "temporary-world-prompt-progress");
        contextualProgressFill = rootVisualElement.Q<VisualElement>(
            "temporary-world-prompt-progress-fill");

        if (promptPanel == null || promptKeys == null || promptBody == null)
            return;

        if (currentPrompt != null)
            SetPromptContent(currentPrompt);

        SetVisible(currentPrompt != null);
        if (contextualOwner != null) DrawContextualPrompt();
        else if (contextualProgressTrack != null)
            contextualProgressTrack.style.display = DisplayStyle.None;
    }

    private void SetPromptContent(TemporaryWorldPromptTrigger prompt)
    {
        if (prompt == null)
            return;

        if (promptBody != null)
            promptBody.text = prompt.GetDisplayText();

        if (promptKeys == null)
            return;

        promptKeys.Clear();
        for (int index = 0; index < prompt.KeyCount; index++)
        {
            TemporaryWorldPromptKey key = prompt.GetKey(index);
            if (key == null)
                continue;

            if (promptKeys.childCount > 0)
            {
                Label arrow = new() { text = "→" };
                arrow.AddToClassList("temporary-world-prompt__key-arrow");
                promptKeys.Add(arrow);
            }

            if (key.Icon != null)
            {
                Image icon = new() { sprite = key.Icon };
                icon.AddToClassList("temporary-world-prompt__key-icon");
                promptKeys.Add(icon);
            }
            else
            {
                Label fallback = new() { text = key.GetFallbackText() };
                fallback.AddToClassList("temporary-world-prompt__key-fallback");
                promptKeys.Add(fallback);
            }
        }
    }

    private void SetVisible(bool value)
    {
        if (promptPanel != null)
        {
            promptPanel.style.display = value
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }

    private bool CanAcceptCurrentInput()
    {
        return currentPrompt == null || !currentPrompt.PausesGame ||
               visibleTimer >= minimumPausedPromptDisplaySeconds;
    }

    private void PauseGameIfRequested(TemporaryWorldPromptTrigger prompt)
    {
        if (isPausedByPrompt || prompt == null || !prompt.PausesGame)
            return;

        isPausedByPrompt = true;
        GamePauseController.RequestPause(this);
    }

    private void ResumeGameIfPausedByPrompt()
    {
        if (!isPausedByPrompt)
            return;

        isPausedByPrompt = false;
        GamePauseController.ReleasePause(this);
    }
}

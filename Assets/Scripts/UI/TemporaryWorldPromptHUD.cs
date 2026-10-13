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
    private TemporaryWorldPromptTrigger currentPrompt;
    private float visibleTimer;
    private float completionTimer = -1f;
    private bool isPausedByPrompt;
    private float timeScaleBeforePromptPause = 1f;

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

        SetVisible(true);
        PauseGameIfRequested(prompt);
        return true;
    }

    public void NotifyInteractionCompleted(TemporaryWorldPromptTrigger prompt)
    {
        if (prompt == null || prompt != currentPrompt || completionTimer >= 0f)
            return;

        completionTimer = interactionCompletionDelay;
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

        if (promptPanel == null || promptKeys == null || promptBody == null)
            return;

        if (currentPrompt != null)
            SetPromptContent(currentPrompt);

        SetVisible(currentPrompt != null);
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

        timeScaleBeforePromptPause = Time.timeScale;
        Time.timeScale = 0f;
        isPausedByPrompt = true;
    }

    private void ResumeGameIfPausedByPrompt()
    {
        if (!isPausedByPrompt)
            return;

        Time.timeScale = timeScaleBeforePromptPause;
        isPausedByPrompt = false;
    }
}

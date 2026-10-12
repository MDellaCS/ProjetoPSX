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

    [Header("Global Trigger Behavior")]
    [Tooltip("Quanto o collider do trigger cresce depois da primeira entrada do player.")]
    [Min(0f)] [SerializeField] private float triggerExpansionPercent = 25f;

    private VisualElement promptPanel;
    private Label promptTitle;
    private Label promptBody;
    private TemporaryWorldPromptTrigger currentPrompt;
    private float visibleTimer;
    private float completionTimer = -1f;

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
    }

    private void Update()
    {
        if (currentPrompt == null)
            return;

        if (completionTimer >= 0f)
        {
            completionTimer -= Time.deltaTime;
            if (completionTimer <= 0f)
                ConsumeCurrentPrompt();
            return;
        }

        visibleTimer += Time.deltaTime;
        if (visibleTimer >= autoConsumeAfterVisibleSeconds)
        {
            ConsumeCurrentPrompt();
            return;
        }

        if (currentPrompt.PromptType == TemporaryWorldPromptType.Input &&
            currentPrompt.WasConfiguredInputPressedThisFrame())
        {
            completionTimer = inputCompletionDelay;
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

        if (promptTitle != null)
            promptTitle.text = prompt.Title;

        if (promptBody != null)
            promptBody.text = prompt.GetDisplayText();

        SetVisible(true);
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
        promptTitle = rootVisualElement.Q<Label>("temporary-world-prompt-title");
        promptBody = rootVisualElement.Q<Label>("temporary-world-prompt-body");

        if (promptPanel == null || promptTitle == null || promptBody == null)
        {
            Debug.LogWarning(
                "TemporaryWorldPromptHUD: UXML precisa de 'temporary-world-prompt', " +
                "'temporary-world-prompt-title' e 'temporary-world-prompt-body'.",
                this
            );
            return;
        }

        if (currentPrompt != null)
        {
            promptTitle.text = currentPrompt.Title;
            promptBody.text = currentPrompt.GetDisplayText();
        }

        SetVisible(currentPrompt != null);
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
}

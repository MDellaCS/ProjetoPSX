using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TemporaryWorldPromptType
{
    Input,
    Interaction
}

/// <summary>
/// Area no mundo que mostra uma unica dica temporaria. A dica consumida fica
/// persistida em PlayerPrefs e nao e exibida novamente nas proximas execucoes.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TemporaryWorldPromptTrigger : MonoBehaviour
{
    private const string PlayerPrefsPrefix = "TemporaryWorldPrompt.";

    [Header("Identity")]
    [Tooltip("Identificador persistente. Nao repita entre triggers. Use Regenerate Prompt ID em duplicatas.")]
    [SerializeField] private string promptId;

    [Header("Prompt")]
    [SerializeField] private TemporaryWorldPromptType promptType;
    [SerializeField] private string title = "TIP";
    [TextArea(2, 4)] [SerializeField] private string message = "Press {KEY}";
    [SerializeField] private Key configuredKey = Key.E;
    [Tooltip("Para Interaction, permite testar a conclusao pressionando a tecla configurada. Desative quando uma interacao real chamar CompleteInteraction().")]
    [SerializeField] private bool keyboardInteractionFallback = true;

    private Collider triggerCollider;
    private TemporaryWorldPromptHUD promptHUD;
    private bool isConsumed;
    private bool hasExpanded;
    private bool playerInside;

    public TemporaryWorldPromptType PromptType => promptType;
    public string Title => title;
    public bool IsConsumed => isConsumed;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        isConsumed = PlayerPrefs.GetInt(GetSaveKey(), 0) == 1;
    }

    private void Update()
    {
        if (!isConsumed && playerInside)
        {
            ResolveHUD();
            if (promptHUD != null && promptHUD.TryShow(this))
                ExpandTriggerArea();
        }

        if (isConsumed || promptType != TemporaryWorldPromptType.Interaction ||
            !keyboardInteractionFallback || !WasConfiguredInputPressedThisFrame())
        {
            return;
        }

        CompleteInteraction();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isConsumed || !IsPlayer(other))
            return;

        playerInside = true;
        ResolveHUD();
        if (promptHUD != null && promptHUD.TryShow(this))
            ExpandTriggerArea();
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            playerInside = false;
    }

    public string GetDisplayText()
    {
        string keyText = configuredKey.ToString().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(message))
        {
            return promptType == TemporaryWorldPromptType.Input
                ? "PRESS [" + keyText + "]"
                : "INTERACT [" + keyText + "]";
        }

        return message.Replace("{KEY}", "[" + keyText + "]");
    }

    public bool WasConfiguredInputPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[configuredKey] != null &&
            keyboard[configuredKey].wasPressedThisFrame;
    }

    /// <summary>
    /// Chame quando a interacao real for concluida. Para testes, o fallback de
    /// teclado tambem chama este metodo ao apertar Configured Key.
    /// </summary>
    public void CompleteInteraction()
    {
        if (isConsumed || promptType != TemporaryWorldPromptType.Interaction)
            return;

        ResolveHUD();
        promptHUD?.NotifyInteractionCompleted(this);
    }

    public void Consume()
    {
        if (isConsumed)
            return;

        isConsumed = true;
        PlayerPrefs.SetInt(GetSaveKey(), 1);
        PlayerPrefs.Save();
    }

    [ContextMenu("Reset Consumed State")]
    public void ResetConsumedState()
    {
        isConsumed = false;
        PlayerPrefs.DeleteKey(GetSaveKey());
        PlayerPrefs.Save();
    }

    [ContextMenu("Regenerate Prompt ID")]
    private void RegeneratePromptId()
    {
        promptId = Guid.NewGuid().ToString("N");
    }

    private void ResolveHUD()
    {
        if (promptHUD == null)
            promptHUD = FindAnyObjectByType<TemporaryWorldPromptHUD>();
    }

    private void ExpandTriggerArea()
    {
        if (hasExpanded || triggerCollider == null || promptHUD == null)
            return;

        float multiplier = 1f + promptHUD.TriggerExpansionPercent / 100f;
        switch (triggerCollider)
        {
            case BoxCollider box:
                box.size *= multiplier;
                break;
            case SphereCollider sphere:
                sphere.radius *= multiplier;
                break;
            case CapsuleCollider capsule:
                capsule.radius *= multiplier;
                capsule.height *= multiplier;
                break;
            default:
                Debug.LogWarning(
                    "TemporaryWorldPromptTrigger: expansao disponivel para Box, Sphere ou Capsule Collider.",
                    this
                );
                break;
        }

        hasExpanded = true;
    }

    private string GetSaveKey()
    {
        return PlayerPrefsPrefix + promptId;
    }

    private static bool IsPlayer(Collider collider)
    {
        return collider != null &&
            (collider.CompareTag("Player") ||
             collider.GetComponentInParent<PlayerMovement>() != null);
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(promptId))
            RegeneratePromptId();

        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !PlayerDebugToolkitHUD.IsVisible)
            return;

        Collider collider = GetComponent<Collider>();
        if (collider == null)
            return;

        Gizmos.color = isConsumed
            ? new Color(.3f, .3f, .3f, .4f)
            : new Color(1f, .7f, .15f, .75f);
        Gizmos.matrix = transform.localToWorldMatrix;

        switch (collider)
        {
            case BoxCollider box:
                Gizmos.DrawWireCube(box.center, box.size);
                break;
            case SphereCollider sphere:
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                break;
            case CapsuleCollider capsule:
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                break;
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}

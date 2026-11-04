using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TemporaryWorldPromptType
{
    Input,
    Interaction
}

public enum TemporaryWorldPromptInputDevice
{
    Keyboard,
    Mouse
}

public enum TemporaryWorldPromptMouseButton
{
    Left,
    Right,
    Middle,
    Back,
    Forward
}

[Serializable]
public class TemporaryWorldPromptKey
{
    [SerializeField] private TemporaryWorldPromptInputDevice inputDevice;
    [SerializeField] private Key key = Key.E;
    [SerializeField] private TemporaryWorldPromptMouseButton mouseButton;
    [Tooltip("Sprite exibido para esta tecla. Se vazio, o HUD usa o nome da tecla como fallback.")]
    [SerializeField] private Sprite icon;

    public Sprite Icon => icon;

    public bool WasPressedThisFrame()
    {
        if (inputDevice == TemporaryWorldPromptInputDevice.Keyboard)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key] != null &&
                   keyboard[key].wasPressedThisFrame;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        return mouseButton switch
        {
            TemporaryWorldPromptMouseButton.Left => mouse.leftButton.wasPressedThisFrame,
            TemporaryWorldPromptMouseButton.Right => mouse.rightButton.wasPressedThisFrame,
            TemporaryWorldPromptMouseButton.Middle => mouse.middleButton.wasPressedThisFrame,
            TemporaryWorldPromptMouseButton.Back => mouse.backButton.wasPressedThisFrame,
            TemporaryWorldPromptMouseButton.Forward => mouse.forwardButton.wasPressedThisFrame,
            _ => false
        };
    }

    public string GetFallbackText()
    {
        return inputDevice == TemporaryWorldPromptInputDevice.Keyboard
            ? key.ToString().ToUpperInvariant()
            : mouseButton switch
            {
                TemporaryWorldPromptMouseButton.Left => "M1",
                TemporaryWorldPromptMouseButton.Right => "M2",
                TemporaryWorldPromptMouseButton.Middle => "M3",
                TemporaryWorldPromptMouseButton.Back => "M4",
                TemporaryWorldPromptMouseButton.Forward => "M5",
                _ => "MOUSE"
            };
    }
}

/// <summary>
/// Area no mundo que mostra uma unica dica temporaria. A dica consumida nao
/// reaparece durante a execucao atual, mas e resetada ao reiniciar o jogo.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TemporaryWorldPromptTrigger : MonoBehaviour
{
    [Header("Prompt")]
    [SerializeField] private TemporaryWorldPromptType promptType;
    [Tooltip("Escreva somente o texto apos {KEY}. O HUD sempre mostra o icone da tecla primeiro.")]
    [TextArea(2, 4)] [SerializeField] private string message = "{KEY} PRESS";
    [Tooltip("Teclas exibidas e reconhecidas em ordem. O HUD coloca uma seta entre cada item.")]
    [SerializeField] private List<TemporaryWorldPromptKey> keySequence = new()
    {
        new TemporaryWorldPromptKey()
    };
    [Tooltip("Tempo maximo entre uma tecla e a proxima da sequencia. Zero desativa o limite.")]
    [Min(0f)] [SerializeField] private float inputSequenceTimeout = .75f;
    [Tooltip("Pausa o jogo enquanto este prompt de Input estiver visivel. O jogo volta ao normal ao concluir a sequencia.")]
    [SerializeField] private bool pauseGameUntilInputCompleted;
    [Tooltip("Para Interaction, permite testar a conclusao usando a sequencia configurada. Desative quando uma interacao real chamar CompleteInteraction().")]
    [SerializeField] private bool keyboardInteractionFallback = true;

    private Collider triggerCollider;
    private PlayerMovement playerMovement;
    private TemporaryWorldPromptHUD promptHUD;
    private bool isConsumed;
    private bool hasExpanded;
    private bool playerInside;
    private int nextSequenceKeyIndex;
    private float sequenceTimer;

    public TemporaryWorldPromptType PromptType => promptType;
    public bool IsConsumed => isConsumed;
    public int KeyCount => keySequence?.Count ?? 0;
    public bool PausesGame => promptType == TemporaryWorldPromptType.Input &&
                              pauseGameUntilInputCompleted;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        isConsumed = false;
        hasExpanded = false;
    }

    private void Update()
    {
        UpdateInputSequenceTimeout();
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();
        playerInside = PlayerAreaOverlap.ContainsPlayer(
            triggerCollider, playerMovement);

        if (!isConsumed && playerInside)
        {
            ResolveHUD();
            if (promptHUD != null && promptHUD.TryShow(this))
                ExpandTriggerArea();
        }
        else if (!playerInside)
        {
            if (promptHUD != null)
                promptHUD.HidePrompt(this);
        }

        if (isConsumed || promptType != TemporaryWorldPromptType.Interaction ||
            !playerInside || !keyboardInteractionFallback ||
            !WasConfiguredInputPressedThisFrame())
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
        if (string.IsNullOrWhiteSpace(message))
        {
            return promptType == TemporaryWorldPromptType.Input
                ? "PRESS"
                : "INTERACT";
        }

        return message.Replace("{KEY}", string.Empty).Trim();
    }

    public TemporaryWorldPromptKey GetKey(int index)
    {
        if (keySequence == null || index < 0 || index >= keySequence.Count)
            return null;

        return keySequence[index];
    }

    public bool WasConfiguredInputPressedThisFrame()
    {
        TemporaryWorldPromptKey expectedKey = GetKey(nextSequenceKeyIndex);
        if (expectedKey == null)
            return false;

        if (!expectedKey.WasPressedThisFrame())
        {
            return false;
        }

        nextSequenceKeyIndex++;
        sequenceTimer = inputSequenceTimeout;
        if (nextSequenceKeyIndex < KeyCount)
            return false;

        nextSequenceKeyIndex = 0;
        sequenceTimer = 0f;
        return true;
    }

    /// <summary>
    /// Chame quando a interacao real for concluida. Para testes, o fallback de
    /// teclado tambem chama este metodo ao concluir a sequencia configurada.
    /// </summary>
    public void CompleteInteraction()
    {
        if (isConsumed || promptType != TemporaryWorldPromptType.Interaction)
            return;

        ResolveHUD();
        if (promptHUD != null)
            promptHUD.NotifyInteractionCompleted(this);
    }

    public void Consume()
    {
        if (isConsumed)
            return;

        isConsumed = true;
    }

    private void ResolveHUD()
    {
        if (promptHUD == null)
            promptHUD = FindAnyObjectByType<TemporaryWorldPromptHUD>();
    }

    private void UpdateInputSequenceTimeout()
    {
        if (nextSequenceKeyIndex == 0 || inputSequenceTimeout <= 0f)
            return;

        sequenceTimer -= Time.unscaledDeltaTime;
        if (sequenceTimer <= 0f)
        {
            nextSequenceKeyIndex = 0;
            sequenceTimer = 0f;
        }
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
                break;
        }

        hasExpanded = true;
    }

    private static bool IsPlayer(Collider collider)
    {
        return collider != null &&
            (collider.CompareTag("Player") ||
             collider.GetComponentInParent<PlayerMovement>() != null);
    }

    private void OnValidate()
    {
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

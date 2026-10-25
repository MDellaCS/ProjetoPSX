using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Apresentador global de dialogos de gameplay. Mantem uma fila para que
/// mensagens concorrentes nunca se sobreponham e nao depende de Timeline.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PanelRenderer))]
public sealed class DialogueController : MonoBehaviour
{
    [Header("Tempo Global")]
    [Tooltip("Tempo minimo em que a primeira pagina de toda sequencia deve permanecer visivel antes de aceitar clique ou Enter. Soma-se ao minimo individual da pagina, usando o maior valor.")]
    [Min(0f)] [SerializeField] private float minimumFirstPageDisplayDuration = .35f;

    private static DialogueController instance;
    private readonly Queue<DialogueSequence> queue = new();

    private PanelRenderer panelRenderer;
    private VisualElement dialogueRoot;
    private Label speakerLabel;
    private Label messageLabel;
    private Label advanceLabel;
    private DialogueSequence currentSequence;
    private List<DialogueLine> currentLines;
    private int currentLineIndex;
    private float lineElapsed;

    public static DialogueController Instance => instance != null
        ? instance : FindAnyObjectByType<DialogueController>();
    public static bool IsDisplaying => Instance != null &&
        Instance.currentSequence != null;
    /// <summary>Indica que a fala atual aceita avanço manual e deve bloquear ataques do jogador.</summary>
    public static bool BlocksPlayerAttacks
    {
        get
        {
            DialogueController controller = Instance;
            return controller != null &&
                controller.GetCurrentLine()?.allowEarlyAdvance == true;
        }
    }
    public DialogueSequence CurrentSequence => currentSequence;
    public int CurrentLineIndex => currentLineIndex;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
        panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (currentSequence == null || GamePauseController.IsPaused)
            return;

        DialogueLine line = GetCurrentLine();
        if (line == null)
        {
            FinishCurrentLine();
            return;
        }

        lineElapsed += Time.deltaTime;
        bool canConfirm = lineElapsed >= GetMinimumDisplayDuration(line);
        if (canConfirm && line.allowEarlyAdvance && WasAdvancePressed())
        {
            FinishCurrentLine();
            return;
        }

        bool canTimeOut = line.finishMode == DialogueFinishMode.Timed ||
            line.finishMode == DialogueFinishMode.TimedOrConfirm;
        if (canTimeOut && lineElapsed >= Mathf.Max(line.displayDuration,
            GetMinimumDisplayDuration(line)))
            FinishCurrentLine();
    }

    /// <summary>Enfileira uma sequencia para exibicao durante o gameplay.</summary>
    public bool TryShow(DialogueSequence sequence)
    {
        if (sequence == null || !sequence.HasLines)
            return false;

        queue.Enqueue(sequence);
        if (currentSequence == null)
            BeginNextSequence();
        return true;
    }

    /// <summary>Executa a resposta de Leave Area do dialogo atual.</summary>
    public bool HandleLeaveArea(DialogueSequence sequence)
    {
        if (sequence == null || currentSequence != sequence)
            return false;

        queue.Clear();
        if (sequence.HasLeaveEarlyLines)
        {
            currentLines = sequence.leaveEarlyLines;
            currentLineIndex = 0;
            BeginCurrentLine();
        }
        else
        {
            currentSequence = null;
            currentLines = null;
            currentLineIndex = 0;
            lineElapsed = 0f;
            BeginNextSequence();
        }

        return true;
    }

    public bool IsShowing(DialogueSequence sequence) =>
        sequence != null && currentSequence == sequence;

    /// <summary>Versao sem retorno para UnityEvents de triggers e alavancas.</summary>
    public void ShowDialogue(DialogueSequence sequence) => TryShow(sequence);

    /// <summary>Fecha a fala atual, se ela aceitar confirmacao neste momento.</summary>
    public void AdvanceCurrentDialogue()
    {
        DialogueLine line = GetCurrentLine();
        if (line != null && line.allowEarlyAdvance &&
            lineElapsed >= GetMinimumDisplayDuration(line))
            FinishCurrentLine();
    }

    private void BeginNextSequence()
    {
        currentSequence = null;
        currentLineIndex = 0;
        while (queue.Count > 0)
        {
            DialogueSequence candidate = queue.Dequeue();
            if (candidate == null || !candidate.HasLines)
                continue;

            currentSequence = candidate;
            currentLines = candidate.lines;
            BeginCurrentLine();
            return;
        }

        SetVisible(false);
    }

    private void BeginCurrentLine()
    {
        DialogueLine line = GetCurrentLine();
        if (line == null)
        {
            // Uma entrada nula editada no Inspector nao deve prender a fila.
            currentLineIndex++;
            if (currentSequence != null && currentLines != null &&
                currentLineIndex < currentLines.Count)
            {
                BeginCurrentLine();
            }
            else
            {
                currentSequence = null;
                currentLines = null;
                BeginNextSequence();
            }
            return;
        }

        lineElapsed = 0f;
        GameAudioSystem.Play2D(line.voiceOverClip, line.voiceOverVolume);
        RefreshVisuals();
    }

    private void FinishCurrentLine()
    {
        if (currentSequence == null)
            return;

        currentLineIndex++;
        if (currentLines != null && currentLineIndex < currentLines.Count)
        {
            BeginCurrentLine();
            return;
        }

        currentSequence = null;
        currentLines = null;
        currentLineIndex = 0;
        lineElapsed = 0f;
        SetVisible(false);
        BeginNextSequence();
    }

    private DialogueLine GetCurrentLine()
    {
        if (currentLines == null || currentLineIndex < 0 ||
            currentLineIndex >= currentLines.Count)
            return null;
        return currentLines[currentLineIndex];
    }

    private float GetMinimumDisplayDuration(DialogueLine line)
    {
        if (line == null)
            return 0f;

        float lineMinimum = Mathf.Max(0f, line.minimumDisplayDuration);
        return currentLineIndex == 0
            ? Mathf.Max(lineMinimum, minimumFirstPageDisplayDuration)
            : lineMinimum;
    }

    private void HandleUIReload(PanelRenderer _, VisualElement root, int __)
    {
        if (root == null)
            return;

        dialogueRoot = root.Q<VisualElement>("dialogue-root");
        speakerLabel = root.Q<Label>("dialogue-speaker");
        messageLabel = root.Q<Label>("dialogue-message");
        advanceLabel = root.Q<Label>("dialogue-advance");
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        DialogueLine line = GetCurrentLine();
        SetVisible(line != null);
        if (line == null)
            return;

        if (messageLabel != null)
            messageLabel.text = line.text ?? string.Empty;
        if (speakerLabel != null)
        {
            bool hasSpeaker = !string.IsNullOrWhiteSpace(line.speakerName);
            speakerLabel.text = line.speakerName ?? string.Empty;
            speakerLabel.style.display = hasSpeaker
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
        if (advanceLabel != null)
        {
            bool mayAdvance = line.allowEarlyAdvance;
            advanceLabel.style.display = mayAdvance
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            advanceLabel.text = mayAdvance ? "CLIQUE / ENTER" : string.Empty;
        }
    }

    private void SetVisible(bool visible)
    {
        if (dialogueRoot != null)
            dialogueRoot.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }

    private static bool WasAdvancePressed()
    {
        return Mouse.current?.leftButton.wasPressedThisFrame == true ||
            Keyboard.current?.enterKey.wasPressedThisFrame == true ||
            Keyboard.current?.numpadEnterKey.wasPressedThisFrame == true;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
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
    [Tooltip("Tempo minimo em que a primeira pagina de toda sequencia deve permanecer visivel antes de aceitar o botao Interact. Soma-se ao minimo individual da pagina, usando o maior valor.")]
    [Min(0f)] [SerializeField] private float minimumFirstPageDisplayDuration = .35f;
    [Tooltip("Tempo apos o fim de uma sequencia antes de aceitar outra interacao de dialogo. Impede que o mesmo Interact que fechou a fala inicie outra imediatamente.")]
    [Min(0f)] [SerializeField] private float postDialogueInteractionCooldown = .35f;
    [Tooltip("Ordem de desenho do painel de dialogo. Valores maiores aparecem sobre interfaces comuns; overlays com ordem superior continuam por cima.")]
    [SerializeField] private int dialogueSortingOrder = 2;
    [Tooltip("Font Asset do TextCore/UI Toolkit usado nas falas. Crie-o pelo menu UI Toolkit > Text > Font Asset; deixe vazio para usar a fonte padrao do Panel Settings.")]
    [SerializeField] private FontAsset dialogueFont;
    [Tooltip("Font Asset Crimson Text usado no nome do speaker; deixe vazio para herdar a fonte do Panel Settings.")]
    [SerializeField] private FontAsset speakerNameFont;
    [Tooltip("Asset global com os nomes e cores reutilizados por todas as sequencias de dialogo.")]
    [SerializeField] private DialogueSpeakerLibrary speakerLibrary;

    private static DialogueController instance;
    private readonly Queue<DialogueSequence> queue = new();

    private PanelRenderer panelRenderer;
    private VisualElement dialogueRoot;
    private VisualElement dialogueFade;
    private Label speakerLabel;
    private Label messageLabel;
    private Texture2D dialogueFadeTexture;
    private DialogueSequence currentSequence;
    private List<DialogueLine> currentLines;
    private int currentLineIndex;
    private float lineElapsed;
    private float nextInteractionAvailableAt;

    public static DialogueController Instance => instance != null
        ? instance : FindAnyObjectByType<DialogueController>();
    public static bool IsDisplaying => Instance != null &&
        Instance.currentSequence != null;
    public static bool IsInteractionCooldownActive => Instance != null &&
        Time.time < Instance.nextInteractionAvailableAt;
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
        {
            panelRenderer.sortingOrder = dialogueSortingOrder;
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        }
    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        if (dialogueFadeTexture != null)
            Destroy(dialogueFadeTexture);
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

        lineElapsed += Time.unscaledDeltaTime;
        bool canConfirm = lineElapsed >= GetMinimumDisplayDuration(line);
        if (canConfirm && line.allowEarlyAdvance && GameInput.InteractPressed)
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
            BeginInteractionCooldown();
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
        DialogueAudio.PlayVoice(line.voiceOverClip, line.voiceOverVolume);
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
        if (queue.Count == 0)
            BeginInteractionCooldown();
        BeginNextSequence();
    }

    private void BeginInteractionCooldown() =>
        nextInteractionAvailableAt = Time.time +
            Mathf.Max(0f, postDialogueInteractionCooldown);

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
        dialogueFade = root.Q<VisualElement>("dialogue-fade");
        speakerLabel = root.Q<Label>("dialogue-speaker");
        messageLabel = root.Q<Label>("dialogue-message");
        dialogueRoot?.RegisterCallback<GeometryChangedEvent>(
            HandleDialogueGeometryChanged);
        messageLabel?.RegisterCallback<GeometryChangedEvent>(
            HandleDialogueGeometryChanged);
        ApplyDialogueStyle();
        RefreshVisuals();
    }

    private void HandleDialogueGeometryChanged(GeometryChangedEvent _)
    {
        UpdateDialogueFadeHeight();
    }

    private void ApplyDialogueStyle()
    {
        if (messageLabel != null && dialogueFont != null)
        {
            messageLabel.style.unityFontDefinition =
                new StyleFontDefinition(
                    FontDefinition.FromSDFFont(dialogueFont));
        }

        if (speakerLabel != null && speakerNameFont != null)
        {
            speakerLabel.style.unityFontDefinition =
                new StyleFontDefinition(
                    FontDefinition.FromSDFFont(speakerNameFont));
        }

        if (dialogueFade == null)
            return;

        if (dialogueFadeTexture == null)
            dialogueFadeTexture = CreateDialogueFadeTexture();
        dialogueFade.style.backgroundImage =
            Background.FromTexture2D(dialogueFadeTexture);
        UpdateDialogueFadeHeight();
    }

    private void UpdateDialogueFadeHeight()
    {
        if (dialogueRoot == null || dialogueFade == null ||
            messageLabel == null)
            return;

        float rootHeight = dialogueRoot.layout.height;
        if (rootHeight <= 0f)
            return;

        const float fadeExtensionAboveText = 24f;
        float height = rootHeight - messageLabel.layout.y +
            fadeExtensionAboveText;
        dialogueFade.style.height = Mathf.Clamp(height, 0f, rootHeight);
    }

    private static Texture2D CreateDialogueFadeTexture()
    {
        const int height = 128;
        var texture = new Texture2D(1, height, TextureFormat.RGBA32,
            false, true)
        {
            name = "Dialogue Bottom Fade",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var pixels = new Color[height];
        for (int y = 0; y < height; y++)
        {
            float fromBottom = (float)y / (height - 1);
            float opacity = 1f * Mathf.Pow(1f - fromBottom, 1.2f);
            pixels[y] = new Color(0f, 0f, 0f, opacity);
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
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
            speakerLabel.text = hasSpeaker
                ? (speakerLibrary != null
                    ? speakerLibrary.FormatSpeakerName(line.speakerName)
                    : line.speakerName.Trim())
                : string.Empty;
            speakerLabel.style.display = hasSpeaker
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            speakerLabel.style.color = speakerLibrary != null
                ? speakerLibrary.GetSpeakerColor(line)
                : Color.white;
            if (speakerLibrary != null)
            {
                speakerLabel.style.fontSize = speakerLibrary.fontSize;
                speakerLabel.style.unityFontStyleAndWeight =
                    speakerLibrary.bold && speakerLibrary.italic
                        ? FontStyle.BoldAndItalic
                        : speakerLibrary.bold ? FontStyle.Bold
                        : speakerLibrary.italic ? FontStyle.Italic
                        : FontStyle.Normal;
                speakerLabel.style.unityTextAlign = speakerLibrary.alignment;
                speakerLabel.style.marginBottom = speakerLibrary.spacingBelow;
            }
        }
    }

    private void SetVisible(bool visible)
    {
        if (dialogueRoot != null)
            dialogueRoot.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }
}

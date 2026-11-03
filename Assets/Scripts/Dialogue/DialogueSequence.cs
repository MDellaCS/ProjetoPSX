using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueFinishMode
{
    Timed,
    Confirm,
    TimedOrConfirm
}

/// <summary>Uma fala configuravel dentro de uma sequencia de dialogo.</summary>
[Serializable]
public sealed class DialogueLine
{
    [Tooltip("Texto exibido na caixa de dialogo.")]
    [TextArea(2, 6)] public string text;
    [Tooltip("Nome opcional exibido acima da fala. Deixe vazio para uma mensagem sem locutor.")]
    public string speakerName;
    [Tooltip("Ative para escolher uma cor exclusiva para este nome, ignorando a lista global de speakers.")]
    public bool overrideSpeakerColor;
    [Tooltip("Cor personalizada para o nome desta fala, usada quando Override Speaker Color esta ativo.")]
    public Color speakerColor = Color.white;
    [Tooltip("Arquivo de voz opcional tocado ao abrir esta fala.")]
    public AudioClip voiceOverClip;
    [Tooltip("Volume da voz desta fala. Nao usa variacao aleatoria de pitch ou volume.")]
    [Range(0f, 1f)] public float voiceOverVolume = 1f;
    [Tooltip("Define se a fala fecha por tempo, confirmacao do jogador ou qualquer um dos dois.")]
    public DialogueFinishMode finishMode = DialogueFinishMode.TimedOrConfirm;
    [Tooltip("Tempo maximo em tela para modos que fecham automaticamente.")]
    [Min(0f)] public float displayDuration = 3f;
    [Tooltip("Permite fechar esta fala antes do tempo maximo pelo mesmo botao Interact usado para inicia-la.")]
    public bool allowEarlyAdvance = true;
    [Tooltip("Tempo minimo antes de aceitar confirmacao ou avance antecipado, evitando fechar no mesmo clique que iniciou a fala.")]
    [Min(0f)] public float minimumDisplayDuration = .15f;
}

/// <summary>Nome reutilizavel de speaker e sua cor.</summary>
[Serializable]
public sealed class DialogueSpeakerStyle
{
    [Tooltip("Nome do speaker como aparece no campo Speaker Name das falas.")]
    public string speakerName;
    [Tooltip("Cor usada no nome deste speaker quando a fala nao tem override personalizado.")]
    public Color color = Color.white;
}

/// <summary>
/// Conteudo reutilizavel de dialogo. Uma sequencia pode conter uma unica
/// mensagem curta ou varias falas exibidas em paginas consecutivas.
/// </summary>
[CreateAssetMenu(fileName = "DialogueSequence", menuName = "Projeto PSX/Dialogue Sequence")]
public sealed class DialogueSequence : ScriptableObject
{
    [Tooltip("Falas exibidas em ordem por esta sequencia.")]
    public List<DialogueLine> lines = new();
    [Tooltip("Falas alternativas exibidas em ordem se o jogador sair da Leave Area enquanto Lines ainda estiverem sendo exibidas. Deixe vazio para nao usar essa opcao.")]
    public List<DialogueLine> leaveEarlyLines = new();

    public bool HasLines => lines != null && lines.Count > 0;
    public bool HasLeaveEarlyLines => leaveEarlyLines != null &&
        leaveEarlyLines.Count > 0;

    private void OnValidate()
    {
        ValidateLines(lines);
        ValidateLines(leaveEarlyLines);
    }

    private static void ValidateLines(List<DialogueLine> dialogueLines)
    {
        if (dialogueLines == null)
            return;

        foreach (DialogueLine line in dialogueLines)
        {
            if (line == null)
                continue;
            line.displayDuration = Mathf.Max(0f, line.displayDuration);
            line.minimumDisplayDuration = Mathf.Max(0f,
                line.minimumDisplayDuration);
            // Confirm sem confirmacao criaria uma fala impossivel de fechar.
            if (line.finishMode == DialogueFinishMode.Confirm)
                line.allowEarlyAdvance = true;
        }
    }
}

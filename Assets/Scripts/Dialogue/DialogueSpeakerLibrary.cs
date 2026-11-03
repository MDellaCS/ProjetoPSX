using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum DialogueSpeakerNameCase
{
    AsTyped,
    Uppercase,
    Lowercase
}

/// <summary>Configuracao global de nomes e cores reutilizada por todos os dialogos.</summary>
[CreateAssetMenu(fileName = "DialogueSpeakerLibrary", menuName = "Projeto PSX/Dialogue Speaker Library")]
public sealed class DialogueSpeakerLibrary : ScriptableObject
{
    [Header("Estilo Global do Nome")]
    [Tooltip("Transforma os nomes de todos os speakers em maiusculas, minusculas ou preserva como foram escritos.")]
    public DialogueSpeakerNameCase nameCase = DialogueSpeakerNameCase.AsTyped;
    [Tooltip("Aplica negrito aos nomes dos speakers.")]
    public bool bold;
    [Tooltip("Aplica italico aos nomes dos speakers.")]
    public bool italic;
    [Tooltip("Tamanho em pixels do nome do speaker.")]
    [Min(1f)] public float fontSize = 26f;
    [Tooltip("Alinhamento horizontal do nome do speaker.")]
    public TextAnchor alignment = TextAnchor.MiddleCenter;
    [Tooltip("Espaco vertical em pixels entre o nome do speaker e a fala.")]
    [Min(0f)] public float spacingBelow = 4f;
    [Tooltip("Cor usada quando o speaker nao existe na lista abaixo.")]
    public Color defaultColor = Color.white;

    [Header("Cores dos Speakers")]
    [Tooltip("Lista global de speakers reutilizada por todas as sequencias de dialogo.")]
    public List<DialogueSpeakerStyle> speakers = new();

    public string FormatSpeakerName(string speakerName)
    {
        string value = (speakerName ?? string.Empty).Trim();
        return nameCase switch
        {
            DialogueSpeakerNameCase.Uppercase => value.ToUpperInvariant(),
            DialogueSpeakerNameCase.Lowercase => value.ToLowerInvariant(),
            _ => value
        };
    }

    public Color GetSpeakerColor(DialogueLine line)
    {
        if (line == null)
            return defaultColor;
        if (line.overrideSpeakerColor)
            return line.speakerColor;
        if (speakers != null)
        {
            foreach (DialogueSpeakerStyle speaker in speakers)
            {
                if (speaker != null && !string.IsNullOrWhiteSpace(speaker.speakerName) &&
                    string.Equals(speaker.speakerName.Trim(),
                        (line.speakerName ?? string.Empty).Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    return speaker.color;
            }
        }
        return defaultColor;
    }
}

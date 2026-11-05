using UnityEngine;

/// <summary>Configuracao do audio de dialogo (Resources/Audio/DialogueAudioSettings).</summary>
[CreateAssetMenu(fileName = "DialogueAudioSettings", menuName = "Projeto PSX/Audio/Dialogue Audio Settings")]
public sealed class DialogueAudioSettings : AudioModuleSettings
{
    [Header("Dialogo")]
    [Tooltip("Ao iniciar uma fala, interrompe a voz anterior que ainda esteja tocando.")]
    public bool interruptPreviousVoice;
    [Tooltip("Prioridade das vozes no mixer Unity: zero e a mais alta. Vozes devem ser raramente cortadas.")]
    [Range(0, 255)] public int priority = 32;
}

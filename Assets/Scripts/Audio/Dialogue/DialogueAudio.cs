using UnityEngine;

/// <summary>
/// Modulo de audio de dialogo: vozes das falas, sempre 2D e sem variacao
/// aleatoria, para o volume ser previsivel. Configurado em
/// Resources/Audio/DialogueAudioSettings.
/// </summary>
public sealed class DialogueAudio : AudioModule<DialogueAudioSettings>
{
    /// <summary>Toca a voz de uma fala. Uma nova voz nao corta a anterior, a menos que a configuracao peca.</summary>
    public static void PlayVoice(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        DialogueAudio module = GetModule<DialogueAudio>();
        if (module.Settings.interruptPreviousVoice)
            module.StopAll();
        module.PlayClip(clip, volume, 1f, Vector3.zero, 0f, 1f, 25f,
            AudioRolloffMode.Linear, module.Settings.priority);
    }

    /// <summary>Interrompe todas as vozes de dialogo (ex.: ao pular uma cutscene).</summary>
    public static void StopVoices() => GetModule<DialogueAudio>().StopAll();
}

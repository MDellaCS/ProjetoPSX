using UnityEngine;

/// <summary>
/// Modulo de audio de SFX de gameplay: golpes, passos, dano, notches de Heat e
/// sons de inimigos. Configurado em Resources/Audio/SfxAudioSettings.
/// </summary>
public sealed class SfxAudio : AudioModule<SfxAudioSettings>
{
    private AudioPlaybackHandle heatNotchPlayback;
    private AudioClip pendingHeatNotchClip;
    private Vector3 pendingHeatNotchPosition;
    private bool heatNotchPending;

    /// <summary>Configuracao do SFX, sem precisar de nenhum objeto na cena.</summary>
    public static SfxAudioSettings CurrentSettings => GetModule<SfxAudio>().Settings;

    public static void Play(SfxCueId cueId, Vector3 position)
    {
        SfxAudio module = GetModule<SfxAudio>();
        module.PlayCue(module.Settings.GetCue(cueId), position);
    }

    /// <summary>Reproduz uma configuracao de som criada pelo proprio objeto de gameplay.</summary>
    public static void Play(AudioCue cue, Vector3 position)
    {
        GetModule<SfxAudio>().PlayCue(cue, position);
    }

    /// <summary>Reproduz um clip avulso em 2D.</summary>
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        GetModule<SfxAudio>().PlayClip(clip, volume, 1f, Vector3.zero, 128);
    }

    /// <summary>Reproduz um SFX e devolve uma referencia que pode interrompe-lo sem afetar outras vozes.</summary>
    public static AudioPlaybackHandle PlayTracked(AudioCue cue, Vector3 position)
    {
        return GetModule<SfxAudio>().PlayCue(cue, position);
    }

    /// <summary>Toca o som configurado do notch (ganho ou perda) de Heat.</summary>
    public static void PlayHeatNotch(int previousNotch, int nextNotch, Vector3 position)
    {
        SfxAudio module = GetModule<SfxAudio>();
        module.heatNotchPlayback?.Stop();
        module.heatNotchPlayback = null;
        module.pendingHeatNotchClip = module.Settings.GetHeatNotchClip(previousNotch, nextNotch);
        module.pendingHeatNotchPosition = position;
        if (module.heatNotchPending) return;
        module.heatNotchPending = true;
        module.StartCoroutine(module.PlayPendingHeatNotch());
    }

    private System.Collections.IEnumerator PlayPendingHeatNotch()
    {
        yield return new WaitForEndOfFrame();
        heatNotchPending = false;
        heatNotchPlayback = PlayClip(pendingHeatNotchClip, Settings.notchVolume, 1f,
            pendingHeatNotchPosition, Settings.notchPriority, false);
        pendingHeatNotchClip = null;
    }
}

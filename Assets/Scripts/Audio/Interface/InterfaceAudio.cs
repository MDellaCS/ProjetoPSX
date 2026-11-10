using UnityEngine;

public enum MenuAudioCue
{
    None,
    Focus,
    Confirm,
    Back,
    MenuOpen,
    MenuClose,
    TabChange,
    Toggle,
    Adjust,
    Unavailable,
    Success,
    Warning
}

/// <summary>API semantica de sons de interface (menus, lojas, HUDs).</summary>
public static class MenuUIAudio
{
    public static void Play(MenuAudioCue cue)
    {
        if (cue != MenuAudioCue.None)
            InterfaceAudio.Play(cue);
    }
}

/// <summary>
/// Modulo de audio de interface: sons 2D de menus e HUDs, configurados em
/// Resources/Audio/InterfaceAudioSettings e controlados pelo volume de interface.
/// </summary>
public sealed class InterfaceAudio : AudioModule<InterfaceAudioSettings>
{
    private float nextFocusSoundAt;

    public static void Play(MenuAudioCue cue)
    {
        InterfaceAudio module = GetModule<InterfaceAudio>();
        if (cue == MenuAudioCue.Focus && Time.unscaledTime < module.nextFocusSoundAt)
            return;

        AudioCue audioCue = module.Settings.GetCue(cue);
        if (audioCue == null || !audioCue.HasClip)
            return;

        if (cue == MenuAudioCue.Focus)
            module.nextFocusSoundAt = Time.unscaledTime +
                Mathf.Max(0f, module.Settings.focusMinimumInterval);

        module.PlayCue(audioCue, Vector3.zero);
    }

    /// <summary>Toca um clip avulso de interface (ex.: titulo de area), sem variacao aleatoria.</summary>
    public static void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
            return;

        GetModule<InterfaceAudio>().PlayClip(clip, volume, pitch, Vector3.zero, 128);
    }
}

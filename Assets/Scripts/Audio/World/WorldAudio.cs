using UnityEngine;

/// <summary>
/// Modulo de audio de mundo: sons posicionais do cenario e de objetos (reacoes
/// de objetos, reflexos, ambiente). Configurado em Resources/Audio/WorldAudioSettings.
/// </summary>
public sealed class WorldAudio : AudioModule<WorldAudioSettings>
{
    /// <summary>Toca um cue configurado na posicao do mundo.</summary>
    public static void PlayAt(AudioCue cue, Vector3 position)
    {
        if (cue == null || !cue.HasClip)
            return;

        GetModule<WorldAudio>().PlayCue(cue, position);
    }

    /// <summary>Toca um clip avulso na posicao do mundo, com os alcances padrao da configuracao.</summary>
    public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
            return;

        WorldAudio module = GetModule<WorldAudio>();
        WorldAudioSettings settings = module.Settings;
        module.PlayClip(clip, volume, 1f, position, settings.defaultSpatialBlend,
            settings.defaultMinDistance, settings.defaultMaxDistance,
            settings.defaultRolloff, settings.defaultPriority);
    }
}

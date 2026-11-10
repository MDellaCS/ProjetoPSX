using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base dos modulos de audio do jogo (Interface, Dialogo e SFX). Cada
/// modulo tem seu proprio pool de AudioSources e sua configuracao em
/// Resources/Audio; todos sao criados sob demanda sob um unico objeto
/// persistente, entao nenhuma cena precisa conter componentes de audio.
/// </summary>
public abstract class AudioModule : MonoBehaviour
{
    protected sealed class Voice
    {
        public AudioSource source;
        public float baseVolume;
        public float startedAt;
        public int playbackToken;
    }

    private static readonly Dictionary<Type, AudioModule> modules = new();
    private static Transform root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        modules.Clear();
        root = null;
    }

    protected readonly List<Voice> voices = new();
    private int nextPlaybackToken;

    protected abstract AudioModuleSettings BaseSettings { get; }

    /// <summary>Devolve o modulo, criando-o (e o objeto persistente) na primeira chamada.</summary>
    protected static T GetModule<T>() where T : AudioModule
    {
        if (modules.TryGetValue(typeof(T), out AudioModule existing) && existing != null)
            return (T)existing;

        if (root == null)
        {
            GameObject rootObject = new("Game Audio (Persistent)");
            DontDestroyOnLoad(rootObject);
            root = rootObject.transform;
        }

        GameObject moduleObject = new(typeof(T).Name);
        moduleObject.transform.SetParent(root, false);
        T module = moduleObject.AddComponent<T>();
        modules[typeof(T)] = module;
        return module;
    }

    /// <summary>Reaplica os volumes das configuracoes do jogador as vozes em execucao.</summary>
    public static void RefreshAllVolumes()
    {
        foreach (AudioModule module in modules.Values)
            if (module != null)
                module.RefreshVolume();
    }

    protected virtual void Awake()
    {
        int count = Mathf.Max(1, BaseSettings.initialVoiceCount);
        while (voices.Count < count)
            CreateVoice();
    }

    public void RefreshVolume()
    {
        float multiplier = GetOutputVolume();
        foreach (Voice voice in voices)
            if (voice.source != null)
                voice.source.volume = multiplier;
    }

    /// <summary>Interrompe todos os sons deste modulo.</summary>
    public void StopAll()
    {
        foreach (Voice voice in voices)
        {
            if (voice.source == null)
                continue;
            voice.source.Stop();
            voice.playbackToken = ++nextPlaybackToken;
        }
    }

    protected float GetOutputVolume()
    {
        AudioModuleSettings settings = BaseSettings;
        if (!settings.moduleEnabled)
            return 0f;

        GameSettingsData applied = GameSettingsService.Applied;
        return settings.volumeChannel switch
        {
            AudioVolumeChannel.Effects => applied.effectsVolume,
            AudioVolumeChannel.Interface => applied.uiVolume,
            _ => 1f
        };
    }

    /// <summary>Toca o cue em 2D, sem atenuacao por distancia.</summary>
    protected AudioPlaybackHandle PlayCue(AudioCue cue, Vector3 position)
    {
        if (cue == null || !BaseSettings.moduleEnabled)
            return null;

        AudioClip clip = cue.GetRandomClip();
        if (clip == null)
            return null;

        return PlayClip(clip,
            cue.volume + UnityEngine.Random.Range(-cue.volumeVariation, cue.volumeVariation),
            cue.pitch + UnityEngine.Random.Range(-cue.pitchVariation, cue.pitchVariation),
            position, cue.priority);
    }

    protected AudioPlaybackHandle PlayClip(AudioClip clip, float volume, float pitch,
        Vector3 position, int priority)
    {
        AudioModuleSettings settings = BaseSettings;
        if (clip == null || !settings.moduleEnabled)
            return null;

        Voice voice = AcquireVoice();
        AudioSource source = voice.source;
        source.Stop();
        source.transform.position = position;
        source.spatialBlend = 0f;
        source.priority = Mathf.Clamp(priority, 0, 255);
        source.dopplerLevel = 0f;
        voice.baseVolume = Mathf.Max(0f, volume) * settings.globalVolume;
        source.volume = GetOutputVolume();
        source.pitch = Mathf.Max(.01f, pitch) * settings.globalPitch;
        voice.startedAt = Time.unscaledTime;
        voice.playbackToken = ++nextPlaybackToken;
        // PlayOneShot aceita escalas acima de 1, ao contrario do limite pratico de
        // AudioSource.volume; isso permite ganho de ate 5x por cue.
        source.PlayOneShot(clip, voice.baseVolume);
        return new AudioPlaybackHandle(this, voices.IndexOf(voice), voice.playbackToken);
    }

    internal void StopPlayback(int voiceIndex, int playbackToken)
    {
        if (voiceIndex < 0 || voiceIndex >= voices.Count)
            return;

        Voice voice = voices[voiceIndex];
        if (voice.playbackToken != playbackToken || voice.source == null)
            return;

        voice.source.Stop();
        voice.playbackToken = ++nextPlaybackToken;
    }

    private Voice AcquireVoice()
    {
        foreach (Voice voice in voices)
            if (voice.source != null && !voice.source.isPlaying)
                return voice;

        AudioModuleSettings settings = BaseSettings;
        int maximum = Mathf.Max(settings.initialVoiceCount, settings.maximumVoiceCount);
        if (voices.Count < maximum)
            return CreateVoice();

        Voice oldest = voices[0];
        foreach (Voice voice in voices)
            if (voice.startedAt < oldest.startedAt)
                oldest = voice;
        return oldest;
    }

    private Voice CreateVoice()
    {
        GameObject voiceObject = new("Voice");
        voiceObject.transform.SetParent(transform, false);
        AudioSource source = voiceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.dopplerLevel = 0f;
        Voice voice = new() { source = source };
        voices.Add(voice);
        return voice;
    }
}

/// <summary>
/// Base tipada de um modulo: carrega a configuracao de Resources/Audio pelo nome
/// da classe de configuracao. Sem o asset, usa os valores padrao da classe.
/// </summary>
public abstract class AudioModule<TSettings> : AudioModule
    where TSettings : AudioModuleSettings
{
    private const string SettingsFolder = "Audio/";
    private TSettings settings;

    public TSettings Settings
    {
        get
        {
            if (settings == null)
                settings = LoadSettings();
            return settings;
        }
    }

    protected override AudioModuleSettings BaseSettings => Settings;

    /// <summary>Configuracao do modulo sem precisar criar o objeto de audio.</summary>
    public static TSettings LoadSettings()
    {
        TSettings loaded = Resources.Load<TSettings>(SettingsFolder + typeof(TSettings).Name);
        if (loaded != null)
            return loaded;

        Debug.LogWarning("Audio: Resources/" + SettingsFolder + typeof(TSettings).Name +
            " nao encontrado; usando valores padrao.");
        return ScriptableObject.CreateInstance<TSettings>();
    }
}

/// <summary>Referencia a uma voz tocando, que pode ser parada sem afetar outros sons.</summary>
public sealed class AudioPlaybackHandle
{
    private AudioModule module;
    private readonly int voiceIndex;
    private readonly int playbackToken;

    internal AudioPlaybackHandle(AudioModule owner, int sourceVoiceIndex, int sourcePlaybackToken)
    {
        module = owner;
        voiceIndex = sourceVoiceIndex;
        playbackToken = sourcePlaybackToken;
    }

    public void Stop()
    {
        if (module == null)
            return;

        module.StopPlayback(voiceIndex, playbackToken);
        module = null;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>Reprodutor pooled de SFX de gameplay, independente do audio de dialogo.</summary>
[DisallowMultipleComponent]
public sealed class GameplayAudioSystem : MonoBehaviour
{
    private sealed class Voice
    {
        public AudioSource source;
        public float baseVolume;
        public float startedAt;
        public int playbackToken;
    }

    private static GameplayAudioSystem instance;
    private readonly List<Voice> voices = new();
    private int nextPlaybackToken;

    [Tooltip("Configuracao global e cues de gameplay. Se vazio, carrega Resources/GameplayAudioSettings.")]
    [SerializeField] private GameplayAudioSettings settings;

    public GameplayAudioSettings Settings
    {
        get
        {
            LoadSettings();
            return settings;
        }
    }

    public static GameplayAudioSystem Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindAnyObjectByType<GameplayAudioSystem>();
            if (instance != null)
                return instance;

            GameObject root = new("GameplayAudioSystem");
            instance = root.AddComponent<GameplayAudioSystem>();
            DontDestroyOnLoad(root);
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        LoadSettings();
        CreateInitialVoices();
    }

    public static void Play(GameplayAudioCueId cueId, Vector3 position)
    {
        GameplayAudioSystem system = Instance;
        system.PlayCue(system.Settings.GetCue(cueId), position);
    }

    /// <summary>Reproduz uma configuracao de som criada pelo proprio objeto de gameplay.</summary>
    public static void Play(GameplayAudioCue cue, Vector3 position)
    {
        Instance.PlayCue(cue, position);
    }

    /// <summary>Reproduz um SFX e devolve uma referencia que pode interrompe-lo sem afetar outras vozes.</summary>
    public static GameplayAudioPlaybackHandle PlayTracked(
        GameplayAudioCue cue, Vector3 position)
    {
        GameplayAudioSystem system = Instance;
        return system.PlayCue(cue, position);
    }

    /// <summary>Atualiza vozes em execucao quando o jogador altera o volume de efeitos.</summary>
    public static void RefreshEffectsVolume()
    {
        if (instance == null)
            return;

        instance.LoadSettings();
        foreach (Voice voice in instance.voices)
            if (voice.source != null)
                voice.source.volume = instance.GetOutputVolumeMultiplier();
    }

    private void LoadSettings()
    {
        if (settings != null)
            return;

        settings = Resources.Load<GameplayAudioSettings>("GameplayAudioSettings");
        if (settings == null)
            settings = ScriptableObject.CreateInstance<GameplayAudioSettings>();
    }

    private void CreateInitialVoices()
    {
        int count = Mathf.Max(1, settings.initialVoiceCount);
        while (voices.Count < count)
            CreateVoice();
    }

    private GameplayAudioPlaybackHandle PlayCue(GameplayAudioCue cue,
        Vector3 position)
    {
        LoadSettings();
        if (!settings.sfxEnabled)
            return null;

        AudioClip clip = ChooseClip(cue);
        if (clip == null)
            return null;

        Voice voice = GetAvailableVoice();
        AudioSource source = voice.source;
        source.Stop();
        source.transform.position = position;
        source.spatialBlend = cue.spatialBlend;
        source.minDistance = Mathf.Max(0f, cue.minDistance);
        source.maxDistance = Mathf.Max(source.minDistance + .01f,
            cue.maxDistance);
        source.rolloffMode = cue.rolloffMode;
        source.priority = cue.priority;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = cue.outputMixerGroup != null
            ? cue.outputMixerGroup : settings.defaultOutputMixerGroup;
        voice.baseVolume = Mathf.Max(0f, cue.volume + Random.Range(
            -cue.volumeVariation, cue.volumeVariation)) * settings.globalVolume;
        source.volume = GetOutputVolumeMultiplier();
        source.pitch = Mathf.Max(.01f, cue.pitch + Random.Range(
            -cue.pitchVariation, cue.pitchVariation)) * settings.globalPitch;
        voice.startedAt = Time.unscaledTime;
        voice.playbackToken = ++nextPlaybackToken;
        // PlayOneShot aceita escalas acima de 1, ao contrario do limite
        // pratico de AudioSource.volume; isso permite ganho de ate 5x por cue.
        source.PlayOneShot(clip, voice.baseVolume);
        return new GameplayAudioPlaybackHandle(this, voices.IndexOf(voice),
            voice.playbackToken);
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

    private static AudioClip ChooseClip(GameplayAudioCue cue)
    {
        if (cue?.clips == null || cue.clips.Length == 0)
            return null;

        int validCount = 0;
        foreach (AudioClip clip in cue.clips)
            if (clip != null)
                validCount++;
        if (validCount == 0)
            return null;

        int selectedIndex = Random.Range(0, validCount);
        foreach (AudioClip clip in cue.clips)
        {
            if (clip == null)
                continue;
            if (selectedIndex-- == 0)
                return clip;
        }

        return null;
    }

    private Voice GetAvailableVoice()
    {
        foreach (Voice voice in voices)
            if (voice.source != null && !voice.source.isPlaying)
                return voice;

        int maximum = Mathf.Max(settings.initialVoiceCount,
            settings.maximumVoiceCount);
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
        GameObject voiceObject = new("Gameplay SFX Voice");
        voiceObject.transform.SetParent(transform, false);
        AudioSource source = voiceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.dopplerLevel = 0f;
        Voice voice = new() { source = source };
        voices.Add(voice);
        return voice;
    }

    private float GetOutputVolumeMultiplier() => settings.sfxEnabled
        ? GameSettingsService.Applied.effectsVolume
        : 0f;
}

/// <summary>Handle de uma voz de gameplay que pode ser parada sem interromper outros SFX.</summary>
public sealed class GameplayAudioPlaybackHandle
{
    private GameplayAudioSystem system;
    private readonly int voiceIndex;
    private readonly int playbackToken;

    internal GameplayAudioPlaybackHandle(GameplayAudioSystem owner,
        int sourceVoiceIndex, int sourcePlaybackToken)
    {
        system = owner;
        voiceIndex = sourceVoiceIndex;
        playbackToken = sourcePlaybackToken;
    }

    public void Stop()
    {
        if (system == null)
            return;

        system.StopPlayback(voiceIndex, playbackToken);
        system = null;
    }
}

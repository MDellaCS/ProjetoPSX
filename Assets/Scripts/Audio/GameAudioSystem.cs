using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Definicao configuravel de um som do jogo. Um cue pode conter variacoes
/// equivalentes para evitar repeticao mecanica do mesmo arquivo.
/// </summary>
[Serializable]
public class AudioCue
{
    [Tooltip("Uma ou mais variacoes do mesmo efeito sonoro.")]
    public AudioClip[] clips;

    [Header("Variation")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float volumeVariation = .08f;
    [Min(0.01f)] public float pitch = 1f;
    [Range(0f, 1f)] public float pitchVariation = .06f;

    [Header("Spatial")]
    [Tooltip("Ligado: o som vem de uma posicao do mundo. Desligado: e som de interface/tela.")]
    public bool is3D = true;
    [Min(0f)] public float minDistance = 1f;
    [Min(0f)] public float maxDistance = 25f;
    [Tooltip("Grupo opcional do Audio Mixer para este som.")]
    public AudioMixerGroup outputMixerGroup;

    public bool HasClip
    {
        get
        {
            if (clips == null)
                return false;

            foreach (AudioClip clip in clips)
            {
                if (clip != null)
                    return true;
            }

            return false;
        }
    }

    public AudioClip GetRandomClip()
    {
        if (!HasClip)
            return null;

        List<AudioClip> validClips = new();
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                validClips.Add(clip);
        }

        return validClips[UnityEngine.Random.Range(0, validClips.Count)];
    }
}

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

/// <summary>
/// API semantica de sons de interface; a configuracao e a reproducao ficam
/// centralizadas no GameAudioSystem global.
/// </summary>
public static class MenuUIAudio
{
    public static void Play(MenuAudioCue cue)
    {
        if (cue != MenuAudioCue.None)
            GameAudioSystem.PlayMenuSound(cue);
    }
}

[Serializable]
public sealed class MenuAudioCueSettings
{
    [Tooltip("Clip tocado para este evento de interface.")]
    public AudioClip clip;
    [Tooltip("Volume deste evento antes dos volumes globais de interface e geral.")]
    [Range(0f, 1f)] public float volume = .7f;
    [Tooltip("Pitch usado ao reproduzir o clip.")]
    [Range(.1f, 3f)] public float pitch = 1f;
    [Tooltip("Grupo opcional do Audio Mixer para este evento.")]
    public AudioMixerGroup outputMixerGroup;
}

/// <summary>
/// Reprodutor central de efeitos sonoros. Mantem um pool de AudioSources para
/// que gameplay apenas solicite um AudioCue, sem criar/destruir objetos a cada
/// impacto. Pode existir na cena para ser configurado; se nao existir, surge
/// automaticamente e toca os cues enviados por codigo.
/// </summary>
[DisallowMultipleComponent]
public class GameAudioSystem : MonoBehaviour
{
    private sealed class PooledSource
    {
        public AudioSource source;
        public float startedAt;
        public float baseVolume;
        public bool usesMenuVolume;
    }

    private static GameAudioSystem instance;
    private readonly List<PooledSource> sources = new();

    [Header("Source Pool")]
    [Tooltip("Fontes criadas logo no inicio para evitar custo no primeiro impacto.")]
    [Min(1)] [SerializeField] private int initialSourceCount = 12;
    [Tooltip("Limite de vozes simultaneas. Ao atingir o limite, o som mais antigo e reutilizado.")]
    [Min(1)] [SerializeField] private int maximumSourceCount = 32;

    [Header("Sons Globais de Interface")]
    [Tooltip("Som curto ao mudar o foco para uma opcao com mouse, teclado ou controle.")]
    [SerializeField] private MenuAudioCueSettings focusSound = new();
    [Tooltip("Som ao confirmar uma opcao ou abrir uma tela interna.")]
    [SerializeField] private MenuAudioCueSettings confirmSound = new();
    [Tooltip("Som ao voltar ou cancelar uma tela interna.")]
    [SerializeField] private MenuAudioCueSettings backSound = new();
    [Tooltip("Som ao abrir a pausa, o menu inicial ou a loja.")]
    [SerializeField] private MenuAudioCueSettings menuOpenSound = new();
    [Tooltip("Som ao fechar a pausa ou outro menu principal.")]
    [SerializeField] private MenuAudioCueSettings menuCloseSound = new();
    [Tooltip("Som ao trocar de aba ou categoria do menu.")]
    [SerializeField] private MenuAudioCueSettings tabChangeSound = new();
    [Tooltip("Som ao alternar uma opcao ligada/desligada.")]
    [SerializeField] private MenuAudioCueSettings toggleSound = new();
    [Tooltip("Som ao aumentar ou reduzir um valor de configuracao.")]
    [SerializeField] private MenuAudioCueSettings adjustSound = new();
    [Tooltip("Som de opcao indisponivel, compra recusada ou requisito nao atendido.")]
    [SerializeField] private MenuAudioCueSettings unavailableSound = new();
    [Tooltip("Som de compra, salvamento ou outra acao concluida com sucesso.")]
    [SerializeField] private MenuAudioCueSettings successSound = new();
    [Tooltip("Som de aviso antes de uma acao importante ou de restaurar configuracoes.")]
    [SerializeField] private MenuAudioCueSettings warningSound = new();
    [Tooltip("Intervalo minimo entre sons consecutivos de foco para evitar repeticao cansativa.")]
    [Min(0f)] [SerializeField] private float focusSoundMinimumInterval = .06f;

    private float nextFocusSoundAt;

    public static GameAudioSystem Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindAnyObjectByType<GameAudioSystem>();
            if (instance != null)
                return instance;

            GameObject systemObject = new GameObject("GameAudioSystem");
            instance = systemObject.AddComponent<GameAudioSystem>();
            DontDestroyOnLoad(systemObject);
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
        maximumSourceCount = Mathf.Max(maximumSourceCount, initialSourceCount);
        EnsureInitialSources();
    }

    /// <summary>Toca um som de mundo na posicao indicada.</summary>
    public static void PlayAt(AudioCue cue, Vector3 position)
    {
        if (cue == null || !cue.HasClip)
            return;

        Instance.Play(cue, position, true);
    }

    /// <summary>Toca um som de interface, sem posicao no mundo.</summary>
    public static void Play2D(AudioCue cue)
    {
        if (cue == null || !cue.HasClip)
            return;

        Instance.Play(cue, Vector3.zero, false);
    }

    /// <summary>
    /// Toca uma voz/interface 2D simples, sem as variacoes aleatorias de um
    /// AudioCue. Usado por dialogos, que precisam de volume previsivel.
    /// </summary>
    public static void Play2D(AudioClip clip, float volume)
    {
        Play2D(clip, volume, 1f, null);
    }

    /// <summary>Toca um som de interface com volume, pitch e grupo de mixer configuraveis.</summary>
    public static void Play2D(AudioClip clip, float volume, float pitch,
        AudioMixerGroup outputMixerGroup = null)
    {
        if (clip == null)
            return;

        Instance.PlaySimple2D(clip, volume, pitch, outputMixerGroup);
    }

    public static void PlayMenuSound(MenuAudioCue cue)
    {
        GameAudioSystem system = Instance;
        if (cue == MenuAudioCue.Focus && Time.unscaledTime < system.nextFocusSoundAt)
            return;

        MenuAudioCueSettings settings = system.GetMenuCueSettings(cue);
        if (settings == null || settings.clip == null)
            return;

        if (cue == MenuAudioCue.Focus)
            system.nextFocusSoundAt = Time.unscaledTime +
                Mathf.Max(0f, system.focusSoundMinimumInterval);

        system.PlayMenuSound(settings);
    }

    private void EnsureInitialSources()
    {
        while (sources.Count < initialSourceCount)
            CreateSource();
    }

    private void Play(AudioCue cue, Vector3 position, bool hasWorldPosition)
    {
        AudioClip clip = cue.GetRandomClip();
        if (clip == null)
            return;

        PooledSource pooledSource = GetAvailableSource();
        AudioSource source = pooledSource.source;

        source.Stop();
        pooledSource.usesMenuVolume = false;
        source.clip = clip;
        source.transform.position = position;
        source.spatialBlend = cue.is3D && hasWorldPosition ? 1f : 0f;
        source.minDistance = cue.minDistance;
        source.maxDistance = Mathf.Max(cue.minDistance, cue.maxDistance);
        source.outputAudioMixerGroup = cue.outputMixerGroup;
        pooledSource.baseVolume = Mathf.Clamp01(cue.volume + UnityEngine.Random.Range(
            -cue.volumeVariation,
            cue.volumeVariation
        ));
        source.volume = pooledSource.baseVolume * GameSettingsService.Applied.effectsVolume;
        source.pitch = Mathf.Max(.01f, cue.pitch + UnityEngine.Random.Range(
            -cue.pitchVariation,
            cue.pitchVariation
        ));
        source.Play();
        pooledSource.startedAt = Time.unscaledTime;
    }

    private void PlaySimple2D(AudioClip clip, float volume, float pitch,
        AudioMixerGroup outputMixerGroup)
    {
        PooledSource pooledSource = GetAvailableSource();
        AudioSource source = pooledSource.source;
        source.Stop();
        pooledSource.usesMenuVolume = false;
        source.clip = clip;
        source.transform.position = Vector3.zero;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = outputMixerGroup;
        pooledSource.baseVolume = Mathf.Clamp01(volume);
        source.volume = pooledSource.baseVolume *
            GameSettingsService.Applied.effectsVolume;
        source.pitch = Mathf.Max(.01f, pitch);
        source.Play();
        pooledSource.startedAt = Time.unscaledTime;
    }

    private void PlayMenuSound(MenuAudioCueSettings settings)
    {
        PooledSource pooledSource = GetAvailableSource();
        AudioSource source = pooledSource.source;
        source.Stop();
        pooledSource.usesMenuVolume = true;
        source.clip = settings.clip;
        source.transform.position = Vector3.zero;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = settings.outputMixerGroup;
        pooledSource.baseVolume = Mathf.Clamp01(settings.volume);
        source.volume = pooledSource.baseVolume *
            GameSettingsService.Applied.uiVolume;
        source.pitch = Mathf.Max(.01f, settings.pitch);
        source.Play();
        pooledSource.startedAt = Time.unscaledTime;
    }

    private MenuAudioCueSettings GetMenuCueSettings(MenuAudioCue cue)
    {
        return cue switch
        {
            MenuAudioCue.Focus => focusSound,
            MenuAudioCue.Confirm => confirmSound,
            MenuAudioCue.Back => backSound,
            MenuAudioCue.MenuOpen => menuOpenSound,
            MenuAudioCue.MenuClose => menuCloseSound,
            MenuAudioCue.TabChange => tabChangeSound,
            MenuAudioCue.Toggle => toggleSound,
            MenuAudioCue.Adjust => adjustSound,
            MenuAudioCue.Unavailable => unavailableSound,
            MenuAudioCue.Success => successSound,
            MenuAudioCue.Warning => warningSound,
            _ => null
        };
    }

    public static void RefreshEffectsVolume()
    {
        if (instance == null)
            return;
        float volume = GameSettingsService.Applied.effectsVolume;
        foreach (PooledSource pooledSource in instance.sources)
            if (!pooledSource.usesMenuVolume)
                pooledSource.source.volume = pooledSource.baseVolume * volume;
    }

    public static void RefreshMenuVolume()
    {
        if (instance == null)
            return;

        float volume = GameSettingsService.Applied.uiVolume;
        foreach (PooledSource pooledSource in instance.sources)
            if (pooledSource.usesMenuVolume)
                pooledSource.source.volume = pooledSource.baseVolume * volume;
    }

    private PooledSource GetAvailableSource()
    {
        foreach (PooledSource pooledSource in sources)
        {
            if (!pooledSource.source.isPlaying)
                return pooledSource;
        }

        if (sources.Count < maximumSourceCount)
            return CreateSource();

        PooledSource oldest = sources[0];
        foreach (PooledSource pooledSource in sources)
        {
            if (pooledSource.startedAt < oldest.startedAt)
                oldest = pooledSource;
        }

        return oldest;
    }

    private PooledSource CreateSource()
    {
        GameObject sourceObject = new GameObject("Pooled Audio Source");
        sourceObject.transform.SetParent(transform);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.dopplerLevel = 0f;

        PooledSource pooledSource = new PooledSource { source = source };
        sources.Add(pooledSource);
        return pooledSource;
    }
}

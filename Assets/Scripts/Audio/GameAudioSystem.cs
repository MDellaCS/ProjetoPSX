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
    }

    private static GameAudioSystem instance;
    private readonly List<PooledSource> sources = new();

    [Header("Source Pool")]
    [Tooltip("Fontes criadas logo no inicio para evitar custo no primeiro impacto.")]
    [Min(1)] [SerializeField] private int initialSourceCount = 12;
    [Tooltip("Limite de vozes simultaneas. Ao atingir o limite, o som mais antigo e reutilizado.")]
    [Min(1)] [SerializeField] private int maximumSourceCount = 32;

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
        source.clip = clip;
        source.transform.position = position;
        source.spatialBlend = cue.is3D && hasWorldPosition ? 1f : 0f;
        source.minDistance = cue.minDistance;
        source.maxDistance = Mathf.Max(cue.minDistance, cue.maxDistance);
        source.outputAudioMixerGroup = cue.outputMixerGroup;
        source.volume = Mathf.Clamp01(cue.volume + UnityEngine.Random.Range(
            -cue.volumeVariation,
            cue.volumeVariation
        ));
        source.pitch = Mathf.Max(.01f, cue.pitch + UnityEngine.Random.Range(
            -cue.pitchVariation,
            cue.pitchVariation
        ));
        source.Play();
        pooledSource.startedAt = Time.unscaledTime;
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

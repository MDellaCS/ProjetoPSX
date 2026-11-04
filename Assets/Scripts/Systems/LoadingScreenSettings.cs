using UnityEngine;

/// <summary>Configuracao global da tela de loading (tempos de espera e fade).</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Systems/Loading Screen Settings",
    fileName = "LoadingScreenSettings")]
public sealed class LoadingScreenSettings : ScriptableObject
{
    public const string ResourcePath = "LoadingScreenSettings";

    [Tooltip("Tempo fixo, em segundos reais, que a tela de loading continua totalmente visivel DEPOIS que o carregamento chegou a 100%, antes de comecar a esmaecer. Nao depende da duracao do carregamento.")]
    [Min(0f)] public float extraWaitAfterReady = .5f;
    [Tooltip("Duracao, em segundos reais, do esmaecimento (fade out) da tela de loading.")]
    [Min(0f)] public float fadeDuration = .3f;

    private static LoadingScreenSettings cached;

    /// <summary>Asset em Resources; se nao existir, usa os valores padrao.</summary>
    public static LoadingScreenSettings Current
    {
        get
        {
            if (cached == null)
                cached = Resources.Load<LoadingScreenSettings>(ResourcePath);
            if (cached == null)
                cached = CreateInstance<LoadingScreenSettings>();
            return cached;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cached = null;
}

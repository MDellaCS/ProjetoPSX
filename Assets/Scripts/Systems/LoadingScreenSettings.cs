using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Configuracao global da tela de loading (tempos de espera e fade).</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Systems/Loading Screen Settings",
    fileName = "LoadingScreenSettings")]
public sealed class LoadingScreenSettings : ScriptableObject
{
    public const string ResourcePath = "LoadingScreenSettings";

    [Tooltip("Tempo fixo, em segundos reais, que a tela de loading continua totalmente visivel DEPOIS que o carregamento chegou a 100%, antes de comecar a esmaecer. Nao depende da duracao do carregamento.")]
    [Min(0f)] public float extraWaitAfterReady = .5f;
    [Tooltip("Tempo minimo, em segundos reais, que a tela permanece visivel desde que e exibida. Zero desativa este limite.")]
    [Min(0f)] public float minimumVisibleDuration = .5f;
    [Tooltip("Duracao, em segundos reais, do fade-in. O carregamento da cena so comeca depois que este fade termina.")]
    [Min(0f)] public float fadeInDuration = .2f;
    [Tooltip("Curva de progresso do fade-in. X e o tempo normalizado; Y controla a opacidade entre transparente e visivel.")]
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [FormerlySerializedAs("fadeDuration")]
    [Tooltip("Duracao, em segundos reais, do fade-out iniciado depois que a cena estiver pronta.")]
    [Min(0f)] public float fadeOutDuration = .2f;
    [Tooltip("Curva de progresso do fade-out. X e o tempo normalizado; Y controla a opacidade entre visivel e transparente.")]
    public AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

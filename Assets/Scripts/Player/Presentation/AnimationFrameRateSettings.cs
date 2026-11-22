using UnityEngine;

/// <summary>Configuracao compartilhada das malhas com poses em intervalos.</summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class AnimationFrameRateSettings : MonoBehaviour
{
    [Tooltip("Numero de atualizacoes visuais por segundo das animacoes, do gore e das particulas de combate. Nao limita o jogo nem altera a simulacao.")]
    [Min(1)] [SerializeField] private int framesPerSecond = 12;

    private static AnimationFrameRateSettings instance;

    public static int FramesPerSecond => instance != null
        ? Mathf.Max(1, instance.framesPerSecond)
        : 12;

    public static long CurrentFrame => (long)System.Math.Floor(
        Time.timeAsDouble * FramesPerSecond);

    private void OnEnable() => instance = this;

    private void OnDisable()
    {
        if (instance == this)
            instance = null;
    }

    private void OnValidate() => framesPerSecond = Mathf.Max(1, framesPerSecond);
}

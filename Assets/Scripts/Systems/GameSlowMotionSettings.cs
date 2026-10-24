using UnityEngine;

/// <summary>Configuracao global e reutilizavel dos efeitos de camera lenta.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Systems/Slow Motion Settings",
    fileName = "GameSlowMotionSettings")]
public sealed class GameSlowMotionSettings : ScriptableObject
{
    [Tooltip("Duracao, em segundos reais, da camera lenta padrao usada ao concluir encontros e por outros eventos de combate.")]
    [Min(0f)] public float duration = .75f;
    [Tooltip("Menor escala de tempo permitida pela curva de intensidade. Impede que um valor 0 pause o jogo permanentemente.")]
    [Range(.001f, 1f)] public float minimumTimeScale = .05f;
    [Tooltip("Controla a velocidade com que o efeito percorre a curva de intensidade. X e o tempo real normalizado; Y e a posicao na curva de intensidade.")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Controla a intensidade da camera lenta. X e o progresso recebido da curva de velocidade; Y e a escala de tempo aplicada ao jogo.")]
    public AnimationCurve intensityCurve = new(
        new Keyframe(0f, .05f), new Keyframe(.08f, .55f),
        new Keyframe(.2f, 1f), new Keyframe(1f, 1f));
}

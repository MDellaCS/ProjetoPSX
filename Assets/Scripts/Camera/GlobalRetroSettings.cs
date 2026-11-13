using UnityEngine;

public enum GlobalRetroResolutionMode
{
    Aparente,
    Interna
}

[CreateAssetMenu(menuName = "Projeto PSX/Camera/Global Retro Settings", fileName = "GlobalRetroSettings")]
public sealed class GlobalRetroSettings : ScriptableObject
{
    [Tooltip("Ativa o efeito global durante o jogo.")]
    public bool ativo = true;
    [Tooltip("Aparente pixeliza a imagem final. Interna reduz a resolucao de renderizacao do URP.")]
    public GlobalRetroResolutionMode resolucao = GlobalRetroResolutionMode.Aparente;
    [Tooltip("Altura desejada em pixels; a largura acompanha a proporcao da tela. No modo interno, o URP limita a escala minima a 10% da resolucao da tela.")]
    [Min(1)] public int altura = 240;
    [Tooltip("Quantidade de tons por canal RGB. 32 corresponde a 5 bits por canal, como a cor de 15 bits do PS1.")]
    [Range(2, 32)] public int niveisCor = 32;
    [Tooltip("Intensidade do dithering ordenado, que suaviza as faixas da reducao de cor.")]
    [Range(0f, 1f)] public float dithering = 0.65f;
    [Tooltip("Mistura da reducao de cores com a imagem original; zero desliga a reducao de cores.")]
    [Range(0f, 1f)] public float intensidadeCor = 1f;
    [Tooltip("Mistura da pixelizacao com a imagem original no modo Aparente.")]
    [Range(0f, 1f)] public float intensidadePixel = 1f;
}

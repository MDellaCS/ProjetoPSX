using UnityEngine;

[CreateAssetMenu(menuName = "Projeto PSX/Weather/Rain Profile", fileName = "RainProfile")]
public sealed class RainProfile : ScriptableObject
{
    [Tooltip("Liga a chuva enquanto este perfil de ambiente estiver ativo.")]
    public bool enabled = true;
    [Tooltip("Quantidade de gotas emitidas por segundo. Valores altos aumentam o custo de renderizacao.")]
    [Min(0f)] public float emissionRate = 350f;
    [Tooltip("Limite de gotas simultaneas para controlar o custo da chuva.")]
    [Range(1, 10000)] public int maxParticles = 1200;
    [Tooltip("Tempo em segundos para aumentar ou reduzir a emissao ao mudar de ambiente.")]
    [Min(0f)] public float transitionTime = 2f;
    [Tooltip("Largura, altura e profundidade do volume de emissao ao redor da camera, em metros.")]
    public Vector3 volumeSize = new(18f, 12f, 18f);
    [Tooltip("Deslocamento vertical do volume em relacao a camera, em metros.")]
    public float heightOffset = 1f;
    [Tooltip("Distancia maxima em metros para antecipar o volume na direcao do movimento da camera.")]
    [Min(0f)] public float movementLead = 2f;
    [Tooltip("Tempo de vida minimo e maximo das gotas, em segundos.")]
    public Vector2 lifetime = new(.5f, 1.1f);
    [Tooltip("Velocidade minima e maxima de queda das gotas, em metros por segundo.")]
    public Vector2 speed = new(12f, 18f);
    [Tooltip("Inclinacao da queda em graus a partir da vertical. Zero significa chuva vertical.")]
    [Range(0f, 80f)] public float angle = 8f;
    [Tooltip("Direcao horizontal para onde a chuva se desloca, em graus no mundo; zero aponta para o eixo Z positivo.")]
    [Range(0f, 360f)] public float direction;
    [Tooltip("Largura minima e maxima de cada gota em metros.")]
    public Vector2 dropWidth = new(.012f, .025f);
    [Tooltip("Comprimento do rastro como multiplicador da largura da gota.")]
    [Min(.01f)] public float lengthScale = 12f;
    [Tooltip("Aumento do comprimento do rastro em funcao da velocidade da gota.")]
    [Min(0f)] public float velocityStretch = .015f;
    [Tooltip("Cor e transparencia das gotas. Menor alfa deixa a chuva mais discreta.")]
    public Color color = new(.72f, .8f, .88f, .38f);
    [Tooltip("Faz os rastros manterem largura visivel quando vistos de frente, inclusive com a camera inclinada.")]
    public bool freeformStretching = true;

    private void OnValidate()
    {
        volumeSize = new Vector3(Mathf.Max(.1f, volumeSize.x), Mathf.Max(.1f, volumeSize.y), Mathf.Max(.1f, volumeSize.z));
        lifetime = Ordered(lifetime, .01f);
        speed = Ordered(speed, .01f);
        dropWidth = Ordered(dropWidth, .001f);
        maxParticles = Mathf.Clamp(maxParticles, 1, 10000);
        CameraRain.ProfileChanged();
    }

    private static Vector2 Ordered(Vector2 range, float min)
    {
        range.x = Mathf.Max(min, range.x);
        range.y = Mathf.Max(range.x, range.y);
        return range;
    }
}

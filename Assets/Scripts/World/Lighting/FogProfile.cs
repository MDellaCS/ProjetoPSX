using UnityEngine;

public enum FogDistanceOrigin { Camera, Player }

[CreateAssetMenu(menuName = "Projeto PSX/Lighting/Fog Profile", fileName = "FogProfile")]
public sealed class FogProfile : ScriptableObject
{
    [Header("Transicao")]
    [Tooltip("Tempo em segundos para misturar os valores ao trocar de ambiente.")]
    [Min(0f)] public float transitionTime = 1f;
    [Tooltip("Define se a distancia para cor, desfoque e nevoa volumetrica e medida a partir da camera ou do jogador; sem jogador, usa a camera.")]
    public FogDistanceOrigin distanceOrigin;
    [Header("Cor por distancia")]
    [Tooltip("Ativa a coloracao gradual do cenario distante.")]
    public bool colorEnabled = true;
    [Tooltip("Cor aplicada ao cenario distante.")]
    public Color color = new(.46f, .52f, .55f, 1f);
    [Tooltip("Distancia da origem escolhida onde a transicao da cor comeca, em metros.")]
    [Min(0f)] public float colorStart = 12f;
    [Tooltip("Distancia da origem escolhida onde a transicao da cor termina, em metros.")]
    [Min(.01f)] public float colorEnd = 65f;
    [Tooltip("Intensidade da cor no inicio da faixa de distancia.")]
    [Range(0, 1)] public float colorMin;
    [Tooltip("Intensidade da cor no final da faixa de distancia.")]
    [Range(0, 1)] public float colorMax = .45f;
    [Tooltip("Formato da transicao de cor: X e a distancia normalizada; Y e a intensidade entre minimo e maximo.")]
    public AnimationCurve colorCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Header("Desfoque por distancia")]
    [Tooltip("Ativa o desfoque progressivo dos objetos distantes.")]
    public bool blurEnabled = true;
    [Tooltip("Distancia da origem escolhida onde a transicao do desfoque comeca, em metros.")]
    [Min(0f)] public float blurStart = 25f;
    [Tooltip("Distancia da origem escolhida onde a transicao do desfoque termina, em metros.")]
    [Min(.01f)] public float blurEnd = 75f;
    [Tooltip("Intensidade do desfoque no inicio da faixa de distancia.")]
    [Range(0, 1)] public float blurMin;
    [Tooltip("Intensidade do desfoque no final da faixa de distancia.")]
    [Range(0, 1)] public float blurMax = .3f;
    [Tooltip("Raio maximo do desfoque em pixels. Valores altos custam o mesmo, mas podem criar halos.")]
    [Range(0, 8)] public float blurRadius = 2f;
    [Tooltip("Formato da transicao de desfoque: X e a distancia normalizada; Y e a intensidade entre minimo e maximo.")]
    public AnimationCurve blurCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Header("Nevoa volumetrica")]
    [Tooltip("Ativa a camada volumetrica animada; aumenta o custo de GPU conforme o numero de amostras.")]
    public bool volumetricEnabled;
    [Tooltip("Cor da camada volumetrica em movimento.")]
    public Color volumetricColor = new(.46f, .51f, .54f, 1f);
    [Tooltip("Densidade da nevoa volumetrica por metro.")]
    [Min(0)] public float density = .025f;
    [Tooltip("Opacidade maxima da camada volumetrica.")]
    [Range(0, 1)] public float volumetricOpacity = .5f;
    [Tooltip("Distancia da origem escolhida onde a intensidade volumetrica comeca a mudar, em metros.")]
    [Min(0f)] public float volumeStart = 8f;
    [Tooltip("Distancia da origem escolhida onde a intensidade volumetrica atinge o valor final, em metros.")]
    [Min(.01f)] public float volumeEnd = 60f;
    [Tooltip("Intensidade volumetrica na distancia inicial.")]
    [Range(0f, 1f)] public float volumeMin;
    [Tooltip("Intensidade volumetrica na distancia final.")]
    [Range(0f, 1f)] public float volumeMax = 1f;
    [Tooltip("Curva da intensidade volumetrica entre as distancias inicial e final.")]
    public AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Limita a nevoa entre alturas mundiais especificas. Desativado preenche todas as alturas.")]
    public bool limitVolumeHeight;
    [Tooltip("Altura mundial da base da camada volumetrica, quando o limite de altura esta ativo, em metros.")]
    public float baseHeight;
    [Tooltip("Altura da camada acima da base quando o limite de altura esta ativo, em metros.")]
    [Min(.01f)] public float height = 5f;
    [Tooltip("Distancia maxima acumulada pela nevoa volumetrica, em metros.")]
    [Min(1)] public float maxDistance = 40f;
    [Tooltip("Quantidade de amostras do volume por pixel; valores maiores aumentam o custo de GPU.")]
    [Range(2, 16)] public int steps = 6;
    [Tooltip("Escala do ruido tridimensional; valores maiores criam massas menores.")]
    [Min(.001f)] public float noiseScale = .12f;
    [Tooltip("Variacao da densidade causada pelo ruido; zero produz nevoa uniforme.")]
    [Range(0, 1)] public float noiseStrength = .5f;
    [Tooltip("Deslocamento da nevoa no mundo por segundo, em metros.")]
    public Vector3 wind = new(.15f, 0, .05f);

    private void OnValidate()
    {
        colorEnd = Mathf.Max(colorStart + .01f, colorEnd);
        blurEnd = Mathf.Max(blurStart + .01f, blurEnd);
        volumeStart = Mathf.Max(0f, volumeStart);
        volumeEnd = Mathf.Max(volumeStart + .01f, volumeEnd);
        height = Mathf.Max(.01f, height);
        maxDistance = Mathf.Max(1f, maxDistance);
        steps = Mathf.Clamp(steps, 2, 16);
        FogRuntime.Invalidate();
    }
}

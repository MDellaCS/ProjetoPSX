using System;
using System.Collections.Generic;
using UnityEngine;

public enum LocalLightShape
{
    Point,
    Spot
}

public enum LocalLightFlickerMode
{
    [Tooltip("Oscilacao suave e organica, como chama de vela ou tocha.")]
    Smooth,
    [Tooltip("Saltos aleatorios entre valores, como fogo agitado ou lampada instavel.")]
    Jitter,
    [Tooltip("Pulsacao regular em seno, como alarme ou sinal.")]
    Pulse,
    [Tooltip("Fica no maximo e cai para o minimo em falhas curtas, como lampada fluorescente com defeito.")]
    Failing
}

[Serializable]
public sealed class LocalLightFlicker
{
    [Tooltip("Liga a oscilacao. Enquanto ligada, a intensidade varia entre Intensidade Minima e Maxima e ignora a Intensidade base.")]
    public bool enabled;
    [Tooltip("Formato da oscilacao: Smooth (chama), Jitter (saltos), Pulse (seno) ou Failing (lampada com defeito).")]
    public LocalLightFlickerMode mode = LocalLightFlickerMode.Smooth;
    [Tooltip("Menor intensidade atingida pela oscilacao, na mesma escala da Intensidade. Pode ser zero para a luz apagar por completo.")]
    [Min(0f)] public float minimumIntensity = .6f;
    [Tooltip("Maior intensidade atingida pela oscilacao, na mesma escala da Intensidade.")]
    [Min(0f)] public float maximumIntensity = 1.2f;
    [Tooltip("Rapidez da oscilacao, em ciclos por segundo aproximados. Em Failing, e a chance de falha por segundo.")]
    [Min(0f)] public float speed = 6f;
    [Tooltip("Quantas vezes por segundo a intensidade e recalculada. Zero atualiza todo frame; 10 a 15 da a cadencia picotada de jogos de PS1/PS2.")]
    [Min(0f)] public float updatesPerSecond = 12f;
    [Tooltip("Quanto o alcance acompanha a oscilacao. 0 mantem o alcance fixo; 0,3 encolhe o alcance ate 30% quando a luz esta no minimo, como uma chama.")]
    [Range(0f, 1f)] public float rangeVariation;
    [Tooltip("Somente Failing: duracao media de cada falha, em segundos.")]
    [Min(.01f)] public float failureDuration = .12f;

    /// <summary>Retorna 0..1 (0 = minimo, 1 = maximo) para o tempo e a fase informados.</summary>
    public float Evaluate(float time, float phase)
    {
        if (updatesPerSecond > 0f)
            time = Mathf.Floor(time * updatesPerSecond) / updatesPerSecond;

        switch (mode)
        {
            case LocalLightFlickerMode.Jitter:
            {
                float step = Mathf.Floor(time * Mathf.Max(.01f, speed) + phase);
                return Hash01(step + phase * 13.1f);
            }
            case LocalLightFlickerMode.Pulse:
                return .5f + .5f * Mathf.Sin((time * speed + phase) * Mathf.PI * 2f);
            case LocalLightFlickerMode.Failing:
            {
                // Janelas do tamanho de uma falha; cada janela sorteia se falha.
                float window = Mathf.Max(.01f, failureDuration);
                float slot = Mathf.Floor(time / window + phase);
                float chancePerWindow = Mathf.Clamp01(speed * window);
                return Hash01(slot * 1.37f + phase) < chancePerWindow
                    ? Hash01(slot * 7.91f) * .25f
                    : 1f;
            }
            default:
            {
                // Duas oitavas de Perlin, esticadas para usar toda a faixa min-max.
                float a = Mathf.PerlinNoise(time * speed + phase, phase * .5f);
                float b = Mathf.PerlinNoise(time * speed * 2.3f + phase, phase + 7f);
                float n = (a * .7f + b * .3f - .5f) * 1.8f + .5f;
                return Mathf.Clamp01(n);
            }
        }
    }

    private static float Hash01(float value)
    {
        float s = Mathf.Sin(value * 12.9898f + 78.233f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    public void Validate()
    {
        minimumIntensity = Mathf.Max(0f, minimumIntensity);
        maximumIntensity = Mathf.Max(minimumIntensity, maximumIntensity);
    }
}

[Serializable]
public sealed class LocalLightDefinition
{
    [Tooltip("Liga ou desliga esta luz individualmente.")]
    public bool enabled = true;
    [Tooltip("Point ilumina ao redor; Spot ilumina em cone na direcao do eixo local Z (lanterna).")]
    public LocalLightShape shape = LocalLightShape.Point;
    [Tooltip("Posicao local da luz em relacao ao objeto emissor, em metros.")]
    public Vector3 localPosition = new(0f, 1.2f, 0f);
    [Tooltip("Rotacao local em graus. Em Spot, o cone aponta para o eixo Z depois desta rotacao.")]
    public Vector3 localEulerAngles;
    [Tooltip("Cor da luz emitida.")]
    public Color color = new(1f, .82f, .62f);
    [Tooltip("Brilho da luz. 1 aplica a cor cheia sobre a superficie no centro da luz; 2 dobra. A escala e linear e nao depende da distancia.")]
    [Min(0f)] public float intensity = 1f;
    [Tooltip("Alcance da luz, em metros. A luz chega exatamente ate aqui, sem queda inverso-quadrado: 40 m iluminam ate 40 m.")]
    [Min(.01f)] public float range = 8f;
    [Tooltip("Fracao do alcance com brilho cheio antes de comecar a queda. 0 comeca a cair na fonte; 0,5 mantem brilho total ate metade do alcance.")]
    [Range(0f, .95f)] public float coreRadius = .1f;
    [Tooltip("Formato da queda. 1 e linear (PS1); 2 concentra a luz perto da fonte; valores menores que 1 empurram o brilho para longe.")]
    [Range(.25f, 4f)] public float falloffExponent = 1.4f;
    [Tooltip("Quantidade de faixas de intensidade por distancia. 0 deixa a queda continua; valores altos criam mais degraus na luz.")]
    [Range(0, 64)] public int bands;
    [Tooltip("Lados dos contornos das faixas no chao. Valores baixos deixam a luz facetada, em estilo low-poly. O minimo e 3.")]
    [Range(3, 32)] public int bandSegments = 8;
    [Tooltip("Quanto a luz contorna as superficies que nao estao de frente para ela. 0 e Lambert puro (lados escuros); 1 ilumina quase por igual, como luz ambiente local.")]
    [Range(0f, 1f)] public float wrap = .2f;
    [Tooltip("Abertura externa do cone Spot, em graus.")]
    [Range(1f, 179f)] public float spotAngle = 45f;
    [Tooltip("Abertura interna do cone Spot, em graus. Entre o angulo interno e o externo a luz se apaga; valores proximos criam borda dura.")]
    [Range(0f, 179f)] public float innerSpotAngle = 30f;
    [Tooltip("Oscilacao opcional da intensidade entre um minimo e um maximo.")]
    public LocalLightFlicker flicker = new();

    public void Validate()
    {
        intensity = Mathf.Max(0f, intensity);
        range = Mathf.Max(.01f, range);
        bands = Mathf.Clamp(bands, 0, 64);
        bandSegments = Mathf.Clamp(bandSegments, 3, 32);
        innerSpotAngle = Mathf.Clamp(innerSpotAngle, 0f, spotAngle);
        flicker ??= new LocalLightFlicker();
        flicker.Validate();
    }
}

[CreateAssetMenu(menuName = "Projeto PSX/World/Local Light Profile", fileName = "LocalLightProfile")]
public sealed class LocalLightProfile : ScriptableObject
{
    [Tooltip("Luzes criadas por cada emissor com este perfil. Cada uma tem posicao, direcao, queda e oscilacao proprias.")]
    public List<LocalLightDefinition> lights = new() { new LocalLightDefinition() };

    // Permite atualizar emissores quando o perfil e editado no Inspector.
    [NonSerialized] private int revision;
    public int Revision => revision;

    private void OnValidate()
    {
        if (lights != null)
            for (int i = 0; i < lights.Count; i++)
                lights[i]?.Validate();
        revision++;
    }
}

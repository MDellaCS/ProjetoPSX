using UnityEngine;

/// <summary>Como uma entidade esta em relacao a um volume de agua.</summary>
public enum WaterZone
{
    /// <summary>Fora da agua (ou imune a ela).</summary>
    None,
    /// <summary>Com os pes na agua e o chao por perto: anda normalmente, com limitacoes.</summary>
    Shallow,
    /// <summary>Sem chao por perto: flutua na superficie e nada.</summary>
    Deep
}

/// <summary>
/// Regras de gameplay de um volume de agua. Um mesmo asset pode ser usado por
/// varios volumes (lago, pantano, esgoto...) e vale para o jogador e para os
/// inimigos.
/// </summary>
[CreateAssetMenu(menuName = "Projeto PSX/World/Water Profile", fileName = "WaterProfile")]
public sealed class WaterProfile : ScriptableObject
{
    [Header("Classificacao")]
    [Tooltip("Profundidade minima, em metros, dos pes abaixo da superficie para a entidade contar como 'na agua'. Evita ativar a agua ao apenas tocar a superficie.")]
    [Min(0f)] public float minimumContactDepth = .15f;
    [Tooltip("Folga, em metros, para continuar 'na agua' ao se afastar da borda. Evita que o contato pisque na margem.")]
    [Min(0f)] public float contactHysteresis = .05f;
    [Tooltip("Profundidade da coluna de agua (da superficie ate o chao abaixo da entidade) a partir da qual a agua e FUNDA. Abaixo disso e rasa.")]
    [Min(.1f)] public float deepWaterMinDepth = 1.4f;
    [Tooltip("Folga, em metros, para sair da agua funda. Evita alternar entre rasa e funda numa borda.")]
    [Min(0f)] public float deepExitHysteresis = .15f;

    [Header("Agua rasa")]
    [Tooltip("Lentidao base de movimento na agua rasa: multiplica a velocidade do jogador e dos inimigos. 1 = igual ao chao.")]
    [Min(0f)] public float shallowSpeedMultiplier = .8f;
    [Tooltip("Multiplica o IMPULSO do dash do jogador e a distancia da esquiva dos inimigos na agua rasa. A distancia real do dash do jogador cai bem mais que o valor, pois depende do impulso ao quadrado. Ex.: 0.75 resulta em cerca de 0.4x.")]
    [Min(0f)] public float shallowDashDistanceMultiplier = .75f;
    [Tooltip("Multiplica o deslocamento dos ataques na agua rasa (avanco dos golpes).")]
    [Min(0f)] public float shallowAttackMovementMultiplier = .6f;

    [Header("Inercia (sensacao de nadar)")]
    [Tooltip("Aceleracao maxima, em m/s2, ao comecar a andar na agua rasa. 0 = igual ao chao (sem efeito). Valores menores dao a sensacao de arrasto.")]
    [Min(0f)] public float shallowAcceleration = 45f;
    [Tooltip("Desaceleracao maxima, em m/s2, ao soltar o movimento na agua rasa. 0 = igual ao chao.")]
    [Min(0f)] public float shallowDeceleration = 35f;
    [Tooltip("Aceleracao maxima, em m/s2, ao nadar na agua funda. 0 = igual ao chao. Valores menores deixam o corpo mais 'pesado' para arrancar.")]
    [Min(0f)] public float deepAcceleration = 14f;
    [Tooltip("Desaceleracao maxima, em m/s2, ao soltar o movimento na agua funda. Valores menores fazem o corpo deslizar mais antes de parar.")]
    [Min(0f)] public float deepDeceleration = 8f;
    [Tooltip("Ao pular da agua funda, soma a profundidade de flutuacao a altura do pulo. Assim o apice fica na mesma altura, em relacao a SUPERFICIE, de um pulo saindo do chao ao nivel dela; sem isso o corpo, que flutua abaixo da superficie, saltaria menos em relacao as margens.")]
    public bool deepJumpCompensatesDepth = true;
    [Tooltip("Velocidade horizontal extra, em m/s, ao pular para fora da agua funda na direcao do movimento. Ajuda a alcancar margens. 0 desliga. Nao altera a altura do pulo.")]
    [Min(0f)] public float deepJumpExitBoost = 2.5f;

    [Header("Agua funda (flutuando)")]
    [Tooltip("Lentidao base de movimento na agua funda: multiplica a velocidade de natacao do jogador e dos inimigos. Independente da agua rasa.")]
    [Min(0f)] public float deepSpeedMultiplier = .6f;
    [Tooltip("Quantos metros os pes ficam abaixo da superficie ao flutuar. Deve ser menor que 'Deep Water Min Depth' para os pes nunca tocarem o chao.")]
    [Min(0f)] public float floatFeetDepth = 1.15f;
    [Tooltip("Rapidez com que o corpo sobe/desce ate a altura de flutuacao (1/segundo). Valores altos prendem o corpo na superficie; baixos deixam mais mole.")]
    [Min(.1f)] public float floatSpringFrequency = 6f;
    [Tooltip("Velocidade vertical maxima, em m/s, do movimento de flutuacao.")]
    [Min(.1f)] public float floatMaxVerticalSpeed = 6f;
    [Tooltip("Liga o bobbing (sobe e desce suave) de quem flutua, igual ao do inimigo voador. Vale para o jogador e para todos os inimigos que nadam.")]
    public bool floatBobbingEnabled = true;
    [Tooltip("Multiplica a amplitude do bobbing na agua. 1 = a mesma do inimigo voador; 0 desliga o movimento sem desligar a opcao.")]
    [Min(0f)] public float floatBobbingScale = 1f;
    [Tooltip("Permite dash (jogador) e esquiva (inimigos) na agua funda.")]
    public bool deepAllowsDash;
    [Tooltip("Permite atacar (jogador e inimigos) na agua funda.")]
    public bool deepAllowsAttacks;

    [Header("Heat do jogador")]
    [Tooltip("Na agua rasa, o jogador conta como fora de combate para o resfriamento passivo do Heat.")]
    public bool coolsHeatInShallowWater = true;
    [Tooltip("Na agua funda, o jogador conta como fora de combate para o resfriamento passivo do Heat.")]
    public bool coolsHeatInDeepWater = true;

    private static WaterProfile fallback;

    /// <summary>Perfil padrao usado por volumes sem perfil atribuido.</summary>
    public static WaterProfile Fallback
    {
        get
        {
            if (fallback == null)
            {
                fallback = CreateInstance<WaterProfile>();
                fallback.name = "Default Water Profile (runtime)";
                fallback.hideFlags = HideFlags.HideAndDontSave;
            }
            return fallback;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (fallback != null)
            DestroyImmediate(fallback);
        fallback = null;
    }

    public float SpeedMultiplier(WaterZone zone) => zone switch
    {
        WaterZone.Shallow => shallowSpeedMultiplier,
        WaterZone.Deep => deepSpeedMultiplier,
        _ => 1f
    };

    public float DashDistanceMultiplier(WaterZone zone) => zone switch
    {
        WaterZone.Shallow => shallowDashDistanceMultiplier,
        WaterZone.Deep => deepAllowsDash ? shallowDashDistanceMultiplier : 0f,
        _ => 1f
    };

    public float AttackMovementMultiplier(WaterZone zone) => zone switch
    {
        WaterZone.Shallow => shallowAttackMovementMultiplier,
        WaterZone.Deep => deepAllowsAttacks ? shallowAttackMovementMultiplier : 0f,
        _ => 1f
    };

    /// <summary>Aceleracao maxima na agua (m/s2); 0 = sem limite proprio.</summary>
    public float Acceleration(WaterZone zone) => zone switch
    {
        WaterZone.Shallow => shallowAcceleration,
        WaterZone.Deep => deepAcceleration,
        _ => 0f
    };

    /// <summary>Desaceleracao maxima na agua (m/s2); 0 = sem limite proprio.</summary>
    public float Deceleration(WaterZone zone) => zone switch
    {
        WaterZone.Shallow => shallowDeceleration,
        WaterZone.Deep => deepDeceleration,
        _ => 0f
    };

    public bool AllowsDash(WaterZone zone) =>
        zone != WaterZone.Deep || deepAllowsDash;

    public bool AllowsAttacks(WaterZone zone) =>
        zone != WaterZone.Deep || deepAllowsAttacks;

    public bool CoolsHeat(WaterZone zone) => zone switch
    {
        WaterZone.Shallow => coolsHeatInShallowWater,
        WaterZone.Deep => coolsHeatInDeepWater,
        _ => false
    };

    private void OnValidate()
    {
        deepWaterMinDepth = Mathf.Max(.1f, deepWaterMinDepth);
        // Os pes precisam ficar abaixo do contato minimo + a amplitude do
        // bobbing (senao a zona piscaria Funda/Nenhuma a cada oscilacao)...
        floatFeetDepth = Mathf.Max(floatFeetDepth,
            minimumContactDepth + contactHysteresis + .4f * Mathf.Max(0f, floatBobbingScale));
        // ...e acima do chao ao flutuar.
        floatFeetDepth = Mathf.Min(floatFeetDepth,
            Mathf.Max(0f, deepWaterMinDepth - .1f));
    }
}

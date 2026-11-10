using UnityEngine;

public enum HandLadderPrefabAxis
{
    X,
    Y,
    Z
}

/// <summary>
/// Tipo de escada: visual (prefabs e medidas dos degraus) e comportamento
/// (velocidades, encaixe, salto e saidas). A <see cref="HandLadder"/>
/// guarda so o que e da instancia (tamanho, posicao, fixacao, zonas e saidas
/// do local); tudo que define COMO a escada se comporta fica aqui e vale para
/// todas as escadas que usam o perfil. Um perfil sem prefabs serve so de
/// comportamento, para escadas com malha propria.
/// </summary>
[CreateAssetMenu(menuName = "Projeto PSX/Level Design/Hand Ladder Profile", fileName = "HandLadderProfile")]
public sealed class HandLadderProfile : ScriptableObject
{
    private static HandLadderProfile defaultProfile;

    /// <summary>Comportamento padrao das escadas sem perfil (sem visual, subida continua).</summary>
    public static HandLadderProfile Default
    {
        get
        {
            if (defaultProfile == null)
            {
                defaultProfile = CreateInstance<HandLadderProfile>();
                defaultProfile.hideFlags = HideFlags.HideAndDontSave;
            }

            return defaultProfile;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => defaultProfile = null;

    [Header("Visual")]
    [Tooltip("Prefab visual repetido em cada degrau. Pode incluir colisor visual; o gerador desativa colliders filhos para a escada nao criar bloqueios redundantes.")]
    public GameObject rungPrefab;
    [Tooltip("Eixo local do prefab que atravessa a largura do degrau. O gerador alinha este eixo com a largura da escada e ajusta o tamanho pela malha real.")]
    public HandLadderPrefabAxis rungWidthAxis = HandLadderPrefabAxis.X;
    [Tooltip("Giro, em graus, em torno do eixo de largura do degrau. Use para orientar a face visual sem mudar a direcao da escada.")]
    public float rungRollDegrees;
    [Tooltip("Espessura do degrau, em metros, nos dois eixos perpendiculares ao comprimento. 0 mantem a espessura original do prefab.")]
    [Min(0f)] public float rungThickness = .06f;
    [Tooltip("Prefab opcional para os dois trilhos laterais. O eixo configurado abaixo e alinhado com a subida; deixe vazio para gerar somente os degraus.")]
    public GameObject railPrefab;
    [Tooltip("Eixo local do prefab que percorre o comprimento dos trilhos. Este eixo e alinhado com a direcao de subida.")]
    public HandLadderPrefabAxis railLengthAxis = HandLadderPrefabAxis.Y;
    [Tooltip("Giro, em graus, em torno do comprimento dos trilhos.")]
    public float railRollDegrees;
    [Tooltip("Espessura dos trilhos, em metros, nos dois eixos perpendiculares ao comprimento (secao quadrada ou redonda).")]
    [Min(.01f)] public float railThickness = .08f;

    [Header("Medidas")]
    [Tooltip("Distancia entre o centro de dois degraus consecutivos, em metros. O jogador sobe de espaco em espaco entre os degraus.")]
    [Min(.05f)] public float rungSpacing = .32f;
    [Tooltip("Largura inicial da escada em metros. Cada instancia pode sobrescrever este valor.")]
    [Min(.1f)] public float defaultWidth = 1.2f;

    [Header("Escalada")]
    [Tooltip("Velocidade ao longo do eixo de subida, em metros por segundo.")]
    [Min(.1f)] public float climbSpeed = 2.2f;
    [Tooltip("Onde o jogador para entre dois degraus: 0 = na altura de um degrau, 0,5 = no meio do vao, 1 = no degrau de cima. Ajuste ate os pes/maos do personagem baterem com os degraus.")]
    [Range(0f, 1f)] public float stepPhase = .5f;
    [Tooltip("Velocidade lateral, em m/s, em escadas largas (muros de escalada). 0 = so sobe e desce.")]
    [Min(0f)] public float lateralClimbSpeed;
    [Tooltip("Distancia do centro do jogador ao plano da escada, em metros.")]
    public float playerForwardOffset = .38f;
    [Tooltip("Distancia, em metros, entre o topo da escada e onde o jogador para ao subir. 0,9 deixa o topo da escada perto do meio do corpo, como nos jogos; depois disso a saida pre-feita leva o jogador ate o topo.")]
    [Min(0f)] public float topGripClearance = .9f;
    [Tooltip("Velocidade, em m/s, do movimento pre-feito que encaixa o jogador na escada. Vale tambem para quem agarra no ar ou em alta velocidade: o encaixe parte de onde ele estiver.")]
    [Min(1f)] public float mountSpeed = 12f;
    [Tooltip("Duracao minima, em segundos, do movimento de encaixe na escada.")]
    [Min(.05f)] public float mountMinDuration = .15f;

    [Header("Saltar e soltar")]
    [Tooltip("Velocidade horizontal, em m/s, ao pular da escada. A direcao e a normal da escada (para onde ela aponta).")]
    [Min(0f)] public float jumpOffSpeed = 7f;
    [Tooltip("Multiplica a altura do pulo ao saltar da escada.")]
    [Min(0f)] public float jumpOffHeightMultiplier = 1f;
    [Tooltip("Velocidade horizontal, em m/s, ao soltar com Agachar. Afasta o jogador da escada para ele cair livre.")]
    [Min(0f)] public float dropAwaySpeed = 1.5f;

    [Header("Saida pre-feita")]
    [Tooltip("Distancia horizontal, em metros, da saida a partir do plano da escada.")]
    [Min(.1f)] public float exitDistance = .55f;
    [Tooltip("Subida extra da saida do topo, em metros, antes de procurar o chao.")]
    [Min(0f)] public float topExitRise = .05f;
    [Tooltip("Duracao, em segundos, do movimento pre-feito que leva o jogador da escada ate o ponto de saida (topo ou base).")]
    [Min(.1f)] public float exitDuration = .5f;
}

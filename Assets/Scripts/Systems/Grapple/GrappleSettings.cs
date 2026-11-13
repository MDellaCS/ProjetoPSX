using UnityEngine;

/// <summary>Gancho de combate (ataque de grapple em inimigo): alcance, assistencia de mira e velocidades.</summary>
[System.Serializable]
public sealed class CombatGrappleSettings
{
    [Header("Alcance e assistencia de mira")]
    [Tooltip("Alcance horizontal do cone frontal de assistencia do grapple, em metros, medido a partir do centro do jogador.")]
    [Min(0f)] public float range = 20f;
    [Tooltip("Abertura do cone frontal em graus para cada lado da direcao do jogador.")]
    [Range(0f, 180f)] public float assistAngle = 20f;
    [Tooltip("Raio horizontal, em metros, da area circular ao redor do jogador que tambem seleciona inimigos para o grapple.")]
    [Min(0f)] public float assistCircleRadius = 3f;
    [Tooltip("Altura total, em metros, da area de assistencia do grapple, dividida igualmente acima e abaixo do centro do jogador.")]
    [Min(0f)] public float assistHeight = 8f;
    [HideInInspector] public float assistNearAngle = 35f;

    [Header("Puxao")]
    [Tooltip("Distancia minima entre as superficies do jogador e do inimigo ao terminar o puxao, em metros.")]
    [Min(0f)] public float pullStopDistance = 1f;
    [Tooltip("Velocidade, em m/s, do puxao de um inimigo mais leve ate o jogador.")]
    [Min(0f)] public float lighterEnemyPullSpeed = 50f;
    [Tooltip("Velocidade, em m/s, do puxao do jogador ate um inimigo mais pesado.")]
    [Min(0f)] public float heavierEnemyPullSpeed = 30f;
    [Tooltip("Tempo, em segundos, que o inimigo fica em stagger depois do puxao.")]
    [Min(0f)] public float postPullStaggerDuration = .05f;
}

/// <summary>Gancho de movimento (swing, escadas, barras e o que mais o jogador agarrar): alcance, assistencia e velocidade.</summary>
[System.Serializable]
public sealed class MovementGrappleSettings
{
    [Header("Alcance e assistencia de mira")]
    [Tooltip("Alcance padrao, em metros, dos alvos de movimento: pontos de balanco (salvo Range Override na ancora) e degraus de escada/barras.")]
    [Min(.1f)] public float range = 14f;
    [Tooltip("Angulo maximo, em graus, entre a mira (camera) e um degrau de escada/barras para ele ser alvo.")]
    [Range(1f, 45f)] public float ladderAimAngle = 12f;
    [Tooltip("Angulo maximo, em graus, entre a mira (camera) e um ponto de balanco. 180 = sem limite (so precisa estar visivel).")]
    [Range(1f, 180f)] public float anchorAimAngle = 180f;

    [Header("Velocidade")]
    [Tooltip("Velocidade, em m/s, com que o gancho puxa o jogador ate uma escada ou barras.")]
    [Min(1f)] public float pullSpeed = 24f;
}

/// <summary>
/// Configuracao global do gancho, separada em Combat (ataque) e Movement
/// (travessia). Fica em Resources/GrappleSettings; sem o asset usa os padroes.
/// </summary>
[CreateAssetMenu(menuName = "Projeto PSX/Grapple Settings", fileName = "GrappleSettings")]
public sealed class GrappleSettings : ScriptableObject
{
    private const string ResourcePath = "GrappleSettings";
    private static GrappleSettings instance;

    [Tooltip("Gancho de combate: o ataque especial que puxa inimigos (ou puxa o jogador ate eles).")]
    [SerializeField] private CombatGrappleSettings combat = new();
    [Tooltip("Gancho de movimento: balanco, escadas, barras e outras travessias.")]
    [SerializeField] private MovementGrappleSettings movement = new();

    public static CombatGrappleSettings Combat => Instance.combat;
    public static MovementGrappleSettings Movement => Instance.movement;

    private static GrappleSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GrappleSettings>(ResourcePath);
                if (instance == null)
                {
                    instance = CreateInstance<GrappleSettings>();
                    instance.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
}

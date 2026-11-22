using System;
using UnityEngine;

[Serializable]
public sealed class DamageNumberStyle
{
    [Tooltip("Exibe os numeros de dano desta categoria quando o F3 esta aberto.")]
    public bool enabled = true;
    [Tooltip("Deslocamento inicial do numero em relacao a entidade atingida.")]
    public Vector3 spawnOffset = new(0f, 2f, 0f);
    [Tooltip("Variacao horizontal aleatoria da posicao inicial do numero.")]
    [Min(0f)] public float horizontalScatter = .35f;
    [Tooltip("Variacao vertical aleatoria da posicao inicial do numero.")]
    [Min(0f)] public float verticalScatter = .15f;
    [Tooltip("Cor do numero exibido ao receber dano.")]
    public Color color = Color.white;
    [Tooltip("Tamanho da fonte do numero na tela.")]
    [Min(1)] public int fontSize = 32;
    [Tooltip("Tempo em segundos que o numero permanece visivel.")]
    [Min(.01f)] public float lifetime = 1.35f;
    [Tooltip("Velocidade com que o numero sobe no mundo.")]
    [Min(0f)] public float riseSpeed = 1.5f;
    [Tooltip("Cor da sombra desenhada atras do numero.")]
    public Color shadowColor = Color.black;
    [Tooltip("Distancia em pixels entre o numero e sua sombra.")]
    [Min(0f)] public float shadowOffsetPixels = 2f;
}

[CreateAssetMenu(menuName = "Projeto PSX/Debug/Damage Number Settings", fileName = "DamageNumberSettings")]
public sealed class DamageNumberSettings : ScriptableObject
{
    private static DamageNumberSettings instance;

    [Tooltip("Aparencia dos numeros exibidos quando o jogador recebe dano.")]
    [SerializeField] private DamageNumberStyle player = new() { color = new Color(1f, .3f, .3f) };
    [Tooltip("Aparencia dos numeros exibidos quando inimigos ou objetos recebem dano.")]
    [SerializeField] private DamageNumberStyle enemy = new();

    public static DamageNumberSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<DamageNumberSettings>("DamageNumberSettings");
                if (instance == null)
                {
                    instance = CreateInstance<DamageNumberSettings>();
                    instance.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            return instance;
        }
    }

    public DamageNumberStyle Player => player;
    public DamageNumberStyle Enemy => enemy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
}

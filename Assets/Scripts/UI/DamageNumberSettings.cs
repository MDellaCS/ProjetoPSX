using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class DamageNumberStyle
{
    [Tooltip("Exibe os numeros de dano desta categoria quando o F3 esta aberto.")]
    public bool enabled = true;
    [Tooltip("Deslocamento inicial do numero em relacao a entidade atingida.")]
    public Vector3 spawnOffset = new(0f, 2f, 0f);
    [HideInInspector] public float horizontalScatter = .35f;
    [HideInInspector] public float verticalScatter = .15f;
    [Tooltip("Cor do numero exibido ao receber dano.")]
    public Color color = Color.white;
    [Tooltip("Tamanho da fonte do numero na tela.")]
    [Min(1)] public int fontSize = 32;
    [Tooltip("Tempo em segundos que cada dano permanece visivel e que o acumulado espera pelo proximo golpe.")]
    [Min(.01f)] public float lifetime = 1.35f;
    [HideInInspector] public float riseSpeed = 1.5f;
    [Tooltip("Distancia em pixels entre os danos individuais empilhados a esquerda.")]
    [Min(0f)] public float stackSpacingPixels = 48f;
    [Tooltip("Distancia em pixels entre as colunas de dano individual e acumulado.")]
    [Min(0f)] public float columnSpacingPixels = 120f;
    [Tooltip("Fonte usada para mostrar os danos individuais e acumulados.")]
    public Font font;
    [Tooltip("Cor da sombra desenhada atras do numero.")]
    public Color shadowColor = Color.black;
    [Tooltip("Distancia em pixels entre o numero e sua sombra.")]
    [Min(0f)] public float shadowOffsetPixels = 2f;
}

[CreateAssetMenu(menuName = "Projeto PSX/Debug/Damage Number Settings", fileName = "DamageNumberSettings")]
public sealed class DamageNumberSettings : ScriptableObject
{
    private static DamageNumberSettings instance;

    [HideInInspector] [SerializeField] private DamageNumberStyle player = new();
    [FormerlySerializedAs("enemy")]
    [Tooltip("Aparencia compartilhada dos numeros de dano de jogadores, inimigos e objetos.")]
    [SerializeField] private DamageNumberStyle style = new();

    public static DamageNumberSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<DamageNumberSettings>("Debug/DamageNumberSettings");
                if (instance == null)
                {
                    instance = CreateInstance<DamageNumberSettings>();
                    instance.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            return instance;
        }
    }

    public DamageNumberStyle Style => style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
}

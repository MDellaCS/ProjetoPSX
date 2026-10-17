using UnityEngine;

[System.Serializable]
public class PlayerAttackData
{
    [Tooltip("Nome usado pelo combo e pelo binding de animacao deste ataque.")]
    public string attackName;

    [Tooltip("Dano base causado ao acertar, antes do multiplicador de Heat dos pesados.")]
    public int damage = 25;

    [Tooltip("Tempo antes da hitbox ficar ativa.")]
    public float windupDuration = 0.1f;
    [Tooltip("Tempo em que a hitbox pode acertar inimigos.")]
    public float activeDuration = 0.15f;
    [Tooltip("Tempo apos o golpe antes de poder iniciar outra acao.")]
    public float recoveryDuration = 0.25f;

    [Tooltip("Distancia da origem do player ate o centro da hitbox.")]
    public float attackDistance = 1.5f;
    [Tooltip("Raio da hitbox esferica do ataque.")]
    public float attackRadius = 1f;

    [Header("Windup + Active Movement")]
    [Min(0f)]
    [Tooltip("Distancia total percorrida durante Windup + Active. A curva distribui esse total entre as duas fases.")]
    [InspectorName("Windup + Active Move Distance")]
    public float windupMoveDistance = 0f;

    [Range(0f, 1f)]
    [Tooltip("Ponto normalizado de Windup + Active em que o avanco comeca. 0 e o inicio do Windup.")]
    [InspectorName("Windup + Active Move Start")]
    public float windupMoveStart = 0f;

    [Range(0f, 1f)]
    [Tooltip("Ponto normalizado de Windup + Active em que o avanco termina. 1 e o fim do Active.")]
    [InspectorName("Windup + Active Move End")]
    public float windupMoveEnd = 1f;

    [Tooltip("Distribuicao do avanco dentro da janela de movimento de Windup + Active.")]
    [InspectorName("Windup + Active Move Curve")]
    public AnimationCurve windupMoveCurve = CreateWindupActiveEaseInCurve();

    [Header("Heat")]
    [Tooltip("Heat ganho diretamente quando este ataque leve acerta.")]
    public float heatGeneratedOnHit = 0f;

    [Header("Stagger")]
    [Min(0)]
    [Tooltip("Dano de Poise aplicado ao inimigo. Pesados recebem multiplicador conforme Heat consumido.")]
    public int staggerDamage = 10;

    [Header("Attack Type")]
    [Tooltip("Marca o ataque como pesado, fazendo consumir Heat no inicio e usar seus multiplicadores.")]
    public bool isHeavy = false;
    [Tooltip("Lanca o inimigo verticalmente usando os valores globais de Launch Up.")]
    public bool isLaunchUp = false;
    [Tooltip("Empurra o inimigo horizontalmente, podendo causar Wall Splat ao bater no cenario.")]
    public bool isPush = false;
    [Tooltip("Aplica Self Launch Up ao player durante o Active, mesmo se o golpe errar.")]
    public bool launchesPlayer = false;
    [Tooltip("Marca o ataque como aereo e aplica seu comportamento de movimento no ar.")]
    public bool isAerial = false;
    [Tooltip("Queda aerea: mantem a hitbox ativa ate o player tocar o chao.")]
    public bool isDive = false;
    [Tooltip("Permite iniciar este ataque somente durante um Dash terrestre.")]
    public bool isDashAttack = false;

    private static AnimationCurve CreateWindupActiveEaseInCurve()
    {
        // y = x²: pouco deslocamento no inicio e aceleracao clara no fim.
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 2f)
        );
    }
}

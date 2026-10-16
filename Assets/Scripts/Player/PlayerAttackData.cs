using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class PlayerAttackData
{
    public string attackName;

    public int damage = 25;

    public float windupDuration = 0.1f;
    public float activeDuration = 0.15f;
    public float recoveryDuration = 0.25f;

    public float attackDistance = 1.5f;
    public float attackRadius = 1f;

    [Header("Windup Movement")]
    [Min(0f)]
    [Tooltip("Distancia total percorrida durante o Windup.")]
    public float windupMoveDistance = 0f;

    [Range(0f, 1f)]
    public float windupMoveStart = 0f;

    [Range(0f, 1f)]
    public float windupMoveEnd = 1f;

    [Tooltip("Distribuicao do avanco dentro da janela de movimento.")]
    public AnimationCurve windupMoveCurve = CreateWindupEaseInCurve();

    [Header("Heat")]
    [Tooltip("Heat ganho diretamente quando este ataque leve acerta.")]
    [FormerlySerializedAs("heatRemovedOnHit")]
    public int heatGeneratedOnHit = 0;

    [Header("Stagger")]
    [Min(0)]
    public int staggerDamage = 10;

    [Header("Attack Type")]
    public bool isHeavy = false;
    public bool isLaunchUp = false;
    [FormerlySerializedAs("isLaunch")]
    [Tooltip("Empurra o inimigo horizontalmente, podendo causar Wall Splat ao bater no cenario.")]
    public bool isPush = false;
    public bool launchesPlayer = false;
    public bool isAerial = false;
    [Tooltip("Queda aerea: mantem a hitbox ativa ate o player tocar o chao.")]
    public bool isDive = false;
    [Tooltip("Permite iniciar este ataque exclusivamente durante um Dash terrestre.")]
    public bool isDashAttack = false;

    private static AnimationCurve CreateWindupEaseInCurve()
    {
        // y = x²: pouco deslocamento no inicio e aceleracao clara no fim.
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 2f)
        );
    }
}

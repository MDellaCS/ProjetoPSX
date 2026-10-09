using UnityEngine;

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

    public int heatGenerated = 0;
    public int heatRemovedOnHit = 0;

    [Header("Stagger")]
    [Min(0)]
    public int staggerDamage = 10;

    [Header("Attack Type")]
    public bool isHeavy = false;
    public bool isLaunchUp = false;
    public bool launchesPlayer = false;
    public bool isAerial = false;
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

/// <summary>
/// Uma unica entrada que executa varios ataques em ordem, sem exigir inputs
/// adicionais. Cada golpe preserva seus proprios dados de dano, hitbox e movimento.
/// </summary>
[System.Serializable]
public class PlayerAttackSequence
{
    public string sequenceName;
    public bool isDashAttack;
    public PlayerAttackData[] strikes;

    public bool HasStrikes => strikes != null && strikes.Length > 0;
}

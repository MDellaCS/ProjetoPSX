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
    public AnimationCurve windupMoveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public int heatGenerated = 0;
    public int heatRemovedOnHit = 0;

    [Header("Stagger")]
    [Min(0)]
    public int staggerDamage = 10;

    [Header("Attack Type")]
    public bool isHeavy = false;
}

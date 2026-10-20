using System;
using UnityEngine;

[Serializable]
public struct EnemyFloatRange
{
    [Tooltip("Valor minimo do sorteio.")] public float min;
    [Tooltip("Valor maximo do sorteio. Repita o minimo para nao sortear.")] public float max;
    public float RandomValue() => UnityEngine.Random.Range(
        Mathf.Min(min, max), Mathf.Max(min, max));
}

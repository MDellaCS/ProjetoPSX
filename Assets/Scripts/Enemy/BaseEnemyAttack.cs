using UnityEngine;

[System.Serializable]
public class BaseEnemyAttack
{
    public string attackName = "Attack";
    [Tooltip("Quando ligado, o inimigo especifico executa este ataque como projetil em vez de hitbox corpo a corpo.")]
    public bool isProjectileAttack;
    [Min(0)] public int damage = 10;
    [Min(0f)] public float maximumRange = 1.8f;
    [Min(0f)] public float windupDuration = 0.6f;
    [Min(0f)] public float activeDuration = 0.12f;
    [Min(0f)] public float recoveryDuration = 0.7f;
    [Min(0f)] public float hitboxDistance = 1f;
    [Min(0.01f)] public float hitboxRadius = 0.75f;
    [Min(0f)] public float knockbackForce = 3f;
    [Min(0f)] public float knockbackDuration = 0.12f;
}

[System.Serializable]
public struct EnemyFloatRange
{
    [Tooltip("Valor minimo do sorteio.")]
    public float min;
    [Tooltip("Valor maximo do sorteio. Use o mesmo valor nos dois campos para nao sortear.")]
    public float max;

    public float RandomValue()
    {
        return Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));
    }
}

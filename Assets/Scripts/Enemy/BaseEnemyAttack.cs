using UnityEngine;

[System.Serializable]
public class BaseEnemyAttack
{
    [Tooltip("Nome de identificacao exibido no debug e usado pelo inimigo.")]
    public string attackName = "Attack";
    [Tooltip("Quando ligado, o inimigo especifico executa este ataque como projetil em vez de hitbox corpo a corpo.")]
    public bool isProjectileAttack;
    [Tooltip("Dano de vida causado ao player.")] [Min(0)] public int damage = 10;
    [Tooltip("Maior distancia em que a IA considera este ataque valido.")] [Min(0f)] public float maximumRange = 1.8f;
    [Tooltip("Preparacao antes de o ataque acertar.")] [Min(0f)] public float windupDuration = 0.6f;
    [Tooltip("Janela em que a hitbox ou projetil pode acertar.")] [Min(0f)] public float activeDuration = 0.12f;
    [Tooltip("Tempo de vulnerabilidade antes de o inimigo agir novamente.")] [Min(0f)] public float recoveryDuration = 0.7f;
    [Tooltip("Distancia ate o centro da hitbox corpo a corpo.")] [Min(0f)] public float hitboxDistance = 1f;
    [Tooltip("Raio da hitbox corpo a corpo.")] [Min(0.01f)] public float hitboxRadius = 0.75f;
    [Tooltip("Forca horizontal aplicada ao player ao conectar.")] [Min(0f)] public float knockbackForce = 3f;
    [Tooltip("Duracao do knockback aplicado ao player.")] [Min(0f)] public float knockbackDuration = 0.12f;
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

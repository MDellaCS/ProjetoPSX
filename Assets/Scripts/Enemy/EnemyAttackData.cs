using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyAttackData
{
    [Tooltip("Nome de identificacao exibido no debug e usado pelo inimigo.")]
    public string attackName = "Attack";
    [Tooltip("Identificador estavel para comportamentos especificos do inimigo. Pode ser vazio em ataques genericos.")]
    public string attackId = "";
    [Header("Selection")]
    [Tooltip("Permite que este ataque seja sorteado como o primeiro golpe de uma acao. Desligue nos golpes que existem apenas dentro de combos.")]
    public bool canStartAttack = true;
    [Tooltip("Peso relativo para iniciar este ataque. Ex.: pesos 60, 35 e 5 resultam em 60%, 35% e 5% entre os ataques validos.")]
    [Min(0f)] public float openingWeight = 100f;
    [Tooltip("Chance de este ataque tentar continuar para um dos proximos ataques configurados. O sorteio dos ramos ocorre somente se esta chance passar.")]
    [Range(0f, 100f)] public float continuationChance = 65f;
    [Tooltip("Possiveis proximos golpes deste ataque. Mais de um item cria um mixup ponderado.")]
    public List<EnemyAttackContinuation> continuations = new();
    [Header("Execution")]
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

/// <summary>
/// Ligacao de combo de um ataque para outro. Os indices se referem a lista de
/// ataques do mesmo inimigo, exibida na mesma ordem no Inspector.
/// </summary>
[System.Serializable]
public class EnemyAttackContinuation
{
    [Tooltip("Indice do proximo ataque na lista de ataques deste inimigo. Indices invalidos sao ignorados.")]
    [Min(0)] public int nextAttackIndex;
    [Tooltip("Peso relativo deste caminho quando houver mais de uma continuacao valida. Ex.: 70 e 30 criam um mixup de 70% / 30%.")]
    [Min(0f)] public float selectionWeight = 100f;
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

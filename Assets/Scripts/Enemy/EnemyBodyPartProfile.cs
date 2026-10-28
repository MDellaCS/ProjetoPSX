using UnityEngine;

/// <summary>Perfil exclusivo das forcas e da fisica das partes do corpo ao morrer.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Enemy/Body Part Profile", fileName = "EnemyBodyPartProfile")]
public sealed class EnemyBodyPartProfile : ScriptableObject
{
    [Header("Launch Force")]
    [Tooltip("Velocidade inicial aplicada na direcao do ataque que matou o inimigo.")]
    [Min(0f)] public float launchSpeed = 3f;
    [Tooltip("Velocidade adicional por ponto de dano do golpe fatal. Use valores pequenos para nao disparar as partes longe demais.")]
    [Min(0f)] public float speedPerDamage = .0005f;
    [Tooltip("Impulso vertical adicionado ao lancamento, em unidades por segundo.")]
    [Min(0f)] public float upwardSpeed = 2.5f;
    [Tooltip("Variacao aleatoria adicionada a velocidade inicial de cada parte.")]
    [Min(0f)] public float randomSpeed = 1.25f;
    [Tooltip("Velocidade angular inicial aleatoria de cada parte.")]
    [Min(0f)] public float randomAngularSpeed = 8f;
    [Tooltip("Raio aleatorio em volta da posicao original onde cada parte nasce. 0 preserva a posicao exata do modelo.")]
    [Min(0f)] public float spawnScatterRadius;

    [Header("Motion")]
    [Tooltip("Tempo em segundos antes de reciclar cada parte lancada.")]
    [Min(0f)] public float lifetime = 5f;
    [Tooltip("Multiplicador da gravidade global aplicado as partes.")]
    [Min(0f)] public float gravityMultiplier = 1f;
    [Tooltip("Massa de cada parte usada nas colisoes com o mundo e outras particulas.")]
    [Min(.001f)] public float mass = .6f;
    [Tooltip("Desaceleracao linear da parte durante o voo e apos tocar o chao.")]
    [Min(0f)] public float linearDamping = .65f;
    [Tooltip("Desaceleracao da rotacao da parte.")]
    [Min(0f)] public float angularDamping = .3f;
}

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Estilo visual e de movimento aplicado a uma particula de combate.</summary>
public enum CombatParticleKind { Mechanical, Cotton }

/// <summary>Perfil iteravel que define a aparencia e fisica de uma emissao.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Combat Particles/Profile", fileName = "CombatParticleProfile")]
public sealed class CombatParticleProfile : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Nome apresentado para identificar este perfil de particulas.")]
    public string displayName = "Combat Particles";
    [Tooltip("Tipo de simulacao das particulas deste perfil.")]
    public CombatParticleKind kind = CombatParticleKind.Mechanical;

    [Header("Count From Damage")]
    [Tooltip("Curva que converte dano realmente causado (X) em quantidade de particulas (Y).")]
    public AnimationCurve countByDamage = AnimationCurve.Linear(0f, 0f, 100f, 10f);
    [Tooltip("Quantidade minima emitida quando o perfil e usado.")]
    [Min(0)] public int minimumCount = 1;
    [Tooltip("Limite de particulas que uma unica emissao deste perfil pode criar.")]
    [Min(1)] public int maximumCount = 24;

    [Header("Visual")]
    [Tooltip("Material opcional. Vazio usa a cor configurada em um cubo de teste.")]
    public Material material;
    [Tooltip("Cor usada pelos cubos de teste quando nenhum material for informado.")]
    public Color testColor = Color.red;
    [Tooltip("Usa o material do visual do inimigo para esta emissao.")]
    public bool useSourceMaterial;
    [Tooltip("Tamanho minimo dos cubos de teste.")]
    [Min(.01f)] public float minimumSize = .08f;
    [Tooltip("Tamanho maximo dos cubos de teste.")]
    [Min(.01f)] public float maximumSize = .22f;
    [Tooltip("Tempo de vida em segundos das particulas.")]
    [Min(0f)] public float lifetime = 1.2f;

    [Header("Impulse")]
    [Tooltip("Velocidade inicial base das particulas.")]
    [Min(0f)] public float baseSpeed = 2f;
    [Tooltip("Velocidade adicional por ponto de dano realmente causado.")]
    [Min(0f)] public float speedPerDamage = .03f;
    [Tooltip("Variacao aleatoria da direcao e velocidade inicial.")]
    [Min(0f)] public float randomSpeed = 1.5f;
    [Tooltip("Componente vertical adicionada ao impulso inicial.")]
    public float upwardSpeed = 1f;
    [Tooltip("Espalha as particulas, separando suas posicoes e direcoes iniciais.")]
    public bool useScatter;
    [Tooltip("Distancia maxima usada para separar as particulas quando Scatter esta ligado.")]
    [Min(0f)] public float scatterRadius = .35f;

    [Header("Visual Collision")]
    [Tooltip("Massa visual usada pela simulacao contra o chao e paredes.")]
    [Min(.001f)] public float mass = .2f;
    [Tooltip("Desaceleracao linear das particulas visuais.")]
    [Min(0f)] public float linearDamping = 1f;
    [Tooltip("Desaceleracao angular das particulas visuais.")]
    [Min(0f)] public float angularDamping = 1f;
    [Tooltip("Multiplicador de gravidade para algodao. Nao afeta pecas mecanicas.")]
    [FormerlySerializedAs("foamGravityMultiplier")]
    [Range(0f, 1f)] public float cottonGravityMultiplier = .12f;
    [Tooltip("Forca lateral de vento para algodao.")]
    [FormerlySerializedAs("foamWindForce")]
    [Min(0f)] public float cottonWindForce = 2f;
}

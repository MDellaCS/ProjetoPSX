using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EnemyMovesetOpening
{
    [Tooltip("ID do Node que pode iniciar uma nova sequencia.")]
    public string targetNodeId;
    [Tooltip("Peso relativo desta abertura entre todas as aberturas validas.")]
    [Min(0f)] public float selectionWeight = 100f;
    [Tooltip("Exige autorizacao do Enemy Director para iniciar esta abertura.")]
    public bool requiresDirectorAuthorization = true;
}

[Serializable]
public sealed class EnemyMoveTransition
{
    [Tooltip("ID do Node executado se este caminho for escolhido.")]
    public string targetNodeId;
    [Tooltip("Chance individual de tentar esta continuacao.")]
    [Range(0f, 100f)] public float continuationChance = 100f;
    [Tooltip("Peso relativo deste caminho entre continuacoes validas.")]
    [Min(0f)] public float selectionWeight = 100f;
    [Tooltip("Tempo minimo apos o fim do Active antes de continuar.")]
    [Min(0f)] public float minimumDelayAfterActive;
    [Tooltip("Janela apos o fim do Active em que esta continuacao permanece valida. Zero usa toda a Recovery.")]
    [Min(0f)] public float continuationWindow;
    [Tooltip("Continua mesmo no limite da sequencia ou quando o alvo sai das condicoes normais de selecao. O ataque ainda pode ser interrompido por eventos de combate.")]
    public bool mandatoryContinuation;
    [Tooltip("Exige que algum Strike do ataque atual tenha acertado o jogador.")]
    public bool requiresHitConfirm;
}

[Serializable]
public sealed class EnemyMoveNode
{
    [Tooltip("ID estavel usado pelas Openings e Transitions.")]
    public string nodeId;
    [Tooltip("Ataque executado ao entrar neste Node.")]
    public EnemyAttackDefinition attack;
    [Tooltip("Caminhos ponderados que podem continuar a sequencia.")]
    public List<EnemyMoveTransition> transitions = new();
}

[CreateAssetMenu(fileName = "EnemyMoveset", menuName = "Projeto PSX/Enemy/Moveset Definition")]
public sealed class EnemyMovesetDefinition : ScriptableObject
{
    [Header("Global Attack Rotation")]
    [Tooltip("Velocidade global de giro durante Windup + Active dos ataques leves.")]
    [Min(0f)] public float lightWindupActiveRotationSpeed = 240f;
    [Tooltip("Velocidade global de giro durante Windup + Active dos ataques pesados.")]
    [Min(0f)] public float heavyWindupActiveRotationSpeed = 180f;
    [Tooltip("Velocidade global de giro durante Windup + Active dos ataques especiais.")]
    [Min(0f)] public float specialWindupActiveRotationSpeed = 120f;

    [Header("Global Hitbox Extension")]
    [Tooltip("Raio global da Capsule criada entre o centro do inimigo e uma Track estendida.")]
    [Min(.01f)] public float extendedArmHitboxRadius = .25f;

    [Header("Sequence Safety")]
    [Tooltip("Numero maximo de ataques na mesma sequencia, inclusive em grafos ciclicos.")]
    [Min(1)] public int maximumAttacksPerSequence = 5;

    [Tooltip("Aberturas ponderadas que podem iniciar uma nova sequencia.")]
    public List<EnemyMovesetOpening> openings = new();
    [Tooltip("Nodes e transicoes que formam combos, mixups e ataques isolados.")]
    public List<EnemyMoveNode> nodes = new();

    public EnemyMoveNode FindNode(string nodeId) => string.IsNullOrWhiteSpace(nodeId)
        ? null : nodes.Find(node => node != null && node.nodeId == nodeId);

    public float GetRotationSpeed(EnemyAttackKind kind) => kind switch
    {
        EnemyAttackKind.Heavy => heavyWindupActiveRotationSpeed,
        EnemyAttackKind.Special => specialWindupActiveRotationSpeed,
        _ => lightWindupActiveRotationSpeed
    };
}

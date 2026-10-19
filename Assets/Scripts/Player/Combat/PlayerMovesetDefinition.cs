using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerAttackInput
{
    Light,
    Heavy
}

[Serializable]
public class PlayerMoveTransition
{
    [Tooltip("Input que escolhe esta transicao.")]
    public PlayerAttackInput input;
    [Tooltip("ID do Node de destino.")]
    public string targetNodeId;
    [Tooltip("Tempo minimo desde o inicio do Active antes de aceitar a transicao.")]
    [Min(0f)] public float minimumDelayAfterWindup;
    [Tooltip("Tempo maximo depois do fim do ataque em que a transicao continua valida.")]
    [Min(0f)] public float continuationWindow = .5f;
    [Tooltip("Exige que o input aconteca depois do Global Pause Delay.")]
    public bool requiresPause;
    [Tooltip("Exige que ao menos um Strike tenha conectado em um inimigo.")]
    public bool requiresHitConfirm;
}

[Serializable]
public class PlayerMoveNode
{
    [Tooltip("ID estavel usado pelas Entries e Transitions.")]
    public string nodeId;
    [Tooltip("Ataque executado ao entrar neste Node.")]
    public PlayerAttackDefinition attack;
    [Tooltip("Caminhos disponiveis a partir deste Node.")]
    public List<PlayerMoveTransition> transitions = new();
}

[Serializable]
public class PlayerMovesetEntry
{
    [Tooltip("Input que inicia esta rota quando nenhum Node esta sendo continuado.")]
    public PlayerAttackInput input;
    [Tooltip("Contexto necessario para esta entrada.")]
    public PlayerAttackContext context = PlayerAttackContext.Ground;
    [Tooltip("ID do primeiro Node da rota.")]
    public string targetNodeId;
}

[CreateAssetMenu(
    fileName = "PlayerMoveset",
    menuName = "Projeto PSX/Player/Moveset Definition")]
public sealed class PlayerMovesetDefinition : ScriptableObject
{
    [Tooltip("Tempo global sem novo input que classifica a proxima entrada como PAUSE.")]
    [Min(0f)] public float globalPauseDelay = .25f;
    [Tooltip("Entradas contextuais para iniciar ataques fora de uma sequencia.")]
    public List<PlayerMovesetEntry> entries = new();
    [Tooltip("Nodes e transicoes que formam todas as rotas do moveset.")]
    public List<PlayerMoveNode> nodes = new();

    public PlayerMoveNode FindNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return null;
        return nodes.Find(node => node != null && node.nodeId == nodeId);
    }
}

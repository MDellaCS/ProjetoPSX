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
    [Tooltip("Exige que o input aconteca depois do Global Pause Delay.")]
    public bool requiresPause;
    [Tooltip("Exige que ao menos um Strike tenha conectado em um inimigo.")]
    public bool requiresHitConfirm;
}

[Serializable]
public class PlayerTransitionTimingSettings
{
    [Tooltip("Tempo minimo desde o fim do Windup antes de aceitar uma transicao desta categoria.")]
    [Min(0f)] public float minimumDelayAfterWindup;
    [Tooltip("Janela total, contada a partir do fim do Windup, em que uma transicao desta categoria pode ser iniciada.")]
    [Min(0f)] public float continuationWindow = .5f;
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

    [Header("Global Attack Rotation")]
    [Tooltip("Velocidade global de giro dos ataques leves durante Windup + Active, em graus por segundo. Zero impede o giro.")]
    [Min(0f)] public float lightWindupActiveRotationSpeed = 1500f;
    [Tooltip("Velocidade global de giro dos ataques pesados durante Windup + Active, em graus por segundo. Zero impede o giro.")]
    [Min(0f)] public float heavyWindupActiveRotationSpeed = 1500f;

    [Header("Global Transition Timing")]
    [Tooltip("Tempos de transicao compartilhados por todos os ataques leves.")]
    public PlayerTransitionTimingSettings lightTransitionTiming = new();
    [Tooltip("Tempos de transicao compartilhados por todos os ataques pesados.")]
    public PlayerTransitionTimingSettings heavyTransitionTiming = new();

    [Header("Global Hitbox Extension")]
    [Tooltip("Raio global da linha de colisao criada entre o centro do player e a hitbox de ataques com Extended Arm Hitbox. Representa metade da grossura total.")]
    [Min(.01f)] public float extendedArmHitboxRadius = .25f;

    [Header("Global Cancel Rules")]
    [Tooltip("Regras de cancelamento compartilhadas por todos os ataques leves deste moveset.")]
    public List<PlayerAttackCancelRule> lightCancelRules = CreateLightCancelRules();
    [Tooltip("Regras de cancelamento compartilhadas por todos os ataques pesados deste moveset.")]
    public List<PlayerAttackCancelRule> heavyCancelRules = CreateHeavyCancelRules();

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

    public List<PlayerAttackCancelRule> GetCancelRules(PlayerAttackKind kind) =>
        kind == PlayerAttackKind.Heavy ? heavyCancelRules : lightCancelRules;

    public float GetWindupActiveRotationSpeed(PlayerAttackKind kind) =>
        kind == PlayerAttackKind.Heavy
            ? heavyWindupActiveRotationSpeed
            : lightWindupActiveRotationSpeed;

    public PlayerTransitionTimingSettings GetTransitionTiming(
        PlayerAttackKind kind) => kind == PlayerAttackKind.Heavy
            ? heavyTransitionTiming
            : lightTransitionTiming;

    private static List<PlayerAttackCancelRule> CreateLightCancelRules() => new()
    {
        CreateRule(PlayerAttackCancelAction.Jump,
            PlayerAttackCancelPhases.Active | PlayerAttackCancelPhases.Recovery, true),
        CreateRule(PlayerAttackCancelAction.Dash,
            PlayerAttackCancelPhases.Active | PlayerAttackCancelPhases.Recovery, true),
        CreateRule(PlayerAttackCancelAction.HeavyAttack,
            PlayerAttackCancelPhases.All, false)
    };

    private static List<PlayerAttackCancelRule> CreateHeavyCancelRules() => new()
    {
        CreateRule(PlayerAttackCancelAction.Jump,
            PlayerAttackCancelPhases.Active | PlayerAttackCancelPhases.Recovery, true),
        CreateRule(PlayerAttackCancelAction.Dash,
            PlayerAttackCancelPhases.Active | PlayerAttackCancelPhases.Recovery, true)
    };

    private static PlayerAttackCancelRule CreateRule(PlayerAttackCancelAction action,
        PlayerAttackCancelPhases phases, bool finishActive) => new()
    {
        action = action,
        allowedPhases = phases,
        finishActiveBeforeCancelling = finishActive
    };
}

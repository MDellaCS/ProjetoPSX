using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerAttackInput
{
    Light,
    Heavy
}

public enum PlayerResourceMode
{
    Heat,
    Health
}

[Serializable]
public sealed class PlayerPityMechanicSettings
{
    [Tooltip("Ativa a protecao de VitalOil deste moveset.")]
    public bool enabled;
    [Tooltip("Ativa o limite de VitalOil maximo que um unico golpe pode remover.")]
    public bool limitDamage = true;
    [Tooltip("Fracao maxima do VitalOil maximo que um golpe pode remover quando a protecao esta disponivel.")]
    [Range(0f, 1f)] public float maxDamagePercent = .8f;
    [Tooltip("Ativa a protecao contra um golpe letal quando o VitalOil esta acima do limite configurado.")]
    public bool preventLethalDamage = true;
    [Tooltip("Fracao minima do VitalOil maximo exigida antes do golpe para impedir a morte.")]
    [Range(0f, 1f)] public float minimumVitalOilPercent = .3f;
    [Tooltip("Fracao do VitalOil maximo preservada ao impedir um golpe letal.")]
    [Range(0f, 1f)] public float survivorVitalOilPercent = .05f;
    [Tooltip("Tempo em segundos ate ambas as protecoes poderem atuar novamente apos reduzir o dano de um golpe.")]
    [Min(0f)] public float cooldownSeconds = 50f;
}

[Serializable]
public class PlayerMoveTransition
{
    [Tooltip("Botao que escolhe esta transicao.")]
    public PlayerAttackInput input;
    [Tooltip("ID do Node de destino.")]
    public string targetNodeId;
    [Tooltip("Exige que o mesmo clique que iniciou o ataque atual continue pressionado ate esta transicao. Cada clique so pode selecionar uma transicao segurada.")]
    public bool requiresHeldInput;
    [Tooltip("Exige que o input aconteca depois do Global Pause Delay. Se nao existir uma rota Pause para este input, a rota normal ainda funciona como fallback dentro da Continuation Window.")]
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
    [Tooltip("Nome curto exibido no F3 para identificar este moveset durante os testes.")]
    public string displayName = "Moveset";
    [Tooltip("Escolhe o recurso defensivo do moveset. Heat usa o sistema de calor; Health usa uma barra de vida tradicional e desativa as mecanicas de calor.")]
    public PlayerResourceMode resourceMode = PlayerResourceMode.Heat;
    [Tooltip("Configura as protecoes de dano do VitalOil neste moveset.")]
    public PlayerPityMechanicSettings pityMechanic = new();
    [Tooltip("Ataque executado automaticamente durante o slide deste moveset. Vazio deixa o slide sem ataque.")]
    public PlayerAttackDefinition slideAttack;
    [Tooltip("Tempo global sem novo input que classifica a proxima entrada como PAUSE.")]
    [Min(0f)] public float globalPauseDelay = .25f;

    [Header("Movement Abilities")]
    [Tooltip("Multiplicador da velocidade de caminhada base do PlayerMovement para este moveset. 1 mantem a velocidade base; valores menores ou maiores ajustam apenas este moveset.")]
    [Min(0f)] public float movementSpeedMultiplier = 1f;
    [Tooltip("Permite iniciar dash no chao ou no ar. Quando desativado, o input de dash nao inicia nem cancela ataques para executar um dash.")]
    public bool allowDash = true;
    [Tooltip("Permite um segundo pulo no ar. Desativado, o jogador só pode pular uma vez antes de tocar o chão.")]
    public bool allowAirJump = true;
    [Tooltip("Permite que o jogador execute a acao de pulo enquanto este moveset estiver ativo, inclusive durante ataques.")]
    public bool allowJump = true;
    [Tooltip("Permite iniciar um dash enquanto o jogador está no ar.")]
    public bool allowAirDash = true;
    [Tooltip("Multiplica a altura global do pulo configurada no PlayerMovement. 1 mantem a altura global; valores maiores ou menores ajustam este moveset.")]
    [Min(0f)] public float jumpHeightMultiplier = 1f;
    [Tooltip("Multiplica a janela global em que segurar o pulo reduz a gravidade, configurada no PlayerMovement. 1 mantem a janela global; valores maiores ou menores alteram quanto segurar pode estender o salto neste moveset.")]
    [Min(.01f)] public float jumpDurationMultiplier = 1f;
    [Tooltip("Impulso horizontal fixo aplicado pelo dash, em unidades por segundo. Soma-se ao momentum atual do jogador.")]
    [Min(0f)] public float dashImpulse = 12.8f;
    [Tooltip("Tempo, em segundos, até o próximo dash poder ser iniciado.")]
    [Min(0f)] public float dashCooldown = 1.3f;

    [Header("Global Attack Rotation")]
    [Tooltip("Velocidade global de giro dos ataques leves durante Windup + Active, em graus por segundo. Zero impede o giro.")]
    [Min(0f)] public float lightWindupActiveRotationSpeed = 1500f;
    [Tooltip("Velocidade global de giro dos ataques pesados durante Windup + Active, em graus por segundo. Zero impede o giro.")]
    [Min(0f)] public float heavyWindupActiveRotationSpeed = 1500f;

    [Header("Held Attack Branches")]
    [Tooltip("Tempo minimo que o clique original precisa permanecer pressionado para habilitar uma transicao marcada como Held Input. Soltar o botao invalida essa branch.")]
    [Min(0f)] public float heldAttackBranchThreshold = .18f;

    [Header("Global Target Assist")]
    [Tooltip("Margem extra, em metros, somada ao alcance calculado das hitboxes de todos os ataques deste moveset para selecionar um alvo assistido. Compensa o volume do inimigo sem permitir que ataques curtos girem para alvos distantes.")]
    [Min(0f)] public float targetAssistRangePadding = .25f;

    [Header("Global Transition Timing")]
    [Tooltip("Tempos de transicao compartilhados por todos os ataques leves.")]
    public PlayerTransitionTimingSettings lightTransitionTiming = new();
    [Tooltip("Tempos de transicao compartilhados por todos os ataques pesados.")]
    public PlayerTransitionTimingSettings heavyTransitionTiming = new();

    [Header("Global Hitbox Extension")]
    [Tooltip("Raio global da linha de colisao criada entre o centro do player e a hitbox de ataques com Extended Arm Hitbox. Representa metade da grossura total.")]
    [Min(.01f)] public float extendedArmHitboxRadius = .25f;
    [Tooltip("Deslocamento vertical global do ponto inicial da linha Extended Arm Hitbox em relação ao centro do jogador. Aplica-se a todos os ataques deste moveset que usam essa extensão.")]
    public float extendedArmHitboxHeightOffset;

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

    public static PlayerMoveTransition FindTransition(
        List<PlayerMoveTransition> transitions, PlayerAttackInput input,
        bool requiresHeldInput, bool isPauseInput)
    {
        if (transitions == null)
            return null;

        PlayerMoveTransition normalFallback = null;
        foreach (PlayerMoveTransition transition in transitions)
        {
            if (transition == null || transition.input != input ||
                transition.requiresHeldInput != requiresHeldInput)
                continue;
            if (transition.requiresPause == isPauseInput)
                return transition;

            if (isPauseInput && !transition.requiresPause &&
                normalFallback == null)
            {
                normalFallback = transition;
            }
        }
        return normalFallback;
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

public static class PlayerHeldBranchRules
{
    public static bool IsEligible(int originalPressSequence,
        int latestPressSequence, bool buttonStillHeld, bool alreadyConsumed,
        float heldDuration, float requiredDuration) =>
        originalPressSequence > 0 &&
        originalPressSequence == latestPressSequence &&
        buttonStillHeld &&
        !alreadyConsumed &&
        heldDuration >= Mathf.Max(0f, requiredDuration);
}

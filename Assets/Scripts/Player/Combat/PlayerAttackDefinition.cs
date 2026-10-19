using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerAttackKind
{
    Light,
    Heavy
}

public enum PlayerAttackContext
{
    Ground,
    Air,
    Dash
}

public enum PlayerHitboxShape
{
    Sphere,
    Capsule,
    Box
}

public enum PlayerHitboxAnchorMode
{
    Procedural,
    FollowAnchors
}

public enum PlayerHitboxAxis
{
    X,
    Y,
    Z
}

public enum PlayerAttackCancelAction
{
    LightAttack,
    HeavyAttack,
    Jump,
    Dash
}

[Flags]
public enum PlayerAttackCancelPhases
{
    None = 0,
    Windup = 1,
    Active = 2,
    Recovery = 4,
    All = Windup | Active | Recovery
}

[Serializable]
public class PlayerAttackCancelRule
{
    [Tooltip("Acao capaz de cancelar este ataque.")]
    public PlayerAttackCancelAction action;
    [Tooltip("Fases em que o cancelamento pode ser solicitado.")]
    public PlayerAttackCancelPhases allowedPhases =
        PlayerAttackCancelPhases.Recovery;
    [Tooltip("Durante Active, espera o golpe terminar antes de cancelar. Desligado cancela imediatamente.")]
    public bool finishActiveBeforeCancelling = true;
}

[Serializable]
public class PlayerAttackMovementDefinition
{
    [Tooltip("Distancia total percorrida para frente durante Windup e Active.")]
    [Min(0f)] public float forwardDistance;
    [Tooltip("Ponto normalizado de Windup + Active em que o movimento comeca.")]
    [Range(0f, 1f)] public float startNormalized;
    [Tooltip("Ponto normalizado de Windup + Active em que o movimento termina.")]
    [Range(0f, 1f)] public float endNormalized = 1f;
    [Tooltip("Curva que distribui o deslocamento dentro da janela configurada.")]
    public AnimationCurve movementCurve = new(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 2f, 2f)
    );
    [Tooltip("Velocidade de giro em graus por segundo durante o Windup. Zero impede giro.")]
    [Min(0f)] public float windupRotationSpeed = 1500f;
}

[Serializable]
public class PlayerAttackEffectsDefinition
{
    [Tooltip("Lanca verticalmente inimigos que permitem Launch Up.")]
    public bool launchUp;
    [Tooltip("Empurra horizontalmente o inimigo conforme o Poise final do golpe.")]
    public bool push;
    [Tooltip("Mantem no ar inimigos que permitem Juggle e ja estejam aereos.")]
    public bool juggle;
    [Tooltip("Lanca verticalmente o proprio player quando este Strike comeca.")]
    public bool selfLaunchUp;
}

[Serializable]
public class PlayerHitboxTrackDefinition
{
    [Tooltip("Nome de autoria desta hitbox dentro do ataque.")]
    public string trackName = "Hitbox";
    [Tooltip("Forma geometrica usada para detectar inimigos.")]
    public PlayerHitboxShape shape = PlayerHitboxShape.Sphere;
    [Tooltip("Procedural interpola os valores locais. Follow Anchors acompanha sockets do modelo sem depender deles para o tempo do ataque.")]
    public PlayerHitboxAnchorMode anchorMode =
        PlayerHitboxAnchorMode.Procedural;
    [Tooltip("ID do anchor principal quando Anchor Mode e Follow Anchors.")]
    public string primaryAnchorId;
    [Tooltip("ID do segundo anchor usado por Capsules em Follow Anchors.")]
    public string secondaryAnchorId;
    [Tooltip("Posicao local no inicio do movimento procedural, relativa ao centro do player.")]
    public Vector3 startLocalPosition;
    [Tooltip("Posicao local no fim do movimento procedural, relativa ao centro do player.")]
    public Vector3 endLocalPosition;
    [Tooltip("Rotacao local inicial do volume procedural.")]
    public Vector3 startLocalEuler;
    [Tooltip("Rotacao local final do volume procedural.")]
    public Vector3 endLocalEuler;
    [Tooltip("Raio de Sphere e Capsule.")]
    [Min(.01f)] public float radius = .5f;
    [Tooltip("Comprimento total da Capsule, incluindo suas extremidades.")]
    [Min(.02f)] public float capsuleHeight = 1f;
    [Tooltip("Eixo longitudinal da Capsule procedural.")]
    public PlayerHitboxAxis capsuleAxis = PlayerHitboxAxis.Z;
    [Tooltip("Tamanho total da Box nos eixos locais.")]
    public Vector3 boxSize = Vector3.one;
    [Tooltip("Inicio desta Track dentro do Active, de 0 a 1.")]
    [Range(0f, 1f)] public float activeStartNormalized;
    [Tooltip("Fim desta Track dentro do Active, de 0 a 1.")]
    [Range(0f, 1f)] public float activeEndNormalized = 1f;
    [Tooltip("Testa tambem o caminho entre a pose anterior e a atual, evitando atravessar inimigos entre frames.")]
    public bool sweep = true;
    [Tooltip("Distancia maxima entre amostras do Sweep. Valores menores aumentam precisao e custo.")]
    [Min(.01f)] public float sweepStepDistance = .25f;
    [Tooltip("Tracks com o mesmo Hit Group atingem cada inimigo somente uma vez em conjunto.")]
    [Min(0)] public int hitGroup;
    [Tooltip("Permite que o mesmo inimigo seja atingido novamente por esta Track depois do intervalo.")]
    public bool allowRepeatHits;
    [Tooltip("Intervalo minimo entre acertos repetidos no mesmo inimigo.")]
    [Min(.01f)] public float repeatHitInterval = .1f;
}

[Serializable]
public class PlayerAttackStrikeDefinition
{
    [Tooltip("Nome de autoria deste impacto dentro do ataque.")]
    public string strikeName = "Strike";
    [Tooltip("Inicio do Strike dentro do Active, de 0 a 1.")]
    [Range(0f, 1f)] public float activeStartNormalized;
    [Tooltip("Fim do Strike dentro do Active, de 0 a 1.")]
    [Range(0f, 1f)] public float activeEndNormalized = 1f;
    [Tooltip("Multiplicador aplicado ao dano final do ataque neste Strike.")]
    [Min(0f)] public float damageMultiplier = 1f;
    [Tooltip("Multiplicador aplicado ao Poise final do ataque neste Strike.")]
    [Min(0f)] public float poiseMultiplier = 1f;
    [Tooltip("Leves geram o Heat global ao conectar quando esta opcao esta ativa.")]
    public bool generatesHeatOnHit = true;
    [Tooltip("Efeitos adicionais aplicados por este Strike.")]
    public PlayerAttackEffectsDefinition effects = new();
    [Tooltip("Volumes que formam este Strike.")]
    public List<PlayerHitboxTrackDefinition> hitboxes = new();
}

[CreateAssetMenu(
    fileName = "PlayerAttack",
    menuName = "Projeto PSX/Player/Attack Definition")]
public sealed class PlayerAttackDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Identificador estavel usado pelo moveset, debug e bindings opcionais de animacao.")]
    public string attackId = "Attack";
    [Tooltip("Nome legivel exibido nas ferramentas de debug.")]
    public string displayName = "Attack";
    [Tooltip("Categoria do ataque, usada por Heat, cancelamentos e impacto.")]
    public PlayerAttackKind kind;
    [Tooltip("Contexto em que o ataque pode ser iniciado.")]
    public PlayerAttackContext context = PlayerAttackContext.Ground;

    [Header("Timing")]
    [Tooltip("Tempo de preparacao antes dos Strikes ficarem ativos.")]
    [Min(0f)] public float windupDuration = .1f;
    [Tooltip("Duracao da timeline que executa os Strikes e Hitbox Tracks.")]
    [Min(.001f)] public float activeDuration = .1f;
    [Tooltip("Tempo de recuperacao depois do Active.")]
    [Min(0f)] public float recoveryDuration = .2f;

    [Header("Power")]
    [Tooltip("Dano base antes das regras de Heat e dos multiplicadores de Strike.")]
    [Min(0)] public int baseDamage = 10;
    [Tooltip("Dano base de Poise antes das regras de Heat e dos multiplicadores de Strike.")]
    [Min(0)] public int basePoiseDamage = 10;

    [Header("Movement")]
    [Tooltip("Movimento de gameplay executado independentemente da animacao.")]
    public PlayerAttackMovementDefinition movement = new();

    [Header("Strikes")]
    [Tooltip("Impactos independentes executados dentro da fase Active.")]
    public List<PlayerAttackStrikeDefinition> strikes = new();

    [Header("Cancels")]
    [Tooltip("Acoes autorizadas a interromper ou encurtar este ataque.")]
    public List<PlayerAttackCancelRule> cancelRules = new();
}

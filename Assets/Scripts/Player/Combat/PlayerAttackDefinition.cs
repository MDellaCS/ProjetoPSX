using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
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

public enum PlayerHitboxTrajectoryMode
{
    Linear,
    Orbit
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
    Dash,
    Grapple
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
    [Tooltip("Distancia percorrida durante Windup e Active: valor positivo avanca e negativo recua, em metros.")]
    public float forwardDistance;
    [Tooltip("Ponto normalizado de Windup + Active em que o movimento comeca.")]
    [Range(0f, 1f)] public float startNormalized;
    [Tooltip("Ponto normalizado de Windup + Active em que o movimento termina.")]
    [Range(0f, 1f)] public float endNormalized = 1f;
    [Tooltip("Curva que distribui o deslocamento dentro da janela configurada.")]
    public AnimationCurve movementCurve = new(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 2f, 2f)
    );
}

[Serializable]
public class PlayerAttackEffectsDefinition
{
    [Tooltip("Lanca para baixo inimigos aereos que permitem Juggle.")]
    public bool launchDown;
    [Tooltip("Lanca o proprio player para baixo quando este Strike comeca e ele esta no ar.")]
    public bool selfLaunchDown;
    [Tooltip("Lanca verticalmente inimigos que permitem Launch Up.")]
    public bool launchUp;
    [Tooltip("Lanca o inimigo para frente com forca baseada no dano realmente causado pelo golpe.")]
    public bool launchForwards;
    [Tooltip("Lanca o inimigo horizontalmente na direcao do player, oposta ao Launch Forwards, com forca baseada no dano causado.")]
    public bool launchBackwards;
    [Tooltip("Mantem no ar inimigos que permitem Juggle e ja estejam aereos.")]
    public bool juggle;
}

/// <summary>Curva opcional que reduz o dano de um impacto conforme ele se afasta de sua origem.</summary>
[Serializable]
public class PlayerAttackDamageFalloffDefinition
{
    [Tooltip("Ativa a reducao de dano por distancia para este impacto.")]
    public bool enabled;
    [Tooltip("Distancia da origem em que o falloff comeca. Ate este ponto o dano permanece no multiplicador inicial da curva.")]
    [Min(0f)] public float startDistance;
    [Tooltip("Distancia da origem em que o fim da curva de falloff e alcancado.")]
    [Min(.01f)] public float endDistance = 1f;
    [Tooltip("Multiplicador de dano por distancia normalizada. X=0 representa Start Distance; X=1 representa End Distance.")]
    public AnimationCurve damageMultiplier = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    public float Evaluate(float distance)
    {
        if (!enabled)
            return 1f;

        float normalizedDistance = Mathf.InverseLerp(startDistance,
            Mathf.Max(startDistance + .001f, endDistance), distance);
        return Mathf.Max(0f, damageMultiplier == null ||
            damageMultiplier.length == 0
            ? 1f
            : damageMultiplier.Evaluate(normalizedDistance));
    }
}

[Serializable]
public class PlayerHitboxTrackDefinition
{
    [Tooltip("Forma geometrica usada para detectar inimigos.")]
    public PlayerHitboxShape shape = PlayerHitboxShape.Sphere;
    [Tooltip("Procedural interpola os valores locais. Follow Anchors acompanha sockets do modelo sem depender deles para o tempo do ataque.")]
    public PlayerHitboxAnchorMode anchorMode =
        PlayerHitboxAnchorMode.Procedural;
    [Tooltip("Linear interpola entre Start e End. Orbit faz a hitbox circular ao redor do centro real do jogador.")]
    public PlayerHitboxTrajectoryMode trajectoryMode =
        PlayerHitboxTrajectoryMode.Linear;
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

    [Header("Procedural Orbit")]
    [Tooltip("Angulo inicial da orbita em graus. Zero aponta para a frente do player, 90 para a direita e 180 para tras.")]
    public float orbitStartAngleDegrees;
    [Tooltip("Angulo final da orbita em graus. Pode ultrapassar 360 para executar voltas completas ou multiplas.")]
    public float orbitEndAngleDegrees = 360f;
    [Tooltip("Distancia horizontal do centro do player no inicio da orbita.")]
    [Min(0f)] public float orbitStartDistance = 1f;
    [Tooltip("Distancia horizontal do centro do player no fim da orbita.")]
    [Min(0f)] public float orbitEndDistance = 1f;
    [Tooltip("Altura local da orbita em relacao ao centro real da capsula de colisao do jogador.")]
    public float orbitHeight;
    [Tooltip("Curva que distribui a progressao angular da orbita.")]
    public AnimationCurve orbitAngleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Curva que distribui o crescimento ou reducao da distancia orbital.")]
    public AnimationCurve orbitDistanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Raio da hitbox no inicio da orbita.")]
    [Min(.01f)] public float orbitStartRadius = .5f;
    [Tooltip("Raio da hitbox no fim da orbita.")]
    [Min(.01f)] public float orbitEndRadius = .5f;
    [Tooltip("Curva que distribui o crescimento ou reducao do raio durante a orbita.")]
    public AnimationCurve orbitRadiusCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
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
    [Tooltip("Quantidade de repeticoes adicionais da trajetoria durante a janela ativa. Zero executa o golpe uma vez; cada repeticao permite um novo acerto por inimigo.")]
    [Min(0)] public int trajectoryRepeats;
    [Tooltip("Espelha a trajetoria procedural no eixo lateral do jogador a cada repeticao alternada.")]
    public bool alternateRepeatSide;
    [Tooltip("Tracks com o mesmo Hit Group compartilham acertos, sons e intervalo de repeticao por alvo e ciclo.")]
    [Min(0)] public int hitGroup;
    [Tooltip("Permite que o mesmo inimigo seja atingido novamente por esta Track depois do intervalo.")]
    public bool allowRepeatHits;
    [Tooltip("Intervalo minimo entre acertos repetidos no mesmo inimigo.")]
    [Min(.01f)] public float repeatHitInterval = .1f;
}

[Serializable]
internal class LegacyPlayerAttackStrikeDefinition
{
    [Tooltip("Reducao opcional do dano conforme o alvo se afasta da origem do Strike.")]
    public PlayerAttackDamageFalloffDefinition damageFalloff = new();
    [Tooltip("Leves geram o Heat global ao conectar quando esta opcao esta ativa.")]
    public bool generatesHeatOnHit = true;
    [Tooltip("Efeitos adicionais aplicados por este Strike.")]
    public PlayerAttackEffectsDefinition effects = new();
    [Tooltip("Cria uma hitbox entre o centro do personagem e cada track ativa deste golpe, preenchendo o espaco ate o impacto.")]
    public bool usesExtendedArmHitbox = false;
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
    [Tooltip("Categoria do ataque, usada por Heat, cancelamentos e impacto.")]
    public PlayerAttackKind kind;
    [Tooltip("Contexto em que o ataque pode ser iniciado.")]
    public PlayerAttackContext context = PlayerAttackContext.Ground;

    [Header("Timing")]
    [Tooltip("Tempo de preparacao antes das hitboxes ficarem ativas.")]
    [Min(0f)] public float windupDuration = .1f;
    [Tooltip("Duracao da fase ativa que executa as hitboxes.")]
    [Min(.001f)] public float activeDuration = .1f;
    [Tooltip("Tempo de recuperacao depois do Active.")]
    [Min(0f)] public float recoveryDuration = .2f;
    [Tooltip("Tempo ate esta definicao poder iniciar novamente, contado desde o primeiro frame do Windup. Zero desativa o cooldown.")]
    [Min(0f)] public float cooldownDuration;
    [Tooltip("Mantem o ataque na fase Active depois do tempo normal ate o player tocar o chao. Use em Dive para a hitbox e a queda terminarem juntas.")]
    public bool holdActiveUntilGrounded;
    [Header("Power")]
    [Tooltip("Dano base em unidades, convertido pelo valor global de dano do jogador antes de Heat.")]
    [InspectorName("Dano base (unidades)")]
    [Min(0)] public int baseDamage = 10;
    [Tooltip("Dano base de Poise antes das regras de Heat.")]
    [Min(0)] public int basePoiseDamage = 10;

    [Header("Movement")]
    [Tooltip("Permite que o jogador se mova durante todas as fases do ataque usando a porcentagem de velocidade do moveset. Quando ativo, ignora o perfil de movimento.")]
    public bool allowMovementWhileAttacking;

    [Tooltip("Perfil de deslocamento durante Windup e Active, usado apenas quando o movimento livre esta desativado. Sem perfil, usa o movimento antigo preservado no ataque.")]
    public PlayerAttackMovementProfile movementProfile;
    [SerializeField, HideInInspector] private PlayerAttackMovementDefinition movement = new();

    public PlayerAttackMovementDefinition Movement => movementProfile != null && movementProfile.movement != null
        ? movementProfile.movement : movement;

    [Tooltip("Reducao opcional do dano conforme o alvo se afasta da origem do ataque.")]
    public PlayerAttackDamageFalloffDefinition damageFalloff = new();
    [Tooltip("Ataques leves geram Heat ao conectar quando esta opcao esta ativa.")]
    public bool generatesHeatOnHit = true;
    [Tooltip("Cria uma hitbox entre o centro do jogador e cada hitbox ativa do ataque.")]
    public bool usesExtendedArmHitbox;
    [Tooltip("Efeitos adicionais aplicados pelo ataque.")]
    public PlayerAttackEffectsDefinition effects = new();
    [Tooltip("Volumes que formam o ataque durante a fase Active.")]
    public List<PlayerHitboxTrackDefinition> hitboxes = new();

    [FormerlySerializedAs("strikes"), SerializeField, HideInInspector]
    private List<LegacyPlayerAttackStrikeDefinition> legacyStrikes;
    [SerializeField, HideInInspector] private int extendedArmMigrationVersion;
    [SerializeField, HideInInspector] private int attackDataVersion;

    private void OnEnable()
    {
        if (attackDataVersion != 0 || legacyStrikes == null ||
            legacyStrikes.Count == 0 || legacyStrikes[0] == null) return;
        var legacy = legacyStrikes[0];
        damageFalloff = legacy.damageFalloff;
        generatesHeatOnHit = legacy.generatesHeatOnHit;
        usesExtendedArmHitbox = extendedArmMigrationVersion == 0
            ? usesExtendedArmHitbox || legacy.usesExtendedArmHitbox
            : legacy.usesExtendedArmHitbox;
        effects = legacy.effects;
        hitboxes = legacy.hitboxes;
        attackDataVersion = 1;
    }
}

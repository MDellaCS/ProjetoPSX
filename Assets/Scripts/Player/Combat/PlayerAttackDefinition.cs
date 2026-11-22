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

public enum PlayerHitboxTrajectoryMode
{
    Linear,
    Orbit,
    [InspectorName("Posição do alvo assistido")]
    TargetAssist
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
    [Tooltip("Nome de autoria desta hitbox dentro do ataque.")]
    public string trackName = "Hitbox";
    [Tooltip("Forma geometrica usada para detectar inimigos.")]
    public PlayerHitboxShape shape = PlayerHitboxShape.Sphere;
    [Tooltip("Procedural interpola os valores locais. Follow Anchors acompanha sockets do modelo sem depender deles para o tempo do ataque.")]
    public PlayerHitboxAnchorMode anchorMode =
        PlayerHitboxAnchorMode.Procedural;
    [Tooltip("Linear interpola entre Start e End. Orbit faz a hitbox circular ao redor do centro real do jogador. Posição do alvo assistido move a hitbox de Start até a posição do inimigo capturada no início do ataque. Exige um alvo válido dentro do alcance global; sem alvo, usa End Local Position.")]
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
    [Tooltip("Reducao opcional do dano conforme o alvo se afasta da origem do Strike.")]
    public PlayerAttackDamageFalloffDefinition damageFalloff = new();
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
    [Tooltip("Tempo ate esta definicao poder iniciar novamente, contado desde o primeiro frame do Windup. Zero desativa o cooldown.")]
    [Min(0f)] public float cooldownDuration;
    [Tooltip("Mantem o ataque na fase Active depois do tempo normal ate o player tocar o chao. Use em Dive para a hitbox e a queda terminarem juntas.")]
    public bool holdActiveUntilGrounded;
    [Header("Power")]
    [Tooltip("Dano base em unidades, convertido pelo valor global de dano do jogador antes de Heat e dos multiplicadores de Strike.")]
    [InspectorName("Dano base (unidades)")]
    [Min(0)] public int baseDamage = 10;
    [Tooltip("Dano base de Poise antes das regras de Heat e dos multiplicadores de Strike.")]
    [Min(0)] public int basePoiseDamage = 10;

    [Header("Movement")]
    [Tooltip("Movimento de gameplay executado independentemente da animacao.")]
    public PlayerAttackMovementDefinition movement = new();

    [Header("Strikes")]
    [Tooltip("Impactos independentes executados dentro da fase Active.")]
    public List<PlayerAttackStrikeDefinition> strikes = new();

    [Header("Extended Arm Hitbox")]
    [Tooltip("Cria uma hitbox reta entre o centro real do player e cada Hitbox Track ativa, evitando espacos vazios em ataques longos ou orbitais.")]
    public bool usesExtendedArmHitbox;

    /// <summary>
    /// Retorna o maior alcance horizontal que este ataque pode cobrir, medido
    /// a partir do centro do jogador antes do deslocamento de ataque.
    /// </summary>
    public float GetTargetAssistRange(float rangePadding)
    {
        float padding = Mathf.Max(0f, rangePadding);
        float reach = padding +
            Mathf.Max(0f, movement?.forwardDistance ?? 0f);
        if (strikes == null)
            return reach;

        foreach (PlayerAttackStrikeDefinition strike in strikes)
        {
            if (strike?.hitboxes == null)
                continue;

            foreach (PlayerHitboxTrackDefinition track in strike.hitboxes)
            {
                if (track == null ||
                    track.anchorMode == PlayerHitboxAnchorMode.FollowAnchors ||
                    track.trajectoryMode == PlayerHitboxTrajectoryMode.TargetAssist)
                {
                    continue;
                }

                reach = Mathf.Max(reach, GetTrackTargetAssistReach(track) +
                    Mathf.Max(0f, movement?.forwardDistance ?? 0f) +
                    padding);
            }
        }

        return reach;
    }

    /// <summary>Indica se alguma hitbox usa o alcance global de Target Assist.</summary>
    public bool UsesTargetAssistTrajectory
    {
        get
        {
            if (strikes == null)
                return false;

            foreach (PlayerAttackStrikeDefinition strike in strikes)
            {
                if (strike?.hitboxes == null)
                    continue;

                foreach (PlayerHitboxTrackDefinition track in strike.hitboxes)
                {
                    if (track != null &&
                        track.trajectoryMode == PlayerHitboxTrajectoryMode.TargetAssist)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private static float GetTrackTargetAssistReach(
        PlayerHitboxTrackDefinition track)
    {
        float centerReach;
        float volumeReach;
        if (track.trajectoryMode == PlayerHitboxTrajectoryMode.Orbit)
        {
            centerReach = Mathf.Max(0f, track.orbitStartDistance,
                track.orbitEndDistance);
            volumeReach = Mathf.Max(.01f, track.orbitStartRadius,
                track.orbitEndRadius);
        }
        else
        {
            centerReach = Mathf.Max(
                HorizontalMagnitude(track.startLocalPosition),
                HorizontalMagnitude(track.endLocalPosition));
            volumeReach = track.shape switch
            {
                PlayerHitboxShape.Box => track.boxSize.magnitude * .5f,
                PlayerHitboxShape.Capsule => Mathf.Max(.01f, track.radius) +
                    Mathf.Max(0f, track.capsuleHeight * .5f - track.radius),
                _ => Mathf.Max(.01f, track.radius)
            };
        }

        return centerReach + volumeReach;
    }

    private static float HorizontalMagnitude(Vector3 value) =>
        Mathf.Sqrt(value.x * value.x + value.z * value.z);

}

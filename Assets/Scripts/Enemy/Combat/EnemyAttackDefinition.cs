using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyAttackKind { Light, Heavy, Special }
public enum EnemyAttackContext { Ground, Air, Any }
public enum EnemyHitboxShape { Sphere, Capsule, Box }
public enum EnemyHitboxAnchorMode { Procedural, FollowAnchors }
public enum EnemyHitboxTrajectoryMode { Linear, Orbit }
public enum EnemyHitboxAxis { X, Y, Z }

[Serializable]
public sealed class EnemyAttackMovementDefinition
{
    [Tooltip("Distancia total percorrida para frente durante Windup e Active.")]
    public float forwardDistance;
    [Tooltip("Distancia lateral total durante Windup e Active. Valores positivos movem para a direita.")]
    public float lateralDistance;
    [Tooltip("Ponto normalizado de Windup + Active em que o movimento comeca.")]
    [Range(0f, 1f)] public float startNormalized;
    [Tooltip("Ponto normalizado de Windup + Active em que o movimento termina.")]
    [Range(0f, 1f)] public float endNormalized = 1f;
    [Tooltip("Curva que distribui o deslocamento dentro da janela configurada.")]
    public AnimationCurve movementCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Mantem o inimigo mirando o jogador durante Windup.")]
    public bool faceTargetDuringWindup = true;
    [Tooltip("Permite corrigir a direcao do ataque durante Active.")]
    public bool faceTargetDuringActive;
}

[Serializable]
public sealed class EnemyAttackRequirements
{
    [Tooltip("Menor distancia horizontal permitida para selecionar o ataque.")]
    [Min(0f)] public float minimumRange;
    [Tooltip("Maior distancia horizontal permitida para selecionar o ataque.")]
    [Min(0f)] public float maximumRange = 1.8f;
    [Tooltip("Estado de solo exigido do proprio inimigo.")]
    public EnemyAttackContext selfContext = EnemyAttackContext.Any;
    [Tooltip("Estado de solo exigido do jogador alvo.")]
    public EnemyAttackContext targetContext = EnemyAttackContext.Any;
    [Tooltip("Maior angulo, em graus, entre a frente do inimigo e o jogador.")]
    [Range(0f, 180f)] public float maximumTargetAngle = 180f;
    [Tooltip("Exige uma linha livre entre o centro do inimigo e o centro do jogador.")]
    public bool requiresLineOfSight;
    [Tooltip("Camadas que podem bloquear a linha de visao deste ataque.")]
    public LayerMask lineOfSightBlockingLayers = ~0;
    [Tooltip("Agressividade efetiva minima necessaria para selecionar o ataque.")]
    [Range(0f, 100f)] public float minimumAggressionPercent;
    [Tooltip("Agressividade efetiva maxima permitida para selecionar o ataque.")]
    [Range(0f, 100f)] public float maximumAggressionPercent = 100f;
    [Tooltip("Distancia ideal usada para favorecer este ataque entre outras opcoes validas.")]
    [Min(0f)] public float preferredRange = 1f;
}

[Serializable]
public sealed class EnemyHitboxTrackDefinition
{
    [Tooltip("Nome de autoria desta hitbox dentro do ataque.")]
    public string trackName = "Hitbox";
    [Tooltip("Forma geometrica usada para detectar o jogador.")]
    public EnemyHitboxShape shape = EnemyHitboxShape.Sphere;
    [Tooltip("Procedural interpola valores locais. Follow Anchors acompanha sockets do modelo.")]
    public EnemyHitboxAnchorMode anchorMode = EnemyHitboxAnchorMode.Procedural;
    [Tooltip("Linear interpola Start e End. Orbit circula ao redor do centro real do inimigo.")]
    public EnemyHitboxTrajectoryMode trajectoryMode = EnemyHitboxTrajectoryMode.Linear;
    [Tooltip("ID do anchor principal quando Anchor Mode e Follow Anchors.")]
    public string primaryAnchorId;
    [Tooltip("ID do segundo anchor usado por Capsules em Follow Anchors.")]
    public string secondaryAnchorId;
    [Tooltip("Posicao local inicial relativa ao centro real do CharacterController.")]
    public Vector3 startLocalPosition;
    [Tooltip("Posicao local final relativa ao centro real do CharacterController.")]
    public Vector3 endLocalPosition;
    [Tooltip("Rotacao local inicial do volume.")]
    public Vector3 startLocalEuler;
    [Tooltip("Rotacao local final do volume.")]
    public Vector3 endLocalEuler;

    [Header("Procedural Orbit")]
    [Tooltip("Angulo inicial da orbita em graus; zero aponta para frente.")]
    public float orbitStartAngleDegrees;
    [Tooltip("Angulo final da orbita; valores acima de 360 fazem multiplas voltas.")]
    public float orbitEndAngleDegrees = 360f;
    [Tooltip("Distancia horizontal inicial da orbita.")]
    [Min(0f)] public float orbitStartDistance = 1f;
    [Tooltip("Distancia horizontal final da orbita.")]
    [Min(0f)] public float orbitEndDistance = 1f;
    [Tooltip("Altura local da orbita em relacao ao centro do inimigo.")]
    public float orbitHeight;
    [Tooltip("Curva de progressao angular da orbita.")]
    public AnimationCurve orbitAngleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Curva de progressao da distancia orbital.")]
    public AnimationCurve orbitDistanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Raio inicial da hitbox orbital.")]
    [Min(.01f)] public float orbitStartRadius = .5f;
    [Tooltip("Raio final da hitbox orbital.")]
    [Min(.01f)] public float orbitEndRadius = .5f;
    [Tooltip("Curva de progressao do raio orbital.")]
    public AnimationCurve orbitRadiusCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Raio de Sphere e Capsule.")]
    [Min(.01f)] public float radius = .5f;
    [Tooltip("Comprimento total da Capsule, incluindo extremidades.")]
    [Min(.02f)] public float capsuleHeight = 1f;
    [Tooltip("Eixo longitudinal da Capsule procedural.")]
    public EnemyHitboxAxis capsuleAxis = EnemyHitboxAxis.Z;
    [Tooltip("Tamanho total da Box nos eixos locais.")]
    public Vector3 boxSize = Vector3.one;
    [Tooltip("Inicio desta Track dentro do Strike, de zero a um.")]
    [Range(0f, 1f)] public float activeStartNormalized;
    [Tooltip("Fim desta Track dentro do Strike, de zero a um.")]
    [Range(0f, 1f)] public float activeEndNormalized = 1f;
    [Tooltip("Testa o caminho entre a pose anterior e a atual para evitar atravessar o jogador.")]
    public bool sweep = true;
    [Tooltip("Distancia maxima entre amostras do Sweep.")]
    [Min(.01f)] public float sweepStepDistance = .25f;
    [Tooltip("Tracks no mesmo Hit Group acertam o jogador somente uma vez em conjunto.")]
    [Min(0)] public int hitGroup;
    [Tooltip("Permite acertar novamente durante o mesmo Strike.")]
    public bool allowRepeatHits;
    [Tooltip("Intervalo minimo entre acertos repetidos.")]
    [Min(.01f)] public float repeatHitInterval = .1f;
}

[Serializable]
public sealed class EnemyAttackStrikeDefinition
{
    [Tooltip("Nome deste impacto dentro do ataque.")]
    public string strikeName = "Strike";
    [Tooltip("Inicio do Strike dentro do Active, de zero a um.")]
    [Range(0f, 1f)] public float activeStartNormalized;
    [Tooltip("Fim do Strike dentro do Active, de zero a um.")]
    [Range(0f, 1f)] public float activeEndNormalized = 1f;
    [Tooltip("Multiplicador aplicado ao dano base neste Strike.")]
    [Min(0f)] public float damageMultiplier = 1f;
    [Tooltip("Multiplicador aplicado ao dano de Poise neste Strike.")]
    [Min(0f)] public float poiseMultiplier = 1f;
    [Tooltip("Reacao solicitada caso este Strike quebre o Poise do jogador.")]
    public CombatReactionType reaction = CombatReactionType.Stagger;
    [Tooltip("Volumes que formam este Strike.")]
    public List<EnemyHitboxTrackDefinition> hitboxes = new();
}

[CreateAssetMenu(fileName = "EnemyAttack", menuName = "Projeto PSX/Enemy/Attack Definition")]
public sealed class EnemyAttackDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Identificador estavel usado pelo moveset, debug e apresentacao.")]
    public string attackId = "Attack";
    [Tooltip("Nome legivel exibido nas ferramentas de autoria e debug.")]
    public string displayName = "Attack";
    [Tooltip("Categoria semantica do ataque.")]
    public EnemyAttackKind kind;
    [Tooltip("Contexto em que o proprio inimigo pode iniciar o ataque.")]
    public EnemyAttackContext context = EnemyAttackContext.Ground;
    [Tooltip("Estado opcional enviado ao Animator sem controlar a simulacao do ataque.")]
    public string animationStateId;
    [Tooltip("Marcadores livres usados por inimigos especificos, como Charge, Sweep ou AirSpin.")]
    public List<string> tags = new();
    [Tooltip("Chave compartilhada por ataques exclusivos. Inimigos com a mesma chave aguardam a fila global e apenas um pode manter o ataque reservado por vez. Deixe vazio para ataques sem fila.")]
    public string specialQueueId;

    [Header("Timing")]
    [Tooltip("Tempo de preparacao antes dos Strikes ficarem ativos.")]
    [Min(0f)] public float windupDuration = .6f;
    [Tooltip("Duracao da timeline que executa Strikes e Hitbox Tracks.")]
    [Min(.001f)] public float activeDuration = .1f;
    [Tooltip("Tempo de recuperacao depois do Active.")]
    [Min(0f)] public float recoveryDuration = .7f;

    [Header("Power")]
    [Tooltip("Dano base antes dos multiplicadores de Strike.")]
    [Min(0)] public int baseDamage = 10;
    [Tooltip("Dano base de Poise antes dos multiplicadores de Strike.")]
    [Min(0)] public int basePoiseDamage = 10;
    [Tooltip("Quando este ataque quebra o Poise do jogador, aplica HeavyStagger, que o torna imune a novos danos e permite sair mais cedo com dash ou pulo.")]
    public bool causesHeavyStagger;

    [Header("Movement")]
    [Tooltip("Movimento de gameplay executado independentemente da animacao.")]
    public EnemyAttackMovementDefinition movement = new();

    [Header("Requirements")]
    [Tooltip("Condicoes necessarias para este ataque ser selecionado.")]
    public EnemyAttackRequirements requirements = new();

    [Header("Strikes")]
    [Tooltip("Impactos independentes executados dentro da fase Active.")]
    public List<EnemyAttackStrikeDefinition> strikes = new();

    [Header("Extended Arm Hitbox")]
    [Tooltip("Cria uma Capsule entre o centro do inimigo e cada Track ativa.")]
    public bool usesExtendedArmHitbox;

    public bool HasTag(string tag) => !string.IsNullOrWhiteSpace(tag) &&
        tags != null && tags.Exists(value =>
            string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
}

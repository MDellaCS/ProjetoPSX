using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Esquiva reativa opcional. Um acerto do jogador pode iniciar uma retirada;
/// inimigos menos agressivos podem encadear ate tres saltos.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyDodge : MonoBehaviour
{
    private const int MaximumPassiveHops = 3;

    [Header("Decision")]
    [Tooltip("Chance de esquivar ao ser alcancado por um ataque do jogador, caso a esquiva esteja disponivel.")]
    [Range(0f, 100f)] [SerializeField] private float dodgeChanceOnHit = 25f;
    [Tooltip("Agressividade efetiva maxima para considerar uma esquiva. Com 5, o inimigo so pode desviar em 5 por cento ou menos. Inclui o modificador do EnemyDirector.")]
    [FormerlySerializedAs("minimumAggressionPercentToDodge")]
    [Range(0f, 100f)] [SerializeField] private float maximumAggressionPercentToDodge = 100f;
    [Tooltip("A partir desta agressividade, o inimigo faz somente um salto. Abaixo dela, pode encadear ate tres.")]
    [Range(0f, 100f)] [SerializeField] private float aggressiveDodgeThreshold = 60f;
    [Tooltip("Chance de um inimigo passivo fazer mais um salto apos o anterior, ate o limite de tres.")]
    [Range(0f, 100f)] [SerializeField] private float passiveExtraHopChance = 65f;
    [Tooltip("Tempo apos a ultima esquiva antes de poder iniciar outra sequencia.")]
    [Min(0f)] [SerializeField] private float dodgeCooldown = 1.5f;

    [Header("Retreat")]
    [Tooltip("Distancia fixa adicionada a cada salto para tras.")]
    [Min(0f)] [SerializeField] private float baseHopDistance = .4f;
    [Tooltip("Multiplica o alcance horizontal real da hitbox do ataque do jogador, incluindo sua distancia ao centro do player, para definir a distancia de cada salto.")]
    [FormerlySerializedAs("attackRadiusDistanceMultiplier")]
    [Min(0f)] [SerializeField] private float attackReachDistanceMultiplier = 1.5f;
    [Tooltip("Deslocamento lateral minimo, em metros, aplicado aleatoriamente a cada salto de esquiva. Valores negativos vao para a esquerda e positivos para a direita do inimigo.")]
    [SerializeField] private float minimumLateralDodgeOffset = -.25f;
    [Tooltip("Deslocamento lateral maximo, em metros, aplicado aleatoriamente a cada salto de esquiva. Valores negativos vao para a esquerda e positivos para a direita do inimigo.")]
    [SerializeField] private float maximumLateralDodgeOffset = .25f;
    [Tooltip("Duracao de cada salto para tras. Distancia dividida por duracao determina a velocidade.")]
    [Min(.01f)] [SerializeField] private float hopDuration = .27f;
    [Tooltip("Velocidade vertical inicial do salto, aplicada apenas quando o inimigo esta apoiado no chao.")]
    [Min(0f)] [SerializeField] private float hopVerticalSpeed = 2f;
    [Tooltip("Tempo extra sem receber ataques do jogador depois do ultimo salto.")]
    [Min(0f)] [SerializeField] private float protectionAfterDodge = .18f;

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyMovement movement;
    private EnemyAttackController attacks;
    private float hopTimeRemaining;
    private float hopDistance;
    private float currentHopTravelDistance;
    private float cooldownRemaining;
    private float protectionRemaining;
    private Vector3 currentHopDirection;
    private int hopsCompleted;
    private bool canChainHops;

    internal bool IsDodging => owner != null &&
        owner.CurrentState == EnemyBase.State.Dodging;
    internal bool IsAvoidingPlayerAttacks => isActiveAndEnabled &&
        (IsDodging || protectionRemaining > 0f);

    private void OnDisable() => CancelDodge();

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyMovement enemyMovement, EnemyAttackController attackController)
    {
        owner = enemy;
        brain = enemyBrain;
        movement = enemyMovement;
        attacks = attackController;
    }

    internal void TickTimers()
    {
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
        if (!IsDodging)
            protectionRemaining = Mathf.Max(0f,
                protectionRemaining - Time.deltaTime);
    }

    internal bool TryAvoidPlayerAttack(float attackReach, int incomingDamage)
    {
        if (!isActiveAndEnabled)
            return false;
        if (IsAvoidingPlayerAttacks)
            return true;
        if (owner == null || owner.IsDead || brain == null ||
            movement == null || movement.IsLaunchedForwards ||
            !CanInterruptCurrentState() ||
            brain.Target == null || cooldownRemaining > 0f ||
            brain.EffectiveAggressionPercent > maximumAggressionPercentToDodge ||
            !EnemyDirector.CanDodgeIncomingDamage(
                incomingDamage, owner.MaxHealth))
        {
            return false;
        }


        bool isAirborne = movement.IsLaunched || !movement.IsGrounded;
        float effectiveChance = dodgeChanceOnHit *
            EnemyDirector.GetDodgeChanceMultiplier(isAirborne);
        if (Random.value * 100f >= effectiveChance)
            return false;

        hopDistance = baseHopDistance +
            Mathf.Max(0f, attackReach) * attackReachDistanceMultiplier;
        // A personalidade do inimigo define a quantidade de saltos; o
        // modificador temporario do Director nao a transforma em agressiva.
        canChainHops = brain.CurrentAggressionPercent <
            aggressiveDodgeThreshold;
        hopsCompleted = 0;
        if (owner.CurrentState == EnemyBase.State.Guarding)
            owner.CancelGuardForDodge();
        if (owner.CurrentState == EnemyBase.State.Staggered)
            owner.EndStaggerForDodge();
        if (owner.CurrentState == EnemyBase.State.Taunting)
            owner.CancelTaunt();
        attacks?.CancelAttack();
        brain.CancelReposition();
        movement.PrepareForDodge();
        owner.CurrentState = EnemyBase.State.Dodging;
        StartHop();
        return true;
    }

    private bool CanInterruptCurrentState()
    {
        // A esquiva reage ao golpe que iria acertar. Por isso ela precisa
        // interromper tanto a locomocao quanto qualquer fase de um ataque ou
        // Taunt, Guarda e Stagger.
        return owner.CurrentState == EnemyBase.State.Combat ||
            owner.CurrentState == EnemyBase.State.Windup ||
            owner.CurrentState == EnemyBase.State.Active ||
            owner.CurrentState == EnemyBase.State.Recovery ||
            owner.CurrentState == EnemyBase.State.Guarding ||
            owner.CurrentState == EnemyBase.State.Taunting ||
            owner.CurrentState == EnemyBase.State.Staggered ||
            owner.CurrentState == EnemyBase.State.TacticalIdle;
    }

    internal Vector3 TickDodge()
    {
        if (!IsDodging)
            return Vector3.zero;

        Vector3 towardPlayer = brain.Target != null
            ? brain.Target.position - transform.position : transform.forward;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude <= .0001f)
            towardPlayer = transform.forward;
        towardPlayer.Normalize();
        movement.FaceDirection(towardPlayer);

        Vector3 retreatVelocity = currentHopDirection *
            (currentHopTravelDistance / Mathf.Max(.01f, hopDuration));
        hopTimeRemaining -= Time.deltaTime;
        if (hopTimeRemaining <= 0f)
        {
            if (canChainHops && hopsCompleted < MaximumPassiveHops &&
                Random.value * 100f < passiveExtraHopChance)
                StartHop();
            else
                FinishDodge();
        }
        return retreatVelocity;
    }

    internal void CancelDodge()
    {
        // Uma interrupcao externa tambem remove a protecao residual, mesmo
        // se o ultimo salto tiver acabado neste mesmo frame.
        protectionRemaining = 0f;
        if (!IsDodging)
            return;
        hopTimeRemaining = 0f;
        cooldownRemaining = dodgeCooldown;
        owner.CurrentState = EnemyBase.State.Combat;
    }

    private void StartHop()
    {
        hopsCompleted++;
        hopTimeRemaining = Mathf.Max(.01f, hopDuration);
        Vector3 towardPlayer = brain.Target != null
            ? brain.Target.position - transform.position : transform.forward;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude <= .0001f)
            towardPlayer = transform.forward;
        towardPlayer.Normalize();

        float lateralOffset = Random.Range(
            Mathf.Min(minimumLateralDodgeOffset, maximumLateralDodgeOffset),
            Mathf.Max(minimumLateralDodgeOffset, maximumLateralDodgeOffset));
        Vector3 hopOffset = -towardPlayer * hopDistance +
            Vector3.Cross(Vector3.up, towardPlayer) * lateralOffset;
        currentHopTravelDistance = hopOffset.magnitude;
        currentHopDirection = currentHopTravelDistance > .0001f
            ? hopOffset / currentHopTravelDistance : -towardPlayer;
        if (movement.IsGrounded)
            movement.SetVerticalVelocity(hopVerticalSpeed);
    }

    private void FinishDodge()
    {
        hopTimeRemaining = 0f;
        protectionRemaining = protectionAfterDodge;
        cooldownRemaining = dodgeCooldown;
        owner.CurrentState = EnemyBase.State.Combat;
        brain.ScheduleNextDecision();
    }
}

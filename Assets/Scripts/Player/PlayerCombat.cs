using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class PlayerCombat : MonoBehaviour
{
    private enum AttackPhase
    {
        None,
        Windup,
        Active,
        Recovery
    }

    private AttackPhase attackPhase = AttackPhase.None;

    private HashSet<EnemyHealth> enemiesHit = new HashSet<EnemyHealth>();

    private PlayerState playerState;
    private PlayerAttackData currentAttack;
    private PlayerHeat playerHeat;
    private bool currentAttackIsHeavy;

    private float attackTimer;
    private float windupElapsed;
    private int currentDamage;
    private float currentAttackRadius;
    private bool aerialDiveCancelQueued;

    private System.Action attackFinishedCallback;
    private System.Action attackHitCallback;

    [Header("Attack Rotation")]
    [FormerlySerializedAs("windupRotationSpeed")]
    [SerializeField] private float attackingRotationSpeed = 300f;

    [Header("Aerial Attack Movement")]
    [Tooltip("Impulso vertical minimo aplicado por qualquer ataque leve aereo.")]
    [Min(0f)] public float aerialLightLiftSpeed = 4f;
    [Tooltip("Multiplicador global de gravidade durante leves aereos.")]
    [Range(0f, 1f)] public float aerialLightGravityMultiplier = .3f;
    [Tooltip("Velocidade vertical da queda de qualquer pesado aereo.")]
    [Min(0f)] public float aerialDiveFallSpeed = 28f;

    [Header("Launch Up")]
    [Tooltip("Velocidade vertical inicial aplicada por qualquer ataque Launch Up.")]
    [Min(0f)] public float launchUpInitialSpeed = 12f;
    [Tooltip("Aceleracao descendente aplicada a inimigos lancados.")]
    [Min(0f)] public float launchUpGravityMagnitude = 30f;

    [Header("Combat Status")]
    [Min(0f)]
    [FormerlySerializedAs("outOfCombatDamageDelay")]
    [SerializeField] private float combatTimerDuration = 10f;

    private PlayerMovement playerMovement;
    private Vector3 attackFacingDirection;
    private bool hasAggroedEnemies;
    private float combatTimer;

    public bool IsAttacking
    {
        get
        {
            return attackPhase != AttackPhase.None;
        }
    }

    public bool HasAggroedEnemies => hasAggroedEnemies;
    public bool IsOutOfCombat =>
        !hasAggroedEnemies &&
        combatTimer <= 0f;
    public float CombatTimer => combatTimer;

    void Awake()
    {
        // O player nasce fora de combate. O timer so e preenchido por aggro ou dano real.
        combatTimer = 0f;
        PlayerInputBuffer.GetOrAdd(gameObject);
        PlayerComboController.GetOrAdd(gameObject);
    }

    void Start()
    {
        playerState = GetComponent<PlayerState>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHeat = GetComponent<PlayerHeat>();
    }

    void Update()
    {
        UpdateCombatStatus();

        if (IsAttacking)
        {
            UpdateAttack();
        }
    }

    private void UpdateCombatStatus()
    {
        hasAggroedEnemies = EnemyCombat.IsAnyEnemyAggroedOn(transform);

        if (hasAggroedEnemies)
        {
            ResetCombatTimer();
            return;
        }

        combatTimer = Mathf.Max(
            0f,
            combatTimer - Time.deltaTime
        );
    }

    public void RegisterDamageDealtToEnemy()
    {
        ResetCombatTimer();
    }

    public void RegisterDamageTakenFromEnemy()
    {
        ResetCombatTimer();
    }

    private void ResetCombatTimer()
    {
        combatTimer = combatTimerDuration;
    }

    public bool StartAttack(
        PlayerAttackData attack,
        System.Action onAttackFinished,
        System.Action onAttackHit,
        int damage,
        float attackRadius,
        bool isHeavy = false
    )
    {
        if (attack == null ||
            IsAttacking ||
            playerState == null ||
            !playerState.IsNormal())
        {
            return false;
        }

        currentAttackIsHeavy = isHeavy;

        currentAttack = attack;

        ApplyAerialAttackMovement(attack);

        currentDamage = damage;
        currentAttackRadius = attackRadius;

        attackFinishedCallback =
            onAttackFinished;

        attackHitCallback =
            onAttackHit;

        enemiesHit.Clear();

        playerState.ChangeState(
            PlayerState.State.Attacking
        );

        attackFacingDirection = transform.forward;

        if (playerMovement != null && playerMovement.DesiredDirection.sqrMagnitude > 0.0001f)
        {
            attackFacingDirection = playerMovement.DesiredDirection.normalized;
        }

        ChangeAttackPhase(
            AttackPhase.Windup,
            currentAttack.windupDuration
        );

        Debug.Log(
            "Ataque iniciado: " +
            currentAttack.attackName
        );

        return true;
    }

    void UpdateAttack()
    {
        switch (attackPhase)
        {
            case AttackPhase.Windup:

                RotateDuringWindup();
                ApplyWindupMovement();

                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                {
                    ChangeAttackPhase(
                        AttackPhase.Active,
                        currentAttack.activeDuration
                    );
                }

                break;

            case AttackPhase.Active:

                RotateDuringActive();
                Attack();

                if (currentAttack.isAerial && currentAttack.isHeavy)
                {
                    if (playerMovement != null && !playerMovement.IsAerialDiving)
                    {
                        ChangeAttackPhase(
                            AttackPhase.Recovery,
                            currentAttack.recoveryDuration
                        );
                    }

                    // O Dive nao usa Active por tempo. Enquanto a queda estiver ativa, a hitbox permanece ativa ate o chao.
                    break;
                }

                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                {
                    if (aerialDiveCancelQueued)
                    {
                        FinishAttack();
                        break;
                    }

                    ChangeAttackPhase(
                        AttackPhase.Recovery,
                        currentAttack.recoveryDuration
                    );
                }

                break;

            case AttackPhase.Recovery:

                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                {
                    FinishAttack();
                }

                break;
        }
    }

    private void FinishAttack()
    {
        // Guarda o callback antes de limpar os dados.
        System.Action callback = attackFinishedCallback;

        ResetAttack();

        // Só libera o estado se ainda estiver atacando.
        if (playerState.IsAttacking())
        {
            playerState.ChangeState(
                PlayerState.State.Normal
            );
        }

        callback?.Invoke();
    }

    private void ResetAttack()
    {
        attackPhase = AttackPhase.None;
        attackTimer = 0f;
        windupElapsed = 0f;

        currentAttack = null;
        currentDamage = 0;
        currentAttackRadius = 0f;
        aerialDiveCancelQueued = false;

        enemiesHit.Clear();

        attackFinishedCallback = null;
        attackHitCallback = null;
        currentAttackIsHeavy = false;
        playerMovement?.StopAerialAttackMovement();
    }

    void ChangeAttackPhase(AttackPhase newPhase, float duration)
    {
        attackPhase = newPhase;

        attackTimer = duration;

        if (newPhase == AttackPhase.Windup)
            windupElapsed = 0f;
    }

    private void ApplyAerialAttackMovement(PlayerAttackData attack)
    {
        if (playerMovement == null || attack == null)
            return;

        if (!attack.isAerial)
            return;

        if (attack.isHeavy)
        {
            playerMovement.BeginAerialDive(aerialDiveFallSpeed);
            return;
        }

        playerMovement.BeginAerialLight(
            aerialLightLiftSpeed,
            aerialLightGravityMultiplier,
            attack.windupDuration + attack.activeDuration
        );
    }

    private void ApplyWindupMovement()
    {
        if (currentAttack == null ||
            playerMovement == null ||
            currentAttack.windupMoveDistance <= 0f ||
            currentAttack.windupDuration <= 0f)
        {
            return;
        }

        float moveStart = Mathf.Min(
            currentAttack.windupMoveStart,
            currentAttack.windupMoveEnd
        );
        float moveEnd = Mathf.Max(
            currentAttack.windupMoveStart,
            currentAttack.windupMoveEnd
        );

        if (moveEnd <= moveStart)
            return;

        float previousTime = windupElapsed / currentAttack.windupDuration;
        windupElapsed = Mathf.Min(
            currentAttack.windupDuration,
            windupElapsed + Time.deltaTime
        );
        float currentTime = windupElapsed / currentAttack.windupDuration;

        float previousProgress = GetWindupMoveProgress(
            previousTime,
            moveStart,
            moveEnd
        );
        float currentProgress = GetWindupMoveProgress(
            currentTime,
            moveStart,
            moveEnd
        );

        float distance = currentAttack.windupMoveDistance *
            (currentProgress - previousProgress);
        // O avanco segue a frente atual. O giro durante o Windup permite
        // steer sem alterar a distancia total planejada para o ataque.
        playerMovement.ApplyAttackMovement(transform.forward * distance);
    }

    private float GetWindupMoveProgress(
        float windupTime,
        float moveStart,
        float moveEnd)
    {
        float normalizedTime = Mathf.InverseLerp(
            moveStart,
            moveEnd,
            windupTime
        );
        AnimationCurve curve = currentAttack.windupMoveCurve;
        return curve != null
            ? Mathf.Clamp01(curve.Evaluate(normalizedTime))
            : normalizedTime;
    }

    void Attack()
    {
        if (currentAttack == null)
            return;

        Vector3 attackPosition =
            transform.position +
            transform.forward *
            currentAttack.attackDistance;

        Collider[] hits = Physics.OverlapSphere(
            attackPosition,
            currentAttackRadius
        );

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();

            if (enemy != null && !enemiesHit.Contains(enemy))
            {
                int finalPoiseDamage = currentAttack.staggerDamage;

                if (currentAttackIsHeavy && playerHeat != null)
                {
                    finalPoiseDamage = playerHeat.CalculateHeavyPoiseDamage(finalPoiseDamage);
                }

                enemy.TakeDamage(
                    currentDamage,
                    transform.position,
                    finalPoiseDamage
                );

                if (currentAttack.isLaunchUp)
                {
                    enemy.ApplyLaunchUp(
                        launchUpInitialSpeed,
                        launchUpGravityMagnitude
                    );
                }

                RegisterDamageDealtToEnemy();

                enemiesHit.Add(enemy);

                attackHitCallback?.Invoke();
            }
        }
    }

    public void CancelAttack()
    {
        if (!IsAttacking)
            return;

        ResetAttack();
    }

    public bool QueueAerialLightDiveCancel()
    {
        if (currentAttack == null ||
            !currentAttack.isAerial ||
            currentAttack.isHeavy)
        {
            return false;
        }

        aerialDiveCancelQueued = true;

        // Durante Recovery, o cancelamento e imediato. Durante Windup ou
        // Active, ele aguarda o fim do Active para nao cortar o golpe no meio.
        if (attackPhase == AttackPhase.Recovery)
            FinishAttack();

        return true;
    }

    private void RotateDuringWindup()
    {
        if (playerMovement == null)
            return;

        Vector3 direction = playerMovement.DesiredDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            attackFacingDirection = direction.normalized;
        }

        RotateTowards(attackFacingDirection);
    }

    private void RotateDuringActive()
    {
        if (playerMovement == null)
        {
            return;
        }

        Vector3 direction = playerMovement.DesiredDirection;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        RotateTowards(direction);
    }

    private void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            attackingRotationSpeed * Time.deltaTime
        );
    }

    void OnDrawGizmos()
    {
        if (attackPhase != AttackPhase.Active)
            return;

        if (currentAttack == null)
            return;

        Vector3 attackPosition =
            transform.position +
            transform.forward *
            currentAttack.attackDistance;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            attackPosition,
            currentAttackRadius
        );
    }
}

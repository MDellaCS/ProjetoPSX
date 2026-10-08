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
    private int currentDamage;
    private float currentAttackRadius;

    private System.Action attackFinishedCallback;
    private System.Action attackHitCallback;

    [Header("Attack Rotation")]
    [SerializeField] private float windupRotationSpeed = 180f;

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
        combatTimer = combatTimerDuration;
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

                Attack();

                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                {
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

        currentAttack = null;
        currentDamage = 0;
        currentAttackRadius = 0f;

        enemiesHit.Clear();

        attackFinishedCallback = null;
        attackHitCallback = null;
        currentAttackIsHeavy = false;
    }

    void ChangeAttackPhase(AttackPhase newPhase, float duration)
    {
        attackPhase = newPhase;

        attackTimer = duration;
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

        if (attackFacingDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(attackFacingDirection);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            windupRotationSpeed * Time.deltaTime
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

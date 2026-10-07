using UnityEngine;
using System.Collections.Generic;

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

    private float attackTimer;
    private int currentDamage;
    private float currentAttackRadius;

    private bool hasTriggeredHitCallback;

    private System.Action attackFinishedCallback;
    private System.Action attackHitCallback;

    public bool IsAttacking
    {
        get
        {
            return attackPhase != AttackPhase.None;
        }
    }

    void Start()
    {
        playerState = GetComponent<PlayerState>();
    }

    void Update()
    {
        if (IsAttacking)
        {
            UpdateAttack();
        }
    }

    public void StartAttack(
        PlayerAttackData attack,
        System.Action onAttackFinished,
        System.Action onAttackHit,
        int damage,
        float attackRadius
    )
    {
        if (attack == null)
            return;

        currentAttack = attack;

        currentDamage = damage;
        currentAttackRadius = attackRadius;

        attackFinishedCallback =
            onAttackFinished;

        attackHitCallback =
            onAttackHit;

        hasTriggeredHitCallback = false;

        enemiesHit.Clear();

        playerState.ChangeState(
            PlayerState.State.Attacking
        );

        ChangeAttackPhase(
            AttackPhase.Windup,
            currentAttack.windupDuration
        );

        Debug.Log(
            "Ataque iniciado: " +
            currentAttack.attackName
        );
    }

    void UpdateAttack()
    {
        switch (attackPhase)
        {
            case AttackPhase.Windup:

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

    void FinishAttack()
    {
        attackPhase = AttackPhase.None;

        currentAttack = null;
        currentDamage = 0;
        currentAttackRadius = 0f;

        enemiesHit.Clear();

        playerState.ChangeState(
            PlayerState.State.Normal
        );

        System.Action callback =
            attackFinishedCallback;

        attackFinishedCallback = null;
        attackHitCallback = null;

        hasTriggeredHitCallback = false;

        callback?.Invoke();
    }

    void ChangeAttackPhase(
        AttackPhase newPhase,
        float duration
    )
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
            EnemyHealth enemy =
                hit.GetComponent<EnemyHealth>();

            if (enemy != null && !enemiesHit.Contains(enemy))
            {
                enemy.TakeDamage(
                    currentDamage,
                    transform.position
                );

                enemiesHit.Add(enemy);

                if (!hasTriggeredHitCallback)
                {
                    hasTriggeredHitCallback = true;

                    attackHitCallback?.Invoke();
                }
            }
        }
    }

    public void CancelAttack()
    {
        attackPhase = AttackPhase.None;

        currentAttack = null;
        currentDamage = 0;
        currentAttackRadius = 0f;

        attackTimer = 0f;

        enemiesHit.Clear();

        attackFinishedCallback = null;
        attackHitCallback = null;

        hasTriggeredHitCallback = false;
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
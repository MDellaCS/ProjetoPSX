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

    private AttackPhase attackPhase =
        AttackPhase.None;

    private HashSet<EnemyHealth> enemiesHit =
        new HashSet<EnemyHealth>();

    private PlayerState playerState;

    private PlayerAttackData currentAttack;

    private float attackTimer;

    private System.Action attackFinishedCallback;

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
        System.Action onAttackFinished
    )
    {
        if (attack == null)
            return;

        currentAttack = attack;

        attackFinishedCallback =
            onAttackFinished;

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

        enemiesHit.Clear();

        playerState.ChangeState(
            PlayerState.State.Normal
        );

        System.Action callback =
            attackFinishedCallback;

        attackFinishedCallback = null;

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
            currentAttack.attackRadius
        );

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy =
                hit.GetComponent<EnemyHealth>();

            if (enemy != null &&
                !enemiesHit.Contains(enemy))
            {
                enemy.TakeDamage(
                    currentAttack.damage,
                    transform.position
                );

                enemiesHit.Add(enemy);
            }
        }
    }

    public void CancelAttack()
    {
        attackPhase = AttackPhase.None;

        currentAttack = null;

        attackTimer = 0f;

        enemiesHit.Clear();

        attackFinishedCallback = null;
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
            currentAttack.attackRadius
        );
    }
}
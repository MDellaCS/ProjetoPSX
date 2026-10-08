using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    private static readonly HashSet<EnemyCombat> activeEnemies = new HashSet<EnemyCombat>();
    public enum EnemyState { Idle, Chasing, Windup, Attacking, Recovery, Staggered, Stunned }
    public EnemyState currentState = EnemyState.Idle;
    public Transform target;

    [Header("Detection")]
    public float detectionRange = 8f;
    public float closeRange = 2.2f;
    public float mediumRange = 4.5f;
    public float windupRotationSpeed = 180f;

    [Header("Attacks")]
    public EnemyAttackData quickSlash = new EnemyAttackData { attackName = "Quick Slash", damage = 15, maximumRange = 2.2f, windupDuration = .2f, activeDuration = .15f, recoveryDuration = .35f, hitboxDistance = 1.2f, hitboxRadius = .8f, knockbackForce = 4f, knockbackDuration = .12f };
    public EnemyAttackData heavyStrike = new EnemyAttackData { attackName = "Heavy Strike", damage = 40, maximumRange = 2.8f, windupDuration = .75f, activeDuration = .2f, recoveryDuration = .9f, hitboxDistance = 1.6f, hitboxRadius = 1f, knockbackForce = 9f, knockbackDuration = .25f };
    public EnemyAttackData charge = new EnemyAttackData { attackName = "Charge", damage = 30, minimumRange = 1.5f, maximumRange = 8f, windupDuration = .65f, activeDuration = .45f, recoveryDuration = 1.2f, hitboxDistance = 1.3f, hitboxRadius = .9f, knockbackForce = 10f, knockbackDuration = .3f, isUnblockable = true, movesForward = true, movementSpeed = 10f };

    [Header("Stagger and Stun")]
    public float staggerDuration = .8f;
    public float staggerKnockbackForce = 8f;
    public float staggerKnockbackDuration = .2f;
    public float stunDuration = 1.2f;
    public int stunDamageBreakpoint = 80;

    private EnemyMovement enemyMovement;
    private EnemyAttackData currentAttack;
    private float stateTimer;
    private int stunDamageTaken;
    private bool hasDealtDamage;
    private Renderer enemyRenderer;
    private Color normalColor;
    private bool isTrainingDummy;
    private bool trainingDummyAttacks;
    private float trainingDummyAttackInterval;
    private EnemyAttackData trainingDummyAttack;

    public bool IsCharging => currentState == EnemyState.Attacking && currentAttack != null && currentAttack.movesForward;
    public float ChargeSpeed => IsCharging ? currentAttack.movementSpeed : 0f;

    void OnEnable() => activeEnemies.Add(this);
    void OnDisable() => activeEnemies.Remove(this);

    public static bool IsAnyEnemyAggroedOn(Transform player)
    {
        foreach (EnemyCombat enemy in activeEnemies)
            if (enemy != null && enemy.target == player && enemy.currentState != EnemyState.Idle)
                return true;
        return false;
    }

    void Start()
    {
        enemyRenderer = GetComponent<Renderer>();
        enemyMovement = GetComponent<EnemyMovement>();
        normalColor = enemyRenderer.material.color;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;

        if (isTrainingDummy)
            stateTimer = trainingDummyAttackInterval;
    }

    void Update()
    {
        if (isTrainingDummy)
        {
            UpdateTrainingDummy();
            return;
        }

        if (target == null) return;
        switch (currentState)
        {
            case EnemyState.Idle: if (DistanceToTarget() <= detectionRange) ChangeState(EnemyState.Chasing); break;
            case EnemyState.Chasing: UpdateChasing(); break;
            case EnemyState.Windup: UpdateWindup(); break;
            case EnemyState.Attacking: UpdateAttacking(); break;
            case EnemyState.Recovery: UpdateTimedState(EnemyState.Chasing); break;
            case EnemyState.Staggered: UpdateTimedState(EnemyState.Chasing); break;
            case EnemyState.Stunned: UpdateTimedState(EnemyState.Chasing); break;
        }
    }

    private void UpdateTrainingDummy()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                if (!trainingDummyAttacks || trainingDummyAttack == null)
                    return;

                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    currentAttack = trainingDummyAttack;
                    ChangeState(EnemyState.Windup);
                }
                break;
            case EnemyState.Windup:
                UpdateWindup();
                break;
            case EnemyState.Attacking:
                UpdateAttacking();
                break;
            case EnemyState.Recovery:
            case EnemyState.Staggered:
            case EnemyState.Stunned:
                UpdateTimedState(EnemyState.Idle);
                break;
        }
    }

    void UpdateChasing()
    {
        float distance = DistanceToTarget();
        if (distance > detectionRange) { ChangeState(EnemyState.Idle); return; }
        currentAttack = SelectAttack(distance);
        if (currentAttack != null) ChangeState(EnemyState.Windup);
    }

    void UpdateWindup()
    {
        if (!isTrainingDummy)
            RotateTowardsTarget();
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) ChangeState(EnemyState.Attacking);
    }

    void UpdateAttacking()
    {
        Attack();
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) ChangeState(EnemyState.Recovery);
    }

    void UpdateTimedState(EnemyState next)
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) ChangeState(next);
    }

    EnemyAttackData SelectAttack(float distance)
    {
        float quick = distance <= closeRange ? 60f : distance <= mediumRange ? 25f : 0f;
        float heavy = distance <= closeRange ? 35f : distance <= mediumRange ? 35f : 0f;
        float dash = distance <= closeRange ? 5f : distance <= mediumRange ? 40f : 100f;
        if (!IsValid(quickSlash, distance)) quick = 0f;
        if (!IsValid(heavyStrike, distance)) heavy = 0f;
        if (!IsValid(charge, distance)) dash = 0f;
        float total = quick + heavy + dash;
        if (total <= 0f) return null;
        float roll = Random.value * total;
        if ((roll -= quick) < 0f) return quickSlash;
        if ((roll -= heavy) < 0f) return heavyStrike;
        return charge;
    }

    bool IsValid(EnemyAttackData attack, float distance) => attack != null && distance >= attack.minimumRange && distance <= attack.maximumRange;
    float DistanceToTarget() { Vector3 offset = target.position - transform.position; offset.y = 0f; return offset.magnitude; }

    void ChangeState(EnemyState state)
    {
        currentState = state;
        if (state == EnemyState.Chasing || state == EnemyState.Idle) { enemyRenderer.material.color = normalColor; }
        else if (state == EnemyState.Windup) { stateTimer = currentAttack.windupDuration; enemyRenderer.material.color = currentAttack.isUnblockable ? Color.red : Color.magenta; }
        else if (state == EnemyState.Attacking) { stateTimer = currentAttack.activeDuration; hasDealtDamage = false; }
        else if (state == EnemyState.Recovery) { stateTimer = currentAttack.recoveryDuration; enemyRenderer.material.color = normalColor; }
        else if (state == EnemyState.Staggered) { stateTimer = staggerDuration; enemyRenderer.material.color = Color.yellow; }
        else if (state == EnemyState.Stunned) { stateTimer = stunDuration; enemyRenderer.material.color = Color.cyan; }
        else if (state == EnemyState.Idle && isTrainingDummy) { stateTimer = trainingDummyAttackInterval; }
    }

    void RotateTowardsTarget()
    {
        Vector3 direction = target.position - transform.position; direction.y = 0f;
        if (direction.sqrMagnitude > .0001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), windupRotationSpeed * Time.deltaTime);
    }

    void Attack()
    {
        if (hasDealtDamage || currentAttack == null) return;
        Vector3 position = transform.position + transform.forward * currentAttack.hitboxDistance;
        foreach (Collider hit in Physics.OverlapSphere(position, currentAttack.hitboxRadius))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player == null) continue;
            bool parried = player.TakeDamage(currentAttack.damage, transform.position, currentAttack.knockbackForce, currentAttack.knockbackDuration, true, currentAttack.isUnblockable);
            hasDealtDamage = true;
            if (parried) ApplyStun();
            break;
        }
    }

    public void ApplyStagger(Vector3 attackerPosition)
    {
        ChangeState(EnemyState.Staggered);
        Vector3 direction = transform.position - attackerPosition; direction.y = 0f;
        if (direction.sqrMagnitude > .0001f) enemyMovement.ApplyKnockback(direction.normalized, staggerKnockbackForce, staggerKnockbackDuration);
    }

    public void ApplyStun() { stunDamageTaken = 0; ChangeState(EnemyState.Stunned); }
    public void RegisterDamageDuringStun(int damage)
    {
        if (currentState != EnemyState.Stunned || damage <= 0) return;
        stunDamageTaken += damage;
        if (stunDamageTaken >= stunDamageBreakpoint)
            ChangeState(isTrainingDummy ? EnemyState.Idle : EnemyState.Chasing);
    }

    public void ConfigureAsTrainingDummy(
        bool attacksRegularly,
        float attackInterval,
        EnemyAttackData attack)
    {
        isTrainingDummy = true;
        trainingDummyAttacks = attacksRegularly;
        trainingDummyAttackInterval = Mathf.Max(0f, attackInterval);
        trainingDummyAttack = attack;
        currentAttack = null;
        currentState = EnemyState.Idle;
        stateTimer = trainingDummyAttackInterval;

        if (enemyRenderer != null)
            enemyRenderer.material.color = normalColor;
    }
}

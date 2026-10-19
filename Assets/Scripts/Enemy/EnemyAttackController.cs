using UnityEngine;

/// <summary>Moveset, sorteios, fases de ataque e hitbox corpo a corpo.</summary>
[DisallowMultipleComponent]
public sealed class EnemyAttackController : MonoBehaviour
{
    [Header("Moveset")]
    [Tooltip("Preenche a lista vazia com os ataques padrao do tipo de inimigo. Desligue para permitir um moveset vazio.")]
    [SerializeField] private bool useDefaultAttacksWhenEmpty = true;
    [Tooltip("Lista de ataques deste inimigo. Cada ataque pode iniciar uma acao, continuar combos e escolher caminhos com pesos.")]
    [SerializeField] private EnemyAttackData[] attacks = System.Array.Empty<EnemyAttackData>();
    [Tooltip("Numero maximo de golpes na mesma sequencia. Impede que caminhos de combo ciclicos mantenham o inimigo atacando para sempre.")]
    [Min(1)] [SerializeField] private int maximumAttacksPerSequence = 5;

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyAttackData currentAttack;
    private float stateTimer;
    private bool hasDamagedDuringActive;
    private int attacksInSequence;

    public EnemyAttackData CurrentAttack => currentAttack;
    public EnemyAttackData[] Attacks => attacks;

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyAttackData[] defaults)
    {
        owner = enemy;
        brain = enemyBrain;
        SetDefaultsIfEmpty(defaults);
    }

    internal void SetDefaultsIfEmpty(EnemyAttackData[] defaults)
    {
        if (useDefaultAttacksWhenEmpty && (attacks == null || attacks.Length == 0))
            attacks = defaults ?? System.Array.Empty<EnemyAttackData>();
    }

    public EnemyAttackData GetAttack(int index) =>
        attacks != null && index >= 0 && index < attacks.Length
            ? attacks[index] : null;

    public bool HasAvailableOpening()
    {
        if (brain == null || brain.Target == null || attacks == null)
            return false;

        for (int i = 0; i < attacks.Length; i++)
            if (IsValidOpening(i))
                return true;

        return false;
    }

    public int SelectOpeningAttackIndex()
    {
        if (brain.Target == null || attacks == null)
            return -1;
        float totalWeight = 0f;
        for (int i = 0; i < attacks.Length; i++)
            if (IsValidOpening(i))
                totalWeight += attacks[i].openingWeight;
        if (totalWeight <= 0f)
            return -1;

        float roll = Random.value * totalWeight;
        for (int i = 0; i < attacks.Length; i++)
        {
            if (!IsValidOpening(i)) continue;
            roll -= attacks[i].openingWeight;
            if (roll <= 0f)
                return i;
        }
        return -1;
    }

    private bool IsValidOpening(int index)
    {
        EnemyAttackData attack = GetAttack(index);
        return attack != null && attack.canStartAttack &&
            attack.openingWeight > 0f &&
            brain.DistanceToTarget() <= attack.maximumRange &&
            owner.CanSelectAttack(attack, index);
    }

    public bool TryBeginAttack(int index)
    {
        if (GetAttack(index) == null || !EnemyDirector.RequestAttackTurn(owner))
            return false;
        attacksInSequence = 0;
        BeginAttack(index);
        return true;
    }

    private void BeginAttack(int index)
    {
        currentAttack = GetAttack(index);
        if (currentAttack == null)
            return;
        attacksInSequence++;
        brain.OnAttackStarted();
        owner.NotifyAttackStarted(currentAttack);
        stateTimer = Mathf.Max(0f, currentAttack.windupDuration);
        hasDamagedDuringActive = false;
        owner.CurrentState = EnemyBase.State.Windup;
    }

    public void TickWindup()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
            return;
        stateTimer = Mathf.Max(0f, currentAttack.activeDuration);
        hasDamagedDuringActive = false;
        owner.CurrentState = EnemyBase.State.Active;
        owner.NotifyAttackActive(currentAttack);
    }

    public void TickActive()
    {
        if (!hasDamagedDuringActive && currentAttack != null &&
            owner.PerformAttack(currentAttack))
            hasDamagedDuringActive = true;
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
            return;
        stateTimer = currentAttack != null
            ? Mathf.Max(0f, currentAttack.recoveryDuration) : 0f;
        owner.CurrentState = EnemyBase.State.Recovery;
    }

    public void TickRecovery()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
            return;
        int next = SelectContinuationAttackIndex();
        if (next >= 0)
        {
            BeginAttack(next);
            return;
        }
        CancelAttack();
        owner.CurrentState = EnemyBase.State.Combat;
        brain.ScheduleNextDecision();
    }

    private int SelectContinuationAttackIndex()
    {
        if (currentAttack == null || brain.Target == null ||
            attacksInSequence >= maximumAttacksPerSequence ||
            currentAttack.continuations == null ||
            currentAttack.continuations.Count == 0 ||
            Random.value > currentAttack.continuationChance / 100f)
            return -1;

        float totalWeight = 0f;
        foreach (EnemyAttackContinuation continuation in currentAttack.continuations)
            if (IsValidContinuation(continuation))
                totalWeight += continuation.selectionWeight;
        if (totalWeight <= 0f)
            return -1;

        float roll = Random.value * totalWeight;
        foreach (EnemyAttackContinuation continuation in currentAttack.continuations)
        {
            if (!IsValidContinuation(continuation)) continue;
            roll -= continuation.selectionWeight;
            if (roll <= 0f)
                return continuation.nextAttackIndex;
        }
        return -1;
    }

    private bool IsValidContinuation(EnemyAttackContinuation continuation)
    {
        if (continuation == null || continuation.selectionWeight <= 0f)
            return false;
        EnemyAttackData attack = GetAttack(continuation.nextAttackIndex);
        return attack != null &&
            brain.DistanceToTarget() <= attack.maximumRange &&
            owner.CanSelectAttack(attack, continuation.nextAttackIndex);
    }

    public void CancelAttack()
    {
        currentAttack = null;
        stateTimer = 0f;
        hasDamagedDuringActive = false;
        attacksInSequence = 0;
    }

    public bool ExecuteMeleeAttack(EnemyAttackData attack)
    {
        Vector3 center = owner.AttackOrigin +
            transform.forward * attack.hitboxDistance;
        foreach (Collider hit in Physics.OverlapSphere(center, attack.hitboxRadius))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player == null) continue;
            player.TakeDamage(attack.damage, owner.AttackOrigin,
                attack.knockbackForce, attack.knockbackDuration, true, transform);
            return true;
        }
        return false;
    }
}

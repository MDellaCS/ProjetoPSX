using UnityEngine;

/// <summary>
/// Defesa frontal opcional. Este componente nao e adicionado automaticamente:
/// apenas inimigos que devem usar guarda precisam dele.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyGuard : MonoBehaviour
{
    [Header("Decision")]
    [Tooltip("Chance, em porcentagem, de levantar guarda sempre que o Brain toma uma decisao em combate.")]
    [Range(0f, 100f)] [SerializeField] private float guardChancePerDecision = 15f;
    [Tooltip("Agressividade efetiva maxima para levantar guarda, incluindo o bonus ou penalidade temporarios do EnemyDirector.")]
    [Range(0f, 100f)] [SerializeField] private float maximumAggressionPercentToConsiderGuard = 50f;
    [Tooltip("Distancia horizontal maxima ao jogador para o Brain considerar levantar guarda.")]
    [Min(0f)] [SerializeField] private float guardStartMaximumRange = 3f;
    [Tooltip("Menor tempo que a guarda permanece levantada antes de poder baixar.")]
    [Min(0f)] [SerializeField] private float minimumGuardDuration = .35f;
    [Tooltip("Maior tempo sorteado para a guarda permanecer levantada.")]
    [Min(0f)] [SerializeField] private float maximumGuardDuration = .8f;
    [Tooltip("Tempo apos baixar ou quebrar a guarda antes de poder defende-la novamente.")]
    [Min(0f)] [SerializeField] private float guardCooldown = 1.2f;

    [Header("Blocked Attack Types")]
    [Tooltip("Permite bloquear ataques leves terrestres do player.")]
    [SerializeField] private bool canGuardPlayerLightAttacks = true;
    [Tooltip("Permite bloquear ataques pesados terrestres do player.")]
    [SerializeField] private bool canGuardPlayerHeavyAttacks = true;
    [Tooltip("Permite bloquear ataques aereos do player.")]
    [SerializeField] private bool canGuardPlayerAerialAttacks = true;
    [Tooltip("Permite bloquear ataques de Dash do player.")]
    [SerializeField] private bool canGuardPlayerDashAttacks = true;

    [Header("Block")]
    [Tooltip("Parte do dano anulada ao bloquear. 0.7 reduz o dano recebido em 70 por cento.")]
    [Range(0f, 1f)] [SerializeField] private float damageReduction = .7f;
    [Tooltip("Parte do dano de Poise anulada ao bloquear.")]
    [Range(0f, 1f)] [SerializeField] private float poiseReduction = .5f;
    [Tooltip("Resistencia da guarda. Ataques reduzem este valor usando seu dano de Poise; a resistencia persiste ao baixar e levantar a guarda e volta ao maximo depois de uma quebra.")]
    [Min(1f)] [SerializeField] private float maxGuardHealth = 120f;
    [Tooltip("Multiplicador de dano a guarda aplicado a pesados do player.")]
    [Min(0f)] [SerializeField] private float heavyGuardDamageMultiplier = 3f;
    [Tooltip("Tempo de Stagger aplicado quando a resistencia da guarda chega a zero.")]
    [Min(0f)] [SerializeField] private float breakStunDuration = 1.2f;

    [Header("Stagger Guard")]
    [Tooltip("Tempo apos entrar em Stagger antes de poder levantar a guarda e encerrar o Stagger mais cedo.")]
    [Min(0f)] [SerializeField] private float staggerGuardDelay = .2f;

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyHealth health;
    private EnemyAttackController attacks;
    private EnemyMovement movement;
    private float guardTimer;
    private float cooldownTimer;
    private float currentGuardHealth;
    private float staggerElapsed;

    public bool IsGuarding => owner != null &&
        owner.CurrentState == EnemyBase.State.Guarding;
    public float CurrentGuardHealth => currentGuardHealth;
    public float MaxGuardHealth => maxGuardHealth;

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyHealth enemyHealth, EnemyAttackController attackController,
        EnemyMovement enemyMovement)
    {
        owner = enemy;
        brain = enemyBrain;
        health = enemyHealth;
        attacks = attackController;
        movement = enemyMovement;
        currentGuardHealth = maxGuardHealth;
    }

    internal void TickCooldown()
    {
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
    }

    internal bool TryStartGuardFromDecision()
    {
        if (!CanStartGuard(allowStagger: false) ||
            Random.value > guardChancePerDecision / 100f)
        {
            return false;
        }

        StartGuard();
        return true;
    }

    internal void NotifyStaggerStarted()
    {
        staggerElapsed = 0f;
    }

    internal void TickStaggerGuard()
    {
        if (owner == null || owner.CurrentState != EnemyBase.State.Staggered)
            return;

        staggerElapsed += Time.deltaTime;
        if (staggerElapsed < staggerGuardDelay ||
            !CanStartGuard(allowStagger: true))
        {
            return;
        }

        StartGuard();
    }

    private bool CanStartGuard(bool allowStagger)
    {
        if (owner == null || brain == null || health == null || owner.IsDead ||
            cooldownTimer > 0f || brain.Target == null || movement == null ||
            !movement.IsGrounded || movement.IsLaunched)
        {
            return false;
        }

        bool canGuardInCurrentState = owner.CurrentState ==
            EnemyBase.State.Staggered
            ? allowStagger
            : owner.CurrentState == EnemyBase.State.Combat;
        if (!canGuardInCurrentState)
            return false;

        return brain.EffectiveAggressionPercent <=
                maximumAggressionPercentToConsiderGuard &&
            brain.DistanceToTarget() <= guardStartMaximumRange;
    }

    private void StartGuard()
    {
        attacks?.CancelAttack();
        brain.CancelReposition();
        guardTimer = Random.Range(
            Mathf.Min(minimumGuardDuration, maximumGuardDuration),
            Mathf.Max(minimumGuardDuration, maximumGuardDuration)
        );
        if (owner.CurrentState == EnemyBase.State.Staggered)
            health.EndStaggerForGuard();
        owner.CurrentState = EnemyBase.State.Guarding;
        health.SetGuardVisual(true);
    }

    internal void TickGuard()
    {
        if (!IsGuarding)
            return;

        // Guarda e combate aereo criariam uma defesa sem contrajogo: qualquer
        // perda de apoio (inclusive Launch Up) encerra a guarda imediatamente.
        if (movement == null || !movement.IsGrounded || movement.IsLaunched)
        {
            EndGuard();
            return;
        }

        guardTimer -= Time.deltaTime;
        if (guardTimer <= 0f)
            EndGuard();
    }

    internal Vector3 GetGuardRetreatVelocity()
    {
        if (!IsGuarding || brain == null || brain.Target == null ||
            movement == null)
        {
            return Vector3.zero;
        }

        Vector3 towardPlayer = brain.Target.position - transform.position;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude <= .0001f)
            return Vector3.zero;

        // FaceDirection ignora a regra de Tank Controls, pois bloquear deve
        // manter o olhar no player mesmo enquanto o inimigo recua.
        movement.FaceDirection(towardPlayer);
        return -towardPlayer.normalized * movement.SlowSpeed;
    }

    internal bool TryBlock(PlayerAttackType attackType,
        Vector3 attackerPosition, ref int damage, ref int poiseDamage)
    {
        if (!IsGuarding || !CanBlockAttackType(attackType))
        {
            return false;
        }

        float guardDamage = Mathf.Max(0, poiseDamage);
        if (attackType == PlayerAttackType.Heavy ||
            attackType == PlayerAttackType.AerialHeavy ||
            attackType == PlayerAttackType.DashHeavy)
        {
            guardDamage *= heavyGuardDamageMultiplier;
        }

        damage = Mathf.Max(0, Mathf.RoundToInt(damage * (1f - damageReduction)));
        poiseDamage = Mathf.Max(0,
            Mathf.RoundToInt(poiseDamage * (1f - poiseReduction)));
        currentGuardHealth -= guardDamage;

        if (currentGuardHealth <= 0f)
        {
            EndGuard(guardBroken: true);
            health.ApplyGuardBreakStagger(attackerPosition, breakStunDuration);
        }

        return true;
    }

    private bool CanBlockAttackType(PlayerAttackType attackType)
    {
        return attackType switch
        {
            PlayerAttackType.Light => canGuardPlayerLightAttacks,
            PlayerAttackType.Heavy => canGuardPlayerHeavyAttacks,
            PlayerAttackType.AerialLight or PlayerAttackType.AerialHeavy =>
                canGuardPlayerAerialAttacks,
            PlayerAttackType.DashLight or PlayerAttackType.DashHeavy =>
                canGuardPlayerDashAttacks,
            _ => false
        };
    }

    private void EndGuard(bool guardBroken = false)
    {
        // A resistencia persiste entre periodos de guarda. Ela so e
        // restaurada depois de uma quebra completa, nunca ao baixar ou
        // levantar a guarda normalmente.
        if (guardBroken)
            currentGuardHealth = maxGuardHealth;
        cooldownTimer = guardCooldown;
        health?.SetGuardVisual(false);
        if (owner != null && owner.CurrentState == EnemyBase.State.Guarding)
        {
            owner.CurrentState = EnemyBase.State.Combat;
            brain?.ScheduleNextDecision();
        }
    }

    internal void CancelForAirborne()
    {
        if (IsGuarding)
            EndGuard();
    }

    internal void CancelForDodge()
    {
        if (IsGuarding)
            EndGuard();
    }
}

/// <summary>Categoria de ataque do player relevante para as defesas inimigas.</summary>
public enum PlayerAttackType
{
    None,
    Light,
    Heavy,
    AerialLight,
    AerialHeavy,
    DashLight,
    DashHeavy
}

using UnityEngine;

/// <summary>Vida, Poise, Stagger, morte e feedback de dano do inimigo.</summary>
[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour
{
    private const float UnkillableHealthRegenPerSecond = 10000f;

    [Header("Health")]
    [Tooltip("Vida maxima do inimigo.")]
    [Min(1)] [SerializeField] private int maxHealth = 100;
    [Tooltip("Impede que a vida chegue a zero; util para bonecos de teste.")]
    [SerializeField] private bool isUnkillable;

    [Header("Poise")]
    [Tooltip("Poise necessario para causar Stagger.")]
    [Min(1)] [SerializeField] private int maxPoise = 60;

    [Header("Stagger")]
    [Tooltip("Tempo em que Stagger impede o inimigo de agir.")]
    [Min(0f)] [SerializeField] private float staggerDuration = .8f;
    [Tooltip("Forca horizontal aplicada ao receber Stagger.")]
    [Min(0f)] [SerializeField] private float staggerKnockbackForce = 8f;
    [Tooltip("Duracao do deslocamento causado por Stagger.")]
    [Min(0f)] [SerializeField] private float staggerKnockbackDuration = .2f;

    [Header("Visual Feedback")]
    [Tooltip("Cor aplicada enquanto o inimigo esta bloqueando ataques.")]
    [SerializeField] private Color guardingStateColor = new(.55f, .2f, .8f, 1f);
    [Tooltip("Cor aplicada durante o windup de um ataque.")]
    [SerializeField] private Color windupStateColor = Color.yellow;
    [Tooltip("Cor aplicada durante a fase ativa de um ataque.")]
    [SerializeField] private Color activeStateColor = new(1f, .25f, .1f, 1f);
    [Tooltip("Cor aplicada durante o recovery de um ataque.")]
    [SerializeField] private Color recoveryStateColor = new(1f, .65f, .1f, 1f);
    [Tooltip("Cor aplicada enquanto o inimigo esta esquivando.")]
    [SerializeField] private Color dodgingStateColor = Color.cyan;
    [Tooltip("Cor aplicada enquanto o inimigo esta em stagger.")]
    [SerializeField] private Color staggeredStateColor = new(1f, .5f, 0f, 1f);
    [Tooltip("Cor aplicada enquanto o inimigo provoca o jogador.")]
    [SerializeField] private Color tauntingStateColor = new(1f, .25f, .7f, 1f);
    [Tooltip("Cor aplicada enquanto o inimigo espera na fila de um ataque especial.")]
    [SerializeField] private Color specialQueueStateColor = new(.2f, .8f, 1f, 1f);

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyMovement movement;
    private EnemyAttackController attacks;
    private EnemyGuard guard;
    private EnemyDodge dodge;
    private EnemyGore gore;
    private EnemyLightAttackReflector lightAttackReflector;
    private Material[] enemyMaterials = System.Array.Empty<Material>();
    private readonly System.Collections.Generic.Dictionary<Material, Color>
        originalMaterialColors = new();
    private DamageNumberSpawner damageNumbers;
    private readonly CombatPoiseState poise = new();
    private float staggerTimer;
    private bool isGuardBreakStagger;
    private bool isShowingGuardVisual;
    private float unkillableHealthRegenRemainder;

    public bool IsDead { get; private set; }
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public float CurrentPoise => poise.Current;
    public int MaxPoise => maxPoise;
    public float HealthPercent => maxHealth > 0
        ? Mathf.Clamp01((float)CurrentHealth / maxHealth) : 0f;

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyMovement locomotion, EnemyAttackController attackController,
        EnemyGuard enemyGuard, EnemyDodge enemyDodge)
    {
        owner = enemy;
        brain = enemyBrain;
        movement = locomotion;
        attacks = attackController;
        guard = enemyGuard;
        dodge = enemyDodge;
        gore = GetComponent<EnemyGore>();
        lightAttackReflector = GetComponent<EnemyLightAttackReflector>();
        CurrentHealth = maxHealth;
        poise.Initialize(maxPoise);
        CacheEnemyMaterials();
        damageNumbers = GetComponent<DamageNumberSpawner>();
        if (damageNumbers == null)
            damageNumbers = gameObject.AddComponent<DamageNumberSpawner>();
        SetStateVisual(owner.CurrentState);
    }

    public void TickPoise()
    {
        bool reacting = owner.CurrentState == EnemyBase.State.Staggered ||
            movement.IsLaunched || movement.IsLaunchedForwards;
        poise.Tick(maxPoise, !reacting);
    }

    /// <summary>
    /// Mantem bonecos marcados como invenciveis prontos para teste. A taxa e
    /// fixa de 10.000 de vida por segundo e nunca ultrapassa a vida maxima.
    /// </summary>
    public void TickHealth()
    {
        if (!isUnkillable || IsDead || CurrentHealth >= maxHealth)
        {
            unkillableHealthRegenRemainder = 0f;
            return;
        }

        float healingWithRemainder = UnkillableHealthRegenPerSecond *
            Time.deltaTime + unkillableHealthRegenRemainder;
        int healing = Mathf.FloorToInt(healingWithRemainder);
        unkillableHealthRegenRemainder = healingWithRemainder - healing;
        if (healing <= 0)
            return;

        int healthBeforeHealing = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healing);
        if (CurrentHealth != healthBeforeHealing)
            owner.RaiseHealthChanged();
    }

    public bool TakeDamage(int damage, Vector3 attackerPosition, int poiseDamage,
        out int damageDealt, out CombatPoiseHitResult poiseResult,
        bool fromPlayerAttack = false,
        PlayerAttackType playerAttackType = PlayerAttackType.None,
        float playerAttackReach = 0f,
        Vector3 impactDirection = default,
        Vector3 impactPosition = default,
        bool hasImpactPosition = false,
        Transform attacker = null)
    {
        damageDealt = 0;
        poiseResult = default;
        if (IsDead) return false;
        if (fromPlayerAttack && lightAttackReflector != null &&
            lightAttackReflector.TryReflect(playerAttackType, attacker))
            return false;
        if (fromPlayerAttack && dodge != null &&
            dodge.TryAvoidPlayerAttack(playerAttackReach, damage))
            return false;
        if (fromPlayerAttack)
            movement.CancelSlideLaunchHorizontalImpulse();
        guard?.TryBlock(playerAttackType, attackerPosition,
            ref damage, ref poiseDamage);
        int finalHitDamage = Mathf.Max(0, damage);
        int healthBeforeDamage = CurrentHealth;
        CurrentHealth -= damage;
        if (damage > 0)
            damageNumbers?.ShowDamage(damage);
        poiseResult = poise.ReceiveHit(poiseDamage, maxPoise);
        if (isGuardBreakStagger)
            poiseResult = default;

        if (CurrentHealth <= 0)
        {
            if (isUnkillable)
                CurrentHealth = 1;
            else
            {
                CurrentHealth = 0;
                damageDealt = Mathf.Max(0, healthBeforeDamage - CurrentHealth);
                brain.OnDamaged(damageDealt);
                Vector3 resolvedDirection = ResolveImpactDirection(attackerPosition,
                    impactDirection);
                Vector3 particleDirection = ResolveParticleImpactDirection(
                    resolvedDirection, impactPosition, hasImpactPosition);
                owner.RaiseHealthChanged();
                CombatParticleSystem.ReportEnemyDamage(owner, damageDealt,
                    true, particleDirection, impactPosition, hasImpactPosition, gore,
                    finalHitDamage, maxHealth);
                Die(damageDealt, resolvedDirection);
                return true;
            }
        }
        damageDealt = Mathf.Max(0, healthBeforeDamage - CurrentHealth);
        brain.OnDamaged(damageDealt);
        owner.RaiseHealthChanged();
        Vector3 resolvedHitDirection = ResolveImpactDirection(attackerPosition,
            impactDirection);
        CombatParticleSystem.ReportEnemyDamage(owner, damageDealt, false,
            ResolveParticleImpactDirection(resolvedHitDirection, impactPosition,
                hasImpactPosition),
            impactPosition, hasImpactPosition, gore);
        return true;
    }

    public void ApplyPoiseStagger(Vector3 attackerPosition, bool superStagger)
    {
        if (IsDead) return;
        dodge?.CancelDodge();
        owner.CancelTaunt();
        bool wasAlreadyStaggered = owner.CurrentState == EnemyBase.State.Staggered;
        isGuardBreakStagger = false;
        movement.ApplyKnockback(attackerPosition, staggerKnockbackForce,
            staggerKnockbackDuration);
        attacks.CancelAttack();
        brain.CancelReposition();
        staggerTimer = staggerDuration * (superStagger ? 2f : 1f);
        owner.CurrentState = EnemyBase.State.Staggered;
        if (!wasAlreadyStaggered)
            guard?.NotifyStaggerStarted();
    }

    internal void EndStaggerForGuard()
    {
        staggerTimer = 0f;
        isGuardBreakStagger = false;
    }

    internal void EndStaggerForDodge()
    {
        staggerTimer = 0f;
        isGuardBreakStagger = false;
    }

    internal void ApplyGuardBreakStagger(Vector3 attackerPosition, float duration)
    {
        if (IsDead || movement.IsLaunched)
            return;

        movement.ApplyKnockback(attackerPosition, staggerKnockbackForce,
            staggerKnockbackDuration);
        attacks.CancelAttack();
        brain.CancelReposition();
        staggerTimer = Mathf.Max(0f, duration);
        isGuardBreakStagger = staggerTimer > 0f;
        owner.CurrentState = staggerTimer > 0f
            ? EnemyBase.State.Staggered : EnemyBase.State.Combat;
    }

    /// <summary>Aplica o Stagger fixo de uma colisao contra superficie.</summary>
    internal void ApplyStaticImpactStagger(float duration)
    {
        if (IsDead) return;
        dodge?.CancelDodge();
        owner.CancelTaunt();
        attacks.CancelAttack();
        brain.CancelReposition();
        staggerTimer = duration;
        owner.CurrentState = duration > 0f
            ? EnemyBase.State.Staggered : EnemyBase.State.Combat;
    }

    internal void TickStagger()
    {
        staggerTimer -= Time.deltaTime;
        if (staggerTimer > 0f) return;
        isGuardBreakStagger = false;
        owner.CurrentState = EnemyBase.State.Combat;
    }

    public void SetUnkillable(bool value) => isUnkillable = value;

    internal void SetGuardVisual(bool isGuarding)
    {
        isShowingGuardVisual = isGuarding;
        ApplyStateVisual(owner != null
            ? owner.CurrentState : EnemyBase.State.Idle);
    }

    /// <summary>Atualiza a cor de estado em todos os renderers do modelo.</summary>
    internal void SetStateVisual(EnemyBase.State state)
    {
        ApplyStateVisual(state);
    }

    private void ApplyStateVisual(EnemyBase.State state)
    {
        if (isShowingGuardVisual || state == EnemyBase.State.Guarding)
        {
            SetEnemyColor(guardingStateColor);
            return;
        }

        switch (state)
        {
            case EnemyBase.State.Windup:
                SetEnemyColor(windupStateColor);
                break;
            case EnemyBase.State.Active:
                SetEnemyColor(activeStateColor);
                break;
            case EnemyBase.State.Recovery:
                SetEnemyColor(recoveryStateColor);
                break;
            case EnemyBase.State.Dodging:
                SetEnemyColor(dodgingStateColor);
                break;
            case EnemyBase.State.Staggered:
                SetEnemyColor(staggeredStateColor);
                break;
            case EnemyBase.State.Taunting:
                SetEnemyColor(tauntingStateColor);
                break;
            case EnemyBase.State.SpecialAttackQueue:
                SetEnemyColor(specialQueueStateColor);
                break;
            default:
                RestoreOriginalMaterialColors();
                break;
        }
    }

    private Vector3 ResolveImpactDirection(Vector3 attackerPosition,
        Vector3 impactDirection)
    {
        impactDirection.y = 0f;
        if (impactDirection.sqrMagnitude > .0001f)
            return impactDirection.normalized;

        Vector3 fallbackDirection = transform.position - attackerPosition;
        fallbackDirection.y = 0f;
        return fallbackDirection.sqrMagnitude > .0001f
            ? fallbackDirection.normalized : transform.forward;
    }

    private Vector3 ResolveParticleImpactDirection(Vector3 fallbackDirection,
        Vector3 impactPosition, bool hasImpactPosition)
    {
        if (!hasImpactPosition)
            return fallbackDirection;

        Vector3 radialDirection = transform.position - impactPosition;
        radialDirection.y = 0f;
        return radialDirection.sqrMagnitude > .0001f
            ? radialDirection.normalized
            : fallbackDirection;
    }

    private void Die(int lethalDamage, Vector3 impactDirection)
    {
        if (IsDead) return;
        IsDead = true;
        GetComponentInChildren<EnemyBodyPartDeathParticles>(true)?.EmitDeathParts(
            impactDirection, lethalDamage);
        owner.RaiseDied();
        Destroy(gameObject);
    }

    private void CacheEnemyMaterials()
    {
        System.Collections.Generic.List<Material> materials = new();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null)
            {
                foreach (Material material in renderer.materials)
                {
                    if (material == null)
                        continue;

                    materials.Add(material);
                    if (!originalMaterialColors.ContainsKey(material))
                        originalMaterialColors.Add(material, material.color);
                }
            }
        }

        enemyMaterials = materials.ToArray();
    }

    private void SetEnemyColor(Color color)
    {
        // Os visuais podem ser ligados ou montados depois da inicializacao
        // do inimigo; atualiza a lista antes de colorir para incluir todas as
        // pecas, inclusive renderers que estavam inativos durante Awake.
        CacheEnemyMaterials();
        foreach (Material material in enemyMaterials)
        {
            if (material != null)
                material.color = color;
        }
    }

    private void RestoreOriginalMaterialColors()
    {
        CacheEnemyMaterials();
        foreach (Material material in enemyMaterials)
        {
            if (material != null &&
                originalMaterialColors.TryGetValue(material, out Color color))
                material.color = color;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerState))]
public class PlayerHealth : MonoBehaviour
{
    private const string DebugGodModeKey = "ProjetoPSX.Debug.GodMode.v1";
    [Header("Vida")]
    [Tooltip("Vida maxima usada por movesets configurados com o recurso Health. Ao chegar a zero, o jogador morre.")]
    [Min(1)] public int maxHealth = 100;
    private int currentHealth;
    [Tooltip("VitalOil maximo do moveset Mechanism. Ao chegar a zero, o jogador morre.")]
    [Min(1)] [SerializeField] private int maxVitalOil = 100;
    private int currentVitalOil;
    private float pityCooldownRemaining;

    [Header("Poise")]
    [Tooltip("Poise maximo do player. Enquanto estiver acima de zero, golpes nao causam Stagger, Juggle ou Launches.")]
    [Min(1)] public int maxPoise = 100;

    [Header("Light Stagger")]
    [Tooltip("Duracao fixa do LightStagger. Nao varia com o dano ou o Poise do ataque que o causou.")]
    [Min(0f)] public float lightStaggerDuration = .8f;
    [Tooltip("Distancia horizontal fixa percorrida pelo jogador ao receber LightStagger, antes de colisoes com o cenario.")]
    [Min(0f)] public float lightStaggerDistance = .6f;
    [Tooltip("Tempo usado para percorrer a distancia de LightStagger. O empurrao desacelera ate parar durante esse intervalo.")]
    [Min(.01f)] public float lightStaggerPushDuration = .16f;

    [Header("Heavy Stagger")]
    [Tooltip("Duracao fixa do HeavyStagger. Nao varia com o dano ou o Poise do ataque que o causou.")]
    [Min(0f)] public float heavyStaggerDuration = 1.2f;
    [Tooltip("Distancia horizontal fixa percorrida pelo jogador ao receber HeavyStagger, antes de colisoes com o cenario.")]
    [Min(0f)] public float heavyStaggerDistance = 1f;
    [Tooltip("Tempo usado para percorrer a distancia de HeavyStagger. O empurrao desacelera ate parar durante esse intervalo.")]
    [Min(.01f)] public float heavyStaggerPushDuration = .22f;
    [Tooltip("Fracao da duracao total do HeavyStagger que precisa passar antes de permitir cancelar com dash ou pulo.")]
    [Range(0f, 1f)] public float heavyStaggerMinimumDurationPercent = .4f;
    [Tooltip("Permite que uma quebra de Poise lance o player para cima.")]
    public bool canBeLaunchedUp = true;
    [Tooltip("Permite que uma quebra de Poise lance o player para baixo enquanto estiver no ar.")]
    public bool canBeLaunchedDown = true;
    [Tooltip("Permite que uma quebra de Poise lance o player horizontalmente para longe do atacante ou de volta em direcao a ele.")]
    public bool canBeLaunchedForwards = true;
    [Tooltip("Permite que uma quebra de Poise mantenha o player em Juggle no ar.")]
    public bool canBeJuggled = true;

    private Material[] playerMaterials = System.Array.Empty<Material>();

    private PlayerState playerState;
    private PlayerAttackController attackController;
    private PlayerCombatStatus combatStatus;
    private PlayerMovement playerMovement;
    private PlayerMovesetController movesetController;

    private PlayerHeat playerHeat;
    private bool godMode;

    private float staggerTimer;
    private float activeStaggerDuration;
    private bool waitingForForcedReactionEnd;
    private readonly CombatPoiseState poise = new();
    private Color normalColor;

    public bool IsGodMode => godMode;
    public event System.Action<float, int, int> ResourcesLostToDamage;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public float HealthPercent => MaxHealth > 0 ? Mathf.Clamp01((float)currentHealth / MaxHealth) : 0f;
    public int CurrentVitalOil => currentVitalOil;
    public int MaxVitalOil => Mathf.Max(1, maxVitalOil);
    public float VitalOilPercent => (float)currentVitalOil / MaxVitalOil;
    public float CurrentPoise => poise.Current;
    public float CurrentStaggerRemaining => playerState != null && playerState.IsStaggered()
        ? Mathf.Max(0f, staggerTimer) : 0f;
    public bool IsHeavyStaggered => playerState != null && playerState.IsHeavyStaggered();
    public bool IsDamageImmune => IsHeavyStaggered;
    public bool CanExitHeavyStaggerEarly => IsHeavyStaggered &&
        staggerTimer <= activeStaggerDuration *
            (1f - Mathf.Clamp01(heavyStaggerMinimumDurationPercent)) + .0001f;

    private void Awake()
    {
        godMode = Debug.isDebugBuild && PlayerPrefs.GetInt(DebugGodModeKey, 0) == 1;
        currentHealth = MaxHealth;
        currentVitalOil = MaxVitalOil;
    }

    void Start()
    {
        // O renderer pertence ao VisualRoot/filho, nao a raiz que concentra
        // a simulacao. Esta referencia e apenas feedback visual opcional.
        CachePlayerMaterials();
        playerState = GetComponent<PlayerState>();
        attackController = GetComponent<PlayerAttackController>();
        combatStatus = GetComponent<PlayerCombatStatus>();
        playerMovement = GetComponent<PlayerMovement>();
        movesetController = GetComponent<PlayerMovesetController>();
        playerHeat = GetComponent<PlayerHeat>();
        poise.Initialize(maxPoise);

        if (playerMaterials.Length > 0)
            normalColor = playerMaterials[0].color;
    }

    void Update()
    {
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;

        if (!GamePauseController.IsPaused && !CutsceneController.IsPlayerControlLocked)
            pityCooldownRemaining = Mathf.Max(0f, pityCooldownRemaining - Time.deltaTime);

        bool blocksPoiseRegen = playerState.IsStaggered() ||
            playerState.IsBurnStunned() || playerState.IsBurnWalking() ||
            playerMovement.IsLaunched;
        poise.Tick(maxPoise, !blocksPoiseRegen);

        if (playerState.IsStaggered())
        {
            if (waitingForForcedReactionEnd)
            {
                bool forcedReactionActive = !playerMovement.IsGrounded ||
                    playerMovement.IsLaunched ||
                    playerMovement.IsAerialDiving ||
                    playerMovement.IsLaunchedForwards;
                if (forcedReactionActive)
                    return;

                waitingForForcedReactionEnd = false;
                EndStagger();
                return;
            }

            staggerTimer -= Time.deltaTime;

            if (staggerTimer <= 0f)
            {
                EndStagger();
            }
        }
    }

    public void DebugSetStatus(bool maximize)
    {
        if (!Debug.isDebugBuild) return;
        currentHealth = maximize ? MaxHealth : 0;
        currentVitalOil = maximize ? MaxVitalOil : 0;
        poise.SetCurrent(maximize ? maxPoise : 0f, maxPoise);
        if (!maximize && playerState != null)
            playerState.ChangeState(PlayerState.State.Dead);
    }

    public void DebugToggleGodMode()
    {
        if (!Debug.isDebugBuild) return;
        godMode = !godMode;
        PlayerPrefs.SetInt(DebugGodModeKey, godMode ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void DebugKillPlayer()
    {
        if (Debug.isDebugBuild && playerState != null && !playerState.IsDead())
            playerState.ChangeState(PlayerState.State.Dead);
    }

    /// <summary>Retorna estados temporarios de dano ao estado inicial de uma nova campanha.</summary>
    public void ResetForNewGame()
    {
        staggerTimer = 0f;
        activeStaggerDuration = 0f;
        waitingForForcedReactionEnd = false;
        poise.Initialize(maxPoise);
        currentHealth = MaxHealth;
        currentVitalOil = MaxVitalOil;
        pityCooldownRemaining = 0f;
        if (playerState != null)
            playerState.ResetForNewGame();
        SetPlayerColor(normalColor);
    }

    /// <summary>Restaura a vida ao trocar o modo de recurso do moveset.</summary>
    public void ResetHealthForResourceMode()
    {
        currentHealth = MaxHealth;
        currentVitalOil = MaxVitalOil;
    }

    /// <summary>Restaura a vida persistida e limpa uma morte anterior.</summary>
    public void RestoreSaveHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 1, MaxHealth);
        if (playerState != null && playerState.IsDead())
            playerState.ChangeState(PlayerState.State.Normal);
    }

    public void RestoreSaveVitalOil(int vitalOil)
    {
        currentVitalOil = Mathf.Clamp(vitalOil, 1, MaxVitalOil);
        pityCooldownRemaining = 0f;
    }

    public void RestoreVitalOil(int amount)
    {
        if (amount > 0 && playerState != null && !playerState.IsDead())
            currentVitalOil = Mathf.Min(MaxVitalOil, currentVitalOil + amount);
    }

    private void StartStagger(bool heavyStagger, Vector3 attackerPosition,
        bool applyStandardPush = true)
    {
        if (playerState == null || playerState.IsDead())
            return;

        playerState.ChangeState(heavyStagger
            ? PlayerState.State.HeavyStagger
            : PlayerState.State.Stagger);

        activeStaggerDuration = heavyStagger
            ? Mathf.Max(0f, heavyStaggerDuration)
            : Mathf.Max(0f, lightStaggerDuration);
        staggerTimer = activeStaggerDuration;
        waitingForForcedReactionEnd = false;
        if (playerMovement != null)
            playerMovement.CancelStaggerPush();

        if (applyStandardPush)
        {
            Vector3 pushDirection = transform.position - attackerPosition;
            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude <= .0001f)
                pushDirection = -transform.forward;
            float distance = heavyStagger
                ? heavyStaggerDistance : lightStaggerDistance;
            float pushDuration = heavyStagger
                ? heavyStaggerPushDuration : lightStaggerPushDuration;
            if (playerMovement != null)
                playerMovement.ApplyStaggerPush(pushDirection, distance,
                    Mathf.Min(activeStaggerDuration, Mathf.Max(.01f, pushDuration)));
        }

        SetPlayerColor(Color.yellow);

        if (attackController != null)
            attackController.CancelAttack();
        if (movesetController != null)
            movesetController.ResetSequence();
    }

    void EndStagger()
    {
        // Se o dano levou o jogador à morte,
        // não podemos voltar para Normal.
        if (playerState.IsDead())
            return;

        staggerTimer = 0f;
        activeStaggerDuration = 0f;
        waitingForForcedReactionEnd = false;
        if (playerMovement != null)
            playerMovement.CancelStaggerPush();

        SetPlayerColor(normalColor);

        playerState.ChangeState(
            PlayerState.State.Normal
        );
    }

    /// <summary>Encerra HeavyStagger quando dash ou pulo e permitido como cancelamento antecipado.</summary>
    public bool TryExitHeavyStaggerEarly()
    {
        if (!CanExitHeavyStaggerEarly)
            return false;

        EndStagger();
        return true;
    }

    /// <summary>Aplica o LightStagger padrao quando o jogador acerta uma defesa refletiva.</summary>
    public void ApplyReflectedLightAttackStagger(Vector3 defenderPosition)
    {
        if (playerState == null || playerState.IsDead() ||
            playerState.IsHeavyStaggered() || playerState.IsBurnStunned() ||
            playerState.IsBurnWalking())
        {
            return;
        }

        StartStagger(false, defenderPosition);
    }

    public bool TakeDamage(
        int damage,
        Vector3 attackerPosition,
        int poiseDamage,
        CombatReactionType requestedReaction,
        bool causedByEnemy = false,
        Transform attacker = null,
        bool causesHeavyStagger = false
    )
    {
        if (playerState.IsDead() || playerState.IsHeavyStaggered())
            return false;

        // Um ataque inimigo conectado mantém o jogador em combate.
        if (causedByEnemy)
        {
            if (combatStatus != null)
                combatStatus.RegisterDamageTakenFromEnemy();
            EnemyBase.NotifyPlayerDamagedByEnemy();
            if (damage > 0 && attacker != null)
            {
                EnemyBase enemy = attacker.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                    enemy.OnDamageDealtToPlayer();
            }
            SfxAudio.Play(SfxCueId.PlayerHurt,
                transform.position);
        }

        DamageNumberSpawner.ShowPlayerDamage(transform, damage);

        PlayerMovesetController activeMoveset = movesetController != null
            ? movesetController : GetComponent<PlayerMovesetController>();
        bool usesHealth = activeMoveset != null && activeMoveset.UsesHealth;
        // GodMode protege somente o recurso final. O golpe ainda confirma
        // dano, mantém o combate ativo, gera feedback e reduz Poise, podendo
        // provocar stagger ou outras reações normalmente.
        if (!godMode)
        {
            float previousHeat = playerHeat != null ? playerHeat.CurrentHeat : 0f;
            int previousHealth = currentHealth;
            int previousVitalOil = currentVitalOil;
            float remaining = Mathf.Max(0, damage);
            if (!usesHealth && playerHeat != null)
            {
                remaining *= playerHeat.HeavyDamageMultiplier;
                float absorbed = Mathf.Min(playerHeat.CurrentHeat, remaining);
                playerHeat.RemoveHeat(absorbed);
                remaining -= absorbed;
            }
            if (usesHealth)
                currentHealth = Mathf.Max(0, currentHealth - Mathf.CeilToInt(remaining));
            else
                currentVitalOil = Mathf.Max(0, currentVitalOil - ApplyPityMechanic(
                    Mathf.CeilToInt(remaining), activeMoveset != null ? activeMoveset.Moveset : null));
            float heatLost = playerHeat != null ? Mathf.Max(0f, previousHeat - playerHeat.CurrentHeat) : 0f;
            int healthLost = previousHealth - currentHealth;
            int vitalOilLost = previousVitalOil - currentVitalOil;
            if (heatLost > 0f || healthLost > 0 || vitalOilLost > 0)
                ResourcesLostToDamage?.Invoke(heatLost, healthLost, vitalOilLost);
            if (usesHealth ? currentHealth == 0 : currentVitalOil == 0)
                playerState.ChangeState(PlayerState.State.Dead);
        }
        CombatPoiseHitResult poiseHit = poise.ReceiveHit(poiseDamage, maxPoise);

        // O impacto pertence ao dano confirmado; knockback e stagger
        // continuam sendo regras separadas abaixo.
        if (causedByEnemy)
        {
            CombatImpactSystem.ReportEnemyAttackImpact(
                attacker,
                transform,
                attackerPosition,
                damage
            );
        }

        // Se chegamos a -1000,
        // PlayerHeat já colocou o jogador
        // no estado Dead.
        if (playerState.IsDead())
            return true;

        // Burn continua vulneravel a dano, mas nao pode ser substituido por
        // knockback/stagger: durante Burning o unico movimento permitido e
        // a caminhada lenta controlada por PlayerHeat.
        if (playerState.IsBurnStunned() || playerState.IsBurnWalking())
            return true;

        if (!poiseHit.CanReact)
            return true;

        ApplyPoiseReaction(poiseHit, requestedReaction, attackerPosition,
            damage, causesHeavyStagger);
        return true;
    }

    private int ApplyPityMechanic(int damage, PlayerMovesetDefinition moveset)
    {
        PlayerPityMechanicSettings pity = moveset != null ? moveset.pityMechanic : null;
        if (damage <= 0 || pity == null || !pity.enabled || pityCooldownRemaining > 0f)
            return damage;

        int limitedDamage = damage;
        if (pity.limitDamage)
            limitedDamage = Mathf.Min(limitedDamage,
                Mathf.FloorToInt(MaxVitalOil * Mathf.Clamp01(pity.maxDamagePercent)));

        bool lethal = damage >= currentVitalOil;
        if (pity.preventLethalDamage && lethal &&
            currentVitalOil >= MaxVitalOil * Mathf.Clamp01(pity.minimumVitalOilPercent))
        {
            int protectedVitalOil = Mathf.Max(1,
                Mathf.CeilToInt(MaxVitalOil * Mathf.Clamp01(pity.survivorVitalOilPercent)));
            limitedDamage = Mathf.Min(limitedDamage, Mathf.Max(0, currentVitalOil - protectedVitalOil));
        }

        if (limitedDamage < damage)
            pityCooldownRemaining = Mathf.Max(0f, pity.cooldownSeconds);
        return limitedDamage;
    }

    private void ApplyPoiseReaction(CombatPoiseHitResult poiseHit,
        CombatReactionType requestedReaction, Vector3 attackerPosition,
        int damageDealt, bool causesHeavyStagger)
    {
        bool airborne = !playerMovement.IsGrounded || playerMovement.IsLaunched;
        CombatReactionType reaction = requestedReaction switch
        {
            CombatReactionType.LaunchUp when canBeLaunchedUp =>
                CombatReactionType.LaunchUp,
            CombatReactionType.LaunchDown when airborne && canBeLaunchedDown =>
                CombatReactionType.LaunchDown,
            CombatReactionType.LaunchForwards when canBeLaunchedForwards =>
                CombatReactionType.LaunchForwards,
            CombatReactionType.LaunchBackwards when canBeLaunchedForwards =>
                CombatReactionType.LaunchBackwards,
            CombatReactionType.Juggle when airborne && canBeJuggled =>
                CombatReactionType.Juggle,
            _ => CombatReactionType.DefaultStagger
        };

        if ((reaction == CombatReactionType.LaunchForwards ||
            reaction == CombatReactionType.LaunchBackwards) && damageDealt <= 0)
            reaction = CombatReactionType.DefaultStagger;

        bool forcedReaction = reaction != CombatReactionType.DefaultStagger;
        StartStagger(causesHeavyStagger, attackerPosition,
            applyStandardPush: !forcedReaction);
        if (!forcedReaction)
            return;

        switch (reaction)
        {
            case CombatReactionType.LaunchUp:
                playerMovement.ApplyLaunchUpReaction();
                break;
            case CombatReactionType.LaunchDown:
                playerMovement.BeginAerialDive(
                    CombatPoiseSystem.Instance.CalculateLaunchForce(damageDealt));
                break;
            case CombatReactionType.LaunchForwards:
                Vector3 direction = transform.position - attackerPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude <= .0001f)
                    direction = transform.forward;
                CombatPoiseSystem rules = CombatPoiseSystem.Instance;
                playerMovement.ApplyLaunchForwards(direction.normalized,
                    rules.CalculateLaunchForce(damageDealt),
                    rules.LaunchForwardsDeceleration);
                break;
            case CombatReactionType.LaunchBackwards:
                Vector3 backwardsDirection = attackerPosition - transform.position;
                backwardsDirection.y = 0f;
                if (backwardsDirection.sqrMagnitude <= .0001f)
                    backwardsDirection = transform.forward;
                CombatPoiseSystem backwardsRules = CombatPoiseSystem.Instance;
                playerMovement.ApplyLaunchForwards(backwardsDirection.normalized,
                    backwardsRules.CalculateLaunchForce(damageDealt),
                    backwardsRules.LaunchForwardsDeceleration);
                break;
            case CombatReactionType.Juggle:
                CombatPoiseSystem juggleRules = CombatPoiseSystem.Instance;
                playerMovement.BeginAerialLight(juggleRules.JuggleLiftSpeed,
                    juggleRules.JuggleGravityMultiplier,
                    juggleRules.JuggleDuration);
                break;
        }

        if (forcedReaction)
            waitingForForcedReactionEnd = true;
    }

    private void CachePlayerMaterials()
    {
        System.Collections.Generic.List<Material> materials = new();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            materials.AddRange(renderer.materials);
        }

        playerMaterials = materials.ToArray();
    }

    private void SetPlayerColor(Color color)
    {
        foreach (Material material in playerMaterials)
        {
            if (material != null)
                material.color = color;
        }
    }

}

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [Tooltip("Vida maxima usada por movesets configurados com o recurso Health. Ao chegar a zero, o jogador morre.")]
    [Min(1)] public int maxHealth = 100;
    private int currentHealth;

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

    [Header("Damage Numbers")]
    [Tooltip("Cor dos numeros de dano recebido pelo player.")]
    [SerializeField] private Color incomingDamageNumberColor = new Color(1f, .3f, .3f);

    [Header("Reset de Progresso")]
    [Tooltip("Permite limpar todos os dados salvos mantendo Y pressionado. O progresso e apagado apenas ao completar o tempo configurado.")]
    [SerializeField] private bool holdToResetAllSavesEnabled = true;
    [Tooltip("Tempo em segundos que Y precisa ficar pressionado para apagar todos os saves, incluindo progresso, currency, opcoes e configuracoes de teclas.")]
    [Min(.1f)] [SerializeField] private float resetAllSavesHoldDuration = 3f;

    private Material[] playerMaterials = System.Array.Empty<Material>();

    private PlayerState playerState;
    private PlayerAttackController attackController;
    private PlayerCombatStatus combatStatus;
    private PlayerMovement playerMovement;
    private PlayerMovesetController movesetController;

    private PlayerHeat playerHeat;
    private DamageNumberSpawner damageNumbers;
    private bool godMode;
    private float resetAllSavesHoldElapsed;
    private bool resetAllSavesTriggered;

    private float staggerTimer;
    private float activeStaggerDuration;
    private bool waitingForForcedReactionEnd;
    private readonly CombatPoiseState poise = new();
    private Color normalColor;

    public bool IsGodMode => godMode;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public float HealthPercent => MaxHealth > 0 ? Mathf.Clamp01((float)currentHealth / MaxHealth) : 0f;
    public float CurrentPoise => poise.Current;
    public float CurrentStaggerRemaining => playerState != null && playerState.IsStaggered()
        ? Mathf.Max(0f, staggerTimer) : 0f;
    public bool IsHeavyStaggered => playerState != null && playerState.IsHeavyStaggered();
    public bool IsDamageImmune => IsHeavyStaggered;
    public bool CanExitHeavyStaggerEarly => IsHeavyStaggered &&
        staggerTimer <= activeStaggerDuration *
            (1f - Mathf.Clamp01(heavyStaggerMinimumDurationPercent)) + .0001f;
    public bool IsHoldingResetAllSaves => resetAllSavesHoldElapsed > 0f;
    public float ResetAllSavesHoldProgress => holdToResetAllSavesEnabled
        ? Mathf.Clamp01(resetAllSavesHoldElapsed / Mathf.Max(.1f, resetAllSavesHoldDuration))
        : 0f;

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
        damageNumbers = GetComponent<DamageNumberSpawner>();
        if (damageNumbers == null)
            damageNumbers = gameObject.AddComponent<DamageNumberSpawner>();
        poise.Initialize(maxPoise);
        currentHealth = MaxHealth;

        if (playerMaterials.Length > 0)
            normalColor = playerMaterials[0].color;
    }

    void Update()
    {
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;

        UpdateDebugShortcuts();

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

    private void UpdateDebugShortcuts()
    {
        if (GamePauseController.IsPaused)
        {
            resetAllSavesHoldElapsed = 0f;
            resetAllSavesTriggered = false;
            return;
        }

        UpdateResetAllSavesShortcut();

        if (GameInput.DebugGodModePressed)
            godMode = !godMode;

        if (GameInput.DebugMaxHeatPressed)
            playerHeat?.FillHeatAndSuperheatForDebug();
    }

    private void UpdateResetAllSavesShortcut()
    {
        if (!holdToResetAllSavesEnabled || CutsceneController.IsPlayerControlLocked ||
            Keyboard.current == null ||
            !Keyboard.current.yKey.isPressed)
        {
            resetAllSavesHoldElapsed = 0f;
            resetAllSavesTriggered = false;
            return;
        }

        if (resetAllSavesTriggered)
            return;

        resetAllSavesHoldElapsed += Time.unscaledDeltaTime;
        if (resetAllSavesHoldElapsed < Mathf.Max(.1f, resetAllSavesHoldDuration))
            return;

        resetAllSavesTriggered = true;
        GameSaveResetService.ResetAllSavesAndReload(gameObject);
    }

    /// <summary>Retorna estados temporarios de dano ao estado inicial de uma nova campanha.</summary>
    public void ResetForNewGame()
    {
        godMode = false;
        staggerTimer = 0f;
        activeStaggerDuration = 0f;
        waitingForForcedReactionEnd = false;
        resetAllSavesHoldElapsed = 0f;
        resetAllSavesTriggered = false;
        poise.Initialize(maxPoise);
        currentHealth = MaxHealth;
        playerState?.ResetForNewGame();
        SetPlayerColor(normalColor);
    }

    /// <summary>Restaura a vida ao trocar o modo de recurso do moveset.</summary>
    public void ResetHealthForResourceMode()
    {
        currentHealth = MaxHealth;
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
        playerMovement?.CancelStaggerPush();

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
            playerMovement?.ApplyStaggerPush(pushDirection, distance,
                Mathf.Min(activeStaggerDuration, Mathf.Max(.01f, pushDuration)));
        }

        SetPlayerColor(Color.yellow);

        attackController?.CancelAttack();
        movesetController?.ResetSequence();
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
        playerMovement?.CancelStaggerPush();

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

    public void TakeDamage(
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
            return;

        // Um ataque inimigo conectado mantém o jogador em combate.
        if (causedByEnemy)
        {
            combatStatus?.RegisterDamageTakenFromEnemy();
            EnemyBase.NotifyPlayerDamagedByEnemy();
            if (damage > 0)
                attacker?.GetComponentInParent<EnemyBase>()?.OnDamageDealtToPlayer();
            GameplayAudioSystem.Play(GameplayAudioCueId.PlayerHurt,
                transform.position);
        }

        damageNumbers?.ShowDamage(damage, incomingDamageNumberColor);

        PlayerMovesetController activeMoveset = movesetController != null
            ? movesetController : GetComponent<PlayerMovesetController>();
        bool usesHealth = activeMoveset != null && activeMoveset.UsesHealth;
        // GodMode protege somente o recurso final. O golpe ainda confirma
        // dano, mantém o combate ativo, gera feedback e reduz Poise, podendo
        // provocar stagger ou outras reações normalmente.
        if (!godMode && usesHealth)
        {
            currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, damage));
            if (currentHealth == 0)
                playerState.ChangeState(PlayerState.State.Dead);
        }
        else if (!godMode)
        {
            playerHeat?.RemoveHeat(damage);
        }
        CombatPoiseHitResult poiseHit = poise.ReceiveHit(poiseDamage, maxPoise);

        // O impacto pertence ao dano confirmado, inclusive se ele levar o
        // player a Cold extremo ou ocorrer durante Burning. Knockback e
        // Stagger continuam sendo regras separadas abaixo.
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
            return;

        // Burn continua vulneravel a dano, mas nao pode ser substituido por
        // knockback/stagger: durante Burning o unico movimento permitido e
        // a caminhada lenta controlada por PlayerHeat.
        if (playerState.IsBurnStunned() || playerState.IsBurnWalking())
            return;

        if (!poiseHit.CanReact)
            return;

        ApplyPoiseReaction(poiseHit, requestedReaction, attackerPosition,
            damage, causesHeavyStagger);
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
                playerMovement.ApplySelfLaunchUp();
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

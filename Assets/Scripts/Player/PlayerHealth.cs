using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [Header("Poise")]
    [Tooltip("Poise maximo do player. Enquanto estiver acima de zero, golpes nao causam Stagger, Juggle ou Launches.")]
    [Min(1)] public int maxPoise = 100;
    [Tooltip("Tempo base em que o player fica sem agir ao sofrer Stagger. Superstagger usa o dobro.")]
    [Min(0f)] public float staggerDuration = 0.8f;
    [Tooltip("Permite que uma quebra de Poise lance o player para cima.")]
    public bool canBeLaunchedUp = true;
    [Tooltip("Permite que uma quebra de Poise lance o player para baixo enquanto estiver no ar.")]
    public bool canBeLaunchedDown = true;
    [Tooltip("Permite que uma quebra de Poise lance o player horizontalmente.")]
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

    private Renderer playerRenderer;

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
    private bool waitingForForcedReactionEnd;
    private bool applySuperStaggerAfterForcedReaction;
    private readonly CombatPoiseState poise = new();
    private Color normalColor;

    public bool IsGodMode => godMode;
    public float CurrentPoise => poise.Current;
    public bool IsHoldingResetAllSaves => resetAllSavesHoldElapsed > 0f;
    public float ResetAllSavesHoldProgress => holdToResetAllSavesEnabled
        ? Mathf.Clamp01(resetAllSavesHoldElapsed / Mathf.Max(.1f, resetAllSavesHoldDuration))
        : 0f;

    void Start()
    {
        // O renderer pertence ao VisualRoot/filho, nao a raiz que concentra
        // a simulacao. Esta referencia e apenas feedback visual opcional.
        playerRenderer = GetComponentInChildren<Renderer>(true);
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

        if (playerRenderer != null)
            normalColor = playerRenderer.material.color;
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
                bool applySuper = applySuperStaggerAfterForcedReaction;
                applySuperStaggerAfterForcedReaction = false;
                if (applySuper)
                    StartStagger(true);
                else
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
        waitingForForcedReactionEnd = false;
        applySuperStaggerAfterForcedReaction = false;
        resetAllSavesHoldElapsed = 0f;
        resetAllSavesTriggered = false;
        poise.Initialize(maxPoise);
        playerState?.ResetForNewGame();
        if (playerRenderer != null)
            playerRenderer.material.color = normalColor;
    }

    void StartStagger(bool superStagger = false)
    {
        playerState.ChangeState(
            PlayerState.State.Stagger
        );

        staggerTimer = staggerDuration * (superStagger ? 2f : 1f);

        if (playerRenderer != null)
            playerRenderer.material.color = Color.yellow;

        attackController?.CancelAttack();
        movesetController?.ResetSequence();
    }

    void EndStagger()
    {
        // Se o dano levou o jogador à morte,
        // não podemos voltar para Normal.
        if (playerState.IsDead())
            return;

        if (playerRenderer != null)
            playerRenderer.material.color = normalColor;

        playerState.ChangeState(
            PlayerState.State.Normal
        );
    }

    public void TakeDamage(
        int damage,
        Vector3 attackerPosition,
        int poiseDamage,
        CombatReactionType requestedReaction,
        bool causedByEnemy = false,
        Transform attacker = null
    )
    {
        if (playerState.IsDead() || godMode)
            return;

        // Um ataque inimigo conectado mantém o jogador em combate.
        if (causedByEnemy)
        {
            combatStatus?.RegisterDamageTakenFromEnemy();
            EnemyBase.NotifyPlayerDamagedByEnemy();
        }

        damageNumbers?.ShowDamage(damage, incomingDamageNumberColor);

        playerHeat.RemoveHeat(damage);
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

        ApplyPoiseReaction(poiseHit, requestedReaction, attackerPosition, damage);
    }

    private void ApplyPoiseReaction(CombatPoiseHitResult poiseHit,
        CombatReactionType requestedReaction, Vector3 attackerPosition,
        int damageDealt)
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
            CombatReactionType.Juggle when airborne && canBeJuggled =>
                CombatReactionType.Juggle,
            CombatReactionType.Stagger => CombatReactionType.Stagger,
            CombatReactionType.None when airborne && canBeJuggled =>
                CombatReactionType.Juggle,
            _ => CombatReactionType.Stagger
        };

        if (reaction == CombatReactionType.LaunchForwards && damageDealt <= 0)
            reaction = CombatReactionType.Stagger;

        bool forcedReaction = reaction != CombatReactionType.Stagger;
        StartStagger(poiseHit.IsSuperStagger && !forcedReaction);
        switch (reaction)
        {
            case CombatReactionType.LaunchUp:
                playerMovement.ApplySelfLaunchUp(
                    playerMovement.GetMaximumJumpLaunchSpeed(),
                    playerMovement.gravity);
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
            case CombatReactionType.Juggle:
                CombatPoiseSystem juggleRules = CombatPoiseSystem.Instance;
                playerMovement.BeginAerialLight(juggleRules.JuggleLiftSpeed,
                    juggleRules.JuggleGravityMultiplier,
                    juggleRules.JuggleDuration);
                break;
        }

        if (forcedReaction)
        {
            waitingForForcedReactionEnd = true;
            applySuperStaggerAfterForcedReaction = poiseHit.IsSuperStagger;
        }
    }

}

using UnityEngine;

/// <summary>
/// Moveset unico do jogador. Cada no define qual botao pode continua-lo e se a
/// entrada aconteceu apos uma pausa deliberada.
/// </summary>
public class PlayerComboController : MonoBehaviour
{
    private enum ComboNode
    {
        None,
        L1,
        L2,
        L3,
        L2Prime,
        L3Prime,
        H1,
        AerialL1,
        AerialL2,
        AerialL3,
        AerialL4,
        AerialL5,
        AerialH1,
        DashL1
    }

    private enum AttackInput
    {
        Light,
        SlowLight,
        Heavy
    }

    private const float SlowLightPhaseDurationMultiplier = 5f;

    [Header("1. Global Combo Timing")]
    [Tooltip("Tempo sem apertar um botao que transforma qualquer proxima entrada em PAUSE.")]
    [Min(0f)] public float globalPauseDelay = 0.25f;
    [Tooltip("Tempo total disponivel depois de um golpe para continuar o combo.")]
    [Min(0.01f)] public float continuationWindow = 0.5f;

    [Header("2. Light Route: L1 - L2 - L3")]
    public PlayerAttackData l1 = Light("L1", 20, .10f, .12f, .20f, 1.45f, .85f, 10, .35f);
    public PlayerAttackData l2 = Light("L2", 28, .12f, .13f, .22f, 1.55f, .90f, 14, .45f);
    public PlayerAttackData l3 = Light("L3", 45, .18f, .18f, .42f, 1.75f, 1.05f, 24, .70f);

    [Header("3. Light Pause Route: L1 - PAUSE - L2' - L3'")]
    public PlayerAttackData l2Prime = Light("L2'", 35, .17f, .16f, .30f, 1.65f, 1.00f, 18, .50f);
    public PlayerAttackData l3Prime = Light("L3'", 55, .25f, .20f, .48f, 1.95f, 1.20f, 30, .85f);

    [Header("4. Heavy Attack")]
    public PlayerAttackData h1 = Heavy("H1", 35, .25f, .18f, .35f, 1.60f, 1.00f, 35, .55f);

    [Header("5. Aerial Route: L1 - L2 - L3 - L4 - L5 (Dive) / H1")]
    public PlayerAttackData aerialL1 = AerialLight("Air L1", 18, .07f, .12f, .1f, 1.30f, .80f, 8);
    public PlayerAttackData aerialL2 = AerialLight("Air L2", 24, .07f, .13f, .1f, 1.45f, .85f, 12);
    public PlayerAttackData aerialL3 = AerialLight("Air L3", 36, .07f, .17f, .1f, 1.65f, .95f, 18);
    public PlayerAttackData aerialL4 = AerialLight("Air L4", 48, .07f, .20f, .3f, 1.85f, 1.05f, 26);
    public PlayerAttackData aerialL5 = AerialDive("Air L5 (Dive)", 65, .08f, .20f, .45f, 1.95f, 1.20f, 35);
    public PlayerAttackData aerialH1 = AerialPushHeavy("Air H1", 40, .08f, .3f, .3f, 1f, 3f, 38);

    [Header("6. Dash Attack")]
    public PlayerAttackData dashL1 = DashLaunchLight("Dash L1", 35, .10f, .18f, .05f, 1.70f, 1.15f, 52, .45f);


    #region Runtime References

    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerHeat playerHeat;
    private PlayerInputBuffer inputBuffer;

    #endregion

    #region Combo State

    private ComboNode currentNode;
    private bool waitingForContinuation;
    private bool hasBufferedInput;
    private AttackInput bufferedInput;
    private bool bufferedInputIsPause;
    private float pauseTimer;
    private bool isAerialCombo;

    #endregion

    private void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        playerState = GetComponent<PlayerState>();
        playerHeat = GetComponent<PlayerHeat>();
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
    }

    private void Update()
    {
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;

        UpdatePauseTimer();
        ReadInput();
        UpdateContinuationWindow();
    }

    private void UpdatePauseTimer()
    {
        if (playerCombat == null || currentNode == ComboNode.None)
            return;

        // O tempo de Pause e de Continuation nasce no primeiro frame de
        // Active. Ele continua contando apos o ataque terminar, ate o combo
        // ser continuado ou cancelado.
        bool isPastWindup = playerCombat.IsAttacking
            ? playerCombat.CurrentAttackPhase != PlayerCombat.AttackPhase.Windup
            : waitingForContinuation;
        if (isPastWindup)
            pauseTimer += Time.deltaTime;
    }

    private void ReadInput()
    {
        TryHandleHeavyPriorityInput();
        TryHandleBufferedInput(
            PlayerBufferedInput.LightAttack,
            AttackInput.Light
        );
        TryHandleBufferedInput(
            PlayerBufferedInput.SlowLightAttack,
            AttackInput.SlowLight
        );
        TryHandleBufferedInput(
            PlayerBufferedInput.HeavyAttack,
            AttackInput.Heavy
        );
    }

    private void TryHandleHeavyPriorityInput()
    {
        if (inputBuffer == null ||
            !inputBuffer.HasBuffered(PlayerBufferedInput.HeavyAttack) ||
            playerCombat == null || playerState == null)
        {
            return;
        }

        // Um pesado nao interrompe outro pesado. Nesse caso o fluxo normal
        // permite apenas o buffer/encadeamento que ja existia.
        if (playerCombat.IsAttacking && playerCombat.IsCurrentAttackHeavy)
        {
            TryHandleBufferedInput(
                PlayerBufferedInput.HeavyAttack,
                AttackInput.Heavy
            );
            return;
        }

        bool canOverrideAttack = playerCombat.IsAttacking;
        bool canInterruptTraversal = playerState.IsDashing() ||
            playerState.IsSliding();
        if (!canOverrideAttack && !canInterruptTraversal)
        {
            TryHandleBufferedInput(
                PlayerBufferedInput.HeavyAttack,
                AttackInput.Heavy
            );
            return;
        }

        if (!inputBuffer.TryConsume(PlayerBufferedInput.HeavyAttack))
            return;

        if (canOverrideAttack && !playerCombat.InterruptForHeavyOverride())
            return;

        if (canInterruptTraversal)
            playerMovement?.InterruptForHeavyAttack();

        CancelCombo();
        Execute(
            IsAirborne() ? ComboNode.AerialH1 : ComboNode.H1,
            AttackInput.Heavy
        );
    }

    private void TryHandleBufferedInput(
        PlayerBufferedInput bufferedAction,
        AttackInput input)
    {
        if (!CanAcceptInput())
        {
            return;
        }

        if (!inputBuffer.TryConsume(bufferedAction))
            return;

        HandleInput(input);
    }

    private bool CanAcceptInput()
    {
        if (playerCombat == null || playerState == null)
            return false;

        if (playerCombat.IsAttacking)
            return currentNode != ComboNode.None && !hasBufferedInput &&
                pauseTimer < continuationWindow;

        if (playerState.IsDashing())
            return true;

        if (waitingForContinuation)
            return playerState.IsNormal();

        return playerState.IsNormal();
    }

    private void HandleInput(AttackInput input)
    {
        if (playerCombat == null || playerState == null)
            return;

        if (playerCombat.IsAttacking)
        {
            if (input == AttackInput.Heavy &&
                IsAerialLightNode(currentNode) &&
                IsAirborne())
            {
                hasBufferedInput = true;
                bufferedInput = input;
                bufferedInputIsPause = pauseTimer >= globalPauseDelay;
                playerCombat.QueueAerialLightHeavyCancel();
                return;
            }

            if (currentNode != ComboNode.None && !hasBufferedInput)
            {
                hasBufferedInput = true;
                bufferedInput = input;
                bufferedInputIsPause = pauseTimer >= globalPauseDelay;
                if (input == AttackInput.Heavy && !GetAttack(currentNode).isHeavy)
                    playerCombat.QueueRecoveryCancel();
            }
            return;
        }

        if (playerState.IsDashing())
        {
            if (playerMovement == null || !playerMovement.IsGroundDash)
                return;

            CancelCombo();
            if (IsLightInput(input))
                Execute(ComboNode.DashL1, input);
            return;
        }

        if (waitingForContinuation)
        {
            if (!isAerialCombo && IsAirborne())
            {
                // Uma rota terrestre nao pode continuar depois que o player
                // saiu do chao. O mesmo input inicia um ataque aereo novo.
                CancelCombo();
                Execute(
                    IsLightInput(input)
                        ? ComboNode.AerialL1
                        : ComboNode.AerialH1,
                    input
                );
                return;
            }

            if (isAerialCombo && !IsAirborne())
            {
                CancelCombo();
                return;
            }

            ContinueCombo(input, pauseTimer >= globalPauseDelay);
            return;
        }

        if (playerState.IsNormal())
        {
            if (IsAirborne())
            {
                Execute(
                    IsLightInput(input)
                        ? ComboNode.AerialL1
                        : ComboNode.AerialH1,
                    input
                );
            }
            else
                Execute(
                    IsLightInput(input) ? ComboNode.L1 : ComboNode.H1,
                    input
                );
        }
    }

    private void UpdateContinuationWindow()
    {
        if (!waitingForContinuation) return;
        if (!playerState.IsNormal() ||
            (isAerialCombo && !IsAirborne()) ||
            (!isAerialCombo && IsAirborne()))
        {
            CancelCombo();
            return;
        }

        if (pauseTimer >= continuationWindow) CancelCombo();
    }

    private void ContinueCombo(AttackInput input, bool paused)
    {
        ComboNode next = GetNextNode(currentNode, input, paused);
        if (next == ComboNode.None)
        {
            CancelCombo();
            Execute(
                IsAirborne()
                    ? (IsLightInput(input) ? ComboNode.AerialL1 : ComboNode.AerialH1)
                    : (IsLightInput(input) ? ComboNode.L1 : ComboNode.H1),
                input
            );
            return;
        }
        Execute(next, input);
    }

    private static ComboNode GetNextNode(ComboNode node, AttackInput input, bool paused)
    {
        switch (node)
        {
            case ComboNode.L1: return IsLightInput(input) ? (paused ? ComboNode.L2Prime : ComboNode.L2) : ComboNode.None;
            case ComboNode.L2: return IsLightInput(input) ? ComboNode.L3 : ComboNode.None;
            case ComboNode.L2Prime: return IsLightInput(input) ? ComboNode.L3Prime : ComboNode.None;
            case ComboNode.H1: return ComboNode.None;
            case ComboNode.AerialL1: return IsLightInput(input) ? ComboNode.AerialL2 : ComboNode.None;
            case ComboNode.AerialL2: return IsLightInput(input) ? ComboNode.AerialL3 : ComboNode.None;
            case ComboNode.AerialL3: return IsLightInput(input) ? ComboNode.AerialL4 : ComboNode.None;
            case ComboNode.AerialL4: return IsLightInput(input) ? ComboNode.AerialL5 : ComboNode.None;
            default: return ComboNode.None;
        }
    }

    private void Execute(ComboNode node, AttackInput input = AttackInput.Light)
    {
        PlayerAttackData attack = GetAttack(node);
        if (attack == null)
        {
            CancelCombo();
            return;
        }

        currentNode = node;
        isAerialCombo = IsAerialNode(node);
        waitingForContinuation = false;
        hasBufferedInput = false;
        bufferedInputIsPause = false;
        pauseTimer = 0f;

        bool started = playerCombat.StartAttack(
            attack,
            OnAttackFinished,
            _ => OnAttackHit(attack),
            attack.damage,
            attack.attackRadius,
            attack.isHeavy,
            input == AttackInput.SlowLight
                ? SlowLightPhaseDurationMultiplier
                : 1f,
            GetHeavyPowerTimingMultiplier(attack)
        );

        if (!started)
        {
            CancelCombo();
            return;
        }
    }

    private void OnAttackHit(PlayerAttackData attack)
    {
        if (playerHeat == null)
            return;

        if (!attack.isHeavy)
        {
            playerHeat.AddHeat(playerHeat.lightAttackHeatGeneratedOnHit);
        }
    }

    private void OnAttackFinished()
    {
        if (hasBufferedInput &&
            bufferedInput == AttackInput.Heavy &&
            IsAerialLightNode(currentNode) &&
            IsAirborne())
        {
            hasBufferedInput = false;
            Execute(ComboNode.AerialH1, AttackInput.Heavy);
            return;
        }

        if (isAerialCombo && !IsAirborne())
        {
            CancelCombo();
            return;
        }

        if (hasBufferedInput)
        {
            AttackInput input = bufferedInput;
            bool paused = bufferedInputIsPause;
            hasBufferedInput = false;
            ContinueCombo(input, paused);
            return;
        }

        if (CanContinue(currentNode) && pauseTimer < continuationWindow)
            waitingForContinuation = true;
        else CancelCombo();
    }

    private static bool CanContinue(ComboNode node)
    {
        return node == ComboNode.L1 || node == ComboNode.L2 ||
            node == ComboNode.L2Prime || node == ComboNode.AerialL1 ||
            node == ComboNode.AerialL2 || node == ComboNode.AerialL3 ||
            node == ComboNode.AerialL4;
    }

    private static bool IsLightInput(AttackInput input)
    {
        return input == AttackInput.Light || input == AttackInput.SlowLight;
    }

    private PlayerAttackData GetAttack(ComboNode node)
    {
        switch (node)
        {
            case ComboNode.L1: return l1; case ComboNode.L2: return l2; case ComboNode.L3: return l3;
            case ComboNode.L2Prime: return l2Prime; case ComboNode.L3Prime: return l3Prime;
            case ComboNode.H1: return h1;
            case ComboNode.AerialL1: return aerialL1;
            case ComboNode.AerialL2: return aerialL2;
            case ComboNode.AerialL3: return aerialL3;
            case ComboNode.AerialL4: return aerialL4;
            case ComboNode.AerialL5: return aerialL5;
            case ComboNode.AerialH1: return aerialH1;
            case ComboNode.DashL1: return dashL1;
            default: return null;
        }
    }

    public void CancelCombo()
    {
        currentNode = ComboNode.None;
        waitingForContinuation = false;
        hasBufferedInput = false;
        bufferedInputIsPause = false;
        pauseTimer = 0f;
        isAerialCombo = false;
    }

    private bool IsAirborne()
    {
        return playerMovement != null && !playerMovement.IsGrounded;
    }

    private float GetHeavyPowerTimingMultiplier(PlayerAttackData attack)
    {
        if (attack == null || !attack.isHeavy || playerHeat == null)
        {
            return 1f;
        }

        float effectiveMultiplier = Mathf.Lerp(1f, playerHeat.HeavyDamageMultiplier, 0.5f);

        return 1f / Mathf.Max(.01f, effectiveMultiplier);
    }

    private static bool IsAerialNode(ComboNode node)
    {
        return node == ComboNode.AerialL1 ||
            node == ComboNode.AerialL2 ||
            node == ComboNode.AerialL3 ||
            node == ComboNode.AerialL4 ||
            node == ComboNode.AerialL5 ||
            node == ComboNode.AerialH1;
    }

    private static bool IsAerialLightNode(ComboNode node)
    {
        return node == ComboNode.AerialL1 ||
            node == ComboNode.AerialL2 ||
            node == ComboNode.AerialL3 ||
            node == ComboNode.AerialL4;
    }

    public static PlayerComboController GetOrAdd(GameObject player)
    {
        PlayerComboController combo = player.GetComponent<PlayerComboController>();
        if (combo == null)
            return player.AddComponent<PlayerComboController>();

        // O combate depende deste leitor de input. Uma recompilacao ou uma
        // instancia salva com o checkbox desligado nao pode deixar o player
        // permanentemente sem ataques.
        if (!combo.enabled)
            combo.enabled = true;

        return combo;
    }

    private static PlayerAttackData Light(string name, int damage, float windup, float active, float recovery, float distance, float radius, int stagger, float windupActiveMoveDistance)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, staggerDamage = stagger, isHeavy = false, windupMoveDistance = windupActiveMoveDistance };
    }

    private static PlayerAttackData Heavy(string name, int damage, float windup, float active, float recovery, float distance, float radius, int stagger, float windupActiveMoveDistance)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, staggerDamage = stagger, isHeavy = true, windupMoveDistance = windupActiveMoveDistance };
    }

    private static PlayerAttackData AerialLight(string name, int damage, float windup, float active, float recovery, float distance, float radius, int stagger)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, staggerDamage = stagger, isHeavy = false, isAerial = true };
    }

    private static PlayerAttackData AerialPushHeavy(string name, int damage, float windup, float active, float recovery, float distance, float radius, int stagger)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, staggerDamage = stagger, isHeavy = true, isAerial = true, isPush = true };
    }

    private static PlayerAttackData AerialDive(string name, int damage, float windup, float active, float recovery, float distance, float radius, int stagger)
    {
        PlayerAttackData attack = AerialLight(name, damage, windup, active, recovery, distance, radius, stagger);
        attack.isDive = true;
        return attack;
    }

    private static PlayerAttackData DashLaunchLight(string name, int damage, float windup, float active, float recovery, float distance, float radius, int stagger, float windupActiveMoveDistance)
    {
        PlayerAttackData attack = Light(name, damage, windup, active, recovery, distance, radius, stagger, windupActiveMoveDistance);
        attack.isDashAttack = true;
        attack.isLaunchUp = true;
        attack.launchesPlayer = true;
        return attack;
    }
}

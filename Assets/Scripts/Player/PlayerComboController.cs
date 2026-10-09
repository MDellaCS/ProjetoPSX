using UnityEngine;
using UnityEngine.InputSystem;

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
        H2,
        H2Prime,
        H1Prime,
        L1Prime,
        L2DoublePrime,
        LightRepeat,
        HeavyRepeat,
        AerialL1,
        AerialL2,
        AerialL3,
        AerialL4,
        AerialH1,
        DashH1
    }

    private enum AttackInput
    {
        Light,
        Heavy
    }

    [Header("1. Global Combo Timing")]
    [Tooltip("Tempo sem apertar um botao que transforma qualquer proxima entrada em PAUSE.")]
    [Min(0f)] public float globalPauseDelay = 0.1f;
    [Tooltip("Tempo total disponivel depois de um golpe para continuar o combo.")]
    [Min(0.01f)] public float continuationWindow = 0.55f;

    [Tooltip("Tempo que L ou H precisa ficar pressionado para entrar no ataque repetido.")]
    [Min(0f)] public float holdToRepeatDelay = 0.3f;

    [Header("2. Light Route: L1 - L2 - L3")]
    public PlayerAttackData l1 = Light("L1", 20, .10f, .12f, .20f, 1.45f, .85f, 250, 10, .35f);
    public PlayerAttackData l2 = Light("L2", 28, .12f, .13f, .22f, 1.55f, .90f, 300, 14, .45f);
    public PlayerAttackData l3 = Light("L3", 45, .18f, .18f, .42f, 1.75f, 1.05f, 450, 24, .70f);

    [Header("3. Light Pause Route: L1 - PAUSE - L2' - L3'")]
    public PlayerAttackData l2Prime = Light("L2'", 35, .17f, .16f, .30f, 1.65f, 1.00f, 350, 18, .50f);
    public PlayerAttackData l3Prime = Light("L3'", 55, .25f, .20f, .48f, 1.95f, 1.20f, 550, 30, .85f);

    [Header("4. Heavy Route: H1 - H2 / H1 - PAUSE - H2'")]
    public PlayerAttackData h1 = Heavy("H1", 35, .25f, .18f, .35f, 1.60f, 1.00f, 500, 35, .55f);
    public PlayerAttackData h2 = LaunchHeavy("H2", 55, .35f, .22f, .55f, 1.90f, 1.25f, 700, 55, .90f);
    public PlayerAttackData h2Prime = Heavy("H2'", 70, .48f, .25f, .80f, 2.10f, 1.45f, 900, 70, 1.15f);

    [Header("5. Mixed Routes: L1 - L2 - H1' / H1 - L1' - L2''")]
    public PlayerAttackData h1Prime = LaunchHeavy("H1'", 65, .30f, .23f, .60f, 2.00f, 1.35f, 800, 65, .95f);
    public PlayerAttackData l1Prime = Light("L1'", 30, .13f, .14f, .25f, 1.55f, .90f, 300, 15, .40f);
    public PlayerAttackData l2DoublePrime = Light("L2''", 60, .25f, .20f, .50f, 1.90f, 1.25f, 550, 32, .75f);

    [Header("6. Hold To Repeat Attacks")]
    public PlayerAttackData lightRepeat = Light("L Repeat", 7, .03f, .08f, .07f, 1.25f, .65f, 30, 2, .08f);
    public PlayerAttackData heavyRepeat = Heavy("H Repeat", 20, .16f, .14f, .24f, 1.50f, .95f, 500, 20, .30f);

    [Header("7. Aerial Route: L1 - L2 - L3 - L4 / H1")]
    public PlayerAttackData aerialL1 = AerialLight("Air L1", 18, .07f, .12f, .1f, 1.30f, .80f, 150, 8);
    public PlayerAttackData aerialL2 = AerialLight("Air L2", 24, .07f, .13f, .1f, 1.45f, .85f, 180, 12);
    public PlayerAttackData aerialL3 = AerialLight("Air L3", 36, .07f, .17f, .1f, 1.65f, .95f, 250, 18);
    public PlayerAttackData aerialL4 = AerialLight("Air L4", 48, .07f, .20f, .3f, 1.85f, 1.05f, 300, 26);
    public PlayerAttackData aerialH1 = AerialHeavy("Air H1", 40, .08f, .3f, .3f, 1f, 3f, 350, 38);

    [Header("8. Dash Attacks")]
    public PlayerAttackSequence dashLightSequence = DashLightSequence();
    public PlayerAttackData dashH1 = DashLaunchHeavy("Dash H1", 35, .10f, .18f, .2f, 1.70f, 1.15f, 380, 52, .45f);


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
    private float waitTimer;
    private bool isAerialCombo;

    #endregion

    #region Hold State

    private float lightHoldTimer;
    private float heavyHoldTimer;
    private bool ignoreLightHoldUntilRelease;
    private bool ignoreHeavyHoldUntilRelease;

    #endregion

    #region Serialized Migration

    [SerializeField, HideInInspector]
    private int windupMovementDataVersion;

    [SerializeField, HideInInspector]
    private int dashAttackDataVersion;

    #endregion

    private void Awake()
    {
        EnsureWindupMovementDefaults();
        EnsureDashAttackDefaults();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        playerState = GetComponent<PlayerState>();
        playerHeat = GetComponent<PlayerHeat>();
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
    }

    private void OnValidate()
    {
        EnsureWindupMovementDefaults();
        EnsureDashAttackDefaults();
    }

    private void EnsureWindupMovementDefaults()
    {
        if (windupMovementDataVersion >= 1)
            return;

        SetWindupMovement(l1, .35f);
        SetWindupMovement(l2, .45f);
        SetWindupMovement(l3, .70f);
        SetWindupMovement(l2Prime, .50f);
        SetWindupMovement(l3Prime, .85f);
        SetWindupMovement(h1, .55f);
        SetWindupMovement(h2, .90f);
        SetWindupMovement(h2Prime, 1.15f);
        SetWindupMovement(h1Prime, .95f);
        SetWindupMovement(l1Prime, .40f);
        SetWindupMovement(l2DoublePrime, .75f);
        SetWindupMovement(lightRepeat, .08f);
        SetWindupMovement(heavyRepeat, .30f);

        windupMovementDataVersion = 1;
    }

    private void EnsureDashAttackDefaults()
    {
        if (dashAttackDataVersion >= 1)
            return;

        if (dashLightSequence == null || !dashLightSequence.HasStrikes)
            dashLightSequence = DashLightSequence();

        dashAttackDataVersion = 1;
    }

    private static void SetWindupMovement(
        PlayerAttackData attack,
        float distance)
    {
        if (attack == null)
            return;

        attack.windupMoveDistance = distance;
    }

    private void Update()
    {
        UpdateHoldTimers();
        ReadInput();
        UpdateContinuationWindow();
    }

    private void UpdateHoldTimers()
    {
        if (Mouse.current == null)
        {
            lightHoldTimer = 0f;
            heavyHoldTimer = 0f;
            ignoreLightHoldUntilRelease = false;
            ignoreHeavyHoldUntilRelease = false;
            return;
        }

        if (!Mouse.current.leftButton.isPressed)
            ignoreLightHoldUntilRelease = false;

        if (!Mouse.current.rightButton.isPressed)
            ignoreHeavyHoldUntilRelease = false;

        lightHoldTimer = Mouse.current.leftButton.isPressed
            ? lightHoldTimer + Time.deltaTime
            : 0f;
        heavyHoldTimer = Mouse.current.rightButton.isPressed
            ? heavyHoldTimer + Time.deltaTime
            : 0f;
    }

    private void ReadInput()
    {
        // Sequencias sao um unico comando. Inputs extras durante a execucao
        // nao viram continuidade nem vazam para um ataque terrestre depois.
        if (playerCombat != null && playerCombat.IsExecutingAttackSequence)
        {
            inputBuffer.TryConsume(PlayerBufferedInput.LightAttack);
            inputBuffer.TryConsume(PlayerBufferedInput.HeavyAttack);
            return;
        }

        TryHandleBufferedInput(
            PlayerBufferedInput.LightAttack,
            AttackInput.Light
        );
        TryHandleBufferedInput(
            PlayerBufferedInput.HeavyAttack,
            AttackInput.Heavy
        );
    }

    private void TryHandleBufferedInput(
        PlayerBufferedInput bufferedAction,
        AttackInput input)
    {
        if (!CanAcceptInput() ||
            !inputBuffer.TryConsume(bufferedAction))
        {
            return;
        }

        HandleInput(input);
    }

    private bool CanAcceptInput()
    {
        if (playerCombat == null || playerState == null)
            return false;

        if (playerCombat.IsAttacking)
            return currentNode != ComboNode.None && !hasBufferedInput;

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
                playerCombat.QueueAerialLightDiveCancel();
                return;
            }

            if (currentNode != ComboNode.None && !hasBufferedInput)
            {
                hasBufferedInput = true;
                bufferedInput = input;
            }
            return;
        }

        if (playerState.IsDashing())
        {
            if (playerMovement == null || !playerMovement.IsGroundDash)
                return;

            CancelCombo();
            if (input == AttackInput.Light)
                ExecuteSequence(dashLightSequence);
            else
                Execute(ComboNode.DashH1);
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
                    input == AttackInput.Light
                        ? ComboNode.AerialL1
                        : ComboNode.AerialH1
                );
                return;
            }

            if (isAerialCombo && !IsAirborne())
            {
                CancelCombo();
                return;
            }

            ContinueCombo(input, waitTimer >= globalPauseDelay);
            return;
        }

        if (playerState.IsNormal())
        {
            if (IsAirborne())
            {
                Execute(
                    input == AttackInput.Light
                        ? ComboNode.AerialL1
                        : ComboNode.AerialH1
                );
            }
            else
            {
                Execute(input == AttackInput.Light ? ComboNode.L1 : ComboNode.H1);
            }
        }
    }

    private void UpdateContinuationWindow()
    {
        if (!waitingForContinuation) return;
        if (playerCombat.IsAttacking || !playerState.IsNormal() ||
            (isAerialCombo && !IsAirborne()) ||
            (!isAerialCombo && IsAirborne()))
        {
            CancelCombo();
            return;
        }

        waitTimer += Time.deltaTime;
        if (waitTimer >= continuationWindow) CancelCombo();
    }

    private void ContinueCombo(AttackInput input, bool paused)
    {
        ComboNode next = GetNextNode(currentNode, input, paused);
        if (next == ComboNode.None) { CancelCombo(); return; }
        Execute(next);
    }

    private static ComboNode GetNextNode(ComboNode node, AttackInput input, bool paused)
    {
        switch (node)
        {
            case ComboNode.L1: return input == AttackInput.Light ? (paused ? ComboNode.L2Prime : ComboNode.L2) : ComboNode.None;
            case ComboNode.L2: return input == AttackInput.Light ? ComboNode.L3 : ComboNode.H1Prime;
            case ComboNode.L2Prime: return input == AttackInput.Light ? ComboNode.L3Prime : ComboNode.None;
            case ComboNode.H1: return input == AttackInput.Heavy ? (paused ? ComboNode.H2Prime : ComboNode.H2) : ComboNode.L1Prime;
            case ComboNode.L1Prime: return input == AttackInput.Light ? ComboNode.L2DoublePrime : ComboNode.None;
            case ComboNode.AerialL1: return input == AttackInput.Light ? ComboNode.AerialL2 : ComboNode.None;
            case ComboNode.AerialL2: return input == AttackInput.Light ? ComboNode.AerialL3 : ComboNode.None;
            case ComboNode.AerialL3: return input == AttackInput.Light ? ComboNode.AerialL4 : ComboNode.None;
            default: return ComboNode.None;
        }
    }

    private void Execute(ComboNode node)
    {
        PlayerAttackData attack = GetAttack(node);
        if (attack == null) { Debug.LogWarning($"Moveset: ataque {node} nao configurado.", this); CancelCombo(); return; }

        currentNode = node;
        isAerialCombo = IsAerialNode(node);
        waitingForContinuation = false;
        hasBufferedInput = false;
        waitTimer = 0f;

        int damage = attack.isHeavy && playerHeat != null
            ? playerHeat.CalculateHeavyDamage(attack.damage)
            : attack.damage;
        int generatedHeat = attack.isHeavy && playerHeat != null
            ? playerHeat.CalculateHeavyHeatGeneration(attack.heatGenerated)
            : 0;

        bool started = playerCombat.StartAttack(
            attack,
            OnAttackFinished,
            (_, _) => OnAttackHit(attack, generatedHeat),
            damage,
            attack.attackRadius,
            attack.isHeavy,
            generatedHeat
        );

        if (!started)
            CancelCombo();
    }

    private void OnAttackHit(PlayerAttackData attack, int generatedHeat)
    {
        if (playerHeat == null)
            return;

        if (attack.isHeavy)
            playerHeat.AddHeat(generatedHeat);
        else
            playerHeat.CoolFromLight(attack.heatRemovedOnHit);
    }

    private void ExecuteSequence(PlayerAttackSequence sequence)
    {
        if (sequence == null || !sequence.HasStrikes)
        {
            Debug.LogWarning("Moveset: sequencia de ataque nao configurada.", this);
            return;
        }

        currentNode = ComboNode.None;
        isAerialCombo = false;
        waitingForContinuation = false;
        hasBufferedInput = false;
        waitTimer = 0f;

        bool started = playerCombat.StartAttackSequence(
            sequence,
            OnAttackFinished,
            OnAttackHit
        );

        if (!started)
            CancelCombo();
    }

    private void OnAttackFinished()
    {
        if (hasBufferedInput &&
            bufferedInput == AttackInput.Heavy &&
            IsAerialLightNode(currentNode) &&
            IsAirborne())
        {
            hasBufferedInput = false;
            Execute(ComboNode.AerialH1);
            return;
        }

        if (TryStartBufferedAttackAfterHold())
            return;

        // Segurar um botao por tempo suficiente transforma qualquer no atual
        // em Hold, inclusive depois de L2, H2 ou rotas mistas.
        if (!isAerialCombo &&
            !IsDashNode(currentNode) &&
            TryGetHeldRepeatNode(out ComboNode repeatNode))
        {
            Execute(repeatNode);
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
            hasBufferedInput = false;
            ContinueCombo(input, false);
            return;
        }

        if (CanContinue(currentNode)) { waitingForContinuation = true; waitTimer = 0f; }
        else CancelCombo();
    }

    private bool TryStartBufferedAttackAfterHold()
    {
        if (!hasBufferedInput)
            return false;

        if (currentNode == ComboNode.LightRepeat &&
            bufferedInput == AttackInput.Heavy)
        {
            ignoreLightHoldUntilRelease = true;
            Execute(ComboNode.H1);
            return true;
        }

        if (currentNode == ComboNode.HeavyRepeat &&
            bufferedInput == AttackInput.Light)
        {
            ignoreHeavyHoldUntilRelease = true;
            Execute(ComboNode.L1);
            return true;
        }

        return false;
    }

    private bool TryGetHeldRepeatNode(out ComboNode repeatNode)
    {
        repeatNode = ComboNode.None;

        if (!ignoreLightHoldUntilRelease &&
            CanRepeatLight() &&
            lightHoldTimer >= holdToRepeatDelay)
        {
            repeatNode = ComboNode.LightRepeat;
            return true;
        }

        if (!ignoreHeavyHoldUntilRelease &&
            CanRepeatHeavy() &&
            heavyHoldTimer >= holdToRepeatDelay)
        {
            repeatNode = ComboNode.HeavyRepeat;
            return true;
        }

        return false;
    }

    private bool CanRepeatLight()
    {
        return currentNode != ComboNode.None;
    }

    private bool CanRepeatHeavy()
    {
        return currentNode != ComboNode.None;
    }

    private static bool CanContinue(ComboNode node)
    {
        return node == ComboNode.L1 || node == ComboNode.L2 || node == ComboNode.L2Prime || node == ComboNode.H1 || node == ComboNode.L1Prime || node == ComboNode.AerialL1 || node == ComboNode.AerialL2 || node == ComboNode.AerialL3;
    }

    private PlayerAttackData GetAttack(ComboNode node)
    {
        switch (node)
        {
            case ComboNode.L1: return l1; case ComboNode.L2: return l2; case ComboNode.L3: return l3;
            case ComboNode.L2Prime: return l2Prime; case ComboNode.L3Prime: return l3Prime;
            case ComboNode.H1: return h1; case ComboNode.H2: return h2; case ComboNode.H2Prime: return h2Prime;
            case ComboNode.H1Prime: return h1Prime; case ComboNode.L1Prime: return l1Prime;
            case ComboNode.L2DoublePrime: return l2DoublePrime;
            case ComboNode.LightRepeat: return lightRepeat;
            case ComboNode.HeavyRepeat: return heavyRepeat;
            case ComboNode.AerialL1: return aerialL1;
            case ComboNode.AerialL2: return aerialL2;
            case ComboNode.AerialL3: return aerialL3;
            case ComboNode.AerialL4: return aerialL4;
            case ComboNode.AerialH1: return aerialH1;
            case ComboNode.DashH1: return dashH1;
            default: return null;
        }
    }

    public void CancelCombo()
    {
        currentNode = ComboNode.None;
        waitingForContinuation = false;
        hasBufferedInput = false;
        waitTimer = 0f;
        isAerialCombo = false;
    }

    private bool IsAirborne()
    {
        return playerMovement != null && !playerMovement.IsGrounded;
    }

    private static bool IsAerialNode(ComboNode node)
    {
        return node == ComboNode.AerialL1 ||
            node == ComboNode.AerialL2 ||
            node == ComboNode.AerialL3 ||
            node == ComboNode.AerialL4 ||
            node == ComboNode.AerialH1;
    }

    private static bool IsAerialLightNode(ComboNode node)
    {
        return node == ComboNode.AerialL1 ||
            node == ComboNode.AerialL2 ||
            node == ComboNode.AerialL3 ||
            node == ComboNode.AerialL4;
    }

    private static bool IsDashNode(ComboNode node)
    {
        return node == ComboNode.DashH1;
    }

    public static PlayerComboController GetOrAdd(GameObject player)
    {
        PlayerComboController combo = player.GetComponent<PlayerComboController>();
        return combo != null ? combo : player.AddComponent<PlayerComboController>();
    }

    private static PlayerAttackData Light(string name, int damage, float windup, float active, float recovery, float distance, float radius, int coolHeat, int stagger, float windupMoveDistance)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, heatRemovedOnHit = coolHeat, staggerDamage = stagger, isHeavy = false, windupMoveDistance = windupMoveDistance };
    }

    private static PlayerAttackData Heavy(string name, int damage, float windup, float active, float recovery, float distance, float radius, int generateHeat, int stagger, float windupMoveDistance)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, heatGenerated = generateHeat, staggerDamage = stagger, isHeavy = true, windupMoveDistance = windupMoveDistance };
    }

    private static PlayerAttackData LaunchHeavy(string name, int damage, float windup, float active, float recovery, float distance, float radius, int generateHeat, int stagger, float windupMoveDistance)
    {
        PlayerAttackData attack = Heavy(name, damage, windup, active, recovery, distance, radius, generateHeat, stagger, windupMoveDistance);
        attack.isLaunchUp = true;
        return attack;
    }

    private static PlayerAttackData AerialLight(string name, int damage, float windup, float active, float recovery, float distance, float radius, int coolHeat, int stagger)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, heatRemovedOnHit = coolHeat, staggerDamage = stagger, isHeavy = false, isAerial = true };
    }

    private static PlayerAttackData AerialHeavy(string name, int damage, float windup, float active, float recovery, float distance, float radius, int generateHeat, int stagger)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, heatGenerated = generateHeat, staggerDamage = stagger, isHeavy = true, isAerial = true };
    }

    private static PlayerAttackData DashLight(string name, int damage, float windup, float active, float recovery, float distance, float radius, int coolHeat, int stagger, float windupMoveDistance)
    {
        PlayerAttackData attack = Light(name, damage, windup, active, recovery, distance, radius, coolHeat, stagger, windupMoveDistance);
        attack.isDashAttack = true;
        return attack;
    }

    private static PlayerAttackSequence DashLightSequence()
    {
        return new PlayerAttackSequence
        {
            sequenceName = "Dash Light",
            isDashAttack = true,
            strikes = new[]
            {
                DashLight("Dash L1", 22, .04f, .12f, .12f, 1.40f, .90f, 180, 10, 1.00f),
                DashLight("Dash L2", 28, .05f, .13f, .14f, 1.55f, .95f, 220, 14, 1.05f),
                DashLight("Dash L3", 42, .08f, .17f, .32f, 1.80f, 1.10f, 320, 24, 1.20f)
            }
        };
    }

    private static PlayerAttackData DashLaunchHeavy(string name, int damage, float windup, float active, float recovery, float distance, float radius, int generateHeat, int stagger, float windupMoveDistance)
    {
        PlayerAttackData attack = LaunchHeavy(name, damage, windup, active, recovery, distance, radius, generateHeat, stagger, windupMoveDistance);
        attack.isDashAttack = true;
        attack.launchesPlayer = true;
        return attack;
    }
}

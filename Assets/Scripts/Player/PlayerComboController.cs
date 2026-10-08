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
        HeavyRepeat
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
    public PlayerAttackData h2 = Heavy("H2", 55, .35f, .22f, .55f, 1.90f, 1.25f, 700, 55, .90f);
    public PlayerAttackData h2Prime = Heavy("H2'", 70, .48f, .25f, .80f, 2.10f, 1.45f, 900, 70, 1.15f);

    [Header("5. Mixed Routes: L1 - L2 - H1' / H1 - L1' - L2''")]
    public PlayerAttackData h1Prime = Heavy("H1'", 65, .30f, .23f, .60f, 2.00f, 1.35f, 800, 65, .95f);
    public PlayerAttackData l1Prime = Light("L1'", 30, .13f, .14f, .25f, 1.55f, .90f, 300, 15, .40f);
    public PlayerAttackData l2DoublePrime = Light("L2''", 60, .25f, .20f, .50f, 1.90f, 1.25f, 550, 32, .75f);

    [Header("6. Hold To Repeat Attacks")]
    public PlayerAttackData lightRepeat = Light("L Repeat", 7, .03f, .08f, .07f, 1.25f, .65f, 30, 2, .08f);
    public PlayerAttackData heavyRepeat = Heavy("H Repeat", 20, .16f, .14f, .24f, 1.50f, .95f, 500, 20, .30f);


    #region Runtime References

    private PlayerCombat playerCombat;
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

    #endregion

    private void Awake()
    {
        EnsureWindupMovementDefaults();
        playerCombat = GetComponent<PlayerCombat>();
        playerState = GetComponent<PlayerState>();
        playerHeat = GetComponent<PlayerHeat>();
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
    }

    private void OnValidate()
    {
        EnsureWindupMovementDefaults();
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
            if (currentNode != ComboNode.None && !hasBufferedInput)
            {
                hasBufferedInput = true;
                bufferedInput = input;
            }
            return;
        }

        if (waitingForContinuation)
        {
            ContinueCombo(input, waitTimer >= globalPauseDelay);
            return;
        }

        if (playerState.IsNormal())
            Execute(input == AttackInput.Light ? ComboNode.L1 : ComboNode.H1);
    }

    private void UpdateContinuationWindow()
    {
        if (!waitingForContinuation) return;
        if (playerCombat.IsAttacking || !playerState.IsNormal())
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
            default: return ComboNode.None;
        }
    }

    private void Execute(ComboNode node)
    {
        PlayerAttackData attack = GetAttack(node);
        if (attack == null) { Debug.LogWarning($"Moveset: ataque {node} nao configurado.", this); CancelCombo(); return; }

        currentNode = node;
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
            () => OnAttackHit(attack, generatedHeat),
            damage,
            attack.attackRadius,
            attack.isHeavy
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

    private void OnAttackFinished()
    {
        if (TryStartBufferedAttackAfterHold())
            return;

        // Segurar um botao por tempo suficiente transforma qualquer no atual
        // em Hold, inclusive depois de L2, H2 ou rotas mistas.
        if (TryGetHeldRepeatNode(out ComboNode repeatNode))
        {
            Execute(repeatNode);
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
        return node == ComboNode.L1 || node == ComboNode.L2 || node == ComboNode.L2Prime || node == ComboNode.H1 || node == ComboNode.L1Prime;
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
            default: return null;
        }
    }

    public void CancelCombo()
    {
        currentNode = ComboNode.None;
        waitingForContinuation = false;
        hasBufferedInput = false;
        waitTimer = 0f;
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
}

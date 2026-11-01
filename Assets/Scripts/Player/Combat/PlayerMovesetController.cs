using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttackController))]
public sealed class PlayerMovesetController : MonoBehaviour
{
    private readonly struct AttackGesture
    {
        public readonly PlayerAttackInput Input;
        public readonly int PressSequence;
        public readonly float PressedAt;
        public readonly bool HeldBranchConsumed;

        public AttackGesture(PlayerAttackInput input, int pressSequence,
            float pressedAt, bool heldBranchConsumed = false)
        {
            Input = input;
            PressSequence = pressSequence;
            PressedAt = pressedAt;
            HeldBranchConsumed = heldBranchConsumed;
        }

        public AttackGesture ConsumeHeldBranch() =>
            new(Input, PressSequence, PressedAt, true);
    }

    [Tooltip("Asset que contem os ataques, entradas e futuras transicoes do moveset.")]
    [SerializeField] private PlayerMovesetDefinition moveset;
    private PlayerInputBuffer inputBuffer;
    private PlayerAttackController attacks;
    private PlayerMovement movement;
    private PlayerState playerState;
    private PlayerMovesetCatalog catalog;
    private string currentNodeId;
    private float activeStartedAt;
    private PlayerMoveNode pendingNode;
    private AttackGesture currentGesture;
    private AttackGesture pendingGesture;
    private bool startingMovesetNode;

    public PlayerMovesetDefinition Moveset => moveset;
    public bool UsesHealth => moveset != null && moveset.resourceMode == PlayerResourceMode.Health;
    public bool UsesHeat => !UsesHealth;
    public string CurrentMovesetName => moveset == null
        ? "NONE"
        : string.IsNullOrWhiteSpace(moveset.displayName)
            ? moveset.name
            : moveset.displayName;

    private void Awake()
    {
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
        attacks = GetComponent<PlayerAttackController>();
        movement = GetComponent<PlayerMovement>();
        playerState = GetComponent<PlayerState>();
        catalog = PlayerMovesetCatalog.Load();
        if (catalog != null && catalog.startingMoveset != null)
            moveset = catalog.startingMoveset;
        attacks.AttackStarted += OnAttackStarted;
        attacks.AttackFinished += OnAttackFinished;
    }
    private void OnDestroy()
    {
        if (attacks == null) return;
        attacks.AttackStarted -= OnAttackStarted;
        attacks.AttackFinished -= OnAttackFinished;
    }
    private void Update()
    {
        if (GamePauseController.IsPaused ||
            CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this)) return;
        if (playerState != null && playerState.IsDead()) return;
        if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            CycleMoveset();
        if (moveset == null) return;
        if (inputBuffer.HasBuffered(PlayerBufferedInput.HeavyAttack)) TryInput(PlayerAttackInput.Heavy);
        else if (inputBuffer.HasBuffered(PlayerBufferedInput.LightAttack)) TryInput(PlayerAttackInput.Light);
        TryHeldContinuation();
    }

    private void CycleMoveset()
    {
        if (catalog == null)
            catalog = PlayerMovesetCatalog.Load();
        if (catalog?.availableMovesets == null || catalog.availableMovesets.Count == 0)
            return;

        int currentIndex = catalog.availableMovesets.IndexOf(moveset);
        for (int offset = 1; offset <= catalog.availableMovesets.Count; offset++)
        {
            int nextIndex = (currentIndex + offset) % catalog.availableMovesets.Count;
            PlayerMovesetDefinition next = catalog.availableMovesets[nextIndex];
            if (next == null || next == moveset)
                continue;

            // Limpa estado antes de cancelar, pois AttackFinished pode consumir
            // um Node pendente imediatamente.
            pendingNode = null;
            pendingGesture = default;
            currentNodeId = null;
            currentGesture = default;
            inputBuffer?.ClearBufferedInputs(ignoreNextFrame: true);
            attacks?.CancelAttack();
            moveset = next;
            GetComponent<PlayerHealth>()?.ResetHealthForResourceMode();
            GetComponent<PlayerHeat>()?.ResetHeatForResourceMode();
            return;
        }
    }

    private void TryInput(PlayerAttackInput input)
    {
        PlayerBufferedInput buffered = input == PlayerAttackInput.Heavy
            ? PlayerBufferedInput.HeavyAttack
            : PlayerBufferedInput.LightAttack;
        if (!inputBuffer.TryGetBufferedAttackGesture(buffered,
            out int pressSequence, out float pressedAt))
        {
            return;
        }

        TryInput(input, new AttackGesture(input, pressSequence, pressedAt),
            false);
    }

    private void TryInput(PlayerAttackInput input, AttackGesture gesture,
        bool heldBranch)
    {
        if (DialogueController.BlocksPlayerAttacks)
        {
            if (!heldBranch)
                inputBuffer?.ClearBufferedAttackInputs();
            return;
        }

        PlayerBufferedInput buffered = input == PlayerAttackInput.Heavy
            ? PlayerBufferedInput.HeavyAttack
            : PlayerBufferedInput.LightAttack;
        if (!heldBranch && !inputBuffer.HasBuffered(buffered))
            return;

        // Enquanto um Node ja esta esperando o fim do ataque atual, deixa o
        // buffer guardar uma acao para o proximo Node. Ela sera resolvida
        // contra o ataque correto assim que este comecar.
        if (pendingNode != null && (heldBranch ||
            input == PlayerAttackInput.Light))
            return;

        bool isGroundDash = movement != null && movement.IsGroundDash;
        PlayerMoveNode continuation = isGroundDash
            ? null
            : FindContinuation(input, heldBranch, heldBranch);
        PlayerMoveNode requestedNode = continuation ??
            (heldBranch ? null : FindEntry(input));
        if (requestedNode == null && heldBranch)
            return;
        if (attacks.IsAttacking && requestedNode?.attack != null &&
            requestedNode.attack == attacks.CurrentAttack.Definition)
        {
            // Um mesmo asset nao pode criar um cancelamento/loop para si proprio,
            // mesmo que a regra global de cancel permita Light ou Heavy Attack.
            return;
        }

        // Nao interrompe o ataque atual nem consome o input se o proximo
        // ataque ainda estiver no cooldown individual.
        if (requestedNode != null &&
            !attacks.CanStartAttack(requestedNode.attack))
        {
            return;
        }
        if (heldBranch && attacks.IsAttacking)
        {
            // Branch segurada aguarda o fim natural do ataque atual.
            pendingNode = requestedNode;
            pendingGesture = gesture.ConsumeHeldBranch();
            return;
        }
        if (attacks.IsAttacking)
        {
            PlayerAttackCancelAction cancel = input == PlayerAttackInput.Heavy ? PlayerAttackCancelAction.HeavyAttack : PlayerAttackCancelAction.LightAttack;
            // O cancelamento pesado pode encerrar o ataque sincronicamente e
            // disparar OnAttackFinished. Limpa antes o leve que ja estava
            // pendente, para ele nao iniciar entre o ataque cancelado e o
            // pesado solicitado pelo jogador.
            PlayerMoveNode pendingBeforeHeavyCancel = null;
            AttackGesture gestureBeforeHeavyCancel = default;
            bool replacesPendingContinuation = input == PlayerAttackInput.Heavy &&
                attacks.CanRequestCancel(cancel);
            if (replacesPendingContinuation)
            {
                pendingBeforeHeavyCancel = pendingNode;
                gestureBeforeHeavyCancel = pendingGesture;
                pendingNode = null;
                pendingGesture = default;
            }
            if (!attacks.RequestCancel(cancel))
            {
                if (replacesPendingContinuation)
                {
                    pendingNode = pendingBeforeHeavyCancel;
                    pendingGesture = gestureBeforeHeavyCancel;
                }
                if (continuation == null) return;
                pendingNode = continuation;
                pendingGesture = heldBranch
                    ? gesture.ConsumeHeldBranch()
                    : gesture;
                if (!heldBranch)
                    inputBuffer.TryConsume(buffered);
                return;
            }

            if (attacks.IsAttacking)
            {
                pendingNode = continuation ??
                    (heldBranch ? null : FindEntry(input));
                if (pendingNode != null)
                {
                    pendingGesture = heldBranch
                        ? gesture.ConsumeHeldBranch()
                        : gesture;
                    if (!heldBranch)
                        inputBuffer.TryConsume(buffered);
                }
                return;
            }
        }
        PlayerMoveNode node = requestedNode;
        if (input == PlayerAttackInput.Heavy &&
            (node == null || node.attack == null ||
             node.attack.context != PlayerAttackContext.Dash))
            movement?.InterruptForHeavyAttack();
        if (!StartNode(node, heldBranch
            ? gesture.ConsumeHeldBranch()
            : gesture))
        {
            return;
        }
        if (!heldBranch)
            inputBuffer.TryConsume(buffered);
    }

    private void TryHeldContinuation()
    {
        if (DialogueController.BlocksPlayerAttacks || pendingNode != null ||
            currentNodeId == null)
        {
            return;
        }

        if (!PlayerHeldBranchRules.IsEligible(
            currentGesture.PressSequence,
            inputBuffer.GetLatestAttackPressSequence(currentGesture.Input),
            inputBuffer.IsAttackButtonHeld(currentGesture.Input),
            currentGesture.HeldBranchConsumed,
            Time.time - currentGesture.PressedAt,
            moveset.heldAttackBranchThreshold))
            return;

        TryInput(currentGesture.Input, currentGesture, true);
    }

    private PlayerMoveNode FindContinuation(PlayerAttackInput input,
        bool heldBranch, bool allowOutsideContinuationWindow = false)
    {
        PlayerMoveNode current = moveset.FindNode(currentNodeId);
        if (current == null || current.attack == null) return null;
        PlayerTransitionTimingSettings timing = moveset.GetTransitionTiming(
            current.attack.kind);
        if (timing == null) return null;

        // O buffer pode ter capturado o clique durante o Windup ou enquanto
        // o Node anterior ainda aguardava sua continuacao. Avalia a janela no
        // instante em que o input pode ser resolvido contra o ataque atual.
        float sinceActive = Time.time - activeStartedAt;
        if (!allowOutsideContinuationWindow &&
            (sinceActive < timing.minimumDelayAfterWindup ||
             sinceActive > timing.continuationWindow))
        {
            return null;
        }

        bool isPauseInput = sinceActive >= moveset.globalPauseDelay;
        PlayerMoveTransition transition = PlayerMovesetDefinition.FindTransition(
            current.transitions, input, heldBranch, isPauseInput);
        return transition != null
            ? moveset.FindNode(transition.targetNodeId)
            : null;
    }

    private PlayerMoveNode FindEntry(PlayerAttackInput input)
    {
        PlayerAttackContext context = movement != null && movement.IsGroundDash
            ? PlayerAttackContext.Dash
            : movement != null && !movement.IsGrounded
                ? PlayerAttackContext.Air
                : PlayerAttackContext.Ground;
        foreach (PlayerMovesetEntry entry in moveset.entries)
            if (entry != null && entry.input == input && entry.context == context) return moveset.FindNode(entry.targetNodeId);
        return null;
    }
    private void OnAttackStarted(PlayerAttackRuntime _)
    {
        if (!startingMovesetNode)
        {
            currentNodeId = null;
            currentGesture = default;
            pendingNode = null;
            pendingGesture = default;
        }
        activeStartedAt = Time.time + (_.WindupDuration);
    }
    private void OnAttackFinished(PlayerAttackRuntime _)
    {
        if (pendingNode == null) return;
        PlayerMoveNode next = pendingNode;
        AttackGesture gesture = pendingGesture;
        pendingNode = null;
        pendingGesture = default;
        StartNode(next, gesture);
    }

    private bool StartNode(PlayerMoveNode node, AttackGesture gesture)
    {
        if (node == null || node.attack == null)
            return false;

        if (node.attack.context == PlayerAttackContext.Dash &&
            (movement == null || !movement.InterruptGroundDashForAttack()))
            return false;

        startingMovesetNode = true;
        bool started = attacks.TryStartAttack(node.attack);
        startingMovesetNode = false;
        if (!started)
            return false;
        currentNodeId = node.nodeId;
        currentGesture = gesture;
        return true;
    }

    public void ResetSequence()
    {
        currentNodeId = null;
        pendingNode = null;
        currentGesture = default;
        pendingGesture = default;
    }

    /// <summary>
    /// O pouso encerra somente uma rota aerea: remove a continuacao pendente
    /// e qualquer clique de ataque capturado no ar, sem tocar combos de chao.
    /// </summary>
    public void ResetAerialSequenceOnLanding()
    {
        PlayerMoveNode current = moveset != null
            ? moveset.FindNode(currentNodeId) : null;
        bool currentIsAir = current?.attack != null &&
            current.attack.context == PlayerAttackContext.Air;
        bool pendingIsAir = pendingNode?.attack != null &&
            pendingNode.attack.context == PlayerAttackContext.Air;
        if (!currentIsAir && !pendingIsAir)
            return;

        ResetSequence();
        inputBuffer?.ClearBufferedAttackInputs();
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttackController))]
public sealed class PlayerMovesetController : MonoBehaviour
{
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
            currentNodeId = null;
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
        if (DialogueController.BlocksPlayerAttacks)
        {
            inputBuffer?.ClearBufferedAttackInputs();
            return;
        }

        PlayerBufferedInput buffered = input == PlayerAttackInput.Heavy ? PlayerBufferedInput.HeavyAttack : PlayerBufferedInput.LightAttack;
        if (!inputBuffer.HasBuffered(buffered))
            return;

        // Enquanto um Node ja esta esperando o fim do ataque atual, deixa o
        // buffer guardar uma acao para o proximo Node. Ela sera resolvida
        // contra o ataque correto assim que este comecar.
        if (input == PlayerAttackInput.Light && pendingNode != null)
            return;

        bool isGroundDash = movement != null && movement.IsGroundDash;
        PlayerMoveNode continuation = isGroundDash
            ? null
            : FindContinuation(input);
        PlayerMoveNode requestedNode = continuation ?? FindEntry(input);
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
        if (attacks.IsAttacking)
        {
            PlayerAttackCancelAction cancel = input == PlayerAttackInput.Heavy ? PlayerAttackCancelAction.HeavyAttack : PlayerAttackCancelAction.LightAttack;
            if (!attacks.RequestCancel(cancel))
            {
                if (continuation == null) return;
                pendingNode = continuation;
                inputBuffer.TryConsume(buffered);
                return;
            }

            if (attacks.IsAttacking)
            {
                pendingNode = continuation ?? FindEntry(input);
                if (pendingNode != null)
                    inputBuffer.TryConsume(buffered);
                return;
            }
        }
        PlayerMoveNode node = requestedNode;
        if (input == PlayerAttackInput.Heavy &&
            (node == null || node.attack == null ||
             node.attack.context != PlayerAttackContext.Dash))
            movement?.InterruptForHeavyAttack();
        if (!StartNode(node)) return;
        inputBuffer.TryConsume(buffered);
    }

    private PlayerMoveNode FindContinuation(PlayerAttackInput input)
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
        if (sinceActive < timing.minimumDelayAfterWindup ||
            sinceActive > timing.continuationWindow)
        {
            return null;
        }

        bool isPauseInput = sinceActive >= moveset.globalPauseDelay;
        PlayerMoveTransition normalFallback = null;
        foreach (PlayerMoveTransition transition in current.transitions)
        {
            if (transition == null || transition.input != input) continue;
            if (transition.requiresPause == isPauseInput)
                return moveset.FindNode(transition.targetNodeId);

            // Uma rota normal continua sendo valida depois de uma pausa se
            // este Node nao tiver um caminho de Pause proprio. Assim L1 pode
            // seguir para L2 em toda a Continuation Window, enquanto L2 ainda
            // escolhe L3p quando sua transicao requiresPause existe.
            if (isPauseInput && !transition.requiresPause &&
                normalFallback == null)
            {
                normalFallback = transition;
            }
        }
        return normalFallback != null
            ? moveset.FindNode(normalFallback.targetNodeId)
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
    private void OnAttackStarted(PlayerAttackRuntime _) { activeStartedAt = Time.time + (_.WindupDuration); }
    private void OnAttackFinished(PlayerAttackRuntime _)
    {
        if (pendingNode == null) return;
        PlayerMoveNode next = pendingNode;
        pendingNode = null;
        StartNode(next);
    }

    private bool StartNode(PlayerMoveNode node)
    {
        if (node == null || node.attack == null)
            return false;

        if (node.attack.context == PlayerAttackContext.Dash &&
            (movement == null || !movement.InterruptGroundDashForAttack()))
            return false;

        if (!attacks.TryStartAttack(node.attack))
            return false;
        currentNodeId = node.nodeId;
        return true;
    }

    public void ResetSequence()
    {
        currentNodeId = null;
        pendingNode = null;
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

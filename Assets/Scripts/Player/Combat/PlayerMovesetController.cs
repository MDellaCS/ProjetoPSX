using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttackController))]
public sealed class PlayerMovesetController : MonoBehaviour
{
    [Tooltip("Asset que contem os ataques, entradas e futuras transicoes do moveset.")]
    [SerializeField] private PlayerMovesetDefinition moveset;
    private PlayerInputBuffer inputBuffer;
    private PlayerAttackController attacks;
    private PlayerMovement movement;
    private string currentNodeId;
    private float activeStartedAt;
    private PlayerMoveNode pendingNode;

    public PlayerMovesetDefinition Moveset => moveset;

    private void Awake()
    {
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
        attacks = GetComponent<PlayerAttackController>(); movement = GetComponent<PlayerMovement>();
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
        if (moveset == null || GamePauseController.IsPaused ||
            CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this)) return;
        if (inputBuffer.HasBuffered(PlayerBufferedInput.HeavyAttack)) TryInput(PlayerAttackInput.Heavy);
        else if (inputBuffer.HasBuffered(PlayerBufferedInput.LightAttack)) TryInput(PlayerAttackInput.Light);
    }

    private void TryInput(PlayerAttackInput input)
    {
        PlayerBufferedInput buffered = input == PlayerAttackInput.Heavy ? PlayerBufferedInput.HeavyAttack : PlayerBufferedInput.LightAttack;
        if (!inputBuffer.TryGetBufferedTimestamp(buffered,
            out float inputTimestamp))
            return;

        // pendingNode ja representa a unica acao de ataque armazenada para o
        // termino do golpe atual. Sem esta protecao, outro clique leve rapido
        // seria resolvido contra o Node anterior e poderia repetir ou quebrar
        // a ordem do combo antes de a proxima etapa realmente iniciar.
        // Pesados continuam livres para interromper o ataque quando suas
        // regras globais de cancelamento permitirem.
        if (input == PlayerAttackInput.Light && pendingNode != null)
        {
            inputBuffer.TryConsume(buffered);
            return;
        }

        bool isGroundDash = movement != null && movement.IsGroundDash;
        PlayerMoveNode continuation = isGroundDash
            ? null
            : FindContinuation(input, inputTimestamp);
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
        PlayerMoveNode node = continuation ?? FindEntry(input);
        if (input == PlayerAttackInput.Heavy &&
            (node == null || node.attack == null ||
             node.attack.context != PlayerAttackContext.Dash))
            movement?.InterruptForHeavyAttack();
        if (!StartNode(node)) return;
        inputBuffer.TryConsume(buffered);
    }

    private PlayerMoveNode FindContinuation(PlayerAttackInput input,
        float inputTimestamp)
    {
        PlayerMoveNode current = moveset.FindNode(currentNodeId);
        if (current == null || current.attack == null) return null;
        PlayerTransitionTimingSettings timing = moveset.GetTransitionTiming(
            current.attack.kind);
        if (timing == null) return null;

        float sinceActive = inputTimestamp - activeStartedAt;
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

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

    public PlayerMovesetDefinition Moveset => moveset;

    private void Awake()
    {
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
        attacks = GetComponent<PlayerAttackController>(); movement = GetComponent<PlayerMovement>();
        attacks.AttackStarted += OnAttackStarted;
    }
    private void OnDestroy()
    {
        if (attacks == null) return;
        attacks.AttackStarted -= OnAttackStarted;
    }
    private void Update()
    {
        if (moveset == null || CombatImpactSystem.IsGameplayFrozen(this)) return;
        if (inputBuffer.HasBuffered(PlayerBufferedInput.HeavyAttack)) TryInput(PlayerAttackInput.Heavy);
        else if (inputBuffer.HasBuffered(PlayerBufferedInput.LightAttack)) TryInput(PlayerAttackInput.Light);
    }

    private void TryInput(PlayerAttackInput input)
    {
        PlayerBufferedInput buffered = input == PlayerAttackInput.Heavy ? PlayerBufferedInput.HeavyAttack : PlayerBufferedInput.LightAttack;
        if (attacks.IsAttacking)
        {
            PlayerAttackCancelAction cancel = input == PlayerAttackInput.Heavy ? PlayerAttackCancelAction.HeavyAttack : PlayerAttackCancelAction.LightAttack;
            if (!attacks.RequestCancel(cancel)) return;
        }
        if (input == PlayerAttackInput.Heavy)
            movement?.InterruptForHeavyAttack();
        PlayerMoveNode node = FindContinuation(input) ?? FindEntry(input);
        if (node == null || !attacks.TryStartAttack(node.attack)) return;
        inputBuffer.TryConsume(buffered); currentNodeId = node.nodeId;
    }

    private PlayerMoveNode FindContinuation(PlayerAttackInput input)
    {
        PlayerMoveNode current = moveset.FindNode(currentNodeId);
        if (current == null || current.attack == null) return null;
        PlayerTransitionTimingSettings timing = moveset.GetTransitionTiming(
            current.attack.kind);
        if (timing == null) return null;
        foreach (PlayerMoveTransition transition in current.transitions)
        {
            if (transition == null || transition.input != input) continue;
            float sinceActive = Time.time - activeStartedAt;
            if (sinceActive < timing.minimumDelayAfterWindup ||
                sinceActive > timing.continuationWindow) continue;
            bool isPauseInput = sinceActive >= moveset.globalPauseDelay;
            if (transition.requiresPause != isPauseInput) continue;
            return moveset.FindNode(transition.targetNodeId);
        }
        return null;
    }

    private PlayerMoveNode FindEntry(PlayerAttackInput input)
    {
        PlayerAttackContext context = movement != null && !movement.IsGrounded ? PlayerAttackContext.Air : PlayerAttackContext.Ground;
        foreach (PlayerMovesetEntry entry in moveset.entries)
            if (entry != null && entry.input == input && entry.context == context) return moveset.FindNode(entry.targetNodeId);
        return null;
    }
    private void OnAttackStarted(PlayerAttackRuntime _) { activeStartedAt = Time.time + (_.WindupDuration); }
    public void ResetSequence() { currentNodeId = null; }
}

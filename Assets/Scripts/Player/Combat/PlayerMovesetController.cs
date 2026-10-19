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
    private float finishedAt = float.NegativeInfinity;

    public PlayerMovesetDefinition Moveset => moveset;

    private void Awake()
    {
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
        attacks = GetComponent<PlayerAttackController>(); movement = GetComponent<PlayerMovement>();
        attacks.AttackStarted += OnAttackStarted; attacks.AttackFinished += OnAttackFinished;
    }
    private void OnDestroy()
    {
        if (attacks == null) return;
        attacks.AttackStarted -= OnAttackStarted; attacks.AttackFinished -= OnAttackFinished;
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
        if (current == null) return null;
        foreach (PlayerMoveTransition transition in current.transitions)
        {
            if (transition == null || transition.input != input) continue;
            float sinceActive = Time.time - activeStartedAt;
            float sinceFinished = Time.time - finishedAt;
            if (sinceActive < transition.minimumDelayAfterWindup || sinceFinished > transition.continuationWindow) continue;
            if (transition.requiresPause && sinceActive < moveset.globalPauseDelay) continue;
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
    private void OnAttackFinished(PlayerAttackRuntime _) { finishedAt = Time.time; }
    public void ResetSequence() { currentNodeId = null; finishedAt = float.NegativeInfinity; }
}

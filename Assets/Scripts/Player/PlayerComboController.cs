using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moveset unico do jogador. Cada no define qual botao pode continua-lo e se a
/// entrada aconteceu apos uma pausa deliberada.
/// </summary>
public class PlayerComboController : MonoBehaviour
{
    private enum ComboNode { None, L1, L2, L3, L2Prime, L3Prime, H1, H2, H2Prime, H1Prime, L1Prime, L2DoublePrime }
    private enum AttackInput { Light, Heavy }

    [Header("Combo Timing")]
    [Tooltip("Tempo sem apertar um botao que transforma a proxima entrada em PAUSE.")]
    [Min(0f)] public float pauseDelay = 0.22f;
    [Tooltip("Tempo total disponivel depois de um golpe para continuar o combo.")]
    [Min(0.01f)] public float continuationWindow = 0.55f;

    [Header("Light Route: L1 - L2 - L3")]
    public PlayerAttackData l1 = Light("L1", 20, .10f, .12f, .20f, 1.45f, .85f, 250, 10);
    public PlayerAttackData l2 = Light("L2", 28, .12f, .13f, .22f, 1.55f, .90f, 300, 14);
    public PlayerAttackData l3 = Light("L3", 45, .18f, .18f, .42f, 1.75f, 1.05f, 450, 24);

    [Header("Light Pause Route: L1 - PAUSE - L2' - L3'")]
    public PlayerAttackData l2Prime = Light("L2'", 35, .17f, .16f, .30f, 1.65f, 1.00f, 350, 18);
    public PlayerAttackData l3Prime = Light("L3'", 55, .25f, .20f, .48f, 1.95f, 1.20f, 550, 30);

    [Header("Heavy Route: H1 - H2 / H1 - PAUSE - H2'")]
    public PlayerAttackData h1 = Heavy("H1", 35, .25f, .18f, .35f, 1.60f, 1.00f, 500, 35);
    public PlayerAttackData h2 = Heavy("H2", 55, .35f, .22f, .55f, 1.90f, 1.25f, 700, 55);
    public PlayerAttackData h2Prime = Heavy("H2'", 70, .48f, .25f, .80f, 2.10f, 1.45f, 900, 70);

    [Header("Mixed Routes: L1 - L2 - H1' / H1 - L1' - L2''")]
    public PlayerAttackData h1Prime = Heavy("H1'", 65, .30f, .23f, .60f, 2.00f, 1.35f, 800, 65);
    public PlayerAttackData l1Prime = Light("L1'", 30, .13f, .14f, .25f, 1.55f, .90f, 300, 15);
    public PlayerAttackData l2DoublePrime = Light("L2''", 60, .25f, .20f, .50f, 1.90f, 1.25f, 550, 32);

    private PlayerCombat combat;
    private PlayerState state;
    private PlayerHeat heat;
    private ComboNode currentNode;
    private bool waitingForContinuation;
    private bool hasBufferedInput;
    private AttackInput bufferedInput;
    private float waitTimer;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        state = GetComponent<PlayerState>();
        heat = GetComponent<PlayerHeat>();
    }

    private void Update()
    {
        ReadInput();
        UpdateContinuationWindow();
    }

    private void ReadInput()
    {
        if (Mouse.current == null) return;
        if (Mouse.current.leftButton.wasPressedThisFrame) HandleInput(AttackInput.Light);
        if (Mouse.current.rightButton.wasPressedThisFrame) HandleInput(AttackInput.Heavy);
    }

    private void HandleInput(AttackInput input)
    {
        if (combat == null || state == null) return;

        if (combat.IsAttacking)
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
            ContinueCombo(input, waitTimer >= pauseDelay);
            return;
        }

        if (state.IsNormal())
            Execute(input == AttackInput.Light ? ComboNode.L1 : ComboNode.H1);
    }

    private void UpdateContinuationWindow()
    {
        if (!waitingForContinuation) return;
        if (combat.IsAttacking || !state.IsNormal()) { CancelCombo(); return; }

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

        int damage = attack.isHeavy && heat != null ? heat.CalculateHeavyDamage(attack.damage) : attack.damage;
        int generatedHeat = attack.isHeavy && heat != null ? heat.CalculateHeavyHeatGeneration(attack.heatGenerated) : 0;
        bool started = combat.StartAttack(attack, OnAttackFinished, () => OnAttackHit(attack, generatedHeat), damage, attack.attackRadius, attack.isHeavy);
        if (!started) CancelCombo();
    }

    private void OnAttackHit(PlayerAttackData attack, int generatedHeat)
    {
        if (heat == null) return;
        if (attack.isHeavy) heat.AddHeat(generatedHeat);
        else heat.CoolFromLight(attack.heatRemovedOnHit);
    }

    private void OnAttackFinished()
    {
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
            case ComboNode.L2DoublePrime: return l2DoublePrime; default: return null;
        }
    }

    public void CancelCombo()
    {
        currentNode = ComboNode.None;
        waitingForContinuation = false;
        hasBufferedInput = false;
        waitTimer = 0f;
    }

    private static PlayerAttackData Light(string name, int damage, float windup, float active, float recovery, float distance, float radius, int coolHeat, int stagger)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, heatRemovedOnHit = coolHeat, staggerDamage = stagger, isHeavy = false };
    }

    private static PlayerAttackData Heavy(string name, int damage, float windup, float active, float recovery, float distance, float radius, int generateHeat, int stagger)
    {
        return new PlayerAttackData { attackName = name, damage = damage, windupDuration = windup, activeDuration = active, recoveryDuration = recovery, attackDistance = distance, attackRadius = radius, heatGenerated = generateHeat, staggerDamage = stagger, isHeavy = true };
    }
}

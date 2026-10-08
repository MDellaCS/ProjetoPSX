using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGuard : MonoBehaviour
{
    [Header("Guard")]
    [Min(1f)] public float maxGuard = 100f;
    [SerializeField] private float currentGuard;

    [Header("Guard Damage Normalization")]
    [Min(0f)] public float minimumGuardDamage = 1f;
    [Min(0f)] public float guardDamageRange = 99f;
    [Min(0.01f)] public float damageReference = 100f;

    [Header("Parry")]
    [Min(0f)] public float parryWindow = 0.2f;

    [Header("Guard Recovery")]
    [Min(0f)] public float recoveryDelay = 2f;
    [Min(0f)] public float recoveryPerSecond = 25f;

    [Header("Guard Break")]
    [Min(0f)] public float guardBreakDuration = 2f;
    [Min(0f)] public float guardAfterBreak = 50f;
    [Min(1f)] public float brokenDamageMultiplier = 1.5f;

    private PlayerState playerState;
    private PlayerCombat playerCombat;
    private PlayerComboController comboController;
    private PlayerInputBuffer inputBuffer;

    private float parryTimer;
    private float recoveryTimer;
    private float breakTimer;

    public float CurrentGuard => currentGuard;
    public float MaxGuard => maxGuard;
    public bool LastDefenseWasParry { get; private set; }

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
        playerCombat = GetComponent<PlayerCombat>();
        comboController = PlayerComboController.GetOrAdd(gameObject);
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);

        currentGuard = maxGuard;
    }

    void Update()
    {
        if (playerState.IsDead())
            return;

        if (playerState.IsGuardBroken())
        {
            breakTimer -= Time.deltaTime;

            if (breakTimer <= 0f)
            {
                currentGuard = Mathf.Clamp(
                    guardAfterBreak, 1f, maxGuard
                );

                playerState.ChangeState(PlayerState.State.Normal);
            }

            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (playerState.IsNormal() &&
                inputBuffer.TryConsume(PlayerBufferedInput.Block))
            {
                StartBlock();
            }

            if (playerState.IsBlocking() &&
                !keyboard.qKey.isPressed)
            {
                StopBlock();
            }
        }

        if (playerState.IsBlocking())
        {
            parryTimer = Mathf.Max(
                0f,
                parryTimer - Time.deltaTime
            );
        }

        recoveryTimer -= Time.deltaTime;

        if (recoveryTimer <= 0f &&
            !playerState.IsBlocking())
        {
            currentGuard = Mathf.Min(
                maxGuard,
                currentGuard + recoveryPerSecond * Time.deltaTime
            );
        }
    }

    void StartBlock()
    {
        playerCombat.CancelAttack();
        comboController.CancelCombo();

        playerState.ChangeState(PlayerState.State.Blocking);
        parryTimer = parryWindow;
    }

    void StopBlock()
    {
        parryTimer = 0f;
        playerState.ChangeState(PlayerState.State.Normal);
    }

    // Retorna true quando o ataque foi defendido.
    public bool TryDefend(int incomingDamage)
    {
        LastDefenseWasParry = false;

        if (!playerState.IsBlocking())
            return false;

        if (parryTimer > 0f)
        {
            LastDefenseWasParry = true;
            parryTimer = 0f;
            recoveryTimer = recoveryDelay;

            Debug.Log("PARRY!");
            return true;
        }

        float damage = Mathf.Max(0f, incomingDamage);

        float guardDamage =
            minimumGuardDamage +
            guardDamageRange *
            (damage / (damage + damageReference));

        currentGuard = Mathf.Max(
            0f,
            currentGuard - Mathf.RoundToInt(guardDamage)
        );

        recoveryTimer = recoveryDelay;

        Debug.Log("BLOCK! Guard: " + currentGuard);

        if (currentGuard <= 0f)
        {
            BreakGuard();
        }

        return true;
    }

    void BreakGuard()
    {
        parryTimer = 0f;
        breakTimer = guardBreakDuration;

        playerCombat.CancelAttack();
        comboController.CancelCombo();

        playerState.ChangeState(PlayerState.State.GuardBroken);

        Debug.Log("GUARD BREAK!");
    }

    public int CalculateIncomingDamage(int damage)
    {
        if (!playerState.IsGuardBroken())
            return damage;

        return Mathf.RoundToInt(
            damage * brokenDamageMultiplier
        );
    }
}

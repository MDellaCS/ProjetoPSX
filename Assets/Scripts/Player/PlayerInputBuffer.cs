using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerBufferedInput
{
    LightAttack,
    HeavyAttack,
    Jump,
    Dash,
    Grapple
}

/// <summary>
/// Armazena uma unica acao recente ate que ela possa ser executada.
/// A primeira acao valida ocupa o buffer; presses adicionais aguardam a proxima
/// janela, evitando que varias acoes sejam enfileiradas ao mesmo tempo.
/// </summary>
public class PlayerInputBuffer : MonoBehaviour
{
    [Header("Global Input Buffer")]
    [Tooltip("Por quanto tempo um input pressionado permanece valido.")]
    [Min(0f)] public float bufferDuration = 0.22f;

    private PlayerBufferedInput bufferedInput;
    private float expirationTime = -1f;
    private float capturedTime = -1f;
    private int lastCapturedFrame = -1;
    private int ignoreInputThroughFrame = -1;
    private PlayerState playerState;
    private PlayerSwingController swingController;
    private PlayerAttackController attackController;

    public bool HasMouseDevice => Mouse.current != null;
    public int LightPressesDetected { get; private set; }
    public int HeavyPressesDetected { get; private set; }

    private void OnEnable() => GamePauseController.PauseChanged += OnPauseChanged;
    private void OnDisable() => GamePauseController.PauseChanged -= OnPauseChanged;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
        swingController = GetComponent<PlayerSwingController>();
        attackController = GetComponent<PlayerAttackController>();
    }

    private void OnPauseChanged(bool _) => ClearBufferedInputs(ignoreNextFrame: true);

    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked)
        {
            ClearBufferedInputs();
            return;
        }
        if (playerState != null && playerState.IsSwinging())
        {
            ClearBufferedInputs();
            return;
        }
        if (playerState != null && playerState.IsGrapplePulling())
        {
            CaptureInputsForCurrentFrame();
            KeepBufferedInputAliveDuringGrapplePull();
            return;
        }
        if (DialogueController.BlocksPlayerAttacks)
            ClearBufferedAttackInputs();
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;

        CaptureInputsForCurrentFrame();
        KeepBufferedGrappleAliveDuringAttack();
    }

    public bool HasBuffered(PlayerBufferedInput input)
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked)
        {
            ClearBufferedInputs();
            return false;
        }
        if (playerState != null && playerState.IsSwinging())
        {
            ClearBufferedInputs();
            return false;
        }
        if ((input == PlayerBufferedInput.LightAttack ||
            input == PlayerBufferedInput.HeavyAttack) &&
            DialogueController.BlocksPlayerAttacks)
        {
            ClearBufferedAttackInputs();
            return false;
        }
        CaptureInputsForCurrentFrame();
        ClearExpiredInput();
        return expirationTime >= Time.time && bufferedInput == input;
    }

    public bool TryConsume(PlayerBufferedInput input)
    {
        if (!HasBuffered(input))
            return false;

        expirationTime = -1f;
        capturedTime = -1f;
        return true;
    }

    /// <summary>Reserva o unico espaco do buffer para executar Grapple ao terminar um cancelamento adiado.</summary>
    public bool TryQueueGrappleUntilAttackEnds()
    {
        if (GamePauseController.IsPaused ||
            CutsceneController.IsPlayerControlLocked ||
            attackController == null || !attackController.IsAttacking)
        {
            return false;
        }

        ClearExpiredInput();
        if (expirationTime >= Time.time)
            return false;

        bufferedInput = PlayerBufferedInput.Grapple;
        capturedTime = Time.time;
        expirationTime = Time.time + bufferDuration;
        return true;
    }

    /// <summary>Remove do buffer apenas a acao indicada, sem descartar outra acao pendente.</summary>
    public void ClearBufferedInput(PlayerBufferedInput input)
    {
        if (expirationTime < 0f || bufferedInput != input)
            return;

        expirationTime = -1f;
        capturedTime = -1f;
    }

    public bool TryGetBufferedTimestamp(PlayerBufferedInput input,
        out float timestamp)
    {
        if (!HasBuffered(input))
        {
            timestamp = -1f;
            return false;
        }

        timestamp = capturedTime;
        return true;
    }

    public static PlayerInputBuffer GetOrAdd(GameObject player)
    {
        PlayerInputBuffer buffer =
            player.GetComponent<PlayerInputBuffer>();

        if (buffer == null)
            return player.AddComponent<PlayerInputBuffer>();

        if (!buffer.enabled)
            buffer.enabled = true;

        return buffer;
    }

    /// <summary>Descarta o buffer e o input do frame atual, usado ao pausar ou retomar.</summary>
    public void ClearBufferedInputs(bool ignoreNextFrame = false)
    {
        expirationTime = -1f;
        capturedTime = -1f;
        lastCapturedFrame = Time.frameCount;
        if (ignoreNextFrame)
            ignoreInputThroughFrame = Time.frameCount + 1;
    }

    /// <summary>Remove somente ataques pendentes, preservando pulo e dash.</summary>
    public void ClearBufferedAttackInputs()
    {
        ClearExpiredInput();
        if (bufferedInput != PlayerBufferedInput.LightAttack &&
            bufferedInput != PlayerBufferedInput.HeavyAttack)
        {
            return;
        }

        expirationTime = -1f;
        capturedTime = -1f;
    }

    private void CaptureInputsForCurrentFrame()
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            Time.frameCount <= ignoreInputThroughFrame)
            return;
        if (lastCapturedFrame == Time.frameCount)
            return;

        lastCapturedFrame = Time.frameCount;

        if (!DialogueController.BlocksPlayerAttacks && GameInput.LightPressed)
        {
            LightPressesDetected++;
            Buffer(PlayerBufferedInput.LightAttack);
        }

        if (!DialogueController.BlocksPlayerAttacks && GameInput.HeavyPressed)
        {
            HeavyPressesDetected++;
            Buffer(PlayerBufferedInput.HeavyAttack);
        }

        if (GameInput.JumpPressed)
            Buffer(PlayerBufferedInput.Jump);

        if (GameInput.DashPressed)
            Buffer(PlayerBufferedInput.Dash);

        if (playerState != null && playerState.IsGrapplePulling() &&
            swingController != null && swingController.SwingPressedThisFrame)
        {
            Buffer(PlayerBufferedInput.Grapple);
        }

    }

    private void Buffer(PlayerBufferedInput input)
    {
        ClearExpiredInput();
        if (expirationTime >= Time.time)
            return;

        bufferedInput = input;
        capturedTime = Time.time;
        expirationTime = Time.time + bufferDuration;
    }

    private void ClearExpiredInput()
    {
        if (playerState != null && playerState.IsGrapplePulling() &&
            expirationTime >= 0f)
        {
            KeepBufferedInputAliveDuringGrapplePull();
            return;
        }

        if (bufferedInput == PlayerBufferedInput.Grapple &&
            attackController != null && attackController.IsAttacking &&
            expirationTime >= 0f)
        {
            KeepBufferedGrappleAliveDuringAttack();
            return;
        }

        if (expirationTime < Time.time)
        {
            expirationTime = -1f;
            capturedTime = -1f;
        }
    }

    private void KeepBufferedInputAliveDuringGrapplePull()
    {
        if (expirationTime >= 0f)
            expirationTime = Time.time + bufferDuration;
    }

    private void KeepBufferedGrappleAliveDuringAttack()
    {
        if (bufferedInput == PlayerBufferedInput.Grapple &&
            attackController != null && attackController.IsAttacking &&
            expirationTime >= 0f)
        {
            expirationTime = Time.time + bufferDuration;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerBufferedInput
{
    LightAttack,
    HeavyAttack,
    Jump,
    Dash
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

    public bool HasMouseDevice => Mouse.current != null;
    public int LightPressesDetected { get; private set; }
    public int HeavyPressesDetected { get; private set; }

    private void OnEnable() => GamePauseController.PauseChanged += OnPauseChanged;
    private void OnDisable() => GamePauseController.PauseChanged -= OnPauseChanged;

    private void OnPauseChanged(bool _) => ClearBufferedInputs(ignoreNextFrame: true);

    private void Update()
    {
        if (GamePauseController.IsPaused)
        {
            ClearBufferedInputs();
            return;
        }
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;

        CaptureInputsForCurrentFrame();
    }

    public bool HasBuffered(PlayerBufferedInput input)
    {
        if (GamePauseController.IsPaused)
        {
            ClearBufferedInputs();
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

    private void CaptureInputsForCurrentFrame()
    {
        if (GamePauseController.IsPaused || Time.frameCount <= ignoreInputThroughFrame)
            return;
        if (lastCapturedFrame == Time.frameCount)
            return;

        lastCapturedFrame = Time.frameCount;

        if (GameInput.LightPressed)
        {
            LightPressesDetected++;
            Buffer(PlayerBufferedInput.LightAttack);
        }

        if (GameInput.HeavyPressed)
        {
            HeavyPressesDetected++;
            Buffer(PlayerBufferedInput.HeavyAttack);
        }

        if (GameInput.JumpPressed)
            Buffer(PlayerBufferedInput.Jump);

        if (GameInput.DashPressed)
            Buffer(PlayerBufferedInput.Dash);

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
        if (expirationTime < Time.time)
        {
            expirationTime = -1f;
            capturedTime = -1f;
        }
    }
}

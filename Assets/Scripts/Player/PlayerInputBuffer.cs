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
    private int lastCapturedFrame = -1;

    public bool HasMouseDevice => Mouse.current != null;
    public int LightPressesDetected { get; private set; }
    public int HeavyPressesDetected { get; private set; }

    private void Update()
    {
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;

        CaptureInputsForCurrentFrame();
    }

    public bool HasBuffered(PlayerBufferedInput input)
    {
        CaptureInputsForCurrentFrame();
        ClearExpiredInput();
        return expirationTime >= Time.time && bufferedInput == input;
    }

    public bool TryConsume(PlayerBufferedInput input)
    {
        if (!HasBuffered(input))
            return false;

        expirationTime = -1f;
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

    private void CaptureInputsForCurrentFrame()
    {
        if (lastCapturedFrame == Time.frameCount)
            return;

        lastCapturedFrame = Time.frameCount;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                LightPressesDetected++;
                Buffer(PlayerBufferedInput.LightAttack);
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                HeavyPressesDetected++;
                Buffer(PlayerBufferedInput.HeavyAttack);
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame)
            Buffer(PlayerBufferedInput.Jump);

        if (keyboard.leftShiftKey.wasPressedThisFrame)
            Buffer(PlayerBufferedInput.Dash);

    }

    private void Buffer(PlayerBufferedInput input)
    {
        ClearExpiredInput();
        if (expirationTime >= Time.time)
            return;

        bufferedInput = input;
        expirationTime = Time.time + bufferDuration;
    }

    private void ClearExpiredInput()
    {
        if (expirationTime < Time.time)
            expirationTime = -1f;
    }
}

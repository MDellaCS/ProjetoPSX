using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerBufferedInput
{
    LightAttack,
    HeavyAttack,
    Jump,
    Dash,
    Block
}

/// <summary>
/// Armazena presses recentes ate que a acao correspondente possa executa-los.
/// A captura tambem acontece sob demanda para nao depender da ordem de Update.
/// </summary>
public class PlayerInputBuffer : MonoBehaviour
{
    [Header("Global Input Buffer")]
    [Tooltip("Por quanto tempo um input pressionado permanece valido.")]
    [Min(0f)] public float bufferDuration = 0.22f;

    private readonly float[] expirationTimes =
        new float[System.Enum.GetValues(typeof(PlayerBufferedInput)).Length];
    private int lastCapturedFrame = -1;

    private void Awake()
    {
        for (int index = 0; index < expirationTimes.Length; index++)
            expirationTimes[index] = -1f;
    }

    private void Update()
    {
        CaptureInputsForCurrentFrame();
    }

    public bool HasBuffered(PlayerBufferedInput input)
    {
        CaptureInputsForCurrentFrame();
        return expirationTimes[(int)input] >= Time.time;
    }

    public bool TryConsume(PlayerBufferedInput input)
    {
        if (!HasBuffered(input))
            return false;

        expirationTimes[(int)input] = -1f;
        return true;
    }

    public static PlayerInputBuffer GetOrAdd(GameObject player)
    {
        PlayerInputBuffer buffer =
            player.GetComponent<PlayerInputBuffer>();

        return buffer != null
            ? buffer
            : player.AddComponent<PlayerInputBuffer>();
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
                Buffer(PlayerBufferedInput.LightAttack);

            if (mouse.rightButton.wasPressedThisFrame)
                Buffer(PlayerBufferedInput.HeavyAttack);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame)
            Buffer(PlayerBufferedInput.Jump);

        if (keyboard.leftShiftKey.wasPressedThisFrame)
            Buffer(PlayerBufferedInput.Dash);

        if (keyboard.qKey.wasPressedThisFrame)
            Buffer(PlayerBufferedInput.Block);
    }

    private void Buffer(PlayerBufferedInput input)
    {
        expirationTimes[(int)input] = Time.time + bufferDuration;
    }
}

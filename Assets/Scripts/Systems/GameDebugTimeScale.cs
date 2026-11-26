using UnityEngine;

/// <summary>Controle temporario de velocidade global para testes de combate.</summary>
public static class GameDebugTimeScale
{
    public const float SlowMotionScale = .25f;
    public static bool IsSlowMotion { get; private set; }
    public static float CurrentScale { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        IsSlowMotion = false;
        CurrentScale = 1f;
    }

    public static void ResetForGameRestart() => ResetSession();

    /// <summary>Alterna entre tempo normal e 25% do tempo normal.</summary>
    public static void Toggle()
    {
        if (GamePauseController.IsPaused)
            return;

        SetScale(IsSlowMotion ? 1f : SlowMotionScale);
    }

    /// <summary>Define o multiplicador de tempo usado pelo debug.</summary>
    public static bool SetScale(float value)
    {
        if (GamePauseController.IsPaused || float.IsNaN(value) ||
            float.IsInfinity(value) || value < 0f)
            return false;
        CurrentScale = value;
        IsSlowMotion = !Mathf.Approximately(value, 1f);
        GameTimeScaleEffects.SetDebugTimeScale(value);
        return true;
    }
}

using UnityEngine;

/// <summary>Controle temporario de velocidade global para testes de combate.</summary>
public static class GameDebugTimeScale
{
    public const float SlowMotionScale = .25f;
    public static bool IsSlowMotion { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => IsSlowMotion = false;

    /// <summary>Alterna entre tempo normal e 25% do tempo normal.</summary>
    public static void Toggle()
    {
        if (GamePauseController.IsPaused)
            return;

        IsSlowMotion = !IsSlowMotion;
        GameTimeScaleEffects.SetDebugTimeScale(IsSlowMotion
            ? SlowMotionScale : 1f);
    }
}

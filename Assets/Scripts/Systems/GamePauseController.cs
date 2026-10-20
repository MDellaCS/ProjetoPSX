using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arbitra pedidos de pausa de menus e eventos do jogo. O tempo volta ao
/// valor anterior apenas quando todos os solicitantes liberam sua pausa.
/// </summary>
public static class GamePauseController
{
    private static readonly HashSet<object> owners = new();
    private static float timeScaleBeforePause = 1f;

    public static bool IsPaused => owners.Count > 0;
    public static event Action<bool> PauseChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        owners.Clear();
        PauseChanged = null;
        timeScaleBeforePause = 1f;
        Time.timeScale = 1f;
    }

    public static void RequestPause(object owner)
    {
        if (owner == null || !owners.Add(owner))
            return;

        if (owners.Count != 1)
            return;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        PauseChanged?.Invoke(true);
    }

    public static void ReleasePause(object owner)
    {
        if (owner == null || !owners.Remove(owner) || owners.Count > 0)
            return;

        Time.timeScale = timeScaleBeforePause;
        PauseChanged?.Invoke(false);
    }
}

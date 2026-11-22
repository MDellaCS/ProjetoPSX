using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SplashTextMessage { BossDefeated, PlayerDied }

public static class SplashTextMessages
{
    // FIFO, no máximo uma cópia de cada tipo (inclusive a que está na tela).
    // A morte fecha a fila para novas vitórias até o reinício da partida.
    private static readonly Queue<SplashTextMessage> pending = new();
    private static bool deathRequested;
    internal static bool DeathPresented { get; set; }
    internal static bool DeathCompleted { get; set; }
    internal static int Revision { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        Reset();
        SceneManager.activeSceneChanged -= SceneChanged;
        SceneManager.activeSceneChanged += SceneChanged;
    }

    private static void SceneChanged(Scene previous, Scene next) => Reset();

    public static bool Request(SplashTextMessage message)
    {
        if (deathRequested || (message != SplashTextMessage.BossDefeated &&
            message != SplashTextMessage.PlayerDied) || pending.Contains(message))
            return false;
        pending.Enqueue(message);
        deathRequested = message == SplashTextMessage.PlayerDied;
        return true;
    }

    internal static bool TryPeek(out SplashTextMessage message)
    {
        message = pending.Count > 0 ? pending.Peek() : default;
        return pending.Count > 0;
    }

    internal static void Complete() => pending.Dequeue();

    internal static void Reset()
    {
        pending.Clear();
        deathRequested = false;
        DeathPresented = false;
        DeathCompleted = false;
        Revision++;
    }
}

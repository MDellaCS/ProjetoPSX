using UnityEngine;

/// <summary>Atualiza o tempo de jogo sem depender de um objeto de cena.</summary>
public sealed class GameSaveRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        GameSaveRuntime existing = FindAnyObjectByType<GameSaveRuntime>(FindObjectsInactive.Include);
        if (existing != null) return;
        GameObject host = new("Game Save Runtime (Persistent)");
        DontDestroyOnLoad(host);
        host.AddComponent<GameSaveRuntime>();
    }

    private void Update() => GameSaveService.Tick(Time.deltaTime);
}

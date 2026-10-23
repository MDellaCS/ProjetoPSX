using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Garante que o botao Play sempre inicie pelo Bootstrap, preservando a cena
/// atualmente aberta apenas como contexto de edicao.
/// </summary>
[InitializeOnLoad]
internal static class BootstrapPlayModeStartScene
{
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

    static BootstrapPlayModeStartScene()
    {
        EditorApplication.delayCall += SetBootstrapAsPlayModeStartScene;
    }

    [MenuItem("Projeto PSX/Play Mode/Usar Bootstrap como cena inicial")]
    private static void SetBootstrapAsPlayModeStartScene()
    {
        SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(
            BootstrapScenePath);
        if (bootstrap != null)
            EditorSceneManager.playModeStartScene = bootstrap;
    }
}

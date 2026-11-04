using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Garante que o botao Play sempre inicie pelo Bootstrap, preservando a cena
/// atualmente aberta apenas como contexto de edicao.
/// </summary>
[InitializeOnLoad]
internal static class BootstrapPlayModeStartScene
{
    private const string ScenesFolder = "Assets/Scenes";

    static BootstrapPlayModeStartScene()
    {
        EditorApplication.delayCall += SetBootstrapAsPlayModeStartScene;
    }

    [MenuItem("Projeto PSX/Play Mode/Usar Bootstrap como cena inicial")]
    private static void SetBootstrapAsPlayModeStartScene()
    {
        if (EditorPrefs.GetBool("SelfLaunchDurationRegression.Run"))
            return;

        SceneAsset bootstrap = null;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene",
            new[] { ScenesFolder });
        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(
                scenePath);
            if (sceneName.IndexOf("Bootstrap",
                    System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            break;
        }

        if (bootstrap != null)
            EditorSceneManager.playModeStartScene = bootstrap;
    }
}

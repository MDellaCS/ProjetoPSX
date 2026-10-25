using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Carrega fases sem recriar o GameRoot. Depois da carga, posiciona o Player
/// persistente no ScenePlayerSpawnPoint da nova fase.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameSceneLoader : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Raiz persistente do Player. Se vazio, encontra o objeto com a tag Player quando uma cena termina de carregar.")]
    [SerializeField] private Transform player;
    [Tooltip("ID do spawn usado quando o carregamento nao informou um ID especifico. Deixe vazio para usar o spawn marcado como padrao.")]
    [SerializeField] private string defaultSpawnId;

    [Header("Menu Principal")]
    [Tooltip("Nome da cena inicial do menu. O GameSceneLoader abre esta cena ao iniciar a sessao.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("Posicao mundial da camera estatica que enquadra a cena do menu.")]
    [SerializeField] private Vector3 mainMenuCameraPosition = new(0f, 5f, -10f);
    [Tooltip("Rotacao mundial em graus da camera estatica do menu.")]
    [SerializeField] private Vector3 mainMenuCameraEulerAngles = new(18f, 0f, 0f);
    [Tooltip("Nome do objeto com CinemachineCamera na cena do menu. A CinemachineCamera usa a posicao, rotacao e lente configuradas nela; sem esse objeto, usa os valores estaticos acima.")]
    [SerializeField] private string mainMenuCameraObjectName = "MenuCameraPose";
    [Tooltip("Nome do objeto da cena do menu que contem o CutsceneController iniciado pelo botao Novo Jogo.")]
    [SerializeField] private string newGameCutsceneObjectName = "NewGameIntro";

    [Header("Atalhos de Teste")]
    [Tooltip("Permite alternar com F9 entre as cenas encontradas em Assets/Scenes, exceto Bootstrap.")]
    [SerializeField] private bool enableDebugSceneShortcuts = true;
    [Tooltip("Cena que pode ser carregada por LoadGameplayScene, por exemplo ao final de uma Timeline.")]
    [SerializeField] private string gameplaySceneName;

    private bool isLoading;
    private string pendingSpawnId;
    private List<string> sceneCyclePaths;

    public bool IsLoading => isLoading;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private IEnumerator Start()
    {
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            SetGameplayPanelsEnabled(false);
            LoadScene(mainMenuSceneName);
            yield break;
        }

        // A primeira cena ja estava carregada quando o GameRoot nasceu, logo
        // ela nao dispara sceneLoaded para este componente. Esperar um frame
        // garante que todos os Spawn Points dela ja executaram Awake/OnEnable.
        yield return null;
        PlacePlayerAtSceneSpawn(SceneManager.GetActiveScene(), defaultSpawnId);
    }

    private void Update()
    {
        if (!enableDebugSceneShortcuts || isLoading || Keyboard.current == null)
            return;

        if (Keyboard.current.f9Key.wasPressedThisFrame)
            LoadNextSceneInFolder();
    }

    private void LoadNextSceneInFolder()
    {
        sceneCyclePaths ??= FindSceneCyclePaths();
        if (sceneCyclePaths.Count == 0)
            return;

        string currentPath = SceneManager.GetActiveScene().path;
        int currentIndex = sceneCyclePaths.IndexOf(currentPath);
        int nextIndex = (currentIndex + 1) % sceneCyclePaths.Count;
        LoadScene(sceneCyclePaths[nextIndex]);
    }

    private static List<string> FindSceneCyclePaths()
    {
        var paths = new List<string>();
#if UNITY_EDITOR
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsCycleScene(path))
                paths.Add(path);
        }
#else
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.StartsWith("Assets/Scenes/", System.StringComparison.OrdinalIgnoreCase) &&
                IsCycleScene(path))
                paths.Add(path);
        }
#endif
        paths.Sort(System.StringComparer.OrdinalIgnoreCase);
        return paths;
    }

    private static bool IsCycleScene(string path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Path.GetFileNameWithoutExtension(path), "Bootstrap",
            System.StringComparison.OrdinalIgnoreCase);

    /// <summary>Carrega a fase principal configurada. Pode ser chamado por UnityEvent.</summary>
    public void LoadGameplayScene() => LoadScene(gameplaySceneName);

    /// <summary>Recarrega somente a fase atual; GameRoot e Player permanecem.</summary>
    public void ReloadCurrentScene() =>
        LoadScene(SceneManager.GetActiveScene().name, defaultSpawnId);

    public void LoadScene(string sceneName) => LoadScene(sceneName, defaultSpawnId);

    public void LoadScene(string sceneName, string spawnId)
    {
        if (isLoading || string.IsNullOrWhiteSpace(sceneName) ||
            !CanLoadScene(sceneName))
        {
            return;
        }

        pendingSpawnId = spawnId;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;
        AsyncOperation operation;
#if UNITY_EDITOR
        string scenePath = ResolveScenePath(sceneName);
        operation = !string.IsNullOrEmpty(scenePath)
            ? EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath,
                new LoadSceneParameters(LoadSceneMode.Single))
            : SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
#else
        operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
#endif
        if (operation != null)
            yield return operation;
        isLoading = false;
    }

    private static bool CanLoadScene(string sceneNameOrPath)
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(ResolveScenePath(sceneNameOrPath)))
            return true;
#endif
        return Application.CanStreamedLevelBeLoaded(sceneNameOrPath);
    }

#if UNITY_EDITOR
    private static string ResolveScenePath(string sceneNameOrPath)
    {
        if (sceneNameOrPath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase) &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneNameOrPath) != null)
            return sceneNameOrPath;

        foreach (string path in FindSceneCyclePaths())
        {
            if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneNameOrPath,
                    System.StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }
#endif

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        PlacePlayerAtSceneSpawn(scene, pendingSpawnId);
        BindSceneCutscenesToPersistentCamera(scene);
        pendingSpawnId = null;
    }

    private void PlacePlayerAtSceneSpawn(Scene scene, string spawnId)
    {
        ResolvePlayer();
        ScenePlayerSpawnPoint spawn = ScenePlayerSpawnPoint.FindForScene(scene,
            spawnId);
        if (player != null && spawn != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
                controller.enabled = false;
            player.SetPositionAndRotation(spawn.transform.position,
                spawn.transform.rotation);
            if (controllerWasEnabled)
                controller.enabled = true;
        }

        CameraFollow cameraFollow = CameraFollow.Instance != null
            ? CameraFollow.Instance : FindAnyObjectByType<CameraFollow>();
        bool isMainMenu = string.Equals(scene.name, mainMenuSceneName,
            System.StringComparison.OrdinalIgnoreCase);
        SetGameplayPanelsEnabled(!isMainMenu);

        PauseMenuHUD pauseMenu = FindAnyObjectByType<PauseMenuHUD>();
        if (isMainMenu)
        {
            CinemachineCamera menuCamera = FindSceneMenuCamera(scene);
            if (menuCamera != null)
                cameraFollow?.EnterMenuCamera(menuCamera);
            else
                SetMenuCameraPose(cameraFollow);
            pauseMenu?.ShowMainMenu(BeginNewGame);
        }
        else
        {
            pauseMenu?.CloseMainMenu();
            cameraFollow?.ExitStaticMenuCamera(player);
        }
    }

    private void BeginNewGame()
    {
        Scene menuScene = SceneManager.GetActiveScene();
        CutsceneController intro = FindNewGameCutscene(menuScene);
        if (intro == null)
        {
            FindAnyObjectByType<PauseMenuHUD>()?.SetMainMenuMessage(
                $"Adicione um CutsceneController em um objeto chamado '{newGameCutsceneObjectName}' nesta cena.");
            return;
        }

        PauseMenuHUD menu = FindAnyObjectByType<PauseMenuHUD>();
        menu?.SetMainMenuMessage(string.Empty);
        menu?.CloseMainMenu();

        CameraFollow cameraFollow = CameraFollow.Instance != null
            ? CameraFollow.Instance : FindAnyObjectByType<CameraFollow>();
        cameraFollow?.ExitStaticMenuCamera(player);

        PlayableDirector director = intro.GetComponent<PlayableDirector>();
        if (director != null)
            director.stopped += OnNewGameCutsceneStopped;
        if (!intro.PlayCutscene())
        {
            if (director != null)
                director.stopped -= OnNewGameCutsceneStopped;
            CinemachineCamera menuCamera = FindSceneMenuCamera(menuScene);
            if (menuCamera != null)
                cameraFollow?.EnterMenuCamera(menuCamera);
            else
                SetMenuCameraPose(cameraFollow);
            menu?.ShowMainMenu(BeginNewGame);
            menu?.SetMainMenuMessage("A cutscene de início não pôde ser iniciada.");
        }
    }

    private void OnNewGameCutsceneStopped(PlayableDirector director)
    {
        director.stopped -= OnNewGameCutsceneStopped;
        SetGameplayPanelsEnabled(true);
    }

    private CutsceneController FindNewGameCutscene(Scene scene)
    {
        foreach (CutsceneController cutscene in
                 FindObjectsByType<CutsceneController>(FindObjectsInactive.Exclude))
        {
            if (cutscene == null || cutscene.gameObject.scene != scene)
                continue;
            if (string.Equals(cutscene.gameObject.name,
                    newGameCutsceneObjectName,
                    System.StringComparison.OrdinalIgnoreCase))
                return cutscene;
        }

        return null;
    }

    private CinemachineCamera FindSceneMenuCamera(Scene scene)
    {
        foreach (CinemachineCamera candidate in
                 FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Exclude))
        {
            if (candidate != null && candidate.gameObject.scene == scene &&
                string.Equals(candidate.gameObject.name,
                    mainMenuCameraObjectName,
                    System.StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    private void SetMenuCameraPose(CameraFollow cameraFollow)
    {
        cameraFollow?.EnterStaticMenuCamera(mainMenuCameraPosition,
            mainMenuCameraEulerAngles);
    }

    private static void SetGameplayPanelsEnabled(bool enabled)
    {
        foreach (PanelRenderer panel in FindObjectsByType<PanelRenderer>(
                     FindObjectsInactive.Include))
        {
            if (panel != null && panel.GetComponent<PauseMenuHUD>() == null)
                panel.enabled = enabled;
        }
    }

    private void BindSceneCutscenesToPersistentCamera(Scene scene)
    {
        CameraFollow follow = CameraFollow.Instance != null
            ? CameraFollow.Instance : FindAnyObjectByType<CameraFollow>();
        CinemachineBrain brain = follow != null
            ? follow.GetComponent<CinemachineBrain>()
            : FindAnyObjectByType<CinemachineBrain>();
        if (brain == null)
            return;

        foreach (PlayableDirector director in
                 FindObjectsByType<PlayableDirector>(FindObjectsInactive.Exclude))
        {
            if (director == null || director.gameObject.scene != scene ||
                director.playableAsset == null)
            {
                continue;
            }

            foreach (PlayableBinding output in director.playableAsset.outputs)
            {
                if (output.sourceObject == null || output.outputTargetType == null)
                {
                    continue;
                }

                if (typeof(CinemachineBrain).IsAssignableFrom(
                        output.outputTargetType))
                {
                    // O Brain esta no GameRoot persistente, portanto referencias
                    // serializadas da cena de fase seriam quebradas por natureza.
                    director.SetGenericBinding(output.sourceObject, brain);
                    continue;
                }

                if (player == null || output.streamName == null ||
                    output.streamName.IndexOf("Player",
                        System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (typeof(Animator).IsAssignableFrom(output.outputTargetType))
                {
                    Animator playerAnimator = player.GetComponentInChildren<Animator>(true);
                    if (playerAnimator != null)
                        director.SetGenericBinding(output.sourceObject,
                            playerAnimator);
                }
                else if (typeof(GameObject).IsAssignableFrom(
                             output.outputTargetType))
                {
                    director.SetGenericBinding(output.sourceObject,
                        player.gameObject);
                }
            }
        }
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }
}

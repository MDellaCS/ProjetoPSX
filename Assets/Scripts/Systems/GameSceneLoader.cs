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
using UnityEngine.Serialization;
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
    [Tooltip("Arquivo da cena inicial do menu. Selecione o asset .unity no Project.")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset mainMenuSceneAsset;
    [SerializeField, HideInInspector] private bool mainMenuSceneAssetInitialized;
#endif
    [FormerlySerializedAs("mainMenuSceneName")]
    [SerializeField, HideInInspector] private string mainMenuScenePath = "Assets/Scenes/1. MainMenu.unity";
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
    [Tooltip("Arquivo da cena carregado por LoadGameplayScene. Selecione o asset .unity no Project.")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset gameplaySceneAsset;
    [SerializeField, HideInInspector] private bool gameplaySceneAssetInitialized;
#endif
    [FormerlySerializedAs("gameplaySceneName")]
    [SerializeField, HideInInspector] private string gameplayScenePath;

    private bool isLoading;
    private string pendingSpawnId;
    private List<string> sceneCyclePaths;
    private LoadingScreenHUD loadingScreen;

    public bool IsLoading => isLoading;
    public bool HasStartupSceneConfigured
    {
        get
        {
            string path = ResolveScenePath(mainMenuScenePath);
            return !string.IsNullOrWhiteSpace(path) && CanLoadScene(path);
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        SynchronizeSceneAsset(mainMenuSceneAsset,
            ref mainMenuScenePath, ref mainMenuSceneAssetInitialized,
            out mainMenuSceneAsset);
        SynchronizeSceneAsset(gameplaySceneAsset,
            ref gameplayScenePath, ref gameplaySceneAssetInitialized,
            out gameplaySceneAsset);
#endif
    }

#if UNITY_EDITOR
    private static void SynchronizeSceneAsset(
        SceneAsset selectedAsset,
        ref string scenePath,
        ref bool selectionInitialized,
        out SceneAsset synchronizedAsset)
    {
        synchronizedAsset = selectedAsset;
        if (selectedAsset != null)
        {
            scenePath = AssetDatabase.GetAssetPath(selectedAsset);
        }
        else if (!selectionInitialized)
        {
            string resolvedPath = ResolveScenePath(scenePath);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                synchronizedAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    resolvedPath);
                scenePath = resolvedPath;
            }
        }
        else
        {
            scenePath = string.Empty;
        }

        selectionInitialized = true;
    }
#endif

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private IEnumerator Start()
    {
        ResolveLoadingScreen();
        if (!string.IsNullOrWhiteSpace(mainMenuScenePath))
        {
            // So esconde os paineis se o menu puder mesmo ser carregado;
            // caso contrario o jogo ficaria sem HUD e sem menu.
            if (HasStartupSceneConfigured)
                SetGameplayPanelsEnabled(false);
            LoadScene(mainMenuScenePath);
            if (HasStartupSceneConfigured)
                yield break;
        }

        // A primeira cena ja estava carregada quando o GameRoot nasceu, logo
        // ela nao dispara sceneLoaded para este componente. Esperar um frame
        // garante que todos os Spawn Points dela ja executaram Awake/OnEnable.
        yield return null;
        PlacePlayerAtSceneSpawn(SceneManager.GetActiveScene(), defaultSpawnId);
        if (loadingScreen != null)
            loadingScreen.HideAfterSceneReady();
    }

    private void Update()
    {
        if (!Debug.isDebugBuild || !enableDebugSceneShortcuts || isLoading || Keyboard.current == null)
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
        !IsBootstrapScenePath(path);

    private static bool IsBootstrapScenePath(string path) =>
        !string.IsNullOrWhiteSpace(path) &&
        Path.GetFileNameWithoutExtension(path).IndexOf("Bootstrap",
            System.StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>Carrega a fase principal configurada. Pode ser chamado por UnityEvent.</summary>
    public void LoadGameplayScene() => LoadScene(gameplayScenePath);

    /// <summary>Recarrega somente a fase atual; GameRoot e Player permanecem.</summary>
    public void ReloadCurrentScene() =>
        LoadScene(SceneManager.GetActiveScene().path, defaultSpawnId);

    public void LoadScene(string scenePath) => LoadScene(scenePath, defaultSpawnId);

    public void LoadScene(string scenePath, string spawnId)
    {
        if (isLoading)
            return;

        string resolvedScenePath = ResolveScenePath(scenePath);
        if (string.IsNullOrWhiteSpace(resolvedScenePath) ||
            !CanLoadScene(resolvedScenePath))
        {
            Debug.LogError($"Nao foi possivel carregar a cena '{scenePath}'. Caminho resolvido: '{resolvedScenePath ?? "<invalido>"}'. A cena precisa existir no projeto e estar no Build Settings fora do Editor.", this);
            return;
        }

        pendingSpawnId = spawnId;
        ResolveLoadingScreen();
        if (loadingScreen != null)
            loadingScreen.Show();
        StartCoroutine(LoadSceneRoutine(resolvedScenePath));
    }

    private IEnumerator LoadSceneRoutine(string scenePath)
    {
        isLoading = true;
        AsyncOperation operation;
#if UNITY_EDITOR
        operation = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
#else
        operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
#endif
        if (operation != null)
        {
            while (!operation.isDone)
            {
                if (loadingScreen != null)
                    loadingScreen.SetProgress(operation.progress / .9f);
                yield return null;
            }
            if (loadingScreen != null)
                loadingScreen.SetProgress(1f);
        }
        else
        {
            Debug.LogError($"Falha ao iniciar o carregamento da cena '{scenePath}'.", this);
            if (loadingScreen != null)
                loadingScreen.HideAfterSceneReady();
        }
        isLoading = false;
    }

    private static bool CanLoadScene(string scenePath)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null;
#else
        return SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0;
#endif
    }

    private static string ResolveScenePath(string scenePathOrLegacyName)
    {
        if (string.IsNullOrWhiteSpace(scenePathOrLegacyName))
            return null;

        if (scenePathOrLegacyName.EndsWith(".unity",
                System.StringComparison.OrdinalIgnoreCase))
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePathOrLegacyName) != null
                ? scenePathOrLegacyName
                : null;
#else
            return SceneUtility.GetBuildIndexByScenePath(scenePathOrLegacyName) >= 0
                ? scenePathOrLegacyName
                : null;
#endif
        }

#if UNITY_EDITOR
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(Path.GetFileNameWithoutExtension(path),
                    scenePathOrLegacyName,
                    System.StringComparison.OrdinalIgnoreCase))
                return path;
        }
#else
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.Equals(Path.GetFileNameWithoutExtension(path),
                    scenePathOrLegacyName,
                    System.StringComparison.OrdinalIgnoreCase))
                return path;
        }
#endif
        return null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        PlacePlayerAtSceneSpawn(scene, pendingSpawnId);
        BindSceneCutscenesToPersistentCamera(scene);
        pendingSpawnId = null;
        ResolveLoadingScreen();
        if (loadingScreen != null)
            loadingScreen.SetProgress(1f);
        if (loadingScreen != null)
            loadingScreen.HideAfterSceneReady();
    }

    private void ResolveLoadingScreen()
    {
        if (loadingScreen != null)
            return;

        loadingScreen = LoadingScreenHUD.Instance;
    }

    private void PlacePlayerAtSceneSpawn(Scene scene, string spawnId)
    {
        ResolvePlayer();
        ScenePlayerSpawnPoint spawn = ScenePlayerSpawnPoint.FindForScene(scene,
            spawnId);
        if (player != null && spawn != null)
        {
            PlayerCharacterController controller =
                player.GetComponent<PlayerCharacterController>();
            if (controller != null)
            {
                controller.Teleport(spawn.transform.position,
                    spawn.transform.rotation);
            }
            else
            {
                CharacterController legacyController =
                    player.GetComponent<CharacterController>();
                bool wasEnabled = legacyController != null &&
                    legacyController.enabled;
                if (wasEnabled)
                    legacyController.enabled = false;
                player.SetPositionAndRotation(spawn.transform.position,
                    spawn.transform.rotation);
                if (wasEnabled)
                    legacyController.enabled = true;
            }
        }

        CameraFollow cameraFollow = CameraFollow.Instance != null
            ? CameraFollow.Instance : FindAnyObjectByType<CameraFollow>();
        string configuredMainMenuPath =
            ResolveScenePath(mainMenuScenePath);
        bool isMainMenu = !string.IsNullOrEmpty(configuredMainMenuPath) &&
            string.Equals(scene.path, configuredMainMenuPath,
                System.StringComparison.OrdinalIgnoreCase);
        SetGameplayPanelsEnabled(!isMainMenu);

        PauseMenuHUD pauseMenu = FindAnyObjectByType<PauseMenuHUD>();
        if (isMainMenu)
        {
            CinemachineCamera menuCamera = FindSceneMenuCamera(scene);
            if (menuCamera != null)
                if (cameraFollow != null)
                    cameraFollow.EnterMenuCamera(menuCamera);
            else
                SetMenuCameraPose(cameraFollow);
            if (pauseMenu != null)
                pauseMenu.ShowMainMenu(BeginNewGame);
        }
        else
        {
            if (pauseMenu != null)
                pauseMenu.CloseMainMenu();
            if (cameraFollow != null)
                cameraFollow.ExitStaticMenuCamera(player);
        }
    }

    private void BeginNewGame()
    {
        Scene menuScene = SceneManager.GetActiveScene();
        CutsceneController intro = FindNewGameCutscene(menuScene);
        if (intro == null)
        {
            PauseMenuHUD missingMenu = FindAnyObjectByType<PauseMenuHUD>();
            if (missingMenu != null)
                missingMenu.SetMainMenuMessage(
                $"Adicione um CutsceneController em um objeto chamado '{newGameCutsceneObjectName}' nesta cena.");
            return;
        }

        PauseMenuHUD menu = FindAnyObjectByType<PauseMenuHUD>();
        if (menu != null)
            menu.SetMainMenuMessage(string.Empty);
        if (menu != null)
            menu.CloseMainMenu();

        CameraFollow cameraFollow = CameraFollow.Instance != null
            ? CameraFollow.Instance : FindAnyObjectByType<CameraFollow>();
        if (cameraFollow != null)
            cameraFollow.ExitStaticMenuCamera(player);

        PlayableDirector director = intro.GetComponent<PlayableDirector>();
        if (director != null)
            director.stopped += OnNewGameCutsceneStopped;
        if (!intro.PlayCutscene())
        {
            if (director != null)
                director.stopped -= OnNewGameCutsceneStopped;
            CinemachineCamera menuCamera = FindSceneMenuCamera(menuScene);
            if (menuCamera != null)
                if (cameraFollow != null)
                    cameraFollow.EnterMenuCamera(menuCamera);
            else
                SetMenuCameraPose(cameraFollow);
            if (menu != null)
                menu.ShowMainMenu(BeginNewGame);
            if (menu != null)
                menu.SetMainMenuMessage("A cutscene de início não pôde ser iniciada.");
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
        if (cameraFollow != null)
            cameraFollow.EnterStaticMenuCamera(mainMenuCameraPosition,
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

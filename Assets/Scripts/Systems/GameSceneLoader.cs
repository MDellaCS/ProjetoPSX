using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

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

    [Header("Bootstrap")]
    [Tooltip("Carrega automaticamente a Gameplay Scene ao iniciar. Ative somente na cena Bootstrap, que contem o GameRoot e nenhum conteudo de fase.")]
    [SerializeField] private bool loadGameplaySceneOnStart;

    [Header("Atalhos de Teste")]
    [Tooltip("Permite trocar rapidamente entre as duas cenas configuradas pelos atalhos abaixo durante o Play Mode.")]
    [SerializeField] private bool enableDebugSceneShortcuts = true;
    [Tooltip("Nome exato, presente no Build Profile, da cena da arena de testes.")]
    [SerializeField] private string testArenaSceneName = "TestArena";
    [Tooltip("Tecla que carrega a cena da arena de testes.")]
    [SerializeField] private Key loadTestArenaKey = Key.F9;
    [Tooltip("Nome exato, presente no Build Profile, da cena principal de gameplay.")]
    [SerializeField] private string gameplaySceneName;
    [Tooltip("Tecla que carrega a cena principal de gameplay.")]
    [SerializeField] private Key loadGameplaySceneKey = Key.F10;

    private bool isLoading;
    private string pendingSpawnId;

    public bool IsLoading => isLoading;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private IEnumerator Start()
    {
        if (loadGameplaySceneOnStart && !string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            LoadGameplayScene();
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

        if (Keyboard.current[loadTestArenaKey].wasPressedThisFrame)
            LoadTestArena();
        else if (Keyboard.current[loadGameplaySceneKey].wasPressedThisFrame)
            LoadGameplayScene();
    }

    /// <summary>Carrega a arena configurada. Pode ser chamado por UnityEvent.</summary>
    public void LoadTestArena() => LoadScene(testArenaSceneName);

    /// <summary>Carrega a fase principal configurada. Pode ser chamado por UnityEvent.</summary>
    public void LoadGameplayScene() => LoadScene(gameplaySceneName);

    /// <summary>Recarrega somente a fase atual; GameRoot e Player permanecem.</summary>
    public void ReloadCurrentScene() =>
        LoadScene(SceneManager.GetActiveScene().name, defaultSpawnId);

    public void LoadScene(string sceneName) => LoadScene(sceneName, defaultSpawnId);

    public void LoadScene(string sceneName, string spawnId)
    {
        if (isLoading || string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return;
        }

        pendingSpawnId = spawnId;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName,
            LoadSceneMode.Single);
        if (operation != null)
            yield return operation;
        isLoading = false;
    }

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
        if (player == null)
            return;

        if (spawn != null)
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
        cameraFollow?.RefreshAfterSceneLoad(player);
    }

    private static void BindSceneCutscenesToPersistentCamera(Scene scene)
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
                if (output.sourceObject == null || output.outputTargetType == null ||
                    !typeof(CinemachineBrain).IsAssignableFrom(
                        output.outputTargetType))
                {
                    continue;
                }

                // O Brain esta no GameRoot persistente, portanto referencias
                // serializadas da cena de fase seriam quebradas por natureza.
                // Atribuir em runtime mantem Timeline e Cinemachine desacoplados
                // das cenas sem pedir um Brain em cada cutscene.
                director.SetGenericBinding(output.sourceObject, brain);
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

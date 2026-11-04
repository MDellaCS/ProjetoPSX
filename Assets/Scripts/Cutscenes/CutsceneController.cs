using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controla uma Timeline de cutscene e expõe um estado global para gameplay.
/// A Timeline continua em tempo normal; apenas os sistemas escolhidos ficam
/// bloqueados, permitindo câmera, animação e eventos de cenário funcionarem.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayableDirector))]
public sealed class CutsceneController : MonoBehaviour
{
    [Header("Timeline")]
    [Tooltip("Playable Director que executa a Timeline desta cutscene. Se vazio, usa o componente deste objeto.")]
    [SerializeField] private PlayableDirector director;
    [Tooltip("Inicia esta cutscene automaticamente quando o objeto for habilitado.")]
    [SerializeField] private bool playOnEnable;

    [Header("Controle de Gameplay")]
    [Tooltip("Bloqueia movimento, ataques, lock-on e buffer do jogador enquanto a Timeline toca.")]
    [SerializeField] private bool lockPlayerControl = true;
    [Tooltip("Congela a IA e os ataques dos inimigos enquanto a Timeline toca.")]
    [SerializeField] private bool freezeEnemies = true;
    [Tooltip("Permite encerrar a cutscene pela tecla configurada, avançando a Timeline para seu estado final.")]
    [SerializeField] private bool allowSkip = true;
    [Tooltip("Player afetado pela cutscene. Se vazio, procura o objeto com a tag Player ao iniciar.")]
    [SerializeField] private Transform player;

    [Header("Troca de Cena")]
    [Tooltip("Carrega outra cena ao terminar normalmente ou ao pular esta cutscene.")]
    [SerializeField] private bool loadSceneOnFinish;
    [Tooltip("Arquivo da cena a carregar ao terminar. Selecione o asset .unity no Project.")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAssetOnFinish;
    [SerializeField, HideInInspector] private bool sceneAssetOnFinishInitialized;
#endif
    [FormerlySerializedAs("sceneNameOnFinish")]
    [SerializeField, HideInInspector] private string scenePathOnFinish;

    [Header("Eventos")]
    [Tooltip("Chamado imediatamente antes de a Timeline começar a tocar.")]
    [SerializeField] private UnityEvent onStarted;
    [Tooltip("Chamado ao fim natural ou ao pular a cutscene, depois de restaurar o gameplay.")]
    [SerializeField] private UnityEvent onFinished;
    [Tooltip("Chamado adicionalmente quando a cutscene foi pulada.")]
    [SerializeField] private UnityEvent onSkipped;

    private static CutsceneController active;
    private bool isRunning;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (sceneAssetOnFinish != null)
        {
            scenePathOnFinish = AssetDatabase.GetAssetPath(sceneAssetOnFinish);
        }
        else if (!sceneAssetOnFinishInitialized)
        {
            string resolvedPath = ResolveSceneAssetPath(scenePathOnFinish);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                sceneAssetOnFinish = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    resolvedPath);
                scenePathOnFinish = resolvedPath;
            }
        }
        else
        {
            scenePathOnFinish = string.Empty;
        }

        sceneAssetOnFinishInitialized = true;
#endif
    }

#if UNITY_EDITOR
    private static string ResolveSceneAssetPath(string pathOrLegacyName)
    {
        if (string.IsNullOrWhiteSpace(pathOrLegacyName))
            return null;

        if (pathOrLegacyName.EndsWith(".unity",
                System.StringComparison.OrdinalIgnoreCase))
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(pathOrLegacyName) != null
                ? pathOrLegacyName
                : null;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(path),
                    pathOrLegacyName,
                    System.StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }
#endif

    public static bool IsPlaying => active != null && active.isRunning;
    public static CutsceneController Active => IsPlaying ? active : null;
    public static bool IsPlayerControlLocked => IsPlaying && active.lockPlayerControl;
    public static bool AreEnemiesFrozen => IsPlaying && active.freezeEnemies;
    public bool IsRunning => isRunning;
    public string TimelineName => director != null && director.playableAsset != null
        ? director.playableAsset.name : "None";
    public double TimelineTime => director != null ? director.time : 0d;
    public double TimelineDuration => director != null ? director.duration : 0d;
    public float TimelineProgress => TimelineDuration <= 0d ? 0f :
        Mathf.Clamp01((float)(TimelineTime / TimelineDuration));
    public DirectorUpdateMode TimelineUpdateMode => director != null
        ? director.timeUpdateMode : DirectorUpdateMode.GameTime;
    public bool CanSkip => allowSkip;
    public string SkipKeyName => GameInputBindings.GetDisplayString("CutsceneSkip");

    private void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();
    }

    private void OnEnable()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();
        if (director != null)
            director.stopped += OnDirectorStopped;
        if (playOnEnable)
            PlayCutscene();
    }

    private void OnDisable()
    {
        if (director != null)
            director.stopped -= OnDirectorStopped;
        if (active == this)
            Finish(false, false);
    }

    private void Update()
    {
        if (!isRunning || !allowSkip || GamePauseController.IsPaused)
            return;
        if (GameInput.CutsceneSkipPressed)
            SkipCutscene();
    }

    /// <summary>Inicia a Timeline se nenhuma outra cutscene estiver ativa.</summary>
    public bool PlayCutscene()
    {
        if (director == null || (active != null && active != this))
            return false;
        if (isRunning)
            return true;

        ResolvePlayer();
        CancelPlayerActions();
        active = this;
        isRunning = true;
        director.time = 0d;
        director.Evaluate();
        onStarted?.Invoke();
        director.Play();
        return true;
    }

    /// <summary>
    /// Versao sem retorno para UnityEvents, como On Activated de uma
    /// StrenghLever ou um Signal Receiver.
    /// </summary>
    public void StartCutscene() => PlayCutscene();

    /// <summary>
    /// Avança a Timeline ao último frame para que o cenário fique no estado
    /// final correto, então devolve o gameplay ao jogador.
    /// </summary>
    public void SkipCutscene()
    {
        if (!isRunning || !allowSkip)
            return;

        director.time = director.duration;
        director.Evaluate();
        Finish(true);
        director.Stop();
    }

    private void OnDirectorStopped(PlayableDirector stopped)
    {
        if (stopped == director && isRunning)
            Finish(false);
    }

    private void Finish(bool skipped, bool allowSceneTransition = true)
    {
        if (!isRunning)
            return;

        isRunning = false;
        if (active == this)
            active = null;
        ClearPlayerInputBuffer();
        onFinished?.Invoke();
        if (skipped)
            onSkipped?.Invoke();

        if (allowSceneTransition && loadSceneOnFinish)
            LoadSceneOnFinish();
    }

    private void LoadSceneOnFinish()
    {
        if (string.IsNullOrWhiteSpace(scenePathOnFinish))
        {
            Debug.LogError(
                $"A cutscene '{name}' esta configurada para trocar de cena, mas Scene Path On Finish esta vazio.",
                this);
            return;
        }

        GameSceneLoader sceneLoader =
            FindAnyObjectByType<GameSceneLoader>();
        if (sceneLoader == null)
        {
            Debug.LogError(
                $"A cutscene '{name}' nao encontrou um GameSceneLoader ativo para carregar '{scenePathOnFinish}'.",
                this);
            return;
        }

        sceneLoader.LoadScene(scenePathOnFinish);
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
            player = found.transform;
    }

    private void CancelPlayerActions()
    {
        if (!lockPlayerControl || player == null)
            return;
        player.GetComponent<PlayerAttackController>()?.CancelAttack();
        ClearPlayerInputBuffer();
    }

    private void ClearPlayerInputBuffer()
    {
        if (player != null)
            player.GetComponent<PlayerInputBuffer>()?.ClearBufferedInputs(true);
    }
}

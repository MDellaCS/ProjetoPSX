using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Tela global de carregamento em UI Toolkit, criada antes da primeira cena.
/// </summary>
[DefaultExecutionOrder(-32000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PanelRenderer))]
public sealed class LoadingScreenHUD : MonoBehaviour
{
    private const string LayoutResource = "LoadingScreenHUD/LoadingScreenHUD";
    private const string PanelSettingsResource = "LoadingScreenHUD/PanelSettings";
    private const int PanelSortingOrder = 32767;

    private static LoadingScreenHUD instance;
    private PanelRenderer panelRenderer;
    private VisualElement uiRoot;
    private VisualElement overlay;
    private VisualElement progressFill;
    private Label loadingLabel;
    private Coroutine hideRoutine;
    private float dotTimer;
    private int dotCount;
    private bool isVisible = true;

    public bool IsLoading { get; private set; }
    public static LoadingScreenHUD Instance => instance;
    public static bool BlocksGameplayInput => instance != null &&
        instance.isVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeFirstScene()
    {
        if (instance == null)
            instance = FindAnyObjectByType<LoadingScreenHUD>(
                FindObjectsInactive.Include);
        if (instance != null)
        {
            instance.IsLoading = false;
            instance.Show();
            return;
        }

        VisualTreeAsset layout = Resources.Load<VisualTreeAsset>(LayoutResource);
        PanelSettings settings = Resources.Load<PanelSettings>(
            PanelSettingsResource);
        if (layout == null || settings == null)
        {
            Debug.LogError("LoadingScreenHUD nao encontrou seus assets em Resources/LoadingScreenHUD.");
            return;
        }

        GameObject root = new("Loading Screen (Persistent)");
        root.SetActive(false);
        PanelRenderer renderer = root.AddComponent<PanelRenderer>();
        renderer.panelSettings = settings;
        renderer.visualTreeAsset = layout;
        settings.sortingOrder = PanelSortingOrder;
        LoadingScreenHUD hud = root.AddComponent<LoadingScreenHUD>();
        hud.panelRenderer = renderer;
        instance = hud;
        DontDestroyOnLoad(root);
        root.SetActive(true);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
        {
            if (panelRenderer.panelSettings != null)
                panelRenderer.panelSettings.sortingOrder = PanelSortingOrder;
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        }
    }

    private void OnDestroy()
    {
        GamePauseController.ReleasePause(this);
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        if (instance == this)
            instance = null;
    }

    private void Start() => StartCoroutine(HandleInitialScene());

    private void Update()
    {
        if (!isVisible)
            return;
        EnsureElements();
        if (loadingLabel == null)
            return;

        dotTimer += Time.unscaledDeltaTime;
        if (dotTimer < .38f)
            return;

        dotTimer = 0f;
        dotCount = (dotCount + 1) % 4;
        loadingLabel.text = "CARREGANDO" + new string('.', dotCount);
    }

    /// <summary>Mostra o overlay e reinicia a barra de progresso.</summary>
    public void Show()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        isVisible = true;
        GamePauseController.RequestPause(this);
        EnsureElements();
        SetProgress(0f);
        if (overlay != null)
            overlay.style.opacity = 1f;
        SetVisible(true);
    }

    /// <summary>Atualiza o preenchimento da barra entre zero e um.</summary>
    public void SetProgress(float progress)
    {
        EnsureElements();
        if (progressFill != null)
            progressFill.style.width = Length.Percent(
                Mathf.Clamp01(progress) * 100f);
    }

    /// <summary>Carrega uma cena quando nao existe GameSceneLoader na cena.</summary>
    public void LoadSceneWithoutSceneLoader(string scenePath)
    {
        if (IsLoading || string.IsNullOrWhiteSpace(scenePath))
            return;

        IsLoading = true;
        Show();
        StartCoroutine(LoadSceneRoutine(scenePath));
    }

    /// <summary>Faz o fade de saida quando a cena esta pronta.</summary>
    public void HideAfterSceneReady()
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HandleInitialScene()
    {
        yield return null;
        if (IsInitialLoadManagedBySceneLoader())
            yield break;

        SetProgress(1f);
        HideAfterSceneReady();
    }

    private static bool IsInitialLoadManagedBySceneLoader()
    {
        GameSceneLoader loader = FindAnyObjectByType<GameSceneLoader>();
        return loader != null && loader.HasStartupSceneConfigured;
    }

    private IEnumerator HideRoutine()
    {
        // O fade so comeca com o carregamento 100% pronto e depois de um
        // tempo fixo e configuravel (LoadingScreenSettings), independente de
        // quanto o carregamento demorou.
        LoadingScreenSettings settings = LoadingScreenSettings.Current;
        SetProgress(1f);
        if (settings.extraWaitAfterReady > 0f)
            yield return new WaitForSecondsRealtime(settings.extraWaitAfterReady);

        float fadeDuration = Mathf.Max(0f, settings.fadeDuration);
        float elapsed = 0f;
        float startOpacity = overlay != null ? overlay.resolvedStyle.opacity : 1f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (overlay != null)
                overlay.style.opacity = Mathf.Lerp(startOpacity, 0f,
                    Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetVisible(false);
        isVisible = false;
        IsLoading = false;
        GamePauseController.ReleasePause(this);
        hideRoutine = null;
    }

    private void HandleUIReload(PanelRenderer _,
        VisualElement rootVisualElement, int __)
    {
        if (rootVisualElement == null)
            return;

        uiRoot = rootVisualElement;

        // O PanelRenderer pode notificar um reload com a arvore ainda vazia
        // (antes de clonar o UXML). Espera o proximo reload em vez de errar.
        if (rootVisualElement.childCount == 0)
        {
            overlay = null;
            progressFill = null;
            loadingLabel = null;
            return;
        }

        overlay = rootVisualElement.Q<VisualElement>("loading-screen-overlay");
        progressFill = rootVisualElement.Q<VisualElement>("loading-progress-fill");
        loadingLabel = rootVisualElement.Q<Label>("loading-label");
        if (overlay == null || progressFill == null || loadingLabel == null)
        {
            Debug.LogError("LoadingScreenHUD: elementos ausentes no UXML.", this);
            return;
        }

        SetProgress(0f);
        SetVisible(isVisible);
    }

    // O PanelRenderer pode recriar a arvore (por exemplo ao ser reativado)
    // sem um novo callback com os elementos prontos. Resolve sob demanda.
    private void EnsureElements()
    {
        if (overlay != null && progressFill != null && loadingLabel != null)
            return;
        VisualElement root = uiRoot;
        if (root == null || root.childCount == 0)
            return;

        overlay = root.Q<VisualElement>("loading-screen-overlay");
        progressFill = root.Q<VisualElement>("loading-progress-fill");
        loadingLabel = root.Q<Label>("loading-label");
        if (overlay != null)
            overlay.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetVisible(bool value)
    {
        EnsureElements();
        if (overlay != null)
            overlay.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private IEnumerator LoadSceneRoutine(string scenePath)
    {
        AsyncOperation operation;
#if UNITY_EDITOR
        operation = UnityEditor.SceneManagement.EditorSceneManager
            .LoadSceneAsyncInPlayMode(scenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
        operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
#endif
        if (operation == null)
        {
            Debug.LogError($"Falha ao iniciar o carregamento da cena '{scenePath}'.");
            IsLoading = false;
            HideAfterSceneReady();
            yield break;
        }

        while (!operation.isDone)
        {
            SetProgress(operation.progress / .9f);
            yield return null;
        }

        SetProgress(1f);
        IsLoading = false;
        HideAfterSceneReady();
    }
}

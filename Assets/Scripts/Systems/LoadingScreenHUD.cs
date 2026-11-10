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
    private const float SpinnerDegreesPerSecond = 300f;

    private static LoadingScreenHUD instance;
    private PanelRenderer panelRenderer;
    private VisualElement uiRoot;
    private VisualElement overlay;
    private VisualElement spinner;
    private Coroutine hideRoutine;
    private Coroutine fadeInRoutine;
    private float spinnerAngle;
    private float visibleSinceRealtime;
    private float readySinceRealtime;
    private float visualOpacity;
    private bool hasVisualOpacity;
    private bool isVisible = true;
    private bool blocksGameplayInput = true;

    public bool IsLoading { get; private set; }
    public static LoadingScreenHUD Instance => instance;
    public static bool BlocksGameplayInput => instance != null &&
        instance.blocksGameplayInput;

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
        hud.Show();
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
        if (spinner == null)
            return;

        spinnerAngle = (spinnerAngle + Time.unscaledDeltaTime * SpinnerDegreesPerSecond) % 360f;
        spinner.style.rotate = new Rotate(new Angle(spinnerAngle, AngleUnit.Degree));
    }

    /// <summary>Mostra o overlay com o spinner girando.</summary>
    public void Show()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (fadeInRoutine != null)
        {
            StopCoroutine(fadeInRoutine);
            fadeInRoutine = null;
        }

        isVisible = true;
        blocksGameplayInput = true;
        visibleSinceRealtime = Time.realtimeSinceStartup;
        GamePauseController.RequestPause(this);
        EnsureElements();
        float startOpacity = hasVisualOpacity ? visualOpacity : 0f;
        SetOpacity(startOpacity);
        SetVisible(true);

        LoadingScreenSettings settings = LoadingScreenSettings.Current;
        float duration = Mathf.Max(0f, settings.fadeInDuration);
        if (duration <= 0f)
        {
            SetOpacity(1f);
            return;
        }

        fadeInRoutine = StartCoroutine(FadeInRoutine(startOpacity,
            duration, settings.fadeInCurve));
    }

    /// <summary>Espera o fade-in terminar antes de comecar a carregar a cena.</summary>
    public IEnumerator WaitForFadeInComplete()
    {
        while (fadeInRoutine != null)
            yield return null;
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
        readySinceRealtime = Time.realtimeSinceStartup;
        hideRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HandleInitialScene()
    {
        yield return null;
        if (IsInitialLoadManagedBySceneLoader())
            yield break;

        HideAfterSceneReady();
    }

    private static bool IsInitialLoadManagedBySceneLoader()
    {
        GameSceneLoader loader = FindAnyObjectByType<GameSceneLoader>();
        return loader != null && loader.HasStartupSceneConfigured;
    }

    private IEnumerator HideRoutine()
    {
        // Nao inicia o fade de saida enquanto o fade de entrada ainda estiver
        // rodando, inclusive no carregamento inicial da aplicacao.
        yield return WaitForFadeInComplete();

        LoadingScreenSettings settings = LoadingScreenSettings.Current;
        float minimumVisibleDuration = Mathf.Max(0f,
            settings.minimumVisibleDuration);
        float visibleDuration = Mathf.Max(0f,
            Time.realtimeSinceStartup - visibleSinceRealtime);
        float waitAfterReady = Mathf.Max(
            Mathf.Max(0f, settings.extraWaitAfterReady -
                (Time.realtimeSinceStartup - readySinceRealtime)),
            minimumVisibleDuration - visibleDuration);
        if (waitAfterReady > 0f)
            yield return new WaitForSecondsRealtime(waitAfterReady);

        // A cena ja esta pronta. Libera gameplay no primeiro frame do fade de
        // saida, mantendo o overlay visivel enquanto ele desaparece.
        blocksGameplayInput = false;
        IsLoading = false;
        GamePauseController.ReleasePause(this);

        float fadeDuration = Mathf.Max(0f, settings.fadeOutDuration);
        float elapsed = 0f;
        float startOpacity = hasVisualOpacity
            ? visualOpacity
            : overlay != null ? overlay.resolvedStyle.opacity : 1f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = EvaluateFadeCurve(settings.fadeOutCurve,
                Mathf.Clamp01(elapsed / fadeDuration));
            SetOpacity(Mathf.Lerp(startOpacity, 0f, progress));
            yield return null;
        }

        SetOpacity(0f);
        SetVisible(false);
        isVisible = false;
        blocksGameplayInput = false;
        IsLoading = false;
        hideRoutine = null;
    }

    private IEnumerator FadeInRoutine(float startOpacity, float duration,
        AnimationCurve curve)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = EvaluateFadeCurve(curve,
                Mathf.Clamp01(elapsed / duration));
            SetOpacity(Mathf.Lerp(startOpacity, 1f, progress));
            yield return null;
        }

        SetOpacity(1f);
        fadeInRoutine = null;
    }

    private static float EvaluateFadeCurve(AnimationCurve curve, float time)
    {
        if (curve == null || curve.length == 0)
            return time;
        return Mathf.Clamp01(curve.Evaluate(time));
    }

    private void SetOpacity(float value)
    {
        visualOpacity = Mathf.Clamp01(value);
        hasVisualOpacity = true;
        if (overlay != null)
            overlay.style.opacity = visualOpacity;
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
            spinner = null;
            return;
        }

        overlay = rootVisualElement.Q<VisualElement>("loading-screen-overlay");
        spinner = rootVisualElement.Q<VisualElement>("loading-spinner");
        if (overlay == null || spinner == null)
        {
            Debug.LogError("LoadingScreenHUD: elementos ausentes no UXML.", this);
            return;
        }

        SetVisible(isVisible);
    }

    // O PanelRenderer pode recriar a arvore (por exemplo ao ser reativado)
    // sem um novo callback com os elementos prontos. Resolve sob demanda.
    private void EnsureElements()
    {
        if (overlay != null && spinner != null)
            return;
        VisualElement root = uiRoot;
        if (root == null || root.childCount == 0)
            return;

        overlay = root.Q<VisualElement>("loading-screen-overlay");
        spinner = root.Q<VisualElement>("loading-spinner");
        if (overlay != null)
        {
            overlay.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasVisualOpacity)
                overlay.style.opacity = visualOpacity;
        }
    }

    private void SetVisible(bool value)
    {
        EnsureElements();
        if (overlay != null)
        {
            overlay.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasVisualOpacity)
                overlay.style.opacity = visualOpacity;
        }
    }

    private IEnumerator LoadSceneRoutine(string scenePath)
    {
        yield return WaitForFadeInComplete();

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
            yield return null;
        }

        IsLoading = false;
        HideAfterSceneReady();
    }
}

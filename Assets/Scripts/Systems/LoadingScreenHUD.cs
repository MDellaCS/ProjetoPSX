using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Tela global de carregamento criada antes da primeira cena. Não depende de
/// objetos ou assets presentes em uma cena específica.
/// </summary>
[DefaultExecutionOrder(-32000)]
[DisallowMultipleComponent]
public sealed class LoadingScreenHUD : MonoBehaviour
{
    private const float FadeDuration = .3f;
    private const float MinimumVisibleTime = .35f;
    private const int CanvasSortOrder = 32767;

    private static LoadingScreenHUD instance;

    private CanvasGroup canvasGroup;
    private Canvas loadingCanvas;
    private Image progressFill;
    private Text loadingLabel;
    private Texture2D rainbowBackgroundTexture;
    private readonly System.Collections.Generic.List<Canvas>
        temporarilyDisabledCanvases = new();
    private Coroutine hideRoutine;
    private float shownAt;
    private float dotTimer;
    private int dotCount;
    private bool isVisible = true;

    public static LoadingScreenHUD Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeFirstScene()
    {
        if (instance != null)
            return;

        var root = new GameObject("LoadingScreen (Persistent)");
        instance = root.AddComponent<LoadingScreenHUD>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        shownAt = Time.unscaledTime;
        CreateInterface();
        SuppressCompetingUi();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        RestoreCompetingUi();
    }

    private void OnDestroy()
    {
        if (rainbowBackgroundTexture != null)
            Destroy(rainbowBackgroundTexture);
    }

    private void Start()
    {
        StartCoroutine(HandleInitialScene());
    }

    private void Update()
    {
        if (!isVisible || loadingLabel == null)
            return;

        dotTimer += Time.unscaledDeltaTime;
        if (dotTimer >= .38f)
        {
            dotTimer = 0f;
            dotCount = (dotCount + 1) % 4;
            loadingLabel.text = "LOADING" + new string('.', dotCount);
        }
    }

    /// <summary>Mostra o overlay e zera o progresso para uma nova carga.</summary>
    public void Show()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        shownAt = Time.unscaledTime;
        isVisible = true;
        SetProgress(0f);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        SuppressCompetingUi();
    }

    /// <summary>Atualiza o preenchimento da barra entre zero e um.</summary>
    public void SetProgress(float progress)
    {
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(progress);
    }

    /// <summary>Carrega uma cena quando não existe GameSceneLoader na cena.</summary>
    public void LoadSceneWithoutSceneLoader(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return;

        Show();
        StartCoroutine(LoadSceneRoutine(scenePath));
    }

    /// <summary>Faz o fade de saída quando a cena está pronta.</summary>
    public void HideAfterSceneReady()
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HandleInitialScene()
    {
        // A cena já existe, mas aguardamos os Start dos sistemas que podem
        // iniciar uma troca imediatamente (por exemplo, Bootstrap -> Menu).
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
        float remaining = MinimumVisibleTime - (Time.unscaledTime - shownAt);
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f,
                Mathf.Clamp01(elapsed / FadeDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isVisible = false;
        RestoreCompetingUi();
        hideRoutine = null;
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        if (!isVisible)
            return;

        SuppressCompetingUi();
        StartCoroutine(SuppressCompetingUiNextFrame());
    }

    private IEnumerator SuppressCompetingUiNextFrame()
    {
        yield return null;
        if (isVisible)
            SuppressCompetingUi();
    }

    private void SuppressCompetingUi()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(
                     FindObjectsInactive.Include))
        {
            if (canvas == null || canvas == loadingCanvas || !canvas.enabled)
                continue;

            canvas.enabled = false;
            temporarilyDisabledCanvases.Add(canvas);
        }

    }

    private void RestoreCompetingUi()
    {
        foreach (Canvas canvas in temporarilyDisabledCanvases)
        {
            if (canvas != null)
                canvas.enabled = true;
        }

        temporarilyDisabledCanvases.Clear();
    }

    private IEnumerator LoadSceneRoutine(string scenePath)
    {
        AsyncOperation operation;
#if UNITY_EDITOR
        operation = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
#else
        operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
#endif
        if (operation == null)
        {
            Debug.LogError($"Falha ao iniciar o carregamento da cena '{scenePath}'.");
            HideAfterSceneReady();
            yield break;
        }

        while (!operation.isDone)
        {
            SetProgress(operation.progress / .9f);
            yield return null;
        }

        SetProgress(1f);
        HideAfterSceneReady();
    }

    private void CreateInterface()
    {
        var canvasObject = new GameObject("Loading Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        loadingCanvas = canvas;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = CanvasSortOrder;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        RawImage background = CreateRainbowBackground(canvasObject.transform);
        background.raycastTarget = true;

        Text title = CreateText("Loading Label", canvasObject.transform,
            104, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(.1f, .49f), new Vector2(.9f, .66f));
        title.color = Color.white;
        Outline titleOutline = title.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0f, .85f);
        titleOutline.effectDistance = new Vector2(3f, -3f);
        title.text = "LOADING";
        loadingLabel = title;

        Image track = CreateImage("Progress Track", canvasObject.transform,
            new Color(.28f, .28f, .28f, 1f), new Vector2(.15f, .39f),
            new Vector2(.85f, .445f), Vector2.zero, Vector2.zero);

        progressFill = CreateImage("Progress Fill", track.transform,
            Color.white, Vector2.zero, Vector2.one,
            new Vector2(5f, 5f), new Vector2(-5f, -5f));
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;
        progressFill.raycastTarget = false;
    }

    private RawImage CreateRainbowBackground(Transform parent)
    {
        var backgroundObject = new GameObject("Rainbow Gradient Background",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        backgroundObject.transform.SetParent(parent, false);

        RectTransform rect = backgroundObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        const int textureWidth = 512;
        const int textureHeight = 2;
        rainbowBackgroundTexture = new Texture2D(textureWidth, textureHeight,
            TextureFormat.RGB24, false, true)
        {
            name = "Loading Rainbow Gradient",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 0,
            hideFlags = HideFlags.DontSave
        };

        var colors = new Color[textureWidth * textureHeight];
        for (int x = 0; x < textureWidth; x++)
        {
            float hue = x / (float)(textureWidth - 1);
            Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
            colors[x] = rainbowColor * .8f;
            colors[textureWidth + x] = rainbowColor;
        }

        rainbowBackgroundTexture.SetPixels(colors);
        rainbowBackgroundTexture.Apply(false, true);

        RawImage background = backgroundObject.GetComponent<RawImage>();
        background.texture = rainbowBackgroundTexture;
        background.color = Color.white;
        return background;
    }

    private static Image CreateImage(string objectName, Transform parent,
        Color color, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string objectName, Transform parent,
        int fontSize, FontStyle fontStyle, TextAnchor alignment,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}

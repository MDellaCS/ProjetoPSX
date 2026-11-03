using System.Collections;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

/// <summary>
/// HUD de UI Toolkit para exibir temporariamente o nome de uma area.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PanelRenderer))]
public class AreaTitleHUD : MonoBehaviour
{
    private static AreaTitleHUD instance;

    [Header("Referencias")]
    [Tooltip("Fonte Font Asset do TextCore/UI Toolkit usada pelo nome e subtitulo da area.")]
    [SerializeField] private FontAsset font;
    [Tooltip("PanelRenderer que desenha o cartao do nome da area.")]
    [SerializeField] private PanelRenderer panelRenderer;
    [Tooltip("Som de interface tocado sempre que um titulo de area e exibido. Vazio desativa o som.")]
    [SerializeField] private AudioClip displaySound;
    [Tooltip("Volume base do som de exibicao, antes do multiplicador global de efeitos.")]
    [Range(0f, 1f)] [SerializeField] private float soundVolume = .8f;
    [Tooltip("Pitch aplicado ao som de exibicao.")]
    [Range(.1f, 3f)] [SerializeField] private float soundPitch = 1f;
    [Tooltip("Grupo opcional do Audio Mixer para encaminhar o som do titulo.")]
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup soundMixerGroup;

    [Header("Aparencia")]
    [Tooltip("Tamanho em pixels de referencia do nome principal da area.")]
    [Min(1f)] [SerializeField] private float titleFontSize = 38f;
    [Tooltip("Tamanho em pixels de referencia da linha secundaria opcional.")]
    [Min(1f)] [SerializeField] private float subtitleFontSize = 22f;
    [Tooltip("Cor aplicada ao texto do nome da area.")]
    [SerializeField] private Color titleColor = Color.white;
    [Tooltip("Cor aplicada ao texto secundario, quando informado.")]
    [SerializeField] private Color subtitleColor = new(1f, 1f, 1f, .8f);
    [Tooltip("Cor e opacidade da linha horizontal sob o nome da area.")]
    [SerializeField] private Color dividerColor = new(1f, 1f, 1f, .8f);
    [Tooltip("Largura maxima do cartao de texto, em pixels de referencia da tela.")]
    [Min(100f)] [SerializeField] private float maxWidth = 560f;
    [Tooltip("Distancia entre o cartao e as bordas direita e inferior da tela.")]
    [SerializeField] private Vector2 screenPadding = new(64f, 56f);
    [Tooltip("Espessura da linha horizontal em pixels de referencia da tela.")]
    [Min(1f)] [SerializeField] private float dividerThickness = 2f;

    [Header("Animacao")]
    [Tooltip("Duracao do aparecimento gradual do nome da area.")]
    [Min(0f)] [SerializeField] private float fadeInSeconds = .4f;
    [Tooltip("Tempo em que o nome permanece totalmente visivel.")]
    [Min(0f)] [SerializeField] private float holdSeconds = 3f;
    [Tooltip("Duracao do desaparecimento gradual do nome da area.")]
    [Min(0f)] [SerializeField] private float fadeOutSeconds = .6f;
    [Tooltip("Ordem de desenho deste HUD no PanelRenderer. Valores maiores aparecem sobre paineis de ordem menor.")]
    [SerializeField] private int sortingOrder = 3;
    private VisualElement titleCard;
    private Label titleLabel;
    private Label subtitleLabel;
    private VisualElement divider;
    private Coroutine displayRoutine;
    private string currentTitle;
    private string currentSubtitle;
    private bool isVisible;
    private bool uiReloadReceived;

    public static void ShowArea(string title, string subtitle = null,
        float displaySeconds = -1f)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        AreaTitleHUD hud = ResolveInstance();
        if (hud == null)
            return;

        hud.Show(title, subtitle, displaySeconds);
    }

    public void Show(string title, string subtitle = null,
        float displaySeconds = -1f)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        currentTitle = title.Trim();
        currentSubtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();
        isVisible = true;
        if (displaySound != null)
            GameAudioSystem.Play2D(displaySound, soundVolume, soundPitch,
                soundMixerGroup);
        RefreshVisuals();

        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        displayRoutine = StartCoroutine(DisplayRoutine(
            displaySeconds >= 0f ? displaySeconds : holdSeconds));
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        if (GetComponentInParent<PersistentGameRoot>() == null)
            DontDestroyOnLoad(transform.root.gameObject);

        if (panelRenderer != null)
        {
            panelRenderer.sortingOrder = sortingOrder;
            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        }
    }

    private IEnumerator Start()
    {
        // PanelRenderer pode carregar o UXML antes que os componentes MonoBehaviour
        // registrem seus callbacks. Reatribuir o mesmo asset força uma recarga com
        // este HUD ja inscrito, e faz o callback entregar a arvore atual.
        yield return null;
        if (!uiReloadReceived && panelRenderer != null)
        {
            VisualTreeAsset source = panelRenderer.visualTreeAsset;
            if (source != null)
            {
                panelRenderer.visualTreeAsset = null;
                panelRenderer.visualTreeAsset = source;
                yield return null;
            }
        }

    }

    private void OnDestroy()
    {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
        if (instance == this)
            instance = null;
    }

    private IEnumerator DisplayRoutine(float visibleSeconds)
    {
        float waitForUI = 0f;
        while (titleCard == null && waitForUI < 5f)
        {
            waitForUI += Time.unscaledDeltaTime;
            yield return null;
        }

        if (titleCard == null)
        {
            displayRoutine = null;
            yield break;
        }

        yield return FadeTo(1f, fadeInSeconds);
        if (visibleSeconds > 0f)
            yield return new WaitForSecondsRealtime(visibleSeconds);
        yield return FadeTo(0f, fadeOutSeconds);
        isVisible = false;
        if (titleCard != null)
            titleCard.style.display = DisplayStyle.None;
        displayRoutine = null;
    }

    private IEnumerator FadeTo(float targetOpacity, float duration)
    {
        if (titleCard == null)
            yield break;

        float startOpacity = titleCard.resolvedStyle.opacity;
        if (duration <= 0f)
        {
            titleCard.style.opacity = targetOpacity;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (titleCard == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            titleCard.style.opacity = Mathf.Lerp(startOpacity, targetOpacity,
                Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (titleCard != null)
            titleCard.style.opacity = targetOpacity;
    }

    private void HandleUIReload(PanelRenderer _, VisualElement root, int __)
    {
        uiReloadReceived = true;
        if (root == null)
            return;

        titleCard = root.Q<VisualElement>("area-title-card");
        titleLabel = root.Q<Label>("area-title-name");
        divider = root.Q<VisualElement>("area-title-divider");
        subtitleLabel = root.Q<Label>("area-title-subtitle");
        if (titleCard == null || titleLabel == null || divider == null || subtitleLabel == null)
            return;

        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (titleLabel == null || subtitleLabel == null || divider == null || titleCard == null)
            return;

        if (font != null)
        {
            StyleFontDefinition fontDefinition = new(
                FontDefinition.FromSDFFont(font));
            titleLabel.style.unityFontDefinition = fontDefinition;
            subtitleLabel.style.unityFontDefinition = fontDefinition;
        }

        titleLabel.text = currentTitle ?? string.Empty;
        subtitleLabel.text = currentSubtitle ?? string.Empty;
        titleLabel.style.fontSize = titleFontSize;
        titleLabel.style.color = titleColor;
        subtitleLabel.style.fontSize = subtitleFontSize;
        subtitleLabel.style.color = subtitleColor;
        divider.style.backgroundColor = dividerColor;
        divider.style.height = dividerThickness;
        bool hasSubtitle = !string.IsNullOrEmpty(currentSubtitle);
        subtitleLabel.style.display = hasSubtitle ? DisplayStyle.Flex : DisplayStyle.None;
        divider.style.display = hasSubtitle ? DisplayStyle.Flex : DisplayStyle.None;
        titleCard.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        titleCard.style.right = screenPadding.x;
        titleCard.style.bottom = screenPadding.y;
        titleCard.style.width = maxWidth;
        titleCard.style.opacity = isVisible ? titleCard.resolvedStyle.opacity : 0f;
    }

    private static AreaTitleHUD ResolveInstance()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<AreaTitleHUD>();
        return instance;
    }

    private void OnValidate()
    {
        titleFontSize = Mathf.Max(1f, titleFontSize);
        subtitleFontSize = Mathf.Max(1f, subtitleFontSize);
        maxWidth = Mathf.Max(100f, maxWidth);
        dividerThickness = Mathf.Max(1f, dividerThickness);
        screenPadding.x = Mathf.Max(0f, screenPadding.x);
        screenPadding.y = Mathf.Max(0f, screenPadding.y);
        soundVolume = Mathf.Clamp01(soundVolume);
        soundPitch = Mathf.Clamp(soundPitch, .1f, 3f);
    }
}

using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public sealed class SplashTextHUD : MonoBehaviour
{
    private enum PresentationType { Linear, Quadratic, Cubic, Quartic, Exponential, Sine, Circular }
    private enum Easing { In, Out, InOut, Off }
    [Header("Aparencia")]
    [Tooltip("Altura do centro do texto em porcentagem da tela, medida a partir do topo.")]
    [Range(0f, 100f)] [SerializeField] private float textHeightPercent = 50f;
    [Tooltip("Tamanho da fonte em pixels de referencia da interface.")]
    [Min(1f)] [SerializeField] private float fontSize = 68f;
    [Tooltip("Exibe o texto em negrito quando ativado.")]
    [SerializeField] private bool bold;

    [Header("Mensagem de vitoria")]
    [Tooltip("Texto exibido quando o ultimo chefe da luta e derrotado.")]
    [SerializeField] private string victoryText = "CHEFE DERROTADO";
    [Tooltip("Cor do texto exibido quando o ultimo chefe e derrotado.")]
    [SerializeField] private Color victoryColor = new Color32(230, 202, 145, 255);
    [Tooltip("Tempo em segundos que a vitória fica totalmente visível.")]
    [Min(0f)] [SerializeField] private float victoryHold = 2.2f;
    [Tooltip("Tempo em segundos entre a solicita??o de vit?ria e o in?cio da mensagem.")]
    [Min(0f)] [SerializeField] private float victoryDelay = 0f;

    [Header("Mensagem de morte")]
    [Tooltip("Texto exibido quando o jogador morre.")]
    [SerializeField] private string deathText = "VOCE MORREU";
    [Tooltip("Cor do texto exibido quando o jogador morre.")]
    [SerializeField] private Color deathColor = new Color32(220, 76, 80, 255);
    [Tooltip("Tempo em segundos que a mensagem de morte fica totalmente visível.")]
    [Min(0f)] [SerializeField] private float deathHold = 2.8f;
    [Tooltip("Tempo em segundos entre a solicita??o de morte e o in?cio da mensagem.")]
    [Min(0f)] [SerializeField] private float deathDelay = 1.5f;
    [Header("Apari??o das letras")]
    [Tooltip("Duracao total, em segundos, entre a primeira e a ultima letra.")]
    [Min(0f)] [SerializeField] private float revealDuration = 1.2f;
    [Tooltip("Formato da progressao usada para distribuir as letras no tempo.")]
    [SerializeField] private PresentationType presentation = PresentationType.Linear;
    [Tooltip("Direcao da progressao: aceleracao, desaceleracao, ambas ou sem easing.")]
    [SerializeField] private Easing easing = Easing.In;
    [Tooltip("Tempo de entrada gradual do texto inteiro, em segundos.")]
    [Min(0f)] [SerializeField] private float fadeInDuration = .35f;
    [Tooltip("Tempo de saida gradual do texto inteiro, em segundos.")]
    [Min(0f)] [SerializeField] private float fadeOutDuration = .35f;
    [Tooltip("Som tocado quando cada letra come?a a aparecer. Vazio desativa o som.")]
    [SerializeField] private AudioClip letterSound;
    [Tooltip("Som tocado no in?cio da ?ltima letra. Vazio desativa esse som.")]
    [SerializeField] private AudioClip finalLetterSound;
    [Tooltip("Volume dos sons das letras, antes do volume global de interface.")]
    [Range(0f, 1f)] [SerializeField] private float letterSoundVolume = .8f;
    [Tooltip("Volume do som da ultima letra, antes do volume global de interface.")]
    [Range(0f, 1f)] [SerializeField] private float finalLetterSoundVolume = .8f;

    [Header("Impacto luminoso")]
    [Tooltip("Ativa o clarão e o halo ao aparecer a última letra.")]
    [SerializeField] private bool bloomEnabled = true;
    [Tooltip("Duração em segundos do clarão após a última letra.")]
    [Min(0f)] [SerializeField] private float bloomDuration = .6f;
    [Tooltip("Intensidade do halo ao redor do texto. Valores maiores reforçam as camadas de brilho.")]
    [Min(0f)] [SerializeField] private float bloomIntensity = .85f;
    [Tooltip("Expansão máxima do halo em porcentagem do tamanho do texto. Valores acima de 100 criam um efeito amplo e exagerado.")]
    [Min(0f)] [SerializeField] private float bloomRadius = 18f;
    [Tooltip("Intensidade do clareamento da cor da mensagem durante o impacto.")]
    [Range(0f, 1f)] [SerializeField] private float bloomTextIntensity = .65f;
    [Tooltip("Evolução da intensidade do impacto ao longo de sua duração, de zero a um.")]
    [SerializeField] private AnimationCurve bloomCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private string[] characters;
    private int[] letterIndices;
    private int soundsPlayed;
    private float bloomStartedAt = -1f;
    private readonly StringBuilder textBuilder = new();
    private SplashTextMessage? preview;
    private bool showingPreview;
    private PanelRenderer rendererUI;
    private Label label;
    private readonly Label[] glowLabels = new Label[3];
    private float elapsed;
    private int revision;
    private SplashTextMessage? current;

    public bool PlayPreview(SplashTextMessage message)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || SplashTextMessages.TryPeek(out _))
            return false;
        preview = message;
        showingPreview = false;
        current = null;
        characters = null;
        elapsed = 0f;
        return true;
    }

    private void OnEnable()
    {
        rendererUI = GetComponent<PanelRenderer>();
        rendererUI.RegisterUIReloadCallback(Bind);
    }

    private void OnDisable()
    {
        rendererUI.UnregisterUIReloadCallback(Bind);
        if (label != null) SetVisible(false);
        label = null;
        System.Array.Clear(glowLabels, 0, glowLabels.Length);
    }

    private void Bind(PanelRenderer _, VisualElement root, int version)
    {
        label = root?.Q<Label>("splash-text");
        glowLabels[0] = root?.Q<Label>("splash-glow-wide");
        glowLabels[1] = root?.Q<Label>("splash-glow-medium");
        glowLabels[2] = root?.Q<Label>("splash-glow-inner");
        Refresh(0f, true);
    }

    private void SetVisible(bool visible)
    {
        label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (Label glow in glowLabels)
            if (glow != null) glow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        label.parent?.EnableInClassList("splash-text-visible", visible);
    }

    // O relógio continua durante slow motion e congela sob qualquer dono de pausa.
    private void LateUpdate() => Refresh(GamePauseController.IsPaused ? 0f : Time.unscaledDeltaTime, false);

    private void Refresh(float delta, bool rebound)
    {
        if (label == null) return;
        label.style.top = Length.Percent(Mathf.Clamp(textHeightPercent, 0f, 100f));
        label.style.fontSize = Mathf.Max(1f, fontSize);
        label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
        foreach (Label glow in glowLabels)
        {
            if (glow == null) continue;
            glow.style.top = label.style.top;
            glow.style.fontSize = label.style.fontSize;
            glow.style.unityFontStyleAndWeight = label.style.unityFontStyleAndWeight;
        }
        if (revision != SplashTextMessages.Revision)
        {
            revision = SplashTextMessages.Revision;
            current = null;
            elapsed = 0f;
            characters = null;
            preview = null;
            showingPreview = false;
            rebound = true;
        }
        bool queued = SplashTextMessages.TryPeek(out var next);
        if (queued && showingPreview)
        {
            preview = null;
            showingPreview = false;
            current = null;
            characters = null;
            elapsed = 0f;
        }
        if (!queued)
        {
            if (!preview.HasValue)
            {
                SetVisible(false);
                current = null;
                return;
            }
            next = preview.Value;
        }
        showingPreview = !queued;
        bool death = next == SplashTextMessage.PlayerDied;
        if (current != next || rebound)
        {
            current = next;
            if (!rebound || characters == null)
                PrepareText(death ? deathText : victoryText);
            label.EnableInClassList("death", death);
            SetVisible(false);
        }
        float delay = Mathf.Max(0f, death ? deathDelay : victoryDelay);
        elapsed += delta;
        if (elapsed < delay) return;
        SetVisible(true);
        if (death && !showingPreview)
        {
            SplashTextMessages.DeathPresented = true;
        }
        float visibleTime = elapsed - delay;
        float revealTime = Mathf.Max(0f, revealDuration);
        UpdateLetters(visibleTime, revealTime, death);
        float hold = Mathf.Max(0f, death ? deathHold : victoryHold);
        float fadeIn = Mathf.Max(0f, fadeInDuration);
        float fadeOut = Mathf.Max(0f, fadeOutDuration);
        float impactEnd = bloomStartedAt >= 0f && bloomEnabled
            ? bloomStartedAt + Mathf.Max(0f, bloomDuration) : 0f;
        float endTime = Mathf.Max(Mathf.Max(revealTime, fadeIn) + hold, impactEnd) + fadeOut;
        float inOpacity = fadeIn > 0f ? Mathf.Clamp01(visibleTime / fadeIn) : 1f;
        float outOpacity = fadeOut > 0f ? Mathf.Clamp01((endTime - visibleTime) / fadeOut) : 1f;
        label.style.opacity = Mathf.Min(inOpacity, outOpacity);
        foreach (Label glow in glowLabels)
            if (glow != null) glow.style.opacity = glow.style.opacity.value * label.style.opacity.value;
        if (visibleTime < endTime) return;
        if (showingPreview)
        {
            preview = null;
            showingPreview = false;
        }
        else
        {
            SplashTextMessages.Complete();
            if (death) SplashTextMessages.DeathCompleted = true;
        }
        SetVisible(false);
        elapsed = 0f;
        current = null;
    }
    private void PrepareText(string message)
    {
        message ??= string.Empty;
        int[] starts = StringInfo.ParseCombiningCharacters(message);
        characters = new string[starts.Length];
        var indices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < starts.Length; i++)
        {
            int end = i + 1 < starts.Length ? starts[i + 1] : message.Length;
            characters[i] = message.Substring(starts[i], end - starts[i]);
            if (!string.IsNullOrWhiteSpace(characters[i])) indices.Add(i);
        }
        letterIndices = indices.ToArray();
        soundsPlayed = 0;
        bloomStartedAt = -1f;
    }

    private void UpdateLetters(float time, float revealDuration, bool death)
    {
        float progress = revealDuration > 0f ? Mathf.Clamp01(time / revealDuration) : 1f;
        float curved = EvaluatePresentation(progress);
        int visibleLetters = letterIndices.Length == 0 ? 0 :
            1 + Mathf.FloorToInt(curved * (letterIndices.Length - 1));
        visibleLetters = Mathf.Max(soundsPlayed, visibleLetters);
        if (bloomStartedAt < 0f && visibleLetters == letterIndices.Length && visibleLetters > 0)
            bloomStartedAt = time;

        float bloom = 0f;
        if (bloomEnabled && bloomStartedAt >= 0f && bloomDuration > 0f)
        {
            float impactProgress = (time - bloomStartedAt) / bloomDuration;
            if (impactProgress < 1f)
                bloom = Mathf.Clamp01(bloomCurve != null
                    ? bloomCurve.Evaluate(Mathf.Clamp01(impactProgress)) : 1f - impactProgress);
        }
        Color baseColor = death ? deathColor : victoryColor;
        Color shadowColor = baseColor;
        shadowColor.a *= Mathf.Clamp01(Mathf.Max(0f, bloomIntensity) * bloom);
        label.style.textShadow = new TextShadow
        {
            color = shadowColor,
            offset = Vector2.zero,
            blurRadius = Mathf.Max(0f, fontSize) * Mathf.Max(0f, bloomRadius) * .01f
        };
        while (!GamePauseController.IsPaused && soundsPlayed < visibleLetters)
        {
            bool last = soundsPlayed == letterIndices.Length - 1;
            AudioClip clip = last ? finalLetterSound : letterSound;
            if (clip != null) InterfaceAudio.PlayClip(clip, last ? finalLetterSoundVolume : letterSoundVolume);
            soundsPlayed++;
        }

        textBuilder.Clear();
        int letter = 0;
        Color textColor = Color.Lerp(baseColor, Color.white,
            bloom * Mathf.Clamp01(bloomTextIntensity));
        label.style.color = textColor;
        string color = ColorUtility.ToHtmlStringRGB(textColor);
        for (int i = 0; i < characters.Length; i++)
        {
            if (letter >= letterIndices.Length || letterIndices[letter] != i)
            {
                textBuilder.Append(characters[i]);
                continue;
            }
            float opacity = letter < visibleLetters ? Mathf.Clamp01(baseColor.a) : 0f;
            textBuilder.Append("<color=#").Append(color)
                .Append(Mathf.RoundToInt(opacity * 255f).ToString("X2")).Append('>')
                .Append(characters[i]).Append("</color>");
            letter++;
        }
        label.text = textBuilder.ToString();
        for (int i = 0; i < glowLabels.Length; i++)
        {
            Label glow = glowLabels[i];
            if (glow == null) continue;
            glow.text = label.text;
            glow.style.color = textColor;
            float layerStrength = Mathf.Clamp01(Mathf.Max(0f, bloomIntensity) * bloom / (i + 1f));
            Color layerColor = baseColor;
            layerColor.a *= layerStrength;
            glow.style.textShadow = new TextShadow
            {
                color = layerColor,
                offset = Vector2.zero,
                blurRadius = Mathf.Max(0f, fontSize) * Mathf.Max(0f, bloomRadius) * .01f * (2f - i * .55f)
            };
            float expansion = Mathf.Max(0f, bloomRadius) * .01f * bloom * (1f - i * .3f);
            glow.style.scale = new Scale(Vector3.one * (1f + expansion));
            glow.style.opacity = layerStrength;
        }
    }

    private float EvaluatePresentation(float progress)
    {
        if (progress <= 0f) return 0f;
        if (progress >= 1f) return 1f;
        if (easing == Easing.Off) return EvaluateShape(progress);
        if (easing == Easing.In) return EvaluateShape(progress * progress);
        if (easing == Easing.Out) return EvaluateShape(1f - (1f - progress) * (1f - progress));
        if (easing == Easing.InOut)
            return EvaluateShape(progress < .5f ? 2f * progress * progress :
                1f - Mathf.Pow(-2f * progress + 2f, 2f) * .5f);
        return EvaluateShape(progress);
    }

    private float EvaluateShape(float value)
    {
        switch (presentation)
        {
            case PresentationType.Quadratic: return value * value;
            case PresentationType.Cubic: return value * value * value;
            case PresentationType.Quartic: return value * value * value * value;
            case PresentationType.Exponential: return (Mathf.Pow(2f, 10f * value) - 1f) / 1023f;
            case PresentationType.Sine: return 1f - Mathf.Cos(value * Mathf.PI * .5f);
            case PresentationType.Circular: return 1f - Mathf.Sqrt(1f - value * value);
            default: return value;
        }
    }

}

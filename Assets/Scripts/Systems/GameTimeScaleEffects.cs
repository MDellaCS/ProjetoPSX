using UnityEngine;

/// <summary>Camada global de efeitos temporarios de velocidade do jogo.</summary>
[DisallowMultipleComponent]
public sealed class GameTimeScaleEffects : MonoBehaviour
{
    private const string SettingsResourceName = "GameSlowMotionSettings";
    private static GameTimeScaleEffects instance;
    private static GameSlowMotionSettings fallbackSettings;
    [Tooltip("Asset global que define a camera lenta padrao. Se vazio, procura Resources/GameSlowMotionSettings e usa valores internos apenas como fallback.")]
    [SerializeField] private GameSlowMotionSettings slowMotionSettings;
    private float debugTimeScale = 1f;
    private float effectDuration;
    private float effectElapsed;
    private AnimationCurve effectSpeedCurve;
    private AnimationCurve effectIntensityCurve;
    private bool isEffectActive;
    private float minimumEffectTimeScale = .05f;

    public static bool IsCompletionSlowMotionActive =>
        instance != null && instance.isEffectActive;

    /// <summary>Volta a escala global ao normal antes de recarregar apos Game Over.</summary>
    public static void ResetForGameRestart()
    {
        if (instance == null)
        {
            Time.timeScale = 1f;
            return;
        }

        instance.isEffectActive = false;
        instance.effectDuration = 0f;
        instance.effectElapsed = 0f;
        instance.debugTimeScale = 1f;
        instance.ApplyCurrentScale();
    }

    /// <summary>Define a escala base usada pelos atalhos de teste.</summary>
    public static void SetDebugTimeScale(float value)
    {
        EnsureInstance().debugTimeScale = Mathf.Max(0f, value);
        instance.ApplyCurrentScale();
    }

    /// <summary>Executa a camera lenta padrao configurada globalmente.</summary>
    public static void PlayDefaultSlowMotion()
    {
        GameTimeScaleEffects effects = EnsureInstance();
        GameSlowMotionSettings settings = effects.ResolveSlowMotionSettings();
        PlayCompletionSlowMotion(settings.duration, settings.speedCurve,
            settings.intensityCurve, settings.minimumTimeScale);
    }

    /// <summary>Executa uma camera lenta global usando tempo nao escalado.</summary>
    public static void PlayCompletionSlowMotion(float duration,
        AnimationCurve speedCurve, AnimationCurve intensityCurve,
        float minimumTimeScale)
    {
        if (duration <= 0f || speedCurve == null || speedCurve.length == 0 ||
            intensityCurve == null || intensityCurve.length == 0)
            return;

        GameTimeScaleEffects effects = EnsureInstance();
        effects.effectDuration = duration;
        effects.effectElapsed = 0f;
        effects.effectSpeedCurve = new AnimationCurve(speedCurve.keys);
        effects.effectIntensityCurve = new AnimationCurve(intensityCurve.keys);
        effects.minimumEffectTimeScale = Mathf.Clamp(minimumTimeScale, .001f, 1f);
        effects.isEffectActive = true;
        effects.ApplyCurrentScale();
    }

    private static GameTimeScaleEffects EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<GameTimeScaleEffects>();
        if (instance != null)
            return instance;

        GameObject root = new("Game Time Scale Effects");
        instance = root.AddComponent<GameTimeScaleEffects>();
        DontDestroyOnLoad(root);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (GetComponentInParent<PersistentGameRoot>() == null)
            DontDestroyOnLoad(gameObject);
    }

    private GameSlowMotionSettings ResolveSlowMotionSettings()
    {
        if (slowMotionSettings != null)
            return slowMotionSettings;

        slowMotionSettings = Resources.Load<GameSlowMotionSettings>(
            SettingsResourceName);
        if (slowMotionSettings != null)
            return slowMotionSettings;

        fallbackSettings ??= ScriptableObject.CreateInstance<GameSlowMotionSettings>();
        return fallbackSettings;
    }

    private void Update()
    {
        if (!isEffectActive || GamePauseController.IsPaused)
            return;

        effectElapsed += Time.unscaledDeltaTime;
        if (effectElapsed >= effectDuration)
        {
            isEffectActive = false;
            ApplyCurrentScale();
            return;
        }

        ApplyCurrentScale();
    }

    private void ApplyCurrentScale()
    {
        if (GamePauseController.IsPaused)
            return;

        float multiplier = 1f;
        if (isEffectActive && effectDuration > 0f)
        {
            float elapsedProgress = effectElapsed / effectDuration;
            float intensityProgress = Mathf.Clamp01(
                effectSpeedCurve.Evaluate(elapsedProgress));
            multiplier = Mathf.Max(minimumEffectTimeScale,
                effectIntensityCurve.Evaluate(intensityProgress));
        }
        Time.timeScale = debugTimeScale * multiplier;
    }
}

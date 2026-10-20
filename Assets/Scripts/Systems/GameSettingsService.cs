using System;
using UnityEngine;

[Serializable]
public sealed class GameSettingsData
{
    public int version = 1;
    public float masterVolume = 1f;
    public float effectsVolume = 1f;
    public bool fullScreen;
    public bool verticalSync;
    public int frameRateCap;
    public int qualityLevel;
    public float cameraSensitivity = 1f;
    public bool invertCameraY;
    public float cameraShake = 1f;
    public float menuScale = 1f;

    public GameSettingsData Copy() => (GameSettingsData)MemberwiseClone();
}

/// <summary>Guarda preferências, aplica prévias e valida dados antigos ou inválidos.</summary>
public static class GameSettingsService
{
    private const string SaveKey = "ProjetoPSX.Settings.V1";
    private static GameSettingsData applied;
    private static GameSettingsData defaults;

    public static event Action Changed;
    public static GameSettingsData Applied
    {
        get
        {
            EnsureLoaded();
            return applied;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSession()
    {
        applied = null;
        defaults = null;
        Changed = null;
        EnsureLoaded();
    }

    public static GameSettingsData CreateDefaults()
    {
        EnsureDefaults();
        return defaults.Copy();
    }

    public static GameSettingsData Snapshot() => Applied.Copy();

    public static void Preview(GameSettingsData value)
    {
        EnsureLoaded();
        applied = Normalize(value);
        ApplyRuntime();
        Changed?.Invoke();
    }

    public static void Save(GameSettingsData value)
    {
        Preview(value);
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(applied));
        PlayerPrefs.Save();
    }

    private static void EnsureDefaults()
    {
        if (defaults != null)
            return;
        defaults = new GameSettingsData
        {
            fullScreen = Screen.fullScreen,
            verticalSync = QualitySettings.vSyncCount > 0,
            qualityLevel = QualitySettings.GetQualityLevel()
        };
    }

    private static void EnsureLoaded()
    {
        if (applied != null)
            return;

        EnsureDefaults();
        GameSettingsData loaded = null;
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try { loaded = JsonUtility.FromJson<GameSettingsData>(json); }
            catch (ArgumentException) { /* Dados inválidos voltam ao padrão. */ }
        }
        applied = Normalize(loaded != null && loaded.version == 1
            ? loaded : defaults);
        ApplyRuntime();
    }

    private static GameSettingsData Normalize(GameSettingsData value)
    {
        GameSettingsData result = (value ?? defaults).Copy();
        result.version = 1;
        result.masterVolume = ClampFinite(result.masterVolume, 0f, 1f, 1f);
        result.effectsVolume = ClampFinite(result.effectsVolume, 0f, 1f, 1f);
        result.cameraSensitivity = ClampFinite(result.cameraSensitivity, .2f, 3f, 1f);
        result.cameraShake = ClampFinite(result.cameraShake, 0f, 1f, 1f);
        result.menuScale = ClampFinite(result.menuScale, .8f, 1.4f, 1f);
        result.frameRateCap = Mathf.Clamp(result.frameRateCap, 0, 240);
        result.qualityLevel = Mathf.Clamp(result.qualityLevel, 0,
            Mathf.Max(0, QualitySettings.names.Length - 1));
        return result;
    }

    private static float ClampFinite(float value, float minimum, float maximum,
        float fallback) => float.IsNaN(value) || float.IsInfinity(value)
            ? fallback : Mathf.Clamp(value, minimum, maximum);

    private static void ApplyRuntime()
    {
        AudioListener.volume = applied.masterVolume;
        GameAudioSystem.RefreshEffectsVolume();
        if (Screen.fullScreen != applied.fullScreen)
            Screen.fullScreen = applied.fullScreen;
        QualitySettings.SetQualityLevel(applied.qualityLevel, true);
        QualitySettings.vSyncCount = applied.verticalSync ? 1 : 0;
        Application.targetFrameRate = applied.frameRateCap == 0
            ? -1 : applied.frameRateCap;
    }
}

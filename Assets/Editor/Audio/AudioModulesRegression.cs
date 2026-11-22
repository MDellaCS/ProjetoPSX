using System;
using UnityEditor;
using UnityEngine;

/// <summary>Valida as configuracoes dos modulos de audio em Resources/Audio (menu Tools/Tests).</summary>
public static class AudioModulesRegression
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static T Load<T>() where T : AudioModuleSettings
    {
        T settings = Resources.Load<T>("Audio/" + typeof(T).Name);
        Check(settings != null, "Falta Resources/Audio/" + typeof(T).Name);
        return settings;
    }

    [MenuItem("Tools/Tests/Run Audio Modules Regression")]
    public static void Run()
    {
        try
        {
            InterfaceAudioSettings ui = Load<InterfaceAudioSettings>();
            DialogueAudioSettings dialogue = Load<DialogueAudioSettings>();
            SfxAudioSettings sfx = Load<SfxAudioSettings>();

            Check(ui.volumeChannel == AudioVolumeChannel.Interface, "Interface usa o volume de interface.");
            Check(dialogue.volumeChannel == AudioVolumeChannel.Effects &&
                sfx.volumeChannel == AudioVolumeChannel.Effects, "Dialogo e SFX usam o volume de efeitos.");

            // Interface: os sons que estavam no Bootstrap foram migrados.
            Check(ui.GetCue(MenuAudioCue.Focus).HasClip && ui.GetCue(MenuAudioCue.Confirm).HasClip &&
                ui.GetCue(MenuAudioCue.Back).HasClip, "Cues de foco, confirmar e voltar migrados.");
            Check(ui.GetCue(MenuAudioCue.MenuOpen) == ui.confirm && ui.GetCue(MenuAudioCue.Toggle) == ui.confirm &&
                ui.GetCue(MenuAudioCue.MenuClose) == ui.back && ui.GetCue(MenuAudioCue.None) == null,
                "Mapeamento dos eventos semanticos de menu.");
            Check(Mathf.Abs(ui.confirm.volume - .123f) < .001f,
                "Volume migrado.");

            // Usa clipes distintos para detectar selecoes incorretas mesmo
            // quando o asset ainda compartilha os mesmos clipes de exemplo.
            SfxAudioSettings notchSettings = ScriptableObject.CreateInstance<SfxAudioSettings>();
            AudioClip[] notchClips = new AudioClip[6];
            try
            {
                for (int i = 0; i < notchClips.Length; i++)
                    notchClips[i] = AudioClip.Create("notch-test-" + i, 1, 1, 44100, false);
                notchSettings.heatFallClip = notchClips[1];
                notchSettings.heatRiseClips = new[] { notchClips[2], notchClips[3] };
                Check(notchSettings.GetHeatNotchClip(2, 1) == notchClips[1] &&
                    notchSettings.GetHeatNotchClip(1, 0) == notchClips[1],
                    "Descer Heat usa o som compartilhado.");
                Check(notchSettings.GetHeatNotchClip(0, 2) == notchClips[3] &&
                    notchSettings.GetHeatNotchClip(0, 1) == notchClips[2],
                    "Subir Heat usa o som do ultimo notch alcancado.");
                Check(notchSettings.GetHeatNotchClip(1, 1) == null,
                    "Sem mudanca de notch nao ha som.");
            }
            finally
            {
                foreach (AudioClip clip in notchClips)
                    if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(notchSettings);
            }

            // SFX migrado do asset antigo.
            Check(sfx.playerLightAttack.attackStart.HasClip && sfx.playerHurt.HasClip &&
                sfx.playerFootstep.HasClip && sfx.enemyFootstep.HasClip, "Sons de SFX preservados.");
            Check(Resources.Load("GameplayAudioSettings") == null && Resources.Load("GameAudioMixer") == null &&
                Resources.Load("Audio/WorldAudioSettings") == null,
                "Assets antigos (GameplayAudioSettings, mixer e audio espacial de mundo) nao devem existir.");

            Debug.Log("PASS: Audio modules regression.");
        }
        catch (Exception exception)
        {
            Debug.LogError("FAIL: Audio modules regression: " + exception);
        }
    }
}

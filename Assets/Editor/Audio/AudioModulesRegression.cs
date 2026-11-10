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

            // Heat: um som manual de ganho e de perda por notch.
            Check(sfx.heatNotches != null && sfx.heatNotches.Length == SfxAudioSettings.DefaultHeatNotchCount,
                "A tabela de notches deve ter " + SfxAudioSettings.DefaultHeatNotchCount + " entradas.");
            for (int notch = 1; notch <= sfx.heatNotches.Length; notch++)
            {
                Check(sfx.GetHeatNotchCue(true, notch) == sfx.heatNotches[notch - 1].gain &&
                    sfx.GetHeatNotchCue(false, notch) == sfx.heatNotches[notch - 1].loss,
                    "Notch " + notch + " usa a propria entrada.");
                Check(sfx.heatNotches[notch - 1].gain.HasClip && sfx.heatNotches[notch - 1].loss.HasClip,
                    "Notch " + notch + " migrado com som de ganho e de perda.");
            }

            Check(sfx.GetHeatNotchCue(true, 99) == sfx.heatNotches[^1].gain,
                "Notch acima da lista usa o ultimo elemento.");
            Check(sfx.GetHeatNotchCue(true, 0) == null, "Notch 0 nao toca.");
            Check(sfx.GetHeatNotchCue(true, 1).pitch < sfx.GetHeatNotchCue(true, 5).pitch,
                "A semente migrada sobe de nota a cada notch.");

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

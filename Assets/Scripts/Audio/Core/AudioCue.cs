using System;
using UnityEngine;

/// <summary>
/// Definicao configuravel de um som. Um cue pode ter variacoes equivalentes do
/// mesmo efeito para evitar repeticao mecanica. E o formato unico de som de
/// todos os modulos de audio (Interface, Dialogo e SFX).
/// </summary>
[Serializable]
public sealed class AudioCue
{
    [Tooltip("Variacoes aleatorias do mesmo efeito. Deixe vazio para desativar este som.")]
    public AudioClip[] clips = Array.Empty<AudioClip>();
    [Tooltip("Volume base deste som antes dos controles globais. Valores acima de 1 aumentam o ganho e podem saturar o audio.")]
    [Range(0f, 5f)] public float volume = 1f;
    [Tooltip("Variacao aleatoria somada ou subtraida do volume base a cada reproducao.")]
    [Range(0f, 1f)] public float volumeVariation;
    [Tooltip("Pitch base deste som. Acelera ou desacelera a reproducao (muda a duracao do som).")]
    [Min(.01f)] public float pitch = 1f;
    [Tooltip("Variacao aleatoria somada ou subtraida do pitch base a cada reproducao.")]
    [Range(0f, 1f)] public float pitchVariation;
    [Tooltip("Prioridade da voz no mixer Unity: zero e a prioridade mais alta, 255 a mais baixa.")]
    [Range(0, 255)] public int priority = 128;

    public bool HasClip
    {
        get
        {
            if (clips == null)
                return false;
            for (int i = 0; i < clips.Length; i++)
                if (clips[i] != null)
                    return true;
            return false;
        }
    }

    /// <summary>Sorteia uma variacao valida sem alocar.</summary>
    public AudioClip GetRandomClip()
    {
        if (clips == null)
            return null;

        int valid = 0;
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] != null)
                valid++;
        if (valid == 0)
            return null;

        int pick = UnityEngine.Random.Range(0, valid);
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
                continue;
            if (pick-- == 0)
                return clips[i];
        }

        return null;
    }
}

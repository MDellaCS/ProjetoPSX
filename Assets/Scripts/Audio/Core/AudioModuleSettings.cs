using UnityEngine;

/// <summary>Qual controle de volume das configuracoes do jogador afeta o modulo (alem do volume geral).</summary>
public enum AudioVolumeChannel
{
    [Tooltip("Slider de volume de efeitos.")]
    Effects,
    [Tooltip("Slider de volume de interface.")]
    Interface,
    [Tooltip("Somente o volume geral.")]
    MasterOnly
}

/// <summary>
/// Configuracao comum de um modulo de audio. Cada modulo (Interface, Dialogo,
/// Mundo, SFX) tem um asset proprio em Resources/Audio, carregado pelo nome da
/// classe, sem depender de nenhum objeto de cena.
/// </summary>
public abstract class AudioModuleSettings : ScriptableObject
{
    [Header("Modulo")]
    [Tooltip("Liga ou desliga todos os sons deste modulo sem afetar os outros.")]
    public bool moduleEnabled = true;
    [Tooltip("Volume global adicional de todos os sons deste modulo.")]
    [Range(0f, 2f)] public float globalVolume = 1f;
    [Tooltip("Pitch global aplicado a todos os sons deste modulo.")]
    [Range(.1f, 3f)] public float globalPitch = 1f;
    [Tooltip("Qual slider das configuracoes do jogador controla o volume deste modulo.")]
    public AudioVolumeChannel volumeChannel = AudioVolumeChannel.Effects;

    [Header("Vozes")]
    [Tooltip("Numero de fontes de audio criadas ao iniciar o modulo.")]
    [Min(1)] public int initialVoiceCount = 8;
    [Tooltip("Limite de fontes simultaneas antes de reutilizar a voz mais antiga.")]
    [Min(1)] public int maximumVoiceCount = 32;

    protected virtual void OnValidate()
    {
        initialVoiceCount = Mathf.Max(1, initialVoiceCount);
        maximumVoiceCount = Mathf.Max(initialVoiceCount, maximumVoiceCount);
    }
}

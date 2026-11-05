using UnityEngine;

/// <summary>Configuracao do audio de mundo (Resources/Audio/WorldAudioSettings).</summary>
[CreateAssetMenu(fileName = "WorldAudioSettings", menuName = "Projeto PSX/Audio/World Audio Settings")]
public sealed class WorldAudioSettings : AudioModuleSettings
{
    [Header("Padrao para clips avulsos")]
    [Tooltip("Quanto um clip avulso (sem cue) usa espacializacao 3D: zero e 2D, um e totalmente 3D.")]
    [Range(0f, 1f)] public float defaultSpatialBlend = 1f;
    [Tooltip("Distancia minima da fonte antes de o volume comecar a diminuir.")]
    [Min(0f)] public float defaultMinDistance = 1f;
    [Tooltip("Distancia maxima de atenuacao da fonte.")]
    [Min(.01f)] public float defaultMaxDistance = 25f;
    [Tooltip("Regra usada para diminuir o volume conforme a distancia.")]
    public AudioRolloffMode defaultRolloff = AudioRolloffMode.Logarithmic;
    [Tooltip("Prioridade das vozes de mundo no mixer Unity: zero e a mais alta.")]
    [Range(0, 255)] public int defaultPriority = 128;
}

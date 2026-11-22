using UnityEngine;

[CreateAssetMenu(menuName = "Projeto PSX/Lighting/Retro Bloom Settings", fileName = "RetroBloomSettings")]
public sealed class RetroBloomSettings : ScriptableObject
{
    [Tooltip("Ativa o halo global apenas nos materiais Retro Lit com emissao reativa ligada.")]
    public bool enabled = true;
    [Tooltip("Forca global do halo. Zero desliga o efeito; valores altos produzem brilho intenso.")]
    [Range(0f, 8f)] public float intensity = 1f;
    [Tooltip("Espalhamento do halo em pixels da textura reduzida. Valores maiores criam um halo mais amplo.")]
    [Range(0.5f, 8f)] public float radius = 2f;
    [Tooltip("Divisor da resolucao da textura do halo. Valores maiores diminuem o custo e os detalhes.")]
    [Range(2, 8)] public int downsample = 4;
    [Tooltip("Shader usado para compor o halo seletivo. Mantenha esta referencia para inclui-lo no jogo.")]
    public Shader compositeShader;
}

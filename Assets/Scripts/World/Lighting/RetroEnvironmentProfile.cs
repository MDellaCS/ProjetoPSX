using UnityEngine;

/// <summary>Luz ambiente de uma cena ou area.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Lighting/Retro Environment Profile", fileName = "RetroEnvironment")]
public sealed class RetroEnvironmentProfile : ScriptableObject
{
    [Header("Luz ambiente")]
    [Tooltip("Luz ambiente recebida pelas superficies voltadas para cima. Funciona igual em qualquer lugar da cena, inclusive perto do player.")]
    public Color ambientSkyColor = new(.16f, .16f, .18f);
    [Tooltip("Luz ambiente recebida pelas superficies voltadas para baixo. Cor mais escura que a de cima da volume as formas sem nenhuma luz.")]
    public Color ambientGroundColor = new(.07f, .06f, .06f);
    [Tooltip("Multiplicador das duas cores ambiente. Zero deixa o mundo escuro, iluminado so pelas luzes locais.")]
    [Min(0f)] public float ambientIntensity = 1f;
    [Tooltip("Intensidade dos reflexos do skybox nos materiais URP Lit (fora do Retro Lit). Zero remove o brilho azulado do skybox padrao.")]
    [Range(0f, 1f)] public float unityReflectionIntensity;

    [Header("Skybox")]
    [Tooltip("Material de skybox aplicado durante este ambiente. Se vazio, usa o skybox definido na cena.")]
    public Material skyboxMaterial;

    [Header("Nevoa")]
    [Tooltip("Perfil de nevoa aplicado junto com este ambiente. Sem perfil, a nevoa fica desligada.")]
    public FogProfile fogProfile;

    [Header("Chuva")]
    [Tooltip("Perfil de chuva do ambiente. Sem perfil, a chuva para gradualmente.")]
    public RainProfile rainProfile;

    public Color AmbientSky => ambientSkyColor * ambientIntensity;
    public Color AmbientGround => ambientGroundColor * ambientIntensity;

    private void OnValidate()
    {
        RetroLightSystem.MarkEnvironmentDirty();
    }
}

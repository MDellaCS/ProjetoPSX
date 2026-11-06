using UnityEngine;

public enum RetroFogCenter
{
    [Tooltip("Distancia medida a partir da camera, como a nevoa classica de PS1/PS2.")]
    Camera,
    [Tooltip("Distancia medida a partir do alvo da camera (o player). A nevoa nao muda quando a camera se afasta ou aproxima.")]
    Player
}

/// <summary>Luz ambiente e nevoa de uma cena ou area.</summary>
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

    [Header("Nevoa")]
    [Tooltip("Liga a nevoa de distancia, calculada no proprio shader Retro Lit (por vertice no modo Vertex, como no PS2).")]
    public bool fogEnabled;
    [Tooltip("Cor da nevoa. Use tons proximos da luz ambiente para o mundo sumir de forma natural.")]
    public Color fogColor = new(.32f, .33f, .33f);
    [Tooltip("Origem da distancia da nevoa: a camera ou o player.")]
    public RetroFogCenter fogCenter = RetroFogCenter.Player;
    [Tooltip("Distancia, em metros, onde a nevoa comeca.")]
    [Min(0f)] public float fogStart = 6f;
    [Tooltip("Distancia, em metros, onde a nevoa atinge a opacidade maxima.")]
    [Min(.01f)] public float fogEnd = 24f;
    [Tooltip("Opacidade maxima da nevoa. 1 esconde totalmente o que esta alem do fim.")]
    [Range(0f, 1f)] public float fogMaxOpacity = 1f;
    [Tooltip("Quanto as luzes locais atravessam a nevoa. 0: a nevoa cobre tudo igual; 1: superficies bem iluminadas continuam visiveis dentro da nevoa, como a lanterna em Silent Hill 2.")]
    [Range(0f, 1f)] public float fogLightPierce = .6f;
    [Tooltip("Limita a nevoa a uma camada perto do chao.")]
    public bool heightFog;
    [Tooltip("Altura da base da camada de nevoa em relacao a origem da nevoa (camera ou player), em metros.")]
    public float heightFogBaseOffset = -1f;
    [Tooltip("Espessura da camada de nevoa acima da base, em metros.")]
    [Min(.01f)] public float heightFogThickness = 4f;

    public Color AmbientSky => ambientSkyColor * ambientIntensity;
    public Color AmbientGround => ambientGroundColor * ambientIntensity;

    private void OnValidate()
    {
        fogEnd = Mathf.Max(fogStart + .01f, fogEnd);
        RetroLightSystem.MarkEnvironmentDirty();
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ferramenta de level design: recorta desta malha tudo que esta dentro de
/// outras malhas fechadas (a "tuneladora"), de forma permanente, afetando o
/// visual e a colisao. O corte e feito no Editor (veja o Inspector deste
/// componente); em tempo de execucao este componente nao faz nada.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[AddComponentMenu("Projeto PSX/Level Design/Mesh Tunnel Cut")]
public sealed class MeshTunnelCut : MonoBehaviour
{
    [Header("Tuneladoras")]
    [Tooltip("Objetos cujo volume sera removido DESTA malha. Cada um precisa ter MeshFilter com uma malha FECHADA (sem buracos), como um cano com tampas. Use o botao 'Adicionar Selecionados' para preencher.")]
    [SerializeField] private List<MeshFilter> cutters = new();

    [Header("Qualidade")]
    [Tooltip("Tamanho maximo, em metros, das arestas perto do corte. Valores MAIORES geram MENOS triangulos (borda do corte mais angular); menores deixam a borda mais suave. Uma boa referencia e a metade do raio do cano. A malha longe do corte nao muda.")]
    [Min(.005f)] [SerializeField] private float maxEdgeLength = .15f;
    [Tooltip("Depois do corte, funde os vertices extras que nao mudam a forma (regioes planas e arestas retas). Reduz bastante a contagem de triangulos sem alterar o visual nem a colisao. Desmarque so para diagnosticar.")]
    [SerializeField] private bool simplifyResult = true;
    [Tooltip("Expande (positivo) ou encolhe (negativo) o volume das tuneladoras, em metros. Use um valor pequeno positivo para deixar uma folga entre os canos.")]
    [SerializeField] private float cutterMargin;

    [Header("Resultado")]
    [Tooltip("Gera um MeshCollider para a malha recortada, copiando as configuracoes de layer do objeto original.")]
    [SerializeField] private bool generateCollider = true;
    [Tooltip("Depois do corte, desliga o visual e os colisores do objeto original (ele continua na cena para poder refazer o corte).")]
    [SerializeField] private bool disableOriginalAfterCut = true;

    [Header("Estado (somente leitura)")]
    [SerializeField] private GameObject resultObject;
    [SerializeField] private string resultMeshPath;
    [SerializeField] private List<Collider> disabledColliders = new();
    [SerializeField] private bool originalRendererWasEnabled = true;
    [TextArea(2, 8)] [SerializeField] private string lastReport;

    public List<MeshFilter> Cutters => cutters;
    public float MaxEdgeLength => maxEdgeLength;
    public bool SimplifyResult => simplifyResult;
    public float CutterMargin => cutterMargin;
    public bool GenerateCollider => generateCollider;
    public bool DisableOriginalAfterCut => disableOriginalAfterCut;
    public GameObject ResultObject { get => resultObject; set => resultObject = value; }
    public string ResultMeshPath { get => resultMeshPath; set => resultMeshPath = value; }
    public List<Collider> DisabledColliders => disabledColliders;
    public bool OriginalRendererWasEnabled
    {
        get => originalRendererWasEnabled;
        set => originalRendererWasEnabled = value;
    }
    public string LastReport { get => lastReport; set => lastReport = value; }
}

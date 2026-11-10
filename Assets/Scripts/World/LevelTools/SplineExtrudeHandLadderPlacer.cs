using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Gera uma HandLadder curva ao longo da parede de uma extrusao circular.
/// A geracao e acionada manualmente pelo Inspector; este componente nao altera
/// nem reconstrui a malha do SplineExtrude.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Level Design/Spline Extrude Hand Ladder Placer")]
public sealed class SplineExtrudeHandLadderPlacer : MonoBehaviour
{
    private const int ArcLengthSubdivisions = 512;

    [Header("Origem")]
    [Tooltip("Extrusao circular usada como origem do tubo. Se vazio, procura um SplineExtrude no mesmo objeto.")]
    [SerializeField] private SplineExtrude source;
    [Tooltip("Indice do spline dentro do Spline Container usado pela extrusao.")]
    [Min(0)] [SerializeField] private int splineIndex;

    [Header("Posicao na parede")]
    [Tooltip("Angulo ao redor do tubo, em graus, medido a partir do vetor Up da spline. Ajuste para escolher a parede desejada.")]
    [Range(0f, 360f)] [SerializeField] private float wallAngleDegrees;
    [Tooltip("Marcado coloca a escada na face interna do tubo e orienta o jogador para dentro. Desmarcado usa a face externa.")]
    [SerializeField] private bool insideTube = true;
    [Tooltip("Distancia em metros entre os trilhos da escada e a parede. Na face interna, valores positivos movem a escada para o centro do tubo.")]
    [Min(0f)] [SerializeField] private float wallClearance = .06f;
    [Tooltip("Largura da escada em metros, medida sobre a circunferencia do tubo.")]
    [Min(.2f)] [SerializeField] private float ladderWidth = .8f;

    [Header("Trecho e amostragem")]
    [Tooltip("Comprimento em metros removido do inicio do trecho extrudido. Deixa a escada afastada da borda do tubo.")]
    [Min(0f)] [SerializeField] private float startTrim;
    [Tooltip("Comprimento em metros removido do fim do trecho extrudido. Deixa a escada afastada da borda do tubo.")]
    [Min(0f)] [SerializeField] private float endTrim;
    [Tooltip("Distancia aproximada em metros entre pontos que definem a curvatura. Menor acompanha melhor curvas e aumenta o numero de pecas geradas.")]
    [Min(.08f)] [SerializeField] private float sampleSpacing = .3f;

    [Header("Escada")]
    [Tooltip("Perfil opcional da escada. Define degraus, trilhos e comportamento; vazio mantem a escalada continua sem gerar pecas visuais.")]
    [SerializeField] private HandLadderProfile ladderProfile;

    [Tooltip("Objeto gerado por esta ferramenta. Atualize ou remova pelo Inspector do componente.")]
    [SerializeField, HideInInspector] private Transform generatedRoot;

    public SplineExtrude Source => source;
    public int SplineIndex => splineIndex;
    public float WallAngleDegrees => wallAngleDegrees;
    public bool InsideTube => insideTube;
    public float WallClearance => wallClearance;
    public float LadderWidth => ladderWidth;
    public float StartTrim => startTrim;
    public float EndTrim => endTrim;
    public float SampleSpacing => sampleSpacing;
    public HandLadderProfile LadderProfile => ladderProfile;
    public Transform GeneratedRoot => generatedRoot;

    private void Reset()
    {
        if (TryGetComponent(out SplineExtrude extrude))
            source = extrude;
    }

    public void SetSource(SplineExtrude value) => source = value;
    public void SetLadderProfile(HandLadderProfile value) => ladderProfile = value;
    public void SetGeneratedRoot(Transform value) => generatedRoot = value;

    /// <summary>Calcula a espinha da escada em coordenadas de mundo.</summary>
    public bool TryBuildSpine(out HandLadderSpine spine, out float length,
        out Vector3 firstPoint, out Vector3 firstTangent, out Vector3 firstApproach,
        out string error)
    {
        spine = null;
        length = 0f;
        firstPoint = Vector3.zero;
        firstTangent = Vector3.up;
        firstApproach = Vector3.forward;
        error = null;

        if (source == null)
        {
            error = "Atribua um SplineExtrude ou coloque este componente no mesmo objeto.";
            return false;
        }

        SplineContainer container = source.Container;
        if (container == null || source.Splines == null || source.Splines.Count == 0)
        {
            error = "O SplineExtrude precisa de um Spline Container com pelo menos um spline.";
            return false;
        }

        int selectedSplineIndex = Mathf.Clamp(splineIndex, 0, source.Splines.Count - 1);
        if (source.Splines[selectedSplineIndex] == null)
        {
            error = "O indice selecionado nao aponta para um spline valido.";
            return false;
        }

        Vector2 range = source.Range;
        float rangeStart = Mathf.Clamp01(Mathf.Min(range.x, range.y));
        float rangeEnd = Mathf.Clamp01(Mathf.Max(range.x, range.y));
        if (rangeEnd - rangeStart < .0001f)
        {
            error = "O Range do SplineExtrude precisa ter comprimento maior que zero.";
            return false;
        }

        // Tabela densa em espaco de mundo: o parametro t nao representa
        // distancias uniformes, especialmente em curvas ou com escala.
        List<ArcSample> arc = BuildArcTable(container, selectedSplineIndex, rangeStart, rangeEnd);
        float arcLength = arc[arc.Count - 1].distance;
        float effectiveStart = Mathf.Clamp(startTrim, 0f, arcLength);
        float effectiveEnd = Mathf.Clamp(endTrim, 0f, arcLength - effectiveStart);
        length = arcLength - effectiveStart - effectiveEnd;
        if (length < .3f)
        {
            error = "O trecho restante e curto demais para gerar uma escada.";
            return false;
        }

        int count = Mathf.Clamp(Mathf.CeilToInt(length / Mathf.Max(.08f, sampleSpacing)) + 1,
            2, 1024);
        spine = new HandLadderSpine
        {
            progress = new float[count],
            left = new Vector3[count],
            center = new Vector3[count],
            right = new Vector3[count],
            normal = new Vector3[count]
        };

        float halfWidth = ladderWidth * .5f;
        float pathLength = length;
        for (int i = 0; i < count; i++)
        {
            float progress = pathLength * i / (count - 1);
            float distance = effectiveStart + progress;
            float t = FindTAtDistance(arc, distance);
            Vector3 tangent = EvaluateWorldTangent(container, selectedSplineIndex,
                t, rangeStart, rangeEnd);
            if (tangent.sqrMagnitude < .0001f)
            {
                error = "Nao foi possivel calcular a direcao da spline em uma das amostras.";
                spine = null;
                return false;
            }

            Vector3 up = (Vector3)container.EvaluateUpVector(selectedSplineIndex, t);
            up = Vector3.ProjectOnPlane(up, tangent).normalized;
            if (up.sqrMagnitude < .0001f)
            {
                Vector3 fallback = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) < .95f
                    ? Vector3.up : Vector3.right;
                up = Vector3.ProjectOnPlane(fallback, tangent).normalized;
            }

            Vector3 radial = Quaternion.AngleAxis(wallAngleDegrees, tangent) * up;
            radial = Vector3.ProjectOnPlane(radial, tangent).normalized;
            Vector3 approach = insideTube ? -radial : radial;
            Vector3 across = Vector3.Cross(tangent, approach).normalized;
            if (across.sqrMagnitude < .0001f)
            {
                error = "A orientacao local do tubo nao permite posicionar a largura da escada.";
                spine = null;
                return false;
            }

            Vector3 axis = (Vector3)container.EvaluatePosition(selectedSplineIndex, t);
            float wallRadius = Mathf.Max(.001f, source.Radius +
                (insideTube ? -wallClearance : wallClearance));
            Vector3 center = axis + radial * wallRadius;

            spine.progress[i] = progress;
            spine.center[i] = center;
            spine.left[i] = center - across * halfWidth;
            spine.right[i] = center + across * halfWidth;
            spine.normal[i] = approach;

            if (i == 0)
            {
                firstPoint = center;
                firstTangent = tangent;
                firstApproach = approach;
            }
        }

        return true;
    }

    private readonly struct ArcSample
    {
        public readonly float t;
        public readonly float distance;
        public ArcSample(float t, float distance) { this.t = t; this.distance = distance; }
    }

    private static List<ArcSample> BuildArcTable(SplineContainer container,
        int index, float start, float end)
    {
        List<ArcSample> samples = new(ArcLengthSubdivisions + 1);
        Vector3 previous = (Vector3)container.EvaluatePosition(index, start);
        samples.Add(new ArcSample(start, 0f));
        float distance = 0f;
        for (int i = 1; i <= ArcLengthSubdivisions; i++)
        {
            float t = Mathf.Lerp(start, end, i / (float)ArcLengthSubdivisions);
            Vector3 position = (Vector3)container.EvaluatePosition(index, t);
            distance += Vector3.Distance(previous, position);
            samples.Add(new ArcSample(t, distance));
            previous = position;
        }
        return samples;
    }

    private static Vector3 EvaluateWorldTangent(SplineContainer container,
        int splineIndex, float t, float rangeStart, float rangeEnd)
    {
        Vector3 tangent = (Vector3)container.EvaluateTangent(splineIndex, t);
        if (tangent.sqrMagnitude > .000001f)
            return tangent.normalized;

        // Em segmentos lineares degenerados ou junto a nos, amostrar a posicao
        // vizinha evita perder a orientacao da escada quando a tangente analitica
        // nao esta disponivel.
        float delta = .001f;
        float before = Mathf.Max(rangeStart, t - delta);
        float after = Mathf.Min(rangeEnd, t + delta);
        if (after - before < .000001f)
            return Vector3.zero;

        Vector3 a = (Vector3)container.EvaluatePosition(splineIndex, before);
        Vector3 b = (Vector3)container.EvaluatePosition(splineIndex, after);
        tangent = b - a;
        return tangent.sqrMagnitude > .000001f ? tangent.normalized : Vector3.zero;
    }

    private static float FindTAtDistance(List<ArcSample> samples, float target)
    {
        int low = 0;
        int high = samples.Count - 1;
        while (low < high - 1)
        {
            int mid = (low + high) / 2;
            if (samples[mid].distance < target) low = mid;
            else high = mid;
        }

        ArcSample a = samples[low];
        ArcSample b = samples[high];
        float span = b.distance - a.distance;
        float blend = span > .00001f ? (target - a.distance) / span : 0f;
        return Mathf.Lerp(a.t, b.t, blend);
    }

    private void OnDrawGizmosSelected()
    {
        if (!InteractionGizmos.Visible || !TryBuildSpine(out HandLadderSpine preview,
                out _, out _, out _, out _, out _))
            return;

        Gizmos.color = new Color(.1f, .55f, .22f, .9f);
        for (int i = 0; i < preview.Count - 1; i++)
        {
            Gizmos.DrawLine(preview.left[i], preview.left[i + 1]);
            Gizmos.DrawLine(preview.right[i], preview.right[i + 1]);
            Gizmos.DrawLine(preview.center[i], preview.center[i + 1]);
        }
        Gizmos.DrawLine(preview.left[0], preview.right[0]);
        Gizmos.DrawLine(preview.left[preview.Count - 1], preview.right[preview.Count - 1]);
    }
}

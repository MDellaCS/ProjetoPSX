using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Vertice completo de uma malha a ser cortada.</summary>
public struct CutVertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;
    public Color color;

    public static CutVertex Lerp(CutVertex a, CutVertex b, float t)
    {
        Vector3 normal = Vector3.Lerp(a.normal, b.normal, t);
        return new CutVertex
        {
            position = Vector3.Lerp(a.position, b.position, t),
            normal = normal.sqrMagnitude > 1e-12f ? normal.normalized : a.normal,
            uv = Vector2.Lerp(a.uv, b.uv, t),
            color = Color.Lerp(a.color, b.color, t)
        };
    }
}

/// <summary>Malha em espaco do mundo, com submalhas (um material por submalha).</summary>
public sealed class CutMesh
{
    public readonly List<CutVertex> vertices = new();
    public readonly List<List<int>> submeshes = new();
    public bool hasNormals = true;
    public bool hasUVs = true;
    public bool hasColors;
}

public sealed class TunnelCutStats
{
    public int inputTriangles;
    public int outputTriangles;
    public int refinementRounds;
    public bool hitRoundLimit;
    public bool hitTriangleLimit;
    public readonly List<string> warnings = new();
}

/// <summary>
/// Subtracao de solidos ("tuneladora"): remove de uma malha tudo que esta dentro
/// de uma ou mais malhas fechadas. O algoritmo refina so os triangulos perto da
/// superficie do cortador, de forma conforme (sem T-junctions, pois a decisao de
/// dividir e tomada por aresta), e recorta os triangulos restantes exatamente na
/// superficie do cortador por bissecao.
/// </summary>
public static class TunnelCutCore
{
    private const float PositionCell = 1e-5f;
    private const int MaximumRounds = 24;
    private const int MaximumTriangles = 3_000_000;
    private const int BisectionSteps = 18;

    private struct Tri
    {
        public CutVertex a, b, c;
        public int pa, pb, pc;
        public int sub;
        public int origin;
    }

    private sealed class State
    {
        public readonly Dictionary<Vector3Int, int> positionIds = new();
        public readonly List<Vector3> positions = new();
        public readonly List<float> gCache = new();
        public readonly Dictionary<long, int> midpointIds = new();
        public readonly Dictionary<long, float> crossingT = new();
        public IReadOnlyList<TunnelSolid> cutters;
        public float margin;

        public int PositionId(Vector3 p)
        {
            Vector3Int key = new(Mathf.RoundToInt(p.x / PositionCell),
                Mathf.RoundToInt(p.y / PositionCell), Mathf.RoundToInt(p.z / PositionCell));
            if (positionIds.TryGetValue(key, out int id))
                return id;
            id = positions.Count;
            positionIds.Add(key, id);
            positions.Add(p);
            gCache.Add(float.NaN);
            return id;
        }

        // Funcao distancia 1-Lipschitz: negativa dentro de qualquer cortador
        // (expandido por 'margin'), positiva fora.
        public float G(Vector3 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < cutters.Count; i++)
                best = Mathf.Min(best, cutters[i].Signed(p));
            return best - margin;
        }

        public float GAt(int positionId)
        {
            float cached = gCache[positionId];
            if (float.IsNaN(cached))
            {
                cached = G(positions[positionId]);
                gCache[positionId] = cached;
            }
            return cached;
        }
    }

    /// <summary>
    /// Remove de <paramref name="source"/> as partes dentro dos cortadores.
    /// <paramref name="margin"/> expande (positivo) ou reduz (negativo) o cortador.
    /// <paramref name="maxEdge"/> e o tamanho maximo de aresta perto do corte.
    /// </summary>
    public static CutMesh Cut(CutMesh source, IReadOnlyList<TunnelSolid> cutters,
        float margin, float maxEdge, out TunnelCutStats stats, bool simplify = true)
    {
        stats = new TunnelCutStats();
        maxEdge = Mathf.Max(.005f, maxEdge);
        State state = new() { cutters = cutters, margin = margin };

        List<Tri> tris = new();
        for (int s = 0; s < source.submeshes.Count; s++)
        {
            List<int> indices = source.submeshes[s];
            for (int i = 0; i + 2 < indices.Count; i += 3)
            {
                CutVertex a = source.vertices[indices[i]];
                CutVertex b = source.vertices[indices[i + 1]];
                CutVertex c = source.vertices[indices[i + 2]];
                tris.Add(new Tri
                {
                    a = a, b = b, c = c, sub = s, origin = tris.Count,
                    pa = state.PositionId(a.position),
                    pb = state.PositionId(b.position),
                    pc = state.PositionId(c.position)
                });
            }
        }

        stats.inputTriangles = tris.Count;
        tris = Refine(state, tris, maxEdge, stats);
        List<Tri> result = Clip(state, tris);
        if (simplify)
            result = Simplify(state, result);
        stats.outputTriangles = result.Count;
        return Assemble(source, result);
    }

    // ---------------------------------------------------------------------
    // Refinamento conforme: a decisao de dividir e por ARESTA.
    // ---------------------------------------------------------------------

    private static List<Tri> Refine(State state, List<Tri> tris, float maxEdge,
        TunnelCutStats stats)
    {
        for (int round = 0; round < MaximumRounds; round++)
        {
            HashSet<long> marked = new();
            for (int t = 0; t < tris.Count; t++)
            {
                Tri tri = tris[t];
                Vector3 pa = state.positions[tri.pa];
                Vector3 pb = state.positions[tri.pb];
                Vector3 pc = state.positions[tri.pc];
                float lab = (pb - pa).magnitude;
                float lbc = (pc - pb).magnitude;
                float lca = (pa - pc).magnitude;
                float longest = Mathf.Max(lab, Mathf.Max(lbc, lca));
                if (longest <= maxEdge)
                    continue;

                Vector3 center = (pa + pb + pc) / 3f;
                float radius = Mathf.Max((pa - center).magnitude,
                    Mathf.Max((pb - center).magnitude, (pc - center).magnitude));
                float gc = state.G(center);
                if (gc - radius >= 0f || gc + radius < 0f)
                    continue;
                // A bola envolvente e conservadora para triangulos finos e
                // longos. Uma amostragem em grade cobre o triangulo com um
                // raio bem menor e evita dividir pedacos que nao tocam o corte.
                if (IsClearOfSurface(state, pa, pb, pc, longest))
                    continue;

                if (longest == lab)
                    marked.Add(EdgeKey(tri.pa, tri.pb));
                else if (longest == lbc)
                    marked.Add(EdgeKey(tri.pb, tri.pc));
                else
                    marked.Add(EdgeKey(tri.pc, tri.pa));
            }

            if (marked.Count == 0)
            {
                stats.refinementRounds = round;
                return tris;
            }

            List<Tri> next = new(tris.Count + marked.Count * 2);
            for (int t = 0; t < tris.Count; t++)
                SplitTriangle(state, tris[t], marked, next);
            tris = next;
            stats.refinementRounds = round + 1;

            if (tris.Count > MaximumTriangles)
            {
                stats.hitTriangleLimit = true;
                stats.warnings.Add("Limite de triangulos atingido durante o refinamento; aumente 'Max Edge Length'.");
                return tris;
            }
        }

        stats.hitRoundLimit = true;
        stats.warnings.Add("Limite de rodadas de refinamento atingido; o corte pode estar grosseiro. Aumente 'Max Edge Length'.");
        return tris;
    }

    private const int CoverageGrid = 4;

    // Verdadeiro se o triangulo inteiro esta, com certeza, fora ou dentro do
    // cortador. Como G e 1-Lipschitz, todo ponto do triangulo esta a no maximo
    // 'cover' de alguma amostra; se todas as amostras estao alem de 'cover' da
    // superficie (mesmo sinal), nenhuma parte do triangulo pode cruza-la.
    private static bool IsClearOfSurface(State state, Vector3 a, Vector3 b,
        Vector3 c, float longestEdge)
    {
        float cover = longestEdge / CoverageGrid * .5774f;
        float minimum = float.MaxValue;
        float maximum = float.MinValue;
        for (int i = 0; i <= CoverageGrid; i++)
        {
            for (int j = 0; j <= CoverageGrid - i; j++)
            {
                int k = CoverageGrid - i - j;
                Vector3 point = (a * i + b * j + c * k) / CoverageGrid;
                float g = state.G(point);
                minimum = Mathf.Min(minimum, g);
                maximum = Mathf.Max(maximum, g);
                if (minimum < cover && maximum >= -cover)
                    return false;
            }
        }

        return minimum >= cover || maximum < -cover;
    }

    private static void SplitTriangle(State state, Tri tri, HashSet<long> marked,
        List<Tri> output)
    {
        bool m01 = marked.Contains(EdgeKey(tri.pa, tri.pb));
        bool m12 = marked.Contains(EdgeKey(tri.pb, tri.pc));
        bool m20 = marked.Contains(EdgeKey(tri.pc, tri.pa));
        int count = (m01 ? 1 : 0) + (m12 ? 1 : 0) + (m20 ? 1 : 0);
        if (count == 0)
        {
            output.Add(tri);
            return;
        }

        // Rotaciona para que, com 1 ou 2 arestas marcadas, os casos fiquem
        // sempre na mesma forma (preservando o winding).
        CutVertex[] v = { tri.a, tri.b, tri.c };
        int[] p = { tri.pa, tri.pb, tri.pc };
        bool[] marks = { m01, m12, m20 };
        int rotation = 0;
        if (count == 1)
            rotation = m01 ? 0 : m12 ? 1 : 2;       // aresta marcada fica em (0,1)
        else if (count == 2)
            rotation = !m20 ? 0 : !m01 ? 1 : 2;     // aresta livre fica em (2,0)

        CutVertex v0 = v[rotation], v1 = v[(rotation + 1) % 3], v2 = v[(rotation + 2) % 3];
        int p0 = p[rotation], p1 = p[(rotation + 1) % 3], p2 = p[(rotation + 2) % 3];

        if (count == 3)
        {
            CutVertex a01 = Mid(state, v0, p0, v1, p1, out int i01);
            CutVertex a12 = Mid(state, v1, p1, v2, p2, out int i12);
            CutVertex a20 = Mid(state, v2, p2, v0, p0, out int i20);
            Add(output, tri, v0, p0, a01, i01, a20, i20);
            Add(output, tri, a01, i01, v1, p1, a12, i12);
            Add(output, tri, a20, i20, a12, i12, v2, p2);
            Add(output, tri, a01, i01, a12, i12, a20, i20);
        }
        else if (count == 2)
        {
            // Marcadas (0,1) e (1,2); livre (2,0).
            CutVertex a01 = Mid(state, v0, p0, v1, p1, out int i01);
            CutVertex a12 = Mid(state, v1, p1, v2, p2, out int i12);
            Add(output, tri, a01, i01, v1, p1, a12, i12);
            // Quadrilatero (v0, a01, a12, v2): usa a menor diagonal.
            float diagonalA = (state.positions[p0] - state.positions[i12]).sqrMagnitude;
            float diagonalB = (state.positions[i01] - state.positions[p2]).sqrMagnitude;
            if (diagonalA <= diagonalB)
            {
                Add(output, tri, v0, p0, a01, i01, a12, i12);
                Add(output, tri, v0, p0, a12, i12, v2, p2);
            }
            else
            {
                Add(output, tri, v0, p0, a01, i01, v2, p2);
                Add(output, tri, a01, i01, a12, i12, v2, p2);
            }
        }
        else
        {
            CutVertex a01 = Mid(state, v0, p0, v1, p1, out int i01);
            Add(output, tri, v0, p0, a01, i01, v2, p2);
            Add(output, tri, a01, i01, v1, p1, v2, p2);
        }
    }

    private static CutVertex Mid(State state, CutVertex a, int ia, CutVertex b,
        int ib, out int midId)
    {
        long key = EdgeKey(ia, ib);
        if (!state.midpointIds.TryGetValue(key, out midId))
        {
            int low = Mathf.Min(ia, ib), high = Mathf.Max(ia, ib);
            midId = state.PositionId((state.positions[low] + state.positions[high]) * .5f);
            state.midpointIds.Add(key, midId);
        }

        CutVertex mid = CutVertex.Lerp(a, b, .5f);
        mid.position = state.positions[midId];
        return mid;
    }

    private static void Add(List<Tri> output, Tri parent, CutVertex a, int pa,
        CutVertex b, int pb, CutVertex c, int pc)
    {
        output.Add(new Tri { a = a, b = b, c = c, pa = pa, pb = pb, pc = pc, sub = parent.sub, origin = parent.origin });
    }

    // ---------------------------------------------------------------------
    // Recorte final na superficie do cortador.
    // ---------------------------------------------------------------------

    private static List<Tri> Clip(State state, List<Tri> tris)
    {
        List<Tri> output = new(tris.Count);
        for (int t = 0; t < tris.Count; t++)
        {
            Tri tri = tris[t];
            bool inA = state.GAt(tri.pa) < 0f;
            bool inB = state.GAt(tri.pb) < 0f;
            bool inC = state.GAt(tri.pc) < 0f;
            int inside = (inA ? 1 : 0) + (inB ? 1 : 0) + (inC ? 1 : 0);
            if (inside == 0)
            {
                output.Add(tri);
                continue;
            }

            if (inside == 3)
                continue;

            CutVertex[] v = { tri.a, tri.b, tri.c };
            int[] p = { tri.pa, tri.pb, tri.pc };
            bool[] isInside = { inA, inB, inC };

            if (inside == 1)
            {
                int i = isInside[0] ? 0 : isInside[1] ? 1 : 2;
                int j = (i + 1) % 3, k = (i + 2) % 3;
                CutVertex xij = Crossing(state, v[i], p[i], v[j], p[j], out int pij);
                CutVertex xik = Crossing(state, v[i], p[i], v[k], p[k], out int pik);
                Emit(output, tri, xij, pij, v[j], p[j], v[k], p[k]);
                Emit(output, tri, xij, pij, v[k], p[k], xik, pik);
            }
            else
            {
                int k = !isInside[0] ? 0 : !isInside[1] ? 1 : 2;
                int i = (k + 1) % 3, j = (k + 2) % 3;
                // (i, j) dentro; k fora; ciclo (i, j, k).
                CutVertex xjk = Crossing(state, v[j], p[j], v[k], p[k], out int pjk);
                CutVertex xki = Crossing(state, v[k], p[k], v[i], p[i], out int pki);
                Emit(output, tri, xjk, pjk, v[k], p[k], xki, pki);
            }
        }

        return output;
    }

    private static void Emit(List<Tri> output, Tri parent, CutVertex a, int pa,
        CutVertex b, int pb, CutVertex c, int pc)
    {
        Vector3 pointA = a.position, pointB = b.position, pointC = c.position;
        if (Vector3.Cross(pointB - pointA, pointC - pointA).sqrMagnitude < 1e-16f)
            return;
        output.Add(new Tri { a = a, b = b, c = c, pa = pa, pb = pb, pc = pc, sub = parent.sub, origin = parent.origin });
    }

    // Ponto exato da superficie do cortador sobre a aresta (from -> to), onde
    // 'from' esta dentro e 'to' fora, ou o inverso. O calculo e canonico (por
    // ids de posicao ordenados), entao os dois triangulos que dividem a aresta
    // obtem exatamente o mesmo ponto.
    private static CutVertex Crossing(State state, CutVertex from, int pFrom,
        CutVertex to, int pTo, out int crossingPositionId)
    {
        int low = Mathf.Min(pFrom, pTo), high = Mathf.Max(pFrom, pTo);
        long key = EdgeKey(low, high);
        Vector3 lowPosition = state.positions[low];
        Vector3 highPosition = state.positions[high];
        if (!state.crossingT.TryGetValue(key, out float t))
        {
            float lowValue = state.GAt(low);
            float lo = 0f, hi = 1f;
            bool lowInside = lowValue < 0f;
            for (int step = 0; step < BisectionSteps; step++)
            {
                float mid = (lo + hi) * .5f;
                bool midInside = state.G(Vector3.Lerp(lowPosition, highPosition, mid)) < 0f;
                if (midInside == lowInside)
                    lo = mid;
                else
                    hi = mid;
            }

            t = (lo + hi) * .5f;
            state.crossingT.Add(key, t);
        }

        Vector3 position = Vector3.Lerp(lowPosition, highPosition, t);
        crossingPositionId = state.PositionId(position);
        // Fracao do caminho de 'from' ate 'to' em que a superficie e cruzada.
        CutVertex result = CutVertex.Lerp(from, to, pFrom == low ? t : 1f - t);
        result.position = position;
        return result;
    }


    // ---------------------------------------------------------------------
    // Simplificacao: remove os vertices internos criados pelo refinamento.
    // ---------------------------------------------------------------------

    // Dentro de um mesmo triangulo original (plano unico), um vertice cujas
    // arestas estao todas fechadas (sem borda de corte) e cujos triangulos
    // vieram do mesmo triangulo pode ser fundido em um vizinho sem mudar a
    // forma, o UV nem a normal interpolados (todos sao funcoes lineares da
    // posicao). Os vertices das bordas e do corte nunca se movem.
    private static List<Tri> Simplify(State state, List<Tri> tris)
    {
        int count = tris.Count;
        Tri[] data = tris.ToArray();
        bool[] dead = new bool[count];
        Dictionary<int, List<int>> adjacency = new();
        for (int t = 0; t < count; t++)
        {
            AddAdjacency(adjacency, data[t].pa, t);
            AddAdjacency(adjacency, data[t].pb, t);
            AddAdjacency(adjacency, data[t].pc, t);
        }

        bool changed = true;
        int guard = 0;
        while (changed && guard++ < 64)
        {
            changed = false;
            List<int> vertices = new(adjacency.Keys);
            foreach (int v in vertices)
            {
                if (TryGetInterior(data, dead, adjacency, v, out List<int> around))
                {
                    if (TryCollapse(state, data, dead, adjacency, v, around, null))
                        changed = true;
                }
                else if (TryGetStraightEdgeVertex(state, data, dead, adjacency, v,
                    out List<int> edgeAround, out int p, out int q) &&
                    TryCollapse(state, data, dead, adjacency, v, edgeAround,
                        new List<int> { p, q }))
                {
                    changed = true;
                }
            }
        }

        List<Tri> result = new(count);
        for (int t = 0; t < count; t++)
            if (!dead[t])
                result.Add(data[t]);
        return result;
    }


    // Vertice sobre uma aresta reta compartilhada por dois triangulos originais:
    // seus triangulos vem de exatamente dois grupos, so duas arestas cruzam de
    // um grupo para o outro (p e q), todas as demais sao internas a um grupo e
    // p, v, q estao alinhados. Fundi-lo em p ou q nao altera a forma.
    private static bool TryGetStraightEdgeVertex(State state, Tri[] data, bool[] dead,
        Dictionary<int, List<int>> adjacency, int v, out List<int> around,
        out int p, out int q)
    {
        around = new List<int>();
        p = q = -1;
        if (!adjacency.TryGetValue(v, out List<int> list))
            return false;

        foreach (int t in list)
            if (!dead[t] && Has(data[t], v) && !around.Contains(t))
                around.Add(t);
        if (around.Count < 2)
            return false;

        // Por vizinho: os triangulos que contem a aresta (v,w).
        Dictionary<int, List<int>> byNeighbor = new();
        foreach (int t in around)
        {
            int[] ids = { data[t].pa, data[t].pb, data[t].pc };
            foreach (int w in ids)
            {
                if (w == v)
                    continue;
                if (!byNeighbor.TryGetValue(w, out List<int> tris))
                {
                    tris = new List<int>();
                    byNeighbor.Add(w, tris);
                }
                tris.Add(t);
            }
        }

        List<int> creases = new();
        HashSet<int> origins = new();
        foreach (int t in around)
            origins.Add(data[t].origin);
        if (origins.Count != 2)
            return false;

        foreach (KeyValuePair<int, List<int>> pair in byNeighbor)
        {
            if (pair.Value.Count != 2)
                return false;
            if (data[pair.Value[0]].origin != data[pair.Value[1]].origin)
                creases.Add(pair.Key);
        }

        if (creases.Count != 2)
            return false;

        p = creases[0];
        q = creases[1];
        Vector3 toP = state.positions[p] - state.positions[v];
        Vector3 toQ = state.positions[q] - state.positions[v];
        if (toP.sqrMagnitude < 1e-12f || toQ.sqrMagnitude < 1e-12f)
            return false;
        Vector3 dirP = toP.normalized;
        Vector3 dirQ = toQ.normalized;
        return Vector3.Dot(dirP, dirQ) < -.9999f;
    }

    private static void AddAdjacency(Dictionary<int, List<int>> adjacency, int vertex, int tri)
    {
        if (!adjacency.TryGetValue(vertex, out List<int> list))
        {
            list = new List<int>();
            adjacency.Add(vertex, list);
        }
        list.Add(tri);
    }

    private static bool Has(Tri t, int v) => t.pa == v || t.pb == v || t.pc == v;

    // Vertice interno: todos os triangulos vivos ao redor vem do mesmo
    // triangulo original e toda aresta que sai dele e usada por 2 triangulos.
    private static bool TryGetInterior(Tri[] data, bool[] dead,
        Dictionary<int, List<int>> adjacency, int v, out List<int> around)
    {
        around = new List<int>();
        if (!adjacency.TryGetValue(v, out List<int> list))
            return false;

        int origin = -1;
        foreach (int t in list)
        {
            if (dead[t] || !Has(data[t], v))
                continue;
            if (around.Contains(t))
                continue;
            if (origin == -1)
                origin = data[t].origin;
            else if (data[t].origin != origin)
                return false;
            around.Add(t);
        }

        if (around.Count < 3)
            return false;

        Dictionary<int, int> edgeUse = new();
        foreach (int t in around)
        {
            int[] ids = { data[t].pa, data[t].pb, data[t].pc };
            foreach (int w in ids)
            {
                if (w == v)
                    continue;
                edgeUse.TryGetValue(w, out int uses);
                edgeUse[w] = uses + 1;
            }
        }

        foreach (int uses in edgeUse.Values)
            if (uses != 2)
                return false;
        return true;
    }

    private static bool TryCollapse(State state, Tri[] data, bool[] dead,
        Dictionary<int, List<int>> adjacency, int v, List<int> around,
        List<int> allowedTargets)
    {
        Vector3 pv = state.positions[v];
        List<int> candidates = new();
        foreach (int t in around)
        {
            int[] ids = { data[t].pa, data[t].pb, data[t].pc };
            foreach (int w in ids)
                if (w != v && !candidates.Contains(w) &&
                    (allowedTargets == null || allowedTargets.Contains(w)))
                    candidates.Add(w);
        }

        candidates.Sort((x, y) =>
            (state.positions[x] - pv).sqrMagnitude.CompareTo((state.positions[y] - pv).sqrMagnitude));

        foreach (int u in candidates)
        {
            if (!CanCollapse(state, data, around, v, u))
                continue;

            CutVertex uVertex = default;
            foreach (int t in around)
            {
                if (data[t].pa == u) { uVertex = data[t].a; break; }
                if (data[t].pb == u) { uVertex = data[t].b; break; }
                if (data[t].pc == u) { uVertex = data[t].c; break; }
            }

            foreach (int t in around)
            {
                if (Has(data[t], u))
                {
                    dead[t] = true;
                    continue;
                }

                Tri tri = data[t];
                if (tri.pa == v) { tri.pa = u; tri.a = uVertex; }
                else if (tri.pb == v) { tri.pb = u; tri.b = uVertex; }
                else { tri.pc = u; tri.c = uVertex; }
                data[t] = tri;
                AddAdjacency(adjacency, u, t);
            }

            adjacency[v].Clear();
            return true;
        }

        return false;
    }

    // Fundir v em u nao pode inverter nem degenerar nenhum triangulo, nem criar
    // arestas duplicadas (condicao de link).
    private static bool CanCollapse(State state, Tri[] data, List<int> around,
        int v, int u)
    {
        HashSet<int> vNeighbors = new();
        HashSet<int> uNeighbors = new();
        int sharing = 0;
        foreach (int t in around)
        {
            int[] ids = { data[t].pa, data[t].pb, data[t].pc };
            bool hasU = Has(data[t], u);
            if (hasU)
                sharing++;
            foreach (int w in ids)
            {
                if (w == v || w == u)
                    continue;
                if (hasU)
                    uNeighbors.Add(w);
                vNeighbors.Add(w);
            }
        }

        if (sharing != 2)
            return false;

        // Vizinhos de v que tambem sao vizinhos de u: so os dois vertices
        // opostos a aresta (v,u) podem ser comuns.
        int common = 0;
        foreach (int w in vNeighbors)
            if (uNeighbors.Contains(w))
                common++;
        if (common != 2)
            return false;

        Vector3 pu = state.positions[u];
        foreach (int t in around)
        {
            if (Has(data[t], u))
                continue;
            Vector3 a = state.positions[data[t].pa];
            Vector3 b = state.positions[data[t].pb];
            Vector3 c = state.positions[data[t].pc];
            Vector3 before = Vector3.Cross(b - a, c - a);
            if (data[t].pa == v) a = pu;
            else if (data[t].pb == v) b = pu;
            else c = pu;
            Vector3 after = Vector3.Cross(b - a, c - a);
            if (after.sqrMagnitude < 1e-14f ||
                Vector3.Dot(before.normalized, after.normalized) < .5f)
                return false;
        }

        return true;
    }
    // ---------------------------------------------------------------------

    private static long EdgeKey(int a, int b) =>
        a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

    private readonly struct VertexKey : IEquatable<VertexKey>
    {
        private readonly CutVertex vertex;
        public VertexKey(CutVertex vertex) => this.vertex = vertex;

        public bool Equals(VertexKey other) =>
            vertex.position == other.vertex.position &&
            vertex.normal == other.vertex.normal &&
            vertex.uv == other.vertex.uv && vertex.color == other.vertex.color;

        public override bool Equals(object obj) => obj is VertexKey key && Equals(key);

        public override int GetHashCode() => HashCode.Combine(
            vertex.position, vertex.normal, vertex.uv, vertex.color);
    }

    private static CutMesh Assemble(CutMesh source, List<Tri> tris)
    {
        CutMesh result = new()
        {
            hasNormals = source.hasNormals,
            hasUVs = source.hasUVs,
            hasColors = source.hasColors
        };
        for (int s = 0; s < source.submeshes.Count; s++)
            result.submeshes.Add(new List<int>());

        Dictionary<VertexKey, int> lookup = new();
        int Index(CutVertex vertex)
        {
            VertexKey key = new(vertex);
            if (lookup.TryGetValue(key, out int index))
                return index;
            index = result.vertices.Count;
            result.vertices.Add(vertex);
            lookup.Add(key, index);
            return index;
        }

        foreach (Tri tri in tris)
        {
            List<int> indices = result.submeshes[tri.sub];
            indices.Add(Index(tri.a));
            indices.Add(Index(tri.b));
            indices.Add(Index(tri.c));
        }

        return result;
    }
}

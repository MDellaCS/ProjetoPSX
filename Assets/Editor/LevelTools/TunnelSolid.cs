using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Solido definido por uma malha fechada (a "tuneladora"). Responde a distancia
/// com sinal ate a superficie: negativa dentro, positiva fora. O sinal usa as
/// normais pseudo-angulares (Baerentzen e Aanaes), por isso funciona perto de
/// quinas e arestas, e nao depende de a malha ser convexa.
/// </summary>
public sealed class TunnelSolid
{
    private struct Node
    {
        public Vector3 min;
        public Vector3 max;
        public int left;
        public int right;
        public int start;
        public int count;
    }

    private const float WeldCell = 1e-4f;
    private const int LeafSize = 4;

    private readonly Vector3[] positions;
    private readonly int[] triangles;
    private readonly int[] weldedCorner;
    private readonly Vector3[] faceNormals;
    private readonly Vector3[] vertexNormals;
    private readonly Dictionary<long, Vector3> edgeNormals = new();
    private readonly List<Node> nodes = new();
    private readonly int[] order;
    private readonly Vector3[] triMin;
    private readonly Vector3[] triMax;

    /// <summary>Arestas usadas por um numero de faces diferente de 2 (buracos ou arestas em T).</summary>
    public int OpenEdges { get; private set; }
    /// <summary>Arestas percorridas duas vezes no mesmo sentido (faces com winding inconsistente).</summary>
    public int InconsistentEdges { get; private set; }
    /// <summary>A malha estava com as faces voltadas para dentro e foi invertida.</summary>
    public bool WasFlipped { get; private set; }
    public Bounds Bounds { get; private set; }
    public int TriangleCount => triangles.Length / 3;
    public bool IsWatertight => OpenEdges == 0 && InconsistentEdges == 0;

    public TunnelSolid(Vector3[] worldPositions, int[] worldTriangles)
    {
        positions = worldPositions;
        triangles = (int[])worldTriangles.Clone();
        int triangleCount = triangles.Length / 3;

        weldedCorner = new int[triangles.Length];
        Dictionary<Vector3Int, int> weld = new();
        int weldedCount = 0;
        for (int i = 0; i < triangles.Length; i++)
        {
            Vector3 p = positions[triangles[i]];
            Vector3Int key = new(Mathf.RoundToInt(p.x / WeldCell),
                Mathf.RoundToInt(p.y / WeldCell), Mathf.RoundToInt(p.z / WeldCell));
            if (!weld.TryGetValue(key, out int id))
            {
                id = weldedCount++;
                weld.Add(key, id);
            }
            weldedCorner[i] = id;
        }

        // Volume com sinal: se negativo, as faces apontam para dentro.
        double volume = 0;
        for (int t = 0; t < triangleCount; t++)
        {
            Vector3 a = positions[triangles[t * 3]];
            Vector3 b = positions[triangles[t * 3 + 1]];
            Vector3 c = positions[triangles[t * 3 + 2]];
            volume += Vector3.Dot(a, Vector3.Cross(b, c));
        }

        if (volume < 0)
        {
            WasFlipped = true;
            for (int t = 0; t < triangleCount; t++)
            {
                (triangles[t * 3 + 1], triangles[t * 3 + 2]) =
                    (triangles[t * 3 + 2], triangles[t * 3 + 1]);
                (weldedCorner[t * 3 + 1], weldedCorner[t * 3 + 2]) =
                    (weldedCorner[t * 3 + 2], weldedCorner[t * 3 + 1]);
            }
        }

        faceNormals = new Vector3[triangleCount];
        vertexNormals = new Vector3[weldedCount];
        triMin = new Vector3[triangleCount];
        triMax = new Vector3[triangleCount];
        Dictionary<long, int> edgeUse = new();
        HashSet<long> directedEdges = new();

        Vector3 boundsMin = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 boundsMax = -boundsMin;
        for (int t = 0; t < triangleCount; t++)
        {
            int i0 = t * 3, i1 = i0 + 1, i2 = i0 + 2;
            Vector3 a = positions[triangles[i0]];
            Vector3 b = positions[triangles[i1]];
            Vector3 c = positions[triangles[i2]];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            Vector3 normal = cross.sqrMagnitude > 1e-18f ? cross.normalized : Vector3.zero;
            faceNormals[t] = normal;
            triMin[t] = Vector3.Min(a, Vector3.Min(b, c));
            triMax[t] = Vector3.Max(a, Vector3.Max(b, c));
            boundsMin = Vector3.Min(boundsMin, triMin[t]);
            boundsMax = Vector3.Max(boundsMax, triMax[t]);

            vertexNormals[weldedCorner[i0]] += normal * CornerAngle(a, b, c);
            vertexNormals[weldedCorner[i1]] += normal * CornerAngle(b, c, a);
            vertexNormals[weldedCorner[i2]] += normal * CornerAngle(c, a, b);

            for (int e = 0; e < 3; e++)
            {
                int from = weldedCorner[i0 + e];
                int to = weldedCorner[i0 + (e + 1) % 3];
                long key = EdgeKey(from, to);
                edgeUse.TryGetValue(key, out int count);
                edgeUse[key] = count + 1;
                edgeNormals.TryGetValue(key, out Vector3 sum);
                edgeNormals[key] = sum + normal;
                long directed = ((long)from << 32) | (uint)to;
                if (!directedEdges.Add(directed))
                    InconsistentEdges++;
            }
        }

        foreach (int uses in edgeUse.Values)
            if (uses != 2)
                OpenEdges++;

        Bounds = new Bounds((boundsMin + boundsMax) * .5f, boundsMax - boundsMin);

        order = new int[triangleCount];
        for (int i = 0; i < triangleCount; i++)
            order[i] = i;
        if (triangleCount > 0)
            BuildNode(0, triangleCount);
    }

    /// <summary>Distancia com sinal ao solido: negativa dentro, positiva fora.</summary>
    public float Signed(Vector3 point)
    {
        if (nodes.Count == 0)
            return float.MaxValue;

        float bestSqr = float.MaxValue;
        int bestTri = -1;
        int bestRegion = 0;
        Vector3 bestPoint = Vector3.zero;
        Search(0, point, ref bestSqr, ref bestTri, ref bestRegion, ref bestPoint);
        if (bestTri < 0)
            return float.MaxValue;

        Vector3 normal;
        int baseIndex = bestTri * 3;
        switch (bestRegion)
        {
            case 1: normal = vertexNormals[weldedCorner[baseIndex]]; break;
            case 2: normal = vertexNormals[weldedCorner[baseIndex + 1]]; break;
            case 3: normal = vertexNormals[weldedCorner[baseIndex + 2]]; break;
            case 4: normal = edgeNormals[EdgeKey(weldedCorner[baseIndex], weldedCorner[baseIndex + 1])]; break;
            case 5: normal = edgeNormals[EdgeKey(weldedCorner[baseIndex + 1], weldedCorner[baseIndex + 2])]; break;
            case 6: normal = edgeNormals[EdgeKey(weldedCorner[baseIndex + 2], weldedCorner[baseIndex])]; break;
            default: normal = faceNormals[bestTri]; break;
        }

        float distance = Mathf.Sqrt(bestSqr);
        return Vector3.Dot(point - bestPoint, normal) < 0f ? -distance : distance;
    }

    private static long EdgeKey(int a, int b) =>
        a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

    private static float CornerAngle(Vector3 corner, Vector3 next, Vector3 previous)
    {
        Vector3 u = next - corner;
        Vector3 v = previous - corner;
        float lengths = u.magnitude * v.magnitude;
        if (lengths < 1e-12f)
            return 0f;
        return Mathf.Acos(Mathf.Clamp(Vector3.Dot(u, v) / lengths, -1f, 1f));
    }

    private int BuildNode(int start, int count)
    {
        Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = -min;
        Vector3 centroidMin = min;
        Vector3 centroidMax = max;
        for (int i = start; i < start + count; i++)
        {
            int t = order[i];
            min = Vector3.Min(min, triMin[t]);
            max = Vector3.Max(max, triMax[t]);
            Vector3 centroid = (triMin[t] + triMax[t]) * .5f;
            centroidMin = Vector3.Min(centroidMin, centroid);
            centroidMax = Vector3.Max(centroidMax, centroid);
        }

        int index = nodes.Count;
        nodes.Add(new Node { min = min, max = max });
        if (count <= LeafSize)
        {
            nodes[index] = new Node { min = min, max = max, start = start, count = count, left = -1, right = -1 };
            return index;
        }

        Vector3 extent = centroidMax - centroidMin;
        int axis = extent.x >= extent.y && extent.x >= extent.z ? 0 : extent.y >= extent.z ? 1 : 2;
        Array.Sort(order, start, count, Comparer<int>.Create((x, y) =>
            ((triMin[x][axis] + triMax[x][axis]).CompareTo(triMin[y][axis] + triMax[y][axis]))));
        int half = count / 2;
        int left = BuildNode(start, half);
        int right = BuildNode(start + half, count - half);
        nodes[index] = new Node { min = min, max = max, left = left, right = right, start = start, count = 0 };
        return index;
    }

    private void Search(int nodeIndex, Vector3 point, ref float bestSqr,
        ref int bestTri, ref int bestRegion, ref Vector3 bestPoint)
    {
        Node node = nodes[nodeIndex];
        if (node.count > 0)
        {
            for (int i = node.start; i < node.start + node.count; i++)
            {
                int t = order[i];
                Vector3 closest = ClosestPointOnTriangle(point,
                    positions[triangles[t * 3]], positions[triangles[t * 3 + 1]],
                    positions[triangles[t * 3 + 2]], out int region);
                float sqr = (point - closest).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestTri = t;
                    bestRegion = region;
                    bestPoint = closest;
                }
            }
            return;
        }

        float leftSqr = BoxSqrDistance(nodes[node.left], point);
        float rightSqr = BoxSqrDistance(nodes[node.right], point);
        int first = leftSqr <= rightSqr ? node.left : node.right;
        int second = first == node.left ? node.right : node.left;
        float firstSqr = first == node.left ? leftSqr : rightSqr;
        float secondSqr = first == node.left ? rightSqr : leftSqr;
        if (firstSqr < bestSqr)
            Search(first, point, ref bestSqr, ref bestTri, ref bestRegion, ref bestPoint);
        if (secondSqr < bestSqr)
            Search(second, point, ref bestSqr, ref bestTri, ref bestRegion, ref bestPoint);
    }

    private static float BoxSqrDistance(Node node, Vector3 point)
    {
        float dx = Mathf.Max(Mathf.Max(node.min.x - point.x, 0f), point.x - node.max.x);
        float dy = Mathf.Max(Mathf.Max(node.min.y - point.y, 0f), point.y - node.max.y);
        float dz = Mathf.Max(Mathf.Max(node.min.z - point.z, 0f), point.z - node.max.z);
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>Ponto mais proximo em um triangulo. region: 0 face, 1-3 vertices A/B/C, 4-6 arestas AB/BC/CA.</summary>
    private static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b,
        Vector3 c, out int region)
    {
        Vector3 ab = b - a, ac = c - a, ap = p - a;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) { region = 1; return a; }

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) { region = 2; return b; }

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            region = 4;
            return a + ab * (d1 / (d1 - d3));
        }

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) { region = 3; return c; }

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            region = 6;
            return a + ac * (d2 / (d2 - d6));
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
        {
            region = 5;
            return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
        }

        float denominator = 1f / (va + vb + vc);
        region = 0;
        return a + ab * (vb * denominator) + ac * (vc * denominator);
    }
}

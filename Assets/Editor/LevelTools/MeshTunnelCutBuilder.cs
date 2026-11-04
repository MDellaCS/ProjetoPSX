using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Aplica e desfaz o corte de um <see cref="MeshTunnelCut"/>. O objeto original
/// nunca e alterado: o resultado e um objeto irmao com a malha recortada salva
/// como asset em Assets/Generated/TunnelCuts.
/// </summary>
public static class MeshTunnelCutBuilder
{
    private const string OutputFolder = "Assets/Generated/TunnelCuts";

    public static void Apply(MeshTunnelCut tool)
    {
        if (tool == null)
            return;

        MeshFilter filter = tool.GetComponent<MeshFilter>();
        MeshRenderer renderer = tool.GetComponent<MeshRenderer>();
        if (filter == null || filter.sharedMesh == null || renderer == null)
        {
            EditorUtility.DisplayDialog("Mesh Tunnel Cut",
                "O objeto precisa ter MeshFilter com uma malha e MeshRenderer.", "OK");
            return;
        }

        Matrix4x4 localToWorld = tool.transform.localToWorldMatrix;
        if (localToWorld.determinant < 0f)
        {
            EditorUtility.DisplayDialog("Mesh Tunnel Cut",
                "Escala negativa nao e suportada. Aplique a escala (ou use escala positiva) e tente de novo.", "OK");
            return;
        }

        StringBuilder report = new();
        Stopwatch stopwatch = Stopwatch.StartNew();
        EditorUtility.DisplayProgressBar("Mesh Tunnel Cut", "Preparando tuneladoras...", .1f);
        try
        {
            // Restaura o estado original antes de refazer, para o corte
            // partir sempre da malha original.
            Restore(tool, deleteAsset: false);

            CutMesh source = ReadMesh(filter.sharedMesh, localToWorld);
            List<TunnelSolid> solids = BuildSolids(tool, filter, report);
            if (solids.Count == 0)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Mesh Tunnel Cut",
                    "Nenhuma tuneladora valida: adicione objetos com MeshFilter cujo volume toca esta malha.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Mesh Tunnel Cut", "Cortando...", .5f);
            CutMesh result = TunnelCutCore.Cut(source, solids, tool.CutterMargin,
                tool.MaxEdgeLength, out TunnelCutStats stats, tool.SimplifyResult);
            foreach (string warning in stats.warnings)
                report.AppendLine("AVISO: " + warning);

            if (result.vertices.Count == 0)
            {
                report.AppendLine("O corte removeu a malha inteira.");
                tool.LastReport = report.ToString();
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Mesh Tunnel Cut",
                    "O corte removeu a malha inteira. Nada foi criado.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Mesh Tunnel Cut", "Salvando malha...", .8f);
            Mesh mesh = BuildMesh(result, localToWorld, filter.sharedMesh);
            string path = SaveMeshAsset(tool, mesh);
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            GameObject resultObject = CreateResultObject(tool, filter, renderer, mesh);

            tool.ResultObject = resultObject;
            tool.ResultMeshPath = path;
            DisableOriginal(tool, renderer);

            report.Insert(0,
                $"Triangulos: {stats.inputTriangles} -> {stats.outputTriangles} " +
                $"({stats.refinementRounds} rodadas, {stopwatch.ElapsedMilliseconds} ms)\n" +
                $"Malha salva em {path}\n");
            tool.LastReport = report.ToString();
            EditorUtility.SetDirty(tool);
            EditorSceneManager.MarkSceneDirty(tool.gameObject.scene);
            Debug.Log($"Mesh Tunnel Cut '{tool.name}': {tool.LastReport}", tool);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>Desfaz o corte: remove o resultado e religa o objeto original.</summary>
    public static void Restore(MeshTunnelCut tool, bool deleteAsset = true)
    {
        if (tool == null)
            return;

        if (tool.ResultObject != null)
            Undo.DestroyObjectImmediate(tool.ResultObject);
        tool.ResultObject = null;

        MeshRenderer renderer = tool.GetComponent<MeshRenderer>();
        if (renderer != null && tool.DisableOriginalAfterCut &&
            !renderer.enabled && tool.OriginalRendererWasEnabled)
        {
            Undo.RecordObject(renderer, "Restaurar Mesh Tunnel Cut");
            renderer.enabled = true;
        }

        foreach (Collider collider in tool.DisabledColliders)
        {
            if (collider == null)
                continue;
            Undo.RecordObject(collider, "Restaurar Mesh Tunnel Cut");
            collider.enabled = true;
        }
        tool.DisabledColliders.Clear();

        if (deleteAsset && !string.IsNullOrEmpty(tool.ResultMeshPath))
        {
            AssetDatabase.DeleteAsset(tool.ResultMeshPath);
            tool.ResultMeshPath = null;
        }

        tool.LastReport = deleteAsset ? string.Empty : tool.LastReport;
        EditorUtility.SetDirty(tool);
    }

    // --- Leitura ---------------------------------------------------------

    private static CutMesh ReadMesh(Mesh mesh, Matrix4x4 localToWorld)
    {
        CutMesh result = new();
        Vector3[] positions = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        Color[] colors = mesh.colors;
        result.hasNormals = normals.Length == positions.Length;
        result.hasUVs = uvs.Length == positions.Length;
        result.hasColors = colors.Length == positions.Length;

        Matrix4x4 normalMatrix = localToWorld.inverse.transpose;
        for (int i = 0; i < positions.Length; i++)
        {
            result.vertices.Add(new CutVertex
            {
                position = localToWorld.MultiplyPoint3x4(positions[i]),
                normal = result.hasNormals
                    ? normalMatrix.MultiplyVector(normals[i]).normalized : Vector3.up,
                uv = result.hasUVs ? uvs[i] : Vector2.zero,
                color = result.hasColors ? colors[i] : Color.white
            });
        }

        for (int s = 0; s < mesh.subMeshCount; s++)
            result.submeshes.Add(new List<int>(mesh.GetTriangles(s)));
        return result;
    }

    private static List<TunnelSolid> BuildSolids(MeshTunnelCut tool, MeshFilter target,
        StringBuilder report)
    {
        List<TunnelSolid> solids = new();
        Bounds targetBounds = target.GetComponent<MeshRenderer>().bounds;
        targetBounds.Expand(Mathf.Max(0f, tool.CutterMargin) * 2f + .01f);

        foreach (MeshFilter cutter in tool.Cutters)
        {
            if (cutter == null || cutter == target || cutter.sharedMesh == null)
                continue;
            if (tool.ResultObject != null &&
                cutter.transform.IsChildOf(tool.ResultObject.transform))
                continue;

            Matrix4x4 matrix = cutter.transform.localToWorldMatrix;
            Mesh mesh = cutter.sharedMesh;
            Vector3[] local = mesh.vertices;
            Vector3[] world = new Vector3[local.Length];
            for (int i = 0; i < local.Length; i++)
                world[i] = matrix.MultiplyPoint3x4(local[i]);

            List<int> triangles = new();
            for (int s = 0; s < mesh.subMeshCount; s++)
                triangles.AddRange(mesh.GetTriangles(s));
            bool mirrored = matrix.determinant < 0f;
            if (mirrored)
            {
                for (int i = 0; i + 2 < triangles.Count; i += 3)
                    (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
            }

            TunnelSolid solid = new(world, triangles.ToArray());
            if (!solid.Bounds.Intersects(targetBounds))
            {
                report.AppendLine($"'{cutter.name}' nao toca esta malha; ignorada.");
                continue;
            }

            if (!solid.IsWatertight)
                report.AppendLine($"AVISO: '{cutter.name}' nao e uma malha fechada " +
                    $"({solid.OpenEdges} arestas abertas, {solid.InconsistentEdges} inconsistentes). " +
                    "O corte pode ficar errado; feche as pontas do cano.");
            if (solid.WasFlipped)
                report.AppendLine($"'{cutter.name}' tinha as faces invertidas; foi corrigido internamente.");
            solids.Add(solid);
        }

        return solids;
    }

    // --- Saida -----------------------------------------------------------

    private static Mesh BuildMesh(CutMesh cut, Matrix4x4 localToWorld, Mesh original)
    {
        Matrix4x4 worldToLocal = localToWorld.inverse;
        Matrix4x4 normalToLocal = localToWorld.transpose;
        int count = cut.vertices.Count;
        Vector3[] positions = new Vector3[count];
        Vector3[] normals = new Vector3[count];
        Vector2[] uvs = new Vector2[count];
        Color[] colors = new Color[count];
        for (int i = 0; i < count; i++)
        {
            CutVertex v = cut.vertices[i];
            positions[i] = worldToLocal.MultiplyPoint3x4(v.position);
            normals[i] = normalToLocal.MultiplyVector(v.normal).normalized;
            uvs[i] = v.uv;
            colors[i] = v.color;
        }

        Mesh mesh = new()
        {
            name = original.name + "_Cut",
            indexFormat = count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.SetVertices(positions);
        if (cut.hasNormals)
            mesh.SetNormals(normals);
        if (cut.hasUVs)
            mesh.SetUVs(0, uvs);
        if (cut.hasColors)
            mesh.SetColors(colors);
        mesh.subMeshCount = cut.submeshes.Count;
        for (int s = 0; s < cut.submeshes.Count; s++)
            mesh.SetTriangles(cut.submeshes[s], s, false);
        mesh.RecalculateBounds();
        if (!cut.hasNormals)
            mesh.RecalculateNormals();
        if (cut.hasUVs)
            mesh.RecalculateTangents();
        return mesh;
    }

    private static string SaveMeshAsset(MeshTunnelCut tool, Mesh mesh)
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder(OutputFolder);
        string sceneName = tool.gameObject.scene.name;
        if (string.IsNullOrEmpty(sceneName))
            sceneName = "Scene";
        string fileName = Sanitize($"{sceneName}_{tool.name}_{GlobalObjectId.GetGlobalObjectIdSlow(tool.gameObject).targetObjectId:X}");
        string path = $"{OutputFolder}/{fileName}.asset";
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        return path;
    }

    private static GameObject CreateResultObject(MeshTunnelCut tool, MeshFilter filter,
        MeshRenderer renderer, Mesh mesh)
    {
        GameObject original = tool.gameObject;
        GameObject result = new(original.name + " (Cut)");
        Undo.RegisterCreatedObjectUndo(result, "Mesh Tunnel Cut");
        result.transform.SetParent(original.transform.parent, false);
        result.transform.SetSiblingIndex(original.transform.GetSiblingIndex() + 1);
        result.transform.localPosition = original.transform.localPosition;
        result.transform.localRotation = original.transform.localRotation;
        result.transform.localScale = original.transform.localScale;
        result.layer = original.layer;
        result.tag = original.tag;
        GameObjectUtility.SetStaticEditorFlags(result,
            GameObjectUtility.GetStaticEditorFlags(original));

        MeshFilter newFilter = result.AddComponent<MeshFilter>();
        newFilter.sharedMesh = mesh;
        MeshRenderer newRenderer = result.AddComponent<MeshRenderer>();
        EditorUtility.CopySerialized(renderer, newRenderer);
        newRenderer.enabled = true;

        if (tool.GenerateCollider)
        {
            MeshCollider collider = result.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            Collider originalCollider = original.GetComponent<Collider>();
            if (originalCollider != null)
            {
                collider.sharedMaterial = originalCollider.sharedMaterial;
                collider.isTrigger = false;
            }
        }

        return result;
    }

    private static void DisableOriginal(MeshTunnelCut tool, MeshRenderer renderer)
    {
        tool.OriginalRendererWasEnabled = renderer.enabled;
        tool.DisabledColliders.Clear();
        if (!tool.DisableOriginalAfterCut)
            return;

        Undo.RecordObject(renderer, "Mesh Tunnel Cut");
        renderer.enabled = false;
        foreach (Collider collider in tool.GetComponents<Collider>())
        {
            if (!collider.enabled)
                continue;
            Undo.RecordObject(collider, "Mesh Tunnel Cut");
            collider.enabled = false;
            tool.DisabledColliders.Add(collider);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static string Sanitize(string value)
    {
        StringBuilder builder = new();
        foreach (char c in value)
            builder.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        return builder.ToString();
    }
}

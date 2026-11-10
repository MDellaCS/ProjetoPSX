using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(WaterVolume))]
public sealed class WaterVolumeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        WaterVolume water = (WaterVolume)target;
        using (new EditorGUI.DisabledScope(water.VolumeCollider == null))
        {
            if (GUILayout.Button("Criar / Atualizar visual da água"))
                CreateSurface(water);
        }
    }

    private static void CreateSurface(WaterVolume water)
    {
        Collider volume = water.VolumeCollider;
        if (volume == null) return;
        Bounds bounds = volume.bounds;
        if (bounds.size.x <= 0f || bounds.size.z <= 0f)
        {
            Debug.LogWarning("O volume precisa ter largura e comprimento maiores que zero para gerar o plano de água.", water);
            return;
        }

        Transform root = water.transform.Find("Water Surface (Generated)");
        GameObject surfaceObject;
        if (root == null)
        {
            surfaceObject = new GameObject("Water Surface (Generated)");
            Undo.RegisterCreatedObjectUndo(surfaceObject, "Criar visual da água");
            Undo.SetTransformParent(surfaceObject.transform, water.transform, "Anexar visual da água");
        }
        else surfaceObject = root.gameObject;

        Transform surfaceTransform = surfaceObject.transform;
        Undo.RecordObject(surfaceTransform, "Posicionar visual da água");
        surfaceTransform.localPosition = Vector3.zero;
        surfaceTransform.localRotation = Quaternion.identity;
        surfaceTransform.localScale = Vector3.one;

        Vector3[] world =
        {
            new(bounds.min.x, water.SurfaceHeightAt(bounds.center.x, bounds.center.z) + .015f, bounds.min.z),
            new(bounds.max.x, water.SurfaceHeightAt(bounds.center.x, bounds.center.z) + .015f, bounds.min.z),
            new(bounds.max.x, water.SurfaceHeightAt(bounds.center.x, bounds.center.z) + .015f, bounds.max.z),
            new(bounds.min.x, water.SurfaceHeightAt(bounds.center.x, bounds.center.z) + .015f, bounds.max.z)
        };
        MeshFilter filter = surfaceObject.GetComponent<MeshFilter>();
        Mesh oldMesh = filter != null ? filter.sharedMesh : null;
        Mesh mesh = new Mesh { name = "Generated Water Surface" };
        Vector3[] vertices = new Vector3[4];
        for (int i = 0; i < 4; i++) vertices[i] = surfaceTransform.InverseTransformPoint(world[i]);
        mesh.vertices = vertices;
        mesh.uv = new[]
        {
            Vector2.zero,
            new Vector2(bounds.size.x * water.TextureRepeatsPerMeter, 0),
            new Vector2(bounds.size.x * water.TextureRepeatsPerMeter, bounds.size.z * water.TextureRepeatsPerMeter),
            new Vector2(0, bounds.size.z * water.TextureRepeatsPerMeter)
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (filter == null) filter = Undo.AddComponent<MeshFilter>(surfaceObject);
        MeshRenderer renderer = surfaceObject.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<MeshRenderer>(surfaceObject);
        Undo.RecordObject(filter, "Atualizar malha da água");
        filter.sharedMesh = mesh;
        Undo.RecordObject(renderer, "Atualizar material da água");
        Material material = water.SurfaceMaterial;
        if (material == null)
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/M_Water.mat");
        if (material != null) renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        water.SetSurfaceVisual(material, renderer);
        EditorUtility.SetDirty(water);
        EditorSceneManager.MarkSceneDirty(water.gameObject.scene);
        Selection.activeGameObject = surfaceObject;
        if (oldMesh != null) Undo.DestroyObjectImmediate(oldMesh);
    }
}

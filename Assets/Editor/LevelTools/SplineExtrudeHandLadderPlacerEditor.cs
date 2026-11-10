using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

[CustomEditor(typeof(SplineExtrudeHandLadderPlacer))]
internal sealed class SplineExtrudeHandLadderPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(serializedObject, "m_Script", "generatedRoot");
        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        SplineExtrudeHandLadderPlacer placer = (SplineExtrudeHandLadderPlacer)target;
        if (placer.Source == null && placer.TryGetComponent(out SplineExtrude extrude))
        {
            Undo.RecordObject(placer, "Atribuir Spline Extrude");
            placer.SetSource(extrude);
            EditorUtility.SetDirty(placer);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "A ferramenta assume uma extrusao circular. A previa verde mostra a escada na parede escolhida. " +
            "A geometria so e criada ao pressionar Gerar/Atualizar; mover a spline nao reconstrui objetos automaticamente.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(Application.isPlaying || placer.Source == null))
        {
            if (GUILayout.Button("Gerar / Atualizar escada"))
                SplineExtrudeHandLadderPlacerGenerator.Generate(placer);

            using (new EditorGUI.DisabledScope(placer.GeneratedRoot == null))
                if (GUILayout.Button("Remover escada gerada"))
                    SplineExtrudeHandLadderPlacerGenerator.Remove(placer);
        }

        if (changed)
            SceneView.RepaintAll();
    }
}

internal static class SplineExtrudeHandLadderPlacerGenerator
{
    private const string GeneratedName = "[Generated] Tube Hand Ladder";

    public static bool Generate(SplineExtrudeHandLadderPlacer placer)
    {
        if (placer == null || Application.isPlaying)
            return false;

        if (!placer.TryBuildSpine(out HandLadderSpine worldSpine,
                out float height, out Vector3 basePoint, out Vector3 tangent,
                out Vector3 approach, out string error))
        {
            Debug.LogWarning($"Nao foi possivel gerar a escada: {error}", placer);
            return false;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Gerar escada no tubo");
        Undo.RecordObject(placer, "Guardar escada gerada");
        RemoveGeneratedObject(placer);

        GameObject rootObject = new(GeneratedName);
        Undo.RegisterCreatedObjectUndo(rootObject, "Gerar escada no tubo");
        Transform root = rootObject.transform;
        root.SetParent(placer.transform, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        Vector3 sourceScale = placer.transform.lossyScale;
        root.localScale = new Vector3(
            1f / Mathf.Max(.0001f, Mathf.Abs(sourceScale.x)),
            1f / Mathf.Max(.0001f, Mathf.Abs(sourceScale.y)),
            1f / Mathf.Max(.0001f, Mathf.Abs(sourceScale.z)));

        GameObject ladderObject = new("Hand Ladder (Spline Extrude)");
        Undo.RegisterCreatedObjectUndo(ladderObject, "Gerar escada no tubo");
        Transform ladderTransform = ladderObject.transform;
        ladderTransform.SetParent(root, false);
        ladderTransform.SetPositionAndRotation(basePoint,
            Quaternion.LookRotation(approach, tangent));

        HandLadder ladder = Undo.AddComponent<HandLadder>(ladderObject);
        SerializedObject serializedLadder = new(ladder);
        SetFloat(serializedLadder, "height", height);
        SetFloat(serializedLadder, "width", placer.LadderWidth);
        SetVector3(serializedLadder, "climbAxis", Vector3.up);
        SetVector3(serializedLadder, "approachAxis", Vector3.forward);
        SetBool(serializedLadder, "isClimbable", true);
        SetBool(serializedLadder, "hasCollision", false);
        SetInt(serializedLadder, "attachment", (int)HandLadderAttachment.Surface);
        SetBool(serializedLadder, "conformToSurface", true);
        SetBool(serializedLadder, "autoSnapToSurface", false);
        SetFloat(serializedLadder, "curveSampleSpacing", placer.SampleSpacing);
        SetInt(serializedLadder, "topExitSide", (int)HandLadderExitSide.Front);
        SetObject(serializedLadder, "profile", placer.LadderProfile);
        serializedLadder.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < worldSpine.Count; i++)
        {
            worldSpine.center[i] = ladderTransform.InverseTransformPoint(worldSpine.center[i]);
            worldSpine.left[i] = ladderTransform.InverseTransformPoint(worldSpine.left[i]);
            worldSpine.right[i] = ladderTransform.InverseTransformPoint(worldSpine.right[i]);
            worldSpine.normal[i] = ladderTransform.InverseTransformDirection(worldSpine.normal[i]).normalized;
        }
        ladder.SetAttachment(HandLadderAttachment.Surface);
        ladder.SetSpine(worldSpine);
        EditorUtility.SetDirty(ladder);

        placer.SetGeneratedRoot(root);
        EditorUtility.SetDirty(placer);
        HandLadderGenerator.GenerateNow(ladder);
        if (placer.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        Selection.activeGameObject = ladderObject;
        Undo.CollapseUndoOperations(undoGroup);
        return true;
    }

    public static void Remove(SplineExtrudeHandLadderPlacer placer)
    {
        if (placer == null || Application.isPlaying || placer.GeneratedRoot == null)
            return;

        Undo.RecordObject(placer, "Remover escada gerada");
        RemoveGeneratedObject(placer);
        if (placer.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        EditorUtility.SetDirty(placer);
    }

    private static void RemoveGeneratedObject(SplineExtrudeHandLadderPlacer placer)
    {
        Transform root = placer.GeneratedRoot;
        if (root != null)
            Undo.DestroyObjectImmediate(root.gameObject);
        placer.SetGeneratedRoot(null);
    }

    private static void SetFloat(SerializedObject serialized, string name, float value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.floatValue = value;
    }

    private static void SetInt(SerializedObject serialized, string name, int value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.intValue = value;
    }

    private static void SetBool(SerializedObject serialized, string name, bool value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.boolValue = value;
    }

    private static void SetVector3(SerializedObject serialized, string name, Vector3 value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.vector3Value = value;
    }

    private static void SetObject(SerializedObject serialized, string name, Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.objectReferenceValue = value;
    }
}

internal static class SplineExtrudeHandLadderPlacerRegression
{
    private const string TestScenePath = "Assets/Scenes/2. HoleHallways.unity";

    [MenuItem("Tools/Tests/Run Spline Extrude Hand Ladder Regression")]
    public static void Run()
    {
        GameObject sourceObject = null;
        try
        {
            sourceObject = new GameObject("RegressionTubeSpline");
            SplineContainer container = sourceObject.AddComponent<SplineContainer>();
            container.Splines = new List<Spline>
            {
                new(new[]
                {
                    new float3(0f, 0f, 0f),
                    new float3(0f, 0f, 10f)
                }, TangentMode.Linear)
            };

            SplineExtrude extrude = sourceObject.AddComponent<SplineExtrude>();
            extrude.Container = container;
            extrude.Radius = 2f;
            SplineExtrudeHandLadderPlacer placer =
                sourceObject.AddComponent<SplineExtrudeHandLadderPlacer>();
            placer.SetSource(extrude);

            if (!placer.TryBuildSpine(out HandLadderSpine spine, out float length,
                    out Vector3 basePoint, out Vector3 tangent, out Vector3 approach,
                    out string error))
                throw new System.InvalidOperationException(error);

            if (spine == null || spine.Count < 2 || Mathf.Abs(length - 10f) > .02f)
                throw new System.InvalidOperationException(
                    "A escada nao amostrou o comprimento real do tubo.");
            if (Vector3.Distance(spine.center[0], basePoint) > .001f ||
                Mathf.Abs(tangent.magnitude - 1f) > .001f)
                throw new System.InvalidOperationException(
                    "A amostra inicial ou a tangente da escada estao incorretas.");

            Vector3 axis = (Vector3)container.EvaluatePosition(0, 0f);
            Vector3 radial = (basePoint - axis).normalized;
            if (Vector3.Dot(approach, radial) > -.99f ||
                Mathf.Abs(Vector3.Distance(basePoint, axis) - 1.94f) > .01f)
                throw new System.InvalidOperationException(
                    "A escada nao respeitou a face interna e a folga da parede do tubo.");

            Debug.Log("Spline Extrude Hand Ladder Regression: aprovada.");
        }
        finally
        {
            if (sourceObject != null)
                Object.DestroyImmediate(sourceObject);
        }
    }

    [MenuItem("Tools/Tests/Run Spline Extrude Hand Ladder Scene Test")]
    public static void RunInHoleHallwaysScene()
    {
        Scene testScene = default;
        Scene previousActiveScene = SceneManager.GetActiveScene();
        GameObject temporaryAuthoring = null;
        bool passed = false;
        try
        {
            testScene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(testScene);
            List<SplineExtrude> extrusions = new();
            foreach (GameObject root in testScene.GetRootGameObjects())
                extrusions.AddRange(root.GetComponentsInChildren<SplineExtrude>(true));
            HandLadderProfile profile = AssetDatabase.LoadAssetAtPath<HandLadderProfile>(
                "Assets/Profiles/HandLadderProfile.asset");

            for (int i = 0; i < extrusions.Count; i++)
            {
                SplineExtrude extrusion = extrusions[i];
                if (extrusion == null)
                    continue;

                temporaryAuthoring = new GameObject("Temporary Spline Ladder Scene Test");
                SplineExtrudeHandLadderPlacer placer =
                    temporaryAuthoring.AddComponent<SplineExtrudeHandLadderPlacer>();
                placer.SetSource(extrusion);
                placer.SetLadderProfile(profile);
                if (!placer.TryBuildSpine(out _, out float length, out _, out _, out _, out _))
                {
                    Object.DestroyImmediate(temporaryAuthoring);
                    temporaryAuthoring = null;
                    continue;
                }

                if (!SplineExtrudeHandLadderPlacerGenerator.Generate(placer))
                    throw new System.InvalidOperationException(
                        $"A geracao falhou para o SplineExtrude '{extrusion.name}'.");

                Transform root = placer.GeneratedRoot;
                HandLadder ladder = root != null
                    ? root.GetComponentInChildren<HandLadder>(true) : null;
                if (ladder == null || !ladder.IsCurved || ladder.Spine == null ||
                    ladder.Spine.Count < 2 || Mathf.Abs(ladder.Height - length) > .02f ||
                    ladder.GeneratedRungCount <= 0)
                    throw new System.InvalidOperationException(
                        $"A escada gerada para '{extrusion.name}' esta incompleta.");

                float midProgress = ladder.Height * .5f;
                Vector3 interactionProbe = ladder.PlanePointAt(midProgress) +
                    ladder.GetNormal(midProgress) * .5f;
                if (!ladder.TryGetEntryAt(interactionProbe, out HandLadderGrip grip) ||
                    float.IsNaN(grip.progress) ||
                    float.IsNaN(ladder.GetGripPosition(grip).x))
                    throw new System.InvalidOperationException(
                        $"A zona de entrada curva nao reconheceu um ponto valido em '{extrusion.name}'.");

                Debug.Log($"Spline Extrude Hand Ladder Scene Test: aprovada em '{extrusion.name}', " +
                    $"comprimento {length:0.00} m, {ladder.Spine.Count} amostras e " +
                    $"{ladder.GeneratedRungCount} degraus.");
                passed = true;
                break;
            }

            if (!passed)
                throw new System.InvalidOperationException(
                    $"Nenhum SplineExtrude valido foi encontrado em {TestScenePath}.");
        }
        finally
        {
            if (temporaryAuthoring != null)
                Object.DestroyImmediate(temporaryAuthoring);

            if (testScene.IsValid())
            {
                // O teste descarta a cena temporaria sem salvar.
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
                EditorSceneManager.CloseScene(testScene, true);
            }
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class HandLadderGenerator
{
    private const string GeneratedName = "[Generated] Hand Ladder";

    public static void GenerateNow(HandLadder ladder)
    {
        if (ladder == null) return;
        Transform old = ladder.GeneratedRoot;
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        GameObject rootObject = new(GeneratedName);
        Undo.RegisterCreatedObjectUndo(rootObject, "Gerar escada de mão");
        Transform root = rootObject.transform;
        root.SetParent(ladder.transform, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;
        int count = 0;
        Undo.RecordObject(ladder, "Atualizar escada de mão");
        EnsureExitMarker(ladder, true);
        EnsureExitMarker(ladder, false);
        HandLadderProfile profile = ladder.Profile;
        GameObject entryObject = new("EntryVolume");
        Undo.RegisterCreatedObjectUndo(entryObject, "Gerar área de interação da escada");
        entryObject.transform.SetParent(root, false);
        BoxCollider entry = entryObject.AddComponent<BoxCollider>();
        entry.isTrigger = true;
        entryObject.transform.localRotation = Quaternion.LookRotation(
            ladder.LocalApproachAxis, ladder.LocalClimbAxis);
        entryObject.AddComponent<HandLadderInteractionArea>();
        entry.center = new Vector3(0f, ladder.Height * .5f, 0f);
        entry.size = new Vector3(ladder.Width + .8f, ladder.Height + .6f, 1f);
        if (profile != null && profile.rungPrefab != null)
        {
            float spacing = Mathf.Max(.05f, profile.rungSpacing + ladder.ExtraGap);
            count = Mathf.Max(1, Mathf.FloorToInt(ladder.Height / spacing) + 1);
            float half = ladder.Width * .5f;
            for (int i = 0; i < count; i++)
            {
                float y = Mathf.Min(i * spacing, ladder.Height);
                Vector3 rungAxis = AxisVector(profile.rungWidthAxis);
                Quaternion rungRotation = AlignAxis(rungAxis,
                    ladder.LocalWidthAxis, profile.rungRollDegrees);
                CreatePiece(profile.rungPrefab, root, $"Rung_{i + 1:000}",
                    ladder.LocalClimbAxis * y, rungRotation,
                    profile.rungWidthAxis, ladder.Width);
            }

            if (ladder.GenerateSideRails && profile.railPrefab != null)
            {
                Vector3 railAxis = AxisVector(profile.railLengthAxis);
                Quaternion railRotation = AlignAxis(railAxis,
                    ladder.LocalClimbAxis, profile.railRollDegrees);
                Vector3 railCenter = ladder.LocalClimbAxis * (ladder.Height * .5f) -
                    ladder.LocalApproachAxis * (profile.railThickness * .5f);
                CreatePiece(profile.railPrefab, root, "Rail_Left",
                    railCenter - ladder.LocalWidthAxis * half, railRotation,
                    profile.railLengthAxis, ladder.Height,
                    profile.railThicknessAxis, profile.railThickness);
                CreatePiece(profile.railPrefab, root, "Rail_Right",
                    railCenter + ladder.LocalWidthAxis * half, railRotation,
                    profile.railLengthAxis, ladder.Height,
                    profile.railThicknessAxis, profile.railThickness);
            }
        }
        ladder.SetGenerated(root, count);
        EditorUtility.SetDirty(ladder);
        EditorSceneManager.MarkSceneDirty(ladder.gameObject.scene);
    }

    private static void EnsureExitMarker(HandLadder ladder, bool isTop)
    {
        string name = isTop ? "TopExit" : "BottomExit";
        Transform marker = isTop ? ladder.TopExit : ladder.BottomExit;
        bool generated = isTop ? ladder.TopExitGenerated : ladder.BottomExitGenerated;
        Vector3 previousPosition = isTop
            ? ladder.LastGeneratedTopExitPosition
            : ladder.LastGeneratedBottomExitPosition;
        bool createdNow = false;
        Vector3 defaultPosition = ladder.LocalClimbAxis * (isTop ? ladder.Height : 0f) +
            ladder.LocalApproachAxis * (ladder.PlayerForwardOffset + .55f);

        if (marker == null)
        {
            marker = ladder.transform.Find(name);
            if (marker == null)
            {
                GameObject markerObject = new(name);
                Undo.RegisterCreatedObjectUndo(markerObject, "Criar ponto de saída da escada");
                marker = markerObject.transform;
                marker.SetParent(ladder.transform, false);
                marker.localPosition = defaultPosition;
                marker.localRotation = Quaternion.identity;
                generated = true;
                createdNow = true;
            }
            else
            {
                // Um transform criado pelo usuário mantém sua posição manual.
                generated = false;
            }
        }

        if (generated)
        {
            bool stillToolOwned = createdNow || (marker.parent == ladder.transform &&
                marker.name == name &&
                (marker.localPosition - previousPosition).sqrMagnitude < .000001f);
            if (!stillToolOwned)
                generated = false;
            else
            {
                Undo.RecordObject(marker, "Atualizar ponto de saída da escada");
                marker.localPosition = defaultPosition;
            }
        }

        if (isTop)
        {
            ladder.SetExitMarkers(marker, ladder.BottomExit);
            ladder.TopExitGenerated = generated;
            if (generated) ladder.LastGeneratedTopExitPosition = marker.localPosition;
        }
        else
        {
            ladder.SetExitMarkers(ladder.TopExit, marker);
            ladder.BottomExitGenerated = generated;
            if (generated) ladder.LastGeneratedBottomExitPosition = marker.localPosition;
        }
    }

    private static void CreatePiece(GameObject prefab, Transform root, string name,
        Vector3 localPosition, Quaternion localRotation,
        HandLadderPrefabAxis fitAxis, float fitSize,
        HandLadderPrefabAxis? secondaryFitAxis = null, float secondaryFitSize = 0f)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) instance = Object.Instantiate(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Gerar módulo da escada");
        instance.name = name;
        Transform t = instance.transform;
        t.SetParent(root, false);
        t.localPosition = localPosition;
        t.localRotation = localRotation;
        Bounds visualBounds = GetLocalVisualBounds(instance);
        Vector3 scale = t.localScale;
        FitScale(ref scale, visualBounds, fitAxis, fitSize);
        if (secondaryFitAxis.HasValue && secondaryFitAxis.Value != fitAxis)
            FitScale(ref scale, visualBounds, secondaryFitAxis.Value,
                secondaryFitSize);
        t.localScale = scale;
        foreach (Collider c in instance.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
    }

    private static void FitScale(ref Vector3 scale, Bounds bounds,
        HandLadderPrefabAxis axis, float targetSize)
    {
        int index = AxisIndex(axis);
        float sourceSize = Mathf.Abs(bounds.size[index]);
        if (sourceSize < .0001f) return;
        float sign = Mathf.Sign(scale[index]);
        scale[index] = sign * Mathf.Max(.001f, targetSize) / sourceSize;
    }

    private static Bounds GetLocalVisualBounds(GameObject instance)
    {
        Transform root = instance.transform;
        Matrix4x4 rootInverse = root.worldToLocalMatrix;
        Bounds result = new(Vector3.zero, Vector3.one);
        bool hasBounds = false;
        foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null || filter.sharedMesh == null) continue;
            AddBounds(ref result, ref hasBounds, filter.sharedMesh.bounds,
                rootInverse * filter.transform.localToWorldMatrix);
        }
        foreach (SkinnedMeshRenderer renderer in
                 instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer == null || renderer.sharedMesh == null) continue;
            AddBounds(ref result, ref hasBounds, renderer.sharedMesh.bounds,
                rootInverse * renderer.transform.localToWorldMatrix);
        }
        return hasBounds ? result : new Bounds(Vector3.zero, Vector3.one);
    }

    private static void AddBounds(ref Bounds aggregate, ref bool hasBounds,
        Bounds source, Matrix4x4 matrix)
    {
        Vector3 min = source.min;
        Vector3 max = source.max;
        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            Vector3 corner = new(x == 0 ? min.x : max.x,
                y == 0 ? min.y : max.y,
                z == 0 ? min.z : max.z);
            Vector3 point = matrix.MultiplyPoint3x4(corner);
            if (!hasBounds)
            {
                aggregate = new Bounds(point, Vector3.zero);
                hasBounds = true;
            }
            else aggregate.Encapsulate(point);
        }
    }

    private static Quaternion AlignAxis(Vector3 source, Vector3 target, float roll)
    {
        source = source.sqrMagnitude > .0001f ? source.normalized : Vector3.right;
        target = target.sqrMagnitude > .0001f ? target.normalized : Vector3.right;
        Quaternion align = Quaternion.FromToRotation(source, target);
        return Quaternion.AngleAxis(roll, target) * align;
    }

    private static Vector3 AxisVector(HandLadderPrefabAxis axis) => axis switch
    {
        HandLadderPrefabAxis.Y => Vector3.up,
        HandLadderPrefabAxis.Z => Vector3.forward,
        _ => Vector3.right
    };

    private static int AxisIndex(HandLadderPrefabAxis axis) => axis switch
    {
        HandLadderPrefabAxis.Y => 1,
        HandLadderPrefabAxis.Z => 2,
        _ => 0
    };

}

[CustomEditor(typeof(HandLadder))]
internal sealed class HandLadderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();
        HandLadder ladder = (HandLadder)target;
        if (ladder.GeneratedRoot == null)
            HandLadderGenerator.GenerateNow(ladder);
        EditorGUILayout.Space();
        if (GUILayout.Button("Create Profile"))
            CreateAndAssignProfile(ladder);
        EditorGUILayout.HelpBox(
            "A prévia é reconstruída ao alterar os parâmetros. TopExit e BottomExit são criados nos extremos da escada e acompanham a altura enquanto automáticos; mova-os ou atribua outros Transforms para ajustar as saídas manualmente.",
            MessageType.Info);
        using (new EditorGUI.DisabledScope(ladder.Profile == null || ladder.Profile.rungPrefab == null))
        {
            if (GUILayout.Button("Atualizar prévia da escada"))
                HandLadderGenerator.GenerateNow(ladder);
        }
        if (changed) HandLadderGenerator.GenerateNow(ladder);
        if (ladder.Profile != null)
            EditorGUILayout.LabelField("Degraus gerados", ladder.GeneratedRungCount.ToString());
    }

    private static void CreateAndAssignProfile(HandLadder ladder)
    {
        const string parentFolder = "Assets/Profiles";
        const string folder = "Assets/Profiles/HandLadders";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            if (!AssetDatabase.IsValidFolder(parentFolder))
                AssetDatabase.CreateFolder("Assets", "Profiles");
            AssetDatabase.CreateFolder(parentFolder, "HandLadders");
        }

        const string assetName = "HandLadderProfile";
        string path = AssetDatabase.GenerateUniqueAssetPath(
            $"{folder}/{assetName}.asset");
        HandLadderProfile profile = CreateInstance<HandLadderProfile>();
        profile.name = assetName;
        AssetDatabase.CreateAsset(profile, path);
        Undo.RecordObject(ladder, "Atribuir perfil da escada");
        SerializedObject serialized = new(ladder);
        serialized.FindProperty("profile").objectReferenceValue = profile;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(ladder);
        EditorGUIUtility.PingObject(profile);
        Selection.activeObject = profile;
        HandLadderGenerator.GenerateNow(ladder);
    }
}

[CustomEditor(typeof(HandLadderProfile))]
internal sealed class HandLadderProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (!EditorGUI.EndChangeCheck()) return;
        HandLadderProfile profile = (HandLadderProfile)target;
        foreach (HandLadder ladder in Object.FindObjectsByType<HandLadder>(
                     FindObjectsInactive.Include))
            if (ladder != null && ladder.gameObject.scene.IsValid() &&
                PrefabStageUtility.GetPrefabStage(ladder.gameObject) == null &&
                ladder.Profile == profile)
                HandLadderGenerator.GenerateNow(ladder);
    }
}

internal static class HandLadderGenerationRegression
{
    [MenuItem("Tools/Tests/Run Hand Ladder Generation Regression")]
    public static void Run()
    {
        GameObject sourceStep = null;
        GameObject ladderObject = null;
        HandLadderProfile profile = null;
        Mesh sourceMesh = null;
        try
        {
            sourceStep = new GameObject("RegressionStepPrefab");
            sourceMesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-1f, -.125f, -.1f), new Vector3(1f, -.125f, -.1f),
                    new Vector3(1f, .125f, -.1f), new Vector3(-1f, .125f, -.1f),
                    new Vector3(-1f, -.125f, .1f), new Vector3(1f, -.125f, .1f),
                    new Vector3(1f, .125f, .1f), new Vector3(-1f, .125f, .1f)
                }
            };
            sourceMesh.RecalculateBounds();
            sourceStep.AddComponent<MeshFilter>().sharedMesh = sourceMesh;
            sourceStep.AddComponent<MeshRenderer>();
            profile = ScriptableObject.CreateInstance<HandLadderProfile>();
            profile.rungPrefab = sourceStep;
            profile.rungSpacing = .5f;
            profile.defaultWidth = 1f;
            profile.rungWidthAxis = HandLadderPrefabAxis.Z;
            profile.railPrefab = sourceStep;
            ladderObject = new GameObject("RegressionHandLadder");
            HandLadder ladder = ladderObject.AddComponent<HandLadder>();
            SerializedObject serialized = new(ladder);
            serialized.FindProperty("profile").objectReferenceValue = profile;
            serialized.FindProperty("height").floatValue = 2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            HandLadderGenerator.GenerateNow(ladder);
            Transform generated = ladder.GeneratedRoot;
            if (generated == null || ladder.GeneratedRungCount != 5 ||
                generated.Find("Rung_001") == null ||
                generated.Find("Rung_005") == null ||
                generated.Find("Rail_Left") == null ||
                generated.Find("Rail_Right") == null ||
                generated.Find("EntryVolume") == null ||
                generated.Find("EntryVolume").GetComponent<HandLadderInteractionArea>() == null ||
                ladder.TopExit == null || ladder.BottomExit == null ||
                Mathf.Abs(ladder.TopExit.localPosition.y - 2f) > .001f ||
                Mathf.Abs(ladder.BottomExit.localPosition.y) > .001f)
                throw new System.InvalidOperationException(
                    "A escada deve gerar 5 degraus, dois trilhos, entrada e pontos de saída nos extremos para 2m/0.5m.");

            Vector3 climbAxis = new(0f, 1f, 1f);
            Vector3 approachAxis = new(0f, 1f, -1f);
            serialized.Update();
            serialized.FindProperty("climbAxis").vector3Value = climbAxis;
            serialized.FindProperty("approachAxis").vector3Value = approachAxis;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            HandLadderGenerator.GenerateNow(ladder);
            Vector3 expectedClimb = climbAxis.normalized;
            Vector3 expectedApproach = approachAxis.normalized;
            Transform entryVolume = ladder.GeneratedRoot.Find("EntryVolume");
            Transform fittedRung = ladder.GeneratedRoot.Find("Rung_002");
            Transform fittedRail = ladder.GeneratedRoot.Find("Rail_Left");
            if ((entryVolume.localRotation * Vector3.up - expectedClimb).sqrMagnitude > .0001f ||
                (entryVolume.localRotation * Vector3.forward - expectedApproach).sqrMagnitude > .0001f ||
                (fittedRung.localPosition - expectedClimb * .5f).sqrMagnitude > .0001f ||
                (fittedRung.localRotation * Vector3.forward - ladder.LocalWidthAxis).sqrMagnitude > .0001f ||
                Mathf.Abs(fittedRung.localScale.z - 5f) > .001f ||
                Mathf.Abs(fittedRail.localScale.y - 8f) > .001f ||
                Mathf.Abs(fittedRail.localScale.x - .04f) > .001f ||
                (ladder.BottomExit.localPosition -
                 expectedApproach * (ladder.PlayerForwardOffset + .55f)).sqrMagnitude > .0001f)
                throw new System.InvalidOperationException(
                    "Eixos locais inclinados devem orientar degraus, area de interacao e saidas.");

            Vector3 stableBottomPosition = ladder.BottomExit.localPosition;
            Vector3 manualTopPosition = ladder.TopExit.localPosition + Vector3.right * .2f;
            ladder.TopExit.localPosition = manualTopPosition;
            serialized.Update();
            serialized.FindProperty("height").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            HandLadderGenerator.GenerateNow(ladder);
            if ((ladder.TopExit.localPosition - manualTopPosition).sqrMagnitude > .000001f ||
                (ladder.BottomExit.localPosition - stableBottomPosition).sqrMagnitude > .000001f)
                throw new System.InvalidOperationException(
                    "Mover o ponto do topo deve preservar a posicao manual ao regenerar a escada.");

            Debug.Log("PASS: Hand ladder generation regression.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("FAIL: Hand ladder generation regression: " + exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
        finally
        {
            if (ladderObject != null) Object.DestroyImmediate(ladderObject);
            if (sourceStep != null) Object.DestroyImmediate(sourceStep);
            if (sourceMesh != null) Object.DestroyImmediate(sourceMesh);
            if (profile != null) Object.DestroyImmediate(profile);
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class HandLadderGenerator
{
    private const string GeneratedName = "[Generated] Hand Ladder";

    /// <summary>Recria o visual (degraus e trilhos). Sem perfil/prefab, so remove o visual antigo.</summary>
    public static void GenerateNow(HandLadder ladder)
    {
        if (ladder == null) return;
        Transform old = ladder.GeneratedRoot;
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        Undo.RecordObject(ladder, "Atualizar escada de mão");

        HandLadderProfile profile = ladder.Profile;
        if (profile == null || profile.rungPrefab == null)
        {
            ladder.SetGenerated(null, 0);
            EditorUtility.SetDirty(ladder);
            return;
        }

        GameObject rootObject = new(GeneratedName);
        Undo.RegisterCreatedObjectUndo(rootObject, "Gerar escada de mão");
        Transform root = rootObject.transform;
        root.SetParent(ladder.transform, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        solidPieces = ladder.HasCollision;
        float spacing = ladder.RungSpacing;
        int count = ladder.RungCount;
        float half = ladder.Width * .5f;
        bool curved = ladder.IsCurved;
        for (int i = 0; i < count; i++)
        {
            float y = Mathf.Min(i * spacing, ladder.Height);
            if (curved)
            {
                // Degrau em duas metades (esquerda-centro e centro-direita): acompanha
                // a curvatura tambem na largura, sem cortar a superficie.
                Vector3 leftPoint = ladder.LocalLanePoint(-1, y);
                Vector3 centerPoint = ladder.LocalLanePoint(0, y);
                Vector3 rightPoint = ladder.LocalLanePoint(1, y);
                for (int half2 = 0; half2 < 2; half2++)
                {
                    Vector3 a = half2 == 0 ? leftPoint : centerPoint;
                    Vector3 b = half2 == 0 ? centerPoint : rightPoint;
                    Vector3 across = b - a;
                    if (across.sqrMagnitude < 1e-6f)
                        continue;
                    CreatePiece(profile.rungPrefab, root,
                        $"Rung_{i + 1:000}_{(half2 == 0 ? "L" : "R")}", (a + b) * .5f,
                        AlignAxis(AxisVector(profile.rungWidthAxis), across, profile.rungRollDegrees),
                        profile.rungWidthAxis, across.magnitude + profile.rungThickness * .5f,
                        profile.rungThickness);
                }

                continue;
            }

            Quaternion rungRotation = AlignAxis(AxisVector(profile.rungWidthAxis),
                ladder.LocalWidthAxis, profile.rungRollDegrees);
            CreatePiece(profile.rungPrefab, root, $"Rung_{i + 1:000}",
                ladder.LocalClimbAxis * y, rungRotation,
                profile.rungWidthAxis, ladder.Width, profile.rungThickness);
        }

        if (ladder.GenerateSideRails && profile.railPrefab != null && curved)
        {
            // Trilhos em segmentos entre as amostras da superficie (com leve sobreposicao).
            HandLadderSpine spine = ladder.Spine;
            for (int lane = -1; lane <= 1; lane += 2)
                for (int i = 0; i < spine.Count - 1; i++)
                {
                    Vector3 a = ladder.LocalLanePoint(lane, spine.progress[i]);
                    Vector3 b = ladder.LocalLanePoint(lane, spine.progress[i + 1]);
                    Vector3 direction = b - a;
                    if (direction.sqrMagnitude < 1e-6f)
                        continue;
                    CreatePiece(profile.railPrefab, root,
                        $"Rail_{(lane < 0 ? "Left" : "Right")}_{i + 1:000}",
                        (a + b) * .5f,
                        AlignAxis(AxisVector(profile.railLengthAxis), direction,
                            profile.railRollDegrees),
                        profile.railLengthAxis,
                        direction.magnitude + profile.railThickness * .6f,
                        profile.railThickness);
                }
        }
        else if (ladder.GenerateSideRails && profile.railPrefab != null)
        {
            Quaternion railRotation = AlignAxis(AxisVector(profile.railLengthAxis),
                ladder.LocalClimbAxis, profile.railRollDegrees);
            // Os trilhos ficam no mesmo plano dos degraus (que os atravessam).
            Vector3 railCenter = ladder.LocalClimbAxis * (ladder.Height * .5f);
            CreatePiece(profile.railPrefab, root, "Rail_Left",
                railCenter - ladder.LocalWidthAxis * half, railRotation,
                profile.railLengthAxis, ladder.Height, profile.railThickness);
            CreatePiece(profile.railPrefab, root, "Rail_Right",
                railCenter + ladder.LocalWidthAxis * half, railRotation,
                profile.railLengthAxis, ladder.Height, profile.railThickness);
        }

        ladder.SetGenerated(root, count);
        EditorUtility.SetDirty(ladder);
        EditorSceneManager.MarkSceneDirty(ladder.gameObject.scene);
    }

    /// <summary>
    /// Encosta a escada na superficie logo atras dela (com Undo). Devolve falso se
    /// nao houver superficie ao alcance; so grava se a pose realmente mudar.
    /// </summary>
    public static bool TrySnapToSurface(HandLadder ladder)
    {
        if (ladder == null || !ladder.TryComputeSurfaceSnap(out Vector3 position, out Quaternion rotation))
            return false;

        Transform t = ladder.transform;
        bool moved = (t.position - position).sqrMagnitude > 1e-8f ||
            Quaternion.Angle(t.rotation, rotation) > .01f;
        if (moved || ladder.Attachment != HandLadderAttachment.Surface)
        {
            Undo.RecordObject(t, "Encaixar escada na superfície");
            Undo.RecordObject(ladder, "Encaixar escada na superfície");
            t.SetPositionAndRotation(position, rotation);
            ladder.SetAttachment(HandLadderAttachment.Surface);
            EditorUtility.SetDirty(ladder);
            EditorUtility.SetDirty(t);
        }

        // Curvatura da superficie: regrava a espinha e o visual so se mudou.
        HandLadderSpine spine = ladder.BuildSurfaceSpine();
        bool spineChanged = spine == null
            ? ladder.HasSpine : !spine.ApproximatelyEquals(ladder.Spine);
        if (spineChanged)
        {
            Undo.RecordObject(ladder, "Acompanhar curvatura da superfície");
            ladder.SetSpine(spine);
            EditorUtility.SetDirty(ladder);
            GenerateNow(ladder);
        }

        return true;
    }

    private static bool solidPieces;

    private static void CreatePiece(GameObject prefab, Transform root, string name,
        Vector3 localPosition, Quaternion localRotation,
        HandLadderPrefabAxis fitAxis, float fitSize, float crossSize = 0f)
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
        // Espessura: ajusta os dois eixos perpendiculares ao comprimento.
        if (crossSize > 0f)
            for (int axis = 0; axis < 3; axis++)
                if (axis != AxisIndex(fitAxis))
                    FitScale(ref scale, visualBounds, (HandLadderPrefabAxis)axis, crossSize);
        t.localScale = scale;
        foreach (Collider c in instance.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        if (solidPieces)
            AddBoxCollider(instance, GetLocalVisualBounds(instance));
    }

    // Colisao simples da peca: uma caixa ajustada a malha visual (ja escalada).
    private static void AddBoxCollider(GameObject instance, Bounds localBounds)
    {
        BoxCollider box = instance.AddComponent<BoxCollider>();
        box.center = localBounds.center;
        box.size = localBounds.size;
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
        InteractionInspectorUtility.DrawProperties(this);
        bool changed = EditorGUI.EndChangeCheck();
        HandLadder ladder = (HandLadder)target;
        bool canGenerate = ladder.Profile != null && ladder.Profile.rungPrefab != null;
        if (ladder.GeneratedRoot == null && canGenerate)
            HandLadderGenerator.GenerateNow(ladder);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Encaixar na superfície"))
            {
                if (!HandLadderGenerator.TrySnapToSurface(ladder))
                    Debug.LogWarning(
                        $"Hand Ladder '{ladder.name}': nenhuma superfície encontrada atrás da escada (seta rosa = frente; procura na direção oposta, até {ladder.SurfaceSearchDistance} m, nas camadas Surface Layers).",
                        ladder);
            }

            if (GUILayout.Button("Criar perfil"))
                CreateAndAssignProfile(ladder);
        }

        using (new EditorGUI.DisabledScope(!canGenerate))
            if (GUILayout.Button("Atualizar visual gerado"))
                HandLadderGenerator.GenerateNow(ladder);
        EditorGUILayout.HelpBox(
            "Gizmos: caixa ciano = zona onde o prompt aparece; seta rosa = normal (direção do salto); linha amarela = trilho do jogador; esferas verde/laranja = saídas do topo/base. " +
            "Sem perfil, a escada não gera visual: use sua própria malha e ajuste Height/Width pelos gizmos.",
            MessageType.Info);
        if (changed && ladder.Attachment != HandLadderAttachment.Surface && ladder.HasSpine)
        {
            Undo.RecordObject(ladder, "Remover curvatura da escada");
            ladder.SetSpine(null);
            EditorUtility.SetDirty(ladder);
        }

        if (changed && ladder.Attachment == HandLadderAttachment.Surface && ladder.AutoSnapToSurface)
            HandLadderGenerator.TrySnapToSurface(ladder);
        if (changed && (canGenerate || ladder.GeneratedRoot != null))
            HandLadderGenerator.GenerateNow(ladder);
        if (canGenerate)
            EditorGUILayout.LabelField("Degraus gerados", ladder.GeneratedRungCount.ToString());
    }

    // Em Surface, ao soltar o mouse depois de mover/girar, encaixa na superficie.
    private void OnSceneGUI()
    {
        HandLadder ladder = (HandLadder)target;
        if (Application.isPlaying || ladder == null ||
            ladder.Attachment != HandLadderAttachment.Surface || !ladder.AutoSnapToSurface)
            return;

        EventType type = Event.current.type;
        if (type != EventType.MouseUp && type != EventType.KeyUp &&
            type != EventType.ExecuteCommand)
            return;
        EditorApplication.delayCall += () =>
        {
            if (ladder != null && !Application.isPlaying)
                HandLadderGenerator.TrySnapToSurface(ladder);
        };
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
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new System.InvalidOperationException(message);
    }

    private static void Set(HandLadder ladder, string field, object value)
    {
        SerializedObject serialized = new(ladder);
        SerializedProperty property = serialized.FindProperty(field);
        switch (value)
        {
            case float f: property.floatValue = f; break;
            case int i: property.intValue = i; break;
            case bool b: property.boolValue = b; break;
            case Vector3 v: property.vector3Value = v; break;
            case Object o: property.objectReferenceValue = o; break;
            case null: property.objectReferenceValue = null; break;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Tests/Run Hand Ladder Generation Regression")]
    public static void Run()
    {
        GameObject sourceStep = null;
        GameObject ladderObject = null;
        GameObject wall = null;
        GameObject ledge = null;
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
            Set(ladder, "profile", profile);
            Set(ladder, "height", 2f);

            // 1. Geracao: degraus, trilhos, nenhuma entrada/saida filha.
            HandLadderGenerator.GenerateNow(ladder);
            Transform generated = ladder.GeneratedRoot;
            Check(generated != null && ladder.GeneratedRungCount == 5 &&
                generated.Find("Rung_005") != null && generated.Find("Rail_Left") != null &&
                generated.Find("Rail_Right") != null && generated.Find("EntryVolume") == null &&
                ladder.TopExit == null && ladder.BottomExit == null,
                "Deve gerar 5 degraus e 2 trilhos, sem volumes nem marcadores filhos.");
            // Altura 2, folga do topo .9 => ate 1.1 m; vaos em .25 e .75.
            Check(ladder.SlotCount == 2 && Mathf.Abs(ladder.SlotHeight(0) - .25f) < .001f &&
                Mathf.Abs(ladder.SlotHeight(1) - .75f) < .001f && ladder.NearestSlot(1.1f) == 1 &&
                Mathf.Abs(ladder.MaxProgress - 1.1f) < .001f,
                "Posicoes de parada devem ficar nos vaos entre degraus e parar antes do topo (.25, .75).");
            Transform rail = generated.Find("Rail_Left");
            // Malha de teste: 2 x .25 x .2 (X, Y, Z); comprimento no Y, espessura em X e Z.
            Check(Mathf.Abs(rail.localScale.x * 2f - profile.railThickness) < .001f &&
                Mathf.Abs(rail.localScale.z * .2f - profile.railThickness) < .001f,
                "A espessura do trilho vale nos dois eixos perpendiculares ao comprimento.");
            Set(ladder, "profile", null);
            HandLadderGenerator.GenerateNow(ladder);
            Check(ladder.GeneratedRoot == null && ladder.transform.childCount == 0,
                "Sem perfil o visual gerado deve ser removido.");
            Check(ladder.RungSpacing == 0f && ladder.SlotCount == 0,
                "Sem perfil nem espacamento manual a subida e continua.");

            Set(ladder, "invertStartAndEnd", true);
            Check((ladder.ClimbDirection - Vector3.down).sqrMagnitude < .001f &&
                (ladder.PlanePointAt(0f) - Vector3.up * 2f).sqrMagnitude < .001f &&
                ladder.TryGetEntryAt(new Vector3(0f, 1.5f, .6f), out HandLadderGrip reversedGrip) &&
                Mathf.Abs(reversedGrip.progress - .5f) < .001f,
                "Inverter inicio/fim deve preservar a geometria e inverter direcao e progresso.");
            Set(ladder, "invertStartAndEnd", false);

            // 2. Zona de entrada da escada solta (frente = +Z, 1 m de largura).
            Check(ladder.TryGetEntryAt(new Vector3(0f, 1f, .6f), out HandLadderGrip grip) &&
                grip.side == 1 && Mathf.Abs(grip.progress - 1f) < .001f,
                "Ponto a frente da escada deve estar na zona de entrada.");
            Check(!ladder.TryGetEntryAt(new Vector3(0f, 1f, 2f), out _),
                "Alem da profundidade a zona nao vale.");
            Check(!ladder.TryGetEntryAt(new Vector3(2f, 1f, .5f), out _),
                "Fora da largura a zona nao vale.");
            Check(!ladder.TryGetEntryAt(new Vector3(0f, 1f, -.5f), out _),
                "Escada solta de um lado nao aceita entrada por tras.");
            Set(ladder, "climbableFromBothSides", true);
            Check(ladder.TryGetEntryAt(new Vector3(0f, 1f, -.5f), out grip) && grip.side == -1,
                "Escada de dois lados deve aceitar o lado de tras (side -1).");
            Check((ladder.GetJumpDirection(-1) - Vector3.back).sqrMagnitude < .0001f,
                "O salto do lado de tras vai para tras.");
            Set(ladder, "climbableFromBothSides", false);

            // 3. Saidas: solta sai pela frente; em superficie, o topo sai por tras.
            HandLadderGrip topGrip = new() { side = 1, progress = 2f };
            Vector3 freeTop = ladder.GetExitPosition(topGrip, true);
            Check(Mathf.Abs(freeTop.z - .93f) < .001f && Mathf.Abs(freeTop.y - 2.05f) < .001f,
                "Saida do topo da escada solta: " + freeTop);
            Set(ladder, "attachment", (int)HandLadderAttachment.Surface);
            Vector3 wallTop = ladder.GetExitPosition(topGrip, true);
            Check(Mathf.Abs(wallTop.z + .55f) < .001f,
                "Saida do topo em superficie vai para tras: " + wallTop);
            Check(ladder.TryGetEntryAt(new Vector3(0f, 2.2f, -.5f), out _),
                "Escada em superficie deve aceitar entrada por cima, da borda.");
            Set(ladder, "attachment", (int)HandLadderAttachment.Free);

            // Escada solta ao lado de uma caixa alta: o topo sai por cima da caixa.
            ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledge.transform.position = new Vector3(0f, 1f, -1.3f);
            ledge.transform.localScale = new Vector3(4f, 2f, 2f);
            Physics.SyncTransforms();
            Vector3 onBox = ladder.GetExitPosition(topGrip, true);
            Check(Mathf.Abs(onBox.z + .55f) < .001f && Mathf.Abs(onBox.y - 2.02f) < .01f,
                "Topo da escada solta junto de uma caixa deve sair sobre a caixa: " + onBox);
            Object.DestroyImmediate(ledge);
            ledge = null;

            // Gancho: degrau mais proximo da mira (degraus a cada .5 m, vaos em .25/.75).
            Set(ladder, "profile", profile);
            Vector3 shooter = new(0f, 1f, 3f);
            Check(ladder.TryGetGrappleTarget(shooter, new Vector3(0f, .4f, -3f), shooter,
                    out HandLadderGrip hook, out Vector3 hookPoint, out float hookAngle) &&
                Mathf.Abs(hookPoint.y - 1.5f) < .06f && Mathf.Abs(hook.progress - .75f) < .001f &&
                hook.side == 1 && hookAngle < 12f,
                $"Gancho deve mirar o degrau 1.5 e parar no vao .75: {hookPoint} {hook.progress}");
            Vector3 behind = new(0f, 1f, -3f);
            Check(!ladder.TryGetGrappleTarget(behind, new Vector3(0f, .4f, 3f), behind,
                    out _, out _, out _),
                "Atras de uma escada de um lado nao ha gancho.");
            Check(!ladder.TryGetGrappleTarget(shooter, new Vector3(0f, 3f, -1f), shooter,
                    out _, out _, out _),
                "Mira fora do angulo nao pega a escada.");

            // Comportamento vem do perfil; sem perfil vale o padrao.
            float profileClimb = ladder.ClimbSpeed;
            Check(Mathf.Abs(profileClimb - profile.climbSpeed) < .0001f,
                "Velocidade deve vir do perfil da escada.");
            Set(ladder, "profile", null);
            Check(Mathf.Abs(ladder.ClimbSpeed - HandLadderProfile.Default.climbSpeed) < .0001f &&
                ladder.RungSpacing == 0f,
                "Sem perfil: comportamento padrao e subida continua.");

            // Escada cosmetica: nada de entrada nem gancho.
            Set(ladder, "isClimbable", false);
            Check(!ladder.TryGetEntryAt(ladder.transform.position + ladder.ApproachDirection * .5f +
                    Vector3.up * .5f, out _) &&
                !ladder.TryGetGrappleTarget(new Vector3(0f, 1f, 3f), new Vector3(0f, .4f, -3f),
                    new Vector3(0f, 1f, 3f), out _, out _, out _),
                "Escada nao escalavel nao tem entrada nem gancho.");
            Set(ladder, "isClimbable", true);

            // Colisao: so com Has Collision as pecas geradas ganham colisor; ignorada ao escalar.
            Set(ladder, "profile", profile);
            Set(ladder, "hasCollision", true);
            HandLadderGenerator.GenerateNow(ladder);
            Collider rungCollider = ladder.GeneratedRoot.Find("Rung_001").GetComponent<Collider>();
            Check(rungCollider != null && rungCollider.enabled, "Has Collision gera colisores nos degraus.");
            ladder.SetPlayerClimbing(true);
            Check(!rungCollider.enabled, "A colisao da escada e ignorada ao escalar.");
            ladder.SetPlayerClimbing(false);
            Set(ladder, "hasCollision", false);
            HandLadderGenerator.GenerateNow(ladder);
            Check(ladder.GeneratedRoot.Find("Rung_001").GetComponent<Collider>() == null,
                "Sem Has Collision os degraus nao tem colisor proprio.");

            Set(ladder, "profile", null);
            // Escada de teto (monkey bars): barras horizontais, normal para baixo.
            Set(ladder, "overhead", true);
            Check((ladder.ClimbDirection - Vector3.forward).sqrMagnitude < .001f &&
                (ladder.ApproachDirection - Vector3.down).sqrMagnitude < .001f,
                $"Escada de teto: barras em {ladder.ClimbDirection}, normal {ladder.ApproachDirection}");
            Check(ladder.TryGetEntryAt(new Vector3(0f, -1f, 1f), out HandLadderGrip hang) &&
                Mathf.Abs(hang.progress - 1f) < .001f,
                "Embaixo das barras deve estar na zona de entrada.");
            Check(!ladder.TryGetEntryAt(new Vector3(0f, 1f, 1f), out _),
                "Acima das barras nao entra.");
            Vector3 hangPosition = ladder.GetGripPosition(new HandLadderGrip { side = 1, progress = 1f });
            Check((hangPosition - new Vector3(0f, -1.75f, 1f)).sqrMagnitude < .001f &&
                Mathf.Abs(ladder.MaxProgress - 2f) < .001f,
                "Pendurado: pes 1.75 m abaixo das barras: " + hangPosition);
            Vector3 under = new(0f, -1f, -.5f);
            Check(ladder.TryGetGrappleTarget(under, new Vector3(0f, 1f, 1.7f), under,
                    out HandLadderGrip overheadHook, out Vector3 overheadPoint, out _) &&
                Mathf.Abs(overheadPoint.z - 1.2f) < .06f && overheadHook.side == 1,
                "Gancho em escada de teto: " + overheadPoint);
            // Escada de teto acompanha a rotacao do objeto (giro de 90 graus em Y).
            ladderObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Check((ladder.ClimbDirection - Vector3.right).sqrMagnitude < .001f &&
                (ladder.ApproachDirection - Vector3.down).sqrMagnitude < .001f,
                "Escada de teto deve girar com o objeto: " + ladder.ClimbDirection);
            ladderObject.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
            Check(ladder.TryGetEntryAt(ladder.transform.position +
                    ladder.ApproachDirection * 1.2f + ladder.ClimbDirection, out _),
                "A zona da escada de teto acompanha a inclinacao do objeto.");
            ladderObject.transform.rotation = Quaternion.identity;
            Set(ladder, "overhead", false);

            // 4. Encaixar na superficie: gira a escada e cola na parede.
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1f, -1.5f);
            wall.transform.localScale = new Vector3(6f, 4f, 1f);
            ladderObject.transform.rotation = Quaternion.Euler(0f, 50f, 0f);
            ladderObject.transform.position = new Vector3(.2f, 0f, -.6f);
            Physics.SyncTransforms();
            Check(HandLadderGenerator.TrySnapToSurface(ladder),
                "Deve encontrar a parede atras da escada.");
            Check((ladder.ApproachDirection - Vector3.forward).sqrMagnitude < .001f &&
                Mathf.Abs(ladder.transform.position.z + .9f) < .01f &&
                ladder.Attachment == HandLadderAttachment.Surface,
                $"Encaixe: normal {ladder.ApproachDirection}, pos {ladder.transform.position}");

            Vector3 snapped = ladder.transform.position;
            Check(HandLadderGenerator.TrySnapToSurface(ladder) &&
                (ladder.transform.position - snapped).sqrMagnitude < 1e-8f,
                "Encaixar de novo nao pode mover a escada.");
            Set(ladder, "surfaceStandoff", .3f);
            Check(ladder.TrySnapToSurface() && Mathf.Abs(ladder.transform.position.z + .7f) < .01f,
                "A folga deve valer a distancia ate a parede: " + ladder.transform.position);

            // Superficie curva: esfera de raio 5; a escada acompanha a curvatura.
            Object.DestroyImmediate(wall);
            wall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wall.transform.position = new Vector3(0f, 2f, -5.9f);
            wall.transform.localScale = Vector3.one * 10f;
            ladderObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -.5f), Quaternion.identity);
            Set(ladder, "height", 4f);
            Set(ladder, "surfaceStandoff", .1f);
            Set(ladder, "surfaceSearchDistance", 3f);
            Physics.SyncTransforms();
            Check(HandLadderGenerator.TrySnapToSurface(ladder) && ladder.IsCurved,
                "Ladder em esfera deve acompanhar a curva.");
            Check(ladder.GetNormal(0f).y < -.3f && ladder.GetNormal(4f).y > .3f,
                $"A normal deve inclinar com a esfera: {ladder.GetNormal(0f)} / {ladder.GetNormal(4f)}");
            Vector3 gripAtBase = ladder.GetGripPosition(new HandLadderGrip { side = 1, progress = 0f });
            float fromCenter = Vector3.Distance(gripAtBase, wall.transform.position);
            Check(Mathf.Abs(fromCenter - (5f + ladder.SurfaceOffset + ladder.PlayerForwardOffset)) < .1f,
                "O jogador deve ficar a offset da superficie curva: " + fromCenter);
            for (int sample = 0; sample < ladder.Spine.Count; sample++)
            {
                float progress = ladder.Spine.progress[sample];
                float actualWidth = Vector3.Distance(
                    ladder.transform.TransformPoint(ladder.LocalLanePoint(-1, progress)),
                    ladder.transform.TransformPoint(ladder.LocalLanePoint(1, progress)));
                Check(Mathf.Abs(actualWidth - ladder.Width) < .12f,
                    $"A largura deve permanecer constante na esfera: {actualWidth} em {progress} m.");
            }
            // Tubo inclinado e largo (pouca curvatura): as duas pontas continuam na superficie.
            Object.DestroyImmediate(wall);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wall.transform.position = new Vector3(0f, 3f, -8.4f);
            wall.transform.rotation = Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(14f, 0f, 0f);
            wall.transform.localScale = new Vector3(12f, 20f, 12f);
            ladderObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -.5f), Quaternion.identity);
            Set(ladder, "height", 6f);
            Set(ladder, "surfaceSearchDistance", 2f);
            Physics.SyncTransforms();
            HandLadderGenerator.TrySnapToSurface(ladder);
            Check(ladder.IsCurved, "Tubo inclinado deve gerar curvatura.");
            Vector3 tubeAxis = wall.transform.up;
            float worstGap = 0f;
            for (int sample = 0; sample <= 12; sample++)
            {
                float along = ladder.Height * sample / 12f;
                Vector3 center = ladder.GetGripPosition(new HandLadderGrip { side = 1, progress = along }) -
                    ladder.GetNormal(along) * ladder.PlayerForwardOffset;
                Vector3 fromAxis = Vector3.ProjectOnPlane(center - wall.transform.position, tubeAxis);
                worstGap = Mathf.Max(worstGap,
                    Mathf.Abs(fromAxis.magnitude - 6f - ladder.SurfaceOffset));
            }

            Check(worstGap < .06f, "Escada no tubo deve colar em toda a altura; pior folga: " + worstGap);
            for (int sample = 0; sample < ladder.Spine.Count; sample++)
            {
                float progress = ladder.Spine.progress[sample];
                float actualWidth = Vector3.Distance(
                    ladder.transform.TransformPoint(ladder.LocalLanePoint(-1, progress)),
                    ladder.transform.TransformPoint(ladder.LocalLanePoint(1, progress)));
                Check(Mathf.Abs(actualWidth - ladder.Width) < .12f,
                    $"A largura deve permanecer constante no tubo: {actualWidth} em {progress} m.");
            }
            // Uma parede reta nao gera espinha.
            Object.DestroyImmediate(wall);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1f, -2f);
            wall.transform.localScale = new Vector3(6f, 8f, 1f);
            ladderObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -1f), Quaternion.identity);
            Physics.SyncTransforms();
            Check(HandLadderGenerator.TrySnapToSurface(ladder) && !ladder.IsCurved,
                "Parede reta nao deve gerar espinha.");

            Debug.Log("PASS: Hand ladder regression.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("FAIL: Hand ladder regression: " + exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
        finally
        {
            if (ladderObject != null) Object.DestroyImmediate(ladderObject);
            if (wall != null) Object.DestroyImmediate(wall);
            if (ledge != null) Object.DestroyImmediate(ledge);
            if (sourceStep != null) Object.DestroyImmediate(sourceStep);
            if (sourceMesh != null) Object.DestroyImmediate(sourceMesh);
            if (profile != null) Object.DestroyImmediate(profile);
        }
    }
}

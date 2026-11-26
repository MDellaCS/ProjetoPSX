using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerAttackDefinition))]
public sealed class PlayerAttackDefinitionEditor : Editor
{
    private const string PreviewSpeedPreference =
        "ProjetoPSX.PlayerCombat.AnimatePreviewSpeed";
    private PlayerAttackPreview previewTarget;
    private float previewTime;
    private float previewSpeed = 1f;
    private bool animatePreview = true;
    private double lastEditorTime;
    private bool wasAnimatingPreview;
    private int curveSourceTrackIndex;
    private int curveDestinationTrackIndex = 1;

    private PlayerAttackDefinition Attack => (PlayerAttackDefinition)target;

    private void OnEnable()
    {
        ResolvePreviewTarget();
        previewSpeed = EditorPrefs.GetFloat(PreviewSpeedPreference, 1f);
        lastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update -= UpdateAnimatedPreview;
        EditorApplication.update += UpdateAnimatedPreview;
        UpdatePreview();
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateAnimatedPreview;
        PlayerAttackPreview.ClearEditorPreview(Attack);
        SceneView.RepaintAll();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Preview Player", previewTarget, typeof(PlayerAttackPreview), true);

        if (previewTarget == null)
        {
            EditorGUILayout.HelpBox("Nao encontrei PlayerAttackPreview na cena aberta. Selecione o Player e execute Tools > Projeto PSX > Player Combat > Configure Selected Player.", MessageType.Warning);
            if (GUILayout.Button("Procurar novamente")) ResolvePreviewTarget();
        }
        else
        {
            animatePreview = EditorGUILayout.Toggle("Animate Preview", animatePreview);
            EditorGUI.BeginChangeCheck();
            previewSpeed = EditorGUILayout.Slider(
                new GUIContent("Animate Preview Speed",
                    "Multiplicador da velocidade da animacao do preview. Nao altera o timing real do ataque."),
                previewSpeed, .05f, 5f);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetFloat(PreviewSpeedPreference, previewSpeed);
            EditorGUI.BeginChangeCheck();
            float duration = GetPreviewDuration();
            EditorGUILayout.LabelField("Preview Cycle Duration", $"{duration:0.###} s");
            previewTime = EditorGUILayout.Slider(
                new GUIContent("Preview Time", "Tempo atual dentro de Windup + Active + Recovery."),
                Mathf.Clamp(previewTime, 0f, duration), 0f, duration);
            if (EditorGUI.EndChangeCheck()) UpdatePreview();
        }

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        AttackDefinitionInspector.Draw(serializedObject, true);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            UpdatePreview();
        }

        DrawOrbitCurveTools();
    }

    private void ResolvePreviewTarget()
    {
        previewTarget = Object.FindAnyObjectByType<PlayerAttackPreview>(FindObjectsInactive.Include);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (previewTarget != null)
            PlayerAttackPreview.SetEditorPreview(previewTarget, Attack, previewTime);
        SceneView.RepaintAll();
    }

    private void UpdateAnimatedPreview()
    {
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastEditorTime);
        lastEditorTime = now;
        if (!animatePreview || previewTarget == null)
        {
            wasAnimatingPreview = false;
            return;
        }

        if (!wasAnimatingPreview)
        {
            wasAnimatingPreview = true;
            return;
        }

        previewTime = Mathf.Repeat(previewTime + delta * previewSpeed,
            GetPreviewDuration());
        UpdatePreview();
        Repaint();
    }

    private float GetPreviewDuration() => Mathf.Max(.001f,
        Attack.windupDuration + Attack.activeDuration + Attack.recoveryDuration);

    private void DrawOrbitCurveTools()
    {
        if (Attack.hitboxes == null || Attack.hitboxes.Count == 0)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ferramentas de Curvas Orbitais", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Espelho temporal usa C2(t) = 1 - C1(1 - t). A segunda metade mantem " +
            "a curva de 0 a 1, mas inverte a aceleracao: onde a primeira acelera, a segunda desacelera.",
            MessageType.Info);

        if (Attack.hitboxes.Count < 2)
        {
            EditorGUILayout.HelpBox("Este ataque precisa de ao menos duas Hitbox Tracks para copiar uma curva para outra.",
                MessageType.None);
            return;
        }

        string[] trackNames = GetTrackNames(Attack.hitboxes, Attack.attackId);
        curveSourceTrackIndex = EditorGUILayout.Popup("Track de origem",
            Mathf.Clamp(curveSourceTrackIndex, 0, trackNames.Length - 1), trackNames);
        curveDestinationTrackIndex = EditorGUILayout.Popup("Track de destino",
            Mathf.Clamp(curveDestinationTrackIndex, 0, trackNames.Length - 1), trackNames);

        using (new EditorGUI.DisabledScope(curveSourceTrackIndex == curveDestinationTrackIndex))
        {
            if (GUILayout.Button("Aplicar curvas opostas (espelho temporal)"))
            {
                PlayerHitboxTrackDefinition source = Attack.hitboxes[curveSourceTrackIndex];
                PlayerHitboxTrackDefinition destination = Attack.hitboxes[curveDestinationTrackIndex];
                Undo.RecordObject(Attack, "Espelhar curvas orbitais");
                destination.orbitAngleCurve = MirrorTemporal(source.orbitAngleCurve);
                destination.orbitDistanceCurve = MirrorTemporal(source.orbitDistanceCurve);
                destination.orbitRadiusCurve = MirrorTemporal(source.orbitRadiusCurve);
                EditorUtility.SetDirty(Attack);
                UpdatePreview();
            }
        }
    }

    private static string[] GetTrackNames(System.Collections.Generic.List<PlayerHitboxTrackDefinition> hitboxes, string attackId)
    {
        string[] names = new string[hitboxes.Count];
        for (int index = 0; index < names.Length; index++)
            names[index] = $"{attackId} - Hitbox {index}";
        return names;
    }

    private static AnimationCurve MirrorTemporal(AnimationCurve source)
    {
        if (source == null || source.length == 0)
            return AnimationCurve.Linear(0f, 0f, 1f, 1f);

        Keyframe[] sourceKeys = source.keys;
        Keyframe[] mirroredKeys = new Keyframe[sourceKeys.Length];
        for (int index = 0; index < sourceKeys.Length; index++)
        {
            Keyframe key = sourceKeys[sourceKeys.Length - 1 - index];
            Keyframe mirrored = new(1f - key.time, 1f - key.value,
                key.outTangent, key.inTangent, key.outWeight, key.inWeight)
            {
                weightedMode = SwapWeightedMode(key.weightedMode)
            };
            mirroredKeys[index] = mirrored;
        }

        AnimationCurve result = new(mirroredKeys)
        {
            preWrapMode = source.preWrapMode,
            postWrapMode = source.postWrapMode
        };
        return result;
    }

    private static WeightedMode SwapWeightedMode(WeightedMode mode)
    {
        bool hasIn = (mode & WeightedMode.In) != 0;
        bool hasOut = (mode & WeightedMode.Out) != 0;
        return (hasIn ? WeightedMode.Out : WeightedMode.None) |
               (hasOut ? WeightedMode.In : WeightedMode.None);
    }
}

internal static class AttackDefinitionInspector
{
    public static void Draw(SerializedObject serializedObject, bool isPlayer)
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyPath == "movement")
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
                DrawMovement(property, isPlayer);
                continue;
            }

            if (isPlayer && property.propertyPath == "movementProfile")
            {
                SerializedProperty allowMovement = serializedObject.FindProperty("allowMovementWhileAttacking");
                using (new EditorGUI.DisabledScope(allowMovement != null && allowMovement.boolValue))
                    EditorGUILayout.PropertyField(property, true);
                continue;
            }

            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawMovement(SerializedProperty movement, bool isPlayer)
    {
        SerializedProperty child = movement.Copy();
        SerializedProperty end = child.GetEndProperty();
        bool enterChildren = true;
        while (child.NextVisible(enterChildren) &&
            !SerializedProperty.EqualContents(child, end))
        {
            enterChildren = false;
            if (isPlayer && child.name != "forwardDistance" &&
                movement.FindPropertyRelative("forwardDistance").floatValue == 0f)
                continue;
            EditorGUILayout.PropertyField(child, true);
        }
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerAttackDefinition))]
public sealed class PlayerAttackDefinitionEditor : Editor
{
    private const string PreviewSpeedPreference =
        "ProjetoPSX.PlayerCombat.AnimatePreviewSpeed";
    private PlayerAttackPreview previewTarget;
    private float previewProgress;
    private float previewSpeed = 1f;
    private bool animatePreview;
    private double lastEditorTime;

    private PlayerAttackDefinition Attack => (PlayerAttackDefinition)target;

    private void OnEnable()
    {
        ResolvePreviewTarget();
        previewSpeed = EditorPrefs.GetFloat(PreviewSpeedPreference, 1f);
        lastEditorTime = EditorApplication.timeSinceStartup;
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
            previewProgress = EditorGUILayout.Slider("Active Progress", previewProgress, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) UpdatePreview();
            EditorGUILayout.HelpBox("O gizmo mostra somente os Strikes e Tracks ativos neste ponto da timeline.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            UpdatePreview();
        }
    }

    private void ResolvePreviewTarget()
    {
        previewTarget = Object.FindAnyObjectByType<PlayerAttackPreview>(FindObjectsInactive.Include);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (previewTarget != null)
            PlayerAttackPreview.SetEditorPreview(previewTarget, Attack, previewProgress);
        SceneView.RepaintAll();
    }

    private void UpdateAnimatedPreview()
    {
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastEditorTime);
        lastEditorTime = now;
        if (!animatePreview || previewTarget == null) return;
        previewProgress = Mathf.Repeat(
            previewProgress + delta * previewSpeed, 1f);
        UpdatePreview();
        Repaint();
    }
}

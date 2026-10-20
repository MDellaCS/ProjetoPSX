using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyAttackDefinition))]
public sealed class EnemyAttackDefinitionEditor : Editor
{
    private const string SpeedPreference =
        "ProjetoPSX.EnemyCombat.AnimatePreviewSpeed";
    private EnemyAttackPreview previewTarget;
    private float previewProgress;
    private float previewSpeed = 1f;
    private bool animatePreview;
    private double previousTime;
    private EnemyAttackDefinition Attack => (EnemyAttackDefinition)target;

    private void OnEnable()
    {
        ResolvePreviewTarget();
        previewSpeed = EditorPrefs.GetFloat(SpeedPreference, 1f);
        previousTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateAnimation;
        UpdatePreview();
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateAnimation;
        EnemyAttackPreview.ClearEditorPreview(Attack);
        SceneView.RepaintAll();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        previewTarget = (EnemyAttackPreview)EditorGUILayout.ObjectField(
            new GUIContent("Preview Enemy",
                "Inimigo usado como origem e orientacao do preview."),
            previewTarget, typeof(EnemyAttackPreview), true);
        if (EditorGUI.EndChangeCheck()) UpdatePreview();
        if (previewTarget == null)
        {
            EditorGUILayout.HelpBox("Nao encontrei EnemyAttackPreview na cena. Selecione um inimigo e use Tools > Projeto PSX > Enemy Combat > Configure Selected Enemy.", MessageType.Warning);
            if (GUILayout.Button("Procurar novamente")) ResolvePreviewTarget();
        }
        else
        {
            animatePreview = EditorGUILayout.Toggle("Animate Preview",
                animatePreview);
            EditorGUI.BeginChangeCheck();
            previewSpeed = EditorGUILayout.Slider(new GUIContent(
                "Animate Preview Speed",
                "Velocidade visual do preview; nao altera o ataque real."),
                previewSpeed, .05f, 5f);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetFloat(SpeedPreference, previewSpeed);
            EditorGUI.BeginChangeCheck();
            previewProgress = EditorGUILayout.Slider("Active Progress",
                previewProgress, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) UpdatePreview();
        }
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) UpdatePreview();
    }

    private void ResolvePreviewTarget()
    {
        if (Selection.activeGameObject != null)
            previewTarget = Selection.activeGameObject.GetComponentInParent<EnemyAttackPreview>();
        if (previewTarget == null)
            previewTarget = Object.FindAnyObjectByType<EnemyAttackPreview>(
                FindObjectsInactive.Include);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (previewTarget != null)
            EnemyAttackPreview.SetEditorPreview(previewTarget, Attack,
                previewProgress);
        SceneView.RepaintAll();
    }

    private void UpdateAnimation()
    {
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - previousTime);
        previousTime = now;
        if (!animatePreview || previewTarget == null) return;
        previewProgress = Mathf.Repeat(previewProgress + delta * previewSpeed, 1f);
        UpdatePreview();
        Repaint();
    }
}

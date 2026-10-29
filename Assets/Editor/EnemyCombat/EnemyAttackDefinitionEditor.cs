using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyAttackDefinition))]
public sealed class EnemyAttackDefinitionEditor : Editor
{
    private const string SpeedPreference =
        "ProjetoPSX.EnemyCombat.AnimatePreviewSpeed";
    private EnemyAttackPreview previewTarget;
    private float previewTime;
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
            float duration = GetPreviewDuration();
            EditorGUILayout.LabelField("Preview Cycle Duration",
                $"{duration:0.###} s");
            previewTime = EditorGUILayout.Slider(
                new GUIContent("Preview Time",
                    "Tempo atual dentro de Windup + Active + Recovery."),
                Mathf.Clamp(previewTime, 0f, duration), 0f, duration);
            if (EditorGUI.EndChangeCheck()) UpdatePreview();
            EditorGUILayout.HelpBox(
                "Em 1x, Animate Preview leva exatamente Windup + Active + Recovery. " +
                "Windup mostra um aro verde; Active mostra as hitboxes; Recovery mostra um aro laranja.",
                MessageType.Info);
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
                previewTime);
        SceneView.RepaintAll();
    }

    private void UpdateAnimation()
    {
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - previousTime);
        previousTime = now;
        if (!animatePreview || previewTarget == null) return;
        previewTime = Mathf.Repeat(previewTime + delta * previewSpeed,
            GetPreviewDuration());
        UpdatePreview();
        Repaint();
    }

    private float GetPreviewDuration() => Mathf.Max(.001f,
        Attack.windupDuration + Attack.activeDuration + Attack.recoveryDuration);
}

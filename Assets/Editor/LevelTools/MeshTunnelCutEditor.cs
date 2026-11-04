using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshTunnelCut))]
public sealed class MeshTunnelCutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MeshTunnelCut tool = (MeshTunnelCut)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1) Selecione as tuneladoras e, por ultimo (Ctrl+clique), este objeto; clique em 'Adicionar Selecionados'. Tambem da para arrastar para a lista.\n" +
            "2) Clique em 'Aplicar Corte'. O objeto original e preservado; o resultado aparece como um objeto irmao '(Cut)'.\n" +
            "As tuneladoras precisam ser malhas FECHADAS (cano com as duas pontas tampadas).",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Adicionar Selecionados"))
                AddSelectedAsCutters(tool);
            if (GUILayout.Button("Limpar Lista"))
            {
                Undo.RecordObject(tool, "Limpar tuneladoras");
                tool.Cutters.Clear();
                EditorUtility.SetDirty(tool);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(.6f, 1f, .6f);
            if (GUILayout.Button("Aplicar Corte", GUILayout.Height(28)))
                MeshTunnelCutBuilder.Apply(tool);
            GUI.backgroundColor = Color.white;

            using (new EditorGUI.DisabledScope(tool.ResultObject == null))
            {
                if (GUILayout.Button("Restaurar Original", GUILayout.Height(28)))
                    MeshTunnelCutBuilder.Restore(tool);
            }
        }

        if (tool.ResultObject != null)
        {
            EditorGUILayout.ObjectField("Resultado", tool.ResultObject,
                typeof(GameObject), true);
        }

        if (!string.IsNullOrEmpty(tool.LastReport))
            EditorGUILayout.HelpBox(tool.LastReport,
                tool.LastReport.Contains("AVISO") ? MessageType.Warning : MessageType.None);
    }

    private static void AddSelectedAsCutters(MeshTunnelCut tool)
    {
        Undo.RecordObject(tool, "Adicionar tuneladoras");
        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == tool.gameObject)
                continue;
            foreach (MeshFilter filter in selected.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null && !tool.Cutters.Contains(filter) &&
                    filter.gameObject != tool.gameObject)
                    tool.Cutters.Add(filter);
            }
        }

        EditorUtility.SetDirty(tool);
    }
}

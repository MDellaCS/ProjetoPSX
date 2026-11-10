using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(KeyItemDefinition))]
public sealed class KeyItemDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (!GUILayout.Button("Regenerar ID"))
            return;

        KeyItemDefinition definition = (KeyItemDefinition)target;
        if (!EditorUtility.DisplayDialog("Regenerar ID do item",
                "Um novo ID pode desvincular este item de saves existentes. Deseja continuar?",
                "Regenerar", "Cancelar"))
            return;

        Undo.RecordObject(definition, "Regenerar ID do item-chave");
        SerializedProperty id = serializedObject.FindProperty("id");
        id.stringValue = System.Guid.NewGuid().ToString("N");
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(definition);
    }
}

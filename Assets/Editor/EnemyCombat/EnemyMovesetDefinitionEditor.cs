using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyMovesetDefinition))]
public sealed class EnemyMovesetDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Graph Validation", EditorStyles.boldLabel);
        List<string> issues = ValidateGraph((EnemyMovesetDefinition)target);
        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("Grafo valido: todos os IDs e destinos foram encontrados.", MessageType.Info);
            return;
        }
        foreach (string issue in issues)
            EditorGUILayout.HelpBox(issue, MessageType.Warning);
    }

    private static List<string> ValidateGraph(EnemyMovesetDefinition moveset)
    {
        List<string> issues = new();
        HashSet<string> ids = new();
        foreach (EnemyMoveNode node in moveset.nodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
            {
                issues.Add("Existe um Node sem Node Id.");
                continue;
            }
            if (!ids.Add(node.nodeId))
                issues.Add("Node Id duplicado: " + node.nodeId);
            if (node.attack == null)
                issues.Add("Node sem Attack: " + node.nodeId);
        }
        foreach (EnemyMovesetOpening opening in moveset.openings)
            if (opening == null || !ids.Contains(opening.targetNodeId))
                issues.Add("Opening aponta para Node inexistente: " +
                    (opening?.targetNodeId ?? "<vazio>"));
        foreach (EnemyMoveNode node in moveset.nodes)
        {
            if (node?.transitions == null) continue;
            foreach (EnemyMoveTransition transition in node.transitions)
                if (transition == null || !ids.Contains(transition.targetNodeId))
                    issues.Add("Transition de " + node.nodeId +
                        " aponta para Node inexistente: " +
                        (transition?.targetNodeId ?? "<vazio>"));
        }
        return issues;
    }
}

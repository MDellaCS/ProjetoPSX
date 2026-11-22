using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class AudioClipFolderEditor
{
    private static readonly GUIContent FolderButton = new("Adicionar clipes de pasta...",
        "Adiciona os clipes da pasta e suas subpastas, preenchendo espaços vazios e preservando os clipes já configurados.");

    public static void DrawButton(Rect position, SerializedProperty clips)
    {
        if (!GUI.Button(position, FolderButton)) return;
        UnityEngine.Object target = clips.serializedObject.targetObject;
        string propertyPath = clips.propertyPath;
        EditorApplication.delayCall += () => SelectFolder(target, propertyPath);
    }

    private static void SelectFolder(UnityEngine.Object target, string propertyPath)
    {
        if (target == null) return;
        string folder = EditorUtility.OpenFolderPanel("Selecionar pasta de áudio", Application.dataPath, "");
        if (string.IsNullOrEmpty(folder)) return;
        string assetPath = FileUtil.GetProjectRelativePath(folder);
        if (string.IsNullOrEmpty(assetPath) || !AssetDatabase.IsValidFolder(assetPath))
        {
            EditorUtility.DisplayDialog("Pasta inválida", "Selecione uma pasta dentro de Assets.", "OK");
            return;
        }
        if (target == null) return;
        SerializedObject serialized = new(target);
        serialized.Update();
        SerializedProperty clips = serialized.FindProperty(propertyPath);
        if (clips != null && clips.isArray && AddFolder(clips, assetPath))
            serialized.ApplyModifiedProperties();
    }

    internal static bool AddFolder(SerializedProperty clips, string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
        var paths = new List<string>(guids.Length);
        foreach (string guid in guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        paths.Sort(CompareNatural);

        var existing = new HashSet<AudioClip>();
        for (int i = 0; i < clips.arraySize; i++)
            if (clips.GetArrayElementAtIndex(i).objectReferenceValue is AudioClip clip)
                existing.Add(clip);

        int nextEmpty = 0;
        bool changed = false;
        foreach (string path in paths)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is not AudioClip clip || !existing.Add(clip)) continue;
                while (nextEmpty < clips.arraySize && clips.GetArrayElementAtIndex(nextEmpty).objectReferenceValue != null)
                    nextEmpty++;
                if (nextEmpty == clips.arraySize) clips.arraySize++;
                clips.GetArrayElementAtIndex(nextEmpty++).objectReferenceValue = clip;
                changed = true;
            }
        }
        return changed;
    }

    // Ordena 2 antes de 10, para listas em que a posição tem significado.
    private static int CompareNatural(string left, string right)
    {
        int i = 0, j = 0;
        while (i < left.Length && j < right.Length)
        {
            if (char.IsDigit(left[i]) && char.IsDigit(right[j]))
            {
                int a = i, b = j;
                while (a < left.Length && left[a] == '0') a++;
                while (b < right.Length && right[b] == '0') b++;
                int endA = a, endB = b;
                while (endA < left.Length && char.IsDigit(left[endA])) endA++;
                while (endB < right.Length && char.IsDigit(right[endB])) endB++;
                int length = (endA - a).CompareTo(endB - b);
                if (length != 0) return length;
                int digits = string.CompareOrdinal(left, a, right, b, endA - a);
                if (digits != 0) return digits;
                i = endA; j = endB;
                continue;
            }
            int comparison = char.ToUpperInvariant(left[i]).CompareTo(char.ToUpperInvariant(right[j]));
            if (comparison != 0) return comparison;
            i++; j++;
        }
        int remaining = (left.Length - i).CompareTo(right.Length - j);
        return remaining != 0 ? remaining : string.CompareOrdinal(left, right);
    }
}

[CustomPropertyDrawer(typeof(AudioCue))]
internal sealed class AudioCueFolderDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            SerializedProperty child = property.Copy();
            SerializedProperty end = child.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
                line.height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(line, child, true);
                if (child.name == "clips")
                {
                    line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
                    line.height = EditorGUIUtility.singleLineHeight;
                    AudioClipFolderEditor.DrawButton(EditorGUI.IndentedRect(line), child);
                }
            }
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded) return height;
        SerializedProperty child = property.Copy();
        SerializedProperty end = child.GetEndProperty();
        bool enterChildren = true;
        while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
        {
            enterChildren = false;
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
            if (child.name == "clips")
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
        }
        return height;
    }
}

[CustomEditor(typeof(AudioModuleSettings), true)]
internal sealed class AudioModuleSettingsFolderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                EditorGUILayout.PropertyField(property, true);
            if (target is SfxAudioSettings &&
                property.propertyPath == "heatRiseClips")
            {
                Rect button = EditorGUILayout.GetControlRect();
                AudioClipFolderEditor.DrawButton(EditorGUI.IndentedRect(button), property);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}

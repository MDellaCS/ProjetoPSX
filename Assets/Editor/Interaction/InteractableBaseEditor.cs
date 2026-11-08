using UnityEditor;
using UnityEngine;

public static class InteractionInspectorUtility
{
    public static void DrawProperties(Editor editor)
    {
        SerializedObject serialized = editor.serializedObject;
        serialized.UpdateIfRequiredOrScript();
        SerializedProperty property = serialized.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!ShouldShow(editor.target, serialized, property))
                continue;

            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                EditorGUILayout.PropertyField(property, true);
        }
        serialized.ApplyModifiedProperties();
    }

    private static bool ShouldShow(Object target, SerializedObject serialized,
        SerializedProperty property)
    {
        SerializedProperty activation = serialized.FindProperty("activationMode");
        bool holdSelected = IsSelected(activation,
            (int)InteractionActivationMode.Hold);
        if (property.name == "holdDuration" && !holdSelected)
            return false;

        SerializedProperty blocked = serialized.FindProperty("blockByGeometry");
        bool geometryBlockingEnabled = IsSelected(blocked, true);
        if ((property.name == "blockingLayers" || property.name == "visibilityTarget") &&
            !geometryBlockingEnabled)
            return false;
        if (target is HandLadder && property.name == "interactionPriority")
            return false;

        if (target is InteractionArea)
        {
            SerializedProperty shape = serialized.FindProperty("areaShape");
            if (shape == null || shape.hasMultipleDifferentValues)
                return true;

            InteractionAreaShape selectedShape =
                (InteractionAreaShape)shape.enumValueIndex;
            if (property.name == "areaCenter" &&
                selectedShape == InteractionAreaShape.ExistingCollider)
                return false;
            if (property.name == "boxSize" && selectedShape != InteractionAreaShape.Box)
                return false;
            if (property.name == "areaRadius" &&
                selectedShape != InteractionAreaShape.Sphere &&
                selectedShape != InteractionAreaShape.Capsule)
                return false;
            if ((property.name == "capsuleHeight" ||
                 property.name == "capsuleDirection") &&
                selectedShape != InteractionAreaShape.Capsule)
                return false;
            if (property.name == "areaFocusPoint" && !geometryBlockingEnabled)
                return false;
        }

        if (target is HandLadder)
        {
            SerializedProperty attachment = serialized.FindProperty("attachment");
            bool surface = IsSelected(attachment, (int)HandLadderAttachment.Surface);
            bool overhead = IsSelected(serialized.FindProperty("overhead"), true);
            bool conform = IsSelected(serialized.FindProperty("conformToSurface"), true);
            if ((property.name == "surfaceLayers" ||
                 property.name == "surfaceSearchDistance" ||
                 property.name == "surfaceStandoff" ||
                 property.name == "conformToSurface" ||
                 property.name == "autoSnapToSurface") && !surface)
                return false;
            if (property.name == "curveSampleSpacing" && (!surface || !conform || overhead))
                return false;
            if (property.name == "climbableFromBothSides" && (surface || overhead))
                return false;
            if (property.name == "hangDepth" && !overhead)
                return false;
            if (property.name == "entryBackDepth" && (!surface || overhead))
                return false;
            SerializedProperty exit = serialized.FindProperty("topExitSide");
            if (property.name == "ledgeSearchDepth" && exit != null &&
                !exit.hasMultipleDifferentValues)
            {
                HandLadderExitSide selectedExit =
                    (HandLadderExitSide)exit.enumValueIndex;
                if (selectedExit == HandLadderExitSide.Front ||
                    selectedExit == HandLadderExitSide.Auto && !surface)
                    return false;
            }
        }
        return true;
    }

    private static bool IsSelected(SerializedProperty property, int value) =>
        property != null && (property.hasMultipleDifferentValues ||
            property.enumValueIndex == value);

    private static bool IsSelected(SerializedProperty property, bool value) =>
        property != null && (property.hasMultipleDifferentValues ||
            property.boolValue == value);
}

[CustomEditor(typeof(InteractableBase), true)]
[CanEditMultipleObjects]
public sealed class InteractableBaseEditor : Editor
{
    public override void OnInspectorGUI() =>
        InteractionInspectorUtility.DrawProperties(this);
}

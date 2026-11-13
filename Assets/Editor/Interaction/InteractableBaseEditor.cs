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
            if (editor.target is InteractionArea area && property.name == "onInteract" &&
                area.HasInteractionHandler)
                continue;
            if (editor.target is HandLadder && !ShouldShowHandLadderProperty(serialized, property))
                continue;
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                EditorGUILayout.PropertyField(property, true);
        }
        serialized.ApplyModifiedProperties();
    }

    private static bool ShouldShowHandLadderProperty(SerializedObject serialized,
        SerializedProperty property)
    {
        SerializedProperty attachment = serialized.FindProperty("attachment");
        bool surface = attachment != null &&
            attachment.enumValueIndex == (int)HandLadderAttachment.Surface;
        bool overhead = serialized.FindProperty("overhead")?.boolValue ?? false;
        bool conform = serialized.FindProperty("conformToSurface")?.boolValue ?? false;
        if (property.name == "allowGrapple" &&
            serialized.FindProperty("isClimbable")?.boolValue == false)
            return false;
        if ((property.name == "surfaceLayers" || property.name == "surfaceSearchDistance" ||
             property.name == "surfaceStandoff" || property.name == "conformToSurface" ||
             property.name == "autoSnapToSurface") && !surface)
            return false;
        if (property.name == "curveSampleSpacing" && (!surface || !conform || overhead))
            return false;
        if (property.name == "climbableFromBothSides" && (surface || overhead))
            return false;
        if (property.name == "hangDepth" && !overhead)
            return false;
        if (property.name == "ledgeSearchDepth")
        {
            SerializedProperty exit = serialized.FindProperty("topExitSide");
            if (exit != null && !exit.hasMultipleDifferentValues)
            {
                HandLadderExitSide side = (HandLadderExitSide)exit.enumValueIndex;
                if (side == HandLadderExitSide.Front ||
                    side == HandLadderExitSide.Auto && !surface)
                    return false;
            }
        }
        return true;
    }
}

[CustomEditor(typeof(InteractionProfile))]
public sealed class InteractionProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();
        InteractionActivationMode activation =
            (InteractionActivationMode)serializedObject.FindProperty("activationMode").enumValueIndex;
        bool blocksGeometry = serializedObject.FindProperty("blockByGeometry").boolValue;
        InteractionAreaShape shape =
            (InteractionAreaShape)serializedObject.FindProperty("areaShape").enumValueIndex;

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if ((property.name == "holdDuration" && activation != InteractionActivationMode.Hold) ||
                (property.name == "blockingLayers" && !blocksGeometry) ||
                (property.name == "boxSize" && shape != InteractionAreaShape.Box) ||
                (property.name == "areaRadius" && shape != InteractionAreaShape.Sphere &&
                    shape != InteractionAreaShape.Capsule) ||
                (property.name == "areaCenter" && shape == InteractionAreaShape.LadderEntry) ||
                ((property.name == "capsuleHeight" || property.name == "capsuleDirection") &&
                    shape != InteractionAreaShape.Capsule) ||
                (property.name.StartsWith("ladderEntry") &&
                    shape != InteractionAreaShape.LadderEntry))
                continue;

            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(InteractableBase), true)]
[CanEditMultipleObjects]
public sealed class InteractableBaseEditor : Editor
{
    public override void OnInspectorGUI() =>
        InteractionInspectorUtility.DrawProperties(this);
}

[CustomEditor(typeof(InteractionArea), true)]
[CanEditMultipleObjects]
public sealed class InteractionAreaEditor : Editor
{
    public override void OnInspectorGUI() =>
        InteractionInspectorUtility.DrawProperties(this);
}


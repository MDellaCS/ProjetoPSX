using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PlayerAttackStrikeDefinition))]
public sealed class PlayerAttackStrikeDefinitionDrawer : EditorPropertyDrawerBase
{
    protected override string GetFoldoutTitle(SerializedProperty property,
        GUIContent label)
    {
        SerializedProperty name = property.FindPropertyRelative("strikeName");
        return string.IsNullOrWhiteSpace(name.stringValue)
            ? label.text : name.stringValue;
    }

    protected override IEnumerable<string> GetVisibleProperties(
        SerializedProperty property)
    {
        yield return "strikeName";
        yield return "activeStartNormalized";
        yield return "activeEndNormalized";
        yield return "damageMultiplier";
        yield return "poiseMultiplier";
        yield return "damageFalloff";
        yield return "generatesHeatOnHit";
        yield return "effects";
        yield return "hitboxes";
    }
}

[CustomPropertyDrawer(typeof(PlayerHitboxTrackDefinition))]
public sealed class PlayerHitboxTrackDefinitionDrawer : EditorPropertyDrawerBase
{
    protected override string GetFoldoutTitle(SerializedProperty property,
        GUIContent label)
    {
        SerializedProperty name = property.FindPropertyRelative("trackName");
        SerializedProperty shape = property.FindPropertyRelative("shape");
        string shapeName = shape.hasMultipleDifferentValues
            ? "Varia" : shape.enumDisplayNames[shape.enumValueIndex];
        return $"{(string.IsNullOrWhiteSpace(name.stringValue) ? label.text : name.stringValue)} ({shapeName})";
    }

    protected override IEnumerable<string> GetVisibleProperties(
        SerializedProperty property)
    {
        SerializedProperty anchorMode = property.FindPropertyRelative("anchorMode");
        SerializedProperty trajectoryMode = property.FindPropertyRelative("trajectoryMode");
        SerializedProperty shape = property.FindPropertyRelative("shape");
        bool followsAnchors = anchorMode.enumValueIndex ==
            (int)PlayerHitboxAnchorMode.FollowAnchors;
        bool orbit = !followsAnchors && trajectoryMode.enumValueIndex ==
            (int)PlayerHitboxTrajectoryMode.Orbit;

        yield return "trackName";
        yield return "shape";
        yield return "anchorMode";

        if (followsAnchors)
        {
            yield return "primaryAnchorId";
            if (shape.enumValueIndex == (int)PlayerHitboxShape.Capsule)
                yield return "secondaryAnchorId";
        }
        else
        {
            yield return "trajectoryMode";
            if (!orbit)
            {
                yield return "startLocalPosition";
                yield return "endLocalPosition";
            }
            yield return "startLocalEuler";
            yield return "endLocalEuler";

            if (orbit)
            {
                yield return "orbitStartAngleDegrees";
                yield return "orbitEndAngleDegrees";
                yield return "orbitStartDistance";
                yield return "orbitEndDistance";
                yield return "orbitHeight";
                yield return "orbitAngleCurve";
                yield return "orbitDistanceCurve";
                if (shape.enumValueIndex != (int)PlayerHitboxShape.Box)
                {
                    yield return "orbitStartRadius";
                    yield return "orbitEndRadius";
                    yield return "orbitRadiusCurve";
                }
            }
        }

        if (shape.enumValueIndex != (int)PlayerHitboxShape.Box)
            yield return "radius";
        if (shape.enumValueIndex == (int)PlayerHitboxShape.Capsule)
        {
            yield return "capsuleHeight";
            yield return "capsuleAxis";
        }
        else if (shape.enumValueIndex == (int)PlayerHitboxShape.Box)
            yield return "boxSize";

        yield return "activeStartNormalized";
        yield return "activeEndNormalized";
        yield return "sweep";
        if (property.FindPropertyRelative("sweep").boolValue)
            yield return "sweepStepDistance";
        yield return "hitGroup";
        yield return "allowRepeatHits";
        if (property.FindPropertyRelative("allowRepeatHits").boolValue)
            yield return "repeatHitInterval";
    }
}

[CustomPropertyDrawer(typeof(PlayerAttackMovementDefinition))]
public sealed class PlayerAttackMovementDefinitionDrawer : EditorPropertyDrawerBase
{
    protected override IEnumerable<string> GetVisibleProperties(
        SerializedProperty property)
    {
        yield return "forwardDistance";
        if (property.FindPropertyRelative("forwardDistance").floatValue <= 0f)
            yield break;

        yield return "startNormalized";
        yield return "endNormalized";
        yield return "movementCurve";
    }
}

[CustomPropertyDrawer(typeof(PlayerAttackDamageFalloffDefinition))]
public sealed class PlayerAttackDamageFalloffDefinitionDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
        GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.FindPropertyRelative("enabled").boolValue)
            return height;

        foreach (string field in new[] { "startDistance", "endDistance", "damageMultiplier" })
            height += EditorGUIUtility.standardVerticalSpacing +
                EditorGUI.GetPropertyHeight(property.FindPropertyRelative(field), true);
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        float y = position.y;
        float line = EditorGUIUtility.singleLineHeight;
        SerializedProperty enabled = property.FindPropertyRelative("enabled");
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, line),
            enabled, label);
        y += line;

        if (enabled.boolValue)
        {
            DrawField(property, "startDistance", position, ref y);
            DrawField(property, "endDistance", position, ref y);
            DrawField(property, "damageMultiplier", position, ref y);
        }
        EditorGUI.EndProperty();
    }

    private static void DrawField(SerializedProperty parent, string name,
        Rect position, ref float y)
    {
        SerializedProperty child = parent.FindPropertyRelative(name);
        float height = EditorGUI.GetPropertyHeight(child, true);
        y += EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, height),
            child, true);
        y += height;
    }
}

public abstract class EditorPropertyDrawerBase : PropertyDrawer
{
    protected abstract IEnumerable<string> GetVisibleProperties(
        SerializedProperty property);

    protected virtual string GetFoldoutTitle(SerializedProperty property,
        GUIContent label) => label.text;

    public override float GetPropertyHeight(SerializedProperty property,
        GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
            return height;

        foreach (string field in GetVisibleProperties(property))
        {
            SerializedProperty child = property.FindPropertyRelative(field);
            height += EditorGUIUtility.standardVerticalSpacing +
                EditorGUI.GetPropertyHeight(child, true);
        }
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Rect header = new(position.x, position.y, position.width,
            EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(header, property.isExpanded,
            GetFoldoutTitle(property, label), true);
        if (property.isExpanded)
        {
            float y = header.yMax + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.indentLevel++;
            foreach (string field in GetVisibleProperties(property))
            {
                SerializedProperty child = property.FindPropertyRelative(field);
                float height = EditorGUI.GetPropertyHeight(child, true);
                Rect childRect = new(position.x, y, position.width, height);
                EditorGUI.PropertyField(childRect, child, true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}

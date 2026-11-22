using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerMovesetDefinition))]
public sealed class PlayerMovesetDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawField("displayName");
        DrawField("resourceMode");
        DrawField("slideAttack");
        if (serializedObject.FindProperty("resourceMode").enumValueIndex == (int)PlayerResourceMode.Heat)
            DrawField("pityMechanic", true);

        MovesetUsage usage = GetUsage();
        if (usage.hasTransitions)
            DrawField("globalPauseDelay");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movement Abilities", EditorStyles.boldLabel);
        DrawField("movementSpeedMultiplier");
        DrawField("allowJump");
        if (serializedObject.FindProperty("allowJump").boolValue)
        {
            DrawField("allowAirJump");
            DrawField("jumpHeightMultiplier");
            DrawField("jumpDurationMultiplier");
        }

        DrawField("allowDash");
        if (serializedObject.FindProperty("allowDash").boolValue)
        {
            DrawField("allowAirDash");
            DrawField("dashImpulse");
            DrawField("dashCooldown");
        }

        if (usage.hasLightAttacks || usage.hasHeavyAttacks)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Attack Rotation", EditorStyles.boldLabel);
            if (usage.hasLightAttacks)
                DrawField("lightWindupActiveRotationSpeed");
            if (usage.hasHeavyAttacks)
                DrawField("heavyWindupActiveRotationSpeed");
        }

        if (usage.hasHeldTransitions)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Held Attack Branches", EditorStyles.boldLabel);
            DrawField("heldAttackBranchThreshold");
        }

        if (usage.hasLightTransitions || usage.hasHeavyTransitions)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Transition Timing", EditorStyles.boldLabel);
            if (usage.hasLightTransitions)
                DrawField("lightTransitionTiming");
            if (usage.hasHeavyTransitions)
                DrawField("heavyTransitionTiming");
        }

        if (usage.hasExtendedArmAttacks)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Hitbox Extension", EditorStyles.boldLabel);
            DrawField("extendedArmHitboxRadius");
            DrawField("extendedArmHitboxHeightOffset");
        }

        if (usage.hasLightAttacks || usage.hasHeavyAttacks)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Cancel Rules", EditorStyles.boldLabel);
            if (usage.hasLightAttacks)
                DrawField("lightCancelRules");
            if (usage.hasHeavyAttacks)
                DrawField("heavyCancelRules");
        }

        EditorGUILayout.Space();
        DrawField("entries", true);
        DrawField("nodes", true);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawField(string propertyName, bool includeChildren = false)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, includeChildren);
    }

    private MovesetUsage GetUsage()
    {
        MovesetUsage usage = new();
        SerializedProperty nodes = serializedObject.FindProperty("nodes");
        if (nodes == null || !nodes.isArray)
            return usage;

        for (int i = 0; i < nodes.arraySize; i++)
        {
            SerializedProperty node = nodes.GetArrayElementAtIndex(i);
            SerializedProperty attackProperty = node.FindPropertyRelative("attack");
            PlayerAttackDefinition attack = attackProperty != null
                ? attackProperty.objectReferenceValue as PlayerAttackDefinition
                : null;
            if (attack != null)
            {
                usage.hasAttacks = true;
                bool heavy = attack.kind == PlayerAttackKind.Heavy;
                usage.hasHeavyAttacks |= heavy;
                usage.hasLightAttacks |= !heavy;
                usage.hasExtendedArmAttacks |= attack.usesExtendedArmHitbox;
            }

            SerializedProperty transitions = node.FindPropertyRelative("transitions");
            if (transitions == null || !transitions.isArray ||
                transitions.arraySize == 0)
                continue;

            usage.hasTransitions = true;
            if (attack != null)
            {
                if (attack.kind == PlayerAttackKind.Heavy)
                    usage.hasHeavyTransitions = true;
                else
                    usage.hasLightTransitions = true;
            }

            for (int transitionIndex = 0;
                transitionIndex < transitions.arraySize; transitionIndex++)
            {
                SerializedProperty transition =
                    transitions.GetArrayElementAtIndex(transitionIndex);
                SerializedProperty held =
                    transition.FindPropertyRelative("requiresHeldInput");
                if (held != null && held.boolValue)
                    usage.hasHeldTransitions = true;
            }
        }

        return usage;
    }

    private struct MovesetUsage
    {
        public bool hasAttacks;
        public bool hasLightAttacks;
        public bool hasHeavyAttacks;
        public bool hasExtendedArmAttacks;
        public bool hasTransitions;
        public bool hasLightTransitions;
        public bool hasHeavyTransitions;
        public bool hasHeldTransitions;
    }
}

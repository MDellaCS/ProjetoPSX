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
        if (serializedObject.FindProperty("resourceMode").enumValueIndex == (int)PlayerResourceMode.Heat)
            DrawField("pityMechanic", true);

        MovesetUsage usage = GetUsage();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movimento", EditorStyles.boldLabel);
        DrawField("movementSpeedMultiplier");
        DrawField("slideAttack");
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

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ataques", EditorStyles.boldLabel);
        DrawField("attackMovementSpeedPercent");
        DrawField("attackMovementDistanceMultiplier");
        if (usage.hasLightAttacks)
            DrawField("lightWindupActiveRotationSpeed");
        if (usage.hasHeavyAttacks)
            DrawField("heavyWindupActiveRotationSpeed");
        if (usage.hasExtendedArmAttacks)
        {
            DrawField("extendedArmHitboxRadius");
            DrawField("extendedArmHitboxHeightOffset");
        }
        if (usage.hasLightAttacks)
            DrawField("lightCancelRules", true);
        if (usage.hasHeavyAttacks)
            DrawField("heavyCancelRules", true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sequencias", EditorStyles.boldLabel);
        if (usage.hasHeldTransitions)
            DrawField("heldAttackBranchThreshold");
        if (usage.hasLightTransitions || usage.hasHeavyTransitions)
            DrawField("comboResetDelay");
        if (usage.hasLightTransitions)
            DrawField("lightTransitionTiming", true);
        if (usage.hasHeavyTransitions)
            DrawField("heavyTransitionTiming", true);
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
        public bool hasLightTransitions;
        public bool hasHeavyTransitions;
        public bool hasHeldTransitions;
    }
}

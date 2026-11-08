using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FixedCameraZone))]
[CanEditMultipleObjects]
public sealed class FixedCameraZoneEditor : Editor
{
    private SerializedProperty cameraMode;
    private SerializedProperty railRotationMode;
    private SerializedProperty railDollyMode;
    private SerializedProperty zoneShape;

    private void OnEnable()
    {
        cameraMode = serializedObject.FindProperty("cameraMode");
        railRotationMode = serializedObject.FindProperty("railRotationMode");
        railDollyMode = serializedObject.FindProperty("railDollyMode");
        zoneShape = serializedObject.FindProperty("zoneShape");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Section("Camera");
        Draw("returnToFreeCamera");
        Draw("useCameraTransition");
        if (serializedObject.FindProperty("useCameraTransition").boolValue)
            Draw("cameraTransition");

        if (!serializedObject.FindProperty("returnToFreeCamera").boolValue)
        {
            Draw("fixedCamera");
            Draw("cameraMode");
            Draw("cameraPriority");
            Draw("activeOnlyInsideZone");

            switch ((CameraZoneMode)cameraMode.enumValueIndex)
            {
                case CameraZoneMode.FollowWithOverrides:
                    DrawFollowSettings();
                    break;
                case CameraZoneMode.StaticLookAtTarget:
                    DrawFollowDelay();
                    break;
                case CameraZoneMode.OnRails:
                    DrawRailSettings();
                    break;
            }
        }

        Section("Volume do trigger");
        Draw("activationVolume");
        Draw("zoneShape");
        if ((int)zoneShape.enumValueIndex == 0)
            Draw("areaSize");
        else
            Draw("sphereRadius");
        Draw("additionalActivationVolumes");
        if (serializedObject.FindProperty("activeOnlyInsideZone").boolValue)
            Draw("exitPaddingDistance", "Margem de saída após entrar (m)");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawFollowSettings()
    {
        Section("Acompanhamento e enquadramento");
        Draw("followPositionOffset");
        Draw("followDistance");
        Draw("followYaw");
        Draw("followPitch");
        Draw("followSmoothTime");
        Draw("followFieldOfView");
    }

    private void DrawRailSettings()
    {
        Section("Trilho e movimento da camera");
        Draw("railSpline");
        Draw("railDollyMode");
        if ((CameraRailDollyMode)railDollyMode.enumValueIndex ==
            CameraRailDollyMode.FollowTarget)
        {
            Draw("railDollyTarget");
            Draw("followDistance");
            Draw("railDistanceOffset");
            Draw("railSearchResolution");
            Draw("railSearchIterations");
            DrawFollowDelay();
        }
        else
        {
            Draw("railManualPosition");
        }

        Section("Offsets de posicao (mundo ou spline)");
        Draw("railOffset", "Deslocamento Mundial XYZ (m)");
        Draw("followPositionOffset", "Follow Position Offset XYZ (m)");

        Section("Orientacao e lente");
        Draw("railRotationMode");
        switch ((CameraRailRotationMode)railRotationMode.enumValueIndex)
        {
            case CameraRailRotationMode.LookAtPlayer:
                Draw("railLookAtTarget");
                Draw("railLookAtOffset");
                Draw("railScreenPosition");
                Draw("railDeadZoneEnabled");
                if (serializedObject.FindProperty("railDeadZoneEnabled").boolValue)
                    Draw("railDeadZoneSize");
                Draw("railHardLimitsEnabled");
                if (serializedObject.FindProperty("railHardLimitsEnabled").boolValue)
                {
                    Draw("railHardLimitsSize");
                    Draw("railHardLimitsOffset");
                }
                Draw("railAimDamping");
                Draw("railLookaheadEnabled");
                if (serializedObject.FindProperty("railLookaheadEnabled").boolValue)
                {
                    Draw("railLookaheadTime");
                    Draw("railLookaheadSmoothing");
                    Draw("railLookaheadIgnoreY");
                }
                break;
            case CameraRailRotationMode.FollowSpline:
                Draw("railWorldRotationOffset");
                Draw("railPitchFollowSpline", "Pitch acompanha a orientação do spline");
                Draw("railYawFollowSpline", "Yaw acompanha a orientação do spline");
                Draw("railRollFollowSpline", "Roll acompanha a orientação do spline");
                break;
        }
        Draw("overrideRailFieldOfView");
        if (serializedObject.FindProperty("overrideRailFieldOfView").boolValue)
            Draw("railFieldOfView");
    }

    private void DrawFollowDelay()
    {
        Draw("overrideFollowDelay");
        if (serializedObject.FindProperty("overrideFollowDelay").boolValue)
            Draw("followDelaySeconds");
    }

    private void Section(string title)
    {
        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private void Draw(string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, true);
    }

    private void Draw(string propertyName, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, new GUIContent(label,
                property.tooltip), true);
    }
}

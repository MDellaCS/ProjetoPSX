using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FixedCameraZone))]
[CanEditMultipleObjects]
public sealed class FixedCameraZoneEditor : Editor
{
    private SerializedProperty cameraMode;
    private SerializedProperty railRotationMode;
    private SerializedProperty zoneShape;

    private void OnEnable()
    {
        cameraMode = serializedObject.FindProperty("cameraMode");
        railRotationMode = serializedObject.FindProperty("railRotationMode");
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
        Draw("exitPaddingDistance");

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
        Section("Spline e posicao");
        Draw("railSpline");
        Draw("railOffset");
        Draw("followPositionOffset");
        Draw("followDistance");
        Draw("railDistanceOffset");

        Section("Orientacao e lente");
        Draw("railRotationMode");
        if ((CameraRailRotationMode)railRotationMode.enumValueIndex ==
            CameraRailRotationMode.LookAtPlayer)
            Draw("railLookAtOffset");
        Draw("overrideRailFieldOfView");
        if (serializedObject.FindProperty("overrideRailFieldOfView").boolValue)
            Draw("railFieldOfView");
        DrawFollowDelay();
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
}

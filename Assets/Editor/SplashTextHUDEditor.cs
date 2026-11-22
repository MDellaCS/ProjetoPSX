using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplashTextHUD))]
public sealed class SplashTextHUDEditor : Editor
{
    private bool requestRejected;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Reproduzir SplashText de vitória"))
            {
                requestRejected = !((SplashTextHUD)target).PlayPreview(
                    SplashTextMessage.BossDefeated);
            }
            if (GUILayout.Button("Reproduzir SplashText de morte"))
            {
                requestRejected = !((SplashTextHUD)target).PlayPreview(
                    SplashTextMessage.PlayerDied);
            }
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Entre no Play Mode para reproduzir a prévia com áudio.", MessageType.Info);
        else if (requestRejected)
            EditorGUILayout.HelpBox(
                "Uma mensagem de jogo está na fila ou o componente está desativado.",
                MessageType.Info);
    }
}

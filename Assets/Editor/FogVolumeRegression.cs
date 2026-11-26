using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FogVolumeRegression
{
    private static double started;
    [MenuItem("Tools/Projeto PSX/Test Volumetric Fog in Play Mode")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/0. Bootstrap.unity");
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.EnterPlaymode();
    }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update += Check;
    }
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup - started < 8) return;
        EditorApplication.update -= Check;
        GameObject cube = null;
        Material material = null;
        FogProfile test = null;
        RetroEnvironmentProfile env = RetroLightSystem.ActiveEnvironment;
        FogProfile original = env != null ? env.fogProfile : null;
        Camera camera = Camera.main;
        Vector3 oldPosition = camera != null ? camera.transform.position : Vector3.zero;
        Quaternion oldRotation = camera != null ? camera.transform.rotation : Quaternion.identity;
        int oldMask = camera != null ? camera.cullingMask : 0;
        CameraClearFlags oldClear = camera != null ? camera.clearFlags : default;
        Color oldBackground = camera != null ? camera.backgroundColor : default;
        try
        {
            if (env == null || original == null || camera == null) throw new System.Exception("Camera ou perfil de ambiente indisponivel.");
            test = Object.Instantiate(original);
            test.transitionTime = 0; test.colorEnabled = false; test.blurEnabled = false;
            test.volumetricEnabled = true; test.volumetricColor = Color.red;
            test.density = .2f; test.volumetricOpacity = 1; test.volumeStart = 10; test.volumeEnd = 30;
            test.volumeMin = 0; test.volumeMax = 1; test.volumeCurve = AnimationCurve.Linear(0, 0, 1, 1);
            test.limitVolumeHeight = false; test.maxDistance = 80; test.steps = 8; test.noiseStrength = 0;
            env.fogProfile = test;
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.layer = 30;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = Color.white;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.rotation = Quaternion.identity;
            float near = Sample(camera, cube, 0, 5);
            float farLow = Sample(camera, cube, 0, 35);
            float farHigh = Sample(camera, cube, 60, 35);
            test.limitVolumeHeight = true; test.baseHeight = 0; test.height = 5;
            float limitedHigh = Sample(camera, cube, 60, 35);
            Debug.Log($"FOG_VOLUME_TEST near={near:F3} farLow={farLow:F3} farHigh={farHigh:F3} limitedHigh={limitedHigh:F3}");
            if (farLow - near < .1f || farHigh - near < .1f || farHigh - limitedHigh < .1f)
                throw new System.Exception("Distancia ou limite de altura nao produziu o resultado esperado.");
            Debug.Log("FOG_VOLUME_TEST PASSED");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        finally
        {
            if (env != null) env.fogProfile = original;
            if (camera != null) { camera.transform.SetPositionAndRotation(oldPosition, oldRotation); camera.cullingMask = oldMask; camera.clearFlags = oldClear; camera.backgroundColor = oldBackground; }
            if (cube != null) Object.DestroyImmediate(cube);
            if (material != null) Object.DestroyImmediate(material);
            if (test != null) Object.DestroyImmediate(test);
        }
        if (Application.isBatchMode) EditorApplication.Exit(0);
        else EditorApplication.ExitPlaymode();
    }
    private static float Sample(Camera camera, GameObject cube, float height, float distance)
    {
        camera.transform.position = new Vector3(0, height, 0);
        cube.transform.position = new Vector3(0, height, distance);
        cube.transform.localScale = Vector3.one * (distance * .8f);
        FogRuntime.Invalidate();
        var rt = new RenderTexture(128, 128, 24);
        var old = camera.targetTexture; var active = RenderTexture.active;
        camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
        var image = new Texture2D(128, 128, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply();
        Color pixel = image.GetPixel(64, 64);
        RenderTexture.active = active; camera.targetTexture = old;
        Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
        return pixel.r - pixel.g;
    }
}

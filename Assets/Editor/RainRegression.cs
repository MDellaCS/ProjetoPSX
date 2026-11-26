using UnityEditor;
using UnityEngine;

public static class RainRegression
{
    [MenuItem("Tools/Projeto PSX/Validate Rain")]
    public static void Run()
    {
        GameObject host = null;
        try
        {
            var day = AssetDatabase.LoadAssetAtPath<RetroEnvironmentProfile>("Assets/Profiles/Lighting/Environment/Day.asset");
            var night = AssetDatabase.LoadAssetAtPath<RetroEnvironmentProfile>("Assets/Profiles/Lighting/Environment/Night.asset");
            var material = Resources.Load<Material>("Weather/Rain");
            if (day == null || night == null || day.rainProfile == null || night.rainProfile == null || material == null || material.shader == null || ShaderUtil.ShaderHasError(material.shader))
                throw new System.Exception("Perfis, material ou shader de chuva invalidos.");
            host = new GameObject("Rain Regression");
            var rain = host.AddComponent<CameraRain>();
            rain.Initialize();
            var particles = host.GetComponentInChildren<ParticleSystem>();
            if (particles == null || particles.main.simulationSpace != ParticleSystemSimulationSpace.World || particles.collision.enabled || particles.GetComponent<ParticleSystemRenderer>().renderMode != ParticleSystemRenderMode.Stretch)
                throw new System.Exception("Configuracao das particulas de chuva invalida.");
            Debug.Log("RAIN_REGRESSION: perfis, shader e configuracao de particulas validos.");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        finally { if (host != null) Object.DestroyImmediate(host); }
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}

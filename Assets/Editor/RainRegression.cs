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
            var configured = Object.Instantiate(day.rainProfile);
            try
            {
                configured.angleRange = new Vector2(20f, 40f);
                configured.directionRange = new Vector2(350f, 370f);
                configured.speed = new Vector2(10f, 20f);
                configured.transparency = .5f;
                configured.cameraSafeRadius = 2f;
                configured.cameraSafeFade = .75f;
                var type = typeof(CameraRain);
                type.GetMethod("Configure", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(rain, new object[] { configured });
                type.GetField("rate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(rain, 200f);
                particles.Play();
                type.GetMethod("EmitDrops", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(rain, new object[] { configured, Vector3.zero, 1f });
                var drops = new ParticleSystem.Particle[256];
                int count = particles.GetParticles(drops);
                if (count != 200) throw new System.Exception("Quantidade de gotas emitidas invalida: " + count);
                float minAngle = 90, maxAngle = 0, minDirection = 999, maxDirection = -999;
                for (int i = 0; i < count; i++)
                {
                    Vector3 v = drops[i].velocity;
                    float inclination = Vector3.Angle(Vector3.down, v);
                    float azimuth = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                    minAngle = Mathf.Min(minAngle, inclination); maxAngle = Mathf.Max(maxAngle, inclination);
                    minDirection = Mathf.Min(minDirection, azimuth); maxDirection = Mathf.Max(maxDirection, azimuth);
                    if (inclination < 19.99f || inclination > 40.01f || azimuth < -10.01f || azimuth > 10.01f || v.magnitude < 9.99f || v.magnitude > 20.01f)
                        throw new System.Exception("Gota fora dos intervalos configurados.");
                }
                var block = new MaterialPropertyBlock();
                particles.GetComponent<ParticleSystemRenderer>().GetPropertyBlock(block);
                if (maxAngle - minAngle < 5f || maxDirection - minDirection < 5f || Mathf.Abs(block.GetFloat("_RainSafeRadius") - 2f) > .001f || Mathf.Abs(block.GetFloat("_RainSafeFade") - .75f) > .001f || Mathf.Abs(particles.main.startColor.color.a - configured.color.a * .5f) > .001f)
                    throw new System.Exception("Variacao, transparencia ou area segura invalidas.");
                Debug.Log($"RAIN_REGRESSION: 200 gotas, angulo {minAngle:F1} a {maxAngle:F1}, direcao {minDirection:F1} a {maxDirection:F1}; transparencia e area segura validas.");
            }
            finally { Object.DestroyImmediate(configured); }
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

using System;
using UnityEditor;
using UnityEngine;

/// <summary>Valida a iluminacao retro sem abrir cenas (menu Tools/Tests).</summary>
public static class RetroLightingRegression
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    [MenuItem("Tools/Tests/Run Retro Lighting Regression")]
    public static void Run()
    {
        try
        {
            CheckAssets();
            CheckShader();
            CheckFalloff();
            CheckFlicker();
            CheckEmitterRegistration();
            CheckFlickerWithRangeMultiplier();
            CheckLightBudget();
            CheckMaterialConversion();
            CheckRenderedLight();
            Debug.Log("PASS: Retro lighting regression.");
        }
        catch (Exception exception)
        {
            Debug.LogError("FAIL: Retro lighting regression: " + exception.Message);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            throw;
        }
    }

    // Usado em linha de comando: -executeMethod RetroLightingRegression.RunBatch
    public static void RunBatch()
    {
        Run();
        EditorApplication.Exit(0);
    }

    private static void CheckAssets()
    {
        RetroLightingSettings settings = Resources.Load<RetroLightingSettings>(RetroLightingSettings.ResourcePath);
        Check(settings != null, "Falta Resources/" + RetroLightingSettings.ResourcePath);
        Check(settings.defaultEnvironment != null, "Settings precisa de um ambiente padrao.");
        Check(settings.maxVisibleLights >= 1 && settings.maxVisibleLights <= RetroLightSystem.MaxLights,
            "Orcamento de luzes fora da faixa.");

        foreach (string path in new[] { "Assets/Profiles/Lighting/MechanismGlow.asset", "Assets/Profiles/Lighting/HumanFlashlight.asset" })
        {
            LocalLightProfile profile = AssetDatabase.LoadAssetAtPath<LocalLightProfile>(path);
            Check(profile != null && profile.lights.Count > 0, "Perfil invalido: " + path);
            foreach (LocalLightDefinition light in profile.lights)
            {
                Check(light.range > 0f && light.intensity > 0f, "Luz sem alcance/intensidade em " + path);
                Check(light.flicker.minimumIntensity <= light.flicker.maximumIntensity, "Flicker min > max em " + path);
            }
        }
    }

    private static void CheckShader()
    {
        Shader shader = Shader.Find(RetroMaterialConverter.ShaderName);
        Check(shader != null, "Shader Retro Lit nao encontrado.");
        Check(!ShaderUtil.ShaderHasError(shader), "Shader Retro Lit tem erros de compilacao.");
        foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader))
            Check(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error,
                "Erro no Retro Lit: " + message.message + " (" + message.file + ":" + message.line + ")");
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Retro/RetroDefault.mat");
        Check(material != null && material.shader == shader, "RetroDefault.mat deve usar Retro Lit.");
    }

    private static void CheckFalloff()
    {
        // Sem inverso-quadrado: alcance enorme continua iluminando de forma linear.
        Check(Mathf.Approximately(RetroLightMath.Attenuation(.5f, 0f, 1f, 0), .5f), "Queda linear na metade do alcance.");
        Check(RetroLightMath.Attenuation(.99f, 0f, 1f, 0) > 0f, "A luz chega perto do alcance.");
        Check(RetroLightMath.Attenuation(1f, 0f, 1f, 0) == 0f, "A luz termina no alcance.");
        Check(Mathf.Approximately(RetroLightMath.Attenuation(.3f, .4f, 2f, 0), 1f), "Nucleo mantem brilho total.");

        for (int i = 0; i < 100; i++)
        {
            float value = RetroLightMath.Attenuation(i / 100f, .1f, 1.4f, 4);
            float scaled = value * 4f;
            Check(Mathf.Abs(scaled - Mathf.Round(scaled)) < 1e-4f, "Faixas devem ser multiplos de 1/4.");
        }
        Check(RetroLightMath.Attenuation(.97f, 0f, 1f, 4) > 0f, "Com faixas, a borda continua iluminada.");

        const int polygonSides = 8;
        float halfSector = Mathf.PI / polygonSides;
        Check(Mathf.Abs(RetroLightMath.PolygonalRadialDistance(1f, 0f, polygonSides) - 1f) < 1e-4f,
            "Distancia na normal da face facetada deve coincidir com o apotema.");
        Check(Mathf.Abs(RetroLightMath.PolygonalRadialDistance(1f / Mathf.Cos(halfSector), halfSector,
            polygonSides) - 1f) < 1e-4f, "Vertices facetados devem permanecer no contorno.");

        float edge = RetroLightMath.NormalizedDistanceFor(.5f, .2f, 2f);
        Check(Mathf.Abs(RetroLightMath.Attenuation(edge, .2f, 2f, 0) - .5f) < 1e-3f, "Inversa da queda (gizmos).");
    }

    private static void CheckFlicker()
    {
        foreach (LocalLightFlickerMode mode in Enum.GetValues(typeof(LocalLightFlickerMode)))
        {
            LocalLightFlicker flicker = new()
            {
                enabled = true, mode = mode, minimumIntensity = .2f, maximumIntensity = 3f,
                speed = mode == LocalLightFlickerMode.Failing ? 1.5f : 4f, updatesPerSecond = 0f
            };
            float lowest = float.MaxValue;
            float highest = float.MinValue;
            for (int i = 0; i < 4000; i++)
            {
                float t = flicker.Evaluate(i * .013f, 1.7f);
                Check(t >= 0f && t <= 1f, mode + " fora de 0..1.");
                float intensity = Mathf.Lerp(flicker.minimumIntensity, flicker.maximumIntensity, t);
                lowest = Mathf.Min(lowest, intensity);
                highest = Mathf.Max(highest, intensity);
            }
            float span = (highest - lowest) / (flicker.maximumIntensity - flicker.minimumIntensity);
            Check(span > .6f, mode + " deve percorrer a maior parte da faixa min-max (percorreu " + span.ToString("0.00") + ").");
        }

        LocalLightFlicker stepped = new() { enabled = true, speed = 5f, updatesPerSecond = 10f };
        Check(Mathf.Approximately(stepped.Evaluate(1.01f, 0f), stepped.Evaluate(1.09f, 0f)),
            "Updates Per Second deve segurar o valor dentro do passo.");
    }

    private static void CheckEmitterRegistration()
    {
        LocalLightProfile profile = ScriptableObject.CreateInstance<LocalLightProfile>();
        profile.lights[0].flicker.enabled = true;
        profile.lights[0].flicker.minimumIntensity = .5f;
        profile.lights[0].flicker.maximumIntensity = 2f;
        GameObject host = new("Retro Light Test") { hideFlags = HideFlags.HideAndDontSave };
        host.SetActive(false);
        try
        {
            int before = RetroLightSystem.RegisteredLightCount;
            LocalLightEmitter emitter = host.AddComponent<LocalLightEmitter>();
            emitter.Profile = profile;
            host.SetActive(true);
            Check(RetroLightSystem.RegisteredLightCount == before + 1, "Emissor deve registrar uma luz.");
            host.SetActive(false);
            Check(RetroLightSystem.RegisteredLightCount == before, "Emissor desativado deve liberar a luz.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    private static void CheckFlickerWithRangeMultiplier()
    {
        LocalLightProfile profile = ScriptableObject.CreateInstance<LocalLightProfile>();
        GameObject host = new("Retro Flicker Range Test") { hideFlags = HideFlags.HideAndDontSave };
        host.SetActive(false);
        try
        {
            LocalLightDefinition definition = profile.lights[0];
            definition.intensity = 4f;
            definition.range = 20f;
            definition.flicker.enabled = true;
            definition.flicker.mode = LocalLightFlickerMode.Pulse;
            definition.flicker.speed = 1f;
            definition.flicker.updatesPerSecond = 0f;
            definition.flicker.minimumIntensity = .5f;
            definition.flicker.maximumIntensity = 1.5f;
            definition.flicker.rangeVariation = .5f;
            LocalLightEmitter emitter = host.AddComponent<LocalLightEmitter>();
            emitter.Profile = profile;
            host.SetActive(true);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var lights = (System.Collections.Generic.List<RetroLight>)typeof(LocalLightEmitter)
                .GetField("lights", flags).GetValue(emitter);
            var apply = typeof(LocalLightEmitter).GetMethod("ApplyFlicker", flags);
            foreach (LocalLightShape shape in Enum.GetValues(typeof(LocalLightShape)))
            {
                definition.shape = shape;
                lights[0].spot = shape == LocalLightShape.Spot;
                apply.Invoke(emitter, new object[] { 0, definition, .75f, 0f });
                Check(Mathf.Abs(lights[0].intensity - 2f) < .001f &&
                    Mathf.Abs(lights[0].range - 10f) < .001f,
                    shape + ": minimo do flicker deve usar intensidade e alcance base.");
                emitter.SetRangeMultiplier(0f);
                emitter.SetRangeMultiplier(2f);
                Check(Mathf.Abs(lights[0].range - 20f) < .001f,
                    shape + ": multiplicador externo deve preservar o alcance do flicker, inclusive apos zero.");
                emitter.SetRangeMultiplier(1f);
                apply.Invoke(emitter, new object[] { 0, definition, .25f, 0f });
                Check(Mathf.Abs(lights[0].intensity - 6f) < .001f &&
                    Mathf.Abs(lights[0].range - 20f) < .001f,
                    shape + ": maximo do flicker deve usar intensidade e alcance base.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    private static void CheckLightBudget()
    {
        GameObject cameraObject = new("Retro Light Test Camera") { hideFlags = HideFlags.HideAndDontSave };
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        RetroLight[] lights = new RetroLight[40];
        try
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i] = new RetroLight { owner = cameraObject.transform, localPosition = new Vector3(0f, 0f, 3f + i), range = 2f };
                RetroLightSystem.Register(lights[i]);
            }
            RetroLight behind = new() { owner = cameraObject.transform, localPosition = new Vector3(0f, 0f, -50f), range = 2f };
            RetroLightSystem.Register(behind);

            RetroLightSystem.RefreshForCamera(camera);
            RetroLightingSettings settings = RetroLightingSettings.Instance;
            int budget = settings != null ? settings.maxVisibleLights : 16;
            Check(RetroLightSystem.VisibleLightCount == Mathf.Min(budget, lights.Length),
                "Orcamento de luzes por camera nao respeitado (" + RetroLightSystem.VisibleLightCount + ").");

            Vector4[] positions = Shader.GetGlobalVectorArray("_RetroLightPosition");
            Check(positions != null && positions.Length == RetroLightSystem.MaxLights, "Array global de luzes com tamanho fixo.");
            Check(Mathf.Abs(positions[0].z - 3f) < .01f, "A luz mais proxima da camera vem primeiro.");
            RetroLightSystem.Unregister(behind);
        }
        finally
        {
            foreach (RetroLight light in lights)
                RetroLightSystem.Unregister(light);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Shader.SetGlobalFloat("_RetroLightCount", 0f);
        }
    }

    private static float RenderBrightness(Camera camera)
    {
        RenderTexture target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        Texture2D texture = new(320, 180, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            long sum = 0;
            foreach (Color32 pixel in texture.GetPixels32())
                sum += pixel.r + pixel.g + pixel.b;
            return sum / (320f * 180f * 3f);
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    // Renderiza um chao grande de 4 vertices com uma luz local. Protege contra a luz "sumir"
    // em malhas pouco subdivididas: o modo padrao (Pixel) precisa iluminar o meio do chao.
    private static void CheckRenderedLight()
    {
        RetroLightingSettings settings = RetroLightingSettings.Instance;
        RetroEnvironmentProfile environment = settings.defaultEnvironment;
        Color sky = environment.ambientSkyColor;
        Color ground = environment.ambientGroundColor;
        RetroLightingModeOverride previousOverride = settings.lightingModeOverride;
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject cameraObject = new("Retro Render Test Camera");
        Material material = new(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Retro/RetroDefault.mat"));
        RetroLight light = new() { localPosition = new Vector3(500f, 2f, 500f), range = 15f, intensity = 2f, color = Color.white };
        try
        {
            Check(Mathf.RoundToInt(material.GetFloat("_LightingMode")) == 2, "O padrao do Retro Lit deve ser Pixel.");
            environment.ambientSkyColor = Color.black;
            environment.ambientGroundColor = Color.black;
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.position = new Vector3(500f, 0f, 500f);
            floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(60f, 60f, 1f);
            floor.GetComponent<Renderer>().sharedMaterial = material;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.enabled = false;
            cameraObject.transform.position = new Vector3(500f, 12f, 490f);
            cameraObject.transform.LookAt(new Vector3(500f, 0f, 500f));

            RetroLightSystem.Register(light);
            settings.lightingModeOverride = RetroLightingModeOverride.PerMaterial;
            float lit = RenderBrightness(camera);
            RetroLightSystem.Unregister(light);
            float dark = RenderBrightness(camera);
            Check(lit > dark + 5f, "A luz local deve iluminar um chao grande (com luz " + lit.ToString("0.0") +
                ", sem luz " + dark.ToString("0.0") + ").");

            RetroLightSystem.Register(light);
            settings.lightingModeOverride = RetroLightingModeOverride.Grid;
            float grid = RenderBrightness(camera);
            Check(grid > dark + 5f, "Modo Grid deve iluminar um chao grande (" + grid.ToString("0.0") + ").");

            material.SetFloat("_LightingModeOverride", 1f);
            float vertexOverride = RenderBrightness(camera);
            Check(vertexOverride < grid - 5f,
                "Override Vertex do material deve prevalecer sobre Grid global.");
            MaterialPropertyBlock block = new();
            RetroLightingOverride.ApplyToRenderer(floor.GetComponent<Renderer>(),
                RetroLightingRendererOverride.Pixel, block);
            float pixelOverride = RenderBrightness(camera);
            Check(pixelOverride > vertexOverride + 5f,
                "Override Pixel do objeto deve prevalecer sobre Vertex do material.");
        }
        finally
        {
            RetroLightSystem.Unregister(light);
            environment.ambientSkyColor = sky;
            environment.ambientGroundColor = ground;
            settings.lightingModeOverride = previousOverride;
            UnityEngine.Object.DestroyImmediate(floor);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(material);
            Shader.SetGlobalFloat("_RetroLightCount", 0f);
        }
    }

    private static void CheckMaterialConversion()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Check(lit != null, "URP Lit nao encontrado.");
        Material material = new(lit);
        try
        {
            material.SetColor("_BaseColor", new Color(.8f, .1f, .1f, 1f));
            material.SetFloat("_AlphaClip", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cutoff", .3f);
            Check(RetroMaterialConverter.Convert(material), "Conversao de URP Lit falhou.");
            Check(material.shader.name == RetroMaterialConverter.ShaderName, "Shader nao trocado.");
            Check(material.GetColor("_BaseColor") == new Color(.8f, .1f, .1f, 1f), "Cor base perdida.");
            Check(material.IsKeywordEnabled("_ALPHATEST_ON") && Mathf.Approximately(material.GetFloat("_Cutoff"), .3f),
                "Recorte por alpha perdido.");
            Check(material.GetColor("_EmissionColor") == Color.black, "Emissao desligada deve ficar preta.");
            Check(Mathf.RoundToInt(material.GetFloat("_LightingMode")) == 2, "Material convertido deve usar Pixel.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(material);
        }
    }
}

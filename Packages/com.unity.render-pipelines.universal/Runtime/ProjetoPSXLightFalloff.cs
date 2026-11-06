using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{

/// <summary>Registra curvas por Light para o calculo customizado da URP.</summary>
public static class ProjetoPSXLightFalloff
{
    private const int RowCount = 64;
    private const int SampleCount = 128;
    private const string TextureName = "_ProjetoPSXLightFalloffLut";
    private static readonly Dictionary<Light, int> Rows = new();
    private static readonly Dictionary<Light, int> FacetedSteps = new();
    private static readonly Stack<int> FreeRows = new();
    private static Texture2D lut;
    private static Texture2D previousLut;
    private static Color[] pixels;
    private static bool dirty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Rows.Clear();
        FacetedSteps.Clear();
        FreeRows.Clear();
        lut = null;
        previousLut = null;
        pixels = null;
        dirty = false;
    }

    public static void Register(Light light, AnimationCurve curve, int lowPolySegments, int lowPolyFacetSteps)
    {
        if (light == null)
            return;

        EnsureTexture();
        if (!Rows.TryGetValue(light, out int row))
        {
            row = FreeRows.Count > 0 ? FreeRows.Pop() : Rows.Count;
            if (row >= RowCount)
            {
                Debug.LogError("Projeto PSX suporta no maximo 64 luzes locais customizadas simultaneas.", light);
                return;
            }
            Rows.Add(light, row);
        }

        if (lowPolyFacetSteps >= 2)
            FacetedSteps[light] = Mathf.Clamp(lowPolyFacetSteps, 2, 8);
        else
            FacetedSteps.Remove(light);

        WriteRow(row, curve, lowPolySegments);
        Flush();
    }

    private static void WriteRow(int row, AnimationCurve curve, int lowPolySegments)
    {
        EnsureTexture();
        float encodedSegments = Mathf.Clamp(lowPolySegments, 0, 32) / 255f;
        for (int i = 0; i < SampleCount; i++)
        {
            float distance01 = i / (float)(SampleCount - 1);
            float intensityLoss = Mathf.Clamp01(curve != null
                ? curve.Evaluate(distance01) : distance01);
            pixels[row * SampleCount + i] = new Color(1f - intensityLoss,
                encodedSegments, 0f, 1f);
        }
        dirty = true;
    }

    public static bool TryGetRow(Light light, out float row)
    {
        if (light != null && Rows.TryGetValue(light, out int index))
        {
            float facetEncoding = FacetedSteps.TryGetValue(light, out int steps) ? steps / 16f : 0f;
            row = index + 1f + facetEncoding;
            return true;
        }
        row = 0f;
        return false;
    }

    public static void Unregister(Light light)
    {
        if (light == null || !Rows.TryGetValue(light, out int row))
            return;
        Rows.Remove(light);
        FacetedSteps.Remove(light);
        FreeRows.Push(row);
        for (int i = 0; i < SampleCount; i++)
            pixels[row * SampleCount + i] = Color.clear;
        dirty = true;
        Flush();
    }

    private static void EnsureTexture()
    {
        if (lut != null)
            return;
        lut = new Texture2D(SampleCount, RowCount, TextureFormat.RGBA32, false, true)
        {
            name = "Proyecto PSX Light Falloff LUT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        pixels = new Color[SampleCount * RowCount];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;
        lut.SetPixels(pixels);
        lut.Apply(false, false);
        previousLut = lut;
        Shader.SetGlobalTexture(TextureName, lut);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookUpdate()
    {
        LightFalloffUpdater.Ensure();
    }

    private static void Flush()
    {
        if (lut != null && previousLut != lut)
        {
            Shader.SetGlobalTexture(TextureName, lut);
            previousLut = lut;
        }
        if (!dirty || lut == null)
            return;
        lut.SetPixels(pixels);
        lut.Apply(false, false);
        dirty = false;
    }

    private sealed class LightFalloffUpdater : MonoBehaviour
    {
        private static LightFalloffUpdater updater;

        public static void Ensure()
        {
            if (updater != null)
                return;
            GameObject host = new("Proyecto PSX Light Falloff");
            host.hideFlags = HideFlags.HideAndDontSave;
            updater = host.AddComponent<LightFalloffUpdater>();
            DontDestroyOnLoad(host);
        }

        private void LateUpdate() => Flush();
        private void OnDestroy()
        {
            updater = null;
            if (lut != null)
                Shader.SetGlobalTexture(TextureName, null);
        }
    }
}
}

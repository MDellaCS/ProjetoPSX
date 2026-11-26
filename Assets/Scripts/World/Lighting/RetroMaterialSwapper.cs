using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Em Play, troca os materiais URP Lit/ProBuilder dos renderers da cena por copias Retro Lit
/// (somente em memoria; nenhum asset ou cena e alterado). Roda uma vez por cena carregada,
/// nunca por frame. Controlado por RetroLightingSettings.autoConvertSceneMaterials.
/// </summary>
public static class RetroMaterialSwapper
{
    private static readonly Dictionary<Material, Material> Cache = new();
    private static bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Cache.Clear();
        hooked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (!hooked)
        {
            hooked = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        // A cena inicial ja carregou antes deste hook.
        SwapScene(default, true);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SwapScene(scene, false);

    /// <summary>Troca os materiais da cena. Sem cena (default), varre todas as cenas carregadas.</summary>
    public static int SwapScene(Scene scene, bool allScenes)
    {
        RetroLightingSettings settings = RetroLightingSettings.Instance;
        if (settings == null || !settings.autoConvertSceneMaterials || RetroMaterialConversion.RetroShader == null)
            return 0;

        // Cache de materiais destruidos (troca de cena) e fora da lista.
        List<Material> stale = null;
        foreach (KeyValuePair<Material, Material> pair in Cache)
            if (pair.Key == null || pair.Value == null)
                (stale ??= new List<Material>()).Add(pair.Key);
        if (stale != null)
            foreach (Material key in stale)
                Cache.Remove(key);

        int swapped = 0;
        List<Material> buffer = new();
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (!allScenes && renderer.gameObject.scene != scene)
                continue;
            swapped += SwapRenderer(renderer, buffer);
        }
        return swapped;
    }

    public static int SwapRenderer(Renderer renderer, List<Material> buffer = null)
    {
        if (renderer == null)
            return 0;
        buffer ??= new List<Material>();
        renderer.GetSharedMaterials(buffer);
        bool changed = false;
        int swapped = 0;
        for (int i = 0; i < buffer.Count; i++)
        {
            Material source = buffer[i];
            if (!RetroMaterialConversion.CanConvert(source))
                continue;
            if (!Cache.TryGetValue(source, out Material retro) || retro == null)
            {
                retro = RetroMaterialConversion.CreateCopy(source);
                if (!RetroMaterialConversion.Convert(retro))
                {
                    Object.Destroy(retro);
                    continue;
                }
                Cache[source] = retro;
            }
            buffer[i] = retro;
            changed = true;
            swapped++;
        }
        if (changed)
            renderer.SetSharedMaterials(buffer);
        return swapped;
    }
}

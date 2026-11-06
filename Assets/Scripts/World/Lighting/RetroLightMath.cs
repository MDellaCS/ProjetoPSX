using UnityEngine;

/// <summary>
/// Espelho em C# da queda usada pelo shader Retro Lit (RetroLighting.hlsl).
/// Usado pelos gizmos e pelos testes; mantenha as duas versoes iguais.
/// </summary>
public static class RetroLightMath
{
    /// <summary>Atenuacao por distancia (0..1). normalizedDistance = distancia / alcance.</summary>
    public static float Attenuation(float normalizedDistance, float coreRadius,
        float falloffExponent, int bands)
    {
        if (normalizedDistance >= 1f)
            return 0f;
        float t = Mathf.Clamp01((normalizedDistance - coreRadius) /
            Mathf.Max(1f - coreRadius, 1e-4f));
        float attenuation = Mathf.Pow(1f - t, Mathf.Max(.01f, falloffExponent));
        return Quantize(attenuation, bands);
    }

    /// <summary>Quantiza em faixas. Arredonda para cima para manter a borda da luz visivel.</summary>
    public static float Quantize(float attenuation, int bands)
    {
        if (bands < 1)
            return attenuation;
        return Mathf.Max(0f, Mathf.Ceil(attenuation * bands - 1e-3f)) / bands;
    }

    /// <summary>Distancia normalizada (0..1) em que a queda continua atinge o valor informado.</summary>
    public static float NormalizedDistanceFor(float attenuation, float coreRadius, float falloffExponent)
    {
        float t = 1f - Mathf.Pow(Mathf.Clamp01(attenuation), 1f / Mathf.Max(.01f, falloffExponent));
        return coreRadius + t * (1f - coreRadius);
    }
}

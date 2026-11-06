#ifndef PROJETOPSX_RETRO_LIGHTING_INCLUDED
#define PROJETOPSX_RETRO_LIGHTING_INCLUDED

// Iluminacao retro do Projeto PSX. Dados enviados por RetroLightSystem.cs.
// A queda por distancia tem um espelho em RetroLightMath.cs: mantenha as duas iguais.

#define RETRO_MAX_LIGHTS 32

float4 _RetroLightPosition[RETRO_MAX_LIGHTS];  // xyz posicao, w 1/alcance
float4 _RetroLightColor[RETRO_MAX_LIGHTS];     // rgb cor * intensidade (linear), w nucleo (0..0.95)
float4 _RetroLightDirection[RETRO_MAX_LIGHTS]; // xyz direcao do Spot, w cos(externo) ou -2 em Point
float4 _RetroLightShape[RETRO_MAX_LIGHTS];     // x 1/(cosInt - cosExt), y expoente, z faixas, w wrap
float _RetroLightCount;

float4 _RetroAmbientSky;     // rgb linear
float4 _RetroAmbientGround;  // rgb linear
float4 _RetroFogColor;       // rgb linear, a = 1 quando a nevoa esta ligada
float4 _RetroFogParams;      // x inicio, y 1/(fim - inicio), z opacidade maxima, w perfuracao por luz
float4 _RetroFogCenter;      // xyz origem da nevoa
float4 _RetroFogHeight;      // x base, y 1/espessura, z ligada
float4 _RetroLightingGlobal; // x modo forcado + 1 (0 = material), y grade forcada, z niveis de cor, w dither

// 0 = Vertex, 1 = Grid, 2 = Pixel
float RetroResolveLightingMode(float materialMode)
{
    return _RetroLightingGlobal.x > 0.5 ? _RetroLightingGlobal.x - 1.0 : materialMode;
}

float RetroResolveGridSize(float materialGrid)
{
    return max(0.01, _RetroLightingGlobal.y > 0.0 ? _RetroLightingGlobal.y : materialGrid);
}

// Amostra a luz no centro de uma celula de mundo: luz em "texels" grandes, independente da malha.
float3 RetroSnapToGrid(float3 positionWS, float3 normalWS, float cellSize)
{
    float3 shifted = positionWS + normalWS * (cellSize * 0.5);
    return (floor(shifted / cellSize) + 0.5) * cellSize;
}

half3 RetroAmbient(half3 normalWS)
{
    half up = normalWS.y * 0.5 + 0.5;
    return lerp(_RetroAmbientGround.rgb, _RetroAmbientSky.rgb, up);
}

half3 RetroMainLight(half3 normalWS)
{
    // Luz direcional principal da URP (sol/lua), opcional. Sem luz direcional a cor e zero.
    Light mainLight = GetMainLight();
    return mainLight.color * (saturate(dot(normalWS, mainLight.direction)) * mainLight.distanceAttenuation);
}

half3 RetroLocalLights(float3 positionWS, half3 normalWS)
{
    half3 total = 0;
    int count = (int)_RetroLightCount;
    UNITY_LOOP
    for (int i = 0; i < count; i++)
    {
        float4 position = _RetroLightPosition[i];
        float3 toLight = position.xyz - positionWS;
        float distanceSq = dot(toLight, toLight);
        float normalizedDistance = sqrt(distanceSq) * position.w;
        if (normalizedDistance >= 1.0)
            continue;

        float4 color = _RetroLightColor[i];
        float4 shape = _RetroLightShape[i];
        float4 direction = _RetroLightDirection[i];
        float3 lightDir = toLight * rsqrt(max(distanceSq, 1e-6));

        // Queda linear ate o alcance (sem inverso-quadrado), moldada pelo nucleo e pelo expoente.
        float t = saturate((normalizedDistance - color.w) / max(1.0 - color.w, 1e-4));
        float attenuation = pow(1.0 - t, shape.y);

        if (direction.w > -1.5)
        {
            float cone = saturate((dot(direction.xyz, -lightDir) - direction.w) * shape.x);
            attenuation *= cone * cone * (3.0 - 2.0 * cone);
        }

        if (shape.z >= 1.0)
            attenuation = max(0.0, ceil(attenuation * shape.z - 1e-3)) / shape.z;

        float wrap = shape.w;
        float diffuse = saturate((dot(normalWS, lightDir) + wrap) / (1.0 + wrap));
        total += color.rgb * (attenuation * diffuse);
    }
    return total;
}

half RetroFogFactor(float3 positionWS)
{
    if (_RetroFogColor.a <= 0.0)
        return 0.0;
    float distanceToCenter = distance(positionWS, _RetroFogCenter.xyz);
    half fog = saturate((distanceToCenter - _RetroFogParams.x) * _RetroFogParams.y);
    if (_RetroFogHeight.z > 0.5)
        fog *= 1.0 - saturate((positionWS.y - _RetroFogHeight.x) * _RetroFogHeight.y);
    return fog * _RetroFogParams.z;
}

half3 RetroApplyFog(half3 color, half fog, half3 localLight)
{
    // Superficies bem iluminadas por luzes locais furam a nevoa (lanterna no nevoeiro).
    half pierce = saturate(dot(localLight, half3(0.299, 0.587, 0.114)) * _RetroFogParams.w);
    return lerp(color, _RetroFogColor.rgb, fog * (1.0 - pierce));
}

// Reducao de cor com dither ordenado 4x4, como a saida de 15/16 bits do PS1/PS2.
half3 RetroQuantize(half3 color, float2 pixelPosition)
{
    float levels = _RetroLightingGlobal.z;
    if (levels < 2.0)
        return color;

    static const float bayer[16] =
    {
         0.0,  8.0,  2.0, 10.0,
        12.0,  4.0, 14.0,  6.0,
         3.0, 11.0,  1.0,  9.0,
        15.0,  7.0, 13.0,  5.0
    };
    uint2 cell = uint2(pixelPosition) & 3u;
    float threshold = (bayer[cell.y * 4u + cell.x] + 0.5) / 16.0 - 0.5;

    half3 clamped = saturate(color);
    half3 gammaColor = LinearToSRGB(clamped);
    float steps = levels - 1.0;
    gammaColor = floor(gammaColor * steps + 0.5 + threshold * _RetroLightingGlobal.w) / steps;
    return SRGBToLinear(saturate(gammaColor)) + (color - clamped);
}

#endif

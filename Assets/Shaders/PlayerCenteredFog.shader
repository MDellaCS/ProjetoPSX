Shader "Hidden/ProjetoPSX/Player Centered Fog"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "PlayerCenteredFog"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Lighting/RetroLighting.hlsl"

            float _PlayerCenteredFogEnabled;
            float4 _PlayerCenteredFogColor;
            float4 _PlayerCenteredFogPlayerPosition;
            float _PlayerCenteredFogStartRadius;
            float _PlayerCenteredFogEndRadius;
            float _PlayerCenteredFogBaseHeightOffset;
            float _PlayerCenteredFogHeight;
            float _PlayerCenteredFogOpacity;
            float _PlayerCenteredFogNoiseScale;
            float _PlayerCenteredFogNoiseStrength;
            float _PlayerCenteredFogNoiseSpeed;
            float _PlayerCenteredVolumetricEnabled;
            float4 _PlayerCenteredVolumetricParams; // density, max distance, steps, opacity
            float4 _PlayerCenteredVolumetricNoise;  // scale, strength, height falloff, anisotropy
            float4 _PlayerCenteredVolumetricWind;
            float4 _PlayerCenteredVolumetricHeight; // base height, layer height
            float4 _PlayerCenteredVolumetricLighting; // scattering strength
            float4 _PlayerCenteredVolumetricColor;
            float4 _PlayerCenteredVolumetricUpperColor;

            float Noise(float2 position)
            {
                float2 cell = floor(position);
                float2 fraction = frac(position);
                fraction = fraction * fraction * (3.0 - 2.0 * fraction);
                float bottomLeft = frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
                float bottomRight = frac(sin(dot(cell + float2(1.0, 0.0), float2(127.1, 311.7))) * 43758.5453);
                float topLeft = frac(sin(dot(cell + float2(0.0, 1.0), float2(127.1, 311.7))) * 43758.5453);
                float topRight = frac(sin(dot(cell + 1.0, float2(127.1, 311.7))) * 43758.5453);
                return lerp(lerp(bottomLeft, bottomRight, fraction.x),
                    lerp(topLeft, topRight, fraction.x), fraction.y);
            }

            float Hash3(float3 p)
            {
                p = frac(p * .1031);
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise3(float3 position)
            {
                float3 cell = floor(position);
                float3 f = frac(position);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash3(cell + float3(0, 0, 0));
                float n100 = Hash3(cell + float3(1, 0, 0));
                float n010 = Hash3(cell + float3(0, 1, 0));
                float n110 = Hash3(cell + float3(1, 1, 0));
                float n001 = Hash3(cell + float3(0, 0, 1));
                float n101 = Hash3(cell + float3(1, 0, 1));
                float n011 = Hash3(cell + float3(0, 1, 1));
                float n111 = Hash3(cell + float3(1, 1, 1));
                float z0 = lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y);
                float z1 = lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y);
                return lerp(z0, z1, f.z);
            }

            float HenyeyGreenstein(float cosine, float anisotropy)
            {
                float g = clamp(anisotropy, -.8, .8);
                float denominator = max(.01, 1.0 + g * g - 2.0 * g * cosine);
                return (1.0 - g * g) / pow(denominator, 1.5);
            }

            float3 RetroFogLocalLightScattering(float3 samplePosition,
                float3 rayDirection, float anisotropy)
            {
                float3 scatteredLight = 0.0;
                int lightLimit = min((int)_RetroLightCount,
                    (int)_PlayerCenteredVolumetricLighting.z);
                [loop]
                for (int lightIndex = 0; lightIndex < lightLimit; lightIndex++)
                {
                    float4 lightPosition = _RetroLightPosition[lightIndex];
                    float3 toLight = lightPosition.xyz - samplePosition;
                    float distanceSquared = dot(toLight, toLight);
                    float distanceToLight = sqrt(distanceSquared);
                    float normalizedDistance = distanceToLight * lightPosition.w;
                    if (normalizedDistance >= 1.0)
                        continue;

                    float3 lightDirection = toLight / max(distanceToLight, .001);
                    float3 fromLight = -lightDirection;
                    float4 lightColor = _RetroLightColor[lightIndex];
                    float4 lightShape = _RetroLightShape[lightIndex];
                    float attenuationT = saturate((normalizedDistance - lightColor.w) /
                        max(1.0 - lightColor.w, 1e-4));
                    float attenuation = pow(1.0 - attenuationT,
                        max(.01, lightShape.y));

                    float4 spotDirection = _RetroLightDirection[lightIndex];
                    if (spotDirection.w > -1.5)
                    {
                        float cone = saturate((dot(spotDirection.xyz, fromLight) -
                            spotDirection.w) * lightShape.x);
                        attenuation *= cone * cone * (3.0 - 2.0 * cone);
                    }

                    float phase = HenyeyGreenstein(dot(fromLight, -rayDirection),
                        anisotropy);
                    // As intensidades retro sao absolutas e podem superar 60;
                    // comprime-as para manter a cor da nevoa sem estourar branco.
                    float3 normalizedLightColor = min(lightColor.rgb * .25, 1.0);
                    scatteredLight += normalizedLightColor *
                        (attenuation * phase);
                }
                return scatteredLight;
            }

            half4 ApplyVolumetricFog(half4 sceneColor, float3 worldPosition)
            {
                if (_PlayerCenteredVolumetricEnabled <= 0.0 ||
                    _PlayerCenteredVolumetricParams.x <= 0.0)
                    return sceneColor;

                float3 ray = worldPosition - _WorldSpaceCameraPos;
                float fullDistance = length(ray);
                float marchDistance = min(fullDistance, _PlayerCenteredVolumetricParams.y);
                if (marchDistance <= .001)
                    return sceneColor;

                float3 rayDirection = ray / max(fullDistance, .001);
                int steps = (int)clamp(_PlayerCenteredVolumetricParams.z, 4.0, 64.0);
                float stepLength = marchDistance / steps;
                float transmittance = 1.0;
                float3 inScattering = 0.0;
                float baseHeight = _PlayerCenteredVolumetricHeight.x;
                float layerHeight = max(.01, _PlayerCenteredVolumetricHeight.y);
                float density = _PlayerCenteredVolumetricParams.x;
                float noiseScale = max(.001, _PlayerCenteredVolumetricNoise.x);
                float noiseStrength = saturate(_PlayerCenteredVolumetricNoise.y);
                float heightFalloff = max(.01, _PlayerCenteredVolumetricNoise.z);
                float3 windOffset = _PlayerCenteredVolumetricWind.xyz * _Time.y;
                Light mainLight = GetMainLight();
                float phase = HenyeyGreenstein(dot(-rayDirection, mainLight.direction),
                    _PlayerCenteredVolumetricNoise.w);
                float3 directionalScatter = mainLight.color *
                    (_PlayerCenteredVolumetricLighting.x * phase);

                [loop]
                for (int i = 0; i < steps; i++)
                {
                    float distanceAlongRay = (i + .5) * stepLength;
                    float3 samplePosition = _WorldSpaceCameraPos +
                        rayDirection * distanceAlongRay;
                    float radialDistance = distance(samplePosition.xz,
                        _PlayerCenteredFogPlayerPosition.xz);
                    float radialWeight = smoothstep(_PlayerCenteredFogStartRadius,
                        max(_PlayerCenteredFogStartRadius + .001,
                            _PlayerCenteredFogEndRadius), radialDistance);
                    float heightWeight = exp(-max(0.0, samplePosition.y - baseHeight) /
                        heightFalloff);
                    heightWeight *= 1.0 - smoothstep(baseHeight + layerHeight,
                        baseHeight + layerHeight * 1.5, samplePosition.y);
                    float noise = Noise3(samplePosition * noiseScale + windOffset);
                    float noiseWeight = lerp(1.0, saturate(noise * 2.0), noiseStrength);
                    float localDensity = density * radialWeight * heightWeight * noiseWeight;
                    float stepTransmittance = exp(-localDensity * stepLength);
                    float contribution = transmittance * (1.0 - stepTransmittance);
                    float heightBlend = saturate((samplePosition.y - baseHeight) / layerHeight);
                    float3 fogColor = lerp(_PlayerCenteredVolumetricColor.rgb,
                        _PlayerCenteredVolumetricUpperColor.rgb, heightBlend);
                    float3 localScatter = RetroFogLocalLightScattering(
                        samplePosition, rayDirection,
                        _PlayerCenteredVolumetricNoise.w) *
                        _PlayerCenteredVolumetricLighting.y;
                    inScattering += fogColor * (1.0 + directionalScatter +
                        localScatter) * contribution;
                    transmittance *= stepTransmittance;
                    if (transmittance < .01)
                        break;
                }

                float opacity = (1.0 - transmittance) *
                    saturate(_PlayerCenteredVolumetricParams.w);
                return half4(sceneColor.rgb * (1.0 - opacity) +
                    inScattering * saturate(_PlayerCenteredVolumetricParams.w), sceneColor.a);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_PlayerCenteredFogEnabled <= 0.0 &&
                    _PlayerCenteredVolumetricEnabled <= 0.0)
                    return sceneColor;

                float rawDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                if (rawDepth <= 0.00001) return sceneColor;
                #else
                if (rawDepth >= 0.99999) return sceneColor;
                #endif

                float3 worldPosition = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                if (_PlayerCenteredVolumetricEnabled > 0.0)
                    sceneColor = ApplyVolumetricFog(sceneColor, worldPosition);

                if (_PlayerCenteredFogEnabled > 0.0)
                {
                    float horizontalDistance = distance(worldPosition.xz,
                        _PlayerCenteredFogPlayerPosition.xz);
                    float radialFog = smoothstep(_PlayerCenteredFogStartRadius,
                        max(_PlayerCenteredFogStartRadius + .001,
                            _PlayerCenteredFogEndRadius), horizontalDistance);
                    float fogFloor = _PlayerCenteredFogPlayerPosition.y +
                        _PlayerCenteredFogBaseHeightOffset;
                    float heightFog = 1.0 - smoothstep(fogFloor,
                        fogFloor + max(.001, _PlayerCenteredFogHeight), worldPosition.y);
                    float animatedNoise = Noise(worldPosition.xz *
                        max(.001, _PlayerCenteredFogNoiseScale) +
                        _Time.y * _PlayerCenteredFogNoiseSpeed);
                    float fogAmount = saturate(radialFog +
                        (animatedNoise - .5) * _PlayerCenteredFogNoiseStrength) *
                        saturate(heightFog) * saturate(_PlayerCenteredFogOpacity);
                    sceneColor.rgb = lerp(sceneColor.rgb,
                        _PlayerCenteredFogColor.rgb, fogAmount);
                }
                return sceneColor;
            }
            ENDHLSL
        }
    }
}

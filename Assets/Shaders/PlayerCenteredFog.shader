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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

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

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_PlayerCenteredFogEnabled <= 0.0)
                    return sceneColor;

                float rawDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                if (rawDepth <= 0.00001) return sceneColor;
                #else
                if (rawDepth >= 0.99999) return sceneColor;
                #endif

                float3 worldPosition = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float horizontalDistance = distance(worldPosition.xz, _PlayerCenteredFogPlayerPosition.xz);
                float radialFog = smoothstep(_PlayerCenteredFogStartRadius,
                    max(_PlayerCenteredFogStartRadius + .001, _PlayerCenteredFogEndRadius), horizontalDistance);
                float fogFloor = _PlayerCenteredFogPlayerPosition.y + _PlayerCenteredFogBaseHeightOffset;
                float heightFog = 1.0 - smoothstep(fogFloor,
                    fogFloor + max(.001, _PlayerCenteredFogHeight), worldPosition.y);
                float animatedNoise = Noise(worldPosition.xz * max(.001, _PlayerCenteredFogNoiseScale) +
                    _Time.y * _PlayerCenteredFogNoiseSpeed);
                float fogAmount = saturate(radialFog + (animatedNoise - .5) * _PlayerCenteredFogNoiseStrength) *
                    saturate(heightFog) * saturate(_PlayerCenteredFogOpacity);
                return lerp(sceneColor, _PlayerCenteredFogColor, fogAmount);
            }
            ENDHLSL
        }
    }
}

Shader "Hidden/ProjetoPSX/Environment Fog"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "EnvironmentFog"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D(_PSXFogCurves); SAMPLER(sampler_PSXFogCurves);
            float4 _PSXFogColor, _PSXFogColorRange, _PSXFogBlurRange, _PSXFogOrigin;
            float _PSXFogAnimationTime;
            float4 _PSXFogVolumeColor, _PSXFogVolumeParams, _PSXFogVolumeRange, _PSXFogVolumeHeight, _PSXFogNoise, _PSXFogWind;

            float Hash(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }
            float Noise(float3 p)
            {
                float3 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash(i), Hash(i+float3(1,0,0)), f.x), lerp(Hash(i+float3(0,1,0)), Hash(i+float3(1,1,0)), f.x), f.y),
                    lerp(lerp(Hash(i+float3(0,0,1)), Hash(i+float3(1,0,1)), f.x), lerp(Hash(i+float3(0,1,1)), Hash(i+1), f.x), f.y), f.z);
            }
            float Curve(float d, float4 range, int channel)
            {
                float t = saturate((d - range.x) / max(.01, range.y - range.x));
                float2 uv = float2((t * 127.0 + .5) / 128.0, .5);
                float4 c = SAMPLE_TEXTURE2D(_PSXFogCurves, sampler_PSXFogCurves, uv);
                return lerp(range.z, range.w, channel == 0 ? c.r : (channel == 1 ? c.g : c.b));
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float raw = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                bool hasGeometry = raw > .00001;
                raw = hasGeometry ? raw : .00001;
                #else
                bool hasGeometry = raw < .99999;
                raw = hasGeometry ? raw : .99999;
                raw = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, raw);
                #endif
                float3 world = ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP);
                float3 ray = world - _WorldSpaceCameraPos;
                float dist = length(ray);
                float fogDistance = distance(world, _PSXFogOrigin.xyz);
                float blur = hasGeometry ? max(0, Curve(fogDistance, _PSXFogBlurRange, 1)) : 0;
                if (blur > .001)
                {
                    float2 pixel = _BlitTexture_TexelSize.xy * blur;
                    float2 offsets[4] = {float2(1,0),float2(-1,0),float2(0,1),float2(0,-1)};
                    half3 sum = scene.rgb; float weight = 1;
                    [unroll] for (int k = 0; k < 4; k++)
                    {
                        float2 p = saturate(uv + offsets[k] * pixel);
                        float sampleRaw = SampleSceneDepth(p);
                        float sampleDepth = LinearEyeDepth(sampleRaw, _ZBufferParams);
                        float centerDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                        float w = saturate(1.0 - abs(sampleDepth-centerDepth) / max(.1, centerDepth*.05));
                        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, p).rgb * w; weight += w;
                    }
                    scene.rgb = sum / weight;
                }
                if (hasGeometry)
                    scene.rgb = lerp(scene.rgb, _PSXFogColor.rgb, saturate(Curve(fogDistance, _PSXFogColorRange, 0)));
                if (_PSXFogVolumeParams.x > .00001 && _PSXFogVolumeParams.y > .00001 && dist > .001)
                {
                    float lengthRay = min(dist, _PSXFogNoise.z);
                    int steps = clamp((int)_PSXFogNoise.w, 2, 16);
                    float stepSize = lengthRay / steps;
                    float3 direction = ray / dist;
                    float accumulated = 0;
                    [loop] for (int i = 0; i < steps; i++)
                    {
                        float3 p = _WorldSpaceCameraPos + direction * ((i + .5) * stepSize);
                        float h = (p.y - _PSXFogVolumeParams.z) / max(.01, _PSXFogVolumeParams.w);
                        float limited = smoothstep(0, .15, h) * (1.0 - smoothstep(.5, 1, h));
                        float heightLimit = lerp(_PSXFogVolumeHeight.x, _PSXFogVolumeHeight.y, _PSXFogVolumeHeight.z);
                        float layer = lerp(1.0, limited, heightLimit);
                        float n = Noise((p - _PSXFogWind.xyz * _PSXFogAnimationTime) * _PSXFogNoise.x);
                        accumulated += layer * lerp(1.0, n * 2.0, _PSXFogNoise.y) * stepSize;
                    }
                    float opacity = min(_PSXFogVolumeParams.y, 1.0 - exp(-accumulated * _PSXFogVolumeParams.x));
                    opacity *= saturate(Curve(fogDistance, _PSXFogVolumeRange, 2));
                    scene.rgb = lerp(scene.rgb, _PSXFogVolumeColor.rgb, saturate(opacity));
                }
                return scene;
            }
            ENDHLSL
        }
    }
}

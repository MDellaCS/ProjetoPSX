// Shader de iluminacao retro do Projeto PSX (late PS1 / PS2 / early PS3).
// Luz ambiente hemisferica + luz direcional da URP + ate 32 luzes locais do
// RetroLightSystem, calculadas por vertice (Gouraud), em grade de mundo ou por pixel.
Shader "Projeto PSX/Retro Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Toggle] _UseVertexColor("Use Vertex Color", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}

        [Enum(Vertex, 0, Grid, 1, Pixel, 2)] _LightingMode("Lighting Mode", Float) = 2
        [Enum(Global, 0, Vertex, 1, Grid, 2, Pixel, 3)] _LightingModeOverride("Lighting Mode Override", Float) = 0
        [HideInInspector] _RendererLightingModeOverride("Renderer Lighting Mode Override", Float) = 0
        _GridSize("Grid Size", Range(0.05, 4)) = 0.5
        [Toggle] _FlatShading("Flat Shading", Float) = 0
        _MetallicShine("Metallic Shine", Range(0, 1)) = 0
        _ShineSharpness("Shine Sharpness", Range(8, 128)) = 48
        _ShineLimit("Shine Limit", Range(0, 8)) = 8
        [Enum(Off, 0, Light, 1, Dark, 2)] _ReactiveGlowMode("Reactive Glow Mode", Float) = 0
        _ReactiveGlowColor("Reactive Glow Color", Color) = (1, 1, 1, 1)
        _ReactiveGlowMax("Reactive Glow Max", Range(0, 8)) = 1
        _ReactiveGlowThreshold("Reactive Glow Threshold", Range(0.01, 8)) = 1

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "Queue" = "Geometry"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap);
        SAMPLER(sampler_EmissionMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half4 _ReactiveGlowColor;
            half _UseVertexColor;
            half _LightingMode;
            half _LightingModeOverride;
            half _RendererLightingModeOverride;
            half _GridSize;
            half _FlatShading;
            half _MetallicShine;
            half _ShineSharpness;
            half _ShineLimit;
            half _ReactiveGlowMode;
            half _ReactiveGlowMax;
            half _ReactiveGlowThreshold;
            half _AlphaClip;
            half _Cutoff;
            half _Cull;
        CBUFFER_END

        void RetroAlphaClip(float2 uv)
        {
            #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
            #endif
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex RetroVert
            #pragma fragment RetroFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RetroLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 vertexLocal : TEXCOORD3;              // rgb luzes locais (modo Vertex)
                half3 vertexMain : TEXCOORD4;               // luz direcional (modo Vertex)
                nointerpolation half3 flatLocal : TEXCOORD5; // mesma luz, sem interpolar (Flat Shading)
                nointerpolation half3 flatMain : TEXCOORD6;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings RetroVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                half3 normalWS = (half3)TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;

                // Gouraud: a luz e calculada so nos vertices e interpolada no poligono.
                if (RetroResolveLightingMode(_LightingMode, _LightingModeOverride, _RendererLightingModeOverride) < 0.5)
                {
                    half3 local = RetroLocalLights(position.positionWS, normalWS);
                    half3 main = RetroMainLight(normalWS);
                    output.vertexLocal = half4(local, 0);
                    output.vertexMain = main;
                    output.flatLocal = local;
                    output.flatMain = main;
                }
                return output;
            }

            half4 RetroFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                if (_UseVertexColor > 0.5)
                    albedo *= input.color;
                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                half3 normalWS = normalize(input.normalWS);
                bool flatShading = _FlatShading > 0.5;
                if (flatShading)
                {
                    // Normal da face, derivada da posicao: cada triangulo recebe um tom unico.
                    float3 faceNormal = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));
                    normalWS = dot(faceNormal, normalWS) < 0.0 ? -faceNormal : faceNormal;
                }

                float mode = RetroResolveLightingMode(_LightingMode, _LightingModeOverride, _RendererLightingModeOverride);
                float3 samplePosition = input.positionWS;
                half3 local;
                half3 main;
                if (mode < 0.5)
                {
                    local = flatShading ? input.flatLocal : input.vertexLocal.rgb;
                    main = flatShading ? input.flatMain : input.vertexMain;
                }
                else
                {
                    if (mode < 1.5)
                        samplePosition = RetroSnapToGrid(input.positionWS, normalWS, RetroResolveGridSize(_GridSize));
                    local = RetroLocalLights(samplePosition, normalWS);
                    main = RetroMainLight(normalWS);
                }

                half shadow = 1.0;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(samplePosition));
                #endif

                half3 ambient = RetroAmbient(normalWS);
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor occlusion = GetScreenSpaceAmbientOcclusion(
                        GetNormalizedScreenSpaceUV(input.positionCS));
                    ambient *= occlusion.indirectAmbientOcclusion;
                    main *= occlusion.directAmbientOcclusion;
                    local *= occlusion.directAmbientOcclusion;
                #endif

                half3 shine = 0;
                if (_MetallicShine > 0.0)
                {
                    half3 viewDir = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                    half3 localShine;
                    RetroLocalLights(samplePosition, normalWS, viewDir, _ShineSharpness, localShine);
                    #if defined(_SCREEN_SPACE_OCCLUSION)
                        localShine *= occlusion.directAmbientOcclusion;
                    #endif
                    shine = localShine * (_MetallicShine * 0.35);
                    half peak = max(shine.r, max(shine.g, shine.b));
                    shine *= min(1.0h, _ShineLimit / max(peak, 0.0001h));
                }

                half3 emissionMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                half3 emission = emissionMask * _EmissionColor.rgb;
                if (_ReactiveGlowMode > 0.5)
                {
                    // Apenas luzes locais do RetroLightSystem controlam a emissao reativa.
                    half brightness = dot(local, half3(0.299, 0.587, 0.114));
                    half response = saturate(brightness / max(_ReactiveGlowThreshold, 0.01h));
                    if (_ReactiveGlowMode > 1.5)
                        response = 1.0h - response;
                    emission += emissionMask * _ReactiveGlowColor.rgb * (_ReactiveGlowMax * response);
                }
                half3 color = albedo.rgb * (ambient + main * shadow + local) + emission + shine;
                color = RetroQuantize(color, input.positionCS.xy);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ReactiveGlowMask"
            Tags { "LightMode" = "ReactiveGlowMask" }
            Cull [_Cull]
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GlowVert
            #pragma fragment GlowFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RetroLighting.hlsl"

            struct GlowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct GlowVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half3 vertexLocal : TEXCOORD3;
                nointerpolation half3 flatLocal : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            GlowVaryings GlowVert(GlowAttributes input)
            {
                GlowVaryings output = (GlowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                if (RetroResolveLightingMode(_LightingMode, _LightingModeOverride, _RendererLightingModeOverride) < 0.5)
                {
                    output.vertexLocal = RetroLocalLights(output.positionWS, output.normalWS);
                    output.flatLocal = output.vertexLocal;
                }
                return output;
            }
            half4 GlowFrag(GlowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                RetroAlphaClip(input.uv);
                if (_ReactiveGlowMode < 0.5) return 0;
                half3 normalWS = normalize(input.normalWS);
                bool flat = _FlatShading > 0.5;
                if (flat)
                {
                    float3 faceNormal = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));
                    normalWS = dot(faceNormal, normalWS) < 0.0 ? -faceNormal : faceNormal;
                }
                float mode = RetroResolveLightingMode(_LightingMode, _LightingModeOverride, _RendererLightingModeOverride);
                half3 local;
                if (mode < 0.5)
                    local = flat ? input.flatLocal : input.vertexLocal;
                else
                {
                    float3 samplePosition = mode < 1.5 ? RetroSnapToGrid(input.positionWS, normalWS, RetroResolveGridSize(_GridSize)) : input.positionWS;
                    local = RetroLocalLights(samplePosition, normalWS);
                }
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    local *= GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS)).directAmbientOcclusion;
                #endif
                half brightness = dot(local, half3(0.299, 0.587, 0.114));
                half response = saturate(brightness / max(_ReactiveGlowThreshold, 0.01h));
                if (_ReactiveGlowMode > 1.5) response = 1.0h - response;
                half3 mask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                return half4(mask * _ReactiveGlowColor.rgb * (_ReactiveGlowMax * response), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(positionCS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                RetroAlphaClip(input.uv);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                RetroAlphaClip(input.uv);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            void DepthNormalsFrag(Varyings input
                , out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                RetroAlphaClip(input.uv);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float3 normalWS = normalize(input.normalWS);
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    outNormalWS = half4(PackFloat2To888(remappedOctNormalWS), 0.0);
                #else
                    outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
                #endif
                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
            }
            ENDHLSL
        }
    }

    CustomEditor "RetroLitShaderGUI"
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

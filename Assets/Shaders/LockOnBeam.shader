Shader "ProjetoPSX/LockOn Beam"
{
    Properties { _BeamColor("Beam Color", Color) = (1,1,1,.12) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Beam"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BeamColor;
            CBUFFER_END
            float4 _LockSpotProtectedMin;
            float4 _LockSpotProtectedMax;
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float depth : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.depth = input.positionOS.z;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                bool hasDepth = depth > 0.00001;
                #else
                bool hasDepth = depth < 0.99999;
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
                #endif
                if (hasDepth && _LockSpotProtectedMin.w > 0.5)
                {
                    float3 scenePoint = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                    if (all(scenePoint >= _LockSpotProtectedMin.xyz) &&
                        all(scenePoint <= _LockSpotProtectedMax.xyz))
                        discard;
                }
                half alpha = _BeamColor.a * smoothstep(0.0, .08, input.depth) * (1.0 - smoothstep(.8, 1.0, input.depth));
                return half4(_BeamColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}

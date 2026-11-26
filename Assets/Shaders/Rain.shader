Shader "ProjetoPSX/Rain"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _RainSafeRadius, _RainSafeFade;
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz); output.positionCS = TransformWorldToHClip(output.positionWS); output.color = input.color; output.uv = input.uv; return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half across = saturate(1.0h - abs(input.uv.x * 2.0h - 1.0h));
                half along = saturate(min(input.uv.y, 1.0h - input.uv.y) * 6.0h);
                float safe = 1.0;
                if (_RainSafeRadius > 0.0)
                    safe = saturate((distance(input.positionWS, _WorldSpaceCameraPos) - _RainSafeRadius) / max(_RainSafeFade, 0.0001));
                return half4(input.color.rgb, input.color.a * across * along * safe);
            }
            ENDHLSL
        }
    }
}

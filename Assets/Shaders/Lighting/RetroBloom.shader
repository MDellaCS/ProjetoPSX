Shader "Hidden/Projeto PSX/Retro Bloom"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            Blend One One
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float _RetroBloomIntensity;
            float _RetroBloomRadius;
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 d = _BlitTexture_TexelSize.xy * _RetroBloomRadius;
                half3 glow = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb * 0.2;
                glow += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(d.x, 0)).rgb * 0.1;
                glow += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(d.x, 0)).rgb * 0.1;
                glow += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, d.y)).rgb * 0.1;
                glow += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0, d.y)).rgb * 0.1;
                glow += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + d).rgb * 0.1;
                glow += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - d).rgb * 0.1;
                glow += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(d.x, -d.y)).rgb * 0.1;
                glow += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x, d.y)).rgb * 0.1;
                return half4(glow * _RetroBloomIntensity, 0);
            }
            ENDHLSL
        }
    }
}

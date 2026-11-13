Shader "Hidden/ProjetoPSX/Global Retro Pixelation"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "GlobalRetroPixelation"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _GlobalRetroParams; // ligado, pixelizacao aparente, altura, niveis
            float4 _GlobalRetroStyle; // dithering, intensidade de cor, intensidade de pixelizacao

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);
                if (_GlobalRetroParams.x < 0.5)
                    return color;

                float height = max(1.0, _GlobalRetroParams.z);
                float2 grid = float2(max(1.0, round(height * _ScreenParams.x / max(_ScreenParams.y, 1.0))), height);
                float2 pixelUv = (floor(uv * grid) + 0.5) / grid;
                if (_GlobalRetroParams.y > 0.5 && _GlobalRetroStyle.z > 0.0)
                {
                    half4 pixel = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, pixelUv, _BlitMipLevel);
                    color = lerp(color, pixel, _GlobalRetroStyle.z);
                }

                if (_GlobalRetroStyle.y > 0.0)
                {
                    float2 p = floor(uv * grid);
                    float x = fmod(p.x, 4.0), y = fmod(p.y, 4.0);
                    float d = (y < 1.0) ? ((x < 1.0) ? -4.0 : (x < 2.0) ? 0.0 : (x < 3.0) ? -3.0 : 1.0) :
                              (y < 2.0) ? ((x < 1.0) ? 2.0 : (x < 2.0) ? -2.0 : (x < 3.0) ? 3.0 : -1.0) :
                              (y < 3.0) ? ((x < 1.0) ? -3.0 : (x < 2.0) ? 1.0 : (x < 3.0) ? -4.0 : 0.0) :
                                          ((x < 1.0) ? 3.0 : (x < 2.0) ? -1.0 : (x < 3.0) ? 2.0 : -2.0);
                    float levels = max(2.0, _GlobalRetroParams.w);
                    float3 encoded = saturate(color.rgb);
                    #ifndef UNITY_COLORSPACE_GAMMA
                    encoded = LinearToSRGB(encoded);
                    #endif
                    float3 quantized = floor(saturate(encoded + d * _GlobalRetroStyle.x / (8.0 * (levels - 1.0))) * (levels - 1.0) + 0.5) / (levels - 1.0);
                    #ifndef UNITY_COLORSPACE_GAMMA
                    quantized = SRGBToLinear(quantized);
                    #endif
                    color.rgb = lerp(color.rgb, quantized, _GlobalRetroStyle.y);
                }
                return color;
            }
            ENDHLSL
        }
    }
}

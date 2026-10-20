Shader "Hidden/ProjetoPSX/Heat Peripheral Blur"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "HeatPeripheralBlur"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Alimentado por CameraGameplayEffects. Nao e uma propriedade do
            // material para que todas as cameras usem o mesmo estado do Heat.
            float _HeatPeripheralBlur;

            half4 SampleScene(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                    saturate(uv), _BlitMipLevel);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 fromCenter = uv * 2.0 - 1.0;
                // O blur comeca fora da regiao central, preservando leitura
                // de combate e focando somente a visao periferica.
                float peripheral = smoothstep(0.35, 1.25,
                    length(fromCenter));
                float blurAmount = max(0.0, _HeatPeripheralBlur) * peripheral;
                float strength = saturate(blurAmount);
                float2 texel = _BlitTexture_TexelSize.xy;
                // O deslocamento cresce mais que linearmente para que o
                // valor 1 ja seja perceptivel em resolucoes altas. A mistura
                // continua limitada a 1, mas valores acima de 1 aumentam o
                // raio, nao apenas a opacidade do efeito.
                float2 blurOffset = texel * (2.0 + blurAmount * 14.0) *
                    blurAmount;

                half4 color = SampleScene(uv) * 0.28;
                color += SampleScene(uv + float2(blurOffset.x, 0.0)) * 0.12;
                color += SampleScene(uv - float2(blurOffset.x, 0.0)) * 0.12;
                color += SampleScene(uv + float2(0.0, blurOffset.y)) * 0.12;
                color += SampleScene(uv - float2(0.0, blurOffset.y)) * 0.12;
                color += SampleScene(uv + blurOffset) * 0.06;
                color += SampleScene(uv - blurOffset) * 0.06;
                color += SampleScene(uv + float2(blurOffset.x, -blurOffset.y)) * 0.06;
                color += SampleScene(uv + float2(-blurOffset.x, blurOffset.y)) * 0.06;

                return lerp(SampleScene(uv), color, strength);
            }
            ENDHLSL
        }
    }
}

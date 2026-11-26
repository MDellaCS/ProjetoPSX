Shader "Hidden/ProjetoPSX/LockOn Darkness"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "LockOnDarkness"
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _LockSpotPosition;
            float4 _LockSpotParameters;
            float4 _LockSpotColor;
            float4 _LockSpotProtectedMin;
            float4 _LockSpotProtectedMax;

            // Quando a camera esta dentro do cone, suas faces externas sao
            // descartadas. Calcula o trecho visivel sem desenhar essas faces.
            float BeamFromInside(float3 cameraPosition, float3 rayDirection, float maxDistance)
            {
                float3 offset = cameraPosition - _LockSpotPosition.xyz;
                float height = -offset.y;
                float tangent = _LockSpotParameters.y;
                float range = _LockSpotPosition.w;
                if (height < 0.0 || height > range || length(offset.xz) > height * tangent)
                    return 0.0;

                float tangentSquared = tangent * tangent;
                float a = dot(rayDirection.xz, rayDirection.xz) - rayDirection.y * rayDirection.y * tangentSquared;
                float b = 2.0 * (dot(offset.xz, rayDirection.xz) - offset.y * rayDirection.y * tangentSquared);
                float c = dot(offset.xz, offset.xz) - offset.y * offset.y * tangentSquared;
                float end = maxDistance;
                float verticalDirection = -rayDirection.y;
                if (verticalDirection > 0.00001)
                    end = min(end, (range - height) / verticalDirection);
                else if (verticalDirection < -0.00001)
                    end = min(end, -height / verticalDirection);

                if (abs(a) < 0.00001)
                {
                    if (b > 0.00001) end = min(end, -c / b);
                }
                else
                {
                    float discriminant = b * b - 4.0 * a * c;
                    if (discriminant >= 0.0)
                    {
                        float root = sqrt(discriminant);
                        float first = (-b - root) / (2.0 * a);
                        float second = (-b + root) / (2.0 * a);
                        if (first > 0.0) end = min(end, first);
                        if (second > 0.0) end = min(end, second);
                    }
                }
                return saturate(max(0.0, end) * 0.5) * saturate(_LockSpotParameters.w);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_LockSpotColor.a <= 0.0) return color;
                float rawDepth = SampleSceneDepth(uv);
                float focus = 0.0;
                #if UNITY_REVERSED_Z
                bool hasDepth = rawDepth > 0.00001;
                #else
                bool hasDepth = rawDepth < 0.99999;
                rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif
                float3 world = 0.0;
                bool protectedTarget = false;
                if (hasDepth)
                {
                    world = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                    protectedTarget = _LockSpotProtectedMin.w > 0.5 &&
                        all(world >= _LockSpotProtectedMin.xyz) && all(world <= _LockSpotProtectedMax.xyz);
                    float vertical = _LockSpotPosition.y - world.y;
                    float radius = max(0.0, vertical) * _LockSpotParameters.y;
                    float horizontal = distance(world.xz, _LockSpotPosition.xz);
                    float softness = max(.01, _LockSpotParameters.z);
                    focus = (1.0 - smoothstep(radius, radius + softness, horizontal));
                    focus *= smoothstep(-softness, 0.0, vertical);
                    focus *= 1.0 - smoothstep(_LockSpotPosition.w, _LockSpotPosition.w + softness, vertical);
                }
                color.rgb *= 1.0 - saturate(_LockSpotParameters.x) * (1.0 - focus);
                float tint = saturate(focus) * saturate(_LockSpotColor.a);
                color.rgb = lerp(color.rgb, color.rgb * _LockSpotColor.rgb, tint);

                if (!protectedTarget && _LockSpotParameters.w > 0.0)
                {
                    #if UNITY_REVERSED_Z
                    float farDepth = 0.00001;
                    #else
                    float farDepth = 0.99999;
                    #endif
                    float3 endpoint = hasDepth ? world : ComputeWorldSpacePosition(uv, farDepth, UNITY_MATRIX_I_VP);
                    float3 ray = endpoint - _WorldSpaceCameraPos;
                    float rayLength = length(ray);
                    if (rayLength > 0.0001)
                    {
                        float beam = BeamFromInside(_WorldSpaceCameraPos, ray / rayLength, rayLength);
                        color.rgb = lerp(color.rgb, _LockSpotColor.rgb, beam);
                    }
                }
                return color;
            }
            ENDHLSL
        }
    }
}

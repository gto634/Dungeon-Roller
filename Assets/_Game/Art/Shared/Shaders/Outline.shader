
Shader "Pattern Rendering/Outline"
{
    Properties
    {
        [NoScaleOffset] _PatternAtlas ("Atlas (pour la palette)", 2D) = "white" {}

        [HideInInspector] _Thickness ("Ancienne épaisseur globale", Float) = 3
        [HideInInspector] _SilhouetteSensitivity ("Ancienne sensibilité globale", Float) = 0.5
        [HideInInspector] _DrawCreases ("Anciennes arêtes globales", Float) = 1
        [HideInInspector] _CreaseAngle ("Ancien angle global", Float) = 40
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off
        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_PatternAtlas);
            float4 _PatternAtlas_TexelSize;
            #include "PatternAtlas.hlsl"
            TEXTURE2D_X(_PatternOutlineNormals);
            TEXTURE2D_X(_PatternOutlineStyle);
            TEXTURE2D_X(_PatternOutlineColor);
            float4 _PatternOutlineSize;

            int2 ClampPixel(int2 p) { return clamp(p, int2(0, 0), int2(_PatternOutlineSize.zw) - 1); }
            float4 NormalAndCode(int2 p) { return LOAD_TEXTURE2D_X(_PatternOutlineNormals, ClampPixel(p)); }



            float SurfaceDepth(int2 p)
            {
                float raw = LoadSceneDepth(uint2(ClampPixel(p)));
                if (unity_OrthoParams.w > 0.5)
                {
                    #if UNITY_REVERSED_Z
                    return -lerp(_ProjectionParams.z, _ProjectionParams.y, raw);
                    #else
                    return -lerp(_ProjectionParams.y, _ProjectionParams.z, raw);
                    #endif
                }
                return _ZBufferParams.z * raw + _ZBufferParams.w;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                int2 p = int2(input.positionCS.xy);
                half4 color = LOAD_TEXTURE2D_X(_BlitTexture, uint2(p));
                float4 style = LOAD_TEXTURE2D_X(_PatternOutlineStyle, p);
                if (style.x <= 0) return color;

                float4 normalAndCode = NormalAndCode(p);
                float code = floor(normalAndCode.a);
                if (code < 1) return color;

                float3 n0 = normalize(normalAndCode.xyz);
                float c = SurfaceDepth(p);
                float scale = unity_OrthoParams.w > 0.5 ? 1 : max(abs(c), 1e-6);
                float threshold = 0.2 * pow(0.01, saturate(style.y)) * scale;
                float width = clamp(style.x, 0.5, 8);
                float softness = clamp(style.w, 0, 1.5);
                float radius = width + softness * 0.5 + 0.5;
                float closest = 100;
                float creaseCos = cos(radians(max(style.z, 0)));
                int2 directions[4] = { int2(1, 0), int2(0, 1), int2(1, 1), int2(1, -1) };

                [unroll]
                for (int d = 0; d < 4; d++)
                {
                    float stepLength = d < 2 ? 1 : 1.41421356;

                    [loop]
                    for (int step = 1; step <= 9; step++)
                    {
                        float distance = step * stepLength;
                        if (distance > radius || distance - 0.5 >= closest) break;
                        int2 offset = directions[d] * step;
                        float a = SurfaceDepth(p - offset);
                        float b = SurfaceDepth(p + offset);
                        bool silhouette = abs(a + b - 2 * c) > threshold
                            && c > min(a, b) + threshold * 0.5;

                        float3 nextNormal = NormalAndCode(p + offset).xyz;
                        bool hasNormal = dot(nextNormal, nextNormal) > 0.01;
                        bool crease = style.z >= 0 && hasNormal
                            && abs(b - c) < 0.05 * scale
                            && dot(n0, normalize(nextNormal)) < creaseCos;
                        if (silhouette || crease)
                        {
                            closest = min(closest, distance - 0.5);
                            break;
                        }

                    }
                }

                float coverage = softness > 0.001
                    ? 1 - smoothstep(width - softness * 0.5, width + softness * 0.5, closest)
                    : step(closest, width);
                float3 ink = LOAD_TEXTURE2D_X(_PatternOutlineColor, p).rgb;
                return half4(lerp(color.rgb, ink, coverage), color.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

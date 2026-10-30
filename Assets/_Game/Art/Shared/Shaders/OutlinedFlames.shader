Shader "Pattern Rendering/Outlined Flames"
{
    Properties
    {
        [MainTexture] _MainTex ("Flame mask (red)", 2D) = "white" {}
        _DistortTex ("Flame distortion", 2D) = "gray" {}
        _DistortScrolling ("Distortion scroll (XY), scale (ZW)", Vector) = (0,-1,1,1)
        _Distort ("Distortion amount", Range(0,1)) = 0.3
        _HdrMultiply ("Flame brightness", Range(0,10)) = 8
        _OutlineColor ("Outline color", Color) = (0.045,0.012,0.005,1)
        _OutlineWidth ("Outline width (pixels)", Range(0,3)) = 1.2
        _Cutoff ("Mask threshold", Range(0.01,0.9)) = 0.1
        [Toggle] _UseEdgeFade ("Fade grazing surfaces", Float) = 1
        _SoftParticleDistance ("Intersection fade distance", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "Flames"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_DistortTex); SAMPLER(sampler_DistortTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _DistortScrolling;
                half4 _OutlineColor;
                float _OutlineWidth;
                half _Distort, _HdrMultiply, _Cutoff, _UseEdgeFade, _SoftParticleDistance;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color;
                if (_UseEdgeFade > 0.5h)
                    output.color.a *= saturate(abs(dot(GetWorldSpaceNormalizeViewDir(positionWS), TransformObjectToWorldNormal(input.normalOS))));
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 distortionUV = input.uv * _DistortScrolling.zw + input.uv + frac(_DistortScrolling.xy * _Time.yy);
                float2 distortion = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortionUV).rg;
                float2 uv = lerp(input.uv, distortion, _Distort * input.uv.y * 0.5h);
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r * input.color.a;
                half pixel = max(fwidth(alpha), 0.002h);
                half feather = pixel * 0.5h;
                half coverage = smoothstep(_Cutoff - feather, _Cutoff + feather, alpha);
                half innerThreshold = _Cutoff + pixel * _OutlineWidth;
                half border = 1.0h - smoothstep(innerThreshold - feather, innerThreshold + feather, alpha);
                border *= step(0.001h, _OutlineWidth) * _OutlineColor.a;
                half3 color = lerp(input.color.rgb * _HdrMultiply, _OutlineColor.rgb, border);
                alpha = coverage * lerp(alpha, input.color.a, border);
                if (_SoftParticleDistance > 0.0h)
                {
                    float depth = SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS));
                    float sceneZ = unity_OrthoParams.w == 0 ? LinearEyeDepth(depth, _ZBufferParams) : LinearDepthToEyeDepth(depth);
                    float thisZ = unity_OrthoParams.w == 0 ? LinearEyeDepth(input.positionCS.z, _ZBufferParams) : LinearDepthToEyeDepth(input.positionCS.z);
                    alpha *= saturate((sceneZ - thisZ) / _SoftParticleDistance);
                }
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

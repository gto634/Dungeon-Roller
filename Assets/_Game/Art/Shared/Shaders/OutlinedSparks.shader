Shader "Pattern Rendering/Outlined Sparks"
{
    Properties
    {
        [MainTexture] _BaseMap ("Spark mask (red)", 2D) = "white" {}
        [MainColor] _BaseColor ("Spark color", Color) = (1,0.38,0.035,1)
        _OutlineColor ("Outline color", Color) = (0.065,0.018,0.008,1)
        _OutlineWidth ("Outline width (pixels)", Range(0,3)) = 0.8
        _Cutoff ("Mask threshold", Range(0.01,0.9)) = 0.12
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "Sparks"
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

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _OutlineColor;
                float _OutlineWidth;
                half _Cutoff;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
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
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color * _BaseColor;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half mask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).r;

                half pixel = max(fwidth(mask), 0.002h);
                half feather = pixel * 0.5h;
                half coverage = smoothstep(_Cutoff - feather, _Cutoff + feather, mask);
                half innerThreshold = _Cutoff + pixel * _OutlineWidth;
                half border = 1.0h - smoothstep(innerThreshold - feather, innerThreshold + feather, mask);
                border *= step(0.001h, _OutlineWidth) * _OutlineColor.a;
                half3 color = lerp(input.color.rgb, _OutlineColor.rgb, border);
                return half4(saturate(color), coverage * input.color.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

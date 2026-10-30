Shader "Pattern Rendering/Confetti Outline"
{
    Properties
    {
        [MainTexture] _BaseMap ("Particle texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline width (pixels)", Range(0,3)) = 1.25
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "Confetti"
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
            half AlphaAt(float2 uv)
            {

                if (any(uv < 0.0) || any(uv > 1.0)) return 0;
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = textureColor * input.color;
                float2 x = ddx(input.uv) * _OutlineWidth;
                float2 y = ddy(input.uv) * _OutlineWidth;
                half minimum = min(min(AlphaAt(input.uv+x), AlphaAt(input.uv-x)), min(AlphaAt(input.uv+y), AlphaAt(input.uv-y)));
                minimum = min(minimum, min(min(AlphaAt(input.uv+(x+y)*.7071), AlphaAt(input.uv-(x+y)*.7071)),
                    min(AlphaAt(input.uv+(x-y)*.7071), AlphaAt(input.uv-(x-y)*.7071))));
                half edge = 1.0h - smoothstep(.45h, .95h, minimum / max(textureColor.a, .001h));
                edge *= step(.001, _OutlineWidth) * _OutlineColor.a;
                color.rgb = lerp(color.rgb, _OutlineColor.rgb, edge);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}

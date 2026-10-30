Shader "Pattern Rendering/UI Pattern"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _PatternRegion ("Pattern region in source pixels", Vector) = (2,2,3,3)
        _PatternPixelSize ("Pattern pixel size at reference resolution", Float) = 4
        _ReferenceResolution ("Canvas reference resolution", Vector) = (1920,1080,0,0)
        _MatchWidthOrHeight ("Canvas width / height match", Range(0,1)) = 0.5
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "ContinuousPattern"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                half4 mask : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect, _PatternRegion, _ReferenceResolution;
            float _PatternPixelSize, _MatchWidthOrHeight;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.screenPosition = ComputeScreenPos(output.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                float2 pixelSize = output.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                output.mask = half4(input.vertex.xy * 2 - rect.xy - rect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;

                half isPattern = step(0.001, max(sprite.r, max(sprite.g, sprite.b)));
                float2 ratios = _ScreenParams.xy / max(_ReferenceResolution.xy, float2(1,1));
                float canvasScale = exp2(lerp(log2(ratios.x), log2(ratios.y), _MatchWidthOrHeight));
                float pixelSize = max(1, floor(_PatternPixelSize * canvasScale + 0.5));
                float2 screenPixels = input.screenPosition.xy / input.screenPosition.w * _ScreenParams.xy;
                float2 patternPixels = floor(screenPixels / pixelSize);
                float2 period = max(_PatternRegion.zw, float2(1,1));


                float2 sourcePixel = _PatternRegion.xy + 0.5;
                fixed3 inkA = tex2D(_MainTex, sourcePixel * _MainTex_TexelSize.xy).rgb;
                fixed3 inkB = tex2D(_MainTex, (sourcePixel + float2(1, 0)) * _MainTex_TexelSize.xy).rgb;
                fixed3 inkC = tex2D(_MainTex, (sourcePixel + float2(2, 0)) * _MainTex_TexelSize.xy).rgb;
                float diagonal = patternPixels.x - patternPixels.y;
                float phase = diagonal - floor(diagonal / period.x) * period.x;
                half lightInk = step(0.5, phase);
                fixed3 ink = lerp(min(inkA, min(inkB, inkC)), max(inkA, max(inkB, inkC)), lightInk);
                fixed4 color = fixed4(lerp(sprite.rgb, ink, isPattern), sprite.a) * input.color;
                #ifdef UNITY_UI_CLIP_RECT
                half2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                color.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}

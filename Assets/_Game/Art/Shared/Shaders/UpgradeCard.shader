Shader "Pattern Rendering/Upgrade Card"
{
    Properties
    {
        [PerRendererData] _MainTex ("Card sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StatColor ("Stat color", Color) = (1,0.3,0.3,1)
        _RarityColor ("Rarity color", Color) = (1,0.7,0.2,1)
        _InnerInset ("Interior inset in sprite pixels", Float) = 5
        _Darkness ("Border grey brightness", Range(0,1)) = 0.54
        _StripeBrightness ("Interior stripe brightness", Range(0,1)) = 0.90
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
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; half4 mask:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _ClipRect;
            fixed4 _Color, _StatColor, _RarityColor, _TextureSampleAdd;
            float _InnerInset, _Darkness, _StripeBrightness, _UIMaskSoftnessX, _UIMaskSoftnessY;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color;
                float2 pixelSize = o.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = half4(v.vertex.xy * 2 - rect.xy - rect.zw, .25 / (.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 source = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float2 edgeDistance = min(i.uv, 1 - i.uv) * _MainTex_TexelSize.zw;
                half interior = step(_InnerInset, min(edgeDistance.x, edgeDistance.y));
                fixed4 tint = lerp(_RarityColor, _StatColor, interior);
                half brightInk = step(.8, source.r);
                half coloredInk = step(.05, source.r);
                half inkBrightness = lerp(_Darkness, _StripeBrightness, interior);
                fixed3 rgb = tint.rgb * lerp(inkBrightness, 1, brightInk) * coloredInk;
                fixed4 color = fixed4(rgb, source.a * tint.a) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                half2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - .001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}

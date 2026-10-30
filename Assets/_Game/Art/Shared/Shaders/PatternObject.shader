Shader "Pattern Rendering/Pattern Object"
{
    Properties
    {
        _PaletteColor ("Couleur (0-8)", Float) = 1
        [HideInInspector] _PaletteCustomColor ("Couleur libre : Palette", Color) = (1, 1, 1, 1)
        _PaletteDarkness ("Assombrissement de la couleur", Range(0, 1)) = 0
        _PatternBand ("Ligne de motif (0-6)", Float) = 0
        _ShadingMode ("Mode : 0 couches, 1 dégradé, 2 uni", Float) = 0
        _Brightness ("Luminosité de la zone éclairée", Range(0, 8)) = 8
        _InkColor ("Couleur de l'encre (0-8, 9 = auto)", Float) = 9
        [HideInInspector] _InkCustomColor ("Couleur libre : Ink", Color) = (1, 1, 1, 1)
        _InkDarkness ("Assombrissement des pixels sombres", Range(0, 1)) = 0
        _ShadowDarkness ("Assombrissement de la couleur auto", Range(0, 1)) = 0.5

        _ShadowLayers ("Nombre de couches", Range(0, 6)) = 2
        _Shadow1Size ("Couche 1 : taille", Range(0, 1)) = 0.5
        _Shadow1Color ("Couche 1 : couleur", Float) = 9
        [HideInInspector] _Shadow1CustomColor ("Couleur libre : Shadow1", Color) = (1, 1, 1, 1)
        _Shadow1Darkness ("Couche 1 : assombrissement", Range(0, 1)) = 0
        _Shadow1Brightness ("Couche 1 : luminosité", Range(0, 8)) = 5
        _Shadow1Band ("Couche 1 : motif (7 = comme l'objet)", Float) = 7
        _Shadow2Size ("Couche 2 : taille", Range(0, 1)) = 0.2
        _Shadow2Color ("Couche 2 : couleur", Float) = 9
        [HideInInspector] _Shadow2CustomColor ("Couleur libre : Shadow2", Color) = (1, 1, 1, 1)
        _Shadow2Darkness ("Couche 2 : assombrissement", Range(0, 1)) = 0
        _Shadow2Brightness ("Couche 2 : luminosité", Range(0, 8)) = 2
        _Shadow2Band ("Couche 2 : motif", Float) = 7
        _Shadow3Size ("Couche 3 : taille", Range(0, 1)) = 0.08
        _Shadow3Color ("Couche 3 : couleur", Float) = 0
        [HideInInspector] _Shadow3CustomColor ("Couleur libre : Shadow3", Color) = (1, 1, 1, 1)
        _Shadow3Darkness ("Couche 3 : assombrissement", Range(0, 1)) = 0
        _Shadow3Brightness ("Couche 3 : luminosité", Range(0, 8)) = 1
        _Shadow3Band ("Couche 3 : motif", Float) = 7
        _Shadow4Size ("Couche 4 : taille", Range(0, 1)) = 0.04
        _Shadow4Color ("Couche 4 : couleur", Float) = 0
        [HideInInspector] _Shadow4CustomColor ("Couleur libre : Shadow4", Color) = (1, 1, 1, 1)
        _Shadow4Darkness ("Couche 4 : assombrissement", Range(0, 1)) = 0
        _Shadow4Brightness ("Couche 4 : luminosité", Range(0, 8)) = 0
        _Shadow4Band ("Couche 4 : motif", Float) = 7
        _Shadow5Size ("Couche 5 : taille", Range(0, 1)) = 0.02
        _Shadow5Color ("Couche 5 : couleur", Float) = 0
        [HideInInspector] _Shadow5CustomColor ("Couleur libre : Shadow5", Color) = (1, 1, 1, 1)
        _Shadow5Darkness ("Couche 5 : assombrissement", Range(0, 1)) = 0
        _Shadow5Brightness ("Couche 5 : luminosité", Range(0, 8)) = 0
        _Shadow5Band ("Couche 5 : motif", Float) = 7
        _Shadow6Size ("Couche 6 : taille", Range(0, 1)) = 0.01
        _Shadow6Color ("Couche 6 : couleur", Float) = 0
        [HideInInspector] _Shadow6CustomColor ("Couleur libre : Shadow6", Color) = (1, 1, 1, 1)
        _Shadow6Darkness ("Couche 6 : assombrissement", Range(0, 1)) = 0
        _Shadow6Brightness ("Couche 6 : luminosité", Range(0, 8)) = 0
        _Shadow6Band ("Couche 6 : motif", Float) = 7

        [ToggleUI] _ReceiveCastShadows ("Recevoir les ombres portées", Float) = 1
        [ToggleUI] _CastShadowGroundOnly ("Ombres portées : sol uniquement", Float) = 1
        _CastShadowColor ("Ombres portées : couleur", Float) = 9
        [HideInInspector] _CastShadowCustomColor ("Couleur libre : CastShadow", Color) = (1, 1, 1, 1)
        _CastShadowDarkness ("Ombres portées : assombrissement", Range(0, 1)) = 0
        _CastShadowBand ("Ombres portées : motif (7 = comme l'objet)", Float) = 7
        _CastShadowBrightness ("Ombres portées : luminosité", Range(0, 8)) = 2
        _CastShadowOpacity ("Ombres portées : opacité", Range(0, 1)) = 1

        _Specular ("Reflet", Float) = 0
        _SpecularSize ("Taille du reflet", Range(0, 1)) = 0.3
        _SpecularDensity ("Densité du reflet", Range(0, 8)) = 3
        _SpecularBand ("Ligne de motif du reflet", Float) = 0
        _SpecularColor ("Couleur du reflet", Float) = 8
        [HideInInspector] _SpecularCustomColor ("Couleur libre : Specular", Color) = (1, 1, 1, 1)

        _SpecularDarkness ("Assombrissement du reflet", Range(0, 1)) = 0

        _Outline ("Contour", Float) = 1
        _OutlineColor ("Couleur du contour", Float) = 0
        [HideInInspector] _OutlineCustomColor ("Couleur libre : Outline", Color) = (0, 0, 0, 1)

        _OutlineDarkness ("Assombrissement du contour", Range(0, 1)) = 0
        _OutlineWidth ("Épaisseur du contour (pixels)", Range(0.5, 8)) = 3
        _OutlineDepthSensitivity ("Sensibilité des silhouettes", Range(0, 1)) = 0.5
        [ToggleUI] _OutlineDrawCreases ("Arêtes intérieures", Float) = 1
        _OutlineCreaseAngle ("Angle minimum des arêtes", Range(1, 180)) = 80
        _OutlineSoftness ("Lissage du contour (pixels)", Range(0, 1.5)) = 0.75

        _Projection ("Projection : 0 monde, 1 objet, 2 écran", Float) = 0
        _PatternDensity ("Pixels du motif par mètre", Float) = 48
        _ScreenPixelSize ("Taille d'un pixel à l'écran", Range(1, 8)) = 2

        [HideInInspector][NoScaleOffset] _TireTrackMask ("Tire track mask", 2D) = "black" {}
        [HideInInspector] _TireTracksEnabled ("Tire tracks enabled", Float) = 0
        [HideInInspector] _TireTracksRoomSize ("Room dimensions", Vector) = (1, 1, 0, 0)
        [HideInInspector] _TireTrackColor ("Tire track color", Color) = (0.025, 0.022, 0.02, 0.8)

        [NoScaleOffset] _PatternAtlas ("Atlas de motifs", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _PaletteCustomColor;
            float4 _InkCustomColor;
            float4 _Shadow1CustomColor;
            float4 _Shadow2CustomColor;
            float4 _Shadow3CustomColor;
            float4 _Shadow4CustomColor;
            float4 _Shadow5CustomColor;
            float4 _Shadow6CustomColor;
            float4 _CastShadowCustomColor;
            float4 _SpecularCustomColor;
            float4 _OutlineCustomColor;
            float _PaletteColor;
            float _PaletteDarkness;
            float _InkDarkness;
            float _PatternBand;
            float _ShadingMode;
            float _Brightness;
            float _InkColor;
            float _ShadowDarkness;
            float _ShadowLayers;
            float _Shadow1Size; float _Shadow1Color; float _Shadow1Darkness; float _Shadow1Brightness; float _Shadow1Band;
            float _Shadow2Size; float _Shadow2Color; float _Shadow2Darkness; float _Shadow2Brightness; float _Shadow2Band;
            float _Shadow3Size; float _Shadow3Color; float _Shadow3Darkness; float _Shadow3Brightness; float _Shadow3Band;
            float _Shadow4Size; float _Shadow4Color; float _Shadow4Darkness; float _Shadow4Brightness; float _Shadow4Band;
            float _Shadow5Size; float _Shadow5Color; float _Shadow5Darkness; float _Shadow5Brightness; float _Shadow5Band;
            float _Shadow6Size; float _Shadow6Color; float _Shadow6Darkness; float _Shadow6Brightness; float _Shadow6Band;
            float _ReceiveCastShadows;
            float _CastShadowGroundOnly;
            float _CastShadowColor;
            float _CastShadowDarkness;
            float _CastShadowBand;
            float _CastShadowBrightness;
            float _CastShadowOpacity;
            float _SpecularDarkness;
            float _OutlineDarkness;
            float _Specular;
            float _SpecularSize;
            float _SpecularDensity;
            float _SpecularBand;
            float _SpecularColor;
            float _Outline;
            float _OutlineColor;
            float _OutlineWidth;
            float _OutlineDepthSensitivity;
            float _OutlineDrawCreases;
            float _OutlineCreaseAngle;
            float _OutlineSoftness;
            float _Projection;
            float _PatternDensity;
            float _ScreenPixelSize;
            float4 _PatternAtlas_TexelSize;
            float4x4 _TireTracksWorldToRoom;
            float4 _TireTracksRoomSize;
            float4 _TireTrackColor;
            float _TireTracksEnabled;
        CBUFFER_END
        #include "PatternAtlas.hlsl"
        TEXTURE2D(_PatternAtlas);
        TEXTURE2D(_TireTrackMask);
        SAMPLER(sampler_TireTrackMask);

        float3 MaterialColor(float index, float4 customColor)
        {
            if (round(index) >= CUSTOM_COLOR) return customColor.rgb;
            float2 pixel = PatternPalettePixel(index);
            return LOAD_TEXTURE2D(_PatternAtlas, int2(pixel.x, _PatternAtlas_TexelSize.w - 1.0 - pixel.y)).rgb;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            static const float3 kLuminance = float3(0.2126, 0.7152, 0.0722);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 objectPos  : TEXCOORD2;
                float3 normalOS   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);

                float3 objectScale = float3(length(UNITY_MATRIX_M._m00_m10_m20),
                                            length(UNITY_MATRIX_M._m01_m11_m21),
                                            length(UNITY_MATRIX_M._m02_m12_m22));
                o.objectPos = v.positionOS.xyz * objectScale;
                o.normalOS  = v.normalOS;
                return o;
            }


            float4 LoadAtlas(float2 pxFromTop)
            {
                int2 p = int2(pxFromTop.x, _PatternAtlas_TexelSize.w - 1.0 - pxFromTop.y);
                return LOAD_TEXTURE2D(_PatternAtlas, p);
            }

            float3 ShadeColor(float index, float4 customColor, float3 paper, float darkness)
            {
                float3 color = (round(index) == AUTO_COLOR)
                    ? paper * (1.0 - saturate(_ShadowDarkness)) : MaterialColor(index, customColor);
                return color * (1.0 - saturate(darkness));
            }

            float PatternMask(float band, float column, float2 local)
            {
                band   = clamp(round(band), 0.0, LAST_BAND);
                column = clamp(round(column), 0.0, LAST_COLUMN);
                return step(0.5, LoadAtlas(float2(column, band) * PatternTileSize() + local).r);
            }


            float2 TriplanarCoords(float3 p, float3 n)
            {
                float3 a = abs(n);
                if (a.y >= a.x && a.y >= a.z)
                    return float2(p.x, n.y >= 0.0 ? -p.z : p.z);
                if (a.x >= a.z)
                    return float2(n.x >= 0.0 ? p.z : -p.z, -p.y);
                return float2(n.z >= 0.0 ? -p.x : p.x, -p.y);
            }

            float2 PatternPixel(Varyings i, float3 normalWS)
            {
                int mode = (int)round(_Projection);
                if (mode == 2)
                    return floor(i.positionCS.xy / max(_ScreenPixelSize, 1.0));

                float2 coords = (mode == 1)
                    ? TriplanarCoords(i.objectPos, normalize(i.normalOS))
                    : TriplanarCoords(i.positionWS, normalWS);
                return floor(coords * max(_PatternDensity, 1.0));
            }


            void AccumulateLight(Light light, float3 n, float3 v, float cosSpecular,
                                 inout float amount, inout float highlight,
                                 inout float directEnergy, inout float blockedEnergy)
            {
                float ndl = dot(n, light.direction);
                float energy = min(dot(light.color, kLuminance) * light.distanceAttenuation, 1.0);

                amount += (ndl * 0.5 + 0.5) * energy;

                float direct = saturate(ndl) * energy;
                directEnergy += direct;
                blockedEnergy += direct * (1.0 - light.shadowAttenuation);

                float3 h = SafeNormalize(float3(light.direction) + v);
                if (energy > 0.25 && ndl > 0.0 && dot(n, h) > cosSpecular)
                    highlight = max(highlight, (_ReceiveCastShadows > 0.5) ? light.shadowAttenuation : 1.0);
            }

            #define APPLY_LAYER(n) \
                if (layerCount >= n && received < _Shadow##n##Size && _Shadow##n##Size < bestSize) \
                { \
                    bestSize = _Shadow##n##Size; \
                    column   = _Shadow##n##Brightness; \
                    inkIndex = _Shadow##n##Color; \
                    inkCustomColor = _Shadow##n##CustomColor; \
                    inkDarkness = _Shadow##n##Darkness; \
                    band     = (round(_Shadow##n##Band) >= SAME_BAND) ? _PatternBand : _Shadow##n##Band; \
                }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 paper = MaterialColor(_PaletteColor, _PaletteCustomColor) * (1.0 - saturate(_PaletteDarkness));
                int shadingMode = (int)round(_ShadingMode);

                float cosSpecular = (_Specular > 0.5 && _SpecularSize > 0.0)
                    ? cos(saturate(_SpecularSize) * 0.785)
                    : 2.0;

                float amount = 0.0;
                float highlight = 0.0;

                float directEnergy = 0.0;
                float blockedEnergy = 0.0;
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(i.positionWS));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord, i.positionWS, half4(1, 1, 1, 1));
                AccumulateLight(mainLight, n, v, cosSpecular, amount, highlight, directEnergy, blockedEnergy);

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        Light dirLight = GetAdditionalLight(dirIndex, i.positionWS, half4(1, 1, 1, 1));
                        AccumulateLight(dirLight, n, v, cosSpecular, amount, highlight, directEnergy, blockedEnergy);
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                        AccumulateLight(addLight, n, v, cosSpecular, amount, highlight, directEnergy, blockedEnergy);
                    LIGHT_LOOP_END
                #endif

                float2 px = PatternPixel(i, n);
                float2 tileSize = PatternTileSize();
                float2 local = px - tileSize * floor(px / tileSize);

                float3 color = paper;

                if (shadingMode != 2)
                {
                    float received = saturate(amount);
                    float column   = round(_Brightness);
                    float inkIndex = _InkColor;
                    float4 inkCustomColor = _InkCustomColor;
                    float inkDarkness = _InkDarkness;
                    float band     = _PatternBand;

                    if (shadingMode == 1)
                    {
                        column = round(received * _Brightness);
                    }
                    else
                    {
                        int layerCount = (int)round(_ShadowLayers);
                        float bestSize = 2.0;
                        APPLY_LAYER(1)
                        APPLY_LAYER(2)
                        APPLY_LAYER(3)
                        APPLY_LAYER(4)
                        APPLY_LAYER(5)
                        APPLY_LAYER(6)
                    }

                    float3 shade = ShadeColor(inkIndex, inkCustomColor, paper, inkDarkness);
                    color = lerp(shade, paper, PatternMask(band, column, local));
                }

                if (highlight > 0.0)
                {
                    float highlightColumn = LAST_COLUMN - round(_SpecularDensity);
                    float highlightMask = PatternMask(_SpecularBand, highlightColumn, local);
                    float3 specularColor = MaterialColor(_SpecularColor, _SpecularCustomColor) * (1.0 - saturate(_SpecularDarkness));
                    color = lerp(color, specularColor, (1.0 - highlightMask) * highlight);
                }

                if (_ReceiveCastShadows > 0.5 && (_CastShadowGroundOnly < 0.5 || n.y >= 0.5))
                {
                    float shadow = (directEnergy > 0.0001) ? saturate(blockedEnergy / directEnergy) : 0.0;
                    float castBand = (round(_CastShadowBand) >= SAME_BAND) ? _PatternBand : _CastShadowBand;
                    float mask = PatternMask(castBand, _CastShadowBrightness, local);
                    float3 castColor = ShadeColor(_CastShadowColor, _CastShadowCustomColor, paper, _CastShadowDarkness);
                    color = lerp(color, castColor, shadow * saturate(_CastShadowOpacity) * (1.0 - mask));
                }

                if (_TireTracksEnabled > 0.5)
                {
                    float3 roomPosition = mul(_TireTracksWorldToRoom, float4(i.positionWS, 1.0)).xyz;
                    float2 trackUV = roomPosition.xz / max(_TireTracksRoomSize.xy, float2(0.0001, 0.0001));
                    float inside = step(0.0, trackUV.x) * step(trackUV.x, 1.0)
                        * step(0.0, trackUV.y) * step(trackUV.y, 1.0);
                    float tracks = SAMPLE_TEXTURE2D(_TireTrackMask, sampler_TireTrackMask, trackUV).r * inside;
                    color = lerp(color, _TireTrackColor.rgb, saturate(tracks * _TireTrackColor.a));
                }
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes v)
            {
                DepthVaryings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half DepthFrag(DepthVaryings i) : SV_Target
            {
                return i.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing

            struct NormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct NormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            NormalsVaryings NormalsVert(NormalsAttributes v)
            {
                NormalsVaryings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 NormalsFrag(NormalsVaryings i) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(i.normalWS), 0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "PatternOutline"
            Tags { "LightMode" = "PatternOutline" }
            ZWrite Off
            ZTest Always
            Cull Back
            Blend Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlineDataVert
            #pragma fragment OutlineDataFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            struct OutlineTargets
            {
                half4 normalAndCode : SV_Target0;
                half4 style : SV_Target1;
                half4 color : SV_Target2;
            };
            OutlineVaryings OutlineDataVert(OutlineAttributes input)
            {
                OutlineVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            OutlineTargets OutlineDataFrag(OutlineVaryings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float sceneDepth = LoadSceneDepth(uint2(input.positionCS.xy));
                #if UNITY_REVERSED_Z
                clip(input.positionCS.z - sceneDepth + 0.0000001);
                #else
                clip(sceneDepth - input.positionCS.z + 0.0000001);
                #endif
                bool enabled = _Outline > 0.5;
                float code = enabled ? 1 : 0;
                OutlineTargets output;
                output.normalAndCode = half4(normalize(input.normalWS), code);
                output.color = half4(MaterialColor(_OutlineColor, _OutlineCustomColor) * (1.0 - saturate(_OutlineDarkness)), 1);
                output.style = half4(enabled ? clamp(_OutlineWidth, 0.5, 8) : 0,
                    saturate(_OutlineDepthSensitivity), _OutlineDrawCreases > 0.5 ? clamp(_OutlineCreaseAngle, 1, 180) : -1,
                    clamp(_OutlineSoftness, 0, 1.5));
                return output;
            }
            ENDHLSL
        }
    }

    CustomEditor "PatternRendering.EditorTools.PatternMaterialGUI"
    FallBack Off
}

// Mountain triplanar — Countdown Nordic
//
// The mountains are ProBuilder geometry, so their UVs are whatever the tool produced:
// stretched on some faces, tiny on others, seams everywhere. Scaling a UV tiling value
// cannot fix that, it only changes how wrong it looks.
//
// So this shader does not use the UVs at all. It projects the texture from the three
// world axes and blends by the surface normal — every face gets the same real-world
// texel size no matter how the mesh was unwrapped. Tiling is then one honest number:
// how many metres one tile covers.
//
// Repetition is broken a second time by sampling the same texture again at a much larger
// scale and multiplying it in. That is what stops a big cliff reading as wallpaper.

Shader "Nordic/Mountain Triplanar"
{
    Properties
    {
        [MainTexture] _BaseAlbedo("Albedo", 2D) = "white" {}
        _BaseNormal("Normal", 2D) = "bump" {}
        _RoughnessMap("Roughness", 2D) = "white" {}
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [Header(Tiling)]
        _TileSize("Tile size (metres per tile)", Float) = 14
        _TriplanarSharpness("Projection sharpness", Range(1, 16)) = 6

        [Header(Break the repetition)]
        _MacroScale("Macro tile multiplier", Range(2, 40)) = 7
        _MacroStrength("Macro strength", Range(0, 1)) = 0.55

        [Header(Surface)]
        _Tint("Tint", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0,1)) = 0.6
        _RoughnessStrength("Roughness strength", Range(0,1)) = 0.75
        _Metallic("Metallic", Range(0,1)) = 0
        _NormalScale("Normal scale", Range(0,3)) = 1
        _OcclusionStrength("Occlusion strength", Range(0,1)) = 1

        [Header(Snow)]
        _SnowAlbedo("Snow albedo", 2D) = "white" {}
        _SnowColor("Snow colour", Color) = (0.9, 0.93, 0.97, 1)
        _SnowAmount("Snow amount", Range(0,1)) = 0.45
        _SnowSharpness("Snow edge", Range(0.01, 1)) = 0.25
        _SnowSmoothness("Snow smoothness", Range(0,1)) = 0.35
        _SnowDirection("Snow direction", Vector) = (0,1,0,0)
        _SnowTileScale("Snow tile multiplier", Range(0.1, 5)) = 1

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float  _TileSize;
            half   _TriplanarSharpness;
            half   _MacroScale;
            half   _MacroStrength;
            half4  _Tint;
            half   _Smoothness;
            half   _RoughnessStrength;
            half   _Metallic;
            half   _NormalScale;
            half   _OcclusionStrength;
            half4  _SnowColor;
            half   _SnowAmount;
            half   _SnowSharpness;
            half   _SnowSmoothness;
            float4 _SnowDirection;
            half   _SnowTileScale;
            half   _Cull;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------- colour
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseAlbedo);   SAMPLER(sampler_BaseAlbedo);
            TEXTURE2D(_BaseNormal);   SAMPLER(sampler_BaseNormal);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_SnowAlbedo);   SAMPLER(sampler_SnowAlbedo);

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
                float  fogCoord   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // three projections, blended by how much the face points down each axis
            float3 TriBlend(float3 n)
            {
                float3 b = pow(abs(n), _TriplanarSharpness);
                return b / max(1e-4, (b.x + b.y + b.z));
            }

            half4 TriSample(TEXTURE2D_PARAM(tex, samp), float3 posWS, float3 blend, float tile)
            {
                float2 uvX = posWS.zy / tile;
                float2 uvY = posWS.xz / tile;
                float2 uvZ = posWS.xy / tile;
                return SAMPLE_TEXTURE2D(tex, samp, uvX) * blend.x
                     + SAMPLE_TEXTURE2D(tex, samp, uvY) * blend.y
                     + SAMPLE_TEXTURE2D(tex, samp, uvZ) * blend.z;
            }

            // whiteout blend: keeps the detail of all three projections instead of
            // fading one out as the face turns
            float3 TriNormal(float3 posWS, float3 blend, float3 n, float tile)
            {
                float2 uvX = posWS.zy / tile;
                float2 uvY = posWS.xz / tile;
                float2 uvZ = posWS.xy / tile;

                float3 tx = UnpackNormalScale(SAMPLE_TEXTURE2D(_BaseNormal, sampler_BaseNormal, uvX), _NormalScale);
                float3 ty = UnpackNormalScale(SAMPLE_TEXTURE2D(_BaseNormal, sampler_BaseNormal, uvY), _NormalScale);
                float3 tz = UnpackNormalScale(SAMPLE_TEXTURE2D(_BaseNormal, sampler_BaseNormal, uvZ), _NormalScale);

                tx = float3(tx.xy + n.zy, abs(tx.z) * n.x);
                ty = float3(ty.xy + n.xz, abs(ty.z) * n.y);
                tz = float3(tz.xy + n.xy, abs(tz.z) * n.z);

                return normalize(tx.zyx * blend.x + ty.xzy * blend.y + tz.xyz * blend.z);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 n = normalize(i.normalWS);
                float3 blend = TriBlend(n);
                float tile = max(0.01, _TileSize);

                half3 albedo = TriSample(TEXTURE2D_ARGS(_BaseAlbedo, sampler_BaseAlbedo), i.positionWS, blend, tile).rgb;

                // second pass at a much larger tile, multiplied in — the same texture
                // stops looking like the same texture
                half3 macro = TriSample(TEXTURE2D_ARGS(_BaseAlbedo, sampler_BaseAlbedo),
                                        i.positionWS, blend, tile * _MacroScale).rgb;
                albedo = lerp(albedo, albedo * macro * 2.0, _MacroStrength);
                albedo *= _Tint.rgb;

                float3 normalWS = TriNormal(i.positionWS, blend, n, tile);

                half rough = TriSample(TEXTURE2D_ARGS(_RoughnessMap, sampler_RoughnessMap), i.positionWS, blend, tile).r;
                half smoothness = saturate(lerp(_Smoothness, 1.0 - rough, _RoughnessStrength));

                half ao = TriSample(TEXTURE2D_ARGS(_OcclusionMap, sampler_OcclusionMap), i.positionWS, blend, tile).r;
                ao = lerp(1.0, ao, _OcclusionStrength);

                // snow settles on whatever faces the sky
                half up = dot(normalWS, normalize(_SnowDirection.xyz + float3(0, 1e-4, 0)));
                half snow = smoothstep(1.0 - _SnowAmount - _SnowSharpness,
                                       1.0 - _SnowAmount + _SnowSharpness, up);
                half3 snowAlbedo = TriSample(TEXTURE2D_ARGS(_SnowAlbedo, sampler_SnowAlbedo),
                                             i.positionWS, blend, tile * _SnowTileScale).rgb * _SnowColor.rgb;
                albedo = lerp(albedo, snowAlbedo, snow);
                smoothness = lerp(smoothness, _SnowSmoothness, snow);

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = i.fogCoord;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.metallic = _Metallic;
                surface.smoothness = smoothness;
                surface.occlusion = ao;
                surface.alpha = 1.0;
                surface.normalTS = float3(0, 0, 1);

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- depth
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }
            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- depth + normals
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                return half4(normalize(i.normalWS) * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}

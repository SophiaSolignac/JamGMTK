// Stylized grass — Countdown Nordic
// Wind, root-to-tip gradient, per-blade random height. No player interaction on purpose.
//
// Opaque + alpha clip, one texture fetch, no normal map (a normal map on a blade three
// pixels wide is spent for nothing), lighting kept to main light + ambient + additional
// lights so the enemy auras still touch it.

Shader "Nordic/Grass"
{
    Properties
    {
        [MainTexture] _BaseMap("Base map", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.4

        [Header(Gradient)]
        _BottomColor("Bottom colour (take from the ground)", Color) = (0.22, 0.26, 0.20, 1)
        _TopColor("Top colour", Color) = (0.55, 0.62, 0.45, 1)
        _GradientPower("Gradient falloff", Range(0.1, 6)) = 1.6
        _Height("Blade height (m)", Float) = 2.0

        [Header(Wind)]
        _WindStrength("Strength", Range(0, 2)) = 0.18
        _WindSpeed("Speed", Range(0, 10)) = 1.4
        _WindScale("Scale", Range(0.01, 2)) = 0.25
        _WindSway("Root stiffness", Range(1, 6)) = 2.0
        _WindDirection("Direction (XZ)", Vector) = (1, 0.35, 0, 0)

        [Header(Variation)]
        _RandomHeight("Random height", Range(0, 0.9)) = 0.35

        [Header(Lighting)]
        _NormalUp("Normals toward up", Range(0, 1)) = 0.6
        _AmbientBoost("Ambient boost", Range(0, 3)) = 1.0

        [HideInInspector] _Cull("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

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
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "NordicGrass.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half2  maskRand   : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                float3 posOS = input.positionOS.xyz;
                half mask, rnd;
                NordicGrassDeform(posOS, mask, rnd);

                o.positionWS = TransformObjectToWorld(posOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.maskRand = half2(mask, rnd);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(tex.a - _Cutoff);

                // root-to-tip gradient: bottom is the ground colour so the grass melts
                // into the floor instead of sitting on it like stickers
                half g = pow(saturate(i.maskRand.x), _GradientPower);
                half3 albedo = tex.rgb * lerp(_BottomColor.rgb, _TopColor.rgb, g);

                // bending the normal toward the sky is what stops thin blades reading as
                // a noisy mess under a low moon
                float3 n = normalize(lerp(normalize(i.normalWS), float3(0, 1, 0), _NormalUp));

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // wrapped diffuse: foliage scatters light, hard lambert looks like plastic
                half ndl = saturate(dot(n, mainLight.direction) * 0.5 + 0.5);
                half3 color = albedo * mainLight.color * ndl * mainLight.shadowAttenuation;

                color += albedo * SampleSH(n) * _AmbientBoost;

                #ifdef _ADDITIONAL_LIGHTS
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; li++)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    half a = saturate(dot(n, l.direction) * 0.5 + 0.5);
                    color += albedo * l.color * a * l.distanceAttenuation * l.shadowAttenuation;
                }
                #endif

                color = MixFog(color, i.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- depth
        // Without this the volumetric fog would raymarch straight through the grass.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "NordicGrass.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 posOS = input.positionOS.xyz;
                half mask, rnd;
                NordicGrassDeform(posOS, mask, rnd);
                o.positionCS = TransformObjectToHClip(posOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- depth + normals
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "NordicGrass.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 posOS = input.positionOS.xyz;
                half mask, rnd;
                NordicGrassDeform(posOS, mask, rnd);
                o.positionCS = TransformObjectToHClip(posOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = normalize(lerp(TransformObjectToWorldNormal(input.normalOS), float3(0, 1, 0), _NormalUp));
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                return half4(normalize(i.normalWS) * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "NordicGrass.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 posOS = input.positionOS.xyz;
                half mask, rnd;
                NordicGrassDeform(posOS, mask, rnd);

                float3 positionWS = TransformObjectToWorld(posOS);
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
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}

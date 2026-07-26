#ifndef SOFT_LAMBERT_LIGHTING_INCLUDED
#define SOFT_LAMBERT_LIGHTING_INCLUDED

// Custom URP lighting for a stylised "soft / half Lambert" look.
// Used by a Shader Graph Custom Function node (File mode) -> function SoftLambertLighting_float.
// The graph is built on the UNLIT master stack, so we declare the URP light/shadow
// keyword variants here; Unity's compiler honours #pragma multi_compile found in
// files included into a pass. Guarded so the graph PREVIEW still compiles.
#ifndef SHADERGRAPH_PREVIEW
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
    #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif

// Wrapped diffuse: remap N.L from [-1,1] to [0,1] (soft wraparound), then sharpen by WrapPower.
// WrapPower = 1 -> classic half-Lambert; higher = tighter terminator, lower = flatter/softer.
half SoftLambertTerm(half3 N, half3 L, half wrapPower)
{
    half ndl = dot(N, L) * 0.5h + 0.5h;
    return pow(saturate(ndl), wrapPower);
}

void SoftLambertLighting_float(float3 WorldPos, float3 WorldNormal, float3 Albedo,
                               float WrapPower, out float3 Color)
{
    half3 N = normalize((half3)WorldNormal);

#ifdef SHADERGRAPH_PREVIEW
    // Preview: light against a fixed key direction so the thumbnail reads.
    half3 L = normalize(half3(0.5h, 0.6h, 0.4h));
    Color = (half3)Albedo * SoftLambertTerm(N, L, (half)WrapPower);
#else
    // --- Main directional light + realtime shadow (shadow coord from world pos, in-fragment) ---
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light mainLight = GetMainLight(shadowCoord);
    half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    half3 diffuse = mainLight.color * (SoftLambertTerm(N, mainLight.direction, (half)WrapPower) * atten);

    // --- Ambient / indirect from the spherical-harmonics probe (per pixel) ---
    diffuse += SampleSH(N);

    Color = (half3)Albedo * diffuse;
#endif
}

#endif // SOFT_LAMBERT_LIGHTING_INCLUDED

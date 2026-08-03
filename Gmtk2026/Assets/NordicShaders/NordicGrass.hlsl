#ifndef NORDIC_GRASS_INCLUDED
#define NORDIC_GRASS_INCLUDED

// Shared by every pass of Nordic/Grass. The vertex deformation MUST be identical in the
// colour pass, the depth pass and the shadow pass — if depth is written from undeformed
// grass, the volumetric fog and any depth effect will disagree with what you can see.

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BottomColor;
    half4  _TopColor;
    half   _GradientPower;
    half   _Cutoff;
    float  _Height;
    half   _RandomHeight;
    half   _WindStrength;
    half   _WindSpeed;
    half   _WindScale;
    half   _WindSway;
    float4 _WindDirection;
    half   _NormalUp;
    half   _AmbientBoost;
CBUFFER_END

// Cheap 2D hash. No texture fetch, no noise sample — wind on thousands of blades has to
// cost nothing per vertex.
float NordicHash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// positionOS  : vertex in object space, moved in place
// heightMask  : 0 at the root, 1 at the tip — drives both the gradient and the sway
// bladeRandom : stable per blade, so blades differ in height and do not sway in lockstep
void NordicGrassDeform(inout float3 positionOS, out half heightMask, out half bladeRandom)
{
    // The prefabs are exported with the pivot at the lowest vertex, so object-space Y
    // already is "height above the ground".
    heightMask = saturate(positionOS.y / max(0.001, _Height));

    // Seed from the blade's own footprint in WORLD space, quantised into 20 cm cells.
    // World space matters: static batching bakes transforms away, so an object-space
    // seed would collapse to one value for a whole batch.
    float3 rootOS = float3(positionOS.x, 0.0, positionOS.z);
    float3 rootWS = TransformObjectToWorld(rootOS);
    bladeRandom = NordicHash(floor(rootWS.xz * 5.0));

    // random height, anchored at the root
    positionOS.y *= lerp(1.0 - _RandomHeight, 1.0 + _RandomHeight, bladeRandom);

    // wind: two sines at different rates so the pattern does not visibly repeat
    float2 dirWS = normalize(_WindDirection.xy + float2(1e-5, 1e-5));
    float  phase = bladeRandom * 6.2831853;
    float  t = _Time.y * _WindSpeed;

    float w1 = sin(dot(rootWS.xz, dirWS) * _WindScale + t + phase);
    float w2 = sin(dot(rootWS.xz, dirWS.yx) * _WindScale * 1.7 - t * 1.3 + phase);
    float sway = (w1 * 0.7 + w2 * 0.3) * _WindStrength;

    // only the top bends; the root stays planted in the ground
    sway *= pow(heightMask, _WindSway);

    // the direction is authored in world space, so bring it into object space or every
    // randomly-yawed clump would blow a different way
    float3 dirOS = TransformWorldToObjectDir(float3(dirWS.x, 0.0, dirWS.y), false);
    positionOS.xz += dirOS.xz * sway;
}

#endif

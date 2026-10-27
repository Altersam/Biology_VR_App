#ifndef BVR2_COMMON_INCLUDED
#define BVR2_COMMON_INCLUDED
// Globals written every frame by Bvr2Environment (C#). Lighting in the vessel is
// simulated in the shaders (no realtime lights, no post-processing: PICO budget).
float4 _B2_Head;        // xyz headlamp position (camera), w range in metres
float4 _B2_HeadDir;     // xyz headlamp direction (view forward), w cone softness
half4  _B2_FogColor;    // deep lumen colour
float4 _B2_Fog;         // x near, y far, z strength
float4 _B2_Pulse;       // x pulse now 0..1, y heart phase (continuous cycles), z flow scroll metres, w alarm 0..1
half4  _B2_AlarmColor;
half4  _B2_Ambient;     // ambient fill colour (rgb) * a
float4 _B2_Zones[4];    // x s-centre, y half width, z radius factor, w 0
float4 _B2_Window;      // x s-centre, y angle 0..1, z half length (m), w half angle (0..0.5); w<=0 off
float4 _B2_WindowStyle; // x ragged edge 0..1, y slit (lens) shape 0/1 — a tear in the wall
half4  _B2_WindowEdge;  // rim colour (a > 0 overrides the material's)
float4 _B2_Plaque[2];   // plaques: x s-centre, y half length, z angle 0..1, w height (m)
float4 _B2_PlaqueB[2];  // x half angle 0..1, y rupture 0..1, z reveal (scanner x-ray) 0..1, w 0
float4 _B2_Field;       // vascular field: x s-centre, y half width, z intensity 0..1, w squeeze(-1)..relax(+1)
half4  _B2_FieldColor;
// _B2_Fog.w = focus dim 0..1 (knowledge moments darken everything farther than ~1.6 m)

half B2PulseShape(float x)
{
    x = frac(x);
    return smoothstep(0.0, 0.07, x) * exp(-x * 5.5);
}

// Travelling pulse wave: the systolic bulge runs down the artery.
half B2PulseAt(float s)
{
    return B2PulseShape(_B2_Pulse.y - s / 70.0);
}

float B2ZoneScale(float s)
{
    float k = 1.0;
    [unroll] for (int i = 0; i < 4; i++)
    {
        float4 z = _B2_Zones[i];
        if (z.y > 0.001)
        {
            float d = abs(s - z.x) / z.y;
            float w = 1.0 - smoothstep(0.55, 1.0, d);
            k *= lerp(1.0, z.z, w);
        }
    }
    return k;
}

float B2Bump(float x) { x = saturate(1.0 - x * x); return x * x; }

// Inset of the wall by plaques at (s, angle 0..1). Mirrors Bvr2Vessel.Inset (C#).
// info: x mask 0..1 of the strongest plaque, y ds (-1..1), z da (-1..1), w index (0/1).
float B2PlaqueInset(float s, float a01, out float4 info)
{
    float inset = 0; info = float4(0, 0, 0, 0);
    [unroll] for (int i = 0; i < 2; i++)
    {
        float4 P = _B2_Plaque[i]; float4 Q = _B2_PlaqueB[i];
        if (P.w > 0.001)
        {
            float ds = (s - P.x) / max(P.y, 0.01);
            float da = (frac(a01 - P.z + 0.5) - 0.5) / max(Q.x, 0.001);
            float m = B2Bump(ds) * B2Bump(da);
            inset += P.w * m * (1.0 + 0.08 * sin(ds * 7.0 + da * 5.0));
            if (m > info.x) info = float4(m, ds, da, i);
        }
    }
    return inset;
}

half3 B2Fog(half3 col, float3 ws)
{
    float d = distance(ws, _B2_Head.xyz);
    half f = smoothstep(_B2_Fog.x, _B2_Fog.y, d) * _B2_Fog.z;
    col = lerp(col, _B2_FogColor.rgb, f);
    return col * (1.0 - _B2_Fog.w * 0.72 * smoothstep(1.6, 3.2, d));
}

// Energy bands of the vascular field running along the controlled stretch of wall.
half3 B2FieldGlow(float s, float angle01)
{
    if (_B2_Field.z <= 0.001) return 0;
    float x = abs(s - _B2_Field.x) / max(_B2_Field.y, 0.01);
    half w = 1.0 - smoothstep(0.6, 1.0, x);
    // rings travel toward the centre while squeezing, outward while relaxing
    float ph = abs(s - _B2_Field.x) * 0.9 + _Time.y * 2.2 * _B2_Field.w;
    half band = pow(saturate(1.0 - abs(frac(ph) - 0.5) * 4.0), 3.0);
    half edge = 1.0 - smoothstep(0.0, 0.08, abs(x - 0.7));
    return _B2_FieldColor.rgb * w * _B2_Field.z * (0.04 + 0.5 * band) + _B2_FieldColor.rgb * edge * _B2_Field.z * 0.6;
}

// Headlamp: a soft spot from the player's head plus a dim omni fill so nearby
// tissue is always readable. Returns diffuse light (rgb) and writes specular.
half3 B2Light(half3 n, float3 ws, half3 v, half gloss, half wrap, out half spec)
{
    float3 toL = _B2_Head.xyz - ws;
    float dist = length(toL);
    half3 l = toL / max(dist, 1e-3);
    half atten = saturate(1.0 - dist / _B2_Head.w);
    atten = atten * (0.6 + 0.4 * atten); // softer falloff: the far wall stays readable
    half cone = smoothstep(0.45, 0.92, dot(-l, normalize(_B2_HeadDir.xyz)));
    half lamp = atten * (0.42 + 0.58 * cone);
    half ndl = saturate((dot(n, l) + wrap) / (1.0 + wrap));
    half3 h = normalize(l + v);
    spec = pow(saturate(dot(n, h)), lerp(8.0, 96.0, gloss)) * gloss * lamp;
    return ndl * lamp * half3(1.0, 0.96, 0.92) + _B2_Ambient.rgb * _B2_Ambient.a;
}

half3 B2Alarm(half3 col)
{
    half a = _B2_Pulse.w * (0.65 + 0.35 * B2PulseShape(_B2_Pulse.y));
    return lerp(col, col * _B2_AlarmColor.rgb * 1.6, a * 0.55);
}
#endif

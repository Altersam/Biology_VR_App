Shader "BVR2/MenuSky"
{
    // The main menu sky: an inside-out dome with a deep blue-to-teal gradient, a soft glow along the horizon,
    // a faint hex grid that fades upward, slow drifting light bands and a few twinkling points.
    Properties
    {
        _Top ("Top", Color) = (0.01, 0.02, 0.05, 1)
        _Horizon ("Horizon", Color) = (0.03, 0.16, 0.22, 1)
        _Bottom ("Bottom", Color) = (0.01, 0.03, 0.05, 1)
        _Glow ("Horizon glow", Color) = (0.12, 0.55, 0.7, 1)
        _Grid ("Grid colour", Color) = (0.3, 0.85, 1, 0.07)
        _Warm ("Warm accent (blood)", Color) = (0.6, 0.08, 0.12, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+100" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Top, _Horizon, _Bottom, _Glow, _Grid, _Warm;
            CBUFFER_END
            struct A { float4 p:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 d:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i) { V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.p = TransformObjectToHClip(i.p.xyz); o.d = i.p.xyz; return o; }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.d); float y = n.y; float t = _Time.y;
                half3 col = y > 0 ? lerp(_Horizon.rgb, _Top.rgb, smoothstep(0.0, 0.75, y)) : lerp(_Horizon.rgb, _Bottom.rgb, smoothstep(0.0, -0.35, y));
                col += _Glow.rgb * exp(-abs(y) * 9.0) * 0.55;
                // spherical coordinates for the patterns
                float u = atan2(n.z, n.x) / 6.2831853 + 0.5, v = asin(clamp(y, -1, 1)) / 3.1415927 + 0.5;
                // hex-ish grid near the horizon
                float2 g = float2(u * 96.0, v * 48.0); g.x += step(1.0, fmod(floor(g.y), 2.0)) * 0.5;
                float2 f = abs(frac(g) - 0.5);
                float gridLine = 1.0 - smoothstep(0.0, 0.06, min(f.x, f.y));
                col += _Grid.rgb * _Grid.a * gridLine * exp(-abs(y) * 5.0);
                // slow light bands drifting around
                float band = sin(u * 18.85 + t * 0.07) * sin(v * 9.4 - t * 0.05);
                col += _Glow.rgb * smoothstep(0.55, 1.0, band) * 0.06 * (1.0 - abs(y));
                // distant warm glow low on one side (the artery world below)
                col += _Warm.rgb * exp(-pow((u - 0.25) * 7.0, 2.0)) * exp(-pow((y + 0.08) * 6.0, 2.0)) * 0.35;
                // twinkling points
                float2 sc = float2(u * 420.0, v * 210.0); float2 cell = floor(sc); float h = Hash(cell);
                float2 off = float2(Hash(cell + 3.1), Hash(cell + 7.7)) - 0.5;
                float star = step(0.985, h) * (1.0 - smoothstep(0.0, 0.18, length(frac(sc) - 0.5 - off * 0.6))) * (0.5 + 0.5 * sin(t * (1.5 + h * 3.0) + h * 40.0));
                col += star * 0.55 * smoothstep(-0.05, 0.25, y) * half3(0.7, 0.9, 1.0);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}

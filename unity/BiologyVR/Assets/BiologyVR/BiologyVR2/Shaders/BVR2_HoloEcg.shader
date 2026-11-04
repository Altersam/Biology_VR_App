Shader "BVR2/HoloEcg"
{
    // Live ECG strip for the vitals panel: a glowing trace scrolling left over a faint grid, one complex
    // per heart beat (globals _B2_HoloBeat = heart cycles, _B2_HoloRate = beats per second; Bvr2HoloPatient).
    Properties
    {
        [HideInInspector] _MainTex ("UI texture (unused)", 2D) = "white" {}
        _Color ("Trace colour", Color) = (0.35, 1, 0.55, 1)
        _Grid ("Grid colour", Color) = (0.2, 0.6, 0.5, 0.25)
        _Window ("Seconds shown", Float) = 4
        _Thickness ("Line thickness (strip heights)", Float) = 0.022
        _Aspect ("Strip width / height", Float) = 4.75
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+12" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _B2_HoloBeat, _B2_HoloRate;
            CBUFFER_START(UnityPerMaterial)
            half4 _Color, _Grid; float _Window, _Thickness, _Aspect;
            CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i) { V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.p = TransformObjectToHClip(i.p.xyz); o.uv = i.uv; return o; }
            float G(float x, float c, float w) { float d = (x - c) / w; return exp(-d * d); }
            float Ecg(float cyc)
            {
                float f = frac(cyc);
                return 0.10 * G(f, 0.12, 0.035) - 0.08 * G(f, 0.27, 0.012) + 1.0 * G(f, 0.30, 0.013) - 0.25 * G(f, 0.33, 0.013) + 0.24 * G(f, 0.56, 0.06);
            }
            float Seg(float2 p, float2 a, float2 b) { float2 ab = b - a; float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-6)); return length(p - a - ab * t); }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float rate = _B2_HoloRate > 0.0 ? _B2_HoloRate : 1.2;
                float now = _B2_HoloBeat > 0.0 ? _B2_HoloBeat : _Time.y * rate;
                float span = _Window * rate;                   // cycles across the strip
                // distance to the trace as a polyline through samples around this column (steep QRS edges stay solid)
                float2 p = float2(i.uv.x * _Aspect, i.uv.y);
                const float h = 0.0035;
                float best = 1e3;
                float2 prev = float2((i.uv.x - 5.0 * h) * _Aspect, 0.22 + 0.62 * Ecg(now - (1.0 - (i.uv.x - 5.0 * h)) * span));
                [unroll] for (int k = -4; k <= 5; k++)
                {
                    float x = min(i.uv.x + k * h, 1.0);
                    float2 q = float2(x * _Aspect, 0.22 + 0.62 * Ecg(now - (1.0 - x) * span));
                    best = min(best, Seg(p, prev, q)); prev = q;
                }
                half lineV = 1.0 - smoothstep(_Thickness * 0.45, _Thickness, best);
                half glow = (1.0 - smoothstep(0.0, _Thickness * 5.0, best)) * 0.45;
                half recent = 0.3 + 0.7 * smoothstep(0.0, 0.85, i.uv.x);          // the newest beats brightest
                // the writing head: a bright dot at the newest point
                float2 headP = float2(_Aspect, 0.22 + 0.62 * Ecg(now));
                half head = (1.0 - smoothstep(0.0, 0.05, length(p - headP))) * 1.2 + (1.0 - smoothstep(0.0, 0.16, length(p - headP))) * 0.35;
                half2 g = abs(frac(i.uv * float2(24, 6)) - 0.5);
                half grid = (1.0 - smoothstep(0.0, 0.035, min(g.x, g.y))) * _Grid.a;
                half fadeEdge = smoothstep(0.0, 0.03, i.uv.x) * smoothstep(0.0, 0.06, i.uv.y) * smoothstep(1.0, 0.94, i.uv.y);
                half3 col = _Color.rgb * ((lineV + glow) * recent + head) + _Grid.rgb * grid;
                half a = saturate(((lineV + glow) * recent + head + grid) * fadeEdge);
                return half4(col * 1.4, a);
            }
            ENDHLSL
        }
    }
}

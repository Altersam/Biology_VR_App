Shader "BVR2/MenuFloor"
{
    // The main menu floor: a dark glassy disc (UV = position / radius) with concentric rings, radial spokes,
    // a ring pulse travelling outward with the heart beat and a soft fade to the edge.
    Properties
    {
        _Base ("Base", Color) = (0.01, 0.04, 0.06, 0.92)
        _Line ("Lines", Color) = (0.3, 0.85, 1, 1)
        _Pulse ("Pulse", Color) = (1, 0.35, 0.4, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _B2_HoloBeat;
            CBUFFER_START(UnityPerMaterial)
            half4 _Base, _Line, _Pulse;
            CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i) { V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.p = TransformObjectToHClip(i.p.xyz); o.uv = i.uv; return o; }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float r = length(i.uv); float a = atan2(i.uv.y, i.uv.x);
                float aa = fwidth(r) * 1.5 + 1e-4;
                float rings = 1.0 - smoothstep(aa, aa * 2.5, abs(frac(r * 9.0 + 0.5) - 0.5) / 9.0);
                float spokes = 1.0 - smoothstep(0.0, 0.0035, abs(frac(a / 6.2831853 * 24.0 + 0.5) - 0.5) / 24.0 * 6.2831853 * r);
                spokes *= smoothstep(0.1, 0.25, r);
                float beat = _B2_HoloBeat > 0 ? _B2_HoloBeat : _Time.y * 1.2;
                float pr = frac(beat * 0.5) * 1.1;
                float pulse = exp(-pow((r - pr) / 0.025, 2.0)) * (1.0 - pr);
                float edge = 1.0 - smoothstep(0.75, 1.0, r);
                float centre = exp(-r * r * 30.0);
                half3 col = _Base.rgb + _Line.rgb * (rings * 0.35 + spokes * 0.12 + centre * 0.4) * edge + _Pulse.rgb * pulse * 0.6 * edge;
                half alpha = saturate(_Base.a * edge + (rings * 0.3 + spokes * 0.1 + pulse * 0.5) * edge);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

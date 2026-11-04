Shader "BVR2/HoloFlow"
{
    // Vessels of the patient hologram: glowing tubes with pulses of blood running along them.
    // UV.x = metres along the vessel from the heart. One pulse leaves the heart per beat
    // (global _B2_HoloBeat = heart cycles, set by Bvr2HoloPatient); veins run the other way and softer.
    Properties
    {
        _Color ("Colour", Color) = (1, 0.25, 0.3, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.1
        _PulseColor ("Pulse colour", Color) = (1, 0.75, 0.7, 1)
        _Pulse ("Pulse (metres per beat, width 0..1, strength, direction +1/-1)", Vector) = (0.55, 0.22, 1.6, 1)
        _Fresnel ("Rim power", Range(0.5, 6)) = 1.5
        _Alpha ("Alpha", Range(0, 1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+6" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _B2_HoloBeat;
            CBUFFER_START(UnityPerMaterial)
            half4 _Color, _PulseColor; half _Intensity, _Fresnel, _Alpha; float4 _Pulse;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; float2 uv:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.ws); o.n = TransformObjectToWorldNormal(i.n); o.uv = i.uv; return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 v = normalize(_WorldSpaceCameraPos - i.ws);
                half f = pow(1.0 - saturate(abs(dot(normalize(i.n), v))), _Fresnel);
                // pulse phase: arteries move away from the heart, veins toward it
                float beat = _B2_HoloBeat > 0.0 ? _B2_HoloBeat : _Time.y * 1.2;
                float ph = frac(i.uv.x / max(_Pulse.x, 1e-3) - beat * _Pulse.w);
                // a sharp front and a soft tail behind it
                half head = smoothstep(1.0 - _Pulse.y, 1.0, ph) * (1.0 - smoothstep(0.985, 1.0, ph));
                half pulse = head * _Pulse.z;
                half core = 0.55 + 0.45 * (1.0 - f);
                half3 col = _Color.rgb * _Intensity * core + _PulseColor.rgb * pulse;
                return half4(col, saturate((0.55 + 0.45 * f + pulse * 0.5) * _Alpha * _Color.a));
            }
            ENDHLSL
        }
    }
}

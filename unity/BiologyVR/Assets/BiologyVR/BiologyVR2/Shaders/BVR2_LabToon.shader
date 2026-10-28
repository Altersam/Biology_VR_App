Shader "BVR2/LabToon"
{
    // Stylised lab surfaces (converted from the SecVarLab toon materials): a colour ramp lit by one
    // fixed "sun" direction plus up to four soft lamp pools, all from globals that Bvr2LabLighting
    // writes. No realtime lights, no shadows — cheap on PICO / Quest.
    Properties
    {
        _BaseColor ("Base colour", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _Ramp ("Ramp (shade → lit)", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _Gloss ("Gloss", Range(0, 1)) = 0.15
        [HideInInspector] _SrcBlend ("Src", Float) = 1
        [HideInInspector] _DstBlend ("Dst", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _EmissionColor; half _Gloss; float _SrcBlend, _DstBlend, _ZWrite, _Cull;
            CBUFFER_END
            float4 _B2_LabSun;        // xyz direction towards the sun, w strength
            half4  _B2_LabSunColor;
            half4  _B2_LabAmbient;    // rgb fill, a strength
            float4 _B2_LabLamps[4];   // xyz position, w range (0 = off)
            half4  _B2_LabLampColor[4];
            float4 _B2_LabFocus;      // xyz point of interest, w radius: everything farther is dimmed a little

            struct A { float4 p:POSITION; float3 n:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.ws); o.n = TransformObjectToWorldNormal(i.n);
                return o;
            }

            half3 Ramp(half t) { return SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(saturate(t), 0.5)).rgb; }

            half4 Frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.n); if (!front) n = -n;
                half3 v = normalize(GetWorldSpaceViewDir(i.ws));
                half3 sun = normalize(_B2_LabSun.xyz + 1e-4);
                half ndl = dot(n, sun);
                half3 albedo = _BaseColor.rgb;
                half3 lit = Ramp(ndl * 0.5 + 0.5) * albedo * lerp(1.0, _B2_LabSunColor.rgb, 0.35) * _B2_LabSun.w;
                half3 top = Ramp(0.86) * albedo;
                half3 lamps = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float4 L = _B2_LabLamps[k];
                    if (L.w <= 0) continue;
                    float3 d = L.xyz - i.ws; float dist = length(d);
                    half att = saturate(1.0 - dist / L.w); att *= att;
                    half wrap = saturate(dot(n, d / max(dist, 1e-3)) * 0.6 + 0.4);
                    lamps += _B2_LabLampColor[k].rgb * att * wrap;
                }
                half3 col = lit + top * lamps + Ramp(0.0) * albedo * _B2_LabAmbient.rgb * _B2_LabAmbient.a;
                half3 h = normalize(sun + v);
                col += pow(saturate(dot(n, h)), 48.0) * _Gloss * _B2_LabSunColor.rgb * 0.6;
                if (_B2_LabFocus.w > 0) col *= lerp(1.0, 0.72, smoothstep(_B2_LabFocus.w, _B2_LabFocus.w * 2.2, distance(i.ws, _B2_LabFocus.xyz)));
                col += _EmissionColor.rgb;
                col = col / (1.0 + max(0, max(col.r, max(col.g, col.b)) - 1.0) * 0.5); // soft shoulder, no harsh clipping
                return half4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}

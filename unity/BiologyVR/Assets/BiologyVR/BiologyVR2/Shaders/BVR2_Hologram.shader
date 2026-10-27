Shader "BVR2/Hologram"
{
    // Textured hologram for the professor and the scan dioramas: keeps the
    // character's colours, adds a cyan rim, moving scanlines, flicker and a
    // vertical "materialise" wipe. Depth pre-pass avoids self-sorting artefacts.
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _RimColor ("Rim", Color) = (0.35, 0.9, 1.0, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.92
        _Holo ("Hologram amount", Range(0, 1)) = 0.45
        _Reveal ("Reveal 0..1", Range(0, 1)) = 1
        _RevealRange ("Reveal range along the axis (min, max)", Vector) = (0, 2, 0, 0)
        _RevealAxis ("Reveal axis (world)", Vector) = (0, 1, 0, 0)
        _ClipPlane ("Clip plane (world normal xyz, w); zero = off", Vector) = (0, 0, 0, 0)
        _Emission ("Emission", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+20" "IgnoreProjector"="True" }
        Pass
        {
            Name "DepthPrime"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ColorMask 0
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; half4 _Tint, _RimColor, _Emission; half _Alpha, _Holo, _Reveal; float4 _RevealRange, _RevealAxis, _ClipPlane;
            CBUFFER_END
            struct A { float4 p:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i) { V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.ws); return o; }
            half4 Frag(V i) : SV_Target
            {
                float y = (dot(i.ws, _RevealAxis.xyz) - _RevealRange.x) / max(_RevealRange.y - _RevealRange.x, 1e-3);
                clip(_Reveal * 1.05 - y);
                if (dot(_ClipPlane.xyz, _ClipPlane.xyz) > 0.0) clip(dot(i.ws, _ClipPlane.xyz) + _ClipPlane.w);
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "Hologram"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; half4 _Tint, _RimColor, _Emission; half _Alpha, _Holo, _Reveal; float4 _RevealRange, _RevealAxis, _ClipPlane;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; float2 uv:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i) { V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.ws); o.n = TransformObjectToWorldNormal(i.n); o.uv = TRANSFORM_TEX(i.uv, _MainTex); return o; }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float y = (dot(i.ws, _RevealAxis.xyz) - _RevealRange.x) / max(_RevealRange.y - _RevealRange.x, 1e-3);
                clip(_Reveal * 1.05 - y);
                half edge = 1.0 - smoothstep(0.0, 0.05, _Reveal * 1.05 - y);
                // projector cut (bust on the comm screen): clip below the plane, glow along the cut
                if (dot(_ClipPlane.xyz, _ClipPlane.xyz) > 0.0)
                {
                    float cd = dot(i.ws, _ClipPlane.xyz) + _ClipPlane.w;
                    clip(cd);
                    edge = max(edge, (1.0 - smoothstep(0.0, 0.012, cd)) * 0.8);
                }
                half3 n = normalize(i.n); half3 v = normalize(_WorldSpaceCameraPos - i.ws);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Tint;
                // Soft studio light from above-front so the face reads in the dark lumen.
                half3 key = normalize(half3(0.3, 0.8, 0.0) + v);
                half lit = 0.55 + 0.45 * saturate(dot(n, key)) + 0.15 * saturate(dot(n, v));
                half fres = pow(1.0 - saturate(dot(n, v)), 2.5);
                half scan = 0.85 + 0.15 * sin(i.ws.y * 220.0 - _Time.y * 6.0);
                half flick = 0.94 + 0.06 * sin(_Time.y * 37.0) * sin(_Time.y * 13.0);
                half3 col = tex.rgb * lit;
                col = lerp(col, col * scan + _RimColor.rgb * 0.12, _Holo);
                col += _RimColor.rgb * fres * (0.35 + _Holo);
                col += _RimColor.rgb * edge * 2.5;
                col += _Emission.rgb;
                half a = _Alpha * lerp(1.0, scan, _Holo * 0.5) * flick;
                a = saturate(a + fres * 0.3 + edge);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}

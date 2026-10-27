Shader "BVR2/Tissue"
{
    // Lit organic surface for props (wall layers, anchors, tools, dioramas).
    // Optional fibre stripes along UV and emissive highlight.
    Properties
    {
        _BaseColor ("Colour", Color) = (0.8, 0.3, 0.3, 1)
        _StripeColor ("Stripe colour", Color) = (0.5, 0.1, 0.12, 1)
        _Stripes ("Stripes (freqU, freqV, sharp, amount)", Vector) = (0, 0, 2, 0)
        _Gloss ("Gloss", Range(0,1)) = 0.5
        _Emission ("Emission", Color) = (0, 0, 0, 1)
        _Rim ("Rim colour", Color) = (0, 0, 0, 1)
        [Toggle] _Unfogged ("Ignore fog", Float) = 0
        _Studio ("Studio key light (exhibits)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BVR2_Common.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _StripeColor, _Emission, _Rim; float4 _Stripes; half _Gloss, _Unfogged, _Studio;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; half4 c:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; float2 uv:TEXCOORD2; half4 c:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i) { V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.ws); o.n = TransformObjectToWorldNormal(i.n); o.uv = i.uv; o.c = i.c; return o; }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.n); half3 v = normalize(_B2_Head.xyz - i.ws);
                half3 albedo = _BaseColor.rgb * i.c.rgb;
                if (_Stripes.w > 0)
                {
                    half s = 0.5 + 0.5 * sin(i.uv.x * _Stripes.x + i.uv.y * _Stripes.y);
                    albedo = lerp(albedo, _StripeColor.rgb, pow(s, _Stripes.z) * _Stripes.w);
                }
                half spec; half3 light = B2Light(n, i.ws, v, _Gloss, 0.5, spec);
                half fres = pow(1.0 - saturate(dot(n, v)), 3.0);
                // studio light for exhibits and holograms: warm key from above-left, cool fill from below
                half3 key = normalize(half3(-0.45, 0.8, -0.25));
                half k = saturate(dot(n, key)) * 0.75 + saturate(dot(n, -key)) * 0.18;
                light = lerp(light, light * 0.45 + half3(1.0, 0.95, 0.9) * k + half3(0.25, 0.35, 0.45) * 0.25, _Studio);
                half3 col = albedo * light + spec * 0.8 + _Emission.rgb + _Rim.rgb * fres;
                if (_Unfogged < 0.5) col = B2Fog(B2Alarm(col), i.ws);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}

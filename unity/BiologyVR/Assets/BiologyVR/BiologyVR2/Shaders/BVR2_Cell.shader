Shader "BVR2/Cell"
{
    Properties
    {
        _BaseColor ("Body colour", Color) = (0.82, 0.08, 0.1, 1)
        _RimColor ("Rim / membrane colour", Color) = (1.0, 0.55, 0.5, 1)
        _CoreColor ("Core colour (dimple / nucleus)", Color) = (0.45, 0.02, 0.06, 1)
        _Gloss ("Gloss", Range(0,1)) = 0.75
        _Wrap ("Light wrap", Range(0,1)) = 0.6
        _Dimple ("Biconcave dimple shading", Range(0,1)) = 0.6
        _Emission ("Self glow", Color) = (0, 0, 0, 1)
        _HighlightColor ("Highlight", Color) = (0.3, 0.95, 1.0, 1)
        _Inst ("Instance (var, highlight, morph, phase)", Vector) = (1, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BVR2_Common.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _RimColor, _CoreColor, _Emission, _HighlightColor; half _Gloss, _Wrap, _Dimple;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Inst)
            UNITY_INSTANCING_BUFFER_END(Props)
            struct A { float4 p:POSITION; float3 n:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; float3 os:TEXCOORD2; float4 inst:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 inst = UNITY_ACCESS_INSTANCED_PROP(Props, _Inst);
                float3 p = i.p.xyz;
                // Soft membrane wobble (morph also drives platelet spikes outward).
                float wob = sin(p.x * 9.0 + _Time.y * 2.3 + inst.w * 6.2831) * sin(p.z * 7.0 + _Time.y * 1.7);
                p += i.n * (wob * 0.015 + inst.z * 0.18 * saturate(wob + 0.4));
                float3 ws = TransformObjectToWorld(p);
                o.p = TransformWorldToHClip(ws); o.ws = ws; o.n = TransformObjectToWorldNormal(i.n); o.os = i.p.xyz; o.inst = inst;
                return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.n); half3 v = normalize(_B2_Head.xyz - i.ws);
                half var = i.inst.x;
                // Biconcave centre reads darker and denser (the "dimple").
                half r = length(i.os.xz);
                half dimple = (1.0 - smoothstep(0.0, 0.62, r)) * _Dimple;
                half3 albedo = lerp(_BaseColor.rgb * var, _CoreColor.rgb, dimple);
                half spec; half3 light = B2Light(n, i.ws, v, _Gloss, _Wrap, spec);
                half fres = pow(1.0 - saturate(dot(n, v)), 3.0);
                half3 col = albedo * light;
                col += fres * _RimColor.rgb * (0.25 + 0.75 * saturate(light.r));     // membrane sheen
                col += albedo * half3(1.0, 0.5, 0.45) * fres * 0.25;                  // subsurface glow
                col += spec * half3(1.0, 0.92, 0.9);
                col += _Emission.rgb;
                half hl = i.inst.y;
                col += _HighlightColor.rgb * hl * (0.35 + 1.4 * fres) * (0.8 + 0.2 * sin(_Time.y * 9.0));
                col = B2Alarm(col);
                return half4(B2Fog(col, i.ws), 1);
            }
            ENDHLSL
        }
    }
}

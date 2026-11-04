Shader "BVR2/LabEmissive"
{
    // Self-lit lab dressing: jar liquids, neon strips, LEDs, lamp bulbs (vertex colour × intensity) and
    // the wall screens / posters (texture). Opaque, unlit, a slight rim brightening so shapes still read.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.2
        _Rim ("Rim brightening", Range(0, 2)) = 0.35
        [Toggle] _VertexTint ("Vertex colours", Float) = 1
        _Scroll ("Scanline (count, speed, amount, 0)", Vector) = (0, 0, 0, 0)
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
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; half4 _Color; half _Intensity, _Rim, _VertexTint; float4 _Scroll;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; half4 c:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; float3 ws:TEXCOORD1; half3 n:TEXCOORD2; half4 c:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.ws); o.n = TransformObjectToWorldNormal(i.n);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex); o.c = i.c; return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Color.rgb * lerp(half3(1, 1, 1), i.c.rgb, (half)_VertexTint);
                half3 v = normalize(GetWorldSpaceViewDir(i.ws));
                half rim = pow(1.0 - saturate(abs(dot(normalize(i.n), v))), 2.0) * _Rim;
                col *= _Intensity * (1.0 + rim);
                if (_Scroll.z > 0) col *= 1.0 - _Scroll.z * 0.5 * (1.0 + sin(i.uv.y * _Scroll.x * 6.2832 + _Time.y * _Scroll.y));
                col = col / (1.0 + max(0, max(col.r, max(col.g, col.b)) - 1.0) * 0.5);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}

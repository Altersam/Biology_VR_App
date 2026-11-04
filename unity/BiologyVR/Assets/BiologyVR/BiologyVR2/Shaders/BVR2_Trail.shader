Shader "BVR2/Trail"
{
    // Swing trails of the hand cells (TrailRenderer ribbons): additive, soft across the ribbon,
    // fading toward the tail (vertex colour from the trail gradient).
    Properties
    {
        _Color ("Colour", Color) = (0.5, 0.95, 1, 0.8)
        _Intensity ("Intensity", Range(0, 4)) = 1.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+5" "IgnoreProjector"="True" }
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
            CBUFFER_START(UnityPerMaterial)
            half4 _Color; half _Intensity;
            CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; half4 c:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 c:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i) { V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.p = TransformObjectToHClip(i.p.xyz); o.uv = i.uv; o.c = i.c * _Color; return o; }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half across = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                half a = across * across * i.c.a;
                return half4(i.c.rgb * _Intensity, a);
            }
            ENDHLSL
        }
    }
}

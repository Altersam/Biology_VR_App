Shader "BVR2/Screen"
{
    // Head-locked comfort vignette and fades. Drawn on a quad just in front of the eyes.
    Properties
    {
        _Color ("Fade colour", Color) = (0, 0, 0, 0)
        _Vignette ("Vignette (strength, inner, outer, 0)", Vector) = (0, 0.45, 0.95, 0)
        _VignetteColor ("Vignette colour", Color) = (0.03, 0.0, 0.01, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Overlay+50" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color, _VignetteColor; float4 _Vignette;
            CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i) { V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.p = TransformObjectToHClip(i.p.xyz); o.uv = i.uv; return o; }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half r = length(i.uv * 2.0 - 1.0);
                half vig = smoothstep(_Vignette.y, _Vignette.z, r) * _Vignette.x;
                half3 col = lerp(_VignetteColor.rgb, _Color.rgb, _Color.a);
                half a = saturate(vig + _Color.a);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}

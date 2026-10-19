Shader "BiologyVR/Arcade Opaque Cell"
{
    Properties { _BaseColor("Tint", Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On Blend One Zero
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; half3 normalWS:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes v) { Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.color=v.color;return o; }
            half4 Frag(Varyings i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);half light=.60h+.40h*saturate(dot(normalize(i.normalWS),normalize(half3(-.3,.7,-.6))));return half4(i.color.rgb*_BaseColor.rgb*light,1); }
            ENDHLSL
        }
    }
}

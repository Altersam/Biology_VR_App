Shader "BiologyVR/Biomedical Device"
{
    Properties { _Work("Active signal",Range(0,2))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half _Work;
            CBUFFER_END
            struct A { float4 position:POSITION;float3 normal:NORMAL;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 position:SV_POSITION;half3 normal:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a)
            {
                V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position=TransformObjectToHClip(a.position.xyz);o.normal=TransformObjectToWorldNormal(a.normal);o.color=a.color;return o;
            }
            half4 frag(V v):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                Light light=GetMainLight();half diffuse=saturate(dot(normalize(v.normal),light.direction));
                half3 surface=v.color.rgb*(.48h+light.color*(diffuse*.52h));
                return half4(surface+v.color.rgb*v.color.a*(.25h+_Work),1);
            }
            ENDHLSL
        }
    }
}

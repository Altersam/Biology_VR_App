Shader "BiologyVR/Gas Bubble"
{
    Properties { _BaseColor("Pearl cyan rim",Color)=(.52,.90,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 position:POSITION;float3 normal:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(a.position.xyz);o.position=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normal);return o;}
            half4 frag(V v):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                half3 n=normalize(v.normal),view=normalize(GetWorldSpaceViewDir(v.world));
                half rim=pow(1-saturate(dot(n,view)),2.3h);
                half glint=pow(saturate(dot(n,normalize(half3(-.4,.7,-.5)))),28);
                return half4(lerp(_BaseColor.rgb,half3(.96,.99,1),glint),.055h+rim*.65h+glint*.25h);
            }
            ENDHLSL
        }
    }
}

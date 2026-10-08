Shader "BiologyVR/Mobile Gas Bubble"
{
    Properties
    {
        _BaseColor("Shell tint",Color)=(.56,.9,1,.07)
        _RimColor("Pearl rim",Color)=(.8,.97,1,.8)
        _RimPower("Rim falloff",Range(1,6))=3
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor,_RimColor;
            half _RimPower;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);o.p=p.positionCS;o.ws=p.positionWS;o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.n),v=GetWorldSpaceNormalizeViewDir(i.ws);
                if(dot(n,v)<0)n=-n;
                half rim=pow(1-saturate(dot(n,v)),_RimPower);
                half glint=pow(saturate(dot(n,normalize(v+half3(-.35,.8,.4)))),50);
                half3 col=lerp(_BaseColor.rgb,_RimColor.rgb,rim)+glint*.5;
                return half4(MixFog(col,i.fog),saturate(_BaseColor.a+rim*_RimColor.a+glint*.25));
            }
            ENDHLSL
        }
    }
}

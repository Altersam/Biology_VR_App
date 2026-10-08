Shader "BiologyVR/Soft Inked Cell"
{
    Properties
    {
        _BaseColor("Cell colour",Color)=(.8,.08,.12,1)
        _EmissionColor("Study feedback",Color)=(0,0,0,1)
        _Smoothness("Soft gloss",Range(0,1))=.35
        _Metallic("Metallic compatibility",Range(0,1))=0
        _InkStrength("Soft contour",Range(0,1))=.32
        _InkColor("Contour tint",Color)=(.16,.09,.20,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor,_EmissionColor,_InkColor;half _Smoothness,_Metallic,_InkStrength;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);o.p=p.positionCS;o.ws=p.positionWS;o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.n),v=GetWorldSpaceNormalizeViewDir(i.ws);Light l=GetMainLight();
                half w=saturate((dot(n,l.direction)+.38)/1.38);
                half bands=.24*smoothstep(.16,.24,w)+.24*smoothstep(.48,.58,w)+.22*smoothstep(.76,.86,w);
                half3 color=_BaseColor.rgb*(.50+bands)+_BaseColor.rgb*SampleSH(n)*.30;
                half edge=smoothstep(.70,.94,1-saturate(dot(n,v)));
                color=lerp(color,_InkColor.rgb,edge*_InkStrength);
                color+=pow(saturate(dot(n,normalize(v+l.direction))),32)*_Smoothness*.13+_EmissionColor.rgb;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}

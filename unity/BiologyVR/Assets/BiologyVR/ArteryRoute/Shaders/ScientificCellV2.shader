Shader "BiologyVR/Scientific Cell V2"
{
    Properties
    {
        _BaseColor("Existing per-cell colour",Color)=(1,1,1,1)
        _PaletteColor("Scientific palette",Color)=(.8,.08,.12,1)
        _EmissionColor("Existing study feedback",Color)=(0,0,0,1)
        _Smoothness("Soft sheen",Range(0,1))=.28
        _Metallic("Compatibility",Range(0,1))=0
        _InkStrength("Coloured silhouette",Range(0,1))=.22
        _InkColor("Silhouette tint",Color)=(.2,.03,.06,1)
        _CellCenter("Object chart centre",Vector)=(0,0,0,0)
        _CellSize("Object chart size",Vector)=(1,1,1,0)
        _CavityAxis("Thin axis",Float)=2
        _CavityStrength("RBC concavity shade",Range(0,1))=0
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
            half4 _BaseColor,_PaletteColor,_EmissionColor,_InkColor;float4 _CellCenter,_CellSize;half _Smoothness,_Metallic,_InkStrength,_CavityAxis,_CavityStrength;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;float3 chart:TEXCOORD3;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);o.p=p.positionCS;o.ws=p.positionWS;o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(p.positionCS.z);o.chart=(i.p.xyz-_CellCenter.xyz)/max(_CellSize.xyz,.0001)*2;return o;}
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.n),v=GetWorldSpaceNormalizeViewDir(i.ws);Light l=GetMainLight();
                half w=saturate((dot(n,l.direction)+.38)/1.38);
                half band=.58+.24*smoothstep(.15,.31,w)+.23*smoothstep(.52,.76,w);
                // Preserve small instance variation without inheriting the old red wash.
                half variation=clamp(dot(_BaseColor.rgb,half3(.333,.333,.333))+.62,.86,1.06);
                half3 base=_PaletteColor.rgb*variation;
                half3 color=base*band+base*SampleSH(n)*.20;
                float2 plane=_CavityAxis<.5?i.chart.yz:_CavityAxis<1.5?i.chart.xz:i.chart.xy;
                half cavity=(1-smoothstep(.24,.72,length(plane)))*_CavityStrength;
                color=lerp(color,color*half3(.53,.36,.46),cavity);
                half edge=pow(1-saturate(dot(n,v)),7);
                color=lerp(color,_InkColor.rgb,edge*_InkStrength);
                color+=pow(saturate(dot(n,normalize(v+l.direction))),24)*_Smoothness*.11;
                return half4(MixFog(color+_EmissionColor.rgb,i.fog),1);
            }
            ENDHLSL
        }
    }
}

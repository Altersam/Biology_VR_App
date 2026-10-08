// Recovery copy reconstructed from the patch applied in this session.
Shader "BiologyVR/Soft Mobile Tissue"
{
    Properties
    {
        _BaseMap("Baked endothelial colour",2D)="white"{}
        _NormalMap("Baked shallow relief",2D)="bump"{}
        _BaseColor("Tissue tint",Color)=(1,1,1,1)
        _EmissionColor("Study accent",Color)=(0,0,0,1)
        _Smoothness("Soft highlight",Range(0,1))=.34
        _NormalStrength("Shallow relief",Range(0,1))=.45
        _Reveal("Healing reveal",Range(0,1))=1
        _PulseAmplitude("Radial pulse",Range(0,.03))=.006
        _Tone("Local vascular tone",Range(.5,1.5))=1
        _ToneCentre("Tone anchor",Vector)=(0,0,0,0)
        _PatchColor("Embedded anatomy colour",Color)=(1,.6,.45,1)
        _PatchMask("Soft oval anatomy mask",Range(0,1))=0
        _AllowWallCutaway("Inspection window on main vessel",Float)=0
        _SectionReveal("Educational plaque cutaway",Range(0,1))=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_ToneCentre;half4 _BaseColor,_EmissionColor,_PatchColor;
            half _Smoothness,_NormalStrength,_Reveal,_PulseAmplitude,_Tone,_PatchMask;
            half _AllowWallCutaway;
            half _SectionReveal;
            CBUFFER_END
            float4 _InspectionOriginWS,_InspectionUpWS,_InspectionForwardWS,_InspectionRightWS;
            float4 _InspectionHalfSize;
            float _InspectionReveal;
            struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;float3 centre:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;half3 n:TEXCOORD1;half3 t:TEXCOORD2;half3 b:TEXCOORD3;float2 uv:TEXCOORD4;half fog:TEXCOORD5;float2 localUV:TEXCOORD6;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i)
            {
                V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float pulse=_PulseAmplitude*(.5+.5*sin(_Time.y*7.5398));
                float weight=1-smoothstep(4,12,distance(TransformObjectToWorld(i.centre),_ToneCentre.xyz));
                i.p.xyz+=(i.p.xyz-i.centre)*(pulse+(_Tone-1)*weight);
                VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);VertexNormalInputs n=GetVertexNormalInputs(i.n,i.t);
                o.p=p.positionCS;o.ws=p.positionWS;o.n=n.normalWS;o.t=n.tangentWS;o.b=n.bitangentWS;
                o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.localUV=i.uv;o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if(_AllowWallCutaway>.5 && _InspectionReveal>.001)
                {
                    float3 d=i.ws-_InspectionOriginWS.xyz;
                    float2 chart=float2(dot(d,_InspectionUpWS.xyz)/_InspectionHalfSize.x,dot(d,_InspectionForwardWS.xyz)/_InspectionHalfSize.y);
                    float2 rounded=abs(chart/max(.001,_InspectionReveal))-.84;
                    float edge=length(max(rounded,0))+min(max(rounded.x,rounded.y),0)-.16;
                    if(abs(dot(d,_InspectionRightWS.xyz))<.7)clip(edge);
                }
                if(_Reveal<.9999)clip(_Reveal-i.localUV.y+.0001);
                if(_SectionReveal>.001)clip(length((i.localUV-.5)*float2(2.9,3.5))-_SectionReveal*.72);
                half3 nm=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv).rgb*2-1;nm.xy*=_NormalStrength;
                half3 n=normalize(i.t*nm.x+i.b*nm.y+i.n*nm.z);half3 v=GetWorldSpaceNormalizeViewDir(i.ws);
                Light light=GetMainLight();half wrap=saturate((dot(n,light.direction)+.45)/1.45);
                half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                half patch=(1-smoothstep(.50,1,length((i.localUV-.5)*2)))*_PatchMask;
                half tissueDetail=dot(base,half3(.2126,.7152,.0722));
                base=lerp(base,_PatchColor.rgb*(.82+.36*tissueDetail),patch);
                half3 col=base*(.48+.60*wrap)+base*SampleSH(n)*.45;
                half3 h=normalize(v+light.direction);half spec=pow(saturate(dot(n,h)),lerp(12,60,_Smoothness));
                col+=spec*half3(.13,.10,.09)*_Smoothness;
                col+=base*pow(1-saturate(dot(n,v)),3)*.055+_EmissionColor.rgb;
                return half4(MixFog(col,i.fog),1);
            }
            ENDHLSL
        }
    }
}

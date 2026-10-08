Shader "BiologyVR/Hybrid Artery"
{
    Properties
    {
        _BaseColor("Color",Color)=(.6,.1,.2,1)
        _Smoothness("Soft PBR highlights",Range(0,1))=.42
        _Metallic("Metallic",Range(0,1))=0
        _Cull("Cull",Float)=0
        _CellSeams("Subtle endothelial cell borders",Float)=0
        _PulseAmplitude("Radial pulse",Float)=0
        _StyleAccent("Stylized accent",Color)=(.08,.85,.82,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Cull [_Cull]
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor,_StyleAccent;
            half _Smoothness,_Metallic,_CellSeams,_PulseAmplitude;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;float4 shadowCoord:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes input)
            {
                Varyings o=(Varyings)0; UNITY_SETUP_INSTANCE_ID(input);UNITY_TRANSFER_INSTANCE_ID(input,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float y=input.uv.y*328-8;
                float3 centre=float3(7.5*sin(y*PI/24),.7*sin(y*PI/96),y);
                input.positionOS.xyz+=(input.positionOS.xyz-centre)*(_PulseAmplitude*(.5+.5*sin(_Time.y*PI*2*1.2)));
                VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.uv=input.uv;o.fog=ComputeFogFactor(p.positionCS.z);o.shadowCoord=GetShadowCoord(p);return o;
            }
            half4 Frag(Varyings i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=NormalizeNormalPerPixel(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
                half3 v=GetWorldSpaceNormalizeViewDir(i.positionWS);
                half3 base=_BaseColor.rgb;
                float2 cell=i.uv*float2(31,198);
                cell.x+=floor(cell.y)*.5;
                float2 edge=min(frac(cell),1-frac(cell));
                half seam=(1-smoothstep(.007,.038,min(edge.x,edge.y)))*.045*_CellSeams;
                base*=1-seam;
                Light mainLight=GetMainLight(i.shadowCoord);
                half ndl=saturate(dot(n,mainLight.direction));
                half band=floor(ndl*3.0h)/2.0h;
                half3 lightColor=mainLight.color*(.72h+.28h*band);
                half3 color=base*(.76h+.46h*band)*lightColor;
                color+=base*SampleSH(n)*.34h+base*.12h;
                half silhouette=smoothstep(.62h,.96h,1-saturate(dot(n,v)));
                color=lerp(color,base*.20h,silhouette*.52h);
                half rim=pow(1-saturate(dot(n,v)),3)*.16h;
                color+=rim*_StyleAccent.rgb;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}

Shader "BiologyVR/Biological Surface"
{
    Properties
    {
        _BaseColor("Biological colour",Color)=(.65,.04,.08,1)
        _DetailMap("Membrane microtexture",2D)="gray"{}
        _DetailScale("Microtexture density",Float)=5
        _DetailStrength("Membrane relief",Range(0,.25))=.06
        _Smoothness("Moist surface",Range(0,1))=.48
        _EmissionColor("Study highlight",Color)=(0,0,0,1)
        _Transmission("Soft tissue rim",Range(0,1))=.12
        _StyleAccent("Stylized accent",Color)=(.06,.82,.72,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Cull Back
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);
            CBUFFER_START(UnityPerMaterial)
             half4 _BaseColor,_EmissionColor,_StyleAccent;
            float _DetailScale;
            half _DetailStrength,_Smoothness,_Transmission;
            CBUFFER_END
            struct A {float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 p:SV_POSITION;float3 ws:TEXCOORD0;float3 os:TEXCOORD1;half3 n:TEXCOORD2;half3 no:TEXCOORD3;half fog:TEXCOORD4;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i)
            {
                V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);
                o.p=p.positionCS;o.ws=p.positionWS;o.os=i.p.xyz;o.n=TransformObjectToWorldNormal(i.n);o.no=i.n;o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 weights=pow(abs(normalize(i.no)),4);weights/=max(dot(weights,half3(1,1,1)),.001h);
                float3 p=i.os*_DetailScale;
                half3 a=SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,p.yz).rgb;
                half3 b=SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,p.zx).rgb;
                half3 c=SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,p.xy).rgb;
                half3 detail=a*weights.x+b*weights.y+c*weights.z;
                half3 n=normalize(i.n);
                // Screen-space height gradient: independent of imported UV/tangent quality.
                float3 dx=ddx(i.ws),dy=ddy(i.ws);
                float3 r1=cross(dy,n),r2=cross(n,dx);
                float det=dot(dx,r1);
                float3 gradient=(ddx(detail.r)*r1+ddy(detail.r)*r2)*sign(det);
                n=normalize(abs(det)*n-_DetailStrength*gradient+1e-7*n);
                 half3 v=GetWorldSpaceNormalizeViewDir(i.ws);
                 Light mainLight=GetMainLight();half ndl=saturate(dot(n,mainLight.direction));half band=floor(ndl*3.0h)/2.0h;
                 half3 base=_BaseColor.rgb*lerp(.90h,1.08h,detail.r);
                 half facing=saturate(dot(n,v));
                 half3 shaped=lerp(base*.56h,base*1.08h,facing);
                 half edge=smoothstep(.04h,.58h,1-facing);
                 half3 color=shaped*(.70h+.38h*band)+shaped*SampleSH(n)*.22h+base*.05h+_EmissionColor.rgb;
                 color+=edge*half3(.34h,.08h,.025h)+pow(facing,26)*.18h;
                 half silhouette=smoothstep(.60h,.96h,1-saturate(dot(n,v)));color=lerp(color,base*.18h,silhouette*.50h);
                 color+=_StyleAccent.rgb*pow(1-saturate(dot(n,v)),3)*_Transmission;
                 return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}

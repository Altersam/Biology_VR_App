Shader "BiologyVR/Cellular Vessel"
{
    Properties{_BaseMap("Flat endothelial cells",2D)="white"{} _NormalMap("Soft membrane normal",2D)="bump"{} _PulseAmplitude("Pulse",Range(0,.04))=.012 _Wetness("Stylized surface",Range(0,1))=.34 _Reveal("Healing reveal",Range(0,1))=1 _StyleTint("Warm laboratory tint",Color)=(1,1,1,1) _StyleAccent("Holographic accent",Color)=(.05,.85,.75,1)}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off
        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
              CBUFFER_START(UnityPerMaterial)float4 _BaseMap_ST;half4 _StyleTint,_StyleAccent;half _PulseAmplitude;half _Wetness;half _Reveal;CBUFFER_END
            struct A{float4 vertex:POSITION;float3 normal:NORMAL;float4 tangent:TANGENT;float2 uv:TEXCOORD0;float3 centre:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 pos:SV_POSITION;float3 world:TEXCOORD0;half3 n:TEXCOORD1;half3 t:TEXCOORD2;half3 b:TEXCOORD3;float2 uv:TEXCOORD4;half fog:TEXCOORD5;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i){V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);i.vertex.xyz+=(i.vertex.xyz-i.centre)*(_PulseAmplitude*(.5+.5*sin(_Time.y*7.5398)));VertexPositionInputs p=GetVertexPositionInputs(i.vertex.xyz);VertexNormalInputs n=GetVertexNormalInputs(i.normal,i.tangent);o.pos=p.positionCS;o.world=p.positionWS;o.n=n.normalWS;o.t=n.tangentWS;o.b=n.bitangentWS;o.uv=i.uv;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
            half4 Frag(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if(_Reveal<.9999)clip(_Reveal-saturate(i.uv.y)+.0001);
                 half3 nm=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv).rgb*2-1;
                  half3 n=normalize(i.t*nm.x+i.b*nm.y+i.n*nm.z)*IS_FRONT_VFACE(face,1,-1);
                  half3 v=GetWorldSpaceNormalizeViewDir(i.world);
                  Light mainLight=GetMainLight();half ndl=saturate(dot(n,mainLight.direction));half band=floor(ndl*3.0h)/2.0h;
                  half3 source=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                  half wave=.5h+.5h*sin(i.uv.y*2.35h+sin(i.uv.x*11.0h)*.55h);
                  half ridge=smoothstep(.48h,.78h,wave);
                  half3 base=source*.72h+_StyleTint.rgb*.30h;
                  base*=lerp(.76h,1.16h,ridge);
                  base+=ridge*half3(.18h,.045h,.015h);
                  half3 color=base*(.62h+.46h*band)+base*SampleSH(n)*.16h;
                  half3 h=normalize(mainLight.direction+v);color+=pow(saturate(dot(n,h)),24)*half3(.34h,.12h,.035h);
                  half silhouette=smoothstep(.58h,.96h,1-saturate(dot(n,v)));color=lerp(color,base*.22h,silhouette*.48h);
                  color+=_StyleAccent.rgb*pow(1-saturate(dot(n,v)),3)*.10h;
                 return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}

Shader "BVR2/Particle"
{
    // Camera-facing soft dots, drawn instanced (plasma proteins, sparks, motes)
    // or on ordinary quads (vertex colour). Additive, no depth write.
    Properties
    {
        _Color ("Colour", Color) = (1, 0.8, 0.7, 0.6)
        _Softness ("Edge softness", Range(0.05, 1)) = 0.6
        _Inst ("Instance (r,g,b,size)", Vector) = (1, 1, 1, 1)
        [Toggle] _Billboard ("Billboard in shader", Float) = 1
        _NearFade ("Fade closer than (m), 0 = off", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BVR2_Common.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color; half _Softness; half _Billboard; float _NearFade;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Inst)
            UNITY_INSTANCING_BUFFER_END(Props)
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; half4 c:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 c:COLOR; float3 ws:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 inst = UNITY_ACCESS_INSTANCED_PROP(Props, _Inst);
                float3 ws;
                if (_Billboard > 0.5)
                {
                    float3 centre = TransformObjectToWorld(float3(0, 0, 0));
                    float3 toCam = normalize(_B2_Head.xyz - centre);
                    float3 right = normalize(cross(float3(0, 1, 0), toCam));
                    float3 up = cross(toCam, right);
                    float size = length(TransformObjectToWorldDir(float3(1, 0, 0), false)) * inst.w;
                    ws = centre + (right * i.p.x + up * i.p.y) * size;
                }
                else ws = TransformObjectToWorld(i.p.xyz);
                o.p = TransformWorldToHClip(ws); o.uv = i.uv; o.ws = ws;
                o.c = i.c * _Color * half4(inst.rgb, 1);
                return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half d = length(i.uv * 2.0 - 1.0);
                half a = saturate(1.0 - smoothstep(1.0 - _Softness, 1.0, d)) * i.c.a;
                float3 toP = i.ws - _B2_Head.xyz; float dist = length(toP);
                half fog = 1.0 - smoothstep(_B2_Fog.x, _B2_Fog.y, dist) * _B2_Fog.z;
                fog *= 1.0 - _B2_Fog.w * 0.8 * smoothstep(1.6, 3.2, dist);
                if (_NearFade > 0.0) fog *= smoothstep(_NearFade * 0.3, _NearFade, dist);
                // motes sparkle inside the headlamp beam, like dust in a torch light
                half beam = smoothstep(0.75, 0.97, dot(toP / max(dist, 1e-3), normalize(_B2_HeadDir.xyz))) * saturate(1.0 - dist / _B2_Head.w);
                return half4(i.c.rgb * (0.6 + 1.6 * beam), a * fog * (0.35 + 0.65 * beam + (1.0 - _Billboard)));
            }
            ENDHLSL
        }
    }
}

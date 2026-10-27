Shader "BVR2/Glow"
{
    // Additive energy surfaces: ring gates, beams, field rings, anchors, beacons.
    // Brightest at grazing angles; optional scrolling bands along UV.x.
    Properties
    {
        _Color ("Colour", Color) = (0.3, 0.9, 1, 1)
        _Intensity ("Intensity", Range(0, 6)) = 1.5
        _Fresnel ("Fresnel power", Range(0, 6)) = 1.5
        _Core ("Face-on amount", Range(0, 1)) = 0.35
        _Bands ("Bands (count, speed, sharpness, amount)", Vector) = (0, 0, 4, 0)
        [Toggle] _Overlay ("Draw on top", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest [_ZTest]
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BVR2_Common.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color; half _Intensity, _Fresnel, _Core, _Overlay, _ZTest; float4 _Bands;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; half4 c:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; float2 uv:TEXCOORD2; half4 c:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(ws); o.ws = ws;
                o.n = TransformObjectToWorldNormal(i.n); o.uv = i.uv; o.c = i.c; return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 v = normalize(_WorldSpaceCameraPos - i.ws);
                half f = pow(1.0 - abs(dot(normalize(i.n), v)), _Fresnel);
                half a = saturate(_Core + f);
                if (_Bands.w > 0)
                {
                    half b = frac(i.uv.x * _Bands.x - _Time.y * _Bands.y);
                    a *= lerp(1.0, pow(saturate(1.0 - abs(b * 2.0 - 1.0)), _Bands.z), _Bands.w);
                }
                half fog = _Overlay > 0.5 ? 1.0 : 1.0 - smoothstep(_B2_Fog.x, _B2_Fog.y * 1.4, distance(i.ws, _B2_Head.xyz)) * _B2_Fog.z;
                return half4(_Color.rgb * _Intensity * i.c.rgb, saturate(a * _Color.a * i.c.a) * fog);
            }
            ENDHLSL
        }
    }
}

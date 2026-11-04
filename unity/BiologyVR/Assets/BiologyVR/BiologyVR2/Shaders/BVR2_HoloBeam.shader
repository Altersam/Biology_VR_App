Shader "BVR2/HoloBeam"
{
    // Soft volumetric light beam on an open cone / cylinder (UV.y = 0 at the source .. 1 at the far end):
    // the projector light of a hologram, a light shaft under a lamp. Streaks drift along the beam.
    Properties
    {
        _Color ("Colour", Color) = (0.4, 0.9, 1, 0.25)
        _Intensity ("Intensity", Range(0, 4)) = 1
        _Falloff ("Fade along the beam", Range(0.2, 6)) = 1.6
        _Edge ("Edge softness (fresnel power)", Range(0, 6)) = 1.5
        _Streaks ("Streaks (count around, speed, amount, 0)", Vector) = (14, 0.4, 0.35, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+2" "IgnoreProjector"="True" }
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
            CBUFFER_START(UnityPerMaterial)
            half4 _Color; half _Intensity, _Falloff, _Edge; float4 _Streaks;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; float2 uv:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.ws); o.n = TransformObjectToWorldNormal(i.n); o.uv = i.uv; return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 v = normalize(_WorldSpaceCameraPos - i.ws);
                // facing the viewer = looking through the middle of the beam: denser; grazing = thin edge
                half facing = saturate(abs(dot(normalize(i.n), v)));
                half body = pow(facing, _Edge);
                half along = pow(saturate(1.0 - i.uv.y), _Falloff) * smoothstep(0.0, 0.06, i.uv.y);
                half s = 0.5 + 0.5 * sin(i.uv.x * 6.2831 * _Streaks.x + sin(i.uv.y * 9.0 - _Time.y * _Streaks.y * 6.0) * 1.5);
                half streak = lerp(1.0, 0.6 + 0.8 * s, _Streaks.z);
                half a = _Color.a * body * along * streak;
                return half4(_Color.rgb * _Intensity, saturate(a));
            }
            ENDHLSL
        }
    }
}

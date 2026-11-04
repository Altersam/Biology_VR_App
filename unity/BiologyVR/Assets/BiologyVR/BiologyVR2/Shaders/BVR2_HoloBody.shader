Shader "BVR2/HoloBody"
{
    // The patient hologram's skin: a see-through shell with a bright rim, fine horizontal scan lines,
    // a scan band sweeping up the body (object-space height), a soft fade at the feet and a slight flicker.
    Properties
    {
        _Color ("Colour", Color) = (0.35, 0.85, 1, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.2
        _Fill ("Face-on amount", Range(0, 1)) = 0.12
        _Fresnel ("Rim power", Range(0.5, 6)) = 2.2
        _Lines ("Scan lines per metre", Float) = 90
        _LineAmount ("Scan line strength", Range(0, 1)) = 0.35
        _Band ("Band (speed cycles/s, width, strength, 0)", Vector) = (0.18, 0.06, 1.4, 0)
        _Height ("Height range along _Up (object space min, max)", Vector) = (0, 1.8, 0, 0)
        _Up ("Object-space up", Vector) = (0, 1, 0, 0)
        _Flicker ("Flicker", Range(0, 1)) = 0.12
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+5" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color; half _Intensity, _Fill, _Fresnel, _Lines, _LineAmount, _Flicker, _Alpha; float4 _Band, _Height, _Up;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; float oy:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.ws = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.ws); o.n = TransformObjectToWorldNormal(i.n);
                o.oy = dot(i.p.xyz, _Up.xyz); return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 v = normalize(_WorldSpaceCameraPos - i.ws);
                half f = pow(1.0 - saturate(abs(dot(normalize(i.n), v))), _Fresnel);
                float t = _Time.y;
                float yn = saturate((i.oy - _Height.x) / max(_Height.y - _Height.x, 1e-3));
                // fine scan lines drifting up
                half lineV = smoothstep(0.75, 1.0, frac(i.ws.y * _Lines - t * 0.6));
                // the sweeping band (up, then a short pause below the feet)
                float bp = frac(t * _Band.x) * 1.3 - 0.15;
                half band = exp(-pow((yn - bp) / max(_Band.y, 1e-3), 2.0)) * _Band.z;
                half flick = 1.0 - _Flicker * (0.5 + 0.5 * sin(t * 37.0) * sin(t * 13.0 + 1.7));
                half feet = smoothstep(0.0, 0.12, yn);
                half a = (_Fill + f * 0.9) * (1.0 + lineV * _LineAmount) + band * (0.25 + f);
                half3 col = _Color.rgb * _Intensity * (1.0 + band * 0.8) + half3(1, 1, 1) * band * 0.15;
                return half4(col, saturate(a * flick * feet * _Alpha * _Color.a));
            }
            ENDHLSL
        }
    }
}

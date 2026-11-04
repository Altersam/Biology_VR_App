Shader "BVR2/Motes"
{
    // Many tiny camera-facing dots animated entirely on the GPU (one static mesh, one draw call):
    //   mode 0 rise  - motes rising in a column (hologram projector, bubbles in a flask), fading in and out;
    //   mode 1 drift - dust floating in the air, twinkling;
    //   mode 2 blink - fixed dots blinking (LEDs on equipment).
    // Mesh: POSITION = the mote's base point, UV0 = quad corner (-1..1), UV1 = (phase 0..1, speed factor), COLOR = tint.
    Properties
    {
        _Color ("Colour", Color) = (0.5, 0.9, 1, 0.8)
        _Intensity ("Intensity", Range(0, 4)) = 1.4
        _Size ("Size (m)", Float) = 0.01
        _Mode ("Mode (0 rise, 1 drift, 2 blink)", Float) = 0
        _Motion ("Motion (rise height, speed m/s or blink Hz, wobble/drift m, 0)", Vector) = (1.5, 0.12, 0.02, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+8" "IgnoreProjector"="True" }
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
            half4 _Color; half _Intensity, _Size, _Mode; float4 _Motion;
            CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; float2 uv1:TEXCOORD1; half4 c:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 c:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float t = _Time.y; float ph = i.uv1.x; float sf = i.uv1.y;
                float3 p = i.p.xyz; half a = 1;
                if (_Mode < 0.5)
                {
                    float h = max(_Motion.x, 1e-3);
                    float travel = frac(ph + t * _Motion.y * sf / h);
                    p.y += travel * h;
                    p.x += sin(t * 1.3 * sf + ph * 19.0) * _Motion.z;
                    p.z += cos(t * 1.1 * sf + ph * 23.0) * _Motion.z;
                    a = sin(3.14159 * travel);
                }
                else if (_Mode < 1.5)
                {
                    p += _Motion.z * float3(sin(t * 0.21 * sf + ph * 6.28), 0.6 * sin(t * 0.17 * sf + ph * 9.1), sin(t * 0.13 * sf + ph * 4.3));
                    a = 0.45 + 0.55 * sin(t * 1.7 * sf + ph * 31.0);
                }
                else
                {
                    a = frac(t * _Motion.y * sf + ph) < 0.5 ? 1.0 : 0.15;
                }
                float size = _Size * (0.6 + 0.8 * frac(ph * 7.31));
                float3 right = UNITY_MATRIX_V._m00_m01_m02; float3 up = UNITY_MATRIX_V._m10_m11_m12;
                float3 ws = TransformObjectToWorld(p) + (right * i.uv.x + up * i.uv.y) * size;
                o.p = TransformWorldToHClip(ws); o.uv = i.uv; o.c = half4(i.c.rgb, i.c.a * saturate(a));
                return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half d = saturate(1.0 - length(i.uv)); d *= d;
                return half4(_Color.rgb * i.c.rgb * _Intensity, saturate(d * _Color.a * i.c.a));
            }
            ENDHLSL
        }
    }
}

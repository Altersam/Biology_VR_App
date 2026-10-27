Shader "BVR2/Endothelium"
{
    Properties
    {
        _PatternTex ("Pattern (R height, G nucleus, B folds, A caustic)", 2D) = "gray" {}
        _NormalTex ("Normal", 2D) = "bump" {}
        _Tiling ("Tiling (s metres, around count)", Vector) = (2.6, 22, 0, 0)
        _Deep ("Groove colour", Color) = (0.42, 0.05, 0.08, 1)
        _Base ("Cell colour", Color) = (0.86, 0.26, 0.28, 1)
        _Sheen ("Sheen colour", Color) = (1.0, 0.72, 0.66, 1)
        _Nucleus ("Nucleus colour", Color) = (0.55, 0.16, 0.32, 1)
        _Gloss ("Wetness", Range(0,1)) = 0.82
        _NormalStrength ("Normal strength", Range(0,2)) = 1.0
        _Translucency ("Translucency", Range(0,1)) = 0.45
        _WindowEdge ("Window edge colour", Color) = (0.35, 0.95, 1.0, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BVR2_Common.hlsl"
            TEXTURE2D(_PatternTex); SAMPLER(sampler_PatternTex);
            TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Tiling; half4 _Deep, _Base, _Sheen, _Nucleus, _WindowEdge; half _Gloss, _NormalStrength, _Translucency;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float4 t:TANGENT; float2 uv:TEXCOORD0; float3 c:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 ws:TEXCOORD0; half3 n:TEXCOORD1; half3 t:TEXCOORD2; float2 uv:TEXCOORD3; half pulse:TEXCOORD4; float4 plq:TEXCOORD5; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o = (V)0; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float s = i.uv.x;
                half pulse = B2PulseAt(s);
                float k = B2ZoneScale(s) * (1.0 + 0.035 * pulse);
                float3 radial = i.p.xyz - i.c; float r0 = max(length(radial), 0.01); float3 rd = radial / r0;
                // plaques push the wall inwards (smooth bump), the normal tilts with the slope
                float4 info; float inset = B2PlaqueInset(s, i.uv.y, info);
                float3 nIn = -i.n;
                if (inset > 0.0005)
                {
                    float e = 0.15, ea = 0.004;
                    float4 tmp;
                    float dIs = (B2PlaqueInset(s + e, i.uv.y, tmp) - B2PlaqueInset(s - e, i.uv.y, tmp)) / (2.0 * e);
                    float dIa = (B2PlaqueInset(s, i.uv.y + ea, tmp) - B2PlaqueInset(s, i.uv.y - ea, tmp)) / (2.0 * ea * 6.2831853 * r0);
                    float3 th = cross(i.t.xyz, rd);
                    nIn = normalize(-rd - i.t.xyz * dIs - th * dIa);
                }
                float3 p = i.c + rd * max(r0 * k - inset, 0.3);
                float3 ws = TransformObjectToWorld(p);
                o.p = TransformWorldToHClip(ws); o.ws = ws;
                o.n = TransformObjectToWorldNormal(nIn); // inside of the tube
                o.t = TransformObjectToWorldDir(i.t.xyz);
                o.uv = i.uv; o.pulse = pulse; o.plq = info;
                return o;
            }
            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv = float2(i.uv.x / _Tiling.x, i.uv.y * _Tiling.y);
                half4 pat = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, uv);
                half3 tn = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, uv), _NormalStrength);
                // close-up detail: a finer copy of the cell relief fades in within a few metres
                float camD = distance(i.ws, _B2_Head.xyz);
                half near = 1.0 - smoothstep(2.5, 7.0, camD);
                if (near > 0.01)
                {
                    half3 tn2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, uv * 3.7 + 0.31), _NormalStrength * 0.7);
                    tn = normalize(half3(tn.xy + tn2.xy * near, tn.z));
                }
                half3 n = normalize(i.n); half3 t = normalize(i.t - n * dot(i.t, n)); half3 b = cross(n, t);
                n = normalize(t * tn.x + b * tn.y + n * tn.z);
                // Large slow folds along the flow (coarse sample) – they bend the normal and shade the albedo.
                float2 cuv = float2(i.uv.x / (_Tiling.x * 9.0), i.uv.y * 3.0);
                half fA = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, cuv + float2(0, 0.004)).b;
                half fB = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, cuv - float2(0, 0.004)).b;
                half folds = (fA + fB) * 0.5; // two taps give both the fold shade and its slope (PICO fill-rate budget)
                n = normalize(n + b * (fA - fB) * 2.2);
                half3 v = normalize(_B2_Head.xyz - i.ws);
                half h = pat.r;
                half cellMask = smoothstep(0.30, 0.5, h);
                half3 albedo = _Base.rgb * (0.80 + 0.20 * cellMask) * (0.70 + 0.30 * folds);
                albedo = lerp(albedo, _Deep.rgb, (1.0 - folds) * 0.35);
                albedo = lerp(albedo, _Nucleus.rgb, pat.g * 0.45);
                albedo *= 1.0 + 0.25 * i.pulse; // systolic flush
                half spec;
                half3 light = B2Light(n, i.ws, v, _Gloss, 0.55, spec);
                // Light shining through thin tissue: warm glow on the side facing away.
                half back = pow(1.0 - saturate(dot(n, v)), 2.0) * _Translucency;
                half caustic = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, float2((i.uv.x - _B2_Pulse.z * 0.35) / (_Tiling.x * 3.1), i.uv.y * 7.0 + 0.37)).a;
                half3 col = albedo * light * (0.82 + 0.45 * caustic) + albedo * back * half3(1.0, 0.45, 0.35) * 0.35;
                col += spec * _Sheen.rgb * (0.75 + 0.5 * folds);
                // Cut-away window for the wall-layer study (rim glows, inside is open).
                if (_B2_Window.w > 0.0)
                {
                    float da = abs(frac(i.uv.y - _B2_Window.y + 0.5) - 0.5) / _B2_Window.w;
                    float ds = abs(i.uv.x - _B2_Window.x) / _B2_Window.z;
                    // a tear: lens-shaped slit, ragged rim, raw tissue around it
                    if (_B2_WindowStyle.y > 0.5) da = da / sqrt(max(1.0 - saturate(ds) * saturate(ds), 0.02));
                    float d = max(da, ds) - (pat.r - 0.5) * _B2_WindowStyle.x * 0.5;
                    clip(d - 1.0);
                    half3 edgeCol = _B2_WindowEdge.a > 0.0 ? _B2_WindowEdge.rgb : _WindowEdge.rgb;
                    half rim = 1.0 - smoothstep(1.0, 1.12 + _B2_WindowStyle.x * 0.35, d);
                    col = lerp(col, col * half3(0.55, 0.12, 0.1), saturate(_B2_WindowStyle.x) * (1.0 - smoothstep(1.0, 1.6, d)));
                    col += edgeCol * rim * 1.4;
                }
                // atherosclerotic plaque: pale fibrous cap, yellow lipid core glowing through where the cap is thin
                half pm = i.plq.x;
                if (pm > 0.01)
                {
                    int pi = (int)(i.plq.w + 0.5);
                    float4 PB = _B2_PlaqueB[pi];
                    half capW = smoothstep(0.02, 0.45, pm);
                    half3 capCol = half3(0.56, 0.41, 0.29) * (0.78 + 0.22 * folds) * (0.88 + 0.12 * cellMask);
                    half coreVis = smoothstep(0.3, 0.95, pm) * (0.35 + 0.65 * pat.g) * (0.45 + 0.55 * PB.z);
                    half3 core = half3(0.95, 0.62, 0.12);
                    half3 pl = lerp(capCol, core, coreVis * 0.5);
                    half3 plCol = pl * light * 0.72 + spec * half3(1.0, 0.92, 0.8) * 0.45 + core * coreVis * (0.05 + 0.18 * PB.z) * (0.85 + 0.15 * i.pulse);
                    col = lerp(col, plCol, capW);
                    // rupture: a fissure along the crest, dark blood inside, hot rim pulsing with the heart
                    if (PB.y > 0.001)
                    {
                        half len = 1.0 - smoothstep(0.35 * PB.y + 0.15, 0.55 * PB.y + 0.25, abs(i.plq.y));
                        half w = abs(i.plq.z + 0.12 * sin(i.plq.y * 9.0));
                        half fissure = (1.0 - smoothstep(0.02, 0.07 + 0.05 * PB.y, w)) * len;
                        half rim = (1.0 - smoothstep(0.07, 0.2, w)) * len;
                        col = lerp(col, half3(0.18, 0.01, 0.02), fissure);
                        col += half3(1.0, 0.18, 0.08) * (rim - fissure) * PB.y * (0.6 + 0.8 * i.pulse);
                    }
                }
                col += B2FieldGlow(i.uv.x, i.uv.y);
                col = B2Alarm(col);
                return half4(B2Fog(col, i.ws), 1);
            }
            ENDHLSL
        }
    }
}

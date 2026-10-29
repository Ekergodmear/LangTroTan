// Tàng Kinh Các Level 6 floating scripture characters: particle shader that fades a golden glyph in, lets it float,
// then dissolves it into specks with a soft ember edge. Needs the particle custom vertex streams
// UV (TEXCOORD0.xy), AgePercent (TEXCOORD0.z) and StableRandom.x (TEXCOORD0.w).
Shader "SonMon/SpiritGlyph"
{
    Properties
    {
        _MainTex ("Glyph atlas (alpha)", 2D) = "white" {}
        _EdgeColor ("Dissolve edge", Color) = (1, 0.95, 0.75, 1)
        _FadeIn ("Fade in (age)", Range(0, 0.5)) = 0.18
        _DissolveStart ("Dissolve start (age)", Range(0, 1)) = 0.62
        _Grain ("Dissolve grain", Float) = 9
        _Intensity ("Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SpiritGlyph"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _EdgeColor;
                float _FadeIn, _DissolveStart, _Grain, _Intensity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float4 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float4 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float age = i.uv.z, rnd = i.uv.w;
                half glyph = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy).a;
                // local glyph coordinates inside its atlas cell drive the dissolve pattern
                float n = ValueNoise(frac(i.uv.xy * float2(4, 2)) * _Grain + rnd * 31.7) * 0.75
                        + ValueNoise(frac(i.uv.xy * float2(4, 2)) * _Grain * 2.3 + rnd * 7.3) * 0.25;
                float t = saturate((age - _DissolveStart) / (1 - _DissolveStart));  // 0 -> 1 over the dissolve
                float keep = smoothstep(t - 0.06, t + 0.06, n);
                float edge = (1 - abs(keep - 0.5) * 2) * step(0.001, t);
                half fade = smoothstep(0, _FadeIn, age);
                half3 col = lerp(i.color.rgb, _EdgeColor.rgb, saturate(edge));
                half a = glyph * fade * keep * i.color.a * _Intensity + glyph * edge * 0.35 * fade;
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}

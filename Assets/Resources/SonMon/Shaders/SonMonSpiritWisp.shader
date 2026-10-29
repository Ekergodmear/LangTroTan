// Tàng Kinh Các Level 6 spirit-energy wisps: thin transparent ribbons whose vertices are displaced by smooth periodic
// noise, so each ribbon sways and curls organically instead of reading as a rigid arc.
// Mesh: UV.x along the wisp (0 at the pillar, 1 at the hall), UV.y across; NORMAL holds the ribbon's side direction;
// vertex colour A holds a per-wisp seed (0..1). Every term is a whole number of cycles per _Cycle seconds of the global
// _SpiritTime, so the loop is seamless.
Shader "SonMon/SpiritWisp"
{
    Properties
    {
        _MainTex ("Streaks (alpha)", 2D) = "white" {}
        _Color ("Colour", Color) = (0.45, 1, 0.88, 0.35)
        _PulseColor ("Pulse colour", Color) = (0.9, 1, 0.95, 1)
        _Cycle ("Loop length (s)", Float) = 10
        _Sway ("Sway amplitude", Float) = 0.12
        _FlowRepeats ("Streak flow (repeats per cycle)", Float) = 2
        _Tiling ("Streak repeats along u", Float) = 2
        _PulseGain ("Pulse gain", Float) = 1.6
        _EndFade ("End fade (u)", Float) = 0.18
        _Intensity ("Intensity", Float) = 1
        _Animate ("Animate", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SpiritWisp"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _SpiritTime;
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color, _PulseColor;
                float _Cycle, _Sway, _FlowRepeats, _Tiling, _PulseGain, _EndFade, _Intensity, _Animate;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            static const float TAU = 6.2831853;

            // Smooth periodic "noise": a few sines with integer cycle counts in time, irrational-ish ratios along u.
            float Wave(float u, float c, float seed, float k)
            {
                return sin(TAU * (u * (1.3 + k) + c * (1 + k) + seed))
                     + 0.6 * sin(TAU * (u * (2.7 + 0.5 * k) - c * (2 + k) + seed * 1.7))
                     + 0.35 * sin(TAU * (u * 4.9 + c * 3 + seed * 3.1));
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float c = frac(_SpiritTime / _Cycle) * _Animate;
                float seed = v.color.a * 7.0;
                float u = v.uv.x;
                float env = sin(3.14159265 * u);  // anchored ends, free middle
                float3 side = normalize(v.normalOS);
                float3 up = float3(0, 1, 0);
                float3 offset = side * Wave(u, c, seed, 0) * _Sway * env
                              + up * Wave(u + 0.37, c, seed + 0.5, 1) * _Sway * 0.7 * env;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz + offset);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float c = frac(_SpiritTime / _Cycle) * _Animate;
                float seed = i.color.a;
                // streaks flowing towards the hall
                half streak = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(i.uv.x * _Tiling - c * _FlowRepeats - seed, i.uv.y)).a;
                // one soft energy pulse per cycle, offset per wisp, gliding from pillar to hall
                float head = frac(c + seed);
                float d = abs(frac(i.uv.x - head + 0.5) - 0.5);
                half pulse = exp(-d * d / 0.008) * _Animate;
                half ends = saturate(i.uv.x / _EndFade) * saturate((1 - i.uv.x) / _EndFade);
                half3 col = lerp(_Color.rgb, _PulseColor.rgb, saturate(pulse));
                half a = streak * ends * _Intensity * _Color.a * (1 + pulse * _PulseGain);
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}

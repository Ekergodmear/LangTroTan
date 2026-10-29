// Tàng Kinh Các Level 6 spirit effects: the jade rune formation (band and corner seals). Mesh UV.x runs along the
// strip, UV.y across it; vertex colour A holds a per-strip phase (0..1 of the cycle).
// Every motion is a whole number of repetitions per _Cycle seconds of the global _SpiritTime, so the loop is
// seamless (no jumps or resets):
//   runes      the inscription drifts _ScrollRepeats texture repeats per cycle
//   activation a wide, smooth wave travels along u _WavesPerCycle times per cycle; each rune lights with a soft
//              stagger behind the wave front, so the formation awakens rune by rune
//   breathing  a slow brightness swell, once per cycle
// _Intensity / _Animate are set per renderer (MaterialPropertyBlock) on mode or LOD changes only.
Shader "SonMon/SpiritFlow"
{
    Properties
    {
        _MainTex ("Rune pattern (alpha)", 2D) = "white" {}
        _Color ("Rune colour", Color) = (0.32, 0.92, 0.78, 0.6)
        _LitColor ("Activated colour", Color) = (0.75, 1, 0.92, 1)
        _Tiling ("Repeats along u", Float) = 1
        _Cycle ("Loop length (s)", Float) = 10
        _ScrollRepeats ("Rune drift (repeats per cycle)", Float) = 1
        _WavesPerCycle ("Activation waves per cycle", Float) = 1
        _WaveWidth ("Activation wave width (u)", Float) = 0.12
        _WaveGain ("Activation gain", Float) = 1.4
        _Breath ("Breathing depth", Range(0, 1)) = 0.3
        _Intensity ("Intensity", Float) = 1
        _Animate ("Animate", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SpiritFlow"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _SpiritTime;  // global, set by SonMonSpiritGathering (seconds, wrapped on a multiple of every cycle)
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color, _LitColor;
                float _Tiling, _Cycle, _ScrollRepeats, _WavesPerCycle, _WaveWidth, _WaveGain, _Breath, _Intensity, _Animate;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float c = frac(_SpiritTime / _Cycle) * _Animate;  // 0..1 through the loop
                float phase = i.color.a;
                float u = i.uv.x * _Tiling;
                half rune = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(u - c * _ScrollRepeats, i.uv.y)).a;
                // activation: rune index along the strip lags a smooth travelling front (wraps cleanly at the cycle)
                float runeU = (floor(u - c * _ScrollRepeats) + 0.5) / max(_Tiling, 1);
                float front = frac(c * _WavesPerCycle + phase);
                float d = frac(runeU - front + 0.5) - 0.5;  // signed distance behind the front, -0.5..0.5
                half wave = exp(-d * d / (_WaveWidth * _WaveWidth)) * _Animate;
                half breath = 1 - _Breath * (0.5 - 0.5 * cos(6.2831853 * (c + phase)));
                half3 col = lerp(_Color.rgb, _LitColor.rgb, saturate(wave));
                half a = rune * _Intensity * _Color.a * (breath + wave * _WaveGain);
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}

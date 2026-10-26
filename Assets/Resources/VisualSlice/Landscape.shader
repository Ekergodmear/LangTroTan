Shader "Xianxia/Painted Landscape"
{
    Properties { _Color("Pigment",Color)=(0.4,0.6,0.5,1) _Rock("Rock mode",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;float _Rock;
            CBUFFER_END
            float4 _XianxiaFog;
            struct A { float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct V { float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;};
            V vert(A v){V o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float3 n=normalize(i.normalWS);Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));float shade=smoothstep(-.2,.8,dot(n,light.direction));
                float grain=noise(i.positionWS.xz*.28)*.10+noise(i.positionWS.xz*2)*.018;
                float strata=sin(i.positionWS.y*2.1+noise(i.positionWS.xz*.22)*3)*.035;
                float illumination=saturate(dot(light.color,float3(.2126,.7152,.0722)));float3 pigment=_Color.rgb*(.95+grain+_Rock*strata);float3 color=pigment*(.72+shade*.28)*lerp(.78,1,light.shadowAttenuation)*lerp(.43,1,illumination)*lerp(float3(.65,.75,.95),float3(1,1,1),illumination);
                float fog=smoothstep(50,135,distance(_WorldSpaceCameraPos,i.positionWS));color=lerp(color,_XianxiaFog.rgb,fog*.96);return half4(color,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Simple Lit/ShadowCaster"
    }
}

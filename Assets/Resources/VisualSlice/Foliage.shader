Shader "Xianxia/Painted Foliage"
{
    Properties { _Color("Leaf pigment",Color)=(0.3,0.5,0.38,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" } Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; CBUFFER_END
            float4 _XianxiaFog;
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
            V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.world.x+=sin(_Time.y*.55+o.world.z*.6)*.025*saturate(o.world.y-1);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);return o;}
            float hash(float3 p){return frac(sin(dot(p,float3(17.1,29.3,63.7)))*43758.5);}
            half4 frag(V i):SV_Target
            {
                float3 n=normalize(i.normal);Light light=GetMainLight();float diffuse=.68+.32*abs(dot(n,light.direction));float fleck=hash(floor(i.world*9))*.085;
                float patches=sin(i.world.x*6+sin(i.world.z*7))*sin(i.world.y*8)*.045;
                float illumination=saturate(dot(light.color,float3(.2126,.7152,.0722)));float3 color=_Color.rgb*(diffuse+fleck+patches);color+=float3(.08,.11,.035)*saturate(n.y)*.3;color*=lerp(.43,1,illumination)*lerp(float3(.65,.75,.95),float3(1,1,1),illumination);
                color=lerp(color,_XianxiaFog.rgb,smoothstep(50,135,distance(_WorldSpaceCameraPos,i.world))*.96);return half4(color,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Simple Lit/ShadowCaster"
    }
}

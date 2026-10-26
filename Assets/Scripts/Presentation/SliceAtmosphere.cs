using UnityEngine;
using UnityEngine.Rendering;
namespace LangTroTan.Presentation
{
    public sealed partial class FullPortView
    {
        Material sliceAtmosphereMaterial;
        void CreateSliceAtmosphere()
        {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2)));}texture.Apply(false,true);
            var mist=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));mist.SetTexture("_BaseMap",texture);mist.SetColor("_BaseColor",Color.white);mist.SetFloat("_Surface",1);mist.SetFloat("_Blend",0);mist.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);mist.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);mist.SetInt("_ZWrite",0);mist.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mist.renderQueue=3000;
            sliceAtmosphereMaterial=mist;
            var clouds=new GameObject("Mountain cloud layer");clouds.transform.position=new Vector3(0,8,30);var ps=clouds.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.maxParticles=28;main.startLifetime=100;main.startSpeed=.025f;main.startSize=new ParticleSystem.MinMaxCurve(10,18);main.startColor=new Color(.85f,.94f,.91f,.10f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.prewarm=true;main.duration=100;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(80,3,18);var emission=ps.emission;emission.rateOverTime=.25f;ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=mist;ps.Play();
            var motes=new GameObject("Quiet spiritual motes");motes.transform.position=new Vector3(0,1,0);var spirit=motes.AddComponent<ParticleSystem>();spirit.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var s=spirit.main;s.maxParticles=42;s.startLifetime=9;s.startSpeed=.11f;s.startSize=.07f;s.startColor=new Color(.84f,.99f,.83f,.7f);s.simulationSpace=ParticleSystemSimulationSpace.World;var area=spirit.shape;area.shapeType=ParticleSystemShapeType.Box;area.scale=new Vector3(18,4,20);var flow=spirit.emission;flow.rateOverTime=4;spirit.GetComponent<ParticleSystemRenderer>().sharedMaterial=mist;spirit.Play();
        }
    }
}

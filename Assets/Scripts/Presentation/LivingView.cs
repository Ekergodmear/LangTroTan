using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using LangTroTan.Domain;

namespace LangTroTan.Presentation
{
    public partial class Game
    {
        readonly Dictionary<int,GameObject> nodeViews=new Dictionary<int,GameObject>(),townViews=new Dictionary<int,GameObject>(),taskViews=new Dictionary<int,GameObject>();
        readonly Dictionary<int,int> nodeStages=new Dictionary<int,int>(),townStages=new Dictionary<int,int>();
        ParticleSystem rain,snow;float livingViewTimer;
        int UnitMaxHp(Unit u){return !u.enemy?Catalog.UnitHp[u.kind]:u.villageId>0?(u.kind==0?110:u.kind==1?90:180):(u.kind==0?60:u.kind==1?100:220);}
        void ResetLivingViews()
        {
            foreach(var go in nodeViews.Values)Destroy(go);foreach(var go in townViews.Values)Destroy(go);foreach(var go in taskViews.Values)Destroy(go);
            nodeViews.Clear();townViews.Clear();taskViews.Clear();nodeStages.Clear();townStages.Clear();selectedResource=-1;selectedNeighbor=-1;
        }
        GameObject NodeView(ResourceNode n)
        {
            var root=new GameObject(n.kind.ToString());root.transform.position=new Vector3(n.x,TerrainHeight(n.x,n.z),n.z);
            if(n.kind==ResourceKind.Tree)
            {
                var bark=Mat("pine trunk",new Color(.28f,.2f,.12f));float h=3.2f+(n.id%5)*.35f;
                if(n.amount>0)
                {
                    Shape("Tree trunk",PrimitiveType.Cylinder,new Vector3(0,h*.27f,0),new Vector3(.25f,h*.27f,.25f),bark,root.transform);
                    if(n.id%4==0)
                    {for(int i=0;i<3;i++)Shape("Deciduous leaves",PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*2.1f)*.5f,h*.7f+i*.15f,Mathf.Cos(i*2.1f)*.45f),new Vector3(1.8f,1.6f,1.7f),Mat("broad leaves",new Color(.29f,.40f,.16f)),root.transform);}
                    else{Cone(root.transform,new Vector3(0,h*.3f,0),1.2f,h*.65f,Mat("pine needles",new Color(.15f,.27f,.16f)));Cone(root.transform,new Vector3(0,h*.53f,0),.9f,h*.55f,Mat("pine tips",new Color(.23f,.34f,.17f)));}
                }
                else
                {
                    Shape("Stump",PrimitiveType.Cylinder,new Vector3(0,.16f,0),new Vector3(.42f,.16f,.42f),bark,root.transform);
                    if(n.regrow<=180&&n.regrow>0)Cone(root.transform,new Vector3(.2f,.14f,0),.28f,.65f,Mat("broad leaves",new Color(.29f,.4f,.16f)));
                }
            }
            if(n.kind==ResourceKind.Stone&&n.amount>0)
            {for(int i=0;i<3;i++)VillageArt.Rock(root.transform,new Vector3(Mathf.Sin(i*2)*.55f,.35f,Mathf.Cos(i*2)*.45f),new Vector3(1.3f,1.1f,1.1f),Mat("Natural rock",new Color(.47f,.48f,.4f)));}
            if(n.kind==ResourceKind.Deer&&n.amount>0)
            {VillageArt.Deer(root.transform,Mat);root.transform.rotation=Quaternion.Euler(0,n.id*47,0);}
            VillageArt.Combine(root.transform);
            var c=root.AddComponent<BoxCollider>();c.center=new Vector3(0,n.kind==ResourceKind.Tree?1.3f:.6f,0);c.size=new Vector3(1.2f,n.kind==ResourceKind.Tree?2.6f:1.2f,1.2f);
            var target=root.AddComponent<ClickTarget>();target.id=n.id;target.resource=true;return root;
        }
        GameObject TownView(NeighborVillage v)
        {
            var root=new GameObject(v.name);root.transform.position=new Vector3(v.x,TerrainHeight(v.x,v.z),v.z);
            if(v.defeated)
            {for(int i=0;i<6;i++)VillageArt.Rock(root.transform,new Vector3(Mathf.Sin(i*2)*2,.3f,Mathf.Cos(i*2)*2),new Vector3(1.3f,.7f,1.2f),Mat("Ruins",new Color(.27f,.26f,.22f)));}
            else
            {
                VillageArt.Building(root.transform,new Building{type="hall"},Mat);VillageArt.Combine(root.transform);
                for(int i=0;i<v.buildings-1;i++)
                {var h=new GameObject("Neighbor house");h.transform.SetParent(root.transform,false);float a=i*Mathf.PI*2/7;h.transform.localPosition=new Vector3(Mathf.Cos(a)*4.5f,0,Mathf.Sin(a)*4.5f);VillageArt.Building(h.transform,new Building{type=i==0?"farm":i==1?"barracks":"house"},Mat);VillageArt.Combine(h.transform);}
            }
            var labelGo=new GameObject("Village name",typeof(RectTransform),typeof(Canvas));labelGo.transform.SetParent(root.transform,false);labelGo.transform.localPosition=new Vector3(0,5.8f,0);labelGo.transform.localScale=Vector3.one*.025f;
            var cv=labelGo.GetComponent<Canvas>();cv.renderMode=RenderMode.WorldSpace;var rect=labelGo.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(320,90);
            var t=Label(rect,v.name.ToUpperInvariant(),0,0,320,80,26,v.warWithPlayer?new Color(1,.45f,.25f):gold);t.alignment=TextAnchor.MiddleCenter;
            var collider=root.AddComponent<BoxCollider>();collider.center=Vector3.up*1.4f;collider.size=new Vector3(7,2.8f,7);var click=root.AddComponent<ClickTarget>();click.id=v.id;click.neighbor=true;return root;
        }
        ParticleSystem WeatherParticles(string name,bool isSnow)
        {
            var go=new GameObject(name);var p=go.AddComponent<ParticleSystem>();var main=p.main;main.startLifetime=isSnow?7:1.4f;main.startSpeed=isSnow?2.5f:20;main.startSize=isSnow?.07f:.024f;main.maxParticles=isSnow?650:1000;main.startColor=isSnow?new Color(.94f,.96f,1,.7f):new Color(.64f,.76f,.83f,.5f);main.simulationSpace=ParticleSystemSimulationSpace.World;
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(52,52,1);var emission=p.emission;emission.rateOverTime=isSnow?90:400;
            var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=new Material(Shader.Find("Sprites/Default"));renderer.renderMode=isSnow?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch;renderer.lengthScale=3;renderer.velocityScale=.035f;go.transform.rotation=Quaternion.Euler(90,0,0);p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);return p;
        }
        void UpdateLivingVisuals()
        {
            if(rain==null){rain=WeatherParticles("Rain",false);snow=WeatherParticles("Snow",true);}
            rain.transform.position=snow.transform.position=cameraFocus+Vector3.up*18;
            bool wet=world.weather==Weather.Rain||world.weather==Weather.Storm,cold=world.weather==Weather.Snow;
            if(wet&&!rain.isPlaying)rain.Play();if(!wet&&rain.isPlaying)rain.Stop();if(cold&&!snow.isPlaying)snow.Play();if(!cold&&snow.isPlaying)snow.Stop();
            Color grass=world.CurrentSeason==Season.Winter?new Color(.75f,.80f,.75f):world.CurrentSeason==Season.Autumn?new Color(.49f,.43f,.22f):world.CurrentSeason==Season.Summer?new Color(.4f,.43f,.2f):new Color(.33f,.43f,.23f);
            if(mats.TryGetValue("Meadow",out var ground))ground.color=Color.Lerp(ground.color,grass,Time.unscaledDeltaTime*2);
            if(mats.TryGetValue("broad leaves",out var leaves))leaves.color=world.CurrentSeason==Season.Autumn?new Color(.64f,.29f,.10f):world.CurrentSeason==Season.Winter?new Color(.46f,.43f,.31f):new Color(.29f,.40f,.16f);
            float daylight=world.Night?.12f:Mathf.Sin(Mathf.Clamp01(world.clock/world.DaylightSeconds)*Mathf.PI)*.95f+.12f;
            float weatherDim=world.weather==Weather.Storm?.55f:world.weather==Weather.Rain||world.weather==Weather.Snow?.72f:1;
            sun.intensity=Mathf.Lerp(sun.intensity,daylight*weatherDim,Time.unscaledDeltaTime*2);sun.transform.rotation=Quaternion.Euler(world.Night?12:12+Mathf.Sin(world.clock/world.DaylightSeconds*Mathf.PI)*45,world.clock/360*210-70,0);
            RenderSettings.ambientLight=Color.Lerp(new Color(.21f,.27f,.39f),new Color(.52f,.6f,.53f),Mathf.Clamp01(daylight*weatherDim));
            foreach(var task in world.tasks)
            {
                if(!taskViews.TryGetValue(task.id,out var actor)){actor=new GameObject("Villager task");VillageArt.Unit(actor.transform,new Unit{kind=1},Mat);VillageArt.Combine(actor.transform,true);actor.AddComponent<ActorMotion>();taskViews[task.id]=actor;}
                var target=new Vector3(task.x,TerrainHeight(task.x,task.z),task.z);var dir=target-actor.transform.position;actor.transform.position=Vector3.Lerp(actor.transform.position,target,.5f);if(dir.sqrMagnitude>.005f)actor.transform.rotation=Quaternion.LookRotation(new Vector3(dir.x,0,dir.z));
            }
            foreach(var id in taskViews.Keys.Where(id=>!world.tasks.Any(t=>t.id==id)).ToArray()){Destroy(taskViews[id]);taskViews.Remove(id);}
            foreach(var town in townViews.Values){var name=town.transform.Find("Village name");if(name!=null)name.rotation=cam.transform.rotation;}
            livingViewTimer+=Time.unscaledDeltaTime;if(livingViewTimer<.3f)return;livingViewTimer=0;
            foreach(var n in world.resources)
            {
                int stage=n.amount>0?2:n.kind==ResourceKind.Tree&&n.regrow<=180&&n.regrow>0?1:0;
                if(nodeStages.TryGetValue(n.id,out int previous)&&previous==stage)continue;
                if(nodeViews.TryGetValue(n.id,out var old))Destroy(old);nodeViews[n.id]=NodeView(n);nodeStages[n.id]=stage;
            }
            foreach(var v in world.villages)
            {
                int stage=v.defeated?-1:v.buildings*10+(v.warWithPlayer?1:0);
                if(townStages.TryGetValue(v.id,out int previous)&&previous==stage)continue;
                if(townViews.TryGetValue(v.id,out var old))Destroy(old);townViews[v.id]=TownView(v);townStages[v.id]=stage;
            }
        }
    }
}

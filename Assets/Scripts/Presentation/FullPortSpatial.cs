using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using LangTroTan.Domain;

namespace LangTroTan.Presentation
{
    public sealed class FullPortNode:MonoBehaviour
    {public string id;public Action<string> selected;void OnMouseUpAsButton(){if(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject())selected?.Invoke(id);}}
    public sealed partial class FullPortView
    {
        Dictionary<string,object> spatial,spatialCatalog;
        readonly Dictionary<string,GameObject> spatialNodes=new Dictionary<string,GameObject>(),spatialActors=new Dictionary<string,GameObject>();
        readonly Dictionary<string,int> spatialStages=new Dictionary<string,int>();
        readonly Dictionary<string,Vector3> spatialActorTargets=new Dictionary<string,Vector3>();
        Light spatialSun;ParticleSystem spatialRain;string spatialWeather="";
        bool spatialPolling,physicalPointerDown,physicalDragged;float spatialPollAt,spatialFullPollAt,physicalZoom=21,physicalPinch;double spatialStaticAt=-1;
        Vector2 physicalPointerStart,physicalPointerLast;Vector3 physicalFocus=new Vector3(0,4,0);
        string placingSpatial;int placingRotation,placingX=4,placingZ=5,spatialCatalogPage;
        GameObject spatialGhost;RectTransform placementControls;
        void ApplySpatial(Dictionary<string,object> area)
        {
            if(spatial==null)
            {foreach(var root in models.Values)Destroy(root);models.Clear();levels.Clear();spatial=new Dictionary<string,object>();}
            double timestamp=N(Get(area,"lastAdvancedAt"));bool newer=timestamp>=N(Get(spatial,"lastAdvancedAt"));bool staticNewer=timestamp>=spatialStaticAt;
            foreach(var item in area){bool geometry=item.Key=="buildings"||item.Key=="nodes";if(geometry?staticNewer:newer)spatial[item.Key]=item.Value;}
            if(area.ContainsKey("buildings")&&staticNewer)spatialStaticAt=timestamp;
            if(spatialCatalog==null)spatialCatalog=Map(LangTroTan.Infrastructure.ServerJson.Parse(Resources.Load<TextAsset>("FullPort/SpatialCatalog").text));
            DrawSpatialModels();RefreshSpatialInfo();if(api.Snapshot!=null)RefreshHeader();
        }
        void DrawSpatialModels()
        {
            var buildings=List(Get(spatial,"buildings"));var alive=new HashSet<string>();
            foreach(var raw in buildings)
            {
                var b=Map(raw);string id=S(Get(b,"id")),type=S(Get(b,"type"));int level=(int)N(Get(b,"level"));alive.Add(id);
                if(SliceEnabled&&models.TryGetValue(id,out var current)&&levels.TryGetValue(id,out int before)&&before!=level&&SonMonArt.TrySwapLevel(current.transform,type,level))levels[id]=level;
                if(!models.TryGetValue(id,out var root)||!levels.TryGetValue(id,out int previous)||previous!=level)
                {
                    if(root!=null)Destroy(root);root=new GameObject(type);var model=new Building{type=type,level=Mathf.Clamp(level,1,4),rotation=(int)N(Get(b,"rotation")),complete=true};
                    if(SliceEnabled&&SonMonArt.TryBuilding(root.transform,type,level)){}else{if(SliceEnabled)SliceArt.Building(root.transform,new Building{type=type,level=model.level,rotation=0,complete=true},Material);else VillageArt.Building(root.transform,model,Material);VillageArt.Combine(root.transform);}
                    var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,1,0);box.size=new Vector3(model.W,3,model.D);
                    if(SliceEnabled){var geometry=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(geometry.Length>0){var bounds=geometry[0].bounds;foreach(var renderer in geometry.Skip(1))bounds.Encapsulate(renderer.bounds);box.center=bounds.center;box.size=bounds.size;}root.transform.rotation=Quaternion.Euler(0,90*(int)N(Get(b,"rotation")),0);}
                    var marker=root.AddComponent<FullPortBuilding>();marker.serverId=id;
                    marker.selected=selected=>{if(physicalDragged||placingSpatial!=null)return;ShowSpatialBuilding(selected);};models[id]=root;levels[id]=level;
                }
                var def=Get(spatialCatalog,type);bool rotated=((int)N(Get(b,"rotation")))%2==1;float width=(float)N(Get(def,rotated?"depth":"width")),depth=(float)N(Get(def,rotated?"width":"depth"));
                root.transform.position=new Vector3((float)N(Get(b,"x"))+width*.5f,0,(float)N(Get(b,"z"))+depth*.5f);
                bool complete=Get(b,"complete") is bool yes&&yes;root.transform.localScale=Vector3.one*(complete?1:.55f);
            }
            foreach(string id in models.Keys.Where(id=>!alive.Contains(id)).ToArray()){Destroy(models[id]);models.Remove(id);levels.Remove(id);}
            int season=(int)N(Get(spatial,"environment","season"));var nodeAlive=new HashSet<string>();
            foreach(var node in List(Get(spatial,"nodes")))
            {
                string id=S(Get(node,"id")),kind=S(Get(node,"kind"));nodeAlive.Add(id);bool full=N(Get(node,"amount"))>0;int stage=(full?2:0)+(kind=="tree"?season*10:0);
                if(spatialStages.TryGetValue(id,out int previous)&&previous==stage)continue;
                if(spatialNodes.TryGetValue(id,out var old))Destroy(old);var root=new GameObject(kind);bool authored=false;root.transform.position=new Vector3((float)N(Get(node,"x")),0,(float)N(Get(node,"z")));
                if(kind=="deer"&&full){authored=SliceEnabled&&SonMonArt.TryCreature(root.transform,StableTreeSeed(id));if(!authored)VillageArt.Deer(root.transform,Material);}
                else if(kind=="stone"&&SliceEnabled&&(authored=SonMonArt.TryRock(root.transform,StableTreeSeed(id),full))){}
                else if(kind=="stone")VillageArt.Rock(root.transform,new Vector3(0,full?.5f:.12f,0),Vector3.one*(full?1.2f:.3f),Material("spatial stone",new Color(.46f,.51f,.48f)));
                else if(SliceEnabled&&kind=="tree"&&full){authored=SonMonArt.TryTree(root.transform,StableTreeSeed(id),season);if(!authored)SliceArt.Tree(root.transform,season,StableTreeSeed(id),Material);}
                else
                {
                    SpatialPart(root.transform,PrimitiveType.Cylinder,new Vector3(0,full?.8f:.18f,0),new Vector3(.22f,full?.8f:.18f,.22f),Material("spatial bark",new Color(.28f,.19f,.12f)));
                    if(full){var leaf=Material("spatial leaves "+season,new[]{new Color(.32f,.54f,.23f),new Color(.23f,.42f,.18f),new Color(.71f,.42f,.13f),new Color(.74f,.8f,.76f)}[season]);
                        VillageArt.Rock(root.transform,new Vector3(0,2.35f,0),new Vector3(2,2,2),leaf);
                        VillageArt.Rock(root.transform,new Vector3(-.53f,1.8f,.24f),new Vector3(1.45f,1.35f,1.5f),leaf);
                        VillageArt.Rock(root.transform,new Vector3(.5f,1.85f,-.22f),new Vector3(1.5f,1.6f,1.4f),leaf);
                    }
                }
                if(!authored)VillageArt.Combine(root.transform);var collider=root.AddComponent<BoxCollider>();float height=full?(kind=="tree"?3:1.5f):.4f;collider.center=Vector3.up*height*.5f;collider.size=new Vector3(1.3f,height,1.3f);
                var marker=root.AddComponent<FullPortNode>();marker.id=id;marker.selected=selected=>{if(!physicalDragged&&placingSpatial==null)ShowSpatialNode(selected);};spatialNodes[id]=root;spatialStages[id]=stage;
            }
            foreach(string id in spatialNodes.Keys.Where(id=>!nodeAlive.Contains(id)).ToArray()){Destroy(spatialNodes[id]);spatialNodes.Remove(id);spatialStages.Remove(id);}
            var jobs=List(Get(spatial,"jobs"));var jobsAlive=new HashSet<string>();
            foreach(var job in jobs)
            {
                string id=S(Get(job,"id"));jobsAlive.Add(id);var target=new Vector3((float)N(Get(job,"x")),0,(float)N(Get(job,"z")));
                if(!spatialActors.TryGetValue(id,out var actor)){actor=new GameObject("Dân phục dịch");actor.transform.position=target;if(SliceEnabled)SliceArt.Character(actor.transform,S(Get(job,"kind")),Material);else VillageArt.Worker(actor.transform,S(Get(job,"kind")),Material);VillageArt.Combine(actor.transform,true);actor.AddComponent<ActorMotion>();spatialActors[id]=actor;}
                spatialActorTargets[id]=target;
            }
            foreach(string id in spatialActors.Keys.Where(id=>!jobsAlive.Contains(id)).ToArray()){Destroy(spatialActors[id]);spatialActors.Remove(id);spatialActorTargets.Remove(id);}
        }
        static int StableTreeSeed(string id){int value=0;foreach(char c in id)value=(value*31+c)&0x7fffffff;return value;}
        void SpatialPart(Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {var part=GameObject.CreatePrimitive(type);part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=scale;part.GetComponent<Renderer>().sharedMaterial=material;Destroy(part.GetComponent<Collider>());}
        void DecorateSpatialGround(GameObject ground)
        {
            var texture=new Texture2D(256,256,TextureFormat.RGB24,false);texture.name="Cỏ Sơn Môn";texture.wrapMode=TextureWrapMode.Repeat;
            for(int y=0;y<256;y++)for(int x=0;x<256;x++){
                float broad=Mathf.PerlinNoise(x*.032f+7,y*.032f+13),grain=Mathf.PerlinNoise(x*.43f,y*.43f);
                texture.SetPixel(x,y,Color.Lerp(new Color(.39f,.44f,.24f),new Color(.70f,.76f,.40f),broad)*(.82f+grain*.25f));
            }
            texture.Apply(false,true);var grass=ground.GetComponent<Renderer>().sharedMaterial;grass.color=new Color(.65f,.77f,.47f);grass.mainTexture=texture;grass.mainTextureScale=Vector2.one*3;
            var paths=new GameObject("Lối đi Sơn Môn");var earth=Material("spatial footpath",new Color(.48f,.41f,.27f));
            foreach(var point in new[]{new Vector3(-8.5f,0,5.5f),new Vector3(-7.5f,0,-6.5f),new Vector3(8.5f,0,5.5f),new Vector3(8.5f,0,-6.5f),new Vector3(14.5f,0,1.5f)}){
                var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(paths.transform,false);part.transform.position=point*.5f+new Vector3(0,.014f,0);part.transform.localScale=new Vector3(1.15f,.02f,point.magnitude);part.transform.rotation=Quaternion.LookRotation(point);part.GetComponent<Renderer>().sharedMaterial=earth;Destroy(part.GetComponent<Collider>());
            }
            VillageArt.Combine(paths.transform);
        }
        IEnumerator PollSpatial(bool full=false)
        {spatialPolling=true;yield return api.SpatialState(!full,ApplySpatial);spatialPolling=false;spatialPollAt=Time.unscaledTime+.5f;}
        void UpdateSpatial()
        {
            if(spatial==null||api==null)return;
            if(!spatialPolling&&Time.unscaledTime>=spatialPollAt){bool full=Time.unscaledTime>=spatialFullPollAt;if(full)spatialFullPollAt=Time.unscaledTime+5;StartCoroutine(PollSpatial(full));}
            foreach(var pair in spatialActors)
            {if(!spatialActorTargets.TryGetValue(pair.Key,out var target))continue;var delta=target-pair.Value.transform.position;pair.Value.transform.position=Vector3.Lerp(pair.Value.transform.position,target,Mathf.Min(1,Time.unscaledDeltaTime*7));if(delta.sqrMagnitude>.0002f)pair.Value.transform.rotation=Quaternion.LookRotation(delta);}
            UpdateSpatialWeather();HandleSpatialCamera();if(placingSpatial!=null)UpdateSpatialGhost();
        }
        void UpdateSpatialWeather()
        {
            string weather=S(Get(spatial,"environment","weather"));bool night=Get(spatial,"environment","night") is bool value&&value;float light=night?.32f:1.05f;if(weather=="storm")light*=.65f;else if(weather=="rain"||weather=="snow")light*=.8f;
            bool authoredSky=SliceEnabled&&SonMonArt.ApplyTimeOfDay((float)N(Get(spatial,"environment","phase")),(int)N(Get(spatial,"environment","season")),night,weather,spatialSun,cam,Mathf.Min(1,Time.unscaledDeltaTime*1.5f));
            if(spatialSun!=null&&!authoredSky)spatialSun.intensity=Mathf.Lerp(spatialSun.intensity,SliceEnabled?light*1.3f:light,Time.unscaledDeltaTime);if(!SliceEnabled)RenderSettings.ambientLight=night?new Color(.24f,.31f,.42f):new Color(.48f,.57f,.50f);
            if(SliceEnabled&&!authoredSky){var sky=night?new Color(.16f,.25f,.34f):new Color(.65f,.77f,.75f);RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,sky,Time.unscaledDeltaTime);cam.backgroundColor=RenderSettings.fogColor;Shader.SetGlobalColor("_XianxiaFog",RenderSettings.fogColor);RenderSettings.ambientSkyColor=Color.Lerp(RenderSettings.ambientSkyColor,night?new Color(.24f,.34f,.48f):new Color(.66f,.77f,.8f),Time.unscaledDeltaTime);spatialSun.color=Color.Lerp(spatialSun.color,night?new Color(.58f,.72f,1):new Color(1,.89f,.73f),Time.unscaledDeltaTime);}
            if(spatialRain==null)
            {
                var root=new GameObject("Mưa và tuyết");root.transform.rotation=Quaternion.Euler(90,0,0);spatialRain=root.AddComponent<ParticleSystem>();spatialRain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=spatialRain.main;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=600;main.startLifetime=3;
                var shape=spatialRain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(52,52,1);var renderer=root.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=SliceEnabled&&sliceAtmosphereMaterial!=null?sliceAtmosphereMaterial:new Material(Shader.Find("Sprites/Default"));
            }
            spatialRain.transform.position=physicalFocus+Vector3.up*18;
            if(spatialWeather==weather)return;spatialWeather=weather;var emission=spatialRain.emission;emission.rateOverTime=weather=="storm"?180:weather=="rain"?110:weather=="snow"?80:0;
            var settings=spatialRain.main;settings.startSpeed=weather=="snow"?2:12;settings.startSize=weather=="snow"?.085f:.022f;settings.startColor=weather=="snow"?new Color(.94f,.97f,1,.8f):new Color(.6f,.75f,.86f,.65f);if(!spatialRain.isPlaying)spatialRain.Play();
        }
        Vector3 SpatialGround(Vector2 point)
        {var ray=cam.ScreenPointToRay(point);var plane=new Plane(Vector3.up,Vector3.zero);return plane.Raycast(ray,out float distance)?ray.GetPoint(distance):Vector3.zero;}
        void SetSpatialCamera(){physicalZoom=Mathf.Clamp(physicalZoom,10,42);physicalFocus.x=Mathf.Clamp(physicalFocus.x,-38,38);physicalFocus.z=Mathf.Clamp(physicalFocus.z,-38,38);cam.orthographicSize=physicalZoom;cam.transform.position=physicalFocus+(SliceEnabled?new Vector3(31,29,-38).normalized*(physicalZoom*2.7f):new Vector3(37,42,-42));cam.transform.LookAt(physicalFocus);}
        void HandleSpatialCamera()
        {
            if(initialSmoke||dialog!=null)return;
            if(UnityEngine.Input.touchCount>=2){float pinch=Vector2.Distance(UnityEngine.Input.GetTouch(0).position,UnityEngine.Input.GetTouch(1).position);if(physicalPinch>0){physicalZoom-=(pinch-physicalPinch)*.025f;SetSpatialCamera();}physicalPinch=pinch;physicalPointerDown=false;return;}physicalPinch=0;
            if(SpatialPointerOverUI()){physicalPointerDown=false;return;}
            float wheel=UnityEngine.Input.mouseScrollDelta.y;if(Mathf.Abs(wheel)>.001f){physicalZoom-=wheel*2;SetSpatialCamera();}
            if(UnityEngine.Input.GetMouseButtonDown(0)){physicalPointerDown=true;physicalDragged=false;physicalPointerStart=physicalPointerLast=UnityEngine.Input.mousePosition;}
            if(UnityEngine.Input.GetMouseButton(0)&&physicalPointerDown)
            {
                Vector2 point=UnityEngine.Input.mousePosition;if((point-physicalPointerStart).magnitude>8)physicalDragged=true;
                if(physicalDragged){var move=point-physicalPointerLast;var right=new Vector3(cam.transform.right.x,0,cam.transform.right.z).normalized;var forward=Vector3.Cross(right,Vector3.up);physicalFocus-=(right*move.x+forward*move.y)*(physicalZoom*2/Screen.height);SetSpatialCamera();}physicalPointerLast=point;
            }
            if(UnityEngine.Input.GetMouseButtonUp(0)&&physicalPointerDown){physicalPointerDown=false;if(!physicalDragged&&placingSpatial!=null){var p=SpatialGround(UnityEngine.Input.mousePosition);placingX=Mathf.FloorToInt(p.x);placingZ=Mathf.FloorToInt(p.z);UpdateSpatialGhost();}}
        }
        static bool SpatialPointerOverUI()
        {if(EventSystem.current==null)return false;return UnityEngine.Input.touchCount>0?EventSystem.current.IsPointerOverGameObject(UnityEngine.Input.GetTouch(0).fingerId):EventSystem.current.IsPointerOverGameObject();}
        void BeginSpatialPlace(string type)
        {
            CancelSpatialPlace();CloseSpatialDetail();CloseDialog();body.gameObject.SetActive(false);placingSpatial=type;placingRotation=0;placingX=4;placingZ=5;
            spatialGhost=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(spatialGhost.GetComponent<Collider>());spatialGhost.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find(SliceEnabled?"Universal Render Pipeline/Unlit":"Standard"));
            placementControls=SliceEnabled?Surface(canvas,"Đặt công trình",254,681,1330,111,paper):Box(canvas,"Đặt công trình",254,681,1330,111,ink);
            if(SliceEnabled)Ink(placementControls,"Chạm đất để chọn vị trí · "+S(Get(spatialCatalog,type,"name")),18,10,1260,36,21,true);else Label(placementControls,"Chạm đất để chọn vị trí · "+S(Get(spatialCatalog,type,"name")),18,10,760,36,21,true);
            if(SliceEnabled){SliceButton(placementControls,"XOAY",18,56,230,42,()=>{placingRotation=(placingRotation+1)%4;UpdateSpatialGhost();});SliceButton(placementControls,"XÁC NHẬN XÂY",265,56,504,42,ConfirmSpatialPlace,true);SliceButton(placementControls,"HỦY",788,56,521,42,CancelSpatialPlace);}
            else{Button(placementControls,"XOAY",18,56,230,42,()=>{placingRotation=(placingRotation+1)%4;UpdateSpatialGhost();});Button(placementControls,"XÁC NHẬN XÂY",265,56,504,42,ConfirmSpatialPlace);Button(placementControls,"HỦY",788,56,521,42,CancelSpatialPlace);}UpdateSpatialGhost();
        }
        void ConfirmSpatialPlace(){if(api.Submit("spatial_place",new Dictionary<string,object>{{"type",placingSpatial},{"x",placingX},{"z",placingZ},{"rotation",placingRotation}}))CancelSpatialPlace();}
        void UpdateSpatialGhost()
        {
            if(spatialGhost==null)return;var def=Get(spatialCatalog,placingSpatial);bool rotated=placingRotation%2==1;float width=(float)N(Get(def,rotated?"depth":"width")),depth=(float)N(Get(def,rotated?"width":"depth"));
            bool valid=true;for(int x=placingX;x<placingX+width;x++)for(int z=placingZ;z<placingZ+depth;z++)if(x*x+z*z>24*24||SpatialOccupied(x,z))valid=false;
            if(List(Get(spatial,"nodes")).Any(n=>N(Get(n,"amount"))>0&&N(Get(n,"x"))>=placingX&&N(Get(n,"x"))<placingX+width&&N(Get(n,"z"))>=placingZ&&N(Get(n,"z"))<placingZ+depth))valid=false;
            spatialGhost.transform.position=new Vector3(placingX+width/2,.15f,placingZ+depth/2);spatialGhost.transform.localScale=new Vector3(width,.12f,depth);spatialGhost.GetComponent<Renderer>().sharedMaterial.color=valid?jade:new Color(.8f,.2f,.16f);
        }
        bool SpatialOccupied(int x,int z)
        {return List(Get(spatial,"buildings")).Any(b=>{var d=Get(spatialCatalog,S(Get(b,"type")));bool r=(int)N(Get(b,"rotation"))%2==1;return x>=N(Get(b,"x"))&&x<N(Get(b,"x"))+N(Get(d,r?"depth":"width"))&&z>=N(Get(b,"z"))&&z<N(Get(b,"z"))+N(Get(d,r?"width":"depth"));});}
        void CancelSpatialPlace(){placingSpatial=null;if(spatialGhost!=null){Destroy(spatialGhost.GetComponent<Renderer>().sharedMaterial);Destroy(spatialGhost);}if(placementControls!=null)Destroy(placementControls.gameObject);}
    }
}

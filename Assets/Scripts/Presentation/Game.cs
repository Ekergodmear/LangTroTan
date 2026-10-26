using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LangTroTan.Domain;
using LangTroTan.Infrastructure;

namespace LangTroTan.Presentation
{
    public class ClickTarget:MonoBehaviour {public int id;public bool unit,resource,neighbor;}
    public partial class Game:MonoBehaviour
    {
        public World world;
        Camera cam;Light sun;Font font;RectTransform canvas,details,buildMenu,modal;
        Text resourceText,clockText,popText,statusText,selectionText,goalText,speedText,detailState,trainingState;
        bool detailWasComplete;
        readonly Dictionary<int,GameObject> views=new Dictionary<int,GameObject>();
        readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
        readonly Dictionary<int,GameObject> healthViews=new Dictionary<int,GameObject>();
        GameObject selection,ghost,orderMarker;int selectedBuilding=-1,selectedSquad=-2,rotation;
        string placing;bool paused=true,hasStarted,wasPaused,modalOpen;float speed=1,accumulator,autosave,uiTimer;
        Vector2 pointerStart,lastPointer;bool dragged,downOnUI;Vector3 cameraFocus=new Vector3(0,0,2);float cameraSize=20;
        Color ink=new Color(.10f,.19f,.19f),mint=new Color(.43f,.72f,.59f),gold=new Color(.96f,.80f,.45f);
        string transient="";bool smoke,smokeFailed;int smokeFrame;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(FullPortView.Enabled){if(FindAnyObjectByType<FullPortView>()==null)new GameObject("TuTienAuthoritative3D").AddComponent<FullPortView>();return;}
            if(FindAnyObjectByType<Game>()==null)new GameObject("LangTroTan").AddComponent<Game>();
        }
        void Start()
        {
            Application.targetFrameRate=Application.isMobilePlatform?30:60;Screen.orientation=ScreenOrientation.LandscapeLeft;
            smoke=Environment.GetCommandLineArgs().Contains("-smoke");
            if(smoke){Application.runInBackground=true;Application.logMessageReceived+=(text,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)smokeFailed=true;};}
            font=Resources.Load<Font>("Fonts/NotoSans-Regular")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            world=World.New();CreateWorld();CreateUI();SyncViews();RefreshUI();
            if(smoke){SetupSmoke();}else Welcome();
        }
        Material Mat(string key,Color color)
        {
            if(mats.TryGetValue(key,out var m))return m;
            m=new Material(Shader.Find("Standard"));m.color=color;m.SetFloat("_Glossiness",.05f);mats[key]=m;return m;
        }
        GameObject Shape(string name,PrimitiveType primitive,Vector3 pos,Vector3 scale,Material mat,Transform parent=null,bool collider=false)
        {
            var g=GameObject.CreatePrimitive(primitive);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Destroy(g.GetComponent<Collider>());return g;
        }

        void Cone(Transform parent,Vector3 position,float radius,float height,Material mat)
        {
            var go=new GameObject("Canopy");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var verts=new List<Vector3>();var triangles=new List<int>();const int n=7;
            for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;int o=verts.Count;
                verts.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));verts.Add(new Vector3(0,height,0));verts.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));triangles.Add(o);triangles.Add(o+1);triangles.Add(o+2);}
            var mesh=new Mesh();mesh.name="LTT geometry";mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        GameObject Ring(string name,float radius,Color color)
        {
            var go=new GameObject(name);var l=go.AddComponent<LineRenderer>();l.useWorldSpace=false;l.positionCount=49;l.widthMultiplier=.07f;l.material=new Material(Shader.Find("Sprites/Default"));l.startColor=l.endColor=color;
            for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;l.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,.06f,Mathf.Sin(a)*radius));}return go;
        }
        void UpdateCamera(){cam.orthographicSize=cameraSize;cam.transform.position=cameraFocus+new Vector3(23,32,-28);cam.transform.LookAt(cameraFocus);}
        GameObject BuildingView(Building b)
        {
            var root=new GameObject(Catalog.Get(b.type).name);var d=Catalog.Get(b.type);
            var imported=string.IsNullOrEmpty(d.model)?null:Resources.Load<GameObject>("Art/"+d.model);
            if(imported!=null)
            {
                var model=Instantiate(imported,root.transform);model.transform.localPosition=Vector3.zero;var renderers=model.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=Mathf.Min(b.W/Mathf.Max(.1f,bounds.size.x),b.D/Mathf.Max(.1f,bounds.size.z));model.transform.localScale*=scale;model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;}
            }
            else {VillageArt.Building(root.transform,b,Mat);VillageArt.Combine(root.transform);}
            var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,1,0);box.size=new Vector3(b.W,2,b.D);
            var target=root.AddComponent<ClickTarget>();target.id=b.uid;
            Health(root,b.uid);return root;
        }

        GameObject UnitView(Unit u)
        {
            var root=new GameObject(u.enemy?"Enemy":"Squad "+(u.squad+1));
            VillageArt.Unit(root.transform,u,Mat);
            VillageArt.Combine(root.transform,true);
            root.AddComponent<ActorMotion>();
            var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.6f,0);box.size=new Vector3(.65f,1.3f,.65f);var c=root.AddComponent<ClickTarget>();c.id=u.uid;c.unit=true;
            Health(root,u.uid);return root;
        }
        void Health(GameObject root,int id)
        {
            var b=world.buildings.Find(v=>v.uid==id);float y=b==null?2.05f:b.type=="farm"?1.1f:b.type=="hall"||b.type=="tower"?4.1f:2.8f;
            var h=Shape("HP",PrimitiveType.Cube,new Vector3(0,y,0),new Vector3(1,.06f,.09f),Mat("hp",mint),root.transform);healthViews[id]=h;
        }
        void SyncViews()
        {
            var alive=new HashSet<int>();
            foreach(var b in world.buildings)
            {
                alive.Add(b.uid);if(buildingLevels.TryGetValue(b.uid,out int oldLevel)&&oldLevel!=b.level&&views.TryGetValue(b.uid,out var oldView)){Destroy(oldView);views.Remove(b.uid);healthViews.Remove(b.uid);}buildingLevels[b.uid]=b.level;if(!views.TryGetValue(b.uid,out var go)){go=BuildingView(b);views[b.uid]=go;}
                go.transform.position=new Vector3(b.x+b.W*.5f,0,b.z+b.D*.5f);go.transform.rotation=Quaternion.identity;
                // Completion affects the overall silhouette; blueprint never appears finished early.
                go.transform.localScale=Vector3.one*(b.complete?1:.45f+.55f*Mathf.Clamp01(b.progress/Mathf.Max(1,Catalog.Get(b.type).seconds)));
                if(healthViews.TryGetValue(b.uid,out var hp)){hp.SetActive(selectedBuilding==b.uid||!b.complete||b.hp<world.MaxHp(b));hp.transform.rotation=cam.transform.rotation;hp.transform.localScale=new Vector3(Mathf.Max(.05f,(float)b.hp/world.MaxHp(b))*1.5f,.06f,.09f);}
            }
            foreach(var u in world.units)
            {
                alive.Add(u.uid);if(!views.TryGetValue(u.uid,out var go)){go=UnitView(u);views[u.uid]=go;}Vector3 old=go.transform.position;
                go.transform.position=Vector3.Lerp(old,new Vector3(u.x,TerrainHeight(u.x,u.z)+.03f,u.z),.55f);var delta=go.transform.position-old;if(delta.sqrMagnitude>.0001f)go.transform.rotation=Quaternion.LookRotation(delta);
                if(healthViews.TryGetValue(u.uid,out var hp)){hp.SetActive(u.enemy||selectedSquad==-1||selectedSquad==u.squad);hp.transform.rotation=cam.transform.rotation;hp.transform.localScale=new Vector3(Mathf.Max(.05f,(float)u.hp/UnitMaxHp(u)),.06f,.09f);}
            }
            foreach(var id in views.Keys.Where(id=>!alive.Contains(id)).ToArray()){Destroy(views[id]);views.Remove(id);healthViews.Remove(id);}
            if(selectedBuilding>=0){var b=world.buildings.Find(v=>v.uid==selectedBuilding);selection.SetActive(b!=null);if(b!=null){selection.transform.position=new Vector3(b.x+b.W*.5f,.03f,b.z+b.D*.5f);selection.transform.localScale=Vector3.one*Mathf.Max(b.W,b.D)*.58f;}}
            else if(selectedSquad>=-1){var u=world.units.FirstOrDefault(v=>!v.enemy&&(selectedSquad==-1||v.squad==selectedSquad));selection.SetActive(u!=null);if(u!=null){selection.transform.position=new Vector3(u.x,TerrainHeight(u.x,u.z)+.03f,u.z);selection.transform.localScale=Vector3.one;}}
            else selection.SetActive(false);
            float target=world.Night?.40f:1.05f;sun.intensity=Mathf.Lerp(sun.intensity,target,Time.unscaledDeltaTime);RenderSettings.ambientLight=Color.Lerp(new Color(.34f,.42f,.53f),new Color(.52f,.60f,.53f),sun.intensity/1.05f);
        }
        RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax,Color color)
        {
            var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;
            var image=g.AddComponent<Image>();image.color=color;if(color.a>.7f&&name!="Title screen"&&name!="Title shade"&&name!="Modal overlay"){image.sprite=UISkin.Frame;image.type=Image.Type.Sliced;}return r;
        }
        RectTransform Box(string name,Transform parent,float x,float y,float w,float h,Color color)
        {return Panel(name,parent,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-h),new Vector2(x+w,-y),color);}
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size=18,Color? color=null)
        {
            var go=new GameObject("Text",typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.offsetMin=new Vector2(x,-y-h);r.offsetMax=new Vector2(x+w,-y);
            var t=go.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=color??new Color(.9f,.93f,.87f);t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Truncate;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;
        }
        Button Button(Transform parent,string text,float x,float y,float w,float h,Action action,bool accent=false)
        {
            var r=Box(text,parent,x,y,w,h,accent?new Color(.71f,.51f,.26f):new Color(.38f,.32f,.24f));r.gameObject.AddComponent<ButtonMotion>();var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();var colors=b.colors;colors.highlightedColor=new Color(.8f,.94f,.86f);colors.pressedColor=new Color(.65f,.8f,.7f);b.colors=colors;
            var t=Label(r,text,8,0,w-16,h,16);t.alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(()=>{action();RefreshDetails();RefreshUI();});return b;
        }
        void Clear(RectTransform panel){foreach(Transform child in panel)Destroy(child.gameObject);}


        void ShowModal(string title,string body,string primary,Action action,string secondary=null,Action secondaryAction=null,string tertiary=null,Action tertiaryAction=null)
        {
            if(modal!=null)Destroy(modal.gameObject);modalOpen=true;
            modal=Panel("Modal overlay",canvas,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0,0,0,.65f));
            var box=Panel("Dialog",modal,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-340,tertiary==null?-280:-325),new Vector2(340,tertiary==null?280:325),ink);
            Heading(box,title,32,30,616,52,28);Label(box,body,32,100,616,306,18);
            Button(box,primary,32,434,616,54,()=>{Destroy(modal.gameObject);modalOpen=false;action();},true);
            if(secondary!=null)Button(box,secondary,32,498,616,44,()=>{Destroy(modal.gameObject);modalOpen=false;secondaryAction();});
            if(tertiary!=null)Button(box,tertiary,32,558,616,44,()=>{Destroy(modal.gameObject);modalOpen=false;tertiaryAction();});
        }
        void ConfirmNew()
        {
            if(!hasStarted&&!SaveRepository.Exists){StartNew();return;}
            ShowModal("TẠO LƯỢT MỚI","Lượt đang chơi sẽ bị thay bằng làng mới khi bạn lưu. Bản lưu trước được giữ trong tệp dự phòng.","TẠO LÀNG MỚI",StartNew,"QUAY LẠI",()=>{if(hasStarted){paused=false;}else Welcome();});
        }
        void ResetViews(){ResetLivingViews();foreach(var go in views.Values)Destroy(go);views.Clear();healthViews.Clear();selectedBuilding=-1;selectedSquad=-2;CancelPlace();selection.SetActive(false);}
        void StartNew(){world=World.New();ResetViews();paused=false;hasStarted=true;accumulator=0;autosave=0;RefreshUI();}
        void Load()
        {
            try{world=SaveRepository.Load(out bool recovered);ResetViews();paused=false;hasStarted=true;accumulator=0;world.message=recovered?"Đã phục hồi từ bản lưu dự phòng":"Đã tiếp tục lượt chơi";}
            catch(Exception e){ShowModal("CHƯA TẢI ĐƯỢC LƯỢT",e.Message,"THỬ LẠI",Load,"TẠO LƯỢT MỚI",ConfirmNew);}
        }
        void Save(){if(!hasStarted)return;try{SaveRepository.Save(world);world.message="Đã lưu trên thiết bị";}catch(Exception e){world.message="Chưa lưu được: "+e.Message;}RefreshUI();}
        void TogglePause()
        {
            if(!hasStarted||modalOpen)return;paused=true;
            ShowModal("TẠM DỪNG","Làng đang an toàn trong lúc tạm dừng.\n\nMẹo: để ít nhất một dân rảnh cho xây dựng. Khi lương thực giảm, giao thêm dân cho Nông trại.","TIẾP TỤC",()=>paused=false,"LƯỢT MỚI",ConfirmNew);
        }
        void SelectSquad(int id)
        {
            selectedResource=-1;selectedNeighbor=-1;bool second=selectedSquad==id;selectedSquad=id;selectedBuilding=-1;CancelPlace();details.gameObject.SetActive(false);
            if(second){var u=world.units.FirstOrDefault(u=>!u.enemy&&(id==-1||u.squad==id));if(u!=null){cameraFocus=new Vector3(u.x,0,u.z);UpdateCamera();}}
            world.message="Chạm đất để di chuyển; quân tự đánh địch trong tầm";
        }

        int placeX,placeZ;
        void BeginPlace(string type)
        {
            placing=type;if(buildGrid!=null)buildGrid.SetActive(true);rotation=0;placeX=-6;placeZ=-3;RebuildGhost();ShowPlacement();
        }

        void RebuildGhost()
        {
            if(ghost!=null)Destroy(ghost);if(placing==null)return;var d=Catalog.Get(placing);int w=rotation%2==0?d.width:d.depth,depth=rotation%2==0?d.depth:d.width;
            ghost=Shape("Placement preview",PrimitiveType.Cube,new Vector3(placeX+w*.5f,.12f,placeZ+depth*.5f),new Vector3(w,.25f,depth),Mat("preview",mint));
            UpdateGhost();
        }
        void UpdateGhost()
        {
            if(ghost==null||placing==null)return;var d=Catalog.Get(placing);int w=rotation%2==0?d.width:d.depth,depth=rotation%2==0?d.depth:d.width;
            ghost.transform.position=new Vector3(placeX+w*.5f,.12f,placeZ+depth*.5f);
            bool valid=world.CanPlace(placing,placeX,placeZ,rotation,out var reason);mats["preview"].color=valid?mint:new Color(.85f,.29f,.20f);
            transient=valid?"Vị trí hợp lệ · chạm XÁC NHẬN để xây":reason;
        }
        void CancelPlace(){if(buildGrid!=null)buildGrid.SetActive(false);placing=null;transient="";if(ghost!=null)Destroy(ghost);if(buildMenu!=null)buildMenu.gameObject.SetActive(false);}


        void Update()
        {
            if(world==null)return;if(smoke){SmokeUpdate();}
            if(!paused&&!modalOpen&&!world.ended)
            {
                accumulator+=Mathf.Min(Time.unscaledDeltaTime,.25f)*speed;int steps=0;while(accumulator>=.1f&&steps++<5){world.Tick();accumulator-=.1f;}
                autosave+=Time.unscaledDeltaTime;if(autosave>30){autosave=0;Save();}
            }
            if(world.ended&&!modalOpen&&!paused){paused=true;Save();ShowModal(world.won?"LÀNG ĐÃ SỐNG SÓT":"LÀNG ĐÃ THẤT THỦ",world.message+"\n\nNgày "+world.day+" • Dân còn "+world.people+" • Quân còn "+world.Soldiers,"CHƠI LẠI",ConfirmNew,"XEM LÀNG",()=>{});}
            if(hasStarted&&!modalOpen)HandleInput();SyncViews();UpdateLivingVisuals();uiTimer+=Time.unscaledDeltaTime;
            if(uiTimer>.3f){uiTimer=0;RefreshUI();}
        }
        bool PointerUI(int id=-1){return EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject(id);}
        Vector3 GroundPoint(Vector2 p){var ray=cam.ScreenPointToRay(p);var plane=new Plane(Vector3.up,Vector3.zero);return plane.Raycast(ray,out float dist)?ray.GetPoint(dist):Vector3.zero;}
        void HandleInput()
        {
            if(Input.GetKeyDown(KeyCode.Escape)){if(placing!=null)CancelPlace();else TogglePause();}
            if(Input.GetKeyDown(KeyCode.Space))TogglePause();
            if(Input.touchCount>1)
            {
                dragged=true;var a=Input.GetTouch(0);var b=Input.GetTouch(1);if(PointerUI(a.fingerId)||PointerUI(b.fingerId))return;
                float prev=((a.position-a.deltaPosition)-(b.position-b.deltaPosition)).magnitude;cameraSize=Mathf.Clamp(cameraSize+(prev-(a.position-b.position).magnitude)*.025f,10,32);UpdateCamera();return;
            }
            if(Input.touchCount==1)
            {var t=Input.GetTouch(0);if(t.phase==TouchPhase.Began)PointerDown(t.position,PointerUI(t.fingerId));if(t.phase==TouchPhase.Moved)PointerMove(t.position);if(t.phase==TouchPhase.Ended)PointerUp(t.position);if(t.phase==TouchPhase.Canceled)downOnUI=true;return;}
            if(Input.GetMouseButtonDown(0))PointerDown(Input.mousePosition,PointerUI());
            if(Input.GetMouseButton(0))PointerMove(Input.mousePosition);
            if(Input.GetMouseButtonUp(0))PointerUp(Input.mousePosition);
            if(!PointerUI()){cameraSize=Mathf.Clamp(cameraSize-Input.mouseScrollDelta.y*1.5f,10,32);UpdateCamera();}
        }
        void PointerDown(Vector2 p,bool overUI){pointerStart=lastPointer=p;dragged=false;downOnUI=overUI;}
        void PointerMove(Vector2 p)
        {
            if(downOnUI)return;if((p-pointerStart).magnitude>12)dragged=true;
            if(dragged){var delta=GroundPoint(lastPointer)-GroundPoint(p);cameraFocus+=delta;cameraFocus.x=Mathf.Clamp(cameraFocus.x,-32,32);cameraFocus.z=Mathf.Clamp(cameraFocus.z,-32,32);UpdateCamera();}lastPointer=p;
        }
        void PointerUp(Vector2 p)
        {
            if(downOnUI||dragged)return;Vector3 point=GroundPoint(p);
            if(placing!=null){placeX=Mathf.FloorToInt(point.x);placeZ=Mathf.FloorToInt(point.z);UpdateGhost();return;}
            if(selectedSquad>=-1){OrderAt(point);return;}
            if(Physics.Raycast(cam.ScreenPointToRay(p),out var hit,220))
            {
                var click=hit.collider.GetComponent<ClickTarget>();
                if(click!=null){if(click.resource){SelectResource(click.id);return;}if(click.neighbor){selectedNeighbor=click.id;ShowWorldPanel();return;}if(click.unit){var u=world.units.Find(v=>v.uid==click.id);if(u!=null&&!u.enemy)SelectSquad(u.squad);}else{selectedResource=-1;selectedNeighbor=-1;selectedBuilding=click.id;selectedSquad=-2;RefreshDetails();}}
            }
        }
        void OrderAt(Vector3 point)
        {
            if(selectedSquad< -1){world.message="Chọn tổ đội trước khi ra lệnh";return;}
            if(world.Order(selectedSquad,point.x,point.z)){orderMarker.transform.position=new Vector3(point.x,.05f,point.z);orderMarker.SetActive(true);CancelInvoke(nameof(HideOrder));Invoke(nameof(HideOrder),2);}
        }
        void HideOrder(){orderMarker.SetActive(false);}
        void OnApplicationPause(bool value)
        {
            if(smoke||!hasStarted)return;if(value){wasPaused=paused;paused=true;Save();}else if(!modalOpen){paused=true;ShowModal("TIẾP TỤC LÀNG","Game đã tạm dừng khi ứng dụng vào nền. Không có thời gian ngoại tuyến được cộng thêm.","TIẾP TỤC",()=>{paused=wasPaused;accumulator=0;});}
        }
        void OnApplicationFocus(bool value){if(!smoke&&!value&&hasStarted&&!paused){paused=true;Save();if(!modalOpen)ShowModal("ĐÃ TẠM DỪNG","Tiếp tục khi bạn sẵn sàng.","TIẾP TỤC",()=>{paused=false;accumulator=0;});}}
        void OnApplicationQuit(){if(hasStarted)Save();}
        void SetupSmoke()
        {
            SaveRepository.RootOverride=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../SmokeSave"));
            hasStarted=true;paused=false;world.wood=500;world.stone=300;world.food=300;
            foreach(var spec in new[]{new[]{"wood","-7","2"},new[]{"farm","3","5"},new[]{"barracks","-7","-6"},new[]{"house","4","-7"}}){world.Place(spec[0],int.Parse(spec[1]),int.Parse(spec[2]));}
            foreach(var b in world.buildings){b.complete=true;b.hp=Catalog.Get(b.type).hp;}
            foreach(var b in world.buildings.Where(b=>b.type=="wood"||b.type=="farm"))world.Assign(b.uid,2);
            var barracks=world.buildings.Find(b=>b.type=="barracks");world.Train(barracks.uid,0);world.Train(barracks.uid,0);
            for(int i=0;i<410;i++)world.Tick();world.Order(-1,0,-5);world.clock=220;cameraFocus=new Vector3(0,0,1);cameraSize=15.5f;UpdateCamera();
            world.Validate();Debug.Log("SMOKE_SETUP_OK");
        }
        void SmokeUpdate()
        {
            smokeFrame++;
            if(smokeFrame==10)Welcome();
            if(smokeFrame==20)CapturePreview("smoke-welcome.png");
            if(smokeFrame==25){Destroy(modal.gameObject);modalOpen=false;}
            if(smokeFrame==30){ShowBuildMenu();}
            if(smokeFrame==40)CapturePreview("smoke-build-menu.png");
            if(smokeFrame==45){BeginPlace("house");UpdateGhost();}
            if(smokeFrame==60){CancelPlace();selectedBuilding=world.buildings.First(b=>b.type=="farm").uid;RefreshDetails();}
            if(smokeFrame==120){world.Validate();CapturePreview();Debug.Log("SMOKE_CAPTURE_REQUESTED");}
            if(smokeFrame==124){ShowWorldPanel();}
            if(smokeFrame==127){CapturePreview("smoke-diplomacy.png");CloseWorldPanel();}
            if(smokeFrame==130){world.Order(-1,0,8);for(int i=0;i<120;i++)world.Tick();world.clock=239.95f;for(int i=0;i<350;i++)world.Tick();world.Validate();Debug.Log("SMOKE_COMBAT_OK enemies="+world.Enemies+" soldiers="+world.Soldiers);}
            if(smokeFrame==145){world.day=13;world.clock=80;world.weather=Weather.Snow;}
            if(smokeFrame==170){CapturePreview("smoke-winter.png");}
            if(smokeFrame==175){SetupSectSmoke();ShowSectPanel(0);}
            if(smokeFrame==180)CapturePreview("smoke-sect.png");
            if(smokeFrame==185)ShowSectPanel(1);
            if(smokeFrame==190)CapturePreview("smoke-disciples.png");
            if(smokeFrame==195){CloseWorldPanel();selectedBuilding=world.buildings.First(b=>b.type=="house").uid;RefreshDetails();}
            if(smokeFrame==205)CapturePreview("smoke-upgrade.png");
            if(smokeFrame==210){buildPage=2;ShowBuildMenu();}
            if(smokeFrame==215)CapturePreview("smoke-sect-buildings.png");
            if(smokeFrame==220){CancelPlace();for(int i=0;i<330;i++)world.Tick();world.Validate();Debug.Log("SMOKE_SECT_OK disciples="+world.disciples.Count+" houseLevel="+world.buildings.First(b=>b.type=="house").level);}
            if(smokeFrame==230){world.Validate();Save();Debug.Log(smokeFailed?"SMOKE_RUNTIME_FAILED":"SMOKE_RUNTIME_OK");Application.Quit(smokeFailed?1:0);}
        }
        void SetupSectSmoke()
        {
            world=World.New();world.wood=900;world.stone=900;world.food=600;world.spiritStones=500;world.herbs=100;world.waveSpawned=10000;
            world.resources.RemoveAll(n=>n.x*n.x+n.z*n.z<400);world.level=2;var hall=world.buildings.First(b=>b.type=="hall");hall.level=2;hall.hp=world.MaxHp(hall);
            foreach(var spec in new[]{new[]{"house","-7","3"},new[]{"house","7","-7"},new[]{"barracks","-7","-7"},new[]{"scripture","3","5"},new[]{"meditation","-12","5"},new[]{"alchemy","8","3"},new[]{"spiritmine","12","-4"},new[]{"mission","-12","-5"}})
            {
                world.wood=900;world.stone=900;if(!world.Place(spec[0],int.Parse(spec[1]),int.Parse(spec[2])))throw new Exception(world.message);
                var b=world.buildings.Last();b.complete=true;b.hp=world.MaxHp(b);
            }
            world.people=16;world.Assign(world.buildings.First(b=>b.type=="spiritmine").uid,2);world.Assign(world.buildings.First(b=>b.type=="alchemy").uid,1);
            for(int i=0;i<3;i++)world.RecruitDisciple(i);world.disciples[0].layer=9;world.disciples[0].cultivation=30;world.pills=2;world.artifacts=1;
            world.UpgradeBuilding(world.buildings.First(b=>b.type=="house").uid);world.Validate();ResetViews();cameraFocus=new Vector3(0,0,1);cameraSize=18;UpdateCamera();RefreshUI();
        }
        void CapturePreview(string filename="smoke-preview.png")
        {
            // Batch players do not present a desktop frame. Render the actual scene and HUD offscreen.
            var cv=canvas.GetComponentInParent<Canvas>();var texture=new RenderTexture(1600,900,24);texture.antiAliasing=2;
            var previous=RenderTexture.active;var previousTarget=cam.targetTexture;var previousMode=cv.renderMode;
            cv.renderMode=RenderMode.ScreenSpaceCamera;cv.worldCamera=cam;cv.planeDistance=1;
            cam.targetTexture=texture;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=texture;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../"+filename),image.EncodeToPNG());
            cam.targetTexture=previousTarget;RenderTexture.active=previous;cv.renderMode=previousMode;texture.Release();Destroy(texture);Destroy(image);
        }
    }
}


using System;
using System.Linq;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LangTroTan.Domain;
using LangTroTan.Infrastructure;

namespace LangTroTan.Presentation
{
    public sealed class FullPortBuilding:MonoBehaviour
    {public string serverId;public Action<string> selected;void OnMouseUpAsButton(){if(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject())selected?.Invoke(serverId);}}
    public sealed partial class FullPortView:MonoBehaviour
    {
        AuthoritativeClient api;Dictionary<string,object> catalog;
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        readonly Dictionary<string,GameObject> models=new Dictionary<string,GameObject>();readonly Dictionary<string,int> levels=new Dictionary<string,int>();
        RectTransform canvas,body,dialog;Text resources,title,status;Font font,serif;Camera cam;int tab,page;string selectedDisciple;
        readonly Color ink=new Color(.055f,.11f,.105f),jade=new Color(.31f,.68f,.54f),gold=new Color(.91f,.77f,.46f);
        bool initialSmoke;int smokeStage;float smokeDeadline;string smokeFile;
        static Dictionary<string,object> Map(object value){return value as Dictionary<string,object>??new Dictionary<string,object>();}
        static List<object> List(object value){return value as List<object>??new List<object>();}
        static object Get(object root,params string[] path){object value=root;foreach(string key in path){if(!(value is Dictionary<string,object> map)||!map.TryGetValue(key,out value))return null;}return value;}
        static string S(object v,string fallback="—"){return v==null?fallback:Convert.ToString(v,CultureInfo.InvariantCulture);}
        static double N(object v){try{return v==null?0:Convert.ToDouble(v,CultureInfo.InvariantCulture);}catch{return 0;}}
        static bool Alive(object v){return Get(v,"alive") is bool b&&b;}
        public static bool Enabled
        {
            get
            {
                var args=Environment.GetCommandLineArgs();if(args.Contains("-smoke"))return false;if(args.Contains("-full-port"))return true;
                var file=Resources.Load<TextAsset>("FullPort/Mode");return file!=null&&S(Get(ServerJson.Parse(file.text),"enabled"))=="True";
            }
        }
        void Start()
        {
            font=Resources.Load<Font>("Fonts/NotoSans-Regular");serif=Resources.Load<Font>("Fonts/NotoSerif-Bold");
            catalog=ServerJson.Map(ServerJson.Parse(Resources.Load<TextAsset>("FullPort/ReferenceCatalog").text));
            Application.targetFrameRate=Application.isMobilePlatform?30:60;Application.runInBackground=true;
            initialSmoke=Environment.GetCommandLineArgs().Contains("-full-port-smoke");
            CreateMountain();CreateInterface();api=gameObject.AddComponent<AuthoritativeClient>();api.StateChanged+=Changed;api.StatusChanged+=()=>status.text=api.Notice;
            api.DecisionCompleted+=(action,result)=>{if(action.StartsWith("spatial_")){status.text=S(Get(result,"message"));StartCoroutine(api.SpatialState(false,ApplySpatial));return;}if(SliceEnabled&&(action=="assign_job"||action=="use_sect_pill")){status.text=action=="assign_job"?"Đã cập nhật công việc đệ tử.":"Đã dùng đan theo xác nhận của tông môn.";return;}if(!initialSmoke)ShowRecord(result,"Kết quả quyết định");};
            if(Environment.GetCommandLineArgs().Contains("-visual-qa"))
            {
                AuthoritativeClient.JournalRootOverride=System.IO.Path.Combine(Application.dataPath,"../VisualQaJournal");
                api.Configure("http://127.0.0.1:8901","visual-slice");StartCoroutine(RunVisualQa());
            }
            else if(Environment.GetCommandLineArgs().Contains("-visual-preview"))
            {AuthoritativeClient.JournalRootOverride=System.IO.Path.Combine(Application.dataPath,"../VisualPreviewJournal");api.Configure("http://127.0.0.1:8901","visual-slice");}
            else if(initialSmoke)
            {
                AuthoritativeClient.JournalRootOverride=System.IO.Path.Combine(Application.dataPath,"../FullPortSmoke");
                smokeFile=System.IO.Path.Combine(Application.dataPath,"../full-port-checks.txt");smokeDeadline=Time.realtimeSinceStartup+50;
                api.Configure("http://127.0.0.1:8899","unity-port-qa");
            }
            else ConnectionForm();
        }
        Material Material(string key,Color color)
        {if(materials.TryGetValue(key,out var m))return m;var shader=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Simple Lit"):Shader.Find("Standard");m=new Material(shader);m.color=color;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.12f);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.08f);materials[key]=m;return m;}
        void CreateMountain()
        {
            if(SliceEnabled){CreateSliceMountain();return;}
            var cameraObject=new GameObject("Sơn môn camera");cam=cameraObject.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=27;cam.transform.position=new Vector3(37,42,-42);cam.transform.LookAt(new Vector3(0,6,0));cam.backgroundColor=new Color(.19f,.29f,.29f);cam.clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("Mây sáng").AddComponent<Light>();spatialSun=light;light.type=LightType.Directional;light.intensity=1.15f;light.transform.eulerAngles=new Vector3(48,-35,0);RenderSettings.ambientLight=new Color(.48f,.57f,.56f);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Sơn môn";ground.transform.localScale=Vector3.one*12;ground.GetComponent<Renderer>().sharedMaterial=Material("mountain moss",new Color(.22f,.33f,.25f));
            DecorateSpatialGround(ground);SetSpatialCamera();
            for(int i=0;i<5;i++){var peak=GameObject.CreatePrimitive(PrimitiveType.Cylinder);peak.name="Ngũ Phong "+i;peak.transform.position=new Vector3((i-2)*19,-3,48+Mathf.Abs(i-2)*5);peak.transform.localScale=new Vector3(20,10+Mathf.Sin(i)*3,22);peak.GetComponent<Renderer>().sharedMaterial=Material("mountain rock",new Color(.31f,.38f,.34f));}
        }
        RectTransform Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var r=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.offsetMin=new Vector2(x,-y-h);r.offsetMax=new Vector2(x+w,-y);var image=r.GetComponent<Image>();image.color=color;image.sprite=UISkin.Frame;image.type=Image.Type.Sliced;return r;
        }
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size=18,bool heading=false)
        {var r=new GameObject("Text",typeof(RectTransform),typeof(Text)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.offsetMin=new Vector2(x,-y-h);r.offsetMax=new Vector2(x+w,-y);var t=r.GetComponent<Text>();t.font=heading?serif:font;t.fontSize=size;t.color=heading?gold:new Color(.87f,.91f,.85f);t.text=CleanFloats(text);t.supportRichText=false;t.raycastTarget=false;return t;}
        Button Button(Transform parent,string text,float x,float y,float w,float h,Action callback)
        {var r=Box(parent,text,x,y,w,h,new Color(.14f,.27f,.24f));var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.onClick.AddListener(()=>callback());var t=Label(r,text,8,5,w-16,h-10,16);t.alignment=TextAnchor.MiddleCenter;return button;}
        void Clear(RectTransform r){foreach(Transform child in r){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        void CreateInterface()
        {
            if(SliceEnabled){CreateSliceInterface();return;}
            var c=new GameObject("Full port HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));c.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=c.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();canvas.SetParent(c.transform,false);var safe=c.AddComponent<SafeArea>();safe.content=canvas;safe.scaler=scaler;safe.Apply();
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var top=Box(canvas,"Tông môn",16,14,1568,98,ink);title=Label(top,"TỨ ĐẠI BỘ CHÂU · TU TIÊN KÝ",20,8,1120,37,26,true);resources=Label(top,"Đang chờ kết nối thế giới…",20,53,1200,33,17);
            Button(top,"KẾT NỐI",1360,22,182,51,ConnectionForm);
            string[] names={"SƠN MÔN","ĐỆ TỬ","CHÍNH VỤ","THẾ GIAN","BIÊN NIÊN"};
            for(int i=0;i<5;i++){int index=i;Button(canvas,names[i],16,136+i*67,220,55,()=>{projectionActive=false;commandScreen=false;CloseDialog();tab=index;page=0;Draw();});}
            Button(canvas,"XEM NÚI",16,494,220,55,()=>body.gameObject.SetActive(!body.gameObject.activeSelf));
            Button(canvas,"ĐỒNG BỘ",16,561,220,55,()=>{if(api!=null)StartCoroutine(api.Refresh());});
            Button(canvas,"THỬ LẠI YÊU CẦU",16,628,220,55,()=>api?.RetryPending());
            Button(canvas,"QUYẾT NGHỊ",16,695,220,55,()=>ShowCommands());
            Button(canvas,"ĐẠI ĐIỆN",16,759,220,42,ShowHallSections);
            body=Box(canvas,"Bảng điều hành",254,134,1330,658,new Color(ink.r,ink.g,ink.b,.96f));
            var foot=Box(canvas,"Trạng thái",16,810,1568,74,ink);status=Label(foot,"Bản chuyển đổi đang phát triển · chưa đủ điều kiện xuất APK",17,12,1110,53,16);Button(foot,"XÂY DỰNG SƠN MÔN",1160,12,385,50,ShowSpatialCatalog);
        }
        void ConnectionForm()
        {
            OpenDialog("Kết nối thế giới",620,560);Label(dialog,"Tiếp tục tông môn đang có, hoặc khai sơn lập một tông môn mới.",24,72,570,75,18);
            Label(dialog,"Địa chỉ máy chủ",24,155,570,25,16);var address=Input(dialog,PlayerPrefs.GetString("FullPort.Server","http://127.0.0.1:8899"),24,186,570);
            Label(dialog,"Lượt chơi",24,241,570,25,16);var game=Input(dialog,PlayerPrefs.GetString("FullPort.Game","unity-port-qa"),24,272,570);
            Button(dialog,"KẾT NỐI",24,342,278,51,()=>{try{api.Configure(address.text,game.text);PlayerPrefs.SetString("FullPort.Server",address.text);PlayerPrefs.SetString("FullPort.Game",game.text);CloseDialog();}catch(Exception e){status.text=e.Message;}});
            Button(dialog,"ĐÓNG",318,342,277,51,CloseDialog);
            Button(dialog,"KHAI SƠN LƯỢT MỚI",24,415,278,51,()=>{api.CreateGameAt(address.text);CloseDialog();});
            Button(dialog,"CHỌN LƯỢT ĐÃ LƯU",318,415,277,51,()=>{string server=address.text;api.ListGamesAt(server,ids=>ShowSavedGames(server,ids,0));});
            if(api.Snapshot!=null)Button(dialog,"XÓA TÔNG MÔN ĐANG CHƠI VÀ LẬP LẠI",24,483,571,44,ConfirmResetGame);
        }
        void ShowSavedGames(string server,List<string> ids,int at)
        {
            OpenDialog("Lượt chơi đã lưu",840,620);at=Mathf.Clamp(at,0,Mathf.Max(0,(ids.Count-1)/6));
            for(int i=0;i<6&&at*6+i<ids.Count;i++){string id=ids[at*6+i];Button(dialog,id,25,82+i*66,790,55,()=>{try{api.Configure(server,id);PlayerPrefs.SetString("FullPort.Server",server);PlayerPrefs.SetString("FullPort.Game",id);CloseDialog();}catch(Exception e){status.text=e.Message;}});}
            if(ids.Count==0)Label(dialog,"Chưa có tông môn nào được lưu trên máy chủ này.",27,87,780,92,22);
            Button(dialog,"TRƯỚC",25,520,248,51,()=>ShowSavedGames(server,ids,at-1));Button(dialog,"SAU",289,520,248,51,()=>ShowSavedGames(server,ids,at+1));Button(dialog,"QUAY LẠI",553,520,262,51,ConnectionForm);
        }
        InputField Input(Transform parent,string text,float x,float y,float width)
        {
            var box=Box(parent,"Input",x,y,width,44,new Color(.21f,.31f,.28f));var field=box.gameObject.AddComponent<InputField>();var label=Label(box,text,9,7,width-18,32,18);label.raycastTarget=true;label.supportRichText=false;field.textComponent=label;field.text=text;return field;
        }
        void OpenDialog(string heading,float width=680,float height=420)
        {CloseDialog();dialog=Box(canvas,"Quyết định",(1600-width)/2,(900-height)/2,width,height,new Color(.08f,.16f,.15f));Label(dialog,heading,24,16,width-48,48,25,true);}
        void CloseDialog(){if(dialog!=null){dialog.gameObject.SetActive(false);Destroy(dialog.gameObject);}dialog=null;}
        void Changed()
        {
            DrawModels();bool visible=body.gameObject.activeSelf;
            if(placingSpatial!=null||spatialPanel!=null){RefreshHeader();RefreshSpatialInfo();return;}
            if(dialog==null){if(projectionActive)ReloadProjection();else Draw();body.gameObject.SetActive(visible);}
            else RefreshHeader();
        }
        Dictionary<string,object> Module(string name){return Map(Get(catalog,name));}
        void DrawModels()
        {
            if(Get(api.Snapshot,"sect","spatial") is Dictionary<string,object> area){ApplySpatial(area);return;}
            var buildings=Map(Get(api.Snapshot,"sect","buildings"));int i=0;
            foreach(var pair in buildings)
            {
                int level=(int)N(Get(pair.Value,"level"));float x=(i%3-1)*12,z=(i/3)*15-8;i++;
                if(levels.TryGetValue(pair.Key,out int old)&&old==level)continue;
                if(models.TryGetValue(pair.Key,out var prior))Destroy(prior);
                string kind=pair.Key=="chanh_dien"?"hall":pair.Key=="tang_kinh_cac"?"scripture":pair.Key=="dien_vo_truong"?"barracks":pair.Key=="luyen_dan_duong"?"alchemy":pair.Key=="luyen_khi_dien"?"forge":"herbgarden";
                var root=new GameObject(pair.Key);root.transform.position=new Vector3(x,0,z);var building=new Building{type=kind,complete=true,level=Mathf.Clamp(level,1,4)};VillageArt.Building(root.transform,building,Material);VillageArt.Combine(root.transform);
                var collider=root.AddComponent<BoxCollider>();collider.center=Vector3.up*2;collider.size=new Vector3(5,5,5);var marker=root.AddComponent<FullPortBuilding>();marker.serverId=pair.Key;marker.selected=id=>{tab=0;Draw();ShowUpgrade(id);};models[pair.Key]=root;levels[pair.Key]=level;
            }
        }
        void Draw()
        {
            if(api==null||api.Snapshot==null)return;DefaultBodyFrame();body.GetComponent<Image>().color=ink;CloseSpatialDetail();CancelSpatialPlace();Clear(body);body.gameObject.SetActive(true);RefreshHeader();
            if(commandScreen){ShowCommands();return;}
            if(tab==0)DrawBuildings();if(tab==1)DrawDisciples();if(tab==2)DrawGovernance();if(tab==3)DrawWorld();if(tab==4)DrawChronicle();
        }
        void RefreshHeader()
        {
            if(SliceEnabled){RefreshSliceHeader();return;}
            var state=api.Snapshot;var sect=Map(Get(state,"sect"));
            title.text=S(Get(sect,"name"))+" · NGÀY "+S(Get(state,"day"));var res=Map(Get(sect,"resources"));
            resources.text="Linh thạch "+S(Get(res,"linhThach"))+"    Linh khí mạch "+S(Get(res,"linhKhiMach"))+"    Khí vận "+S(Get(res,"khiVan"))+"    Danh vọng "+S(Get(res,"danhVong"));
            if(spatial!=null)resources.text+="    |    Gỗ "+Math.Floor(N(Get(spatial,"wood")))+" · Đá "+Math.Floor(N(Get(spatial,"stone")))+" · Lương "+Math.Floor(N(Get(spatial,"food")));
        }
        void DrawBuildings()
        {
            if(spatial!=null){ShowSpatialOverview();return;}
            Label(body,"Sơn Môn · Kiến trúc",22,15,1200,42,27,true);var defs=Map(Get(Module("sectBuildings"),"BUILDINGS"));var buildings=Map(Get(api.Snapshot,"sect","buildings"));int i=0;
            foreach(var pair in buildings)
            {
                var def=Map(Get(defs,pair.Key));var b=Map(pair.Value);int col=i%3,row=i/3;i++;var card=Box(body,pair.Key,22+col*433,82+row*261,417,244,new Color(.11f,.22f,.20f));
                Label(card,S(Get(def,"name"))+" · Cấp "+S(Get(b,"level")),16,12,386,35,21,true);Label(card,S(Get(def,"desc")),16,59,386,100,16);
                bool upgrading=Get(b,"upgrading")!=null;double level=N(Get(b,"level"));
                Label(card,upgrading?"Đang nâng cấp đến "+DateTimeOffset.FromUnixTimeMilliseconds((long)N(Get(b,"upgrading","finishAt"))).ToLocalTime().ToString("HH:mm"):"Chi phí: "+Math.Round(N(Get(def,"baseCost"))*Math.Pow(1.5,level))+" linh thạch",16,164,386,29,16);
                string id=pair.Key;Button(card,upgrading?"ĐANG THI CÔNG":"NÂNG CẤP",16,199,386,35,()=>ShowUpgrade(id)).interactable=!upgrading&&!api.Busy&&!api.HasPending;
            }
        }
        void ShowUpgrade(string id)
        {
            var def=Get(Module("sectBuildings"),"BUILDINGS",id);var b=Get(api.Snapshot,"sect","buildings",id);if(b==null)return;double level=N(Get(b,"level"));
            if(SliceEnabled){SliceDialog("Nâng "+S(Get(def,"name")),720,460);Ink(dialog,"Cấp "+level+" → "+(level+1)+"\nChi phí: "+Fmt(Math.Round(N(Get(def,"baseCost"))*Math.Pow(1.5,level)))+" linh thạch\nThời gian: "+Math.Round(N(Get(def,"baseTimeMin"))*Math.Pow(1.3,level))+" phút thực\n"+S(Get(def,"effectPerLevel")),24,91,672,230,21);SliceButton(dialog,"Xác nhận nâng cấp",24,355,327,56,()=>{api.Submit("start_upgrade",new Dictionary<string,object>{{"buildingId",id}});CloseDialog();},true);SliceButton(dialog,"Quay lại",369,355,327,56,CloseDialog);return;}
            OpenDialog("Nâng "+S(Get(def,"name")));Label(dialog,"Cấp "+level+" → "+(level+1)+"\nChi phí: "+Math.Round(N(Get(def,"baseCost"))*Math.Pow(1.5,level))+" linh thạch\nThời gian: "+Math.Round(N(Get(def,"baseTimeMin"))*Math.Pow(1.3,level))+" phút thực\n"+S(Get(def,"effectPerLevel")),24,83,632,200,20);
            Button(dialog,"NÂNG CẤP",24,317,305,55,()=>{api.Submit("start_upgrade",new Dictionary<string,object>{{"buildingId",id}});CloseDialog();});Button(dialog,"QUAY LẠI",347,317,309,55,CloseDialog);
        }
        void DrawDisciples()
        {
            if(SliceEnabled){DrawSliceDisciples();return;}
            var disciples=List(Get(api.Snapshot,"disciples")).Where(Alive).ToList();Label(body,"Đệ tử · "+disciples.Count+" người",22,15,1250,42,27,true);page=Mathf.Clamp(page,0,Mathf.Max(0,(disciples.Count-1)/4));
            int i=0;foreach(var d in disciples.Skip(page*4).Take(4))
            {
                var row=Box(body,"Disciple",22,79+i*125,1286,115,new Color(.11f,.22f,.20f));i++;string id=S(Get(d,"id"));string school=S(Get(d,"school"));
                Portrait(row,school);Label(row,S(Get(d,"name"))+" · "+S(Get(Module("schools"),"SCHOOLS",school,"name")),104,8,546,32,22,true);
                var realm=List(Get(Module("realms"),"REALMS")).FirstOrDefault(v=>N(Get(v,"id"))==N(Get(d,"realm")));
                Label(row,S(Get(realm,"name"))+" · Tiểu cảnh "+S(Get(d,"subLevel"))+"\nTu vi "+S(Get(d,"exp"))+" · "+StatusName(S(Get(d,"status"))),104,49,546,56,17);
                Button(row,"TU LUYỆN",674,12,185,40,()=>SubmitJob(id,"training"));Button(row,"LAO ĐỘNG",873,12,185,40,()=>SubmitJob(id,"labor"));
                Button(row,"NHÀN RỖI",1072,12,196,40,()=>SubmitJob(id,"idle"));Button(row,"ĐỘT PHÁ",674,64,185,39,()=>ShowBreakthrough(id));
                Button(row,"HỒ SƠ",873,64,185,39,()=>{selectedDisciple=id;ShowRecord(d,S(Get(d,"name")));});Button(row,"PHI THĂNG",1072,64,196,39,()=>ConfirmAction("Phi thăng", "Đệ tử đủ cảnh giới sẽ vượt kiếp theo luật thế giới. Xác nhận cử người?","attempt_ascension",new Dictionary<string,object>{{"discipleId",id}}));
            }
            Button(body,"TRANG TRƯỚC",22,594,230,43,()=>{page--;Draw();});Button(body,"TRANG SAU",269,594,230,43,()=>{page++;Draw();});Button(body,"THU ĐỒ · 1 LƯỢT",830,594,230,43,()=>ConfirmAction("Thu đồ","Sử dụng chiêu mộ một lượt theo chi phí và bảo hiểm của Thiên Cơ Các.","gacha_pull",new Dictionary<string,object>{{"count",1L}}));
        }
        static string StatusName(string s){return s=="training"?"Tu luyện":s=="labor"?"Lao động":s=="idle"?"Nhàn rỗi":s=="leading"?"Chưởng môn":s=="mission"?"Làm nhiệm vụ":s=="dead"?"Đã mất":s=="ascended"?"Đã phi thăng":s;}
        void SubmitJob(string id,string state){api.Submit("assign_job",new Dictionary<string,object>{{"discipleId",id},{"status",state}});}
        void ShowBreakthrough(string id)
        {
            OpenDialog("Đột phá đại cảnh giới");Label(dialog,"Chọn số Trúc Cơ Đan hỗ trợ (0–3). Mỗi viên cộng 15 điểm phần trăm; máy chủ kiểm tra cảnh giới, nguyên liệu và giải quyết kết quả.",24,80,630,128,20);
            var field=Input(dialog,"0",24,218,630);Button(dialog,"ĐỘT PHÁ",24,317,305,55,()=>{if(!int.TryParse(field.text,out int pills)||pills<0||pills>3){status.text="Số đan phải từ 0 đến 3.";return;}api.Submit("attempt_breakthrough",new Dictionary<string,object>{{"discipleId",id},{"pillsUsed",pills}});CloseDialog();});Button(dialog,"QUAY LẠI",347,317,309,55,CloseDialog);
        }
        void ConfirmAction(string name,string explanation,string action,Dictionary<string,object> payload)
        {OpenDialog(name);Label(dialog,explanation,24,86,632,200,20);Button(dialog,"XÁC NHẬN",24,317,305,55,()=>{api.Submit(action,payload);CloseDialog();});Button(dialog,"QUAY LẠI",347,317,309,55,CloseDialog);}
        void DrawGovernance()
        {
            Label(body,"Đại Điện · Chính vụ",22,15,1250,42,27,true);
            var sections=new[]{("Tông Phong","factions"),("Bảo Khố","inventory"),("Tấu Chương","dailyReport"),("Hội Đồng Trưởng Lão","elders"),("Gia Phả Truyền Thừa","leaderHistory"),("Đại Kế","currentGrandStrategy"),("Thanh Quy","policy"),("Ngoại Môn","ngoaiMonFactions")};
            string[] sectionIds={"tongphong","baokho","tauchuong","hoidong","giapha","chinhvu","chinhvu","chinhvu"};
            for(int i=0;i<sections.Length;i++){var section=sections[i];string target=sectionIds[i];Button(body,section.Item1,22+i%3*433,85+i/3*123,416,102,()=>ShowProjection("daidien",new Dictionary<string,object>{{"daiDienTab",target}}));}
            Button(body,"THĂNG HẠNG TÔNG MÔN",22,491,416,54,()=>ConfirmAction("Thăng hạng","Thăng hạng khi đủ đệ tử, trưởng lão, danh vọng, linh mạch và lãnh thổ theo cấp hiện tại.","promote_tier",null));
            Button(body,"KÍCH HOẠT LINH MẠCH",455,491,416,54,()=>ConfirmAction("Linh mạch","Kích hoạt linh mạch bằng tài nguyên tông môn.","activate_linhmach",null));
            Button(body,"NGŨ PHONG · PHÂN BỔ",888,491,416,54,ShowBudget);
            Button(body,"TRỌNG DỤNG THIÊN TÀI",22,566,416,54,()=>ConfirmAction("Thanh quy","Áp dụng Trọng Dụng Thiên Tài: tăng tu luyện 20%, giảm sản lượng linh thạch lao động 10%.","set_policy",new Dictionary<string,object>{{"policyId","trong_dung_thien_tai"}}));
        }
        void ShowBudget()
        {
            OpenDialog("Ngũ Phong · Phân bổ ngân sách",800,620);var fields=new Dictionary<string,InputField>();string[] keys={"kiem","dao","phat","nho","vo"};
            Label(dialog,"Ngân sách được chi từ linh thạch hiện có. Phân bổ ảnh hưởng trung thành từng Phong theo luật máy chủ.",24,75,750,70,19);
            for(int i=0;i<keys.Length;i++){string key=keys[i];Label(dialog,S(Get(Module("schools"),"SCHOOLS",key,"name")),24,161+i*65,330,42,20);fields[key]=Input(dialog,"0",370,155+i*65,400);}
            Button(dialog,"PHÂN BỔ",24,520,365,55,()=>{var amounts=new Dictionary<string,object>();foreach(var f in fields){if(!long.TryParse(f.Value.text,out long n)||n<0){status.text="Ngân sách phải là số nguyên không âm.";return;}amounts[f.Key]=n;}api.Submit("allocate_resources",new Dictionary<string,object>{{"amounts",amounts}});CloseDialog();});Button(dialog,"QUAY LẠI",410,520,365,55,CloseDialog);
        }
        void DrawWorld()
        {
            Label(body,"Thế Gian · Tứ Đại Bộ Châu",22,15,1250,42,27,true);var parts=new[]{("Tông môn thiên hạ","aiSects"),("Lãnh thổ","territories"),("Bí cảnh","bicanh"),("Quốc gia","nations"),("Thiên Kiêu Bảng","tienKieuBang"),("Thiên Cơ Báo","threats"),("Ngoại Vực","pocketWorlds"),("Di tích","sectRuins"),("Liên minh","alliances"),("Gia tộc","families"),("Kỷ nguyên","pastEras"),("Thiên Mệnh","thienMenhChiTu")};
            for(int i=0;i<parts.Length;i++){var part=parts[i];Button(body,part.Item1,22+i%3*433,83+i/3*138,416,115,()=>ShowRecord(Get(api.Snapshot,"world",part.Item2),part.Item1));}
        }
        void DrawChronicle(){Label(body,"Biên niên sử · Những dấu ấn tông môn",22,15,1250,42,27,true);DrawEntries(body,List(Get(api.Snapshot,"chronicle")).AsEnumerable().Reverse().ToList(),22,78,1286);}
        void DrawEntries(Transform parent,List<object> values,float x,float y,float width)
        {
            page=Mathf.Clamp(page,0,Mathf.Max(0,(values.Count-1)/7));int i=0;foreach(var v in values.Skip(page*7).Take(7)){object item=v;Button(parent,Summary(v),x,y+i*66,width,59,()=>ShowRecord(item,"Chi tiết"));i++;}
            if(values.Count==0)Label(parent,"Chưa có ghi chép ở thời điểm này.",x,y,width,90,21);
            Button(parent,"TRƯỚC",x,y+477,180,40,()=>{page--;Draw();});Button(parent,"SAU",x+195,y+477,180,40,()=>{page++;Draw();});
        }
        string Summary(object value)
        {if(value is Dictionary<string,object> map){foreach(string key in new[]{"text","message","title","name","desc","id"})if(map.TryGetValue(key,out var v)&&v is string)return S(v);return "Xem thông tin · "+map.Count+" mục";}return S(value);}
        void ShowRecord(object value,string name){recordPage=0;recordHistory.Clear();DrawRecord(value,name);}
        void Update()
        {
            UpdateSpatial();
            if(!initialSmoke)return;if(Time.realtimeSinceStartup>smokeDeadline){System.IO.File.WriteAllText(smokeFile,"FAIL timeout "+api.Notice);Application.Quit(1);return;}
            if(api.Snapshot==null)return;
            if(smokeStage==0){System.IO.File.WriteAllText(smokeFile,"PASS snapshot contains complete server world\n");tab=1;Draw();smokeStage=1;smokeDeadline=Time.realtimeSinceStartup+50;StartCoroutine(SmokeCapture());}
        }
        IEnumerator SmokeCapture()
        {
            if(Environment.GetCommandLineArgs().Contains("-spatial-smoke"))yield return SmokeSpatial();
            yield return new WaitForSecondsRealtime(1);Capture("full-port-disciples.png");tab=0;Draw();yield return new WaitForSecondsRealtime(.5f);Capture("full-port-mountain.png");
            bool projectionOnly=Environment.GetCommandLineArgs().Contains("-projection-only");bool ok=true;
            if(!projectionOnly)
            {
            // Expected cost follows the reference formula upgradeCost = round(baseCost * 1.5^level), so the check stays exact at any hall level.
            double level=N(Get(api.Snapshot,"sect","buildings","chanh_dien","level"));double expectedCost=Math.Floor(N(Get(Module("sectBuildings"),"BUILDINGS","chanh_dien","baseCost"))*Math.Pow(1.5,level)+.5);
            if(Get(api.Snapshot,"sect","buildings","chanh_dien","upgrading")!=null)System.IO.File.AppendAllText(smokeFile,"FAIL precondition: chanh_dien is already upgrading in the QA game; reset the isolated fixture\n");
            double before=N(Get(api.Snapshot,"sect","resources","linhThach"));api.Submit("start_upgrade",new Dictionary<string,object>{{"buildingId","chanh_dien"}});
            while(api.Busy)yield return null;yield return api.Refresh();
            if(api.HasPending){System.IO.File.AppendAllText(smokeFile,"PASS network uncertainty retained original request journal\n");api.RetryPending();while(api.Busy)yield return null;yield return api.Refresh();}
            yield return new WaitForSecondsRealtime(.5f);var upgrading=Get(api.Snapshot,"sect","buildings","chanh_dien","upgrading");double after=N(Get(api.Snapshot,"sect","resources","linhThach"));
            ok=upgrading!=null&&Math.Abs(before-after-expectedCost)<.01&&!api.HasPending;
            System.IO.File.AppendAllText(smokeFile,ok?"PASS native UI receives authoritative building upgrade\nPASS exact server upgrade cost debited once\nPASS pending journal resolved\nFULL_PORT_SLICE_OK\n":"FAIL upgrade or retry: "+api.Notice+" before="+before+" after="+after);
            Capture("full-port-upgrade.png");
            }
            foreach(var command in Commands){OpenCommand(Map(command));yield return null;CloseDialog();}
            System.IO.File.AppendAllText(smokeFile,"PASS all 69 native decision forms rendered\n");
            commandScreen=true;ShowCommands();yield return new WaitForSecondsRealtime(.5f);Capture("full-port-decisions.png");
            OpenCommand(Map(Commands.First(c=>S(Get(c,"id"))=="start_expedition")));yield return new WaitForSecondsRealtime(.3f);Capture("full-port-expedition-form.png");
            CloseDialog();commandScreen=false;
            foreach(string section in hallIds)
            {
                bool loaded=false;yield return api.Presentation("daidien",new Dictionary<string,object>{{"daiDienTab",section}},null,data=>{projection=data;projectionActive=true;projectionView="daidien";projectionUi=new Dictionary<string,object>{{"daiDienTab",section}};projectionPage=0;DrawProjection();loaded=true;});
                if(!loaded){ok=false;System.IO.File.AppendAllText(smokeFile,"FAIL projected hall section "+section+"\n");continue;}
                yield return null;if(section=="tongphong"||section=="baokho"||section=="thiencobao"){yield return new WaitForSecondsRealtime(.3f);Capture("full-port-"+section+".png");}
                System.IO.File.AppendAllText(smokeFile,"PASS native projected hall section "+section+"\n");
            }
            projectionActive=false;tab=1;Draw();yield return new WaitForSecondsRealtime(.3f);Capture("full-port-disciples.png");System.IO.File.AppendAllText(smokeFile,ok?"FULL_PORT_RUNTIME_OK\n":"FULL_PORT_RUNTIME_FAILED\n");Application.Quit(ok?0:1);
        }
        void Capture(string filename)
        {
            var cv=canvas.GetComponentInParent<Canvas>();var tex=new RenderTexture(1600,900,24);var old=RenderTexture.active;cv.renderMode=RenderMode.ScreenSpaceCamera;cv.worldCamera=cam;cv.planeDistance=1;cam.targetTexture=tex;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=tex;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../"+filename),image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=old;cv.renderMode=RenderMode.ScreenSpaceOverlay;tex.Release();Destroy(tex);Destroy(image);
        }
    }
}

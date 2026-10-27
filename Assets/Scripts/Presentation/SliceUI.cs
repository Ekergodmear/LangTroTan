using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LangTroTan.Presentation
{
    public sealed class SliceSurface:MaskableGraphic
    {
        public float bevel=12;public Color rim=new Color(.63f,.57f,.40f,.55f);public bool round;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;int count=round?48:8;var points=new Vector2[count];
            if(round)for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;points[i]=rect.center+new Vector2(Mathf.Cos(a)*rect.width*.5f,Mathf.Sin(a)*rect.height*.5f);}
            else{float b=Mathf.Min(bevel,Mathf.Min(rect.width,rect.height)*.28f);points=new[]{new Vector2(rect.xMin+b,rect.yMin),new Vector2(rect.xMax-b,rect.yMin),new Vector2(rect.xMax,rect.yMin+b),new Vector2(rect.xMax,rect.yMax-b),new Vector2(rect.xMax-b,rect.yMax),new Vector2(rect.xMin+b,rect.yMax),new Vector2(rect.xMin,rect.yMax-b),new Vector2(rect.xMin,rect.yMin+b)};}
            vh.AddVert(rect.center,color,new Vector2(.5f,.5f));for(int i=0;i<count;i++)vh.AddVert(points[i],color,Vector2.zero);for(int i=0;i<count;i++)vh.AddTriangle(0,1+i,1+(i+1)%count);
            for(int i=0;i<count;i++){int j=(i+1)%count,index=vh.currentVertCount;var a=points[i];var b=points[j];var ai=Vector2.MoveTowards(a,rect.center,1.25f);var bi=Vector2.MoveTowards(b,rect.center,1.25f);vh.AddVert(a,rim,Vector2.zero);vh.AddVert(b,rim,Vector2.zero);vh.AddVert(bi,rim,Vector2.zero);vh.AddVert(ai,rim,Vector2.zero);vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);}
        }
    }
    public sealed partial class FullPortView
    {
        static bool SliceEnabled=>!Environment.GetCommandLineArgs().Contains("-visual-baseline");
        readonly Color paper=new Color(.95f,.925f,.855f,.98f),inkText=new Color(.16f,.24f,.24f),mutedText=new Color(.36f,.42f,.39f),deepJade=new Color(.13f,.31f,.30f),bronze=new Color(.56f,.42f,.22f);
        readonly Dictionary<string,Text> sliceResources=new Dictionary<string,Text>();Text sliceCalendar;
        readonly Dictionary<int,SliceSurface> sliceNav=new Dictionary<int,SliceSurface>();
        string inventoryCategory="all",inventorySelection;int inventoryPage;Dictionary<string,object> inventoryData;
        readonly Dictionary<string,Sprite> sliceImages=new Dictionary<string,Sprite>();
        static string CleanFloats(string value)=>Regex.Replace(value??"",@"(?<![\w.])-?\d+\.\d{4,}(?![\w.])",m=>double.TryParse(m.Value,NumberStyles.Float,CultureInfo.InvariantCulture,out double n)?n.ToString("N1",CultureInfo.GetCultureInfo("vi-VN")):m.Value);
        static string Fmt(object value,int decimals=0)=>N(value).ToString("N"+decimals,CultureInfo.GetCultureInfo("vi-VN"));
        RectTransform Surface(Transform parent,string name,float x,float y,float w,float h,Color color,bool round=false)
        {
            var r=new GameObject(name,typeof(RectTransform),typeof(SliceSurface)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.offsetMin=new Vector2(x,-y-h);r.offsetMax=new Vector2(x+w,-y);var g=r.GetComponent<SliceSurface>();g.color=color;g.round=round;return r;
        }
        Text Ink(Transform parent,string text,float x,float y,float w,float h,int size=18,bool heading=false,Color? color=null)
        {var label=Label(parent,CleanFloats(text),x,y,w,h,size,heading);label.color=color??(heading?inkText:mutedText);return label;}
        Button SliceButton(Transform parent,string text,float x,float y,float w,float h,Action callback,bool primary=false)
        {
            var r=Surface(parent,text,x,y,w,h,primary?deepJade:new Color(.91f,.895f,.82f));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<SliceSurface>();b.onClick.AddListener(()=>callback());var colors=b.colors;colors.highlightedColor=new Color(.92f,1,.96f);colors.pressedColor=new Color(.74f,.83f,.77f);b.colors=colors;
            var t=Ink(r,text,10,6,w-20,h-12,17,true,primary?paper:deepJade);t.alignment=TextAnchor.MiddleCenter;return b;
        }
        void BodyFrame(float x=90,float y=124,float w=1420,float h=660)
        {body.offsetMin=new Vector2(x,-y-h);body.offsetMax=new Vector2(x+w,-y);}
        void DefaultBodyFrame()=>BodyFrame(254,134,1330,658);
        void SliceBody(string kicker,string name,string description)
        {
            CloseSpatialDetail();CancelSpatialPlace();Clear(body);body.gameObject.SetActive(true);BodyFrame();var image=body.GetComponent<Image>();image.color=Color.clear;
            Surface(body,"Parchment folio",0,0,1420,660,paper);Ink(body,kicker.ToUpperInvariant(),30,17,1170,26,14,false,bronze);Ink(body,name,30,45,1180,48,32,true);Ink(body,description,31,96,1330,40,17);
        }
        Sprite SliceImage(string source)
        {
            if(sliceImages.TryGetValue(source,out var sprite))return sprite;if(artMap==null)artMap=Map(LangTroTan.Infrastructure.ServerJson.Parse(Resources.Load<TextAsset>("FullPort/ArtMap").text));
            string key=S(Get(artMap,source),source);var texture=Resources.Load<Texture2D>(key);if(texture==null)return null;sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f);sliceImages[source]=sprite;return sprite;
        }
        void Art(Transform parent,string source,float x,float y,float w,float h,bool crop=false)
        {
            var r=new GameObject("Approved 2D artwork",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.offsetMin=new Vector2(x,-y-h);r.offsetMax=new Vector2(x+w,-y);var image=r.GetComponent<Image>();image.sprite=SliceImage(source);image.color=image.sprite==null?Color.clear:Color.white;image.preserveAspect=!crop;image.raycastTarget=false;
        }
        void CreateSliceInterface()
        {
            var c=new GameObject("Xianxia slice HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));c.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=c.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();canvas.SetParent(c.transform,false);var safe=c.AddComponent<SafeArea>();safe.content=canvas;safe.scaler=scaler;safe.Apply();new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
            var top=Surface(canvas,"Compact resource ribbon",20,18,1560,82,paper);var crest=Surface(top,"Sect jade seal",13,10,60,60,deepJade,true);var emblem=new GameObject("Lotus sect emblem",typeof(RectTransform),typeof(SliceSeal)).GetComponent<RectTransform>();emblem.SetParent(crest,false);emblem.anchorMin=Vector2.zero;emblem.anchorMax=Vector2.one;emblem.offsetMin=Vector2.one*9;emblem.offsetMax=-Vector2.one*9;emblem.GetComponent<SliceSeal>().color=new Color(.89f,.79f,.55f);emblem.GetComponent<SliceSeal>().raycastTarget=false;
            title=Ink(top,"Thanh Vân Tông",88,9,282,35,25,true);sliceCalendar=Ink(top,"Sơn môn giữa tầng mây",89,45,290,26,14);
            string[] keys={"linhThach","linhKhiMach","khiVan","danhVong","wood","stone","food"};string[] names={"LINH THẠCH","LINH MẠCH","KHÍ VẬN","DANH VỌNG","GỖ","ĐÁ","LƯƠNG"};
            for(int i=0;i<keys.Length;i++){float x=388+i*143;Ink(top,names[i],x,13,139,24,12,false,bronze);sliceResources[keys[i]]=Ink(top,"—",x,37,139,33,22,true);}
            resources=Ink(top,"",0,0,1,1,1);resources.gameObject.SetActive(false);SliceButton(top,"KẾT NỐI",1395,20,148,44,ConnectionForm);
            var dock=Surface(canvas,"Navigation dock",397,800,806,80,paper);string[] labels={"Sơn Môn","Đệ Tử","Đại Điện","Thế Gian","Biên Niên","Thêm"};
            for(int i=0;i<labels.Length;i++){int index=i;var b=SliceButton(dock,labels[i],13+i*132,17,121,47,()=>SliceNavigate(index),i==0);sliceNav[i]=b.GetComponent<SliceSurface>();}
            var notice=Surface(canvas,"World notice",20,827,355,47,new Color(.12f,.24f,.23f,.89f));status=Ink(notice,"Sơn môn đang chờ khai mở…",14,9,328,31,14,false,paper);
            SliceButton(canvas,"＋  KIẾN THIẾT",1240,815,340,60,ShowSpatialCatalog,true);
            body=Box(canvas,"Legacy interface host",254,134,1330,658,ink);
        }
        void SliceNavigate(int index)
        {
            projectionActive=false;commandScreen=false;CloseDialog();CloseSpatialDetail();CancelSpatialPlace();
            if(index==2){tab=2;ShowHallSections();RefreshSliceHeader();return;}if(index==5){OpenDialog("Điều hành tông môn",680,420);Button(dialog,"QUYẾT NGHỊ",24,86,632,52,()=>{CloseDialog();ShowCommands();});Button(dialog,"ĐỒNG BỘ",24,150,632,52,()=>StartCoroutine(api.Refresh()));Button(dialog,"THỬ LẠI THAO TÁC ĐANG CHỜ",24,214,632,52,()=>api.RetryPending());Button(dialog,"QUAY LẠI",24,317,632,55,CloseDialog);return;}
            tab=index==3?3:index==4?4:index;page=0;Draw();
        }
        void RefreshSliceHeader()
        {
            var state=api.Snapshot;title.text=S(Get(state,"sect","name"));var env=Get(spatial,"environment");int season=Mathf.Clamp((int)N(Get(env,"season")),0,3);sliceCalendar.text="Ngày "+Fmt(Get(state,"day"))+" · "+new[]{"Xuân","Hạ","Thu","Đông"}[season]+" · "+(Get(env,"night") is bool night&&night?"Đêm":"Ban ngày");
            foreach(var pair in sliceResources){object value=Get(state,"sect","resources",pair.Key)??Get(spatial,pair.Key);pair.Value.text=Fmt(value,pair.Key=="linhKhiMach"?1:0);}
            int active=projectionActive&&projectionView=="daidien"?2:tab;foreach(var pair in sliceNav){pair.Value.color=pair.Key==active?deepJade:new Color(.91f,.895f,.82f);foreach(var label in pair.Value.GetComponentsInChildren<Text>())label.color=pair.Key==active?paper:deepJade;}
        }
        void Progress(Transform p,float x,float y,float width,float ratio,string text)
        {var track=Surface(p,"Cultivation track",x,y,width,9,new Color(.75f,.78f,.69f));var fill=Surface(track,"Cultivation progress",0,0,Mathf.Max(1,width*Mathf.Clamp01(ratio)),9,deepJade);fill.GetComponent<SliceSurface>().raycastTarget=false;Ink(p,text,x,y+17,width,28,15);}
        void SliceDialog(string heading,float width=720,float height=560)
        {OpenDialog(heading,width,height);Clear(dialog);dialog.GetComponent<Image>().color=Color.clear;Surface(dialog,"Parchment decision",0,0,width,height,paper);Ink(dialog,heading,24,20,width-48,49,28,true);}
        void ScrollCopy(Transform parent,string copy,float x,float y,float width,float height)
        {
            var view=Surface(parent,"Scrollable item explanation",x,y,width,height,new Color(.97f,.955f,.89f));view.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var content=new GameObject("Explanation content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(view,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);
            var text=Ink(content,copy,5,0,width-21,1200,17);float preferred=text.preferredHeight;content.sizeDelta=new Vector2(0,Mathf.Max(height,preferred+12));text.rectTransform.pivot=new Vector2(0,1);text.rectTransform.anchoredPosition=new Vector2(5,0);text.rectTransform.sizeDelta=new Vector2(width-21,preferred+12);
            var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.viewport=view;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=24;scroll.movementType=ScrollRect.MovementType.Clamped;
        }
        // Mirrors the reference rule canAssignJob (js/systems/workerAssignment.js) and the web UI's three
        // assignment buttons. The server still decides; this only hides commands it would always reject.
        // "training" is never player-assignable: the Ngoại Môn policy sets it.
        static bool CanAssignJob(string current,string target,string school)
        {
            if(target=="idle")return new[]{"idle","labor","training","alchemy","forge"}.Contains(current);
            if(current!="idle")return false;
            if(target=="alchemy")return school=="dao"||school=="phat";
            if(target=="forge")return school=="kiem"||school=="vo";
            return target=="labor";
        }
        static (string status,string label)? SliceJobAction(object d)
        {
            string status=S(Get(d,"status")),school=S(Get(d,"school"));
            if(status!="idle"&&CanAssignJob(status,"idle",school))return ("idle","Thu hồi");
            if(CanAssignJob(status,"alchemy",school))return ("alchemy","Xếp luyện đan");
            if(CanAssignJob(status,"forge",school))return ("forge","Xếp rèn khí");
            return null;
        }
        void DrawSliceDisciples()
        {
            var disciples=List(Get(api.Snapshot,"disciples")).Where(Alive).ToList();SliceBody("Đệ tử lục · Nhân tài tông môn","Đồng hành trên tiên lộ",disciples.Count+" đệ tử · Theo dõi tu vi, phân công và quyết định đột phá.");page=Mathf.Clamp(page,0,Mathf.Max(0,(disciples.Count-1)/4));
            string[] portraits={"505095f6e4bd07347fe7","172bc83538b7965b0bc0","4cdd6ea2c6f1d2439d03","0b3fd3908a713ebbf5fa","4d446a0d2751d757aa17"};string[] schools={"kiem","dao","phat","nho","vo"};
            int i=0;foreach(var d in disciples.Skip(page*4).Take(4)){
                string id=S(Get(d,"id")),school=S(Get(d,"school"));int si=Mathf.Max(0,Array.IndexOf(schools,school));var card=Surface(body,"Disciple portrait card",28+i*345,153,331,413,new Color(.985f,.97f,.91f));i++;
                Art(card,"FullPortArt/"+portraits[si],12,12,139,190);Ink(card,S(Get(d,"rarity")),165,15,145,30,18,true,bronze);Ink(card,S(Get(Module("schools"),"SCHOOLS",school,"name")),165,54,145,56,19,true);
                string state=StatusName(S(Get(d,"status")));var badge=Surface(card,"Disciple status",161,124,152,42,new Color(.83f,.88f,.80f));Ink(badge,state,7,8,139,28,15,true,deepJade).alignment=TextAnchor.MiddleCenter;
                Ink(card,S(Get(d,"name")),16,211,300,34,22,true);var realm=List(Get(Module("realms"),"REALMS")).FirstOrDefault(r=>N(Get(r,"id"))==N(Get(d,"realm")));Ink(card,S(Get(realm,"name"))+" · Tầng "+Fmt(Get(d,"subLevel")),16,251,299,30,16);
                double exp=N(Get(d,"exp")),required=Math.Round(80*N(Get(d,"realm"))*(1+.1*N(Get(d,"subLevel"))));Progress(card,16,292,299,(float)(exp/Math.Max(1,required)),"Tu vi  "+Fmt(exp)+" / "+Fmt(required));
                var job=SliceJobAction(d);
                if(job!=null){string target=job.Value.status;var action=SliceButton(card,job.Value.label,14,351,143,45,()=>SubmitJob(id,target),true);action.name="AssignJob:"+id+":"+target;}
                SliceButton(card,"Hồ sơ",job!=null?172:14,351,job!=null?143:301,45,()=>ShowSliceDisciple(d));
            }
            SliceButton(body,"‹",28,592,55,44,()=>{page--;Draw();});Ink(body,(page+1)+" / "+Mathf.Max(1,(disciples.Count+3)/4),98,603,130,28,16);SliceButton(body,"›",200,592,55,44,()=>{page++;Draw();});SliceButton(body,"THU ĐỒ",1136,590,255,48,()=>ConfirmAction("Thu đồ","Chiêu mộ một lượt theo chi phí Thiên Cơ Các.","gacha_pull",new Dictionary<string,object>{{"count",1L}}),true);
        }
        void ShowSliceDisciple(object d)
        {
            string id=S(Get(d,"id"));SliceDialog(S(Get(d,"name")),720,560);Ink(dialog,"Cảnh giới: "+DisplayValue("realm",Get(d,"realm"))+" · Tầng "+Fmt(Get(d,"subLevel"))+"\nTu vi: "+Fmt(Get(d,"exp"))+"\nTrạng thái: "+StatusName(S(Get(d,"status"))),24,86,672,140,21);
            SliceButton(dialog,"Lao động",24,240,321,47,()=>SubmitJob(id,"labor"));SliceButton(dialog,"Nhàn rỗi",368,240,328,47,()=>SubmitJob(id,"idle"));SliceButton(dialog,"Đột phá",24,303,321,47,()=>ShowBreakthrough(id),true);SliceButton(dialog,"Phi thăng",368,303,328,47,()=>ConfirmAction("Phi thăng","Xác nhận cử đệ tử vượt kiếp theo luật tông môn?","attempt_ascension",new Dictionary<string,object>{{"discipleId",id}}));SliceButton(dialog,"Hồ sơ chi tiết",24,368,672,47,()=>ShowRecord(d,S(Get(d,"name"))));SliceButton(dialog,"Quay lại",24,470,672,51,CloseDialog);
        }
        void DrawSliceInventory()
        {
            inventoryData=Map(Get(projection,"specialized"));var groups=List(Get(inventoryData,"groups"));var all=groups.SelectMany(g=>List(Get(g,"items"))).ToList();var filtered=groups.Where(g=>inventoryCategory=="all"||S(Get(g,"category","id"))==inventoryCategory).SelectMany(g=>List(Get(g,"items"))).ToList();
            SliceBody("Đại Điện / Bảo Khố","Bảo Khố Tông Môn",all.Count+" loại bảo vật · "+Fmt(all.Sum(item=>N(Get(item,"qty"))))+" phần đang lưu giữ · Chọn vật phẩm để xem công dụng thật.");
            int k=0;foreach(var pair in new[]{("all","Tất cả"),("pill","Đan dược"),("artifact","Pháp khí"),("manual","Công pháp"),("material","Nguyên liệu")}){string key=pair.Item1;SliceButton(body,pair.Item2,28+k*190,148,177,43,()=>{inventoryCategory=key;inventoryPage=0;inventorySelection=null;DrawSliceInventory();},inventoryCategory==key);k++;}
            if(!filtered.Any()){Ink(body,"Chưa có vật phẩm trong mục này.",46,263,820,120,23,true);inventorySelection=null;}
            else if(!filtered.Any(item=>S(Get(item,"id"))==inventorySelection))inventorySelection=S(Get(filtered[0],"id"));
            inventoryPage=Mathf.Clamp(inventoryPage,0,Mathf.Max(0,(filtered.Count-1)/6));int index=0;
            foreach(var item in filtered.Skip(inventoryPage*6).Take(6)){
                string id=S(Get(item,"id"));int col=index%3,row=index/3;index++;var tile=Surface(body,"Inventory item "+id,28+col*282,214+row*179,266,163,inventorySelection==id?new Color(.83f,.89f,.82f):new Color(.98f,.965f,.91f));
                var button=tile.gameObject.AddComponent<Button>();button.targetGraphic=tile.GetComponent<SliceSurface>();button.onClick.AddListener(()=>{inventorySelection=id;DrawSliceInventory();});
                string src=S(Get(item,"icon"),"");if(src!="")Art(tile,src,12,12,94,87);else Ink(tile,"◇",34,22,73,74,42,true,bronze);
                Ink(tile,"× "+Fmt(Get(item,"qty")),127,35,123,36,26,true);Ink(tile,S(Get(item,"name")),14,110,238,41,19,true);
            }
            var detail=Surface(body,"Selected treasure",902,214,490,357,new Color(.88f,.89f,.82f));var selected=filtered.FirstOrDefault(item=>S(Get(item,"id"))==inventorySelection);
            if(selected!=null){Ink(detail,S(Get(selected,"name")),23,20,443,45,27,true);Ink(detail,Fmt(Get(selected,"qty"))+" phần trong kho",24,76,440,29,18,false,bronze);var usability=Get(selected,"usability");string labels=string.Join(" · ",List(Get(usability,"labels")).Select(x=>S(x)));string explanation=string.Join("\n",List(Get(usability,"explanations")).Select(x=>S(x)));Ink(detail,labels,24,122,440,55,18,true);ScrollCopy(detail,explanation,24,185,440,91);
                var nav=Get(usability,"nav");if(nav!=null)SliceButton(detail,S(Get(nav,"label")),23,293,444,45,()=>UseSliceItem(nav),true);
            }
            SliceButton(body,"‹",28,592,55,44,()=>{inventoryPage--;DrawSliceInventory();});Ink(body,(inventoryPage+1)+" / "+Mathf.Max(1,(filtered.Count+5)/6),98,603,130,28,16);SliceButton(body,"›",200,592,55,44,()=>{inventoryPage++;DrawSliceInventory();});SliceButton(body,"CÁC MỤC ĐẠI ĐIỆN",1055,590,337,48,ShowHallSections);
        }
        void UseSliceItem(object nav)
        {
            if(S(Get(nav,"act"))!="pill-use-pick"){ProjectionIntent(new Dictionary<string,object>{{"data",new Dictionary<string,object>{{"act",S(Get(nav,"act"))},{"id",S(Get(nav,"id"))}}}});return;}
            ShowSlicePillPicker(S(Get(nav,"id")),0);
        }
        void ShowSlicePillPicker(string itemId,int at)
        {
            SliceDialog("Chọn đệ tử dùng đan",800,650);var people=List(Get(api.Snapshot,"disciples")).Where(Alive).ToList();at=Mathf.Clamp(at,0,Mathf.Max(0,(people.Count-1)/6));int i=0;
            foreach(var d in people.Skip(at*6).Take(6)){string id=S(Get(d,"id")),name=S(Get(d,"name"));var recipient=SliceButton(dialog,name+" · "+S(Get(Module("schools"),"SCHOOLS",S(Get(d,"school")),"name"))+" · "+StatusName(S(Get(d,"status"))),24,91+i*68,752,54,()=>{
                SliceDialog("Xác nhận dùng đan",720,440);Ink(dialog,"Dùng 1 "+S(Get(Module("items"),"ITEMS",itemId,"name"),"đan dược")+" cho "+name+".\nMáy chủ kiểm tra công dụng, điều kiện và giới hạn sử dụng.",24,102,672,180,21);SliceButton(dialog,"Xác nhận sử dụng",24,326,327,56,()=>{api.Submit("use_sect_pill",new Dictionary<string,object>{{"discipleId",id},{"itemId",itemId}});CloseDialog();},true);SliceButton(dialog,"Quay lại",369,326,327,56,CloseDialog);
            });recipient.gameObject.name="PillRecipient:"+id;i++;}
            int current=at;SliceButton(dialog,"‹ Trước",24,499,232,43,()=>ShowSlicePillPicker(itemId,current-1));Ink(dialog,(at+1)+" / "+Math.Max(1,(people.Count+5)/6),279,508,220,29,17);SliceButton(dialog,"Sau ›",544,499,232,43,()=>ShowSlicePillPicker(itemId,current+1));
            SliceButton(dialog,"Quay lại",24,560,752,50,CloseDialog);
        }
        void CreateSliceMountain()
        {
            var c=new GameObject("Jade mountain camera");cam=c.AddComponent<Camera>();cam.orthographic=false;cam.fieldOfView=42;cam.backgroundColor=new Color(.65f,.77f,.75f);cam.clearFlags=CameraClearFlags.SolidColor;cam.farClipPlane=230;cam.allowHDR=true;
            var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;var volume=new GameObject("Subtle jade grading").AddComponent<Volume>();volume.isGlobal=true;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();var tone=volume.profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);var grading=volume.profile.Add<ColorAdjustments>();grading.contrast.Override(8);grading.saturation.Override(-4);var bloom=volume.profile.Add<Bloom>();bloom.intensity.Override(.15f);bloom.threshold.Override(1.1f);
            spatialSun=new GameObject("Warm mountain sunlight").AddComponent<Light>();spatialSun.type=LightType.Directional;spatialSun.color=new Color(1,.89f,.73f);spatialSun.intensity=1.3f;spatialSun.shadows=LightShadows.Soft;spatialSun.shadowStrength=.7f;spatialSun.transform.rotation=Quaternion.Euler(48,-32,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.66f,.77f,.8f);RenderSettings.ambientEquatorColor=new Color(.58f,.64f,.58f);RenderSettings.ambientGroundColor=new Color(.3f,.36f,.32f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.65f,.77f,.75f);RenderSettings.fogStartDistance=45;RenderSettings.fogEndDistance=145;
            Shader.SetGlobalColor("_XianxiaFog",RenderSettings.fogColor);
            var world=new GameObject("Authored Xianxia environment");if(SonMonArt.TryEnvironment(world.transform)){cam.clearFlags=CameraClearFlags.Skybox;cam.farClipPlane=420;RenderSettings.fogStartDistance=70;RenderSettings.fogEndDistance=260;}else SliceArt.Environment(world.transform,Material);CreateSliceAtmosphere();SetSpatialCamera();
        }
    }
}

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LangTroTan.Domain;
using LangTroTan.Infrastructure;

namespace LangTroTan.Presentation
{
    public partial class Game
    {
        Text woodValue,stoneValue,foodValue,foodFlow;
        readonly Text[] squadValues=new Text[3];readonly Button[] squadCards=new Button[3];
        Texture2D mapTexture;RawImage mapImage;Font titleFont;readonly Color32[] mapPixels=new Color32[128*128];
        Image Icon(Transform parent,int index,float x,float y,float size)
        {
            var r=new GameObject("Illustration",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.offsetMin=new Vector2(x,-y-size);r.offsetMax=new Vector2(x+size,-y);
            var image=r.GetComponent<Image>();image.sprite=UISkin.Icon(index);image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        Text Heading(Transform parent,string text,float x,float y,float w,float h,int size=24)
        {
            var label=Label(parent,text,x,y,w,h,size,gold);label.font=titleFont??font;return label;
        }
        void Rule(Transform parent,float x,float y,float width)
        {Box("Gold divider",parent,x,y,width,1,new Color(gold.r,gold.g,gold.b,.38f)).GetComponent<Image>().raycastTarget=false;}
        Text ResourceChip(Transform parent,int icon,string title,float x)
        {
            var r=Box(title,parent,x,13,207,70,new Color(.23f,.19f,.13f));Icon(r,icon,9,6,57);
            Label(r,title,76,8,120,20,12,new Color(.79f,.75f,.62f));return Label(r,"0",76,27,120,34,25,new Color(1,.94f,.77f));
        }
        void CreateUI()
        {
            titleFont=Resources.Load<Font>("Fonts/NotoSerif-Bold")??font;
            var c=new GameObject("HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));c.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=c.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();canvas.SetParent(c.transform,false);
            var safe=c.AddComponent<SafeArea>();safe.content=canvas;safe.scaler=scaler;safe.Apply();
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var top=Panel("Kingdom resources",canvas,new Vector2(0,1),Vector2.one,new Vector2(16,-110),new Vector2(-16,-14),ink);
            Icon(top,15,15,14,66);Heading(top,"THANH VÂN\nTÔNG",91,9,145,78,19);
            woodValue=ResourceChip(top,0,"GỖ",246);stoneValue=ResourceChip(top,1,"ĐÁ",465);foodValue=ResourceChip(top,2,"LƯƠNG THỰC",684);resourceText=woodValue;
            clockText=Label(top,"",918,9,237,65,14,gold);
            Button(top,"LƯU",1170,25,108,48,Save);Button(top,"DỪNG",1288,25,108,48,TogglePause);
            var sp=Button(top,"1×",1406,25,132,48,()=>speed=speed==1?2:1);speedText=sp.GetComponentInChildren<Text>();
            var goals=Box("Journey",canvas,16,128,258,188,ink);Heading(goals,"Khai sơn lập phái",17,13,226,37,18);Rule(goals,18,54,222);
            goalText=Label(goals,"",18,61,224,75,13);Button(goals,"TÔNG MÔN / ĐỆ TỬ",13,139,232,35,()=>ShowSectPanel());
            var map=Box("Map frame",canvas,16,332,258,232,ink);Heading(map,"Vùng đất của bạn",17,11,224,32,17);
            var mapRect=Box("Map",map,13,49,232,136,Color.clear);Destroy(mapRect.GetComponent<Image>());
            var rawGo=new GameObject("Live minimap",typeof(RectTransform),typeof(RawImage),typeof(MiniMapClick));var rr=rawGo.GetComponent<RectTransform>();rr.SetParent(mapRect,false);rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;rr.offsetMin=rr.offsetMax=Vector2.zero;
            mapImage=rawGo.GetComponent<RawImage>();mapTexture=new Texture2D(128,128,TextureFormat.RGB24,false);mapTexture.filterMode=FilterMode.Point;mapImage.texture=mapTexture;
            rawGo.GetComponent<MiniMapClick>().onClick=p=>{cameraFocus=new Vector3((p.x-.5f)*96,0,(p.y-.5f)*96);UpdateCamera();};
            Button(map,"SƠN MÔN",13,192,109,30,()=>{cameraFocus=Vector3.zero;UpdateCamera();});Button(map,"THẾ GIỚI",128,192,117,30,ShowWorldPanel);
            var people=Box("Village population",canvas,16,580,258,154,ink);Icon(people,3,14,15,63);Heading(people,"NGOẠI MÔN",91,14,150,31,17);
            popText=Label(people,"",91,49,150,67,14);Button(people,"NHẬN NGOẠI MÔN",13,108,232,34,()=>world.Recruit());
            var bottom=Panel("Army command",canvas,Vector2.zero,new Vector2(1,0),new Vector2(16,14),new Vector2(-16,126),ink);
            for(int i=0;i<3;i++)
            {
                int squad=i;var b=Button(bottom,"",14+i*118,12,108,88,()=>SelectSquad(squad));squadCards[i]=b;
                Icon(b.transform,15,7,7,49);Label(b.transform,"TỔ "+(i+1),55,13,48,23,12,gold);squadValues[i]=Label(b.transform,"0 / 4",55,39,48,26,14);
                Label(b.transform,"CHỌN ĐỘI",12,63,84,20,10,new Color(.77f,.75f,.65f)).alignment=TextAnchor.MiddleCenter;
            }
            Button(bottom,"TOÀN QUÂN",378,15,140,39,()=>SelectSquad(-1));Button(bottom,"GIỮ VỊ TRÍ",378,62,140,38,()=>{foreach(var u in world.units.Where(u=>!u.enemy&&(selectedSquad==-1||u.squad==selectedSquad)))u.hold=true;world.message="Tổ đội giữ vị trí";});
            Button(bottom,"RÚT VỀ",528,15,122,39,()=>OrderAt(new Vector3(0,0,-5)));Button(bottom,"BỎ CHỌN",528,62,122,38,()=>{selectedSquad=-2;selectedBuilding=-1;CancelPlace();RefreshDetails();});
            selectionText=Heading(bottom,"Ngôi làng đang chờ bạn",680,14,545,35,20);
            Label(bottom,"Chọn tổ rồi chạm đất để ra lệnh.\nChạm công trình để quản lý dân và nâng cấp.",680,58,545,46,14);
            var build=Button(bottom,"",1260,15,280,84,()=>{if(buildMenu.gameObject.activeSelf)CancelPlace();else ShowBuildMenu();},true);Icon(build.transform,4,12,5,70);Heading(build.transform,"XÂY DỰNG",95,12,175,37,23);Label(build.transform,"Kiến thiết tông môn",95,49,175,23,12);
            details=Panel("Building details",canvas,new Vector2(1,0),new Vector2(1,1),new Vector2(-350,145),new Vector2(-16,-127),ink);details.gameObject.SetActive(false);
            buildMenu=Panel("Construction catalog",canvas,new Vector2(1,0),new Vector2(1,1),new Vector2(-426,145),new Vector2(-16,-127),ink);buildMenu.gameObject.SetActive(false);
            var status=Panel("Orders feedback",canvas,new Vector2(0,0),new Vector2(1,0),new Vector2(294,144),new Vector2(-442,198),new Color(ink.r,ink.g,ink.b,.9f));statusText=Label(status,"",17,7,790,42,14);
            foodFlow=Label(top,"",918,70,220,20,11,mint);
        }
        void Welcome()
        {
            if(modal!=null)Destroy(modal.gameObject);modalOpen=true;
            modal=Panel("Title screen",canvas,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Color.black);
            var image=modal.GetComponent<Image>();image.sprite=UISkin.Background();image.type=Image.Type.Simple;image.color=Color.white;
            var shade=Panel("Title shade",modal,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Color.white);shade.GetComponent<Image>().sprite=UISkin.Fade;shade.GetComponent<Image>().type=Image.Type.Simple;shade.GetComponent<Image>().raycastTarget=false;
            Label(modal,"KHAI SƠN  /  THU ĐỒ  /  TU LUYỆN",82,135,580,33,16,gold);
            Heading(modal,"THANH VÂN\nTÔNG",76,189,690,220,76);
            Rule(modal,83,430,110);
            Label(modal,"Khai sơn lập phái. Kiến thiết điện các, thu nhận\nđệ tử, luyện đan và dẫn dắt tông môn.",83,464,620,86,21,new Color(.94f,.92f,.82f));
            Button(modal,SaveRepository.Exists?"TIẾP TỤC HÀNH TRÌNH":"KHAI SƠN LẬP PHÁI",83,590,446,66,()=>{Destroy(modal.gameObject);modalOpen=false;if(SaveRepository.Exists)Load();else StartNew();},true);
            Button(modal,"LƯỢT MỚI",83,672,216,51,()=>{Destroy(modal.gameObject);modalOpen=false;ConfirmNew();});
            Button(modal,"CÁCH CHƠI",313,672,216,51,()=>{Destroy(modal.gameObject);ShowModal("Sống cùng thế giới","1. Xây Trạm gỗ và Nông trại.\n2. Chạm công trình đã xong để giao dân.\n3. Xây Trại lính và tuyển lính giáo.\n4. Chọn tổ đội, chạm đất để di chuyển.\n5. Dự trữ gỗ và thức ăn cho mùa đông.\n6. Mở Thế giới để giao thương hoặc tuyên chiến.\n\nKéo nền để xem làng. Chụm hai ngón để thu phóng.","TRỞ VỀ",Welcome);});
            Label(modal,"BẢN ĐANG PHÁT TRIỂN 0.4  •  LƯU LƯỢT CHƠI TRÊN THIẾT BỊ",84,807,760,32,12,new Color(.65f,.68f,.62f));
        }
        string Description(string id)
        {
            switch(id){case "scripture":return "Đệ tử ngoại môn nghiên cứu kinh điển, tăng tốc tu luyện cho các đệ tử đã thu nhận.";case "meditation":return "Hội tụ linh khí, mỗi cấp tăng 20% tốc độ tu luyện. Tổng hiệu ứng điện các tối đa 3 lần.";case "spiritmine":return "Phân công ngoại môn khai thác linh thạch, tiền tệ cho thu đồ, đột phá và chế tác.";case "herbgarden":return "Trồng linh thảo theo mùa, cung cấp nguyên liệu luyện đan và điều dưỡng.";case "alchemy":return "Tự luyện Trúc Cơ Đan: mỗi mẻ cần 15 linh thạch, 8 linh thảo và 60 công-giây.";case "forge":return "Tự luyện pháp khí: mỗi mẻ cần 20 linh thạch, 12 đá và 60 công-giây.";case "mission":return "Mở nhiệm vụ săn yêu thú cho từng đệ tử. Kết quả tự mô phỏng theo cảnh giới, tư chất và pháp khí.";case "house":return "Mái ấm cho những người sống sót. Thêm 4 chỗ ở cho làng.";case "wood":return "Xẻ gỗ từ khu rừng. Cần cây còn trữ lượng trong bán kính 12 ô.";case "stone":return "Khai thác vật liệu xây dựng. Cần mỏ còn trữ lượng trong bán kính 12 ô.";case "farm":return "Nuôi sống cả làng. Gieo hạt, chờ cây lớn và thu hoạch. Tốc độ phụ thuộc mùa và độ ẩm.";case "store":return "Giữ vật tư an toàn. Thêm 300 sức chứa cho mỗi tài nguyên.";case "barracks":return "Biến dân rảnh thành người bảo vệ làng. Tối đa 4 lượt đào tạo.";case "tower":return "Canh giữ từ trên cao và tự bắn kẻ địch. Cần Nhà chính cấp 2.";case "wall":return "Hàng cọc nhọn làm chậm bước tiến của kẻ tập kích.";case "hunter":return "Cử thợ săn thu thịt từ đàn nai trong 12 ô.";case "forester":return "Dân làm vườn ươm giúp các gốc cây trong 12 ô mọc lại nhanh hơn.";case "well":return "Giữ độ ẩm ruộng trong 9 ô ít nhất 55%.";case "hearth":return "Giảm 40% lượng gỗ sưởi tiêu thụ vào mùa đông.";case "gate":return "Lối đi cho quân ta qua hàng phòng thủ.";default:return "Trái tim của ngôi làng. Giữ công trình này đứng vững để sống sót.";}
        }
        void ShowBuildMenu()
        {
            selectedResource=-1;selectedNeighbor=-1;selectedSquad=-2;selectedBuilding=-1;details.gameObject.SetActive(false);buildMenu.gameObject.SetActive(true);Clear(buildMenu);
            Heading(buildMenu,"Kiến thiết tông môn",19,13,372,44,25);int pages=(Catalog.Buildings.Length-2)/9+1;buildPage=Mathf.Clamp(buildPage,0,pages-1);Label(buildMenu,"Trang "+(buildPage+1)+"/"+pages+" · Chọn công trình để xây",20,61,370,32,13);Rule(buildMenu,19,100,372);
            for(int i=1+buildPage*9;i<Catalog.Buildings.Length&&i<1+(buildPage+1)*9;i++)
            {
                var d=Catalog.Buildings[i];int idx=i,col=(i-1-buildPage*9)%3,row=(i-1-buildPage*9)/3;
                var card=Button(buildMenu,"",17+col*126,115+row*138,120,130,()=>BeginPlace(Catalog.Buildings[idx].id));
                Icon(card.transform,UISkin.BuildingIcon(d.id),19,1,82);Label(card.transform,d.name,4,83,112,23,13,gold).alignment=TextAnchor.MiddleCenter;
                Label(card.transform,d.wood+" gỗ · "+d.stone+" đá",3,108,114,18,11).alignment=TextAnchor.MiddleCenter;
                if(d.id=="tower"&&world.level<2){card.interactable=false;Label(card.transform,"CẤP 2",26,33,68,31,14,gold).alignment=TextAnchor.MiddleCenter;}
            }
            Button(buildMenu,"TRANG TRƯỚC",18,546,177,34,()=>{buildPage=Mathf.Max(0,buildPage-1);ShowBuildMenu();});Button(buildMenu,"TRANG SAU",207,546,182,34,()=>{buildPage=Mathf.Min(pages-1,buildPage+1);ShowBuildMenu();});
            Button(buildMenu,"ĐÓNG DANH MỤC",17,588,372,39,CancelPlace);
        }
        void ShowPlacement()
        {
            Clear(buildMenu);var d=Catalog.Get(placing);Heading(buildMenu,d.name,19,15,372,46,26);Rule(buildMenu,19,70,372);
            Icon(buildMenu,UISkin.BuildingIcon(d.id),30,87,144);Label(buildMenu,Description(d.id),192,99,190,146,16);
            var cost=Box("Cost",buildMenu,20,263,370,76,new Color(.23f,.19f,.13f));Icon(cost,0,8,10,47);Label(cost,d.wood.ToString(),60,18,70,37,23,gold);Icon(cost,1,143,10,47);Label(cost,d.stone.ToString(),196,18,70,37,23,gold);
            Label(buildMenu,"Thời gian xây: "+d.seconds+" giây\nCần 1 dân rảnh trong suốt quá trình xây.\n\nChạm đất để chọn vị trí. Viền xanh là đặt được.",22,358,365,130,15);
            Button(buildMenu,"XOAY CÔNG TRÌNH",19,475,372,43,()=>{rotation=(rotation+1)%2;RebuildGhost();});
            Button(buildMenu,"XÁC NHẬN XÂY",19,529,372,51,()=>{if(world.Place(placing,placeX,placeZ,rotation))CancelPlace();else transient=world.message;},true);
            Button(buildMenu,"CHỌN CÔNG TRÌNH KHÁC",19,590,372,37,()=>{CancelPlace();ShowBuildMenu();});
        }
        void RefreshDetails()
        {
            if(selectedResource>=0){RefreshResourceDetails();return;}if(selectedBuilding<0){details.gameObject.SetActive(false);return;}
            var b=world.buildings.Find(v=>v.uid==selectedBuilding);if(b==null){selectedBuilding=-1;details.gameObject.SetActive(false);return;}
            details.gameObject.SetActive(true);Clear(details);var d=Catalog.Get(b.type);detailWasComplete=b.complete;detailLevel=b.level;trainingState=null;upgradeStatus=null;
            Heading(details,d.name+" · C"+b.level,18,13,296,42,21);Rule(details,18,63,296);Icon(details,UISkin.BuildingIcon(b.type),18,76,114);
            detailState=Label(details,"",148,92,168,85,17,gold);
            Label(details,Description(b.type),19,200,296,85,14);
            if(b.complete&&d.slots>0)
            {
                Rule(details,18,294,296);Heading(details,"Dân làm việc  "+b.workers+" / "+world.WorkSlots(b),19,306,296,37,20);
                Button(details,"− BỚT DÂN",19,359,141,54,()=>world.Assign(b.uid,-1));Button(details,"+ GIAO DÂN",171,359,142,54,()=>world.Assign(b.uid,1),true);
                if(b.type=="farm")FarmActions(b);else Label(details,"Dân rảnh còn lại: "+world.Idle+"\nLinh thạch, linh thảo xem tại Tông Môn.",19,438,296,59,14,mint);
            }
            if(b.complete&&b.type=="barracks")
            {
                for(int i=0;i<3;i++){int k=i;Button(details,Catalog.UnitNames[i]+" · "+Catalog.UnitFood[i]+" lương thực",19,295+i*53,295,46,()=>world.Train(b.uid,k));}
                trainingState=Label(details,"",19,463,296,60,14,mint);Button(details,"HỦY LƯỢT ĐÀO TẠO CUỐI",19,503,295,34,()=>world.CancelTraining(b.uid));
            }
            if(b.type=="hall")
            {
                Heading(details,"CHÁNH ĐIỆN CẤP "+world.level,19,303,296,35,19);Label(details,"Chỗ ở: "+world.people+" / "+world.Housing+"\nTinh thần: "+(int)world.morale+" / 100",19,350,296,69,16,mint);
                Button(details,"ĐIỀU HÀNH TÔNG MÔN",19,446,295,60,()=>ShowSectPanel(),true);
            }
            if(b.complete){upgradeStatus=Button(details,"NÂNG CẤP",19,548,295,34,()=>ShowUpgrade(b.uid),true).GetComponentInChildren<Text>();Button(details,"SỬA · 10 GỖ",19,590,141,36,()=>world.Repair(b.uid));}
            if(b.type!="hall")Button(details,b.complete?"THÁO DỠ":"HỦY XÂY 80%",b.complete?170:19,590,b.complete?144:295,36,()=>{world.Demolish(b.uid);selectedBuilding=-1;});
        }
        void UpdateMap()
        {
            if(mapTexture==null)return;var pixels=mapPixels;
            for(int z=0;z<128;z++)for(int x=0;x<128;x++)
            {
                float wx=(x/128f-.5f)*96,wz=(z/128f-.5f)*96,r=Mathf.Sqrt(wx*wx+wz*wz),noise=Mathf.PerlinNoise(x*.24f,z*.24f);
                Color color=r<15?new Color(.32f,.42f,.22f):Color.Lerp(new Color(.08f,.19f,.12f),new Color(.25f,.33f,.17f),noise);
                if(r<7||(Mathf.Abs(wx)<1.4f&&wz>-12&&wz<25)||(Mathf.Abs(wz+4)<1.1f&&wx>-3&&wx<24))color=new Color(.49f,.4f,.24f);
                if(wx>15&&wx<28&&Mathf.Abs(wz)<12&&noise>.55f)color=new Color(.42f,.44f,.37f);pixels[z*128+x]=color;
            }
            Action<float,float,int,Color> dot=(x,z,size,color)=>{int a=Mathf.Clamp((int)((x/96+.5f)*128),0,127),b=Mathf.Clamp((int)((z/96+.5f)*128),0,127);for(int dx=-size;dx<=size;dx++)for(int dz=-size;dz<=size;dz++)if(a+dx>=0&&a+dx<128&&b+dz>=0&&b+dz<128)pixels[(b+dz)*128+a+dx]=color;};
            foreach(var b in world.buildings)dot(b.x+b.W*.5f,b.z+b.D*.5f,b.type=="hall"?3:2,b.type=="hall"?gold:new Color(.77f,.69f,.45f));
            foreach(var n in world.resources.Where(n=>n.amount>0&&n.kind==ResourceKind.Deer))dot(n.x,n.z,1,new Color(.7f,.5f,.3f));
            foreach(var v in world.villages)dot(v.x,v.z,3,v.defeated?Color.gray:v.warWithPlayer?new Color(.9f,.25f,.13f):new Color(.4f,.66f,.83f));
            foreach(var u in world.units)dot(u.x,u.z,1,u.enemy?new Color(.9f,.25f,.13f):mint);dot(cameraFocus.x,cameraFocus.z,1,Color.white);
            var corners=new Vector3[4];var plane=new Plane(Vector3.up,Vector3.zero);
            for(int i=0;i<4;i++){var ray=cam.ViewportPointToRay(new Vector3(i==0||i==3?0:1,i<2?0:1,0));if(plane.Raycast(ray,out float d))corners[i]=ray.GetPoint(d);}
            for(int edge=0;edge<4;edge++)for(int i=0;i<100;i++){var p=Vector3.Lerp(corners[edge],corners[(edge+1)%4],i/99f);dot(p.x,p.z,0,new Color(.73f,.69f,.47f));}
            mapTexture.SetPixels32(pixels);mapTexture.Apply();
        }
        void RefreshUI()
        {
            if(resourceText==null)return;woodValue.text=((int)world.wood).ToString();stoneValue.text=((int)world.stone).ToString();foodValue.text=((int)world.food).ToString();
            popText.text=world.people+" / "+world.Housing+" người\n"+world.Idle+" dân rảnh\nTinh thần "+(int)world.morale+"%";
            foodFlow.text="Lương thực "+world.foodRate.ToString("+0.0;-0.0;0.0")+" / giây";foodFlow.color=world.foodRate<0?new Color(.95f,.48f,.27f):mint;
            int remain=Mathf.Max(0,Mathf.CeilToInt((world.Night?360:world.DaylightSeconds)-world.clock));clockText.text="NGÀY "+world.day+" · "+world.SeasonName+" · NĂM "+world.Year+"\n"+world.WeatherName+" "+world.Temperature.ToString("0")+"°C\n"+(world.Night?"Đêm còn ":"Đêm sau ")+(remain/60).ToString("00")+":"+(remain%60).ToString("00");speedText.text=speed+"×";
            statusText.text=string.IsNullOrEmpty(transient)?world.message:transient;
            bool wood=world.buildings.Any(b=>b.type=="wood"&&b.workers>0),farm=world.buildings.Any(b=>b.type=="farm"&&b.workers>0);
            goalText.text=world.SectRank+"\nLinh thạch "+(int)world.spiritStones+" · Linh thảo "+(int)world.herbs+"\nĐệ tử "+world.disciples.Count+" · Danh vọng "+(int)world.sectReputation;

            selectionText.text=placing!=null?"Chọn đất để dựng "+Catalog.Get(placing).name:selectedBuilding>=0?"Chăm lo ngôi làng":selectedSquad==-1?"Toàn quân chờ lệnh":selectedSquad>=0?"Tổ "+(selectedSquad+1)+" đang chờ lệnh":"Ngôi làng đang chờ bạn";
            for(int i=0;i<3;i++){squadValues[i].text=world.units.Count(u=>!u.enemy&&u.squad==i)+" / "+World.SquadSize;squadCards[i].image.color=(selectedSquad==i||selectedSquad==-1)?new Color(.42f,.52f,.36f):new Color(.27f,.23f,.17f);}
            UpdateMap();if(placing!=null)UpdateGhost();RefreshLivingUI();
            if(selectedBuilding>=0)
            {
                var b=world.buildings.Find(v=>v.uid==selectedBuilding);if(b==null||b.complete!=detailWasComplete||b.level!=detailLevel){RefreshDetails();return;}
                var d=Catalog.Get(b.type);if(detailState!=null)detailState.text=b.hp+" / "+world.MaxHp(b)+" HP\n"+(b.complete?"Hoạt động":"Đang xây "+(int)(100*b.progress/d.seconds)+"%");
                if(upgradeStatus!=null)upgradeStatus.text=b.upgrading?"NÂNG "+(int)b.upgradeProgress+"/"+world.UpgradeSeconds(b)+" GIÂY · HỦY":b.level>=3?"ĐÃ ĐẠT CẤP 3":"NÂNG LÊN CẤP "+(b.level+1);
                if(trainingState!=null){var q=world.queue.Where(t=>t.building==b.uid).ToArray();trainingState.text="Đào tạo: "+q.Length+" / 4"+(q.Length>0?"\n"+Catalog.UnitNames[q[0].kind]+" · "+(int)q[0].progress+" / "+Catalog.UnitTime[q[0].kind]+" giây":"");}
            }
        }
    }
}

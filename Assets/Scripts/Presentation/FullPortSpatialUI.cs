using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LangTroTan.Presentation
{
    public sealed partial class FullPortView
    {
        RectTransform spatialPanel;Text spatialInfo;string spatialBuildingId,spatialNodeId;
        void CloseSpatialDetail()
        {if(spatialPanel!=null){spatialPanel.gameObject.SetActive(false);Destroy(spatialPanel.gameObject);}spatialPanel=null;spatialInfo=null;spatialBuildingId=null;spatialNodeId=null;}
        void ShowSpatialCatalog()
        {
            if(spatial==null){status.text="Hãy kết nối tông môn để xây dựng.";return;}
            if(SliceEnabled){ShowSliceBuildingCatalog();return;}
            CancelSpatialPlace();CloseSpatialDetail();OpenDialog("Xây dựng Sơn Môn",1140,650);
            string[] types={"house","store","wood","stone","farm","hunter","forester","well","hearth","wall","gate"};
            int pages=(types.Length+5)/6;spatialCatalogPage=Mathf.Clamp(spatialCatalogPage,0,pages-1);
            int i=0;foreach(string type in types.Skip(spatialCatalogPage*6).Take(6))
            {
                var d=Get(spatialCatalog,type);int x=24+i%3*365,y=78+i/3*228;i++;
                var card=Box(dialog,type,x,y,350,212,new Color(.11f,.22f,.20f));
                Label(card,S(Get(d,"name")),14,12,323,36,22,true);
                Label(card,"Gỗ "+S(Get(d,"wood"))+" · Đá "+S(Get(d,"stone"))+"\nDiện tích "+S(Get(d,"width"))+" × "+S(Get(d,"depth"))+" ô\nXây trong "+Math.Ceiling(N(Get(d,"seconds"))/60)+" phút · cần 1 người",14,58,323,94,18);
                Button(card,"CHỌN VỊ TRÍ",14,160,323,40,()=>BeginSpatialPlace(type));
            }
            Button(dialog,"TRƯỚC",24,557,260,48,()=>{spatialCatalogPage--;ShowSpatialCatalog();});
            Button(dialog,"SAU",302,557,260,48,()=>{spatialCatalogPage++;ShowSpatialCatalog();});
            Button(dialog,"NHẬN DÂN PHỤC DỊCH",580,557,535,48,()=>{api.Submit("spatial_recruit",null);});
        }
        void OpenSpatialDetail(string name)
        {
            if(SliceEnabled){CloseSpatialDetail();body.gameObject.SetActive(false);spatialPanel=Surface(canvas,name,1234,132,346,538,paper);Ink(spatialPanel,name,18,17,310,54,23,true);spatialInfo=Ink(spatialPanel,"",18,82,310,240,17);SliceButton(spatialPanel,"Đóng",18,481,310,42,CloseSpatialDetail);return;}
            CloseSpatialDetail();body.gameObject.SetActive(false);spatialPanel=Box(canvas,name,1210,134,374,538,ink);
            Label(spatialPanel,name,18,12,338,62,23,true);spatialInfo=Label(spatialPanel,"",18,82,338,240,18);
            Button(spatialPanel,"ĐÓNG",18,481,338,42,CloseSpatialDetail);
        }
        Button ContextButton(Transform parent,string text,float x,float y,float w,float h,Action action)=>SliceEnabled?SliceButton(parent,text,x,y,Mathf.Min(w,310),h,action):Button(parent,text,x,y,w,h,action);
        void ShowSliceBuildingCatalog()
        {
            CancelSpatialPlace();CloseSpatialDetail();CloseDialog();projectionActive=false;commandScreen=false;
            SliceBody("Sơn Môn / Kiến thiết","Gây dựng cơ nghiệp","Chọn công trình, đặt trên đất trống và giữ lối đi từ Sơn Môn. Vật tư và tiến độ được máy chủ xác nhận.");
            string[] types={"house","store","wood","stone","farm","hunter","forester","well","hearth","wall","gate"};spatialCatalogPage=Mathf.Clamp(spatialCatalogPage,0,(types.Length-1)/6);int i=0;
            foreach(string type in types.Skip(spatialCatalogPage*6).Take(6)){int x=28+i%3*462,y=154+i/3*205;i++;var d=Get(spatialCatalog,type);var card=Surface(body,type,x,y,439,187,new Color(.98f,.965f,.91f));Ink(card,S(Get(d,"name")),19,15,400,35,23,true);Ink(card,Fmt(Get(d,"wood"))+" gỗ · "+Fmt(Get(d,"stone"))+" đá    |    "+S(Get(d,"width"))+" × "+S(Get(d,"depth"))+" ô",19,63,400,29,18);Ink(card,Math.Ceiling(N(Get(d,"seconds"))/60)+" phút · 1 người xây",19,97,400,27,16);SliceButton(card,"Chọn vị trí",18,133,403,40,()=>BeginSpatialPlace(type),true);}
            SliceButton(body,"‹ Trước",28,590,190,46,()=>{spatialCatalogPage--;ShowSliceBuildingCatalog();});SliceButton(body,"Sau ›",236,590,190,46,()=>{spatialCatalogPage++;ShowSliceBuildingCatalog();});SliceButton(body,"Trở về Sơn Môn",1050,590,340,46,()=>{tab=0;Draw();});
        }
        void ShowSpatialOverview()
        {
            if(SliceEnabled){CloseSpatialDetail();body.gameObject.SetActive(false);spatialPanel=Surface(canvas,"Sơn Môn",1254,124,326,160,paper);spatialInfo=Ink(spatialPanel,"",18,17,290,133,17);RefreshSpatialInfo();return;}
            OpenSpatialDetail("Sơn Môn · Tông môn");RefreshSpatialInfo();
            ContextButton(spatialPanel,"XÂY THÊM CÔNG TRÌNH",18,322,338,40,ShowSpatialCatalog);
            ContextButton(spatialPanel,"NHẬN DÂN PHỤC DỊCH",18,371,338,40,()=>api.Submit("spatial_recruit",null));
            Label(spatialPanel,"Kéo để di chuyển · Cuộn hoặc chụm hai ngón để thu phóng. Chạm nhà, cây, đá để quản lý.",18,423,338,56,16);
        }
        void ShowSpatialBuilding(string id)
        {
            var b=List(Get(spatial,"buildings")).FirstOrDefault(v=>S(Get(v,"id"))==id);if(b==null)return;
            string core=S(Get(b,"coreId"),"");if(core!=""){ShowUpgrade(core);return;}
            string type=S(Get(b,"type"));OpenSpatialDetail(S(Get(spatialCatalog,type,"name")));spatialBuildingId=id;RefreshSpatialInfo();
            int slots=(int)N(Get(spatialCatalog,type,"slots"));
            if(slots>0){ContextButton(spatialPanel,"− NGƯỜI",18,322,148,40,()=>SpatialBuildingAction("spatial_assign",-1));ContextButton(spatialPanel,"+ NGƯỜI",180,322,148,40,()=>SpatialBuildingAction("spatial_assign",1));}
            ContextButton(spatialPanel,"NÂNG CẤP",18,371,148,40,()=>SpatialBuildingAction("spatial_upgrade"));
            ContextButton(spatialPanel,"HỦY NÂNG",180,371,148,40,()=>SpatialBuildingAction("spatial_cancel_upgrade"));
            if(type=="farm"){
                ContextButton(spatialPanel,"GIEO LÚA",18,423,148,40,()=>SpatialBuildingAction("spatial_sow",0,"wheat"));
                ContextButton(spatialPanel,"GIEO RAU",180,423,148,40,()=>SpatialBuildingAction("spatial_sow",0,"vegetable"));
            }else{
                ContextButton(spatialPanel,"SỬA CHỮA",18,423,148,40,()=>SpatialBuildingAction("spatial_repair"));
                ContextButton(spatialPanel,"THÁO DỠ",180,423,148,40,()=>ConfirmAction("Tháo công trình","Hoàn 30% vật tư nếu xây xong, 80% nếu đang xây. Người làm sẽ được thu hồi.","spatial_demolish",new Dictionary<string,object>{{"buildingId",id}}));
            }
        }
        void SpatialBuildingAction(string action,int delta=0,string crop=null)
        {if(spatialBuildingId==null)return;var payload=new Dictionary<string,object>{{"buildingId",spatialBuildingId}};if(delta!=0)payload["delta"]=delta;if(crop!=null)payload["crop"]=crop;api.Submit(action,payload);}
        void ShowSpatialNode(string id)
        {
            var n=List(Get(spatial,"nodes")).FirstOrDefault(v=>S(Get(v,"id"))==id);if(n==null)return;
            string kind=S(Get(n,"kind"));OpenSpatialDetail(kind=="tree"?"Rừng cây":kind=="stone"?"Mạch đá":"Đàn hươu");spatialNodeId=id;RefreshSpatialInfo();
            ContextButton(spatialPanel,kind=="deer"?"CỬ THỢ SĂN":"CỬ NGƯỜI KHAI THÁC",18,322,338,40,()=>api.Submit("spatial_harvest",new Dictionary<string,object>{{"nodeId",id}}));
            ContextButton(spatialPanel,"HỦY CHUYẾN",18,371,338,40,()=>api.Submit("spatial_cancel_harvest",new Dictionary<string,object>{{"nodeId",id}}));
            if(kind=="tree")ContextButton(spatialPanel,"TRỒNG LẠI · 5 GỖ",18,423,338,40,()=>api.Submit("spatial_replant",new Dictionary<string,object>{{"nodeId",id}}));
        }
        string SpatialTime(object timestamp)
        {double remaining=N(timestamp)-N(Get(spatial,"lastAdvancedAt"));return remaining>0?Math.Ceiling(remaining/60000)+" phút":"Đã xong";}
        void RefreshSpatialInfo()
        {
            if(spatialInfo==null)return;
            if(spatialBuildingId!=null){var b=List(Get(spatial,"buildings")).FirstOrDefault(v=>S(Get(v,"id"))==spatialBuildingId);if(b==null){CloseSpatialDetail();return;}
                string type=S(Get(b,"type"));int level=(int)N(Get(b,"level"));var d=Get(spatialCatalog,type);double hp=N(Get(d,"hp"))+Math.Floor(N(Get(d,"hp"))/2)*(level-1);
                spatialInfo.text="Cấp "+level+" / 3 · HP "+Math.Floor(N(Get(b,"hp")))+" / "+hp+"\nNgười làm: "+S(Get(b,"workers"))+" / "+(N(Get(d,"slots"))>0?N(Get(d,"slots"))+level-1:0)+"\nDân rảnh: "+S(Get(spatial,"idle"))+" · Sức chứa: "+S(Get(spatial,"housing"))+"\n";
                if(!(Get(b,"complete") is bool done&&done))spatialInfo.text+="Đang xây: "+SpatialTime(Get(b,"finishAt"))+"\n";
                else if(N(Get(b,"upgradeAt"))>0)spatialInfo.text+="Đang nâng: "+SpatialTime(Get(b,"upgradeAt"))+"\n";
                else if(level<3)spatialInfo.text+="Nâng cấp: "+Math.Max(20,N(Get(d,"wood")))*level+" gỗ · "+Math.Max(10,N(Get(d,"stone")))*level+" đá\nThời gian: "+5*level+" phút\n";
                if(type=="farm")spatialInfo.text+="Vụ: "+(S(Get(b,"crop"))=="vegetable"?"Rau":"Lúa")+" · "+Math.Floor(N(Get(b,"cropProgress"))*100)+"%\nĐộ ẩm: "+Math.Floor(N(Get(b,"moisture")))+"% · Giống: 5 lương thực";
            }else if(spatialNodeId!=null){var n=List(Get(spatial,"nodes")).FirstOrDefault(v=>S(Get(v,"id"))==spatialNodeId);var job=List(Get(spatial,"jobs")).FirstOrDefault(v=>S(Get(v,"nodeId"))==spatialNodeId);string phase=S(Get(job,"phase"),"");
                spatialInfo.text="Còn lại: "+Math.Floor(N(Get(n,"amount")))+" / "+S(Get(n,"maximum"))+"\nDân rảnh: "+S(Get(spatial,"idle"))+"\nChuyến khai thác: "+List(Get(spatial,"jobs")).Count+" / 4\n\n"+(job!=null?(phase=="outbound"?"Đang đi đến nguồn":phase=="work"?"Đang khai thác":"Đang mang vật tư về kho"):"Mỗi chuyến cần một người rảnh.");
                if(N(Get(n,"amount"))==0&&N(Get(n,"regrowAt"))>0)spatialInfo.text+="\nHồi phục sau: "+SpatialTime(Get(n,"regrowAt"));
            }else{
                string weather=S(Get(spatial,"environment","weather"));string[] seasons={"Xuân","Hạ","Thu","Đông"};int season=Mathf.Clamp((int)N(Get(spatial,"environment","season")),0,3);
                if(SliceEnabled){spatialInfo.text=seasons[season]+" · "+(weather=="rain"?"Mưa":weather=="snow"?"Tuyết":weather=="storm"?"Bão":weather=="cloudy"?"Nhiều mây":"Nắng")+" · "+Fmt(Get(spatial,"environment","temperature"))+"°C\nDân phục dịch "+Fmt(Get(spatial,"people"))+" / "+Fmt(Get(spatial,"housing"))+" · Rảnh "+Fmt(Get(spatial,"idle"))+"\nTinh thần "+Fmt(Get(spatial,"morale"))+"% · "+List(Get(spatial,"buildings")).Count+" công trình\nKéo để di chuyển · Cuộn để thu phóng";return;}
                spatialInfo.text="Năm "+S(Get(spatial,"environment","year"))+" · Mùa "+seasons[season]+"\n"+(Get(spatial,"environment","night") is bool night&&night?"Đêm":"Ban ngày")+" · "+(weather=="rain"?"Mưa":weather=="snow"?"Tuyết":weather=="storm"?"Bão":weather=="cloudy"?"Nhiều mây":"Nắng")+" · "+S(Get(spatial,"environment","temperature"))+"°C\n\nDân phục dịch: "+S(Get(spatial,"people"))+" / "+S(Get(spatial,"housing"))+"\nNgười rảnh: "+S(Get(spatial,"idle"))+" · Tinh thần: "+Math.Floor(N(Get(spatial,"morale")))+"%\nCông trình: "+List(Get(spatial,"buildings")).Count+" / 80\nSức chứa mỗi tài nguyên: "+S(Get(spatial,"capacity"));
            }
        }
        IEnumerator SmokeSpatial()
        {
            yield return api.SpatialState(false,ApplySpatial);if(spatial==null){System.IO.File.AppendAllText(smokeFile,"FAIL spatial snapshot missing\n");yield break;}
            tab=0;Draw();yield return new WaitForSecondsRealtime(.4f);Capture("full-port-spatial-overview.png");
            ShowSpatialCatalog();yield return new WaitForSecondsRealtime(.2f);Capture("full-port-spatial-catalog.png");
            BeginSpatialPlace("house");yield return new WaitForSecondsRealtime(.2f);Capture("full-port-spatial-place.png");CancelSpatialPlace();
            var node=List(Get(spatial,"nodes")).Where(n=>S(Get(n,"kind"))=="tree"&&N(Get(n,"amount"))>0&&!List(Get(spatial,"jobs")).Any(j=>S(Get(j,"nodeId"))==S(Get(n,"id")))).OrderBy(n=>Math.Abs(N(Get(n,"x")))+Math.Abs(N(Get(n,"z")))).FirstOrDefault();
            if(node!=null){string id=S(Get(node,"id"));ShowSpatialNode(id);yield return new WaitForSecondsRealtime(.2f);Capture("full-port-spatial-node.png");
                api.Submit("spatial_harvest",new Dictionary<string,object>{{"nodeId",id}});while(api.Busy)yield return null;yield return api.SpatialState(false,ApplySpatial);
                yield return new WaitForSecondsRealtime(1);Capture("full-port-spatial-worker.png");
                System.IO.File.AppendAllText(smokeFile,List(Get(spatial,"jobs")).Any(j=>S(Get(j,"nodeId"))==id)&&!api.HasPending?"PASS authoritative harvest creates animated native worker\n":"FAIL missing spatial worker\n");
            }
            CloseSpatialDetail();var store=List(Get(spatial,"buildings")).FirstOrDefault(b=>S(Get(b,"type"))=="store");if(store!=null){ShowSpatialBuilding(S(Get(store,"id")));yield return new WaitForSecondsRealtime(.2f);Capture("full-port-spatial-building.png");}CloseSpatialDetail();
            System.IO.File.AppendAllText(smokeFile,"PASS native spatial overview, catalog, placement ghost and details rendered\n");tab=1;Draw();
        }
    }
}

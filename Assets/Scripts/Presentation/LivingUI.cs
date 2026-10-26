using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using LangTroTan.Domain;

namespace LangTroTan.Presentation
{
    public partial class Game
    {
        int selectedResource=-1,selectedNeighbor=-1,buildPage;Text resourceSummary,resourceTaskSummary,cropSummary;
        void SelectResource(int id)
        {CancelPlace();selectedBuilding=-1;selectedNeighbor=-1;selectedResource=id;selectedSquad=-2;RefreshDetails();}
        void RefreshResourceDetails()
        {
            var n=world.resources.Find(v=>v.id==selectedResource);if(n==null){selectedResource=-1;details.gameObject.SetActive(false);return;}
            details.gameObject.SetActive(true);Clear(details);int icon=n.kind==ResourceKind.Tree?0:n.kind==ResourceKind.Stone?1:3;
            Heading(details,n.kind==ResourceKind.Tree?"Cây rừng":n.kind==ResourceKind.Stone?"Mỏ đá":"Đàn nai",18,15,296,42,24);Rule(details,18,65,296);Icon(details,icon,95,80,130);
            resourceSummary=Label(details,"Trữ lượng: "+(int)n.amount+" / "+(int)n.maximum+"\n"+(n.amount>0?"Có thể khai thác":"Phục hồi sau khoảng "+Mathf.CeilToInt(n.regrow)+" giây"),20,232,293,87,18,gold);
            var task=world.tasks.Find(t=>t.node==n.id);resourceTaskSummary=Label(details,task==null?"Cử một dân rảnh đến nguồn này. Người đó thu một chuyến rồi tự mang tài nguyên về làng.":task.phase==0?"Dân đang trên đường đến nguồn":task.phase==1?"Đang khai thác nguồn tài nguyên":"Đang mang vật tư về làng",20,333,293,85,15);
            Button(details,n.kind==ResourceKind.Tree?"CỬ DÂN CHẶT CÂY":n.kind==ResourceKind.Stone?"KHAI THÁC ĐÁ":"CỬ DÂN ĐI SĂN",20,432,293,48,()=>world.BeginHarvest(n.id),true);
            if(n.kind==ResourceKind.Tree)Button(details,"TRỒNG LẠI · 5 GỖ",20,494,293,40,()=>world.PlantTree(n.id));
            Button(details,"HỦY CHUYẾN KHAI THÁC",20,546,293,34,()=>world.CancelHarvest(n.id));
            Button(details,"ĐÓNG",20,590,293,36,()=>{selectedResource=-1;details.gameObject.SetActive(false);});
        }
        void ShowWorldPanel()
        {
            selectedResource=-1;selectedBuilding=-1;details.gameObject.SetActive(false);
            if(modal!=null)Destroy(modal.gameObject);modalOpen=true;modal=Panel("Modal overlay",canvas,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0,0,0,.7f));
            var box=Panel("World diplomacy",modal,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-520,-337),new Vector2(520,337),ink);
            Heading(box,"Thế giới và các làng lân cận",24,15,990,48,29);
            Label(box,"Ngày "+world.day+" · Năm "+world.Year+" · Mùa "+world.SeasonName+" "+world.SeasonDay+"/"+World.DaysPerSeason+" · "+world.WeatherName+" · "+world.Temperature.ToString("0")+"°C",25,73,990,35,18,gold);
            Label(box,"Mỗi mùa kéo dài 4 ngày. Mưa tưới ruộng; giông làm chậm việc ngoài trời. Mùa đông cần dự trữ thức ăn và gỗ sưởi.",25,115,990,49,15);Rule(box,25,176,990);
            for(int i=0;i<world.villages.Count;i++)
            {
                var v=world.villages[i];var row=Box(v.name,box,23,191+i*137,994,125,new Color(.32f,.27f,.2f));Icon(row,4,10,8,82);
                Heading(row,v.name+(v.defeated?" · Thất thủ":v.warWithPlayer?" · Chiến tranh":""),104,8,440,33,21);
                Label(row,"Dân "+v.population+" · Quân tại làng "+v.army+" · Công trình "+v.buildings+" · Quan hệ "+v.relation+"\n"+v.decision+"\nHP "+(int)v.hp+" / "+(int)v.maxHp,104,45,482,74,14);
                int id=v.id;
                Button(row,"XEM LÀNG",607,10,174,43,()=>{cameraFocus=new Vector3(v.x,0,v.z);UpdateCamera();CloseWorldPanel();});
                Button(row,v.warWithPlayer?"ĐÌNH CHIẾN":"TẶNG QUÀ",796,10,182,43,()=>{if(v.warWithPlayer)world.OfferPeace(id);else world.Gift(id);WorldActionFeedback();});
                Button(row,"GIAO THƯƠNG",607,66,174,43,()=>{world.Trade(id);WorldActionFeedback();});
                Button(row,v.warWithPlayer?"ĐƯA QUÂN TỚI":"TUYÊN CHIẾN",796,66,182,43,()=>{if(v.warWithPlayer){SelectSquad(-1);world.Order(-1,v.x,v.z);cameraFocus=new Vector3(v.x,0,v.z);UpdateCamera();CloseWorldPanel();}else ConfirmWar(id);},true);
            }
            Label(box,"Quà: 30 thức ăn · Giao thương: 40 thức ăn đổi 25 đá · Đình chiến: 60 thức ăn và 40 gỗ",25,614,790,37,13);
            Button(box,"VỀ LÀNG",838,613,178,40,CloseWorldPanel);
        }
        void CloseWorldPanel(){if(modal!=null)Destroy(modal.gameObject);modalOpen=false;}
        void WorldActionFeedback(){var message=world.message;ShowModal("Ngoại giao",message,"TRỞ VỀ BẢN ĐỒ",ShowWorldPanel);}
        void ConfirmWar(int id)
        {
            var v=world.villages.Find(n=>n.id==id);if(v==null)return;
            ShowModal("Tuyên chiến với "+v.name,"Hai làng sẽ trở thành kẻ địch. Quân AI có thể xuất kích tấn công làng của bạn. Giao thương và tặng quà bị ngừng cho đến khi đình chiến.","TUYÊN CHIẾN",()=>{world.DeclareWar(id);ShowWorldPanel();},"QUAY LẠI",ShowWorldPanel);
        }
        void FarmActions(Building b)
        {
            string growth=!b.planted?"Cần gieo hạt":b.crop=="vegetable"&&world.Temperature<=0?"Rau đang ngủ đông":(b.crop=="vegetable"?"Rau":"Lúa mì")+" · "+Mathf.Min(100,(int)(b.cropProgress*100))+"%";
            cropSummary=Label(details,growth+" · Ẩm "+(int)b.moisture+"%",19,424,296,29,14,mint);
            Button(details,"GIEO LÚA · 5 ĂN",19,464,144,46,()=>{world.Sow(b.uid,"wheat",true);});
            Button(details,"TRỒNG RAU · 5 ĂN",170,464,144,46,()=>{world.Sow(b.uid,"vegetable",true);});
        }
        void RefreshLivingUI()
        {
            if(selectedResource>=0)
            {
                var n=world.resources.Find(v=>v.id==selectedResource);if(n!=null&&resourceSummary!=null)resourceSummary.text="Trữ lượng: "+(int)n.amount+" / "+(int)n.maximum+"\n"+(n.amount>0?"Có thể khai thác":"Phục hồi: "+Mathf.CeilToInt(n.regrow)+" giây");
                var task=world.tasks.Find(t=>t.node==selectedResource);if(resourceTaskSummary!=null)resourceTaskSummary.text=task==null?"Dân rảnh: "+world.Idle+" · Chọn tác vụ để cử người đi.":task.phase==0?"Đang đi tới nguồn":task.phase==1?"Đang khai thác":"Đang mang vật tư về làng";
            }
            var b=world.buildings.Find(v=>v.uid==selectedBuilding&&v.type=="farm");if(b!=null&&cropSummary!=null)cropSummary.text=(b.crop=="vegetable"?"Rau":"Lúa mì")+" · "+(int)(b.cropProgress*100)+"% · Ẩm "+(int)b.moisture+"%";
        }
    }
}

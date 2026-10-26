using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using LangTroTan.Domain;

namespace LangTroTan.Presentation
{
    public partial class Game
    {
        readonly Dictionary<int,int> buildingLevels=new Dictionary<int,int>();
        Text upgradeStatus;int detailLevel,sectPage,sectTab;
        void SectImage(Transform parent,string key,float x,float y,float width,float height)
        {
            var texture=Resources.Load<Texture2D>("Art/Sect/"+key);if(texture==null)return;
            var rect=new GameObject(key,typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.offsetMin=new Vector2(x,-y-height);rect.offsetMax=new Vector2(x+width,-y);rect.GetComponent<RawImage>().texture=texture;rect.GetComponent<RawImage>().raycastTarget=false;
        }
        void ShowUpgrade(int id)
        {
            var b=world.buildings.Find(v=>v.uid==id);if(b==null)return;
            if(b.upgrading){ShowModal("Hủy nâng cấp?","Hoàn 80% vật tư đã trả. Công trình vẫn giữ cấp hiện tại.","HỦY NÂNG CẤP",()=>{world.CancelUpgrade(id);CloseWorldPanel();RefreshDetails();},"TIẾP TỤC XÂY",()=>{CloseWorldPanel();});return;}
            if(b.level>=3){world.message="Đã đạt cấp 3 trong bản thử nghiệm";return;}
            string effect=b.type=="house"?"Mỗi cấp thêm 4 chỗ ở.":b.type=="store"?"Mỗi cấp thêm 300 sức chứa mỗi vật tư.":b.type=="hall"?"Thăng hạng tông môn, mở điện các và ghế đệ tử.":"Mỗi cấp thêm HP, hiệu suất lao động +20% nhân dồn; công trình có chỗ làm thêm 1 chỗ.";
            ShowModal("Nâng "+Catalog.Get(b.type).name+" → Cấp "+(b.level+1),world.UpgradeCost(b)+"\nThời gian: "+world.UpgradeSeconds(b)+" giây mô phỏng. Cần 1 dân rảnh.\n"+effect+"\nCông trình vẫn hoạt động trong thời gian nâng. Công trình thường không vượt cấp Chánh Điện.\nChánh Điện cấp 2 cần Đệ Tử Xá và Diễn Võ Trường; cấp 3 cần Tàng Kinh Các, 3 đệ tử, 30 danh vọng.","BẮT ĐẦU NÂNG",()=>{world.UpgradeBuilding(id);CloseWorldPanel();RefreshDetails();},"QUAY LẠI",()=>CloseWorldPanel());
        }
        void SectFeedback()
        {string text=world.message;ShowModal("Tông môn",text,"TRỞ VỀ",()=>ShowSectPanel());}
        void ShowSectPanel(int tab=-1)
        {
            if(tab>=0){sectTab=tab;sectPage=0;}
            if(modal!=null)Destroy(modal.gameObject);modalOpen=true;
            modal=Panel("Sect overlay",canvas,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.01f,.04f,.04f,.88f));
            var box=Panel("Sect command",modal,Vector2.one*.5f,Vector2.one*.5f,new Vector2(-590,-355),new Vector2(590,355),ink);
            Heading(box,world.sectName+" · "+world.SectRank,25,16,1090,43,29);
            Label(box,"Linh thạch "+(int)world.spiritStones+"    Linh thảo "+(int)world.herbs+"    Trúc Cơ Đan "+world.pills+"    Pháp khí "+world.artifacts+"    Danh vọng "+(int)world.sectReputation,26,65,1120,30,17,mint);
            string[] tabs={"SƠN MÔN","ĐỆ TỬ","THANH QUY","BIÊN NIÊN"};
            for(int i=0;i<4;i++){int t=i;Button(box,tabs[i],25+i*282,110,271,45,()=>ShowSectPanel(t),sectTab==i);}Rule(box,25,169,1125);
            if(sectTab==0)
            {
                Heading(box,"Khai sơn · Kiến thiết · Truyền đạo",29,189,1050,39,23);
                Label(box,"1. Xây Đệ Tử Xá và Diễn Võ Trường, nâng Chánh Điện lên cấp 2.\n2. Xây Linh Mỏ, Dược Viên; phân công ngoại môn để có linh thạch và linh thảo.\n3. Thu đồ từ người rảnh. Đệ tử tự tu luyện; Tàng Kinh Các và Tụ Linh Trận tăng tốc.\n4. Đạt tầng 9 và đầy tu vi để đột phá. Trúc Cơ Đan tăng 15 điểm % thành công.\n5. Xây Nhiệm Vụ Điện, cử đệ tử săn yêu thú để tăng danh vọng.\n6. Đủ 3 đệ tử, 30 danh vọng và Tàng Kinh Các để nâng Chánh Điện cấp 3.",30,245,735,228,17);
                Label(box,"Bản chuyển thể Unity: ngày 6 phút khi đang chơi, lưu tại thiết bị. Chưa có máy chủ chạy khi đóng game.\nTu luyện mở đến Nguyên Anh; nhiệm vụ tự mô phỏng, quân phòng thủ RTS vẫn giữ để bảo vệ Sơn Môn.",30,502,1090,65,16,mint);
                SectImage(box,"School0",812,206,330,290);Button(box,"XÂY ĐIỆN CÁC",30,586,350,51,()=>{CloseWorldPanel();buildPage=1;ShowBuildMenu();},true);
            }
            if(sectTab==1)
            {
                int maxPage=Mathf.Max(0,(world.disciples.Count-1)/3);sectPage=Mathf.Clamp(sectPage,0,maxPage);
                if(world.disciples.Count==0)Label(box,"Chưa có đệ tử. Nâng Chánh Điện cấp 2 rồi thu đồ bên dưới.\nMỗi lần thu đồ chuyển một người rảnh thành đệ tử, không tạo thêm dân.",35,208,1090,85,20);
                foreach(var pair in world.disciples.Skip(sectPage*3).Take(3).Select((d,i)=>new{d,i}))
                {
                    var d=pair.d;var row=Box("Disciple "+d.id,box,25,184+pair.i*132,1125,122,new Color(.12f,.25f,.24f));
                    SectImage(row,"Portrait"+d.school,10,8,76,106);Heading(row,d.name+" · "+World.Schools[d.school],99,8,470,31,20);
                    string activity=d.wounded>0?"Dưỡng thương "+Mathf.CeilToInt(d.wounded)+"s":d.missionTime>0?"Săn yêu thú "+Mathf.CeilToInt(d.missionTime)+"s":"Tu luyện";
                    Label(row,world.RealmLabel(d)+" · Tư chất "+d.aptitude+" · Tu vi "+(int)d.cultivation+"/"+(30+d.realm*20)+"\n"+activity+" · Tâm ma "+d.demon+" · Lực chiến "+world.DisciplePower(d)+(d.equipped?" · Có pháp khí":""),99,48,470,60,14);
                    int id=d.id;Button(row,"ĐỘT PHÁ",585,13,163,42,()=>ConfirmBreakthrough(id));Button(row,"SĂN YÊU THÚ",762,13,174,42,()=>{world.SendMission(id);SectFeedback();});
                    Button(row,"PHÁP KHÍ",950,13,160,42,()=>{world.EquipDisciple(id);SectFeedback();});Button(row,"ĐIỀU DƯỠNG",585,67,163,41,()=>{world.HealDisciple(id);SectFeedback();});
                    Label(row,"Đột phá: "+Mathf.RoundToInt(world.BreakthroughChance(d,false)*100)+"% · dùng đan +15 điểm %",764,72,344,31,13,mint);
                }
                Label(box,"Thu đồ: 30 thức ăn + 20 linh thạch · còn "+world.Idle+" người rảnh · trang "+(sectPage+1)+"/"+(maxPage+1),29,584,1080,28,14,mint);
                for(int i=0;i<5;i++){int school=i;Button(box,World.Schools[i],27+i*172,623,162,42,()=>{world.RecruitDisciple(school);SectFeedback();},true);}
                Button(box,"‹",894,623,55,42,()=>{sectPage--;ShowSectPanel();});Button(box,"›",957,623,55,42,()=>{sectPage++;ShowSectPanel();});
            }
            if(sectTab==2)
            {
                string[] ids={"balanced","cultivation","prosperity"},names={"Cân bằng nội ngoại","Trọng Dụng Thiên Tài","Quảng Thu Môn Đồ"},desc={"Tu luyện và sản nghiệp ở mức cơ bản.","Tu luyện nhanh hơn 20%, sản nghiệp tu tiên giảm 10%.","Linh Mỏ, Dược Viên, luyện đan/khí nhanh hơn 30%; tu luyện chậm 10%."};
                for(int i=0;i<3;i++){string policy=ids[i];int y=198+i*130;Heading(box,names[i]+(world.sectPolicy==policy?" · ĐANG ÁP DỤNG":""),33,y,1050,35,23);Label(box,desc[i],34,y+42,810,42,17);Button(box,"ÁP DỤNG",920,y+25,217,49,()=>{world.SetPolicy(policy);ShowSectPanel();},true);}
            }
            if(sectTab==3)
            {
                int maxPage=Mathf.Max(0,(world.chronicle.Count-1)/8);sectPage=Mathf.Clamp(sectPage,0,maxPage);
                int i=0;foreach(var e in world.chronicle.AsEnumerable().Reverse().Skip(sectPage*8).Take(8)){Label(box,"Ngày "+e.day+" · "+e.text,32,190+i*49,1105,47,17);i++;}
                Button(box,"MỚI HƠN",29,623,190,42,()=>{sectPage--;ShowSectPanel();});Button(box,"CŨ HƠN",231,623,190,42,()=>{sectPage++;ShowSectPanel();});
            }
            Button(box,"VỀ SƠN MÔN",1019,668,132,30,CloseWorldPanel);
        }
        void ConfirmBreakthrough(int id)
        {
            var d=world.disciples.Find(v=>v.id==id);if(d==null)return;
            ShowModal("Đột phá · "+d.name,"Cần đạt đỉnh cảnh giới và đầy tu vi, "+(30*(d.realm+1))+" linh thạch.\nTỷ lệ thường "+(int)(world.BreakthroughChance(d,false)*100)+"%; dùng Trúc Cơ Đan "+(int)(world.BreakthroughChance(d,true)*100)+"%.\nThất bại: mất chi phí và tu vi tích lũy, tâm ma +2, dưỡng thương 90 giây.\nChọn một cách đột phá bên dưới.","DÙNG ĐAN",()=>{world.Breakthrough(id,true);SectFeedback();},"KHÔNG DÙNG ĐAN",()=>{world.Breakthrough(id,false);SectFeedback();},"QUAY LẠI",()=>ShowSectPanel());
        }
    }
}

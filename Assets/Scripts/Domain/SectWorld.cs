using System;
using System.Linq;
using System.Collections.Generic;

namespace LangTroTan.Domain
{
    [Serializable] public class Disciple
    {
        public int id,school,realm,layer=1,aptitude=60,demon;
        public string name,activity="Tu luyện";
        public float cultivation,missionTime,wounded;
        public bool equipped;
    }
    [Serializable] public class ChronicleEntry { public int day;public string text; }
    public partial class World
    {
        public int sectVersion;
        public float spiritStones,herbs,sectReputation;
        public int pills,artifacts;
        public string sectName="Thanh Vân Tông",sectPolicy="balanced";
        public List<Disciple> disciples=new List<Disciple>();
        public List<ChronicleEntry> chronicle=new List<ChronicleEntry>();
        public static readonly string[] Schools={"Kiếm Tu","Đạo Giáo","Phật Giáo","Nho Gia","Võ Gia"};
        public static readonly string[] Realms={"Luyện Khí","Trúc Cơ","Kết Đan","Nguyên Anh","Hóa Thần","Độ Kiếp","Luyện Hư","Hợp Thể","Đại Thừa","Chân Tiên","Kim Tiên","Đại La Kim Tiên"};
        public string SectRank {get{return level==1?"Bất Nhập Lưu":level==2?"Tam Lưu Tông Môn":"Nhị Lưu Tông Môn";}}
        public static bool IsSectBuilding(string type){return new[]{"scripture","meditation","spiritmine","herbgarden","alchemy","forge","mission"}.Contains(type);}
        public int WorkSlots(Building b){int n=Catalog.Get(b.type).slots;return n==0?0:n+b.level-1;}
        public int MaxHp(Building b){return Catalog.Get(b.type).hp+(b.type=="hall"?500:Catalog.Get(b.type).hp/2)*(b.level-1);}
        public float ProductionMultiplier(Building b){return (float)Math.Pow(1.2,b.level-1);}
        public int UpgradeWood(Building b){return b.type=="hall"?120*b.level:Math.Max(20,Catalog.Get(b.type).wood)*b.level;}
        public int UpgradeStone(Building b){return b.type=="hall"?80*b.level:Math.Max(10,Catalog.Get(b.type).stone)*b.level;}
        public int UpgradeSpirit(Building b){return IsSectBuilding(b.type)?30*b.level:b.type=="hall"&&b.level==2?120:0;}
        public int UpgradeSeconds(Building b){return (b.type=="hall"?60:30)*b.level;}
        public string UpgradeCost(Building b){return UpgradeWood(b)+" gỗ · "+UpgradeStone(b)+" đá"+(UpgradeSpirit(b)>0?" · "+UpgradeSpirit(b)+" linh thạch":"");}
        public void InitializeSect()
        {
            if(sectVersion!=0)return;
            sectVersion=1;spiritStones=80;herbs=15;sectReputation=0;pills=0;artifacts=0;
            disciples=new List<Disciple>();chronicle=new List<ChronicleEntry>();
            if(string.IsNullOrEmpty(sectName))sectName="Thanh Vân Tông";
            sectPolicy="balanced";Record("Lập Thanh Vân Tông · Chưởng Môn bắt đầu khai sơn.");
        }
        public void Record(string text)
        {
            chronicle.Add(new ChronicleEntry{day=day,text=text});if(chronicle.Count>120)chronicle.RemoveAt(0);message=text;
        }
        public bool UpgradeBuilding(int id)
        {
            var b=buildings.Find(v=>v.uid==id);if(ended||b==null||!b.complete)return false;
            if(b.upgrading)return Fail("Công trình đang nâng cấp");if(b.level>=3)return Fail("Đã đạt cấp 3 của bản thử nghiệm");
            if(Idle<1)return Fail("Cần một dân rảnh phụ trách nâng cấp");
            if(b.type!="hall"&&b.level>=level)return Fail("Cần nâng Chánh Điện trước");
            if(b.type=="hall"&&b.level==1&&(!buildings.Any(v=>v.type=="house"&&v.complete)||!buildings.Any(v=>v.type=="barracks"&&v.complete)))return Fail("Cần Đệ Tử Xá và Diễn Võ Trường đã hoàn thành");
            if(b.type=="hall"&&b.level==2&&(!buildings.Any(v=>v.type=="scripture"&&v.complete)||disciples.Count<3||sectReputation<30))return Fail("Cần Tàng Kinh Các, 3 đệ tử và 30 danh vọng");
            if(wood<UpgradeWood(b)||stone<UpgradeStone(b)||spiritStones<UpgradeSpirit(b))return Fail("Nâng cấp cần "+UpgradeCost(b));
            wood-=UpgradeWood(b);stone-=UpgradeStone(b);spiritStones-=UpgradeSpirit(b);b.upgrading=true;b.upgradeProgress=0;
            Record("Bắt đầu nâng "+Catalog.Get(b.type).name+" lên cấp "+(b.level+1));return true;
        }
        public bool CancelUpgrade(int id)
        {
            var b=buildings.Find(v=>v.uid==id);if(ended||b==null||!b.upgrading)return false;
            wood=Add(wood,UpgradeWood(b)*.8f);stone=Add(stone,UpgradeStone(b)*.8f);spiritStones+=UpgradeSpirit(b)*.8f;b.upgrading=false;b.upgradeProgress=0;
            Record("Hủy nâng cấp "+Catalog.Get(b.type).name+" · hoàn 80% vật tư");return true;
        }
        public bool RecruitDisciple(int school)
        {
            if(ended||school<0||school>=Schools.Length)return false;
            if(level<2)return Fail("Cần Chánh Điện cấp 2 để thu đồ");if(Idle<1)return Fail("Cần một dân rảnh xin nhập môn");
            if(disciples.Count>=Math.Min(12,level*4))return Fail("Đã đủ ghế đệ tử; cần thăng cấp tông môn");
            if(food<30||spiritStones<20)return Fail("Thu đồ cần 30 thức ăn và 20 linh thạch");
            food-=30;spiritStones-=20;int id=uid++;
            var d=new Disciple{id=id,name=new[]{"Lâm","Trần","Diệp","Mộ","Tô"}[id%5]+" "+new[]{"Thanh Vân","Thiên Minh","Ngọc Dao","Vô Trần","Tử Y"}[(id/5)%5],school=school,aptitude=45+(int)(NextRandom()%51)};
            disciples.Add(d);Record(d.name+" bái nhập "+sectName+" · "+Schools[school]);return true;
        }
        public bool SetPolicy(string policy)
        {
            if(ended||!new[]{"balanced","cultivation","prosperity"}.Contains(policy))return false;
            sectPolicy=policy;Record("Thanh quy: "+(policy=="cultivation"?"Chuyên tâm tu luyện":policy=="prosperity"?"Phát triển sản nghiệp":"Cân bằng nội ngoại"));return true;
        }
        public int RealmLayers(Disciple d){return d.realm==0?9:4;}
        public string RealmLabel(Disciple d){return Realms[d.realm]+(d.realm==0?" tầng "+d.layer:" · "+new[]{"Sơ Kỳ","Trung Kỳ","Hậu Kỳ","Đỉnh Phong"}[d.layer-1]);}
        public float BreakthroughChance(Disciple d,bool usePill)
        {return Math.Max(0,Math.Min(1,Math.Max(.20f,.90f-.05f*(d.realm+1))-d.demon*.01f+(usePill?.15f:0)));}
        public bool Breakthrough(int id,bool usePill)
        {
            var d=disciples.Find(v=>v.id==id);if(ended||d==null)return false;
            if(d.missionTime>0||d.wounded>0)return Fail("Đệ tử đang làm nhiệm vụ hoặc dưỡng thương");
            if(d.realm>=(level<3?1:3))return Fail("Cần thăng tông môn; bản thử nghiệm mở đến Nguyên Anh");
            if(d.layer<RealmLayers(d)||d.cultivation<30+d.realm*20)return Fail("Cần đạt đỉnh cảnh giới và tích đầy tu vi");
            int cost=30*(d.realm+1);if(spiritStones<cost||usePill&&pills<1)return Fail("Thiếu linh thạch hoặc Trúc Cơ Đan");
            spiritStones-=cost;if(usePill)pills--;
            bool success=Random01()<BreakthroughChance(d,usePill);d.cultivation=0;
            if(success){d.realm++;d.layer=1;d.demon=Math.Max(0,d.demon-1);sectReputation+=10;Record(d.name+" đột phá "+Realms[d.realm]);}
            else {d.demon+=2;d.wounded=90;Record(d.name+" đột phá thất bại · tâm ma +2, dưỡng thương 90 giây");}return true;
        }
        public bool HealDisciple(int id)
        {
            var d=disciples.Find(v=>v.id==id);if(ended||d==null||d.wounded<=0&&d.demon==0)return false;
            if(herbs<10||spiritStones<10)return Fail("Điều dưỡng cần 10 linh thảo và 10 linh thạch");
            herbs-=10;spiritStones-=10;d.wounded=0;d.demon=Math.Max(0,d.demon-2);Record("Đã điều dưỡng "+d.name);return true;
        }
        public bool EquipDisciple(int id)
        {
            var d=disciples.Find(v=>v.id==id);if(ended||d==null||d.equipped||artifacts<1)return Fail("Cần một pháp khí chưa trang bị");
            artifacts--;d.equipped=true;Record(d.name+" nhận pháp khí · tăng sức mạnh nhiệm vụ");return true;
        }
        public int DisciplePower(Disciple d){return 20+d.realm*40+d.layer*4+d.aptitude/5+(d.equipped?35:0)-d.demon*2;}
        public bool SendMission(int id)
        {
            var d=disciples.Find(v=>v.id==id);if(ended||d==null)return false;
            if(!buildings.Any(b=>b.type=="mission"&&b.complete))return Fail("Cần Nhiệm Vụ Điện");
            if(d.missionTime>0||d.wounded>0)return Fail("Đệ tử chưa sẵn sàng");
            if(food<20||spiritStones<5)return Fail("Nhiệm vụ cần 20 thức ăn và 5 linh thạch");
            food-=20;spiritStones-=5;d.missionTime=180;d.activity="Săn yêu thú";Record(d.name+" nhận nhiệm vụ săn yêu thú · 180 giây");return true;
        }
        void TickSect(float dt)
        {
            foreach(var b in buildings.Where(v=>v.upgrading))
            {
                b.upgradeProgress+=dt;if(b.upgradeProgress<UpgradeSeconds(b))continue;
                int old=MaxHp(b);b.level++;b.hp+=MaxHp(b)-old;b.upgrading=false;b.upgradeProgress=0;if(b.type=="hall")level=b.level;
                Record(Catalog.Get(b.type).name+" hoàn thành cấp "+b.level);
            }
            foreach(var b in buildings.Where(v=>v.complete&&v.workers>0))
            {
                float work=b.workers*ProductionMultiplier(b)*dt*(sectPolicy=="prosperity"?1.3f:sectPolicy=="cultivation"?.9f:1);
                if(b.type=="spiritmine")spiritStones=Math.Min(1000000,spiritStones+work*.25f);
                if(b.type=="herbgarden")herbs=Math.Min(1000000,herbs+work*.12f*FarmGrowthFactor);
                if(b.type!="alchemy"&&b.type!="forge")continue;
                bool alchemy=b.type=="alchemy";if(spiritStones<(alchemy?15:20)||alchemy&&herbs<8||!alchemy&&stone<12)continue;
                if(alchemy&&pills>=999||!alchemy&&artifacts>=999)continue;
                b.craftProgress+=work;if(b.craftProgress<60)continue;b.craftProgress-=60;
                spiritStones-=alchemy?15:20;if(alchemy){herbs-=8;pills++;}else{stone-=12;artifacts++;}
            }
            float boost=1+buildings.Where(b=>b.complete&&b.type=="meditation").Sum(b=>.2f*b.level)+buildings.Where(b=>b.complete&&b.type=="scripture").Sum(b=>.1f*b.workers*ProductionMultiplier(b));
            boost=Math.Min(3,boost)*(sectPolicy=="cultivation"?1.2f:sectPolicy=="prosperity"?.9f:1);
            foreach(var d in disciples)
            {
                if(d.wounded>0){d.wounded=Math.Max(0,d.wounded-dt);continue;}
                if(d.missionTime>0)
                {
                    d.missionTime=Math.Max(0,d.missionTime-dt);if(d.missionTime>0)continue;
                    bool win=Random01()<Math.Min(.95f,.35f+DisciplePower(d)*.005f);
                    if(win){spiritStones+=35;herbs+=12;sectReputation+=12;Record(d.name+" hoàn thành săn yêu thú · +35 linh thạch, +12 linh thảo, +12 danh vọng");}
                    else {d.wounded=120;sectReputation+=2;Record(d.name+" rút lui khỏi yêu thú · dưỡng thương 120 giây");}d.activity="Tu luyện";continue;
                }
                d.cultivation=Math.Min(30+d.realm*20,d.cultivation+dt*.35f*(d.aptitude/60f)*boost);
                if(d.layer<RealmLayers(d)&&d.cultivation>=30+d.realm*20){d.cultivation=0;d.layer++;Record(d.name+" đạt "+Realms[d.realm]+" tầng "+d.layer);}
            }
        }
        public void ValidateSect()
        {
            if(sectVersion!=1||disciples==null||chronicle==null||disciples.Count>12||chronicle.Count>120||level<1||level>3)throw new InvalidOperationException("Sai trạng thái tông môn");
            foreach(float v in new[]{spiritStones,herbs,sectReputation})if(float.IsNaN(v)||float.IsInfinity(v)||v<0)throw new InvalidOperationException("Sai tài nguyên tu tiên");
            if(pills<0||pills>999||artifacts<0||artifacts>999||!new[]{"balanced","cultivation","prosperity"}.Contains(sectPolicy))throw new InvalidOperationException("Sai bảo khố hoặc thanh quy");
            var ids=new HashSet<int>(buildings.Select(b=>b.uid).Concat(units.Select(u=>u.uid)).Concat(resources.Select(r=>r.id)).Concat(tasks.Select(t=>t.id)).Concat(villages.Select(v=>v.id)));
            foreach(var d in disciples)if(!ids.Add(d.id)||d.id>=uid||d.realm<0||d.realm>3||d.layer<1||d.layer>RealmLayers(d)||d.aptitude<45||d.aptitude>95||float.IsNaN(d.cultivation)||float.IsInfinity(d.cultivation)||d.cultivation<0||d.cultivation>30+d.realm*20+.01f||d.missionTime<0||d.wounded<0||d.school<0||d.school>=5)throw new InvalidOperationException("Sai đệ tử");
            foreach(var b in buildings)if(b.level<1||b.level>3||b.upgrading&&!b.complete||b.upgrading&&b.level==3||b.upgradeProgress<0||float.IsNaN(b.upgradeProgress)||float.IsInfinity(b.upgradeProgress))throw new InvalidOperationException("Sai nâng cấp công trình");
        }
    }
}

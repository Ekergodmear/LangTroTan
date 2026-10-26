using System;
using System.Collections.Generic;
using System.Linq;

namespace LangTroTan.Domain
{
    [Serializable] public class BuildingDef
    {
        public string id, name, model;
        public int width, depth, wood, stone, hp, seconds, slots;
        public BuildingDef(string id, string name, int w,int d,int wood,int stone,int hp,int seconds,int slots=0,string model="")
        { this.id=id;this.name=name; width=w;depth=d;this.wood=wood;this.stone=stone;this.hp=hp;this.seconds=seconds;this.slots=slots;this.model=model; }
    }
    public static class Catalog
    {
        public static readonly BuildingDef[] Buildings={
            new BuildingDef("hall","Chánh Điện",4,4,0,0,2000,0,0,"TownCenter_FirstAge_Level1"),
            new BuildingDef("house","Đệ Tử Xá",2,2,40,10,350,25,0,"House_FirstAge_Level1"),
            new BuildingDef("store","Kho",2,2,50,20,500,30,0,"Storage_FirstAge_Level1"),
            new BuildingDef("wood","Trạm gỗ",2,2,30,0,300,20,3,"Lumbermill_FirstAge_Level1"),
            new BuildingDef("stone","Mỏ đá",2,2,40,10,350,25,3,"MiningCamp_FirstAge_Level1"),
            new BuildingDef("farm","Linh Điền",3,3,45,0,300,25,3,"Farm_FirstAge_Level1"),
            new BuildingDef("barracks","Diễn Võ Trường",3,3,70,30,700,40,0,"Barracks_FirstAge_Level1"),
            new BuildingDef("wall","Tường",1,1,10,5,450,8),
            new BuildingDef("gate","Cổng",2,1,25,10,650,15),
            new BuildingDef("tower","Tháp canh",2,2,60,40,600,35,0,"Tower_FirstAge_Level1"),
            new BuildingDef("hunter","Lều thợ săn",2,2,40,10,350,25,3),
            new BuildingDef("forester","Vườn ươm",2,2,35,10,350,25,2),
            new BuildingDef("well","Giếng nước",2,2,35,20,500,30),
            new BuildingDef("hearth","Nhà sưởi",2,2,45,20,500,30),
            new BuildingDef("scripture","Tàng Kinh Các",3,3,90,50,800,45,2),
            new BuildingDef("meditation","Tụ Linh Trận",3,3,70,65,650,40),
            new BuildingDef("spiritmine","Linh Mỏ",2,2,65,45,550,35,3),
            new BuildingDef("herbgarden","Dược Viên",3,3,55,25,450,30,3),
            new BuildingDef("alchemy","Luyện Đan Đường",3,3,85,55,750,45,2),
            new BuildingDef("forge","Luyện Khí Điện",3,3,95,65,850,50,2),
            new BuildingDef("mission","Nhiệm Vụ Điện",2,2,65,35,600,35)
        };
        public static BuildingDef Get(string id) { return Array.Find(Buildings,b=>b.id==id); }
        public static string[] UnitNames={"Lính giáo","Cung thủ","Khiên binh"};
        public static int[] UnitFood={20,30,40}, UnitTime={20,25,30}, UnitHp={110,65,180};
    }
    [Serializable] public struct Cell {public int x,z; public Cell(int x,int z){this.x=x;this.z=z;} }
    [Serializable] public class Building
    {
        public int uid,x,z,rotation,hp,workers,level=1;
        public string type;
        public float progress, cooldown,upgradeProgress,craftProgress; public bool upgrading;
        public bool complete;
        public string crop="wheat";public bool planted=true,autoSow=true;public float cropProgress,moisture=65;public int harvests;
        public int W { get {var d=Catalog.Get(type);return rotation%2==0?d.width:d.depth;} }
        public int D { get {var d=Catalog.Get(type);return rotation%2==0?d.depth:d.width;} }
    }
    [Serializable] public class Training {public int building,kind; public float progress;}
    [Serializable] public class Unit
    {
        public int uid,kind,hp,squad,villageId,attackVillageId;
        public bool enemy,hold;
        public float x,z,goalX,goalZ,cooldown,repath;
        public List<Cell> path=new List<Cell>();
        public int pathIndex;
    }
    [Serializable] public partial class World
    {
        public int schema=2,uid=1,day=1,people=8,ticks,level=1,waveSpawned,finalWave=0;
        public float wood=120,stone=60,food=120,morale=70,clock,eatClock,moraleClock,zeroClock;
        public bool ended,won;
        public string message="Bắt đầu với Trạm gỗ và Nông trại. Giữ đủ dân rảnh để xây.";
        public List<Building> buildings=new List<Building>();
        public List<Training> queue=new List<Training>();
        public List<Unit> units=new List<Unit>();
        public float foodRate {get{return FoodProductionEstimate-Consumption;}}
        public int Soldiers {get{return units.Count(u=>!u.enemy);}}
        public int Idle {get{return people-Soldiers-queue.Count-buildings.Sum(b=>b.workers)-buildings.Count(b=>!b.complete)-tasks.Count-disciples.Count-buildings.Count(b=>b.upgrading);}}
        public int Housing {get{return Math.Min(MaxPeople,8+buildings.Where(b=>b.type=="house"&&b.complete).Sum(b=>4*b.level));}}
        public int Capacity {get{return 300+buildings.Where(b=>b.type=="store"&&b.complete).Sum(b=>300*b.level);}}
        public bool Night {get{return clock>=DaylightSeconds;}}
        public int Enemies {get{return units.Count(u=>HostileToPlayer(u));}}
        public World(){}
        public static World New()
        {
            var w=new World(); w.buildings.Add(new Building{uid=w.uid++,type="hall",x=-2,z=-2,hp=2000,complete=true});
            w.buildings.Add(new Building{uid=w.uid++,type="store",x=4,z=-2,hp=500,complete=true});w.InitializeLivingWorld();w.InitializeSect();return w;
        }
        public bool Fail(string text){message=text;return false;}
        float Rate(string type){return buildings.Where(b=>b.type==type&&b.complete).Sum(b=>b.workers)*(morale<30?.75f:1);}
        public bool Blocked(int x,int z,bool enemy=false)
        {return x< -48||x>=48||z< -48||z>=48||buildings.Any(b=>(b.type!="gate"||enemy)&&x>=b.x&&x<b.x+b.W&&z>=b.z&&z<b.z+b.D);}
        public bool CanPlace(string type,int x,int z,int rotation,out string reason)
        {
            reason=""; var d=Catalog.Get(type);if(d==null||type=="hall"){reason="Công trình không hợp lệ";return false;}
            if(ended){reason="Lượt chơi đã kết thúc";return false;}
            if(type=="tower"&&level<2){reason="Cần Nhà chính cấp 2";return false;}
            if(buildings.Count>=80 || (type=="tower"&&buildings.Count(b=>b.type==type)>=8)){reason="Đã đạt giới hạn công trình";return false;}
            if(Idle<=0){reason="Cần một dân rảnh";return false;}
            if(resources.Any(n=>n.amount>0&&n.x>=x&&n.x<x+(rotation%2==0?d.width:d.depth)&&n.z>=z&&n.z<z+(rotation%2==0?d.depth:d.width))){reason="Cần khai thác hết nguồn tài nguyên trên ô đất";return false;}
            if(IsSectBuilding(type)&&level<2){reason="Cần Chánh Điện cấp 2 để lập điện tu tiên";return false;}
            if(wood<d.wood||stone<d.stone){reason="Không đủ gỗ hoặc đá";return false;}
            int width=rotation%2==0?d.width:d.depth,depth=rotation%2==0?d.depth:d.width;
            for(int a=x;a<x+width;a++) for(int b=z;b<z+depth;b++)
            {
                if(a*a+b*b>24*24){reason="Ngoài vùng xây dựng";return false;}
                if(buildings.Any(v=>a>=v.x&&a<v.x+v.W&&b>=v.z&&b<v.z+v.D)){reason="Ô đất đã có công trình";return false;}
                if(units.Any(v=>(int)Math.Floor(v.x)==a&&(int)Math.Floor(v.z)==b)){reason="Có quân đang đứng tại đây";return false;}
            }
            // A connected two-cell corridor must remain from the hall's rally area to the region edge.
            var preview=new Building{uid=-1,type=type,x=x,z=z,rotation=rotation};buildings.Add(preview);
            bool exits=HasExit(); buildings.Remove(preview);
            if(!exits){reason="Cần giữ một lối đi rộng 2 ô cho làng";return false;}
            return true;
        }
        bool HasExit()
        {
            var q=new Queue<Cell>();var visited=new HashSet<int>();q.Enqueue(new Cell(-1,-4));
            int[] dx={1,-1,0,0},dz={0,0,1,-1};
            while(q.Count>0){var c=q.Dequeue();int key=(c.x+48)*96+c.z+48;if(!visited.Add(key))continue;
                if(Blocked(c.x,c.z)||Blocked(c.x+1,c.z)||Blocked(c.x,c.z+1)||Blocked(c.x+1,c.z+1))continue;
                if(Math.Abs(c.x)>=23||Math.Abs(c.z)>=23)return true;
                for(int i=0;i<4;i++)if(Math.Abs(c.x+dx[i])<25&&Math.Abs(c.z+dz[i])<25)q.Enqueue(new Cell(c.x+dx[i],c.z+dz[i]));
            }return false;
        }
        public bool Place(string type,int x,int z,int rotation=0)
        {
            if(!CanPlace(type,x,z,rotation,out var why))return Fail(why);var d=Catalog.Get(type);
            wood-=d.wood;stone-=d.stone;buildings.Add(new Building{uid=uid++,type=type,x=x,z=z,rotation=rotation,hp=d.hp/5});
            foreach(var u in units){u.path.Clear();u.repath=0;}
            message="Đang xây "+d.name+" · "+d.seconds+" giây";return true;
        }
        public bool Assign(int id,int delta)
        {
            var b=buildings.Find(v=>v.uid==id);if(b==null||!b.complete||ended)return false;
            if(delta>0&&(Idle<=0||b.workers>=WorkSlots(b)))return Fail("Hết dân rảnh hoặc đã đủ chỗ làm");
            if(delta<0&&b.workers==0)return false;b.workers+=delta;return true;
        }
        public bool Demolish(int id)
        {
            var b=buildings.Find(v=>v.uid==id);if(b==null||b.type=="hall"||ended)return false;
            var d=Catalog.Get(b.type);float refund=b.complete?.3f:.8f;
            wood=Math.Min(Capacity,wood+(int)(d.wood*refund));stone=Math.Min(Capacity,stone+(int)(d.stone*refund));
            RemoveBuilding(b);message="Đã tháo công trình và hoàn vật tư theo mức quy định";return true;
        }
        void RemoveBuilding(Building b)
        {
            foreach(var t in queue.Where(t=>t.building==b.uid).ToArray()){food=Math.Min(Capacity,food+Catalog.UnitFood[t.kind]);queue.Remove(t);}
            buildings.Remove(b);foreach(var u in units){u.path.Clear();u.repath=0;}
        }
        public bool Upgrade()
        {
            var hall=buildings.Find(b=>b.type=="hall");return hall!=null&&UpgradeBuilding(hall.uid);
        }

        public bool Repair(int id)
        {
            var b=buildings.Find(v=>v.uid==id);if(ended||b==null||!b.complete)return false;
            int max=MaxHp(b),amount=Math.Min(100,max-b.hp);
            if(amount<=0)return Fail("Công trình còn nguyên vẹn");if(wood<10)return Fail("Cần 10 gỗ để sửa");
            wood-=10;b.hp+=amount;message="Đã sửa +"+amount+" HP";return true;
        }
        public bool Train(int id,int kind)
        {
            var b=buildings.Find(v=>v.uid==id);if(ended||b==null||b.type!="barracks"||!b.complete||kind<0||kind>2)return false;
            if(kind>0&&level<2)return Fail("Cần Nhà chính cấp 2");
            if(Idle<=0)return Fail("Cần dân rảnh để huấn luyện");if(Soldiers+queue.Count>=MaxSoldiers)return Fail("Giới hạn 24 lính gồm cả người đang học");
            if(queue.Count(t=>t.building==id)>=4)return Fail("Hàng đợi đã đầy");if(food<Catalog.UnitFood[kind])return Fail("Thiếu lương thực");
            food-=Catalog.UnitFood[kind];queue.Add(new Training{building=id,kind=kind});message="Đã xếp hàng "+Catalog.UnitNames[kind];return true;
        }
        public bool CancelTraining(int id)
        {
            var t=queue.LastOrDefault(v=>v.building==id);if(t==null||ended)return false;
            food=Math.Min(Capacity,food+Catalog.UnitFood[t.kind]);queue.Remove(t);return true;
        }
        public bool Recruit()
        {
            if(ended||day<2)return Fail("Dân mới bắt đầu tới từ ngày 2");
            if(people>=Housing)return Fail("Cần thêm chỗ ở");if(food<15||morale<50)return Fail("Cần 15 thức ăn và tinh thần ít nhất 50");
            if(recruitsToday>=2)return Fail("Hôm nay đã nhận đủ 2 người");people++;recruitsToday++;food-=15;message="Một người sống sót đã gia nhập";return true;
        }
        public int recruitsToday;
        public bool Order(int squad,float x,float z,bool hold=false)
        {
            if(ended)return false;var members=units.Where(u=>!u.enemy&&(squad<0||u.squad==squad)).ToArray();
            if(members.Length==0)return Fail("Tổ đội chưa có lính");
            if(Blocked((int)Math.Floor(x),(int)Math.Floor(z)))return Fail("Điểm đến bị chặn");
            var paths=new List<List<Cell>>();
            foreach(var u in members){var p=FindPath(u.x,u.z,x,z,false);if(p==null)return Fail("Không có đường tới điểm đến");paths.Add(p);}
            for(int i=0;i<members.Length;i++){var u=members[i];u.goalX=x;u.goalZ=z;u.hold=hold;u.path=paths[i];u.pathIndex=0;u.repath=3;}
            message=hold?"Giữ vị trí":"Đã nhận lệnh di chuyển";return true;
        }
        public List<Cell> FindPath(float sx,float sz,float tx,float tz,bool enemy)
        {
            int startX=(int)Math.Floor(sx),startZ=(int)Math.Floor(sz),endX=(int)Math.Floor(tx),endZ=(int)Math.Floor(tz);
            if(Blocked(endX,endZ,enemy))return null;if(startX==endX&&startZ==endZ)return new List<Cell>();
            int Key(int x,int z){return (x+48)*96+z+48;}
            int start=Key(startX,startZ),end=Key(endX,endZ);
            var open=new List<int>{start};var closed=new HashSet<int>();var came=new Dictionary<int,int>();var costs=new Dictionary<int,int>{{start,0}};
            int[] dx={1,-1,0,0},dz={0,0,1,-1};
            while(open.Count>0)
            {
                int best=0,bestF=int.MaxValue;
                for(int i=0;i<open.Count;i++){int k=open[i],f=costs[k]+Math.Abs(k/96-48-endX)+Math.Abs(k%96-48-endZ);if(f<bestF){best=i;bestF=f;}}
                int current=open[best];open.RemoveAt(best);if(!closed.Add(current))continue;
                if(current==end){var path=new List<Cell>();while(current!=start){path.Add(new Cell(current/96-48,current%96-48));current=came[current];}path.Reverse();return path;}
                int cx=current/96-48,cz=current%96-48;
                for(int i=0;i<4;i++){int x=cx+dx[i],z=cz+dz[i];if(Blocked(x,z,enemy))continue;int n=Key(x,z);if(closed.Contains(n))continue;
                    int g=costs[current]+1;if(!costs.ContainsKey(n)||g<costs[n]){costs[n]=g;came[n]=current;if(!open.Contains(n))open.Add(n);}}
                if(closed.Count>9216)break;
            }return null;
        }
        public void Tick(float dt=.1f)
        {
            if(ended)return;ticks++;AdvanceCalendar(dt);eatClock+=dt;moraleClock+=dt;
            LivingEconomy(dt);TickSect(dt);
            while(eatClock>=10){eatClock-=10;float need=Consumption*10;bool fed=food+.0001f>=need;food=Math.Max(0,food-need);morale=Math.Max(0,Math.Min(100,morale+(fed?1:-4)));}
            if(morale<=0){zeroClock+=dt;if(zeroClock>=60){if(Idle>0)people--;else{var b=buildings.Find(v=>v.workers>0);if(b!=null){b.workers--;people--;}}zeroClock=0;message="Một người đã rời làng vì đói";}}else zeroClock=0;
            foreach(var b in buildings.Where(b=>!b.complete)){var d=Catalog.Get(b.type);b.progress+=dt;b.hp=Math.Min(d.hp,(int)(d.hp*(.2f+.8f*Math.Min(1,b.progress/d.seconds))));if(b.progress>=d.seconds){b.complete=true;b.hp=d.hp;message=d.name+" đã xây xong";}}
            foreach(var b in buildings.Where(b=>b.type=="barracks"&&b.complete))
            {
                var t=queue.Find(v=>v.building==b.uid);if(t==null)continue;t.progress+=dt*ProductionMultiplier(b);
                if(t.progress>=Catalog.UnitTime[t.kind]){int squad=Enumerable.Range(0,3).First(s=>units.Count(u=>!u.enemy&&u.squad==s)<SquadSize);
                    var c=NearestFree(b.x+b.W/2f,b.z-1.5f,false);units.Add(new Unit{uid=uid++,kind=t.kind,hp=Catalog.UnitHp[t.kind],x=c.x+.5f,z=c.z+.5f,goalX=c.x+.5f,goalZ=c.z+.5f,squad=squad});queue.Remove(t);message=Catalog.UnitNames[t.kind]+" sẵn sàng";}
            }
            if(Night&&waveSpawned<day){waveSpawned=day;if(day==1)SpawnWave(1);else if(day%3==0)SpawnWildlife();}
            
            Combat(dt);TickNeighbors(dt);
            var hall=buildings.Find(b=>b.type=="hall");
            if(hall==null||hall.hp<=0||people<=0){ended=true;won=false;message="Làng đã thất thủ. Hãy thử lại với nhiều quân hơn.";}
            
        }
        float Add(float current,float amount){return current>=Capacity?current:Math.Min(Capacity,current+amount);}
        Cell NearestFree(float x,float z,bool enemy)
        {
            int cx=(int)Math.Floor(x),cz=(int)Math.Floor(z);
            for(int r=0;r<12;r++)for(int a=-r;a<=r;a++)for(int b=-r;b<=r;b++)if(!Blocked(cx+a,cz+b,enemy))return new Cell(cx+a,cz+b);
            return new Cell(0,-5);
        }
        public void SpawnWave(int n)
        {
            int[] beasts={4,4,4,4,6,6,8},raiders={0,2,4,6,8,10,12},breakers={0,0,1,2,2,3,4};
            int at=Math.Max(0,Math.Min(6,n-1));
            for(int kind=0;kind<3;kind++){int count=kind==0?beasts[at]:kind==1?raiders[at]:breakers[at];
                for(int i=0;i<count;i++){if(Enemies>=60)break;float x=(i%5-2)*1.4f,z=27+i/5;
                    if(n%2==0){float tmp=x;x=z;z=tmp;}var c=NearestFree(x,z,true);
                    units.Add(new Unit{uid=uid++,enemy=true,kind=kind,hp=kind==0?60:kind==1?100:220,x=c.x+.5f,z=c.z+.5f,goalX=0,goalZ=-3.5f});}}
            message="Tập kích đêm "+n+"! Tập hợp quân bảo vệ Nhà chính.";
        }
        void Combat(float dt)
        {
            foreach(var b in buildings.Where(b=>b.type=="tower"&&b.complete)){b.cooldown-=dt;if(b.cooldown<=0){var target=units.Where(u=>HostileToPlayer(u)&&u.hp>0&&Dist(u.x,u.z,b.x+b.W*.5f,b.z+b.D*.5f)<7).OrderBy(u=>Dist(u.x,u.z,b.x,b.z)).FirstOrDefault();if(target!=null){target.hp-=14+4*(b.level-1);b.cooldown=1.5f;}}}
            foreach(var u in units.OrderBy(u=>u.uid).ToArray())
            {
                if(u.hp<=0)continue;u.cooldown-=dt;u.repath-=dt;
                var target=units.Where(v=>Hostile(u,v)&&v.hp>0).OrderBy(v=>Dist(u.x,u.z,v.x,v.z)).FirstOrDefault();
                float range=!u.enemy&&u.kind==1?6:1.2f;
                if(target!=null&&Dist(u.x,u.z,target.x,target.z)<=range)
                {if(u.cooldown<=0){int damage=u.enemy?(u.kind==0?7:u.kind==1?10:16):(u.kind==0?12:u.kind==1?10:8);float modifier=(!u.enemy&&u.kind==0&&target.enemy&&target.kind==0&&target.villageId<=0)?1.25f:1;
                        if(!target.enemy&&target.kind==2)modifier*=.75f;target.hp-=Math.Max(1,(int)(damage*modifier));u.cooldown=u.kind==0?1.2f:u.kind==1?1.4f:1.5f;}continue;}
                if(AttackVillage(u,dt))continue;
                if(u.enemy)
                {
                    var b=HostileToPlayer(u)?buildings.OrderBy(v=>DistanceToBuilding(u,v)).FirstOrDefault():null;
                    if(b!=null&&DistanceToBuilding(u,b)<1.2f){if(u.cooldown<=0){b.hp-=u.kind==2?32:u.kind==0?7:10;u.cooldown=u.kind==2?2:1.3f;}continue;}
                    if(target!=null&&Dist(u.x,u.z,target.x,target.z)<5){u.goalX=target.x;u.goalZ=target.z;}else if(u.attackVillageId>0){var town=villages.Find(v=>v.id==u.attackVillageId&&!v.defeated);if(town!=null){u.goalX=town.x;u.goalZ=town.z;}}else{u.goalX=0;u.goalZ=-3.5f;}
                }
                if(!u.hold)Move(u,dt);
            }
            foreach(var dead in units.Where(u=>u.hp<=0).ToArray()){if(!dead.enemy)people--;units.Remove(dead);}
            foreach(var b in buildings.Where(b=>b.hp<=0).ToArray())RemoveBuilding(b);
        }
        static float DistanceToBuilding(Unit u,Building b){float x=Math.Max(b.x,Math.Min(b.x+b.W,u.x)),z=Math.Max(b.z,Math.Min(b.z+b.D,u.z));return Dist(u.x,u.z,x,z);}
        public static float Dist(float x,float z,float a,float b){return (float)Math.Sqrt((x-a)*(x-a)+(z-b)*(z-b));}
        void Move(Unit u,float dt)
        {
            if(Dist(u.x,u.z,u.goalX,u.goalZ)<.2f)return;
            if(u.repath<=0||u.path==null||u.pathIndex>=u.path.Count)
            {u.path=FindPath(u.x,u.z,u.goalX,u.goalZ,u.enemy)??new List<Cell>();u.pathIndex=0;u.repath=3;}
            if(u.pathIndex>=u.path.Count)return;var cell=u.path[u.pathIndex];
            if(Blocked(cell.x,cell.z,u.enemy)){u.path.Clear();u.repath=0;return;}
            float x=cell.x+.5f,z=cell.z+.5f,d=Dist(u.x,u.z,x,z),speed=u.enemy?(u.kind==0?2:u.kind==1?1.6f:1.2f):(u.kind==2?1.34f:1.67f);
            if(d<speed*dt){u.x=x;u.z=z;u.pathIndex++;}else{u.x+=(x-u.x)/d*speed*dt;u.z+=(z-u.z)/d*speed*dt;}
        }
        public void Validate()
        {
            if(schema!=2)throw new InvalidOperationException("Phiên bản save chưa được hỗ trợ");
            if(buildings==null||units==null||queue==null||buildings.Count>80||people<0||people>MaxPeople||Idle<0||Soldiers+queue.Count>MaxSoldiers)throw new InvalidOperationException("Sai bất biến dân số");
            if(float.IsNaN(wood)||float.IsInfinity(wood)||float.IsNaN(stone)||float.IsInfinity(stone)||float.IsNaN(food)||float.IsInfinity(food)||wood<0||stone<0||food<0||day<1)throw new InvalidOperationException("Sai dữ liệu tài nguyên hoặc ngày");
            var ids=new HashSet<int>();foreach(var b in buildings){if(Catalog.Get(b.type)==null||!ids.Add(b.uid)||b.workers<0||b.workers>WorkSlots(b))throw new InvalidOperationException("Sai dữ liệu công trình");}
            foreach(var u in units)if(!ids.Add(u.uid)||u.kind<0||u.kind>2||u.hp<=0||!u.enemy&&(u.squad<0||u.squad>2))throw new InvalidOperationException("Sai dữ liệu quân");
            foreach(var t in queue)if(t.kind<0||t.kind>2||!buildings.Any(b=>b.uid==t.building&&b.type=="barracks"))throw new InvalidOperationException("Sai dữ liệu tuyển quân");
            if(!ended&&buildings.Count(b=>b.type=="hall")!=1)throw new InvalidOperationException("Thiếu Nhà chính");
            if(ids.Count>0&&uid<=ids.Max())throw new InvalidOperationException("ID không hợp lệ");
            ValidateLiving();ValidateSect();
        }
    }
}

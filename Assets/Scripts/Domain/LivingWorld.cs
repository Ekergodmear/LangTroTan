using System;
using System.Collections.Generic;
using System.Linq;

namespace LangTroTan.Domain
{
    public enum Season {Spring,Summer,Autumn,Winter}
    public enum Weather {Clear,Cloudy,Rain,Storm,Snow}
    public enum ResourceKind {Tree,Stone,Deer}
    [Serializable] public class ResourceNode
    {
        public int id;public ResourceKind kind;public float x,z,amount,maximum,regrow;
    }
    [Serializable] public class HarvestTask
    {
        public int id,node,phase,pathIndex;public float x,z,progress,carried,repath;public ResourceKind kind;
        public List<Cell> path=new List<Cell>();
    }
    [Serializable] public class NeighborVillage
    {
        public int id,population=8,army=3,buildings=2,relation=10,warTarget=-2,lastRaidDay=-10,warDay;
        public string name,decision="Phát triển làng";
        public float x,z,wood=80,stone=30,food=100,hp=2400,maxHp=2400,aggression,decisionClock,recruitClock,prepare;
        public bool warWithPlayer,defeated;
    }
    public partial class World
    {
        public const int DaysPerSeason=4,MaxPeople=40,MaxSoldiers=24,SquadSize=8;
        public bool endless=true;
        public Weather weather=Weather.Clear;
        public float weatherClock,environmentClock,heatingClock;
        public uint randomState=73013;
        public List<ResourceNode> resources=new List<ResourceNode>();
        public List<HarvestTask> tasks=new List<HarvestTask>();
        public List<NeighborVillage> villages=new List<NeighborVillage>();
        public Season CurrentSeason {get{return (Season)(((day-1)/DaysPerSeason)%4);}}
        public int Year {get{return (day-1)/(DaysPerSeason*4)+1;}}
        public int SeasonDay {get{return (day-1)%DaysPerSeason+1;}}
        public float DaylightSeconds {get{return new[]{240f,270f,220f,180f}[(int)CurrentSeason];}}
        public float Temperature {get{return new[]{16f,29f,12f,-3f}[(int)CurrentSeason]+(Night?-5:0)+(weather==Weather.Rain?-3:weather==Weather.Storm?-5:weather==Weather.Snow?-2:0);}}
        public float Consumption {get{return people*.05f*(CurrentSeason==Season.Winter?1.2f:1);}}
        public float OutsideWork {get{return weather==Weather.Storm?.55f:weather==Weather.Rain?.85f:weather==Weather.Snow?.7f:1;}}
        public float FarmGrowthFactor {get{return new[]{1.1f,1f,.85f,.35f}[(int)CurrentSeason]*(weather==Weather.Rain?1.15f:weather==Weather.Storm?.7f:1);}}
        public float FoodProductionEstimate
        {
            get
            {
                float farms=buildings.Where(b=>b.type=="farm"&&b.complete&&b.workers>0&&b.planted).Sum(b=>(b.crop=="vegetable"?55f/85:80f/120)*b.workers*.6f*FarmGrowthFactor*(b.moisture<20?.4f:1)*(morale<30?.75f:1));
                float hunting=buildings.Where(b=>b.type=="hunter"&&b.complete&&SourceFor(b,ResourceKind.Deer)!=null).Sum(b=>b.workers)*.45f*OutsideWork;
                return farms+hunting;
            }
        }
        uint NextRandom(){if(randomState==0)randomState=73013;randomState^=randomState<<13;randomState^=randomState>>17;randomState^=randomState<<5;return randomState;}
        float Random01(){return (NextRandom()%1000000)/1000000f;}
        public void InitializeLivingWorld()
        {
            resources=new List<ResourceNode>();tasks=new List<HarvestTask>();villages=new List<NeighborVillage>();
            for(int i=0;i<125;i++)
            {
                float a=Random01()*(float)Math.PI*2,r=10+Random01()*30,x=(float)Math.Cos(a)*r,z=(float)Math.Sin(a)*r;
                if(Math.Abs(x)<2||Math.Abs(z+4)<1.5f)continue;
                resources.Add(new ResourceNode{id=uid++,kind=ResourceKind.Tree,x=x,z=z,amount=180,maximum=180});
            }
            foreach(var c in new[]{new Cell(-10,5),new Cell(-12,8),new Cell(-9,10),new Cell(12,7)})resources.Add(new ResourceNode{id=uid++,kind=ResourceKind.Tree,x=c.x+.5f,z=c.z+.5f,amount=180,maximum=180});
            foreach(var c in new[]{new Cell(14,1),new Cell(16,4),new Cell(19,8),new Cell(14,-9),new Cell(-19,-11),new Cell(-27,18),new Cell(28,17),new Cell(27,-20)})resources.Add(new ResourceNode{id=uid++,kind=ResourceKind.Stone,x=c.x+.5f,z=c.z+.5f,amount=900,maximum=900});
            foreach(var c in new[]{new Cell(-13,-12),new Cell(-17,14),new Cell(12,16),new Cell(19,-14),new Cell(-25,5),new Cell(24,2)})resources.Add(new ResourceNode{id=uid++,kind=ResourceKind.Deer,x=c.x+.5f,z=c.z+.5f,amount=90,maximum=90});
            villages.Add(new NeighborVillage{id=uid++,name="Mộc Khê",x=-34,z=27,aggression=.15f,relation=35});
            villages.Add(new NeighborVillage{id=uid++,name="Xích Sơn",x=34,z=27,aggression=.85f,relation=-15});
            villages.Add(new NeighborVillage{id=uid++,name="Bắc Lĩnh",x=33,z=-31,aggression=.45f,relation=5});
            resources.RemoveAll(n=>villages.Any(v=>Dist(n.x,n.z,v.x,v.z)<7));
        }
        public void UpgradeSave()
        {
            InitializeSect();if(schema!=1)return;
            bool oldVictory=ended&&won;schema=2;endless=true;finalWave=0;
            InitializeLivingWorld();
            resources.RemoveAll(n=>buildings.Any(b=>n.x>=b.x&&n.x<b.x+b.W&&n.z>=b.z&&n.z<b.z+b.D));
            foreach(var b in buildings.Where(b=>b.type=="farm")){b.crop="wheat";b.planted=true;b.autoSow=true;b.moisture=65;}
            if(oldVictory){ended=false;won=false;clock=Math.Min(clock,359);}
            message="Đã chuyển làng sang sinh tồn dài hạn với bốn mùa";
        }
        public void AdvanceCalendar(float dt)
        {
            clock+=dt;
            while(clock>=360)
            {
                clock-=360;day++;recruitsToday=0;
                foreach(var v in villages.Where(v=>!v.defeated))v.relation=Math.Max(-100,Math.Min(100,v.relation+(v.warWithPlayer?-1:1)));
                message="Ngày "+day+" · "+SeasonName+", năm "+Year;
            }
            weatherClock+=dt;
            while(weatherClock>=120){weatherClock-=120;RollWeather();}
        }
        void RollWeather()
        {
            float r=Random01();
            if(CurrentSeason==Season.Winter)weather=r<.48f?Weather.Snow:r<.78f?Weather.Cloudy:Weather.Clear;
            else if(CurrentSeason==Season.Summer)weather=r<.62f?Weather.Clear:r<.8f?Weather.Cloudy:r<.94f?Weather.Rain:Weather.Storm;
            else weather=r<.36f?Weather.Clear:r<.59f?Weather.Cloudy:r<.93f?Weather.Rain:Weather.Storm;
        }
        public string SeasonName {get{return new[]{"Xuân","Hạ","Thu","Đông"}[(int)CurrentSeason];}}
        public string WeatherName {get{return new[]{"Trời nắng","Nhiều mây","Mưa","Mưa giông","Tuyết rơi"}[(int)weather];}}
        public ResourceNode SourceFor(Building b,ResourceKind kind)
        {
            return resources.Where(n=>n.kind==kind&&n.amount>0&&Dist(n.x,n.z,b.x+b.W*.5f,b.z+b.D*.5f)<=12).OrderBy(n=>Dist(n.x,n.z,b.x,b.z)).FirstOrDefault();
        }
        float Take(ResourceNode n,float amount)
        {
            if(n==null||amount<=0)return 0;float taken=Math.Min(n.amount,amount);n.amount-=taken;
            if(n.amount<.001f){n.amount=0;n.regrow=n.kind==ResourceKind.Tree?720:n.kind==ResourceKind.Deer?360:0;}
            return taken;
        }
        public bool BeginHarvest(int nodeId)
        {
            var n=resources.Find(v=>v.id==nodeId);if(ended||n==null)return false;
            if(n.amount<=0)return Fail("Nguồn đã cạn; cây và đàn nai cần thời gian phục hồi");
            if(Idle<=0)return Fail("Cần một dân rảnh");if(tasks.Count>=4)return Fail("Tối đa bốn chuyến khai thác thủ công cùng lúc");
            if(tasks.Any(t=>t.node==nodeId))return Fail("Đã có người tới nguồn này");
            var start=NearestFree(0,-4,false);var path=FindPath(start.x+.5f,start.z+.5f,n.x,n.z,false);if(path==null)return Fail("Không có đường tới nguồn này");
            tasks.Add(new HarvestTask{id=uid++,node=n.id,kind=n.kind,x=start.x+.5f,z=start.z+.5f,path=path});
            message=n.kind==ResourceKind.Tree?"Dân đang đi chặt cây":n.kind==ResourceKind.Deer?"Thợ săn đang tìm đàn nai":"Dân đang đi khai thác đá";return true;
        }
        public bool PlantTree(int nodeId)
        {
            var n=resources.Find(v=>v.id==nodeId);if(ended||n==null||n.kind!=ResourceKind.Tree||n.amount>0)return Fail("Chọn một gốc cây đã chặt");
            if(wood<5)return Fail("Cần 5 gỗ cho dụng cụ và cây giống");if(n.regrow>0&&n.regrow<=180)return Fail("Cây giống đang lớn");
            wood-=5;n.regrow=180;message="Đã trồng lại cây · lớn sau 180 giây khi thời tiết thuận lợi";return true;
        }
        public bool CancelHarvest(int nodeId)
        {
            var t=tasks.Find(v=>v.node==nodeId);if(t==null||ended)return false;tasks.Remove(t);message="Đã hủy chuyến khai thác; vật tư chưa về kho bị bỏ lại";return true;
        }
        public bool Sow(int buildingId,string crop,bool autoSow)
        {
            var b=buildings.Find(v=>v.uid==buildingId&&v.type=="farm"&&v.complete);if(ended||b==null)return false;
            if(crop!="wheat"&&crop!="vegetable")return false;if(food<5)return Fail("Cần 5 lương thực làm hạt giống");
            food-=5;b.crop=crop;b.cropProgress=0;b.planted=true;b.autoSow=autoSow;message=crop=="wheat"?"Đã gieo lúa mì":"Đã trồng rau · rau ngừng lớn khi nhiệt độ dưới 0°C";return true;
        }
        void LivingEconomy(float dt)
        {
            foreach(var b in buildings.Where(b=>b.complete&&b.workers>0))
            {
                float workers=b.workers*ProductionMultiplier(b)*(morale<30?.75f:1);
                if(b.type=="wood"&&wood<Capacity)wood=Add(wood,Take(SourceFor(b,ResourceKind.Tree),workers*.8f*OutsideWork*dt));
                if(b.type=="stone"&&stone<Capacity)stone=Add(stone,Take(SourceFor(b,ResourceKind.Stone),workers*.5f*OutsideWork*dt));
                if(b.type=="hunter"&&food<Capacity)food=Add(food,Take(SourceFor(b,ResourceKind.Deer),workers*.45f*OutsideWork*dt));
                if(b.type=="farm")
                {
                    bool irrigated=buildings.Any(v=>v.type=="well"&&v.complete&&Dist(v.x,v.z,b.x,b.z)<9);
                    b.moisture=Math.Max(0,Math.Min(100,b.moisture+dt*(weather==Weather.Rain||weather==Weather.Storm?.8f:CurrentSeason==Season.Summer?-.16f:-.06f)));
                    if(irrigated)b.moisture=Math.Max(55,b.moisture);
                    if(b.planted)
                    {
                        float growth=workers*.6f*FarmGrowthFactor*(b.moisture<20?.4f:1)*(b.crop=="vegetable"&&Temperature<=0?0:1);
                        b.cropProgress+=dt*growth/(b.crop=="vegetable"?85:120);
                        if(b.cropProgress>=1&&food<Capacity)
                        {
                            food=Add(food,b.crop=="vegetable"?55:80);b.cropProgress=0;b.harvests++;b.planted=false;message="Thu hoạch "+(b.crop=="vegetable"?"rau":"lúa mì")+" thành công";
                            if(b.autoSow&&food>=5){food-=5;b.planted=true;}
                        }
                    }
                }
                if(b.type=="forester")foreach(var n in resources.Where(n=>n.kind==ResourceKind.Tree&&n.amount<=0&&n.regrow>0&&Dist(n.x,n.z,b.x,b.z)<12))n.regrow=Math.Max(.01f,n.regrow-dt*workers*2);
            }
            foreach(var n in resources.Where(n=>n.amount<=0&&n.regrow>0))
            {
                float factor=n.kind==ResourceKind.Tree&&CurrentSeason==Season.Winter?.3f:1;n.regrow=Math.Max(0,n.regrow-dt*factor);
                if(n.regrow==0)n.amount=n.maximum;
            }
            foreach(var task in tasks.ToArray())TickTask(task,dt);
            if(CurrentSeason==Season.Winter)
            {
                heatingClock+=dt;while(heatingClock>=10){heatingClock-=10;float required=people*.12f*(buildings.Any(b=>b.type=="hearth"&&b.complete)?.6f:1);
                    if(wood<required)morale=Math.Max(0,morale-1);wood=Math.Max(0,wood-required);}
            }else heatingClock=0;
        }
        void TickTask(HarvestTask t,float dt)
        {
            t.repath=Math.Max(0,t.repath-dt);
            var n=resources.Find(v=>v.id==t.node);
            if(t.phase==1)
            {
                t.progress+=dt*OutsideWork;
                if(t.progress>=(t.kind==ResourceKind.Deer?15:10))
                {
                    t.carried=Take(n,t.kind==ResourceKind.Tree?24:t.kind==ResourceKind.Stone?16:28);t.phase=2;t.pathIndex=0;t.path=FindPath(t.x,t.z,0,-4,false)??new List<Cell>();
                }return;
            }
            if(t.pathIndex<t.path.Count)
            {
                var c=t.path[t.pathIndex];if(Blocked(c.x,c.z)){if(t.repath==0){t.path=FindPath(t.x,t.z,t.phase==0?n.x:0,t.phase==0?n.z:-4,false)??new List<Cell>();t.pathIndex=0;t.repath=3;}return;}
                float x=c.x+.5f,z=c.z+.5f,d=Dist(t.x,t.z,x,z),step=dt*2;
                if(d<=step){t.x=x;t.z=z;t.pathIndex++;}else{t.x+=(x-t.x)/d*step;t.z+=(z-t.z)/d*step;}return;
            }
            if(t.phase==0){if(Dist(t.x,t.z,n.x,n.z)>2){if(t.repath==0){t.path=FindPath(t.x,t.z,n.x,n.z,false)??new List<Cell>();t.pathIndex=0;t.repath=3;}return;}t.phase=1;return;}
            if(Dist(t.x,t.z,0,-4)>2){if(t.repath==0){t.path=FindPath(t.x,t.z,0,-4,false)??new List<Cell>();t.pathIndex=0;t.repath=3;}return;}
            if(t.kind==ResourceKind.Tree)wood=Add(wood,t.carried);else if(t.kind==ResourceKind.Stone)stone=Add(stone,t.carried);else food=Add(food,t.carried);
            tasks.Remove(t);message="Dân đã mang "+(int)t.carried+" "+(t.kind==ResourceKind.Tree?"gỗ":t.kind==ResourceKind.Stone?"đá":"thịt")+" về làng";
        }
        public bool Gift(int villageId)
        {
            var v=villages.Find(n=>n.id==villageId);if(ended||v==null||v.defeated||v.warWithPlayer)return Fail("Không thể tặng quà khi đang chiến tranh");
            if(food<30)return Fail("Quà hữu nghị cần 30 lương thực");food-=30;v.food+=30;v.relation=Math.Min(100,v.relation+18);if(v.relation>=35&&v.prepare>0&&v.warTarget==0){v.prepare=0;v.warTarget=-2;v.decision="Hủy kế hoạch chiến tranh sau ngoại giao";}message="Quan hệ với "+v.name+" được cải thiện";return true;
        }
        public bool Trade(int villageId)
        {
            var v=villages.Find(n=>n.id==villageId);if(ended||v==null||v.defeated||v.warWithPlayer||v.relation<0)return Fail("Cần hòa bình và quan hệ không âm để giao thương");
            if(food<40||v.stone<25)return Fail("Cần 40 lương thực và làng bạn có ít nhất 25 đá");if(stone+25>Capacity)return Fail("Kho không đủ chỗ cho 25 đá");
            food-=40;stone+=25;v.food+=40;v.stone-=25;v.relation=Math.Min(100,v.relation+3);message="Đã đổi 40 lương thực lấy 25 đá từ "+v.name;return true;
        }
        public bool DeclareWar(int villageId)
        {
            var v=villages.Find(n=>n.id==villageId);if(ended||v==null||v.defeated)return false;if(v.warWithPlayer)return Fail("Hai làng đang chiến tranh");
            v.warWithPlayer=true;v.warTarget=0;v.warDay=day;v.relation=-80;v.prepare=0;v.decision="Chiến tranh với làng của bạn";message="Đã tuyên chiến với "+v.name+". Đưa quân tới Nhà chính của họ để tấn công.";return true;
        }
        public bool OfferPeace(int villageId)
        {
            var v=villages.Find(n=>n.id==villageId);if(ended||v==null||!v.warWithPlayer||v.defeated)return false;
            if(food<60||wood<40)return Fail("Đề nghị đình chiến cần 60 lương thực và 40 gỗ");
            food-=60;wood-=40;v.food+=60;v.wood+=40;v.warWithPlayer=false;v.warTarget=-2;v.relation=-10;v.prepare=0;v.decision="Đình chiến và tái thiết";v.lastRaidDay=day;
            foreach(var u in units.Where(u=>u.villageId==v.id&&u.attackVillageId==0).ToArray()){v.army++;units.Remove(u);}
            message="Đã ký đình chiến với "+v.name;return true;
        }
        public bool HostileToPlayer(Unit u){return u.enemy&&(u.villageId<=0||villages.Any(v=>v.id==u.villageId&&v.warWithPlayer&&!v.defeated));}
        bool Hostile(Unit a,Unit b)
        {
            int ta=a.enemy?(a.villageId>0?a.villageId:-1):0,tb=b.enemy?(b.villageId>0?b.villageId:-1):0;
            if(ta==tb)return false;if(ta==-1||tb==-1)return true;
            if(ta==0)return HostileToPlayer(b);if(tb==0)return HostileToPlayer(a);
            return villages.Any(v=>!v.defeated&&((v.id==ta&&v.warTarget==tb)||(v.id==tb&&v.warTarget==ta)));
        }
        void TickNeighbors(float dt)
        {
            foreach(var v in villages.Where(v=>!v.defeated&&v.hp>0))
            {
                float farming=CurrentSeason==Season.Winter?.45f:1;
                v.wood=Math.Min(1000,v.wood+v.population*.035f*OutsideWork*dt);v.stone=Math.Min(600,v.stone+v.population*.014f*dt);
                v.food=Math.Max(0,Math.Min(1500,v.food+(v.population*.075f*farming-v.population*.025f)*dt));
                v.recruitClock+=dt;v.decisionClock+=dt;
                if(v.recruitClock>=35)
                {
                    v.recruitClock-=35;
                    int active=units.Count(u=>u.villageId==v.id);
                    if(v.army+active<12&&v.food>=18&&v.wood>=8){v.army++;v.food-=18;v.wood-=8;}
                }
                if(v.prepare>0)
                {
                    v.prepare=Math.Max(0,v.prepare-dt);
                    if(v.prepare==0){if(v.warTarget==0){v.warWithPlayer=true;v.relation=-70;v.warDay=day;message=v.name+" đã tuyên chiến!";}v.decision="Đang chiến tranh";}
                }
                if(v.decisionClock<45)continue;v.decisionClock-=45;
                if(v.hp<v.maxHp&&v.wood>=10){v.hp=Math.Min(v.maxHp,v.hp+80);v.wood-=10;v.decision="Sửa chữa làng";}
                else if(v.buildings<8&&v.wood>=65&&v.stone>=25&&v.food>=20){v.buildings++;v.population=Math.Min(20,v.population+2);v.wood-=65;v.stone-=25;v.food-=20;v.maxHp+=150;v.hp+=150;v.decision="Xây nhà và tăng dân";}
                else if(v.army<5)v.decision="Tuyển quân phòng thủ";
                else if(v.warTarget==-2&&day>=3&&day-v.lastRaidDay>=2&&v.aggression>.65f)
                {
                    var rival=villages.Where(n=>n.id!=v.id&&!n.defeated).OrderBy(n=>n.army).FirstOrDefault();
                    bool player=v.relation<-20||rival==null||Random01()<.55f;
                    if(player&&v.relation>=35){v.decision="Giữ quan hệ hữu nghị";continue;}
                    v.warTarget=player?0:rival.id;v.prepare=120;v.decision="Chuẩn bị chiến tranh";message=v.name+" đang tập hợp quân. Còn khoảng hai phút chuẩn bị.";
                }
                else if(v.warTarget==-2)v.decision=CurrentSeason==Season.Winter?"Dự trữ qua mùa đông":"Tích lũy tài nguyên";
                if(v.warTarget>=0&&v.prepare==0&&v.army>=4&&day-v.lastRaidDay>=1)
                {
                    var target=v.warTarget==0?null:villages.Find(n=>n.id==v.warTarget&&!n.defeated);
                    if(v.warTarget>0&&target==null){v.warTarget=-2;continue;}
                    int count=Math.Min(v.army,Math.Max(0,60-units.Count(u=>u.enemy)));if(count<2)continue;
                    v.army-=count;v.lastRaidDay=day;
                    for(int i=0;i<count;i++)
                    {
                        var c=NearestFree(v.x+(i%3-1),v.z-4-i/3,true);int kind=i%4==0?2:i%3==0?1:0;
                        units.Add(new Unit{uid=uid++,enemy=true,villageId=v.id,attackVillageId=v.warTarget,kind=kind,hp=kind==2?180:kind==1?90:110,x=c.x+.5f,z=c.z+.5f,goalX=target==null?0:target.x,goalZ=target==null?-3.5f:target.z});
                    }
                    message=v.name+" đã xuất quân "+count+" người";
                }
            }
            foreach(var v in villages.Where(v=>!v.defeated&&v.hp<=0))
            {
                v.defeated=true;v.decision="Làng đã thất thủ";
                if(v.warWithPlayer){wood=Add(wood,80);stone=Add(stone,40);food=Add(food,60);message="Đã đánh bại "+v.name+" và thu hồi vật tư";}
                foreach(var u in units.Where(u=>u.villageId==v.id).ToArray())units.Remove(u);
                foreach(var other in villages.Where(n=>n.warTarget==v.id)){other.warTarget=-2;other.prepare=0;other.decision="Tái thiết sau chiến tranh";}
            }
        }
        bool AttackVillage(Unit u,float dt)
        {
            NeighborVillage target=null;
            if(!u.enemy)target=villages.Where(v=>v.warWithPlayer&&!v.defeated&&Dist(u.x,u.z,v.x,v.z)<3.1f).OrderBy(v=>Dist(u.x,u.z,v.x,v.z)).FirstOrDefault();
            else if(u.attackVillageId>0)target=villages.Find(v=>v.id==u.attackVillageId&&!v.defeated&&Dist(u.x,u.z,v.x,v.z)<3.1f);
            if(target==null)return false;
            if(u.cooldown<=0){target.hp=Math.Max(0,target.hp-(u.kind==2?24:12));u.cooldown=1.4f;}return true;
        }
        void SpawnWildlife()
        {
            for(int i=0;i<2&&units.Count(u=>u.enemy)<60;i++)units.Add(new Unit{uid=uid++,enemy=true,villageId=-1,kind=0,hp=60,x=-7+i*2,z=26,goalX=0,goalZ=-3.5f});
            message="Thú hoang đang tiến về làng trong đêm";
        }
        void ValidateLiving()
        {
            if(resources==null||tasks==null||villages==null||resources.Count>1000||villages.Count!=3||tasks.Count>4)throw new InvalidOperationException("Sai dữ liệu thế giới");
            if(!Enum.IsDefined(typeof(Weather),weather)||float.IsNaN(clock)||float.IsInfinity(clock)||clock<0||clock>=360||float.IsNaN(morale)||morale<0||morale>100)throw new InvalidOperationException("Sai lịch hoặc thời tiết");
            foreach(var n in resources)if(n.amount<0||n.amount>n.maximum||float.IsNaN(n.amount)||n.regrow<0)throw new InvalidOperationException("Sai nguồn tài nguyên");
            foreach(var t in tasks)if(!resources.Any(n=>n.id==t.node)||t.phase<0||t.phase>2||t.carried<0||t.path==null)throw new InvalidOperationException("Sai công việc khai thác");
            foreach(var v in villages)if(v.army<0||v.army>60||v.population<0||v.food<0||v.wood<0||v.stone<0||float.IsNaN(v.hp))throw new InvalidOperationException("Sai dữ liệu làng AI");
            var ids=new HashSet<int>(buildings.Select(b=>b.uid).Concat(units.Select(u=>u.uid)));
            foreach(var id in resources.Select(n=>n.id).Concat(tasks.Select(t=>t.id)).Concat(villages.Select(v=>v.id)))if(!ids.Add(id))throw new InvalidOperationException("ID thế giới bị trùng");
            if(ids.Count>0&&uid<=ids.Max())throw new InvalidOperationException("Bộ đếm ID không hợp lệ");
        }
    }
}

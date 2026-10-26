using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using LangTroTan.Domain;
using LangTroTan.Infrastructure;

public static class LivingChecks
{
    static List<string> log;
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL LIVING: "+label);log.Add("PASS "+label);Debug.Log("PASS "+label);}
    static void Step(World w,int count){for(int i=0;i<count;i++)w.Tick();}
    static World Rich(){var w=World.New();w.food=600;w.wood=600;w.stone=600;w.buildings[0].hp=1000000;foreach(var v in w.villages)v.aggression=0;return w;}
    static Building Farm(World w)
    {var b=new Building{uid=w.uid++,type="farm",x=6,z=4,complete=true,hp=300,workers=2};w.buildings.Add(b);return b;}
    public static List<string> Run()
    {
        log=new List<string>();var w=Rich();w.Validate();
        Check(w.endless&&w.resources.Count>100&&w.villages.Count==3,"living world starts with finite resources and three neighbors");
        w.day=7;w.clock=359.95f;w.Tick();Check(w.day==8&&!w.ended,"survival continues beyond day seven");
        w=Rich();w.AdvanceCalendar(360*400);Check(w.day==401&&w.Year==26&&!w.ended&&w.clock<360,"calendar advances four hundred days without a victory cap");
        w=Rich();Check(w.CurrentSeason==Season.Spring,"spring starts year");w.day=5;Check(w.CurrentSeason==Season.Summer&&w.DaylightSeconds==270,"summer and longer daylight");w.day=9;Check(w.CurrentSeason==Season.Autumn,"autumn follows summer");w.day=13;Check(w.CurrentSeason==Season.Winter&&w.DaylightSeconds==180,"winter and shorter daylight");w.day=17;Check(w.CurrentSeason==Season.Spring&&w.Year==2,"four seasons roll into next year");
        w=Rich();var weather=new HashSet<Weather>();for(int i=0;i<100;i++){w.AdvanceCalendar(120);weather.Add(w.weather);}Check(weather.Contains(Weather.Clear)&&weather.Contains(Weather.Rain)&&weather.Contains(Weather.Snow),"seeded weather produces sunshine rain and winter snow");
        w=Rich();var node=w.resources.First(n=>n.kind==ResourceKind.Tree&&World.Dist(n.x,n.z,0,-4)<17);w.wood=50;float amount=node.amount;
        Check(w.BeginHarvest(node.id)&&w.Idle==7,"manual logging reserves one villager");Check(!w.BeginHarvest(node.id),"duplicate harvesting job rejected");Step(w,1800);
        Check(w.tasks.Count==0&&w.Idle==8&&w.wood==74&&Math.Abs(node.amount-(amount-24))<.01f,"logger travels harvests and returns finite timber");
        node.amount=0;node.regrow=720;w.wood=20;Check(w.PlantTree(node.id)&&w.wood==15&&node.regrow==180,"replanting stump has a cost and growth timer");Step(w,1810);Check(node.amount==node.maximum,"planted tree regrows");
        w=Rich();var deer=w.resources.First(n=>n.kind==ResourceKind.Deer);float meat=deer.amount;Check(w.BeginHarvest(deer.id),"manual hunting can be ordered");Step(w,2200);Check(w.tasks.Count==0&&deer.amount<meat,"hunter completes a finite hunting trip");
        w=Rich();var farm=Farm(w);w.food=120;Step(w,100);Check(farm.cropProgress>0&&farm.harvests==0&&w.food<120,"crops grow before producing harvest");Step(w,1100);Check(farm.harvests>=1&&farm.planted,"mature crop harvested and automatically replanted");
        var winter=Rich();winter.day=13;winter.weather=Weather.Cloudy;var winterFarm=Farm(winter);Step(winter,100);w=Rich();var springFarm=Farm(w);Step(w,100);Check(winterFarm.cropProgress<springFarm.cropProgress,"winter reduces crop growth");
        Check(winter.Sow(winterFarm.uid,"vegetable",false),"player can choose a vegetable crop");float progress=winterFarm.cropProgress;Step(winter,100);Check(winterFarm.cropProgress==progress,"vegetables pause in freezing temperatures");
        w=Rich();farm=Farm(w);farm.moisture=1;w.buildings.Add(new Building{uid=w.uid++,type="well",x=8,z=8,hp=500,complete=true});w.Tick();Check(farm.moisture>=55,"nearby well irrigates farm");
        w=Rich();w.day=13;float wood=w.wood;Step(w,101);Check(w.wood<wood,"winter heating consumes stored wood");
        w=Rich();var v=w.villages[0];float before=w.food;int relation=v.relation;Check(w.Gift(v.id)&&w.food==before-30&&v.relation>relation,"diplomatic gift costs food and improves relations");
        w.stone=100;v.stone=100;before=w.food;Check(w.Trade(v.id)&&w.stone==125&&w.food==before-40&&v.stone==75,"trade debits both economies atomically");
        Check(w.DeclareWar(v.id)&&v.warWithPlayer&&!w.Trade(v.id),"war changes hostility and blocks trading");Check(w.OfferPeace(v.id)&&!v.warWithPlayer,"paid peace ends player war");
        w=Rich();v=w.villages[0];v.army=3;v.food=0;v.wood=0;v.population=0;v.recruitClock=35;w.Tick();Check(v.army==3,"AI cannot recruit without resources");v.food=100;v.wood=100;v.recruitClock=35;w.Tick();Check(v.army==4&&v.food<100&&v.wood<100,"AI spends its own resources to recruit");
        w=Rich();w.day=3;v=w.villages[1];v.aggression=.9f;v.relation=-40;v.army=8;v.buildings=8;v.food=1000;v.wood=1000;v.stone=500;Step(w,2000);
        Check(v.warWithPlayer&&w.units.Any(u=>u.villageId==v.id)&&v.lastRaidDay==3,"aggressive neighbor prepares declares war and dispatches an army");
        w=Rich();var a=w.villages[0];var b=w.villages[1];a.warTarget=b.id;b.warTarget=a.id;
        var ua=new Unit{uid=w.uid++,villageId=a.id,attackVillageId=b.id,enemy=true,hp=100,x=0,z=-5};var ub=new Unit{uid=w.uid++,villageId=b.id,attackVillageId=a.id,enemy=true,hp=100,x=0,z=-4.5f};w.units.Add(ua);w.units.Add(ub);w.Tick();
        Check(ua.hp<100&&ub.hp<100&&w.people==8,"AI villages fight each other without consuming player population");
        w=Rich();v=w.villages[1];v.warWithPlayer=true;v.hp=0;w.Tick();Check(v.defeated&&v.hp==0&&!w.ended,"defeated neighboring village does not auto revive or end sandbox");
        w=Rich();w.schema=1;w.day=7;w.ended=true;w.won=true;before=w.food;w.UpgradeSave();w.Validate();Check(w.schema==2&&!w.ended&&w.endless&&w.food==before,"old victorious seven-day save migrates to continuing sandbox");
        string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Reports/LivingSave");Directory.CreateDirectory(dir);SaveRepository.RootOverride=dir;
        try
        {
            w=Rich();w.day=15;w.weather=Weather.Snow;w.villages[1].warWithPlayer=true;w.villages[1].warTarget=0;Farm(w).cropProgress=.52f;SaveRepository.Save(w);
            var copy=SaveRepository.Load(out bool recovered);Check(!recovered&&copy.day==15&&copy.weather==Weather.Snow&&copy.villages[1].warWithPlayer&&copy.buildings.Last().cropProgress>.5f,"season weather diplomacy and crop state survive save round trip");
        }
        finally{SaveRepository.RootOverride=null;}
        File.WriteAllLines(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Reports/LivingChecks.txt"),log);return log;
    }
}

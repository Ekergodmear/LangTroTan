using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LangTroTan.Domain;
using LangTroTan.Infrastructure;

public static class SectChecks
{
    static List<string> log;
    static void Check(bool ok,string name){if(!ok)throw new Exception("SECT FAIL: "+name);log.Add("PASS "+name);}
    static World Rich()
    {var w=World.New();w.waveSpawned=10000;w.wood=600;w.stone=600;w.food=600;w.spiritStones=500;w.herbs=100;w.resources.RemoveAll(n=>n.x*n.x+n.z*n.z<350);return w;}
    static Building Ready(World w,string type,int x,int z)
    {if(!w.Place(type,x,z))throw new Exception(w.message);var b=w.buildings.Last();b.complete=true;b.hp=w.MaxHp(b);return b;}
    static void Step(World w,int n){for(int i=0;i<n;i++)w.Tick();}
    static void Level(World w,int n){w.level=n;var b=w.buildings.First(v=>v.type=="hall");b.level=n;b.hp=w.MaxHp(b);}
    public static List<string> Run()
    {
        log=new List<string>();var w=Rich();Check(w.disciples.Count==0&&w.Idle==8&&w.sectVersion==1,"sect foundation does not duplicate population");
        Check(!w.RecruitDisciple(0)&&!w.Place("alchemy",8,8),"sect buildings and recruitment lock before hall two");
        var house=Ready(w,"house",6,4);var drill=Ready(w,"barracks",-8,-8);var hall=w.buildings.First(b=>b.type=="hall");
        Check(!w.UpgradeBuilding(house.uid),"ordinary building cannot exceed hall tier");
        float wood=w.wood;Check(w.UpgradeBuilding(hall.uid)&&hall.upgrading&&w.level==1&&w.Idle==7,"hall upgrade reserves builder and takes time");
        Check(!w.UpgradeBuilding(hall.uid)&&w.wood==wood-120,"repeated upgrade does not double charge");
        Check(w.CancelUpgrade(hall.uid)&&!hall.upgrading&&w.Idle==8&&Math.Abs(w.wood-(wood-24))<.01f,"cancel upgrade returns eighty percent and builder");
        w.wood=600;w.stone=600;Check(w.UpgradeBuilding(hall.uid),"hall upgrade restarts after cancellation");Step(w,601);
        Check(w.level==2&&hall.level==2&&!hall.upgrading&&hall.hp==2500,"hall completes timed upgrade and gains max hp");
        Check(w.UpgradeBuilding(house.uid),"house starts upgrade after hall unlock");Step(w,301);
        Check(house.level==2&&w.Housing==16&&w.MaxHp(house)==525,"upgraded house increases housing and hp");
        var store=w.buildings.First(b=>b.type=="store");w.wood=600;w.stone=600;w.UpgradeBuilding(store.uid);Step(w,301);Check(w.Capacity==900,"store upgrade expands resource capacity");
        int idle=w.Idle;float food=w.food;Check(w.RecruitDisciple(0)&&w.Idle==idle-1&&w.people==8&&w.food==food-30,"disciple converts one available person without population inflation");
        var d=w.disciples[0];Check(d.school==0&&d.aptitude>=45&&d.aptitude<=95,"disciple has school and deterministic aptitude");
        Step(w,100);Check(d.cultivation>0,"disciple accumulates cultivation over simulation ticks");
        Check(w.RealmLayers(d)==9,"source realm table uses nine qi layers");d.realm=1;d.layer=4;Check(w.RealmLayers(d)==4&&w.RealmLabel(d).Contains("Đỉnh Phong"),"later realms use four named sublevels");d.realm=0;d.layer=1;
        d.cultivation=30;w.Tick();Check(d.layer==2&&d.cultivation==0,"minor realm advances without manual action");
        wood=w.spiritStones;Check(!w.Breakthrough(d.id,false)&&w.spiritStones==wood,"early breakthrough rejects without debit");
        d.layer=9;d.cultivation=30;Check(Math.Abs(w.BreakthroughChance(d,false)-.85f)<.001f&&Math.Abs(w.BreakthroughChance(d,true)-1)<.001f,"source breakthrough base eighty five percent and one pill adds fifteen points");
        w.pills=1;wood=w.spiritStones;Check(w.Breakthrough(d.id,true)&&d.realm==1&&d.layer==1&&w.pills==0&&w.spiritStones==wood-30,"guaranteed qi breakthrough consumes one pill and stones");
        d.layer=4;d.cultivation=50;Check(!w.Breakthrough(d.id,false),"higher realm blocked until next sect tier");
        d.demon=100;d.realm=0;d.layer=9;d.cultivation=30;Check(w.Breakthrough(d.id,false)&&d.realm==0&&d.wounded==90&&d.demon==102,"failed breakthrough applies injury and heart demon");
        float herb=w.herbs;Check(w.HealDisciple(d.id)&&d.wounded==0&&w.herbs==herb-10,"healing consumes actual herbs and stones");d.demon=0;
        w.artifacts=1;Check(w.EquipDisciple(d.id)&&d.equipped&&w.artifacts==0&&!w.EquipDisciple(d.id),"artifact cannot be equipped twice or duplicate inventory");
        var mine=Ready(w,"spiritmine",8,-8);w.Assign(mine.uid,1);wood=w.spiritStones;Step(w,100);Check(w.spiritStones>wood,"assigned spirit mine generates stones");
        var alchemy=Ready(w,"alchemy",8,8);w.Assign(alchemy.uid,1);w.spiritStones=100;w.herbs=100;int pills=w.pills;Step(w,601);Check(w.pills==pills+1&&w.herbs<100,"alchemy produces an actual pill from ingredients");
        w.Assign(mine.uid,-1);w.spiritStones=0;alchemy.craftProgress=0;pills=w.pills;Step(w,610);Check(w.pills==pills&&alchemy.craftProgress==0,"workshop cannot craft for free when materials absent");
        w.spiritStones=500;var forge=Ready(w,"forge",-13,6);w.Assign(forge.uid,1);Step(w,601);Check(w.artifacts>=1,"forge produces inventory artifact");
        Check(!w.SendMission(d.id),"missions require mission hall");var mission=Ready(w,"mission",-10,2);w.food=600;wood=w.spiritStones;Check(w.SendMission(d.id)&&d.missionTime==180&&w.spiritStones==wood-5,"mission pays supplies and reserves disciple");
        wood=w.spiritStones;Check(!w.SendMission(d.id)&&w.spiritStones==wood,"duplicate mission cannot double debit");Step(w,1801);Check(d.missionTime==0&&w.sectReputation>=12,"mission resolves automatically into success or retreat");
        Check(w.SetPolicy("cultivation")&&w.sectPolicy=="cultivation"&&!w.SetPolicy("bogus"),"policy validates allowed rules");
        for(int i=0;i<125;i++)w.Record("Journal "+i);Check(w.chronicle.Count==120,"chronicle retention bounded to latest one hundred twenty events");
        w=Rich();w.sectVersion=0;w.spiritStones=0;w.InitializeSect();wood=w.spiritStones;w.InitializeSect();Check(w.spiritStones==wood&&w.chronicle.Count==1,"old save sect initialization is idempotent");
        Level(w,2);w.RecruitDisciple(4);house=Ready(w,"house",6,4);w.UpgradeBuilding(house.uid);w.Tick();w.Validate();
        string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Reports/SectSave");Directory.CreateDirectory(dir);SaveRepository.RootOverride=dir;
        try{SaveRepository.Save(w);var copy=SaveRepository.Load(out bool recovered);Check(!recovered&&copy.disciples.Count==1&&copy.buildings.Last().upgrading&&copy.buildings.Last().upgradeProgress>0&&copy.spiritStones==w.spiritStones,"save roundtrip preserves disciple upgrade timer and sect resources");}finally{SaveRepository.RootOverride=null;}
        File.WriteAllLines("Reports/SectChecks.txt",log);return log;
    }
}

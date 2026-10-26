using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using LangTroTan.Domain;
using LangTroTan.Infrastructure;

public static class ProjectBuild
{
    static List<string> results=new List<string>();
    static void Check(bool value,string label){if(!value)throw new Exception("FAIL: "+label);results.Add("PASS "+label);Debug.Log("PASS "+label);}
    static void Advance(World w,int ticks){for(int i=0;i<ticks;i++)w.Tick();}
    static Building Ready(World w,string type,int x,int z)
    {
        if(!w.Place(type,x,z))throw new Exception(w.message);var b=w.buildings.Last();b.complete=true;b.hp=Catalog.Get(type).hp;return b;
    }
    static World Rich(){var w=World.New();w.wood=600;w.stone=600;w.food=600;w.resources.RemoveAll(n=>n.x*n.x+n.z*n.z<144);return w;}
    [MenuItem("LÃ ng Tro TÃ n/Run checks and build Windows")]
    public static void Build()
    {
        try
        {
            RunChecks();
            PlayerSettings.companyName="LangTroTan";PlayerSettings.productName="LÃ ng Tro TÃ n";
            PlayerSettings.bundleVersion="0.4.0";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=false;
            PlayerSettings.enableFrameTimingStats=true;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            // Legacy input works on desktop and touch devices; package migration is a separate milestone.
            var playerSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=playerSettings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;playerSettings.ApplyModifiedPropertiesWithoutUndo();}
            Directory.CreateDirectory("Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Village.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Village.unity",true)};
            // Ensure runtime-created materials survive shader stripping in a scene without renderers.
            var shaders=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var always=shaders.FindProperty("m_AlwaysIncludedShaders");
            foreach(var name in new[]{"Standard","Sprites/Default"})
            {var shader=Shader.Find(name);if(shader!=null){int at=always.arraySize;always.InsertArrayElementAtIndex(at);always.GetArrayElementAtIndex(at).objectReferenceValue=shader;}}
            shaders.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            string root=Directory.GetParent(Application.dataPath).FullName;
            string folder=Environment.GetCommandLineArgs().Contains("-visual-baseline")?"VisualBaseline":Environment.GetCommandLineArgs().Contains("-visual-slice-build")?"VisualSlice":"Windows_FullPort_Dev";
            string dest=Path.Combine(root,"Builds/"+folder+"/LangTroTan.exe");Directory.CreateDirectory(Path.GetDirectoryName(dest));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Village.unity"},locationPathName=dest,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Path.Combine(root,"BUILD_RESULT.txt"),"Build "+report.summary.result+"\n"+report.summary.totalSize+" bytes\n"+string.Join("\n",results));
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.totalErrors);
            Debug.Log("LANG_TRO_TAN_BUILD_OK "+dest);
        }
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    [MenuItem("LÃ ng Tro TÃ n/Run domain and save checks")]
    public static void RunChecks()
    {
        results.Clear();var w=World.New();w.Validate();
        Check(w.people==8&&w.Idle==8&&w.Capacity==600&&w.wood==120&&w.buildings.Count==2,"new game baseline");
        w.resources.RemoveAll(n=>n.x>=6&&n.x<9&&n.z>=4&&n.z<7);w.wood=45;Check(w.Place("farm",6,4),"place farm exact cost");
        Check(!w.Place("farm",10,4)&&w.wood==0&&w.buildings.Count==3&&w.Idle==7,"atomic placement no duplicate debit");
        w=Rich();Check(!w.Place("farm",0,0)&&w.wood==600,"overlap rejected without debit");
        Check(!w.Place("farm",30,30),"outside build radius rejected");
        w=Rich();for(int i=0;i<8;i++)Check(w.Place("wall",-10+i,-8),"reserve builder "+i);
        Check(w.Idle==0&&!w.Place("wall",-1,-8),"no builder rejects construction");
        w=Rich();
        var house=Ready(w,"house",6,4);Check(w.Housing==12,"housing after completion");
        float before=w.wood;w.Demolish(house.uid);Check(Math.Abs(w.wood-(before+12))<.01f,"completed demolition refund");
        w=Rich();w.Place("house",6,4);before=w.wood;w.Demolish(w.buildings.Last().uid);Check(w.wood==before+32,"blueprint refund eighty percent");
        w=Rich();var farm=Ready(w,"farm",6,4);Check(w.Assign(farm.uid,1)&&w.Assign(farm.uid,1),"assign workers");
        w.food=120;Advance(w,101);Check(Math.Abs(w.food-116)<.2f&&farm.cropProgress>0,"farm grows a crop while villagers consume food");
        w=Rich();var barracks=Ready(w,"barracks",6,4);Check(!w.Train(barracks.uid,1),"archer locked at level one");
        float food=w.food;Check(w.Train(barracks.uid,0)&&w.Idle==7,"training reserves person");
        Check(w.CancelTraining(barracks.uid)&&w.food==food&&w.Idle==8,"training cancellation restores person and food");
        for(int i=0;i<4;i++)w.Train(barracks.uid,0);
        Check(w.queue.Count==4&&!w.Train(barracks.uid,0),"barracks queue capacity four");
        while(w.queue.Count>0)w.CancelTraining(barracks.uid);
        w.Train(barracks.uid,0);Advance(w,205);Check(w.Soldiers==1&&w.queue.Count==0&&w.Idle==7,"soldier completes and remains in population");
        Check(w.Order(0,-5,-5)&&w.units[0].path.Count>0,"squad receives reachable path");
        Check(!w.Order(0,0,0),"blocked order rejected");
        float startX=w.units[0].x;Advance(w,20);Check(Math.Abs(w.units[0].x-startX)>.1f,"unit follows path over ticks");
        w.Validate();
        string root=Directory.GetParent(Application.dataPath).FullName,saveDir=Path.Combine(root,"Reports/TestSave");
        Directory.CreateDirectory(saveDir);SaveRepository.RootOverride=saveDir;
        SaveRepository.Save(w);SaveRepository.Save(w);var loaded=SaveRepository.Load(out bool recovered);
        Check(!recovered&&loaded.Soldiers==1&&loaded.people==w.people&&Math.Abs(loaded.food-w.food)<.001f,"save checksum and round trip");
        File.WriteAllText(SaveRepository.PathName,"corrupt");loaded=SaveRepository.Load(out recovered);Check(recovered&&loaded.Soldiers==1,"backup recovers corrupt primary");
        SaveRepository.RootOverride=null;
        w=World.New();w.clock=239.95f;w.Tick();Check(w.waveSpawned==1&&w.Enemies==4,"night one spawns four enemies once");w.Tick();Check(w.Enemies==4,"wave not duplicated");
        w=World.New();w.day=7;w.waveSpawned=7;w.Tick();Check(!w.ended,"clearing night seven does not end sandbox");
        w=World.New();w.day=7;w.waveSpawned=7;w.buildings.Find(b=>b.type=="hall").hp=0;w.Tick();Check(w.ended&&!w.won,"defeat precedes victory in same tick");
        w=World.New();w.units.Add(new Unit{uid=w.uid++,kind=0,hp=110,x=0,z=-5,goalX=0,goalZ=-5});
        w.units.Add(new Unit{uid=w.uid++,kind=0,hp=60,enemy=true,x=0,z=-4.5f});w.Tick();
        Check(w.units[1].hp==45&&w.units[0].hp==103,"combat spear bonus and enemy damage");
        Advance(w,70);Check(w.Enemies==0&&w.Soldiers==1&&w.people==8,"combat removes defeated enemy without changing friendly population");
        results.AddRange(LivingChecks.Run());results.AddRange(SectChecks.Run());results.AddRange(FullPortChecks.Run());Directory.CreateDirectory(Path.Combine(root,"Reports"));File.WriteAllLines(Path.Combine(root,"Reports/Checks.txt"),results);
        Debug.Log("LANG_TRO_TAN_CHECKS_OK "+results.Count);
    }
}


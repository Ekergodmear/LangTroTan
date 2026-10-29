using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using LangTroTan.Infrastructure;

namespace LangTroTan.Presentation
{
    public sealed partial class FullPortView
    {
        string visualOutput;readonly List<string> visualChecks=new List<string>();
        void VisualCheck(bool condition,string message){visualChecks.Add((condition?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(visualOutput,"regression.txt"),visualChecks);if(!condition){Application.Quit(1);throw new Exception("VISUAL_QA_FAIL "+message);}Debug.Log("VISUAL_QA_PASS "+message);}
        IEnumerator RunVisualQa()
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            visualOutput=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../VisualSlice",Environment.GetCommandLineArgs().Contains("-visual-baseline")?"Before":"After"));Directory.CreateDirectory(visualOutput);
            File.WriteAllText(Path.Combine(visualOutput,"regression.txt"),"RUNNING\n");
            float deadline=Time.realtimeSinceStartup+30;while(api.Snapshot==null&&Time.realtimeSinceStartup<deadline)yield return null;
            VisualCheck(api.Snapshot!=null,"isolated QA server snapshot received");
            if(SonMonTiers.Enabled&&Environment.GetCommandLineArgs().Contains("-tier-only"))
            {
                Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(2);projectionActive=false;tab=0;Draw();CloseSpatialDetail();SonMonSky.PreviewPhase=.30f;SonMonSky.PreviewSeason=0;yield return new WaitForSecondsRealtime(2);yield return LevelRegression();
                visualChecks.Add("COMPLETE");File.WriteAllLines(Path.Combine(visualOutput,"regression.txt"),visualChecks);Debug.Log("VISUAL_QA_COMPLETE "+visualOutput);Application.Quit();yield break;
            }
            if(SonMonTiers.Enabled&&Environment.GetCommandLineArgs().Contains("-tier-live"))
            {
                Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(2);yield return LiveUpgradeRegression();
                visualChecks.Add("COMPLETE");File.WriteAllLines(Path.Combine(visualOutput,"regression.txt"),visualChecks);Debug.Log("VISUAL_QA_COMPLETE "+visualOutput);Application.Quit();yield break;
            }
            foreach(int width in new[]{1600,1920})
            {
                int height=width*9/16;Screen.SetResolution(width,height,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(2);
                projectionActive=false;tab=0;Draw();yield return new WaitForSecondsRealtime(2);VisualCapture("son-mon",width,height);
                yield return ProfileVisual("son-mon",width,height);
                var b=List(Get(spatial,"buildings")).First(v=>Get(v,"coreId")!=null);ShowSpatialBuilding(S(Get(b,"id")));yield return null;VisualCheck(dialog!=null,"core building opens authoritative upgrade decision");CloseDialog();
                tab=1;Draw();yield return new WaitForSecondsRealtime(.5f);VisualCapture("de-tu",width,height);VisualCheck(body.GetComponentsInChildren<Button>().Length>=4,"disciple contextual controls present");if(SliceEnabled)yield return ProfileVisual("de-tu",width,height);
                bool loaded=false;yield return api.Presentation("daidien",new Dictionary<string,object>{{"daiDienTab","baokho"}},null,data=>{projection=data;projectionActive=true;projectionView="daidien";projectionUi=new Dictionary<string,object>{{"daiDienTab","baokho"}};projectionPage=0;DrawProjection();loaded=true;});VisualCheck(loaded,"Bao Kho source projection available");yield return new WaitForSecondsRealtime(.5f);VisualCapture("dai-dien",width,height);
                if(SliceEnabled)yield return ProfileVisual("dai-dien",width,height);
                projectionActive=false;tab=0;Draw();CloseSpatialDetail();yield return new WaitForSecondsRealtime(.5f);VisualCapture("hud",width,height);
            }
            if(SliceEnabled&&Environment.GetCommandLineArgs().Contains("-visual-regression"))yield return VisualInteractionRegression();
            if(SliceEnabled){CloseDialog();CloseSpatialDetail();projectionActive=false;tab=0;Draw();CloseSpatialDetail();var focus=physicalFocus;float zoom=physicalZoom;physicalFocus=new Vector3(0,2,0);physicalZoom=11;SetSpatialCamera();yield return new WaitForSecondsRealtime(.5f);VisualCapture("architecture-detail",1920,1080);physicalFocus=focus;physicalZoom=zoom;SetSpatialCamera();}
            if(SliceEnabled&&SonMonKit.Instance!=null)yield return SonMonTimeOfDayCaptures();
            visualChecks.Add("COMPLETE");File.WriteAllLines(Path.Combine(visualOutput,"regression.txt"),visualChecks);Debug.Log("VISUAL_QA_COMPLETE "+visualOutput);Application.Quit();
        }
        void VisualCapture(string name,int width,int height)
        {
            Canvas.ForceUpdateCanvases();var cv=canvas.GetComponentInParent<Canvas>();var target=new RenderTexture(width,height,24){antiAliasing=2};var previous=RenderTexture.active;
            cv.renderMode=RenderMode.ScreenSpaceCamera;cv.worldCamera=cam;cv.planeDistance=1;cam.targetTexture=target;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(visualOutput,name+"-"+width+".png"),image.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=previous;cv.renderMode=RenderMode.ScreenSpaceOverlay;target.Release();Destroy(target);Destroy(image);
            var overflow=new List<string>();foreach(var label in canvas.GetComponentsInChildren<Text>()){
                if(label.GetComponentInParent<ScrollRect>()!=null||string.IsNullOrEmpty(label.text))continue;
                if(label.preferredHeight>label.rectTransform.rect.height+3)overflow.Add(label.text+" [height "+label.preferredHeight+" > "+label.rectTransform.rect.height+"]");
            }
            File.WriteAllText(Path.Combine(visualOutput,"layout-"+name+"-"+width+".json"),ServerJson.Stringify(new Dictionary<string,object>{{"width",width},{"height",height},{"textOverflow",overflow.Cast<object>().ToList()},{"screenshotIsUnityCameraRender",true}}));
        }
        Button FindSliceButton(Transform root,string caption)=>root.GetComponentsInChildren<Button>().First(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text==caption));
        // Presentation-only captures for the Milestone B review: Chánh Điện hero close-up, dawn and night.
        // The clock override never reaches the server; it is cleared before the run ends.
        IEnumerator SonMonTimeOfDayCaptures()
        {
            CloseDialog();CloseSpatialDetail();projectionActive=false;tab=0;Draw();CloseSpatialDetail();
            var hall=models.TryGetValue("core:chanh_dien",out var root)?root.transform.position:Vector3.zero;
            IEnumerator Settle(float phase){SonMonSky.PreviewPhase=phase;SonMonSky.PreviewSeason=0;for(int i=0;i<3;i++){SonMonArt.ApplyTimeOfDay(phase,0,false,"clear",spatialSun,cam,1);yield return new WaitForSecondsRealtime(.4f);}}
            void Hero(){cam.transform.position=hall+new Vector3(4.6f,2.4f,-8.4f);cam.transform.LookAt(hall+new Vector3(0,2.2f,0));}
            yield return Settle(.30f);Hero();yield return new WaitForSecondsRealtime(.3f);Hero();VisualCapture("hall-closeup",1920,1080);
            yield return CharacterCaptures();
            if(SonMonTiers.Enabled)yield return LevelRegression();
            yield return WorldScaleRegression();
            yield return Settle(.03f);SetSpatialCamera();yield return new WaitForSecondsRealtime(.3f);VisualCapture("dawn-son-mon",1920,1080);
            Hero();VisualCapture("dawn-hall-closeup",1920,1080);
            yield return Settle(.85f);SetSpatialCamera();yield return new WaitForSecondsRealtime(1.5f);VisualCapture("night-son-mon",1920,1080);
            Hero();VisualCapture("night-hall-closeup",1920,1080);
            SonMonSky.PreviewPhase=null;SonMonSky.PreviewSeason=null;SetSpatialCamera();
        }
        // Spirit deer and disciple prototype review captures (camera only).
        IEnumerator CharacterCaptures()
        {
            Transform deer=null;foreach(var node in spatialNodes.Values)if(node!=null&&node.name=="deer"&&node.GetComponentInChildren<SonMonPuppet>()!=null){deer=node.transform;break;}
            if(deer!=null)
            {
                var model=deer.GetComponentInChildren<SonMonPuppet>().transform;
                cam.transform.position=model.position+model.rotation*new Vector3(1.5f,1.25f,-2.6f);cam.transform.LookAt(model.position+Vector3.up*.95f);
                yield return new WaitForSecondsRealtime(.4f);cam.transform.LookAt(model.position+Vector3.up*.95f);VisualCapture("spiritdeer-closeup",1920,1080);
                physicalFocus=new Vector3(deer.position.x,2,deer.position.z);physicalZoom=13;SetSpatialCamera();yield return new WaitForSecondsRealtime(.4f);VisualCapture("spiritdeer-in-sonmon",1920,1080);
            }
            else VisualCheck(false,"a server deer node is rendered as the spirit deer");
            var disciple=GameObject.Find("SonMon Disciple (idle)");
            if(disciple!=null)
            {
                var d=disciple.transform;
                cam.transform.position=d.position+d.rotation*new Vector3(.75f,1.05f,-1.9f);cam.transform.LookAt(d.position+Vector3.up*.8f);
                yield return new WaitForSecondsRealtime(.4f);cam.transform.LookAt(d.position+Vector3.up*.8f);VisualCapture("disciple-closeup",1920,1080);
                physicalFocus=new Vector3(d.position.x,2,d.position.z);physicalZoom=21;SetSpatialCamera();yield return new WaitForSecondsRealtime(.4f);VisualCapture("disciple-gameplay",1920,1080);
            }
            else VisualCheck(false,"disciple prototype present in the Son Mon scene");
            physicalFocus=new Vector3(0,4,0);physicalZoom=21;SetSpatialCamera();
        }
        // Building visual levels (scripture, server levels 1..10) and the upgrade transition. Simulated levels go
        // through the normal draw path as modified copies of the spatial snapshot (server untouched, polling
        // paused); the original snapshot is restored and the server level re-checked afterwards.
        const string LevelBuilding="core:tang_kinh_cac";
        int LevelMarkers()=>FindObjectsByType<FullPortBuilding>(FindObjectsSortMode.None).Count(m=>m!=null&&m.serverId==LevelBuilding);
        void FrameLevel(GameObject root){cam.transform.position=root.transform.position+new Vector3(5.6f,4.3f,-8.6f);cam.transform.LookAt(root.transform.position+Vector3.up*2.5f);}
        void CaptureLevel(GameObject root,string name){FrameLevel(root);VisualCapture(name,1920,1080);}
        // One visual container, nothing retired left behind, no temporary glow material left on the building.
        bool CleanVisual(GameObject root)=>root.transform.Cast<Transform>().Count(c=>c.name=="SonMonVisual")==1&&GameObject.Find("SonMonVisual (retired)")==null&&root.transform.Find("Fx_building_upgrade")==null
            &&root.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m==null||!m.name.EndsWith("(upgrade glow)")));
        IEnumerator FinishFx(GameObject root){float limit=Time.realtimeSinceStartup+10;while(SonMonUpgradeFx.IsPlaying(root.transform,out _)&&Time.realtimeSinceStartup<limit)yield return null;yield return null;yield return null;}
        IEnumerator WaitFx(GameObject root,float elapsed){while(SonMonUpgradeFx.IsPlaying(root.transform,out var fx)&&fx.Elapsed<elapsed){FrameLevel(root);yield return null;}}
        // Captures the transition at its key beats: energy gathering, rune formation, model change, burst, settled.
        IEnumerator CaptureFx(GameObject root,string prefix)
        {
            string[] beats={"1-gather","2-formation","3-change","4-burst"};float[] at={.7f,1.6f,2.25f,2.65f};
            for(int i=0;i<beats.Length;i++){yield return WaitFx(root,at[i]);if(SonMonUpgradeFx.IsPlaying(root.transform,out _))CaptureLevel(root,prefix+"-"+beats[i]);}
            yield return FinishFx(root);CaptureLevel(root,prefix+"-5-settled");
        }
        IEnumerator LevelRegression()
        {
            string id=LevelBuilding;
            if(!models.TryGetValue(id,out var root)){VisualCheck(false,"scripture core building present");yield break;}
            var kit=SonMonKit.Instance;int serverLevel=levels[id],max=MaxBuildingLevel;
            int Expected(int level)=>SonMonTiers.Resolve(kit,"scripture","tang_kinh_cac",level,out var e,out _)?e.level:-1;
            SonMonVisualTag Tag()=>SonMonArt.VisualTag(root.transform);
            VisualCheck(max==10,"server reports MAX_BUILDING_LEVEL "+max);
            VisualCheck(!SonMonTiers.TryGetSet(kit,"scripture",null,out _)&&!SonMonTiers.TryGetSet(kit,"barracks",null,out _),"player-built buildings never resolve a core family (identity-based mapping)");
            VisualCheck(SonMonTiers.TryGetSet(kit,"scripture","tang_kinh_cac",out var set)&&set.maxLevel==max&&Enumerable.Range(1,max).All(l=>set.levels.Count(e=>e.level==l)==1),"scripture defines exactly one visual entry per level 1.."+max);
            VisualCheck(root.GetComponent<SonMonUpgradeFx>()==null,"saved level "+serverLevel+" loads without the upgrade animation");
            VisualCheck(Tag()!=null&&Tag().level==serverLevel&&Tag().modelLevel==Expected(serverLevel),"server level "+serverLevel+" renders the level-"+Expected(serverLevel)+" model");
            CaptureLevel(root,"level-scripture-server-L"+serverLevel);
            while(spatialPolling)yield return null;spatialPolling=true;spatialSimulation=true;  // let an in-flight poll land first
            var original=Get(spatial,"buildings");int modelCount=models.Count;
            void Feed(int level){var copy=new List<object>();foreach(var raw in List(original)){var b=Map(raw);if(S(Get(b,"id"))==id){var changed=new Dictionary<string,object>(b);changed["level"]=(double)level;copy.Add(changed);}else copy.Add(raw);}spatial["buildings"]=copy;DrawSpatialModels();}
            void Same(string when)=>VisualCheck(models[id]==root&&LevelMarkers()==1&&models.Count==modelCount,when+": same building GameObject, no duplicate");

            // 1. Every level 10..1. Reaching 10 is a rise (transition); stepping down snaps instantly.
            Feed(10);if(serverLevel<10)VisualCheck(SonMonUpgradeFx.IsPlaying(root.transform,out _),"rise "+serverLevel+" -> 10 plays the upgrade transition");yield return FinishFx(root);
            var heights=new Dictionary<int,Vector3>();
            for(int level=10;level>=1;level--)
            {
                Feed(level);yield return null;var tag=Tag();Same("level "+level);
                var left=root.GetComponent<SonMonUpgradeFx>();VisualCheck(left==null,"level "+level+" lower-level update snaps without animation"+(left!=null?" [fx playing="+left.Playing+" t="+left.Elapsed.ToString("0.00")+" "+left.FromLevel+"->"+left.ToLevel+" swapped="+left.Swapped+"]":""));
                VisualCheck(tag!=null&&tag.level==level&&tag.modelLevel==Expected(level)&&tag.Fallback==(Expected(level)!=level),"level "+level+" shows model L"+Expected(level)+(tag!=null&&tag.Fallback?" (dev fallback, plaque)":" ("+tag?.status+")"));
                VisualCheck(!tag.Fallback||tag.transform.Find("DevPlaque")!=null,"level "+level+" fallback is labelled as development-only");
                VisualCheck(root.GetComponent<BoxCollider>().size.y>1&&CleanVisual(root),"level "+level+" fitted selection collider and a single clean visual");
                SonMonArt.LocalVisualBounds(root.transform,tag.transform,out var bounds);heights[level]=bounds.size;
                CaptureLevel(root,"level-scripture-L"+level.ToString("00"));
            }
            bool ladder=true;string sizes="";for(int level=1;level<=max;level++){sizes+=" L"+level+"="+heights[level].y.ToString("0.00")+"h/"+Mathf.Max(heights[level].x,heights[level].z).ToString("0.00")+"w";if(level>1&&(heights[level].y+.01f<heights[level-1].y||Mathf.Max(heights[level].x,heights[level].z)+.01f<Mathf.Max(heights[level-1].x,heights[level-1].z)))ladder=false;}
            // Art Bible §2 (revised): a level smaller than the one below is flagged for visual review, not a code failure.
            visualChecks.Add((ladder?"PASS no level is smaller than the level below:":"REVIEW a level is smaller than the level below (asset review item):")+sizes);

            // 2. Reduced effects 1 -> 4: short glow and change, no particles or formation.
            SonMonUpgradeFx.SessionOverride=true;float started=Time.realtimeSinceStartup;Feed(4);
            VisualCheck(SonMonUpgradeFx.IsPlaying(root.transform,out var reduced)&&reduced.Reduced&&root.transform.Find("Fx_building_upgrade")==null,"reduced effects: transition without particles or formation");
            yield return FinishFx(root);float reducedTime=Time.realtimeSinceStartup-started;SonMonUpgradeFx.SessionOverride=false;
            VisualCheck(reducedTime<SonMonUpgradeFx.ReducedDuration+.6f&&Tag().level==4&&CleanVisual(root),"reduced effects finish in "+reducedTime.ToString("0.00")+" s at level 4");

            // 3. Full transition 4 -> 5 (fallback model -> ForgeGUI candidate), non-blocking: selection works mid-way.
            Feed(5);VisualCheck(SonMonUpgradeFx.IsPlaying(root.transform,out var full)&&!full.Reduced&&full.FromLevel==4&&full.ToLevel==5,"server-confirmed rise 4 -> 5 starts the full transition");
            yield return WaitFx(root,1.0f);ShowSpatialBuilding(id);yield return null;
            var state=dialog!=null?dialog.GetComponentsInChildren<Text>().FirstOrDefault(t=>t.name=="UpgradeState"):null;
            VisualCheck(state!=null&&state.text.Contains("Đang thăng cấp 4 → 5"),"building stays selectable during the transition and the panel shows it");CloseDialog();
            yield return CaptureFx(root,"upgrade-fx-L4-L5");
            VisualCheck(Tag().level==5&&Tag().modelLevel==Expected(5)&&CleanVisual(root),"transition ends on the level-5 model with materials restored");Same("after transition");
            VisualCheck(status.text.Contains("Cấp 5"),"UI announces the new level after the transition: "+status.text);

            // 4. Performance: idle window vs. transition window (no captures).
            var idle=new List<double>();var during=new List<double>();float until=Time.realtimeSinceStartup+SonMonUpgradeFx.FullDuration;
            while(Time.realtimeSinceStartup<until){FrameLevel(root);yield return null;idle.Add(Time.unscaledDeltaTime*1000);}
            Feed(6);while(SonMonUpgradeFx.IsPlaying(root.transform,out _)){FrameLevel(root);yield return null;during.Add(Time.unscaledDeltaTime*1000);}
            yield return null;yield return null;  // Unity destroys the effect object at the end of the frame the transition ends
            object Stats(List<double> v){v.Sort();return new Dictionary<string,object>{{"samples",v.Count},{"median",v.Count>0?v[v.Count/2]:0},{"p95",v.Count>0?v[Math.Min(v.Count-1,(int)(v.Count*.95))]:0},{"max",v.Count>0?v[v.Count-1]:0}};}
            double idleMedian=idle.Count>0?idle.OrderBy(x=>x).ElementAt(idle.Count/2):0,fxMedian=during.Count>0?during.OrderBy(x=>x).ElementAt(during.Count/2):0;
            File.WriteAllText(Path.Combine(visualOutput,"performance-upgrade-fx.json"),ServerJson.Stringify(new Dictionary<string,object>{{"gpu",SystemInfo.graphicsDeviceName},{"resolution",Screen.width+"x"+Screen.height},{"idleFrameMs",Stats(idle)},{"transitionFrameMs",Stats(during)},{"transition","5 -> 6 full effects"}}));
            VisualCheck(fxMedian<=Math.Max(idleMedian*1.5,idleMedian+4),"transition frame time median "+fxMedian.ToString("0.0")+" ms vs idle "+idleMedian.ToString("0.0")+" ms");
            VisualCheck(Tag().level==6&&CleanVisual(root),"transition 5 -> 6 ends clean");

            // 5. Repeated updates: a newer level before the model change, then after it.
            Feed(7);yield return WaitFx(root,.5f);Feed(8);
            VisualCheck(SonMonUpgradeFx.IsPlaying(root.transform,out var retarget)&&retarget.ToLevel==8&&!retarget.Swapped,"newer level 8 during 6 -> 7 retargets before the model change");
            yield return FinishFx(root);VisualCheck(Tag().level==8&&CleanVisual(root),"retargeted transition ends on level 8");Same("retarget before change");
            Feed(9);yield return WaitFx(root,2.35f);Feed(10);yield return null;
            VisualCheck(SonMonUpgradeFx.IsPlaying(root.transform,out var late)&&late.ToLevel==10&&Tag().level==10,"newer level 10 after the 8 -> 9 model change swaps again at once");
            yield return FinishFx(root);VisualCheck(Tag().level==10&&CleanVisual(root),"repeated updates end on level 10 with one visual");Same("retarget after change");

            // 6. Cancellation: a lower level mid-transition stops it and snaps.
            Feed(3);yield return null;Feed(4);yield return WaitFx(root,1.0f);Feed(2);
            VisualCheck(!SonMonUpgradeFx.IsPlaying(root.transform,out _)&&Tag().level==2,"lower level mid-transition cancels it and snaps to level 2");
            yield return null;yield return null;VisualCheck(CleanVisual(root),"cancelled transition leaves no effect or glow material behind");Same("cancel");

            // 7. Restore the authoritative snapshot.
            spatial["buildings"]=original;DrawSpatialModels();yield return FinishFx(root);
            VisualCheck(models[id]==root&&Tag()!=null&&Tag().level==serverLevel&&CleanVisual(root),"restored snapshot returns to server level "+serverLevel);
            spatialPolling=false;spatialSimulation=false;
            Dictionary<string,object> fresh=null;yield return api.SpatialState(false,area=>fresh=area);
            var serverCopy=List(Get(fresh,"buildings")).Select(Map).FirstOrDefault(b=>S(Get(b,"id"))==id);
            VisualCheck(serverCopy!=null&&(int)N(Get(serverCopy,"level"))==serverLevel,"authoritative server level unchanged by the visual tests");
            yield return UpgradeUiChecks(root,serverLevel,"");
            physicalFocus=new Vector3(0,4,0);physicalZoom=21;SetSpatialCamera();
        }
        // Upgrade decision UI against the real server: MAX state and server-side rejection at the cap, or the
        // normal confirm state below it.
        IEnumerator UpgradeUiChecks(GameObject root,int serverLevel,string prefix)
        {
            int max=MaxBuildingLevel;ShowSpatialBuilding(LevelBuilding);yield return new WaitForSecondsRealtime(.3f);
            var state=dialog!=null?dialog.GetComponentsInChildren<Text>().FirstOrDefault(t=>t.name=="UpgradeState"):null;
            bool confirm=dialog!=null&&dialog.GetComponentsInChildren<Button>().Any(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text=="Xác nhận nâng cấp"));
            if(serverLevel>=max)
            {
                VisualCheck(state!=null&&state.text.Contains("CẤP TỐI ĐA")&&!confirm,"level "+serverLevel+" upgrade panel shows MAX LEVEL and offers no upgrade");
                VisualCapture(prefix+"upgrade-ui-max",1920,1080);CloseDialog();
                yield return AwaitVisualAction();double stones=N(Get(api.Snapshot,"sect","resources","linhThach"));
                api.Submit("start_upgrade",new Dictionary<string,object>{{"buildingId","tang_kinh_cac"}});yield return AwaitVisualAction();
                var after=Get(api.Snapshot,"sect","buildings","tang_kinh_cac");
                VisualCheck((int)N(Get(after,"level"))==serverLevel&&Get(after,"upgrading")==null&&N(Get(api.Snapshot,"sect","resources","linhThach"))>=stones,"server rejects "+serverLevel+" -> "+(serverLevel+1)+" and deducts nothing (notice: "+api.Notice+")");
                VisualCheck(!api.HasPending,"rejected upgrade request journal resolved");
            }
            else
            {
                VisualCheck(state!=null&&state.text.Contains("→ "+(serverLevel+1)+" / "+max)&&confirm,"level "+serverLevel+" upgrade panel shows the next level out of "+max+" and a confirm button");
                VisualCapture(prefix+"upgrade-ui-L"+serverLevel,1920,1080);CloseDialog();
            }
        }
        // -tier-live: the isolated fixture is seeded at level 9 with an upgrade finishing shortly after launch.
        // Nothing is simulated: the server clock completes the upgrade, the poll delivers level 10 and the
        // transition must play from that authoritative update.
        IEnumerator LiveUpgradeRegression()
        {
            string id=LevelBuilding;SonMonSky.PreviewPhase=.30f;SonMonSky.PreviewSeason=0;
            projectionActive=false;tab=0;Draw();CloseSpatialDetail();
            float limit=Time.realtimeSinceStartup+20;while(!models.ContainsKey(id)&&Time.realtimeSinceStartup<limit)yield return null;
            if(!models.TryGetValue(id,out var root)){VisualCheck(false,"scripture core building present");yield break;}
            yield return new WaitForSecondsRealtime(1.5f);
            VisualCheck(levels[id]==9&&root.GetComponent<SonMonUpgradeFx>()==null,"seeded save loads Tàng Kinh Các at level 9 without animation");
            VisualCheck(Get(api.Snapshot,"sect","buildings","tang_kinh_cac","upgrading")!=null,"server reports the 9 -> 10 upgrade in progress");
            ShowSpatialBuilding(id);yield return new WaitForSecondsRealtime(.3f);
            var state=dialog!=null?dialog.GetComponentsInChildren<Text>().FirstOrDefault(t=>t.name=="UpgradeState"):null;
            VisualCheck(state!=null&&state.text.Contains("Đang nâng cấp đến"),"upgrade panel shows the server upgrade in progress");VisualCapture("live-upgrade-ui-in-progress",1920,1080);CloseDialog();
            CaptureLevel(root,"live-L09-before");
            limit=Time.realtimeSinceStartup+240;while(levels[id]!=10&&Time.realtimeSinceStartup<limit){FrameLevel(root);yield return null;}
            VisualCheck(levels[id]==10,"authoritative server completes the upgrade to level 10");
            VisualCheck(SonMonUpgradeFx.IsPlaying(root.transform,out var fx)&&fx.FromLevel==9&&fx.ToLevel==10,"server-confirmed level 10 plays the 9 -> 10 transition");
            yield return CaptureFx(root,"live-fx-L9-L10");
            var tag=SonMonArt.VisualTag(root.transform);
            VisualCheck(models[id]==root&&LevelMarkers()==1&&tag!=null&&tag.level==10&&CleanVisual(root),"live transition ends on level 10, same GameObject, clean");
            VisualCheck(status.text.Contains("Cấp 10"),"UI announces level 10 after the transition: "+status.text);
            yield return AwaitVisualAction();
            yield return UpgradeUiChecks(root,10,"live-");
            SonMonSky.PreviewPhase=null;SonMonSky.PreviewSeason=null;
        }
        // Visual world scale (SonMonWorldScale). Structure checks always run; size checks only when the rollout flag is on.
        IEnumerator WorkerVisualChecks()
        {
            var actor=spatialActors.Values.First();var visual=actor.transform.Find("Visual");
            VisualCheck(visual!=null&&actor.transform.localScale==Vector3.one,"worker geometry lives under a dedicated Visual child; root transform unscaled");
            var legs=actor.GetComponentsInChildren<Transform>().Where(t=>t.name=="Leg").ToArray();
            VisualCheck(legs.Length==2&&legs.All(l=>l.IsChildOf(visual)),"worker legs are inside the Visual child and found by ActorMotion");
            if(!SonMonWorldScale.Enabled)yield break;
            float h=SonMonWorldScale.RenderedHeight(visual),target=SonMonWorldScale.Settings.characterHeight;
            VisualCheck(Mathf.Abs(h-target)<.05f,"worker renders at "+h.ToString("0.00")+" (target "+target+")");
            float swing=0;for(int i=0;i<30;i++){yield return null;swing=Mathf.Max(swing,legs.Max(l=>Quaternion.Angle(l.localRotation,Quaternion.identity)));}
            VisualCheck(swing>2,"worker walk cycle still animates the legs at display scale (max swing "+swing.ToString("0")+"°)");
            VisualCheck(Mathf.Abs(VisualMinY(visual)-actor.transform.position.y)<.08f,"worker feet stay grounded at display scale");
            var focus=physicalFocus;float zoom=physicalZoom;
            foreach(int z in new[]{10,21,30}){physicalFocus=new Vector3(actor.transform.position.x,2,actor.transform.position.z);physicalZoom=z;SetSpatialCamera();yield return new WaitForSecondsRealtime(.3f);physicalFocus=new Vector3(actor.transform.position.x,2,actor.transform.position.z);physicalZoom=z;SetSpatialCamera();VisualCapture("worldscale-worker-zoom"+z,1920,1080);}
            physicalFocus=focus;physicalZoom=zoom;SetSpatialCamera();
        }
        static float VisualMinY(Transform visual){float min=float.MaxValue;foreach(var r in visual.GetComponentsInChildren<Renderer>())if(r.enabled)min=Mathf.Min(min,r.bounds.min.y);return min;}
        IEnumerator WorldScaleRegression()
        {
            var idle=GameObject.Find("SonMon Disciple (idle)");var walker=GameObject.Find("SonMon Disciple (walking)");
            foreach(var d in new[]{idle,walker})
            {
                if(d==null){VisualCheck(false,"environment disciple present");yield break;}
                var cv=d.GetComponent<SonMonCharacterVisual>();
                VisualCheck(cv!=null&&cv.visual!=null&&cv.visual.parent==d.transform&&d.transform.localScale==Vector3.one,d.name+": display scale targets its single visual child; root scale 1");
                if(!SonMonWorldScale.Enabled)continue;
                float h=SonMonWorldScale.RenderedHeight(cv.visual),target=SonMonWorldScale.Settings.characterHeight;
                VisualCheck(Mathf.Abs(h-target)<.05f,d.name+" renders at "+h.ToString("0.00")+" (target "+target+")");
                VisualCheck(Mathf.Abs(VisualMinY(cv.visual)-d.transform.position.y)<.08f,d.name+" feet grounded (min "+VisualMinY(cv.visual).ToString("0.00")+" vs root "+d.transform.position.y.ToString("0.00")+")");
            }
            if(!SonMonWorldScale.Enabled)yield break;
            var start=walker.transform.position;float lowest=float.MaxValue,highest=float.MinValue;
            for(float t=0;t<2.5f;t+=Time.unscaledDeltaTime){yield return null;float g=VisualMinY(walker.GetComponent<SonMonCharacterVisual>().visual)-walker.transform.position.y;lowest=Mathf.Min(lowest,g);highest=Mathf.Max(highest,g);}
            VisualCheck(Vector3.Distance(start,walker.transform.position)>.1f,"walking disciple still moves along its route (movement unchanged)");
            VisualCheck(lowest>-.08f&&highest<.12f,"walking disciple stays grounded during the walk cycle ("+lowest.ToString("0.00")+".."+highest.ToString("0.00")+")");
            // Scripture Pavilion normalised to the standard door height.
            if(models.TryGetValue(LevelBuilding,out var root)&&SonMonArt.VisualTag(root.transform) is SonMonVisualTag tag&&SonMonArt.LocalVisualBounds(root.transform,tag.transform,out var b))
            {
                float expected=tag.modelLevel==1?1.72f:tag.modelLevel==5?3.63f:0;
                VisualCheck(expected==0||Mathf.Abs(b.size.y-expected)<.08f,"Tàng Kinh Các level "+tag.level+" model normalised to door 0.52: "+b.size.x.ToString("0.00")+" x "+b.size.z.ToString("0.00")+" x "+b.size.y.ToString("0.00"));
                VisualCheck(Mathf.Abs(root.GetComponent<BoxCollider>().size.y-b.size.y)<.15f,"selection collider refits to the normalised building ("+root.GetComponent<BoxCollider>().size.y.ToString("0.00")+" vs model "+b.size.y.ToString("0.00")+")");
            }
            var focus=physicalFocus;float zoom=physicalZoom;
            foreach(var (name,at) in new[]{("scripture",models.TryGetValue(LevelBuilding,out var s)?s.transform.position:Vector3.zero),("disciples",idle.transform.position)})
                foreach(int z in new[]{10,21,30})
                {
                    physicalFocus=new Vector3(at.x,2,at.z);physicalZoom=z;SetSpatialCamera();yield return new WaitForSecondsRealtime(.3f);
                    physicalFocus=new Vector3(at.x,2,at.z);physicalZoom=z;SetSpatialCamera();VisualCapture("worldscale-"+name+"-zoom"+z,1920,1080);
                }
            physicalFocus=focus;physicalZoom=zoom;SetSpatialCamera();
        }
        static string HierarchyPath(Transform t)=>t.parent==null?t.name:HierarchyPath(t.parent)+"/"+t.name;
        // GraphicRaycaster ignores graphics that have not been rendered yet (depth -1), so a freshly rebuilt
        // screen cannot be clicked in the frame it was created. Wait until the button is drawn, as a player would.
        IEnumerator ClickVisualButton(Button button)
        {
            for(int frame=0;frame<30&&(button.targetGraphic==null||button.targetGraphic.depth<0);frame++)yield return null;
            Canvas.ForceUpdateCanvases();var point=RectTransformUtility.WorldToScreenPoint(null,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point};var hits=new List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
            bool reached=hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button;
            if(!reached)
            {
                var raycasters=FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
                var buttonCanvas=button.GetComponentInParent<Canvas>().rootCanvas;
                Debug.Log("VISUAL_QA_RAYCAST point="+point+" screen="+Screen.width+"x"+Screen.height+" displays="+Display.displays.Length+" relative="+Display.RelativeMouseAt(point)
                    +" inRect="+RectTransformUtility.RectangleContainsScreenPoint(button.GetComponent<RectTransform>(),point,null)
                    +" buttonCanvas="+buttonCanvas.name+" mode="+buttonCanvas.renderMode+" display="+buttonCanvas.targetDisplay+" eventSystems="+FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length
                    +" raycasters="+string.Join(",",raycasters.Select(r=>r.name+(r.IsActive()?":on":":off")))
                    +" hits="+string.Join(" | ",hits.Take(5).Select(h=>HierarchyPath(h.gameObject.transform))));
            }
            VisualCheck(reached,"pointer raycast reaches visible button: "+button.name);button.onClick.Invoke();
        }
        IEnumerator AwaitVisualAction(){while(api.Busy||api.IsRefreshing)yield return null;yield return api.Refresh();while(api.IsRefreshing)yield return null;}
        IEnumerator VisualInteractionRegression()
        {
            object pillReceipt=null;api.DecisionCompleted+=(action,result)=>{if(action=="use_sect_pill")pillReceipt=result;};
            // Exercise a real player assignment the reference rules allow (luyện đan / rèn round-trip preferred, else thu hồi).
            yield return AwaitVisualAction();projectionActive=false;tab=1;Draw();
            var people=List(Get(api.Snapshot,"disciples")).Where(Alive).ToList();
            var disciple=people.Where(d=>SliceJobAction(d)!=null).OrderBy(d=>SliceJobAction(d).Value.status=="idle"?1:0).FirstOrDefault();
            VisualCheck(disciple!=null,"QA fixture has a disciple the reference rules allow to be assigned");
            string id=S(Get(disciple,"id")),oldStatus=S(Get(disciple,"status")),school=S(Get(disciple,"school")),jobTarget=SliceJobAction(disciple).Value.status,buttonName="AssignJob:"+id+":"+jobTarget;
            page=people.IndexOf(disciple)/4;Draw();
            Button jobButton=null;for(int frame=0;frame<60;frame++){jobButton=body.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==buttonName);if(jobButton!=null&&jobButton.targetGraphic!=null&&jobButton.targetGraphic.depth>=0)break;yield return null;}
            VisualCheck(jobButton!=null,"disciple card shows the allowed assignment button: "+buttonName);
            yield return ClickVisualButton(jobButton);yield return AwaitVisualAction();CloseDialog();
            string newStatus=S(Get(List(Get(api.Snapshot,"disciples")).First(d=>S(Get(d,"id"))==id),"status"));
            if(newStatus!=jobTarget)Debug.Log("VISUAL_QA_ASSIGN id="+id+" old="+oldStatus+" jobTarget="+jobTarget+" new="+newStatus+" notice="+api.Notice+" pending="+api.HasPending);
            VisualCheck(newStatus==jobTarget,"real disciple card button applies authoritative assignment "+oldStatus+" -> "+jobTarget);VisualCheck(!api.HasPending,"assignment request journal resolved");
            if(CanAssignJob(newStatus,oldStatus,school)){SubmitJob(id,oldStatus);yield return AwaitVisualAction();CloseDialog();}
            yield return api.Presentation("daidien",new Dictionary<string,object>{{"daiDienTab","baokho"}},null,data=>{projection=data;projectionActive=true;projectionView="daidien";projectionUi=new Dictionary<string,object>{{"daiDienTab","baokho"}};inventoryCategory="all";DrawProjection();});
            var inventory=List(Get(inventoryData,"groups")).SelectMany(g=>List(Get(g,"items"))).ToList();foreach(var item in inventory){string itemId=S(Get(item,"id"));inventorySelection=itemId;DrawSliceInventory();VisualCheck(body.GetComponentsInChildren<Text>().Any(t=>t.text==S(Get(item,"name"))),"selected treasure displays canonical name: "+itemId);}
            inventorySelection="tu_khi_dan";DrawSliceInventory();double beforeQty=N(Get(api.Snapshot,"sect","inventory","tu_khi_dan"));double beforeExp=N(Get(List(Get(api.Snapshot,"disciples")).First(d=>S(Get(d,"id"))==id),"exp"));
            yield return ClickVisualButton(FindSliceButton(body,"Dùng tăng tu vi"));VisualCheck(dialog!=null,"treasure action opens native recipient picker");var personButton=dialog.GetComponentsInChildren<Button>().First(b=>b.gameObject.name=="PillRecipient:"+id);yield return ClickVisualButton(personButton);yield return ClickVisualButton(FindSliceButton(dialog,"Xác nhận sử dụng"));yield return AwaitVisualAction();CloseDialog();
            File.WriteAllText(Path.Combine(visualOutput,"pill-receipt.json"),ServerJson.Stringify(new Dictionary<string,object>{{"expectedDiscipleId",id},{"receipt",pillReceipt},{"beforeQuantity",beforeQty},{"afterQuantity",Get(api.Snapshot,"sect","inventory","tu_khi_dan")}}));
            VisualCheck(N(Get(api.Snapshot,"sect","inventory","tu_khi_dan"))==beforeQty-1,"native Bao Kho use consumes exactly one real pill");VisualCheck(S(Get(pillReceipt,"discipleId"))==id&&N(Get(pillReceipt,"expGain"))==40&&Math.Abs(N(Get(pillReceipt,"expAfter"))-N(Get(pillReceipt,"expBefore"))-40)<.000001,"native Bao Kho authoritative receipt grants forty EXP to the exact selected disciple id");VisualCheck(!api.HasPending,"pill request journal resolved");
            projectionActive=false;tab=0;Draw();CloseSpatialDetail();double oldWood=N(Get(spatial,"wood"));int oldCount=List(Get(spatial,"buildings")).Count;
            BeginSpatialPlace("house");Vector3 focus=physicalFocus;physicalFocus+=new Vector3(2,0,1);SetSpatialCamera();VisualCheck(cam.transform.position!=Vector3.zero,"camera pan and zoom recompute playable world view");physicalFocus=focus;SetSpatialCamera();yield return ClickVisualButton(FindSliceButton(placementControls,"XÁC NHẬN XÂY"));while(api.Busy)yield return null;yield return api.SpatialState(false,ApplySpatial);
            VisualCheck(List(Get(spatial,"buildings")).Count==oldCount+1&&N(Get(spatial,"wood"))==oldWood-40,"native placement confirmation creates one house and debits exact wood");var house=List(Get(spatial,"buildings")).Last(b=>S(Get(b,"type"))=="house");api.Submit("spatial_demolish",new Dictionary<string,object>{{"buildingId",Get(house,"id")}});while(api.Busy)yield return null;yield return api.SpatialState(false,ApplySpatial);
            VisualCheck(List(Get(spatial,"buildings")).Count==oldCount&&N(Get(spatial,"wood"))==oldWood-8,"cancelled QA construction restores original building count and eighty percent materials");
            var coreModel=models["core:chanh_dien"];var collider=coreModel.GetComponent<BoxCollider>();var target=collider.bounds.center+Vector3.up*collider.bounds.extents.y*.5f;VisualCheck(Physics.Raycast(cam.ScreenPointToRay(cam.WorldToScreenPoint(target)),out var hit)&&hit.collider==collider,"3D roof and building geometry are covered by selectable bounds");
            var tree=List(Get(spatial,"nodes")).First(n=>S(Get(n,"kind"))=="tree"&&N(Get(n,"amount"))>0);ShowSpatialNode(S(Get(tree,"id")));yield return ClickVisualButton(FindSliceButton(spatialPanel,"CỬ NGƯỜI KHAI THÁC"));while(api.Busy)yield return null;yield return api.SpatialState(false,ApplySpatial);VisualCheck(spatialActors.Count>0,"harvest interaction creates authoritative animated 3D worker");yield return new WaitForSecondsRealtime(2);VisualCapture("worker-interaction",1920,1080);
            yield return WorkerVisualChecks();yield return ClickVisualButton(FindSliceButton(spatialPanel,"HỦY CHUYẾN"));while(api.Busy)yield return null;yield return api.SpatialState(false,ApplySpatial);VisualCheck(List(Get(spatial,"jobs")).Count==0,"harvest cancellation releases QA worker");
        }
        IEnumerator ProfileVisual(string area,int width,int height)
        {
            var cv=canvas.GetComponentInParent<Canvas>();var previousMode=cv.renderMode;bool cameraEnabled=cam.enabled;var target=new RenderTexture(width,height,24){antiAliasing=2};target.Create();cam.enabled=false;cam.targetTexture=target;cv.renderMode=RenderMode.ScreenSpaceCamera;cv.worldCamera=cam;cv.planeDistance=1;Canvas.ForceUpdateCanvases();
            var frames=new List<double>();var cpu=new List<double>();var gpu=new List<double>();var batches=new List<double>();var triangles=new List<double>();
            var batch=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");var tri=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");var timings=new FrameTiming[1];
            yield return new WaitForSecondsRealtime(2);double end=Time.realtimeSinceStartupAsDouble+8;double previous=Time.realtimeSinceStartupAsDouble;
            while(Time.realtimeSinceStartupAsDouble<end){FrameTimingManager.CaptureFrameTimings();cam.Render();yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add((now-previous)*1000);previous=now;
                if(FrameTimingManager.GetLatestTimings(1,timings)>0){if(timings[0].cpuFrameTime>0)cpu.Add(timings[0].cpuFrameTime);if(timings[0].gpuFrameTime>0)gpu.Add(timings[0].gpuFrameTime);}
                if(batch.Valid)batches.Add(batch.LastValue);if(tri.Valid)triangles.Add(tri.LastValue);
            }
            batch.Dispose();tri.Dispose();
            cam.targetTexture=null;cam.enabled=cameraEnabled;cv.renderMode=previousMode;target.Release();Destroy(target);
            Func<List<double>,object> stats=values=>{values.Sort();return new Dictionary<string,object>{{"samples",values.Count},{"median",values.Count>0?values[values.Count/2]:(object)null},{"p95",values.Count>0?values[Math.Min(values.Count-1,(int)(values.Count*.95))]:(object)null}};};
            var result=new Dictionary<string,object>{{"width",Screen.width},{"height",Screen.height},{"requestedWidth",width},{"gpu",SystemInfo.graphicsDeviceName},{"graphicsAPI",SystemInfo.graphicsDeviceType.ToString()},{"vramMB",SystemInfo.graphicsMemorySize},{"pipeline",UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline==null?"Built-in":UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name},{"renderMethod","Explicit complete scene + Canvas render to 2x MSAA texture per frame; no occluded-window idle samples"},{"wallFrameMs",stats(frames)},{"cpuFrameMs",stats(cpu)},{"gpuFrameMs",stats(gpu)},{"drawCalls",stats(batches)},{"triangles",stats(triangles)},{"unityAllocatedMB",Profiler.GetTotalAllocatedMemoryLong()/1048576d},{"managedMB",GC.GetTotalMemory(false)/1048576d},{"enabledVisibleRenderers",UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&r.isVisible)},{"developmentPlayer",Debug.isDebugBuild},{"vsync",QualitySettings.vSyncCount},{"frameCap",Application.targetFrameRate}};
            File.WriteAllText(Path.Combine(visualOutput,"performance-"+area+"-"+width+".json"),ServerJson.Stringify(result));
        }
    }
}

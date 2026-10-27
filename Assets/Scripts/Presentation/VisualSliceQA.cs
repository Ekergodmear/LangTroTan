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
            var tree=List(Get(spatial,"nodes")).First(n=>S(Get(n,"kind"))=="tree"&&N(Get(n,"amount"))>0);ShowSpatialNode(S(Get(tree,"id")));yield return ClickVisualButton(FindSliceButton(spatialPanel,"CỬ NGƯỜI KHAI THÁC"));while(api.Busy)yield return null;yield return api.SpatialState(false,ApplySpatial);VisualCheck(spatialActors.Count>0,"harvest interaction creates authoritative animated 3D worker");yield return new WaitForSecondsRealtime(2);VisualCapture("worker-interaction",1920,1080);yield return ClickVisualButton(FindSliceButton(spatialPanel,"HỦY CHUYẾN"));while(api.Busy)yield return null;yield return api.SpatialState(false,ApplySpatial);VisualCheck(List(Get(spatial,"jobs")).Count==0,"harvest cancellation releases QA worker");
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

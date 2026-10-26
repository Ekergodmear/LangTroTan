using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LangTroTan.Infrastructure;

namespace LangTroTan.Presentation
{
    public sealed partial class FullPortView
    {
        bool projectionActive;string projectionView="daidien";int projectionPage,projectionRequest;
        Dictionary<string,object> projectionUi=new Dictionary<string,object>(),projectionParameters=new Dictionary<string,object>(),projection;
        Dictionary<string,object> artMap;
        int projectionSelectPage;
        readonly Dictionary<string,Sprite> projectionSprites=new Dictionary<string,Sprite>();
        static readonly string[] hallIds={"chinhvu","tongphong","tauchuong","hoidong","giapha","lanhtho","ngoaigiao","quocgia","thiencobao","khivan","thienmenh","biennienthu","thienhaky","baokho"};
        static readonly string[] hallTitles={"Chính Vụ","Tông Phong","Tấu Chương","Hội Đồng Trưởng Lão","Gia Phả Truyền Thừa","Lãnh Thổ","Ngoại Giao","Quốc Gia","Thiên Cơ Báo","Khí Vận","Thiên Mệnh","Biên Niên Sử","Thiên Hạ Ký","Bảo Khố"};
        void ShowHallSections()
        {
            if(api?.Snapshot==null){status.text="Cần kết nối thế giới trước.";return;}
            CloseSpatialDetail();CancelSpatialPlace();projectionActive=false;commandScreen=false;CloseDialog();Clear(body);body.gameObject.SetActive(true);Label(body,"Đại Điện · Nghị sự tông môn",22,15,1240,43,27,true);
            for(int i=0;i<hallIds.Length;i++){int index=i;Button(body,hallTitles[i],22+i%3*433,80+i/3*108,416,91,()=>ShowProjection("daidien",new Dictionary<string,object>{{"daiDienTab",hallIds[index]}}));}
        }
        void ShowProjection(string view,Dictionary<string,object> ui=null,Dictionary<string,object> parameters=null)
        {
            if(api?.Snapshot==null){status.text="Cần kết nối thế giới trước.";return;}
            CloseDialog();commandScreen=false;projectionActive=true;projectionView=view;projectionUi=ui??new Dictionary<string,object>();projectionParameters=parameters??new Dictionary<string,object>();
            if(view=="disciple"&&projectionParameters.TryGetValue("discipleId",out var person))projectionUi["selectedDiscipleId"]=person;
            projectionUi["readNotifications"]=LoadReadNotifications();projectionPage=0;ReloadProjection();
        }
        void ReloadProjection()
        {
            int sequence=++projectionRequest;
            StartCoroutine(api.Presentation(projectionView,projectionUi,projectionParameters,result=>{if(sequence!=projectionRequest||!projectionActive)return;projection=result;DrawProjection();}));
        }
        float NodeHeight(object node)
        {
            string kind=S(Get(node,"type"));if(kind=="image")return 205;if(kind=="button"||kind=="input"||kind=="select")return 54;if(kind=="heading")return 48;
            var generator=new TextGenerator();var settings=new TextGenerationSettings{font=font,fontSize=17,lineSpacing=1,fontStyle=FontStyle.Normal,richText=false,scaleFactor=1,generationExtents=new Vector2(1260,0),verticalOverflow=VerticalWrapMode.Overflow,horizontalOverflow=HorizontalWrapMode.Wrap};
            return Mathf.Max(32,generator.GetPreferredHeight(S(Get(node,"text")),settings)+14);
        }
        void DrawProjection()
        {
            if(!projectionActive||projection==null)return;
            if(SliceEnabled&&projectionView=="daidien"&&S(Get(projectionUi,"daiDienTab"))=="baokho"&&Get(projection,"specialized")!=null){DrawSliceInventory();RefreshHeader();return;}
            DefaultBodyFrame();body.GetComponent<Image>().color=ink;CloseSpatialDetail();CancelSpatialPlace();Clear(body);body.gameObject.SetActive(true);RefreshHeader();
            string name=projectionView=="daidien"?hallTitles[Mathf.Max(0,Array.IndexOf(hallIds,S(Get(projectionUi,"daiDienTab"))))]:"Hồ sơ tông môn";
            Label(body,name+" · Ngày "+S(Get(projection,"day")),22,13,1250,42,27,true);
            var nodes=List(Get(projection,"nodes")).Where(n=>S(Get(n,"data","act"))!="daidien-tab").ToList();
            var pages=new List<List<object>>();var heights=new List<float>();float total=0;var current=new List<object>();
            foreach(var node in nodes){float height=NodeHeight(node);if(total+height>496&&current.Count>0){pages.Add(current);current=new List<object>();total=0;}current.Add(node);total+=height+7;}if(current.Count>0)pages.Add(current);
            if(pages.Count==0)pages.Add(new List<object>());projectionPage=Mathf.Clamp(projectionPage,0,pages.Count-1);float y=74;
            foreach(var node in pages[projectionPage])
            {
                var item=node;string kind=S(Get(node,"type")),text=S(Get(node,"text"),"");float height=NodeHeight(node);
                if(kind=="heading")Label(body,text,26,y,1270,height,22,true);
                else if(kind=="button")Button(body,text,25,y,1280,height,()=>ProjectionIntent(item)).interactable=!(Get(node,"disabled") is bool disabled&&disabled);
                else if(kind=="image")DrawProjectionImage(S(Get(node,"src")),text,y,height);
                else if(kind=="select")Button(body,text+" · CHỌN",25,y,1280,height,()=>{projectionSelectPage=0;ProjectionSelect(item);});
                else if(kind=="input")
                {
                    if(S(Get(node,"inputType"))=="checkbox")Button(body,"CHỌN / BỎ CHỌN ĐỆ TỬ",25,y,1280,height,()=>ProjectionIntent(item));
                    else {var input=Input(body,S(Get(node,"value"),""),25,y,1280);input.onEndEdit.AddListener(value=>ProjectionInput(item,value));}
                }
                else Label(body,text,26,y,1270,height,17);
                y+=height+7;
            }
            Button(body,"TRƯỚC",24,595,185,43,()=>{projectionPage--;DrawProjection();});Button(body,"SAU",222,595,185,43,()=>{projectionPage++;DrawProjection();});
            Label(body,"Trang "+(projectionPage+1)+" / "+pages.Count,427,604,395,33,16);Button(body,"CÁC MỤC ĐẠI ĐIỆN",897,595,407,43,ShowHallSections);
        }
        void DrawProjectionImage(string source,string alt,float y,float height)
        {
            if(artMap==null)artMap=ServerJson.Map(ServerJson.Parse(Resources.Load<TextAsset>("FullPort/ArtMap").text));
            if(!artMap.TryGetValue(source,out var key)){Label(body,alt,27,y,1270,Mathf.Min(70,height),18);return;}
            if(!projectionSprites.TryGetValue(source,out var sprite))
            {var texture=Resources.Load<Texture2D>(S(key));if(texture==null)return;sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f);projectionSprites[source]=sprite;}
            var box=Box(body,"Tranh minh họa",27,y,1270,height,Color.white);box.GetComponent<Image>().sprite=sprite;box.GetComponent<Image>().preserveAspect=true;box.GetComponent<Image>().raycastTarget=false;
        }
        void ProjectionSelect(object node)
        {
            OpenDialog("Lựa chọn",760,660);var options=List(Get(node,"options"));projectionSelectPage=Mathf.Clamp(projectionSelectPage,0,Mathf.Max(0,(options.Count-1)/7));int i=0;foreach(var option in options.Skip(projectionSelectPage*7).Take(7))
            {var item=option;Button(dialog,S(Get(item,"text")),25,81+i*61,710,52,()=>{ProjectionInput(node,S(Get(item,"value")));CloseDialog();});i++;}
            Button(dialog,"TRƯỚC",25,583,176,44,()=>{projectionSelectPage--;ProjectionSelect(node);});Button(dialog,"SAU",215,583,176,44,()=>{projectionSelectPage++;ProjectionSelect(node);});Button(dialog,"QUAY LẠI",405,583,330,44,CloseDialog);
        }
        void ProjectionInput(object node,string value)
        {
            var data=Map(Get(node,"data"));string act=S(Get(data,"act"));
            if(ExtraProjectionInput(data,act,value))return;
            if(act=="daidien-tab"){projectionUi["daiDienTab"]=value;projectionPage=0;ReloadProjection();return;}
            if(act=="disciple-filter"||data.ContainsKey("disciple-search"))
            {var filters=Map(Get(projectionUi,"discipleFilters"));string field=data.ContainsKey("disciple-search")?"query":S(Get(data,"filter"));filters[field]=value;projectionUi["discipleFilters"]=filters;projectionPage=0;ReloadProjection();return;}
            if(act=="chronicle-filter"||data.ContainsKey("chronicle-search"))
            {var filter=Map(Get(projectionUi,"chronicle"));filter[data.ContainsKey("chronicle-search")?"query":S(Get(data,"field"))]=value;filter["page"]=0L;projectionUi["chronicle"]=filter;projectionPage=0;ReloadProjection();return;}
            if(act=="set-policy"){OpenCommand(Map(Commands.First(c=>S(Get(c,"id"))=="set_policy")));commandPayload["policyId"]=value;DrawCommand();return;}
            status.text="Đã chọn: "+value+". Mở Quyết nghị để xem xét và xác nhận thay đổi.";
        }
        void ProjectionIntent(object node)
        {
            var data=Map(Get(node,"data"));string act=S(Get(data,"act")),id=S(Get(data,"id"),"");
            if(ExtraProjectionIntent(node,data,act,id))return;
            if(act=="daidien-tab"){ShowProjection("daidien",new Dictionary<string,object>{{"daiDienTab",id}});return;}
            if(act=="ops-home"){ShowProjection("mountain");return;}
            if(act=="ops-tab"){ShowProjection("operations",new Dictionary<string,object>{{"operationsTab",id}});return;}
            if(act=="ops-select"){ShowUpgrade(id);return;}
            if(act=="ops-refresh"||act=="refresh-snapshot"){StartCoroutine(api.Refresh());ReloadProjection();return;}
            if(act=="set-view"){ShowProjection(id);return;}
            if(act=="open-panel"||act=="open-modal"){ShowProjection("panel",null,new Dictionary<string,object>{{"type",id}});return;}
            if(act=="gov-section"){ShowProjection("governance",new Dictionary<string,object>{{"governanceSection",id}});return;}
            if(act=="governance-legacy"){ShowHallSections();return;}
            if(act=="gov-select"){projectionUi["governanceSelection"]=new Dictionary<string,object>{{"kind",Get(data,"kind")},{"id",id}};ShowProjection("governanceContext",projectionUi);return;}
            if(act=="world-select"){projectionUi["worldSelection"]=new Dictionary<string,object>{{"kind",Get(data,"kind")},{"id",id}};ShowProjection("worldContext",projectionUi);return;}
            if(act=="world-clear"){projectionUi["worldSelection"]=null;ShowProjection("world",projectionUi);return;}
            if(act=="world-tab"){ShowProjection("panel",new Dictionary<string,object>{{"worldSection",id}},new Dictionary<string,object>{{"type","worldExperience"}});return;}
            if(act=="open-decisions"){ShowProjection("daidien",new Dictionary<string,object>{{"daiDienTab","tauchuong"}});return;}
            if(act=="open-disciple"||act=="disciple-detail"){ShowProjection("disciple",null,new Dictionary<string,object>{{"discipleId",id}});return;}
            if(act=="chronicle-tab"||act=="chronicle-page"||act=="chronicle-select"||act=="chronicle-follow"||act=="chronicle-unfollow")
            {
                var filter=Map(Get(projectionUi,"chronicle"));
                if(act=="chronicle-tab"){filter["tab"]=id;filter["category"]="all";filter["page"]=0L;}
                if(act=="chronicle-page")filter["page"]=long.TryParse(id,out long n)?n:0L;
                if(act=="chronicle-select")filter["selected"]=Uri.UnescapeDataString(S(Get(data,"key"),""));
                if(act=="chronicle-follow"){filter["entity"]=Uri.UnescapeDataString(S(Get(data,"key"),""));filter["tab"]="timeline";filter["page"]=0L;}
                if(act=="chronicle-unfollow"){filter["entity"]="";filter["selected"]="";}
                projectionUi["chronicle"]=filter;ShowProjection(act=="chronicle-select"?"chronicleContext":"chronicle",projectionUi);return;
            }
            if(act=="chronicle-character"){ShowProjection("disciple",new Dictionary<string,object>{{"discipleDetailTab","history"}},new Dictionary<string,object>{{"discipleId",id}});return;}
            if(act=="chronicle-world"){ShowProjection("world",new Dictionary<string,object>{{"worldSelection",new Dictionary<string,object>{{"kind",Get(data,"kind")},{"id",id}}}});return;}
            if(act=="chronicle-return"){ShowProjection("chronicle",projectionUi);return;}
            if(act=="history-view"||act=="select-historian"||act=="select-tien-kieu-bang")
            {projectionUi[act=="history-view"?"historyView":act=="select-historian"?"selectedHistorianId":"tienKieuBangContinent"]=id;projectionPage=0;ReloadProjection();return;}
            if(act=="pill-use-pick"||act=="artifact-grant-pick")
            {string action=act=="pill-use-pick"?"use_sect_pill":"grant_sect_artifact";OpenCommand(Map(Commands.First(c=>S(Get(c,"id"))==action)));commandPayload["itemId"]=id;DrawCommand();return;}
            if(act=="ops-preview")
            {
                string kind=S(Get(data,"kind"));string action=kind=="building"?"start_upgrade":kind=="policy"?"set_policy":kind=="strategy"?"activate_grand_strategy":kind=="artifact"?"grant_sect_artifact":kind=="pill"?"use_sect_pill":"";
                var command=Commands.FirstOrDefault(c=>S(Get(c,"id"))==action);if(command!=null){OpenCommand(Map(command));Prefill(id,data);return;}
            }
            var targets=List(Get(node,"commands"));if(targets.Count==1)
            {string action=S(targets[0]);var command=Commands.FirstOrDefault(c=>S(Get(c,"id"))==action);if(command!=null){OpenCommand(Map(command));Prefill(id,data,Map(Get(node,"bindings")));return;}}
            if(act=="toggle-expedition-squad"||act=="toggle-bicanh-squad"||act=="toggle-combat-squad")
            {
                string field=act=="toggle-expedition-squad"?"expeditionSelections":act=="toggle-bicanh-squad"?"bicanhSelections":"combatSquad";
                if(field=="combatSquad"){var list=List(Get(projectionUi,field));if(list.Contains(id))list.Remove(id);else list.Add(id);projectionUi[field]=list;}
                else {var map=Map(Get(projectionUi,field));string group=S(Get(data,field=="expeditionSelections"?"continent":"bicanh"));var list=List(Get(map,group));if(list.Contains(id))list.Remove(id);else if(list.Count<5)list.Add(id);map[group]=list;projectionUi[field]=map;}ReloadProjection();return;
            }
            if(act=="close-modal"){CloseDialog();ShowHallSections();return;}
            if(act=="noop"||act=="toggle-context")return;
            ShowCommands();status.text="Chọn quyết nghị tương ứng để xem người, vật phẩm và phương án trước khi xác nhận.";
        }
        void Prefill(string id,Dictionary<string,object> data,Dictionary<string,object> bindings=null)
        {
            var fields=List(Get(activeCommand,"payloadFields")).Select(SafeString).ToArray();
            if(bindings!=null)
            {
                foreach(var field in bindings)
                {
                    string[] binding=S(field.Value).Split(':');object value=null;
                    if(binding.Length==2){if(binding[0]=="data")data.TryGetValue(binding[1],out value);else if(binding[0]=="ui")projectionUi.TryGetValue(binding[1],out value);}
                    if(value!=null&&fields.Contains(field.Key))commandPayload[field.Key]=value;
                }
            }
            else {var idFields=fields.Where(f=>f.EndsWith("Id")).ToList();if(idFields.Count==1&&!string.IsNullOrEmpty(id))commandPayload[idFields[0]]=id;}
            if(data.TryGetValue("item",out var item))commandPayload["itemId"]=item;
            if(data.TryGetValue("pills",out var pills)&&long.TryParse(S(pills),out long n))commandPayload["pillsUsed"]=n;
            if(data.TryGetValue("decision",out var decision)&&fields.Contains("decision"))commandPayload["decision"]=decision;
            DrawCommand();
        }
    }
}

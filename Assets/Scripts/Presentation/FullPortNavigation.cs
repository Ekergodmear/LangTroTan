using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LangTroTan.Infrastructure;

namespace LangTroTan.Presentation
{
    public sealed partial class FullPortView
    {
        string ReadNotificationsKey {get{return "TuTien.Unity.Read:"+api.ServerUrl+":"+api.GameId;}}
        List<object> LoadReadNotifications(){try{return List(ServerJson.Parse(PlayerPrefs.GetString(ReadNotificationsKey,"[]")));}catch{return new List<object>();}}
        void MarkRead(IEnumerable<object> ids)
        {var read=LoadReadNotifications().Concat(ids).Where(id=>id is string).Distinct().Reverse().Take(1000).Reverse().ToList();PlayerPrefs.SetString(ReadNotificationsKey,ServerJson.Stringify(read));projectionUi["readNotifications"]=read;}
        bool ExtraProjectionInput(Dictionary<string,object> data,string act,string value)
        {
            if(act=="mission-board-category"||act=="mission-board-sort"||data.ContainsKey("mission-board-search"))
            {var board=Map(Get(projectionUi,"missionBoard"));board[data.ContainsKey("mission-board-search")?"search":act=="mission-board-category"?"category":"sort"]=value;board["page"]=0L;projectionUi["missionBoard"]=board;projectionPage=0;ReloadProjection();return true;}
            return false;
        }
        bool ExtraProjectionIntent(object node,Dictionary<string,object> data,string act,string id)
        {
            switch(act)
            {
                case "toggle-nav":case "close-shell-drawers":return true;
                case "cancel-gameplay-preview":case "cancel-breakthrough":CloseDialog();ReloadProjection();return true;
                case "confirm-gameplay-preview":case "ops-confirm":ShowCommands();return true;
                case "artifact-grant-clear":projectionUi["artifactGrantItemId"]=null;ReloadProjection();return true;
                case "pill-use-clear":projectionUi["pillUseItemId"]=null;ReloadProjection();return true;
                case "open-events":ShowProjection("attention",new Dictionary<string,object>{{"attentionTab","events"}});return true;
                case "open-notifications":ShowProjection("attention",new Dictionary<string,object>{{"attentionTab","notifications"}});return true;
                case "attention-tab":projectionUi["attentionTab"]=id;ShowProjection("attention",projectionUi);return true;
                case "attention-select-decision":case "attention-select-and-review":
                    projectionUi["selectedDecision"]=new Dictionary<string,object>{{"kind",Get(data,"kind")},{"key",id}};
                    ShowProjection(act=="attention-select-and-review"?"attentionReview":"attentionContext",projectionUi);return true;
                case "attention-review":ShowProjection("attentionReview",projectionUi);return true;
                case "attention-select-event":case "attention-select-notification":
                    projectionUi["selectedEvent"]=id;if(act=="attention-select-notification")MarkRead(new object[]{id});ShowProjection("attentionContext",projectionUi);return true;
                case "attention-mark-all-read":MarkRead(List(Get(projection,"notifications")));ReloadProjection();return true;
                case "attention-open-chronicle":ShowProjection("chronicle",projectionUi);return true;
                case "open-log":ShowProjection("panel",null,new Dictionary<string,object>{{"type","log"}});return true;
                case "open-noimon-combat":ShowProjection("panel",new Dictionary<string,object>{{"mountainSubView","combat"}},new Dictionary<string,object>{{"type","noimon"}});return true;
                case "world-history":projectionUi["worldSelection"]=new Dictionary<string,object>{{"kind","history"},{"id",id}};ShowProjection("worldContext",projectionUi);return true;
                case "world-open-territory-detail":case "world-open-power-profile":projectionUi["worldSection"]=act=="world-open-territory-detail"?"territoryDetail":"powerProfile";ShowProjection("world",projectionUi);return true;
                case "mission-subtab":projectionUi["missionSubTab"]=id;ShowProjection("panel",projectionUi,new Dictionary<string,object>{{"type","nhiemvuduong"},{"subtab",id}});return true;
                case "mission-board-material":case "mission-board-page":
                    var board=Map(Get(projectionUi,"missionBoard"));
                    if(act=="mission-board-material"){board["materialOnly"]=id=="1";board["page"]=0L;}else board[S(Get(data,"section"))=="active"?"activePage":"page"]=long.TryParse(id,out long p)?Math.Max(0,p):0L;
                    projectionUi["missionBoard"]=board;projectionPage=0;ReloadProjection();return true;
                case "disciple-back":ShowProjection("disciples",projectionUi);return true;
                case "disciple-detail-tab":projectionUi["discipleDetailTab"]=id;projectionPage=0;ReloadProjection();return true;
                case "governance-decisions":ShowProjection("attention",new Dictionary<string,object>{{"attentionTab","decisions"}});return true;
                case "view-wiki-entity":
                    projectionUi["thienHaKyEntity"]=new Dictionary<string,object>{{"type",Get(data,"type")},{"key",Uri.UnescapeDataString(S(Get(data,"key"),""))},{"name",Uri.UnescapeDataString(S(Get(data,"name"),""))}};projectionPage=0;ReloadProjection();return true;
                case "close-wiki-entity":projectionUi["thienHaKyEntity"]=null;projectionPage=0;ReloadProjection();return true;
                case "open-wiki-entity-from-elsewhere":
                    string type=S(Get(data,"type"));var history=Map(Get(projectionUi,"chronicle"));history["entity"]=(type=="person"?"person":type=="family"?"family":"power")+":"+Uri.UnescapeDataString(S(Get(data,"key"),""));history["tab"]="timeline";history["page"]=0L;history["category"]="all";history["query"]="";projectionUi["chronicle"]=history;ShowProjection("chronicle",projectionUi);return true;
                case "view-leader-legacy":projectionUi["daiDienTab"]="thienhaky";projectionUi["leaderLegacyId"]=id;ShowProjection("daidien",projectionUi);return true;
                case "close-leader-legacy":projectionUi["leaderLegacyId"]=null;ReloadProjection();return true;
                case "view-legendary-story":projectionUi["legendaryStory"]=List(Get(api.Snapshot,"world","legendaryStories")).FirstOrDefault(s=>S(Get(s,"id"))==id);projectionPage=0;ReloadProjection();return true;
                case "close-legendary-story":projectionUi["legendaryStory"]=null;projectionPage=0;ReloadProjection();return true;
                case "preview-peak-demand":OpenCommand(Map(Commands.First(c=>S(Get(c,"id"))=="fulfill_peak_demand")));return true;
                case "wait-dai-khao-hach":ShowProjection("panel",null,new Dictionary<string,object>{{"type","ngoaimon"}});return true;
                case "breakthrough":case "prepare-breakthrough":case "set-breakthrough-pills":
                    OpenCommand(Map(Commands.First(c=>S(Get(c,"id"))=="attempt_breakthrough")));commandPayload["discipleId"]=id;
                    if(long.TryParse(S(Get(data,"pills")),out long pills))commandPayload["pillsUsed"]=Math.Max(0,Math.Min(3,pills));DrawCommand();return true;
                case "reset-game":ConfirmResetGame();return true;
            }
            return false;
        }
        void ConfirmResetGame()
        {
            OpenDialog("Xóa lượt chơi và khai sơn lại?");Label(dialog,"Toàn bộ tiến trình của tông môn hiện tại sẽ bị xóa trên máy chủ. Chỉ xác nhận nếu bạn muốn bắt đầu lại hoàn toàn.",25,92,630,180,21);
            Button(dialog,"XÓA VÀ BẮT ĐẦU LẠI",25,317,305,55,()=>{CloseDialog();api.ResetGame();});Button(dialog,"GIỮ LƯỢT HIỆN TẠI",347,317,308,55,CloseDialog);
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LangTroTan.Infrastructure;

namespace LangTroTan.Presentation
{
    public sealed partial class FullPortView
    {
        sealed class Choice {public object value;public string name;public Choice(object v,string n){value=v;name=n;}}
        Dictionary<string,object> activeCommand,commandPayload;
        List<object> commands;string commandGroup="Tất cả";int commandPage,pickerPage;string pickerSearch="";
        bool commandScreen;
        List<object> Commands {get{return commands??(commands=List(Get(ServerJson.Parse(Resources.Load<TextAsset>("FullPort/Commands").text),"commands")));}}
        void ShowCommands()
        {
            if(api?.Snapshot==null){status.text="Cần kết nối thế giới trước.";return;}CloseSpatialDetail();CancelSpatialPlace();projectionActive=false;commandScreen=true;CloseDialog();Clear(body);body.gameObject.SetActive(true);
            Label(body,"Quyết nghị · Điều hành tông môn",22,15,1250,42,27,true);
            var groups=new[]{"Tất cả"}.Concat(Commands.Select(c=>S(Get(c,"group"))).Distinct()).ToArray();
            for(int i=0;i<groups.Length;i++){string group=groups[i];Button(body,group,22+i%7*184,75+i/7*49,174,40,()=>{commandGroup=group;commandPage=0;ShowCommands();});}
            var rows=Commands.Where(c=>commandGroup=="Tất cả"||S(Get(c,"group"))==commandGroup).ToList();commandPage=Mathf.Clamp(commandPage,0,Mathf.Max(0,(rows.Count-1)/9));int index=0;
            foreach(var command in rows.Skip(commandPage*9).Take(9))
            {object item=command;Button(body,S(Get(command,"title")),22+index%3*433,236+index/3*99,417,84,()=>OpenCommand(Map(item)));index++;}
            Button(body,"TRANG TRƯỚC",22,576,230,46,()=>{commandPage--;ShowCommands();});Button(body,"TRANG SAU",270,576,230,46,()=>{commandPage++;ShowCommands();});Label(body,"Trang "+(commandPage+1)+" / "+Math.Max(1,(rows.Count+8)/9),539,584,620,35,17);
        }
        void OpenCommand(Dictionary<string,object> command)
        {
            if(S(Get(command,"id"))=="allocate_resources"){ShowBudget();return;}
            activeCommand=command;commandPayload=new Dictionary<string,object>();
            if(S(Get(command,"id"))=="fulfill_peak_demand")commandPayload["demandId"]="dao_alchemy_support_v1";
            DrawCommand();
        }
        void DrawCommand()
        {
            string id=S(Get(activeCommand,"id"));OpenDialog(S(Get(activeCommand,"title")),880,660);
            Label(dialog,CommandHint(id),25,78,830,90,19);
            var fields=List(Get(activeCommand,"payloadFields")).Select(SafeString).Where(f=>f!="demandId").ToArray();
            for(int i=0;i<fields.Length;i++)
            {
                string field=fields[i];int y=183+i*72;Label(dialog,FieldName(field),26,y+7,277,45,18,true);
                if(new[]{"count","slot","pillsUsed","durationYears"}.Contains(field))
                {
                    object current;commandPayload.TryGetValue(field,out current);var input=Input(dialog,current==null?(field=="count"?"1":field=="durationYears"?"": "0"):S(current),315,y,538);
                    input.onValueChanged.AddListener(text=>{if(string.IsNullOrWhiteSpace(text)&&field=="durationYears"){commandPayload.Remove(field);return;}if(long.TryParse(text,out long n))commandPayload[field]=n;else commandPayload.Remove(field);});
                    if(field!="durationYears"&&current==null)commandPayload[field]=field=="count"?1L:0L;
                }
                else
                {
                    bool chosen=commandPayload.TryGetValue(field,out var value);string label=chosen?ChoiceLabel(id,field,value):"CHỌN "+FieldName(field).ToUpperInvariant();
                    Button(dialog,label,315,y,538,47,()=>{pickerPage=0;pickerSearch="";ShowPicker(field);});
                }
            }
            Button(dialog,"XEM LẠI VÀ XÁC NHẬN",25,567,405,55,()=>
            {
                foreach(string field in fields)if(field!="durationYears"&&!commandPayload.ContainsKey(field)){status.text="Chưa chọn "+FieldName(field).ToLowerInvariant()+".";return;}
                if(commandPayload.TryGetValue("discipleIds",out var team)&&List(team).Count==0){status.text="Cần chọn ít nhất một đệ tử.";return;}
                var payload=new Dictionary<string,object>(commandPayload);string preview=string.Join("\n",fields.Where(payload.ContainsKey).Select(f=>FieldName(f)+": "+ChoiceLabel(id,f,payload[f])));
                ConfirmAction(S(Get(activeCommand,"title")),preview+"\n\n"+CommandHint(id),id,payload);
            });
            Button(dialog,"QUAY LẠI",450,567,405,55,ShowCommands);
        }
        static string SafeString(object value){return S(value);}
        static string FieldName(string field)
        {
            var names=new Dictionary<string,string>{{"buildingId","Công trình"},{"discipleId","Đệ tử"},{"discipleIds","Đội hình"},{"pillsUsed","Số Trúc Cơ Đan"},{"count","Số lượt thu đồ"},{"decision","Phán quyết"},{"priorityId","Ưu tiên"},{"type","Hình thức tôn vinh"},{"policyId","Thanh quy"},{"phongId","Phong thành lập"},{"workshop","Xưởng"},{"recipeId","Công thức"},{"side","Lập trường"},{"typeId","Hành hương"},{"status","Công việc"},{"itemId","Vật phẩm"},{"manualId","Công pháp"},{"rumorId","Tin đồn"},{"skillId","Kỹ năng"},{"slot","Ô kỹ năng (từ 0)"},{"bicanhId","Bí cảnh"},{"geniusId","Thiên tài"},{"actionId","Đường lối"},{"trackId","Truyền thừa"},{"continentId","Đại châu"},{"expeditionId","Đoàn thám hiểm"},{"aiSectId","Tông môn"},{"targetId","Thế lực Ngoại Vực"},{"groundId","Cấm địa"},{"candidateId","Người kế vị"},{"voterId","Người được vận động"},{"method","Phương án công thành"},{"outcomeId","Hậu chiến"},{"pocketWorldId","Tiểu thế giới"},{"strategyId","Đại Kế"},{"durationYears","Thời hạn (năm)"},{"choice","Đường lối kế vị"}};
            return names.TryGetValue(field,out string name)?name:"Lựa chọn";
        }
        string ChoiceLabel(string action,string field,object value)
        {
            if(value is List<object> list)return string.Join(", ",list.Select(v=>Options(action,"discipleId").FirstOrDefault(c=>Equals(c.value,v))?.name??"Đệ tử"));
            return Options(action,field).FirstOrDefault(c=>Equals(c.value,value))?.name??S(value);
        }
        string CommandHint(string action)
        {
            if(action.StartsWith("war_")||action=="rebellion_suppress")return "Quyết định quân sự có thể gây thương vong, mất đệ tử hoặc thay đổi lãnh thổ. Chọn đội hình và cân nhắc trước khi xác nhận.";
            if(action=="fulfill_peak_demand")return "Dành 6 Huyền Linh Thảo hỗ trợ Đan Phong. Chỉ có thể thực hiện khi kiến nghị đủ điều kiện, mỗi kiến nghị một lần.";
            if(action=="attempt_breakthrough")return "Đột phá có thể thất bại hoặc phát sinh tâm ma. Mỗi Trúc Cơ Đan cộng 15 điểm phần trăm, tối đa 3 viên.";
            if(action=="start_upgrade")return "Chi phí và thời gian nâng cấp tăng theo cấp hiện tại. Công trình đang thi công không nhận thêm lệnh nâng.";
            if(action=="grant_sect_artifact"||action=="grant_khaohach_item"||action=="use_sect_pill")return "Vật phẩm được lấy từ Bảo Khố. Ban thưởng và dùng đan sẽ tiêu hao tồn kho.";
            return "Chọn người và phương án phù hợp với tình hình tông môn. Quyết định chỉ có hiệu lực khi đủ điều kiện; kết quả sẽ được cập nhật vào thế giới.";
        }
        void ShowPicker(string field)
        {
            string action=S(Get(activeCommand,"id"));OpenDialog("Chọn "+FieldName(field).ToLowerInvariant(),970,710);
            var all=Options(action,field);var input=Input(dialog,pickerSearch,25,77,919);input.onEndEdit.AddListener(text=>{pickerSearch=text;pickerPage=0;ShowPicker(field);});
            var options=all.Where(c=>string.IsNullOrWhiteSpace(pickerSearch)||c.name.IndexOf(pickerSearch,StringComparison.OrdinalIgnoreCase)>=0).ToList();pickerPage=Mathf.Clamp(pickerPage,0,Mathf.Max(0,(options.Count-1)/7));int i=0;
            var selected=commandPayload.TryGetValue(field,out var existing)?List(existing):new List<object>();
            foreach(var option in options.Skip(pickerPage*7).Take(7))
            {
                var choice=option;bool multi=field=="discipleIds";bool enabled=selected.Any(v=>Equals(v,choice.value));
                Button(dialog,(multi?(enabled?"✓ ":"○ "):"")+choice.name,25,139+i*62,919,54,()=>
                {
                    if(multi){if(enabled)selected.RemoveAll(v=>Equals(v,choice.value));else selected.Add(choice.value);commandPayload[field]=selected;ShowPicker(field);}
                    else{commandPayload[field]=choice.value;DrawCommand();}
                });i++;
            }
            if(options.Count==0)Label(dialog,"Hiện chưa có đối tượng phù hợp để lựa chọn.",27,160,913,90,21);
            Button(dialog,"TRƯỚC",25,596,212,42,()=>{pickerPage--;ShowPicker(field);});Button(dialog,"SAU",251,596,212,42,()=>{pickerPage++;ShowPicker(field);});
            Button(dialog,field=="discipleIds"?"XONG · "+selected.Count+" NGƯỜI":"QUAY LẠI",477,596,467,42,DrawCommand);
        }
        List<Choice> Choices(object values)
        {
            var result=new List<Choice>();if(values is Dictionary<string,object> map){foreach(var p in map){var obj=Map(p.Value);result.Add(new Choice(Get(obj,"id")??p.Key,S(Get(obj,"name"),S(Get(obj,"title"),S(Get(obj,"label"),p.Key)))));}}
            else foreach(var v in List(values)){var obj=Map(v);if(obj.Count>0)result.Add(new Choice(Get(obj,"id")??Get(obj,"candidateId"),Summary(obj)));else result.Add(new Choice(v,S(v)));}
            return result.Where(c=>c.value!=null).ToList();
        }
        List<Choice> EnumChoices(params string[] pairs)
        {var result=new List<Choice>();foreach(var pair in pairs){int at=pair.IndexOf('|');result.Add(new Choice(at<0?pair:pair.Substring(0,at),at<0?pair:pair.Substring(at+1)));}return result;}
        List<Choice> Options(string action,string field)
        {
            object state=api.Snapshot;
            if(field=="discipleId"||field=="discipleIds"||field=="candidateId"||field=="voterId")return Choices(List(Get(state,"disciples")).Where(d=>action=="honor_ancestor"||Alive(d)).ToList());
            if(field=="buildingId")return Choices(Get(Module("sectBuildings"),"BUILDINGS"));
            if(field=="aiSectId")return Choices(Get(state,"world","aiSects"));
            if(field=="continentId")return Choices(Get(Module("worldMap"),"CONTINENTS"));
            if(field=="phongId")return Choices(Get(Module("phongCatalog"),"EXTRA_PHONG"));
            if(field=="priorityId")return Choices(Get(Module("missionTypes"),"MISSION_PRIORITIES"));
            if(field=="policyId")return action=="set_ngoaimon_policy"?Choices(Get(Module("ngoaiMonPolicy"),"NGOAI_MON_POLICIES")):Choices(Get(Module("sectPolicies"),"SECT_POLICIES"));
            if(field=="workshop")return EnumChoices("alchemy|Luyện Đan Đường","forge|Luyện Khí Điện");
            if(field=="recipeId"){var list=EnumChoices("auto|Tự cân đối tồn kho");list.AddRange(Choices(Get(Module("recipes"),"ALCHEMY_RECIPES")));list.AddRange(Choices(Get(Module("recipes"),"FORGE_RECIPES")));foreach(var c in list){var item=Get(Module("items"),"ITEMS",S(c.value));if(item!=null)c.name=S(Get(item,"name"),c.name);}return list;}
            if(field=="itemId")return Choices(Get(Module("items"),"ITEMS"));
            if(field=="manualId")return Choices(Get(state,"sect","manuals"));
            if(field=="rumorId")return Choices(Get(state,"sect","rumors"));
            if(field=="skillId")return Choices(Get(Module("skills"),"SKILLS"));
            if(field=="bicanhId")return Choices(Get(state,"world","bicanh"));
            if(field=="geniusId")return Choices(Get(state,"world","geniuses"));
            if(field=="trackId"){var list=new List<Choice>{new Choice(null,"Truyền thừa trường phái")};list.AddRange(Choices(Get(state,"world","legacies")));return list;}
            if(field=="typeId")return Choices(Get(Module("pilgrimage"),"PILGRIMAGE_TYPES"));
            if(field=="expeditionId")return Choices(Get(state,"world","expeditions"));
            if(field=="targetId")return Choices(List(Get(state,"world","aiSects")).Where(a=>Get(a,"ngoaiVuc") is bool yes&&yes&&Alive(a)).ToList());
            if(field=="groundId")return Choices(Get(Module("forbiddenGrounds"),"FORBIDDEN_GROUNDS"));
            if(field=="method")return EnumChoices("surround|Vây thành","assault|Cưỡng ép phá trận");
            if(field=="outcomeId")return Choices(Get(Module("diplomacyData"),"WAR_OUTCOMES"));
            if(field=="pocketWorldId")return Choices(Get(state,"world","pocketWorlds"));
            if(field=="strategyId")return Choices(Get(Module("grandStrategies"),"GRAND_STRATEGIES"));
            if(field=="status")return EnumChoices("training|Tu luyện","labor|Lao động","alchemy|Luyện đan","forge|Luyện khí","idle|Nhàn rỗi");
            if(field=="type")return EnumChoices("statue|Dựng tượng","to_su|Tôn làm Tổ sư");
            if(field=="side")return EnumChoices("proponent|Ủng hộ người đề xướng","opponent|Ủng hộ người phản đối","neutral|Trung lập");
            if(field=="choice")return EnumChoices("keep|Giữ đường lối tiền nhiệm","reform|Cải cách","reject|Bác bỏ đề xuất");
            if(field=="actionId")return action=="diplomacy_action"?EnumChoices("gift|Tặng lễ vật","trade|Giao thương","insult|Khiêu khích","declareWar|Ban bố diệt môn chiến"):EnumChoices("invite|Cử trưởng lão đón","bribe|Mang lễ vật trọng kim","marry|Gả đệ tử kết thân","alliance|Nhờ đồng minh tiến cử","kidnap|Bắt cóc cưỡng ép","legacy|Cho truyền thừa sớm","promise_rank|Hứa chức vị Chấp Sự");
            if(field=="decision")
            {
                switch(action)
                {
                    case "approve_petition":return EnumChoices("allow|Cho phép","escort|Cử người hộ tống","deny|Từ chối");
                    case "resolve_recipe_research":return EnumChoices("approve|Duyệt nghiên cứu","delay|Trì hoãn");
                    case "resolve_allocation_dispute":return EnumChoices("ignore|Bỏ qua","warn|Cảnh cáo","investigate|Điều tra");
                    case "resolve_war_captive":return EnumChoices("kill|Xử tử","release|Phóng thích","recruit|Thu nhận");
                    case "resolve_rumor":return EnumChoices("watch|Theo dõi","ignore|Bỏ qua","suppress|Trấn áp");
                    case "resolve_clique_event":return EnumChoices("support|Hỗ trợ","disband|Giải tán","watch|Theo dõi");
                    case "resolve_chan_truyen_challenge":return EnumChoices("accept|Cho phép thách đấu","reject|Bác bỏ","delay|Trì hoãn");
                    case "resolve_council_vote":return EnumChoices("proceed|Thi hành quyết nghị","withdraw|Rút đề xuất");
                    case "resolve_grand_strategy_moment":return EnumChoices("seize|Nắm bắt","negotiate|Thương lượng","investigate|Điều tra","ignore|Bỏ qua","mentor|Đích thân chỉ dạy","faction_manage|Giao Phong quản lý","autonomy|Cho tự quản","give_a|Trao bên thứ nhất","give_b|Trao bên thứ hai","contest|Tổ chức tranh tài","fund|Cấp ngân sách","approve|Chấp thuận","deny|Từ chối","resources|Viện trợ tài nguyên","troops|Viện trợ đệ tử","refuse|Khước từ");
                }
            }
            return new List<Choice>();
        }
    }
}

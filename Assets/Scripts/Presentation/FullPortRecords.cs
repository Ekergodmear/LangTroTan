using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LangTroTan.Presentation
{
    public sealed partial class FullPortView
    {
        sealed class RecordLocation {public object value;public string name;public int page;}
        readonly Stack<RecordLocation> recordHistory=new Stack<RecordLocation>();int recordPage;
        static readonly Sprite[] portraits=new Sprite[5];
        static readonly Dictionary<string,string> recordLabels=new Dictionary<string,string>
        {
            {"name","Tên"},{"day","Ngày"},{"text","Ghi chép"},{"message","Diễn biến"},{"title","Danh hiệu"},{"desc","Mô tả"},{"level","Cấp"},{"tier","Phẩm cấp"},
            {"linhThach","Linh thạch"},{"linhKhiMach","Linh khí mạch"},{"khiVan","Khí vận"},{"danhVong","Danh vọng"},{"resources","Nguồn lực"},{"inventory","Bảo khố"},
            {"school","Trường phái"},{"realm","Cảnh giới"},{"subLevel","Tiểu cảnh"},{"exp","Tu vi"},{"rarity","Tư chất"},{"stars","Số sao"},{"status","Công việc"},{"rank","Chức vị"},
            {"traits","Tính cách"},{"loyalty","Trung thành"},{"ambition","Tham vọng"},{"bravery","Dũng khí"},{"greed","Tham tài"},{"diligence","Chuyên cần"},{"compassion","Nhân từ"},
            {"alive","Còn tại thế"},{"fame","Danh vọng cá nhân"},{"achievements","Chiến công"},{"biography","Tiểu sử"},{"gender","Giới tính"},{"bornAt","Ngày nhập thế"},
            {"equippedSkills","Kỹ năng đã học"},{"unlockedSkillSlots","Số ô kỹ năng"},{"statModifiers","Hiệu ứng"},{"masterId","Sư phụ"},{"inheritance","Truyền thừa"},
            {"familyId","Gia tộc"},{"bloodlineId","Huyết mạch"},{"personalDestiny","Vận mệnh"},{"omen","Điềm báo"},{"karma","Nhân quả"},{"corruption","Tâm ma"},{"heartDemonTier","Tâm cảnh"},
            {"stability","Ổn định"},{"leaderId","Chưởng môn"},{"grandElderId","Đại Trưởng Lão"},{"heirId","Thiếu Tông Chủ"},{"factions","Ngũ Phong"},{"influence","Ảnh hưởng"},
            {"foundedPhong","Các Phong đã lập"},{"policy","Thanh quy"},{"ngoaiMonPolicy","Thanh quy Ngoại Môn"},{"currentGrandStrategy","Đại Kế hiện tại"},{"grandStrategyHistory","Lịch sử Đại Kế"},
            {"leaderHistory","Lịch sử Chưởng môn"},{"leaderBornDay","Ngày kế vị"},{"lineage","Đạo thống"},{"publicOpinion","Dư luận"},{"rumors","Tin đồn"},{"council","Hội đồng"},
            {"missions","Nhiệm vụ"},{"missionPriority","Ưu tiên nhiệm vụ"},{"dailyReport","Tấu chương"},{"manuals","Công pháp"},{"ngoaiMonFactions","Bè phái Ngoại Môn"},
            {"trueDisciples","Thân truyền đệ tử"},{"honoredAncestors","Tổ sư được tôn vinh"},{"destiny","Thiên mệnh"},{"destinyProgress","Tiến trình thiên mệnh"},{"destinyAwakened","Thiên mệnh thức tỉnh"},
            {"linhMachLevel","Cấp linh mạch"},{"ascendedCount","Số người phi thăng"},{"worldInfluence","Ảnh hưởng thiên hạ"},{"omens","Điềm báo"},{"pocketWorldLaws","Luật tiểu thế giới"},
            {"aiSects","Tông môn thiên hạ"},{"territories","Lãnh thổ"},{"bicanh","Bí cảnh"},{"expeditions","Viễn chinh"},{"geniuses","Thiên tài"},{"alliances","Liên minh"},
            {"amity","Thiện cảm"},{"army","Binh lực"},{"power","Sức mạnh"},{"personality","Tính cách"},{"war","Chiến tranh"},{"stage","Giai đoạn"},{"nodesCaptured","Lãnh thổ đã chiếm"},
            {"continentId","Đại châu"},{"ownerSectId","Tông môn sở hữu"},{"ownerAiSectId","Thế lực sở hữu"},{"leader","Người lãnh đạo"},{"population","Dân số"},{"wealth","Tài sản"},
            {"nations","Quốc gia"},{"families","Gia tộc"},{"members","Thành viên"},{"branches","Chi nhánh"},{"prestige","Uy tín"},{"founderId","Người khai sáng"},
            {"heavenlyWill","Thiên Ý"},{"eraId","Thời đại"},{"pastEras","Các thời đại đã qua"},{"threats","Mối đe dọa"},{"apocalypse","Đại kiếp tái sinh"},
            {"legacies","Di sản"},{"legaciesArchive","Di sản cổ"},{"legendaryStories","Truyền kỳ"},{"folklore","Giai thoại"},{"classics","Kinh điển"},{"historians","Sử quan"},
            {"sectRuins","Di tích tông môn"},{"activeRuins","Di tích đang khai quật"},{"pocketWorlds","Tiểu thế giới"},{"tienKieuBang","Thiên Kiêu Bảng"},
            {"daiHoiThienHaHistory","Lịch sử Đại Hội Thiên Hạ"},{"tournamentHistory","Lịch sử đại hội"},{"daiKiepHistory","Lịch sử Đại Kiếp"},{"thienTrieu","Thiên Triều"},
            {"worldProtagonist","Nhân vật thiên hạ"},{"thienMenhChiTu","Thiên Mệnh Chi Tử"},{"protagonist","Nhân vật nổi bật"},{"characterEra","Nhân vật thời đại"},
            {"amount","Số lượng"},{"quantity","Số lượng"},{"cost","Chi phí"},{"reward","Phần thưởng"},{"rewardLinhThach","Linh thạch thưởng"},{"difficulty","Độ khó"},
            {"endsAt","Kết thúc"},{"finishAt","Hoàn thành"},{"startedAt","Bắt đầu"},{"startDay","Ngày bắt đầu"},{"endDay","Ngày kết thúc"},{"sinceDay","Từ ngày"},
            {"result","Kết quả"},{"success","Thành công"},{"reason","Lý do"},{"history","Lịch sử"},{"origin","Nguồn gốc"},{"creatorName","Người sáng tạo"},
            {"acquiredDay","Ngày tiếp nhận"},{"teachingDiscipleId","Đệ tử đang lĩnh hội"},{"type","Loại"},{"id","Danh tính"},{"priority","Ưu tiên"},{"deadlineDay","Hạn chót"}
        };
        void Portrait(Transform parent,string school)
        {
            int index=Array.IndexOf(new[]{"kiem","dao","phat","nho","vo"},school);if(index<0)return;var texture=Resources.Load<Texture2D>("Art/Sect/Portrait"+index);if(texture==null)return;
            if(portraits[index]==null)portraits[index]=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f);
            var box=Box(parent,"Chân dung",12,10,76,96,Color.white);var image=box.GetComponent<Image>();image.sprite=portraits[index];image.preserveAspect=true;image.raycastTarget=false;
        }
        string DisplayLabel(string key)
        {
            if(recordLabels.TryGetValue(key,out string label))return label;
            foreach(var source in new[]{Get(Module("items"),"ITEMS"),Get(Module("sectFactions"),"FACTIONS"),Get(Module("sectBuildings"),"BUILDINGS")})
            {var obj=Get(source,key);if(obj!=null)return S(Get(obj,"name"),"Thông tin");}
            return "Thông tin";
        }
        string DisplayValue(string key,object value)
        {
            if(value==null)return "Chưa có";if(value is bool flag)return flag?"Có":"Không";
            if(value is List<object> list)return list.Count+" mục · chạm để xem";if(value is Dictionary<string,object> map)return map.Count+" mục · chạm để xem";
            if(key=="school")return S(Get(Module("schools"),"SCHOOLS",S(value),"name"));
            if(key=="realm"){var realm=List(Get(Module("realms"),"REALMS")).FirstOrDefault(r=>N(Get(r,"id"))==N(value));return S(Get(realm,"name"));}
            if(key=="status")return StatusName(S(value));if(key=="gender")return S(value)=="male"?"Nam":"Nữ";
            if(key.EndsWith("Id")){var person=List(Get(api.Snapshot,"disciples")).FirstOrDefault(d=>Equals(Get(d,"id"),value));if(person!=null)return S(Get(person,"name"));var sect=List(Get(api.Snapshot,"world","aiSects")).FirstOrDefault(d=>Equals(Get(d,"id"),value));if(sect!=null)return S(Get(sect,"name"));}
            if(key.EndsWith("At")&&N(value)>100000000000)return DateTimeOffset.FromUnixTimeMilliseconds((long)N(value)).ToLocalTime().ToString("dd/MM HH:mm");
            return S(value);
        }
        void DrawRecord(object value,string name)
        {
            OpenDialog(name,1080,730);var entries=new List<KeyValuePair<string,object>>();
            if(value is Dictionary<string,object> map)entries=map.Where(p=>!p.Key.EndsWith("Index")&&p.Key!="id").ToList();
            else if(value is List<object> list)entries=list.Select((v,i)=>new KeyValuePair<string,object>(Summary(v),v)).ToList();
            else entries.Add(new KeyValuePair<string,object>("text",value));
            recordPage=Mathf.Clamp(recordPage,0,Mathf.Max(0,(entries.Count-1)/8));int i=0;
            foreach(var entry in entries.Skip(recordPage*8).Take(8))
            {
                var item=entry;bool collection=value is List<object>;string label=collection?item.Key:DisplayLabel(item.Key);string text=DisplayValue(item.Key,item.Value);
                if(item.Value is Dictionary<string,object>||item.Value is List<object>)
                {
                    Button(dialog,label+" · "+text,24,79+i*66,1032,57,()=>{recordHistory.Push(new RecordLocation{value=value,name=name,page=recordPage});recordPage=0;DrawRecord(item.Value,label);});
                }
                else {Label(dialog,label,27,82+i*66,280,54,17,true);Label(dialog,text,321,82+i*66,728,54,17);}
                i++;
            }
            if(entries.Count==0)Label(dialog,"Chưa có ghi chép ở thời điểm này.",26,90,1020,90,21);
            Button(dialog,"TRƯỚC",25,625,180,42,()=>{recordPage--;DrawRecord(value,name);});Button(dialog,"SAU",220,625,180,42,()=>{recordPage++;DrawRecord(value,name);});
            Label(dialog,"Trang "+(recordPage+1)+" / "+Math.Max(1,(entries.Count+7)/8),420,633,260,33,16);
            Button(dialog,"QUAY LẠI",704,625,350,42,()=>{if(recordHistory.Count>0){var previous=recordHistory.Pop();recordPage=previous.page;DrawRecord(previous.value,previous.name);}else CloseDialog();});
        }
    }
}

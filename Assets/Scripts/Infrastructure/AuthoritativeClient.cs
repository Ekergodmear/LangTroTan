using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LangTroTan.Infrastructure
{
    public sealed class AuthoritativeClient:MonoBehaviour
    {
        public string ServerUrl {get;private set;}
        public string GameId {get;private set;}
        public Dictionary<string,object> Snapshot {get;private set;}
        public bool Busy {get;private set;}
        public bool HasPending {get{return pendingBody!=null;}}
        public string Notice {get;private set;}="Chưa kết nối";
        public bool IsRefreshing=>polling;
        public event Action StateChanged,StatusChanged;
        public event Action<string,object> DecisionCompleted;
        public static string JournalRootOverride;
        string pendingBody;bool polling;float pollAt;int generation;
        string JournalPath {get{return Path.Combine(JournalRootOverride??Application.persistentDataPath,"tu-tien-server-pending.json");}}
        public void Configure(string url,string gameId)
        {
            if(Busy||HasPending)throw new InvalidOperationException("Cần giải quyết thao tác đang chờ trước khi đổi lượt chơi.");
            if(!Uri.TryCreate(url,UriKind.Absolute,out var u)||u.Scheme!="http"&&u.Scheme!="https"||!string.IsNullOrEmpty(u.UserInfo)||!string.IsNullOrEmpty(u.Query)||!string.IsNullOrEmpty(u.Fragment))throw new ArgumentException("Địa chỉ máy chủ không hợp lệ.");
            if(string.IsNullOrWhiteSpace(gameId)||gameId.Length>128)throw new ArgumentException("Tên lượt chơi không hợp lệ.");
            ServerUrl=url.TrimEnd('/');GameId=gameId;Snapshot=null;generation++;pollAt=0;
            if(File.Exists(JournalPath))
            {
                var journal=ServerJson.Map(ServerJson.Parse(File.ReadAllText(JournalPath)));
                if((string)journal["server"]==ServerUrl&&(string)journal["gameId"]==GameId)
                {pendingBody=(string)journal["body"];ServerJson.Map(ServerJson.Parse(pendingBody));SetNotice("Có thao tác chưa xác nhận từ lần trước. Đồng bộ rồi thử lại cùng yêu cầu.");}
                else SetNotice("Có thao tác của lượt chơi khác đang chờ; dữ liệu đó được giữ nguyên.");
            }
            StartCoroutine(Refresh());
        }
        void Update(){if(ServerUrl!=null&&!Busy&&!polling&&Time.unscaledTime>=pollAt)StartCoroutine(Refresh());}
        void SetNotice(string s){Notice=s;StatusChanged?.Invoke();}
        string Endpoint(string suffix){return ServerUrl+"/api/games/"+Uri.EscapeDataString(GameId)+suffix;}
        public IEnumerator Refresh()
        {
            if(ServerUrl==null||polling)yield break;polling=true;int stamp=generation;
            using(var req=UnityWebRequest.Get(Endpoint("/state")))
            {
                req.timeout=20;yield return req.SendWebRequest();
                if(stamp==generation)
                {
                    if(req.result!=UnityWebRequest.Result.Success)SetNotice("Không thể đồng bộ lượt chơi. "+ResponseError(req));
                    else
                    {
                        try
                        {
                            var state=ServerJson.Map(ServerJson.Parse(req.downloadHandler.text));
                            if(!state.ContainsKey("sect")||!state.ContainsKey("disciples")||!state.ContainsKey("world")||!state.ContainsKey("day"))throw new FormatException("Thiếu dữ liệu thế giới.");
                            Snapshot=state;SetNotice(HasPending?"Đã đồng bộ; còn một thao tác cần xác nhận lại.":"Đã đồng bộ thế giới");StateChanged?.Invoke();
                        }catch(Exception e){SetNotice("Phản hồi không hợp lệ: "+e.Message);}
                    }
                }
            }
            polling=false;pollAt=Time.unscaledTime+5;
        }
        public bool Submit(string action,Dictionary<string,object> payload)
        {
            if(Busy||HasPending){SetNotice("Hãy giải quyết thao tác đang chờ trước.");return false;}
            if(ServerUrl==null||Snapshot==null){SetNotice("Cần đồng bộ lượt chơi trước khi quyết định.");return false;}
            var envelope=new Dictionary<string,object>{{"action",action},{"payload",payload??new Dictionary<string,object>()},{"requestId","unity_"+Guid.NewGuid().ToString("N")}};
            pendingBody=ServerJson.Stringify(envelope);
            try{PersistPending();}catch(Exception e){pendingBody=null;SetNotice("Không ghi được yêu cầu an toàn: "+e.Message);return false;}
            StartCoroutine(SendPending());return true;
        }
        public void RetryPending(){if(!Busy&&HasPending)StartCoroutine(SendPending());}
        public IEnumerator SpatialState(bool motionOnly,Action<Dictionary<string,object>> ready)
        {
            if(ServerUrl==null)yield break;
            using(var req=UnityWebRequest.Get(Endpoint("/spatial-state")+(motionOnly?"?motion=1":"")))
            {
                req.timeout=15;yield return req.SendWebRequest();if(req.result!=UnityWebRequest.Result.Success){SetNotice(ResponseError(req));yield break;}
                Dictionary<string,object> result=null;try{result=ServerJson.Map(ServerJson.Parse(req.downloadHandler.text));}catch(Exception e){SetNotice("Chưa đọc được Sơn Môn: "+e.Message);}
                if(result!=null)ready(result);
            }
        }
        public void ListGamesAt(string url,Action<List<string>> ready)
        {
            if(!ValidAddress(url)){SetNotice("Địa chỉ máy chủ không hợp lệ.");return;}StartCoroutine(ListGamesRoutine(url.TrimEnd('/'),ready));
        }
        static bool ValidAddress(string url){return Uri.TryCreate(url,UriKind.Absolute,out var u)&&(u.Scheme=="http"||u.Scheme=="https")&&string.IsNullOrEmpty(u.UserInfo)&&string.IsNullOrEmpty(u.Query)&&string.IsNullOrEmpty(u.Fragment);}
        IEnumerator ListGamesRoutine(string url,Action<List<string>> ready)
        {
            using(var req=UnityWebRequest.Get(url+"/api/games"))
            {
                req.timeout=20;yield return req.SendWebRequest();if(req.result!=UnityWebRequest.Result.Success){SetNotice(ResponseError(req));yield break;}
                var ids=new List<string>();try{var result=ServerJson.Map(ServerJson.Parse(req.downloadHandler.text));foreach(var item in result["gameIds"] as List<object>??new List<object>())ids.Add(Convert.ToString(item));}catch(Exception e){SetNotice("Không đọc được danh sách lượt: "+e.Message);}
                ready(ids);
            }
        }
        public void CreateGameAt(string url)
        {
            if(Busy||HasPending){SetNotice("Cần giải quyết quyết định đang chờ trước khi lập lượt mới.");return;}
            if(!ValidAddress(url)){SetNotice("Địa chỉ máy chủ không hợp lệ.");return;}StartCoroutine(CreateGameRoutine(url.TrimEnd('/')));
        }
        IEnumerator CreateGameRoutine(string url)
        {
            Busy=true;generation++;string key="FullPort.Creation:"+url;string id=PlayerPrefs.GetString(key,"");if(string.IsNullOrEmpty(id)){id="unity_"+Guid.NewGuid().ToString("N");PlayerPrefs.SetString(key,id);PlayerPrefs.Save();}
            using(var req=new UnityWebRequest(url+"/api/games","POST"))
            {
                req.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(ServerJson.Stringify(new Dictionary<string,object>{{"gameId",id}})));req.downloadHandler=new DownloadHandlerBuffer();req.SetRequestHeader("Content-Type","application/json");req.timeout=20;
                yield return req.SendWebRequest();Busy=false;
                if(req.result!=UnityWebRequest.Result.Success&&req.responseCode!=409){SetNotice("Chưa lập được lượt mới; lần thử lại sẽ dùng cùng lượt để tránh tạo trùng. "+ResponseError(req));yield break;}
                PlayerPrefs.DeleteKey(key);PlayerPrefs.SetString("FullPort.Server",url);PlayerPrefs.SetString("FullPort.Game",id);PlayerPrefs.Save();Configure(url,id);
            }
        }
        public void ResetGame()
        {
            if(Busy||HasPending||ServerUrl==null){SetNotice("Cần giải quyết quyết định đang chờ trước khi tạo lại lượt chơi.");return;}StartCoroutine(ResetGameRoutine());
        }
        IEnumerator ResetGameRoutine()
        {
            Busy=true;generation++;string url=ServerUrl;
            using(var req=UnityWebRequest.Delete(Endpoint("")))
            {
                req.downloadHandler=new DownloadHandlerBuffer();req.timeout=20;yield return req.SendWebRequest();Busy=false;
                if(req.result!=UnityWebRequest.Result.Success&&req.responseCode!=404){SetNotice("Chưa xóa được lượt chơi: "+ResponseError(req));yield break;}
            }
            Snapshot=null;yield return CreateGameRoutine(url);
        }
        public IEnumerator Presentation(string view,Dictionary<string,object> ui,Dictionary<string,object> parameters,Action<Dictionary<string,object>> ready)
        {
            if(ServerUrl==null)yield break;
            string body=ServerJson.Stringify(new Dictionary<string,object>{{"view",view},{"ui",ui??new Dictionary<string,object>()},{"params",parameters??new Dictionary<string,object>()}});
            using(var req=new UnityWebRequest(Endpoint("/unity-presentation"),"POST"))
            {
                req.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));req.downloadHandler=new DownloadHandlerBuffer();req.SetRequestHeader("Content-Type","application/json");
                req.timeout=20;yield return req.SendWebRequest();if(req.result!=UnityWebRequest.Result.Success){SetNotice(ResponseError(req));yield break;}
                Dictionary<string,object> result=null;try{result=ServerJson.Map(ServerJson.Parse(req.downloadHandler.text));}catch(Exception e){SetNotice("Chưa đọc được nội dung: "+e.Message);}
                if(result!=null)ready(result);
            }
        }
        void PersistPending()
        {
            // Never overwrite an unresolved command belonging to another game/server.
            if(File.Exists(JournalPath))
            {
                var old=ServerJson.Map(ServerJson.Parse(File.ReadAllText(JournalPath)));
                if((string)old["server"]!=ServerUrl||(string)old["gameId"]!=GameId)throw new IOException("Có yêu cầu khác cần phục hồi trước.");
            }
            string dir=Path.GetDirectoryName(JournalPath);Directory.CreateDirectory(dir);
            string json=ServerJson.Stringify(new Dictionary<string,object>{{"server",ServerUrl},{"gameId",GameId},{"body",pendingBody}});
            File.WriteAllText(JournalPath+".tmp",json,Encoding.UTF8);
            if(File.Exists(JournalPath))File.Replace(JournalPath+".tmp",JournalPath,null);else File.Move(JournalPath+".tmp",JournalPath);
        }
        IEnumerator SendPending()
        {
            if(Busy||pendingBody==null)yield break;Busy=true;generation++;SetNotice("Đang gửi quyết định…");
            using(var req=new UnityWebRequest(Endpoint("/action"),"POST"))
            {
                req.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(pendingBody));req.downloadHandler=new DownloadHandlerBuffer();req.SetRequestHeader("Content-Type","application/json");req.timeout=20;
                yield return req.SendWebRequest();
                bool uncertain=req.responseCode==0||req.responseCode==408||req.responseCode==429||req.responseCode>=500;
                if(uncertain)SetNotice("Chưa xác nhận kết quả. Yêu cầu được giữ để thử lại, tránh trừ tài nguyên hai lần. "+ResponseError(req));
                else
                {
                    bool valid=false;object body=null;try{body=ServerJson.Parse(req.downloadHandler.text);valid=ServerJson.Map(body).ContainsKey("ok")||req.responseCode>=400;}catch{}
                    if(!valid)SetNotice("Máy chủ trả phản hồi không xác định; yêu cầu vẫn được giữ.");
                    else
                    {
                        try
                        {
                            string action=(string)ServerJson.Map(ServerJson.Parse(pendingBody))["action"];
                            File.Delete(JournalPath);pendingBody=null;SetNotice(req.result==UnityWebRequest.Result.Success?"Đã ghi nhận quyết định.":ResponseError(req));
                            if(req.result==UnityWebRequest.Result.Success&&ServerJson.Map(body).TryGetValue("result",out var result))DecisionCompleted?.Invoke(action,result);
                        }
                        catch(Exception e){SetNotice("Kết quả đã nhận nhưng chưa cập nhật được nhật ký: "+e.Message);}
                    }
                }
            }
            Busy=false;pollAt=0;yield return Refresh();
        }
        static string ResponseError(UnityWebRequest request)
        {
            try{var map=ServerJson.Map(ServerJson.Parse(request.downloadHandler.text));if(map.TryGetValue("error",out var e))return e is string text?text:ServerJson.Stringify(e);}catch{}
            return request.responseCode==0?"Không kết nối được máy chủ.":"Máy chủ chưa xử lý được yêu cầu ("+request.responseCode+").";
        }
    }
}

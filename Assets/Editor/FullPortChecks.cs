using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LangTroTan.Infrastructure;

public static class FullPortChecks
{
    static List<string> results;
    static void Check(bool ok,string label){if(!ok)throw new Exception("FULL PORT FAIL: "+label);results.Add("PASS "+label);}
    static bool Reject(string json){try{ServerJson.Parse(json);return false;}catch(FormatException){return true;}}
    public static List<string> Run()
    {
        results=new List<string>();
        var obj=ServerJson.Map(ServerJson.Parse("{\"id\":9223372036854775807,\"người\":\"Đệ tử\\nThanh Vân\",\"nested\":[true,false,null,{\"linhThach\":0.125}]}"));
        Check(obj["id"] is long n&&n==long.MaxValue,"server ids preserve integer precision");
        Check((string)obj["người"]=="Đệ tử\nThanh Vân","Vietnamese text and newline preserved");
        Check(ServerJson.Stringify(ServerJson.Parse(ServerJson.Stringify(obj)))==ServerJson.Stringify(obj),"nested object array bool null and decimal round trip");
        Check((string)ServerJson.Parse("\"\\u0110\\u1ec7 \\ud83d\\ude80\"")=="Đệ 🚀","escaped Unicode including surrogate pair");
        foreach(var bad in new[]{"{\"x\":1,\"x\":2}","[1,]","01","1.","1e","1e9999","true x","\"bad\n\"","{\"x\":}","["})Check(Reject(bad),"malformed server JSON rejected: "+bad.Replace("\n","\\n"));
        var data=Resources.Load<TextAsset>("FullPort/ReferenceCatalog");var catalog=ServerJson.Map(ServerJson.Parse(data.text));Check(catalog.Count==76,"all seventy six reference data modules present");
        var state=ServerJson.Map(ServerJson.Parse(Resources.Load<TextAsset>("FullPort/InitialServerState").text));
        string roundTrip=ServerJson.Stringify(ServerJson.Parse(ServerJson.Stringify(state)));
        Check(ServerJson.Stringify(state)==roundTrip,"complete initial server snapshot retained without DTO field loss");
        var systems=ServerJson.Map(ServerJson.Parse(File.ReadAllText("Port/FullPortCoverage.json")));
        var counts=ServerJson.Map(systems["counts"]);Check(Convert.ToInt32(counts["systems"])==100,"inventory locks one hundred server gameplay systems");
        var actions=(List<object>)ServerJson.Map(ServerJson.Parse(Resources.Load<TextAsset>("FullPort/ServerActionNames").text))["actions"];
        var coverage=(List<object>)systems["actions"];var ids=coverage.Select(x=>(string)ServerJson.Map(x)["id"]).OrderBy(x=>x).ToArray();
        Check(actions.Count==69&&actions.Select(x=>(string)x).OrderBy(x=>x).SequenceEqual(ids),"all sixty nine server action names match coverage manifest");
        var forms=(List<object>)ServerJson.Map(ServerJson.Parse(Resources.Load<TextAsset>("FullPort/Commands").text))["commands"];
        Check(forms.Count==69&&forms.Select(v=>(string)ServerJson.Map(v)["id"]).OrderBy(v=>v).SequenceEqual(ids),"native command forms cover all server command ids");
        foreach(var form in forms)
        {
            var f=ServerJson.Map(form);Check(!string.IsNullOrWhiteSpace((string)f["title"])&&!string.IsNullOrWhiteSpace((string)f["group"]),"localized command form: "+f["id"]);
        }
        bool blocked=false;try{FullPortReleaseGate.Verify();}catch(InvalidOperationException){blocked=true;}
        Check(blocked,"APK export remains blocked while port incomplete");
        File.WriteAllLines("Reports/FullPortChecks.txt",results);return results;
    }
}

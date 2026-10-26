using System;
using System.IO;
using UnityEngine;

public static class FullPortReleaseGate
{
    [Serializable] sealed class Coverage
    {
        public bool releaseAllowed;
        public ActionCoverage[] actions;
        public RequirementCoverage[] requirements;
        public Acceptance acceptance;
    }
    [Serializable] sealed class ActionCoverage
    { public string id;public bool unityUIImplemented,integrationVerified; }
    [Serializable] sealed class RequirementCoverage
    { public string id;public bool verified; }
    [Serializable] sealed class Acceptance
    {
        public bool native3DVerified,allSystemsMapped,allViewsVerified,serverPersistenceVerified;
        public bool retryAndRestartVerified,realTimeCalendarVerified,androidSourceBuildVerified;
    }
    public static void Verify()
    {
        string path=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Port/FullPortCoverage.json");
        if(!File.Exists(path))throw new InvalidOperationException("Chưa có kiểm kê chuyển đổi đầy đủ. Không xuất APK.");
        var c=JsonUtility.FromJson<Coverage>(File.ReadAllText(path));
        if(c==null||!c.releaseAllowed||c.actions==null||c.actions.Length!=69||c.acceptance==null)
            throw new InvalidOperationException("Chuyển đổi toàn bộ sang Unity 3D chưa hoàn tất. APK đang bị khóa theo yêu cầu người dùng.");
        foreach(var a in c.actions)if(string.IsNullOrEmpty(a.id)||!a.unityUIImplemented||!a.integrationVerified)
            throw new InvalidOperationException("Chưa nghiệm thu thao tác Unity: "+a.id);
        if(c.requirements==null||c.requirements.Length==0)throw new InvalidOperationException("Chưa kiểm kê phần 3D và sinh tồn cần giữ lại.");
        foreach(var r in c.requirements)if(!r.verified)throw new InvalidOperationException("Chưa nghiệm thu yêu cầu: "+r.id);
        var p=c.acceptance;
        if(!p.native3DVerified||!p.allSystemsMapped||!p.allViewsVerified||!p.serverPersistenceVerified||!p.retryAndRestartVerified||!p.realTimeCalendarVerified||!p.androidSourceBuildVerified)
            throw new InvalidOperationException("Chưa đủ bằng chứng nghiệm thu bản chuyển đổi; không xuất APK.");
    }
}

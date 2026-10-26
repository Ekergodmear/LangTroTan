using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using LangTroTan.Domain;

namespace LangTroTan.Infrastructure
{
    [Serializable] public class SaveEnvelope {public int schema=1; public string checksum,payload;}
    public static class SaveRepository
    {
        public static string RootOverride;
        public static string PathName {get{return Path.Combine(RootOverride??Application.persistentDataPath,"lang-tro-tan-v01.json");}}
        public static bool Exists {get{return File.Exists(PathName)||File.Exists(PathName+".bak");}}
        static string Hash(string text){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));}
        public static void Save(World world)
        {
            world.Validate();string payload=JsonUtility.ToJson(world);var env=new SaveEnvelope{checksum=Hash(payload),payload=payload};
            string file=PathName,tmp=file+".tmp";Directory.CreateDirectory(Path.GetDirectoryName(file));
            using(var stream=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None))
            {byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(env));stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            Read(tmp);
            if(File.Exists(file))File.Replace(tmp,file,file+".bak");else File.Move(tmp,file);
        }
        static World Read(string path)
        {
            if(new FileInfo(path).Length>4*1024*1024)throw new InvalidDataException("Save vượt giới hạn dung lượng");
            var env=JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path,Encoding.UTF8));
            if(env==null||env.schema!=1||env.payload==null||env.checksum!=Hash(env.payload))throw new InvalidDataException("Save hỏng hoặc khác phiên bản");
            var world=JsonUtility.FromJson<World>(env.payload);if(world==null)throw new InvalidDataException("Save rỗng");world.UpgradeSave();world.Validate();return world;
        }
        public static World Load(out bool recovered)
        {
            recovered=false;try{return Read(PathName);}catch(Exception primary)
            {
                try{var w=Read(PathName+".bak");recovered=true;return w;}
                catch(Exception){throw new InvalidDataException("Không đọc được save chính hoặc dự phòng. "+primary.Message);}
            }
        }
    }
}

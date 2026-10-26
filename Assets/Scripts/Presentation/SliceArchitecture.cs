using System;
using System.Collections.Generic;
using UnityEngine;
using LangTroTan.Domain;
namespace LangTroTan.Presentation
{
    // Authored parametric 3D meshes. No purchased model or illustration is presented as a 3D asset.
    public static partial class SliceArt
    {
        static Func<string,Color,Material> material;
        static Material Stone=>material("slice limestone",new Color(.72f,.75f,.70f));
        static Material Wood=>material("slice rosewood",new Color(.29f,.105f,.075f));
        static Material Jade=>material("slice glazed tile",new Color(.105f,.27f,.29f));
        static Material Trim=>material("slice bronze",new Color(.61f,.43f,.19f));
        static Material Wall=>material("slice ivory plaster",new Color(.91f,.86f,.71f));
        static GameObject Part(Transform p,string name,PrimitiveType primitive,Vector3 at,Vector3 scale,Material m)
        {var go=GameObject.CreatePrimitive(primitive);go.name=name;go.transform.SetParent(p,false);go.transform.localPosition=at;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.Destroy(go.GetComponent<Collider>());return go;}
        static void Box(Transform p,string name,Vector3 at,Vector3 scale,Material m)=>Part(p,name,PrimitiveType.Cube,at,scale,m);
        static void Beam(Transform p,Vector3 a,Vector3 b,float radius,Material m)
        {
            var direction=(b-a).normalized;var right=Vector3.Cross(direction,Mathf.Abs(direction.y)>.9f?Vector3.forward:Vector3.up).normalized*radius*.5f;var forward=Vector3.Cross(direction,right).normalized*radius*.5f;var v=new List<Vector3>();var t=new List<int>();
            for(int end=0;end<2;end++)for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;v.Add((end==0?a:b)+right*Mathf.Cos(angle)+forward*Mathf.Sin(angle));}for(int i=0;i<6;i++){int j=(i+1)%6;t.AddRange(new[]{i,j,i+6,j,j+6,i+6});}Mesh(p,"Carved beam",v,t,m);
        }
        static void Mesh(Transform p,string name,List<Vector3> vertices,List<int> indices,Material m)
        {var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(p,false);var mesh=new UnityEngine.Mesh{name="LTT "+name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=m;}
        static float RoofY(float x,float z,float w,float d,float y,float rise)
        {float r=Mathf.Clamp01(Mathf.Max(Mathf.Abs(z)/(d*.5f),Mathf.Max(0,Mathf.Abs(x)-w*.18f)/(w*.32f)));return y+rise*Mathf.Pow(1-r,.72f)+rise*.3f*Mathf.Pow(r,9);}
        static void Roof(Transform p,float w,float d,float y,float rise)
        {
            const int nx=28,nz=20;var v=new List<Vector3>();var t=new List<int>();for(int iz=0;iz<=nz;iz++)for(int ix=0;ix<=nx;ix++){float x=(ix/(float)nx-.5f)*w,z=(iz/(float)nz-.5f)*d;v.Add(new Vector3(x,RoofY(x,z,w,d,y,rise),z));}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,b=a+nx+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}Mesh(p,"Curved glazed roof",v,t,Jade);
            var rib=material("slice tile ridges",new Color(.18f,.38f,.38f));
            for(float x=-w*.45f;x<=w*.45f;x+=.24f)for(int side=-1;side<=1;side+=2)for(int j=0;j<7;j++){
                float z1=side*d*.5f*j/7,z2=side*d*.5f*(j+1)/7;Beam(p,new Vector3(x,RoofY(x,z1,w,d,y,rise)+.025f,z1),new Vector3(x,RoofY(x,z2,w,d,y,rise)+.025f,z2),.035f,rib);
            }
            for(int side=-1;side<=1;side+=2){var a=new Vector3(-w*.5f,RoofY(-w*.5f,side*d*.5f,w,d,y,rise),side*d*.5f);var b=new Vector3(w*.5f,a.y,a.z);Beam(p,a,b,.065f,Trim);}
            Beam(p,new Vector3(-w*.19f,y+rise+.07f,0),new Vector3(w*.19f,y+rise+.07f,0),.08f,Trim);
            foreach(int side in new[]{-1,1}){for(int j=0;j<5;j++){float x1=side*w*(.18f+j*.045f),x2=side*w*(.18f+(j+1)*.045f);float a=j/4f,b=(j+1)/4f;Beam(p,new Vector3(x1,y+rise+.06f+a*a*.25f,0),new Vector3(x2,y+rise+.06f+b*b*.25f,0),.08f,Trim);}Part(p,"Ridge pearl",PrimitiveType.Sphere,new Vector3(side*w*.42f,y+rise+.44f,0),Vector3.one*.13f,Trim);}
        }
        static void Lantern(Transform p,Vector3 at)
        {
            var silk=material("slice lantern silk",new Color(.82f,.30f,.105f));silk.EnableKeyword("_EMISSION");silk.SetColor("_EmissionColor",new Color(.7f,.18f,.025f));
            Beam(p,at+Vector3.up*.34f,at+Vector3.up*.64f,.018f,Trim);Part(p,"Silk lantern",PrimitiveType.Sphere,at,new Vector3(.25f,.37f,.25f),silk);
            foreach(float y in new[]{-.19f,.19f})Part(p,"Lantern cap",PrimitiveType.Cylinder,at+Vector3.up*y,new Vector3(.21f,.018f,.21f),Trim);Beam(p,at-Vector3.up*.2f,at-Vector3.up*.4f,.025f,Trim);
        }
        static void Storey(Transform p,float w,float d,float floor,float h,bool main)
        {
            Box(p,"Ivory walls",new Vector3(0,floor+h*.5f,0),new Vector3(w*.79f,h,d*.72f),Wall);
            foreach(int side in new[]{-1,1}){
                for(int i=-1;i<=1;i++){float x=i*w*.38f;Beam(p,new Vector3(x,floor,side*d*.38f),new Vector3(x,floor+h+.08f,side*d*.38f),.105f,Wood);
                    Box(p,"Dougong bracket",new Vector3(x,floor+h-.07f,side*d*.40f),new Vector3(.29f,.10f,.36f),Wood);Box(p,"Gold bracket",new Vector3(x,floor+h+.035f,side*d*.42f),new Vector3(.39f,.08f,.4f),Trim);}
                Beam(p,new Vector3(-w*.4f,floor+h,side*d*.38f),new Vector3(w*.4f,floor+h,side*d*.38f),.10f,Wood);
            }
            for(int side=-1;side<=1;side+=2){float x=side*w*.25f;
                Box(p,"Window recess",new Vector3(x,floor+h*.52f,-d*.366f),new Vector3(w*.23f,h*.53f,.05f),Wood);
                Box(p,"Paper window",new Vector3(x,floor+h*.52f,-d*.38f),new Vector3(w*.2f,h*.46f,.025f),material("slice rice glass",new Color(.71f,.77f,.65f)));
                for(int i=-2;i<=2;i++)Box(p,"Window lattice",new Vector3(x+i*w*.035f,floor+h*.52f,-d*.394f),new Vector3(.018f,h*.48f,.022f),Wood);
                foreach(float t in new[]{.34f,.52f,.7f})Box(p,"Window crossbar",new Vector3(x,floor+h*t,-d*.396f),new Vector3(w*.22f,.022f,.025f),Wood);
                if(main)Lantern(p,new Vector3(side*w*.42f,floor+h*.67f,-d*.49f));
            }
            Box(p,"Double carved door",new Vector3(0,floor+h*.42f,-d*.375f),new Vector3(w*.18f,h*.84f,.09f),Wood);
            foreach(int side in new[]{-1,1})Part(p,"Door ring",PrimitiveType.Sphere,new Vector3(side*w*.032f,floor+h*.43f,-d*.43f),Vector3.one*.07f,Trim);
            if(main){Box(p,"Inscribed plaque",new Vector3(0,floor+h*.84f,-d*.44f),new Vector3(w*.32f,.26f,.09f),Jade);for(int i=-1;i<=1;i++)Box(p,"Plaque engraving",new Vector3(i*.16f,floor+h*.84f,-d*.495f),new Vector3(.035f,.12f,.015f),Trim);}
            Roof(p,w*1.13f,d*1.14f,floor+h,.68f*h);
        }
        public static bool Building(Transform p,Building b,Func<string,Color,Material> mat)
        {
            material=mat;float w=b.W,d=b.D;
            if(b.type=="farm"){
                Box(p,"Tilled field",new Vector3(0,.07f,0),new Vector3(w,.13f,d),material("slice soil",new Color(.27f,.19f,.12f)));
                for(int i=0;i<7;i++)for(int j=0;j<6;j++){float x=(i/6f-.5f)*w*.85f,z=(j/5f-.5f)*d*.84f;Beam(p,new Vector3(x,.12f,z),new Vector3(x+.04f,.48f,z),.018f,material("slice wheat",new Color(.61f,.59f,.22f)));}return true;
            }
            if(b.type=="wall"){Box(p,"White boundary wall",new Vector3(0,.65f,0),new Vector3(w*.9f,1.3f,d*.9f),Wall);Roof(p,w+.1f,d+.1f,1.3f,.2f);return true;}
            if(b.type=="gate"){foreach(int i in new[]{-1,1})Box(p,"Gate pillar",new Vector3(i*w*.39f,1.4f,0),new Vector3(.26f,2.8f,.4f),Wood);Roof(p,w*1.2f,1.3f,2.8f,.65f);Box(p,"Gate plaque",new Vector3(0,2.45f,-.24f),new Vector3(w*.7f,.36f,.12f),Jade);return true;}
            Box(p,"Stone foundation",new Vector3(0,.16f,0),new Vector3(w,.32f,d),Stone);Box(p,"Moulded stone plinth",new Vector3(0,.36f,0),new Vector3(w*.96f,.12f,d*.96f),Stone);
            for(int i=0;i<4;i++)Box(p,"Stone stairs",new Vector3(0,.055f+i*.065f,-d*.5f-.33f+i*.085f),new Vector3(w*.43f,.11f+i*.13f,.5f-i*.07f),Stone);
            if(b.type=="spiritmine"){
                for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;var glow=material("slice spirit crystal",new Color(.16f,.65f,.7f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.04f,.34f,.4f));var crystal=Part(p,"Spirit crystal",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*w*.25f,.8f,Mathf.Sin(a)*d*.24f),new Vector3(.24f,1.1f+(i%3)*.2f,.24f),glow);crystal.transform.localRotation=Quaternion.Euler(12,i*47,18);}Formation(p,w*.44f,.45f);return true;
            }
            int floors=b.type=="scripture"?3:b.type=="hall"?2:1;float h=b.type=="hall"?1.55f:1.35f;
            for(int i=0;i<floors;i++){float scale=1-i*.19f;Storey(p,w*scale,d*scale,.43f+i*(h+.72f),h,i==0);}
            if(b.type=="alchemy"){var bronze=material("slice cauldron",new Color(.25f,.36f,.29f));Part(p,"Alchemy cauldron",PrimitiveType.Sphere,new Vector3(w*.31f,.8f,-d*.5f),new Vector3(.65f,.65f,.65f),bronze);Part(p,"Cauldron rim",PrimitiveType.Cylinder,new Vector3(w*.31f,1.06f,-d*.5f),new Vector3(.7f,.035f,.7f),Trim);}
            foreach(int i in new[]{-1,1}){Box(p,"Terrace post",new Vector3(i*w*.45f,.65f,-d*.43f),new Vector3(.1f,.46f,.1f),Stone);Part(p,"Stone finial",PrimitiveType.Sphere,new Vector3(i*w*.45f,.9f,-d*.43f),Vector3.one*.15f,Stone);}
            return true;
        }
        public static void Formation(Transform p,float radius,float height)
        {
            var glow=material("slice formation",new Color(.29f,.74f,.69f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.1f,.6f,.5f));
            foreach(float ratio in new[]{1f,.83f})for(int i=0;i<48;i++){float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;Beam(p,new Vector3(Mathf.Cos(a)*radius*ratio,height,Mathf.Sin(a)*radius*ratio),new Vector3(Mathf.Cos(b)*radius*ratio,height,Mathf.Sin(b)*radius*ratio),.018f,glow);}
        }
    }
}

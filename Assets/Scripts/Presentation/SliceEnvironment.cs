using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LangTroTan.Presentation
{
    public static partial class SliceArt
    {
        public static void Tree(Transform p,int season,int seed,Func<string,Color,Material> mat)
        {
            material=mat;float angle=(seed%313)*.3f;p.localRotation=Quaternion.Euler(0,angle*57,0);float height=2.7f+(seed%7)*.13f;
            var bark=material("slice cedar bark",new Color(.31f,.25f,.18f));var leaf=material("slice canopy "+season,new[]{new Color(.25f,.44f,.31f),new Color(.20f,.37f,.28f),new Color(.63f,.35f,.18f),new Color(.62f,.72f,.68f)}[season]);
            Color pigment=leaf.color;leaf.shader=Shader.Find("Xianxia/Painted Foliage");leaf.color=pigment;
            Beam(p,Vector3.zero,new Vector3(.12f,height*.73f,0),.17f,bark);
            for(int i=0;i<3;i++){float a=angle+i*2.399f;var tip=new Vector3(Mathf.Cos(a)*.55f,height*(.63f+i*.13f),Mathf.Sin(a)*.55f);Beam(p,new Vector3(.08f,height*.4f,0),tip,.065f,bark);}
            Canopy(p,new Vector3(.05f,height*.87f,0),new Vector3(2.65f+(seed%3)*.22f,1.95f,2.45f),leaf,seed);
        }
        static void Canopy(Transform p,Vector3 at,Vector3 scale,Material m,int seed)
        {
            var v=new List<Vector3>();var t=new List<int>();const int n=12,l=7;
            for(int y=0;y<=l;y++)for(int x=0;x<=n;x++){
                float a=x*Mathf.PI*2/n,b=y*Mathf.PI/l;float jitter=1+.16f*Mathf.Sin(a*3+seed)+.055f*Mathf.Sin(a*7+b*4);v.Add(at+Vector3.Scale(new Vector3(Mathf.Sin(b)*Mathf.Cos(a)*jitter,Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a)*jitter),scale*.5f));}
            for(int y=0;y<l;y++)for(int x=0;x<n;x++){int a=y*(n+1)+x,b=a+n+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}Mesh(p,"Sculpted cedar crown",v,t,m);
        }
        static void Cliff(Transform p,Vector3 at,float radius,float height,int seed,Material m)
        {
            const int n=36,layers=18;var v=new List<Vector3>();var t=new List<int>();
            for(int y=0;y<=layers;y++)for(int x=0;x<=n;x++){float u=y/(float)layers,a=x*Mathf.PI*2/n;float r=radius*(1-.65f*u)*(.84f+.17f*Mathf.Sin(a*5+seed)+.09f*Mathf.Sin(a*11+u*8));if(y==layers)r*=.4f;
                v.Add(at+new Vector3(Mathf.Cos(a)*r+Mathf.Sin(u*2+seed)*u*2,u*height,Mathf.Sin(a)*r));}
            for(int y=0;y<layers;y++)for(int x=0;x<n;x++){int a=y*(n+1)+x,b=a+n+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}Mesh(p,"Weathered mountain massif",v,t,m);
            Canopy(p,at+new Vector3(0,height,0),new Vector3(radius*.9f,1.7f,radius*.8f),material("slice distant foliage",new Color(.30f,.46f,.42f)),seed);
        }
        static void Bamboo(Transform p,Vector3 at,int seed)
        {
            var stem=material("slice bamboo",new Color(.24f,.40f,.25f));var foliage=material("slice bamboo leaf",new Color(.38f,.55f,.35f));foliage.SetInt("_Cull",0);float h=2.4f+(seed%5)*.34f;
            Color green=foliage.color;foliage.shader=Shader.Find("Xianxia/Painted Foliage");foliage.color=green;
            for(int stalk=0;stalk<3;stalk++){var start=at+new Vector3(stalk*.24f,0,(stalk%2)*.27f);var top=start+new Vector3(.25f,h-stalk*.2f,.1f);Beam(p,start,top,.055f,stem);
                for(int i=1;i<5;i++){var pos=Vector3.Lerp(start,top,i/5f);Part(p,"Bamboo node",PrimitiveType.Sphere,pos,Vector3.one*.075f,foliage);}
                for(int i=0;i<5;i++){float a=(seed+i*2.3f)*2;var startLeaf=Vector3.Lerp(start,top,.6f+i*.075f);var tip=startLeaf+new Vector3(Mathf.Cos(a)*.75f,.15f,Mathf.Sin(a)*.75f);Beam(p,startLeaf,tip,.018f,stem);
                    var verts=new List<Vector3>{startLeaf,tip,tip+new Vector3(.22f,-.12f,.22f),startLeaf+new Vector3(.10f,0,.1f)};Mesh(p,"Bamboo leaf fan",verts,new List<int>{0,1,2,0,2,3},foliage);}
            }
        }
        public static void Environment(Transform p,Func<string,Color,Material> mat)
        {
            material=mat;var grass=material("slice meadow",new Color(.40f,.56f,.44f));grass.shader=Shader.Find("Xianxia/Painted Landscape");grass.color=new Color(.40f,.56f,.44f);
            var terrain=new List<Vector3>();var terrainIndices=new List<int>();const int cells=128;for(int z=0;z<=cells;z++)for(int x=0;x<=cells;x++){float px=(x/(float)cells-.5f)*380,pz=(z/(float)cells-.5f)*380;float edge=Mathf.SmoothStep(0,1,(Mathf.Max(Mathf.Abs(px),Mathf.Abs(pz))-48)/34);float y=-.11f+edge*(3+Mathf.PerlinNoise(px*.022f+12,pz*.022f+8)*15);terrain.Add(new Vector3(px,y,pz));}
            for(int z=0;z<cells;z++)for(int x=0;x<cells;x++){int a=z*(cells+1)+x,b=a+cells+1;terrainIndices.AddRange(new[]{a,b,a+1,a+1,b,b+1});}Mesh(p,"Mountain foothill terrain",terrain,terrainIndices,grass);
            var stone=material("slice courtyard stone",new Color(.65f,.70f,.65f));
            Box(p,"Grand courtyard",new Vector3(0,-.045f,0),new Vector3(19,.08f,21),stone);
            var joint=material("slice paving joints",new Color(.48f,.56f,.51f));for(int i=-10;i<=10;i++){
                Box(p,"Courtyard grout",new Vector3(i*.9f,.001f,0),new Vector3(.018f,.008f,20.5f),joint);Box(p,"Courtyard grout",new Vector3(0,.001f,i),new Vector3(18.5f,.008f,.018f),joint);
            }
            var backdrop=material("slice mountain blue",new Color(.37f,.51f,.52f));var far=material("slice distant mountain",new Color(.53f,.65f,.64f));foreach(var rock in new[]{backdrop,far}){Color pigment=rock.color;rock.shader=Shader.Find("Xianxia/Painted Landscape");rock.color=pigment;rock.SetFloat("_Rock",1);}
            for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;float r=i<6?54:75;Cliff(p,new Vector3(Mathf.Cos(a)*r,-10,Mathf.Sin(a)*r),7+(i%3)*2,24+(i%5)*3,i,i<6?backdrop:far);}
            for(int i=0;i<22;i++){float a=i*2.399f;float r=21+(i%4)*3;Bamboo(p,new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),i);}
            var pale=material("slice waterfall",new Color(.66f,.84f,.82f));pale.EnableKeyword("_EMISSION");pale.SetColor("_EmissionColor",new Color(.05f,.14f,.14f));
            foreach(var at in new[]{new Vector3(-34,0,28),new Vector3(31,0,39)}){
                Cliff(p,at+new Vector3(-1,-5,5),6,23,3,backdrop);
                for(int i=0;i<5;i++){var g=Part(p,"Waterfall ribbon",PrimitiveType.Cube,at+new Vector3(i*.25f,8,0),new Vector3(.29f,18,.12f),pale);g.AddComponent<SliceWater>().offset=i;}
                Part(p,"Waterfall pool",PrimitiveType.Cylinder,at+Vector3.down*.02f,new Vector3(6,.04f,4),material("slice pool",new Color(.29f,.57f,.57f)));
            }
            // The perimeter gate is scenic. The buildable gate remains a separate authoritative entity.
            var gate=new GameObject("Ceremonial perimeter gate");gate.transform.SetParent(p,false);gate.transform.localPosition=new Vector3(0,0,-13);Building(gate.transform,new LangTroTan.Domain.Building{type="gate",level=1,complete=true},mat);gate.transform.localScale=new Vector3(2,1.4f,1);
            var formation=new GameObject("Courtyard formation");formation.transform.SetParent(p,false);formation.transform.localPosition=new Vector3(0,.02f,-7);Formation(formation.transform,2,.03f);
            VillageArt.Combine(p);
        }
        public static void Character(Transform p,string kind,Func<string,Color,Material> mat)
        {
            material=mat;var cloth=material("slice character robe",new Color(.69f,.78f,.73f));var sash=material("slice character sash",new Color(.14f,.33f,.34f));var skin=material("slice character skin",new Color(.91f,.76f,.61f));
            for(int side=-1;side<=1;side+=2){Box(p,"Leg",new Vector3(side*.09f,.22f,0),new Vector3(.12f,.43f,.15f),sash);Beam(p,new Vector3(side*.2f,.84f,0),new Vector3(side*.28f,.6f,.08f),.14f,cloth);}
            Part(p,"Silk robe",PrimitiveType.Capsule,new Vector3(0,.64f,0),new Vector3(.36f,.46f,.27f),cloth);Box(p,"Jade sash",new Vector3(0,.6f,0),new Vector3(.39f,.08f,.29f),sash);
            Part(p,"Anime face",PrimitiveType.Sphere,new Vector3(0,1.07f,.01f),new Vector3(.29f,.34f,.25f),skin);var hair=material("slice character hair",new Color(.075f,.09f,.12f));Part(p,"Sculpted hair",PrimitiveType.Sphere,new Vector3(0,1.16f,-.025f),new Vector3(.31f,.24f,.28f),hair);Part(p,"Hair knot",PrimitiveType.Sphere,new Vector3(0,1.31f,-.02f),Vector3.one*.14f,hair);
            foreach(int side in new[]{-1,1})Box(p,"Eye",new Vector3(side*.063f,1.09f,.126f),new Vector3(.035f,.025f,.018f),hair);
            Beam(p,new Vector3(.32f,.35f,0),new Vector3(.32f,1.08f,.06f),.035f,Wood);Box(p,"Work tool",new Vector3(.36f,1.04f,.06f),new Vector3(.24f,.11f,.05f),Stone);
        }
    }
    public sealed class SliceWater:MonoBehaviour{public float offset;void Update(){transform.localScale=new Vector3(.24f+Mathf.Sin(Time.time*1.6f+offset)*.035f,18,.12f);}}
}

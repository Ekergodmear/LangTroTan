using System;
using System.Collections.Generic;
using UnityEngine;
using LangTroTan.Domain;

namespace LangTroTan.Presentation
{
    public static class VillageArt
    {
        static Func<string,Color,Material> material;
        static Material Wood=>material("oak",new Color(.34f,.22f,.12f));
        static Material Stone=>material("masonry",new Color(.52f,.51f,.42f));
        static Material Plaster=>material("lime",new Color(.91f,.81f,.59f));
        static Material Metal=>material("iron",new Color(.38f,.43f,.43f));
        static Material Teal=>material("teal",new Color(.12f,.34f,.35f));
        static GameObject Part(Transform parent,string name,PrimitiveType primitive,Vector3 p,Vector3 scale,Material mat,Vector3? angle=null)
        {
            var g=GameObject.CreatePrimitive(primitive);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
            if(angle.HasValue)g.transform.localEulerAngles=angle.Value;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.Destroy(g.GetComponent<Collider>());return g;
        }
        static void Box(Transform p,string n,Vector3 pos,Vector3 s,Material m){Part(p,n,PrimitiveType.Cube,pos,s,m);}
        static void Beam(Transform p,Vector3 a,Vector3 b,float width,Material m)
        {var g=Part(p,"Timber",PrimitiveType.Cube,(a+b)*.5f,new Vector3(width,(a-b).magnitude,width),m);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static void Roof(Transform p,float w,float d,float y,float rise,Material m)
        {
            var g=new GameObject("Pitched roof");g.transform.SetParent(p,false);
            var mesh=new Mesh();mesh.name="LTT geometry";mesh.vertices=new[]{new Vector3(-w*.5f,y,-d*.5f),new Vector3(w*.5f,y,-d*.5f),new Vector3(0,y+rise,-d*.5f),new Vector3(-w*.5f,y,d*.5f),new Vector3(w*.5f,y,d*.5f),new Vector3(0,y+rise,d*.5f)};
            mesh.triangles=new[]{0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4};mesh.RecalculateNormals();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;
            foreach(float z in new[]{-d*.5f,d*.5f}){Beam(p,new Vector3(-w*.5f,y,z),new Vector3(0,y+rise,z),.11f,Wood);Beam(p,new Vector3(w*.5f,y,z),new Vector3(0,y+rise,z),.11f,Wood);}
            Beam(p,new Vector3(0,y+rise,-d*.53f),new Vector3(0,y+rise,d*.53f),.12f,Wood);
            // Horizontal shingle courses give the roof a readable hand-built texture.
            for(int side=-1;side<=1;side+=2)for(int row=1;row<5;row++)
            {float t=row/5f;Beam(p,new Vector3(side*w*.5f*t,y+rise*(1-t)+.02f,-d*.49f),new Vector3(side*w*.5f*t,y+rise*(1-t)+.02f,d*.49f),.035f,material("roof seam",new Color(.30f,.26f,.19f)));}
        }
        static void Crate(Transform p,Vector3 pos,float s)
        {
            Box(p,"Crate",pos,new Vector3(s,s,s),material("crate",new Color(.60f,.42f,.20f)));
            for(int i=-1;i<=1;i+=2){Box(p,"Crate strap",pos+new Vector3(i*s*.35f,0,-s*.51f),new Vector3(s*.1f,s*1.05f,.035f),Wood);}
            Beam(p,pos+new Vector3(-s*.4f,-s*.4f,-s*.52f),pos+new Vector3(s*.4f,s*.4f,-s*.52f),.055f,Wood);
        }
        static void Barrel(Transform p,Vector3 pos,float s)
        {
            Part(p,"Barrel",PrimitiveType.Cylinder,pos,new Vector3(s,s*.55f,s),material("barrel",new Color(.52f,.32f,.13f)));
            foreach(float y in new[]{-.25f,.25f})Part(p,"Iron hoop",PrimitiveType.Cylinder,pos+Vector3.up*y*s,new Vector3(s*1.02f,.035f,s*1.02f),Metal);
        }
        static void Banner(Transform p,float x,float y,float z)
        {
            Box(p,"Banner",new Vector3(x,y,z),new Vector3(.38f,.85f,.04f),Teal);
            Box(p,"Gold trim",new Vector3(x,y-.36f,z-.025f),new Vector3(.39f,.06f,.045f),material("brass",new Color(.85f,.62f,.24f)));
            Box(p,"Banner emblem",new Vector3(x,y+.05f,z-.03f),new Vector3(.10f,.3f,.04f),material("brass",new Color(.85f,.62f,.24f)));
        }
        static void Base(Transform p,float w,float d)
        {
            Box(p,"Stone footing",new Vector3(0,.1f,0),new Vector3(w,.2f,d),Stone);
            for(int i=0;i<5;i++)Box(p,"Foundation stone",new Vector3(-w*.4f+i*w*.2f,.2f,-d*.505f),new Vector3(w*.18f,.23f,.13f),material("stone "+i,new Color(.46f+i*.016f,.45f+i*.018f,.37f+i*.02f)));
        }
        static void Fence(Transform p,float w,float d)
        {
            for(int side=-1;side<=1;side+=2)
            {for(int i=0;i<5;i++)Box(p,"Fence post",new Vector3(-w*.48f+i*w*.24f,.28f,side*d*.48f),new Vector3(.07f,.56f,.07f),Wood);
                Box(p,"Fence rail",new Vector3(0,.3f,side*d*.48f),new Vector3(w,.075f,.07f),Wood);}
        }
        public static void Building(Transform p,Domain.Building b,Func<string,Color,Material> mat)
        {
            material=mat;float w=b.W*.9f,d=b.D*.9f;Base(p,w,d);
            if(World.IsSectBuilding(b.type)||b.type=="hall"||b.type=="house"||b.type=="barracks")
            {
                var jade=material("sect jade",new Color(.12f,.43f,.38f));var ivory=material("sect ivory",new Color(.84f,.84f,.7f));
                var vermilion=material("sect red",new Color(.47f,.16f,.11f));var brass=material("sect gold",new Color(.8f,.64f,.28f));
                if(b.type=="meditation")
                {
                    Part(p,"Jade platform",PrimitiveType.Cylinder,new Vector3(0,.25f,0),new Vector3(w,.13f,d),jade);
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Part(p,"Spirit obelisk",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*w*.37f,.7f,Mathf.Sin(a)*d*.37f),new Vector3(.18f,.8f,.18f),brass,new Vector3(0,i*45,15));}
                    Part(p,"Spirit crystal",PrimitiveType.Cube,new Vector3(0,1.2f,0),Vector3.one*.5f,jade,new Vector3(30,45,30));return;
                }
                if(b.type=="herbgarden"||b.type=="spiritmine")
                {
                    for(int i=0;i<9;i++){float x=(i%3-1)*w*.28f,z=(i/3-1)*d*.28f;
                        if(b.type=="spiritmine")Part(p,"Linh thach crystal",PrimitiveType.Cube,new Vector3(x,.45f,z),new Vector3(.35f,.7f,.35f),jade,new Vector3(15,i*30,25));
                        else {Box(p,"Herb bed",new Vector3(x,.2f,z),new Vector3(.5f,.2f,.5f),Wood);Point(p,new Vector3(x,.3f,z),.2f,.65f,jade);}}
                    Fence(p,w,d);return;
                }
                int floors=(b.type=="hall"||b.type=="scripture"?2:1)+(b.level-1);
                for(int floor=0;floor<floors;floor++)
                {
                    float y=.3f+floor*.95f,ww=w*(1-floor*.13f),dd=d*(1-floor*.13f);
                    Box(p,"Ivory pavilion",new Vector3(0,y+.37f,0),new Vector3(ww*.76f,.75f,dd*.72f),ivory);
                    foreach(int side in new[]{-1,1}){Box(p,"Red column",new Vector3(side*ww*.37f,y+.42f,-dd*.38f),new Vector3(.12f,.85f,.12f),vermilion);}
                    Roof(p,ww,dd,y+.8f,.42f,jade);
                    foreach(int side in new[]{-1,1})foreach(int zside in new[]{-1,1})Beam(p,new Vector3(side*ww*.35f,y+.92f,zside*dd*.5f),new Vector3(side*ww*.56f,y+1.12f,zside*dd*.5f),.08f,brass);
                }
                Box(p,"Entrance",new Vector3(0,.7f,-d*.37f),new Vector3(w*.25f,.75f,.05f),Wood);
                Box(p,"Sect plaque",new Vector3(0,1.22f,-d*.39f),new Vector3(w*.4f,.23f,.08f),brass);
                if(b.type=="alchemy"||b.type=="forge")
                {
                    Part(p,"Bronze furnace",PrimitiveType.Cylinder,new Vector3(w*.33f,.65f,-d*.5f),new Vector3(.55f,.45f,.55f),brass);Smoke(p,new Vector3(w*.33f,1.3f,-d*.5f));
                }
                if(b.type=="barracks")for(int i=0;i<3;i++)Beam(p,new Vector3(-w*.3f+i*.3f,.25f,-d*.48f),new Vector3(-w*.3f+i*.3f,1.1f,-d*.48f),.045f,Metal);
                return;
            }
            if(b.type=="well")
            {
                Part(p,"Well masonry",PrimitiveType.Cylinder,new Vector3(0,.45f,0),new Vector3(1.1f,.4f,1.1f),Stone);
                Part(p,"Well water",PrimitiveType.Cylinder,new Vector3(0,.86f,0),new Vector3(.76f,.014f,.76f),material("water",new Color(.13f,.39f,.49f)));
                foreach(int side in new[]{-1,1})Box(p,"Well post",new Vector3(side*.61f,1,0),new Vector3(.12f,2,.12f),Wood);
                Roof(p,1.65f,1.3f,1.9f,.5f,Teal);Beam(p,new Vector3(0,1.8f,0),new Vector3(0,.95f,0),.035f,Metal);Barrel(p,new Vector3(.5f,.3f,-.55f),.35f);return;
            }
            if(b.type=="hearth")
            {
                Box(p,"Hearth stone",new Vector3(0,.6f,0),new Vector3(1.1f,1.1f,.8f),Stone);Box(p,"Fireplace opening",new Vector3(0,.45f,-.42f),new Vector3(.65f,.65f,.05f),Wood);
                Point(p,new Vector3(0,.17f,-.5f),.19f,.55f,material("hearth flame",new Color(1,.52f,.1f)));Box(p,"Hearth chimney",new Vector3(0,1.55f,0),new Vector3(.5f,1.5f,.5f),Stone);Smoke(p,new Vector3(0,2.4f,0));return;
            }
            if(b.type=="forester")
            {
                for(int i=0;i<6;i++){float x=(i%3-1)*.5f,z=i<3?-.4f:.35f;Crate(p,new Vector3(x,.2f,z),.35f);Point(p,new Vector3(x,.4f,z),.18f,.6f,material("nursery green",new Color(.27f,.48f,.16f)));}
                Roof(p,w,d*.6f,1.5f,.5f,Teal);for(int x=-1;x<=1;x+=2)Box(p,"Nursery post",new Vector3(x*w*.4f,.75f,d*.28f),new Vector3(.1f,1.5f,.1f),Wood);return;
            }
            if(b.type=="farm")
            {
                Box(p,"Tilled soil",new Vector3(0,.16f,0),new Vector3(w,.15f,d),material("earth",new Color(.26f,.18f,.08f)));
                var wheat=material("ripe grain",new Color(.93f,.69f,.22f));
                for(int a=0;a<7;a++)for(int z=0;z<6;z++)
                {float x=-w*.39f+a*w*.13f,zz=-d*.36f+z*d*.145f,grainHeight=.45f+Mathf.Sin(a*3+z)*.08f;
                    Beam(p,new Vector3(x,.2f,zz),new Vector3(x,grainHeight+.2f,zz),.028f,wheat);Point(p,new Vector3(x,grainHeight+.16f,zz),.08f,.24f,wheat);}
                Fence(p,w,d);Crate(p,new Vector3(w*.4f,.25f,-d*.42f),.35f);return;
            }
            if(b.type=="wall"||b.type=="gate")
            {
                int count=b.type=="wall"?5:2;
                for(int i=0;i<count;i++){float x=b.type=="wall"?-w*.4f+i*w*.2f:(i==0?-w*.44f:w*.44f);float postHeight=b.type=="wall"?1.25f:1.65f;
                    Part(p,"Palisade post",PrimitiveType.Cylinder,new Vector3(x,postHeight*.5f,0),new Vector3(.17f,postHeight*.5f,.17f),Wood);
                    Point(p,new Vector3(x,postHeight,0),.12f,.27f,Wood);}
                foreach(float y in new[]{.42f,.9f})Box(p,"Palisade binding",new Vector3(0,y,.06f),new Vector3(w,.12f,.1f),Wood);
                if(b.type=="gate"){Box(p,"Gate arch",new Vector3(0,1.7f,0),new Vector3(w,.19f,.2f),Wood);Banner(p,0,1.5f,-.12f);}return;
            }
            if(b.type=="tower")
            {
                Box(p,"Stone watch base",new Vector3(0,.8f,0),new Vector3(w*.7f,1.4f,d*.7f),Stone);
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(p,"Tower support",new Vector3(x*w*.36f,1.9f,z*d*.36f),new Vector3(.13f,2.7f,.13f),Wood);
                Box(p,"Watch platform",new Vector3(0,2.4f,0),new Vector3(w,.18f,d),Wood);
                for(int side=-1;side<=1;side+=2){Box(p,"Watch railing",new Vector3(0,2.65f,side*d*.43f),new Vector3(w,.11f,.1f),Wood);}
                Roof(p,w*1.1f,d*1.1f,3.1f,.75f,Teal);Banner(p,0,1.83f,-d*.38f);return;
            }
            if(b.type=="stone")
            {
                for(int i=0;i<6;i++)Rock(p,new Vector3(Mathf.Sin(i*2)*w*.25f,.35f,Mathf.Cos(i*3)*d*.25f),new Vector3(.7f,.65f,.65f),Stone);
                for(int x=-1;x<=1;x+=2)Box(p,"Quarry gantry",new Vector3(x*w*.35f,1.1f,d*.2f),new Vector3(.16f,2.2f,.16f),Wood);
                Box(p,"Quarry crossbar",new Vector3(0,2.1f,d*.2f),new Vector3(w,.18f,.18f),Wood);Beam(p,new Vector3(0,2.1f,d*.2f),new Vector3(0,.8f,d*.2f),.035f,Metal);Crate(p,new Vector3(w*.3f,.3f,-d*.35f),.5f);return;
            }
            if(b.type=="wood")
            {
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(p,"Sawmill post",new Vector3(x*w*.36f,.8f,z*d*.35f),new Vector3(.13f,1.6f,.13f),Wood);
                Roof(p,w,d*.9f,1.6f,.6f,Teal);Box(p,"Cutting bench",new Vector3(0,.6f,0),new Vector3(w*.85f,.16f,.45f),Wood);
                Part(p,"Saw blade",PrimitiveType.Cylinder,new Vector3(0,.88f,-.05f),new Vector3(.55f,.025f,.55f),Metal,new Vector3(90,0,0));
                for(int i=0;i<4;i++)Part(p,"Stacked timber",PrimitiveType.Cylinder,new Vector3(-w*.25f,.2f+i*.12f,-d*.38f),new Vector3(.18f,.55f,.18f),Wood,new Vector3(0,0,90));return;
            }
            float h=b.type=="hall"?2.25f:b.type=="barracks"?1.7f:1.45f;
            var roof=material("roof "+b.type,b.type=="hall"||b.type=="store"?new Color(.16f,.37f,.37f):b.type=="barracks"?new Color(.47f,.20f,.13f):new Color(.63f,.29f,.13f));
            Box(p,"Plaster walls",new Vector3(0,h*.5f+.2f,0),new Vector3(w*.88f,h,d*.84f),Plaster);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<3;i++)Box(p,"Timber frame",new Vector3(-w*.42f+i*w*.42f,h*.5f+.2f,side*d*.43f),new Vector3(.10f,h+.1f,.11f),Wood);
                foreach(float y in new[]{.35f,h*.66f,h+.15f})Box(p,"Horizontal frame",new Vector3(0,y,side*d*.43f),new Vector3(w*.94f,.1f,.1f),Wood);
            }
            Roof(p,w*1.12f,d*1.08f,h+.2f,w*.3f,roof);
            Box(p,"Oak doorway",new Vector3(0,.7f,-d*.44f),new Vector3(w*.23f,1,.07f),Wood);
            for(int i=-1;i<=1;i+=2)
            {
                Box(p,"Window frame",new Vector3(i*w*.28f,1.05f,-d*.445f),new Vector3(.32f,.43f,.08f),Wood);
                Box(p,"Warm window",new Vector3(i*w*.28f,1.05f,-d*.46f),new Vector3(.23f,.32f,.055f),material("lamplight",new Color(1,.70f,.27f)));
            }
            Box(p,"Door step",new Vector3(0,.15f,-d*.57f),new Vector3(w*.32f,.25f,.36f),Stone);
            Box(p,"Chimney",new Vector3(w*.25f,h+.6f,d*.18f),new Vector3(.29f,1.2f,.34f),Stone);
            if(b.type=="hall")
            {
                Banner(p,-w*.35f,1.45f,-d*.48f);Banner(p,w*.35f,1.45f,-d*.48f);
                Roof(p,w*.45f,.85f,1.4f,.42f,roof);Crate(p,new Vector3(-w*.37f,.39f,-d*.46f),.55f);
            }
            if(b.type=="store"){Crate(p,new Vector3(w*.36f,.33f,-d*.43f),.45f);Crate(p,new Vector3(w*.36f,.77f,-d*.43f),.4f);Barrel(p,new Vector3(-w*.33f,.35f,-d*.4f),.45f);}
            if(b.type=="house"){Barrel(p,new Vector3(w*.4f,.32f,-d*.35f),.36f);Box(p,"Window planter",new Vector3(-w*.28f,.75f,-d*.52f),new Vector3(.42f,.13f,.24f),Wood);Part(p,"Flowers",PrimitiveType.Sphere,new Vector3(-w*.28f,.88f,-d*.52f),new Vector3(.38f,.17f,.2f),material("flowers",new Color(.64f,.26f,.36f)));}
            if(b.type=="barracks")
            {
                Banner(p,w*.35f,1.3f,-d*.48f);Box(p,"Weapon rack",new Vector3(-w*.32f,.6f,-d*.46f),new Vector3(.65f,.12f,.14f),Wood);
                for(int i=0;i<3;i++){var pos=new Vector3(-w*.46f+i*.22f,0,-d*.46f);Beam(p,pos,new Vector3(pos.x,1.3f,pos.z),.045f,Wood);Point(p,pos+Vector3.up*1.3f,.07f,.23f,Metal);}
            }
            Smoke(p,new Vector3(w*.25f,h+1.2f,d*.18f));
        }
        static void Smoke(Transform parent,Vector3 pos)
        {
            var g=new GameObject("Chimney smoke");g.transform.SetParent(parent,false);g.transform.localPosition=pos;var ps=g.AddComponent<ParticleSystem>();
            var main=ps.main;main.startLifetime=3;main.startSpeed=.18f;main.startSize=.23f;main.startColor=new Color(.83f,.81f,.72f,.15f);main.maxParticles=9;main.gravityModifier=-.012f;
            var emission=ps.emission;emission.rateOverTime=2;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.04f;
            var col=ps.colorOverLifetime;col.enabled=true;var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Color.gray,0),new GradientColorKey(Color.gray,1)},new[]{new GradientAlphaKey(.16f,0),new GradientAlphaKey(0,1)});col.color=grad;
            var r=ps.GetComponent<ParticleSystemRenderer>();r.material=new Material(Shader.Find("Sprites/Default"));ps.Play();
        }
        public static void Rock(Transform parent,Vector3 pos,Vector3 scale,Material m)
        {
            var go=new GameObject("Faceted rock");go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;
            var source=new[]{new Vector3(-.6f,-.3f,-.5f),new Vector3(.5f,-.3f,-.5f),new Vector3(.6f,-.3f,.5f),new Vector3(-.5f,-.3f,.6f),new Vector3(-.25f,.55f,-.2f),new Vector3(.3f,.45f,.25f)};
            int[] indices={0,4,1,1,4,5,1,5,2,2,5,3,3,5,4,3,4,0,4,3,5,0,1,2,0,2,3};var v=new List<Vector3>();var tri=new List<int>();for(int i=0;i<indices.Length;i++){tri.Add(i);v.Add(source[indices[i]]);}var mesh=new Mesh();mesh.name="LTT geometry";mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=m;
        }
        static void Point(Transform p,Vector3 pos,float radius,float height,Material m)
        {
            var g=new GameObject("Point");g.transform.SetParent(p,false);g.transform.localPosition=pos;var mesh=new Mesh();mesh.name="LTT geometry";var verts=new List<Vector3>();var tri=new List<int>();
            for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5,b=(i+1)*Mathf.PI*2/5;int k=verts.Count;verts.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));verts.Add(new Vector3(0,height,0));verts.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));tri.Add(k);tri.Add(k+1);tri.Add(k+2);}mesh.SetVertices(verts);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;
        }
        public static void Unit(Transform p,Domain.Unit u,Func<string,Color,Material> mat)
        {
            material=mat;
            if(u.enemy&&u.kind==0&&u.villageId<=0)
            {
                var fur=material("wolf",new Color(.34f,.30f,.23f));Box(p,"Wolf body",new Vector3(0,.42f,0),new Vector3(.42f,.35f,.85f),fur);Box(p,"Wolf head",new Vector3(0,.61f,.43f),new Vector3(.32f,.32f,.36f),fur);Box(p,"Snout",new Vector3(0,.54f,.65f),new Vector3(.23f,.17f,.25f),fur);
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(p,"Leg",new Vector3(x*.15f,.2f,z*.27f),new Vector3(.1f,.4f,.12f),fur);
                Point(p,new Vector3(-.1f,.72f,.42f),.07f,.22f,fur);Point(p,new Vector3(.1f,.72f,.42f),.07f,.22f,fur);Beam(p,new Vector3(0,.44f,-.4f),new Vector3(0,.58f,-.7f),.11f,fur);return;
            }
            var cloth=material(u.enemy?"raider cloth":"guard cloth",u.enemy?new Color(.52f,.20f,.12f):new Color(.14f,.40f,.42f));var skin=material("skin",new Color(.84f,.64f,.42f));
            for(int i=-1;i<=1;i+=2){Box(p,"Boot",new Vector3(i*.13f,.13f,.03f),new Vector3(.17f,.26f,.25f),Wood);Box(p,"Leg",new Vector3(i*.13f,.36f,0),new Vector3(.16f,.36f,.17f),cloth);}
            Box(p,"Tunic",new Vector3(0,.71f,0),new Vector3(.44f,.5f,.30f),cloth);Box(p,"Belt",new Vector3(0,.58f,-.01f),new Vector3(.46f,.075f,.32f),Wood);
            Part(p,"Head",PrimitiveType.Sphere,new Vector3(0,1.08f,0),new Vector3(.31f,.37f,.3f),skin);
            Part(p,"Helmet crown",PrimitiveType.Sphere,new Vector3(0,1.22f,0),new Vector3(.35f,.22f,.35f),Metal);
            Part(p,"Helmet brim",PrimitiveType.Cylinder,new Vector3(0,1.16f,0),new Vector3(.43f,.025f,.43f),Metal);
            for(int i=-1;i<=1;i+=2){Part(p,"Shoulder armor",PrimitiveType.Sphere,new Vector3(i*.26f,.86f,0),new Vector3(.23f,.21f,.3f),Metal);Beam(p,new Vector3(i*.27f,.8f,0),new Vector3(i*.31f,.51f,.08f),.14f,cloth);}
            Box(p,"Cape",new Vector3(0,.77f,-.19f),new Vector3(.38f,.65f,.04f),cloth);
            if(u.kind==2){Part(p,"Shield",PrimitiveType.Cylinder,new Vector3(-.39f,.69f,.04f),new Vector3(.6f,.055f,.6f),Teal,new Vector3(0,0,90));Beam(p,new Vector3(.35f,.4f,0),new Vector3(.35f,1.25f,0),.07f,Wood);Box(p,"Hammer",new Vector3(.35f,1.15f,0),new Vector3(.42f,.24f,.25f),Metal);}
            else if(u.kind==0){Beam(p,new Vector3(.38f,.12f,.08f),new Vector3(.38f,1.6f,.08f),.045f,Wood);Point(p,new Vector3(.38f,1.6f,.08f),.08f,.3f,Metal);}
            else{Beam(p,new Vector3(.34f,.32f,0),new Vector3(.43f,.82f,.15f),.065f,Wood);Beam(p,new Vector3(.43f,.82f,.15f),new Vector3(.34f,1.2f,0),.065f,Wood);Beam(p,new Vector3(.34f,.32f,0),new Vector3(.34f,1.2f,0),.014f,Metal);}
        }
        public static void Worker(Transform p,string kind,Func<string,Color,Material> mat)
        {
            material=mat;var cloth=material("worker robe",new Color(.42f,.35f,.23f));var skin=material("worker skin",new Color(.84f,.64f,.42f));
            for(int i=-1;i<=1;i+=2){Box(p,"Leg",new Vector3(i*.13f,.29f,0),new Vector3(.16f,.5f,.17f),cloth);Beam(p,new Vector3(i*.26f,.83f,0),new Vector3(i*.31f,.51f,.08f),.14f,cloth);}
            Box(p,"Robe",new Vector3(0,.71f,0),new Vector3(.44f,.5f,.3f),cloth);Box(p,"Belt",new Vector3(0,.58f,0),new Vector3(.46f,.07f,.32f),Wood);
            Part(p,"Head",PrimitiveType.Sphere,new Vector3(0,1.08f,0),new Vector3(.31f,.37f,.3f),skin);Point(p,new Vector3(0,1.2f,0),.36f,.18f,cloth);
            Beam(p,new Vector3(.36f,.36f,0),new Vector3(.36f,1.25f,0),.05f,Wood);
            Box(p,kind=="tree"?"Axe":"Tool",new Vector3(.42f,1.16f,0),kind=="tree"?new Vector3(.28f,.22f,.07f):new Vector3(.42f,.08f,.08f),Metal);
        }
        public static void Combine(Transform root,bool keepLegs=false)
        {
            var owner=root.gameObject.AddComponent<GeneratedMeshOwner>();
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                if(keepLegs&&f.name=="Leg")continue;
                if(f.GetComponent<SliceWater>()!=null)continue;
                var renderer=f.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||f.sharedMesh==null)continue;
                if(f.sharedMesh.name.StartsWith("LTT "))owner.meshes.Add(f.sharedMesh);
                var mat=renderer.sharedMaterial;if(!groups.ContainsKey(mat))groups[mat]=new List<CombineInstance>();
                groups[mat].Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});renderer.enabled=false;
            }
            foreach(var pair in groups)
            {
                var go=new GameObject("Combined "+pair.Key.name);go.transform.SetParent(root,false);var mesh=new Mesh();mesh.name="LTT combined";mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.UploadMeshData(true);owner.meshes.Add(mesh);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
            }
        }
        public static void Deer(Transform p,Func<string,Color,Material> mat)
        {
            material=mat;var fur=material("deer fur",new Color(.59f,.37f,.18f));var light=material("deer cream",new Color(.85f,.73f,.49f));
            Box(p,"Deer body",new Vector3(0,.75f,0),new Vector3(.45f,.5f,1.05f),fur);Beam(p,new Vector3(0,.8f,.35f),new Vector3(0,1.25f,.55f),.23f,fur);Box(p,"Deer head",new Vector3(0,1.32f,.65f),new Vector3(.27f,.29f,.4f),fur);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Beam(p,new Vector3(x*.15f,.7f,z*.35f),new Vector3(x*.17f,.05f,z*.4f),.085f,fur);
            for(int side=-1;side<=1;side+=2){Beam(p,new Vector3(side*.08f,1.44f,.58f),new Vector3(side*.3f,1.85f,.4f),.035f,light);Beam(p,new Vector3(side*.19f,1.66f,.49f),new Vector3(side*.36f,1.77f,.59f),.028f,light);}
            Box(p,"Deer tail",new Vector3(0,.85f,-.57f),new Vector3(.13f,.16f,.25f),light);
        }
    }
    public sealed class GeneratedMeshOwner:MonoBehaviour
    {
        public readonly HashSet<Mesh> meshes=new HashSet<Mesh>();
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
    }
}


using System;
using System.Collections.Generic;
using UnityEngine;

namespace LangTroTan.Presentation
{
    public partial class Game
    {
        GameObject buildGrid;
        float TerrainHeight(float x,float z)
        {return Mathf.Clamp01((Mathf.Sqrt(x*x+z*z)-24)/18)*Mathf.PerlinNoise(x*.055f+7,z*.055f+3)*5;}
        void CreateWorld()
        {
            var cg=new GameObject("Camera");cam=cg.AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.nearClipPlane=.1f;cam.farClipPlane=200;
            cam.backgroundColor=new Color(.48f,.57f,.50f);cam.clearFlags=CameraClearFlags.SolidColor;cg.AddComponent<AudioListener>();cameraSize=16.5f;UpdateCamera();
            sun=new GameObject("Late afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(43,-35,0);sun.intensity=1.05f;sun.color=new Color(1,.89f,.72f);sun.shadows=LightShadows.Soft;
            QualitySettings.shadowDistance=Application.isMobilePlatform?65:85;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowCascades=2;QualitySettings.antiAliasing=2;RenderSettings.ambientLight=new Color(.52f,.60f,.53f);sun.shadowStrength=.65f;sun.shadowBias=.035f;sun.shadowNormalBias=.3f;
            RenderSettings.fog=true;RenderSettings.fogColor=cam.backgroundColor;RenderSettings.fogStartDistance=55;RenderSettings.fogEndDistance=125;RenderSettings.fogMode=FogMode.Linear;
            var groundMat=Mat("Meadow",new Color(.33f,.43f,.23f));var tex=new Texture2D(256,256);tex.wrapMode=TextureWrapMode.Repeat;
            for(int x=0;x<256;x++)for(int z=0;z<256;z++){float v=.77f+Mathf.PerlinNoise(x*.08f,z*.08f)*.3f;tex.SetPixel(x,z,new Color(v,v,v));}tex.Apply();groundMat.mainTexture=tex;groundMat.mainTextureScale=new Vector2(10,10);
            int n=72;var verts=new Vector3[(n+1)*(n+1)];var uv=new Vector2[verts.Length];var tri=new List<int>();
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){float a=x*2-72,b=z*2-72;int k=z*(n+1)+x;verts[k]=new Vector3(a,TerrainHeight(a,b)-.015f,b);uv[k]=new Vector2(x/(float)n,z/(float)n);
                if(x<n&&z<n){tri.Add(k);tri.Add(k+n+1);tri.Add(k+1);tri.Add(k+1);tri.Add(k+n+1);tri.Add(k+n+2);}}
            var mesh=new Mesh();mesh.vertices=verts;mesh.uv=uv;mesh.SetTriangles(tri,0);mesh.RecalculateNormals();var terrain=new GameObject("Rolling meadow");terrain.AddComponent<MeshFilter>().sharedMesh=mesh;terrain.AddComponent<MeshRenderer>().sharedMaterial=groundMat;
            var dirt=Mat("Village earth",new Color(.53f,.42f,.27f));
            var plaza=new GameObject("Worn village clearing");var pv=new List<Vector3>{new Vector3(1,.007f,0)};var pt=new List<int>();
            for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32,r=6.8f+Mathf.Sin(i*1.7f)*.55f;pv.Add(new Vector3(1+Mathf.Cos(a)*r,.008f,Mathf.Sin(a)*r*.73f));if(i>0){pt.Add(0);pt.Add(i+1);pt.Add(i);}}
            var pm=new Mesh();pm.SetVertices(pv);pm.SetTriangles(pt,0);pm.RecalculateNormals();plaza.AddComponent<MeshFilter>().sharedMesh=pm;plaza.AddComponent<MeshRenderer>().sharedMaterial=dirt;
            Path(new[]{new Vector3(-.5f,0,-16),new Vector3(-1,0,-8),new Vector3(-3.2f,0,-3),new Vector3(-3.3f,0,3),new Vector3(-.5f,0,9),new Vector3(0,0,22)},1.25f,dirt);
            Path(new[]{new Vector3(-3,0,-3),new Vector3(4,0,-4),new Vector3(10,0,-4.5f),new Vector3(21,0,-2)},1.1f,dirt);
            var nature=new GameObject("Forest and meadow");var rng=new System.Random(83);
            var bark=Mat("pine trunk",new Color(.28f,.20f,.12f));var pine=Mat("pine needles",new Color(.15f,.27f,.16f));var leaves=Mat("broad leaves",new Color(.29f,.40f,.16f));
            var grass=Mat("grass blades",new Color(.37f,.45f,.18f));
            for(int i=0;i<180;i++)
            {
                float x=-23+(float)rng.NextDouble()*46,z=-22+(float)rng.NextDouble()*44;if(x*x+z*z<90)continue;
                if(i%5==0){Shape("Wildflower",PrimitiveType.Sphere,new Vector3(x,.19f,z),new Vector3(.15f,.19f,.15f),Mat("Meadow flowers",new Color(.75f,.65f,.32f)),nature.transform);}
                else{var blade=Shape("Grass clump",PrimitiveType.Cube,new Vector3(x,.16f,z),new Vector3(.045f,.32f,.2f),grass,nature.transform);blade.transform.localRotation=Quaternion.Euler(0,i*51,13);}
            }
            VillageArt.Combine(nature.transform);
            selection=Ring("Selected",1.4f,gold);selection.SetActive(false);orderMarker=Ring("Order",.7f,gold);orderMarker.SetActive(false);CreateBuildGrid();
        }
        void Path(Vector3[] points,float width,Material mat)
        {
            var v=new List<Vector3>();var tris=new List<int>();
            for(int i=0;i<points.Length;i++)
            {
                var dir=points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)];var side=Vector3.Cross(dir.normalized,Vector3.up)*width*.5f;
                v.Add(points[i]+side+Vector3.up*.018f);v.Add(points[i]-side+Vector3.up*.018f);
                if(i>0){int k=i*2;tris.Add(k-2);tris.Add(k);tris.Add(k-1);tris.Add(k-1);tris.Add(k);tris.Add(k+1);}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();var go=new GameObject("Village footpath");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        void CreateBuildGrid()
        {
            buildGrid=new GameObject("Construction grid");var verts=new List<Vector3>();var tris=new List<int>();
            for(int axis=0;axis<2;axis++)for(int i=-24;i<=24;i++)
            {
                if(axis==0){int k=verts.Count;verts.Add(new Vector3(i-.012f,.035f,-24));verts.Add(new Vector3(i+.012f,.035f,-24));verts.Add(new Vector3(i+.012f,.035f,24));verts.Add(new Vector3(i-.012f,.035f,24));tris.AddRange(new[]{k,k+3,k+1,k+1,k+3,k+2});}
                else{int k=verts.Count;verts.Add(new Vector3(-24,.036f,i-.012f));verts.Add(new Vector3(24,.036f,i-.012f));verts.Add(new Vector3(24,.036f,i+.012f));verts.Add(new Vector3(-24,.036f,i+.012f));tris.AddRange(new[]{k,k+3,k+1,k+1,k+3,k+2});}
            }
            var m=new Mesh();m.SetVertices(verts);m.SetTriangles(tris,0);m.RecalculateNormals();buildGrid.AddComponent<MeshFilter>().sharedMesh=m;
            var gridMat=new Material(Shader.Find("Sprites/Default"));gridMat.color=new Color(.95f,.88f,.61f,.2f);buildGrid.AddComponent<MeshRenderer>().sharedMaterial=gridMat;buildGrid.SetActive(false);
        }
    }
}

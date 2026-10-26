using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace LangTroTan.Presentation
{
    public static class UISkin
    {
        static Sprite frame,fade;static Texture2D atlas;static readonly Dictionary<int,Sprite> icons=new Dictionary<int,Sprite>();
        public static Sprite Frame
        {
            get
            {
                if(frame!=null)return frame;int n=64;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);tex.wrapMode=TextureWrapMode.Clamp;
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    int edge=Mathf.Min(Mathf.Min(x,n-1-x),Mathf.Min(y,n-1-y));float grain=Mathf.Sin(y*1.7f+x*.06f)*.018f+Mathf.Sin(y*.35f+x*.02f)*.028f;
                    var c=new Color(.74f+grain,.68f+grain,.58f+grain,1);
                    if(edge<2)c=new Color(.15f,.12f,.075f,1);else if(edge<4)c=new Color(.9f,.75f,.43f,1);else if(edge<6)c=new Color(.35f,.27f,.15f,1);
                    if((new Vector2(x-9,y-9).sqrMagnitude<5)||(new Vector2(x-54,y-9).sqrMagnitude<5)||(new Vector2(x-9,y-54).sqrMagnitude<5)||(new Vector2(x-54,y-54).sqrMagnitude<5))c=new Color(.94f,.82f,.55f,1);
                    tex.SetPixel(x,y,c);
                }
                tex.Apply();frame=Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));return frame;
            }
        }
        public static Sprite Fade
        {
            get
            {
                if(fade!=null)return fade;var t=new Texture2D(128,2,TextureFormat.RGBA32,false);
                for(int x=0;x<128;x++)for(int y=0;y<2;y++)t.SetPixel(x,y,new Color(.035f,.045f,.035f,Mathf.Lerp(.94f,0,Mathf.SmoothStep(0,1,x/127f))));
                t.Apply();fade=Sprite.Create(t,new Rect(0,0,128,2),Vector2.one*.5f);return fade;
            }
        }
        public static Sprite Icon(int index)
        {
            if(icons.TryGetValue(index,out var s))return s;
            if(index>=16){var names=new[]{"School0","School2","School1","School3","School1","School4","School0"};var t=Resources.Load<Texture2D>("Art/Sect/"+names[Mathf.Clamp(index-16,0,6)]);if(t==null)return null;s=Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,100);icons[index]=s;return s;}
            if(atlas==null)atlas=Resources.Load<Texture2D>("Art/UI/MedievalIcons");if(atlas==null)return null;
            float w=atlas.width/4f,h=atlas.height/4f;int col=index%4,row=index/4;
            s=Sprite.Create(atlas,new Rect(col*w,(3-row)*h,w,h),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);icons[index]=s;return s;
        }
        public static int BuildingIcon(string id)
        {
            switch(id){case "scripture":return 16;case "meditation":return 17;case "spiritmine":return 18;case "herbgarden":return 19;case "alchemy":return 20;case "forge":return 21;case "mission":return 22;case "hall":return 16;case "house":return 5;case "farm":return 6;case "store":return 7;case "wood":return 8;case "stone":return 9;case "barracks":return 10;case "tower":return 11;case "wall":return 12;case "gate":return 13;case "hunter":return 3;case "forester":return 8;case "well":return 1;case "hearth":return 4;default:return 15;}
        }
        public static Sprite Background()
        {
            var t=Resources.Load<Texture2D>("Art/Sect/SectBackdrop");return t==null?null:Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f);
        }
    }
    public sealed class MiniMapClick:MonoBehaviour,IPointerClickHandler
    {
        public Action<Vector2> onClick;
        public void OnPointerClick(PointerEventData data)
        {
            var r=(RectTransform)transform;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(r,data.position,data.pressEventCamera,out var p))onClick?.Invoke(new Vector2((p.x-r.rect.xMin)/r.rect.width,(p.y-r.rect.yMin)/r.rect.height));
        }
    }
    public sealed class ButtonMotion:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        Vector3 goal=Vector3.one;
        public void OnPointerDown(PointerEventData e){goal=Vector3.one*.96f;}
        public void OnPointerUp(PointerEventData e){goal=Vector3.one;}
        public void OnPointerExit(PointerEventData e){goal=Vector3.one;}
        void Update(){transform.localScale=Vector3.Lerp(transform.localScale,goal,Time.unscaledDeltaTime*20);}
    }
}

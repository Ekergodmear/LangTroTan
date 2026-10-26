using UnityEngine;
using UnityEngine.UI;

namespace LangTroTan.Presentation
{
    public sealed class SafeArea:MonoBehaviour
    {
        public RectTransform content;
        public CanvasScaler scaler;
        Rect last;Vector2 lastSize;
        public static Vector2 ReferenceFor(Rect safe,int width,int height)
        {
            float w=Mathf.Clamp(safe.width/Mathf.Max(1,width),.1f,1);
            float h=Mathf.Clamp(safe.height/Mathf.Max(1,height),.1f,1);
            return new Vector2(1600/w,900/h);
        }
        void Update(){if(last!=Screen.safeArea||lastSize!=new Vector2(Screen.width,Screen.height))Apply();}
        public void Apply()
        {
            if(content==null||scaler==null)return;
            var safe=Screen.safeArea;if(safe.width<=0||safe.height<=0)safe=new Rect(0,0,Screen.width,Screen.height);
            float w=Mathf.Max(1,Screen.width),h=Mathf.Max(1,Screen.height);
            content.anchorMin=new Vector2(safe.xMin/w,safe.yMin/h);content.anchorMax=new Vector2(safe.xMax/w,safe.yMax/h);
            content.offsetMin=content.offsetMax=Vector2.zero;
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution=ReferenceFor(safe,Screen.width,Screen.height);
            last=safe;lastSize=new Vector2(Screen.width,Screen.height);
        }
    }
}

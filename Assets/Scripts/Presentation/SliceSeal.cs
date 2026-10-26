using UnityEngine;
using UnityEngine.UI;
namespace LangTroTan.Presentation
{
    public sealed class SliceSeal:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;var centre=rect.center;float radius=Mathf.Min(rect.width,rect.height)*.48f;
            for(int petal=0;petal<6;petal++){
                float angle=petal*Mathf.PI/3;var along=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle));var across=new Vector2(along.y,-along.x);int first=vh.currentVertCount;vh.AddVert(centre,color,Vector2.zero);
                for(int j=0;j<=16;j++){float t=j/16f*Mathf.PI*2;float length=(1-Mathf.Cos(t))*.5f;var point=centre+along*radius*length+across*Mathf.Sin(t)*radius*.28f;vh.AddVert(point,color,Vector2.zero);if(j>0)vh.AddTriangle(first,first+j,first+j+1);}
            }
        }
    }
}

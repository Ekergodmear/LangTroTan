using System.Linq;
using UnityEngine;

namespace LangTroTan.Presentation
{
    public sealed class ActorMotion:MonoBehaviour
    {
        Transform[] legs;Vector3 previous;float blend,phase;
        void Start(){legs=GetComponentsInChildren<Transform>().Where(t=>t.name=="Leg").ToArray();previous=transform.position;}
        void LateUpdate()
        {
            float distance=Vector3.Distance(previous,transform.position);previous=transform.position;blend=Mathf.Lerp(blend,distance>.001f?1:0,Time.deltaTime*12);phase+=Time.deltaTime*11;
            for(int i=0;i<legs.Length;i++)legs[i].localRotation=Quaternion.Euler(Mathf.Sin(phase+i*Mathf.PI)*26*blend,0,0);
        }
    }
}

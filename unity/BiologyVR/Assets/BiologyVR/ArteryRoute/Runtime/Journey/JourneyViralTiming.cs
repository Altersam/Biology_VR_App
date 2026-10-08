using UnityEngine;
namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Final-exam pulse/mark rhythm on an existing incoming virion.</summary>
    public sealed class JourneyViralTiming:MonoBehaviour
    {
        public bool Shielded;
        public bool Pulsed{get;private set;}
        float born;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        public bool WindowOpen=>Time.time-born>.3f&&Mathf.Repeat(Time.time-born,1.6f)>.25f&&Mathf.Repeat(Time.time-born,1.6f)<1.35f;
        void Awake(){born=Time.time;renderers=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();}
        public void Pulse(){Shielded=false;Pulsed=true;}
        void LateUpdate()
        {
            foreach(var r in renderers)if(r){r.GetPropertyBlock(block);block.SetColor("_EmissionColor",WindowOpen?Shielded?new Color(.12f,.055f,.005f):new Color(.005f,.055f,.055f):Color.black);r.SetPropertyBlock(block);}
        }
    }
}

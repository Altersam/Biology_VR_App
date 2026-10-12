using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public enum JourneyImmuneVirusState{Flowing,Targetable,Targeted,Engaged,BeingPhagocytosed,Neutralized}
    public sealed class JourneyImmuneVirus:MonoBehaviour
    {
        public JourneyTarget target;
        public Renderer body;
        public SphereCollider contact;
        public JourneyImmuneVirusState State{get;private set;}=JourneyImmuneVirusState.Neutralized;
        public float arc,laneX,laneY,speed;
        public Vector3 initialScale;
        public int Generation{get;private set;}
        public bool Live=>State!=JourneyImmuneVirusState.Neutralized;
        public void Begin(float entry,float x,float y,float velocity)
        {arc=entry;laneX=x;laneY=y;speed=velocity;Generation++;SetState(JourneyImmuneVirusState.Flowing);transform.localScale=initialScale;gameObject.SetActive(true);contact.enabled=true;}
        public void SetState(JourneyImmuneVirusState state){State=state;}
        public void Return(){SetState(JourneyImmuneVirusState.Neutralized);contact.enabled=false;gameObject.SetActive(false);}
    }
}

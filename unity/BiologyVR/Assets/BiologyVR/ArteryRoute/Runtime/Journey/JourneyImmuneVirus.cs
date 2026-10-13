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
        public int Integrity{get;private set;}=3;
        public void Damage(){Integrity=Mathf.Max(0,Integrity-1);}
        public bool Live=>State!=JourneyImmuneVirusState.Neutralized;
        public void Begin(float entry,float x,float y,float velocity)
        {arc=entry;laneX=x;laneY=y;speed=velocity;Generation++;Integrity=3;SetState(JourneyImmuneVirusState.Flowing);transform.localScale=initialScale;gameObject.SetActive(true);contact.enabled=true;}
        public void SetState(JourneyImmuneVirusState state){State=state;}
        public void Return(){SetState(JourneyImmuneVirusState.Neutralized);contact.enabled=false;gameObject.SetActive(false);}
    }
}

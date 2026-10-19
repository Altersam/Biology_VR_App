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
        public bool evasive;
        public Transform[] spikeGroups;
        public Vector3 MotionVelocity{get;private set;}
        float motionAge,motionSeed;
        bool singleContactTutorial;
        public void Damage(){Integrity=Mathf.Max(0,Integrity-1);RefreshSpikes();}
        public void UseSingleContactTutorial(){singleContactTutorial=true;Integrity=1;RefreshSpikes();}
        void RefreshSpikes(){if(spikeGroups==null)return;int visible=singleContactTutorial&&Integrity>0?3:Integrity;for(int i=0;i<spikeGroups.Length;i++)if(spikeGroups[i])spikeGroups[i].gameObject.SetActive(i<visible);}
        public bool Live=>State!=JourneyImmuneVirusState.Neutralized;
        public void Begin(float entry,float x,float y,float velocity)
        {arc=entry;laneX=x;laneY=y;speed=velocity;Generation++;singleContactTutorial=false;Integrity=3;motionAge=0;motionSeed=Random.Range(0,6.28f);RefreshSpikes();SetState(JourneyImmuneVirusState.Flowing);transform.localScale=initialScale;gameObject.SetActive(true);contact.enabled=true;}
        public void StepEvasive(JourneyWorld world,float dt)
        {
            motionAge+=dt;float t=motionAge,s=motionSeed;
            float forward=.75f*Mathf.Sin(t*1.45f+s),x=laneX+.78f*Mathf.Sin(t*1.85f+s*.7f),y=laneY+.68f*Mathf.Sin(t*2.05f+s*1.3f);
            transform.SetPositionAndRotation(world.mover.path.Offset(arc+forward,x,y),world.mover.path.Frame(arc)*Quaternion.Euler(t*23,t*31,t*13));MotionVelocity=world.mover.path.Forward(arc)*(.75f*1.45f*Mathf.Cos(t*1.45f+s))+world.mover.path.Right(arc)*(.78f*1.85f*Mathf.Cos(t*1.85f+s*.7f))+world.mover.path.Up(arc)*(.68f*2.05f*Mathf.Cos(t*2.05f+s*1.3f));
        }
        public Vector3 PredictPosition(JourneyWorld world,float seconds)
        {
            if(!evasive)return world.mover.path.Offset(arc+speed*seconds,laneX,laneY);
            float t=motionAge+seconds,s=motionSeed;
            return world.mover.path.Offset(arc+.75f*Mathf.Sin(t*1.45f+s),laneX+.78f*Mathf.Sin(t*1.85f+s*.7f),laneY+.68f*Mathf.Sin(t*2.05f+s*1.3f));
        }
        public void SetState(JourneyImmuneVirusState state){State=state;}
        public void Return(){SetState(JourneyImmuneVirusState.Neutralized);contact.enabled=false;gameObject.SetActive(false);}
    }
}

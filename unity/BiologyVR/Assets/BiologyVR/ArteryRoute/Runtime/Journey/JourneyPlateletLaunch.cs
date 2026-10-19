using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Flight/adhesion layer for actual leased platelets; existing puzzle stores the plug.</summary>
    public sealed class JourneyPlateletLaunch:MonoBehaviour
    {
        public JourneyWorld world;
        public LineRenderer preview;
        public int Hits{get;private set;}
        public int Misses{get;private set;}
        public bool Flying=>flying;
        public bool PreviewOnWound{get;private set;}
        public Vector3 PreviewContact{get;private set;}
        public float LastAdhesionTime{get;private set;}=-100;
        public float ResultUntil{get;private set;}
        public string ResultMessage{get;private set;}
        JourneyTarget flying;
        Vector3 velocity;
        float age;
        int scene=-1;
        Vector3 LaunchVelocity(JourneyTarget target,Transform aim,float charge)
        {
            float speed=Mathf.Lerp(2,7,charge);var direction=aim.forward;
            if(TryAimWound(aim,out var point))
            {
                float travel=Vector3.Distance(target.transform.position,point)/speed;direction=(point-target.transform.position-world.mover.path.Forward(world.mover.Distance)*(.15f*travel)).normalized;
            }
            return direction*speed;
        }
        public bool TryAimWound(Transform aim,out Vector3 point)
        {
            point=Vector3.zero;if(!aim||world.mission.SceneNumber!=12)return false;
            var outward=world.woundState.Outward;var plane=new Plane(outward,world.woundSite.position-outward*.08f);var ray=new Ray(aim.position,aim.forward);
            if(!plane.Raycast(ray,out float distance)||distance>12)return false;
            point=ray.GetPoint(distance);var offset=point-world.woundSite.position;var path=world.mover.path;float s=path.Anchor(11)+5;
            return Mathf.Abs(Vector3.Dot(offset,path.Forward(s)))<=1.30f&&Mathf.Abs(Vector3.Dot(offset,path.Up(s)))<=.28f;
        }
        public void Preview(JourneyTarget target,Transform aim,float charge)
        {
            var cell=target.GetComponent<JourneyPlateletEntity>();preview.enabled=cell&&cell.ScannerActivated;preview.positionCount=16;
            PreviewOnWound=TryAimWound(aim,out var point);PreviewContact=point;
            var start=target.transform.position;var v=LaunchVelocity(target,aim,charge)+world.mover.path.Forward(world.mover.Distance)*.15f;
            float duration=PreviewOnWound?Vector3.Distance(start,point)/Mathf.Max(.1f,v.magnitude):1.2f;
            for(int i=0;i<16;i++)preview.SetPosition(i,start+v*(i/15f*duration));
        }
        public void Launch(JourneyTarget target,Transform aim,float charge)
        {
            preview.enabled=false;var cell=target.GetComponent<JourneyPlateletEntity>();
            if(!cell||!cell.ScannerActivated||flying){world.bloodEntities.Release(target,true);ResultMessage="Сначала активируй тромбоцит левым Scanner.";ResultUntil=Time.time+4;world.mission.FeedbackMessage(ResultMessage);return;}
            ResultUntil=0;
            flying=target;velocity=LaunchVelocity(target,aim,charge);age=0;cell.SetFlying(true);target.GetComponent<SphereCollider>().enabled=false;target.GetComponent<XRGrabInteractable>().enabled=false;world.mission.GetComponent<JourneyInstrumentRig>().PhagocyteReleased();
        }
        void Update()
        {
            if(scene!=world.mission.SceneNumber){if(flying)Return(false);scene=world.mission.SceneNumber;preview.enabled=false;ResultUntil=0;}
            if(!world.freeTools.Held){preview.enabled=false;PreviewOnWound=false;}
            if(!flying)return;age+=Time.deltaTime;var from=flying.transform.position;var to=from+(velocity+world.mover.path.Forward(world.mover.Distance)*.15f)*Time.deltaTime;
            flying.transform.position=to;
            if(world.mission.SceneNumber==12)
            {
                if(world.hemostasisPuzzle.TryAdhereAtWound(from,to)){Hits++;LastAdhesionTime=Time.time;ResultMessage="ПРИКРЕПИЛСЯ · утечка уменьшается";ResultUntil=Time.time+2;world.mission.FeedbackMessage(ResultMessage);Return(true);return;}
            }
            if(age>2.5f){Misses++;ResultMessage="Мимо. Направь конец траектории внутрь контура раны.";ResultUntil=Time.time+4;world.mission.FeedbackMessage(ResultMessage);Return(false);}
        }
        void Return(bool used){var cell=flying.GetComponent<JourneyPlateletEntity>();cell.SetFlying(false);world.bloodEntities.Release(flying,true);flying=null;}
        static float Segment(Vector3 point,Vector3 a,Vector3 b){var d=b-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,d)/Mathf.Max(.00001f,d.sqrMagnitude));return Vector3.Distance(point,a+d*t);}
    }
}

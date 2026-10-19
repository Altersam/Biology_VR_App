using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public enum JourneyPlateletState{Flowing,Inactive,Activated,Adhered,Aggregated}
    public sealed class JourneyPlateletEntity:MonoBehaviour
    {
        public JourneyPlateletState State{get;private set;}=JourneyPlateletState.Flowing;
        public MeshFilter body;
        public Mesh resting,activated;
        public bool ScannerActivated{get;private set;}
        public bool Flying{get;private set;}
        public void ActivateByScanner(){ScannerActivated=true;SetState(JourneyPlateletState.Activated);}
        public void SetFlying(bool value){Flying=value;}
        public void ResetFlow(){ScannerActivated=Flying=false;SetState(JourneyPlateletState.Flowing);}
        Mesh inactiveMesh;
        Vector3 inactiveScale;
        public void SetState(JourneyPlateletState state)
        {
            bool wasActive=State==JourneyPlateletState.Activated||State==JourneyPlateletState.Adhered||State==JourneyPlateletState.Aggregated;State=state;if(!body||!activated)return;
            bool active=state==JourneyPlateletState.Activated||state==JourneyPlateletState.Adhered||state==JourneyPlateletState.Aggregated;
            if(active&&!wasActive){inactiveMesh=body.sharedMesh;inactiveScale=body.transform.localScale;}
            if(active){float width=body.sharedMesh.bounds.size.magnitude*body.transform.localScale.x;body.sharedMesh=activated;if(body.transform!=transform)body.transform.localScale=Vector3.one*(width/Mathf.Max(.001f,activated.bounds.size.magnitude));}
            else if(wasActive&&inactiveMesh){body.sharedMesh=inactiveMesh;if(body.transform!=transform)body.transform.localScale=inactiveScale;}
        }
    }
}

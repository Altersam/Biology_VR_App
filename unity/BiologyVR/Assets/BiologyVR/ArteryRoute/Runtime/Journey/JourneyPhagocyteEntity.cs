using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public enum JourneyPhagocyteState{Flowing,Scanned,Activated,Captured,Charged,Launched,Phagocytosis,Recovering}
    public sealed class JourneyPhagocyteEntity:MonoBehaviour
    {
        public JourneyTarget target;
        public Transform[] pseudopodia;
        public Renderer body;
        public JourneyPhagocyteState State{get;private set;}
        public int CellIndex{get;private set;}=-1;
        public int Generation{get;private set;}
        public int RemainingCapacity{get;private set;}=3;
        public bool Studied{get;set;}
        public Vector3 launchVelocity;
        public float stateAge;
        public bool Busy=>State==JourneyPhagocyteState.Captured||State==JourneyPhagocyteState.Charged||State==JourneyPhagocyteState.Launched||State==JourneyPhagocyteState.Phagocytosis||State==JourneyPhagocyteState.Recovering;
        public void ResetForCell(int index,int generation){CellIndex=index;Generation=generation;RemainingCapacity=3;Studied=false;SetState(JourneyPhagocyteState.Flowing);}
        public void SetState(JourneyPhagocyteState state){State=state;stateAge=0;}
        public void Used(){RemainingCapacity=Mathf.Max(0,RemainingCapacity-1);}
        public void RestoreCapacity(int value){RemainingCapacity=Mathf.Clamp(value,0,3);}
        public void Animate(float phase,Vector3 toward)
        {
            if(pseudopodia==null)return;
            for(int i=0;i<pseudopodia.Length;i++)
            {var limb=pseudopodia[i];limb.gameObject.SetActive(phase>.02f);if(!limb.gameObject.activeSelf)continue;var direction=Quaternion.AngleAxis(i*360f/pseudopodia.Length,Vector3.forward)*new Vector3(.6f,.3f,.4f);if(toward.sqrMagnitude>.001f)direction=Vector3.Lerp(direction.normalized,toward.normalized,.55f);limb.localPosition=direction.normalized*.16f;limb.localRotation=Quaternion.LookRotation(direction);limb.localScale=new Vector3(.055f,.055f,.07f+phase*.23f);}
        }
    }
}

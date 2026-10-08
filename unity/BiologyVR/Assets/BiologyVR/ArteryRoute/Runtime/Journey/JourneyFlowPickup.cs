using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>A study cell binds to one real pooled blood-flow slot, not a stationary display dock.</summary>
    public sealed class JourneyFlowPickup:MonoBehaviour
    {
        public JourneyBloodFlow flow;
        public JourneyTarget target;
        public int poolIndex;
        public float startDistance=1.5f;
        public Vector2 startLane;
        public bool InStudyFlow=>flow&&target&&target.mission&&target.mission.SceneNumber==3&&gameObject.activeInHierarchy;
        public bool Held=>target&&(target.grabbed||target.DesktopHeld)||(grab&&grab.isSelected);
        public bool Rejoining{get;private set;}
        public int HiddenReentries{get;private set;}
        XRGrabInteractable grab;
        Vector3 releasePosition,releaseScale,normalScale;
        Quaternion releaseRotation;
        float blend;
        readonly Plane[] viewPlanes=new Plane[6];
        void Awake(){grab=GetComponent<XRGrabInteractable>();normalScale=transform.localScale;}
        public bool TryHiddenReentry(float arc,Camera camera,out float entryArc)
        {
            entryArc=arc;
            if(!InStudyFlow||Held||Rejoining||!camera||arc<flow.path.Anchor(3)+5)return false;
            float incomingArc=Mathf.Max(.1f,flow.path.Anchor(3)-2.5f);
            Vector3 incoming=flow.path.Offset(incomingArc,startLane.x,startLane.y);
            GeometryUtility.CalculateFrustumPlanes(camera,viewPlanes);
            // A generous bound covers the model, label and stereo-eye separation.
            // Never wrap either endpoint while the player can see it.
            if(GeometryUtility.TestPlanesAABB(viewPlanes,new Bounds(transform.position,Vector3.one*2))||
                GeometryUtility.TestPlanesAABB(viewPlanes,new Bounds(incoming,Vector3.one*2)))return false;
            entryArc=incomingArc;HiddenReentries++;return true;
        }
        public void StartReturn(Transform parent,Vector3 scale)
        {
            if(!flow)return;
            transform.SetParent(parent,true);releasePosition=transform.position;releaseRotation=transform.rotation;releaseScale=transform.localScale;normalScale=scale;
            Rejoining=true;blend=0;flow.ResumePickupAt(poolIndex,releasePosition);
        }
        public void FollowPool(Transform source,float deltaTime)
        {
            if(!InStudyFlow||Held)return;
            if(Rejoining)
            {
                blend=Mathf.Min(1,blend+deltaTime/.7f);float t=blend*blend*(3-2*blend);
                transform.SetPositionAndRotation(Vector3.Lerp(releasePosition,source.position,t),Quaternion.Slerp(releaseRotation,source.rotation,t));
                transform.localScale=Vector3.Lerp(releaseScale,normalScale,t);
                if(blend>=1)Rejoining=false;
            }
            else transform.SetPositionAndRotation(source.position,source.rotation);
        }
    }
}

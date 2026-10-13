using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute.Journey
{
    [Serializable] public sealed class JourneyBloodInteractiveSlot
    {
        public string type;
        public JourneyTarget target;
        public MeshFilter body;
        public SphereCollider contact;
        public XRGrabInteractable grab;
        public int index=-1;
        public float touched;
        public Vector3 originalScale;
        public Renderer[] legacyRenderers;
        public bool[] legacyVisibility;
        [NonSerialized] public Vector3 originalContactCenter;
        [NonSerialized] public float originalContactRadius;
        [NonSerialized] public bool returning;
    }
    /// <summary>A bounded set of existing XRI targets leases concrete cells from the existing visual pool.</summary>
    [DefaultExecutionOrder(450)]
    public sealed class JourneyBloodInteractionPool:MonoBehaviour
    {
        public JourneyWorld world;
        public JourneyContinuousBloodstream stream;
        public JourneyBloodInteractiveSlot[] slots;
        int lastScene=-1;
        int[] capacityGeneration,capacityRemaining;
        void Awake(){capacityGeneration=new int[stream.flow.cells.Length];capacityRemaining=new int[capacityGeneration.Length];for(int i=0;i<capacityGeneration.Length;i++)capacityGeneration[i]=-1;foreach(var slot in slots){slot.originalContactCenter=slot.contact.center;slot.originalContactRadius=slot.contact.radius;}}
        readonly RaycastHit[] obstacles=new RaycastHit[64];
        public int Acquisitions{get;private set;}
        public int Returns{get;private set;}
        public bool HasPhagocyteCapacity(int index)=>capacityGeneration[index]!=stream.Generation(index)||capacityRemaining[index]>0;
        bool wasActive;
        public bool Active=>world.freeTools?world.mission.SceneNumber>=3&&world.mission.SceneNumber<=16:world.mission.SceneNumber==3||world.mission.SceneNumber==8||world.mission.SceneNumber==9||world.mission.SceneNumber==15&&world.mission.Current?.target=="immune-engulf";
        public static string ResearchType(string species)
        {if(species.StartsWith("erythrocyte"))return "rbc";if(species=="leukocyte"||species=="phagocyte"||species=="platelet"||species=="hemoglobin")return species;return "plasma";}
        public bool Owns(JourneyTarget target){foreach(var slot in slots)if(slot.target==target)return slot.index>=0;return false;}
        public JourneyBloodInteractiveSlot Slot(JourneyTarget target){foreach(var slot in slots)if(slot.target==target)return slot;return null;}
        public bool Preview(Ray ray,float maxDistance,out string type,out Vector3 point)
        {
            type=null;point=Vector3.zero;if(!Active||!stream.Pick(ray,maxDistance,out int index,out _))return false;
            type=ResearchType(stream.flow.species[index]);point=stream.flow.cells[index].position;return world.freeTools||world.mission.SceneNumber==3||type=="phagocyte";
        }
        public JourneyTarget Promote(Ray ray,float maxDistance)
        {
            if(!Active||!world.mission.Ready||!stream.Pick(ray,maxDistance,out int index,out float distance))return null;
            string type=ResearchType(stream.flow.species[index]);if(!world.freeTools&&world.mission.SceneNumber!=3&&type!="phagocyte")return null;
            foreach(var existing in slots)if(existing.index>=0&&existing.contact.enabled&&existing.contact.Raycast(ray,out var already,distance)){existing.touched=Time.time;return existing.target;}
            int obstaclesCount=Physics.RaycastNonAlloc(ray,obstacles,distance,~0,QueryTriggerInteraction.Ignore);if(obstaclesCount==obstacles.Length)return null;for(int i=0;i<obstaclesCount;i++){var hit=obstacles[i];if(!hit.collider.transform.IsChildOf(world.mover.origin.transform)&&!hit.collider.GetComponentInParent<JourneyTarget>())return null;}
            JourneyBloodInteractiveSlot preferred=null;
            foreach(var candidate in slots)if(candidate.type==type){if(candidate.index==index){candidate.touched=Time.time;return candidate.target;}var cell=candidate.target.GetComponent<JourneyPhagocyteEntity>();if(candidate.index<0){preferred=candidate;break;}if(!candidate.target.grabbed&&!candidate.target.DesktopHeld&&!(cell&&cell.Busy)&&!(world.analysis&&world.analysis.Subject==candidate.target))preferred=candidate;}
            foreach(var slot in slots)if(slot.type==type)
            {
                if(slot!=preferred)continue;
                var phag=slot.target.GetComponent<JourneyPhagocyteEntity>();
                if(slot.index>=0&&(slot.target.grabbed||slot.target.DesktopHeld||phag&&phag.Busy||world.analysis&&world.analysis.Subject==slot.target))return slot.target;
                if(slot.index==index){slot.touched=Time.time;return slot.target;}
                Release(slot.target);slot.index=index;slot.touched=Time.time;Acquisitions++;stream.SetLease(index,true);
                var source=stream.flow.cells[index];slot.target.transform.SetParent(world.transform,true);slot.target.transform.SetPositionAndRotation(source.position,source.rotation);slot.target.transform.localScale=Vector3.one;
                slot.body.sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;slot.body.GetComponent<Renderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;slot.body.transform.localScale=source.localScale;
                foreach(var surface in slot.legacyRenderers)if(surface)surface.enabled=false;slot.body.gameObject.SetActive(true);slot.body.GetComponent<Renderer>().enabled=true;slot.target.gameObject.SetActive(true);slot.contact.enabled=true;slot.grab.enabled=true;
                slot.contact.center=slot.body.sharedMesh.bounds.center*source.localScale.x;slot.contact.radius=Mathf.Max(.07f,slot.body.sharedMesh.bounds.extents.magnitude*source.localScale.x*.8f);
                 slot.target.SetHomePose();if(phag){phag.ResetForCell(index,stream.Generation(index));if(capacityGeneration[index]==phag.Generation)phag.RestoreCapacity(capacityRemaining[index]);}
                var platelet=slot.target.GetComponent<JourneyPlateletEntity>();if(platelet)platelet.SetState(JourneyPlateletState.Flowing);
                return slot.target;
            }
            return null;
        }
        public JourneyTarget PromoteNear(Vector3 hand)
        {
            if(!Active||!world.mission.FreeBloodResearch)return null;
            foreach(var slot in slots)if(slot.index>=0&&slot.contact.enabled&&(slot.contact.ClosestPoint(hand)-hand).sqrMagnitude<.04f)return slot.target;
            if(!stream.PickNear(hand,.12f,out int index))return null;var nearest=stream.flow.cells[index];var direction=(nearest.position-hand).normalized;if(direction.sqrMagnitude<.01f)direction=world.mover.viewCamera.transform.forward;
            return Promote(new Ray(hand-direction*.5f,direction),.9f);
        }
        public bool Release(JourneyTarget target,bool force=false)
        {
            var slot=Slot(target);if(slot==null||slot.index<0)return false;
            if(slot.returning)return true;
            if(!force&&world.mission.SceneNumber==3&&world.analysis&&world.analysis.Opened&&world.analysis.Subject==target){stream.ResumeLease(slot.index,target.transform.position);slot.touched=Time.time;return true;}
            slot.returning=true;if(slot.grab.isSelected&&slot.grab.interactionManager)slot.grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)slot.grab);
            var phag=target.GetComponent<JourneyPhagocyteEntity>();if(phag){capacityGeneration[slot.index]=phag.Generation;capacityRemaining[slot.index]=phag.RemainingCapacity;phag.SetState(JourneyPhagocyteState.Flowing);phag.launchVelocity=Vector3.zero;phag.Animate(0,Vector3.zero);}
            stream.ResumeLease(slot.index,target.transform.position);stream.SetLease(slot.index,false);slot.index=-1;Returns++;
            slot.contact.enabled=false;slot.grab.enabled=false;if(Active||slot.legacyRenderers.Length==0)target.gameObject.SetActive(false);slot.body.gameObject.SetActive(false);slot.returning=false;return true;
        }
        public void Touch(JourneyTarget target){var slot=Slot(target);if(slot!=null)slot.touched=Time.time;}
        void LateUpdate()
        {
            if(lastScene!=world.mission.SceneNumber||wasActive!=Active)
            {
                foreach(var slot in slots)
                {
                    if(slot.index>=0)Release(slot.target,true);slot.target.transform.localScale=slot.originalScale;
                    slot.contact.center=slot.originalContactCenter;slot.contact.radius=slot.originalContactRadius;slot.contact.enabled=slot.grab.enabled=!Active&&slot.legacyRenderers.Length>0;
                    for(int i=0;i<slot.legacyRenderers.Length;i++)if(slot.legacyRenderers[i])slot.legacyRenderers[i].enabled=!Active&&slot.legacyVisibility[i];
                }
                lastScene=world.mission.SceneNumber;wasActive=Active;
            }
            if(!Active)return;
            foreach(var slot in slots)
            {
                if(slot.index<0){slot.target.gameObject.SetActive(false);continue;}
                var phag=slot.target.GetComponent<JourneyPhagocyteEntity>();
                if(slot.target.grabbed||slot.target.DesktopHeld||phag&&phag.Busy||world.analysis&&world.analysis.Subject==slot.target)continue;
                slot.target.transform.SetPositionAndRotation(stream.flow.cells[slot.index].position,stream.flow.cells[slot.index].rotation);
                bool retain=phag&&world.immunity&&world.immunity.Active&&phag.State==JourneyPhagocyteState.Activated&&Vector3.Distance(slot.target.transform.position,world.mover.viewCamera.transform.position)<14||world.mission.FreeBloodResearch&&world.research.FocusedTarget==slot.target&&Vector3.Distance(slot.target.transform.position,world.mover.viewCamera.transform.position)<12;
                if(!retain&&Time.time-slot.touched>3)Release(slot.target);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    [DefaultExecutionOrder(700)]
    public sealed class JourneyErythrocyteAnalysis:MonoBehaviour
    {
        public JourneyWorld world;
        public JourneyTarget[] molecules;
        public GameObject diagram;
        public MeshRenderer[] hemoglobinCopies;
        public Text caption;
        public JourneyTarget Subject{get;private set;}
        public bool Opened{get;private set;}
        public int Opens{get;private set;}
        static readonly Vector3[] offsets={new Vector3(.38f,.22f,0),new Vector3(-.34f,.20f,0),new Vector3(-.31f,.33f,0),new Vector3(.28f,-.30f,0)};
        public bool RayThroughContents(Ray ray,out float distance)
        {
            distance=0;if(!Opened||!Subject)return false;var delta=Subject.transform.position-ray.origin;
            float along=Vector3.Dot(delta,ray.direction);if(along<0||delta.sqrMagnitude-along*along>1)return false;
            distance=along+1;return true;
        }
        public bool IsMolecular(string id)=>id=="hemoglobin"||id=="heme"||id=="oxygen"||id=="co2";
        public bool Available(string id)
        {if(!IsMolecular(id))return true;if(!Opened)return false;if(Subject&&Subject.targetId=="plasma")return id=="co2";return id=="hemoglobin"||world.research.Find("hemoglobin").State==JourneyResearchState.Investigated;}
        public bool Open(JourneyTarget subject)
        {
            if(!world.mission.FreeBloodResearch||!subject||(subject.targetId!="rbc"&&subject.targetId!="plasma")||world.research.Find(subject.targetId).State!=JourneyResearchState.Investigated)return false;
            Subject=subject;Opened=true;Opens++;diagram.SetActive(true);
            int count=Random.Range(2,5);for(int i=0;i<hemoglobinCopies.Length;i++)hemoglobinCopies[i].gameObject.SetActive(subject.targetId=="rbc"&&i<count);
            world.mission.FeedbackMessage("Режим анализа: учебная схема содержимого, а не разрушение клетки. Выбери гемоглобин или транспорт CO₂.");
            world.research.RefreshAvailability();return true;
        }
        public void Close()
        {
            Opened=false;Subject=null;diagram.SetActive(false);
            foreach(var item in molecules)
            {
                var grab=item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();if(grab&&grab.isSelected&&grab.interactionManager)grab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
                item.gameObject.SetActive(false);
            }
        }
        void LateUpdate()
        {
            if(!Opened){diagram.SetActive(false);return;}
            if(!world.mission.FreeBloodResearch||!Subject||!Subject.gameObject.activeInHierarchy||!Subject.grabbed&&!Subject.DesktopHeld&&Vector3.Distance(Subject.transform.position,world.mover.viewCamera.transform.position)>10){Close();return;}
            var camera=world.mover.viewCamera.transform;var centre=Subject.transform.position;
            diagram.transform.SetPositionAndRotation(centre,camera.rotation);
            caption.text=Subject.targetId=="plasma"?"АНАЛИЗ / CO₂ · HCO₃⁻ · ПЛАЗМА":"АНАЛИЗ ЭРИТРОЦИТА / Hb → ГЕМ Fe²⁺ → O₂";caption.transform.rotation=camera.rotation;
            for(int i=0;i<molecules.Length;i++)
            {
                var item=molecules[i];bool show=Available(item.targetId);if(item.gameObject.activeSelf!=show)item.gameObject.SetActive(show);
                if(!show||item.grabbed||item.DesktopHeld)continue;
                var offset=offsets[i];if(item.targetId=="oxygen"&&item.GetComponent<JourneyBloodCargo>()?.detached==true)offset+=new Vector3(-.14f,.22f,0);
                item.transform.SetPositionAndRotation(centre+camera.rotation*offset,camera.rotation);
                foreach(var collider in item.GetComponents<Collider>())collider.enabled=true;
            }
        }
    }
}

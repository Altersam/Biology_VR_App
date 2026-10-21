using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    [DefaultExecutionOrder(1200)]
    public sealed class JourneyBloodPayload:MonoBehaviour
    {
        public JourneyWorld world;
        public Transform[] backgroundCargo;
        public JourneyTarget oxygen,co2,heme;
        public Mesh gasMesh,ironMesh;
        public Mesh co2Mesh;
        public Material oxygenMaterial,co2Material,ironMaterial;
        Renderer[] sourceRenderers,cargoRenderers;
        void Awake(){sourceRenderers=new Renderer[backgroundCargo.Length];cargoRenderers=new Renderer[backgroundCargo.Length];for(int i=0;i<backgroundCargo.Length;i++){sourceRenderers[i]=backgroundCargo[i].parent.GetComponent<Renderer>();cargoRenderers[i]=backgroundCargo[i].GetComponent<Renderer>();}}
        public void UpdateSource(int index,string species)
        {
            if(backgroundCargo==null||index>=backgroundCargo.Length||!backgroundCargo[index])return;
            var cargo=backgroundCargo[index];bool o2=species=="erythrocyte-O2",carbon=species=="erythrocyte-CO2",iron=species=="hemoglobin";
            cargo.gameObject.SetActive(o2||carbon||iron);if(!cargo.gameObject.activeSelf)return;
            cargo.GetComponent<MeshFilter>().sharedMesh=iron?ironMesh:carbon&&co2Mesh?co2Mesh:gasMesh;
            cargo.GetComponent<Renderer>().sharedMaterial=iron?ironMaterial:o2?oxygenMaterial:co2Material;
            cargo.localPosition=iron?new Vector3(.12f,.20f,0):new Vector3(0,.24f,0);
            cargo.localScale=Vector3.one*(iron?.15f:.30f);
        }
        void LateUpdate()
        {
            for(int i=0;i<backgroundCargo.Length;i++)cargoRenderers[i].enabled=sourceRenderers[i].enabled;
            Attach(oxygen,"rbc","erythrocyte-O2",new Vector3(0,.14f,0));
            Attach(co2,"rbc","erythrocyte-CO2",new Vector3(0,.14f,0));
            Attach(heme,"hemoglobin","hemoglobin",new Vector3(.09f,.12f,0));
        }
        void Attach(JourneyTarget topic,string type,string species,Vector3 offset)
        {
            var slot=world.bloodEntities.slots;
            JourneyTarget carrier=null;
            foreach(var entry in slot)if(entry.index>=0&&entry.type==type&&world.bloodEntities.stream.flow.species[entry.index]==species){carrier=entry.target;break;}
            topic.gameObject.SetActive(carrier);if(!carrier)return;
            var bond=topic.GetComponent<JourneyFlowBond>();bond.carrier=carrier;
            // Cargo belongs to the sampled carrier, not to the player's changing gaze.
            var frame=carrier.transform.rotation;topic.transform.SetPositionAndRotation(carrier.transform.position+frame*offset,frame);
        }
        public bool Available(JourneyTarget target)
        {var bond=target?target.GetComponent<JourneyFlowBond>():null;return bond&&bond.carrier&&world.bloodEntities.Owns(bond.carrier)&&target.gameObject.activeInHierarchy;}
    }
}

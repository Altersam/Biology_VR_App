using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyPlaqueDetail : MonoBehaviour
    {
        [SerializeField] JourneyWorld world;
        [SerializeField] JourneyPlaqueState state;
        [SerializeField] GameObject detailRoot;
        [SerializeField] Renderer cap;
        [SerializeField] Transform[] lipids,foam;
        [SerializeField] int[] sectors;
        [SerializeField] Transform collagen;
        [SerializeField] Text[] labels;
        Vector3[] scales;Vector3[] lipidPositions;
        Vector3[] foamScales;
        readonly float[] reduction=new float[3];
        MaterialPropertyBlock block;
        int lastScene=-1,lastStep=-1;
        float elapsed;
        public float FormationProgress=>Mathf.Clamp01(elapsed/2.4f);
        public bool Ready=>FormationProgress>.999f;
        public int LipidLobules=>lipids==null?0:lipids.Length;
        public void Configure(JourneyWorld w,JourneyPlaqueState controller,GameObject root,Renderer capSurface,Transform[] droplets,int[] zoneIndices,Transform[] cells,Transform fibres,Text[] captions)
        {
            world=w;state=controller;detailRoot=root;cap=capSurface;lipids=droplets;sectors=zoneIndices;foam=cells;collagen=fibres;labels=captions;Cache();detailRoot.SetActive(false);
        }
        void OnEnable(){if(Application.isPlaying&&world)Cache();}
        void Cache()
        {
            block=new MaterialPropertyBlock();scales=new Vector3[lipids.Length];lipidPositions=new Vector3[lipids.Length];foamScales=new Vector3[foam.Length];
            for(int i=0;i<lipids.Length;i++){scales[i]=lipids[i].localScale;lipidPositions[i]=lipids[i].localPosition;}
            for(int i=0;i<foam.Length;i++)foamScales[i]=foam[i].localScale;
        }
        void LateUpdate()
        {
            if(!world||!state||!world.mission||lipids==null)return;
            if(scales==null)Cache();var mission=world.mission;bool active=mission.SceneNumber==6;
            if(active&&(lastScene!=6||(mission.StepIndex==0&&lastStep>0)))
            {elapsed=0;for(int i=0;i<3;i++)reduction[i]=0;}
            lastScene=mission.SceneNumber;lastStep=mission.StepIndex;
            bool reveal=(active&&mission.StepIndex>=2)||world.IsEpisodeCompleted(6);detailRoot.SetActive(reveal);
            if(reveal)elapsed=Mathf.Min(2.4f,elapsed+Time.deltaTime);
            float progress=FormationProgress;
            if(cap)
            {
                cap.GetPropertyBlock(block);block.SetFloat("_SectionReveal",reveal?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.1f,.55f,progress)):0);cap.SetPropertyBlock(block);
            }
            if(!reveal)return;
            for(int sector=0;sector<3;sector++)reduction[sector]=Mathf.MoveTowards(reduction[sector],state.IsZoneProcessed(sector)?1:0,Time.deltaTime/.7f);
            for(int i=0;i<lipids.Length;i++)
            {
                float amount=1-Mathf.SmoothStep(0,1,reduction[sectors[i]]);
                float formation=Mathf.SmoothStep(.03f,1,Mathf.InverseLerp(0,.48f,progress));
                lipids[i].localScale=scales[i]*Mathf.Max(.012f,amount)*formation;
                lipids[i].localPosition=lipidPositions[i];
            }
            for(int i=0;i<foam.Length;i++)foam[i].localScale=foamScales[i]*Mathf.SmoothStep(.015f,1,Mathf.InverseLerp(.22f,.68f,progress))*(.72f+.28f*state.PathologyFraction);
            if(collagen)collagen.gameObject.SetActive(progress>.60f);
            if(labels!=null)foreach(var label in labels)if(label){label.gameObject.SetActive(active&&progress>.82f);label.transform.rotation=world.mover.viewCamera.transform.rotation;}
        }
    }
}

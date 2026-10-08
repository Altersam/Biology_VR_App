using System;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Small reusable mesh pool: cells leave the lumen through the one WALL_A defect.</summary>
    [DisallowMultipleComponent]
    public sealed class JourneyWoundState : MonoBehaviour
    {
        [Serializable]
        sealed class Contact
        {
            public JourneyTarget target;
            public Collider[] colliders;
            public bool[] originallyEnabled;
        }

        [SerializeField] JourneyWorld world;
        [SerializeField] Transform[] cells;
        [SerializeField] Renderer[] cellRenderers;
        [SerializeField] Contact[] contacts;
        [SerializeField] Renderer backing;
        [SerializeField] GameObject collagen;
        readonly float[] ages=new float[9];
        readonly bool[] live=new bool[9];
        JourneyMission mission;
        Vector3 outward,up,forward;
        float clock;
        int previousScene=-1,previousStep=-1,cursor;
        bool subscribed;
        Vector3 clotScale,fibrinScale;
        Vector3[] plateletScales;
        bool healingPoseCaptured;
        const float Lifetime=1.65f;

        public Transform[] Cells=>cells;
        public float LeakStrength{get;private set;}
        public int ActiveCellCount{get;private set;}
        public Vector3 Outward=>outward;

        public void Configure(JourneyWorld configuredWorld,Transform[] pool,Renderer matrix,GameObject scaffold,JourneyTarget[] diagnosticTargets)
        {
            Unsubscribe();world=configuredWorld;mission=world.mission;cells=pool;backing=matrix;collagen=scaffold;
            cellRenderers=new Renderer[cells.Length];
            for(int i=0;i<cells.Length;i++)cellRenderers[i]=cells[i].GetComponent<Renderer>();
            contacts=new Contact[diagnosticTargets.Length];
            for(int i=0;i<contacts.Length;i++)
            {
                var colliders=diagnosticTargets[i].GetComponentsInChildren<Collider>(true);
                var enabled=new bool[colliders.Length];for(int j=0;j<enabled.Length;j++)enabled[j]=colliders[j].enabled;
                contacts[i]=new Contact{target=diagnosticTargets[i],colliders=colliders,originallyEnabled=enabled};
            }
            ConfigureFrame();NormalizeDiagnosticContacts();ResetPool();
            if(Application.isPlaying){Subscribe();ApplyState();}
        }

        void OnEnable()
        {
            if(!Application.isPlaying||!world)return;
            mission=world.mission;ConfigureFrame();NormalizeDiagnosticContacts();ResetPool();Subscribe();ApplyState();
        }
        void OnDisable(){Unsubscribe();ResetPool();}
        void OnDestroy(){Unsubscribe();}
        void Subscribe(){if(subscribed||!mission)return;mission.Changed+=ApplyState;subscribed=true;}
        void Unsubscribe(){if(!subscribed||!mission)return;mission.Changed-=ApplyState;subscribed=false;}

        void ConfigureFrame()
        {
            if(!world||!world.mover||!world.mover.path)return;
            var path=world.mover.path;float s=path.Anchor(11)+5;
            outward=path.Right(s);up=path.Up(s);forward=path.Forward(s);
        }
        void NormalizeDiagnosticContacts()
        {
            if(contacts==null)return;
            foreach(var entry in contacts)
            {
                if(!entry.target||(entry.target.targetId!="wound"&&entry.target.targetId!="leak"&&entry.target.targetId!="repair"&&entry.target.targetId!="healed-wall"))continue;
                var contact=entry.target.GetComponent<SphereCollider>();if(!contact)continue;
                // These semantic contacts describe the current wall defect. Bounds
                // from a hidden imported teaching assembly contain distant offsets.
                contact.center=Vector3.zero;
                var scale=entry.target.transform.lossyScale;
                float size=Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
                contact.radius=.32f/Mathf.Max(.0001f,size);
            }
        }

        void ApplyState()
        {
            if(!mission)return;
            int scene=mission.SceneNumber;
            if(scene!=previousScene||(mission.StepIndex==0&&previousStep>0))
            {
                if(scene==14)
                {
                    if(previousScene!=14||!healingPoseCaptured)CaptureHealingPose();
                    if(world.clot)world.clot.gameObject.SetActive(true);
                    if(world.fibrin)world.fibrin.SetActive(true);
                }
                bool withinSameWound=previousScene>=11&&previousScene<=14&&scene>=11&&scene<=14;
                if(!withinSameWound)ResetPool();
                LeakStrength=DesiredStrength();
                if(!withinSameWound&&(scene==11||scene==12))for(int i=0;i<cells.Length;i++)
                {live[i]=true;ages[i]=Lifetime*i/cells.Length;}
            }
            previousScene=scene;previousStep=mission.StepIndex;
            bool atWound=scene>=11&&scene<=14;
            bool visited=world.IsEpisodeCompleted(11)||world.IsEpisodeCompleted(12)||world.IsEpisodeCompleted(13)||world.IsEpisodeCompleted(14);
            if(backing)backing.enabled=(atWound||visited)&&world.RepairGrowth<.999f;
            if(collagen)collagen.SetActive((atWound||visited)&&world.RepairGrowth<.999f&&world.AttachedPlatelets<3);
            // The old diagnostic targets share one position. Only the current
            // diagnostic contact may intercept a ray; physical pool cells have none.
            if(contacts!=null)foreach(var contact in contacts)
            {
                if(!contact.target)continue;
                bool enabled=atWound&&mission.Goals!=null&&mission.Current!=null&&mission.Current.target==contact.target.targetId;
                for(int i=0;i<contact.colliders.Length;i++)if(contact.colliders[i])
                    contact.colliders[i].enabled=enabled&&contact.originallyEnabled[i];
            }
        }

        float DesiredStrength()
        {
            if(!mission)return 0f;
            switch(mission.SceneNumber)
            {
                case 11:return 1f;
                case 12:return (1f-.84f*Mathf.Clamp01(world.AttachedPlatelets/6f))*(1f-world.FibrinGrowth)*(world.hemostasisPuzzle?1f-.75f*world.hemostasisPuzzle.Connections/3f:1f);
                case 13:return mission.ClotBalance<.28f?.75f:mission.ClotBalance<.5f?.35f:0f;
                default:return 0f;
            }
        }

        void LateUpdate()
        {
            if(!world||!mission||cells==null||cells.Length==0)return;
            if(previousScene!=mission.SceneNumber||previousStep!=mission.StepIndex)ApplyState();
            float desired=DesiredStrength();
            LeakStrength=Mathf.MoveTowards(LeakStrength,desired,Time.deltaTime*1.4f);
            bool atWound=mission.SceneNumber>=11&&mission.SceneNumber<=14;
            if(!atWound){ResetPool();return;}
            if(backing)backing.enabled=world.RepairGrowth<.999f;
            if(collagen)collagen.SetActive(world.RepairGrowth<.999f&&!(mission.SceneNumber>=12&&world.AttachedPlatelets>=3));
            if(mission.SceneNumber==14)UpdateHealingPresentation();
            clock+=Time.deltaTime*LeakStrength;
            while(clock>=.18f)
            {
                clock-=.18f;
                for(int i=0;i<cells.Length;i++)
                {
                    int index=(cursor+i)%cells.Length;if(live[index])continue;
                    live[index]=true;ages[index]=0;cursor=(index+1)%cells.Length;break;
                }
            }
            ActiveCellCount=0;
            for(int i=0;i<cells.Length;i++)
            {
                if(live[i]){ages[i]+=Time.deltaTime;if(ages[i]>=Lifetime)live[i]=false;}
                if(cellRenderers[i])cellRenderers[i].enabled=live[i];
                if(!live[i]||!cells[i])continue;
                ActiveCellCount++;
                float t=Mathf.Clamp01(ages[i]/Lifetime);
                cells[i].position=SamplePath(i,t);
                cells[i].rotation=Quaternion.LookRotation(outward,up)*Quaternion.Euler(18f+i*19f,0,t*75f+i*31f);
            }
        }

        public Vector3 SamplePath(int index,float t)
        {
            float lane=(index%3-1)*.13f;
            var site=world.woundSite.position;
            var p0=site-outward*1.00f-forward*.70f+up*lane;
            var p1=site-outward*.38f-forward*.25f+up*lane*.55f;
            var p2=site+outward*.38f+forward*.08f+up*lane*.55f;
            var p3=site+outward*1.15f+forward*.22f+up*lane;
            float u=1f-t;
            return u*u*u*p0+3f*u*u*t*p1+3f*u*t*t*p2+t*t*t*p3;
        }

        void CaptureHealingPose()
        {
            clotScale=world.clot?world.clot.localScale:Vector3.one;
            fibrinScale=world.fibrin?world.fibrin.transform.localScale:Vector3.one;
            plateletScales=new Vector3[world.platelets.Length];
            for(int i=0;i<plateletScales.Length;i++)plateletScales[i]=world.platelets[i]?world.platelets[i].transform.localScale:Vector3.one;
            healingPoseCaptured=true;
        }

        void UpdateHealingPresentation()
        {
            if(!healingPoseCaptured)CaptureHealingPose();
            float growth=world.RepairGrowth;
            float remaining=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.18f,1f,growth));
            bool visible=growth<.999f;
            if(world.clot){world.clot.gameObject.SetActive(visible);world.clot.localScale=clotScale*Mathf.Max(.01f,remaining);}
            if(world.fibrin){world.fibrin.SetActive(visible&&world.FibrinRemoval<.999f);world.fibrin.transform.localScale=fibrinScale*Mathf.Max(.01f,remaining*(1-world.FibrinRemoval));}
            for(int i=0;i<world.platelets.Length;i++)if(world.platelets[i])
            {world.platelets[i].SetActive(visible&&i<world.AttachedPlatelets);world.platelets[i].transform.localScale=plateletScales[i]*Mathf.Max(.01f,remaining);}
        }

        void ResetPool()
        {
            clock=0;cursor=0;ActiveCellCount=0;
            for(int i=0;i<live.Length;i++){live[i]=false;ages[i]=0;}
            if(cellRenderers!=null)foreach(var r in cellRenderers)if(r)r.enabled=false;
        }
    }
}

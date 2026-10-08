using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyLayerInspection : MonoBehaviour
    {
        [SerializeField] JourneyWorld world;
        [SerializeField] Transform patch;
        [SerializeField] JourneyTarget[] bands;
        [SerializeField] JourneyTarget activation;
        [SerializeField] Renderer[] renderers;
        [SerializeField] Collider[] contacts;
        [SerializeField] Collider activationContact;
        [SerializeField] MeshFilter[] filters;
        [SerializeField] Mesh[] closed,opened;
        [SerializeField] MeshCollider vesselCollider;
        [SerializeField] Mesh vesselOriginal,vesselWindow;
        [SerializeField] Text[] labels;
        public void SetLabels(Text[] value){labels=value;}
        Mesh[] runtime;
        Vector3[][] fullPositions,openPositions,fullNormals,openNormals,positions,normals;
        readonly float[] fractions=new float[4];
        MaterialPropertyBlock block;
        JourneyMission mission;bool subscribed;
        int lastScene=-1,lastStep=-1;
        public JourneyTarget[] BandTargets=>bands;
        public int FocusedBand{get;private set;}=-1;
        public bool AnimationBusy{get;private set;}
        public float RevealProgress{get;private set;}
        public const float RevealDuration=.85f;
        public bool ReadyForScan(string id)
        {
            if(id=="endothelium")return true;
            int index=id=="intima"?0:id=="media"?1:id=="adventitia"?2:-1;
            return index<0||(RevealProgress>.999f&&fractions[index]>.999f);
        }

        public void Configure(JourneyWorld w,Transform root,JourneyTarget[] targets,JourneyTarget control,Renderer[] surfaces,Collider[] colliders,Collider controlContact,MeshFilter[] meshes,Mesh[] full,Mesh[] folded,MeshCollider vessel,Mesh window)
        {
            world=w;patch=root;bands=targets;activation=control;renderers=surfaces;contacts=colliders;activationContact=controlContact;filters=meshes;closed=full;opened=folded;vesselCollider=vessel;vesselOriginal=vessel.sharedMesh;vesselWindow=window;
            foreach(var r in renderers)r.gameObject.SetActive(false);patch.gameObject.SetActive(false);
        }
        void OnEnable()
        {
            if(!Application.isPlaying||!world)return;
            mission=world.mission;Initialize();Subscribe();ApplyState();
        }
        void Initialize()
        {
            if((runtime!=null&&fullPositions!=null)||filters==null)return;
            block=new MaterialPropertyBlock();if(runtime==null)runtime=new Mesh[4];fullPositions=new Vector3[4][];openPositions=new Vector3[4][];fullNormals=new Vector3[4][];openNormals=new Vector3[4][];positions=new Vector3[4][];normals=new Vector3[4][];
            for(int i=0;i<4;i++)
            {
                fullPositions[i]=closed[i].vertices;openPositions[i]=opened[i].vertices;fullNormals[i]=closed[i].normals;openNormals[i]=opened[i].normals;
                positions[i]=new Vector3[fullPositions[i].Length];normals[i]=new Vector3[positions[i].Length];
                if(!runtime[i])runtime[i]=Instantiate(closed[i]);runtime[i].MarkDynamic();runtime[i].name="Runtime unfolding "+bands[i].targetId;
                var bounds=closed[i].bounds;bounds.Encapsulate(opened[i].bounds);runtime[i].bounds=bounds;filters[i].sharedMesh=runtime[i];
            }
        }
        void Subscribe(){if(subscribed||!mission)return;mission.Changed+=ApplyState;subscribed=true;}
        void OnDisable(){if(subscribed&&mission)mission.Changed-=ApplyState;subscribed=false;Shader.SetGlobalFloat("_InspectionReveal",0);if(vesselCollider&&vesselOriginal)vesselCollider.sharedMesh=vesselOriginal;}
        void OnDestroy(){OnDisable();if(runtime!=null)foreach(var mesh in runtime)if(mesh)Destroy(mesh);}
        void ApplyState()
        {
            if(!mission||runtime==null)return;
            bool active=mission.SceneNumber==4;
            bool dormant=world.IsEpisodeCompleted(4)&&!active;
            if(active&&(lastScene!=4||(mission.StepIndex==0&&lastStep>0)))
            {RevealProgress=0;for(int i=0;i<4;i++){fractions[i]=0;Write(i,0);}}
            lastScene=mission.SceneNumber;lastStep=mission.StepIndex;
            patch.gameObject.SetActive(active||dormant);FocusedBand=active?(mission.StepIndex==0?0:mission.StepIndex>=2&&mission.StepIndex<=4?mission.StepIndex-1:-1):-1;
            for(int i=0;i<4;i++)
            {
                renderers[i].enabled=true;renderers[i].gameObject.SetActive(dormant||active&&(i==0||mission.StepIndex>=2));
                contacts[i].enabled=active&&i==FocusedBand;
            }
            if(activationContact)activationContact.enabled=active&&mission.StepIndex==1;
            if(!active&&!dormant){Shader.SetGlobalFloat("_InspectionReveal",0);if(vesselCollider.sharedMesh!=vesselOriginal)vesselCollider.sharedMesh=vesselOriginal;AnimationBusy=false;}
            if(labels!=null)foreach(var label in labels)if(label)label.gameObject.SetActive(active);
            RestorePose();
        }
        void LateUpdate()
        {
            if(!mission||runtime==null)return;
            if(lastScene!=mission.SceneNumber||lastStep!=mission.StepIndex)ApplyState();
            if(mission.SceneNumber!=4)return;
            RestorePose();AnimationBusy=false;
            float target=mission.StepIndex>=2?1:0;
            RevealProgress=Mathf.MoveTowards(RevealProgress,target,Time.deltaTime/RevealDuration);
            var path=world.mover.path;float s=path.Anchor(4)+3;
            Shader.SetGlobalVector("_InspectionOriginWS",path.Offset(s,path.Radius(s),0));Shader.SetGlobalVector("_InspectionUpWS",path.Up(s));Shader.SetGlobalVector("_InspectionForwardWS",path.Forward(s));Shader.SetGlobalVector("_InspectionRightWS",path.Right(s));
            Shader.SetGlobalVector("_InspectionHalfSize",new Vector4(path.Radius(s)*.40f,1.22f,0,0));Shader.SetGlobalFloat("_InspectionReveal",Mathf.SmoothStep(0,1,RevealProgress));
            var colliderMesh=RevealProgress>.02f?vesselWindow:vesselOriginal;if(vesselCollider.sharedMesh!=colliderMesh)vesselCollider.sharedMesh=colliderMesh;
            for(int i=0;i<3;i++)
            {
                float wanted=mission.StepIndex>=i+2?1:0;
                float value=Mathf.MoveTowards(fractions[i],wanted,Time.deltaTime/RevealDuration);
                if(!Mathf.Approximately(value,fractions[i])){fractions[i]=value;Write(i,Mathf.SmoothStep(0,1,value));}
                AnimationBusy|=Mathf.Abs(wanted-value)>.001f;
            }
            for(int i=0;i<4;i++)if(contacts[i] is MeshCollider contact)
            {
                // Switch immutable baked collision at the completed fold pose;
                // never recook the deforming runtime mesh each frame.
                var mesh=fractions[i]>.999f?opened[i]:closed[i];
                if(contact.sharedMesh!=mesh)contact.sharedMesh=mesh;
            }
            for(int i=0;i<4;i++)
            {
                renderers[i].GetPropertyBlock(block);block.SetColor("_EmissionColor",i==FocusedBand?new Color(.008f,.025f,.035f):Color.black);renderers[i].SetPropertyBlock(block);
                if(labels!=null&&i<labels.Length&&labels[i])
                {
                    labels[i].gameObject.SetActive(i==0||mission.StepIndex>=2);
                    Vector3 point=filters[i].transform.TransformPoint(Vector3.Lerp(fullPositions[i][262],openPositions[i][262],Mathf.SmoothStep(0,1,fractions[i])));var camera=world.mover.viewCamera.transform;
                    labels[i].transform.SetPositionAndRotation(point+(camera.position-point).normalized*.22f+camera.right*.58f,camera.rotation);
                    labels[i].color=i==FocusedBand?new Color(.55f,1,1):new Color(.88f,.95f,.98f);
                }
            }
        }
        void Write(int i,float t)
        {
            for(int j=0;j<positions[i].Length;j++){positions[i][j]=Vector3.Lerp(fullPositions[i][j],openPositions[i][j],t);normals[i][j]=Vector3.Lerp(fullNormals[i][j],openNormals[i][j],t).normalized;}
            runtime[i].vertices=positions[i];runtime[i].normals=normals[i];
        }
        void RestorePose()
        {
            if(!patch||!world)return;var path=world.mover.path;float s=path.Anchor(4)+3;
            patch.SetPositionAndRotation(path.Centre(s),path.Frame(s));
            foreach(var target in bands)if(target){target.transform.SetPositionAndRotation(patch.position,patch.rotation);target.transform.localScale=Vector3.one;}
            if(activation){activation.transform.SetPositionAndRotation(patch.position,patch.rotation);activation.transform.localScale=Vector3.one;}
        }
    }
}

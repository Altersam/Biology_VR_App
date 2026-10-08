// Recovery copy reconstructed from the patch applied in this session.
using UnityEngine;
namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyArtComposition : MonoBehaviour
    {
        public JourneyWorld world;
        public Renderer[] wallChunks;
        public Renderer[] branchRenderers;
        public float[] chunkDistances;
        public Transform[] wallAttachments;
        public string[] attachmentIds;
        public GameObject layerCutaway,plateletComparison;
        public Mesh restingPlateletMesh,activatedPlateletMesh;
        public Material[] restingPlateletMaterials,activatedPlateletMaterials;
        public UnityEngine.UI.Text[] bloodLabels;
        public int LastScene {get;private set;}
        bool bloodDocked;
        public void Stage(int scene)
        {
            LastScene=scene;bloodDocked=false;var path=world.mover.path;float s=path.Anchor(scene);
            if(layerCutaway)layerCutaway.SetActive(scene==4||world.IsEpisodeCompleted(4));
            if(plateletComparison)plateletComparison.SetActive(world.IsEpisodeCompleted(3));
            if(scene==11)foreach(var p in world.platelets)if(p)p.SetActive(false);
            if(scene==3)
            {
                Place("rbc",s+1.05f,.22f,-.12f,Quaternion.Euler(65,15,0));
                Place("hemoglobin",s+1.70f,.35f,-.20f,Quaternion.Euler(60,0,0));
                Place("heme",s+1.4f,.92f,.10f,Quaternion.Euler(65,0,0));
                Place("oxygen",s+1.3f,.95f,.36f,Quaternion.identity);
                Place("co2",s+1.5f,.5f,-.33f,Quaternion.identity);
                Place("leukocyte",s+1.60f,-.50f,-.45f,Quaternion.Euler(60,0,0));
                Place("platelet",s+1.45f,.65f,.20f,Quaternion.Euler(65,0,0));
                Place("plasma",s+1.85f,.70f,-.36f,Quaternion.identity);
            }
            else if(scene==8)foreach(var id in new[]{"virus-study","genome","capsid","epitope"})Place(id,s+1.5f,.25f,0,Quaternion.Euler(55,10,0));
            if(attachmentIds!=null)for(int i=0;i<attachmentIds.Length;i++)
            {
                var t=world.Find(attachmentIds[i]);var anchor=wallAttachments[i];
                if(!t||!anchor||!t.gameObject.activeInHierarchy||t.grabbed)continue;
                if(scene!=6&&(attachmentIds[i]=="plaque"||attachmentIds[i]=="cap"||attachmentIds[i]=="lipid"))continue;
                t.transform.SetPositionAndRotation(anchor.position,anchor.rotation);t.SetHomePose();
            }
            if(scene>=11&&scene<=14)
            {
                foreach(var id in new[]{"wound","healed-wall","repair","leak","clot","fibrin"})
                {var t=world.Find(id);if(t){t.transform.position=world.woundSite.position;t.SetHomePose();}}
                if(scene==12)Place("platelet",s+1,.50f,-.20f,Quaternion.Euler(65,0,0));
            }
        }
        void Place(string id,float s,float x,float y,Quaternion rot)
        {
            var t=world.Find(id);if(!t||t.grabbed)return;
            t.transform.SetPositionAndRotation(world.mover.path.Offset(s,x,y),world.mover.path.Frame(s)*rot);t.SetHomePose();
        }
        public void ShowPlateletComparison(){if(plateletComparison)plateletComparison.SetActive(true);}
        public void SetPlateletVisual(GameObject p,bool activated)
        {
            if(!p)return;var filter=p.GetComponent<MeshFilter>();var mesh=activated?activatedPlateletMesh:restingPlateletMesh;
            if(filter&&mesh)filter.sharedMesh=mesh;
            var renderer=p.GetComponent<Renderer>();var materials=activated?activatedPlateletMaterials:restingPlateletMaterials;
            if(renderer&&materials!=null&&materials.Length>0)renderer.sharedMaterials=materials;
        }
        public void PrepareGoal(StudyGoal goal)
        {
            if (!world || !world.mission) return;
            if(world.mission.SceneNumber==5&&world.flowModel)world.flowModel.gameObject.SetActive(world.mission.StepIndex>=1);
            if(world.mission.SceneNumber==8)
            {
                if(world.virusInspection)return;
                // These targets share the same complete cutaway. Only the studied instance is visible.
                foreach(var id in new[]{"virus-study","genome","capsid","epitope"})
                {var t=world.Find(id);if(t)t.gameObject.SetActive(goal!=null&&goal.target==id);}
                return;
            }
            if(world.mission.SceneNumber!=3)return;
            int step = world.mission.StepIndex;
            // Exact canonical scene03 sequence: Grab, Scan, Enlarge, Hb, Heme, O2 off/on,
            // CO2, WBC, resting platelet, compare activated platelet, plasma.
            foreach (var id in new[]{"rbc","hemoglobin","heme","oxygen","co2","leukocyte","platelet","plasma"})
            {
                bool visible = id == "rbc" || id=="leukocyte" || id=="platelet" || id=="plasma" || (step >= 3 && step <= 6 && (id == "hemoglobin" || id == "heme" || id == "oxygen"))
                    || (step == 7 && id == "co2") || (step == 8 && id == "leukocyte")
                    || ((step == 9 || step == 10) && id == "platelet") || (step >= 11 && id == "plasma");
                var t = world.Find(id);if(t && t.gameObject.activeSelf != visible)t.gameObject.SetActive(visible);
            }
            if(plateletComparison)plateletComparison.SetActive(step == 10);
        }
        public bool TryViewFocus(out Vector3 point)
        {
            point=Vector3.zero;if(!world||!world.mission)return false;
            int scene=world.mission.SceneNumber;var path=world.mover.path;
            if(scene==8&&world.virusInspection){point=world.virusInspection.FocusPoint;return true;}
            if(scene==9){point=path.Offset(path.Anchor(9)+7,0,0);return true;}
            if(scene==4||scene==6){float s=path.Anchor(scene)+(scene==4?3:4);point=path.Offset(s,path.Radius(s)-.5f,0);return true;}
            if(scene==7||scene==8||scene==9||scene==15)
            {
                string id=scene==7?"embolus":scene==8?"virus-study":scene==9?"phagocyte":"infected-cell";
                var target=world.Find(id);if(target&&target.gameObject.activeInHierarchy){point=target.transform.position;return true;}
            }
            if(scene==16){var target=world.Find("healed-wall");if(target&&target.gameObject.activeInHierarchy){point=target.transform.position;return true;}}
            if(scene>=11&&scene<=14&&world.woundSite){point=world.woundSite.position;return true;}
            return false;
        }
        void Update()
        {
            if(!world||!world.mover||wallChunks==null)return;
            if(world.mission.SceneNumber==3&&world.mission.StepIndex>=3&&!bloodDocked)
            {
                var rbc=world.Find("rbc");
                if(rbc&&rbc.DesktopHeld&&!world.mover.UsingTrackedInput)rbc.KeyboardGrab();
                if(rbc&&!rbc.grabbed)
                {
                    var flowing=rbc.GetComponent<JourneyFlowPickup>();
                    if(flowing&&flowing.InStudyFlow){bloodDocked=true;}
                    else
                    {
                    var path=world.mover.path;float s=path.Anchor(3);
                    rbc.SetPresentationPose(path.Offset(s+1.8f,1.0f,-.22f),path.Frame(s)*Quaternion.Euler(70,15,0));bloodDocked=true;
                    }
                }
            }
            bool inLab=world.mission.SceneNumber>=17&&world.labRoom&&world.labRoom.activeInHierarchy;
            float visibility=world.mover.viewCamera.farClipPlane+18f;
            for(int i=0;i<wallChunks.Length;i++)if(wallChunks[i])wallChunks[i].enabled=!inLab&&world.mission.SceneNumber<18&&Mathf.Abs(chunkDistances[i]-world.mover.Distance)<visibility;
            if(branchRenderers!=null)foreach(var r in branchRenderers)if(r)r.enabled=!inLab;
        }
        void LateUpdate()
        {
            if(bloodLabels==null||!world||!world.mover.viewCamera)return;
            var ids=new[]{"rbc","leukocyte","platelet","plasma"};var camera=world.mover.viewCamera.transform;
            for(int i=0;i<bloodLabels.Length&&i<ids.Length;i++)if(bloodLabels[i])
            {
                bool active=world.mission.SceneNumber==3;bloodLabels[i].gameObject.SetActive(active);if(!active)continue;
                var target=world.Find(ids[i]);if(!target)continue;
                float above=i==0?.20f:i==1?.23f:.15f;
                bloodLabels[i].transform.SetPositionAndRotation(target.transform.position+camera.up*above,camera.rotation);
            }
        }
    }
}

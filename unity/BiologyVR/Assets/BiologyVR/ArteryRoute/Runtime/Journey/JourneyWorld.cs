using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyWorld : MonoBehaviour
    {
        public JourneyMission mission;
        public JourneyMover mover;
        public GameObject[] locationRoots;
        public JourneyTarget[] targets;
        public Transform woundSite,clot,phagocyte,infected,tCell,flowModel,bubble,trap;
        public GameObject fibrin,repairPatch,occlusion,labRoom;
        public GameObject[] debris,platelets;
        public GameObject[] remainingComplexes;
        public GameObject virusPrefab;
        public Renderer[] toneWall;
        public JourneyArtComposition art;
        public JourneyPlaqueState plaqueState;
        public JourneyWoundState woundState;
        public JourneyLayerInspection layerInspection;
        public JourneyPlaqueDetail plaqueDetail;
        public JourneyVirusInspection virusInspection;
        public JourneyHemostasisPuzzle hemostasisPuzzle;
        public JourneyTarget[] recoveryDebris;
        public float FibrinRemoval{get;private set;}
        bool[] recoveryScanned;
        int selectedRecovery=-1;
        bool recoveryBusy,recoveryAcceptReady,fibrinolysis;
        public JourneyTarget[] residualTargets;
        public bool AntibodyBound{get;private set;}
        public int FinalPulseHits{get;private set;}
        bool antibodyPlacement,complexEngulfReady,complexEngulfBusy;
        int selectedResidual=-1,finalSpawnSerial;
        public int HomeostasisChecks{get;private set;}
        public bool EmbolusCaptured=>captured;
        public float VesselTone{get;private set;}=1;
        MaterialPropertyBlock toneBlock;
        public float EmbolusStability{get;private set;}
        public const float EmbolusHoldDistance=1.15f;
        const float EmbolusSettleDuration=.45f;
        Vector3 embolusVelocity;
        float embolusArc;
        bool embolusDrifting;
        public Vector3 LastInteractionPoint{get;private set;}
        readonly Dictionary<string,JourneyTarget> map=new Dictionary<string,JourneyTarget>();
        readonly List<GameObject> incoming=new List<GameObject>();
        GameObject selectedIncoming;
        public System.Collections.Generic.IReadOnlyList<GameObject> IncomingViruses=>incoming;
        bool captured,depositing,sourceRemoved;
        bool residualsPrepared;
        [Serializable] public sealed class EpisodeMemory
        {
            public int scene,step,progress;
            public bool visited,completed;
            public int treatedLipidZones,capDamage,attachedPlatelets,markedViruses,clearedDebris;
            public bool embolusCaptured,embolusStabilized,virusSourceRemoved;
            public float radius,pressure,clotBalance,fibrin,repair;
        }
        [SerializeField] EpisodeMemory[] episodeMemory=new EpisodeMemory[14];
        public bool IsEpisodeCompleted(int scene)=>scene>=3&&scene<=16&&episodeMemory!=null&&episodeMemory.Length>scene-3&&episodeMemory[scene-3]!=null&&episodeMemory[scene-3].completed;
        public void RememberEpisode()
        {
            if(!mission||mission.SceneNumber<3||mission.SceneNumber>16||mission.Goals==null)return;
            if(episodeMemory==null||episodeMemory.Length!=14)episodeMemory=new EpisodeMemory[14];
            int i=mission.SceneNumber-3;var state=episodeMemory[i]??(episodeMemory[i]=new EpisodeMemory{scene=mission.SceneNumber});
            state.visited=true;state.step=mission.StepIndex;state.progress=mission.Progress;state.completed=mission.Complete&&(mission.SceneNumber!=15||mission.Victory);
            state.treatedLipidZones=plaqueState?plaqueState.ProcessedCount:0;state.capDamage=plaqueState?plaqueState.CapHits:0;
            state.attachedPlatelets=AttachedPlatelets;state.markedViruses=MarkedViruses;state.clearedDebris=ClearedDebris;
            state.embolusCaptured=captured;state.embolusStabilized=mission.SceneNumber==7&&mission.Complete;state.virusSourceRemoved=sourceRemoved;
            state.radius=mission.ModelRadius;state.pressure=mission.PressureSystolic;state.clotBalance=mission.ClotBalance;state.fibrin=fibrinGrowth;state.repair=repairGrowth;
        }
        public void ForgetEpisode(int scene){if(scene>=3&&scene<=16&&episodeMemory!=null)episodeMemory[scene-3]=new EpisodeMemory{scene=scene,visited=true};}
        public void ResetJourneyMemory(){episodeMemory=new EpisodeMemory[14];}
        Vector3[] residualPositions;
        Vector3 repairStartScale=Vector3.one,clotBaseScale=Vector3.one,fibrinBaseScale=Vector3.one;
        public bool Returning{get;private set;}
        public bool Victory{get;private set;}
        public bool ResidualsCleared=>remainingComplexes!=null&&remainingComplexes.Length==3&&remainingComplexes.All(c=>!c||!c.activeSelf);
        float waveTimer,repairGrowth,fibrinGrowth,embolusTimer;
        public float RepairGrowth=>repairGrowth;
        public float FibrinGrowth=>fibrinGrowth;
        public int AttachedPlatelets{get;private set;}
        public int MarkedViruses{get;private set;}
        public int EngulfedViruses{get;private set;}
        bool waveHandoffRequested;
        int waveGeneration;
        public int ClearedDebris{get;private set;}
        public string FocusId{get;private set;}="rbc";
        const float DormantRootDistance=118f;
        readonly Dictionary<int,Dictionary<Renderer,bool>> dormantRenderers=new Dictionary<int,Dictionary<Renderer,bool>>();
        void Awake(){RebuildTargetMap();if(remainingComplexes!=null)residualPositions=remainingComplexes.Select(c=>c?c.transform.position:Vector3.zero).ToArray();if(clot)clotBaseScale=clot.localScale;if(fibrin)fibrinBaseScale=fibrin.transform.localScale;if(fibrin)fibrin.SetActive(false);if(repairPatch){repairStartScale=repairPatch.transform.localScale;repairPatch.SetActive(false);}if(occlusion)occlusion.SetActive(false);if(labRoom)labRoom.SetActive(false);}
        void OnEnable(){RebuildTargetMap();}
        void OnDisable(){RestoreDormantRenderers();}
        void RebuildTargetMap()
        {
            // Managed dictionaries do not survive an editor script reload. The
            // serialized registry remains authoritative and excludes transient proxies.
            map.Clear();if(targets==null)return;
            foreach(var target in targets)if(target&&!string.IsNullOrEmpty(target.targetId))
            {map[target.targetId]=target;target.mission=mission;}
        }
        public JourneyTarget Find(string id)
        {
            if(string.IsNullOrEmpty(id))return null;
            if(map.TryGetValue(id,out var cached)&&cached)return cached;
            if(targets!=null)foreach(var target in targets)if(target&&target.targetId==id)
            {target.mission=mission;map[id]=target;return target;}
            return null;
        }
        public void ShowScene(int scene)
        {
            RestoreDormantRenderers();
            Victory=false;
            if(scene<17&&labRoom)labRoom.SetActive(false);
            if(scene<17&&art)
            {RenderSettings.fog=true;RenderSettings.fogStartDistance=18;RenderSettings.fogEndDistance=54;RenderSettings.fogColor=new Color(.62f,.32f,.36f);mover.viewCamera.backgroundColor=RenderSettings.fogColor;}
            if(scene>=18)
            {
                foreach(var root in locationRoots)if(root)root.SetActive(false);
                foreach(var t in targets)if(t)t.gameObject.SetActive(false);
                if(labRoom)
                {
                    labRoom.SetActive(true);
                    var free=new[]{"rbc","leukocyte","platelet","plaque","embolus","virus-study","t-cell"};
                    for(int i=0;i<free.Length;i++)
                    {
                        var t=Find(free[i]);if(!t)continue;
                        if(free[i]=="plaque"){t.transform.localScale=Vector3.one*.25f;foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=true;}
                        t.transform.SetParent(labRoom.transform,true);
                        var center=labRoom.transform.TransformPoint(i<4?new Vector3(-1.45f+i*.97f,1.22f,.70f):new Vector3(-.95f+(i-4)*.95f,2.05f,.80f));
                        t.transform.SetPositionAndRotation(center,Quaternion.identity);t.gameObject.SetActive(true);
                        float size=free[i]=="rbc"?.30f:free[i]=="platelet"?.16f:free[i]=="embolus"?.33f:.40f;
                        FitLabModel(t,size,center);t.SetHomePose();
                    }
                    PlaceLabCamera();
                }
                PrepareGoal(mission.Current);
                return;
            }
             int location=mover.path.locationIds[scene-3];
             // Roots contain persistent tissue geometry, not gameplay lifetimes.
             // Rendering chunks are distance culled; progression never disables them.
             foreach(var root in locationRoots)if(root)root.SetActive(true);
             foreach(var t in targets)if(t)
             {
                 if(t.grabbed)continue;
                 foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;
             }
            string[] keys=scene switch
            {
                3=>new[]{"rbc","hemoglobin","heme","oxygen","co2","leukocyte","platelet","plasma"},
                4=>new[]{"endothelium","layers","intima","media","adventitia"},5=>new[]{"flow","flow-model","normal-flow","small-radius","large-radius","pressure"},
                6=>new[]{"plaque","plaque-flow","pulse-mode","lipid","cap"},7=>new[]{"embolus","attract-mode"},8=>new[]{"virus-study","genome","capsid","epitope"},
                9=>new[]{"immune-mode","phagocyte"},10=>new[]{"pressure-low","pressure-stable","pressure-return","tone-mode","tone"},
                11=>new[]{"wound","leak","pressure-low"},12=>new[]{"wound","platelet","thrombin","fibrin"},13=>new[]{"clot","insufficient","excessive","optimal","open-flow"},
                 14=>new[]{"debris","cleanup-mode","plasmin","repair","healed-wall"},15=>new[]{"epitope","antibody-A","antibody-B","antibody-C","neutralized","infected-cell","t-cell","remaining"},
                16=>new[]{"healed-wall","vitals","homeostasis"},_=>new[]{"return-field"}
            };
            for(int i=0;i<keys.Length;i++)
            {
                var t=Find(keys[i]);if(!t)continue;
                if(t.grabbed)continue;
                t.transform.SetParent(locationRoots[location].transform,true);
                t.transform.position=mover.path.Offset(mover.path.Anchor(scene)+1.5f+(i/3)*.38f,(i%3-1)*.40f,-.30f-(i/3)*.33f);
                t.transform.rotation=mover.path.Frame(mover.path.Anchor(scene))*Quaternion.Euler(45,0,0);
                t.gameObject.SetActive(true);
                // The layer inspector owns its real shell/control contacts. Re-enabling
                // legacy teaching spheres would let rays scan empty space in the lumen.
                if(!(scene==4&&layerInspection))foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=true;
            }
            if(scene==5)
            {
                // Experimental controls are an interaction surface, not a stack of
                // overlapping spheres. Keep every control on a separate ray path.
                PlaceExperimentTarget("flow",mover.path.Anchor(5)+1.8f,-.78f,.62f);
                PlaceExperimentTarget("flow-model",mover.path.Anchor(5)+1.8f,.0f,.62f);
                PlaceExperimentTarget("small-radius",mover.path.Anchor(5)+1.8f,.78f,.62f);
                PlaceExperimentTarget("large-radius",mover.path.Anchor(5)+1.8f,-.78f,-.25f);
                PlaceExperimentTarget("pressure",mover.path.Anchor(5)+1.8f,.0f,-.25f);
                PlaceExperimentTarget("normal-flow",mover.path.Anchor(5)+1.8f,.78f,-.25f);
            }
            if(scene==10)
            {
                float s=mover.path.Anchor(10)+1.8f;
                PlaceExperimentTarget("pressure-low",s,-.85f,.3f);PlaceExperimentTarget("tone-mode",s,0,.3f);PlaceExperimentTarget("tone",s,.85f,.3f);
                PlaceExperimentTarget("pressure-stable",s,-.45f,-.5f);PlaceExperimentTarget("pressure-return",s,.45f,-.5f);
            }
            if(scene>=11&&scene<=14)
            {
                foreach(var id in new[]{"wound","healed-wall","repair","leak","clot","fibrin"})
                {var t=Find(id);if(t&&woundSite)t.transform.position=woundSite.position-mover.path.Right(mover.path.Anchor(11)+5)*.10f;}
            }
            if(scene==14&&phagocyte){phagocyte.SetParent(locationRoots[location].transform,true);phagocyte.position=mover.path.Offset(mover.path.Anchor(scene)+2,1,-.3f);phagocyte.gameObject.SetActive(true);}
            if(scene==6&&Find("plaque")){var p=Find("plaque").transform;p.position=mover.path.Offset(mover.path.Anchor(6)+4,2.65f,-.2f);p.rotation=Quaternion.identity;}
            if(scene==7)ResetEmbolusCheckpoint();
            if(scene==9){sourceRemoved=false;MarkedViruses=EngulfedViruses=0;waveHandoffRequested=false;waveGeneration++;ClearWave();}
            if(scene==11){if(clot)clot.gameObject.SetActive(false);if(fibrin)fibrin.SetActive(false);if(repairPatch)repairPatch.SetActive(false);AttachedPlatelets=0;fibrinGrowth=0;repairGrowth=0;}
            if(scene==12)
            {
                AttachedPlatelets=0;fibrinGrowth=0;repairGrowth=0;if(repairPatch)repairPatch.SetActive(false);
                if(fibrin)fibrin.SetActive(false);
                 if(clot){clot.gameObject.SetActive(true);clot.localScale=clotBaseScale*.15f;}
                for(int i=0;i<platelets.Length;i++)if(platelets[i])
                 {var p=platelets[i];if(hemostasisPuzzle)continue;p.transform.SetParent(locationRoots[location].transform,true);p.transform.position=mover.path.Offset(mover.path.Anchor(12)+2,-1.2f+i*.35f,-.35f);p.transform.localScale=Vector3.one*.16f;p.SetActive(true);if(art)art.SetPlateletVisual(p,false);}
                 if(hemostasisPuzzle)hemostasisPuzzle.Begin();
            }
            if(scene==14)
            {
                ClearedDebris=0;repairGrowth=0;FibrinRemoval=0;fibrinolysis=false;recoveryBusy=false;selectedRecovery=-1;
                if(repairPatch)repairPatch.SetActive(false);foreach(var d in debris)if(d)d.SetActive(true);
                if(recoveryDebris!=null)
                {
                    recoveryScanned=new bool[recoveryDebris.Length];float s=mover.path.Anchor(11)+5;
                    for(int i=0;i<recoveryDebris.Length;i++){var t=recoveryDebris[i];t.transform.position=woundSite.position-mover.path.Right(s)*(.72f+i*.12f)+mover.path.Up(s)*((i-1)*.42f);t.transform.localScale=Vector3.one;t.gameObject.SetActive(true);foreach(var c in t.GetComponents<Collider>())c.enabled=true;}
                    var proxy=Find("debris");if(proxy){foreach(var c in proxy.GetComponents<Collider>())c.enabled=false;foreach(var r in proxy.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
                }
                foreach(var id in new[]{"cleanup-mode","plasmin"}){var t=Find(id);if(t)t.transform.position=woundSite.position-mover.path.Right(mover.path.Anchor(11)+5)*.85f+mover.path.Up(mover.path.Anchor(11)+5)*.8f+mover.path.Forward(mover.path.Anchor(11)+5)*(id=="plasmin"?.45f:-.45f);}
            }
            if(scene==15){AntibodyBound=false;FinalPulseHits=finalSpawnSerial=0;antibodyPlacement=complexEngulfReady=complexEngulfBusy=false;selectedResidual=-1;sourceRemoved=false;residualsPrepared=false;if(remainingComplexes!=null)for(int i=0;i<remainingComplexes.Length;i++)if(remainingComplexes[i]){remainingComplexes[i].SetActive(false);if(residualPositions!=null&&i<residualPositions.Length)remainingComplexes[i].transform.position=residualPositions[i];}if(infected){infected.localScale=Vector3.one*.70f;infected.position=mover.path.Offset(mover.path.Anchor(15)+3.8f,1.4f,-.1f);infected.gameObject.SetActive(true);}if(phagocyte){phagocyte.SetParent(locationRoots[location].transform,true);phagocyte.position=mover.path.Offset(mover.path.Anchor(15)+2,-1.25f,-.3f);phagocyte.gameObject.SetActive(true);}}
            if(scene==15&&infected)
            {var contact=infected.GetComponent<SphereCollider>();if(contact){contact.center=Vector3.zero;contact.radius=.42f/Mathf.Max(.001f,infected.lossyScale.x);}}
            if(scene==16)
            {
                HomeostasisChecks=0;if(infected)infected.gameObject.SetActive(false);ClearWave();
                var ids=new[]{"check-pressure","check-temperature","check-flow","check-wall","check-virus","check-hemostasis"};
                for(int i=0;i<ids.Length;i++){var t=Find(ids[i]);if(!t)continue;t.transform.SetParent(locationRoots[location].transform,true);t.transform.SetPositionAndRotation(mover.path.Offset(mover.path.Anchor(16)+1.8f,(i%3-1)*.65f,.45f-(i/3)*.70f),mover.path.Frame(mover.path.Anchor(16)));t.gameObject.SetActive(true);foreach(var c in t.GetComponents<Collider>())c.enabled=true;}
            }
            if(scene!=9&&scene!=15)ClearWave();
            if(scene==17)StartCoroutine(ReturnToLab());
            if(art)art.Stage(scene);
            PrepareGoal(mission.Current);
        }
        void PlaceExperimentTarget(string id,float s,float x,float y)
        {
            var target=Find(id);if(!target)return;target.transform.SetParent(locationRoots[mover.path.locationIds[mission.SceneNumber-3]].transform,true);target.transform.SetPositionAndRotation(mover.path.Offset(s,x,y),mover.path.Frame(s)*Quaternion.Euler(45,0,0));target.gameObject.SetActive(true);target.SetHomePose();
        }
        void StreamLocationRoots()
        {
            if(!mover||mover.path==null||locationRoots==null||locationRoots.Length==0||!mission||mission.SceneNumber>=17)return;
            int current=Mathf.Clamp(mission?mover.path.locationIds[Mathf.Clamp(mission.SceneNumber-3,0,mover.path.locationIds.Length-1)]:0,0,locationRoots.Length-1);
            for(int rootIndex=0;rootIndex<locationRoots.Length;rootIndex++)
            {
                var root=locationRoots[rootIndex];if(!root)continue;
                float closest=float.MaxValue;
                for(int scene=3;scene<=16;scene++)if(mover.path.locationIds[scene-3]==rootIndex)closest=Mathf.Min(closest,Vector3.Distance(mover.viewCamera.transform.position,mover.path.Centre(mover.path.Anchor(scene))));
                bool near=rootIndex==current||closest<DormantRootDistance;
                // Keep component/target state alive. Root deactivation invokes XRI
                // selection/home reset callbacks and is not an exact-state streaming path.
                // Only suspend distant rendering, retaining each renderer's authored state.
                if(near)
                {
                    if(dormantRenderers.TryGetValue(rootIndex,out var saved))
                    {foreach(var pair in saved)if(pair.Key)pair.Key.enabled=pair.Value;dormantRenderers.Remove(rootIndex);}
                }
                else if(!dormantRenderers.ContainsKey(rootIndex))
                {
                    var saved=new Dictionary<Renderer,bool>();
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){saved[renderer]=renderer.enabled;renderer.enabled=false;}
                    dormantRenderers[rootIndex]=saved;
                }
                if(!near&&dormantRenderers.TryGetValue(rootIndex,out var suspended))foreach(var pair in suspended)if(pair.Key)pair.Key.enabled=false;
            }
        }
        public void ApplyLateLocationCulling()=>StreamLocationRoots();
        public bool LocationRenderingCulled(int index)=>dormantRenderers.ContainsKey(index);
        void RestoreDormantRenderers()
        {
            foreach(var saved in dormantRenderers.Values)foreach(var pair in saved)if(pair.Key)pair.Key.enabled=pair.Value;
            dormantRenderers.Clear();
        }
        // There is deliberately no "disable previous root after arrival" path.
        public void ShowVictory()
        {
            Victory=true;sourceRemoved=true;ClearWave();
            if(infected)infected.gameObject.SetActive(false);
            if(virusPrefab)virusPrefab.SetActive(false);
            if(tCell)tCell.gameObject.SetActive(true);
            if(locationRoots!=null&&locationRoots.Length>7)
                foreach(var t in locationRoots[7].GetComponentsInChildren<Transform>(true))
                    if(t.name.IndexOf("Virion",System.StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("Virus",System.StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("Infected",System.StringComparison.OrdinalIgnoreCase)>=0)t.gameObject.SetActive(false);
            mission?.FeedbackMessage("ПОБЕДА: источник инфекции устранён, вирусная нагрузка 0 %, поток стабилен.");
        }
        void PositionSharedTarget(string id,float distance)
        {
            var target=Find(id);if(!target||mover==null||mover.path==null)return;
            target.transform.position=mover.path.Offset(distance,0,-mover.path.Radius(distance)+.35f);
            target.transform.rotation=mover.path.Frame(distance)*Quaternion.Euler(70,0,0);
        }
        public void PrepareGoal(StudyGoal goal)
        {
            if(goal!=null)FocusId=goal.target;
            if(art)art.PrepareGoal(goal);
            else if(mission.SceneNumber==3)PrepareBloodLesson();
            if(mission.SceneNumber==15&&!Victory)
            {
                bool binding=goal?.action==StudyAction.Bind;
                foreach(var id in new[]{"antibody-A","antibody-B","antibody-C"}){var t=Find(id);if(t)t.gameObject.SetActive(binding||id=="antibody-A"&&AntibodyBound);}
                var epitope=Find("epitope");if(epitope&&infected){epitope.transform.position=infected.position+(mover.viewCamera.transform.position-infected.position).normalized*.40f;epitope.gameObject.SetActive(goal?.target=="epitope"||binding||AntibodyBound);}
                var neutralized=Find("neutralized");if(neutralized){neutralized.gameObject.SetActive(goal?.target=="neutralized");if(epitope)neutralized.transform.position=epitope.transform.position;}
                if(remainingComplexes!=null&&mission.StepIndex==6&&!residualsPrepared){residualsPrepared=true;foreach(var complex in remainingComplexes)if(complex)complex.SetActive(true);}
            }
            foreach(var t in targets)if(t)foreach(var r in t.GetComponentsInChildren<Renderer>(true)){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetColor("_EmissionColor",t.targetId==FocusId?new Color(.006f,.018f,.03f):Color.black);r.SetPropertyBlock(block);}
        }
        void PrepareBloodLesson()
        {
            var visible=new HashSet<string>();
            switch(mission.StepIndex)
            {
                case 0:case 1:case 2:visible.UnionWith(new[]{"rbc"});break;
                case 3:case 4:case 5:case 6:case 7:visible.UnionWith(new[]{"rbc","hemoglobin","heme","oxygen"});break;
                case 8:visible.UnionWith(new[]{"rbc","co2"});break;
                case 9:visible.UnionWith(new[]{"rbc","leukocyte"});break;
                case 10:case 11:visible.UnionWith(new[]{"rbc","platelet"});break;
                default:visible.UnionWith(new[]{"rbc","plasma"});break;
            }
            foreach(var id in new[]{"rbc","hemoglobin","heme","oxygen","co2","leukocyte","platelet","plasma"})
            {
                var target=Find(id);if(target)target.gameObject.SetActive(visible.Contains(id));
            }
        }
        public bool CanApply(StudyAction action,string target)
        {
            if(mission.SceneNumber==16&&target.StartsWith("check-"))
            {
                switch(target){case "check-pressure":return mission.PressureSystolic>=110&&mission.PressureSystolic<=130;case "check-temperature":return mission.Temperature>=36.3f&&mission.Temperature<=37.2f;case "check-flow":return mission.FlowMultiplier>.95f&&(!occlusion||!occlusion.activeSelf);case "check-wall":return repairGrowth>.999f;case "check-virus":return mission.ViralLoad==0&&sourceRemoved&&ResidualsCleared&&incoming.Count==0;case "check-hemostasis":return mission.ClotBalance>.5f&&mission.ClotBalance<.72f&&repairGrowth>.999f;}
            }
            if(mission.SceneNumber==14&&recoveryDebris!=null)
            {
                if(target=="debris")return action==StudyAction.Scan?selectedRecovery>=0&&!recoveryScanned[selectedRecovery]:recoveryAcceptReady;
                if(target=="plasmin")return ClearedDebris==recoveryDebris.Length;
                if(target=="repair")return FibrinRemoval>=.99f;
            }
            if(mission.SceneNumber==12&&hemostasisPuzzle&&!hemostasisPuzzle.CanApply(action,target))return false;
            if(target=="pulse-mode"&&mission.SceneNumber==6&&plaqueDetail)return plaqueDetail.Ready;
            if(action==StudyAction.Scan&&mission.SceneNumber==4&&layerInspection)return layerInspection.ReadyForScan(target);
            if(action==StudyAction.Pulse&&target=="lipid"&&plaqueState)return plaqueState.CanPulse;
            if(action==StudyAction.Capture)return bubble!=null&&bubble.gameObject.activeInHierarchy&&!captured&&!depositing;
            if(action==StudyAction.Deposit)return captured&&EmbolusStability>=1&&EmbolusInsideTrap();
            if(action==StudyAction.Mark)return incoming.Count>0;
            if(mission.SceneNumber==9&&action==StudyAction.Direct&&target=="phagocyte")return MarkedViruses>=8&&EngulfedViruses>=MarkedViruses;
            if(mission.SceneNumber==10&&action==StudyAction.Measure&&target=="pressure-return")return mission.PressureSystolic<=92.1f;
            if(target=="remaining")return sourceRemoved;
            if(mission.SceneNumber==15&&action==StudyAction.Bind&&target.StartsWith("antibody"))return antibodyPlacement;
            if(mission.SceneNumber==15&&target=="neutralized")return complexEngulfReady;
            if(target=="healed-wall"&&mission.SceneNumber==14)return repairGrowth>=.999f;
            if(target=="open-flow")return mission.ClotBalance>.5f&&mission.ClotBalance<.72f;
            return true;
        }
        public void Primary(JourneyTarget target=null)
        {
            var goal=mission.Current;if(goal==null)return;
            string id=target?target.targetId:goal.target;
            var interactionTarget=target?target:Find(id);LastInteractionPoint=interactionTarget?interactionTarget.transform.position:Vector3.zero;
            if(goal.action==StudyAction.Mark)
            {
                if(target&&!incoming.Contains(target.gameObject)){mission.FeedbackMessage("Помечай входящую вирусную частицу, на которую наведен инструмент.");return;}
                selectedIncoming=target?target.gameObject:incoming.Count>0?incoming[0]:null;
                if(mission.SceneNumber==15&&selectedIncoming)
                {
                    var timing=selectedIncoming.GetComponent<JourneyViralTiming>();
                    if(timing&&!timing.WindowOpen){mission.RecordPhysicalError("Окно закрыто: дождись мягкой подсветки цели.");return;}
                    if(timing&&timing.Shielded){timing.Pulse();FinalPulseHits++;mission.RecordManualPulse();var feedback=mission.GetComponent<JourneyToolFeedback>();if(feedback)feedback.Confirm(StudyAction.Pulse,"incoming-virus");return;}
                }
            }
            if(goal.action==StudyAction.Pulse&&id=="lipid"&&plaqueState&&!plaqueState.SelectZone(target))return;
            if(goal.action==StudyAction.Grab){Find(id)?.KeyboardGrab();return;}
            if(goal.action==StudyAction.Enlarge){Find(id)?.Enlarge();return;}
            if(goal.action==StudyAction.Radius)
            {
                if(mission.SceneNumber==10){mission.FeedbackMessage("Наведи BioTool на регулятор тонуса, удерживай Trigger и настрой радиус движением руки.");return;}
                mission.SetRadius(id=="small-radius"?.65f:id=="large-radius"?1.35f:.82f);return;
            }
            if(goal.action==StudyAction.Pressure){mission.SetPressure(155);return;}
            if(goal.action==StudyAction.Balance){if(hemostasisPuzzle){mission.FeedbackMessage("Изменяй пробку руками: убирай/добавляй тромбоциты и соединения фибрина.");return;}mission.SetBalance(id=="insufficient"?.18f:id=="excessive"?.95f:.62f);return;}
            if(mission.SceneNumber==14&&goal.action==StudyAction.Direct&&id=="debris"&&recoveryDebris!=null)
            {
                int index=Array.IndexOf(recoveryDebris,target);
                if(index<0||!recoveryScanned[index]){mission.RecordPhysicalError("Сначала распознай выбранный debris сканером.");return;}
                if(!recoveryBusy){recoveryBusy=true;selectedRecovery=index;StartCoroutine(CleanupRecovery(index));}return;
            }
            if(mission.SceneNumber==15&&goal.action==StudyAction.Bind){mission.FeedbackMessage("Возьми комплементарное антитело и отпусти у найденного эпитопа.");return;}
            if(mission.SceneNumber==15&&goal.action==StudyAction.Direct&&id=="neutralized")
            {if(!complexEngulfBusy){complexEngulfBusy=true;StartCoroutine(EngulfAntibodyComplex());}return;}
            if(mission.SceneNumber==15&&goal.target=="remaining"&&residualTargets!=null)
            {selectedResidual=Array.IndexOf(residualTargets,target);if(selectedResidual<0){mission.RecordPhysicalError("Наведи BioTool на конкретный остаточный комплекс.");return;}}
            if(mission.SceneNumber==9&&goal.action==StudyAction.Direct&&id=="phagocyte")
            {
                if(!waveHandoffRequested){waveHandoffRequested=true;StartCoroutine(ConfirmWaveHandoff());}
                return;
            }
            if(goal.action==StudyAction.Deposit){TryStabilizeEmbolus();return;}
            if(goal.action==StudyAction.Scan){mission.Accept(StudyAction.Scan,id);return;}
            mission.Accept(goal.action,id);
        }
        public void Apply(StudyAction action,string target,int progress)
        {
            if(mission.SceneNumber==16&&target.StartsWith("check-"))HomeostasisChecks++;
            switch(target)
            {
                case "oxygen":if(Find("oxygen")){var o=Find("oxygen").transform;var heme=Find("heme").transform;o.position=heme.position+(action==StudyAction.Detach?mover.viewCamera.transform.right*.35f:Vector3.zero);}break;
                case "platelet":
                    if(mission.SceneNumber==12&&hemostasisPuzzle)
                    {if(action==StudyAction.Direct)AttachedPlatelets++;else if(action==StudyAction.Activate)hemostasisPuzzle.ActivatePlatelets();break;}
                    if(action==StudyAction.Compare&&art)art.ShowPlateletComparison();
                    else if(action==StudyAction.Compare&&platelets.Length>0){var comparison=platelets[0];comparison.transform.SetParent(locationRoots[mover.path.locationIds[0]].transform,true);comparison.transform.position=mover.path.Offset(mover.path.Anchor(3)+3,1.15f,-.25f);comparison.transform.localScale=Vector3.one*.21f;comparison.SetActive(true);}
                    if(action==StudyAction.Activate){for(int n=0;n<platelets.Length&&n<AttachedPlatelets;n++)if(platelets[n]){platelets[n].transform.localScale=Vector3.one*.21f;platelets[n].transform.Rotate(Vector3.forward,35f);if(art)art.SetPlateletVisual(platelets[n],true);}}
                     if(action==StudyAction.Direct){AttachedPlatelets++;if(platelets.Length>0){var p=platelets[(AttachedPlatelets-1)%platelets.Length];p.SetActive(true);p.transform.localScale=Vector3.one*(AttachedPlatelets<=3?.15f:.21f);if(art)art.SetPlateletVisual(p,AttachedPlatelets>3);StartCoroutine(MoveAgent(p.transform,woundSite.position+mover.path.Up(mover.Distance)*((AttachedPlatelets%3-1)*.18f),.8f));}if(clot)clot.localScale=clotBaseScale*Mathf.Lerp(.15f,1f,Mathf.Clamp01(AttachedPlatelets/6f));}
                    break;
                case "layers":foreach(var key in new[]{"intima","media","adventitia"})if(Find(key))Find(key).gameObject.SetActive(true);break;
                case "lipid":if(action==StudyAction.Pulse&&plaqueState)plaqueState.ApplySelectedPulse();else if(Find("lipid"))Find("lipid").transform.localScale*=.72f;break;
                case "embolus":
                    if(action==StudyAction.Capture){captured=true;EmbolusStability=0;embolusVelocity=Vector3.zero;}
                    else if(action==StudyAction.Deposit){captured=false;EmbolusStability=1;embolusVelocity=Vector3.zero;bubble.gameObject.SetActive(false);}
                    break;
                case "thrombin":if(action==StudyAction.Activate&&hemostasisPuzzle&&mission.SceneNumber==12)hemostasisPuzzle.StartReaction();break;
                case "fibrin":if(hemostasisPuzzle&&mission.SceneNumber==12)break;if(fibrin){fibrin.SetActive(true);fibrinGrowth=.01f;}break;
                case "normal-flow":mission.SetRadius(1);mission.SetPressure(120);break;
                case "vitals":mission.RecordHomeostasisMeasurements();mission.FeedbackMessage("Контрольные показатели сняты: сравни их с исходными.");break;
                case "homeostasis":mission.FeedbackMessage("Гомеостаз подтверждён: поток, давление и температура вернулись к рабочему диапазону.");break;
                case "debris":
                    if(mission.SceneNumber==14&&recoveryDebris!=null)
                    {if(action==StudyAction.Scan)recoveryScanned[selectedRecovery]=true;else if(action==StudyAction.Direct)ClearedDebris++;break;}
                    int i=ClearedDebris++;if(i<debris.Length&&debris[i]){if(phagocyte)StartCoroutine(Phagocytose(debris[i]));else debris[i].SetActive(false);}break;
                case "plasmin":if(action==StudyAction.Activate&&mission.SceneNumber==14)fibrinolysis=true;break;
                case "antibody-A":AntibodyBound=true;break;
                case "phagocyte":
                    if(mission.SceneNumber!=9&&phagocyte)StartCoroutine(MoveAgent(phagocyte,woundSite.position,1.1f));break;
                case "repair":if(repairPatch){repairPatch.SetActive(true);repairGrowth=.01f;}break;
                case "incoming-virus":if(incoming.Count>0){var virus=selectedIncoming&&incoming.Contains(selectedIncoming)?selectedIncoming:incoming[0];incoming.Remove(virus);selectedIncoming=null;MarkedViruses++;StartCoroutine(Neutralize(virus,mission.SceneNumber==9));}break;
                case "neutralized":if(mission.SceneNumber!=15&&phagocyte&&Find("neutralized"))StartCoroutine(MoveAgent(phagocyte,Find("neutralized").transform.position,1.0f));break;
                case "t-cell":if(tCell&&infected)StartCoroutine(RemoveSource());break;
                case "remaining":int residual=selectedResidual>=0?selectedResidual:progress;if(remainingComplexes!=null&&residual<remainingComplexes.Length&&remainingComplexes[residual])StartCoroutine(Neutralize(remainingComplexes[residual]));if(progress>=2){foreach(var t in targets)if(t&&t.targetId.StartsWith("antibody"))t.gameObject.SetActive(false);ClearWave();}break;
                case "return-field":StartCoroutine(ReturnToLab());break;
            }
        }
        public void UpdateExperiment(float radius,float pressure)
        {
            if(mission.SceneNumber==5&&flowModel)flowModel.localScale=new Vector3(radius,radius,1);
            if(mission.SceneNumber==10)VesselTone=radius;
            if(toneBlock==null)toneBlock=new MaterialPropertyBlock();
            foreach(var r in toneWall)if(r){r.GetPropertyBlock(toneBlock);toneBlock.SetFloat("_Tone",VesselTone);toneBlock.SetVector("_ToneCentre",mover.path.Centre(mover.path.Anchor(10)+4));r.SetPropertyBlock(toneBlock);}
        }
        public void UpdateClot(float balance)
        {
              if(clot){clot.gameObject.SetActive(true);clot.localScale=clotBaseScale*Mathf.Lerp(.20f,1.45f,balance);}
             if(occlusion)occlusion.SetActive(balance>.86f);
             if(occlusion&&mission.SceneNumber==13&&balance>.86f)
             {occlusion.transform.position=woundSite.position-mover.path.Right(mover.path.Anchor(11)+5)*.55f;occlusion.transform.localScale=Vector3.one*1.05f;}
        }
        public void StartPhysicalFibrinGrowth(){if(fibrin){fibrin.SetActive(true);fibrinGrowth=.01f;}}
        public void SetPhysicalPlateletCount(int count){AttachedPlatelets=count;}
        public void ShowCapDamage(int hits){if(plaqueState)plaqueState.ShowCapDamage(hits);if(occlusion){occlusion.SetActive(true);occlusion.transform.localScale=Vector3.one*Mathf.Lerp(.25f,1.4f,hits/3f);}}
        public void ResetCheckpoint(int scene){captured=false;depositing=false;ClearWave();if(occlusion)occlusion.SetActive(false);if(scene==7)ResetEmbolusCheckpoint();if(scene==6&&plaqueState)plaqueState.CheckPoint();else if(scene==6&&Find("lipid"))Find("lipid").transform.localScale=Vector3.one*.25f;}
        void ClearWave(){foreach(var v in incoming)if(v)Destroy(v);incoming.Clear();waveTimer=0;}
        void Update()
        {
            StreamLocationRoots();
            if(mover.Moving)return;
            if(mission.SceneNumber==7&&!captured&&!depositing&&bubble&&bubble.gameObject.activeInHierarchy)
            {
                float dt=Time.deltaTime;embolusTimer+=dt;embolusArc+=dt*.55f;
                if(embolusDrifting)
                {
                    embolusVelocity=Vector3.Lerp(embolusVelocity,mover.path.Forward(embolusArc)*.55f,1-Mathf.Exp(-3*dt));
                    bubble.position+=embolusVelocity*dt;
                }
                else bubble.position=mover.path.Offset(embolusArc,Mathf.Sin(embolusTimer*1.7f)*.42f,Mathf.Cos(embolusTimer*1.35f)*.35f);
                bubble.rotation=mover.path.Frame(embolusArc)*Quaternion.Euler(40,embolusTimer*28,0);
                if(embolusArc>mover.path.Anchor(7)+13||Vector3.Distance(bubble.position,mover.viewCamera.transform.position)>12)
                {ResetEmbolusCheckpoint();mission.RetryEmbolus(true);}
            }
             if(fibrinGrowth>0&&fibrin){fibrinGrowth=Mathf.Min(1,fibrinGrowth+Time.deltaTime*.45f);fibrin.transform.localScale=fibrinBaseScale*fibrinGrowth;}
            if(mission.SceneNumber==14&&fibrinolysis){FibrinRemoval=Mathf.Min(1,FibrinRemoval+Time.deltaTime*.32f);fibrinGrowth=1-FibrinRemoval;if(FibrinRemoval>=1&&fibrin)fibrin.SetActive(false);}
            if(repairGrowth>0&&repairPatch){repairGrowth=Mathf.Min(1,repairGrowth+Time.deltaTime*.30f);var r=repairPatch.GetComponent<Renderer>();if(r){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetFloat("_Reveal",repairGrowth);r.SetPropertyBlock(block);}}
            bool wave=(mission.SceneNumber==9||mission.SceneNumber==15)&&mission.Current?.action==StudyAction.Mark&&!sourceRemoved;
            if(wave&&virusPrefab)
            {
                waveTimer+=Time.deltaTime;
                if(waveTimer>1.25f&&incoming.Count<6){waveTimer=0;var v=Instantiate(virusPrefab);v.name="Incoming virus from artery bend";v.SetActive(true);v.transform.localScale=Vector3.one*.28f;var target=v.AddComponent<JourneyTarget>();target.targetId="incoming-virus";target.label="Входящий вирион";target.mission=mission;int layer=LayerMask.NameToLayer("JourneyTarget");if(layer>=0)v.layer=layer;var sphere=v.AddComponent<SphereCollider>();sphere.radius=.78f;v.transform.position=mover.path.Offset(mover.Distance+12,Mathf.Sin(Time.time)*.8f,Mathf.Cos(Time.time)*.6f);if(mission.SceneNumber==15){var timing=v.AddComponent<JourneyViralTiming>();timing.Shielded=(finalSpawnSerial++%2)==0;}incoming.Add(v);}
            }
            foreach(var virus in incoming.ToArray())if(virus)
            {
                var toward=(mover.viewCamera.transform.position-virus.transform.position).normalized;virus.transform.position+=toward*Time.deltaTime*.95f;virus.transform.Rotate(0,Time.deltaTime*13,0);
                if(Vector3.Distance(virus.transform.position,mover.viewCamera.transform.position)<.9f){incoming.Remove(virus);Destroy(virus);mission.MissVirus();}
            }
        }
        public void UpdateEmbolusField(float deltaTime,Transform aim,bool held,float holdDistance=EmbolusHoldDistance)
        {
            if(!mission||mission.SceneNumber!=7||!captured||!bubble)return;
            if(!held||!aim){ReleaseEmbolus();return;}
            float dt=Mathf.Clamp(deltaTime,0,.05f);
            // Finite-speed damped attraction opposes the flow. The hand transports
            // the field; there is no automatic path-to-trap tween.
            var point=aim.position+aim.forward*Mathf.Clamp(holdDistance,.45f,6.5f);
            if(Vector3.Distance(bubble.position,aim.position)>7){ReleaseEmbolus();return;}
            bubble.position=Vector3.SmoothDamp(bubble.position,point,ref embolusVelocity,.22f,2.8f,dt)+mover.path.Forward(embolusArc)*(.08f*dt);
            EmbolusStability=EmbolusInsideTrap()?Mathf.Clamp01(EmbolusStability+dt/EmbolusSettleDuration):0;
            if(EmbolusStability>=1)TryStabilizeEmbolus();
        }
        public void ReleaseEmbolus()
        {
            if(!captured)return;captured=false;EmbolusStability=0;embolusDrifting=true;
            // Preserve the release pose and velocity; only the local capture goal
            // retries, so earlier scanning/mode selection and the world stay intact.
            mission.RetryEmbolus(false);
        }
        bool EmbolusInsideTrap()
        {
            if(!bubble||!trap||!trap.gameObject.activeInHierarchy)return false;
            var contact=trap.GetComponent<SphereCollider>();if(!contact||!contact.enabled)return false;
            var centre=trap.TransformPoint(contact.center);var scale=trap.lossyScale;
            float radius=contact.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            return Vector3.Distance(bubble.position,centre)<radius*.82f;
        }
        bool TryStabilizeEmbolus()
        {
            return captured&&mission.Current!=null&&mission.Current.action==StudyAction.Deposit&&EmbolusStability>=1&&EmbolusInsideTrap()&&mission.Accept(StudyAction.Deposit,"embolus");
        }
        public void ResetEmbolusCheckpoint()
        {
            captured=depositing=embolusDrifting=false;embolusTimer=0;embolusArc=mover.path.Anchor(7)+3;embolusVelocity=Vector3.zero;EmbolusStability=0;
            if(bubble){bubble.position=mover.path.Offset(embolusArc,0,.35f);bubble.gameObject.SetActive(true);}
        }
        IEnumerator MoveAgent(Transform t,Vector3 target,float duration)
        {
            if(!t)yield break;var start=t.position;
            for(float time=0;time<duration;time+=Time.deltaTime){if(!t)yield break;t.position=Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,time/duration));yield return null;}if(t)t.position=target;
        }
        IEnumerator Phagocytose(GameObject item){yield return MoveAgent(phagocyte,item.transform.position,.8f);item.SetActive(false);}
        public bool ScanRecoveryDebris(JourneyTarget target)
        {
            int index=recoveryDebris==null?-1:Array.IndexOf(recoveryDebris,target);
            if(index<0||recoveryScanned[index]){mission.RecordPhysicalError("Этот debris уже распознан. Найди другой фрагмент.");return false;}
            selectedRecovery=index;return mission.Accept(StudyAction.Scan,"debris");
        }
        IEnumerator CleanupRecovery(int index)
        {
            var item=recoveryDebris[index];if(phagocyte)yield return MoveAgent(phagocyte,item.transform.position,.65f);
            var scale=item.transform.localScale;
            for(float t=0;t<1;t+=Time.deltaTime/.3f){if(!item)yield break;item.transform.localScale=scale*(1-t*.95f);yield return null;}
            item.gameObject.SetActive(false);recoveryAcceptReady=true;
            if(mission.SceneNumber==14&&mission.Current?.action==StudyAction.Direct)mission.Accept(StudyAction.Direct,"debris");
            recoveryAcceptReady=recoveryBusy=false;
        }
        public bool PlaceAntibody(JourneyTarget target)
        {
            if(mission.SceneNumber!=15||mission.Current?.action!=StudyAction.Bind)return false;
            var epitope=Find("epitope");if(!epitope||Vector3.Distance(target.transform.position,epitope.transform.position)>.28f){mission.RecordPhysicalError("Антитело отпущено вне эпитопа. Повтори размещение.");return false;}
            antibodyPlacement=true;bool accepted=mission.Accept(StudyAction.Bind,target.targetId);antibodyPlacement=false;
            if(accepted){foreach(var c in target.GetComponents<Collider>())c.enabled=false;StartCoroutine(MoveAgent(target.transform,epitope.transform.position,.25f));}
            return accepted;
        }
        IEnumerator EngulfAntibodyComplex()
        {
            var target=Find("neutralized");if(phagocyte&&target)yield return MoveAgent(phagocyte,target.transform.position,.75f);
            var antibody=Find("antibody-A");if(antibody)antibody.gameObject.SetActive(false);AntibodyBound=false;
            complexEngulfReady=true;if(mission.SceneNumber==15&&mission.Current?.target=="neutralized")mission.Accept(StudyAction.Direct,"neutralized");complexEngulfBusy=false;
        }
        IEnumerator Neutralize(GameObject virus,bool wave=false)
        {
            int generation=waveGeneration;
            if(phagocyte)yield return MoveAgent(virus.transform,phagocyte.position,1);else yield return new WaitForSeconds(.5f);
            if(virus)
            {
                virus.SetActive(false);if(wave&&mission.SceneNumber==9&&generation==waveGeneration)EngulfedViruses++;
                if(remainingComplexes==null||!remainingComplexes.Contains(virus))Destroy(virus);
            }
            mission.ConfirmVictory();
        }
        IEnumerator ConfirmWaveHandoff()
        {
            int generation=waveGeneration;
            mission.FeedbackMessage("Фагоцит поглощает помеченные частицы — дождись завершения.");
            while(mission&&generation==waveGeneration&&mission.SceneNumber==9&&mission.Current?.action==StudyAction.Direct&&EngulfedViruses<MarkedViruses)yield return null;
            if(generation!=waveGeneration)yield break;
            waveHandoffRequested=false;
            if(mission&&mission.SceneNumber==9&&mission.Current?.action==StudyAction.Direct&&MarkedViruses>=8&&EngulfedViruses>=MarkedViruses)
                mission.Accept(StudyAction.Direct,"phagocyte");
        }
        IEnumerator RemoveSource()
        {
            yield return MoveAgent(tCell,infected.position,.8f);
            var original=infected.localScale;for(float t=0;t<1;t+=Time.deltaTime*.55f){infected.localScale=original*(1-t*.95f);yield return null;}infected.gameObject.SetActive(false);
            sourceRemoved=true;
        }
        IEnumerator ReturnToLab()
        {
            Returning=true;
            mission.FeedbackMessage("Возврат масштаба: микромир → 1:1");
            for(float t=0;t<1;t+=Time.deltaTime*.20f){RenderSettings.fogColor=Color.Lerp(new Color(.50f,.22f,.20f),new Color(.13f,.55f,.85f),t);mover.viewCamera.backgroundColor=RenderSettings.fogColor;RenderSettings.fogStartDistance=Mathf.Lerp(15,0,t);RenderSettings.fogEndDistance=Mathf.Lerp(70,.5f,t);yield return null;}
            if(labRoom){labRoom.SetActive(true);PlaceLabCamera();}
            foreach(var root in locationRoots)if(root)root.SetActive(false);RenderSettings.fog=false;
            Returning=false;
            mover.viewCamera.backgroundColor=new Color(.76f,.84f,.86f);
            mission.FeedbackMessage("Масштаб восстановлен. Можно открыть итоговый отчёт.");
        }
        void PlaceLabCamera()
        {
            if(!labRoom||mover==null||mover.origin==null||mover.origin.Camera==null)return;
            mover.RecenterInLaboratory();
        }
        static void FitLabModel(JourneyTarget target,float diameter,Vector3 center)
        {
            var lod=target.GetComponent<LODGroup>();
            var renderers=lod&&lod.GetLODs().Length>0?lod.GetLODs()[0].renderers:target.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
            if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var r in renderers)if(r)bounds.Encapsulate(r.bounds);
            float factor=diameter/Mathf.Max(.001f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
            target.transform.localScale*=factor;
            bounds=renderers[0].bounds;foreach(var r in renderers)if(r)bounds.Encapsulate(r.bounds);
            target.transform.position+=center-bounds.center;
        }
    }
}

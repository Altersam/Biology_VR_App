using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class PlayerFeedbackXRPlaytest
    {
        const string Folder=JourneyGeometry.Root+"/Reports/Polish/PlayerFeedbackRework";
        static JourneyMission mission;static JourneyInput input;static Transform leftPose,rightPose;
        static XRSimulatedController left,right;static XRSimulatedHMD head;
        static XRSimulatedControllerState ls,rs;static XRSimulatedHMDState hs;
        static bool running,arcade,patrol,integrated,focused,prelude;static readonly List<string> checks=new List<string>();
        [MenuItem("Biology VR/Test Player Feedback Rework Through XR Triggers")]
        public static void Run()=>Start(false);
        [MenuItem("Biology VR/Test Arcade HUD Opaque Cells And Evasive Virus Through XR")]
        public static void Arcade()=>Start(true);
        [MenuItem("Biology VR/Test Immune Patrol And Platelet Reverse Through XR")]
        public static void Patrol()=>Start(false,true);
        [MenuItem("Biology VR/Test Integrated Patrol Wound Route With XR")]
        public static void Integrated()=>Start(false,true,true);
        [MenuItem("Biology VR/Test Focused Patrol Wound XR")]
        public static void Focused()=>Start(false,true,true,true);
        [MenuItem("Biology VR/Test Human Controls Prelude XR")]
        public static void Prelude()=>Start(false,true,true,false,true);
        [MenuItem("Biology VR/Inspect Final Scanner Targets With XR")]
        public static void SensorAudit(){Start(false,true,true,false,false);sensorAudit=true;}
        static bool sensorAudit;
        static string ReportFolder=>integrated?PatrolWoundIntegration.Reports:patrol?JourneyGeometry.Root+"/Reports/Polish/ImmunePatrol":arcade?JourneyGeometry.Root+"/Reports/Polish/ArcadeVisualPass":Folder;
        static void Start(bool refinement,bool immunePatrol=false,bool route=false,bool focus=false,bool intro=false){if(!EditorApplication.isPlaying||running)throw new InvalidOperationException("Fresh Play Mode required");arcade=refinement;patrol=immunePatrol;integrated=route;focused=focus;prelude=intro;mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();input=mission.GetComponent<JourneyInput>();running=true;checks.Clear();Directory.CreateDirectory(ReportFolder);if(patrol)Application.logMessageReceived+=RuntimeLog;EditorCoroutineUtility.StartCoroutineOwnerless(Guard());}
        static void Report()=>File.WriteAllText(ReportFolder+(prelude?"/XR_Prelude.json":focused?"/XR_Focused.json":"/XR_Input.json"),JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,running,passed=!running&&checks.Contains(prelude?"PASS: Prelude XR route complete":integrated?"PASS: Integration XR route complete":patrol?"PASS: Immune patrol and platelet XR regression complete":"PASS: Feedback XR trigger sequence complete")&&!checks.Any(c=>c.StartsWith("BLOCKED")),method=prelude?"Actual simulated XR controls; normal route 03-08, 180-cell budget; no success APIs.":integrated?(focused?"Isolated Scene10 entry; ":"One Scene09 entry fixture; ")+"ordinary left Y transitions through patrol/wound/balance/recovery. Actual controller/HMD states, 180-cell mobile budget, Scanner, Reverse aimed at the visible tear (not hidden slots), deliberate misses, and remote platelet balance. No mission success APIs. Simulated XR; headset verification separate.":patrol?"Actual XRSimulatedController/HMD states, original action bindings, XRI capture, left Scanner, right Reverse and left Y Continue. Isolated Scene08/10 entry fixtures; no success APIs; not physical-headset testing.":"Actual XRSimulatedController/HMD states, original action bindings, XRI field selection and tracked hand motion. Isolated Scene05/07/08 fixtures; not physical-headset testing.",checks},Formatting.Indented));
        static void Check(bool value,string text){checks.Add((value?"PASS: ":"BLOCKED: ")+text);Report();if(!value)throw new InvalidOperationException(text);}
        static void RuntimeLog(string message,string stackTrace,LogType type){if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;string entry="BLOCKED: Runtime "+type+": "+message+"\n"+stackTrace;if(!checks.Contains(entry)){checks.Add(entry);Report();}}
        static IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Test());try{while(stack.Count>0){var current=stack.Peek();bool moved=false;Exception error=null;try{moved=current.MoveNext();}catch(Exception e){error=e;}if(error!=null){checks.Add("BLOCKED: "+error);Debug.LogException(error);yield break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current.Current is IEnumerator nested)stack.Push(nested);else yield return current.Current;}}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Application.logMessageReceived-=RuntimeLog;running=false;Report();}}
        static void Queue(){if(InputState.currentUpdateType!=InputUpdateType.Dynamic&&InputState.currentUpdateType!=InputUpdateType.Fixed)return;InputSystem.QueueStateEvent(left,ls);InputSystem.QueueStateEvent(right,rs);InputSystem.QueueStateEvent(head,hs);}
        static IEnumerator Test()
        {
            bool background=Application.runInBackground;Application.runInBackground=true;
            var simulators=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b is XRDeviceSimulator||b is XRInteractionSimulator).Cast<Behaviour>().ToArray();var enabled=simulators.Select(s=>s.enabled).ToArray();var settings=InputSystem.settings;var behavior=settings.backgroundBehavior;var editor=settings.editorInputBehaviorInPlayMode;bool explicitAim=input.useExplicitAimSources;
            try
            {
                foreach(var simulator in simulators)simulator.enabled=false;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                SimulatedInputLayoutLoader.Initialize();left=InputSystem.AddDevice<XRSimulatedController>();right=InputSystem.AddDevice<XRSimulatedController>();head=InputSystem.AddDevice<XRSimulatedHMD>();InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
                leftPose=mission.mover.origin.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Left Controller");rightPose=mission.mover.origin.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Right Controller");
                ls=new XRSimulatedControllerState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity};rs=ls;hs=new XRSimulatedHMDState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity,centerEyeRotation=Quaternion.identity,leftEyeRotation=Quaternion.identity,rightEyeRotation=Quaternion.identity};input.useExplicitAimSources=true;InputSystem.onBeforeUpdate+=Queue;yield return Wait(.4f);
                var world=mission.world;Check(world.freeTools&&input.scanAction.action.enabled&&input.toolAction.action.enabled,"Original XR Scanner/BioTool actions are live with the new field tool");
                if(sensorAudit){yield return FinalSensorAudit();yield break;}
                if(prelude){yield return PreludeTest();yield break;}
                if(integrated){yield return IntegratedTest();yield break;}
                if(patrol){yield return PatrolTest();yield break;}
                var source=Find("erythrocyte-O2");if(!source){world.bloodEntities.stream.RequestInflux("erythrocyte-O2");yield return Wait(1);source=Find("erythrocyte-O2");}Check(source,"Oxygen-loaded carrier is in ordinary flow");
                Pose(false,source.position,Quaternion.identity);rs.grip=1;rs=rs.WithButton(ControllerButton.GripButton,true);yield return Wait(.4f);Check(!world.freeTools.Held&&world.research.RequiredInvestigated==0,"Middle-finger Grip cannot capture a flowing sample or grant research credit");rs.grip=0;rs=rs.WithButton(ControllerButton.GripButton,false);yield return Wait(.15f);
                yield return Capture("erythrocyte-O2");var rbc=world.freeTools.Held;yield return Scan(rbc);yield return Scan(world.freeTools.payload.oxygen);Check(world.freeTools.Held==rbc&&!world.analysis.Opened,"Independent left Scanner studies bound oxygen while right Trigger holds the same RBC");CaptureView("07_XR_Bound_Cargo_And_Panels.png");Trigger(false,false);yield return Wait(.3f);Check(!world.freeTools.Held&&!world.bloodEntities.Owns(rbc),"Actual right Trigger release returns the sampled RBC to flow");
                if(!arcade)
                {
                mission.Enter(5,false);yield return Wait(.3f);Look(world.Find("hdx-diameter").transform.position);yield return Wait(.15f);yield return Scan(world.Find("hdx-diameter"));yield return Mode(BioToolMode.Tonus);Aim(false,world.hemodynamics.toneContact.transform.position);yield return Wait(.2f);Trigger(false,true);yield return Wait(.2f);
                var fieldStart=rightPose.position;var fieldRotation=rightPose.rotation;var up=mission.mover.path.Up(world.hemodynamics.SectionArc);
                Pose(false,fieldStart+up*.25f,fieldRotation);yield return Wait(.35f);Check(mission.StepIndex==2&&world.hemodynamics.RadiusRatio>1.2f,"Actual tracked hand expansion operates vascular field without dragging points");Pose(false,fieldStart+up*.47f,fieldRotation);yield return Wait(.35f);Check(mission.StepIndex==3&&world.GetComponent<JourneyLiveTelemetry>().Critical,"Further XR hand expansion triggers critical dynamic monitor state");CaptureView("11_XR_Vascular_Field_Critical.png");Pose(false,fieldStart,fieldRotation);yield return Wait(.9f);Trigger(false,false);yield return Wait(.15f);yield return Scan(world.Find("hdx-final"));Check(mission.Complete,"XR hand correction plus real Scanner finishes the vessel lesson");
                mission.Enter(7,false);yield return Wait(.3f);yield return Mode(BioToolMode.Capture);
                for(int n=0;n<6;n++)
                {
                    var bubble=world.Find("embolus");Look((bubble.transform.position+world.trap.position)*.5f);yield return Wait(.15f);Aim(false,bubble.GetComponent<Collider>().bounds.center);yield return Wait(.15f);Trigger(false,true);float began=Time.time;
                    while(!world.EmbolusCaptured&&Time.time-began<5){Aim(false,bubble.GetComponent<Collider>().bounds.center);yield return null;}Check(world.EmbolusCaptured,"Actual XR Trigger captures evasive bubble "+(n+1));bubble=world.Find("embolus");
                    var origin=input.bioTool.position;var targetOrigin=world.trap.position-(world.trap.position-mission.mover.viewCamera.transform.position).normalized*JourneyWorld.EmbolusHoldDistance;began=Time.time;
                    while(world.embolusWave.StabilizedCount<n+1&&Time.time-began<20){var handOrigin=Vector3.Lerp(origin,targetOrigin,Mathf.Clamp01((Time.time-began)/1.5f));var target=Vector3.MoveTowards(bubble.GetComponent<Collider>().bounds.center,world.trap.position,.05f);AimFrom(handOrigin,target);yield return null;}
                    if(world.embolusWave.StabilizedCount<n+1)checks.Add("XR embolus transport: bubble="+bubble.transform.position+" trap="+world.trap.position+" hand="+input.bioTool.position+" blocked="+input.ToolBlockedByUi+" lock="+input.LockedTarget?.targetId+" stability="+world.EmbolusStability);
                    Check(world.embolusWave.StabilizedCount==n+1,"Actual XR aimed field carries bubble into trap "+(n+1)+"/6");Trigger(false,false);yield return Wait(.15f);
                }
                Check(mission.Complete,"All six evasive emboli pass through XR attraction controls");
                }
                mission.Enter(8,false);yield return Wait(.3f);
                int attempts=0;
                while(!mission.Complete&&attempts++<(arcade?14:3))
                {
                    int n=world.immunity.DamageContacts;
                    yield return Capture("phagocyte");var cell=world.freeTools.Held.GetComponent<JourneyPhagocyteEntity>();yield return Scan(cell.target);
                    yield return Mode(BioToolMode.Bio);Check(world.freeTools.Held==cell.target,"A/B mode transition retains the captured phagocyte as the user changes tools");Trigger(false,true);yield return Wait(.85f);Check(cell.State==JourneyPhagocyteState.Activated,"Right activation Trigger activates the actually scanned cell");yield return Mode(BioToolMode.Reverse);
                    var virus=world.immunity.TutorialVirus;Aim(false,virus.transform.position);yield return Wait(.8f);var begin=input.bioTool.position;var rotation=rightPose.rotation;var back=-input.bioTool.forward;var offset=rightPose.position-begin;float age=Time.time;
                    while(Time.time-age<.4f){Pose(false,begin+offset+back*.20f*Mathf.Clamp01((Time.time-age)/.4f),rotation);yield return null;}yield return Wait(.25f);
                    if(arcade){age=Time.time;var origin=input.bioTool.position;while(Time.time-age<.65f){float lead=Mathf.Clamp((Vector3.Distance(origin,virus.transform.position)-.45f)/4,.15f,.9f);AimFrom(origin,virus.transform.position+virus.MotionVelocity*lead-world.immunity.BloodVelocity(virus.transform.position)*lead);yield return null;}}
                    Check(world.freeTools.ReverseCharge>.8f&&world.immunity.trajectory.enabled,"Tracked reverse hand pull charges the held cell and previews its trajectory");CaptureView("08_XR_Reverse_Hand_Pull.png");Trigger(false,false);float launched=Time.time;
                    while(world.immunity.DamageContacts<=n&&Time.time-launched<5)yield return Wait(.1f);if(!arcade||world.immunity.DamageContacts>n)Check(world.immunity.DamageContacts==n+1,"Actual XR release contacts the moving virus "+(n+1)+"/3");yield return Wait(1.3f);
                }
                Check(mission.Complete&&world.immunity.Neutralized==1,"Three real XR field cycles complete the virus encounter");if(arcade)Check(world.immunity.shatter.FinalBreaks==1&&world.immunity.shatter.Bursts>=3,"XR contacts produce visible shell fragments and a final viral break");Check(true,"Feedback XR trigger sequence complete");
            }
            finally{InputSystem.onBeforeUpdate-=Queue;if(left!=null)InputSystem.RemoveDevice(left);if(right!=null)InputSystem.RemoveDevice(right);if(head!=null)InputSystem.RemoveDevice(head);input.useExplicitAimSources=explicitAim;for(int i=0;i<simulators.Length;i++)if(simulators[i])simulators[i].enabled=enabled[i];settings.backgroundBehavior=behavior;settings.editorInputBehaviorInPlayMode=editor;Application.runInBackground=background;}
        }
        static Transform Find(string species)
        {
            var stream=mission.world.bloodEntities.stream;Transform result=null;float best=8;
            if(species=="phagocyte")foreach(var slot in mission.world.bloodEntities.slots)if(slot.type==species&&slot.index>=0&&slot.contact.enabled&&slot.target.GetComponent<JourneyPhagocyteEntity>().RemainingCapacity>0&&!slot.target.GetComponent<JourneyPhagocyteEntity>().Busy&&ClearSight(slot.target.transform)){float d=Vector3.Distance(slot.target.transform.position,mission.mover.viewCamera.transform.position);if(d<best){best=d;result=slot.target.transform;}}
            for(int i=0;i<stream.flow.VisibleCellLimit;i++){var cell=stream.flow.cells[i];if(stream.Leased(i)||stream.flow.species[i]!=species||!cell.GetComponent<Renderer>().enabled||species=="phagocyte"&&!mission.world.bloodEntities.HasPhagocyteCapacity(i)||!ClearSight(cell))continue;float d=Vector3.Distance(cell.position,mission.mover.viewCamera.transform.position);if(d>1.5f&&d<best){best=d;result=cell;}}return result;
        }
        static bool ClearSight(Transform target){var from=mission.mover.viewCamera.transform.position;var delta=target.position-from;var owner=target.GetComponent<JourneyTarget>();foreach(var hit in Physics.RaycastAll(new Ray(from,delta.normalized),delta.magnitude,~0,QueryTriggerInteraction.Ignore)){if(hit.collider.transform.IsChildOf(mission.mover.origin.transform)||owner&&hit.collider.GetComponentInParent<JourneyTarget>()==owner)continue;if(hit.distance<delta.magnitude-.25f)return false;}return true;}
        static IEnumerator Capture(string species)
        {
            Trigger(false,false);yield return Wait(.15f);yield return Mode(BioToolMode.Capture);var world=mission.world;world.bloodEntities.stream.RequestInflux(species);float start=Time.time;
            while(Time.time-start<40){var source=Find(species);if(!source){world.bloodEntities.stream.RequestInflux(species);Look(mission.mover.viewCamera.transform.position+mission.mover.path.Forward(mission.mover.Distance)*5);yield return Wait(.2f);continue;}Look(source.position);yield return Wait(.15f);Aim(false,source.position);yield return Wait(.1f);Trigger(false,true);float began=Time.time;while(!world.freeTools.Held&&Time.time-began<2){Aim(false,source.position);yield return null;}if(world.freeTools.Held){var slot=world.bloodEntities.Slot(world.freeTools.Held);if(slot!=null&&world.bloodEntities.stream.flow.species[slot.index]==species){yield return Wait(.55f);Check(slot.grab.isSelected,"Actual right Trigger field selects the concrete "+species+" instance through XRI");yield break;}}Trigger(false,false);yield return Wait(.15f);}
            Check(false,"Actual XR field captures requested "+species);
        }
        static IEnumerator Scan(JourneyTarget target)
        {
            int accepted=mission.AcceptedActions;var contact=mission.SceneNumber==6&&target.targetId=="plaque"?mission.world.plaqueChallenge.diagnosisContact:target.GetComponentsInChildren<Collider>().FirstOrDefault(c=>c.enabled);
            Check(contact,"Enabled physical Scanner contact: "+target.targetId);Trigger(true,false);Aim(true,contact.bounds.center);yield return Wait(.15f);Trigger(true,true);float began=Time.time;
            while(Time.time-began<4)
            {
                Aim(true,contact.bounds.center);yield return null;
                if(target.targetId=="check-hemostasis"&&Time.time-began>3.8f)
                    checks.Add("Final scan live: raw="+input.RawScannerPressed+" UI="+input.ScannerBlockedByUi+" enabled="+input.enabled+" locked="+input.LockedTarget?.targetId+" blocked="+input.AimObstruction?.name+" progress="+input.ScanProgress+" targetEnabled="+target.enabled+" rays="+string.Join(",",Physics.RaycastAll(input.scanner.position,input.scanner.forward,4,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance.ToString("F2"))));
                if(input.ScannerBlockedByUi){Trigger(true,false);yield return Wait(.15f);Aim(true,contact.bounds.center);yield return Wait(.15f);Trigger(true,true);}
                if(mission.AcceptedActions>accepted||target.GetComponent<JourneyPlateletEntity>()?.ScannerActivated==true||target.GetComponent<JourneyPhagocyteEntity>()?.Studied==true)break;
            }
            Trigger(true,false);yield return Wait(.15f);
            bool okay=mission.SceneNumber==3?mission.world.research.Find(target.targetId).State==JourneyResearchState.Investigated:target.GetComponent<JourneyPhagocyteEntity>()?target.GetComponent<JourneyPhagocyteEntity>().Studied:target.GetComponent<JourneyPlateletEntity>()?target.GetComponent<JourneyPlateletEntity>().ScannerActivated:mission.AcceptedActions>accepted;
            if(!okay)checks.Add("XR scan: target="+target.targetId+" goal="+mission.Current?.target+" lock="+input.LockedTarget?.targetId+" blocked="+input.ScannerBlockedByUi+" obstacle="+input.AimObstruction?.name+" contact="+contact.bounds+" aim="+input.scanner.position+" active="+target.gameObject.activeInHierarchy+" enabled="+contact.enabled+" direction="+input.scanner.forward+" targetDirection="+(contact.bounds.center-input.scanner.position).normalized);Check(okay,"Actual independent left Trigger scans "+target.targetId);
        }
        static IEnumerator PatrolTest()
        {
            var world=mission.world;Check(world.immunePatrol&&world.plateletLaunch,"Installed immune patrol and launched platelet layers");
            mission.Enter(8,false);yield return Wait(.4f);var virus=world.immunity.TutorialVirus;
            Check(virus.Integrity==1&&virus.spikeGroups.All(g=>g.gameObject.activeSelf),"Tutorial has a full silhouette and needs one contact");
            int attempts=0;
            while(!mission.Complete&&attempts++<14){yield return Capture("phagocyte");yield return Scan(world.freeTools.Held);Check(world.freeTools.Held.GetComponent<JourneyPhagocyteEntity>().State==JourneyPhagocyteState.Activated,"Left Scanner activates the held phagocyte");yield return FireVirus(virus);yield return Wait(1.3f);}
            Check(mission.Complete&&world.immunity.DamageContacts==1,"One actual XR launch completes the tutorial");
            yield return Continue(4.2f);Check(mission.SceneNumber==9&&mission.Ready,"Actual left Y enters the later viral wave");
            virus=world.immunity.viruses.First(v=>v.Live);int hits=0;attempts=0;
            while(virus.Live&&attempts++<18){yield return Capture("phagocyte");yield return Scan(world.freeTools.Held);int before=virus.Integrity;yield return FireVirus(virus);if(virus.Integrity<before){hits++;Check(virus.Integrity==before-1&&virus.spikeGroups.Count(g=>g.gameObject.activeSelf)==virus.Integrity,"XR contact removes one third of late viral spikes "+hits+"/3");}yield return Wait(1.3f);}
            Check(hits==3&&!virus.Live,"Later virus needs three actual XR contacts");
            mission.Enter(10,false);yield return Wait(.4f);yield return PatrolAndWound();
        }
        static IEnumerator IntegratedTest()
        {
            var world=mission.world;PatrolWoundIntegration.Validate(world);world.GetComponent<JourneyBloodFlow>().SetVisibleCellLimit(180);
            if(focused){mission.Enter(10,false);yield return Wait(.4f);yield return PatrolAndWound();yield return BalanceWound();yield return RecoveryTest();Check(true,"Integration XR route complete");yield break;}
            mission.Enter(9,false);yield return Wait(.4f);
            int attempts=0;
            while(!mission.Complete&&attempts++<64)
            {
                float spawn=Time.time;while(world.immunity.LiveCount==0&&Time.time-spawn<15)yield return Wait(.15f);Check(world.immunity.LiveCount>0,"Next viral particle arrives from the existing encounter pool");
                var virus=world.immunity.viruses.First(v=>v.Live);yield return Capture("phagocyte");yield return Scan(world.freeTools.Held);int before=virus.Integrity;yield return FireVirus(virus);
                if(virus.Integrity<before)Check(virus.spikeGroups.Count(g=>g.gameObject.activeSelf)==virus.Integrity,"Visible later viral damage tracks actual contacts");yield return Wait(1.3f);
            }
            Check(mission.Complete&&world.immunity.LiveCount==0,"Entire later viral wave completed through XR controls");
            yield return Continue(10);Check(mission.SceneNumber==10&&mission.Ready,"Ordinary left Y transition 09 to 10 enters patrol rather than a duplicate radius lesson");
            yield return PatrolAndWound();yield return BalanceWound();yield return RecoveryTest();yield return FinalTest();Check(true,"Integration XR route complete");
        }
        static IEnumerator PatrolAndWound()
        {
            var world=mission.world;var immune=world.immunePatrol;
            Check(immune.Stage==ImmunePatrolStage.Discovery&&mission.Goals.Count==3,"Scene10 starts with immune discovery");
            Check(!world.hemodynamics.Active&&!world.hemodynamics.arRoot.activeInHierarchy&&mission.Goals.All(g=>g.action!=StudyAction.Radius)&&mission.LearningTerm=="Иммунный патруль","Old radius controls and briefing are absent in the actual patrol stage");
            if(integrated){yield return Mode(BioToolMode.Tonus);Trigger(false,true);yield return Wait(.25f);Trigger(false,false);Check(Mathf.Abs(world.hemodynamics.RadiusRatio-1)<.001f&&mission.StepIndex==0,"Residual TONUS input cannot stretch the immune wall or skip discovery");}
            Look(immune.wallTarget.transform.position);yield return Wait(.15f);yield return Scan(immune.wallTarget);Check(mission.StepIndex==1,"Left Scanner reveals surveillance theory");
            Look(immune.revealTarget.transform.position);yield return Wait(.15f);yield return Scan(immune.revealTarget);yield return Wait(1);Check(mission.StepIndex==2,"Second deliberate left Scanner hold opens the existing wall cutaway");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"PatrolWoundIntegration/01_Revealed_Tissue.png");
            var origin=mission.mover.origin.transform;var position=origin.position;var rotation=origin.rotation;
            foreach(int index in new[]{4,0,5,2,1,3})
            {
                var focus=immune.focuses[index];yield return Mode(BioToolMode.Reverse);Look(focus.transform.position);yield return Wait(.2f);Aim(false,focus.transform.position);yield return Wait(.15f);Trigger(false,true);float began=Time.time;
                while(!immune.CurrentFocus&&Time.time-began<3){Aim(false,focus.transform.position);yield return null;}Trigger(false,false);Check(immune.CurrentFocus==focus,"Actual right Reverse Trigger selects focus "+(index+1));
                began=Time.time;bool captured=false;while(focus.State!=ImmuneFocusState.Treated&&Time.time-began<30){if(!captured&&immune.TotalResponders>=focus.responders){ArteryPolishReview.Capture(mission.mover.viewCamera,"PatrolWoundIntegration/02_WBC_Response.png");captured=true;}yield return Wait(.15f);}Check(focus.State==ImmuneFocusState.Treated,"Pooled WBC treat focus "+(index+1));
                Check(Vector3.Distance(origin.position,position)<.001f&&Quaternion.Angle(origin.rotation,rotation)<.01f,"XR Origin remains stationary during patrol response");
                Check(immune.focuses.Count(f=>f.cells.Any(c=>c.enabled))<=3,"At most three pathological clusters are presented simultaneously");
            }
            yield return Wait(1);Check(mission.Complete&&immune.ReadyToLeave&&immune.Treated==6&&immune.TotalResponders==24&&immune.MaximumResponders<=5,"Six focuses use 24 returned WBC and at most five simultaneous responders");
            yield return Continue(10);Check(mission.SceneNumber==11&&mission.Ready,"Actual left Y travels from patrol to wound diagnostics");
            Check(mission.PressureSystolic<110&&Vector3.Distance(mission.mover.viewCamera.transform.position,world.woundSite.position)<4.5f,"Wound station is readable at close range and independently reports low blood pressure");
            Look(world.woundSite.position);yield return Wait(.2f);ArteryPolishReview.Capture(mission.mover.viewCamera,"PatrolWoundIntegration/04_Wound_Discovery.png");
            foreach(var id in new[]{"wound","leak","pressure-low"}){Look(world.Find(id).transform.position);yield return Wait(.15f);yield return Scan(world.Find(id));}
            yield return Continue(4.2f);Check(mission.SceneNumber==12&&mission.Ready,"XR Scanner diagnostics and left Y enter the same wound platelet stage");
            Check(world.hemostasisPuzzle.platelets.All(p=>p.Placed||!p.gameObject.activeInHierarchy),"Platelets come from common blood flow rather than a dedicated row");
            var woundView=world.GetComponent<JourneyLacerationView>();Check(woundView.interactionRim&&woundView.interactionRim.enabled&&woundView.plateletHint,"Existing wound view presents its interaction contour and contextual platelet hint");
            if(integrated)
            {
                yield return Capture("platelet");yield return Mode(BioToolMode.Reverse);yield return LaunchPlatelet(world.woundSite.position);yield return Wait(3);
                Check(world.hemostasisPuzzle.Attached==0,"An unscanned platelet cannot grant adhesion or task progress");
                yield return Capture("platelet");yield return Scan(world.freeTools.Held);yield return Mode(BioToolMode.Reverse);int misses=world.plateletLaunch.Misses;
                yield return LaunchPlatelet(world.woundSite.position+mission.mover.path.Up(mission.mover.Distance)*1.1f);yield return Wait(3);
                Check(world.hemostasisPuzzle.Attached==0&&world.plateletLaunch.Misses==misses+1,"A deliberate shot outside the visible tear misses without granting progress");
            }
            int placed=0,attempts=0;
            while(!mission.Complete&&attempts++<25)
            {
                yield return Capture("platelet");yield return Wait(.15f);Check(mission.GetComponent<JourneyHolographicScreens>().taskText.text.Contains("Scanner"),"Task block explains Scanner activation separately from educational theory");yield return Scan(world.freeTools.Held);yield return Mode(BioToolMode.Reverse);
                var point=world.woundSite.position;
                Look(point);yield return Wait(.15f);Aim(false,point);yield return Wait(.15f);Trigger(false,true);float began=Time.time;
                while(Time.time-began<.8f){Aim(false,point);yield return null;}Check(world.freeTools.ReverseCharge>.8f&&world.plateletLaunch.preview.enabled&&world.plateletLaunch.PreviewOnWound,"Actual right Reverse Trigger charges the platelet and confirms the visible wound aim");
                if(placed==0)ArteryPolishReview.Capture(mission.mover.viewCamera,"PatrolWoundIntegration/05_Wound_Ready_To_Release.png");Trigger(false,false);
                began=Time.time;while(world.hemostasisPuzzle.Attached==placed&&Time.time-began<3)yield return Wait(.1f);
                Check(world.hemostasisPuzzle.Attached==placed+1,"Aiming at the visible tear attaches the platelet without hidden slot coordinates");
                if(world.hemostasisPuzzle.Attached>placed){placed=world.hemostasisPuzzle.Attached;yield return Wait(.3f);Check(world.woundState.LeakStrength<=Mathf.Pow(1-placed/11f,1.5f)+.1f,"XR launched adhesion reduces actual blood leakage "+placed+"/11");}
                if(placed==11)break;
            }
            yield return Wait(7);Check(placed==11&&mission.Complete&&world.woundState.LeakStrength<.025f,"Eleven actual XR platelet launches form the plug/fibrin and stop outward blood loss");Check(true,"Immune patrol and platelet XR regression complete");
            Check(world.hemostasisPuzzle.platelets.Where(p=>p.Placed).All(p=>Vector3.Dot(p.transform.up,-world.woundState.Outward)>.95f),"Attached platelet faces remain visible from the vessel lumen");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"PatrolWoundIntegration/03_Wound_Closed.png");
        }
        static IEnumerator BalanceWound()
        {
            var world=mission.world;var puzzle=world.hemostasisPuzzle;var path=mission.mover.path;float arc=path.Anchor(11)+5;
            yield return Continue(4.2f);Check(mission.SceneNumber==13&&puzzle.Attached==11,"Ordinary Continue retains the same eleven-platelet plug in balance stage");
            Look(world.woundSite.position);yield return Wait(.15f);yield return Scan(world.Find("clot"));
            yield return Mode(BioToolMode.Capture);
            var aside=world.woundSite.position-path.Right(arc)*.65f+path.Up(arc)*1.25f;
            for(int i=0;i<9;i++){var piece=puzzle.platelets.Where(p=>p.Placed).OrderBy(p=>(p.transform.position-mission.mover.viewCamera.transform.position).sqrMagnitude).First();yield return MovePiece(piece,aside+path.Forward(arc)*((i-4)*.28f));}
            yield return Wait(1);Check(mission.Current?.target=="excessive"&&puzzle.Attached==2,"Actual remote field removal demonstrates insufficient plug and advances balance task");
            for(int i=0;i<18&&puzzle.Attached<14;i++){int count=puzzle.Attached;var piece=puzzle.platelets.Where(p=>!p.Placed).OrderBy(p=>(p.transform.position-mission.mover.viewCamera.transform.position).sqrMagnitude).First();yield return MovePiece(piece,world.woundSite.position-path.Right(arc)*.22f);Check(puzzle.Attached==count+1,"Returning a field-held platelet to the visible wound attaches it without hidden slots");}
            yield return Wait(1);Check(mission.Current?.target=="optimal"&&puzzle.Attached==14,"Actual field placement of reserve platelets demonstrates excessive plug");
            for(int i=0;i<3;i++){var piece=puzzle.platelets.Where(p=>p.Placed).OrderBy(p=>(p.transform.position-mission.mover.viewCamera.transform.position).sqrMagnitude).First();yield return MovePiece(piece,aside+path.Forward(arc)*(i-1)*.3f);}
            yield return Wait(1);Check(mission.Current?.target=="open-flow"&&puzzle.Attached==11,"Actual correction restores optimal plug while preserving lumen");
            Look(world.Find("open-flow").transform.position);yield return Wait(.15f);yield return Scan(world.Find("open-flow"));Check(mission.Complete,"Same-wound balance and Scanner flow confirmation are completable through XR controls");
        }
        static IEnumerator MovePiece(JourneyHemostasisPiece piece,Vector3 point)
        {
            Trigger(false,false);yield return Wait(.15f);Look(piece.transform.position);yield return Wait(.15f);Aim(false,piece.transform.position);yield return Wait(.15f);Trigger(false,true);float began=Time.time;
            while(!input.plateletCapture.HeldPiece&&Time.time-began<3){Aim(false,piece.transform.position);yield return null;}
            if(!input.plateletCapture.HeldPiece)checks.Add("Balance aim: lock="+input.LockedTarget?.targetId+" contact="+input.LockedContact?.name+" blocker="+input.AimObstruction?.name+" UI="+input.ToolBlockedByUi+" goal="+mission.Current?.target+" position="+piece.transform.position);
            Check(input.plateletCapture.HeldPiece,"Existing BioTool field remotely captures a visible plug platelet");
            began=Time.time;
            while(Time.time-began<1.5f){var ray=(point-mission.mover.viewCamera.transform.position).normalized;AimFrom(point-ray*input.plateletCapture.FieldDistance,point);yield return null;}
            Trigger(false,false);yield return Wait(.3f);
        }
        static IEnumerator LaunchPlatelet(Vector3 point)
        {Look(point);yield return Wait(.15f);Aim(false,point);yield return Wait(.15f);Trigger(false,true);float began=Time.time;while(Time.time-began<.8f){Aim(false,point);yield return null;}Trigger(false,false);}
        static IEnumerator RecoveryTest()
        {
            var world=mission.world;yield return Continue(4.2f);Check(mission.SceneNumber==14,"Ordinary Continue enters recovery at the same wound");
            foreach(var debris in world.recoveryDebris){Look(debris.transform.position);yield return Wait(.15f);yield return Scan(debris);}
            yield return Scan(world.Find("cleanup-mode"));yield return Mode(BioToolMode.Reverse);
            foreach(var debris in world.recoveryDebris){Look(debris.transform.position);yield return Wait(.15f);yield return Tool(debris);yield return Wait(2);}
            Check(world.ClearedDebris==3,"Actual Reverse contacts clean all three scanned debris objects");
            Look(world.Find("plasmin").transform.position);yield return Wait(.15f);yield return Scan(world.Find("plasmin"));yield return Scan(world.Find("plasmin"));yield return Wait(4);
            Look(world.Find("repair").transform.position);yield return Wait(.15f);yield return Scan(world.Find("repair"));yield return Wait(4);yield return Scan(world.Find("healed-wall"));Check(mission.Complete&&world.RepairGrowth>.999f,"Scanner activation completes fibrinolysis, repair and healed-wall confirmation");
        }
        static IEnumerator Tool(JourneyTarget target)
        {Aim(false,target.GetComponentsInChildren<Collider>().First(c=>c.enabled).bounds.center);yield return Wait(.15f);Trigger(false,true);yield return Wait(.25f);Trigger(false,false);yield return Wait(.15f);}
        static IEnumerator FinalTest()
        {
            var world=mission.world;yield return Continue(10);Check(mission.SceneNumber==15,"Normal Continue enters the final immune episode");
            Look(world.Find("infected-cell").transform.position);yield return Wait(.15f);yield return Scan(world.Find("infected-cell"));int attempts=0;
            while(mission.Current?.target=="immune-engulf"&&attempts++<70)
            {
                float spawn=Time.time;while(world.immunity.LiveCount==0&&Time.time-spawn<15)yield return Wait(.15f);Check(world.immunity.LiveCount>0,"Final viral particles are available");var virus=world.immunity.viruses.First(v=>v.Live);
                yield return Capture("phagocyte");yield return Scan(world.freeTools.Held);yield return FireVirus(virus);yield return Wait(1.3f);
            }
            Check(mission.Current?.target=="epitope","Final innate defence completes through actual cell launches");
            Look(world.Find("epitope").transform.position);yield return Wait(.15f);yield return Scan(world.Find("epitope"));yield return Mode(BioToolMode.Capture);
            var antibody=world.Find("antibody-A");Look(antibody.transform.position);yield return Wait(.15f);Aim(false,antibody.GetComponent<Collider>().bounds.center);yield return Wait(.15f);float depth=Vector3.Distance(input.bioTool.position,antibody.transform.position);Trigger(false,true);yield return Wait(.6f);Check(world.freeTools.Held==antibody&&world.freeTools.Selected.isSelected,"Existing field/XRI captures the complementary antibody");
            var point=world.Find("epitope").transform.position;float began=Time.time;while(Time.time-began<1.5f){var ray=(point-mission.mover.viewCamera.transform.position).normalized;AimFrom(point-ray*depth,point);yield return null;}Trigger(false,false);yield return Wait(.5f);Check(world.AntibodyBound,"Releasing the actual antibody at its epitope performs binding");
            yield return Mode(BioToolMode.Reverse);Look(world.Find("neutralized").transform.position);yield return Wait(.15f);yield return Tool(world.Find("neutralized"));yield return Wait(2);
            Look(world.Find("t-cell").transform.position);yield return Wait(.15f);yield return Tool(world.Find("t-cell"));yield return Wait(4);
            foreach(var residual in world.residualTargets){Look(residual.transform.position);yield return Wait(.15f);int before=mission.AcceptedActions;yield return Tool(residual);yield return Wait(1.5f);Check(mission.AcceptedActions==before+1&&!residual.gameObject.activeInHierarchy,"One Reverse press clears exactly one distinct residual complex");}
            Check(mission.Complete&&mission.Victory&&world.ResidualsCleared,"Final complexes and infected source clear with actual Reverse actions");yield return Continue(.6f);Check(mission.SceneNumber==16,"Victory unlocks final homeostasis checks");
            foreach(var id in new[]{"check-pressure","check-temperature","check-flow","check-wall","check-virus","check-hemostasis"}){Look(world.Find(id).transform.position);yield return Wait(.15f);yield return Scan(world.Find(id));}
            Check(mission.Complete&&world.HomeostasisChecks==6,"Six final Scanner checks confirm restored homeostasis");yield return Continue(6);yield return Continue(.5f);Check(mission.SceneNumber==18&&mission.FreeResearchUnlocked,"Normal Continue returns to laboratory and opens the final report");
        }
        static IEnumerator FinalSensorAudit()
        {
            var w=mission.world;mission.Enter(16,false);yield return Wait(.5f);var rows=new List<object>();
            foreach(var id in new[]{"check-pressure","check-temperature","check-flow","check-wall","check-virus","check-hemostasis"})
            {
                var target=w.Find(id);foreach(var other in w.targets)if(other&&other.targetId.StartsWith("check-"))other.GetComponent<Collider>().enabled=other==target;
                var collider=target.GetComponent<Collider>();Look(target.transform.position);yield return Wait(.2f);Aim(true,collider.bounds.center);yield return Wait(.2f);Trigger(true,true);yield return Wait(.4f);
                rows.Add(new{id,raw=input.RawScannerPressed,ui=input.ScannerBlockedByUi,locked=input.LockedTarget?input.LockedTarget.targetId:null,obstruction=input.AimObstruction?input.AimObstruction.name:null,progress=input.ScanProgress,targetEnabled=target.enabled,origin=input.scanner.position.ToString(),direction=input.scanner.forward.ToString(),centre=collider.bounds.center.ToString(),rays=Physics.RaycastAll(input.scanner.position,input.scanner.forward,4,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance).ToArray()});
                Trigger(true,false);yield return Wait(.15f);
            }
            File.WriteAllText(FlowLearningConsistencyBuilder.Reports+"/FinalSensorAudit.json",JsonConvert.SerializeObject(rows,Formatting.Indented));sensorAudit=false;
        }
        static IEnumerator PreludeTest()
        {
            var world=mission.world;world.GetComponent<JourneyBloodFlow>().SetVisibleCellLimit(180);
            foreach(var species in new[]{"erythrocyte-O2","leukocyte","platelet","albumin","phagocyte"})
            {
                yield return Capture(species);yield return Scan(world.freeTools.Held);var screens=mission.GetComponent<JourneyHolographicScreens>();var topic=world.freeTools.Held.targetId;
                Check(screens.ShowPractice&&screens.LearnedTopic==topic&&screens.theoryText.text.Length>200&&screens.taskRoot.gameObject.activeInHierarchy,"Scanning "+species+" opens full persistent theory while retaining the task");
                if(species=="erythrocyte-O2")Check(screens.theoryText.text.Contains("гемоглобин")&&screens.theoryText.text.Contains("CO")&&screens.theoryText.text.Contains("Связано с темой"),"Erythrocyte theory includes linked Hb/heme/oxygen/CO2 information");
                Trigger(false,false);yield return Wait(.25f);Check(screens.LearnedTopic==topic&&screens.ShowPractice,"Theory remains open after returning the sample to blood");
            }
            yield return Capture("hemoglobin");yield return Scan(world.freeTools.Held);yield return Scan(world.freeTools.payload.heme);var carrier=world.freeTools.Held;var localBond=carrier.transform.InverseTransformPoint(world.freeTools.payload.heme.transform.position);
            Look(mission.mover.viewCamera.transform.position+mission.mover.path.Up(mission.mover.Distance));yield return Wait(.25f);Check(Vector3.Distance(localBond,carrier.transform.InverseTransformPoint(world.freeTools.payload.heme.transform.position))<.005f,"Heme/iron stays attached to Hb when the player turns their head");Trigger(false,false);yield return Wait(.2f);
            yield return CheckIndependentRays();
            Check(mission.Complete&&world.research.RequiredInvestigated==5,"All five blood types investigated through independent XR capture and Scanner");
            yield return Continue(10);Check(mission.SceneNumber==4,"Normal Continue enters artery layers");
            foreach(var id in new[]{"endothelium","layers","intima","media","adventitia"}){var target=world.Find(id);Look(target.GetComponentsInChildren<Collider>().First(c=>c.enabled).bounds.center);yield return Wait(.15f);yield return Scan(target);yield return Wait(1);}
            Check(mission.Complete,"Existing artery-layer cutaway fully completable with Scanner");
            yield return Continue(10);Look(world.Find("hdx-diameter").transform.position);yield return Wait(.15f);yield return Scan(world.Find("hdx-diameter"));yield return Mode(BioToolMode.Tonus);Aim(false,world.hemodynamics.toneContact.transform.position);yield return Wait(.2f);Trigger(false,true);yield return Wait(.2f);
            var start=rightPose.position;var rotation=rightPose.rotation;var up=mission.mover.path.Up(world.hemodynamics.SectionArc);Pose(false,start+up*.25f,rotation);yield return Wait(.35f);Check(mission.StepIndex==2,"Tracked hand expansion advances the original vascular lesson");Pose(false,start+up*.47f,rotation);yield return Wait(.35f);Check(mission.StepIndex==3&&world.hemodynamics.Critical,"Critical vascular state is visible and recoverable");Pose(false,start,rotation);yield return Wait(.9f);Trigger(false,false);yield return Wait(.15f);yield return Scan(world.Find("hdx-final"));Check(mission.Complete,"Tracked hand correction and Scanner finish vascular lesson");
            yield return Continue(10);Look(world.Find("plaque").transform.position);yield return Wait(.15f);yield return Scan(world.Find("plaque"));yield return Mode(BioToolMode.Impulse);float began=Time.time;bool firing=false;
            while(!mission.Complete&&Time.time-began<52)
            {
                var lipid=world.plaqueChallenge.lipids.Where(p=>p.gameObject.activeInHierarchy&&p.contact.enabled).OrderBy(p=>(p.transform.position-mission.mover.viewCamera.transform.position).sqrMagnitude).FirstOrDefault();
                if(lipid){Look(lipid.contact.bounds.center);yield return Wait(.04f);Aim(false,lipid.contact.bounds.center);if(!firing||input.ToolBlockedByUi){Trigger(false,false);yield return Wait(.12f);Aim(false,lipid.contact.bounds.center);yield return Wait(.12f);Trigger(false,true);firing=true;}}
                yield return Wait(.04f);
            }
            Trigger(false,false);if(!mission.Complete)checks.Add("Plaque diagnostic: destroyed="+world.plaqueChallenge.DestroyedCount+" remaining="+world.plaqueChallenge.RemainingCount+" lock="+input.LockedTarget?.name+" obstacle="+input.AimObstruction?.name+" UI="+input.ToolBlockedByUi+" shots="+world.freeTools.Shots);Check(mission.Complete&&world.plaqueChallenge.Cleared,"Real XR impulse clears the growing plaque");
            yield return Continue(10);yield return Mode(BioToolMode.Capture);
            for(int n=0;n<6;n++)
            {
                var bubble=world.Find("embolus");Look((bubble.transform.position+world.trap.position)*.5f);yield return Wait(.15f);Aim(false,bubble.GetComponent<Collider>().bounds.center);yield return Wait(.15f);Trigger(false,true);began=Time.time;
                while(!world.EmbolusCaptured&&Time.time-began<5){Aim(false,bubble.GetComponent<Collider>().bounds.center);yield return null;}Check(world.EmbolusCaptured,"Actual XR Trigger captures bubble "+(n+1));bubble=world.Find("embolus");
                var origin=input.bioTool.position;var targetOrigin=world.trap.position-(world.trap.position-mission.mover.viewCamera.transform.position).normalized*JourneyWorld.EmbolusHoldDistance;began=Time.time;
                while(world.embolusWave.StabilizedCount<n+1&&Time.time-began<20){var handOrigin=Vector3.Lerp(origin,targetOrigin,Mathf.Clamp01((Time.time-began)/1.5f));var target=Vector3.MoveTowards(bubble.GetComponent<Collider>().bounds.center,world.trap.position,.05f);AimFrom(handOrigin,target);yield return null;}
                Check(world.embolusWave.StabilizedCount==n+1,"XR field deposits bubble "+(n+1)+"/6");Trigger(false,false);yield return Wait(.15f);
            }
            Check(mission.Complete,"All six emboli cleared");yield return Continue(10);Check(mission.SceneNumber==8,"Ordinary transition reaches tutorial virus");int attempts=0;
            while(!mission.Complete&&attempts++<14){yield return Capture("phagocyte");yield return Scan(world.freeTools.Held);yield return FireVirus(world.immunity.TutorialVirus);yield return Wait(1.3f);}
            Check(mission.Complete&&world.immunity.DamageContacts==1,"Tutorial completes after one actual XR contact");Check(true,"Prelude XR route complete");
        }
        static IEnumerator CheckIndependentRays()
        {
            var rig=mission.GetComponent<JourneyInstrumentRig>();var camera=mission.mover.viewCamera.transform;
            Look(camera.position+mission.mover.path.Forward(mission.mover.Distance)*5);yield return Wait(.15f);
            for(int i=0;i<3;i++)
            {
                Aim(true,camera.position+camera.forward*5+camera.right*(i-1));Aim(false,camera.position+camera.forward*5-camera.right*(i-1));yield return Wait(.2f);
                foreach(var pair in new[]{(line:rig.scannerGuide,aim:input.scanner),(line:rig.bioGuide,aim:input.bioTool)})
                {Check(pair.line.enabled&&Vector3.Angle(pair.line.GetPosition(1)-pair.line.GetPosition(0),pair.aim.forward)<.2f,"Independent guide remains collinear with its current controller aim");}
            }
            Check(rig.captureRays.All(r=>!r.enabled)&&!rig.captureTether.enabled,"Release leaves no converging rays or tether at a previous interaction");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"FlowLearningConsistency/Independent_XR_Rays.png");
        }
        static IEnumerator Continue(float travel){ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return Wait(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return Wait(travel);}
        static IEnumerator FireVirus(JourneyImmuneVirus virus)
        {
            yield return Mode(BioToolMode.Reverse);Look(virus.transform.position);yield return Wait(.15f);Aim(false,virus.transform.position);yield return Wait(.15f);Trigger(false,true);float began=Time.time;
            while(Time.time-began<.9f){AimFrom(input.bioTool.position,virus.transform.position);yield return null;}
            Check(mission.world.freeTools.ReverseCharge>.8f&&mission.world.immunity.trajectory.enabled,"Actual right Reverse Trigger charges and previews the activated phagocyte");int before=mission.world.immunity.DamageContacts;Trigger(false,false);began=Time.time;while(mission.world.immunity.DamageContacts==before&&Time.time-began<4.2f)yield return Wait(.05f);
        }
        static IEnumerator Mode(BioToolMode mode){var controller=mission.GetComponent<BioToolModeController>();int attempts=0;while(controller.CurrentMode!=mode&&attempts++<5){rs=rs.WithButton(ControllerButton.PrimaryButton,true);yield return Wait(.08f);rs=rs.WithButton(ControllerButton.PrimaryButton,false);yield return Wait(.08f);}Check(controller.CurrentMode==mode,"Actual right A selects "+mode);}
        static void Trigger(bool isLeft,bool value){if(isLeft){ls.trigger=value?1:0;ls=ls.WithButton(ControllerButton.TriggerButton,value);}else{rs.trigger=value?1:0;rs=rs.WithButton(ControllerButton.TriggerButton,value);}}
        static void Pose(bool isLeft,Vector3 position,Quaternion rotation){var transform=isLeft?leftPose:rightPose;var state=isLeft?ls:rs;state.devicePosition=transform.parent.InverseTransformPoint(position);state.deviceRotation=Quaternion.Inverse(transform.parent.rotation)*rotation;if(isLeft)ls=state;else rs=state;}
        static void Aim(bool isLeft,Vector3 target)
        {
            var view=mission.mover.viewCamera.transform;var origin=view.position;var guard=mission.GetComponent<JourneyHud>().inputGuard;
            foreach(float lateral in new[]{isLeft?-.5f:.5f,.75f,-.75f,0f})
            {
                origin=view.position+view.right*lateral-view.up*.2f+view.forward*.4f;bool blocked=false;
                foreach(var surface in guard.surfaces){if(!surface||!surface.gameObject.activeInHierarchy)continue;var plane=new Plane(surface.forward,surface.position);var ray=new Ray(origin,(target-origin).normalized);if(plane.Raycast(ray,out float distance)&&distance>0&&distance<Vector3.Distance(origin,target)){var local=surface.InverseTransformPoint(ray.GetPoint(distance));if(surface.rect.Contains(new Vector2(local.x,local.y)))blocked=true;}}
                if(!blocked)break;
            }
            var controller=isLeft?leftPose:rightPose;var aim=isLeft?input.scanner:input.bioTool;var mount=Quaternion.Inverse(controller.rotation)*aim.rotation;var offset=Quaternion.Inverse(controller.rotation)*(aim.position-controller.position);var rotation=Quaternion.LookRotation(target-origin,view.up)*Quaternion.Inverse(mount);Pose(isLeft,origin-rotation*offset,rotation);
        }
        static void AimFrom(Vector3 origin,Vector3 target){var mount=Quaternion.Inverse(rightPose.rotation)*input.bioTool.rotation;var offset=Quaternion.Inverse(rightPose.rotation)*(input.bioTool.position-rightPose.position);var rotation=Quaternion.LookRotation(target-origin,mission.mover.viewCamera.transform.up)*Quaternion.Inverse(mount);Pose(false,origin-rotation*offset,rotation);}
        static void Look(Vector3 point){var camera=mission.mover.viewCamera.transform;var rotation=Quaternion.Inverse(camera.parent.rotation)*Quaternion.LookRotation(point-camera.position,mission.mover.path.Up(mission.mover.Distance));hs.deviceRotation=hs.centerEyeRotation=hs.leftEyeRotation=hs.rightEyeRotation=rotation;}
        static IEnumerator Wait(float duration){float time=Time.time;double deadline=EditorApplication.timeSinceStartup+Mathf.Max(60,duration*2+5);while(Time.time-time<duration){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("XR input player loop stopped");yield return null;}}
        static void CaptureView(string file)=>ArteryPolishReview.Capture(mission.mover.viewCamera,(arcade?"ArcadeVisualPass/":"PlayerFeedbackRework/")+file);
    }
}

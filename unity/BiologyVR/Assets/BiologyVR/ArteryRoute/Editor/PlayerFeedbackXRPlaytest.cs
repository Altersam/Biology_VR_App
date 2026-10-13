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
        static bool running;static readonly List<string> checks=new List<string>();
        [MenuItem("Biology VR/Test Player Feedback Rework Through XR Triggers")]
        public static void Run(){if(!EditorApplication.isPlaying||running)throw new InvalidOperationException("Fresh Play Mode required");mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();input=mission.GetComponent<JourneyInput>();running=true;checks.Clear();Directory.CreateDirectory(Folder);EditorCoroutineUtility.StartCoroutineOwnerless(Guard());}
        static void Report()=>File.WriteAllText(Folder+"/XR_Input.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,running,passed=!running&&checks.Contains("PASS: Feedback XR trigger sequence complete")&&!checks.Any(c=>c.StartsWith("BLOCKED")),method="Actual XRSimulatedController/HMD states, original action bindings, XRI field selection and tracked hand motion. Isolated Scene05/07/08 fixtures; not physical-headset testing.",checks},Formatting.Indented));
        static void Check(bool value,string text){checks.Add((value?"PASS: ":"BLOCKED: ")+text);Report();if(!value)throw new InvalidOperationException(text);}
        static IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Test());try{while(stack.Count>0){var current=stack.Peek();bool moved=false;Exception error=null;try{moved=current.MoveNext();}catch(Exception e){error=e;}if(error!=null){checks.Add("BLOCKED: "+error);Debug.LogException(error);yield break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current.Current is IEnumerator nested)stack.Push(nested);else yield return current.Current;}}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;Report();}}
        static void Queue(){if(InputState.currentUpdateType!=InputUpdateType.Dynamic&&InputState.currentUpdateType!=InputUpdateType.Fixed)return;InputSystem.QueueStateEvent(left,ls);InputSystem.QueueStateEvent(right,rs);InputSystem.QueueStateEvent(head,hs);}
        static IEnumerator Test()
        {
            var simulators=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b is XRDeviceSimulator||b is XRInteractionSimulator).Cast<Behaviour>().ToArray();var enabled=simulators.Select(s=>s.enabled).ToArray();var settings=InputSystem.settings;var behavior=settings.backgroundBehavior;var editor=settings.editorInputBehaviorInPlayMode;bool explicitAim=input.useExplicitAimSources;
            try
            {
                foreach(var simulator in simulators)simulator.enabled=false;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                SimulatedInputLayoutLoader.Initialize();left=InputSystem.AddDevice<XRSimulatedController>();right=InputSystem.AddDevice<XRSimulatedController>();head=InputSystem.AddDevice<XRSimulatedHMD>();InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
                leftPose=mission.mover.origin.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Left Controller");rightPose=mission.mover.origin.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Right Controller");
                ls=new XRSimulatedControllerState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity};rs=ls;hs=new XRSimulatedHMDState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity,centerEyeRotation=Quaternion.identity,leftEyeRotation=Quaternion.identity,rightEyeRotation=Quaternion.identity};input.useExplicitAimSources=true;InputSystem.onBeforeUpdate+=Queue;yield return Wait(.4f);
                var world=mission.world;Check(world.freeTools&&input.scanAction.action.enabled&&input.toolAction.action.enabled,"Original XR Scanner/BioTool actions are live with the new field tool");
                var source=Find("erythrocyte-O2");if(!source){world.bloodEntities.stream.RequestInflux("erythrocyte-O2");yield return Wait(1);source=Find("erythrocyte-O2");}Check(source,"Oxygen-loaded carrier is in ordinary flow");
                Pose(false,source.position,Quaternion.identity);rs.grip=1;rs=rs.WithButton(ControllerButton.GripButton,true);yield return Wait(.4f);Check(!world.freeTools.Held&&world.research.RequiredInvestigated==0,"Middle-finger Grip cannot capture a flowing sample or grant research credit");rs.grip=0;rs=rs.WithButton(ControllerButton.GripButton,false);yield return Wait(.15f);
                yield return Capture("erythrocyte-O2");var rbc=world.freeTools.Held;yield return Scan(rbc);yield return Scan(world.freeTools.payload.oxygen);Check(world.freeTools.Held==rbc&&!world.analysis.Opened,"Independent left Scanner studies bound oxygen while right Trigger holds the same RBC");CaptureView("07_XR_Bound_Cargo_And_Panels.png");Trigger(false,false);yield return Wait(.3f);Check(!world.freeTools.Held&&!world.bloodEntities.Owns(rbc),"Actual right Trigger release returns the sampled RBC to flow");
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
                mission.Enter(8,false);yield return Wait(.3f);
                for(int n=0;n<3;n++)
                {
                    yield return Capture("phagocyte");var cell=world.freeTools.Held.GetComponent<JourneyPhagocyteEntity>();yield return Scan(cell.target);
                    yield return Mode(BioToolMode.Bio);Check(world.freeTools.Held==cell.target,"A/B mode transition retains the captured phagocyte as the user changes tools");Trigger(false,true);yield return Wait(.85f);Check(cell.State==JourneyPhagocyteState.Activated,"Right activation Trigger activates the actually scanned cell");yield return Mode(BioToolMode.Reverse);
                    var virus=world.immunity.TutorialVirus;Aim(false,virus.transform.position);yield return Wait(.8f);var begin=input.bioTool.position;var rotation=rightPose.rotation;var back=-input.bioTool.forward;var offset=rightPose.position-begin;float age=Time.time;
                    while(Time.time-age<.4f){Pose(false,begin+offset+back*.20f*Mathf.Clamp01((Time.time-age)/.4f),rotation);yield return null;}yield return Wait(.25f);Check(world.freeTools.ReverseCharge>.8f&&world.immunity.trajectory.enabled,"Tracked reverse hand pull charges the held cell and previews its trajectory");CaptureView("08_XR_Reverse_Hand_Pull.png");Trigger(false,false);float launched=Time.time;
                    while(world.immunity.DamageContacts<=n&&Time.time-launched<5)yield return Wait(.1f);Check(world.immunity.DamageContacts==n+1,"Actual XR release contacts the slow virus "+(n+1)+"/3");yield return Wait(1.3f);
                }
                Check(mission.Complete&&world.immunity.Neutralized==1,"Three real XR field cycles complete the virus encounter");Check(true,"Feedback XR trigger sequence complete");
            }
            finally{InputSystem.onBeforeUpdate-=Queue;if(left!=null)InputSystem.RemoveDevice(left);if(right!=null)InputSystem.RemoveDevice(right);if(head!=null)InputSystem.RemoveDevice(head);input.useExplicitAimSources=explicitAim;for(int i=0;i<simulators.Length;i++)if(simulators[i])simulators[i].enabled=enabled[i];settings.backgroundBehavior=behavior;settings.editorInputBehaviorInPlayMode=editor;}
        }
        static Transform Find(string species){var stream=mission.world.bloodEntities.stream;Transform result=null;float best=8;for(int i=0;i<stream.flow.VisibleCellLimit;i++){var cell=stream.flow.cells[i];if(stream.Leased(i)||stream.flow.species[i]!=species||!cell.GetComponent<Renderer>().enabled||species=="phagocyte"&&!mission.world.bloodEntities.HasPhagocyteCapacity(i))continue;float d=Vector3.Distance(cell.position,mission.mover.viewCamera.transform.position);if(d>1.5f&&d<best){best=d;result=cell;}}return result;}
        static IEnumerator Capture(string species)
        {
            Trigger(false,false);yield return Wait(.15f);yield return Mode(BioToolMode.Capture);var world=mission.world;world.bloodEntities.stream.RequestInflux(species);float start=Time.time;
            while(Time.time-start<40){var source=Find(species);if(!source){yield return Wait(.2f);continue;}Look(source.position);yield return Wait(.15f);Aim(false,source.position);yield return Wait(.1f);Trigger(false,true);float began=Time.time;while(!world.freeTools.Held&&Time.time-began<2){Aim(false,source.position);yield return null;}if(world.freeTools.Held){var slot=world.bloodEntities.Slot(world.freeTools.Held);if(slot!=null&&world.bloodEntities.stream.flow.species[slot.index]==species){yield return Wait(.55f);Check(slot.grab.isSelected,"Actual right Trigger field selects the concrete "+species+" instance through XRI");yield break;}}Trigger(false,false);yield return Wait(.15f);}
            Check(false,"Actual XR field captures requested "+species);
        }
        static IEnumerator Scan(JourneyTarget target){int step=mission.StepIndex;Aim(true,target.GetComponent<Collider>().bounds.center);yield return Wait(.15f);Trigger(true,true);float began=Time.time;while(Time.time-began<1.2f){Aim(true,target.GetComponent<Collider>().bounds.center);yield return null;}Trigger(true,false);yield return Wait(.15f);bool okay=mission.SceneNumber==3?mission.world.research.Find(target.targetId).State==JourneyResearchState.Investigated:target.GetComponent<JourneyPhagocyteEntity>()?target.GetComponent<JourneyPhagocyteEntity>().Studied:mission.StepIndex>step;if(!okay)checks.Add("XR scan: target="+target.targetId+" goal="+mission.Current?.target+" lock="+input.LockedTarget?.targetId+" blocked="+input.ScannerBlockedByUi+" obstacle="+input.AimObstruction?.name+" contact="+target.GetComponent<Collider>().bounds+" aim="+input.scanner.position);Check(okay,"Actual independent left Trigger scans "+target.targetId);}
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
        static IEnumerator Wait(float duration){float time=Time.time;double deadline=EditorApplication.timeSinceStartup+Mathf.Max(15,duration*2+5);while(Time.time-time<duration){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("XR input player loop stopped");yield return null;}}
        static void CaptureView(string file)=>ArteryPolishReview.Capture(mission.mover.viewCamera,"PlayerFeedbackRework/"+file);
    }
}

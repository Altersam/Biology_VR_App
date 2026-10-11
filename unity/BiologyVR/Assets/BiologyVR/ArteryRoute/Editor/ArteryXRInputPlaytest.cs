using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>Editor QA driver for the simulator's real device layouts. Never completes objectives by API.</summary>
    public static class ArteryXRInputPlaytest
    {
        static XRSimulatedController left,right;static XRSimulatedHMD head;
        static XRSimulatedControllerState ls,rs;static JourneyMission mission;static Transform lc,rc;
        static Behaviour[] simulators;
        static bool[] simulatorEnabled;
        static XRSimulatedHMDState hs;
        static float runtimeGrip;static bool runtimeGripButton;
        static readonly List<string> notes=new List<string>();static bool running,previewOnly,plaqueFixture,controllerFixture;
        [MenuItem("Biology VR/Playtest Scene03 Through XR Input")]
        public static void Run()
        {Start(false);}
        [MenuItem("Biology VR/Preview Biomedical Devices Through XR Input")]
        public static void PreviewDevices()=>Start(true);
        [MenuItem("Biology VR/Test Plaque Challenge Through XR Input")]
        public static void PlaqueOnly()=>Start(false,true);
        [MenuItem("Biology VR/Test XR Controller Animation Guides And UI Lifecycle")]
        public static void ControllersOnly()=>Start(false,false,true);
        static void Start(bool preview,bool plaque=false,bool controllers=false)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Start a fresh Play Mode session first");
            if(running)throw new InvalidOperationException("XR playtest already running");
            mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();
            if(mission.SceneNumber!=3||mission.StepIndex!=0)throw new InvalidOperationException("Fresh Scene03 required; this test does not reset/complete mission state. Actual scene="+mission.SceneNumber+" step="+mission.StepIndex);
            previewOnly=preview;plaqueFixture=plaque;controllerFixture=controllers;running=true;EditorCoroutineUtility.StartCoroutineOwnerless(GuardedPlay());
        }
        static IEnumerator GuardedPlay()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Play());
            try
            {
                while(stack.Count>0)
                {
                    var current=stack.Peek();bool moved=false;Exception failure=null;
                    try{moved=current.MoveNext();}catch(Exception exception){failure=exception;}
                    if(failure!=null){notes.Add("BLOCKED: "+failure.Message);Debug.LogException(failure);yield break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                    if(current.Current is IEnumerator nested){stack.Push(nested);continue;}
                    yield return current.Current;
                }
            }
            finally
            {
                while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
                running=false;WriteReport();
            }
        }
        static void WriteReport()
        {
            if(controllerFixture){Directory.CreateDirectory(ArteryPolishReview.Reports+"/XRControllers");File.WriteAllText(ArteryPolishReview.Reports+"/XRControllers/Input_Lifecycle.md","# XR controller lifecycle\n\nUTC: "+DateTime.UtcNow.ToString("O")+"\n\nStatus: "+(running?"RUNNING":notes.Contains("PASS: Controller lifecycle input regression complete")&&!notes.Any(n=>n.StartsWith("BLOCKED"))?"PASS":"BLOCKED")+"\n\n"+string.Join("\n\n",notes));return;}
            if(plaqueFixture){Directory.CreateDirectory(PlaqueChallengeBuilder.Reports);File.WriteAllText(PlaqueChallengeBuilder.Reports+"/XR_Plaque_Input.md","# Actual XR plaque fixture\n\nUTC: "+DateTime.UtcNow.ToString("O")+"\n\n"+string.Join("\n\n",notes));return;}
            if(previewOnly){Directory.CreateDirectory(BiomedicalLivingPass.Reports+"/Stage1");File.WriteAllText(BiomedicalLivingPass.Reports+"/Stage1/XR_Input_Preview.md","# Controller-mounted device preview\n\nUTC: "+DateTime.UtcNow.ToString("O")+"\n\n"+string.Join("\n\n",notes));return;}
            bool full=notes.Contains("PASS: Six actual final measurements produce mission result and grades");
            File.WriteAllText(ArteryPolishReview.Reports+"/VR_INPUT_PLAYTHROUGH_V7.md","# V8 Sequential XR Input Playthrough\n\nGenerated UTC: "+DateTime.UtcNow.ToString("O")+"\n\nStatus: "+(running?"RUNNING":full&&!notes.Any(n=>n.StartsWith("BLOCKED"))?"FULL SIMULATOR PASS":"FINISHED — inspect checks/blocker")+"\n\n## Scene03→16 and VERY FAR\n\n"+string.Join("\n\n",notes)+"\n\n## Scope\n\nOnly completed PASS sequences above establish verification for this run. No debug enter/force-next/mission Accept is called by this driver. Scenes05/10 use in-vessel measurements, manual TONUS and physical hypothesis cards when the experiment is installed. Scanner/BioTool are controller-mounted visuals, not physically grabbable instruments. Physical Quest/Pico, thermal/FPS and joystick comfort remain unverified. VERY FAR uses tracked HMD state excursions, not a mission reset. Scenes12–16 are verified only when their actual hand/tool/measurement sequences above pass.\n");
        }
        static void Require(bool condition,string text)
        {notes.Add((condition?"PASS: ":"BLOCKED: ")+text);WriteReport();if(!condition)throw new InvalidOperationException(text);}
        static IEnumerator Play()
        {
            notes.Clear();notes.Add("XR Input System simulator layouts; controller states queued into actual action bindings and XRI. Not a physical headset test.");
            var w=mission.world;var input=mission.GetComponent<JourneyInput>();bool explicitAim=input.useExplicitAimSources;
            var settings=InputSystem.settings;var background=settings.backgroundBehavior;
            var editorBehavior=settings.editorInputBehaviorInPlayMode;
            try
            {
                SimulatedInputLayoutLoader.Initialize();
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                simulators=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(s=>s is XRDeviceSimulator||s is XRInteractionSimulator).Cast<Behaviour>().ToArray();simulatorEnabled=simulators.Select(s=>s.enabled).ToArray();foreach(var simulator in simulators)simulator.enabled=false;
                left=InputSystem.AddDevice<XRSimulatedController>();right=InputSystem.AddDevice<XRSimulatedController>();head=InputSystem.AddDevice<XRSimulatedHMD>();
                InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
                lc=mission.mover.origin.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Left Controller");rc=mission.mover.origin.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Right Controller");
                ls=new XRSimulatedControllerState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity};rs=ls;
                hs=new XRSimulatedHMDState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity,centerEyeRotation=Quaternion.identity,leftEyeRotation=Quaternion.identity,rightEyeRotation=Quaternion.identity};
                InputSystem.onBeforeUpdate+=QueueRuntimeStates;InputSystem.onAfterUpdate+=ObserveRuntimeState;
                input.useExplicitAimSources=true;
                yield return new EditorWaitForSeconds(.45f);
                if(controllerFixture){yield return ControllerLifecycle();yield break;}
                if(plaqueFixture)
                {
                    mission.Enter(6,false);yield return GameSeconds(.25f);
                    yield return ToolAction("plaque-flow");yield return Scan("plaque");yield return GameSeconds(2.8f);yield return ToolAction("pulse-mode");yield return PulseGrowingMass();
                    Require(mission.Complete&&w.plaqueChallenge.Cleared,"Actual XR pulse challenge completes without success APIs");yield break;
                }
                var screens=mission.GetComponent<JourneyHolographicScreens>();
                if(screens)
                {
                    var view=mission.mover.viewCamera.transform;Pose(true,view.position+view.rotation*new Vector3(-.55f,-.50f,.55f),view.rotation);yield return GameSeconds(.10f);
                    Require(!screens.hud.panelRoot.GetComponent<Canvas>().enabled&&screens.theoryText.text.Contains(mission.LearningNote),"Tracked view opens main theory without the old side-panel overlay");
                    var keyboard=InputSystem.AddDevice<Keyboard>();
                    try
                    {
                        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return GameSeconds(.08f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return GameSeconds(.12f);
                        Aim(false,screens.theoryRoot.position);yield return GameSeconds(.15f);float top=screens.theoryScroll.verticalNormalizedPosition;
                        rs.primary2DAxis=new Vector2(0,-.9f);yield return GameSeconds(.45f);rs.primary2DAxis=Vector2.zero;yield return GameSeconds(.10f);
                        Require(screens.theoryScroll.verticalNormalizedPosition<top-.1f&&screens.ScrollInputs>0,"Actual right XR thumbstick scrolls the aimed holographic theory panel");
                        Directory.CreateDirectory(ArteryPolishReview.Reports+"/ActiveInstruments");ArteryPolishReview.Capture(mission.mover.viewCamera,"ActiveInstruments/08_Holographic_Theory_Actual_XR_Scroll.png");
                        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return GameSeconds(.08f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return GameSeconds(.08f);
                    }
                    finally{InputSystem.RemoveDevice(keyboard);}
                }
                Require(input.scanAction.action.enabled&&input.toolAction.action.enabled,"Existing Scanner/BioTool actions enabled");
                var tools=mission.GetComponent<JourneyInstrumentRig>();
                if(tools)
                {
                    int step=mission.StepIndex;
                    foreach(var expected in new[]{BioToolMode.Capture,BioToolMode.Tonus,BioToolMode.Bio,BioToolMode.Impulse})
                    {
                        rs=rs.WithButton(ControllerButton.PrimaryButton,true);yield return GameSeconds(.08f);
                        rs=rs.WithButton(ControllerButton.PrimaryButton,false);yield return GameSeconds(.08f);
                        Require(tools.modes.CurrentMode==expected&&mission.StepIndex==step,"Actual right A selects "+expected+" without Continue or mission success");
                    }
                    foreach(var expected in new[]{BioToolMode.Bio,BioToolMode.Tonus,BioToolMode.Capture,BioToolMode.Impulse})
                    {rs=rs.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.08f);rs=rs.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.08f);Require(tools.modes.CurrentMode==expected&&mission.StepIndex==step&&mission.SceneNumber==3,"Actual right B selects previous mode without Continue: "+expected);}
                    Require(tools.scannerVR.gameObject.activeInHierarchy&&tools.bioVR.gameObject.activeInHierarchy&&!tools.scannerPC.gameObject.activeInHierarchy&&!tools.bioPC.gameObject.activeInHierarchy,"Tracked input displays only VR tool views");
                    Require(tools.scannerVR.GetComponentsInChildren<Collider>(true).Length==0&&tools.bioVR.GetComponentsInChildren<Collider>(true).Length==0,"Biomedical tool views add no physical contacts");
                    notes.Add("Installed device mesh triangles: Scanner="+tools.scannerVR.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3)+"; BioTool="+tools.bioVR.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3));
                }
                var rbc=w.Find("rbc");Vector3 centre=rbc.GetComponent<SphereCollider>().bounds.center;
                Pose(false,centre+Vector3.right*.02f,Quaternion.identity);Pose(true,centre+Vector3.left*.4f,Quaternion.identity);
                yield return new EditorWaitForSeconds(.3f);Grip(false,true);yield return new EditorWaitForSeconds(.45f);
                notes.Add("Diagnostic: step="+mission.StepIndex+" ready="+mission.Ready+" RBC selected="+rbc.GetComponent<XRGrabInteractable>().isSelected+" grabbed="+rbc.grabbed+" runtime right grip="+runtimeGrip+" button="+runtimeGripButton+" deviceEnabled="+right.enabled+" focused="+Application.isFocused+" controller="+rc.position+" RBC="+rbc.transform.position+" bounds="+rbc.GetComponent<SphereCollider>().bounds+" collider="+rbc.GetComponent<SphereCollider>().enabled+" grab="+rbc.GetComponent<XRGrabInteractable>().enabled);
                notes.Add("RBC target enabled="+rbc.isActiveAndEnabled+" mission matches="+(rbc.mission==mission)+" selecting="+string.Join(",",rbc.GetComponent<XRGrabInteractable>().interactorsSelecting.Select(t=>t.transform.name)));
                foreach(string field in new[]{"eventsAttached","grab","disabling","lastAcceptedGrabId"})notes.Add("RBC "+field+"="+typeof(JourneyTarget).GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(rbc));
                foreach(var interactor in mission.mover.origin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true))notes.Add("Interactor "+interactor.name+" "+interactor.GetType().Name+" enabled="+interactor.isActiveAndEnabled+" hover="+interactor.hasHover+" selected="+interactor.hasSelection+" targets="+string.Join(",",interactor.interactablesSelected.Select(t=>t.transform.name))+" selectActive="+interactor.isSelectActive+" pos="+interactor.transform.position);
                foreach(var behaviour in mission.mover.origin.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    var theme=behaviour.GetType().GetProperty("affordanceTheme");
                    if(theme!=null&&theme.GetValue(behaviour)==null)notes.Add("Uninitialized affordance theme: "+behaviour.name+" / "+behaviour.GetType().Name+" active="+behaviour.isActiveAndEnabled);
                }
                Require(mission.StepIndex==1&&rbc.GetComponent<XRGrabInteractable>().isSelected,"Right grip actually selects RBC and advances Grab");
                if(previewOnly)
                {
                    var camera=mission.mover.viewCamera.transform;
                    Pose(false,camera.position+camera.rotation*new Vector3(.45f,-.10f,1.8f),camera.rotation);yield return GameSeconds(.35f);
                    Aim(true,rbc.transform.position);Trigger(true,true);yield return GameSeconds(.35f);
                    Directory.CreateDirectory(ArteryPolishReview.Reports+"/BiomedicalLivingPass/Stage1");
                    ArteryPolishReview.Capture(mission.mover.viewCamera,"BiomedicalLivingPass/Stage1/01_Scanner_In_Hand.png");Trigger(true,false);yield return GameSeconds(.12f);
                    Grip(false,false);Pose(true,camera.position+camera.rotation*new Vector3(-.40f,-.36f,.60f),camera.rotation);
                    Aim(false,camera.position+camera.forward*3);int before=mission.AcceptedActions;Trigger(false,true);yield return GameSeconds(.30f);
                    Require(mission.AcceptedActions==before,"Device visual preview does not complete a wrong biological objective");
                    ArteryPolishReview.Capture(mission.mover.viewCamera,"BiomedicalLivingPass/Stage1/02_BioTool_In_Hand.png");
                    Trigger(false,false);yield return GameSeconds(.1f);ArteryPolishReview.Capture(mission.mover.viewCamera,"BiomedicalLivingPass/Stage1/03_Devices_And_UI.png");
                    Require(tools&&tools.ActiveBioTool.prongs.Length==4,"Rebuilt BioTool retains four animated working elements");yield break;
                }
                Aim(true,rbc.transform.position);Trigger(true,true);yield return new EditorWaitForSeconds(.18f);Trigger(true,false);yield return new EditorWaitForSeconds(.2f);
                Require(mission.StepIndex==1,"Early Scanner release does not complete scan");
                Aim(true,rbc.transform.position);Trigger(true,true);yield return new EditorWaitForSeconds(.4f);
                var guideEnd=input.GuideEndpoint(input.scanner,out bool guideContact);notes.Add("During RBC scan: raw="+input.RawScannerPressed+" action="+input.scanAction.action.IsPressed()+" enabled="+input.enabled+" blocked="+input.ScannerBlockedByUi+" progress="+input.ScanProgress+" locked="+(input.LockedTarget?input.LockedTarget.name:"none")+" contact="+(input.LockedContact?input.LockedContact.name:"none")+" obstruction="+(input.AimObstruction?input.AimObstruction.name:"none")+" guideContact="+guideContact+" guideEnd="+guideEnd+" source="+input.scanner.position+" target="+rbc.transform.position+" targetBounds="+rbc.GetComponent<Collider>().bounds);
                Directory.CreateDirectory(ArteryVisualV6.Reports);ArteryPolishReview.Capture(mission.mover.viewCamera,"BioWorldV6/07_Scanner_Actual_XR_Input.png");
                yield return new EditorWaitForSeconds(.6f);Trigger(true,false);yield return new EditorWaitForSeconds(.15f);
                notes.Add("Held RBC scan diagnostic: blocked="+input.ScannerBlockedByUi+" held="+input.ScannerHeld+" progress="+input.ScanProgress+" contact="+(input.LockedContact?input.LockedContact.name:"none")+" obstruction="+(input.AimObstruction?input.AimObstruction.name:"none"));
                Require(mission.StepIndex==2,"Held real Scanner trigger completes raycast scan");
                centre=rbc.transform.position;Pose(true,centre+Vector3.left*.07f,Quaternion.identity);yield return new EditorWaitForSeconds(.2f);Grip(true,true);yield return new EditorWaitForSeconds(.4f);
                Require(rbc.GetComponent<XRGrabInteractable>().interactorsSelecting.Count==2,"Both grips select the same RBC");
                Vector3 reference=centre;Pose(true,reference+Vector3.left*.28f,Quaternion.identity);Pose(false,reference+Vector3.right*.28f,Quaternion.identity);yield return new EditorWaitForSeconds(.5f);
                Require(mission.StepIndex==3,"Separating simulated hands triggers educational enlargement");
                float expandedScale=rbc.transform.lossyScale.x;
                Pose(true,reference+Vector3.left*.04f,Quaternion.identity);Pose(false,reference+Vector3.right*.04f,Quaternion.identity);yield return GameSeconds(.3f);
                Require(rbc.transform.lossyScale.x<expandedScale*.8f&&mission.StepIndex==3,"Bringing both hands together reduces scale without another objective");
                Grip(true,false);yield return new EditorWaitForSeconds(.2f);
                Require(rbc.GetComponent<XRGrabInteractable>().isSelected,"Releasing one hand leaves object in other hand");
                Grip(false,false);yield return new EditorWaitForSeconds(.6f);
                Aim(true,rbc.GetComponent<SphereCollider>().bounds.center);yield return GameSeconds(.12f);Trigger(true,true);yield return GameSeconds(1f);Trigger(true,false);yield return GameSeconds(.2f);
                Require(mission.StepIndex==3,"Scanning the wrong visible target does not complete Hb objective");
                Pose(false,rbc.GetComponent<Collider>().bounds.center,Quaternion.identity);yield return GameSeconds(.12f);Grip(false,true);yield return GameSeconds(.25f);
                Require(rbc.grabbed,"RBC re-extracted from flow for Hb/heme study");
                foreach(string id in new[]{"hemoglobin","heme"}){yield return Scan(id);}
                Require(mission.StepIndex==5,"Hb and heme actually scanned through device trigger");
                Grip(false,false);yield return GameSeconds(.15f);
                var heldHeme=w.Find("heme");Pose(true,heldHeme.GetComponent<Collider>().bounds.center,Quaternion.identity);yield return GameSeconds(.12f);Grip(true,true);yield return GameSeconds(.25f);
                var oxygen=w.Find("oxygen");Pose(false,oxygen.transform.position,Quaternion.identity);yield return new EditorWaitForSeconds(.2f);Grip(false,true);yield return new EditorWaitForSeconds(.35f);
                Require(mission.StepIndex==6,"Physically gripping oxygen detaches it");
                Pose(false,w.Find("heme").transform.position,Quaternion.identity);yield return new EditorWaitForSeconds(.35f);Grip(false,false);yield return new EditorWaitForSeconds(.3f);
                Require(mission.StepIndex==7,"Placing and releasing O2 near heme binds it");
                Grip(true,false);yield return GameSeconds(.18f);
                yield return Scan("co2");
                var white=w.Find("leukocyte");var whitePickup=white.GetComponent<JourneyFlowPickup>();
                var whitePosition=white.transform.position;yield return GameSeconds(.3f);
                Require(whitePickup&&Vector3.Distance(whitePosition,white.transform.position)>.02f,"WBC actually travels in blood flow before XR grip catch");
                Pose(false,white.transform.position,Quaternion.identity);yield return GameSeconds(.12f);Grip(false,true);yield return GameSeconds(.25f);
                Require(white.GetComponent<XRGrabInteractable>().isSelected&&white.grabbed,"Right XR grip catches travelling WBC");
                yield return Scan("leukocyte");Require(mission.StepIndex==9&&white.grabbed,"WBC held scan changes goal to platelet without losing hold");
                var releasedWhitePosition=white.transform.position;Grip(false,false);yield return GameSeconds(.08f);
                Require(!white.grabbed&&Vector3.Distance(releasedWhitePosition,white.transform.position)<.15f,"XR release removes WBC hold without an abrupt home teleport");yield return GameSeconds(.8f);
                Require(!whitePickup.Rejoining&&Vector3.Distance(white.transform.position,w.GetComponent<JourneyBloodFlow>().cells[whitePickup.poolIndex].position)<.015f,"Released XR WBC rejoins actual pool transform and continues downstream");
                yield return Scan("platelet");
                Require(mission.StepIndex==10,"Gas and formed elements scanned through actual Scanner action");
                Aim(false,w.Find("platelet").transform.position);Trigger(false,true);yield return new EditorWaitForSeconds(.15f);Trigger(false,false);yield return new EditorWaitForSeconds(.15f);
                Require(mission.StepIndex==11,"BioTool comparison requires actual ray and trigger");
                yield return Scan("plasma");Require(mission.Complete,"All Scene03 objectives completed without Accept/Primary/debug calls");
                string folder=ArteryPolishReview.Reports+"/V7Input";Directory.CreateDirectory(folder);ArteryWallInteractionValidation.Capture(w,"V7Input/Scene03_XR_Input_Complete.png");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene03_Player_WithHUD.png");
                var origin=mission.mover.origin.transform;Quaternion originRotation=origin.rotation,headRotation=mission.mover.viewCamera.transform.localRotation;
                var scanner=input.scanner;var tool=input.bioTool;var blood=w.GetComponent<JourneyBloodFlow>();
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.15f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);
                Require(mission.SceneNumber==4&&mission.mover.Moving,"Actual left Y InputAction starts completed 03→04 travel");
                Require(w.locationRoots[mission.mover.path.locationIds[0]].activeInHierarchy&&w.IsEpisodeCompleted(3),"Completed Scene03 location stays active during transition");
                yield return ObserveBloodTravel(10f);
                Require(mission.SceneNumber==4&&mission.Ready,"03→04 smooth travel arrives without debug enter/force-next");
                Require(Quaternion.Angle(originRotation,origin.rotation)<.01f&&Quaternion.Angle(headRotation,mission.mover.viewCamera.transform.localRotation)<.01f,"Origin and tracked head rotation preserved across travel");
                Require(input.scanner==scanner&&input.bioTool==tool&&scanner.gameObject.activeInHierarchy&&tool.gameObject.activeInHierarchy,"Same live Scanner/BioTool aim instances persist across travel");
                ArteryWallInteractionValidation.Capture(w,"V7Input/Scene04_Arrival_XR_Input.png");
                yield return Scan("endothelium");
                var layers=w.Find("layers");var layerControl=layers.GetComponentsInChildren<Collider>().First(c=>c.enabled);
                Aim(false,layerControl.bounds.center);yield return GameSeconds(.15f);Trigger(false,true);yield return GameSeconds(.15f);Trigger(false,false);yield return GameSeconds(.15f);
                Require(mission.SceneNumber==4&&mission.StepIndex==2,"Real BioTool trigger activates wall layer unfolding");
                yield return GameSeconds(1.05f);
                foreach(string id in new[]{"intima","media","adventitia"}){yield return Scan(id);yield return GameSeconds(1.05f);}
                Require(mission.Complete&&w.IsEpisodeCompleted(4),"All Scene04 wall shells scanned through real Scanner actions");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene04_Player_WithHUD.png");
                var layerSurface=w.Find("media").GetComponentsInChildren<Renderer>().First(r=>r.name=="Scene04_Band_media");
                Quaternion look=Quaternion.Inverse(mission.mover.viewCamera.transform.parent.rotation)*Quaternion.LookRotation(layerSurface.bounds.center-mission.mover.viewCamera.transform.position,Vector3.up);
                hs.deviceRotation=hs.centerEyeRotation=hs.leftEyeRotation=hs.rightEyeRotation=look;yield return GameSeconds(.2f);
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene04_HMD_LookAtWall_WithHUD.png");
                var hud=mission.GetComponent<JourneyHud>();
                Require(hud&&hud.vitals&&hud.vitals.gameObject.activeInHierarchy&&hud.vitals.rectTransform.parent==hud.panelRoot,"Vitals remain on compact HUD outside optional experiment module");
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);
                Require(mission.SceneNumber==5&&mission.mover.Moving,"Real left Y continues completed Scene04");yield return GameSeconds(10f);Require(mission.SceneNumber==5&&mission.Ready,"Transition 04→05 arrives through normal travel");
                if(w.hemodynamics)yield return HemodynamicEpisode(false);
                else
                {
                foreach(string id in new[]{"flow","flow-model"})yield return ToolAction(id);
                Require(mission.StepIndex==2,"Scene05 flow measurement and holographic model activation use BioTool trigger");
                yield return ToolAction("small-radius");yield return ToolAction("large-radius");yield return ToolAction("pressure");yield return ToolAction("normal-flow");
                Require(mission.Complete&&w.IsEpisodeCompleted(5)&&Mathf.Abs(mission.ModelRadius-1)<.01f&&Mathf.Abs(mission.ModelPressure-120)<.01f,"Scene05 radius/pressure experiment completes and returns to normal flow");
                }
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene05_Player_WithHUD.png");
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);
                Require(mission.SceneNumber==6&&mission.mover.Moving,"Real left Y continues completed Scene05");yield return GameSeconds(10f);Require(mission.SceneNumber==6&&mission.Ready,"Transition 05→06 arrives through normal travel");
                yield return ToolAction("plaque-flow");yield return Scan("plaque");yield return GameSeconds(2.8f);Require(w.plaqueChallenge?w.plaqueChallenge.Ready:w.plaqueDetail&&w.plaqueDetail.Ready,"Scene06 plaque formation completes before pulse mode");yield return ToolAction("pulse-mode");Require(mission.StepIndex==3,"Scene06 plaque inspection and pulse mode use real instruments");
                if(w.plaqueChallenge)yield return PulseGrowingMass();else for(int zone=0;zone<3;zone++)yield return PulseZone(zone);
                Require(mission.Complete&&w.IsEpisodeCompleted(6)&&(w.plaqueChallenge?w.plaqueChallenge.Cleared:w.plaqueState.ProcessedCount==3&&w.plaqueState.CapHits==0),"Distinct real lipid contacts are fully treated through BioTool input");
                yield return GameSeconds(.8f);
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene06_Player_WithHUD.png");
                var plaqueRoot=w.locationRoots[mission.mover.path.locationIds[6-3]];
                var cap=w.plaqueChallenge?w.plaqueChallenge.repairResidue.GetComponent<MeshFilter>():plaqueRoot.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="Plaque_Cap");
                var capMesh=cap.sharedMesh;var capVertices=capMesh.vertices;var capPosition=cap.transform.position;
                Quaternion previousOriginRotation=origin.rotation,previousHeadRotation=mission.mover.viewCamera.transform.localRotation;
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);
                Require(mission.SceneNumber==7&&mission.mover.Moving,"Real left Y continues completed Scene06");yield return GameSeconds(10f);Require(mission.SceneNumber==7&&mission.Ready,"Transition 06→07 arrives through normal travel");
                Require(plaqueRoot.activeInHierarchy&&w.IsEpisodeCompleted(6)&&(w.plaqueChallenge?w.plaqueChallenge.Cleared:w.plaqueState.ProcessedCount==3)&&cap.sharedMesh==capMesh&&cap.sharedMesh.vertices.SequenceEqual(capVertices)&&Vector3.Distance(capPosition,cap.transform.position)<.001f,"Completed plaque retains root, treatment state, mesh instance/vertices and pose across 06→07");
                Require((w.plaqueChallenge?w.plaqueChallenge.lipids.All(z=>!z.contact.enabled):w.plaqueState.Zones.All(z=>!z.Contact.enabled))&&cap.GetComponent<Renderer>().enabled,"Completed plaque retains restored wall and dormant lipid contacts");
                Require(Quaternion.Angle(previousOriginRotation,origin.rotation)<.01f&&Quaternion.Angle(previousHeadRotation,mission.mover.viewCamera.transform.localRotation)<.01f&&input.scanner==scanner&&input.bioTool==tool,"Head/origin rotation and instrument instances persist across 06→07");
                if(w.embolusWave){Directory.CreateDirectory(ArteryPolishReview.Reports+"/InfluxAndEmboli");ArteryPolishReview.Capture(mission.mover.viewCamera,"InfluxAndEmboli/01_Six_Bubbles_Actual_XR_Arrival.png");}
                yield return TrackScan("embolus");yield return ToolAction("attract-mode");
                Aim(false,w.bubble.position);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.1f);Trigger(false,false);yield return GameSeconds(.12f);
                Require(mission.StepIndex==2&&!w.EmbolusCaptured,"Releasing BioTool before charge finishes does not capture embolus");
                yield return TrackToolCapture("embolus");
                Require(mission.StepIndex==3&&w.EmbolusCaptured,"Held BioTool trigger captures moving embolus after charge");
                yield return GameSeconds(.25f);Require(!mission.Complete&&w.EmbolusStability==0,"Capture outside trap does not auto-deposit");
                LookWithHead(w.bubble.position);yield return GameSeconds(.12f);ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene07_Held_Field_WithHUD.png");
                Vector3 releasePosition=w.bubble.position;int scansBeforeRelease=mission.ScannerActions;Trigger(false,false);yield return GameSeconds(.18f);
                Require(!w.EmbolusCaptured&&mission.StepIndex==2&&!mission.Complete,"Releasing trigger breaks attraction and retries only capture goal");
                Require(Vector3.Distance(releasePosition,w.bubble.position)<.75f&&mission.ScannerActions==scansBeforeRelease,"Release preserves nearby bubble pose and completed scanning");
                Vector3 driftPosition=w.bubble.position;yield return GameSeconds(.4f);Require(Vector3.Distance(driftPosition,w.bubble.position)>.02f,"Released embolus drifts again with the flow");
                yield return TrackToolCapture("embolus");Require(w.EmbolusCaptured&&mission.StepIndex==3,"Released embolus can be re-captured through trigger input");
                yield return CarryEmbolusToTrap();Trigger(false,false);yield return GameSeconds(.18f);
                if(w.embolusWave)while(!mission.Complete){yield return TrackToolCapture("embolus");yield return CarryEmbolusToTrap();Trigger(false,false);yield return GameSeconds(.18f);}
                Require(mission.Complete&&w.IsEpisodeCompleted(7)&&!w.EmbolusCaptured&&(w.embolusWave?w.embolusWave.Complete:!w.bubble.gameObject.activeInHierarchy),"Held field physically captures and deposits every gas embolus inside trap");
                if(w.embolusWave)ArteryPolishReview.Capture(mission.mover.viewCamera,"InfluxAndEmboli/04_Six_Bubbles_Actual_XR_Complete.png");
                LookWithHead(w.trap.position);yield return GameSeconds(.12f);
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene07_Player_WithHUD.png");
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);
                Require(mission.SceneNumber==8&&mission.mover.Moving,"Real left Y continues completed Scene07");yield return GameSeconds(10f);Require(mission.SceneNumber==8&&mission.Ready,"Transition 07→08 arrives through normal travel");
                yield return Scan("virus-study");
                var virus=w.Find("virus-study");var virusGrab=virus.GetComponent<XRGrabInteractable>();Require(virusGrab,"Study virus has authored XRI grab support");
                Aim(false,virus.transform.position);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.2f);Trigger(false,false);yield return GameSeconds(.18f);
                Require(mission.StepIndex==1,"BioTool click cannot replace two-hand virus enlargement");
                Vector3 virusCentre=virus.transform.position;var rightAxis=mission.mover.path.Right(mission.mover.Distance);var handFrame=mission.mover.path.Frame(mission.mover.Distance);
                Pose(false,virusCentre+rightAxis*.07f,handFrame);Pose(true,virusCentre-rightAxis*.07f,handFrame);yield return GameSeconds(.2f);
                Grip(false,true);yield return GameSeconds(.2f);Grip(true,true);yield return GameSeconds(.25f);
                Require(virusGrab.interactorsSelecting.Count==2&&virus.grabbed,"Both real grips select the same study virus");
                Pose(false,virusCentre+rightAxis*.30f,handFrame);Pose(true,virusCentre-rightAxis*.30f,handFrame);yield return GameSeconds(.4f);
                Require(mission.StepIndex==2&&w.virusInspection.Enlarged,"Two-hand separation enlarges the persistent intact/cutaway model");
                Require(Vector3.Distance(virus.transform.position,w.virusInspection.CurrentSurface.bounds.center)<.3f,"Virus visual follows its held XRI target");
                LookWithHead(w.virusInspection.CurrentSurface.bounds.center);yield return GameSeconds(.12f);ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene08_TwoHand_Cutaway_WithHUD.png");
                Grip(true,false);yield return GameSeconds(.15f);Require(virusGrab.isSelected,"One released virus grip keeps the other grip selected");Grip(false,false);yield return GameSeconds(.55f);
                Require(w.virusInspection.Enlarged,"Educational virus scale persists after releasing both hands");
                foreach(string id in new[]{"genome","capsid","epitope"})yield return Scan(id);
                Require(mission.Complete&&w.IsEpisodeCompleted(8),"All Scene08 parts scanned without tool enlargement/debug completion");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene08_Player_WithHUD.png");
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);
                Require(mission.SceneNumber==9&&mission.mover.Moving,"Real left Y continues completed Scene08");yield return GameSeconds(4f);Require(mission.SceneNumber==9&&mission.Ready,"Same-site transition 08→09 arrives through normal travel");
                yield return ToolAction("immune-mode");
                var wrongWaveTarget=w.Find("phagocyte");Aim(false,wrongWaveTarget.GetComponent<Collider>().bounds.center);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.3f);Trigger(false,false);yield return GameSeconds(.18f);
                Require(w.MarkedViruses==0&&mission.StepIndex==1,"Charging BioTool at the phagocyte cannot mark an unrelated incoming virion");
                yield return GameSeconds(.55f);
                Require(w.IncomingViruses.Count>=6,"Rapid immune assault starts with a six-target burst");
                for(int mark=0;mark<JourneyMission.AssaultVirusCount;mark++)yield return MarkIncomingVirus(mark);
                Require(mission.StepIndex==2&&w.MarkedViruses==JourneyMission.AssaultVirusCount,"Twenty-four individual incoming virions hit through actual rapid-fire trigger input");
                var phagocytePosition=w.phagocyte.position;
                yield return ToolAction("phagocyte");Require(mission.Complete&&w.IsEpisodeCompleted(9)&&mission.ViralLoad==0&&w.EngulfedViruses==JourneyMission.AssaultVirusCount,"Scene09 handoff completes only after all twenty-four actual phagocyte ingestions");
                Require(Vector3.Distance(phagocytePosition,w.phagocyte.position)<.01f,"Scene09 phagocyte stays in its current episode instead of moving to the future wound");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene09_Player_WithHUD.png");
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);
                Require(mission.SceneNumber==10&&mission.mover.Moving,"Real left Y continues completed Scene09");yield return GameSeconds(10f);Require(mission.SceneNumber==10&&mission.Ready,"Transition 09→10 arrives through normal travel");
                if(w.hemodynamics)yield return HemodynamicEpisode(true);
                else
                {
                yield return ToolAction("pressure-low");yield return ToolAction("tone-mode");
                Require(mission.StepIndex==2&&mission.PressureSystolic==92,"Scene10 pressure loss measured before opening vascular control");
                yield return AdjustToneWithHand();
                Require(mission.StepIndex==3&&mission.ModelRadius>=.75f&&mission.ModelRadius<=.88f&&mission.PressureSystolic==100,"Manual BioTool motion and settling produce moderate constriction and temporary stabilization");
                yield return ToolAction("pressure-stable");
                Aim(false,w.Find("pressure-return").GetComponent<Collider>().bounds.center);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.15f);Trigger(false,false);yield return GameSeconds(.12f);
                Require(mission.StepIndex==4&&mission.PressureSystolic>92.1f,"Early repeat measurement cannot complete the pressure-drop goal");
                yield return GameSeconds(3f);yield return ToolAction("pressure-return");Require(mission.Complete&&w.IsEpisodeCompleted(10)&&mission.PressureSystolic==92,"Repeated real measurement detects delayed pressure fall despite vascular tone");
                }
                var tone=w.VesselTone;var toneRoot=w.locationRoots[mission.mover.path.locationIds[10-3]];Quaternion toneHead=mission.mover.viewCamera.transform.localRotation,toneOrigin=origin.rotation;
                LookWithHead(mission.mover.path.Centre(mission.mover.path.Anchor(10)+4));yield return GameSeconds(.12f);ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene10_Manual_Tone_Complete_WithHUD.png");
                toneHead=mission.mover.viewCamera.transform.localRotation;
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);
                Require(mission.SceneNumber==11&&mission.mover.Moving,"Real left Y continues completed Scene10");yield return GameSeconds(10f);Require(mission.SceneNumber==11&&mission.Ready,"Transition 10→11 arrives through normal travel");
                var toneProperties=new MaterialPropertyBlock();w.toneWall[0].GetPropertyBlock(toneProperties);
                if(w.hemodynamics)Require(toneRoot.activeInHierarchy&&Mathf.Abs(w.VesselTone-tone)<.001f&&Mathf.Abs(Shader.GetGlobalFloat("_HemoRadius")-1)<.001f,"Late diagnosis preserves the intact local wall without an unnecessary radius intervention");
                else Require(toneRoot.activeInHierarchy&&Mathf.Abs(w.VesselTone-tone)<.001f&&Mathf.Abs(toneProperties.GetFloat("_Tone")-tone)<.001f,"Treated tone root and shader radius persist on entering wound diagnosis");
                Require(Quaternion.Angle(toneHead,mission.mover.viewCamera.transform.localRotation)<.01f&&Quaternion.Angle(toneOrigin,origin.rotation)<.01f&&input.scanner==scanner&&input.bioTool==tool,"10→11 preserves head/origin rotation and original instrument instances");
                var continuous=w.GetComponent<JourneyContinuousBloodstream>();float splitWait=Time.time;int previousLeaks=continuous.WoundLeakedTotal;
                while(continuous.WoundLeakedTotal==previousLeaks&&Time.time-splitWait<60)yield return GameSeconds(.2f);
                Require(w.woundState&&w.woundState.splitMainFlow&&w.woundState.LeakStrength>.95f&&continuous.WoundLeakedTotal>previousLeaks&&continuous.WoundContinuedTotal>continuous.WoundLeakedTotal,"Scene11 diverts existing primary-pool cells while most continue downstream");
                var woundRoot=w.locationRoots[mission.mover.path.locationIds[11-3]];var woundPosition=w.woundSite.position;
                yield return Scan("wound");yield return ToolAction("leak");yield return ToolAction("pressure-low");
                Require(mission.Complete&&w.IsEpisodeCompleted(11)&&mission.PressureSystolic==92,"Scene11 Scanner/measurement sequence identifies wound, leak and pressure loss");
                Require(woundRoot.activeInHierarchy&&Vector3.Distance(woundPosition,w.woundSite.position)<.001f&&w.woundState.LeakStrength>.95f,"Diagnosed wound remains in the same physical location and keeps its open leak until treated");
                LookWithHead(w.woundSite.position);yield return GameSeconds(.12f);ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene11_Wound_Diagnosis_WithHUD.png");
                var sameWound=w.woundSite;var sameLeakCells=w.woundState.Cells.ToArray();
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);yield return GameSeconds(4f);
                Require(mission.SceneNumber==12&&mission.Ready&&w.woundSite==sameWound&&w.woundState.Cells.SequenceEqual(sameLeakCells),"Scene12 continues same wound and leak instances from Scene11");
                var puzzle=w.hemostasisPuzzle;Require(puzzle&&puzzle.flowLayering&&puzzle.RequiredPlatelets==11&&puzzle.platelets.Length==14,"Eleven flow platelets and three balance reserves authored at existing wound");
                yield return PlaceWoundPiece(puzzle.platelets[0],puzzle.Slot(0)+puzzle.world.woundState.Outward*(-1.1f));
                Require(puzzle.Attached==0&&mission.StepIndex==0&&!puzzle.platelets[0].Placed,"Wrong physical platelet placement stays unaccepted and can be re-grabbed");
                yield return GameSeconds(.8f);
                for(int i=0;i<puzzle.RequiredPlatelets;i++)yield return CapturePlateletWithField(puzzle.platelets[i],puzzle.Slot(i));
                yield return GameSeconds(.5f);Require(puzzle.Attached==11&&mission.StepIndex==1&&!puzzle.Activated&&w.FibrinGrowth==0&&w.woundState.LeakStrength>.25f&&!mission.Complete,"Eleven automatically adherent platelets form only a loose primary plug with residual leakage");
                LookWithHead(w.woundSite.position);yield return GameSeconds(.12f);ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene12_HandPlaced_Platelet_Plug.png");
                Aim(false,w.woundSite.position);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.3f);Trigger(false,false);yield return GameSeconds(.15f);
                Require(!puzzle.Activated&&mission.StepIndex==1&&w.FibrinGrowth==0,"A single BioTool trigger cannot stabilize or heal the primary plug");
                var activation=puzzle.activationPuzzle;yield return PlaceBiochemicalCard(activation.tokens[3],activation.slots[0].position);
                Require(activation.Mistakes>0&&activation.Progress==0&&!puzzle.Activated,"Actual EDTA card is rejected: calcium chelation inhibits coagulation");
                for(int i=0;i<3;i++){yield return PlaceBiochemicalCard(activation.tokens[i],activation.slots[i].position);Require(activation.Progress==i+1,"Real XR grip/release solves stabilization stage "+(i+1));}
                yield return GameSeconds(.5f);Require(activation.Solved&&puzzle.Activated&&!mission.Complete&&w.FibrinGrowth>0&&w.woundState.LeakStrength>.08f,"Activation, aggregation and thrombin/fibrin solution starts gradual fibrin stabilization");
                yield return GameSeconds(7f);
                Require(mission.Complete&&w.IsEpisodeCompleted(12)&&puzzle.Connections==3&&puzzle.StabilizationComplete&&w.woundState.LeakStrength<.025f&&w.RepairGrowth==0,"Fibrin gradually stops bleeding; endothelial healing remains a later episode");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene12_Spatial_Fibrin_Complete.png");
                var placedPositions=puzzle.platelets.Take(puzzle.RequiredPlatelets).Select(p=>p.transform.position).ToArray();
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);yield return GameSeconds(4f);
                Require(mission.SceneNumber==13&&mission.Ready&&w.woundSite==sameWound&&puzzle.Attached==11&&puzzle.Connections==3,"Scene13 inherits same eleven-platelet plug and fibrin links without reset");
                Require(puzzle.platelets.Take(puzzle.RequiredPlatelets).Select((p,i)=>Vector3.Distance(p.transform.position,placedPositions[i])<.001f).All(v=>v),"Adherent platelet transforms persist across 12→13");yield return Scan("clot");
                for(int i=0;i<puzzle.RequiredPlatelets-2;i++)yield return PlaceWoundPiece(puzzle.platelets[i],puzzle.Slot(i)-w.woundState.Outward*1.2f);
                for(int i=0;i<3;i++)yield return PlaceWoundPiece(puzzle.strands[i],puzzle.starts[i].position);
                yield return GameSeconds(.9f);Require(mission.StepIndex==2&&mission.ClotBalance<.28f&&w.woundState.LeakStrength>.6f,"Removing real platelets/fibrin produces UNDER with renewed blood leak");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene13_UNDER_Hands.png");
                for(int i=0;i<puzzle.RequiredPlatelets-2;i++)yield return PlaceWoundPiece(puzzle.platelets[i],puzzle.Slot(i));
                for(int i=0;i<3;i++)yield return PlaceWoundPiece(puzzle.strands[i],puzzle.ends[i].position);
                for(int i=puzzle.RequiredPlatelets;i<puzzle.platelets.Length;i++)yield return PlaceWoundPiece(puzzle.platelets[i],puzzle.Slot(i));
                yield return GameSeconds(.9f);Require(mission.StepIndex==3&&mission.ClotBalance>.86f&&w.occlusion.activeInHierarchy&&mission.FlowMultiplier<.5f,"Adding reserve platelets by hand produces OVER obstruction and slowed flow");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene13_OVER_Hands.png");
                for(int i=puzzle.RequiredPlatelets;i<puzzle.platelets.Length;i++)yield return PlaceWoundPiece(puzzle.platelets[i],puzzle.Slot(i)-w.woundState.Outward*1.3f);
                yield return GameSeconds(.9f);Require(mission.StepIndex==4&&mission.ClotBalance>.5f&&mission.ClotBalance<.72f&&!w.occlusion.activeInHierarchy,"Physical correction returns to OPTIMAL with open lumen");yield return ToolAction("open-flow");
                Require(mission.Complete&&w.IsEpisodeCompleted(13),"Scene13 balance completed by actual piece removal/replacement without slider");
                var recoveryPlateletPositions=puzzle.platelets.Take(puzzle.RequiredPlatelets).Select(p=>p.transform.position).ToArray();
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);yield return GameSeconds(4f);
                Require(mission.SceneNumber==14&&mission.Ready&&w.woundSite==sameWound&&puzzle.Attached==11&&puzzle.Connections==3,"Scene14 inherits optimal same-wound plug and network without resetting them");
                Require(puzzle.platelets.Take(puzzle.RequiredPlatelets).Select((p,i)=>Vector3.Distance(p.transform.position,recoveryPlateletPositions[i])<.001f).All(v=>v),"Scene13 platelet positions are retained at recovery entry");
                for(int i=0;i<3;i++)yield return ScanOwnedDebris(w.recoveryDebris[i]);
                Require(mission.StepIndex==1&&w.ClearedDebris==0,"Scanner identifies three distinct debris without prematurely deleting them");yield return ToolAction("cleanup-mode");
                for(int i=0;i<3;i++)yield return CleanupOwnedDebris(w.recoveryDebris[i]);
                Require(w.ClearedDebris==3&&w.recoveryDebris.All(t=>!t.gameObject.activeInHierarchy),"Three selected debris physically engulfed before progressing to fibrinolysis");
                yield return Scan("plasmin");yield return ToolAction("plasmin");yield return GameSeconds(3.4f);
                Require(w.FibrinRemoval>.99f&&w.FibrinGrowth<.001f&&!w.fibrin.activeInHierarchy,"Player-activated plasmin progressively removes the existing fibrin network");
                yield return ToolAction("repair");
                Aim(true,w.Find("healed-wall").transform.position);yield return GameSeconds(.12f);Trigger(true,true);yield return GameSeconds(.9f);Trigger(true,false);yield return GameSeconds(.18f);
                Require(!mission.Complete&&w.RepairGrowth<1,"Early Scanner cannot claim fully restored endothelium during repair");
                yield return GameSeconds(3f);yield return Scan("healed-wall");
                Require(mission.Complete&&w.IsEpisodeCompleted(14)&&w.RepairGrowth>.99f&&w.woundSite==sameWound&&w.woundState.ActiveCellCount==0,"Actual recovery process closes and scans the same original wound");
                LookWithHead(w.woundSite.position);yield return GameSeconds(.12f);ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene14_Actual_Endothelial_Recovery.png");
                var recoveredWall=w.repairPatch;var recoveredRoot=w.locationRoots[mission.mover.path.locationIds[14-3]];Quaternion recoveryHead=mission.mover.viewCamera.transform.localRotation,recoveryOrigin=origin.rotation;
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);yield return GameSeconds(10f);
                Require(mission.SceneNumber==15&&mission.Ready&&w.woundSite==sameWound&&w.repairPatch==recoveredWall&&w.RepairGrowth>.99f,"Final-focus travel preserves repaired original wound and wall instance");
                Require(Quaternion.Angle(recoveryHead,mission.mover.viewCamera.transform.localRotation)<.01f&&Quaternion.Angle(recoveryOrigin,origin.rotation)<.01f&&input.scanner==scanner&&input.bioTool==tool,"14→15 preserves origin/gaze and original tools");
                LookWithHead(w.infected.position);yield return GameSeconds(.12f);yield return Scan("infected-cell");Require(mission.StepIndex==1,"Final exam starts by finding/scanning an actual infected cell before wave actions");
                var wrongFinal=w.Find("t-cell");Aim(false,wrongFinal.GetComponent<Collider>().bounds.center);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.3f);Trigger(false,false);yield return GameSeconds(.18f);Require(mission.Progress==0,"Wrong final biological target cannot substitute an incoming virion");
                for(int i=0;i<4;i++)yield return MarkFinalVirus(i);
                Require(mission.StepIndex==2&&w.FinalPulseHits>=2,"Final timed wave includes real shield pulses followed by four charged marks");yield return Scan("epitope");
                yield return PlaceAntibodyWithGrip(w.Find("antibody-B"),w.Find("epitope").transform.position);
                Require(mission.StepIndex==3&&!w.AntibodyBound,"Physically placed wrong antibody is rejected without advancing final exam");
                yield return PlaceAntibodyWithGrip(w.Find("antibody-A"),w.Find("epitope").transform.position);
                Require(mission.StepIndex==4&&w.AntibodyBound,"Complementary hand-placed antibody binds actual antigen site");yield return ToolAction("neutralized");yield return ToolAction("t-cell");yield return GameSeconds(3f);
                for(int i=0;i<3;i++)yield return CleanupResidual(w.residualTargets[i]);yield return GameSeconds(1.3f);
                Require(mission.Complete&&mission.Victory&&w.ResidualsCleared&&mission.ViralLoad==0&&!w.infected.gameObject.activeInHierarchy,"Actual final immune actions eliminate source and three residual complexes before victory");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene15_Actual_Immune_Victory.png");
                ls=ls.WithButton(ControllerButton.SecondaryButton,true);yield return GameSeconds(.12f);ls=ls.WithButton(ControllerButton.SecondaryButton,false);yield return GameSeconds(.2f);
                Require(mission.SceneNumber==16&&mission.Ready&&!mission.Complete&&w.HomeostasisChecks==0,"Homeostasis requires new real measurements instead of auto-completing on entry");
                foreach(string id in new[]{"check-pressure","check-temperature","check-flow"})yield return ToolAction(id);
                yield return Scan("check-wall");yield return Scan("check-virus");yield return ToolAction("check-hemostasis");
                Require(mission.Complete&&w.HomeostasisChecks==6&&mission.PressureSystolic==120&&mission.Temperature==36.8f&&mission.ViralLoad==0&&mission.Feedback.Contains("MISSION COMPLETE"),"Six actual final measurements produce mission result and grades");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene16_Actual_Homeostasis_Checks.png");
                yield return FarReentry(sameWound,recoveredWall);
            }
            finally
            {
                input.useExplicitAimSources=explicitAim;
                InputSystem.onBeforeUpdate-=QueueRuntimeStates;InputSystem.onAfterUpdate-=ObserveRuntimeState;
                settings.backgroundBehavior=background;
                settings.editorInputBehaviorInPlayMode=editorBehavior;
                if(left!=null)InputSystem.RemoveDevice(left);if(right!=null)InputSystem.RemoveDevice(right);if(head!=null)InputSystem.RemoveDevice(head);
                if(simulators!=null)for(int i=0;i<simulators.Length;i++)if(simulators[i])simulators[i].enabled=simulatorEnabled[i];
                running=false;
                WriteReport();
            }
        }
        static void QueueRuntimeStates()
        {
            // Editor updates have separate state buffers and can consume queued events
            // before the player sees them. Submit at the player's actual input update.
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic&&InputState.currentUpdateType!=InputUpdateType.Fixed)return;
            InputSystem.QueueStateEvent(left,ls);InputSystem.QueueStateEvent(right,rs);InputSystem.QueueStateEvent(head,hs);
        }
        static IEnumerator ControllerLifecycle()
        {
            var input=mission.GetComponent<JourneyInput>();var rig=input.GetComponent<JourneyInstrumentRig>();var modes=rig.modes;var camera=mission.mover.viewCamera.transform;var screens=mission.GetComponent<JourneyHolographicScreens>();int accepted=mission.AcceptedActions;
            var away=camera.position+camera.forward*4+camera.right*3;Aim(true,away);Aim(false,away);yield return GameSeconds(.15f);
            Require(input.enabled&&rig.ScannerGuideVisible&&rig.BioGuideVisible,"Both XR aiming guides persist while triggers are released");
            foreach(var mode in new[]{BioToolMode.Capture,BioToolMode.Tonus,BioToolMode.Bio,BioToolMode.Impulse})
            {
                int animations=rig.bioVR.ModeTransitions;var positions=rig.bioVR.prongs.Select(p=>p.localPosition).ToArray();var rotations=rig.bioVR.prongs.Select(p=>p.localRotation).ToArray();
                rs=rs.WithButton(ControllerButton.PrimaryButton,true);yield return GameSeconds(.20f);rs=rs.WithButton(ControllerButton.PrimaryButton,false);
                Require(modes.CurrentMode==mode&&rig.bioVR.ModeTransitions==animations+1&&rig.bioVR.ModeTransitionAmount>.1f,"Actual A switches mode with an explicit animation: "+mode);
                Require(rig.bioVR.prongs.Where((p,i)=>Vector3.Distance(p.localPosition,positions[i])>.001f||Quaternion.Angle(p.localRotation,rotations[i])>1).Any(),"Working crown changes pose during mode selection: "+mode);
                int activations=rig.bioVR.ActivationEvents;Trigger(false,true);yield return GameSeconds(.20f);
                Require(input.ToolHeld&&rig.bioVR.ActivationEvents==activations+1&&rig.bioVR.ActivationAmount>.1f,"Trigger animates selected mode even without an eligible target: "+mode);
                if(mode==BioToolMode.Tonus||mode==BioToolMode.Bio)Require(rig.fieldRing.enabled,"Area field remains visible on a miss: "+mode);
                Trigger(false,false);yield return GameSeconds(.45f);Require(rig.BioGuideVisible&&!input.ToolHeld,"BioTool aiming guide persists after release: "+mode);
            }
            Trigger(true,true);yield return GameSeconds(.2f);Require(input.ScannerHeld&&rig.ActiveScanner.ActivationAmount>.1f&&rig.ScannerBeamVisible,"Scanner trigger starts animation and beam even before target lock");Trigger(true,false);yield return GameSeconds(.2f);
            Aim(false,screens.theoryRoot.position);yield return GameSeconds(.2f);Require(input.enabled&&!input.ToolBlockedByUi,"Idle UI hover does not disable the input component");
            Trigger(false,true);yield return GameSeconds(.15f);Require(input.ToolBlockedByUi&&!input.ToolHeld&&input.enabled&&rig.BioGuideVisible,"UI consumes only its own pressed trigger while keeping input and guides alive");
            Aim(true,away);Trigger(true,true);yield return GameSeconds(.2f);Require(input.ScannerHeld&&!input.ScannerBlockedByUi,"Scanner hand continues working while BioTool hand interacts with UI");Trigger(true,false);
            Aim(false,away);yield return GameSeconds(.15f);Require(input.ToolBlockedByUi&&!input.ToolHeld,"Dragging the held UI trigger off-panel does not fire a biological action");
            Trigger(false,false);yield return GameSeconds(.15f);Trigger(false,true);yield return GameSeconds(.2f);Require(!input.ToolBlockedByUi&&input.ToolHeld,"Release and re-press restores tool immediately after UI interaction");Trigger(false,false);yield return GameSeconds(.2f);
            input.enabled=false;yield return GameSeconds(.15f);input.enabled=true;yield return GameSeconds(.15f);Trigger(false,true);yield return GameSeconds(.2f);Require(input.ToolHeld&&input.toolAction.action.enabled,"Input enable cycle restores live trigger polling without stale hold state");Trigger(false,false);yield return GameSeconds(.15f);
            Require(mission.AcceptedActions==accepted&&mission.SceneNumber==3&&mission.StepIndex==0,"Controller visual/UI checks never complete an unrelated biological goal");
            Directory.CreateDirectory(ArteryPolishReview.Reports+"/XRControllers");ArteryPolishReview.Capture(mission.mover.viewCamera,"XRControllers/Persistent_Aiming_Guides.png");
            Require(true,"Controller lifecycle input regression complete");
        }
        static IEnumerator ObserveBloodTravel(float seconds)
        {
            var stream=mission.world.GetComponent<JourneyContinuousBloodstream>();var camera=mission.mover.viewCamera;var planes=new Plane[6];var previous=new Dictionary<Transform,Vector3>();float began=Time.time,maxDelta=0;int samples=0;
            while(Time.time-began<seconds)
            {
                GeometryUtility.CalculateFrustumPlanes(camera,planes);var visible=new HashSet<Transform>();
                foreach(var cell in stream.MainCellsBetween(mission.mover.Distance-35,mission.mover.Distance+35))
                {
                    if(!GeometryUtility.TestPlanesAABB(planes,cell.GetComponent<Renderer>().bounds))continue;visible.Add(cell);
                    if(previous.TryGetValue(cell,out var point)){float delta=Vector3.Distance(cell.position,point);maxDelta=Mathf.Max(maxDelta,delta);samples++;if(delta>Mathf.Max(.25f,Time.deltaTime*3))Require(false,"Visible blood cell jumped during travel: "+delta);}
                    previous[cell]=cell.position;
                }
                foreach(var cell in previous.Keys.Where(c=>!visible.Contains(c)).ToArray())previous.Remove(cell);
                yield return null;
            }
            notes.Add("Travel blood samples="+samples+" maximum consecutive visible displacement="+maxDelta);
            Require(samples>50,"Moving visible blood remains continuous across the actual scene transition");
        }
        static void ObserveRuntimeState()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic&&InputState.currentUpdateType!=InputUpdateType.Fixed)return;
            runtimeGrip=right.grip.ReadValue();runtimeGripButton=right.gripButton.isPressed;
        }
        static IEnumerator Scan(string id)
        {
            int before=mission.StepIndex;Require(mission.Current!=null&&mission.Current.action==StudyAction.Scan&&mission.Current.target==id,"Scanner objective expected for "+id);
            var moving=mission.world.Find(id).GetComponent<JourneyFlowPickup>();bool caught=false;
            if(mission.SceneNumber==3&&moving&&!moving.Held)
            {
                var item=mission.world.Find(id);Pose(false,item.GetComponent<Collider>().bounds.center,Quaternion.identity);yield return GameSeconds(.12f);Grip(false,true);yield return GameSeconds(.25f);
                Require(item.grabbed,"Actual XR grip extracts "+id+" from blood before scanning");caught=true;
            }
            if(moving&&moving.InStudyFlow&&!moving.Held){yield return TrackScan(id);yield break;}
            var target=mission.world.Find(id);var contact=id=="plaque"&&mission.world.plaqueChallenge?mission.world.plaqueChallenge.diagnosisContact:target.GetComponentsInChildren<Collider>().First(c=>c.enabled);
            notes.Add("Contact "+id+": "+contact.name+" / "+contact.GetType().Name+" layerInspector="+(mission.world.layerInspection!=null));
            if(mission.SceneNumber==4)Require(contact is MeshCollider&&contact.name.StartsWith("Scene04_Band_"),"Scanner uses actual wall shell contact for "+id);
            Aim(true,contact.bounds.center);yield return GameSeconds(.12f);Trigger(true,true);yield return GameSeconds(1.05f);
            var input=mission.GetComponent<JourneyInput>();notes.Add("Scan "+id+": step="+mission.StepIndex+" enabled="+input.isActiveAndEnabled+" lock="+(input.LockedTarget?input.LockedTarget.targetId:"none")+" progress="+input.ScanProgress+" blocker="+(input.AimObstruction?input.AimObstruction.name:"none")+" aim="+input.scanner.position+" target="+target.transform.position+" contact="+contact.bounds.center);
            Trigger(true,false);yield return GameSeconds(.18f);Require(mission.StepIndex==before+1,"Scanner completed "+id);
            if(caught){Grip(false,false);yield return GameSeconds(.8f);Require(!moving.Held&&!moving.Rejoining,"Extracted "+id+" returns smoothly into moving blood pool");}
        }
        static IEnumerator ToolAction(string id)
        {
            int before=mission.StepIndex;Require(mission.Current!=null&&mission.Current.target==id,"BioTool objective expected for "+id);
            var target=mission.world.Find(id);Require(target,"BioTool target exists: "+id);
            var contact=target.GetComponentsInChildren<Collider>().FirstOrDefault(c=>c.enabled);Require(contact,"BioTool target contact enabled: "+id);
            Aim(false,contact.bounds.center);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.30f);Trigger(false,false);yield return GameSeconds(.18f);
            if(id=="phagocyte"&&mission.SceneNumber==9)yield return GameSeconds(1.2f);
            if(id=="neutralized"&&mission.SceneNumber==15)yield return GameSeconds(1f);
            var input=mission.GetComponent<JourneyInput>();notes.Add("Tool "+id+": locked="+(input.LockedTarget?input.LockedTarget.targetId:"none")+" charge="+input.ChargeProgress+" radius="+mission.ModelRadius+" pressure="+mission.ModelPressure+" aim="+input.bioTool.position+" contact="+contact.bounds.center);
            Require(mission.StepIndex==before+1,"BioTool completed "+id);
        }
        static IEnumerator TrackScan(string id)
        {
            int before=mission.StepIndex;var target=mission.world.Find(id);Require(mission.Current!=null&&mission.Current.action==StudyAction.Scan&&mission.Current.target==id,"Moving scanner objective expected for "+id);
            var contact=target.GetComponentsInChildren<Collider>().First(c=>c.enabled);var input=mission.GetComponent<JourneyInput>();Trigger(true,true);
            float start=Time.time;double deadline=EditorApplication.timeSinceStartup+12;
            while(Time.time-start<1.05f)
            {
                Aim(true,contact.bounds.center);
                if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Player loop did not advance for moving scan");
                yield return null;
            }
            Trigger(true,false);yield return GameSeconds(.18f);notes.Add("Tracked scan "+id+": locked="+(input.LockedTarget?input.LockedTarget.targetId:"none")+" progress="+input.ScanProgress);
            Require(mission.StepIndex==before+1,"Scanner completed moving target "+id);
        }
        static IEnumerator TrackToolCapture(string id)
        {
            int before=mission.StepIndex;var target=mission.world.Find(id);var contact=target.GetComponentsInChildren<Collider>().First(c=>c.enabled);var input=mission.GetComponent<JourneyInput>();
            Require(mission.Current!=null&&mission.Current.action==StudyAction.Capture&&mission.Current.target==id,"Moving BioTool capture objective expected for "+id);
            Aim(false,contact.bounds.center);yield return GameSeconds(.12f);Trigger(false,true);float start=Time.time;double deadline=EditorApplication.timeSinceStartup+12;
            while(mission.StepIndex==before&&Time.time-start<2f)
            {
                Aim(false,contact.bounds.center);if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Player loop did not advance for moving BioTool capture");yield return null;
            }
            notes.Add("Tracked capture "+id+": locked="+(input.LockedTarget?input.LockedTarget.targetId:"none")+" charge="+input.ChargeProgress+" blocker="+(input.AimObstruction?input.AimObstruction.name:"none")+" input enabled="+input.isActiveAndEnabled+" aim="+input.bioTool.position+" target="+contact.bounds.center);
            Require(mission.StepIndex==before+1,"BioTool captured moving target "+id);
        }
        static IEnumerator CarryEmbolusToTrap()
        {
            var w=mission.world;var input=mission.GetComponent<JourneyInput>();
            Vector3 start=input.bioTool.position+input.bioTool.forward*JourneyWorld.EmbolusHoldDistance;
            Quaternion rotation=Quaternion.LookRotation(w.trap.position-mission.mover.viewCamera.transform.position,mission.mover.path.Up(mission.mover.Distance));
            Vector3 aimOffset=Vector3.Scale(rc.InverseTransformPoint(input.bioTool.position),rc.lossyScale);
            float began=Time.time;double deadline=EditorApplication.timeSinceStartup+16;
            int before=mission.StepIndex;
            while(mission.StepIndex==before&&Time.time-began<7f)
            {
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/1.25f));
                Vector3 fieldPoint=Vector3.Lerp(start,w.trap.position,t);
                Vector3 controllerPosition=fieldPoint-rotation*Vector3.forward*JourneyWorld.EmbolusHoldDistance-rotation*aimOffset;
                Pose(false,controllerPosition,rotation);
                if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Player loop did not advance for held-field transport");
                yield return null;
            }
            notes.Add("Transport result: bubble="+w.bubble.position+" trap="+w.trap.position+" stability="+w.EmbolusStability);
            Require(mission.StepIndex==before+1&&w.EmbolusStability>=1,"Only sustained in-trap placement completes geometric deposit");
        }
        static IEnumerator PulseZone(int index)
        {
            var zone=mission.world.plaqueState.Zones[index];var contact=zone.Contact;Require(contact&&contact.enabled,"Lipid zone contact enabled: "+index);
            var input=mission.GetComponent<JourneyInput>();Aim(false,contact.ClosestPoint(input.bioTool.position));yield return GameSeconds(.12f);
            var activeTools=mission.GetComponent<JourneyInstrumentRig>();
            if(activeTools&&index==0){LookWithHead(contact.bounds.center);yield return GameSeconds(.12f);Aim(false,contact.ClosestPoint(input.bioTool.position));yield return GameSeconds(.12f);}
            float spin=activeTools?activeTools.ActiveBioTool.RotationDegrees:0;int shots=activeTools?activeTools.ImpulseBursts:0;
            var crownPositions=activeTools?activeTools.ActiveBioTool.prongs.Select(p=>p.localPosition).ToArray():null;
            Trigger(false,true);yield return GameSeconds(.30f);
            if(activeTools&&index==0)
            {
                Require(activeTools.modes.CurrentMode==BioToolMode.Impulse&&activeTools.ActiveBioTool.RotationDegrees>spin+10,"IMPULSE rotates original four GLB BioTool prongs during actual charge");
                Require(activeTools.ActiveBioTool.prongs.Where((p,i)=>Vector3.Distance(p.localPosition,crownPositions[i])>.005f).Count()==4,"All four BioTool prong pivots orbit the common device axis, rather than spinning in place");
                Require(activeTools.ImpulseBursts>shots&&activeTools.PulseVisible&&!activeTools.CaptureBeamVisible,"Accepted actual precision pulse produces a travelling impulse ring, not a capture tether");
                Directory.CreateDirectory(ArteryPolishReview.Reports+"/ActiveInstruments");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"ActiveInstruments/02_Impulse_Actual_XR_Input.png");
            }
            Trigger(false,false);yield return GameSeconds(.18f);
            notes.Add("Pulse zone "+index+": locked="+(input.LockedTarget?input.LockedTarget.targetId:"none")+" selected="+mission.world.plaqueState.SelectedZoneIndex+" progress="+mission.Progress+" step="+mission.StepIndex+" contact="+contact.bounds.center+" aim="+input.bioTool.position);
            Require(mission.Progress==index+1||mission.StepIndex==4,"BioTool processed lipid zone "+index);
        }
        static IEnumerator PulseGrowingMass()
        {
            var challenge=mission.world.plaqueChallenge;LookWithHead(challenge.site.position);yield return GameSeconds(.15f);
            int before=challenge.DestroyedCount;Trigger(false,true);float started=Time.time;
            while(!challenge.Cleared&&Time.time-started<35)
            {
                var cell=challenge.lipids.Where(p=>p.contact.enabled).OrderBy(p=>(p.contact.bounds.center-mission.mover.viewCamera.transform.position).sqrMagnitude).FirstOrDefault();
                if(cell)Aim(false,cell.contact.bounds.center);yield return GameSeconds(.04f);
            }
            Trigger(false,false);yield return GameSeconds(.3f);
            notes.Add("Plaque XR diagnostic: before="+before+" destroyed="+challenge.DestroyedCount+" accepted="+challenge.AcceptedHits+" elapsed="+challenge.Elapsed+" lock="+(mission.GetComponent<JourneyInput>().LockedTarget?mission.GetComponent<JourneyInput>().LockedTarget.name:"none")+" obstruction="+(mission.GetComponent<JourneyInput>().AimObstruction?mission.GetComponent<JourneyInput>().AimObstruction.name:"none"));
            Require(challenge.Cleared&&challenge.DestroyedCount==challenge.SpawnedCount&&challenge.AcceptedHits>=challenge.SpawnedCount,"Actual right trigger fragments the original pile plus newly arriving lipids before occlusion");
            Directory.CreateDirectory(ArteryPolishReview.Reports+"/PlaqueChallenge");ArteryPolishReview.Capture(mission.mover.viewCamera,"PlaqueChallenge/05_Actual_XR_Cleared_Lumen.png");
        }
        static IEnumerator MarkIncomingVirus(int index)
        {
            double deadline=EditorApplication.timeSinceStartup+8;
            while(mission.world.IncomingViruses.Count==0)
            {if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("No incoming virion spawned for real-input wave check");yield return null;}
            var virus=mission.world.IncomingViruses[0];var contact=virus.GetComponent<Collider>();
            Require(contact&&contact.enabled,"Incoming virion has active target contact: "+index);
            Aim(false,contact.bounds.center);yield return GameSeconds(.12f);Trigger(false,true);
            float began=Time.time;int before=mission.world.MarkedViruses;
            while(virus&&mission.world.MarkedViruses==before&&Time.time-began<1.5f)
            {if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Player loop did not advance while charging at incoming virion");Aim(false,contact.bounds.center);yield return null;}
            Trigger(false,false);yield return GameSeconds(.18f);
            Require(mission.world.MarkedViruses==before+1,"Rapid BioTool hit marks the aimed moving virion: "+index);
        }
        static IEnumerator AdjustToneWithHand()
        {
            var input=mission.GetComponent<JourneyInput>();var control=mission.world.Find("tone");var contact=control.GetComponent<Collider>();
            Aim(false,contact.bounds.center);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.3f);Trigger(false,false);yield return GameSeconds(.15f);
            Require(mission.StepIndex==2&&Mathf.Abs(mission.ModelRadius-1)<.01f,"Holding/clicking tone without hand movement cannot auto-set radius");
            Aim(false,contact.bounds.center);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.12f);Require(input.AdjustingTone,"Tone control locks to a correctly aimed held trigger");
            Vector3 start=rc.position;Quaternion rotation=rc.rotation;var up=mission.mover.path.Up(mission.mover.Distance);
            float began=Time.time;double deadline=EditorApplication.timeSinceStartup+10;
            while(Time.time-began<.5f)
            {Pose(false,start-up*(.5f*Mathf.Clamp01((Time.time-began)/.5f)),rotation);if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Tone drag player loop stalled");yield return null;}
            yield return GameSeconds(.2f);Require(mission.StepIndex==2&&mission.ModelRadius<.68f&&input.ToneStability==0,"Excessive manual constriction warns but cannot complete moderate-tone goal");
            began=Time.time;Vector3 unsafePosition=rc.position;Vector3 safePosition=start-up*.22f;
            while(Time.time-began<.55f)
            {Pose(false,Vector3.Lerp(unsafePosition,safePosition,Mathf.Clamp01((Time.time-began)/.55f)),rotation);yield return null;}
            yield return GameSeconds(.3f);Require(mission.StepIndex==2,"Passing into safe tone range still requires a sustained steady hold");
            LookWithHead(contact.bounds.center);yield return GameSeconds(.12f);ArteryPolishReview.Capture(mission.mover.viewCamera,"V7Input/Scene10_Held_Manual_Tone_WithHUD.png");
            yield return GameSeconds(.8f);Trigger(false,false);yield return GameSeconds(.12f);
            Require(mission.StepIndex==3,"Correcting hand position and settling finishes tone through InputActions");
        }
        static IEnumerator HemodynamicEpisode(bool late)
        {
            var experiment=mission.world.hemodynamics;LookWithHead(experiment.toneContact.transform.position);yield return GameSeconds(.2f);
            Directory.CreateDirectory(HemodynamicsExperimentBuilder.Reports);
            if(!late)
            {
                Require(experiment.display.compactTheory.activeInHierarchy&&!mission.world.flowModel.gameObject.activeInHierarchy,"Early experiment uses a compact task and AR on the vessel instead of a separate flow hologram");
                foreach(var id in new[]{"hdx-p1","hdx-p2","hdx-speed","hdx-diameter"})yield return Scan(id);
                Require(experiment.Case==1&&experiment.ResistanceRatio>3&&experiment.VelocityRatio>1.7f,"Four real Scanner measurements reveal fixed-Q narrowing and its pressure/velocity consequences");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"HemodynamicsExperiment/05_XR_Narrow_Experiment.png");
                yield return AdjustHemodynamicHand();yield return Scan("hdx-check1");
                Require(experiment.Case==2&&experiment.VelocityRatio<.7f&&experiment.FormulaDiscovered,"Second experiment starts wide; discovered resistance relation is shown locally");
                yield return AdjustHemodynamicHand();yield return Scan("hdx-check2");
                Require(experiment.Case==3&&experiment.RadiusRatio==1&&experiment.P1==100&&experiment.P2==95,"Normal diameter with both pressures lower requires an outside-cause hypothesis");
                foreach(var id in new[]{"hdx-diameter","hdx-speed","hdx-p1","hdx-p2"})yield return Scan(id);
            }
            else
            {
                Require(experiment.Case==3&&mission.PressureSystolic==92&&mission.Goals.Count==7,"Later low-pressure episode requires independent diagnosis instead of repeated tone training");
                foreach(var id in new[]{"hdx-diameter","hdx-speed","hdx-p1","hdx-p2","hdx-wall"})yield return Scan(id);
            }
            int wrong=experiment.Mistakes;yield return PlaceHemodynamicCard(experiment.cards.First(c=>c.code==0),experiment.cardSlot.position);Require(experiment.Mistakes>wrong&&mission.Current?.action==StudyAction.Bind,"Actual wrong physical hypothesis/action is rejected by the collected measurements");
            yield return PlaceHemodynamicCard(experiment.cards.First(c=>c.code==2),experiment.cardSlot.position);Require(mission.Current?.action==StudyAction.Scan&&experiment.RadiusRatio==1,"Evidence-based physical card resolves diagnosis without changing the normal vessel radius");
            yield return Scan(late?"hdx-forward":"hdx-final");Require(mission.Complete&&mission.world.IsEpisodeCompleted(late?10:5),late?"Late pressure diagnosis completes through Scanner/XRI and leads to further investigation":"All three early experiments complete through real Scanner, held TONUS and physical card inputs");
            ArteryPolishReview.Capture(mission.mover.viewCamera,late?"HemodynamicsExperiment/06_XR_Late_Diagnosis.png":"HemodynamicsExperiment/07_XR_Experiment_Complete.png");
        }
        static IEnumerator AdjustHemodynamicHand()
        {
            var experiment=mission.world.hemodynamics;var contact=experiment.toneContact.GetComponent<Collider>();int step=mission.StepIndex;float radius=experiment.RadiusRatio;
            Aim(false,contact.bounds.center);yield return GameSeconds(.15f);Trigger(false,true);yield return GameSeconds(.35f);
            Require(experiment.Adjusting&&mission.StepIndex==step&&experiment.Stability==0,"A stationary held XR TONUS trigger cannot solve the experiment");
            Vector3 from=rc.position;Quaternion rotation=rc.rotation;var up=mission.mover.path.Up(experiment.SectionArc);float began=Time.time;
            while(Time.time-began<.65f){Pose(false,from+up*((1-radius)/.85f)*Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/.65f)),rotation);yield return null;}
            yield return GameSeconds(.2f);Require(mission.StepIndex==step,"Correcting radius by hand still requires a stable hold");yield return GameSeconds(1f);Trigger(false,false);yield return GameSeconds(.15f);
            Require(mission.StepIndex==step+1&&experiment.RadiusRatio>=.95f&&experiment.RadiusRatio<=1.05f,"Real XR hand displacement and settling restore normal experimental diameter");
        }
        static IEnumerator PlaceHemodynamicCard(JourneyHemodynamicsCard card,Vector3 destination)
        {
            Pose(false,card.transform.position,Quaternion.identity);yield return GameSeconds(.2f);Grip(false,true);yield return GameSeconds(.25f);Require(card.grab.isSelected,"Actual XR grip selects a hemodynamic hypothesis card: "+card.code);
            Vector3 from=rc.position,offset=card.transform.position-rc.position;float began=Time.time;
            while(Time.time-began<.7f){Pose(false,Vector3.Lerp(from,destination-offset,Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/.7f))),Quaternion.identity);yield return null;}
            yield return GameSeconds(.12f);Grip(false,false);yield return GameSeconds(.35f);
        }
        static IEnumerator PlaceWoundPiece(JourneyHemostasisPiece piece,Vector3 destination)
        {
            Pose(false,piece.transform.position,Quaternion.identity);yield return GameSeconds(.2f);Grip(false,true);yield return GameSeconds(.25f);
            Require(piece.Held,"Real right grip selects "+piece.name);
            Vector3 start=rc.position;var offset=piece.transform.position-rc.position;float began=Time.time;
            double deadline=EditorApplication.timeSinceStartup+10;
            while(Time.time-began<.7f)
            {
                Pose(false,Vector3.Lerp(start,destination-offset,Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/.7f))),Quaternion.identity);
                if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Player loop stalled during spatial hand placement");yield return null;
            }
            yield return GameSeconds(.12f);Grip(false,false);yield return GameSeconds(.35f);
            notes.Add("Released "+piece.name+" position="+piece.transform.position+" intended="+destination+" accepted="+piece.Placed);
        }
        static IEnumerator CapturePlateletWithField(JourneyHemostasisPiece piece,Vector3 destination)
        {
            var input=mission.GetComponent<JourneyInput>();var field=input.plateletCapture;var camera=mission.mover.viewCamera.transform;
            if(Vector3.Distance(piece.transform.position,camera.position)>9)
            {
                LookWithHead(camera.position+mission.mover.path.Up(mission.mover.Distance)*4);yield return GameSeconds(.2f);float waiting=Time.time;
                while(Vector3.Distance(piece.transform.position,camera.position)>9&&Time.time-waiting<15)yield return GameSeconds(.1f);
            }
            LookWithHead(piece.transform.position);yield return GameSeconds(.12f);Aim(false,piece.transform.position);yield return GameSeconds(.12f);Trigger(false,true);float began=Time.time;
            while(!field.Holding&&Time.time-began<3){Aim(false,piece.transform.position);yield return null;}
            Require(field.Holding&&field.HeldPiece==piece,"Held real right Trigger catches flow platelet "+piece.index);
            Vector3 start=input.bioTool.position+input.bioTool.forward*field.FieldDistance;
            Quaternion rotation=Quaternion.LookRotation(destination-camera.position,mission.mover.path.Up(mission.mover.Distance));
            Vector3 offset=Vector3.Scale(rc.InverseTransformPoint(input.bioTool.position),rc.lossyScale);began=Time.time;
            var localRotation=Quaternion.Inverse(rc.rotation)*input.bioTool.rotation;var controllerRotation=rotation*Quaternion.Inverse(localRotation);
            while(!piece.Placed&&Time.time-began<6)
            {
                Vector3 point=Vector3.Lerp(start,destination,Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/1.25f)));
                Pose(false,point-rotation*Vector3.forward*field.FieldDistance-controllerRotation*offset,controllerRotation);yield return null;
            }
            Trigger(false,false);yield return GameSeconds(.2f);
            notes.Add("Platelet transport diagnostic: index="+piece.index+" position="+piece.transform.position+" destination="+destination+" distance="+Vector3.Distance(piece.transform.position,destination)+" holding="+field.Holding+" blocked="+input.ToolBlockedByUi+" mode="+input.GetComponent<BioToolModeController>().CurrentMode);
            Require(piece.Placed&&!field.Holding&&piece.GetComponent<MeshFilter>().sharedMesh==piece.puzzle.activated,"BioTool approach causes automatic adhesion and shape change without release placement: "+piece.index);
        }
        static IEnumerator PlaceBiochemicalCard(JourneyPlateletActivationToken token,Vector3 destination)
        {
            Pose(false,token.transform.position,Quaternion.identity);yield return GameSeconds(.2f);Grip(false,true);yield return GameSeconds(.25f);Require(token.grab.isSelected,"Actual XR grip selects biochemical card "+token.code);
            Vector3 from=rc.position,offset=token.transform.position-rc.position;float began=Time.time;
            while(Time.time-began<.7f){Pose(false,Vector3.Lerp(from,destination-offset,Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/.7f))),Quaternion.identity);yield return null;}
            yield return GameSeconds(.12f);Grip(false,false);yield return GameSeconds(.35f);
        }
        static IEnumerator ScanOwnedDebris(JourneyTarget target)
        {
            int before=mission.Progress;int step=mission.StepIndex;
            Aim(true,target.GetComponent<Collider>().bounds.center);yield return GameSeconds(.12f);Trigger(true,true);yield return GameSeconds(1.05f);Trigger(true,false);yield return GameSeconds(.18f);
            Require(mission.Progress==before+1||mission.StepIndex==step+1,"Scanner identifies actual debris "+target.name);
        }
        static IEnumerator CleanupOwnedDebris(JourneyTarget target)
        {
            int before=mission.world.ClearedDebris;
            Aim(false,target.GetComponent<Collider>().bounds.center);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.15f);Trigger(false,false);yield return GameSeconds(1.15f);
            Require(mission.world.ClearedDebris==before+1&&!target.gameObject.activeInHierarchy,"BioTool assists actual phagocytosis of "+target.name);
        }
        static IEnumerator MarkFinalVirus(int index)
        {
            double deadline=EditorApplication.timeSinceStartup+12;
            while(mission.world.IncomingViruses.Count==0){if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Final wave did not spawn");yield return null;}
            var virus=mission.world.IncomingViruses[0];var contact=virus.GetComponent<Collider>();var timing=virus.GetComponent<JourneyViralTiming>();int before=mission.Progress;
            while(virus&&mission.Progress==before&&mission.StepIndex==1)
            {
                Aim(false,contact.bounds.center);
                if(!timing.WindowOpen){yield return null;continue;}
                Trigger(false,true);float began=Time.time;
                while(virus&&Time.time-began<.3f){Aim(false,contact.bounds.center);yield return null;}
                Trigger(false,false);yield return GameSeconds(.12f);
                if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Final timed pulse/mark window not completed");
            }
            Require(mission.Progress==before+1||mission.StepIndex==2,"Real pulse/mark in timing window handles final virion "+index);
        }
        static IEnumerator PlaceAntibodyWithGrip(JourneyTarget target,Vector3 point)
        {
            var grab=target.GetComponent<XRGrabInteractable>();Pose(false,target.transform.position,Quaternion.identity);yield return GameSeconds(.2f);Grip(false,true);yield return GameSeconds(.25f);Require(grab&&grab.isSelected,"Real grip selects "+target.targetId);
            var from=rc.position;var offset=target.transform.position-rc.position;float start=Time.time;
            while(Time.time-start<.65f){Pose(false,Vector3.Lerp(from,point-offset,Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-start)/.65f))),Quaternion.identity);yield return null;}
            yield return GameSeconds(.12f);Grip(false,false);yield return GameSeconds(.4f);
        }
        static IEnumerator CleanupResidual(JourneyTarget target)
        {
            int before=mission.Progress;int step=mission.StepIndex;
            Aim(false,target.GetComponent<Collider>().bounds.center);yield return GameSeconds(.12f);Trigger(false,true);yield return GameSeconds(.15f);Trigger(false,false);yield return GameSeconds(1.15f);
            Require(!target.gameObject.activeInHierarchy&&(mission.Progress==before+1||mission.StepIndex==step+1),"Actual selected residual complex cleared: "+target.name);
        }
        static void TrackedHeadPosition(Vector3 worldPoint)
        {
            var camera=mission.mover.viewCamera.transform;var local=camera.parent.InverseTransformPoint(worldPoint);
            hs.devicePosition=hs.centerEyePosition=hs.leftEyePosition=hs.rightEyePosition=local;
        }
        static IEnumerator FarReentry(Transform wound,GameObject recoveredWall)
        {
            var w=mission.world;var path=mission.mover.path;var origin=mission.mover.origin.transform;
            var cap=w.plaqueChallenge?w.plaqueChallenge.repairResidue.GetComponent<MeshFilter>():w.locationRoots[path.locationIds[6-3]].GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="Plaque_Cap");var mesh=cap.sharedMesh;var vertices=mesh.vertices;var position=cap.transform.position;
            var savedHead=hs;Quaternion originRotation=origin.rotation;
            TrackedHeadPosition(path.Centre(path.Length-3));yield return GameSeconds(.25f);
            Require(w.LocationRenderingCulled(path.locationIds[6-3])&&!cap.GetComponent<Renderer>().enabled,"VERY FAR tracked-device excursion hides old plaque renderers");
            Require(w.LocationRenderingCulled(path.locationIds[11-3]),"VERY FAR tracked-device excursion culls recovered wound location");
            TrackedHeadPosition(path.Centre(path.Anchor(6)+3));yield return GameSeconds(.25f);
            Require(!w.LocationRenderingCulled(path.locationIds[6-3])&&cap.GetComponent<Renderer>().enabled&&(w.plaqueChallenge?w.plaqueChallenge.Cleared:w.plaqueState.ProcessedCount==3)&&cap.sharedMesh==mesh&&mesh.vertices.SequenceEqual(vertices)&&Vector3.Distance(position,cap.transform.position)<.001f,"Plaque re-entry restores exact treated mesh, pose and treatment state");
            TrackedHeadPosition(path.Centre(path.Anchor(11)+3));yield return GameSeconds(.25f);
            Require(!w.LocationRenderingCulled(path.locationIds[11-3])&&w.woundSite==wound&&w.repairPatch==recoveredWall&&w.RepairGrowth>.99f&&w.woundState.ActiveCellCount==0,"Recovered wound re-entry retains repaired wall instance/state and no leak");
            Require(Quaternion.Angle(originRotation,origin.rotation)<.01f,"VERY FAR excursions do not reset XR Origin heading");
            hs=savedHead;yield return GameSeconds(.2f);
            notes.Add("VERY FAR method: tracked HMD state excursions along existing path after final measurements; no scene reset, no mission API success. This does not verify physical-headset comfort or joystick navigation.");WriteReport();
        }
        static IEnumerator GameSeconds(float seconds)
        {
            float start=Time.time;double deadline=EditorApplication.timeSinceStartup+12;
            while(Time.time-start<seconds)
            {
                if(!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Player loop did not advance for input hold");
                yield return null;
            }
        }
        static void Pose(bool isLeft,Vector3 worldPosition,Quaternion worldRotation)
        {
            Transform controller=isLeft?lc:rc;var state=isLeft?ls:rs;state.devicePosition=controller.parent.InverseTransformPoint(worldPosition);state.deviceRotation=Quaternion.Inverse(controller.parent.rotation)*worldRotation;
            if(isLeft){ls=state;InputSystem.QueueStateEvent(left,ls);}else{rs=state;InputSystem.QueueStateEvent(right,rs);}
        }
        static void Aim(bool isLeft,Vector3 point)
        {
            var camera=mission.mover.viewCamera.transform;var position=camera.position+camera.right*(isLeft?-.25f:.25f)-camera.up*.18f+camera.forward*.45f;
            // The QA actor chooses an unobstructed hand pose rather than aiming
            // through the visible theory panel at a biological target behind it.
            var guard=mission.GetComponent<JourneyHud>().inputGuard;
            foreach(float lateral in new[]{isLeft?-.25f:.25f,.85f,-.85f,1.2f})
            {
                position=camera.position+camera.right*lateral-camera.up*.18f+camera.forward*.45f;bool blocked=false;
                if(guard&&guard.surfaces!=null)foreach(var surface in guard.surfaces)
                {
                    if(!surface||!surface.gameObject.activeInHierarchy)continue;var canvas=surface.GetComponentInParent<Canvas>();if(canvas&&!canvas.enabled)continue;
                    var ray=new Ray(position,(point-position).normalized);var plane=new Plane(surface.forward,surface.position);
                    if(plane.Raycast(ray,out float distance)&&distance>0&&distance<Vector3.Distance(position,point)-.1f)
                    {var local=surface.InverseTransformPoint(ray.GetPoint(distance));if(surface.rect.Contains(new Vector2(local.x,local.y)))blocked=true;}
                }
                if(!blocked)break;
            }
            var input=mission.GetComponent<JourneyInput>();var aim=isLeft?input.scanner:input.bioTool;var controller=isLeft?lc:rc;
            var rotation=Quaternion.LookRotation(point-position,camera.up);var localRotation=Quaternion.Inverse(controller.rotation)*aim.rotation;
            var offset=Quaternion.Inverse(controller.rotation)*(aim.position-controller.position);var controllerRotation=rotation*Quaternion.Inverse(localRotation);
            Pose(isLeft,position-controllerRotation*offset,controllerRotation);
        }
        static void LookWithHead(Vector3 point)
        {
            var camera=mission.mover.viewCamera.transform;
            Quaternion rotation=Quaternion.Inverse(camera.parent.rotation)*Quaternion.LookRotation(point-camera.position,mission.mover.path.Up(mission.mover.Distance));
            // QA actor input, never a forced camera/origin transform in gameplay.
            hs.deviceRotation=hs.centerEyeRotation=hs.leftEyeRotation=hs.rightEyeRotation=rotation;
        }
        static void Grip(bool isLeft,bool held)
        {if(isLeft){ls.grip=held?1:0;ls=ls.WithButton(ControllerButton.GripButton,held);InputSystem.QueueStateEvent(left,ls);}else{rs.grip=held?1:0;rs=rs.WithButton(ControllerButton.GripButton,held);InputSystem.QueueStateEvent(right,rs);}}
        static void Trigger(bool isLeft,bool held)
        {if(isLeft){ls.trigger=held?1:0;ls=ls.WithButton(ControllerButton.TriggerButton,held);InputSystem.QueueStateEvent(left,ls);}else{rs.trigger=held?1:0;rs=rs.WithButton(ControllerButton.TriggerButton,held);InputSystem.QueueStateEvent(right,rs);}}
    }
}

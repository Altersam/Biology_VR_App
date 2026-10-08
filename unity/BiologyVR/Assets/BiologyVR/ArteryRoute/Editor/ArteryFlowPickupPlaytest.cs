using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>Keyboard/mouse QA actor: moving cell catch, real scanning and release after goal changes.</summary>
    public static class ArteryFlowPickupPlaytest
    {
        static Keyboard keyboard;static Mouse mouse;static KeyboardState keys;
        static Vector2 pendingDelta,pendingScroll,pointer;static ushort buttons;
        static JourneyMission mission;static bool running,testingEmbolus,cursorMode;
        static readonly List<string> checks=new List<string>();
        const string Folder=ArteryPolishReview.Reports+"/FlowPickup";
        [MenuItem("Biology VR/Test Flow Cell Catch And Release Through PC Input")]
        public static void Run()=>Start(false);
        [MenuItem("Biology VR/Test Scene07 Through PC Input")]
        public static void RunEmbolus()=>Start(true);
        [MenuItem("Biology VR/Test Native Mouse Cursor And Spatial Placement")]
        public static void RunCursor()=>Start(false,true);
        static void Start(bool embolus,bool cursor=false)
        {
            mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();
            if(!EditorApplication.isPlaying||running||!mission||(!embolus&&(mission.SceneNumber!=3||mission.StepIndex!=0)))throw new InvalidOperationException("Fresh Scene03 required: playing="+EditorApplication.isPlaying+" running="+running+" scene="+(mission?mission.SceneNumber:-1)+" step="+(mission?mission.StepIndex:-1));
            testingEmbolus=embolus;cursorMode=cursor;running=true;EditorCoroutineUtility.StartCoroutineOwnerless(Guard(Test()));
        }
        static void Check(bool success,string text)
        {checks.Add((success?"PASS: ":"BLOCKED: ")+text);if(!success)throw new InvalidOperationException(text);}
        static IEnumerator Guard(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    var current=stack.Peek();bool moved=false;Exception error=null;
                    try{moved=current.MoveNext();}catch(Exception e){error=e;}
                    if(error!=null){checks.Add("BLOCKED: "+error.Message);Debug.LogException(error);yield break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                    if(current.Current is IEnumerator nested){stack.Push(nested);continue;}yield return current.Current;
                }
            }
            finally
            {
                while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;
                Directory.CreateDirectory(Folder);File.WriteAllText(Folder+(cursorMode?"/NativeCursor_PC_Input.json":testingEmbolus?"/Scene07_PC_Input.json":"/PC_Input.json"),JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,passed=!checks.Exists(c=>c.StartsWith("BLOCKED")),method=cursorMode?"Native MouseState position/buttons/scroll and KeyboardState; Scene03 fresh start, isolated Enter(scene,false) fixtures for Scene07/12/10/14; no success APIs":testingEmbolus?"Isolated Scene07 fixture: initial Enter(7,false) only; all scan/capture/release/transport/deposit through actual keyboard/mouse events, no success APIs":"Actual keyboard/mouse Input System events; no mission success/enter APIs",checks},Formatting.Indented));
            }
        }
        static void Queue()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic&&InputState.currentUpdateType!=InputUpdateType.Fixed)return;
            InputSystem.QueueStateEvent(keyboard,keys);
            bool dynamic=InputState.currentUpdateType==InputUpdateType.Dynamic;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=pointer,buttons=buttons,delta=dynamic?pendingDelta:Vector2.zero,scroll=dynamic?pendingScroll:Vector2.zero});
            if(dynamic)pendingDelta=pendingScroll=Vector2.zero;
        }
        static void UseTestDevices()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic&&InputState.currentUpdateType!=InputUpdateType.Fixed)return;
            // Physical Windows mouse events can otherwise change Mouse.current
            // after our queued state, so the actor and game read different devices.
            keyboard?.MakeCurrent();mouse?.MakeCurrent();
        }
        static IEnumerator Test()
        {
            checks.Clear();Directory.CreateDirectory(Folder);var settings=InputSystem.settings;var bg=settings.backgroundBehavior;var editor=settings.editorInputBehaviorInPlayMode;
            try
            {
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);keys=new KeyboardState();buttons=0;pendingDelta=pendingScroll=Vector2.zero;
                pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);
                InputSystem.onBeforeUpdate+=Queue;InputSystem.onAfterUpdate+=UseTestDevices;
                if(testingEmbolus){yield return TestEmbolus();yield break;}
                if(cursorMode){yield return TestCursor();yield return TestEmbolus();yield return TestPlacement();yield return TestTone();yield return TestDormantRecovery();yield break;}
                var w=mission.world;var flow=w.GetComponent<JourneyBloodFlow>();var rbc=w.Find("rbc");var white=w.Find("leukocyte");
                var before=rbc.transform.position;yield return Wait(.35f);
                Check(Vector3.Distance(before,rbc.transform.position)>.025f,"Interactive RBC actually moves in a real blood-pool slot before catch");
                Check(flow.interactiveCells.Length==3&&flow.cells.Length==360,"Three interactive species use existing 360-cell pool, no mass physics population");
                // Allow a late-start player to look away, then observe the same pool
                // slot pass back through reach. No time/state/mission staging APIs.
                yield return HiddenReentry(rbc);
                yield return AimAt(rbc.transform.position);yield return Press(Key.G,.1f);yield return Wait(.4f);
                var input=mission.GetComponent<JourneyInput>();checks.Add("RBC catch diagnostic: viewport="+mission.mover.viewCamera.WorldToViewportPoint(rbc.transform.position)+" cameraAngles="+mission.mover.viewCamera.transform.localEulerAngles+" held="+rbc.DesktopHeld+" step="+mission.StepIndex+" lock="+(input.LockedTarget?input.LockedTarget.targetId:"none")+" blocker="+(input.AimObstruction?input.AimObstruction.name:"none")+" inputEnabled="+input.enabled);
                ScreenCapture.CaptureScreenshot(Folder+"/Catch_Diagnostic.png");
                Check(mission.StepIndex==1&&rbc.DesktopHeld,"Aimed G catches moving RBC through desktop input");yield return Press(Key.F,1.0f);Check(mission.StepIndex==2,"Held F scans the smoothly caught RBC");
                yield return Press(Key.Z,.1f);yield return Wait(.9f);Check(mission.StepIndex==3&&!rbc.DesktopHeld&&!rbc.GetComponent<JourneyFlowPickup>().Rejoining,"Enlarged RBC is released smoothly back into stream, no static dock");
                yield return Scan("hemoglobin");yield return Scan("heme");
                var oxygen=w.Find("oxygen");yield return AimAt(oxygen.transform.position);yield return Press(Key.G,.1f);yield return Wait(.4f);
                Check(mission.StepIndex==6&&oxygen.DesktopHeld,"Desktop G physically detaches O2 without a BioTool shortcut");
                var heme=w.Find("heme");yield return AimAt(heme.transform.position);
                float distance=Vector3.Distance(mission.mover.viewCamera.transform.position,heme.transform.position);pendingScroll=new Vector2(0,Mathf.Round((distance-1.05f)/JourneyDesktopControls.DepthPerWheelStep)*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.6f);yield return Press(Key.G,.1f);
                Check(mission.StepIndex==7&&!oxygen.DesktopHeld,"Mouse aim/wheel and G release place O2 near heme");
                yield return Scan("co2");Check(mission.StepIndex==8,"Actual gas scan advances to WBC");
                var whitePickup=white.GetComponent<JourneyFlowPickup>();var whiteBefore=white.transform.position;yield return Wait(.35f);
                Check(Vector3.Distance(whiteBefore,white.transform.position)>.025f,"WBC is travelling with its real pool slot, not frozen on a pedestal");
                yield return AimAt(white.transform.position);yield return Press(Key.G,.1f);yield return Wait(.4f);
                Check(white.DesktopHeld,"Aimed G catches moving WBC");yield return Press(Key.F,1f);Check(mission.StepIndex==9&&white.DesktopHeld,"WBC scan changes objective to platelet while the actual WBC remains held");
                var releasePosition=white.transform.position;yield return Press(Key.G,.06f);
                Check(!white.DesktopHeld&&!w.Find("platelet").DesktopHeld,"G releases actual held WBC after goal changes; does not grab the next platelet");
                Check(Vector3.Distance(releasePosition,white.transform.position)<.12f,"WBC release has no abrupt positional teleport");yield return Wait(.8f);
                Check(!whitePickup.Rejoining&&Vector3.Distance(white.transform.position,flow.cells[whitePickup.poolIndex].position)<.015f,"Released WBC rejoins exact moving pool transform after smooth blend");
                var resumed=white.transform.position;yield return Wait(.4f);Check(Vector3.Distance(resumed,white.transform.position)>.04f,"Released WBC continues travelling instead of sticking to camera/home");
                yield return AimAt(white.transform.position);yield return Press(Key.G,.1f);yield return Wait(.3f);Check(white.DesktopHeld&&mission.StepIndex==9,"Studied WBC can be caught again without advancing platelet goal");yield return Press(Key.G,.08f);yield return Wait(.8f);
                var platelet=w.Find("platelet");yield return AimAt(platelet.transform.position);yield return Press(Key.G,.1f);yield return Wait(.4f);Check(platelet.DesktopHeld&&!white.DesktopHeld,"After WBC release, aimed G catches the moving platelet rather than sticking to old species");yield return Press(Key.F,1.05f);yield return Press(Key.G,.08f);yield return Wait(.8f);
                Check(mission.StepIndex==10&&!white.DesktopHeld&&!platelet.DesktopHeld,"Next species can be caught, scanned and released after WBC release");
                ScreenCapture.CaptureScreenshot(Folder+"/Released_WBC_And_Platelet_Goal.png");
            }
            finally
            {InputSystem.onBeforeUpdate-=Queue;InputSystem.onAfterUpdate-=UseTestDevices;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);settings.backgroundBehavior=bg;settings.editorInputBehaviorInPlayMode=editor;}
        }
        static IEnumerator TestEmbolus()
        {
            mission.Enter(7,false);yield return Wait(.4f);
            var world=mission.world;var input=mission.GetComponent<JourneyInput>();var camera=mission.mover.viewCamera;
            Quaternion rotation=camera.transform.rotation;
            Check(!mission.mover.UsingTrackedInput&&!input.useExplicitAimSources,"Mouse test uses the desktop camera aim, no simulated XR hands");
            keys=new KeyboardState(Key.F);float start=Time.time;
            while(mission.StepIndex==0&&Time.time-start<4)yield return AimAt(world.bubble.position);
            keys=new KeyboardState();yield return Wait(.15f);Check(mission.StepIndex==1,"Held F with actual mouse tracking scans the moving gas embolus");
            yield return AimAt(world.Find("attract-mode").transform.position);yield return Press(Key.E,.1f);Check(mission.StepIndex==2,"Aimed E selects the existing attraction mode");
            yield return CaptureEmbolus();yield return Wait(.25f);Check(!mission.Complete&&world.EmbolusStability==0,"Desktop capture outside the zone cannot auto-deposit");
            var release=world.bubble.position;keys=new KeyboardState();yield return Wait(.12f);
            Check(!world.EmbolusCaptured&&mission.StepIndex==2&&Vector3.Distance(release,world.bubble.position)<.6f,"Releasing E preserves nearby pose and retries only capture");
            yield return CaptureEmbolus();yield return Wait(.15f);
            yield return AimAt(world.trap.position);yield return Wait(.6f);
            Check(!mission.Complete&&world.EmbolusStability==0,"Mouse direction alone at the wrong depth does not complete stabilization");
            float depthBefore=input.DesktopFieldDistance;float desired=Vector3.Distance(camera.transform.position,world.trap.position);
            pendingScroll=new Vector2(0,-JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.12f);
            Check(Mathf.Abs(input.DesktopFieldDistance-(depthBefore-JourneyDesktopControls.DepthPerWheelStep))<.015f,"One native normalized wheel step (1 by default) changes field depth by 0.15m, not 0.0025m");
            pendingScroll=new Vector2(0,Mathf.Round((desired-input.DesktopFieldDistance)/JourneyDesktopControls.DepthPerWheelStep)*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.2f);
            Check(Mathf.Abs(input.DesktopFieldDistance-desired)<.09f&&Mathf.Abs(depthBefore-input.DesktopFieldDistance)>.1f,"Whole native wheel ticks change field depth to the reachable zone");
            start=Time.time;while(!mission.Complete&&Time.time-start<5)yield return Wait(.1f);
            Check(mission.Complete&&world.EmbolusStability>=1&&!world.EmbolusCaptured&&!world.bubble.gameObject.activeInHierarchy,"Held E plus mouse aim/wheel physically transports and settles the embolus inside the real trigger");
            if(cursorMode)Check(Quaternion.Angle(rotation,camera.transform.rotation)<.01f,"Scene07 cursor transport succeeds without RMB, camera rotation or a virtual XR controller");
            keys=new KeyboardState();ScreenCapture.CaptureScreenshot(Folder+"/Scene07_Mouse_Stabilized.png");
        }
        static IEnumerator CaptureEmbolus()
        {
            keys=new KeyboardState(Key.E);float start=Time.time;
            while(!mission.world.EmbolusCaptured&&Time.time-start<4)yield return AimAt(mission.world.bubble.position);
            Check(mission.world.EmbolusCaptured&&mission.StepIndex==3,"Actual held E catches the moving embolus after charge");
        }
        static IEnumerator AimAt(Vector3 point)
        {
            if(cursorMode){pointer=mission.mover.viewCamera.WorldToScreenPoint(point);yield return Wait(.1f);yield break;}
            var camera=mission.mover.viewCamera.transform;
            var local=camera.parent.InverseTransformDirection(point-camera.position);var desired=Quaternion.LookRotation(local,Vector3.up).eulerAngles;var current=camera.localEulerAngles;
            for(int attempt=0;attempt<4;attempt++)
            {
                current=camera.localEulerAngles;float yaw=Mathf.DeltaAngle(current.y,desired.y),pitch=Mathf.DeltaAngle(current.x,desired.x);
                if(Mathf.Abs(yaw)<.5f&&Mathf.Abs(pitch)<.5f)break;
                pendingDelta=new Vector2(yaw/.12f,-pitch/.12f);buttons=2;yield return Wait(.10f);buttons=0;yield return Wait(.05f);
            }
            yield return Wait(.05f);
        }
        static IEnumerator TestCursor()
        {
            var target=mission.world.Find("rbc");var camera=mission.mover.viewCamera;Quaternion rotation=camera.transform.rotation;
            yield return AimAt(target.transform.position);buttons=1;yield return Wait(.4f);
            var controls=mission.GetComponent<JourneyInput>().DesktopControls;var grab=target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            checks.Add("Native grab diagnostic: active="+controls.Active+" grip="+controls.GrabHeld+" holding="+controls.Holding+" hover="+(controls.HoveredGrab()?controls.HoveredGrab().name:"none")+" targetSelected="+grab.isSelected+" grabbed="+target.grabbed+" step="+mission.StepIndex+" pointer="+pointer+" screen="+Screen.width+"x"+Screen.height+" target="+target.transform.position+" camera="+camera.transform.position+" mousePressed="+Mouse.current.leftButton.isPressed+" enabled="+mouse.enabled+" currentMatches="+(Mouse.current==mouse));
            ScreenCapture.CaptureScreenshot(Folder+"/Native_Cursor_Diagnostic.png");
            Check(mission.StepIndex==1&&target.grabbed&&target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().isSelected,"Native LMB holds RBC through actual desktop XRI interactor");
            var before=target.transform.position;pointer+=new Vector2(Screen.width*.09f,0);yield return Wait(.35f);
            Check(Vector3.Distance(before,target.transform.position)>.1f&&Quaternion.Angle(rotation,camera.transform.rotation)<.01f,"Mouse cursor drags the held object without rotating the camera");
            yield return Press(Key.F,1.05f);Check(mission.StepIndex==2,"F scans the dragged object under the cursor");
            pendingScroll=new Vector2(0,-JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.3f);buttons=0;yield return Wait(.9f);
            Check(!target.grabbed&&!target.GetComponent<JourneyFlowPickup>().Rejoining,"Mouse release returns RBC smoothly to real flow");
        }
        static IEnumerator TestPlacement()
        {
            mission.Enter(12,false);yield return Wait(.4f);var puzzle=mission.world.hemostasisPuzzle;
            var piece=puzzle.platelets[0];
            // Frame source and wound with real RMB look, then drag using cursor only.
            bool previous=cursorMode;cursorMode=false;yield return AimAt((piece.transform.position+puzzle.Slot(0))*.5f);cursorMode=previous;
            yield return AimAt(piece.transform.position);buttons=1;yield return Wait(.3f);
            Check(piece.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().isSelected,"Native LMB selects a physical platelet piece, not a semantic Direct shortcut");
            var desktop=mission.GetComponent<JourneyInput>().DesktopControls;
            yield return AimAt(puzzle.Slot(0));
            float distance=Vector3.Distance(mission.mover.viewCamera.transform.position,puzzle.Slot(0));
            pendingScroll=new Vector2(0,Mathf.Round((distance-desktop.GrabDepth)/JourneyDesktopControls.DepthPerWheelStep)*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.5f);buttons=0;yield return Wait(.5f);
            Check(puzzle.Attached==1&&piece.Placed&&mission.StepIndex==1,"Mouse pointer/wheel and LMB release physically place the first platelet into the real adhesion slot");
            ScreenCapture.CaptureScreenshot(Folder+"/Scene12_Mouse_Placement.png");
        }
        static IEnumerator TestTone()
        {
            mission.Enter(10,false);yield return Wait(.4f);
            cursorMode=false;yield return AimAt(mission.world.Find("tone").transform.position);cursorMode=true;
            yield return AimAt(mission.world.Find("pressure-low").transform.position);yield return Press(Key.E,.1f);
            Check(mission.StepIndex==1,"Cursor E measures initial Scene10 pressure");
            yield return AimAt(mission.world.Find("tone-mode").transform.position);yield return Press(Key.E,.1f);
            Check(mission.StepIndex==2,"Cursor E selects the vascular mode");
            yield return AimAt(mission.world.Find("tone").transform.position);keys=new KeyboardState(Key.E);yield return Wait(1.05f);
            Check(mission.StepIndex==2&&Mathf.Abs(mission.ModelRadius-1)<.01f,"Holding E without wheel motion cannot auto-set a successful tone");
            pendingScroll=new Vector2(0,-14*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.2f);
            Check(mission.StepIndex==2&&mission.ModelRadius<.68f,"Whole normalized wheel ticks produce an excessive constriction, without success");
            pendingScroll=new Vector2(0,7*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(1.05f);
            Check(mission.StepIndex==3&&mission.ModelRadius>=.75f&&mission.ModelRadius<=.88f,"Mouse wheel corrects the tone and sustained safe setting completes the real regulator goal");keys=new KeyboardState();
        }
        static IEnumerator TestDormantRecovery()
        {
            mission.Enter(14,false);yield return Wait(.3f);
            var puzzle=mission.world.hemostasisPuzzle;
            foreach(var strand in puzzle.strands)Check(strand.Grab&&!strand.Grab.enabled,"Inactive fibrin strand can be disabled safely on isolated recovery entry: "+strand.name);
        }
        static IEnumerator HiddenReentry(JourneyTarget target)
        {
            var pickup=target.GetComponent<JourneyFlowPickup>();var camera=mission.mover.viewCamera;
            var path=pickup.flow.path;int before=pickup.HiddenReentries;
            yield return AimAt(camera.transform.position+path.Right(path.Anchor(3))*4);
            float start=Time.time;
            while(pickup.HiddenReentries==before&&Time.time-start<35)yield return Wait(.1f);
            checks.Add("Reentry diagnostic: camera="+camera.transform.position+" rotation="+camera.transform.eulerAngles+" cell="+target.transform.position+" incoming="+path.Offset(path.Anchor(3)-2.5f,pickup.startLane.x,pickup.startLane.y)+" tracked="+mission.mover.UsingTrackedInput+" reentries="+pickup.HiddenReentries+" before="+before);
            Check(pickup.HiddenReentries>before,"Late-start RBC recycles only while both outgoing/incoming positions are outside the camera frustum");
            yield return AimAt(camera.transform.position+path.Forward(path.Anchor(3))*4);
            start=Time.time;
            while(Time.time-start<25)
            {
                var viewport=camera.WorldToViewportPoint(target.transform.position);
                if(viewport.z>1.25f&&viewport.z<3&&viewport.x>.1f&&viewport.x<.9f&&viewport.y>.1f&&viewport.y<.9f)break;
                yield return Wait(.1f);
            }
            Check(Vector3.Distance(camera.transform.position,target.transform.position)<3.5f,"Same pooled RBC returns through the nearby catch zone without scene reset");
        }
        static IEnumerator Scan(string id)
        {
            int before=mission.StepIndex;var target=mission.world.Find(id);yield return AimAt(target.GetComponent<Collider>().bounds.center);yield return Press(Key.F,1.05f);
            Check(mission.StepIndex==before+1,"Actual mouse/Scanner scan: "+id);
        }
        static IEnumerator Press(Key key,float duration)
        {keys=new KeyboardState(key);yield return Wait(duration);keys=new KeyboardState();yield return Wait(.12f);}
        static IEnumerator Wait(float time)
        {
            float start=Time.time;double deadline=EditorApplication.timeSinceStartup+10;
            while(Time.time-start<time){if(!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Player input loop did not advance");yield return null;}
        }
    }
}

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
    /// <summary>Keyboard/mouse QA actor: moving cell catch, real scanning and release after goal changes.</summary>
    public static class ArteryFlowPickupPlaytest
    {
        static Keyboard keyboard;static Mouse mouse;static KeyboardState keys;
        static Vector2 pendingDelta,pendingScroll,pointer;static ushort buttons;
        static JourneyMission mission;static bool running,testingEmbolus,cursorMode,continuousOnly,plaqueOnly;
        static readonly List<string> checks=new List<string>();
        const string Folder=ArteryPolishReview.Reports+"/FlowPickup";
        [MenuItem("Biology VR/Test Flow Cell Catch And Release Through PC Input")]
        public static void Run()=>Start(false);
        [MenuItem("Biology VR/Test Scene07 Through PC Input")]
        public static void RunEmbolus()=>Start(true);
        [MenuItem("Biology VR/Test Native Mouse Cursor And Spatial Placement")]
        public static void RunCursor()=>Start(false,true);
        [MenuItem("Biology VR/Test Endless Random Bloodstream")]
        public static void RunContinuous()=>Start(false,false,true);
        [MenuItem("Biology VR/Test Growing Plaque Through Actual PC Input")]
        public static void RunPlaque()=>Start(false,true,false,true);
        static void Start(bool embolus,bool cursor=false,bool continuous=false,bool plaque=false)
        {
            mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();
            if(!EditorApplication.isPlaying||running||!mission||(!embolus&&(mission.SceneNumber!=3||mission.StepIndex!=0)))throw new InvalidOperationException("Fresh Scene03 required: playing="+EditorApplication.isPlaying+" running="+running+" scene="+(mission?mission.SceneNumber:-1)+" step="+(mission?mission.StepIndex:-1));
            testingEmbolus=embolus;cursorMode=cursor;continuousOnly=continuous;plaqueOnly=plaque;running=true;EditorCoroutineUtility.StartCoroutineOwnerless(Guard(Test()));
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
                Directory.CreateDirectory(Folder);File.WriteAllText(Folder+(plaqueOnly?"/Plaque_Challenge_PC.json":continuousOnly?"/Continuous_Random_Flow.json":cursorMode?"/NativeCursor_PC_Input.json":testingEmbolus?"/Scene07_PC_Input.json":"/PC_Input.json"),JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,passed=!checks.Exists(c=>c.StartsWith("BLOCKED")),method=plaqueOnly?"Isolated Enter(6,false) fixture; real mouse/keyboard scan, shots, failure, automatic local retry and Continue; no success APIs":continuousOnly?"240 simulated seconds at timeScale 5; real forward/back mouse look; no scene reset or mission success APIs":cursorMode?"Native MouseState position/buttons/scroll and KeyboardState; Scene03 fresh start, isolated Enter(scene,false) fixtures for Scene09/07/12/10/14; no success APIs":testingEmbolus?"Isolated Scene07 fixture: initial Enter(7,false) only; all scan/capture/release/transport/deposit through actual keyboard/mouse events, no success APIs":"Actual keyboard/mouse Input System events; no mission success/enter APIs",checks},Formatting.Indented));
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
            var drivers=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b is XRDeviceSimulator||b is XRInteractionSimulator).Cast<Behaviour>().ToArray();var enabled=drivers.Select(b=>b.enabled).ToArray();
            var simulatedDevices=InputSystem.devices.Where(d=>d.enabled&&d.layout.StartsWith("XRSimulated")).ToArray();
            try
            {
                foreach(var driver in drivers)driver.enabled=false;foreach(var device in simulatedDevices)if(device.added)InputSystem.DisableDevice(device);
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);keys=new KeyboardState();buttons=0;pendingDelta=pendingScroll=Vector2.zero;
                pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);
                InputSystem.onBeforeUpdate+=Queue;InputSystem.onAfterUpdate+=UseTestDevices;
                if(plaqueOnly){yield return TestPlaque();yield break;}
                if(continuousOnly){yield return TestContinuous();yield break;}
                if(testingEmbolus){yield return TestEmbolus();yield break;}
                if(cursorMode){yield return TestBackground();yield return TestScreens();yield return TestCursor();yield return TestAssault();yield return TestEmbolus();yield return TestPlacement();yield return TestTone();yield return TestDormantRecovery();yield break;}
                var w=mission.world;var flow=w.GetComponent<JourneyBloodFlow>();var rbc=w.Find("rbc");var white=w.Find("leukocyte");
                var before=rbc.transform.position;yield return Wait(.35f);
                Check(Vector3.Distance(before,rbc.transform.position)>.025f,"Interactive RBC actually moves in a real blood-pool slot before catch");
                Check(flow.interactiveCells.Length==5&&flow.cells.Length==360,"Five interactive blood components use existing 360-cell pool, no mass physics population");
                // Allow a late-start player to look away, then observe the same pool
                // slot pass back through reach. No time/state/mission staging APIs.
                yield return HiddenReentry(rbc);
                yield return AimAt(rbc.transform.position);yield return Press(Key.G,.1f);yield return Wait(.4f);
                var input=mission.GetComponent<JourneyInput>();checks.Add("RBC catch diagnostic: viewport="+mission.mover.viewCamera.WorldToViewportPoint(rbc.transform.position)+" cameraAngles="+mission.mover.viewCamera.transform.localEulerAngles+" held="+rbc.DesktopHeld+" step="+mission.StepIndex+" lock="+(input.LockedTarget?input.LockedTarget.targetId:"none")+" blocker="+(input.AimObstruction?input.AimObstruction.name:"none")+" inputEnabled="+input.enabled);
                ScreenCapture.CaptureScreenshot(Folder+"/Catch_Diagnostic.png");
                Check(mission.StepIndex==1&&rbc.DesktopHeld,"Aimed G catches moving RBC through desktop input");yield return Press(Key.F,1.0f);Check(mission.StepIndex==2,"Held F scans the smoothly caught RBC");
                yield return Press(Key.Z,.1f);yield return Wait(.4f);Check(mission.StepIndex==3&&rbc.DesktopHeld,"Enlarged RBC stays extracted for studying its moving Hb/heme cargo");
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
            {InputSystem.onBeforeUpdate-=Queue;InputSystem.onAfterUpdate-=UseTestDevices;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);for(int i=0;i<drivers.Length;i++)if(drivers[i])drivers[i].enabled=enabled[i];foreach(var device in simulatedDevices)if(device.added)InputSystem.EnableDevice(device);settings.backgroundBehavior=bg;settings.editorInputBehaviorInPlayMode=editor;}
        }
        static IEnumerator TestEmbolus()
        {
            mission.Enter(7,false);yield return Wait(.4f);
            var world=mission.world;var input=mission.GetComponent<JourneyInput>();var camera=mission.mover.viewCamera;
            if(world.embolusWave)
            {
                bool previousCursor=cursorMode;cursorMode=false;var centre=world.embolusWave.targets.Aggregate(Vector3.zero,(sum,t)=>sum+t.transform.position)/world.embolusWave.targets.Length;yield return AimAt(centre);cursorMode=previousCursor;
                Directory.CreateDirectory(ArteryPolishReview.Reports+"/InfluxAndEmboli");ArteryPolishReview.Capture(camera,"InfluxAndEmboli/01_Six_Bubbles_Actual_PC_Start.png");
            }
            Quaternion rotation=camera.transform.rotation;
            Check(!mission.mover.UsingTrackedInput&&!input.useExplicitAimSources,"Mouse test uses the desktop camera aim, no simulated XR hands");
            keys=new KeyboardState(Key.F);float start=Time.time;
            while(mission.StepIndex==0&&Time.time-start<4)yield return AimAt(world.bubble.position);
            keys=new KeyboardState();yield return Wait(.15f);Check(mission.StepIndex==1,"Held F with actual mouse tracking scans the moving gas embolus");
            yield return AimAt(world.Find("attract-mode").transform.position);yield return Press(Key.E,.1f);Check(mission.StepIndex==2,"Aimed E selects the existing attraction mode");
            yield return CaptureEmbolus();yield return Wait(.25f);Check(!mission.Complete&&world.EmbolusStability==0,"Desktop capture outside the zone cannot auto-deposit");
            var activeTools=mission.GetComponent<JourneyInstrumentRig>();
            if(activeTools)
            {
                float stopped=activeTools.ActiveBioTool.RotationDegrees;yield return Wait(.2f);
                Check(activeTools.modes.CurrentMode==BioToolMode.Capture&&Mathf.Abs(activeTools.ActiveBioTool.RotationDegrees-stopped)<.01f,"CAPTURE mode stops prong rotation during actual held field");
                Check(activeTools.CaptureBeamVisible&&activeTools.captureRays.All(r=>r.enabled)&&activeTools.ActiveBioTool.HookAmount>.5f,"Four converging capture rays and flexible tether accompany real captured bubble");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"ActiveInstruments/03_Capture_Hook_Actual_Input.png");
            }
            if(cursorMode){Directory.CreateDirectory(ArteryVisualV6.Reports);ScreenCapture.CaptureScreenshot(ArteryVisualV6.Reports+"/06_BioTool_Actual_PC_Input.png");yield return Wait(.12f);}
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
            start=Time.time;while(world.EmbolusCaptured&&Time.time-start<5)yield return Wait(.1f);
            Check(world.EmbolusStability>=1&&!world.EmbolusCaptured&&(world.embolusWave?world.embolusWave.StabilizedCount==1:mission.Complete),"Held E plus mouse aim/wheel physically transports and settles the first embolus inside the real trigger");
            if(cursorMode)Check(Quaternion.Angle(rotation,camera.transform.rotation)<.01f,"Scene07 cursor transport succeeds without RMB, camera rotation or a virtual XR controller");
            keys=new KeyboardState();ScreenCapture.CaptureScreenshot(Folder+"/Scene07_Mouse_Stabilized.png");
            if(world.embolusWave)
            {
                Directory.CreateDirectory(ArteryPolishReview.Reports+"/InfluxAndEmboli");ArteryPolishReview.Capture(camera,"InfluxAndEmboli/02_Six_Emboli_Progress.png");
                while(!mission.Complete)
                {
                    keys=new KeyboardState();yield return Wait(.12f);int before=world.embolusWave.StabilizedCount;yield return CaptureEmbolus();
                    if(before==3)
                    {int deposited=world.embolusWave.StabilizedCount;int step=mission.StepIndex;var held=world.bubble;keys=new KeyboardState();yield return Wait(.15f);Check(!world.EmbolusCaptured&&world.embolusWave.StabilizedCount==deposited&&mission.StepIndex==step-1&&world.bubble==held,"Releasing the fourth bubble retries only its capture and preserves three deposited bubbles");yield return CaptureEmbolus();}
                    yield return AimAt(world.trap.position);desired=Vector3.Distance(camera.transform.position,world.trap.position);
                    pendingScroll=new Vector2(0,Mathf.Round((desired-input.DesktopFieldDistance)/JourneyDesktopControls.DepthPerWheelStep)*JourneyDesktopControls.WheelUnitsPerStep);
                    start=Time.time;while(world.EmbolusCaptured&&Time.time-start<7)yield return Wait(.1f);
                    Check(world.embolusWave.StabilizedCount==before+1,"Real mouse-held field deposits gas embolus "+(before+1)+" / 6");
                }
                keys=new KeyboardState();Check(world.embolusWave.Complete&&world.embolusWave.StabilizedCount==6,"All six independent gas bubbles require physical capture and deposit");ArteryPolishReview.Capture(camera,"InfluxAndEmboli/03_All_Six_Neutralized.png");
            }
        }
        static IEnumerator TestScreens()
        {
            var screens=mission.GetComponent<JourneyHolographicScreens>();if(!screens)yield break;
            Directory.CreateDirectory(ArteryPolishReview.Reports+"/ActiveInstruments");yield return Wait(.2f);
            Check(screens.theoryText.text.Contains(mission.LearningNote)&&screens.overview.activeInHierarchy&&!screens.referenceView.activeInHierarchy&&screens.shortTheory.text.Split('\n').Length<=4,"Clean overview shows short theory and task; full scientific reference stays available separately");
            Check(screens.shortTheory.material.shader.name=="BiologyVR/Holographic Overlay UI"&&screens.vitalValues.All(t=>t.cachedTextGenerator.vertexCount>4),"Foreground HUD material is applied and all five large vitals values generate visible text geometry");
            var viewport=mission.mover.viewCamera.WorldToViewportPoint(screens.theoryRoot.position);
            Check(viewport.x>.12f&&viewport.x<.45f&&viewport.y>.2f&&viewport.y<.8f,"VR-compatible theory panel centre stays inside comfortable front-left view");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"ActiveInstruments/05_Holographic_Screens_Default.png");
            var plan=screens.referenceButton.GetComponent<RectTransform>();pointer=mission.mover.viewCamera.WorldToScreenPoint(plan.TransformPoint(plan.rect.center));yield return Wait(.1f);buttons=1;yield return Wait(.1f);buttons=0;yield return Wait(.15f);
            Check(screens.ShowPractice&&mission.StepIndex==0,"Actual native click on Details opens reference without grabbing blood behind the UI");
            Check(screens.ShowPractice&&screens.theoryScroll.content.rect.height>screens.theoryScroll.viewport.rect.height,"Research plan extends scrollable main theory without hiding its explanation");
            pointer=mission.mover.viewCamera.WorldToScreenPoint(screens.theoryScroll.viewport.position);yield return Wait(.1f);
            float top=screens.theoryScroll.verticalNormalizedPosition;pendingScroll=new Vector2(0,-8*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.3f);
            Check(screens.theoryScroll.verticalNormalizedPosition<top-.03f,"Actual native mouse wheel scrolls full theory through EventSystem/ScrollRect");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"ActiveInstruments/06_Holographic_Theory_Scrolled.png");
            yield return Press(Key.Tab,.08f);pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);
        }
        static IEnumerator TestBackground()
        {
            var flow=mission.world.GetComponent<JourneyBloodFlow>();if(!flow.layeredStudyBackground)yield break;
            string folder=ArteryPolishReview.Reports+"/BiomedicalLivingPass/Stage2";Directory.CreateDirectory(folder);
            var reserved=new HashSet<int>(flow.interactiveCells.Select(p=>p.poolIndex));var background=flow.cells.Where((c,i)=>!reserved.Contains(i)).ToArray();
            var positions=background.Select(c=>c.position).ToArray();yield return Wait(.45f);
            Check(background.Length==355&&flow.cells.Length==360,"Atmospheric pass reuses 355 visual pool slots and preserves five interactive slots");
            Check(background.All(c=>c.GetComponentsInChildren<Collider>(true).Length==0&&c.GetComponentsInChildren<Rigidbody>(true).Length==0&&c.GetComponentsInChildren<JourneyTarget>(true).Length==0),"Background versions contain no colliders, rigidbodies or mission targets");
            Check(background.Where((c,i)=>Vector3.Distance(c.position,positions[i])>.02f).Count()>250,"Real background population flows continuously instead of freezing around the lesson");
            var camera=mission.mover.viewCamera;var planes=GeometryUtility.CalculateFrustumPlanes(camera);int[] depthCounts=new int[3];
            foreach(var cell in background)
            {
                var renderer=cell.GetComponent<Renderer>();if(!renderer.enabled||!GeometryUtility.TestPlanesAABB(planes,renderer.bounds))continue;
                float d=Vector3.Distance(camera.transform.position,cell.position);depthCounts[d<9?0:d<22?1:2]++;
            }
            checks.Add("Visible background depth counts: "+string.Join(", ",depthCounts));
            checks.Add("Background diagnostic: camera="+camera.transform.position+" facing="+camera.transform.forward+" enabled="+background.Count(c=>c.GetComponent<Renderer>().enabled)+" first="+background[0].position+" bounds="+background[0].GetComponent<Renderer>().bounds+" anchor="+flow.path.Anchor(3));
            ArteryPolishReview.Capture(camera,"BiomedicalLivingPass/Stage2/Background_Diagnostic.png");
            Check(depthCounts[0]>3&&depthCounts[1]>12&&depthCounts[2]>5,"Background is visible in near, middle and far layers of the first authored view");
            var materials=background.SelectMany(c=>c.GetComponent<Renderer>().sharedMaterials).Distinct().ToArray();
            var triangles=background.Sum(c=>c.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);
            File.WriteAllText(folder+"/RuntimeBackground.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,pool=flow.cells.Length,background=background.Length,visibleDepthCounts=depthCounts,backgroundMeshTriangles=triangles,sharedMaterials=materials.Length,allInstancedMaterials=materials.All(m=>m.enableInstancing),editorBatches=UnityStats.batches,editorSetPass=UnityStats.setPassCalls,hardwareFPSMeasured=false},Formatting.Indented));
            ArteryPolishReview.Capture(camera,"BiomedicalLivingPass/Stage2/04_Living_Bloodstream_With_UI.png");
            yield return Press(Key.F1,.08f);yield return Wait(.6f);ArteryPolishReview.Capture(camera,"BiomedicalLivingPass/Stage2/05_Flow_Into_Depth.png");
            yield return Press(Key.F1,.08f);yield return Wait(.6f);
            checks.Add("Living blood flow depth counts: "+string.Join(", ",depthCounts));
        }
        static IEnumerator TestContinuous()
        {
            var flow=mission.world.GetComponent<JourneyBloodFlow>();var stream=flow.continuousBackground;
            Check(stream&&stream.BackgroundCount==355,"Endless circulation owns the existing 355 background slots");
            var excluded=new HashSet<int>(flow.interactiveCells.Select(p=>p.poolIndex));var cells=flow.cells.Where((c,i)=>!excluded.Contains(i)).ToArray();var camera=mission.mover.viewCamera;
            var forward=flow.path.Forward(flow.mover.Distance);float scale=Time.timeScale;float start=Time.time;int minAhead=int.MaxValue,minBehind=int.MaxValue;
            try
            {
                Time.timeScale=5;
                for(int sample=0;sample<24;sample++)
                {
                    yield return Wait(10);
                    int ahead=0,behind=0;
                    foreach(var cell in cells){var delta=cell.position-camera.transform.position;float dot=Vector3.Dot(delta,forward);if(delta.sqrMagnitude<65*65&&delta.sqrMagnitude>3*3){if(dot>0)ahead++;else behind++;}}
                    minAhead=Math.Min(minAhead,ahead);minBehind=Math.Min(minBehind,behind);
                    checks.Add($"t={Time.time-start:0}s ahead={ahead} behind={behind} recycled={stream.RecycledCells} changes={stream.RandomSpeciesChanges} branch passages="+string.Join(",",stream.BranchPassages));
                    if(sample==5||sample==17)
                    {yield return AimAt(camera.transform.position-forward*5);yield return Wait(.5f);Directory.CreateDirectory(ArteryPolishReview.Reports+"/BiomedicalLivingPass/Stage2");ArteryPolishReview.Capture(camera,"BiomedicalLivingPass/Stage2/06_Continuous_Backward_View.png");yield return AimAt(camera.transform.position+forward*5);}
                }
                Check(minAhead>=12&&minBehind>=12,"Foreground and rear blood populations remain supplied throughout four simulated minutes");
                Check(stream.RecycledCells>25&&stream.RandomSpeciesChanges>20,"Pool re-enters repeatedly with random biological species instead of a repeating fixed order");
                Check(stream.BranchPassages.All(n=>n>3),"Particles continuously enter every authored artery branch");
                Check(mission.SceneNumber==3&&mission.StepIndex==0,"Long-running circulation does not advance or reset the biological lesson");
                File.WriteAllText(ArteryPolishReview.Reports+"/BiomedicalLivingPass/Stage2/ContinuousRuntime.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,runSeed=stream.RunSeed,simulatedSeconds=Time.time-start,minAhead,minBehind,stream.RecycledCells,stream.RandomSpeciesChanges,stream.BranchPassages,types=stream.CurrentSpeciesCounts,hardwareFPSMeasured=false},Formatting.Indented));
            }
            finally{Time.timeScale=scale;}
        }
        static IEnumerator TestPlaque()
        {
            mission.Enter(6,false);yield return Wait(.3f);var challenge=mission.world.plaqueChallenge;Check(challenge&&challenge.SpawnedCount==30&&challenge.lipids.Length==90,"Plaque starts with thirty targets and uses a ninety-slot reinforcement pool");
            Directory.CreateDirectory(ArteryPolishReview.Reports+"/PlaqueChallenge");cursorMode=false;yield return AimAt(challenge.site.position);cursorMode=true;
            yield return AimAt(mission.world.Find("plaque-flow").GetComponent<Collider>().bounds.center);yield return Press(Key.E,.1f);Check(mission.StepIndex==1,"Actual BioTool measures the plaque episode");
            yield return AimAt(challenge.diagnosisContact.bounds.center);yield return Press(Key.F,1.05f);Check(mission.StepIndex==2,"Actual Scanner identifies the full growing mass");
            yield return Wait(2.8f);yield return AimAt(mission.world.Find("pulse-mode").GetComponent<Collider>().bounds.center);yield return Press(Key.E,.1f);Check(mission.StepIndex==3,"Actual input opens timed plaque pulse challenge");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"PlaqueChallenge/01_Initial_Growing_Mass.png");
            int bornBefore=challenge.SpawnedCount;yield return Wait(4.1f);Check(challenge.SpawnedCount-bornBefore>=4&&challenge.SpawnedCount-bornBefore<=10&&challenge.IncomingCount>0,"Random one/two-lipid batches enter from blood each second and visibly fly toward the mass");ArteryPolishReview.Capture(mission.mover.viewCamera,"PlaqueChallenge/06_Lipids_Entering_From_Blood.png");
            var scale=Time.timeScale;int retries=mission.Retries;
            try{Time.timeScale=8;double deadline=EditorApplication.timeSinceStartup+15;while(!challenge.Failed){if(EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("No occlusion reached while withholding shots");yield return Wait(.5f);}}
            finally{Time.timeScale=scale;}
            Check(challenge.Failed&&challenge.FlowFraction<.001f&&!mission.Complete,"Ignoring growth reaches real timed occlusion, not mission success");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"PlaqueChallenge/02_Occlusion_And_Monitor.png");
            yield return Wait(3.3f);Check(!challenge.Failed&&challenge.DestroyedCount==0&&mission.StepIndex==3&&mission.Progress==0&&mission.Retries==retries+1,"Occlusion retries only the combat attempt, preserving diagnosis and controller mode");
            keys=new KeyboardState(Key.E);float start=Time.time;bool impactCaptured=false;
            while(!challenge.Cleared&&Time.time-start<35)
            {
                var target=challenge.lipids.Where(p=>p.contact.enabled).OrderBy(p=>(p.contact.bounds.center-mission.mover.viewCamera.transform.position).sqrMagnitude).FirstOrDefault();
                if(target)
                {
                    var viewport=mission.mover.viewCamera.WorldToViewportPoint(target.contact.bounds.center);
                    if(viewport.z<=0||viewport.x<.08f||viewport.x>.92f||viewport.y<.08f||viewport.y>.92f)
                    {cursorMode=false;yield return AimAt(target.contact.bounds.center);cursorMode=true;}
                    pointer=mission.mover.viewCamera.WorldToScreenPoint(target.contact.bounds.center);
                }
                if(!impactCaptured&&challenge.DestroyedCount>=5){ArteryPolishReview.Capture(mission.mover.viewCamera,"PlaqueChallenge/03_Actual_Fragmentation_And_Shards.png");impactCaptured=true;}
                yield return Wait(.04f);
            }
            keys=new KeyboardState();yield return Wait(.5f);
            var input=mission.GetComponent<JourneyInput>();checks.Add("Plaque firing diagnostic: destroyed="+challenge.DestroyedCount+" hits="+challenge.AcceptedHits+" lock="+(input.LockedTarget?input.LockedTarget.name:"none")+" obstruction="+(input.AimObstruction?input.AimObstruction.name:"none")+" elapsed="+challenge.Elapsed);
            Check(challenge.Cleared&&challenge.DestroyedCount==challenge.SpawnedCount&&challenge.ReinforcementCount>0&&mission.Complete,"Actual shots destroy original and newly arriving lipids; clearance requires no remaining inbound or attached elements");
            Check(challenge.lipids.All(p=>!p.contact.enabled)&&!challenge.bulk.gameObject.activeSelf&&challenge.FlowFraction>.999f,"Complete fragmentation removes hit contacts and obstructing bulk, restoring open flow");
            Check(Mathf.Abs(mission.PressureSystolic-120)<.1f&&Mathf.Abs(challenge.PulseBpm-72)<.1f&&Mathf.Abs(challenge.OxygenPercent-98)<.1f,"Monitor pressure, pulse, oxygen and local flow recover with mass clearance");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"PlaqueChallenge/04_Cleared_Lumen_And_Recovery.png");
            var originalSite=challenge.site;yield return Press(Key.RightArrow,.12f);yield return Wait(10f);
            Check(mission.SceneNumber==7&&mission.Ready&&challenge.site==originalSite&&challenge.Cleared,"Real Continue preserves cleared plaque state and proceeds to embolus episode");
        }
        static IEnumerator TestAssault()
        {
            mission.Enter(9,false);yield return Wait(.3f);var world=mission.world;
            yield return AimAt(world.Find("immune-mode").GetComponent<Collider>().bounds.center);yield return Press(Key.E,.10f);yield return Wait(.55f);
            Check(mission.StepIndex==1&&world.IncomingViruses.Count>=6,"Shooter begins with six simultaneous targets instead of one distant virion");
            var target=world.IncomingViruses[0];yield return AimAt(target.GetComponent<Collider>().bounds.center);int before=world.MarkedViruses;
            yield return Press(Key.E,.05f);
            Check(world.MarkedViruses==before+1,"Short native E tap immediately hits an aimed virion without a charge hold");
            keys=new KeyboardState(Key.E);float start=Time.time;
            while(mission.StepIndex==1&&Time.time-start<20)
            {
                if(world.IncomingViruses.Count>0)
                {
                    var virus=world.IncomingViruses[0];pointer=mission.mover.viewCamera.WorldToScreenPoint(virus.GetComponent<Collider>().bounds.center);
                }
                if(world.MarkedViruses>=4&&world.MarkedViruses<6)ArteryPolishReview.Capture(mission.mover.viewCamera,"ActiveInstruments/07_Rapid_Viral_Assault_PC.png");
                yield return Wait(.04f);
            }
            keys=new KeyboardState();yield return Wait(.2f);
            Check(mission.StepIndex==2&&world.MarkedViruses==JourneyMission.AssaultVirusCount&&world.AssaultHits==JourneyMission.AssaultVirusCount,"Sustained actual E with cursor tracking fires a complete twenty-four-target wave");
            Check(world.AssaultPeakTargets>=6&&world.AssaultShots>=world.AssaultHits,"Rapid-fire wave records simultaneous targets and individual shot/hit results");
        }
        static IEnumerator CaptureEmbolus()
        {
            keys=new KeyboardState(Key.E);float start=Time.time;
            while(!mission.world.EmbolusCaptured&&Time.time-start<6)
            {
                var camera=mission.mover.viewCamera;var v=camera.WorldToViewportPoint(mission.world.bubble.position);
                if(cursorMode&&(v.z<=0||v.x<.06f||v.x>.94f||v.y<.06f||v.y>.94f)){cursorMode=false;yield return AimAt(mission.world.bubble.position);cursorMode=true;}
                yield return AimAt(mission.world.bubble.position);
            }
            var input=mission.GetComponent<JourneyInput>();checks.Add("Embolus capture diagnostic: step="+mission.StepIndex+" placed="+(mission.world.embolusWave?mission.world.embolusWave.StabilizedCount:0)+" target="+mission.world.bubble.name+" charge="+input.ChargeProgress+" distance="+Vector3.Distance(mission.world.bubble.position,mission.mover.viewCamera.transform.position)+" lock="+(input.LockedTarget?input.LockedTarget.name:"none")+" blocker="+(input.AimObstruction?input.AimObstruction.name:"none"));
            Check(mission.world.EmbolusCaptured&&mission.Current?.action==StudyAction.Deposit,"Actual held E catches the selected moving embolus after charge");
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
            keys=new KeyboardState(Key.F);yield return Wait(.4f);
            var activeTools=mission.GetComponent<JourneyInstrumentRig>();
            if(activeTools)
            {
                Check(UnityEngine.Object.FindObjectsByType<JourneyInstrumentVisual>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==4&&!activeTools.scannerVR.gameObject.activeInHierarchy&&!activeTools.bioVR.gameObject.activeInHierarchy,"Re-installed tools have four total views and no duplicate VR renderers in desktop mode");
                Check(activeTools.ActiveScanner.rotor&&activeTools.ActiveScanner.RotationDegrees>10&&activeTools.ScannerBeamVisible,"Medical Scanner ring rotates and soft short diagnostic beam appears during real F hold");
                Check(activeTools.ActiveScanner.activeRim&&activeTools.ActiveScanner.activeRim.enabled,"Diagnostic sweep highlights the working Scanner rotor");
                ArteryPolishReview.Capture(mission.mover.viewCamera,"ActiveInstruments/01_Scanner_Actual_Input.png");
            }
            Directory.CreateDirectory(ArteryVisualV6.Reports);ScreenCapture.CaptureScreenshot(ArteryVisualV6.Reports+"/05_Scanner_Actual_PC_Input.png");
            yield return Wait(.65f);keys=new KeyboardState();yield return Wait(.12f);Check(mission.StepIndex==2,"F scans the dragged object under the cursor");
            if(activeTools)
            {
                Check(!activeTools.ScannerBeamVisible,"Scanner beam stops after F release");
                foreach(var expected in new[]{BioToolMode.Capture,BioToolMode.Tonus,BioToolMode.Bio,BioToolMode.Impulse})
                {yield return Press(Key.C,.08f);Check(activeTools.modes.CurrentMode==expected&&mission.StepIndex==2,"C cycles actual BioTool mode without changing scan/goal: "+expected);}
            }
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
            var activeTools=mission.GetComponent<JourneyInstrumentRig>();if(activeTools)
            {Check(activeTools.modes.CurrentMode==BioToolMode.Tonus&&activeTools.fieldRing.enabled&&!activeTools.PulseVisible,"TONUS uses a broad field instead of a shot");ArteryPolishReview.Capture(mission.mover.viewCamera,"ActiveInstruments/04_Tonus_Actual_Input.png");}
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

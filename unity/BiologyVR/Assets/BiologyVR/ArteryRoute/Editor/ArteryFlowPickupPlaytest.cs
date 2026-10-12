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
        static JourneyMission mission;static bool running,testingEmbolus,cursorMode,continuousOnly,plaqueOnly,lacerationOnly,persistenceOnly,mobilePersistence,hemodynamicsOnly,freeResearchOnly,organismOnly,waveOnly;
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
        [MenuItem("Biology VR/Test Laceration Eleven Platelets And Activation Through PC Input")]
        public static void RunLaceration()=>Start(false,true,false,false,true);
        [MenuItem("Biology VR/Test Dense Bloodstream Across All Artery Scenes")]
        public static void RunPersistence()=>Start(false,false,false,false,false,true);
        [MenuItem("Biology VR/Test Dense Bloodstream With Mobile Pool Budget")]
        public static void RunMobilePersistence()=>Start(false,false,false,false,false,true,true);
        [MenuItem("Biology VR/Test Hemodynamic Experiment Through PC Input")]
        public static void RunHemodynamics()=>Start(false,true,false,false,false,false,false,true);
        [MenuItem("Biology VR/Test All Free Blood Research Orders Through PC Input")]
        public static void RunFreeResearch()=>Start(false,true,false,false,false,false,false,false,true);
        [MenuItem("Biology VR/Test Connected Living Organism Through PC Input")]
        public static void RunOrganism()=>Start(false,true,false,false,false,false,false,false,false,true);
        [MenuItem("Biology VR/Test Connected Viral Wave Through PC Input")]
        public static void RunImmuneWave()=>Start(false,true,false,false,false,false,false,false,false,true,true);
        static void Start(bool embolus,bool cursor=false,bool continuous=false,bool plaque=false,bool laceration=false,bool persistence=false,bool mobilePool=false,bool hemodynamics=false,bool freeResearch=false,bool organism=false,bool wave=false)
        {
            mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();
            if(!EditorApplication.isPlaying||running||!mission||(!embolus&&(mission.SceneNumber!=3||mission.StepIndex!=0)))throw new InvalidOperationException("Fresh Scene03 required: playing="+EditorApplication.isPlaying+" running="+running+" scene="+(mission?mission.SceneNumber:-1)+" step="+(mission?mission.StepIndex:-1));
            testingEmbolus=embolus;cursorMode=cursor;continuousOnly=continuous;plaqueOnly=plaque;lacerationOnly=laceration;persistenceOnly=persistence;mobilePersistence=mobilePool;hemodynamicsOnly=hemodynamics;freeResearchOnly=freeResearch;organismOnly=organism;waveOnly=wave;running=true;EditorCoroutineUtility.StartCoroutineOwnerless(Guard(Test()));
        }
        static void Check(bool success,string text)
        {checks.Add((success?"PASS: ":"BLOCKED: ")+text);if(organismOnly){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Connected_Organism_Progress.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,running,checks},Formatting.Indented));}if(!success)throw new InvalidOperationException(text);}
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
                string output=waveOnly?"/Connected_Wave_PC.json":organismOnly?"/Connected_Organism_PC.json":freeResearchOnly?(mission.world.bloodEntities?"/Connected_Research_Orders_PC.json":"/Free_Blood_Research_PC_Input.json"):hemodynamicsOnly?"/Hemodynamics_PC_Input.json":mobilePersistence?"/Mobile_All_Scenes_Persistent_Blood.json":persistenceOnly?"/All_Scenes_Persistent_Blood.json":lacerationOnly?"/Laceration_PC_Input.json":plaqueOnly?"/Plaque_Challenge_PC.json":continuousOnly?"/Continuous_Random_Flow.json":cursorMode?"/NativeCursor_PC_Input.json":testingEmbolus?"/Scene07_PC_Input.json":"/PC_Input.json";
                string method=freeResearchOnly?(mission.world.bloodEntities?"120 five-type permutations at timeScale 2; real MouseState/KeyboardState; explicit Enter/release fixtures between orders, normal partial re-entry, no success APIs":"Fresh Scene03; real mouse/keyboard/XRI. Explicit Enter(3,false,true) fixtures reset only between 24 permutations; normal Enter fixtures test partial re-entry; no mission success API calls"):hemodynamicsOnly?"Isolated Scene05/10 entry fixtures; actual Scanner F, held E/wheel TONUS, XRI physical card placement and normal Continue; no success APIs":persistenceOnly?"Isolated scene-entry fixtures 03–16 with normal travel; actual RMB mouse look, 90-second final-stage dwell; no mission success APIs":lacerationOnly?"Isolated Enter(11,false); real held E field capture/automatic adhesion, scanner and XRI biochemical cards; normal Continue; no success APIs":plaqueOnly?"Isolated Enter(6,false) fixture; real mouse/keyboard scan, shots, failure, automatic local retry and Continue; no success APIs":continuousOnly?"240 simulated seconds at timeScale 5; real forward/back mouse look; no scene reset or mission success APIs":cursorMode?"Native MouseState position/buttons/scroll and KeyboardState; isolated episode fixtures; no success APIs":testingEmbolus?"Isolated Scene07 fixture: all actions through keyboard/mouse input":"Actual keyboard/mouse Input System events; no mission success/enter APIs";
                Directory.CreateDirectory(Folder);File.WriteAllText(Folder+output,JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,passed=!checks.Exists(c=>c.StartsWith("BLOCKED")),mobilePoolBudget=mobilePersistence?180:360,method,checks},Formatting.Indented));
                if(organismOnly)File.WriteAllText(Folder+"/Connected_Organism_Progress.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,running,checks},Formatting.Indented));
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
                if(waveOnly){mission.Enter(9,false);yield return Wait(.4f);yield return TestConnectedWave();yield break;}
                if(organismOnly){yield return TestOrganism();yield break;}
                if(freeResearchOnly){if(mission.world.bloodEntities)yield return TestConnectedResearchOrders();else yield return TestFreeResearch();yield break;}
                if(hemodynamicsOnly){yield return TestHemodynamics();yield break;}
                if(persistenceOnly){yield return TestPersistentAllScenes();yield break;}
                if(lacerationOnly){yield return TestLaceration();yield break;}
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
            var persistent=mission.world.GetComponent<JourneyContinuousBloodstream>();float plaqueArc=mission.mover.path.Anchor(6)+5;var persistentCells=persistent.MainCellsBetween(plaqueArc-20,plaqueArc+20).ToArray();var persistentPoses=persistentCells.Select(c=>c.position).ToArray();yield return Wait(.8f);
            Check(persistentCells.Length>=25&&persistentCells.Where((c,i)=>Vector3.Distance(c.position,persistentPoses[i])>.3f).Count()>=20,"Even a failed occlusion attempt preserves dense continuously moving background blood");
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
        static IEnumerator NativePlace(GameObject go,Vector3 destination)
        {
            cursorMode=false;yield return AimAt(go.transform.position);cursorMode=true;yield return AimAt(go.transform.position);buttons=1;yield return Wait(.25f);
            var grab=go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if(!grab||!grab.isSelected){var desktop=mission.GetComponent<JourneyInput>().DesktopControls;checks.Add("Placement diagnostic: item="+go.name+" active="+go.activeInHierarchy+" position="+go.transform.position+" grabEnabled="+(grab&&grab.enabled)+" hover="+(desktop.HoveredGrab()?desktop.HoveredGrab().name:"none")+" uiHover="+mission.GetComponent<JourneyHolographicScreens>().PointerOverTheory+" pointer="+pointer+" camera="+mission.mover.viewCamera.transform.position+" rayHits="+string.Join(",",Physics.RaycastAll(desktop.PointerRay(),35,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance.ToString("F2"))));}
            Check(grab&&grab.isSelected,"Actual LMB catches moving/physical item: "+go.name);
            cursorMode=false;yield return AimAt(destination);cursorMode=true;yield return AimAt(destination);
            var controls=mission.GetComponent<JourneyInput>().DesktopControls;float distance=Vector3.Distance(mission.mover.viewCamera.transform.position,destination);
            pendingScroll=new Vector2(0,Mathf.Round((distance-controls.GrabDepth)/JourneyDesktopControls.DepthPerWheelStep)*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.55f);buttons=0;yield return Wait(.35f);
        }
        static IEnumerator TestLaceration()
        {
            mission.Enter(11,false);yield return Wait(.4f);var world=mission.world;var puzzle=world.hemostasisPuzzle;Check(puzzle.flowLayering&&puzzle.RequiredPlatelets==11,"Layering lesson requires eleven flow platelets");
            Directory.CreateDirectory(ArteryPolishReview.Reports+"/LacerationHemostasis");cursorMode=false;yield return AimAt(world.woundSite.position);cursorMode=true;
            var stream=world.GetComponent<JourneyContinuousBloodstream>();float savedTimeScale=Time.timeScale;
            try{Time.timeScale=6;float began=Time.time;while((stream.WoundCandidates<24||stream.WoundLeakedTotal==0)&&Time.time-began<150)yield return Wait(1);}
            finally{Time.timeScale=savedTimeScale;}
            Check(world.woundState.splitMainFlow&&stream.WoundLeakedTotal>0&&stream.WoundContinuedTotal>stream.WoundLeakedTotal,"Real primary-pool cells split: some exit through the tear while most continue downstream");
            checks.Add("Split-flow totals: main="+stream.WoundContinuedTotal+" leak="+stream.WoundLeakedTotal+" candidates="+stream.WoundCandidates);
            yield return ReviewWoundFlow();
            ArteryPolishReview.Capture(mission.mover.viewCamera,"LacerationHemostasis/01_Jagged_Tear_And_Outward_Blood.png");
            yield return AimAt(world.Find("wound").GetComponent<Collider>().bounds.center);yield return Press(Key.F,1.05f);
            yield return AimAt(world.Find("leak").GetComponent<Collider>().bounds.center);yield return Press(Key.E,.1f);
            yield return AimAt(world.Find("pressure-low").GetComponent<Collider>().bounds.center);yield return Press(Key.E,.1f);Check(mission.Complete,"Scanner and BioTool diagnose the actual wound before layering");
            yield return Press(Key.RightArrow,.1f);yield return Wait(4f);Check(mission.SceneNumber==12&&mission.Ready,"Normal Continue enters same-wound platelet lesson");
            var before=puzzle.platelets.Take(11).Select(p=>p.transform.position).ToArray();yield return Wait(.4f);Check(puzzle.platelets.Take(11).Where((p,i)=>Vector3.Distance(before[i],p.transform.position)>.06f).Count()==11,"All eleven required platelets drift in blood rather than on a stationary tray");
            yield return NativePlace(puzzle.platelets[0].gameObject,puzzle.Slot(0));Check(puzzle.Attached==0&&mission.Progress==0,"Manual release does not mechanically impale a platelet or bypass BioTool adhesion");yield return Wait(.75f);
            for(int i=0;i<11;i++)
            {yield return NativeCapturePlatelet(puzzle.platelets[i],puzzle.Slot(i));Check(puzzle.Attached==i+1&&puzzle.platelets[i].Placed,"Held BioTool guides platelet "+(i+1)+" / 11; adhesion and shape change are automatic");if(i==5)ArteryPolishReview.Capture(mission.mover.viewCamera,"LacerationHemostasis/02_Platelet_Layers_From_Flow.png");}
            keys=new KeyboardState();yield return Wait(.4f);Check(mission.StepIndex==1&&!puzzle.Activated&&!mission.Complete&&world.FibrinGrowth==0&&world.woundState.LeakStrength>.25f&&puzzle.activationPuzzle.panel.activeInHierarchy,"11/11 is only a loose primary plug: fibrin absent and residual bleeding persists");
            var activation=puzzle.activationPuzzle;ArteryPolishReview.Capture(mission.mover.viewCamera,"LacerationHemostasis/03_Activation_Sequence_Game.png");
            yield return AimAt(world.woundSite.position);yield return Press(Key.E,.15f);Check(!puzzle.Activated&&world.FibrinGrowth==0&&!mission.Complete,"One BioTool click cannot stabilize or heal the wound");
            yield return NativePlace(activation.tokens[3].gameObject,activation.slots[0].position);Check(activation.Mistakes>0&&activation.Progress==0&&!puzzle.Activated,"EDTA distractor is rejected because calcium chelation inhibits coagulation");
            for(int i=0;i<3;i++){yield return NativePlace(activation.tokens[i].gameObject,activation.slots[i].position);Check(activation.Progress==i+1,"Actual card release solves biochemical sequence step "+(i+1));}
            yield return Wait(.5f);Check(activation.Solved&&puzzle.Activated&&!mission.Complete&&world.FibrinGrowth>0&&world.woundState.LeakStrength>.08f,"Correct activation/aggregation/thrombin-fibrin sequence starts gradual stabilization, not instantaneous closure");
            float partial=world.woundState.LeakStrength;yield return Wait(7f);Check(puzzle.StabilizationComplete&&mission.Complete&&world.woundState.LeakStrength<.025f&&world.woundState.LeakStrength<partial,"Fibrin net forms over time and only then bleeding gradually ceases");ArteryPolishReview.Capture(mission.mover.viewCamera,"LacerationHemostasis/04_Activated_Layers_And_Healing_Start.png");
            cursorMode=false;yield return AimAt(world.woundSite.position);cursorMode=true;CaptureClear("08_Stable_Fibrin_Endothelium_Not_Yet_Healed.png");
            var site=world.woundSite;var poses=puzzle.platelets.Take(11).Select(p=>p.transform.position).ToArray();yield return Press(Key.RightArrow,.1f);yield return Wait(4f);Check(mission.SceneNumber==13&&world.woundSite==site&&puzzle.Attached==11&&puzzle.platelets.Take(11).Where((p,i)=>Vector3.Distance(p.transform.position,poses[i])<.01f).Count()==11,"Continue preserves the same tear and eleven hand-placed layers for balance stage");
        }
        static IEnumerator TestPersistentAllScenes()
        {
            var world=mission.world;var stream=world.GetComponent<JourneyContinuousBloodstream>();var flow=world.GetComponent<JourneyBloodFlow>();var path=mission.mover.path;float scale=Time.timeScale;
            Directory.CreateDirectory(ArteryPolishReview.Reports+"/PersistentBlood");
            if(mobilePersistence)flow.SetVisibleCellLimit(180);
            try
            {
                Time.timeScale=4;
                for(int scene=3;scene<=16;scene++)
                {
                    if(scene!=3)mission.Enter(scene,true);
                    while(!mission.Ready)yield return Wait(.25f);
                    yield return Wait(2f);cursorMode=false;float arc=mission.mover.Distance;
                    yield return AimAt(path.Centre(Mathf.Min(path.Length,arc+18)));yield return Wait(.4f);int ahead=stream.VisibleMovingCells;
                    var cells=stream.MainCellsBetween(arc-35,arc+35).ToArray();var poses=cells.Select(c=>c.position).ToArray();yield return Wait(1f);
                    int moving=cells.Where((c,i)=>Vector3.Distance(poses[i],c.position)>.3f).Count();
                    yield return AimAt(path.Centre(Mathf.Max(0,arc-18)));yield return Wait(.4f);int behind=stream.VisibleMovingCells;
                    checks.Add("Scene "+scene+": nearbyMain="+stream.NearbyMainCells+" front="+ahead+" back="+behind+" moving="+moving+" palette="+stream.CurrentSpeciesCounts.Count(c=>c>0)+" refills="+stream.DensityRefills+" simulated="+stream.SimulatedSeconds+" streamEnabled="+stream.isActiveAndEnabled+" flowEnabled="+flow.isActiveAndEnabled+" flowMover="+(flow.mover?flow.mover.name:"none")+" flowCamera="+(flow.mover&&flow.mover.viewCamera?flow.mover.viewCamera.name:"none")+" camera="+mission.mover.viewCamera.transform.position+" arc="+arc+" cell6="+flow.cells[6].position+" cell6active="+flow.cells[6].gameObject.activeInHierarchy);
                    Check(stream.NearbyMainCells>=(mobilePersistence?45:90)&&ahead>=12&&behind>=12&&moving>=(mobilePersistence?35:75),"Scene "+scene+" keeps a dense moving pool in both directions after normal travel");
                    Check(stream.CurrentSpeciesCounts.All(c=>c>0),"Scene "+scene+" retains every existing blood visual species");
                    if(scene==3||scene==11||scene==15||scene==16)
                    {
                        yield return AimAt(path.Centre(Mathf.Min(path.Length,arc+18)));
                        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var states=canvases.Select(c=>c.enabled).ToArray();
                        try{foreach(var canvas in canvases)canvas.enabled=false;ArteryPolishReview.Capture(mission.mover.viewCamera,"PersistentBlood/Scene"+scene.ToString("00")+(mobilePersistence?"_Mobile_Dense_Flow.png":"_Dense_Flow.png"));}
                        finally{for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=states[i];}
                    }
                }
                float start=Time.time;int minNear=int.MaxValue,minVisible=int.MaxValue;
                while(Time.time-start<90)
                {yield return Wait(.5f);minNear=Mathf.Min(minNear,stream.NearbyMainCells);minVisible=Mathf.Min(minVisible,stream.VisibleMovingCells);}
                checks.Add("Scene16 ninety-second dwell: minimumNearby="+minNear+" minimumVisible="+minVisible);
                Check(minNear>=(mobilePersistence?45:90)&&minVisible>=12,"Final-stage circulation remains saturated during prolonged dwell rather than exhausting");
            }
            finally{Time.timeScale=scale;}
        }
        static IEnumerator ReviewWoundFlow()
        {
            var world=mission.world;var path=mission.mover.path;float arc=path.Anchor(11)+5;var centre=path.Centre(arc);var forward=path.Forward(arc);var flow=world.GetComponent<JourneyBloodFlow>();
            var stream=world.GetComponent<JourneyContinuousBloodstream>();
            var zones=new[]{stream.MainCellsBetween(arc-12,arc-3).ToArray(),stream.MainCellsBetween(arc-3,arc+3).ToArray(),stream.MainCellsBetween(arc+3,arc+14).ToArray()};
            var poses=zones[2].Select(c=>c.position).ToArray();yield return Wait(1f);
            Check(zones[0].Length>0&&zones[1].Length>0&&zones[2].Length>0&&zones[2].Where((c,i)=>Vector3.Dot(c.position-poses[i],forward)>.05f).Any(),"Existing pool retains moving blood before, at and downstream of the defect");
            checks.Add("Wound-zone primary-pool elements: before="+zones[0].Length+" at="+zones[1].Length+" downstream="+zones[2].Length);
            cursorMode=false;yield return AimAt(path.Centre(arc+14));cursorMode=true;CaptureClear("05_Overall_Main_Stream_Before_Tear.png");
            cursorMode=false;yield return AimAt(world.woundSite.position);cursorMode=true;CaptureClear("06_Tear_And_Shared_Pool_Diversion.png");
            cursorMode=false;yield return AimAt(path.Centre(arc+9));cursorMode=true;CaptureClear("07_Continuing_Downstream_Blood.png");
            cursorMode=false;yield return AimAt(world.woundSite.position);cursorMode=true;
        }
        static void CaptureClear(string file)
        {
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var states=canvases.Select(c=>c.enabled).ToArray();
            try{foreach(var canvas in canvases)canvas.enabled=false;ArteryPolishReview.Capture(mission.mover.viewCamera,"LacerationHemostasis/"+file);}
            finally{for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=states[i];}
        }
        static IEnumerator NativeCapturePlatelet(JourneyHemostasisPiece item,Vector3 destination)
        {
            var input=mission.GetComponent<JourneyInput>();var field=input.plateletCapture;
            if(Vector3.Distance(item.transform.position,mission.mover.viewCamera.transform.position)>9)
            {
                keys=new KeyboardState();cursorMode=false;var camera=mission.mover.viewCamera;yield return AimAt(camera.transform.position+mission.mover.path.Up(mission.mover.Distance)*4);cursorMode=true;
                float waiting=Time.time;while(Vector3.Distance(item.transform.position,camera.transform.position)>9&&Time.time-waiting<15)yield return Wait(.1f);
                Check(Vector3.Distance(item.transform.position,camera.transform.position)<9,"Distant platelet returns to the catch zone only after leaving the view");
            }
            cursorMode=false;yield return AimAt(item.transform.position);cursorMode=true;keys=new KeyboardState(Key.E);float began=Time.time;
            while(!field.Holding&&Time.time-began<3)yield return AimAt(item.transform.position);
            checks.Add("Platelet capture diagnostic: index="+item.index+" distance="+Vector3.Distance(item.transform.position,mission.mover.viewCamera.transform.position)+" progress="+field.CaptureProgress+" held="+(field.HeldPiece?field.HeldPiece.index:-1)+" placed="+item.Placed+" contact="+(input.LockedContact?input.LockedContact.name:"none")+" blocker="+(input.AimObstruction?input.AimObstruction.name:"none")+" step="+mission.StepIndex+" mode="+input.GetComponent<BioToolModeController>().CurrentMode);
            Check(field.Holding&&field.HeldPiece==item,"Actual held E captures specific moving platelet: "+item.index);
            cursorMode=false;yield return AimAt(destination);cursorMode=true;yield return AimAt(destination);
            float depth=Vector3.Distance(mission.mover.viewCamera.transform.position,destination);pendingScroll=new Vector2(0,Mathf.Round((depth-field.FieldDistance)/JourneyDesktopControls.DepthPerWheelStep)*JourneyDesktopControls.WheelUnitsPerStep);
            began=Time.time;while(!item.Placed&&Time.time-began<6)yield return Wait(.08f);
            keys=new KeyboardState();yield return Wait(.15f);Check(item.Placed&&!field.Holding,"Near-field platelet adheres to exposed endothelium without release or a healing click");
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
                ushort held=(ushort)(buttons&1);pendingDelta=new Vector2(yaw/.12f,-pitch/.12f);buttons=(ushort)(held|2);yield return Wait(.10f);buttons=held;yield return Wait(.05f);
            }
            yield return Wait(.05f);
        }
        static IEnumerator TestCursor()
        {
            if(mission.FreeBloodResearch){yield return TestFreeResearchEntry();yield break;}
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
        static IEnumerator TestFreeResearchEntry()
        {
            var research=mission.world.research;var white=mission.world.Find("leukocyte");cursorMode=false;yield return AimAt(white.transform.position);cursorMode=true;yield return AimAt(white.transform.position);buttons=1;yield return Wait(.3f);
            Check(white.grabbed&&research.Find("leukocyte").State==JourneyResearchState.Investigating&&research.RequiredInvestigated==0,"Actual first LMB catch opens leukocyte research without requiring an erythrocyte or granting premature completion");
            yield return ResearchScan("leukocyte");buttons=0;yield return Wait(.9f);
            Check(!white.grabbed&&research.RequiredInvestigated==1,"Release preserves completed research and returns the cell to the existing moving pool");
        }
        static IEnumerator ResearchScan(string id)
        {
            var target=mission.world.Find(id);var camera=mission.mover.viewCamera;var research=mission.world.research;
            if(Vector3.Distance(target.transform.position,camera.transform.position)>15)
            {
                cursorMode=false;yield return AimAt(camera.transform.position+mission.mover.path.Up(mission.mover.Distance)*5);cursorMode=true;float waiting=Time.time;
                while(Vector3.Distance(target.transform.position,camera.transform.position)>15&&Time.time-waiting<25)yield return Wait(.08f);
            }
            cursorMode=false;yield return AimAt(target.GetComponent<Collider>().bounds.center);cursorMode=true;keys=new KeyboardState();float began=Time.time,hold=0;int scans=research.UniqueScans;bool repeated=research.Find(id).State==JourneyResearchState.Investigated;
            var input=mission.GetComponent<JourneyInput>();
            while(Time.time-began<15&&hold<1.05f)
            {
                pointer=camera.WorldToScreenPoint(target.GetComponent<Collider>().bounds.center);yield return Wait(.03f);
                // Wait for a genuine clear ray to this moving cell. Overlapping
                // species must not be mistaken for an ordered-object restriction.
                if(input.LockedTarget==target){keys=new KeyboardState(Key.F);hold+=.03f;}
                else{keys=new KeyboardState();hold=0;}
                if(!repeated&&research.Find(id).State==JourneyResearchState.Investigated&&research.Focused==research.Find(id))break;
            }
            keys=new KeyboardState();yield return Wait(.12f);
            if(research.Find(id).State!=JourneyResearchState.Investigated||research.Focused!=research.Find(id))checks.Add("Research scan diagnostic: id="+id+" actual="+(research.Focused?research.Focused.theory.id:"none")+" lock="+(input.LockedTarget?input.LockedTarget.name:"none")+" blocker="+(input.AimObstruction?input.AimObstruction.name:"none")+" distance="+Vector3.Distance(target.transform.position,camera.transform.position));
            Check(research.Find(id).State==JourneyResearchState.Investigated&&research.Focused==research.Find(id)&&research.UniqueScans==scans+(repeated?0:1),"Actual Scanner independently investigates or reopens: "+id);
            Check(mission.LearningTerm==research.Find(id).theory.displayName&&mission.GetComponent<JourneyHolographicScreens>().theoryTitle.text==mission.LearningTerm,"Theory belongs to selected object: "+id);
        }
        static IEnumerator TestFreeResearch()
        {
            var research=mission.world.research;var screens=mission.GetComponent<JourneyHolographicScreens>();Directory.CreateDirectory(FreeBloodResearchBuilder.Reports);
            Check(research&&research.objects.Length==8&&research.RequiredCount==4&&research.RequiredInvestigated==0&&research.objects.All(o=>o.State==JourneyResearchState.NotInvestigated),"All eight existing objects start independently; only four main blood components are mandatory");
            yield return Wait(.35f);Check(research.RequiredInvestigated==0&&research.Introductions==0,"Proximity and passive hovering never investigate an object or start a professor lecture");
            yield return Press(Key.RightArrow,.1f);Check(mission.SceneNumber==3&&!mission.Complete,"Continue is unavailable before all mandatory research");
            yield return TestFreeResearchEntry();ArteryPolishReview.Capture(mission.mover.viewCamera,"FreeBloodResearch/01_Leukocyte_First.png");
            int intro=research.Introductions;yield return ResearchScan("leukocyte");Check(research.RequiredInvestigated==1&&research.Introductions==intro,"Repeat scan opens the card without duplicated progress or tutorial introduction");
            var buttonRect=screens.referenceButton.GetComponent<RectTransform>();pointer=mission.mover.viewCamera.WorldToScreenPoint(buttonRect.TransformPoint(buttonRect.rect.center));yield return Wait(.1f);buttons=1;yield return Wait(.12f);buttons=0;yield return Wait(.2f);
            Check(research.Focused.Page==1&&research.RequiredInvestigated==1,"Actual More button reveals the next short theory block without changing research progress");
            yield return ResearchScan("plasma");var snapshot=research.Capture().Select(s=>s.id+":"+s.state+":"+s.page).ToArray();int partial=research.RequiredInvestigated;
            mission.Enter(4,false);yield return Wait(.2f);mission.Enter(3,false);yield return Wait(.3f);Check(partial==2&&research.RequiredInvestigated==2&&snapshot.SequenceEqual(research.Capture().Select(s=>s.id+":"+s.state+":"+s.page)),"Normal stage re-entry preserves independent partial states and theory pages");
            mission.Enter(3,false,true);yield return Wait(.3f);
            foreach(var id in new[]{"oxygen","hemoglobin","heme","co2"})yield return ResearchScan(id);
            Check(research.RequiredInvestigated==0&&research.OptionalInvestigated==4&&!mission.Complete&&research.Find("rbc").State==JourneyResearchState.NotInvestigated,"Oxygen, hemoglobin, heme/Fe²⁺ and CO2 can all be investigated before erythrocyte and do not replace mandatory components");
            Check(research.Find("heme").theory.pages.Any(p=>p.text.Contains("Fe²⁺"))&&research.Find("co2").theory.pages.Any(p=>p.text.Contains("HCO₃⁻"))&&research.Find("plasma").theory.pages.Any(p=>p.text.Contains("Электролиты")),"Molecular data includes heme iron, bicarbonate gas transport and plasma electrolytes");ArteryPolishReview.Capture(mission.mover.viewCamera,"FreeBloodResearch/02_Molecular_Details_Before_RBC.png");
            string[] core={"rbc","leukocyte","platelet","plasma"};int orders=0;
            foreach(var a in core)foreach(var b in core.Where(x=>x!=a))foreach(var c in core.Where(x=>x!=a&&x!=b))
            {
                var d=core.First(x=>x!=a&&x!=b&&x!=c);var order=new[]{a,b,c,d};mission.Enter(3,false,true);yield return Wait(.2f);
                for(int i=0;i<order.Length;i++){yield return ResearchScan(order[i]);Check(research.RequiredInvestigated==i+1&&mission.Complete==(i==3),"Independent completion count for order "+string.Join(" → ",order)+" at "+(i+1)+"/4");}
                orders++;int before=research.UniqueScans;int announced=research.Introductions;yield return ResearchScan(order[0]);Check(research.UniqueScans==before&&research.Introductions==announced&&research.RequiredInvestigated==4,"Completed research remains idempotent in order "+orders);
                yield return Wait(.2f);Check(mission.SceneNumber==3&&screens.hud.continueButton.gameObject.activeInHierarchy,"Last object unlocks Continue without auto-transition in order "+orders);
            }
            Check(orders==24,"All twenty-four permutations passed through actual Scanner input");ArteryPolishReview.Capture(mission.mover.viewCamera,"FreeBloodResearch/03_Four_Components_Complete.png");
            var blood=mission.world.GetComponent<JourneyContinuousBloodstream>();Check(Time.timeScale>0&&blood.NearbyMainCells>=90,"World, wall and saturated bloodstream continue during research and theory");
            yield return Press(Key.RightArrow,.12f);yield return Wait(10f);Check(mission.SceneNumber==4&&mission.Ready&&mission.world.IsEpisodeCompleted(3),"Actual Continue proceeds to existing wall lesson and remembers completed free research");
        }
        static Transform FindBackgroundType(string type)
        {
            var stream=mission.world.bloodEntities.stream;var camera=mission.mover.viewCamera.transform;Transform result=null;float best=10;
            foreach(var slot in mission.world.bloodEntities.slots)if(slot.index>=0&&slot.type==type&&slot.contact.enabled&&ClearBloodSight(slot.target.transform)&&(type!="phagocyte"||slot.target.GetComponent<JourneyPhagocyteEntity>().RemainingCapacity>0&&!slot.target.GetComponent<JourneyPhagocyteEntity>().Busy)){float distance=Vector3.Distance(camera.position,slot.target.transform.position);if(distance<best){best=distance;result=slot.target.transform;}}
            for(int i=0;i<stream.flow.VisibleCellLimit;i++)
            {
                var cell=stream.flow.cells[i];if(stream.Leased(i)||!cell.GetComponent<Renderer>().enabled||JourneyBloodInteractionPool.ResearchType(stream.flow.species[i])!=type||type=="phagocyte"&&!mission.world.bloodEntities.HasPhagocyteCapacity(i))continue;
                float distance=Vector3.Distance(camera.position,cell.position);if(distance<best&&distance>1.4f&&ClearBloodSight(cell)){best=distance;result=cell;}
            }
            return result;
        }
        static bool ClearBloodSight(Transform target)
        {
            var camera=mission.mover.viewCamera.transform;var delta=target.position-camera.position;var owner=target.GetComponent<JourneyTarget>();
            foreach(var hit in Physics.RaycastAll(new Ray(camera.position,delta.normalized),delta.magnitude,~0,QueryTriggerInteraction.Collide))
            {if(hit.collider.transform.IsChildOf(mission.mover.origin.transform)||hit.collider.GetComponentInParent<JourneyTarget>()==owner&&owner)continue;if(hit.collider.isTrigger&&!hit.collider.GetComponentInParent<JourneyTarget>())continue;if(hit.distance<delta.magnitude-.15f)return false;}return true;
        }
        static IEnumerable<string[]> ResearchPermutations(string[] remaining)
        {
            if(remaining.Length==0){yield return new string[0];yield break;}
            foreach(var first in remaining)foreach(var rest in ResearchPermutations(remaining.Where(id=>id!=first).ToArray()))yield return new[]{first}.Concat(rest).ToArray();
        }
        static IEnumerator TestConnectedResearchOrders()
        {
            var world=mission.world;var research=world.research;float timeScale=Time.timeScale;int orders=0;
            try
            {
                Time.timeScale=2;
                foreach(var order in ResearchPermutations(new[]{"phagocyte","rbc","leukocyte","platelet","plasma"}))
                {
                    world.analysis.Close();foreach(var slot in world.bloodEntities.slots)world.bloodEntities.Release(slot.target,true);
                    mission.Enter(3,false,true);yield return Wait(.2f);
                    Check(research.RequiredInvestigated==0&&research.OptionalInvestigated==0&&world.analysis.molecules.All(t=>!t.gameObject.activeInHierarchy),"Independent empty journal and no ambient molecules at order "+(orders+1));
                    yield return Press(Key.RightArrow,.06f);Check(mission.SceneNumber==3&&!mission.Complete,"Continue is gated before research in order "+(orders+1));
                    for(int i=0;i<order.Length;i++)
                    {
                        yield return ScanBloodType(order[i]);if(research.RequiredInvestigated!=i+1)checks.Add("Order count diagnostic: "+string.Join(",",research.Capture().Select(s=>s.id+":"+s.state)));Check(research.RequiredInvestigated==i+1&&mission.Complete==(i==4),"Exact independent progress "+(i+1)+"/5: "+string.Join(" → ",order));
                        if(orders==0&&i==1){var snapshot=research.Capture().Select(s=>s.id+":"+s.state+":"+s.page).ToArray();mission.Enter(4,false);yield return Wait(.2f);mission.Enter(3,false);yield return Wait(.2f);Check(research.RequiredInvestigated==2&&snapshot.SequenceEqual(research.Capture().Select(s=>s.id+":"+s.state+":"+s.page)),"Normal partial stage re-entry preserves journal states and pages");}
                    }
                    int before=research.UniqueScans,announced=research.Introductions;yield return ScanBloodType(order[0]);
                    Check(research.UniqueScans==before&&research.Introductions==announced&&research.RequiredInvestigated==5&&mission.SceneNumber==3,"Repeat investigation is idempotent and never auto-transitions in order "+(orders+1));
                    orders++;File.WriteAllText(Folder+"/Connected_Research_Orders_Progress.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,running=true,orders,total=120,lastOrder=order},Formatting.Indented));
                }
                Check(orders==120,"All 120 five-population orders complete through actual Scanner input");
            }
            finally{Time.timeScale=timeScale;File.WriteAllText(Folder+"/Connected_Research_Orders_Progress.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,running=false,orders,total=120},Formatting.Indented));}
            yield return Press(Key.RightArrow,.12f);yield return Wait(10);Check(mission.SceneNumber==4&&mission.Ready&&world.IsEpisodeCompleted(3),"Normal Continue follows completed five-type research to the original vessel-wall lesson");
        }
        static IEnumerator ScanBloodType(string type)
        {
            Transform candidate=null;float began=Time.time;mission.world.bloodEntities.stream.RequestInflux(type=="rbc"?"erythrocyte":type=="plasma"?"albumin":type);
            if(!FindBackgroundType(type)){cursorMode=false;yield return AimAt(mission.mover.viewCamera.transform.position+mission.mover.path.Forward(mission.mover.Distance)*5);cursorMode=true;}
            float requested=Time.time;
            while(!(candidate=FindBackgroundType(type))&&Time.time-began<60){if(Time.time-requested>6){mission.world.bloodEntities.stream.RequestInflux(type=="rbc"?"erythrocyte":type=="plasma"?"albumin":type);cursorMode=false;yield return AimAt(mission.mover.viewCamera.transform.position+mission.mover.path.Forward(mission.mover.Distance)*5);cursorMode=true;requested=Time.time;}yield return Wait(.15f);}
            Check(candidate,"A concrete "+type+" enters from the ordinary blood population");
            if(freeResearchOnly){yield return ScanResearchTypeHeld(type,candidate);yield break;}
            cursorMode=false;yield return AimAt(candidate.position);cursorMode=true;keys=new KeyboardState();began=Time.time;
            while(Time.time-began<25)
            {
                if(!candidate.gameObject.activeInHierarchy||!ClearBloodSight(candidate)){keys=new KeyboardState();var next=FindBackgroundType(type);if(next)candidate=next;}
                JourneyTarget promoted=null;float closest=10;foreach(var slot in mission.world.bloodEntities.slots)if(slot.type==type&&slot.index>=0&&ClearBloodSight(slot.target.transform)&&(type!="phagocyte"||slot.target.GetComponent<JourneyPhagocyteEntity>().RemainingCapacity>0&&!slot.target.GetComponent<JourneyPhagocyteEntity>().Busy)){float distance=Vector3.Distance(slot.target.transform.position,mission.mover.viewCamera.transform.position);if(distance<closest){closest=distance;promoted=slot.target;}}
                var targetPoint=promoted?promoted.GetComponent<Collider>().bounds.center:candidate.position;var viewport=mission.mover.viewCamera.WorldToViewportPoint(targetPoint);
                if(viewport.z<=0||viewport.x<.05f||viewport.x>.95f||viewport.y<.05f||viewport.y>.95f){keys=new KeyboardState();cursorMode=false;yield return AimAt(targetPoint);cursorMode=true;}
                pointer=mission.mover.viewCamera.WorldToScreenPoint(targetPoint);yield return Wait(.03f);
                var ray=mission.GetComponent<JourneyInput>().DesktopControls.PointerRay();bool correct=false;float nearest=12;
                foreach(var slot in mission.world.bloodEntities.slots)if(slot.index>=0&&slot.contact.enabled&&slot.contact.Raycast(ray,out var contact,nearest)){nearest=contact.distance;correct=slot.type==type;}
                if(nearest==12&&mission.world.bloodEntities.stream.Pick(ray,12,out int picked,out _))correct=JourneyBloodInteractionPool.ResearchType(mission.world.bloodEntities.stream.flow.species[picked])==type;
                var liveInput=mission.GetComponent<JourneyInput>();if(liveInput.ScannerBlockedByUi){keys=new KeyboardState();yield return Wait(.08f);cursorMode=false;yield return AimAt(targetPoint);cursorMode=true;}
                keys=new KeyboardState(Key.F);
                if(mission.SceneNumber==3&&mission.world.research.Find(type).State==JourneyResearchState.Investigated&&mission.world.research.Focused?.theory.id==type&&mission.GetComponent<JourneyInput>().ScanProgress>=.99f)break;
                if(mission.SceneNumber!=3&&promoted&&mission.GetComponent<JourneyInput>().LockedTarget==promoted&&promoted.GetComponent<JourneyPhagocyteEntity>()?.State==JourneyPhagocyteState.Activated)break;
            }
            keys=new KeyboardState();yield return Wait(.15f);
            if(mission.SceneNumber!=3&&!mission.world.immunity.phagocytes.Any(c=>c.State==JourneyPhagocyteState.Activated&&c.gameObject.activeInHierarchy&&c.RemainingCapacity>0)){var input=mission.GetComponent<JourneyInput>();checks.Add("Late activation diagnostic: hit="+(input.LockedTarget?input.LockedTarget.name:"none")+" step="+mission.StepIndex+" blocked="+input.ScannerBlockedByUi+" obstacle="+(input.AimObstruction?input.AimObstruction.name:"none")+" candidate="+candidate.position+" phagStates="+string.Join(",",mission.world.immunity.phagocytes.Select(c=>c.name+":"+c.State+":"+c.RemainingCapacity+":"+c.gameObject.activeSelf)));}
            if(mission.SceneNumber==3&&mission.world.research.Find(type).State!=JourneyResearchState.Investigated){var input=mission.GetComponent<JourneyInput>();checks.Add("Live type scan diagnostic: requested="+type+" focused="+(mission.world.research.Focused?mission.world.research.Focused.theory.id:"none")+" lock="+(input.LockedTarget?input.LockedTarget.name:"none")+" progress="+input.ScanProgress+" canInvestigate="+mission.world.research.CanInvestigate(input.LockedTarget)+" leases="+string.Join(",",mission.world.bloodEntities.slots.Select(s=>s.type+":"+s.index+":"+s.target.gameObject.activeSelf)));}
            Check(mission.SceneNumber==3?mission.world.research.Find(type).State==JourneyResearchState.Investigated:mission.world.immunity.phagocytes.Any(c=>c.State==JourneyPhagocyteState.Activated),"Real Scanner promotes and investigates/activates selected flowing "+type);
        }
        static IEnumerator ScanResearchTypeHeld(string type,Transform candidate)
        {
            JourneyTarget held=null;float began=Time.time;keys=new KeyboardState();
            while(!held&&Time.time-began<30)
            {
                if(!candidate||!candidate.gameObject.activeInHierarchy)candidate=FindBackgroundType(type);
                if(!candidate){mission.world.bloodEntities.stream.RequestInflux(type=="rbc"?"erythrocyte":type=="plasma"?"albumin":type);cursorMode=false;yield return AimAt(mission.mover.viewCamera.transform.position+mission.mover.path.Forward(mission.mover.Distance)*5);cursorMode=true;yield return Wait(.3f);continue;}
                cursorMode=false;yield return AimAt(candidate.position);cursorMode=true;pointer=mission.mover.viewCamera.WorldToScreenPoint(candidate.position);yield return null;buttons=1;
                float catching=Time.time;
                while(Time.time-catching<.6f&&!held){pointer=mission.mover.viewCamera.WorldToScreenPoint(candidate.position);yield return null;held=mission.world.bloodEntities.slots.Where(s=>s.type==type&&s.index>=0&&s.grab.isSelected&&s.target.grabbed).Select(s=>s.target).FirstOrDefault();}
                if(!held){buttons=0;yield return Wait(.1f);candidate=FindBackgroundType(type);}
            }
            Check(held,"Actual LMB selects the requested concrete pool instance: "+type);
            pendingScroll=new Vector2(0,Mathf.Round((1.5f-mission.GetComponent<JourneyInput>().DesktopControls.GrabDepth)/JourneyDesktopControls.DepthPerWheelStep)*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.4f);began=Time.time;
            var input=mission.GetComponent<JourneyInput>();
            while(Time.time-began<8)
            {
                if(input.LockedTarget!=held){keys=new KeyboardState();pointer=mission.mover.viewCamera.WorldToScreenPoint(held.GetComponent<Collider>().bounds.center);}
                else keys=new KeyboardState(Key.F);
                yield return null;
                if(mission.world.research.Find(type).State==JourneyResearchState.Investigated&&mission.world.research.Focused?.theory.id==type&&input.ScanProgress>=.99f)break;
            }
            keys=new KeyboardState();buttons=0;yield return Wait(.15f);
            Check(mission.world.research.Find(type).State==JourneyResearchState.Investigated&&mission.world.research.Focused?.theory.id==type,"Real held Scanner investigates the selected population: "+type);
        }
        static IEnumerator TestOrganism()
        {
            var world=mission.world;var stream=world.bloodEntities.stream;Directory.CreateDirectory(LivingOrganismJourneyBuilder.Reports);yield return Wait(.4f);
            Check(stream.flow.cells.Length==360&&stream.BackgroundCount==360&&stream.flow.cells.All(c=>c.GetComponents<Collider>().Length==0&&c.GetComponents<Rigidbody>().Length==0),"Existing 360-cell background pool remains lightweight without mass physics or XR components");
            Check(world.bloodEntities.slots.Length==7&&world.research.RequiredCount==5,"Seven bounded interactive representatives serve five mandatory blood populations");
            Check(world.analysis.molecules.All(t=>!t.gameObject.activeInHierarchy),"Large O2/CO2/Hb/heme are absent from ordinary flow before nested analysis");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/01_Live_Blood_Background.png");
            foreach(var type in new[]{"phagocyte","platelet","leukocyte","plasma","rbc"})yield return ScanBloodType(type);
            Check(mission.Complete&&world.research.RequiredInvestigated==5,"Phagocyte-first free research independently completes five populations");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/02_Selected_Pool_Cell_Research.png");
            var rbc=world.Find("rbc");keys=new KeyboardState(Key.F);float deepStart=Time.time;while(!world.analysis.Opened&&Time.time-deepStart<6){var point=rbc.GetComponent<Collider>().bounds.center;var v=mission.mover.viewCamera.WorldToViewportPoint(point);if(v.z<=0||v.x<.05f||v.x>.95f||v.y<.05f||v.y>.95f){cursorMode=false;yield return AimAt(point);cursorMode=true;}pointer=mission.mover.viewCamera.WorldToScreenPoint(point);yield return Wait(.03f);}keys=new KeyboardState();yield return Wait(.15f);if(!world.analysis.Opened){var input=mission.GetComponent<JourneyInput>();checks.Add("Deep diagnostic: rootActive="+rbc.gameObject.activeSelf+" focus="+(world.research.Focused?world.research.Focused.theory.id:"none")+" hit="+(input.LockedTarget?input.LockedTarget.name:"none")+" status="+world.research.Find("rbc").State);}Check(world.analysis.Opened,"Held Deep Scan opens the selected RBC's local educational content");
            yield return ResearchScan("hemoglobin");yield return ResearchScan("heme");yield return ResearchScan("oxygen");yield return ResearchScan("co2");Check(world.research.OptionalInvestigated==4,"Hb, heme/Fe2+, O2 and CO2 are accessible only through nested analysis");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/03_Deep_Scan_RBC_Contents.png");
            mission.Enter(8,false);yield return Wait(.3f);Check(world.immunity.LiveCount==1&&mission.Goals.Count==4,"First encounter contains one flowing virus and one cell-manipulation tutorial");
            var virus=world.immunity.TutorialVirus;cursorMode=false;yield return AimAt(virus.transform.position);cursorMode=true;yield return Scan("virus-study");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/04_First_Flowing_Virus.png");
            yield return ScanBloodType("phagocyte");Check(mission.Current?.action==StudyAction.Capture,"Scanner initiates active phagocyte state in the model before BioTool capture");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/05_Scanner_Activated_Phagocyte.png");
            yield return LaunchPhagocytePC(true);yield return Wait(3.1f);Check(!mission.Complete&&world.immunity.LiveCount==1&&world.immunity.phagocytes.Any(c=>c.RemainingCapacity==3),"First miss preserves virus and phagocyte capacity for reuse without punishment");
            yield return LaunchPhagocytePC(false);float start=Time.time;bool engulfCaptured=false;while(!mission.Complete&&Time.time-start<8){if(!engulfCaptured&&world.immunity.phagocytes.Any(c=>c.State==JourneyPhagocyteState.Phagocytosis&&c.stateAge>.55f)){ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/08_Gradual_Phagocytosis.png");engulfCaptured=true;}yield return Wait(.1f);}
            Check(mission.Complete&&world.immunity.TutorialCompleted&&world.immunity.Neutralized==1&&world.immunity.LiveCount==0,"One actual launch and gradual phagocytosis complete the tutorial, without a wave");
            yield return Press(Key.RightArrow,.12f);yield return Wait(4f);Check(mission.SceneNumber==9&&world.immunity.TutorialCompleted&&mission.Current?.target=="immune-engulf","Normal continuation reuses the learned mechanism in a blood-filled wave");
            yield return TestConnectedWave();
            mission.Enter(8,false);yield return Wait(.25f);Check(mission.Complete&&world.immunity.LiveCount==0,"Completed first-virus tutorial is remembered and does not spawn or repeat on normal re-entry");
        }
        static IEnumerator TestConnectedWave()
        {
            var world=mission.world;var stream=world.bloodEntities.stream;
            float waveStart=Time.time;int attempts=0;
            while(!mission.Complete&&Time.time-waveStart<220)
            {
                if(!world.immunity.phagocytes.Any(c=>c.gameObject.activeInHierarchy&&c.State==JourneyPhagocyteState.Activated&&c.RemainingCapacity>0))yield return ScanBloodType("phagocyte");
                while(world.immunity.LiveCount==0&&!mission.Complete)yield return Wait(.1f);if(mission.Complete)break;
                int before=world.immunity.Neutralized;yield return LaunchPhagocytePC(false);float awaiting=Time.time;
                while(world.immunity.Neutralized==before&&Time.time-awaiting<5)yield return Wait(.1f);attempts++;
                if(world.immunity.Neutralized>0&&world.immunity.Neutralized%3==0)ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/09_Later_Wave_Within_Blood.png");
            }
            checks.Add("Later wave elapsed="+(Time.time-waveStart)+" attempts="+attempts+" neutralized="+world.immunity.Neutralized+" missed="+world.immunity.Missed+" load="+mission.ViralLoad+" maximumLive="+world.immunity.MaximumLive+" nearbyCells="+stream.NearbyMainCells);
            Check(mission.Complete&&world.immunity.Neutralized>=12&&world.immunity.MaximumLive>1&&mission.ViralLoad<1&&stream.NearbyMainCells>70,"Later multi-phase viral population uses the same cells inside normal saturated blood and clears Viral Load before completion");
        }
        static IEnumerator LaunchPhagocytePC(bool miss)
        {
            var immunity=mission.world.immunity;float recovery=Time.time;while(!immunity.phagocytes.Any(c=>c.State==JourneyPhagocyteState.Activated&&c.gameObject.activeInHierarchy&&c.RemainingCapacity>0)&&Time.time-recovery<5)yield return Wait(.08f);
            var cell=immunity.phagocytes.Where(c=>c.State==JourneyPhagocyteState.Activated&&c.gameObject.activeInHierarchy&&c.RemainingCapacity>0).OrderBy(c=>Vector3.Distance(c.transform.position,mission.mover.viewCamera.transform.position)).First();cursorMode=false;yield return AimAt(cell.target.GetComponent<Collider>().bounds.center);cursorMode=true;keys=new KeyboardState(Key.E);float began=Time.time;
            while(!immunity.Held&&Time.time-began<3){var capturePoint=cell.target.GetComponent<Collider>().bounds.center;var v=mission.mover.viewCamera.WorldToViewportPoint(capturePoint);if(v.z<=0||v.x<.05f||v.x>.95f||v.y<.05f||v.y>.95f){cursorMode=false;yield return AimAt(capturePoint);cursorMode=true;}pointer=mission.mover.viewCamera.WorldToScreenPoint(capturePoint);yield return Wait(.03f);}
            if(immunity.Held!=cell){var input=mission.GetComponent<JourneyInput>();var contact=cell.target.GetComponent<Collider>();checks.Add("Immune capture diagnostic: state="+cell.State+" held="+(immunity.Held?immunity.Held.name:"none")+" mode="+input.GetComponent<BioToolModeController>().CurrentMode+" hit="+(input.LockedTarget?input.LockedTarget.name:"none")+" position="+cell.transform.position+" distance="+Vector3.Distance(cell.transform.position,mission.mover.viewCamera.transform.position)+" collider="+contact.enabled+" bounds="+contact.bounds+" blocked="+input.ToolBlockedByUi+" step="+mission.StepIndex);}
            Check(immunity.Held==cell,"Actual held BioTool Capture attracts the active phagocyte rather than firing a projectile");yield return Wait(.9f);
            var slot=mission.world.bloodEntities.Slot(cell.target);var meshSize=Vector3.Scale(slot.body.sharedMesh.bounds.size,slot.body.transform.lossyScale);float diameter=Mathf.Max(meshSize.x,Mathf.Max(meshSize.y,meshSize.z));
            if(diameter>.46f||slot.legacyRenderers.Any(r=>r.enabled))checks.Add("Held geometry diagnostic: cell="+cell.name+" rootScale="+cell.transform.lossyScale+" diameter="+diameter+" bodyScale="+cell.body.transform.lossyScale+" legacy="+string.Join(",",slot.legacyRenderers.Select(r=>r.name+":"+r.enabled)));
            Check(diameter<=.46f&&slot.legacyRenderers.All(r=>!r.enabled),"Held cell retains lightweight pool size and hides its legacy study display");
            if(immunity.Tutorial)ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/06_BioTool_Holds_Phagocyte.png");
            pendingScroll=new Vector2(0,-13*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.2f);Check(immunity.PullDistance>.24f&&immunity.ChargeProgress>.99f&&immunity.trajectory.enabled,"Continuous held field clamps pull to 25cm and shows blood-drift trajectory");
            var camera=mission.mover.viewCamera;var virus=immunity.Tutorial?immunity.TutorialVirus:immunity.viruses.Where(v=>v.Live&&v.State!=JourneyImmuneVirusState.Engaged&&v.State!=JourneyImmuneVirusState.BeingPhagocytosed).OrderBy(v=>Vector3.Distance(camera.transform.position,v.transform.position)).First();var point=miss?camera.transform.position+mission.mover.path.Up(mission.mover.Distance)*4:ImmuneInterceptPoint(immunity,virus,camera.transform.position,1.2f);
            cursorMode=false;yield return AimAt(point);cursorMode=true;float settling=Time.time;
            while(Time.time-settling<1.1f){point=miss?camera.transform.position+mission.mover.path.Up(mission.mover.Distance)*4:ImmuneInterceptPoint(immunity,virus,camera.transform.position,1.2f);yield return AimAt(point);yield return Wait(.03f);}
            if(immunity.Tutorial)ArteryPolishReview.Capture(mission.mover.viewCamera,"LivingOrganismJourney/07_Pulled_Cell_And_Trajectory.png");keys=new KeyboardState();yield return Wait(.15f);
            Check(cell.State==JourneyPhagocyteState.Launched||cell.State==JourneyPhagocyteState.Phagocytosis,"Trigger release launches the actual phagocyte with additive blood-flow velocity");
        }
        internal static Vector3 ImmuneInterceptPoint(JourneyInnateImmunity immunity,JourneyImmuneVirus virus,Vector3 origin,float heldDistance)
        {
            // Solve a lead from observable motion and the public trajectory model;
            // the only intervention is still normal mouse/controller aiming.
            Vector3 viralVelocity=immunity.world.mover.path.Forward(virus.arc)*virus.speed;
            Vector3 drift=immunity.BloodVelocity(origin),bestDirection=(virus.transform.position-origin).normalized;float error=float.MaxValue;
            for(float t=.06f;t<=2.7f;t+=.03f){Vector3 delta=virus.transform.position+viralVelocity*t-origin-drift*t;float travel=heldDistance+4.2f*(1-Mathf.Exp(-.7f*t))/.7f;float difference=Mathf.Abs(delta.magnitude-travel);if(difference<error){error=difference;bestDirection=delta.normalized;}}
            return origin+bestDirection*8;
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
            if(puzzle.flowLayering){Check(puzzle.Attached==0&&!piece.Placed&&mission.StepIndex==0,"Manual release cannot bypass BioTool adhesion");yield return Wait(.75f);yield return NativeCapturePlatelet(piece,puzzle.Slot(0));Check(puzzle.Attached==1&&mission.Progress==1,"Actual held Capture guides the first platelet into automatic adhesion");}
            else Check(puzzle.Attached==1&&piece.Placed&&mission.StepIndex==1,"Mouse pointer/wheel and LMB release physically place the first platelet into the real adhesion slot");
            ScreenCapture.CaptureScreenshot(Folder+"/Scene12_Mouse_Placement.png");
        }
        static IEnumerator TestTone()
        {
            if(mission.world.hemodynamics){yield return TestHemodynamics();yield break;}
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
        static IEnumerator TestHemodynamics()
        {
            mission.Enter(5,false);yield return Wait(.4f);var experiment=mission.world.hemodynamics;var stream=mission.world.GetComponent<JourneyContinuousBloodstream>();Directory.CreateDirectory(HemodynamicsExperimentBuilder.Reports);
            cursorMode=false;yield return AimAt(experiment.toneContact.transform.position);cursorMode=true;
            Check(experiment&&mission.Goals.Count==14&&experiment.Case==0&&Mathf.Abs(experiment.P2-112)<.01f&&Mathf.Abs(experiment.DiameterMm-4.8f)<.01f,"Early episode begins as a normal in-vessel experiment with four physical measurements");
            Check(!mission.world.flowModel.gameObject.activeInHierarchy&&experiment.display.compactTheory.activeInHierarchy&&!mission.GetComponent<JourneyHolographicScreens>().theoryRoot.gameObject.activeInHierarchy,"Large legacy flow hologram is replaced by compact in-world experiment UI");
            foreach(var id in new[]{"hdx-p1","hdx-p2","hdx-speed","hdx-diameter"})yield return Scan(id);
            Check(experiment.Case==1&&experiment.RadiusRatio<.8f&&experiment.ResistanceRatio>3&&experiment.VelocityRatio>1.7f&&experiment.TracerBursts>=4,"Scanner FLOW collects A/B/speed/diameter; fixed-Q narrowing increases resistance, speed and pressure drop");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"HemodynamicsExperiment/01_Narrow_In_Vessel_AR.png");
            yield return AdjustHemodynamicsMouse();yield return Scan("hdx-check1");
            Check(experiment.Case==2&&experiment.RadiusRatio>1.2f&&experiment.VelocityRatio<.7f&&experiment.ResistanceRatio<.5f&&experiment.FormulaDiscovered,"Wide-vessel experiment slows local velocity; resistance relation appears after discovering it");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"HemodynamicsExperiment/02_Wide_AR_And_Discovered_Relation.png");
            yield return AdjustHemodynamicsMouse();yield return Scan("hdx-check2");
            Check(experiment.Case==3&&Mathf.Abs(experiment.RadiusRatio-1)<.001f&&experiment.P1==100&&experiment.P2==95&&experiment.FlowRatio<.7f,"Third puzzle lowers both pressures at normal diameter rather than repeating tone correction");
            int step=mission.StepIndex;yield return AimAt(experiment.toneContact.transform.position);yield return Press(Key.E,.4f);Check(mission.StepIndex==step&&Mathf.Abs(experiment.RadiusRatio-1)<.001f,"BioTool cannot modify the mystery vessel before diagnostic evidence");
            foreach(var id in new[]{"hdx-diameter","hdx-speed","hdx-p1","hdx-p2"})yield return Scan(id);
            yield return NativePlace(experiment.cards.First(c=>c.code==0).gameObject,experiment.cardSlot.position);Check(experiment.Mistakes>0&&mission.Current?.action==StudyAction.Bind,"Physical wrong narrowing hypothesis is rejected with a data-based explanation");
            yield return NativePlace(experiment.cards.First(c=>c.code==2).gameObject,experiment.cardSlot.position);Check(experiment.AcceptedHypotheses>0&&mission.Current?.target=="hdx-final","Physical evidence-based outside-cause hypothesis succeeds without changing radius");yield return Scan("hdx-final");Check(mission.Complete&&stream.NearbyMainCells>=90,"Experiment ends after repeat verification with dense blood still circulating");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"HemodynamicsExperiment/03_Outside_Cause_Conclusion.png");
            yield return Press(Key.RightArrow,.12f);yield return Wait(10f);Check(mission.SceneNumber==6&&mission.Ready&&!experiment.Active,"Normal Continue preserves the route to plaque and restores normal HUD");
            mission.Enter(10,false);yield return Wait(.4f);Check(mission.Goals.Count==7&&mission.PressureSystolic==92,"Late episode is independent low-pressure diagnosis, not another tone tutorial");
            foreach(var id in new[]{"hdx-diameter","hdx-speed","hdx-p1","hdx-p2","hdx-wall"})yield return Scan(id);
            yield return NativePlace(experiment.cards.First(c=>c.code==1).gameObject,experiment.cardSlot.position);Check(mission.Current?.action==StudyAction.Bind,"Unsupported local dilation cannot replace late diagnosis");
            yield return NativePlace(experiment.cards.First(c=>c.code==2).gameObject,experiment.cardSlot.position);yield return Scan("hdx-forward");Check(mission.Complete&&Mathf.Abs(experiment.RadiusRatio-1)<.001f,"Late diagnosis finishes without an unjustified radius intervention");
            ArteryPolishReview.Capture(mission.mover.viewCamera,"HemodynamicsExperiment/04_Late_Independent_Diagnosis.png");
            yield return Press(Key.RightArrow,.12f);yield return Wait(10f);Check(mission.SceneNumber==11&&mission.Ready&&mission.PressureSystolic==92&&mission.world.woundState.LeakStrength>.95f,"Evidence-based late decision leads normally to the existing leaking wound episode");
        }
        static IEnumerator AdjustHemodynamicsMouse()
        {
            var experiment=mission.world.hemodynamics;int step=mission.StepIndex;yield return AimAt(experiment.toneContact.transform.position);keys=new KeyboardState(Key.E);yield return Wait(.4f);
            Check(experiment.Adjusting&&mission.StepIndex==step&&experiment.Stability==0,"Held E locks TONUS but a stationary click cannot solve the radius puzzle");
            pendingScroll=new Vector2(0,Mathf.Round((1-experiment.RadiusRatio)/.025f)*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.3f);Check(mission.StepIndex==step,"Correct radius still requires a stable hold rather than an instant success");yield return Wait(1f);keys=new KeyboardState();yield return Wait(.2f);
            Check(mission.StepIndex==step+1&&experiment.RadiusRatio>=.95f&&experiment.RadiusRatio<=1.05f,"Actual held E and wheel correction produce a stable normal diameter");
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
            int before=mission.StepIndex;var target=mission.world.Find(id);var point=target.GetComponent<Collider>().bounds.center;var viewport=mission.mover.viewCamera.WorldToViewportPoint(point);
            if(cursorMode&&(viewport.z<=0||viewport.x<.08f||viewport.x>.92f||viewport.y<.08f||viewport.y>.92f)){cursorMode=false;yield return AimAt(point);cursorMode=true;}
            yield return AimAt(point);yield return Press(Key.F,1.05f);
            if(mission.StepIndex!=before+1){var input=mission.GetComponent<JourneyInput>();checks.Add("Scanner diagnostic: id="+id+" lock="+(input.LockedTarget?input.LockedTarget.name:"none")+" blocked="+input.ScannerBlockedByUi+" obstacle="+(input.AimObstruction?input.AimObstruction.name:"none")+" position="+point+" step="+mission.StepIndex);}
            Check(mission.StepIndex==before+1,"Actual mouse/Scanner scan: "+id);
        }
        static IEnumerator Press(Key key,float duration)
        {keys=new KeyboardState(key);yield return Wait(duration);keys=new KeyboardState();yield return Wait(.12f);}
        static IEnumerator Wait(float time)
        {
            float start=Time.time;double deadline=EditorApplication.timeSinceStartup+Mathf.Max(12,time*2+5);
            while(Time.time-start<time){if(!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Player input loop did not advance");yield return null;}
        }
    }
}

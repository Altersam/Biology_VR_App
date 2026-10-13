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
    public static class PlayerFeedbackInputPlaytest
    {
        const string Folder=JourneyGeometry.Root+"/Reports/Polish/PlayerFeedbackRework";
        static JourneyMission mission;
        static Keyboard keyboard;static Mouse mouse;
        static KeyboardState keys;static Vector2 pointer,delta,wheel;static ushort buttons;
        static bool running;static readonly List<string> checks=new List<string>();
        static bool obstaclesOnly;
        [MenuItem("Biology VR/Test Player Feedback Rework Through Real PC Input")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying||running)throw new InvalidOperationException("Fresh Play Mode required");
            mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();obstaclesOnly=false;running=true;checks.Clear();Directory.CreateDirectory(Folder);EditorCoroutineUtility.StartCoroutineOwnerless(Guard());
        }
        [MenuItem("Biology VR/Test Feedback Plaque And All Emboli Through PC Input")]
        public static void Obstacles(){if(!EditorApplication.isPlaying||running)throw new InvalidOperationException("Fresh Play Mode required");mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();obstaclesOnly=true;running=true;checks.Clear();Directory.CreateDirectory(Folder);EditorCoroutineUtility.StartCoroutineOwnerless(Guard());}
        static void Report()=>File.WriteAllText(Folder+(obstaclesOnly?"/Obstacles_PC.json":"/PC_Input.json"),JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,running,passed=!running&&checks.Contains(obstaclesOnly?"PASS: Feedback obstacles input sequence complete":"PASS: Feedback rework input sequence complete")&&!checks.Any(c=>c.StartsWith("BLOCKED")),method="Real MouseState/KeyboardState and XRI field hand. Isolated episode-entry fixtures; no success API calls.",checks},Formatting.Indented));
        static void Check(bool value,string text){checks.Add((value?"PASS: ":"BLOCKED: ")+text);Report();if(!value)throw new InvalidOperationException(text);}
        static IEnumerator Guard()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Test());
            try{while(stack.Count>0){var current=stack.Peek();bool moved=false;Exception error=null;try{moved=current.MoveNext();}catch(Exception e){error=e;}if(error!=null){checks.Add("BLOCKED: "+error);Debug.LogException(error);yield break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current.Current is IEnumerator nested)stack.Push(nested);else yield return current.Current;}}
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;Report();}
        }
        static void Queue()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic&&InputState.currentUpdateType!=InputUpdateType.Fixed)return;
            InputSystem.QueueStateEvent(keyboard,keys);bool dynamic=InputState.currentUpdateType==InputUpdateType.Dynamic;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=pointer,buttons=buttons,delta=dynamic?delta:Vector2.zero,scroll=dynamic?wheel:Vector2.zero});if(dynamic)delta=wheel=Vector2.zero;
        }
        static void Current(){keyboard?.MakeCurrent();mouse?.MakeCurrent();}
        static IEnumerator Test()
        {
            var settings=InputSystem.settings;var behavior=settings.backgroundBehavior;var editor=settings.editorInputBehaviorInPlayMode;
            bool background=Application.runInBackground;
            var simulators=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b is XRDeviceSimulator||b is XRInteractionSimulator).Cast<Behaviour>().ToArray();var states=simulators.Select(b=>b.enabled).ToArray();
            try
            {
                foreach(var simulator in simulators)simulator.enabled=false;Application.runInBackground=true;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Queue;InputSystem.onAfterUpdate+=Current;
                keys=new KeyboardState();pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);yield return Wait(.3f);
                if(obstaclesOnly){yield return ObstaclesTest();yield break;}
                var world=mission.world;var pool=world.bloodEntities;var stream=pool.stream;
                Check(world.freeTools&&world.freeTools.payload&&!world.analysis.enabled,"Installed field tool, bound cargo and no automatic analysis diagram");
                Check(stream.palette.Any(p=>p.id=="erythrocyte-O2")&&stream.palette.Any(p=>p.id=="erythrocyte-CO2")&&stream.palette.Any(p=>p.id=="hemoglobin")&&stream.palette.First(p=>p.id=="phagocyte").weight>=9,"O2/CO2 erythrocytes, empty RBC, Hb carrier and ample phagocytes share the blood population");
                Check(pool.slots.All(s=>(s.grab.interactionLayers.value&1)==0)&&stream.flow.cells.All(c=>c.GetComponents<Collider>().Length==0),"Flow cells use only the BioTool field layer; background has no mass physics");
                yield return Capture("erythrocyte-O2");var held=world.freeTools.Held;yield return Scan(held);yield return Scan(world.freeTools.payload.oxygen);
                Check(world.freeTools.Held==held&&!world.analysis.Opened&&!world.freeTools.payload.co2.gameObject.activeInHierarchy,"Captured oxygen-loaded RBC stays held during scan and does not generate CO2/Hb/iron around it");CaptureView("01_Bound_Oxygen_RBC.png");
                keys=new KeyboardState();yield return Wait(.25f);Check(!world.freeTools.Held&&!pool.Owns(held),"Trigger release returns the actual selected RBC to the moving background");
                yield return Capture("erythrocyte-CO2");yield return Scan(world.freeTools.payload.co2);Check(world.freeTools.payload.oxygen.gameObject.activeSelf==false,"CO2-loaded RBC has its own cargo, not a simultaneous automatic oxygen model");keys=new KeyboardState();yield return Wait(.2f);
                yield return Capture("hemoglobin");yield return Scan(world.freeTools.Held);yield return Scan(world.freeTools.payload.heme);Check(world.freeTools.payload.heme.GetComponent<JourneyFlowBond>().carrier==world.freeTools.Held,"Heme/iron is attached to the captured Hb carrier");CaptureView("02_Hb_With_Bound_Iron.png");keys=new KeyboardState();yield return Wait(.2f);
                foreach(var species in new[]{"leukocyte","platelet","albumin","phagocyte"}){yield return Capture(species);yield return Scan(world.freeTools.Held);keys=new KeyboardState();yield return Wait(.2f);}
                Check(mission.Complete&&world.research.RequiredInvestigated==5,"All five blood populations complete using BioTool capture and actual Scanner");keys=new KeyboardState(Key.RightArrow);yield return Wait(.08f);keys=new KeyboardState();float travel=Time.time;while(!mission.Ready&&Time.time-travel<15)yield return Wait(.15f);Check(mission.SceneNumber==4&&mission.Ready,"Normal Continue preserves travel from completed blood research to the artery-wall lesson");
                yield return Mode(BioToolMode.Impulse);int shots=world.freeTools.Shots;keys=new KeyboardState(Key.E);yield return Wait(.55f);keys=new KeyboardState();Check(world.freeTools.Shots>=shots+2,"Impulse fires in ordinary free research without a mission Pulse goal");
                mission.Enter(5,false);yield return Wait(.3f);yield return Scan(world.Find("hdx-diameter"));Check(mission.StepIndex==1,"Scanner measures a real wall area without AR point/card hunt");
                yield return Mode(BioToolMode.Tonus);yield return Aim(world.Find("hdx-diameter").transform.position);keys=new KeyboardState(Key.E);yield return Wait(.2f);wheel=new Vector2(0,8*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.25f);
                Check(mission.StepIndex==2&&world.hemodynamics.RadiusRatio>1.2f,"Actual held vascular field and wheel expansion change diameter and advance the expansion goal");
                wheel=new Vector2(0,9*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.3f);Check(mission.StepIndex==3&&world.hemodynamics.Critical,"Further manual expansion produces critical physiology rather than a static HUD");CaptureView("03_Critical_Vascular_Field.png");
                wheel=new Vector2(0,Mathf.Round((1-world.hemodynamics.RadiusRatio)/.035f)*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(1);keys=new KeyboardState();yield return Wait(.1f);yield return Scan(world.Find("hdx-final"));Check(mission.Complete,"Safe diameter plus repeat Scanner measurement finishes the vessel lesson");
                Check(!world.hemodynamics.arRoot.activeSelf&&world.mission.GetComponent<JourneyHolographicScreens>().vitalsRoot.gameObject.activeInHierarchy,"Formulas and dynamic readings stay on the two original screens");
                mission.Enter(7,false);yield return Wait(.3f);yield return Mode(BioToolMode.Capture);var bubble=world.Find("embolus");var first=bubble.transform.position;yield return Wait(.6f);Check(Vector3.Distance(first,bubble.transform.position)>.04f&&world.embolusWave.targets.All(t=>!t.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>()||!t.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().enabled),"Emboli drift unpredictably and physical Grip is disabled for every bubble");
                yield return Aim(bubble.transform.position);keys=new KeyboardState(Key.E);float catching=Time.time;
                while(!world.EmbolusCaptured&&Time.time-catching<5){pointer=mission.mover.viewCamera.WorldToScreenPoint(bubble.GetComponent<Collider>().bounds.center);yield return null;}
                Check(world.EmbolusCaptured,"Actual Trigger/ E in attraction mode captures an embolus without a hidden mode-selection objective");
                yield return Aim(mission.mover.viewCamera.transform.position+mission.mover.path.Up(mission.mover.Distance)*4);yield return Wait(.4f);Check(world.EmbolusCaptured&&world.EmbolusStability==0,"Losing aim suspends attraction and deposit stability until reticle returns");CaptureView("06_Evasive_Embolus_Lost_Aim.png");keys=new KeyboardState();yield return Wait(.15f);Check(!world.EmbolusCaptured,"Releasing Trigger stops the embolus field");
                mission.Enter(8,false);yield return Wait(.3f);Check(world.immunity.LiveCount==1&&world.immunity.TutorialVirus.speed<.1f,"One slow tutorial virus waits in front of the player");
                for(int hit=0;hit<3;hit++)
                {
                    yield return Capture("phagocyte");var cell=world.freeTools.Held.GetComponent<JourneyPhagocyteEntity>();yield return Scan(cell.target);
                    yield return Mode(BioToolMode.Bio);keys=new KeyboardState(Key.E);yield return Wait(.85f);Check(cell.Studied&&cell.State==JourneyPhagocyteState.Activated,"Captured phagocyte is really scanned then activated using the activation mode");
                    yield return Mode(BioToolMode.Reverse);keys=new KeyboardState(Key.E);yield return Aim(world.immunity.TutorialVirus.transform.position);yield return Wait(.9f);CaptureView("04_Reverse_Charged_Phagocyte.png");keys=new KeyboardState();yield return Wait(.2f);
                    float began=Time.time;while(world.immunity.DamageContacts<=hit&&Time.time-began<5)yield return Wait(.1f);
                    Check(world.immunity.DamageContacts==hit+1,"Real Reverse Trigger release gives actual viral contact "+(hit+1)+"/3");
                    if(hit<2)Check(world.immunity.LiveCount==1&&!mission.Complete&&world.immunity.TutorialVirus.Integrity==2-hit,"Partial contact leaves the virus alive and requires another cell");
                    yield return Wait(1.3f);
                }
                Check(mission.Complete&&world.immunity.LiveCount==0,"Three independently captured/scanned/activated cells complete the first virus");
                mission.Enter(11,false);yield return Wait(.3f);yield return Scan(world.Find("wound"));yield return Mode(BioToolMode.Bio);yield return Click(world.Find("leak"));yield return Click(world.Find("pressure-low"));Check(mission.Complete,"Later wound diagnostics remain completable with manual activation/measurement mode");
                mission.Enter(3,false,true);yield return Wait(.2f);float scale=Time.timeScale;
                try{Time.timeScale=4;yield return Wait(62);}finally{Time.timeScale=scale;}
                Check(world.GetComponent<JourneyLiveTelemetry>().assistance.gameObject.activeInHierarchy,"One minute without objective progress displays context help below the right screen");CaptureView("05_Context_Help_And_Readable_Screens.png");
                Check(true,"Feedback rework input sequence complete");
            }
            finally{InputSystem.onBeforeUpdate-=Queue;InputSystem.onAfterUpdate-=Current;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);for(int i=0;i<simulators.Length;i++)if(simulators[i])simulators[i].enabled=states[i];Application.runInBackground=background;settings.backgroundBehavior=behavior;settings.editorInputBehaviorInPlayMode=editor;}
        }
        static Transform Source(string species)
        {
            var stream=mission.world.bloodEntities.stream;Transform best=null;float distance=8;
            for(int i=0;i<stream.flow.VisibleCellLimit;i++){var cell=stream.flow.cells[i];if(stream.Leased(i)||stream.flow.species[i]!=species||!cell.GetComponent<Renderer>().enabled||species=="phagocyte"&&!mission.world.bloodEntities.HasPhagocyteCapacity(i))continue;float d=Vector3.Distance(cell.position,mission.mover.viewCamera.transform.position);if(d>1.5f&&d<distance){distance=d;best=cell;}}
            return best;
        }
        static IEnumerator ObstaclesTest()
        {
            var world=mission.world;yield return Mode(BioToolMode.Impulse);var sample=Source("erythrocyte");Check(sample,"A background blood cell is available as an ordinary impulse target");yield return Aim(sample.position);int broken=world.freeTools.BrokenCells;keys=new KeyboardState(Key.E);float targeting=Time.time;while(world.freeTools.BrokenCells==broken&&Time.time-targeting<3){pointer=mission.mover.viewCamera.WorldToScreenPoint(sample.position);yield return null;}keys=new KeyboardState();yield return Wait(.1f);if(world.freeTools.BrokenCells==broken){var input=mission.GetComponent<JourneyInput>();checks.Add("Impulse diagnostic: shots="+world.freeTools.Shots+" hit="+input.LockedTarget?.targetId+" blocked="+input.ToolBlockedByUi+" obstacle="+input.AimObstruction?.name+" source="+sample.position+" pointer="+pointer);}Check(world.freeTools.BrokenCells>broken,"Impulse can actually destroy a blood-pool object outside a plaque combat goal");
            mission.Enter(6,false);yield return Wait(3);var plaque=world.plaqueChallenge;yield return Scan(world.Find("plaque"));
            Check(mission.Current?.action==StudyAction.Pulse&&mission.PressureSystolic>120,"Plaque raises measured upstream pressure and Scanner leads straight to impulse action");
            var stream=world.bloodEntities.stream;var path=mission.mover.path;float arc=path.Anchor(6);
            var cells=stream.MainCellsBetween(arc-8,arc+4).Take(10).ToArray();var points=cells.Select(c=>c.position).ToArray();yield return Wait(.4f);float shift=cells.Select((c,i)=>Vector3.Distance(c.position,points[i])).DefaultIfEmpty(100).Average();Check(cells.Length>0&&shift<.35f,"Primary-pool blood slows and queues upstream of the plaque instead of bypassing it with density teleportation");
            yield return Mode(BioToolMode.Impulse);keys=new KeyboardState(Key.E);float start=Time.time;
            while(!mission.Complete&&Time.time-start<50)
            {
                var target=plaque.lipids.Where(l=>l.gameObject.activeInHierarchy&&l.contact.enabled).OrderBy(l=>Vector3.Distance(l.transform.position,mission.mover.viewCamera.transform.position)).FirstOrDefault();
                if(target){var point=target.contact.bounds.center;var v=mission.mover.viewCamera.WorldToViewportPoint(point);if(v.z<=0||v.x<.1f||v.x>.9f||v.y<.1f||v.y>.9f){keys=new KeyboardState();yield return Aim(point);keys=new KeyboardState(Key.E);}pointer=mission.mover.viewCamera.WorldToScreenPoint(point);}yield return Wait(.04f);
            }
            keys=new KeyboardState();yield return Wait(.2f);Check(mission.Complete&&plaque.Cleared,"Actual free-mode impulse destroys the original and arriving plaque lipids");CaptureView("09_Plaque_Released_Flow.png");
            mission.Enter(7,false);yield return Wait(.3f);yield return Mode(BioToolMode.Capture);
            for(int i=0;i<6;i++)
            {
                var target=world.Find("embolus");yield return Aim(target.GetComponent<Collider>().bounds.center);keys=new KeyboardState(Key.E);float began=Time.time;
                while(!world.EmbolusCaptured&&Time.time-began<6){pointer=mission.mover.viewCamera.WorldToScreenPoint(target.GetComponent<Collider>().bounds.center);yield return null;}
                Check(world.EmbolusCaptured,"Actual reticle tracking captures evasive embolus "+(i+1));
                target=world.Find("embolus");
                began=Time.time;bool framed=false;
                while(world.embolusWave.StabilizedCount<i+1&&Time.time-began<22)
                {
                    var aim=mission.GetComponent<JourneyInput>();var contact=target.GetComponent<Collider>().bounds.center;var trap=world.trap.position;var camera=mission.mover.viewCamera;var viewport=camera.WorldToViewportPoint(contact);
                    if(!framed||viewport.z<=0||viewport.x<.08f||viewport.x>.92f||viewport.y<.08f||viewport.y>.92f){yield return Aim((contact+trap)*.5f);framed=true;}
                    var move=Vector3.MoveTowards(contact,trap,.065f);pointer=camera.WorldToScreenPoint(move);float desired=Vector3.Distance(camera.transform.position,trap);int ticks=Mathf.RoundToInt((desired-aim.DesktopFieldDistance)/JourneyDesktopControls.DepthPerWheelStep);if(ticks!=0)wheel=new Vector2(0,Mathf.Clamp(ticks,-1,1)*JourneyDesktopControls.WheelUnitsPerStep);yield return Wait(.025f);
                }
                if(world.embolusWave.StabilizedCount!=i+1)checks.Add("Transport diagnostic: bubble="+target.transform.position+" trap="+world.trap.position+" fieldDepth="+mission.GetComponent<JourneyInput>().DesktopFieldDistance+" stability="+world.EmbolusStability+" lock="+mission.GetComponent<JourneyInput>().LockedTarget?.targetId);
                Check(world.embolusWave.StabilizedCount==i+1,"Continuous aimed field transports and deposits embolus "+(i+1)+"/6");keys=new KeyboardState();yield return Wait(.15f);
            }
            Check(mission.Complete,"All six evasive emboli finish through real attraction input");CaptureView("10_All_Evasive_Emboli_Deposited.png");Check(true,"Feedback obstacles input sequence complete");
        }
        static IEnumerator Capture(string species)
        {
            keys=new KeyboardState();yield return Wait(.15f);yield return Mode(BioToolMode.Capture);var world=mission.world;world.bloodEntities.stream.RequestInflux(species);float began=Time.time;
            while(Time.time-began<40)
            {
                var cell=Source(species);if(!cell){keys=new KeyboardState();yield return Aim(mission.mover.viewCamera.transform.position+mission.mover.path.Forward(mission.mover.Distance)*5);world.bloodEntities.stream.RequestInflux(species);yield return Wait(.25f);continue;}
                keys=new KeyboardState();yield return Aim(cell.position);keys=new KeyboardState(Key.E);float trying=Time.time;
                while(!world.freeTools.Held&&Time.time-trying<2){pointer=mission.mover.viewCamera.WorldToScreenPoint(cell.position);yield return null;}
                if(world.freeTools.Held){var slot=world.bloodEntities.Slot(world.freeTools.Held);if(slot!=null&&slot.index>=0&&world.bloodEntities.stream.flow.species[slot.index]==species){yield return Wait(.55f);Check(slot.grab.isSelected&&world.freeTools.Held.grabbed,"Actual BioTool field XRI capture of "+species);yield break;}keys=new KeyboardState();yield return Wait(.2f);}
            }
            Check(false,"Capture requested moving carrier "+species);
        }
        static IEnumerator Scan(JourneyTarget target)
        {
            Check(target&&target.gameObject.activeInHierarchy,"Scanner topic is present on its physical carrier: "+(target?target.targetId:"missing"));
            var contact=mission.SceneNumber==6&&target.targetId=="plaque"?mission.world.plaqueChallenge.diagnosisContact:target.GetComponent<Collider>();
            var controller=mission.GetComponent<BioToolModeController>();bool hold=mission.world.freeTools.Held&&controller.CurrentMode==BioToolMode.Capture;float began=Time.time;int step=mission.StepIndex;
            if(!hold){keys=new KeyboardState();yield return Aim(contact.bounds.center);began=Time.time;}
            while(Time.time-began<4){pointer=mission.mover.viewCamera.WorldToScreenPoint(contact.bounds.center);keys=hold?new KeyboardState(Key.F,Key.E):new KeyboardState(Key.F);yield return null;if(mission.SceneNumber==3?mission.world.research.Find(target.targetId)?.State==JourneyResearchState.Investigated:mission.StepIndex>step||target.GetComponent<JourneyPhagocyteEntity>()?.Studied==true)break;}
            keys=hold?new KeyboardState(Key.E):new KeyboardState();yield return Wait(.1f);
            if(mission.SceneNumber==3?mission.world.research.Find(target.targetId)?.State!=JourneyResearchState.Investigated:mission.StepIndex==step&&target.GetComponent<JourneyPhagocyteEntity>()?.Studied!=true){var input=mission.GetComponent<JourneyInput>();checks.Add("Scan diagnostic: hit="+(input.LockedTarget?input.LockedTarget.targetId:"none")+" goal="+mission.Current?.target+" step="+mission.StepIndex+" focus="+(mission.world.research.Focused?mission.world.research.Focused.theory.id:"none")+" held="+(mission.world.freeTools.Held?mission.world.freeTools.Held.targetId:"none")+" blocked="+input.ScannerBlockedByUi+" progress="+input.ScanProgress+" colliderEnabled="+target.GetComponent<Collider>().enabled+" collider="+target.GetComponent<Collider>().bounds+" pointer="+pointer);}
            Check(mission.SceneNumber==3?mission.world.research.Find(target.targetId)?.State==JourneyResearchState.Investigated:mission.StepIndex>step||target.GetComponent<JourneyPhagocyteEntity>()?.Studied==true,"Actual Scanner hold studies "+target.targetId);
        }
        static IEnumerator Mode(BioToolMode mode)
        {
            var controller=mission.GetComponent<BioToolModeController>();int guard=0;
            while(controller.CurrentMode!=mode&&guard++<5){keys=new KeyboardState(Key.C);yield return Wait(.07f);keys=new KeyboardState();yield return Wait(.07f);}
            Check(controller.CurrentMode==mode,"Actual C selects manual mode "+mode);
        }
        static IEnumerator Click(JourneyTarget target){int step=mission.StepIndex;keys=new KeyboardState();yield return Aim(target.GetComponent<Collider>().bounds.center);keys=new KeyboardState(Key.E);yield return Wait(.12f);keys=new KeyboardState();yield return Wait(.15f);Check(mission.StepIndex==step+1,"Actual BioTool action completes "+target.targetId);}
        static IEnumerator Aim(Vector3 point)
        {
            var camera=mission.mover.viewCamera.transform;var local=camera.parent.InverseTransformDirection(point-camera.position);var desired=Quaternion.LookRotation(local,Vector3.up).eulerAngles;
            for(int i=0;i<5;i++){float yaw=Mathf.DeltaAngle(camera.localEulerAngles.y,desired.y),pitch=Mathf.DeltaAngle(camera.localEulerAngles.x,desired.x);if(Mathf.Abs(yaw)<.3f&&Mathf.Abs(pitch)<.3f)break;delta=new Vector2(yaw/.12f,-pitch/.12f);buttons=2;yield return Wait(.06f);buttons=0;yield return Wait(.04f);}
            pointer=mission.mover.viewCamera.WorldToScreenPoint(point);yield return Wait(.04f);
        }
        static IEnumerator Wait(float duration){float start=Time.time;double deadline=EditorApplication.timeSinceStartup+Mathf.Max(60,duration*2+5);while(Time.time-start<duration){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)throw new InvalidOperationException("Input fixture player loop stopped");yield return null;}}
        static void CaptureView(string name)=>ArteryPolishReview.Capture(mission.mover.viewCamera,"PlayerFeedbackRework/"+name);
    }
}

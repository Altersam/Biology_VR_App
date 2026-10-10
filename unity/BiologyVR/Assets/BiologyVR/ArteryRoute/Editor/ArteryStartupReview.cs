using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryStartupReview
    {
        [MenuItem("Biology VR/Apply Readable Scene03 Onboarding")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var hud=m.GetComponent<JourneyHud>();
            if(!m.GetComponent<JourneyDesktopOnboarding>()){var pc=m.gameObject.AddComponent<JourneyDesktopOnboarding>();pc.hud=hud;}
            var art=m.world.art;var ids=new[]{"rbc","leukocyte","platelet","plasma"};var captions=new[]{"Эритроцит • перенос кислорода","Лейкоцит • иммунная защита","Тромбоцит • остановка кровотечения","Плазма • белки и ионы"};
            var flow=m.world.GetComponent<JourneyBloodFlow>();var moving=new System.Collections.Generic.List<JourneyFlowPickup>();
            var used=new System.Collections.Generic.HashSet<int>();
            foreach(var spec in new[]{(id:"rbc",species:"erythrocyte",distance:1.15f,x:.22f,y:-.12f),(id:"leukocyte",species:"leukocyte",distance:1.60f,x:-.65f,y:-.45f),(id:"platelet",species:"platelet",distance:1.45f,x:.80f,y:.25f),(id:"co2",species:"co2",distance:2.10f,x:-.40f,y:.50f),(id:"plasma",species:"plasma-proteins",distance:2.40f,x:1.05f,y:-.40f)})
            {
                var target=m.world.Find(spec.id);int index=Array.FindIndex(flow.species,s=>s==spec.species);if(index<0||used.Contains(index))index=Enumerable.Range(0,Mathf.Min(flow.mobileCellLimit,flow.cells.Length)).First(i=>!used.Contains(i));used.Add(index);
                var pickup=target.GetComponent<JourneyFlowPickup>();if(!pickup)pickup=target.gameObject.AddComponent<JourneyFlowPickup>();pickup.flow=flow;pickup.target=target;pickup.poolIndex=index;pickup.startDistance=spec.distance;pickup.startLane=new Vector2(spec.x,spec.y);moving.Add(pickup);
                var grab=EnsureGrab(target);grab.smoothPosition=true;grab.smoothPositionAmount=8;grab.smoothRotation=true;grab.smoothRotationAmount=8;EditorUtility.SetDirty(grab);EditorUtility.SetDirty(pickup);
            }
            flow.interactiveCells=moving.ToArray();EditorUtility.SetDirty(flow);
            foreach(var spec in new[]{(id:"hemoglobin",carrier:"rbc",offset:new Vector3(-.33f,.14f,0)),(id:"heme",carrier:"hemoglobin",offset:new Vector3(.33f,.14f,0)),(id:"oxygen",carrier:"heme",offset:new Vector3(0,.16f,0))})
            {
                var target=m.world.Find(spec.id);EnsureGrab(target);
                var cargo=target.GetComponent<JourneyBloodCargo>();if(!cargo)cargo=target.gameObject.AddComponent<JourneyBloodCargo>();
                cargo.target=target;cargo.carrier=m.world.Find(spec.carrier);cargo.displayOffset=spec.offset;EditorUtility.SetDirty(cargo);
            }
            foreach(string id in new[]{"hemoglobin","heme","oxygen","co2","plasma"})
            {
                var target=m.world.Find(id);if(!target)continue;
                foreach(var renderer in target.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(!renderer.enabled)continue;
                    for(var t=renderer.transform;t&&t!=target.transform;t=t.parent)t.gameObject.SetActive(true);
                }
            }
            art.bloodLabels=new Text[4];
            for(int i=0;i<4;i++)
            {
                var name="Scene03 orientation label — "+ids[i];var old=art.transform.Find(name);var text=old?old.GetComponent<Text>():ArteryAnnotations.Label(art.transform,name,captions[i]);text.fontSize=15;text.text=captions[i];text.rectTransform.sizeDelta=new Vector2(270,36);text.transform.localScale=Vector3.one*.00125f;art.bloodLabels[i]=text;
            }
            EditorUtility.SetDirty(art);EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);
        }
        static UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable EnsureGrab(JourneyTarget target)
        {
            var body=target.GetComponent<Rigidbody>();if(!body)body=target.gameObject.AddComponent<Rigidbody>();body.useGravity=false;body.isKinematic=true;
            var grab=target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if(!grab){grab=target.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();grab.movementType=UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Kinematic;grab.throwOnDetach=false;}
            EditorUtility.SetDirty(body);EditorUtility.SetDirty(grab);return grab;
        }
        [MenuItem("Biology VR/Review Actual Desktop Scene03 Start")]
        public static void Capture()=>EditorCoroutineUtility.StartCoroutineOwnerless(Check());
        static IEnumerator Check()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();if(!EditorApplication.isPlaying||m.SceneNumber!=3||m.StepIndex!=0)throw new InvalidOperationException("Fresh Scene03 step0 required; no staging/reset used");
            var settings=InputSystem.settings;var previous=settings.editorInputBehaviorInPlayMode;var bg=settings.backgroundBehavior;Keyboard keyboard=null;Mouse mouse=null;KeyboardState state=new KeyboardState();Vector2 mouseDelta=Vector2.zero;ushort mouseButtons=0;
            string folder=ArteryPolishReview.Reports+"/StartupPC";Directory.CreateDirectory(folder);
            void Queue(){if(InputState.currentUpdateType==InputUpdateType.Dynamic||InputState.currentUpdateType==InputUpdateType.Fixed){InputSystem.QueueStateEvent(keyboard,state);if(InputState.currentUpdateType==InputUpdateType.Dynamic){InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f),buttons=mouseButtons,delta=mouseDelta});mouseDelta=Vector2.zero;}}}
            try
            {
                settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Queue;
                yield return new EditorWaitForSeconds(.5f);
                var w=m.world;var camera=m.mover.viewCamera;var points=new[]{"rbc","leukocyte","platelet","plasma"}.Select(id=>new{id,active=w.Find(id).gameObject.activeInHierarchy,viewport=camera.WorldToViewportPoint(w.Find(id).transform.position).ToString()}).ToArray();
                var screen=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.name=="PC onboarding — screen space");
                File.WriteAllText(folder+"/ActualStart.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,method="Fresh authored Play Mode camera; no camera LookAt/mission stage",scene=m.SceneNumber,step=m.StepIndex,camera=camera.transform.position.ToString(),rotation=camera.transform.rotation.ToString(),screenHud=screen.enabled,targets=points},Formatting.Indented));
                ScreenCapture.CaptureScreenshot(folder+"/01_Actual_Start_With_PC_Instructions.png");yield return new EditorWaitForSeconds(.35f);
                var look=Quaternion.LookRotation(camera.transform.parent.InverseTransformDirection(w.Find("rbc").transform.position-camera.transform.position),Vector3.up).eulerAngles;var current=camera.transform.localEulerAngles;
                mouseDelta=new Vector2(Mathf.DeltaAngle(current.y,look.y)/.12f,-Mathf.DeltaAngle(current.x,look.x)/.12f);mouseButtons=2;yield return new EditorWaitForSeconds(.1f);mouseButtons=0;yield return new EditorWaitForSeconds(.05f);
                state=new KeyboardState(Key.G);yield return new EditorWaitForSeconds(.18f);state=new KeyboardState();yield return new EditorWaitForSeconds(.2f);
                if(m.StepIndex!=1)throw new InvalidOperationException("Real PC G failed to take RBC");
                state=new KeyboardState(Key.F);yield return new EditorWaitForSeconds(1.15f);state=new KeyboardState();yield return new EditorWaitForSeconds(.2f);
                if(m.StepIndex!=2)throw new InvalidOperationException("Real PC F failed to scan held RBC at centre aim");
                state=new KeyboardState(Key.Z);yield return new EditorWaitForSeconds(.18f);state=new KeyboardState();yield return new EditorWaitForSeconds(.6f);
                if(m.StepIndex!=3||w.Find("rbc").DesktopHeld)throw new InvalidOperationException("PC enlargement must dock RBC and free central view for parts");
                foreach(string id in new[]{"hemoglobin","heme","oxygen"})if(!w.Find(id).GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled&&r.gameObject.activeInHierarchy))throw new InvalidOperationException(id+" collider exists but no visible renderer");
                ScreenCapture.CaptureScreenshot(folder+"/02_G_F_Z_Reveals_Hb_Heme.png");
                File.WriteAllText(folder+"/DesktopInput.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,passed=true,method="Actual KeyboardState G/F/Z Input System events",step=m.StepIndex,visibleHb=w.Find("hemoglobin").GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy),visibleHeme=w.Find("heme").GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy)},Formatting.Indented));
            }
            finally{InputSystem.onBeforeUpdate-=Queue;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);settings.editorInputBehaviorInPlayMode=previous;settings.backgroundBehavior=bg;}
        }
    }
}

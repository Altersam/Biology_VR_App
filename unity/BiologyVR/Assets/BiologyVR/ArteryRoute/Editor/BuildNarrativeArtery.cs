using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using BiologyVR.ArteryRoute;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class BuildNarrativeArtery
    {
        public const string ScenePath=JourneyGeometry.Root+"/Scenes/ArteryNarrativeVR.unity";
        public static string LastResult{get;private set;}="Not built";
        static bool pending;
        [MenuItem("Biology VR/Build Spatial Narrative Artery")]
        public static void Schedule(){if(pending)return;pending=true;LastResult="Building";EditorApplication.delayCall+=Run;}
        [MenuItem("Biology VR/Build Spatial Narrative Artery Now")]
        public static void BuildNow(){if(pending){pending=false;EditorApplication.delayCall-=Run;}Run();}
        static void Run(){try{Build();}catch(Exception e){LastResult="FAILED: "+e;Debug.LogException(e);}finally{pending=false;}}
        static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            ArteryGlbImporter.Folder(JourneyGeometry.Root+"/Generated");ArteryGlbImporter.Folder(JourneyGeometry.Root+"/Scenes");ArteryGlbImporter.Folder(JourneyGeometry.Root+"/Reports");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(BuildArteryVrScene.Root+"/Imported/Visual/HybridArteryVisual.prefab");if(!source)throw new InvalidOperationException("First import the hybrid visual assets");
            var path=JourneyGeometry.BuildPath();var branches=JourneyGeometry.Branches(path);var material=JourneyGeometry.VesselMaterial();
            var previous=SceneManager.GetActiveScene();if(previous.isDirty&&!string.IsNullOrEmpty(previous.path))EditorSceneManager.SaveScene(previous);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);scene.name="ArteryNarrativeVR";
            var environment=new GameObject("Organic Spatial Artery — 3D Branching Lumen");
            var mainMesh=JourneyGeometry.Tube(path,branches);JourneyGeometry.Save(mainMesh,JourneyGeometry.Root+"/Generated/MainLumen.asset");
            var wall=Surface("Continuous Main Artery",AssetDatabase.LoadAssetAtPath<Mesh>(JourneyGeometry.Root+"/Generated/MainLumen.asset"),material,environment.transform);
            for(int i=0;i<branches.Length;i++)
            {
                var mesh=JourneyGeometry.Tube(branches[i],null,true,path);JourneyGeometry.Save(mesh,JourneyGeometry.Root+$"/Generated/Branch_{i}.asset");
                Surface($"Organic side branch {i+1}",AssetDatabase.LoadAssetAtPath<Mesh>(JourneyGeometry.Root+$"/Generated/Branch_{i}.asset"),material,environment.transform);
            }
            var overlay=new GameObject("Teaching Overlay — optional flow guides");
            var guide=new GameObject("Direction of blood flow").AddComponent<LineRenderer>();guide.transform.SetParent(overlay.transform,false);guide.positionCount=path.points.Length/5+1;guide.widthMultiplier=.020f;guide.material=Unlit("Guide",new Color(.10f,.63f,.93f));guide.useWorldSpace=true;
            for(int i=0;i<guide.positionCount;i++)guide.SetPosition(i,path.Offset(path.Length*i/(guide.positionCount-1),0,-path.Radius(path.Length*i/(guide.positionCount-1))+.08f));
            var rigPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");if(!rigPrefab)throw new InvalidOperationException("XR rig prefab absent");
            var rig=(GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);rig.name="XR Origin — Scientific mission";
            foreach(var provider in rig.GetComponentsInChildren<LocomotionProvider>(true))provider.enabled=false;
             var origin=rig.GetComponent<XROrigin>();var camera=origin.Camera;camera.tag="MainCamera";camera.nearClipPlane=.035f;camera.farClipPlane=180;camera.fieldOfView=70;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
             var mover=rig.AddComponent<JourneyMover>();mover.path=path;mover.origin=origin;mover.viewCamera=camera;mover.teachingOverlay=overlay.transform;
             rig.AddComponent<QuestPicoPerformanceProfile>();
            var mission=rig.AddComponent<JourneyMission>();var world=environment.AddComponent<JourneyWorld>();mover.mission=mission;mission.mover=mover;mission.world=world;world.mission=mission;world.mover=mover;
            var library=(GameObject)PrefabUtility.InstantiatePrefab(source);library.name="Source asset templates (hidden)";library.SetActive(false);
            var originals=library.GetComponentsInChildren<Transform>(true);
            world.locationRoots=Enumerable.Range(0,9).Select(i=>new GameObject($"Location {i}: "+new[]{"blood + layers","descent experiment","plaque ascent","gas embolus","virus encounter + wave","pressure loss","ONE WOUND: diagnosis → haemostasis → balance → healing","ONE FINAL FOCUS: immune response → homeostasis","return transition"}[i])).ToArray();
            // Reuse imported artwork, but remove repeated straight-rail navigation and background pools.
            for(int sid=3;sid<=15;sid++)
            {
                if(new[]{9,13,16}.Contains(sid))continue;
                int location=path.locationIds[sid-3];var episode=originals.FirstOrDefault(t=>t.name==$"Episode_{sid:00}");if(!episode)continue;
                foreach(Transform original in episode)
                {
                    string name=original.name;
                     if(original.GetComponent<BloodCellFollower>()||name.Contains("_Lesson_")||name.Contains("_Title_")||name.Contains("_HUD_")||name.Contains("Label")||name.Contains("_Model_Edge")||name.Contains("Interaction")||name.Contains("Professor")||name.Contains("Scale_Note")||name.Contains("Guide")||name.Contains("Outline")||name.Contains("Leak_Arrow")||name.Contains("Flow_Arrow"))continue;
                    var clone=UnityEngine.Object.Instantiate(original.gameObject);clone.name=name;clone.SetActive(true);clone.transform.SetParent(world.locationRoots[location].transform,true);
                    Reproject(clone.transform,(sid-3)*24-4,path.Anchor(sid),path);
                    foreach(var flow in clone.GetComponentsInChildren<BloodCellFollower>(true))UnityEngine.Object.DestroyImmediate(flow);
                }
            }
            var targets=new List<JourneyTarget>();
            // A coherent near-hand learning deck, re-used at each shared location as needed.
             var models=new Dictionary<string,string>{{"rbc","AB_S03_RBC_Study"},{"hemoglobin","AB_S03_Hemoglobin"},{"heme","AB_S03_Heme_Study"},{"oxygen","AB_S03_O2"},{"co2","AB_S03_CO2"},{"leukocyte","AB_S03_Leukocyte_Study"},{"platelet","AB_S03_Platelet_Study"},{"plasma","AB_S03_Plasma_Solute_0"},{"virus-study","AB_S08_Virion_Cutaway"},{"genome","AB_S08_Virion_Cutaway"},{"capsid","AB_S08_Virion_Cutaway"},{"epitope","AB_S08_Virion_Cutaway"},{"plaque","AB_S06_Plaque"},{"lipid","HD_S06_Lipid_Droplet_0"},{"cap","HD_S06_Cap_Fiber_0"},{"embolus","AB_S07_Gas_Embolus"},{"phagocyte","AB_S14_Macrophage"},{"thrombin","AB_S12_Thrombin"},{"wound","AB_S12_Defect_Outline"},{"leak","AB_S11_Leak_RBC_0"},{"fibrin","HD_S12_Dense_Fibrin_Network"},{"clot","AB_S12_Clot_Root"},{"repair","AB_S14_Healing_Cell_0"},{"healed-wall","AB_S14_Healing_Cell_0"},{"neutralized","AB_S15_Neutralized_Virion"},{"antibody-A","AB_S15_Antibody_Choice_0"},{"antibody-B","AB_S15_Antibody_Choice_1"},{"antibody-C","AB_S15_Antibody_Choice_2"},{"infected-cell","AB_S15_Infected_Source"},{"t-cell","AB_S15_Cytotoxic_T"},{"debris","HD_S14_Debris_0"}};
             models["intima"]="AB_S04_Layer_0";models["media"]="AB_S04_Layer_1";models["adventitia"]="AB_S04_Layer_2";
            string[] semantic={"rbc","hemoglobin","heme","oxygen","co2","leukocyte","platelet","plasma","endothelium","layers","intima","media","adventitia","flow","flow-model","small-radius","large-radius","pressure","normal-flow","plaque","plaque-flow","pulse-mode","lipid","cap","embolus","attract-mode","virus-study","genome","capsid","epitope","immune-mode","phagocyte","pressure-low","pressure-stable","pressure-return","tone-mode","tone","wound","leak","thrombin","fibrin","clot","insufficient","excessive","optimal","open-flow","debris","repair","healed-wall","antibody-A","antibody-B","antibody-C","neutralized","infected-cell","t-cell","remaining","vitals","homeostasis","return-field"};
            for(int i=0;i<semantic.Length;i++)
            {
                string id=semantic[i];int sceneNumber=TargetScene(id);float s=path.Anchor(sceneNumber);
                var template=models.TryGetValue(id,out var sourceName)?originals.FirstOrDefault(t=>t.name==sourceName):null;
                if(id=="debris"&&template==null)template=originals.FirstOrDefault(t=>t.name.Contains("S14_Debris"));
                 var ob=new GameObject("Research target — "+id);
                 if(template&&template.GetComponentsInChildren<MeshFilter>(true).Length>0)
                 {
                     var visual=UnityEngine.Object.Instantiate(template.gameObject,ob.transform);visual.name=template.name+" — complete educational visual";visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
                     ob.transform.localScale=Vector3.one*(id=="rbc"?.17f:id=="plaque"?.42f:id=="embolus"?.45f:.24f);
                 }
                 else
                 {
                     var model=GameObject.CreatePrimitive(PrimitiveType.Sphere);model.transform.SetParent(ob.transform,false);model.transform.localScale=Vector3.one*.085f;UnityEngine.Object.DestroyImmediate(model.GetComponent<Collider>());model.GetComponent<Renderer>().sharedMaterial=Unlit(id,new Color(.12f,.60f,.86f));
                 }
                 var target=ob.AddComponent<JourneyTarget>();target.targetId=id;target.label=id;target.mission=mission;targets.Add(target);
                 int deckColumn=i%4-1;int deckRow=(i/4)%3;float x=deckColumn*.48f;float y=-.72f+deckRow*.42f;float z=.72f+(i/12)*.24f;ob.transform.position=path.Offset(s+z,x,y);ob.transform.rotation=path.Frame(s)*Quaternion.Euler(70,0,0);
                 ob.transform.SetParent(world.locationRoots[path.locationIds[sceneNumber-3]].transform,true);
                 var bounds=new Bounds(ob.transform.position,Vector3.one*.12f);foreach(var r in ob.GetComponentsInChildren<Renderer>(true))bounds.Encapsulate(r.bounds);var collider=ob.AddComponent<SphereCollider>();collider.center=ob.transform.InverseTransformPoint(bounds.center);collider.radius=Mathf.Clamp(bounds.extents.magnitude/Mathf.Max(.001f,ob.transform.lossyScale.x),.08f,1.2f);
                 ob.SetActive(false);
                if(new[]{"rbc","oxygen","leukocyte","platelet","hemoglobin","antibody-A","antibody-B","antibody-C"}.Contains(id))
                {
                    var rb=ob.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;var grab=ob.AddComponent<XRGrabInteractable>();grab.throwOnDetach=false;grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.useDynamicAttach=true;
                }
            }
             world.targets=targets.ToArray();
             foreach(var diagnostic in targets.Where(t=>t.targetId=="wound"))foreach(var r in diagnostic.GetComponentsInChildren<Renderer>(true))r.enabled=false;
             Func<string,Transform> T=id=>targets.First(x=>x.targetId==id).transform;
             var plaqueTarget=T("plaque");foreach(Transform child in plaqueTarget.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
             plaqueTarget.localScale=Vector3.one;
             foreach(var part in world.locationRoots[2].GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("AB_S06_Plaque")||t.name.StartsWith("AB_S06_FoamCell")||t.name.StartsWith("AB_S06_Lipid_")||t.name.StartsWith("HD_S06_Lipid_Droplet")||t.name.StartsWith("HD_S06_Cap_Fiber")).ToArray())part.SetParent(plaqueTarget,true);
             plaqueTarget.position=path.Offset(path.Anchor(6)+4,2.65f,-.2f);BiologicalLookDev.FitAssembly(plaqueTarget,plaqueTarget.position,2.4f);
             var plaqueCollider=plaqueTarget.GetComponent<SphereCollider>();plaqueCollider.center=Vector3.zero;plaqueCollider.radius=1.35f;
             world.bubble=T("embolus");world.infected=T("infected-cell");world.tCell=T("t-cell");world.phagocyte=T("phagocyte");world.flowModel=T("flow-model");
             var sourceCell=originals.First(t=>t.name=="AB_S15_Infected_Source");
             foreach(var part in world.locationRoots[7].GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("AB_S15_Budding_Virion")||t.name.StartsWith("HD_S15_Intracellular_Virion")).ToArray())
             {
                 var reference=originals.First(t=>t.name==part.name);part.SetParent(world.infected,false);part.localPosition=sourceCell.InverseTransformPoint(reference.position);part.localRotation=Quaternion.Inverse(sourceCell.rotation)*reference.rotation;var scale=sourceCell.lossyScale;part.localScale=new Vector3(reference.lossyScale.x/scale.x,reference.lossyScale.y/scale.y,reference.lossyScale.z/scale.z);
             }
             foreach(var duplicate in world.locationRoots[7].GetComponentsInChildren<Transform>(true).Where(t=>new[]{"AB_S15_Infected_Source","AB_S15_Cytotoxic_T","AB_S15_Macrophage","AB_S15_Neutralized_Virion"}.Contains(t.name)).ToArray())UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
             world.remainingComplexes=Enumerable.Range(0,3).Select(i=>{var ob=CloneMesh(originals,"AB_S15_Neutralized_Virion","Residual neutralized complex "+i);ob.transform.SetParent(world.locationRoots[7].transform,false);ob.transform.position=path.Offset(path.Anchor(15)+2+i*.45f,-.6f+i*.55f,-.4f);ob.transform.rotation=path.Frame(path.Anchor(15));ob.transform.localScale=Vector3.one*.25f;ob.SetActive(false);return ob;}).ToArray();
            var wound=new GameObject("WALL_A — persistent defect/state controller");wound.transform.position=path.Offset(path.Anchor(11)+5,path.Radius(path.Anchor(11)+5)-.10f,0);world.woundSite=wound.transform;
            T("wound").position=world.woundSite.position;T("healed-wall").position=world.woundSite.position;
            var trap=GameObject.CreatePrimitive(PrimitiveType.Sphere);trap.name="Embolus stabilization trigger";trap.transform.position=path.Offset(path.Anchor(7)+3,-1.8f,-.7f);trap.transform.localScale=Vector3.one*.75f;trap.GetComponent<SphereCollider>().isTrigger=true;trap.GetComponent<Renderer>().sharedMaterial=Unlit("Safe stabilization",new Color(.05f,.55f,.8f));world.trap=trap.transform;trap.transform.SetParent(world.locationRoots[3].transform,true);
            var clotRoot=new GameObject("ONE plug and stable clot — WALL_A");clotRoot.transform.SetParent(world.locationRoots[6].transform,false);clotRoot.transform.position=world.woundSite.position-path.Right(path.Anchor(11)+5)*.8f;world.clot=clotRoot.transform;
            var clotAssets=world.locationRoots[6].GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Plug_Platelet")||t.name.Contains("Clot_RBC")).ToArray();foreach(var t in clotAssets)t.SetParent(clotRoot.transform,true);
            var fibrinRoot=new GameObject("ONE fibrin network growing over platelet plug");fibrinRoot.transform.SetParent(world.locationRoots[6].transform,false);fibrinRoot.transform.position=clotRoot.transform.position;world.fibrin=fibrinRoot;
            foreach(var t in world.locationRoots[6].GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Fibrin_")||t.name.Contains("Dense_Fibrin")).ToArray())t.SetParent(fibrinRoot.transform,true);
            world.platelets=Enumerable.Range(0,6).Select(i=>{var ob=CloneMesh(originals,"AB_S12_Approaching_Platelet_0","Controllable platelet "+i);ob.transform.position=path.Offset(path.Anchor(11)+2,-1.2f+i*.35f,-.35f);ob.transform.localScale=Vector3.one*.16f;ob.transform.SetParent(world.locationRoots[6].transform,true);return ob;}).ToArray();
            world.debris=Enumerable.Range(0,3).Select(i=>{var ob=CloneMesh(originals,originals.First(t=>t.name.Contains("S14_Debris")).name,"Cellular debris "+i);ob.transform.position=path.Offset(path.Anchor(11)+4+i*.45f,2.7f,-.45f+i*.3f);ob.transform.localScale=Vector3.one*.3f;ob.transform.SetParent(world.locationRoots[6].transform,true);return ob;}).ToArray();
             var backingMaterial=AssetDatabase.LoadAssetAtPath<Material>(BuildArteryVrScene.Root+"/Imported/Visual/Materials/HD_CellPink.mat");
             if(!backingMaterial)throw new InvalidOperationException("Missing HD_CellPink tissue material");
             BiologicalLookDev.Patch("WALL_A_Subendothelial_Matrix",path,.18f,environment.transform,backingMaterial);
             var collagenRoot=new GameObject("WALL_A exposed collagen scaffold");collagenRoot.transform.SetParent(environment.transform,false);collagenRoot.transform.position=world.woundSite.position;
             var collagen=CloneMesh(originals,"HD_S12_Dense_Fibrin_Network","Subendothelial collagen fibre geometry");collagen.transform.SetParent(collagenRoot.transform,false);collagen.transform.localPosition=Vector3.zero;collagen.transform.rotation=path.Frame(path.Anchor(11)+5);collagen.transform.localScale=Vector3.one;
             BiologicalLookDev.FitAssembly(collagenRoot.transform,world.woundSite.position+path.Right(path.Anchor(11)+5)*.08f,1.9f);
             var repairMaterial=new Material(material){name="RecoveredEndothelium"};repairMaterial.SetFloat("_PulseAmplitude",0);repairMaterial.SetFloat("_Reveal",0);JourneyGeometry.Save(repairMaterial,JourneyGeometry.Root+"/Generated/RecoveredEndothelium.mat");
             world.repairPatch=BiologicalLookDev.Patch("WALL_A_Recovered_Endothelium",path,-.025f,world.locationRoots[6].transform,AssetDatabase.LoadAssetAtPath<Material>(JourneyGeometry.Root+"/Generated/RecoveredEndothelium.mat"));
             BiologicalLookDev.FitAssembly(clotRoot.transform,world.woundSite.position-path.Right(path.Anchor(11)+5)*.20f,2.4f);
             BiologicalLookDev.FitAssembly(fibrinRoot.transform,world.woundSite.position-path.Right(path.Anchor(11)+5)*.30f,2.65f);
            var blocked=GameObject.CreatePrimitive(PrimitiveType.Sphere);blocked.name="Checkpoint thrombotic occlusion / excess response";blocked.transform.position=path.Centre(path.Anchor(11)+4);blocked.transform.localScale=Vector3.one*2;blocked.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(BuildArteryVrScene.Root+"/Imported/Visual/Materials/HD_Violet.mat");world.occlusion=blocked;
            world.toneWall=new[]{wall.GetComponent<Renderer>()};
            world.virusPrefab=CloneMesh(originals,"AB_S09_Wave_Virion_0","Incoming virus template");world.virusPrefab.SetActive(false);world.virusPrefab.transform.SetParent(environment.transform,true);
            AddBlood(originals,path,mover,mission,environment.transform);
             world.labRoom=null;
            var input=rig.AddComponent<JourneyInput>();input.mission=mission;input.world=world;input.mover=mover;
            input.scanner=rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Left Controller");input.bioTool=rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Right Controller");
            if(UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>()==null)new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
             BuildUi(camera,mission,input);
             BiologicalLookDev.Apply(world.locationRoots.Concat(new[]{environment}).ToArray());
             camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.009f,.018f);
             RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.22f,.15f,.18f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.024f;RenderSettings.fogColor=new Color(.34f,.10f,.13f);
            var key=new GameObject("Moving lumen key light").AddComponent<Light>();key.type=LightType.Point;key.range=19;key.intensity=7;key.color=new Color(1,.78f,.66f);key.transform.SetParent(camera.transform,false);key.transform.localPosition=new Vector3(.8f,1.3f,3.2f);
            var rim=new GameObject("Soft cool fill").AddComponent<Light>();rim.type=LightType.Point;rim.range=15;rim.intensity=4;rim.color=new Color(.36f,.70f,1);rim.transform.SetParent(camera.transform,false);rim.transform.localPosition=new Vector3(-1.1f,.5f,.9f);
            BuildArteryVrScene.ConfigurePipeline();
            for(int i=0;i<world.locationRoots.Length;i++)world.locationRoots[i].SetActive(i==0);
             origin.transform.SetPositionAndRotation(path.Centre(path.Anchor(3))-Vector3.up*origin.CameraYOffset,Quaternion.LookRotation(path.Forward(path.Anchor(3)),Vector3.up));
             BrightStylizedLabLook.Apply();
             ArteryReferenceVisualOverhaul.Apply();
             EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
             LastResult=JsonConvert.SerializeObject(new{scene=ScenePath,pathLength=path.Length,verticalRange=path.points.Max(p=>p.y)-path.points.Min(p=>p.y),locations=9,branches=branches.Length,sharedVirusAnchor=path.Anchor(8)==path.Anchor(9),sharedWoundAnchors=new[]{11,12,13,14}.All(i=>Mathf.Abs(path.Anchor(i)-path.Anchor(11))<.01f),sharedFinalAnchor=path.Anchor(15)==path.Anchor(16),storyScenes=13,finalBossScene=15,continuousBloodFlow=true,detailedBriefings=true,endsAtArteryVictory=true},Formatting.Indented);
            File.WriteAllText(JourneyGeometry.Root+"/Reports/BuildReport.json",LastResult);Debug.Log("Spatial narrative artery READY: "+LastResult);
            Selection.activeGameObject=environment;
        }
        static GameObject Surface(string name,Mesh mesh,Material material,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;}
        static void Reproject(Transform t,float oldAnchor,float newAnchor,ArteryJourneyPath path)
        {
            float best=10000,oldY=oldAnchor;
            for(float y=oldAnchor-12;y<oldAnchor+25;y+=.2f){float d=(ArteryRouteController.Centre(y)-t.position).sqrMagnitude;if(d<best){best=d;oldY=y;}}
            var delta=t.position-ArteryRouteController.Centre(oldY);float x=Vector3.Dot(delta,ArteryRouteController.Right(oldY)),z=Vector3.Dot(delta,ArteryRouteController.Up(oldY));float s=newAnchor+(oldY-oldAnchor)*1.2f;
            var oldFrame=Quaternion.LookRotation(ArteryRouteController.Forward(oldY),ArteryRouteController.Up(oldY));var rotation=path.Frame(s)*Quaternion.Inverse(oldFrame)*t.rotation;t.SetPositionAndRotation(path.Offset(s,x,z),rotation);
        }
        static int TargetScene(string id)
        {
            if(new[]{"endothelium","layers","intima","media","adventitia"}.Contains(id))return 4;
            if(new[]{"flow","flow-model","small-radius","large-radius","pressure","normal-flow"}.Contains(id))return 5;
            if(new[]{"plaque","plaque-flow","pulse-mode","lipid","cap"}.Contains(id))return 6;
            if(new[]{"embolus","attract-mode"}.Contains(id))return 7;
            if(new[]{"virus-study","genome","capsid","epitope","immune-mode","phagocyte"}.Contains(id))return 8;
            if(new[]{"pressure-low","pressure-stable","pressure-return","tone-mode","tone"}.Contains(id))return 10;
            if(new[]{"wound","leak","platelet","thrombin","fibrin","clot","insufficient","excessive","optimal","open-flow","debris","repair","healed-wall"}.Contains(id))return id=="platelet"?3:11;
            if(new[]{"antibody-A","antibody-B","antibody-C","neutralized","infected-cell","t-cell","remaining","vitals","homeostasis"}.Contains(id))return 15;
            if(id=="return-field")return 17;return 3;
        }
        static GameObject CloneMesh(Transform[] originals,string name,string newName)
        {
            var source=originals.FirstOrDefault(t=>t.name==name);if(!source)throw new InvalidOperationException("Missing imported model: "+name);
            var go=UnityEngine.Object.Instantiate(source.gameObject);go.name=newName;go.transform.SetParent(null,false);foreach(var flow in go.GetComponentsInChildren<BloodCellFollower>(true))UnityEngine.Object.DestroyImmediate(flow);return go;
        }
        static Material Unlit(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name};m.SetColor("_BaseColor",color);return m;}
        static void AddBlood(Transform[] originals,ArteryJourneyPath path,JourneyMover mover,JourneyMission mission,Transform parent)
        {
            var flow=parent.gameObject.AddComponent<JourneyBloodFlow>();flow.path=path;flow.mover=mover;flow.mission=mission;
            int count=100;flow.cells=new Transform[count];flow.offsets=new float[count];flow.lanesX=new float[count];flow.lanesY=new float[count];var random=new System.Random(135);
            var rbc=originals.First(t=>t.name=="AB_S03_RBC_Flow_00");var white=originals.First(t=>t.name=="AB_S03_Rare_Leukocyte");var platelet=originals.First(t=>t.name=="AB_S03_Resting_Platelet_0");
            for(int i=0;i<count;i++){var source=i%23==0?white:i%7==0?platelet:rbc;var go=new GameObject("Continuous bloodstream cell "+i);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;go.transform.localScale=Vector3.one*(i%23==0?.5f:i%7==0?.6f:.32f);go.transform.localRotation=Quaternion.Euler(65,i*13,0);flow.cells[i]=go.transform;flow.offsets[i]=i*39f/count;flow.lanesX[i]=(float)(random.NextDouble()*2.1+1)*(i%2==0?1:-1);flow.lanesY[i]=(float)(random.NextDouble()*2.6-1.3);}
        }
        static GameObject Lab(Transform[] source)
        {
             var root=new GameObject("Restored laboratory at 1:1");root.transform.position=new Vector3(-80,0,-40);
             foreach(var spec in new[]{("Floor",new Vector3(0,-.1f,0),new Vector3(8,.18f,8)),("Rear wall",new Vector3(0,2,4),new Vector3(8,4,.18f)),("Research table",new Vector3(0,.7f,1.1f),new Vector3(2,.12f,1.1f)),("Shrinking chamber",new Vector3(2.7f,1.25f,0),new Vector3(1.6f,2.5f,1.6f))}){var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=spec.Item1;box.transform.SetParent(root.transform,false);box.transform.localPosition=spec.Item2;box.transform.localScale=spec.Item3;box.GetComponent<Renderer>().sharedMaterial=Unlit(spec.Item1,spec.Item1=="Shrinking chamber"?new Color(.10f,.35f,.57f):new Color(.68f,.77f,.87f));}
             var teacher=CloneMesh(source,"AB_S03_Professor","Professor in restored laboratory");teacher.transform.SetParent(root.transform,false);teacher.transform.localPosition=new Vector3(0,1.3f,1.0f);teacher.transform.localRotation=Quaternion.Euler(0,180,0);teacher.transform.localScale=Vector3.one*.9f;return root;
        }
        static Canvas Panel(Camera camera,string name,Vector3 p,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=camera;go.AddComponent<ArteryTrackedRaycaster>();var rt=go.GetComponent<RectTransform>();rt.SetParent(camera.transform,false);rt.localPosition=p;rt.sizeDelta=size;rt.localScale=Vector3.one*.0024f;
            var back=new GameObject("Background",typeof(RectTransform),typeof(Image)).GetComponent<Image>();back.color=new Color(.02f,.06f,.115f,.96f);back.raycastTarget=false;Stretch(back.rectTransform,rt);return c;
        }
        static void Stretch(RectTransform t,Transform p){t.SetParent(p,false);t.anchorMin=Vector2.zero;t.anchorMax=Vector2.one;t.offsetMin=t.offsetMax=Vector2.zero;}
        static Text Text(Canvas c,string name,int font,Vector2 min,Vector2 max)
        {
            var text=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=font;text.color=new Color(.78f,.94f,1);text.raycastTarget=false;text.alignment=TextAnchor.UpperLeft;var rt=text.rectTransform;rt.SetParent(c.transform,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(12,7);rt.offsetMax=new Vector2(-12,-7);return text;
        }
        static Button Button(Canvas panel,string name,Vector2 min,Vector2 max,Action click,out Text label)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));var rt=go.GetComponent<RectTransform>();rt.SetParent(panel.transform,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(5,5);rt.offsetMax=new Vector2(-5,-5);go.GetComponent<Image>().color=new Color(.04f,.28f,.40f);var button=go.GetComponent<Button>();button.onClick.AddListener(()=>click());label=new GameObject("Caption",typeof(RectTransform),typeof(Text)).GetComponent<Text>();Stretch(label.rectTransform,rt);label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=19;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.text=name;label.raycastTarget=false;return button;
        }
        static Slider Slider(Canvas canvas,string name,Vector2 min,Vector2 max,float low,float high,float value,Action<float> change)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Slider));var rt=go.GetComponent<RectTransform>();rt.SetParent(canvas.transform,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(13,4);rt.offsetMax=new Vector2(-13,-4);
            var bar=new GameObject("Background",typeof(RectTransform),typeof(Image)).GetComponent<Image>();Stretch(bar.rectTransform,rt);bar.color=new Color(.15f,.32f,.42f);bar.raycastTarget=false;
            var handle=new GameObject("Handle",typeof(RectTransform),typeof(Image)).GetComponent<Image>();handle.rectTransform.SetParent(rt,false);handle.rectTransform.sizeDelta=new Vector2(20,30);handle.color=new Color(.15f,.76f,.90f);
            var caption=new GameObject(name+" label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();caption.rectTransform.SetParent(canvas.transform,false);caption.rectTransform.anchorMin=new Vector2(min.x,min.y+.045f);caption.rectTransform.anchorMax=new Vector2(max.x,min.y+.12f);caption.rectTransform.offsetMin=new Vector2(14,0);caption.rectTransform.offsetMax=new Vector2(-14,0);caption.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");caption.fontSize=15;caption.color=new Color(1f,.92f,.66f,1);caption.text=name+"  ["+low+"–"+high+"]";caption.alignment=TextAnchor.MiddleLeft;caption.raycastTarget=false;
            var s=go.GetComponent<Slider>();s.handleRect=handle.rectTransform;s.targetGraphic=handle;s.minValue=low;s.maxValue=high;s.value=value;s.onValueChanged.AddListener(v=>change(v));return s;
        }
        static void BuildUi(Camera camera,JourneyMission mission,JourneyInput input)
        {
            var hud=mission.gameObject.AddComponent<JourneyHud>();hud.mission=mission;hud.input=input;
            var top=Panel(camera,"Narrative chapter",new Vector3(0,.85f,2.35f),new Vector2(650,70));hud.title=Text(top,"Chapter",27,Vector2.zero,Vector2.one);
             var left=Panel(camera,"Current scenario actions",new Vector3(-1.02f,.10f,2.35f),new Vector2(335,420));hud.goal=Text(left,"Goal",24,new Vector2(0,.58f),Vector2.one);hud.progress=Text(left,"Progress",18,new Vector2(0,.50f),new Vector2(1,.62f));hud.feedback=Text(left,"Feedback",20,new Vector2(0,.28f),new Vector2(1,.52f));
             Button(left,"Действие",new Vector2(0,.15f),new Vector2(1,.28f),input.FocusedAction,out hud.primaryLabel);hud.continueButton=Button(left,"Продолжить →",Vector2.zero,new Vector2(1,.15f),mission.Continue,out var ignored);
             var info=Panel(camera,"Learning term and professor briefing",new Vector3(0,-.72f,2.35f),new Vector2(700,170));hud.info=Text(info,"Learning note",18,Vector2.zero,Vector2.one);
             var right=Panel(camera,"Live measurements and experiments",new Vector3(1.02f,.16f,2.35f),new Vector2(340,430));hud.vitals=Text(right,"Measurements",21,new Vector2(0,.61f),Vector2.one);
            hud.radius=Slider(right,"Радиус",new Vector2(.02f,.42f),new Vector2(.98f,.52f),.5f,1.5f,1,mission.SetRadius);hud.pressure=Slider(right,"ΔP",new Vector2(.02f,.28f),new Vector2(.98f,.38f),60,170,120,mission.SetPressure);hud.balance=Slider(right,"Баланс гемостаза",new Vector2(.02f,.40f),new Vector2(.98f,.53f),0,1,.3f,mission.SetBalance);
            Button(right,"Сканер",new Vector2(0,0),new Vector2(.49f,.14f),input.Scan,out ignored);Button(right,"BioTool",new Vector2(.51f,0),new Vector2(1,.14f),input.Primary,out ignored);
            if(UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null)new GameObject("XR EventSystem",typeof(EventSystem),typeof(XRUIInputModule));
        }
    }
}

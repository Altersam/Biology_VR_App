using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using Unity.XR.CoreUtils;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class BuildArteryVrScene
    {
        public const string Root="Assets/BiologyVR/ArteryRoute";
        public const string ScenePath=Root+"/Scenes/AnimeArteryRouteVR.unity";
        public static string LastResult {get;private set;}="Not built";
        static bool pending;
        [MenuItem("Biology VR/Import and Build Complete VR Artery")]
        public static void Schedule()
        {
            if(pending)return;
            pending=true;LastResult="Building";
            EditorApplication.delayCall+=Run;
        }
        static void Run()
        {
            try { Build(); }
            catch(Exception e) {LastResult="FAILED: "+e;Debug.LogException(e);}
            finally {pending=false;}
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building");
            string source=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Anime_Artery_Route/Hybrid_Detailed"));
            string glb=Path.Combine(source,"Anime_Artery_Hybrid_Detailed.glb");
            string physics=Path.Combine(source,"Anime_Artery_Hybrid_Colliders.glb");
            if(!File.Exists(glb)||!File.Exists(physics))throw new FileNotFoundException("Hybrid GLBs missing in "+source);
            var shader=Shader.Find("BiologyVR/Hybrid Artery");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Hybrid URP shader has errors");
            ArteryGlbImporter.Folder(Root+"/Sources");ArteryGlbImporter.Folder(Root+"/Scenes");ArteryGlbImporter.Folder(Root+"/Reports");
            ConfigurePipeline();
            File.Copy(glb,Root+"/Sources/HybridVisual.glb",true);File.Copy(physics,Root+"/Sources/HybridPhysics.glb",true);
            var importer=new ArteryGlbImporter();var visualPrefab=importer.Import(glb,Root+"/Imported/Visual");
            var physicsImporter=new ArteryGlbImporter();var physicsPrefab=physicsImporter.Import(physics,Root+"/Imported/Physics",true);
            var current=SceneManager.GetActiveScene();
            if(current.isDirty && string.IsNullOrEmpty(current.path))throw new InvalidOperationException("Save the current untitled scene first; imported prefabs are ready.");
            if(current.isDirty)EditorSceneManager.SaveScene(current);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);scene.name="AnimeArteryRouteVR";
            var world=new GameObject("Biology VR — Hybrid Artery 03–16");Undo.RegisterCreatedObjectUndo(world,"Build VR artery");
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);visual.transform.SetParent(world.transform,false);
            PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var collision=(GameObject)PrefabUtility.InstantiatePrefab(physicsPrefab);collision.transform.SetParent(world.transform,false);
            const string rigPath="Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            var rigAsset=AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if(rigAsset==null)throw new InvalidOperationException("XR rig missing: "+rigPath);
            var rig=(GameObject)PrefabUtility.InstantiatePrefab(rigAsset);rig.name="XR Origin — Biology VR";
            var origin=rig.GetComponentInChildren<XROrigin>(true);
            if(origin==null||origin.Camera==null)throw new InvalidOperationException("Prefab lacks XROrigin or camera");
            origin.Camera.tag="MainCamera";origin.Camera.nearClipPlane=.035f;origin.Camera.farClipPlane=120;
            origin.Camera.clearFlags=CameraClearFlags.SolidColor;origin.Camera.backgroundColor=new Color(.12f,.035f,.045f);origin.Camera.fieldOfView=70;
            var cameraData=origin.Camera.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;
            foreach(var provider in rig.GetComponentsInChildren<LocomotionProvider>(true))provider.enabled=false;
            if(UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>()==null)new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var route=rig.AddComponent<ArteryRouteController>();route.origin=origin;route.viewCamera=origin.Camera;route.autoTour=false;
            route.episodes=Enumerable.Range(3,14).Select(i=>visual.transform.Find($"Episode_{i:00}")).ToArray();
            route.teachingOverlay=new GameObject("Teaching Overlay — Toggle O");route.teachingOverlay.transform.SetParent(world.transform,false);
            foreach(var t in visual.GetComponentsInChildren<Transform>(true))
                if(t.parent==visual.transform&&(t.name.Contains("Bend_")||t.name.Contains("Flow_Guide")||t.name.Contains("Flow_Arrow")))t.SetParent(route.teachingOverlay.transform,true);
            // Conservative visibility window, no renderer/Animator cost for distant episode props.
            for(int i=0;i<route.episodes.Length;i++)if(route.episodes[i])route.episodes[i].gameObject.SetActive(i<=1);
            CreateLighting(world.transform,rig.transform);
            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.40f,.31f,.37f);RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.44f,.18f,.16f);RenderSettings.fogDensity=.010f;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.18f);bloom.threshold.Override(1.1f);
            var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.Override(.35f);grade.saturation.Override(8);grade.contrast.Override(5);
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var previous=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Imported/ArteryLighting.asset");
            if(previous!=null){UnityEngine.Object.DestroyImmediate(profile);profile=previous;}else AssetDatabase.CreateAsset(profile,Root+"/Imported/ArteryLighting.asset");
            var volume=new GameObject("Hybrid color grade and soft bloom").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            var presenter=rig.AddComponent<ArteryStudyPresenter>();presenter.route=route;
            presenter.rightController=rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Right Controller");
            AttachInstrument(visual,rig,"AB_Preview_Scanner","Left Controller","Left-hand Scanner");
            AttachInstrument(visual,rig,"AB_Preview_BioTool","Right Controller","Right-hand BioTool");
            presenter.studyTemplates=new[]{StudyTemplate(visual,"AB_S03_RBC_Study","RBC_Study"),StudyTemplate(visual,"AB_S03_Leukocyte_Study","WBC_Study"),StudyTemplate(visual,"AB_S03_Platelet_Study","Platelet_Study")};
            CreateUi(route,presenter,origin.Camera);
            var eye=ArteryRouteController.Centre(-4)+ArteryRouteController.Up(-4)*.1f;
            origin.transform.SetPositionAndRotation(eye,Quaternion.LookRotation((ArteryRouteController.Centre(4)-eye).normalized,Vector3.up));
            ConfigureOpenXr();
            EditorSceneManager.SaveScene(scene,ScenePath);
            var otherScenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();otherScenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=otherScenes.ToArray();
            AssetDatabase.SaveAssets();Selection.activeGameObject=world;
            if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.AlignViewToObject(origin.Camera.transform);
            LastResult=JsonConvert.SerializeObject(new {scene=ScenePath,visualNodes=importer.NodeCount,visualMeshes=importer.MeshCount,physicsMeshes=physicsImporter.MeshCount,episodeCount=route.episodes.Length,flowObjects=visual.GetComponentsInChildren<BloodCellFollower>(true).Length,xrOrigin=origin.name,xrControllers=rig.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Left Controller"||t.name=="Right Controller"),materials=AssetDatabase.FindAssets("t:Material",new[]{Root+"/Imported"}).Length,offlineImport=true});
            File.WriteAllText(Root+"/Reports/ImportReport.json",LastResult);
            Debug.Log("Biology VR READY: "+LastResult);
        }
        static void ConfigureOpenXr()
        {
            var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if(settings!=null)
            {
                foreach(var feature in settings.GetFeatures<OpenXRInteractionFeature>())
                    if(new[]{"OculusTouchControllerProfile","ValveIndexControllerProfile","HTCViveControllerProfile","MicrosoftMotionControllerProfile","KHRSimpleControllerProfile"}.Contains(feature.GetType().Name)){Undo.RecordObject(feature,"Enable VR controller profile");feature.enabled=true;EditorUtility.SetDirty(feature);}
                EditorUtility.SetDirty(settings);
            }
            var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if(general!=null){Undo.RecordObject(general,"Enable OpenXR startup");general.InitManagerOnStart=true;EditorUtility.SetDirty(general);}
        }
        [MenuItem("Biology VR/Configure Artery VR Lighting")]
        public static void ConfigurePipeline()
        {
            const string path=Root+"/ArteryVR_URP.asset";
            var own=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if(own==null)
            {
                var original=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if(original==null)throw new InvalidOperationException("Project has no URP asset");
                own=UnityEngine.Object.Instantiate(original);own.name="Biology VR Artery URP";
                AssetDatabase.CreateAsset(own,path);
            }
            Undo.RecordObject(own,"Configure VR artery lighting");
            var serialized=new SerializedObject(own);
            var mode=serialized.FindProperty("m_AdditionalLightsRenderingMode");
            if(mode==null)throw new InvalidOperationException("URP additional-light property not found");
            mode.intValue=(int)LightRenderingMode.PerPixel;
            serialized.ApplyModifiedProperties();
            own.maxAdditionalLightsCount=4;own.supportsHDR=true;own.useSRPBatcher=true;own.msaaSampleCount=4;
            EditorUtility.SetDirty(own);
            GraphicsSettings.defaultRenderPipeline=own;QualitySettings.renderPipeline=own;
            AssetDatabase.SaveAssets();
            Debug.Log("Biology VR: dedicated URP asset, four per-pixel lights, MSAA 4x and SRP batching enabled.");
        }
        [MenuItem("Biology VR/Capture Desktop Preview")]
        public static void CapturePreview()
        {
            var camera=Camera.main;if(camera==null)throw new InvalidOperationException("No main camera");
            ArteryGlbImporter.Folder(Root+"/Reports");
            var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32);
            var request=new UniversalRenderPipeline.SingleCameraRequest {destination=rt};
            var old=RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=rt;
                var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                var route=UnityEngine.Object.FindFirstObjectByType<ArteryRouteController>();
                File.WriteAllBytes(Root+"/Reports/Preview_"+(route?route.station+3:3).ToString("D2")+".png",image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally {RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            Debug.Log("Biology VR: full-resolution preview saved in Reports.");
        }
        static GameObject StudyTemplate(GameObject visual,string name,string filename)
        {
            var source=visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
            if(source==null)throw new InvalidOperationException("Study model absent: "+name);
            var filter=source.GetComponent<MeshFilter>();var renderer=source.GetComponent<MeshRenderer>();
            if(filter==null||renderer==null)throw new InvalidOperationException("No mesh on "+name);
            var go=new GameObject(filename);go.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Imported/"+filename+".prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;
        }
        static void AttachInstrument(GameObject visual,GameObject rig,string sourceName,string controllerName,string name)
        {
            var source=visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==sourceName);
            var controller=rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==controllerName);
            if(source==null||controller==null)throw new InvalidOperationException("Missing instrument or controller: "+sourceName);
            var tool=new GameObject(name);tool.transform.SetParent(controller,false);
            tool.transform.localPosition=new Vector3(0,.05f,.055f);tool.transform.localRotation=Quaternion.Euler(0,180,0);tool.transform.localScale=Vector3.one*.45f;
            tool.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
            tool.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;
        }
        static void CreateLighting(Transform world,Transform rig)
        {
            var sun=new GameObject("Soft directional fill").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.90f,.83f,1);sun.intensity=.4f;sun.transform.rotation=Quaternion.Euler(45,-25,0);sun.shadows=LightShadows.None;
            for(int i=0;i<14;i++)
            {
                var key=new GameObject($"Episode {i+3:00} soft lumen light").AddComponent<Light>();key.type=LightType.Point;key.range=15;key.intensity=5;key.color=new Color(1,.66f,.55f);key.shadows=LightShadows.None;
                key.transform.SetParent(world,false);key.transform.position=ArteryRouteController.Centre(i*24+5)+Vector3.up*1.8f;
            }
            var follow=new GameObject("Near-camera blue rim").AddComponent<Light>();follow.type=LightType.Point;follow.range=12;follow.intensity=3;follow.color=new Color(.45f,.72f,1);follow.transform.SetParent(rig,false);follow.transform.localPosition=new Vector3(-1.5f,1.2f,2);
        }
        static Canvas CanvasPanel(string name,Camera camera,Vector3 p,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            go.AddComponent<ArteryTrackedRaycaster>();var rt=go.GetComponent<RectTransform>();rt.SetParent(camera.transform,false);rt.localPosition=p;rt.localScale=Vector3.one*.0022f;rt.sizeDelta=size;
            var image=new GameObject("Background",typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.color=new Color(.018f,.065f,.12f,.95f);image.raycastTarget=false;Stretch(image.rectTransform,rt);
            return canvas;
        }
        static void Stretch(RectTransform child,Transform parent){child.SetParent(parent,false);child.anchorMin=Vector2.zero;child.anchorMax=Vector2.one;child.offsetMin=child.offsetMax=Vector2.zero;}
        static Text Text(string name,Canvas panel,int size,Vector2 min,Vector2 max)
        {
            var text=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=new Color(.82f,.95f,1);text.raycastTarget=false;
            text.alignment=TextAnchor.UpperLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            var rt=text.rectTransform;rt.SetParent(panel.transform,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(14,10);rt.offsetMax=new Vector2(-14,-10);return text;
        }
        static void Button(string caption,Canvas panel,float x,Action callback)
        {
            var go=new GameObject(caption,typeof(RectTransform),typeof(Image),typeof(Button));var rt=go.GetComponent<RectTransform>();rt.SetParent(panel.transform,false);rt.anchorMin=new Vector2(x,.02f);rt.anchorMax=new Vector2(x+.30f,.16f);rt.offsetMin=new Vector2(4,4);rt.offsetMax=new Vector2(-4,-4);
            go.GetComponent<Image>().color=new Color(.06f,.33f,.48f);go.GetComponent<Button>().onClick.AddListener(()=>callback());
            var label=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();Stretch(label.rectTransform,rt);label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=18;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.text=caption;label.raycastTarget=false;
        }
        static void CreateUi(ArteryRouteController route,ArteryStudyPresenter p,Camera camera)
        {
            var title=CanvasPanel("VR chapter heading",camera,new Vector3(0,.85f,2.35f),new Vector2(680,82));p.titleText=Text("Chapter",title,28,Vector2.zero,Vector2.one);
            var lesson=CanvasPanel("VR lesson and controls",camera,new Vector3(-1.05f,.12f,2.35f),new Vector2(330,370));p.lessonText=Text("Lesson",lesson,22,new Vector2(0,.17f),Vector2.one);
            Button("←",lesson,.01f,route.PreviousStation);Button("Тур",lesson,.34f,route.ToggleTour);Button("→",lesson,.67f,route.NextStation);
            var hud=CanvasPanel("VR vital-signs snapshot",camera,new Vector3(1.03f,.49f,2.35f),new Vector2(320,120));p.vitalsText=Text("Vitals",hud,22,Vector2.zero,Vector2.one);
            var scan=CanvasPanel("VR scanner result",camera,new Vector3(1.04f,-.15f,2.35f),new Vector2(320,285));p.scanText=Text("Scan",scan,20,new Vector2(0,.16f),Vector2.one);Button("Скан",scan,.04f,p.Scan);Button("Пауза",scan,.39f,route.TogglePause);
            if(UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null){var events=new GameObject("XR Event System",typeof(EventSystem));events.AddComponent<XRUIInputModule>();}
        }
        [MenuItem("Biology VR/Finalize Imported Materials")]
        public static void FinalizeMaterials()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Imported/Visual/Materials/HD_RBC.mat");
            if(material==null)throw new InvalidOperationException("RBC material missing");
            int fixedCount=0;
            foreach(var flow in UnityEngine.Object.FindObjectsByType<BloodCellFollower>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!flow.name.Contains("RBC"))continue;
                var renderer=flow.GetComponent<MeshRenderer>();if(!renderer)continue;
                Undo.RecordObject(renderer,"Restore lit blood-cell material");renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();fixedCount++;
            }
            var current=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(current);EditorSceneManager.SaveScene(current);
            Debug.Log("Biology VR: lit materials restored on "+fixedCount+" background erythrocytes; scene saved.");
        }
    }
}

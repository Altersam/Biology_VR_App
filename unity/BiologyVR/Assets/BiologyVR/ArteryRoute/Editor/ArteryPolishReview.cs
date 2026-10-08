using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryPolishReview
    {
        public const string Reports=JourneyGeometry.Root+"/Reports/Polish";
        public static string LastResult="Not run";
        static string captureFolder;
        [MenuItem("Biology VR/Open Original Preview")]
        public static void OpenOriginal()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(BuildNarrativeArtery.ScenePath);
        }
        [MenuItem("Biology VR/Open VisualRework Preview")]
        public static void OpenRework()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ArteryScenePolish.ScenePath);
        }
        [MenuItem("Biology VR/Inspect Active Geometry")]
        public static void InspectGeometry()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();
            if(EditorApplication.isPlaying&&m)m.Enter(15,false);
            var rows=new List<object>();
            foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var r=f.GetComponent<Renderer>();if(!r||!r.enabled||!f.gameObject.activeInHierarchy||!f.sharedMesh)continue;
                long triangles=0;for(int i=0;i<f.sharedMesh.subMeshCount;i++)triangles+=f.sharedMesh.GetIndexCount(i)/3;
                if(triangles>4000)rows.Add(new{name=f.name,mesh=f.sharedMesh.name,triangles,asset=AssetDatabase.GetAssetPath(f.sharedMesh),target=f.GetComponentInParent<JourneyTarget>()?.targetId});
            }
            File.WriteAllText(Reports+"/HeavyMeshes.json",JsonConvert.SerializeObject(rows,Formatting.Indented));
        }
        [MenuItem("Biology VR/Capture Polished Stations")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
            LastResult="Running";EditorCoroutineUtility.StartCoroutineOwnerless(CaptureAll());
        }
        [MenuItem("Biology VR/Capture Player Baseline")]
        public static void Baseline()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
            Directory.CreateDirectory(Reports);Capture(Camera.main,"01_BEFORE.png");
        }
        static IEnumerator CaptureAll()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var w=m.world;var camera=m.mover.viewCamera;var hud=m.GetComponent<JourneyHud>();
            Directory.CreateDirectory(Reports);var captures=new List<string>();var budgets=new List<object>();
            captureFolder=Reports+"/Capture_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(captureFolder);
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var enabled=canvases.Select(c=>c.enabled).ToArray();
            try
            {
                for(int scene=3;scene<=15;scene++)
                {
                    m.Enter(scene,false);yield return new EditorWaitForSeconds(.30f);
                    foreach(var c in canvases)if(c)c.enabled=false;
                    captures.Add(Capture(camera,$"Station_{scene:00}_Player.png"));
                    if(scene==3)
                    {
                        captures.Add(Capture(camera,"02_AFTER_PLAYER_VIEW.png"));
                        foreach(var c in canvases)if(c)c.enabled=true;
                        if(hud)hud.CloseBriefing();captures.Add(Capture(camera,"06_AFTER_GAMEPLAY_UI.png"));
                        foreach(var c in canvases)if(c)c.enabled=false;
                        // Real APIs, including grab/scan/enlarge; these reveal the molecular teaching phase.
                        var rbc=w.Find("rbc");rbc.KeyboardGrab();m.Accept(StudyAction.Scan,"rbc");rbc.Enlarge();rbc.KeyboardGrab();
                        yield return new EditorWaitForSeconds(.50f);
                        captures.Add(Capture(camera,"Scene03_MolecularReveal.png"));
                    }
                    if(scene==4)
                    {
                        w.Primary();w.Primary();yield return new EditorWaitForSeconds(1.05f);
                        captures.Add(Capture(camera,"Scene04_Layers_Revealed.png"));
                        for(int i=0;i<3;i++){w.Primary();yield return new EditorWaitForSeconds(1.05f);captures.Add(Capture(camera,"Scene04_Layer_Step_"+(i+1)+".png"));}
                    }
                    if(scene==6)
                    {
                        w.Primary();w.Primary();yield return new EditorWaitForSeconds(1.2f);captures.Add(Capture(camera,"Scene06_Plaque_Formation.png"));
                        yield return new EditorWaitForSeconds(1.5f);w.Primary();yield return new EditorWaitForSeconds(.75f);
                        captures.Add(Capture(camera,"Scene06_Three_Lipid_Zones.png"));
                        for(int i=0;i<3;i++){w.Primary(w.plaqueState.Zones[i].Target);yield return new EditorWaitForSeconds(.8f);captures.Add(Capture(camera,"Scene06_Pulse_"+(i+1)+".png"));}
                        yield return new EditorWaitForSeconds(.8f);captures.Add(Capture(camera,"Scene06_Treated_Plaque.png"));
                    }
                    if(scene==12)
                    {
                        for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"platelet");m.Accept(StudyAction.Activate,"platelet");
                        for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"platelet");m.Accept(StudyAction.Activate,"thrombin");m.Accept(StudyAction.Activate,"fibrin");
                        yield return new EditorWaitForSeconds(2.6f);captures.Add(Capture(camera,"Scene12_Platelet_Fibrin.png"));
                    }
                    if(scene==5)
                    {
                        w.Primary();yield return new EditorWaitForSeconds(.3f);captures.Add(Capture(camera,"Scene05_FlowModel.png"));
                    }
                    if(scene==14)
                    {
                        for(int i=0;i<3;i++)w.Primary();w.Primary();
                        yield return new EditorWaitForSeconds(1.6f);captures.Add(Capture(camera,"Scene14_Healing_In_Progress.png"));
                        yield return new EditorWaitForSeconds(2.1f);w.Primary();captures.Add(Capture(camera,"Scene14_Healed_Wall.png"));
                    }
                    budgets.Add(Budget(camera,scene));
                }
                m.Enter(16,false);yield return new EditorWaitForSeconds(.2f);captures.Add(Capture(camera,"Scene16_Homeostasis.png"));
                m.Enter(17,false);yield return new EditorWaitForSeconds(5.8f);captures.Add(Capture(camera,"Scene17_LaboratoryReturn.png"));
                m.Continue();yield return new EditorWaitForSeconds(.2f);captures.Add(Capture(camera,"Scene18_FreeResearch.png"));
                foreach(var c in canvases)if(c)c.enabled=true;captures.Add(Capture(camera,"Scene18_Report_UI.png"));foreach(var c in canvases)if(c)c.enabled=false;
                m.Enter(3,false);yield return new EditorWaitForSeconds(.30f);
                foreach(var c in canvases)if(c)c.enabled=false;
                captures.Add(Closeup(camera,w.Find("rbc"),"04_AFTER_RBC.png",m.mover));
                m.Accept(StudyAction.Grab,"rbc");m.Accept(StudyAction.Scan,"rbc");w.Find("rbc").Enlarge();
                for(int i=0;i<5;i++)w.Primary(); // Hb/heme/O2 detach/bind/CO2 to reach WBC.
                yield return new EditorWaitForSeconds(.20f);
                captures.Add(Closeup(camera,w.Find("leukocyte"),"05_AFTER_WBC.png",m.mover));
                m.Enter(3,false);yield return new EditorWaitForSeconds(.20f);
                var p=camera.transform.position;var q=camera.transform.rotation;
                float s=m.mover.Distance+2;camera.transform.position=m.mover.path.Offset(s,2.25f,.1f);camera.transform.LookAt(m.mover.path.Offset(s+.4f,m.mover.path.Radius(s),.1f));
                captures.Add(Capture(camera,"03_AFTER_TUNNEL.png"));camera.transform.SetPositionAndRotation(p,q);
                var shader=Shader.Find("BiologyVR/Soft Mobile Tissue");
                LastResult=JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),captures,budgets,shaderError=!shader||ShaderUtil.ShaderHasError(shader),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,hardwareVRTested=false,method="Unity URP camera captures at XR player pose; Editor geometry estimates, not Quest profiling"},Formatting.Indented);
                File.WriteAllText(Reports+"/VisualReview.json",LastResult);
            }
            finally
            {
                for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=enabled[i];
                captureFolder=null;
                m.Enter(3,false);
            }
        }
        static object Budget(Camera camera,int scene)
        {
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);int potentialTriangles=0,renderers=0;var mats=new HashSet<Material>();
            foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||!GeometryUtility.TestPlanesAABB(planes,r.bounds))continue;
                var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;renderers++;
                for(int i=0;i<f.sharedMesh.subMeshCount;i++)potentialTriangles+=(int)f.sharedMesh.GetIndexCount(i)/3;
                foreach(var mat in r.sharedMaterials)if(mat)mats.Add(mat);
            }
            return new{scene,potentialFrustumTriangles=potentialTriangles,potentialFrustumRenderers=renderers,materials=mats.Count,editorBatches=UnityStats.batches,editorSetPass=UnityStats.setPassCalls,editorAllocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),gpuFrameTimeAvailable=false};
        }
        static string Closeup(Camera camera,JourneyTarget target,string name,JourneyMover mover)
        {
            var p=camera.transform.position;var q=camera.transform.rotation;float fov=camera.fieldOfView;
            var rs=target.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
            if(rs.Length==0)return "No visible target for "+name;
            var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            camera.transform.position=bounds.center-mover.path.Forward(mover.Distance)*Mathf.Max(.70f,bounds.size.magnitude*1.15f)+mover.path.Up(mover.Distance)*.05f;camera.transform.LookAt(bounds.center);camera.fieldOfView=45;
            try{return Capture(camera,name);}finally{camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=fov;}
        }
        internal static string Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;float previousAspect=camera.aspect;
            try
            {
                camera.aspect=rt.width/(float)rt.height;
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                string path=(captureFolder??Reports)+"/"+name;File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);return path;
            }
            finally{camera.aspect=previousAspect;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}

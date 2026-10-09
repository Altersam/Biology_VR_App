using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryV6Capture
    {
        [MenuItem("Biology VR/Coral V6/Capture First Pass From Player")]
        public static void Run()=>EditorCoroutineUtility.StartCoroutineOwnerless(Capture());
        static IEnumerator Capture()
        {
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();var m=w.mission;
            if(!EditorApplication.isPlaying||m.SceneNumber!=3||m.StepIndex!=0)throw new InvalidOperationException("Fresh authored Scene03 required");
            string folder=ArteryVisualV6.Reports;Directory.CreateDirectory(folder);
            var camera=w.mover.viewCamera;var position=camera.transform.position;var rotation=camera.transform.rotation;
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var enabled=canvases.Select(c=>c.enabled).ToArray();
            try
            {
                yield return new EditorWaitForSeconds(.3f);
                ScreenCapture.CaptureScreenshot(folder+"/01_Player_Overview.png");yield return new EditorWaitForSeconds(.25f);
                var stats=new{batches=UnityStats.batches,drawCalls=UnityStats.drawCalls,triangles=UnityStats.triangles,vertices=UnityStats.vertices};
                foreach(var canvas in canvases)canvas.enabled=false;
                var p=w.mover.path;float s=p.Anchor(3)+3;
                camera.transform.SetPositionAndRotation(p.Offset(s,p.Radius(s)-1.2f,0),Quaternion.LookRotation(p.Right(s),p.Up(s)));
                yield return new EditorWaitForSeconds(.12f);ArteryPolishReview.Capture(camera,"BioWorldV6/02_Wall_Close.png");
                camera.transform.SetPositionAndRotation(position,p.Frame(p.Anchor(3)+5));yield return new EditorWaitForSeconds(.12f);
                ArteryPolishReview.Capture(camera,"BioWorldV6/03_Tunnel_Depth.png");
                var rbc=w.Find("rbc");Vector3 eye=rbc.transform.position-p.Forward(p.Anchor(3))*1.05f+p.Up(p.Anchor(3))*.12f;
                camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(rbc.transform.position-eye,p.Up(p.Anchor(3))));yield return new EditorWaitForSeconds(.12f);
                ArteryPolishReview.Capture(camera,"BioWorldV6/04_RBC_Contrast.png");
                File.WriteAllText(folder+"/Capture.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,method="01 is fresh authored player camera with PC overlay, no scene Enter/Accept or forced facing; 02-04 are camera-only art review views, no mission staging",playerPosition=position.ToString("R"),playerRotation=rotation.ToString("R"),editorStats=stats,statsScope="Editor only, not Quest/Pico GPU/FPS",hardwareVerified=false},Formatting.Indented));
            }
            finally
            {camera.transform.SetPositionAndRotation(position,rotation);for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=enabled[i];}
        }
    }
}

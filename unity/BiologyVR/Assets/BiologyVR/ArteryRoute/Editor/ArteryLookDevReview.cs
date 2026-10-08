using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryLookDevReview
    {
        public static string LastResult {get;private set;}="Not reviewed";
        [MenuItem("Biology VR/Capture Artery Material Review")]
        public static void Capture()
        {
            var mission=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();
            if(!EditorApplication.isPlaying||!mission)throw new InvalidOperationException("Open ArteryNarrativeVR and enter Play Mode");
            var mover=mission.mover;var world=mission.world;
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var enabled=canvases.Select(c=>c.enabled).ToArray();
            var go=new GameObject("Temporary material review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(mover.viewCamera);camera.enabled=false;
            var lamp=go.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=12;lamp.intensity=2.8f;lamp.color=new Color(1,.88f,.82f);
            var target=new RenderTexture(1600,1000,24);var prior=RenderTexture.active;
            var captures=new System.Collections.Generic.List<string>();
            try
            {
                foreach(var canvas in canvases)canvas.enabled=false;
                foreach(int scene in new[]{3,6,11,15})
                {
                    mission.Enter(scene,false);mover.TravelTo(scene,true);
                    camera.transform.SetPositionAndRotation(mover.viewCamera.transform.position,mover.viewCamera.transform.rotation);
                    if(scene==6){var t=world.Find("plaque");camera.transform.LookAt(t.transform.position);}
                    if(scene==11)camera.transform.LookAt(world.woundSite.position);
                    if(scene==15)camera.transform.LookAt(world.infected.position);
                    captures.Add(Render(camera,target,$"Review_{scene:00}_Environment.png"));
                    if(scene==6){var t=world.Find("plaque");camera.transform.position=t.transform.position-mover.path.Right(mover.Distance)*2-mover.path.Forward(mover.Distance)*.9f;camera.transform.LookAt(t.transform.position);captures.Add(Render(camera,target,"Review_Plaque_Closeup.png"));}
                    if(scene==11){float s=mover.path.Anchor(11)+5;camera.transform.position=mover.path.Offset(s,mover.path.Radius(s)-2.0f,-.3f);camera.transform.LookAt(world.woundSite.position);captures.Add(Render(camera,target,"Review_Wound_Closeup_Final.png"));}
                    if(scene==3)
                    {
                        var t=world.Find("rbc");var rs=t.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                        camera.transform.position=bounds.center-mover.path.Forward(mover.Distance)*.8f+mover.path.Up(mover.Distance)*.20f;camera.transform.LookAt(bounds.center);
                        captures.Add(Render(camera,target,"Review_RBC_Membrane.png"));
                        float s=mover.Distance+2;camera.transform.position=mover.path.Offset(s,2.3f,0);camera.transform.LookAt(mover.path.Offset(s,mover.path.Radius(s),0));captures.Add(Render(camera,target,"Review_Endothelium_Closeup.png"));
                    }
                }
                foreach(int scene in new[]{4,5,7,8,9,10,12,13,14})
                {
                    mission.Enter(scene,false);mover.TravelTo(scene,true);
                    camera.transform.SetPositionAndRotation(mover.viewCamera.transform.position,mover.viewCamera.transform.rotation);
                    captures.Add(Render(camera,target,$"Audit_{scene:00}_Clean.png"));
                }
                var wall=AssetDatabase.LoadAssetAtPath<Texture2D>(JourneyGeometry.Root+"/Generated/EndotheliumColor.asset");
                var shaders=new[]{"BiologyVR/Cellular Vessel","BiologyVR/Biological Surface"}.Select(Shader.Find).ToArray();
                LastResult=JsonConvert.SerializeObject(new{captures,wallTexture=wall.width+"x"+wall.height,mipmaps=wall.mipmapCount,shaderErrors=shaders.Any(s=>!s||ShaderUtil.ShaderHasError(s)),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,look="Bright stylized retro-futuristic biological laboratory",palette="coral red / warm orange / cream / lime / turquoise / cyan",targetDevices="Meta Quest + Pico 4 Enterprise via Android OpenXR",mobileProfile="ArteryQuestPico_URP",hardwareVRTested=false},Formatting.Indented);
                File.WriteAllText(JourneyGeometry.Root+"/Reports/MaterialReview.json",LastResult);
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=prior;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);
                for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=enabled[i];
                mission.Enter(3,false);mover.TravelTo(3,true);
            }
        }
        static string Render(Camera camera,RenderTexture rt,string filename)
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            string path=JourneyGeometry.Root+"/Reports/"+filename;File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);return path;
        }
    }
}

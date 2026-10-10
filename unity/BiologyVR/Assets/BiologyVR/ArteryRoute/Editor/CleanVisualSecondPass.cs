using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class CleanVisualSecondPass
    {
        public const string Folder=JourneyGeometry.Root+"/Reports/CleanHolographicSecondPass";
        [MenuItem("Biology VR/Backup Before Holographic Second Pass")]
        public static void Backup()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            Directory.CreateDirectory(Folder+"/Backup");var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();
            EditorSceneManager.SaveScene(world.gameObject.scene);
            string scene=Folder+"/Backup/ArteryNarrativeVR_before_second_pass.unity";
            if(!File.Exists(scene)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,scene))throw new IOException("Scene backup failed");
            foreach(string kind in new[]{"Scanner","BioTool"})foreach(string name in new[]{"DeviceAtlas.png","DeviceEmission.png","Device.mat","Active"+kind+".prefab"})
            {string source=ActiveInstrumentBuilder.Folder+"/"+kind+"/"+name,dest=Folder+"/Backup/"+kind+"_"+name;if(File.Exists(source)&&!File.Exists(dest)&&!AssetDatabase.CopyAsset(source,dest))throw new IOException("Asset backup failed: "+source);}
        }
        [MenuItem("Biology VR/Apply Holographic Readability Second Pass")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var shader=Shader.Find("BiologyVR/Holographic Overlay UI");
            if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Readable UI shader did not compile");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();var input=world.mission.GetComponent<JourneyInput>();
            string before=ArteryVisualV8.GeometrySignature(world);var scanner=input.scanner;var bio=input.bioTool;var scanAction=input.scanAction;var toolAction=input.toolAction;
            ActiveInstrumentBuilder.Apply();CleanHolographicScreensBuilder.Apply();
            string after=ArteryVisualV8.GeometrySignature(world);
            if(before!=after||scanner!=input.scanner||bio!=input.bioTool||scanAction!=input.scanAction||toolAction!=input.toolAction)throw new InvalidOperationException("Presentation changed gameplay geometry/input bindings");
            File.WriteAllText(Folder+"/SecondPass.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometryPreserved=true,geometryBefore=before,geometryAfter=after,aimTransformsPreserved=true,inputActionsPreserved=true,targets=world.targets.Length,bloodPool=world.GetComponent<JourneyBloodFlow>().cells.Length,changes="stronger readable glass / foreground UI / larger typography / active device rim indicators",physicsAdded=0,postEffectsAdded=false,hardwareVerified=false},Formatting.Indented));
        }
    }
}

using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class CleanVisualFirstPass
    {
        public const string Folder=JourneyGeometry.Root+"/Reports/CleanHolographicFirstPass";
        [MenuItem("Biology VR/Backup Before Clean Holographic First Pass")]
        public static void Backup()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            Directory.CreateDirectory(Folder+"/Backup");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();EditorSceneManager.SaveScene(world.gameObject.scene);
            string scene=Folder+"/Backup/ArteryNarrativeVR_before_clean_visual_pass.unity";
            if(!File.Exists(scene)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,scene))throw new IOException("Visual scene backup failed");
            foreach(string kind in new[]{"Scanner","BioTool"})foreach(string name in new[]{"DeviceAtlas.png","Device.mat","Active"+kind+".prefab"})
            {string source=ActiveInstrumentBuilder.Folder+"/"+kind+"/"+name,dest=Folder+"/Backup/"+kind+"_"+name;if(File.Exists(source)&&!File.Exists(dest)&&!AssetDatabase.CopyAsset(source,dest))throw new IOException("Visual asset backup failed: "+source);}
        }
        [MenuItem("Biology VR/Apply Clean Holographic Visual First Pass")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();var input=world.mission.GetComponent<JourneyInput>();
            string before=ArteryVisualV8.GeometrySignature(world);var scanAim=input.scanner;var bioAim=input.bioTool;var scanAction=input.scanAction;var toolAction=input.toolAction;
            ActiveInstrumentBuilder.Apply();CleanHolographicScreensBuilder.Apply();
            string after=ArteryVisualV8.GeometrySignature(world);
            bool preserved=before==after&&scanAim==input.scanner&&bioAim==input.bioTool&&scanAction==input.scanAction&&toolAction==input.toolAction;
            if(!preserved)throw new InvalidOperationException("Visual pass changed biological geometry or input references");
            File.WriteAllText(Folder+"/FirstPass.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometryPreserved=preserved,geometryBefore=before,geometryAfter=after,aimTransformsPreserved=true,inputActionsPreserved=true,backgroundPool=world.GetComponent<JourneyBloodFlow>().cells.Length,targets=world.targets.Length,style="cream / graphite / cyan / amber",ui="reusable overview + full scrollable reference / five large vitals tiles",physicsAdded=0,volumetric=false,postEffectsAdded=false,hardwareVerified=false,phase="FIRST PASS — stop for visual assessment"},Formatting.Indented));
        }
    }
}

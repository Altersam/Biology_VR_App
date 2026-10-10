using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class BiomedicalLivingPass
    {
        public const string Reports=JourneyGeometry.Root+"/Reports/BiomedicalLivingPass";
        [MenuItem("Biology VR/Stage 1 Install Biomedical Devices")]
        public static void Tools()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();var input=world.mission.GetComponent<JourneyInput>();Directory.CreateDirectory(Reports+"/Backup");Directory.CreateDirectory(Reports+"/Stage1");
            EditorSceneManager.SaveScene(world.gameObject.scene);string backup=Reports+"/Backup/ArteryNarrativeVR_before_biomedical_living_pass.unity";
            if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Scene backup failed");
            foreach(string kind in new[]{"Scanner","BioTool"}){string source=ActiveInstrumentBuilder.Folder+"/"+kind+"/Active"+kind+".prefab",dest=Reports+"/Backup/Before_"+kind+".prefab";if(!File.Exists(dest)&&!AssetDatabase.CopyAsset(source,dest))throw new IOException("Device backup failed");}
            string before=ArteryVisualV8.GeometrySignature(world);var scanner=input.scanner;var bio=input.bioTool;var scan=input.scanAction;var tool=input.toolAction;
            ActiveInstrumentBuilder.Apply();string after=ArteryVisualV8.GeometrySignature(world);
            if(before!=after||scanner!=input.scanner||bio!=input.bioTool||scan!=input.scanAction||tool!=input.toolAction)throw new InvalidOperationException("Device geometry affected gameplay bindings");
            File.WriteAllText(Reports+"/Stage1/Devices.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,realMeshesRebuilt=true,geometryPreserved=true,geometryBefore=before,geometryAfter=after,aimAndInputPreserved=true,sharedOpaqueVertexPalette=true,physicsAdded=0,hardwareVerified=false},Formatting.Indented));
        }
    }
}

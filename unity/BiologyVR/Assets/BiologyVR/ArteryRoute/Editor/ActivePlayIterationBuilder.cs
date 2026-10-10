using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ActivePlayIterationBuilder
    {
        [MenuItem("Biology VR/Apply Orbital Tools And Active Blood Lesson")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();
            string folder=ActiveInstrumentBuilder.Reports;Directory.CreateDirectory(folder);
            string backup=folder+"/ArteryNarrativeVR_before_orbital_flow_shooter.unity";
            EditorSceneManager.SaveScene(m.gameObject.scene);
            if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(m.gameObject.scene.path,backup))throw new IOException("Iteration scene backup failed");
            ActiveInstrumentBuilder.Apply();ArteryStartupReview.Apply();
            var flow=m.world.GetComponent<JourneyBloodFlow>();
            File.WriteAllText(folder+"/ActivePlayIteration.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,orbitalCrown=true,assaultQuota=JourneyMission.AssaultVirusCount,assaultFireInterval=JourneyInput.AssaultFireInterval,interactiveFlowSlots=flow.interactiveCells.Length,backgroundPool=flow.cells.Length,bloodCargo="Hb → heme → O2 travel with the extracted RBC",requiresExtractionBeforeScan=true,hardwareVerified=false},Formatting.Indented));
        }
    }
}

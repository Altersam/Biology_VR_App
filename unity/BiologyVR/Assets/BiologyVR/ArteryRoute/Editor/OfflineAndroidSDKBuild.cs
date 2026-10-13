using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class OfflineAndroidSDKBuild
    {
        public static void BuildPicoVerified()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(UniversalMobileBuild.ScenePath);
            Configure();UniversalMobileBuild.BuildPico();
            var result=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(JourneyGeometry.Root+"/Reports/PicoBuildReport.json"));
            if((string)result["result"]!="Succeeded")throw new InvalidOperationException("PICO player build failed; see PicoBuildReport.json");
        }
        public static void BuildQuestVerified()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(UniversalMobileBuild.ScenePath);
            Configure();MetaQuestBuild.Build();
            var result=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(JourneyGeometry.Root+"/Reports/MetaQuestBuildReport.json"));
            if((string)result["result"]!="Succeeded")throw new InvalidOperationException("Meta Quest player build failed; see MetaQuestBuildReport.json");
        }
        [MenuItem("Biology VR/Use Verified Local Android SDK On Z")]
        public static void Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            string sdk=Path.GetFullPath("Builds/AndroidSDK_Offline");
            if(!File.Exists(Path.Combine(sdk,"platforms/android-34/android.jar")))throw new InvalidOperationException("Prepare the verified SDK copy first");
            if(!EditorPrefs.HasKey("BiologyVR.PreviousSDKRoot"))EditorPrefs.SetString("BiologyVR.PreviousSDKRoot",AndroidExternalToolsSettings.sdkRootPath);
            AndroidExternalToolsSettings.sdkRootPath=sdk;
            Debug.Log("Local Android SDK configured on Z; installed package verification does not query remote repositories.");
        }
        [MenuItem("Biology VR/Restore Previous Android SDK Root")]
        public static void Restore()
        {
            if(EditorPrefs.HasKey("BiologyVR.PreviousSDKRoot")){AndroidExternalToolsSettings.sdkRootPath=EditorPrefs.GetString("BiologyVR.PreviousSDKRoot");EditorPrefs.DeleteKey("BiologyVR.PreviousSDKRoot");}
        }
    }
}

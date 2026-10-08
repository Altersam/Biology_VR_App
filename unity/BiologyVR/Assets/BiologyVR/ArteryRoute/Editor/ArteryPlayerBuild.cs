using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryPlayerBuild
    {
        public static string LastResult {get;private set;}="Not built";
        static bool pending;
        [MenuItem("Biology VR/Build Windows PC VR Player")]
        public static void Schedule()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before a player build");
            if(pending)return;pending=true;LastResult="Building";EditorApplication.delayCall+=Build;
        }
        static void Build()
        {
            try
            {
                var folder=Path.GetFullPath("Builds/WindowsVR");Directory.CreateDirectory(folder);
                var target=UnityEditor.Build.NamedBuildTarget.Standalone;
                var backend=PlayerSettings.GetScriptingBackend(target);
                BuildReport report;
                try
                {
                    // This editor installation has Mono player support, but no IL2CPP module.
                    PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.Mono2x);
                    report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{BuildArteryVrScene.ScenePath},locationPathName=Path.Combine(folder,"BiologyVR.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                }
                finally {PlayerSettings.SetScriptingBackend(target,backend);}
                LastResult=JsonConvert.SerializeObject(new {result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,sizeBytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,path=report.summary.outputPath,backend="Mono",headsetTestingRequired=true},Formatting.Indented);
                File.WriteAllText(BuildArteryVrScene.Root+"/Reports/PlayerBuildReport.json",LastResult);
                Debug.Log("Biology VR player build: "+LastResult);
            }
            catch(Exception e){LastResult="FAILED: "+e;Debug.LogException(e);}
            finally{pending=false;}
        }
    }
}

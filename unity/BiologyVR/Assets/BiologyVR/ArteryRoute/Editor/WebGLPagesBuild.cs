using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class WebGLPagesBuild
    {
        [MenuItem("Biology VR/Build WebGL For GitHub Pages")]
        public static void QueueBuild()=>EditorCoroutineUtility.StartCoroutineOwnerless(BuildRoutine());
        static IEnumerator BuildRoutine()
        {
            yield return null;
            string reportPath=JourneyGeometry.Root+"/Reports/WebGLBuildReport.json";
            try
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
                if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("Install WebGL Build Support in Unity Hub");
                PlayerSettings.productName="Biology VR";
                ArteryInterfaceFont.Apply();
                PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback=true;
                PlayerSettings.WebGL.threadsSupport=false;
                PlayerSettings.WebGL.dataCaching=true;
                var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.WebGL);
                if(general){general.InitManagerOnStart=false;EditorUtility.SetDirty(general);}
                if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL)
                    if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("WebGL target switch failed");
                AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
                string folder=Path.GetFullPath("Builds/WebGL/play");Directory.CreateDirectory(folder);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{UniversalMobileBuild.ScenePath},locationPathName=folder,target=BuildTarget.WebGL,options=BuildOptions.None});
                File.WriteAllText(Path.Combine(folder,".nojekyll"),string.Empty);
                File.WriteAllText(reportPath,JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,path=folder,compression="gzip with decompression fallback",threads=false,browserVerified=false},Formatting.Indented));
            }
            catch(Exception error)
            {Directory.CreateDirectory(Path.GetDirectoryName(reportPath));File.WriteAllText(reportPath,JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result="Failed",reason=error.Message},Formatting.Indented));Debug.LogException(error);}
        }
    }
}

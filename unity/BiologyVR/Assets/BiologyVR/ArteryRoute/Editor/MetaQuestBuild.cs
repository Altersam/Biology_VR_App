using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class MetaQuestBuild
    {
        const string Reports=JourneyGeometry.Root+"/Reports";
        static bool running;

        [MenuItem("Biology VR/Configure Meta Quest")]
        public static void Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            UniversalMobileBuild.ConfigureAndroidPlayer();
            PlayerSettings.productName="Biology VR";PlayerSettings.companyName="BiologyVR";
            PlayerSettings.bundleVersion="1.0.1";PlayerSettings.Android.bundleVersionCode=2;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.Activity;
            PlayerSettings.Android.splitApplicationBinary=false;
            PlayerSettings.Android.optimizedFramePacing=true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            EditorUserBuildSettings.buildAppBundle=false;
            // The installed PICO package's global preprocessor calls Trim even
            // when its XR feature is disabled. Local sideloading needs no app ID.
            var platform=Unity.XR.PXR.PXR_PlatformSetting.Instance;
            if(platform.appID==null)platform.appID=string.Empty;
            platform.startTimeEntitlementCheck=false;EditorUtility.SetDirty(platform);
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if(!settings)throw new InvalidOperationException("Android OpenXR settings missing");
            foreach(var feature in settings.GetFeatures<OpenXRFeature>())
            {
                if(!feature)continue;
                feature.enabled=feature is MetaQuestFeature||feature is OculusTouchControllerProfile||
                    feature is MetaQuestTouchPlusControllerProfile||feature is MetaQuestTouchProControllerProfile||
                    feature.GetType().Name=="OpenXRCompositionLayersFeature";
                if(feature is Unity.XR.OpenXR.Features.PICOSupport.PICOFeature pico)pico.isPicoSupport=false;
                EditorUtility.SetDirty(feature);
            }
            var meta=settings.GetFeature<MetaQuestFeature>();
            if(!meta||!settings.GetFeature<OculusTouchControllerProfile>())throw new InvalidOperationException("Meta Quest Support/Oculus Touch profile missing");
            var serialized=new SerializedObject(meta);var devices=serialized.FindProperty("targetDevices");
            var supported=new List<string>();
            for(int i=0;i<devices.arraySize;i++)
            {
                var entry=devices.GetArrayElementAtIndex(i);string name=entry.FindPropertyRelative("manifestName").stringValue;
                bool enabled=name=="quest2"||name=="cambria"||name=="eureka"||name=="quest3s";
                entry.FindPropertyRelative("enabled").boolValue=enabled;if(enabled)supported.Add(name);
            }
            serialized.FindProperty("m_symmetricProjection").boolValue=false;
            serialized.FindProperty("lateLatchingMode").boolValue=false;
            serialized.FindProperty("m_multiviewRenderRegionsOptimizationMode").enumValueIndex=0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            settings.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;EditorUtility.SetDirty(settings);
            var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if(!general||!general.Manager)throw new InvalidOperationException("Android XR manager missing");
            if(!general.Manager.activeLoaders.Any(l=>l is OpenXRLoader))
                if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Android))throw new InvalidOperationException("Cannot assign Android OpenXR loader");
            general.InitManagerOnStart=true;general.Manager.automaticLoading=true;general.Manager.automaticRunning=true;
            EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);
            UniversalMobileBuild.ConfigureMobilePipeline();UniversalMobileBuild.ApplyPerformanceComponent();
            var hud=UnityEngine.Object.FindFirstObjectByType<JourneyHud>();if(hud){hud.showDebugButtons=false;EditorUtility.SetDirty(hud);}
            AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
            var issues=new List<OpenXRFeature.ValidationRule>();
            // Meta's validation callbacks consult selectedBuildTargetGroup even
            // when Android is passed. Validate after the actual target switch.
            if(EditorUserBuildSettings.selectedBuildTargetGroup==BuildTargetGroup.Android)OpenXRProjectValidation.GetCurrentValidationIssues(issues,BuildTargetGroup.Android);
            File.WriteAllText(Reports+"/MetaQuestConfiguration.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,device="Meta Quest 2 / Pro / 3 / 3S",supportedDevices=supported,minSdk=29,targetSdk=34,graphics="OpenGLES3",architecture="ARM64",il2cpp=true,enabledFeatures=settings.GetFeatures<OpenXRFeature>().Where(f=>f&&f.enabled).Select(f=>f.GetType().Name).ToArray(),validation=issues.Select(i=>new{i.message,i.error}).ToArray(),headsetVerified=false},Formatting.Indented));
        }

        [MenuItem("Biology VR/Build Meta Quest APK")]
        public static void QueueBuild()
        {
            if(EditorApplication.isPlaying||running)throw new InvalidOperationException("Use Edit Mode and wait for the current build");
            running=true;
            File.WriteAllText(Reports+"/MetaQuestBuildReport.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result="Queued",headsetVerified=false},Formatting.Indented));
            EditorCoroutineUtility.StartCoroutineOwnerless(BuildRoutine());
        }
        static IEnumerator BuildRoutine()
        {
            yield return null;
            try{Build();}
            catch(Exception error)
            {File.WriteAllText(Reports+"/MetaQuestBuildReport.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result="Failed",reason=error.Message,headsetVerified=false},Formatting.Indented));Debug.LogException(error);}
            finally{running=false;}
        }
        public static void Build()
        {
            if(Path.GetFullPath(".").Any(c=>c>127))throw new InvalidOperationException("Use Biology VR/Reopen At ASCII Build Alias before Android builds");
            Configure();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Android Build Support is missing");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
                if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Android target switch failed");
            EditorUserBuildSettings.selectedBuildTargetGroup=BuildTargetGroup.Android;
            Configure();
            string folder=Path.GetFullPath("Builds/MetaQuest");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{UniversalMobileBuild.ScenePath},locationPathName=Path.Combine(folder,"BiologyVR_MetaQuest.apk"),target=BuildTarget.Android,options=BuildOptions.Development});
            bool exists=File.Exists(report.summary.outputPath)&&new FileInfo(report.summary.outputPath).Length>0;
            File.WriteAllText(Reports+"/MetaQuestBuildReport.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result=report.summary.result==BuildResult.Succeeded&&exists?"Succeeded":"Failed",unityResult=report.summary.result.ToString(),apkExists=exists,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,sizeBytes=exists?(ulong)new FileInfo(report.summary.outputPath).Length:0,seconds=report.summary.totalTime.TotalSeconds,path=report.summary.outputPath,device="Meta Quest",headsetVerified=false},Formatting.Indented));
        }
    }

    /// <summary>Build-only filtering; never edits vendor package import settings.</summary>
    public sealed class MetaQuestVendorPluginFilter:IPreprocessBuildWithReport,IPostprocessBuildWithReport
    {
        public int callbackOrder=>10000;
        readonly List<PluginImporter> excluded=new List<PluginImporter>();
        public void OnPreprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.Android)return;
            var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var meta=settings?settings.GetFeature<MetaQuestFeature>():null;
            if(!meta||!meta.enabled)return;
            foreach(var importer in PluginImporter.GetAllImporters())
                if(importer.assetPath.StartsWith("Packages/com.unity.xr.openxr.picoxr/",StringComparison.Ordinal))
                {importer.SetIncludeInBuildDelegate(path=>false);excluded.Add(importer);}
        }
        public void OnPostprocessBuild(BuildReport report)
        {foreach(var importer in excluded)if(importer)importer.SetIncludeInBuildDelegate(path=>true);excluded.Clear();}
    }
}

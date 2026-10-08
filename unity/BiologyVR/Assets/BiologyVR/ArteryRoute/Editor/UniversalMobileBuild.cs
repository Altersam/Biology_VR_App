using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using Unity.XR.OpenXR.Features.PICOSupport;
using UnityEditor.XR.OpenXR.Features;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.Management.Metadata;
using Unity.EditorCoroutines.Editor;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class UniversalMobileBuild
    {
        internal static string ScenePath=>File.Exists(ArteryScenePolish.ScenePath)?ArteryScenePolish.ScenePath:JourneyGeometry.Root+"/Scenes/ArteryNarrativeVR.unity";
        const string QuestPicoUrp=JourneyGeometry.Root+"/Generated/ArteryQuestPico_URP.asset";
        public static string LastResult{get;private set;}="Not configured";

        [MenuItem("Biology VR/Configure Pico 4 Enterprise")]
        public static void ConfigurePico()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            ConfigureAndroidPlayer();
            PlayerSettings.productName="Biology VR";PlayerSettings.companyName="BiologyVR";
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.Activity;
            PlayerSettings.Android.splitApplicationBinary=false;
            PlayerSettings.Android.optimizedFramePacing=true;
            PlayerSettings.bundleVersion="1.0.1";PlayerSettings.Android.bundleVersionCode=2;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            EditorUserBuildSettings.buildAppBundle=false;
            var platform=Unity.XR.PXR.PXR_PlatformSetting.Instance;
            if(platform.appID==null)platform.appID=string.Empty;
            platform.startTimeEntitlementCheck=false;EditorUtility.SetDirty(platform);
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if(!settings)throw new InvalidOperationException("Android OpenXR settings missing");
            foreach(var feature in settings.GetFeatures<OpenXRFeature>())
            {
                if(!feature)continue;
                feature.enabled=feature is PICOFeature||feature is PICO4ControllerProfile||feature.GetType().Name=="OpenXRCompositionLayersFeature";
                EditorUtility.SetDirty(feature);
            }
            var pico=settings.GetFeature<PICOFeature>();var controller=settings.GetFeature<PICO4ControllerProfile>();
            if(!pico||!controller)throw new InvalidOperationException("Install PICO OpenXR Plugin before configuring Pico");
            pico.isPicoSupport=true;settings.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;EditorUtility.SetDirty(settings);
            var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if(!general||!general.Manager)throw new InvalidOperationException("Android XR manager missing");
            if(!general.Manager.activeLoaders.Any(l=>l is OpenXRLoader))
                if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Android))throw new InvalidOperationException("Cannot assign Android OpenXR loader");
            general.InitManagerOnStart=true;general.Manager.automaticLoading=true;general.Manager.automaticRunning=true;EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);
            ConfigureMobilePipeline();ApplyPerformanceComponent();
            var hud=UnityEngine.Object.FindFirstObjectByType<JourneyHud>();if(hud){hud.showDebugButtons=false;EditorUtility.SetDirty(hud);}
            AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
            var issues=new System.Collections.Generic.List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues,BuildTargetGroup.Android);
            var flow=UnityEngine.Object.FindFirstObjectByType<JourneyBloodFlow>();
            File.WriteAllText(JourneyGeometry.Root+"/Reports/PicoConfiguration.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,device="Pico 4 Enterprise",androidSupported=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android),minSdk=29,targetSdk=34,graphics="OpenGLES3",architecture="ARM64",picoSdk=PICOFeature.SDKVersion,enabledFeatures=settings.GetFeatures<OpenXRFeature>().Where(f=>f&&f.enabled).Select(f=>f.GetType().Name).ToArray(),validation=issues.Select(i=>new{i.message,i.error}).ToArray(),interactivePoolIndices=flow?flow.interactiveCells.Select(c=>c.poolIndex).ToArray():Array.Empty<int>(),headsetVerified=false},Formatting.Indented));
            Debug.Log("Pico configuration saved; see Reports/PicoConfiguration.json");
        }

        [MenuItem("Biology VR/Build Pico 4 Enterprise APK")]
        public static void QueuePicoBuild()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            File.WriteAllText(JourneyGeometry.Root+"/Reports/PicoBuildReport.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result="Queued",device="Pico 4 Enterprise",headsetVerified=false},Formatting.Indented));
            EditorCoroutineUtility.StartCoroutineOwnerless(PicoBuildRoutine());
        }
        static System.Collections.IEnumerator PicoBuildRoutine()
        {
            yield return null;
            try{BuildPico();}
            catch(Exception error)
            {
                File.WriteAllText(JourneyGeometry.Root+"/Reports/PicoBuildReport.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result="Failed",reason=error.Message,headsetVerified=false},Formatting.Indented));
                Debug.LogException(error);
            }
        }
        public static void BuildPico()
        {
            ConfigurePico();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Android module installed? Restart Unity to load its build extensions.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
                if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Android target switch failed");
            ConfigurePico();
            string folder=Path.GetFullPath("Builds/Pico4Enterprise");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(folder,"BiologyVR_Pico4Enterprise.apk"),target=BuildTarget.Android,options=BuildOptions.Development});
            bool apkExists=File.Exists(report.summary.outputPath)&&new FileInfo(report.summary.outputPath).Length>0;
            LastResult=JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result=report.summary.result==BuildResult.Succeeded&&apkExists?"Succeeded":"Failed",unityResult=report.summary.result.ToString(),apkExists,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,sizeBytes=apkExists?(ulong)new FileInfo(report.summary.outputPath).Length:0,unityReportedSizeBytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,path=report.summary.outputPath,device="Pico 4 Enterprise",headsetVerified=false},Formatting.Indented);
            File.WriteAllText(JourneyGeometry.Root+"/Reports/PicoBuildReport.json",LastResult);Debug.Log(LastResult);
        }

        [MenuItem("Biology VR/Save And Restart Editor For Android Module")]
        public static void RestartForAndroid()
        {if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();EditorApplication.OpenProject(Path.GetFullPath("."));}

        [MenuItem("Biology VR/Return Editor To Desktop Target")]
        public static void ReturnToDesktop()
        {if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64);}

        [MenuItem("Biology VR/Reopen At ASCII Build Alias")]
        public static void ReopenAtAsciiAlias()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            string original=Path.GetFullPath("."),alias=Path.Combine(Path.GetTempPath(),"opencode","BiologyVR_Bio_v_0_1");
            if(!Directory.Exists(alias))throw new InvalidOperationException("Create the ASCII junction with Tools/create_ascii_build_alias.ps1 first");
            if(original.Any(c=>c>127))EditorPrefs.SetString("BiologyVR.Pico.OriginalProjectPath",original);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();EditorApplication.OpenProject(alias);
        }
        [MenuItem("Biology VR/Return To Original Project Path")]
        public static void ReopenOriginalPath()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            string path=EditorPrefs.GetString("BiologyVR.Pico.OriginalProjectPath",string.Empty);
            if(File.Exists("TRANSFER_VERIFICATION.json"))
            {
                var saved=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("TRANSFER_VERIFICATION.json"));
                string destination=(string)saved["destination"];
                if(Directory.Exists(destination))path=destination;
            }
            if(!Directory.Exists(path))throw new InvalidOperationException("Original project path is missing");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();EditorApplication.OpenProject(path);
        }

        [MenuItem("Biology VR/Configure Universal OpenXR — Quest + Pico 4 Enterprise")]
        public static void ConfigureUniversalOpenXR()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            ConfigureAndroidPlayer();ConfigureAndroidXR();ConfigureMobilePipeline();ApplyPerformanceComponent();
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            LastResult="Universal Android OpenXR configured for Meta Quest and Pico 4 Enterprise";
            Debug.Log(LastResult);
        }

        [MenuItem("Biology VR/Build Universal Android OpenXR APK")]
        public static void BuildUniversalAndroid()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            ConfigureAndroidPlayer();ConfigureAndroidXR();ConfigureMobilePipeline();
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
                if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))
                    throw new InvalidOperationException("Android build support is not installed in this Unity editor");
            var folder=Path.GetFullPath("Builds/UniversalAndroid");Directory.CreateDirectory(folder);
            var apk=Path.Combine(folder,"BiologyVR_Quest_Pico.apk");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=apk,target=BuildTarget.Android,options=BuildOptions.None});
            LastResult=JsonConvert.SerializeObject(new{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,sizeBytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,path=report.summary.outputPath,target="Android ARM64 OpenXR",devices="Meta Quest / Pico 4 Enterprise",il2cpp=true},Formatting.Indented);
            File.WriteAllText(JourneyGeometry.Root+"/Reports/UniversalAndroidBuildReport.json",LastResult);
            Debug.Log("Biology VR universal Android build: "+LastResult);
        }
        [MenuItem("Biology VR/Build Android Development After XR Pass")]
        public static void BuildDevelopment()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            string resultPath=JourneyGeometry.Root+"/Reports/AndroidDevelopmentBuild.json";
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
            {LastResult=JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,status="BLOCKED",reason="Android Build Support is not installed",headsetVerified=false},Formatting.Indented);File.WriteAllText(resultPath,LastResult);return;}
            ConfigureAndroidPlayer();ConfigureAndroidXR();ConfigureMobilePipeline();
            if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Android target switch failed");
            string folder=Path.GetFullPath("Builds/AndroidDevelopment");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(folder,"BiologyVR_Development.apk"),target=BuildTarget.Android,options=BuildOptions.Development|BuildOptions.AllowDebugging});
            LastResult=JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,report.summary.totalSize,path=report.summary.outputPath,development=true,headsetVerified=false},Formatting.Indented);File.WriteAllText(resultPath,LastResult);
        }

        internal static void ConfigureAndroidPlayer()
        {
            var android=UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android,"com.biologyvr.arteryjourney");
            PlayerSettings.bundleVersion="1.0.0";
            PlayerSettings.Android.bundleVersionCode=1;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.androidIsGame=true;
            PlayerSettings.SetScriptingBackend(android,ScriptingImplementation.IL2CPP);
            try{PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan,GraphicsDeviceType.OpenGLES3});}catch(Exception e){Debug.LogWarning("Could not set Android graphics API order: "+e.Message);}
            EditorUserBuildSettings.androidBuildSystem=AndroidBuildSystem.Gradle;
        }
        static void ConfigureAndroidXR()
        {
            var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if(settings!=null)
            {
                foreach(var feature in settings.GetFeatures<OpenXRFeature>())
                {
                    if(!feature)continue;
                    string n=feature.GetType().Name;
                    bool generic=n.Contains("KHRSimple")||n.Contains("HTCVive")||n.Contains("MicrosoftMotion")||n.Contains("OculusTouch");
                    bool vendorOnly=n.Contains("MetaQuestFeature")||n.Contains("OculusQuestFeature");
                    feature.enabled=generic&&!vendorOnly;
                    if(feature.enabled)EditorUtility.SetDirty(feature);
                }
                EditorUtility.SetDirty(settings);
            }
            var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if(general!=null)
            {
                general.InitManagerOnStart=true;EditorUtility.SetDirty(general);
                if(general.Manager!=null)
                {
                    general.Manager.automaticLoading=true;general.Manager.automaticRunning=true;EditorUtility.SetDirty(general.Manager);
                }
            }
        }
        internal static void ConfigureMobilePipeline()
        {
            ArteryInterfaceFont.Apply();
            var current=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(!current)throw new InvalidOperationException("URP asset is missing");
            var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(QuestPicoUrp);
            if(!mobile){mobile=UnityEngine.Object.Instantiate(current);mobile.name="Artery Quest Pico Universal URP";AssetDatabase.CreateAsset(mobile,QuestPicoUrp);}
            mobile.supportsHDR=false;mobile.msaaSampleCount=2;mobile.renderScale=.82f;mobile.maxAdditionalLightsCount=2;mobile.useSRPBatcher=true;mobile.supportsCameraDepthTexture=false;mobile.supportsCameraOpaqueTexture=false;
            GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.renderPipeline=mobile;EditorUtility.SetDirty(mobile);
        }
        internal static void ApplyPerformanceComponent()
        {
            var profile=UnityEngine.Object.FindFirstObjectByType<QuestPicoPerformanceProfile>();
            if(!profile)
            {
                var root=new GameObject("Quest + Pico mobile performance profile");profile=root.AddComponent<QuestPicoPerformanceProfile>();
            }
            profile.mobileBloodCells=48;profile.disablePostProcessing=true;profile.disableRealtimeShadows=true;
            EditorUtility.SetDirty(profile.gameObject);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using BiologyVR.ArteryRoute.Editor;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// PICO 4 and Meta Quest APKs of Biology VR 2. Reuses the project's verified headset
    /// configuration (UniversalMobileBuild.ConfigurePico / MetaQuestBuild.Configure: OpenXR
    /// features, ARM64, IL2CPP, GLES3, SDK 29/34, mobile URP) and then builds ONLY the new scene,
    /// passed explicitly — Build Settings are not touched.
    /// Biology VR 2 is a separate app (own package id and name), so it installs next to the
    /// working version instead of replacing it. Player settings changed for the build
    /// (name, id, version) are restored afterwards; the old release pipeline keeps its values.
    /// Android builds need the ASCII path: Biology VR/Reopen At ASCII Build Alias.
    /// </summary>
    public static class Bvr2AndroidBuild
    {
        public const string Version = "0.11.0";
        public const int AndroidCode = 12;
        public const string AppId = "com.biologyvr.biologyvr2";
        public const string ProductName = "Biology VR 2";
        const string MobileUrp = "Assets/BiologyVR/ArteryJourney/Generated/ArteryQuestPico_URP.asset";
        const string Reports = Bvr2SceneBuilder.Root + "/Reports";
        static bool running;

        public enum Headset { Pico, Quest }

        [MenuItem("Biology VR 2/Android/Build PICO + Quest APKs", false, 200)] static void MenuBoth() => Queue(Headset.Pico, Headset.Quest);
        [MenuItem("Biology VR 2/Android/Build PICO APK", false, 201)] static void MenuPico() => Queue(Headset.Pico);
        [MenuItem("Biology VR 2/Android/Build Quest APK", false, 202)] static void MenuQuest() => Queue(Headset.Quest);

        public static string ApkName(Headset h) => h == Headset.Pico ? $"BiologyVR2_Pico4_v{Version}.apk" : $"BiologyVR2_MetaQuest_v{Version}.apk";

        static void Queue(params Headset[] targets)
        {
            if (EditorApplication.isPlaying || running) throw new InvalidOperationException("Use Edit Mode and wait for the current build");
            running = true;
            Directory.CreateDirectory(Reports);
            Write("AndroidBuild.json", new { utc = DateTime.UtcNow, result = "Queued", version = Version, versionCode = AndroidCode, targets = targets.Select(t => t.ToString()).ToArray(), headsetVerified = false });
            EditorCoroutineUtility.StartCoroutineOwnerless(Routine(targets));
        }

        // Windows must not fall asleep in the middle of an IL2CPP build (it did once: the build died).
        [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);
        static void KeepAwake(bool on)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor) return;
            try { SetThreadExecutionState(on ? 0x80000001u : 0x80000000u); } catch (Exception e) { Debug.LogWarning("[BVR2] keep-awake: " + e.Message); }
        }

        static IEnumerator Routine(Headset[] targets)
        {
            KeepAwake(true);
            yield return null;
            var results = new List<Dictionary<string, object>>();
            var startTarget = EditorUserBuildSettings.activeBuildTarget;
            foreach (var t in targets)
            {
                Dictionary<string, object> r;
                try { r = Build(t); }
                catch (Exception e)
                {
                    r = new Dictionary<string, object> { ["headset"] = t.ToString(), ["result"] = "Failed", ["reason"] = e.Message };
                    Debug.LogException(e);
                }
                results.Add(r);
                Write($"AndroidBuild_{t}.json", r);
                yield return null;
            }
            running = false; KeepAwake(false);
            bool ok = results.All(r => (string)r["result"] == "Succeeded");
            Write("AndroidBuild.json", new { utc = DateTime.UtcNow, result = ok ? "Succeeded" : "Failed", version = Version, versionCode = AndroidCode, appId = AppId, startTarget = startTarget.ToString(), results, headsetVerified = false });
            Debug.Log($"[BVR2] Android builds {(ok ? "succeeded" : "FAILED")}: " + string.Join(", ", results.Select(r => r["headset"] + "=" + r["result"])));
        }

        public static Dictionary<string, object> Build(Headset headset)
        {
            if (Path.GetFullPath(".").Any(c => c > 127)) throw new InvalidOperationException("Use Biology VR/Reopen At ASCII Build Alias before Android builds (Cyrillic path)");
            if (!File.Exists(Bvr2SceneBuilder.ScenePath)) throw new InvalidOperationException("Scene missing — run Biology VR 2/Build Scene");
            EditorSceneManager.SaveOpenScenes();
            if (SceneManager.GetActiveScene().path != Bvr2SceneBuilder.ScenePath) EditorSceneManager.OpenScene(Bvr2SceneBuilder.ScenePath, OpenSceneMode.Single);

            var android = NamedBuildTarget.Android;
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileUrp);
            int msaa = urp ? urp.msaaSampleCount : 4; float scale = urp ? urp.renderScale : 1f;
            try
            {
                Configure(headset);
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)) throw new InvalidOperationException("Android Build Support is missing");
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android)) throw new InvalidOperationException("Android target switch failed");
                EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
                Configure(headset); // vendor validation reads the selected group: configure again after the switch
                // keep the readable mobile profile (MSAA ×4, full scale): text and subtitles matter more
                // than the last few percent of GPU; Bvr2MobileQuality lowers the scale on the headset if needed
                urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileUrp);
                if (urp) { urp.msaaSampleCount = msaa; urp.renderScale = scale; EditorUtility.SetDirty(urp); }
                PlayerSettings.productName = ProductName;
                PlayerSettings.SetApplicationIdentifier(android, AppId);
                PlayerSettings.bundleVersion = Version;
                PlayerSettings.Android.bundleVersionCode = AndroidCode;
                AssetDatabase.SaveAssets();

                string folder = Path.GetFullPath("Builds/BiologyVR2"); Directory.CreateDirectory(folder);
                string apk = Path.Combine(folder, ApkName(headset));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Bvr2SceneBuilder.ScenePath }, locationPathName = apk, target = BuildTarget.Android, options = BuildOptions.None });
                bool exists = File.Exists(apk) && new FileInfo(apk).Length > 0;
                var errors = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content).Take(12).ToArray();
                return new Dictionary<string, object>
                {
                    ["utc"] = DateTime.UtcNow, ["headset"] = headset.ToString(),
                    ["device"] = headset == Headset.Pico ? "PICO 4 / PICO 4 Enterprise" : "Meta Quest 2 / Pro / 3 / 3S",
                    ["result"] = report.summary.result == BuildResult.Succeeded && exists ? "Succeeded" : "Failed",
                    ["unityResult"] = report.summary.result.ToString(), ["errors"] = report.summary.totalErrors, ["warnings"] = report.summary.totalWarnings,
                    ["errorMessages"] = errors, ["seconds"] = report.summary.totalTime.TotalSeconds,
                    ["path"] = apk, ["file"] = Path.GetFileName(apk), ["bytes"] = exists ? new FileInfo(apk).Length : 0, ["sha256"] = exists ? Sha256(apk) : "",
                    ["scene"] = Bvr2SceneBuilder.ScenePath, ["appId"] = AppId, ["productName"] = ProductName, ["version"] = Version, ["versionCode"] = AndroidCode,
                    ["msaa"] = msaa, ["renderScale"] = scale, ["headsetVerified"] = false
                };
            }
            finally { RestoreWorkingApp(); }
        }

        /// <summary>Player settings of the working version (old release pipeline), restored after every
        /// BVR2 build. Restoring fixed values, not a snapshot: a build killed half-way (the PC fell asleep
        /// on 27.10) leaves BVR2 values saved in ProjectSettings, and a snapshot would keep them.</summary>
        [MenuItem("Biology VR 2/Android/Restore Player Settings Of Working Version", false, 240)]
        public static void RestoreWorkingApp()
        {
            PlayerSettings.productName = "Biology VR";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.biologyvr.arteryjourney");
            PlayerSettings.bundleVersion = BiologyReleaseVersion.Version;
            PlayerSettings.Android.bundleVersionCode = BiologyReleaseVersion.AndroidCode;
            AssetDatabase.SaveAssets();
        }

        [InitializeOnLoadMethod]
        static void HealAfterInterruptedBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (BuildPipeline.isBuildingPlayer || running) return;
                if (PlayerSettings.productName != ProductName && PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != AppId) return;
                RestoreWorkingApp();
                Debug.LogWarning("[BVR2] Player settings still had Biology VR 2 values after an interrupted build — restored the working version (Biology VR, com.biologyvr.arteryjourney, " + BiologyReleaseVersion.Version + ").");
            };
        }

        static void Configure(Headset headset)
        {
            if (headset == Headset.Pico) UniversalMobileBuild.ConfigurePico();
            else MetaQuestBuild.Configure();
        }

        static string Sha256(string path)
        {
            using var sha = SHA256.Create(); using var s = File.OpenRead(path);
            return string.Concat(sha.ComputeHash(s).Select(b => b.ToString("x2")));
        }

        static void Write(string name, object data) => File.WriteAllText(Path.Combine(Reports, name), JsonConvert.SerializeObject(data, Formatting.Indented));
    }
}

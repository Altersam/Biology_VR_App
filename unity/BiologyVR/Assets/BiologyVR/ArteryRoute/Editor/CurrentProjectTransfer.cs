using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class CurrentProjectTransfer
    {
        public const string Manifest="CURRENT_PROJECT_TRANSFER.json";

        [MenuItem("Biology VR/Prepare Current Project For Transfer")]
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before preparing the saved project");
            EditorSceneManager.SaveOpenScenes();
            if(SceneManager.GetActiveScene().path!=ArteryScenePolish.ScenePath)EditorSceneManager.OpenScene(ArteryScenePolish.ScenePath);
            EditorSettings.serializationMode=SerializationMode.ForceText;
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ArteryScenePolish.ScenePath);
            // The old prototype and template scenes are not entry points of this version.
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ArteryScenePolish.ScenePath,true)};
            // URP 17 keeps xrSystemData only as an obsolete compatibility field;
            // actual XR resources are managed by GraphicsSettings. Migrate the assets.
            AssetDatabase.ForceReserializeAssets(new[]{"Assets/Settings/Project Configuration/Android Preset.asset","Assets/Settings/Project Configuration/Standalone Preset.asset"}.Where(File.Exists));
            AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
            Audit();
        }

        [MenuItem("Biology VR/Audit Current Scene Dependencies")]
        public static void Audit()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ArteryScenePolish.ScenePath)throw new InvalidOperationException("Open the current VisualRework master scene first");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();
            if(!world)throw new InvalidOperationException("JourneyWorld is missing from the current scene");
            var missingScripts=new List<string>();
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(t.GetComponents<Component>().Any(c=>!c))missingScripts.Add(t.name);
            var roots=new HashSet<string>{scene.path};
            var code=new HashSet<string>();
            var all=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(p)).ToArray();
            // C# helper classes are not all serialized MonoBehaviours. Include code,
            // shader source, XR/input configuration, Resources and StreamingAssets.
            var sourceExtensions=new HashSet<string>{".cs",".asmdef",".asmref",".shader",".hlsl",".cginc",".compute",".shadervariants",".inputactions"};
            foreach(string path in all)
            {
                if(sourceExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))code.Add(path);
                if(path.StartsWith("Assets/Resources/")||path.StartsWith("Assets/StreamingAssets/")||path.StartsWith("Assets/XR/"))roots.Add(path);
            }
            foreach(string file in Directory.GetFiles("ProjectSettings","*.asset",SearchOption.AllDirectories))
                foreach(Match match in Regex.Matches(File.ReadAllText(file),@"guid:\s*([0-9a-fA-F]{32})"))
                {string path=AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);if(path.StartsWith("Assets/")&&File.Exists(path))roots.Add(path);}
            var kept=new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(),true).Where(p=>p.StartsWith("Assets/")&&File.Exists(p)));
            foreach(string path in code)kept.Add(path);
            foreach(string path in roots)kept.Add(path);
            // Keep the active texture-bake provenance, not all historical capture folders.
            foreach(string path in all)if(path.StartsWith("Assets/BiologyVR/ArteryJourney/Generated/Artery_Concept_V4/")&&Path.GetExtension(path)==".json")kept.Add(path);
            var unresolved=new List<object>();
            foreach(string path in kept.Where(p=>new[]{".unity",".prefab",".mat",".asset"}.Contains(Path.GetExtension(p))))
            {
                byte[] prefix=new byte[5];using(var stream=File.OpenRead(path))stream.Read(prefix,0,prefix.Length);
                if(System.Text.Encoding.ASCII.GetString(prefix)!="%YAML")continue;
                foreach(Match match in Regex.Matches(File.ReadAllText(path),@"guid:\s*([0-9a-fA-F]{32})"))
                {
                    string guid=match.Groups[1].Value;if(guid.StartsWith("00000000"))continue;
                    if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))unresolved.Add(new{asset=path,guid});
                }
            }
            var tissue=world.art&&world.art.wallChunks!=null?world.art.wallChunks.FirstOrDefault(r=>r&&r.sharedMaterial):null;
            var startup=new{scene=scene.path,world=world.name,targets=world.targets.Length,bloodPool=world.GetComponent<JourneyBloodFlow>()?.cells.Length??0,currentWallMaterial=tissue?AssetDatabase.GetAssetPath(tissue.sharedMaterial):"missing",desktopInput=world.mission.GetComponent<JourneyInput>()!=null};
            bool passed=missingScripts.Count==0&&unresolved.Count==0&&startup.desktopInput&&startup.bloodPool>0;
            File.WriteAllText(Manifest,JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,source=Path.GetFullPath("."),passed,startup,missingScripts,unresolved,assetCount=kept.Count,totalAssetBytes=kept.Sum(p=>new FileInfo(p).Length),assets=kept.OrderBy(p=>p,StringComparer.Ordinal).ToArray(),omittedAssets=all.Where(p=>!kept.Contains(p)).OrderBy(p=>p,StringComparer.Ordinal).ToArray()},Formatting.Indented));
            if(!passed)throw new InvalidOperationException("Dependency audit found missing references; inspect "+Manifest);
            Debug.Log("Current scene dependency audit passed: "+kept.Count+" assets");
        }

        [MenuItem("Biology VR/Open Prepared Unity Project")]
        public static void OpenPreparedProject()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            var request=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Tools/prepared_project.json"));
            string path=(string)request["path"];
            if(!File.Exists(Path.Combine(path,"ProjectSettings","ProjectVersion.txt")))throw new InvalidOperationException("Prepared Unity project is missing");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();EditorApplication.OpenProject(path);
        }

        [MenuItem("Biology VR/Open Verified Bio v0.1 Copy")]
        public static void OpenCopy()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            string path=Path.Combine(Path.GetTempPath(),"opencode","BiologyVR_Bio_v_0_1");
            if(!Directory.Exists(path))throw new InvalidOperationException("Create the saved project's ASCII junction first");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();EditorApplication.OpenProject(path);
        }
    }

    [InitializeOnLoad]
    public static class CurrentMasterSceneStartup
    {
        static CurrentMasterSceneStartup(){EditorApplication.update+=Initialize;}
        static void Initialize()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            EditorApplication.update-=Initialize;
            var master=AssetDatabase.LoadAssetAtPath<SceneAsset>(ArteryScenePolish.ScenePath);
            if(!master)return;
            EditorSceneManager.playModeStartScene=master;
            var scene=SceneManager.GetActiveScene();
            if(!EditorApplication.isPlaying&&!scene.isDirty&&(string.IsNullOrEmpty(scene.path)||scene.path=="Assets/Scenes/SampleScene.unity"))
                EditorSceneManager.OpenScene(ArteryScenePolish.ScenePath);
        }
    }
}

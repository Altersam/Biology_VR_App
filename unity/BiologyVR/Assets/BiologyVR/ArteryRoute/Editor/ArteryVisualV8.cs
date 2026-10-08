using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryVisualV8
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/Artery_Visual_V2";
        public const string Reports=ArteryPolishReview.Reports+"/VisualV8";
        static bool captureRunning;
        static readonly JsonSerializerSettings JsonSettings=new JsonSerializerSettings{ReferenceLoopHandling=ReferenceLoopHandling.Ignore};
        static string Digest(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");}
        static JourneyWorld World()=>UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();
        public static string GeometrySignature(JourneyWorld w)
        {
            var rows=new List<object>();
            foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID))
            {
                object shape=c is SphereCollider s?(object)new{centre=s.center.ToString("R"),radius=s.radius}:c is BoxCollider b?new{centre=b.center.ToString("R"),size=b.size.ToString("R")}:c is MeshCollider m?new{mesh=AssetDatabase.GetAssetPath(m.sharedMesh),vertices=m.sharedMesh?m.sharedMesh.vertexCount:0,convex=m.convex}:null;
                rows.Add(new{id=c.GetInstanceID(),name=c.name,type=c.GetType().Name,c.enabled,c.isTrigger,position=c.transform.position.ToString("R"),rotation=c.transform.rotation.ToString("R"),scale=c.transform.lossyScale.ToString("R"),shape});
            }
            return Digest(JsonConvert.SerializeObject(new{path=JsonUtility.ToJson(w.mover.path),colliders=rows,targets=w.targets.Select(t=>new{t.targetId,position=t.transform.position.ToString("R"),rotation=t.transform.rotation.ToString("R"),scale=t.transform.localScale.ToString("R")})},JsonSettings));
        }
        [MenuItem("Biology VR/Visual V8/Save Fallback And Audit")]
        public static void Audit()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var w=World();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);
            var scene=w.gameObject.scene;EditorSceneManager.SaveScene(scene);
            string backup=ArteryPolishReview.Reports+"/Backups/ArteryNarrativeVR_before_VisualV8.unity";
            if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(scene.path,backup))throw new IOException("Fallback scene copy failed");
            var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            File.WriteAllText(Reports+"/BeforeAudit.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometry=GeometrySignature(w),materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Select(m=>new{m.name,asset=AssetDatabase.GetAssetPath(m),shader=m.shader.name}),wallUV=w.art.wallChunks.Take(3).Select(r=>new{r.name,bounds=r.GetComponent<MeshFilter>().sharedMesh.bounds.ToString("R"),uv=r.GetComponent<MeshFilter>().sharedMesh.uv.Take(10).Select(t=>t.ToString("R"))}),lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(l=>new{l.name,l.enabled,color=l.color.ToString("R"),l.intensity,l.range})},Formatting.Indented,JsonSettings));
        }
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        static void ImportTexture(string path,bool normal,bool srgb)
        {
            AssetDatabase.ImportAsset(path);var i=(TextureImporter)AssetImporter.GetAtPath(path);
            i.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;i.sRGBTexture=srgb;i.mipmapEnabled=true;
            i.wrapMode=TextureWrapMode.Repeat;i.filterMode=FilterMode.Trilinear;i.anisoLevel=4;i.maxTextureSize=2048;
            var android=i.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=2048;android.format=TextureImporterFormat.ASTC_6x6;i.SetPlatformTextureSettings(android);i.SaveAndReimport();
        }
        [MenuItem("Biology VR/Visual V8/Apply Materials And Lighting")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var w=World();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);
            string before=GeometrySignature(w);
            foreach(string map in new[]{"Base","Normal","Detail"})ImportTexture(Folder+"/Endothelium_"+map+"_V2.png",map=="Normal",map=="Base");
            var tissue=Shader.Find("BiologyVR/Artery Visual V2");var cell=Shader.Find("BiologyVR/Scientific Cell V2");
            if(!tissue||!cell||ShaderUtil.ShaderHasError(tissue)||ShaderUtil.ShaderHasError(cell))throw new InvalidOperationException("Visual V2 shader must compile cleanly");
            var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>!w.labRoom||!r.transform.IsChildOf(w.labRoom.transform)).ToArray();
            int materialCountBefore=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Count();
            var mapping=new Dictionary<Material,Material>();
            foreach(var source in renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().ToArray())
            {
                bool existing=AssetDatabase.GetAssetPath(source).StartsWith(Folder);
                bool wall=source.shader.name=="BiologyVR/Soft Mobile Tissue"||source.shader==tissue;
                bool biological=source.shader.name=="BiologyVR/Soft Inked Cell"||source.shader==cell||source.name.IndexOf("Virus",StringComparison.OrdinalIgnoreCase)>=0||source.name.IndexOf("Final focus",StringComparison.OrdinalIgnoreCase)>=0;
                if(!wall&&!biological)continue;
                var mat=new Material(source){name=existing?source.name:source.name+"_Visual_V2",enableInstancing=true};mat.shader=wall?tissue:cell;
                string n=source.name.ToLowerInvariant();
                if(wall)
                {
                    bool endothelium=n.Contains("endothelium")||n.Contains("subendothelial")||n.Contains("membrane")||n.Contains("recovery")||n.Contains("recovered")||n.Contains("wound");
                    if(endothelium)
                    {
                        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Endothelium_Base_V2.png"));mat.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Endothelium_Normal_V2.png"));
                        if(n.Contains("scene04")){float s=w.mover.path.Anchor(4)+3;mat.SetTextureScale("_BaseMap",new Vector2(.8f*w.mover.path.Radius(s)/3.5f,2.44f/5.6f));mat.SetTextureOffset("_BaseMap",new Vector2(-.4f*w.mover.path.Radius(s)/3.5f,(s-1.22f)/5.6f));}
                    }
                    mat.SetTexture("_DetailMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Endothelium_Detail_V2.png"));mat.SetFloat("_NormalStrength",1.10f);mat.SetFloat("_Smoothness",.28f);
                    if(n.Contains("cap"))mat.SetColor("_PatchColor",Hex("FFF0C7"));
                    if(n.Contains("core"))mat.SetColor("_PatchColor",Hex("F5AB31"));
                }
                else
                {
                    Color palette=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):Color.white;Color ink=Hex("633947");
                    if(n.Contains("rbc")||n.Contains("ruby")){palette=Hex("E9303C");ink=Hex("760F29");}
                    else if(n.Contains("granule")){palette=Hex("C6AEDF");ink=Hex("775495");}
                    else if(n.Contains("whitecell")||n.Contains("lavender_white")||n.Contains("pearl")){palette=Hex("F5EAF8");ink=Hex("977DB5");}
                    else if(n.Contains("platelet")||n.Contains("peach")){palette=Hex("F9B76A");ink=Hex("AF6882");}
                    else if(n.Contains("lipid")||n.Contains("gold")){palette=Hex("FFC14F");ink=Hex("A96322");}
                    else if(n.Contains("genome")){palette=Hex("F6A06A");ink=Hex("9C586D");}
                    else if(n.Contains("fibrouscap")||n.Contains("fibrin")||n.Contains("ivory")){palette=Hex("FFF0CF");ink=Hex("AD8992");}
                    else if(n.Contains("virus")||n.Contains("violet")||n.Contains("macrophage")||n.Contains("nucleus")){palette=Hex("AF66CD");ink=Hex("63316F");}
                    else if(n.Contains("cyan")){palette=Hex("54DDE5");ink=Hex("287986");}
                    mat.SetColor("_PaletteColor",palette);mat.SetColor("_InkColor",ink);mat.SetFloat("_InkStrength",.20f);mat.SetFloat("_Smoothness",.28f);
                    if(n.Contains("rbc")||n.Contains("ruby"))
                    {
                        var renderer=renderers.FirstOrDefault(r=>r.sharedMaterials.Contains(source)&&r.GetComponent<MeshFilter>());
                        if(renderer){var bounds=renderer.GetComponent<MeshFilter>().sharedMesh.bounds;var size=bounds.size;mat.SetVector("_CellCenter",bounds.center);mat.SetVector("_CellSize",size);mat.SetFloat("_CavityAxis",size.x<size.y&&size.x<size.z?0:size.y<size.z?1:2);mat.SetFloat("_CavityStrength",.48f);}
                    }
                }
                string asset=Folder+"/"+mat.name.Replace("/","_")+".mat";JourneyGeometry.Save(mat,asset);mapping[source]=AssetDatabase.LoadAssetAtPath<Material>(asset);
            }
            foreach(var r in renderers)
            {var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++)if(slots[i]&&mapping.TryGetValue(slots[i],out var v))slots[i]=v;r.sharedMaterials=slots;EditorUtility.SetDirty(r);}
            var macrophage=new Material(cell){name="Violet_Macrophage_Visual_V2",enableInstancing=true};macrophage.SetColor("_BaseColor",Color.white);macrophage.SetColor("_PaletteColor",Hex("C4A4DF"));macrophage.SetColor("_InkColor",Hex("73548F"));macrophage.SetFloat("_InkStrength",.2f);JourneyGeometry.Save(macrophage,Folder+"/Violet_Macrophage_Visual_V2.mat");
            macrophage=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Violet_Macrophage_Visual_V2.mat");
            foreach(var r in renderers.Where(r=>r.name.Contains("Embedded foam cell")||r.name.Contains("Foam macrophage")))r.sharedMaterials=Enumerable.Repeat(macrophage,r.sharedMaterials.Length).ToArray();
            // Only visual material arrays are remapped; data/input/physics fields remain untouched.
            foreach(var component in new UnityEngine.Object[]{w.art})
            {
                var so=new SerializedObject(component);var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue is Material old&&mapping.TryGetValue(old,out var replacement))p.objectReferenceValue=replacement;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {if(w.labRoom&&light.transform.IsChildOf(w.labRoom.transform))continue;light.color=new Color(1,.97f,.91f);if(light.type==LightType.Directional){light.intensity=1.15f;light.transform.rotation=Quaternion.Euler(35,-24,0);}light.shadows=LightShadows.None;EditorUtility.SetDirty(light);}
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.67f,.66f,.67f);RenderSettings.ambientEquatorColor=new Color(.43f,.39f,.38f);RenderSettings.ambientGroundColor=new Color(.27f,.23f,.24f);
            RenderSettings.fogColor=new Color(.80f,.71f,.66f);RenderSettings.fogStartDistance=35;RenderSettings.fogEndDistance=85;
            var root=GameObject.Find("POLISH — anatomy, chunks and materials");var environment=root.GetComponent<ArteryVisualEnvironmentV8>();if(!environment)environment=root.AddComponent<ArteryVisualEnvironmentV8>();environment.world=w;
            string after=GeometrySignature(w);if(before!=after)throw new InvalidOperationException("Visual application changed geometry/targets/colliders: restore fallback");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
            File.WriteAllText(Reports+"/Apply.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometryBefore=before,geometryAfter=after,geometryPreserved=before==after,materialCountBefore,activeMaterials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Count(),replacements=mapping.Select(pair=>new{old=AssetDatabase.GetAssetPath(pair.Key),version=AssetDatabase.GetAssetPath(pair.Value)}),shaderErrors=ShaderUtil.ShaderHasError(tissue)||ShaderUtil.ShaderHasError(cell)},Formatting.Indented));
        }
        [MenuItem("Biology VR/Visual V8/Capture Before")]
        public static void Before()=>BeginCapture("Before");
        [MenuItem("Biology VR/Visual V8/Capture After")]
        public static void After()=>BeginCapture("After");
        [MenuItem("Biology VR/Cartoon V3/Capture Before")]
        public static void BeforeCartoon()=>BeginCapture("BeforeCartoonV3");
        [MenuItem("Biology VR/Cartoon V3/Capture After")]
        public static void AfterCartoon()=>BeginCapture("CartoonV3");
        [MenuItem("Biology VR/Concept V4/Capture Before")]
        public static void BeforeConcept()=>BeginCapture("BeforeConceptV4");
        [MenuItem("Biology VR/Concept V4/Capture After")]
        public static void AfterConcept()=>BeginCapture("ConceptV4");
        static void BeginCapture(string version)
        {if(!EditorApplication.isPlaying||captureRunning)throw new InvalidOperationException("Fresh Play Mode and no other capture required");captureRunning=true;EditorCoroutineUtility.StartCoroutineOwnerless(Capture(version));}
        static IEnumerator Capture(string version)
        {
            var w=World();var m=w.mission;var camera=w.mover.viewCamera;var playerPosition=camera.transform.position;var playerRotation=camera.transform.rotation;
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None);var canvasStates=canvases.Select(c=>c.enabled).ToArray();
            string destination=Reports+"/"+version;Directory.CreateDirectory(destination);
            try
            {
                m.Enter(3,false);yield return new EditorWaitForSeconds(.4f);
                var p=w.mover.path;float s=p.Anchor(3);
                camera.transform.SetPositionAndRotation(p.Centre(s)+p.Up(s)*.15f,p.Frame(s+2));
                ArteryPolishReview.Capture(camera,"VisualV8/"+version+"/01_Scene03_Overview.png");
                foreach(var c in canvases)c.enabled=false;
                camera.transform.SetPositionAndRotation(p.Offset(s+3,p.Radius(s+3)-1.15f,0),Quaternion.LookRotation(p.Right(s+3),p.Up(s+3)));
                ArteryPolishReview.Capture(camera,"VisualV8/"+version+"/02_Scene03_Wall_Close.png");
                m.Enter(4,false);yield return new EditorWaitForSeconds(.3f);s=p.Anchor(4)+3;
                camera.transform.SetPositionAndRotation(p.Centre(s-1),Quaternion.LookRotation(p.Offset(s,p.Radius(s)-.05f,0)-p.Centre(s-1),p.Up(s)));
                ArteryPolishReview.Capture(camera,"VisualV8/"+version+"/03_Scene04_Endothelium.png");
                m.Enter(6,false);yield return new EditorWaitForSeconds(.3f);m.Accept(StudyAction.Measure,"plaque-flow");m.Accept(StudyAction.Scan,"plaque");yield return new EditorWaitForSeconds(3f);s=p.Anchor(6)+4;
                camera.transform.SetPositionAndRotation(p.Centre(s-1.2f),Quaternion.LookRotation(p.Offset(s,p.Radius(s)-.3f,0)-p.Centre(s-1.2f),p.Up(s)));
                ArteryPolishReview.Capture(camera,"VisualV8/"+version+"/04_Scene06_Plaque.png");
                m.Enter(11,false);yield return new EditorWaitForSeconds(.4f);s=p.Anchor(11)+5;
                camera.transform.SetPositionAndRotation(p.Centre(s-1.6f),Quaternion.LookRotation(w.woundSite.position-p.Centre(s-1.6f),p.Up(s)));
                ArteryPolishReview.Capture(camera,"VisualV8/"+version+"/05_Scene11_Wound.png");
                m.Enter(3,false);yield return new EditorWaitForSeconds(.3f);s=p.Anchor(3)+4;
                var hidden=new Dictionary<Renderer,bool>();
                foreach(var root in w.locationRoots)foreach(var r in root.GetComponentsInChildren<Renderer>(true)){hidden[r]=r.enabled;r.enabled=false;}
                foreach(var r in w.GetComponent<JourneyBloodFlow>().cells.Where(t=>t).SelectMany(t=>t.GetComponentsInChildren<Renderer>(true))){hidden[r]=r.enabled;r.enabled=false;}
                foreach(var r in w.mover.origin.GetComponentsInChildren<Renderer>(true)){hidden[r]=r.enabled;r.enabled=false;}
                camera.transform.SetPositionAndRotation(p.Centre(s),p.Frame(s+4));
                ArteryPolishReview.Capture(camera,"VisualV8/"+version+"/06_Tunnel_Environment_Only.png");
                foreach(var pair in hidden)if(pair.Key)pair.Key.enabled=pair.Value;
                File.WriteAllText(destination+"/Capture.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,version,method="Unity URP camera; staged scenes only; environment-only frame disables cells/UI/instruments temporarily",hardwareVRTested=false},Formatting.Indented));
            }
            finally{for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=canvasStates[i];camera.transform.SetPositionAndRotation(playerPosition,playerRotation);captureRunning=false;}
        }
    }
}

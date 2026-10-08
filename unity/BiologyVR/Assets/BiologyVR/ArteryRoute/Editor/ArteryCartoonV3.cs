using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryCartoonV3
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/Artery_Cartoon_V3";
        public const string ConceptFolder=JourneyGeometry.Root+"/Generated/Artery_Concept_V4";
        [MenuItem("Biology VR/Cartoon V3/Prepare Fallback")]
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            Directory.CreateDirectory(Folder);
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene);
            string backup=ArteryPolishReview.Reports+"/Backups/ArteryNarrativeVR_before_CartoonV3.unity";
            if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(scene.path,backup))throw new IOException("Fallback copy failed");
        }
        [MenuItem("Biology VR/Cartoon V3/Apply Muted Wall")]
        public static void Apply()=>ApplyVariant(Folder,"V3",false);
        [MenuItem("Biology VR/Concept V4/Prepare Fallback")]
        public static void PrepareConcept()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");Directory.CreateDirectory(ConceptFolder);
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene);
            string backup=ArteryPolishReview.Reports+"/Backups/ArteryNarrativeVR_before_ConceptV4.unity";
            if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(scene.path,backup))throw new IOException("Fallback copy failed");
        }
        [MenuItem("Biology VR/Concept V4/Apply Bright Warm Wall")]
        public static void ApplyConcept()=>ApplyVariant(ConceptFolder,"V4",true);
        static void ApplyVariant(string folder,string version,bool bright)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();string before=ArteryVisualV8.GeometrySignature(w);
            var shader=Shader.Find("BiologyVR/Artery Visual V2");if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Tissue shader did not compile");
            foreach(string map in new[]{"Base","Normal","Detail"})
            {
                string path=folder+"/Endothelium_"+map+"_"+version+".png";AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new IOException("Missing cartoon wall bake");
                importer.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=map=="Base";importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;
                var mobile=importer.GetPlatformTextureSettings("Android");mobile.overridden=true;mobile.maxTextureSize=2048;mobile.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(mobile);importer.SaveAndReimport();
            }
            var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var versions=new Dictionary<Material,Material>();int countBefore=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Count();
            foreach(var source in renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader==shader).Distinct().ToArray())
            {
                bool existing=AssetDatabase.GetAssetPath(source).StartsWith(folder);var mat=new Material(source){name=existing?source.name:source.name.Replace("_Visual_V2","").Replace("_Cartoon_V3","").Replace("_Concept_V4","")+(bright?"_Concept_V4":"_Cartoon_V3"),enableInstancing=true};
                string name=source.name.ToLowerInvariant();
                if(name.Contains("endothelium")||name.Contains("subendothelial")||name.Contains("membrane")||name.Contains("recovery")||name.Contains("recovered")||name.Contains("wound"))
                {
                    mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Endothelium_Base_"+version+".png"));mat.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Endothelium_Normal_"+version+".png"));
                    mat.SetTexture("_DetailMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Endothelium_Detail_"+version+".png"));
                }
                mat.SetFloat("_CartoonBands",bright?.8f:.9f);mat.SetFloat("_VisualExposure",bright?1.02f:.92f);mat.SetFloat("_NormalStrength",bright?.85f:.65f);mat.SetFloat("_Smoothness",bright?.23f:.16f);mat.SetFloat("_SheenStrength",bright?.08f:.055f);mat.SetFloat("_RimStrength",bright?.03f:.02f);
                string path=folder+"/"+mat.name+".mat";JourneyGeometry.Save(mat,path);versions[source]=AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            foreach(var r in renderers){var slots=r.sharedMaterials;bool changed=false;for(int i=0;i<slots.Length;i++)if(slots[i]&&versions.TryGetValue(slots[i],out var v)){slots[i]=v;changed=true;}if(changed){r.sharedMaterials=slots;EditorUtility.SetDirty(r);}}
            string after=ArteryVisualV8.GeometrySignature(w);if(before!=after)throw new InvalidOperationException("Wall material change altered geometry/targets");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
            File.WriteAllText(folder+"/Apply.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometryPreserved=before==after,geometryBefore=before,geometryAfter=after,materialCountBefore=countBefore,materialCountAfter=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Count(),materials=versions.Values.Select(m=>AssetDatabase.GetAssetPath(m)),brightness=bright?1.02f:.92f,normalStrength=bright?.85f:.65f,cartoonBands=bright?.8f:.9f,oldVersionPreserved=true},Formatting.Indented));
        }
    }
}

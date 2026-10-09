using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryVisualV6
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/Artery_Coral_V6";
        public const string Reports=ArteryPolishReview.Reports+"/BioWorldV6";
        [MenuItem("Biology VR/Coral V6/Save Current Visual Backup")]
        public static void Backup()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            Directory.CreateDirectory(Reports+"/Backup");Directory.CreateDirectory(Folder);
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene);
            string saved=Reports+"/Backup/ArteryNarrativeVR_before_CoralV6.unity";
            if(!File.Exists(saved)&&!AssetDatabase.CopyAsset(scene.path,saved))throw new IOException("Scene backup failed");
            foreach(string path in new[]{"Assets/BiologyVR/ArteryRoute/Shaders/ArteryVisualV2.shader","Assets/BiologyVR/ArteryRoute/Runtime/Journey/ArteryVisualEnvironmentV8.cs","Assets/BiologyVR/ArteryRoute/Runtime/Journey/JourneyArteryPresentation.cs"})
            {string copy=Reports+"/Backup/"+Path.GetFileName(path)+".txt";if(!File.Exists(copy))File.Copy(path,copy);}
            AssetDatabase.Refresh();
        }
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        [MenuItem("Biology VR/Coral V6/Apply Smooth Artery First Pass")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            Backup();
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();string before=ArteryVisualV8.GeometrySignature(w);
            var maps=new Dictionary<string,Texture2D>();
            foreach(string map in new[]{"Base","Normal","Detail"})
            {
                string path=Folder+"/Endothelium_"+map+"_V6.png";AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new IOException("Bake V6 first");
                importer.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=map=="Base";
                importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;
                var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=2048;android.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
                maps[map]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var tissue=Shader.Find("BiologyVR/Artery Visual V2");var cell=Shader.Find("BiologyVR/Scientific Cell V2");
            if(!tissue||!cell||ShaderUtil.ShaderHasError(tissue)||ShaderUtil.ShaderHasError(cell))throw new InvalidOperationException("Shaders must compile without errors");
            var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>!w.labRoom||!r.transform.IsChildOf(w.labRoom.transform)).ToArray();
            var changes=new Dictionary<Material,Material>();
            foreach(var source in renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().ToArray())
            {
                string n=source.name.ToLowerInvariant();bool wall=source.shader==tissue;
                bool rbc=source.shader==cell&&(n.Contains("rbc")||n.Contains("ruby")||n.Contains("erythrocyte"));
                bool wbc=source.shader==cell&&(n.Contains("whitecell")||n.Contains("leukocyte"));
                bool platelet=source.shader==cell&&n.Contains("platelet");
                bool lipid=source.shader==cell&&n.Contains("lipid");bool clot=source.shader==cell&&n.Contains("clot");
                if(!wall&&!rbc&&!wbc&&!platelet&&!lipid&&!clot)continue;
                var mat=new Material(source){name=source.name.Replace("_BioWorld_V5","").Replace("_Visual_V2","").Replace("_Coral_V6","")+"_Coral_V6",enableInstancing=true};
                if(wall)
                {
                    if(n.Contains("endothelium")||n.Contains("subendothelial")||n.Contains("membrane")||n.Contains("recovery")||n.Contains("wound"))
                    {mat.SetTexture("_BaseMap",maps["Base"]);mat.SetTexture("_NormalMap",maps["Normal"]);mat.SetTexture("_DetailMap",maps["Detail"]);}
                    mat.SetFloat("_OrganicRelief",0);mat.SetFloat("_NormalStrength",.70f);mat.SetFloat("_CartoonBands",.55f);
                    mat.SetFloat("_VisualExposure",1.04f);mat.SetFloat("_Smoothness",.19f);mat.SetFloat("_SheenStrength",.055f);mat.SetFloat("_RimStrength",.018f);
                    mat.SetFloat("_CircumferenceTiles",n.Contains("coral_endothelium")?7:0);mat.SetColor("_ZoneTint",Color.white);
                    mat.SetColor("_DepthColor",Hex("A04A51"));mat.SetFloat("_DepthStrength",.26f);
                    if(n.Contains("embeddedcore"))mat.SetColor("_PatchColor",Hex("C99B4E"));
                }
                else
                {
                    if(rbc){mat.SetColor("_PaletteColor",Hex("C51F32"));mat.SetColor("_InkColor",Hex("771C32"));mat.SetFloat("_CavityStrength",.46f);mat.SetFloat("_Smoothness",.34f);mat.SetFloat("_InkStrength",.14f);}
                    if(wbc){mat.SetColor("_PaletteColor",Hex("F0F4FB"));mat.SetColor("_InkColor",Hex("8E88AE"));mat.SetFloat("_InkStrength",.14f);}
                    if(platelet)mat.SetColor("_PaletteColor",Hex("F1AF54"));
                    if(lipid)mat.SetColor("_PaletteColor",Hex("D0A34F"));
                    if(clot)mat.SetColor("_PaletteColor",Hex("8F2940"));
                }
                string asset=Folder+"/"+mat.name+".mat";JourneyGeometry.Save(mat,asset);changes[source]=AssetDatabase.LoadAssetAtPath<Material>(asset);
            }
            foreach(var r in renderers)
            {var slots=r.sharedMaterials;bool changed=false;for(int i=0;i<slots.Length;i++)if(slots[i]&&changes.TryGetValue(slots[i],out var next)){slots[i]=next;changed=true;}if(changed){r.sharedMaterials=slots;EditorUtility.SetDirty(r);}}
            // Remap only material references used by existing visual writers.
            foreach(var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var so=new SerializedObject(behaviour);var field=so.GetIterator();bool changed=false;
                while(field.Next(true))if(field.propertyType==SerializedPropertyType.ObjectReference&&field.objectReferenceValue is Material old&&changes.TryGetValue(old,out var next)){field.objectReferenceValue=next;changed=true;}
                if(changed)so.ApplyModifiedPropertiesWithoutUndo();
            }
            var decor=GameObject.Find("BIOWORLD V5 — story guidance");if(decor){decor.SetActive(false);EditorUtility.SetDirty(decor);}
            var presentation=UnityEngine.Object.FindFirstObjectByType<JourneyArteryPresentation>();if(presentation){presentation.PulseAmount=.012f;presentation.PulseBpm=72;EditorUtility.SetDirty(presentation);}
            var environment=UnityEngine.Object.FindFirstObjectByType<ArteryVisualEnvironmentV8>();if(environment)
            {environment.fogColor=Hex("994F55");environment.fogStart=55;environment.fogEnd=110;environment.localPathologyTint=true;EditorUtility.SetDirty(environment);}
            var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>!w.labRoom||!l.transform.IsChildOf(w.labRoom.transform)).ToArray();
            var environmental=lights.Where(l=>!w.mover.origin||!l.transform.IsChildOf(w.mover.origin.transform)).ToArray();
            var key=environmental.FirstOrDefault(l=>l.type==LightType.Directional&&l.name.Contains("warm"))??environmental.FirstOrDefault(l=>l.type==LightType.Directional);
            if(!key)
            {
                var go=new GameObject("Artery V6 — warm main light");key=go.AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(35,-24,0);
                lights=lights.Concat(new[]{key}).ToArray();
            }
            foreach(var light in lights)
            {if(w.mover.origin&&light.transform.IsChildOf(w.mover.origin.transform))continue;light.enabled=light==key;if(light==key){light.color=new Color(1,.96f,.91f);light.intensity=1.05f;}light.shadows=LightShadows.None;EditorUtility.SetDirty(light);}
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.59f,.57f);RenderSettings.ambientEquatorColor=new Color(.43f,.37f,.36f);RenderSettings.ambientGroundColor=new Color(.26f,.21f,.23f);
            RenderSettings.sun=key;
            string after=ArteryVisualV8.GeometrySignature(w);if(before!=after)throw new InvalidOperationException("Visual-only pass altered gameplay geometry");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
            File.WriteAllText(Folder+"/Apply.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometryPreserved=before==after,geometryBefore=before,geometryAfter=after,normalStrengthBefore=1.1f,normalStrengthAfter=.70f,realtimePanelDisplacement=false,permanentCyanDecor=false,pulseAmount=.012f,pulseBpm=72,mainRealtimeLights=lights.Count(l=>l.enabled&&(!w.mover.origin||!l.transform.IsChildOf(w.mover.origin.transform))),newPhysicsObjects=0,textureReadsFragment=3,textureReadsVertex=0,materials=changes.Values.Select(m=>AssetDatabase.GetAssetPath(m)).ToArray(),scope="First visual pass only; hardware FPS unverified"},Formatting.Indented));
        }
    }
}

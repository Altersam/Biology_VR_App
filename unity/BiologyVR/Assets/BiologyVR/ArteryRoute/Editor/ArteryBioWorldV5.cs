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
    public static class ArteryBioWorldV5
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/Artery_BioWorld_V5";
        [MenuItem("Biology VR/BioWorld V5/Apply Organic Panels And Story Zones")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();if(!w)throw new InvalidOperationException("Current artery scene missing");
            string before=ArteryVisualV8.GeometrySignature(w);
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(ArteryPolishReview.Reports+"/BioWorldV5");
            var textures=new Dictionary<string,Texture2D>();
            foreach(string map in new[]{"Base","Normal","Detail"})
            {
                string path=Folder+"/Endothelium_"+map+"_V5.png";AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new IOException("Bake V5 textures first");
                importer.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=map=="Base";importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
                importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;
                var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=2048;android.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();textures[map]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var shader=Shader.Find("BiologyVR/Artery Visual V2");if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Wall shader must compile cleanly");
            var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>!w.labRoom||!r.transform.IsChildOf(w.labRoom.transform)).ToArray();
            var versions=new Dictionary<Material,Material>();
            foreach(var source in renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader==shader).Distinct().ToArray())
            {
                var mat=new Material(source){name=source.name.Replace("_Concept_V4","").Replace("_Visual_V2","").Replace("_BioWorld_V5","")+"_BioWorld_V5",enableInstancing=true};
                string name=source.name.ToLowerInvariant();
                if(name.Contains("endothelium")||name.Contains("subendothelial")||name.Contains("membrane")||name.Contains("recovery")||name.Contains("wound"))
                {mat.SetTexture("_BaseMap",textures["Base"]);mat.SetTexture("_NormalMap",textures["Normal"]);mat.SetTexture("_DetailMap",textures["Detail"]);}
                mat.SetFloat("_CartoonBands",.72f);mat.SetFloat("_VisualExposure",1.03f);mat.SetFloat("_NormalStrength",1.1f);
                mat.SetFloat("_Smoothness",.22f);mat.SetFloat("_SheenStrength",.075f);mat.SetFloat("_RimStrength",.025f);
                bool main=name.Contains("coral_endothelium");mat.SetFloat("_OrganicRelief",main?.07f:0);
                mat.SetFloat("_CircumferenceTiles",main?7:0);mat.SetColor("_ZoneTint",Color.white);
                mat.SetVector("_ProtectedArcs",new Vector4(w.mover.path.Anchor(4)+3,w.mover.path.Anchor(6)+4,w.mover.path.Anchor(11)+5,2.8f));
                string path=Folder+"/"+mat.name+".mat";JourneyGeometry.Save(mat,path);versions[source]=AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            foreach(var r in renderers)
            {var slots=r.sharedMaterials;bool changed=false;for(int i=0;i<slots.Length;i++)if(slots[i]&&versions.TryGetValue(slots[i],out var next)){slots[i]=next;changed=true;}if(changed){r.sharedMaterials=slots;EditorUtility.SetDirty(r);}}
            var artData=new SerializedObject(w.art);var property=artData.GetIterator();while(property.Next(true))
                if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue is Material old&&versions.TryGetValue(old,out var replacement))property.objectReferenceValue=replacement;
            artData.ApplyModifiedPropertiesWithoutUndo();
            BuildGuidance(w);
            string after=ArteryVisualV8.GeometrySignature(w);if(before!=after)throw new InvalidOperationException("Visual pass changed gameplay geometry, targets or collider state");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
            var view=UnityEngine.Object.FindFirstObjectByType<ArteryBioWorldV5View>();
            File.WriteAllText(Folder+"/Apply.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometryPreserved=before==after,geometryBefore=before,geometryAfter=after,wallPanelGrid="7 x 3",visualReliefMeters=.07f,protectedInspectionPlaqueWound=true,zoneCount=view.labels.Length,decorativeRendererCount=view.decorations.Length,newColliders=0,materials=versions.Values.Select(m=>AssetDatabase.GetAssetPath(m)).ToArray(),hardwareVerified=false},Formatting.Indented));
        }
        static void BuildGuidance(JourneyWorld w)
        {
            var old=GameObject.Find("BIOWORLD V5 — story guidance");if(old)UnityEngine.Object.DestroyImmediate(old);
            var root=new GameObject("BIOWORLD V5 — story guidance");var view=root.AddComponent<ArteryBioWorldV5View>();view.world=w;
            view.walls=w.art.wallChunks;view.wallArcs=w.art.chunkDistances;
            var cyan=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="BioWorld_Cyan_Guidance_V5",enableInstancing=true};cyan.SetColor("_BaseColor",new Color(.22f,.79f,.87f));
            var lime=new Material(cyan){name="BioWorld_Healthy_Lime_V5"};lime.SetColor("_BaseColor",new Color(.63f,.81f,.24f));
            JourneyGeometry.Save(cyan,Folder+"/Cyan_Guidance.mat");JourneyGeometry.Save(lime,Folder+"/Healthy_Lime.mat");
            cyan=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Cyan_Guidance.mat");lime=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Healthy_Lime.mat");
            int[] anchors={3,5,6,7,11,12,14};view.firstScenes=new[]{3,5,6,7,11,12,14};view.lastScenes=new[]{4,5,6,10,11,13,16};
            view.captions=new[]{"01 · СОСТАВ КРОВИ\nЗдоровая артерия · перенос кислорода","02 · КРОВОТОК\nРадиус · давление · движение клеток","03 · ХОЛЕСТЕРИНОВАЯ БЛЯШКА\nСужение просвета · точная работа BioTool","04 · ЗАДЕРЖКА ПОТОКА\nЭмбол · стабилизация · иммунная защита","05 · ПОВРЕЖДЕНИЕ СТЕНКИ\nТот же дефект · диагностика утечки","06 · ГЕМОСТАЗ\nТромбоциты · фибрин · сохранение просвета","07 · ВОССТАНОВЛЕНИЕ\nОчистка · заживление · возвращение баланса"};
            view.labels=new UnityEngine.UI.Text[anchors.Length];view.labelPositions=new Vector3[anchors.Length];view.zoneArcs=new float[anchors.Length];
            var renderers=new List<Renderer>();var zones=new List<int>();
            for(int zone=0;zone<anchors.Length;zone++)
            {
                float s=w.mover.path.Anchor(anchors[zone]);view.zoneArcs[zone]=s;
                var zoneRoot=new GameObject(view.captions[zone].Split('\n')[0]);zoneRoot.transform.SetParent(root.transform,false);
                float offset=zone>=4?3.8f:4.2f;
                view.labelPositions[zone]=w.mover.path.Offset(s+offset,-1.55f,1.20f);
                var label=ArteryAnnotations.Label(zoneRoot.transform,"Cyan biological focus",view.captions[zone]);label.fontSize=18;label.rectTransform.sizeDelta=new Vector2(520,85);label.transform.localScale=Vector3.one*.0020f;
                label.transform.SetPositionAndRotation(view.labelPositions[zone],w.mover.path.Frame(s));view.labels[zone]=label;
                // A partial ceiling arc, never a blocking full ring or a corridor of neon.
                var arc=new Vector3[29];float radius=w.mover.path.Radius(s)-.09f;
                for(int i=0;i<arc.Length;i++){float angle=Mathf.Lerp(.23f,.77f,i/(float)(arc.Length-1))*Mathf.PI;arc[i]=w.mover.path.Offset(s+offset,Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius);}
                renderers.Add(Line(zoneRoot.transform,"Ceiling holographic orientation",arc,.022f,cyan));zones.Add(zone);
                for(int mark=0;mark<2;mark++)
                {
                    float a=s+offset+3+mark*3;var p=w.mover.path;float floor=-p.Radius(a)+.14f;
                    var points=new[]{p.Offset(a,-.23f,floor),p.Offset(a+.50f,0,floor),p.Offset(a,.23f,floor)};
                    renderers.Add(Line(zoneRoot.transform,"Subtle progression chevron",points,.019f,zone==0?lime:cyan));zones.Add(zone);
                }
                // Few rounded biological accents for the healthy introduction only.
                if(zone==0)for(int i=0;i<3;i++)
                {
                    var cell=GameObject.CreatePrimitive(PrimitiveType.Sphere);cell.name="Healthy wall lime accent";UnityEngine.Object.DestroyImmediate(cell.GetComponent<Collider>());cell.transform.SetParent(zoneRoot.transform,false);
                    var p=w.mover.path;float a=s+4+i*.8f;cell.transform.SetPositionAndRotation(p.Offset(a,-p.Radius(a)+.11f,1.0f),p.Frame(a));cell.transform.localScale=new Vector3(.07f,.15f,.22f);
                    var r=cell.GetComponent<Renderer>();r.sharedMaterial=lime;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;renderers.Add(r);zones.Add(zone);
                }
            }
            view.decorations=renderers.ToArray();view.decorationZone=zones.ToArray();EditorUtility.SetDirty(view);
        }
        static LineRenderer Line(Transform parent,string name,Vector3[] points,float width,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=width;line.sharedMaterial=material;
            line.numCapVertices=3;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;return line;
        }
    }
}

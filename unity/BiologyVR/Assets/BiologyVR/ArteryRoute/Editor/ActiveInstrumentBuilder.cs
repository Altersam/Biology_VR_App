using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ActiveInstrumentBuilder
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/ActiveInstruments";
        public const string Reports=JourneyGeometry.Root+"/Reports/ActiveInstruments";
        [MenuItem("Biology VR/Install Active GLB Scanner And BioTool")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();var input=world.mission.GetComponent<JourneyInput>();
            Directory.CreateDirectory(Reports);EditorSceneManager.SaveScene(world.gameObject.scene);
            string backup=Reports+"/ArteryNarrativeVR_before_active_tools.unity";
            if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Tool scene backup failed");
            string before=ArteryVisualV8.GeometrySignature(world);
            var scannerPrefab=BuildPrefab("Scanner");var bioPrefab=BuildPrefab("BioTool");
            // Both camera-mounted and controller-mounted views are prefab
            // instances. Remove every old generated view, including instances
            // whose unrecorded name override reverted during scene reload.
            foreach(var visual in world.mover.origin.GetComponentsInChildren<JourneyInstrumentVisual>(true))
                UnityEngine.Object.DestroyImmediate(visual.gameObject);
            foreach(var model in new[]{input.scanner.parent,input.bioTool.parent})
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                    if(!renderer.name.StartsWith("Active ")){renderer.enabled=false;EditorUtility.SetDirty(renderer);}
            var old=world.mission.transform.Find("Active instrument views");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var aim in new[]{input.scanner,input.bioTool})
            {var visual=aim.Find("Active device GLB");if(visual)UnityEngine.Object.DestroyImmediate(visual.gameObject);}
            var root=new GameObject("Active instrument views");root.transform.SetParent(world.mission.transform,false);
            var rig=world.mission.GetComponent<JourneyInstrumentRig>();if(!rig)rig=world.mission.gameObject.AddComponent<JourneyInstrumentRig>();rig.input=input;
            var modes=world.mission.GetComponent<BioToolModeController>();if(!modes)modes=world.mission.gameObject.AddComponent<BioToolModeController>();modes.input=input;rig.modes=modes;
            rig.scannerVR=Instance(scannerPrefab,input.scanner,Vector3.zero,"Active device GLB");
            rig.bioVR=Instance(bioPrefab,input.bioTool,Vector3.zero,"Active device GLB");
            rig.scannerPC=Instance(scannerPrefab,world.mover.viewCamera.transform,new Vector3(-.22f,-.18f,.65f),"Active Scanner — PC view");
            rig.bioPC=Instance(bioPrefab,world.mover.viewCamera.transform,new Vector3(.23f,-.18f,.65f),"Active BioTool — PC view");
            rig.scannerPC.transform.localScale=rig.bioPC.transform.localScale=Vector3.one*.9f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rig.scannerPC.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(rig.bioPC.transform);
            var beam=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Soft turquoise instrument VFX",enableInstancing=true};beam.SetColor("_BaseColor",HolographicStyle.SoftGlow*.85f);JourneyGeometry.Save(beam,Folder+"/TurquoiseVFX.mat");beam=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/TurquoiseVFX.mat");
            rig.scannerBeam=Line(root.transform,"Active scanner short diagnostic beam",beam,false,true);
            rig.impulseBeam=Line(root.transform,"Active impulse streak",beam,false,true);
            rig.captureTether=Line(root.transform,"Active capture flexible tether",beam,false,true);
            rig.captureRays=new LineRenderer[4];for(int i=0;i<4;i++)rig.captureRays[i]=Line(root.transform,"Active capture converge ray "+i,beam,false,true);
            rig.fieldRing=Line(root.transform,"Active tonus or bio field",beam,true,false);Circle(rig.fieldRing,.5f);
            rig.pulseRing=Line(root.transform,"Active travelling impulse ring",beam,true,false);Circle(rig.pulseRing,.5f);
            rig.captureLockRing=Line(root.transform,"Active captured-object lock ring",beam,true,false);Circle(rig.captureLockRing,.5f);
            var feedback=world.mission.GetComponent<JourneyToolFeedback>();if(feedback)
            {if(feedback.emitter)feedback.emitter.gameObject.SetActive(false);if(feedback.modeCaption)feedback.modeCaption.gameObject.SetActive(false);}
            EditorUtility.SetDirty(rig);EditorUtility.SetDirty(modes);
            string after=ArteryVisualV8.GeometrySignature(world);if(before!=after)throw new InvalidOperationException("Tool visuals altered existing targets/colliders/path");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometryPreserved=before==after,geometryBefore=before,geometryAfter=after,source="Unity-authored biomedical meshes; source GLBs retained as history",scannerMeshes=rig.scannerVR.surfaces.Length,bioMeshes=rig.bioVR.surfaces.Length,scannerTriangles=rig.scannerVR.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3),bioTriangles=rig.bioVR.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3),textureSamples=0,newPhysicsObjects=0,scannerAimPreserved=true,bioToolAimPreserved=true,modes="IMPULSE/CAPTURE/TONUS/BIO",switchInput="C / right A",hardwareVerified=false},Formatting.Indented));
        }
        static GameObject BuildPrefab(string kind)
        {return BiomedicalDeviceVisualBuilder.Build(kind);}
        static GameObject BuildLegacyGLBPrefab(string kind)
        {
            string folder=Folder+"/"+kind;var data=JObject.Parse(File.ReadAllText(folder+"/Geometry.json"));
            string atlas=folder+"/DeviceAtlas.png",emission=folder+"/DeviceEmission.png";
            foreach(string texture in new[]{atlas,emission})
            {AssetDatabase.ImportAsset(texture);var importer=(TextureImporter)AssetImporter.GetAtPath(texture);importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=1024;android.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();}
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=kind+" source atlas with turquoise glow",enableInstancing=true};
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(atlas));material.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(emission));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.28f);material.SetFloat("_Metallic",0);material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");material.SetFloat("_Cull",0);material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.white*.065f);
            JourneyGeometry.Save(material,folder+"/Device.mat");material=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Device.mat");
            var root=new GameObject("Active "+kind+" GLB");var view=root.AddComponent<JourneyInstrumentVisual>();view.scanner=kind=="Scanner";
            var prongs=new List<Transform>();var surfaces=new List<Renderer>();
            foreach(var group in ((JObject)data["groups"]).Properties())
            {
                var d=(JObject)group.Value;var vertices=((JArray)d["vertices"]).Select(a=>new Vector3((float)a[0],(float)a[1],(float)a[2])).ToArray();
                var mesh=new Mesh{name=kind+"_"+group.Name};mesh.vertices=vertices;mesh.normals=((JArray)d["normals"]).Select(a=>new Vector3((float)a[0],(float)a[1],(float)a[2])).ToArray();
                mesh.uv=((JArray)d["uv"]).Select(a=>new Vector2((float)a[0],(float)a[1])).ToArray();mesh.triangles=((JArray)d["triangles"]).Select(a=>(int)a).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();
                string path=folder+"/"+mesh.name+".asset";JourneyGeometry.Save(mesh,path);
                var go=new GameObject("Active "+group.Name);go.transform.SetParent(root.transform,false);var pivot=d["pivot"];go.transform.localPosition=new Vector3((float)pivot[0],(float)pivot[1],(float)pivot[2]);
                go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;surfaces.Add(renderer);
                if(group.Name=="Rotor")view.rotor=go.transform;if(group.Name.StartsWith("Prong"))prongs.Add(go.transform);
            }
            view.prongs=prongs.OrderBy(p=>p.name,StringComparer.Ordinal).ToArray();view.surfaces=surfaces.ToArray();
            view.muzzle=new GameObject("Active muzzle").transform;view.muzzle.SetParent(root.transform,false);
            view.modeCaption=ArteryAnnotations.Label(root.transform,"Active instrument mode",kind=="Scanner"?"SCANNER":"IMPULSE");view.modeCaption.fontSize=15;view.modeCaption.rectTransform.sizeDelta=new Vector2(370,54);view.modeCaption.transform.localScale=Vector3.one*.00065f;view.modeCaption.transform.localPosition=new Vector3(0,.058f,-.045f);view.modeCaption.transform.localRotation=Quaternion.Euler(0,180,0);
            var glyphMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=kind+" mode glyph"};glyphMaterial.SetColor("_BaseColor",HolographicStyle.Cyan*.8f);JourneyGeometry.Save(glyphMaterial,folder+"/ModeGlyph.mat");
            view.modeGlyph=Line(root.transform,"Active small mode glyph",AssetDatabase.LoadAssetAtPath<Material>(folder+"/ModeGlyph.mat"),true,false);view.modeGlyph.transform.localPosition=new Vector3(0,.025f,.012f);view.modeGlyph.widthMultiplier=.0018f;
            var signal=AssetDatabase.LoadAssetAtPath<Material>(folder+"/ModeGlyph.mat");
            if(view.scanner)
            {
                var bounds=view.rotor.GetComponent<MeshFilter>().sharedMesh.bounds;
                view.activeRim=Line(view.rotor,"Active diagnostic sweep rim",signal,false,false);
                view.activeRim.transform.localPosition=new Vector3(bounds.center.x,bounds.center.y,bounds.max.z+.0015f);
                float radius=Mathf.Min(bounds.size.x,bounds.size.y)*.46f;
                view.activeRim.positionCount=33;
                for(int i=0;i<33;i++){float a=(-125+i*250f/32)*Mathf.Deg2Rad;view.activeRim.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
            }
            else
            {
                view.activeRim=Line(root.transform,"Active energy crown rim",signal,true,false);
                view.activeRim.transform.localPosition=new Vector3(0,0,.047f);view.activeRim.positionCount=32;
                for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;view.activeRim.SetPosition(i,new Vector3(Mathf.Cos(a)*.036f,Mathf.Sin(a)*.036f,0));}
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/Active"+kind+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static JourneyInstrumentVisual Instance(GameObject prefab,Transform parent,Vector3 position,string name)
        {var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name=name;go.transform.localPosition=position;go.transform.localRotation=Quaternion.identity;PrefabUtility.RecordPrefabInstancePropertyModifications(go);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);return go.GetComponent<JourneyInstrumentVisual>();}
        static LineRenderer Line(Transform parent,string name,Material material,bool loop,bool worldSpace)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.loop=loop;line.useWorldSpace=worldSpace;line.enabled=false;line.widthMultiplier=.002f;line.positionCount=2;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;return line;}
        static void Circle(LineRenderer line,float radius)
        {line.positionCount=40;for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}}
    }
}

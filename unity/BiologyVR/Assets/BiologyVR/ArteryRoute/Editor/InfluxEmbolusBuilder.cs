using System;
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
    public static class InfluxEmbolusBuilder
    {
        public const string Reports=JourneyGeometry.Root+"/Reports/InfluxAndEmboli";
        [MenuItem("Biology VR/Install Flow-fed Plaque And Six Emboli")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Directory.CreateDirectory(Reports+"/Backup");
            EditorSceneManager.SaveScene(world.gameObject.scene);string backup=Reports+"/Backup/ArteryNarrativeVR_before_influx_and_six_emboli.unity";if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Scene backup failed");
            PlaqueChallengeBuilder.Apply();
            var original=world.targets.First(t=>t.targetId=="embolus");var source=original.GetComponentsInChildren<MeshFilter>(true).First(f=>f.sharedMesh&&f.GetComponent<Renderer>());
            var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphereMesh=sphere.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sphere);
            var gasMaterial=new Material(Shader.Find("BiologyVR/Gas Bubble")){name="Gas bubble — translucent pearl rim",enableInstancing=true};gasMaterial.SetColor("_BaseColor",new Color(.52f,.90f,1));string gasPath=PlaqueChallengeBuilder.Folder+"/GasBubble_Pearl.mat";JourneyGeometry.Save(gasMaterial,gasPath);gasMaterial=AssetDatabase.LoadAssetAtPath<Material>(gasPath);
            foreach(var renderer in original.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=false;
            var oldVisual=original.transform.Find("Active pearl gas bubble");if(oldVisual)UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
            var mainVisual=new GameObject("Active pearl gas bubble",typeof(MeshFilter),typeof(MeshRenderer));mainVisual.transform.SetParent(original.transform,false);mainVisual.transform.localScale=Vector3.one*.60f/Mathf.Max(.001f,original.transform.lossyScale.x);mainVisual.GetComponent<MeshFilter>().sharedMesh=sphereMesh;mainVisual.GetComponent<MeshRenderer>().sharedMaterial=gasMaterial;mainVisual.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var location=world.locationRoots[world.mover.path.locationIds[7-3]].transform;var old=location.Find("Scene07 — six gas emboli");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Scene07 — six gas emboli");root.transform.SetParent(location,false);
            var wave=world.GetComponent<JourneyEmbolusWave>();if(!wave)wave=world.gameObject.AddComponent<JourneyEmbolusWave>();wave.world=world;wave.targets=new JourneyTarget[6];wave.targets[0]=original;
            wave.modeContacts=world.Find("attract-mode").GetComponentsInChildren<Collider>(true);wave.modeVisuals=world.Find("attract-mode").GetComponentsInChildren<Renderer>(true);
            for(int i=1;i<6;i++)
            {
                var go=new GameObject("Gas embolus "+(i+1)+" / 6");go.transform.SetParent(root.transform,false);go.layer=original.gameObject.layer;
                var target=go.AddComponent<JourneyTarget>();target.targetId="embolus";target.label="Газовый эмбол "+(i+1)+" / 6";target.mission=world.mission;
                float diameter=.55f+i*.055f;var contact=go.AddComponent<SphereCollider>();contact.radius=diameter*.48f;contact.isTrigger=true;
                var visual=new GameObject("Shared gas bubble shell",typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(go.transform,false);visual.layer=go.layer;visual.transform.localScale=Vector3.one*diameter;visual.GetComponent<MeshFilter>().sharedMesh=sphereMesh;var renderer=visual.GetComponent<MeshRenderer>();renderer.sharedMaterial=gasMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;wave.targets[i]=target;go.SetActive(false);
            }
            world.embolusWave=wave;EditorUtility.SetDirty(world);EditorUtility.SetDirty(wave);
            ActiveInstrumentBuilder.Apply();CleanHolographicScreensBuilder.Apply();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,initialPlaqueElements=30,pooledPlaqueSlots=90,randomFlowArrivalsPerSecond="1–2",shootableIncoming=true,emboli=wave.targets.Length,captureModes="A next / B previous / left Y continue",physicsBodiesAdded=0,hardwareVerified=false},Formatting.Indented));
        }
    }
}

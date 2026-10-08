using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryReferenceVisualOverhaul
    {
        const string RootName="REFERENCE LOOK — embedded anatomy and station accents";
        [MenuItem("Biology VR/Apply Reference Artery Visual Overhaul")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            var scene=SceneManager.GetActiveScene();var mover=UnityEngine.Object.FindFirstObjectByType<JourneyMover>();var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();
            if(!mover||!world||!mover.path)throw new InvalidOperationException("Open ArteryNarrativeVR first");
            var old=GameObject.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old);
            var root=new GameObject(RootName);var controller=root.AddComponent<ArteryReferenceVisualController>();controller.mission=world.mission;
            var sceneRoots=new List<GameObject>();var sceneIds=new List<int>();
            MakeStationRings(root.transform,mover.path);
            var plaque=world.Find("plaque");
            if(plaque)
            {
                float s=mover.path.Anchor(6)+4.0f;plaque.transform.position=mover.path.Offset(s,mover.path.Radius(s)-.62f,0);plaque.transform.rotation=mover.path.Frame(s)*Quaternion.Euler(90,0,0);plaque.transform.localScale=Vector3.one*.62f;
            }
            var plaqueRoot=CreatePlaqueInset(root.transform,mover.path,mover.path.Anchor(6)+4.0f);sceneRoots.Add(plaqueRoot);sceneIds.Add(6);
            var layerRoot=CreateLayerCutaway(root.transform,mover.path,mover.path.Anchor(4)+2.6f);sceneRoots.Add(layerRoot);sceneIds.Add(4);
            var woundRoot=CreateWoundAccent(root.transform,mover.path,mover.path.Anchor(11)+5);sceneRoots.Add(woundRoot);sceneIds.Add(11);
            var clotRoot=CreateClotAccent(root.transform,mover.path,mover.path.Anchor(12)+3.5f);sceneRoots.Add(clotRoot);sceneIds.Add(12);
            var finalRoot=CreateFinalFocusAccent(root.transform,mover.path,mover.path.Anchor(15)+3.5f);sceneRoots.Add(finalRoot);sceneIds.Add(15);
            world.labRoom=CreateLaboratoryRoom(root.transform,mover.path);
            controller.sceneRoots=sceneRoots.ToArray();controller.sceneIds=sceneIds.ToArray();controller.Refresh();
            ConfigureReferenceLights(scene);
            var hud=UnityEngine.Object.FindFirstObjectByType<JourneyHud>();if(hud)JourneyHudOverhaul.Apply(hud);
            BrightStylizedLabLook.Apply();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Reference artery visual overhaul applied: embedded plaque, wall cutaway, wound and final focus accents");
        }
        static void MakeStationRings(Transform parent,ArteryJourneyPath path)
        {
            foreach(var spec in new[]{(3,new Color(.15f,.90f,.86f)),(4,new Color(.95f,.70f,.26f)),(6,new Color(1f,.46f,.16f)),(11,new Color(.16f,.92f,.76f)),(15,new Color(.58f,.35f,1f))})
            {
                float s=path.Anchor(spec.Item1)+2.5f;var go=new GameObject($"Station {spec.Item1:00} holographic ring");go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=40;line.widthMultiplier=.026f;line.material=Unlit($"Station {spec.Item1:00} cyan accent",spec.Item2);for(int i=0;i<40;i++){float a=i/40f*Mathf.PI*2;line.SetPosition(i,path.Offset(s,Mathf.Cos(a)*(path.Radius(s)-.07f),Mathf.Sin(a)*(path.Radius(s)-.07f)));}
            }
        }
        static GameObject CreatePlaqueInset(Transform parent,ArteryJourneyPath path,float s)
        {
            var root=new GameObject("Scene 06 — plaque embedded in intima");root.transform.SetParent(parent,false);var center=path.Offset(s,path.Radius(s)-.30f,0);var bulge=GameObject.CreatePrimitive(PrimitiveType.Sphere);bulge.name="Lipid core inside arterial wall";bulge.transform.SetParent(root.transform,false);bulge.transform.position=center;bulge.transform.rotation=path.Frame(s);bulge.transform.localScale=new Vector3(.95f,.45f,.22f);bulge.GetComponent<Renderer>().sharedMaterial=Unlit("Lipid core warm amber",new Color(1f,.46f,.08f));UnityEngine.Object.DestroyImmediate(bulge.GetComponent<Collider>());var cap=GameObject.CreatePrimitive(PrimitiveType.Sphere);cap.name="Fibrous cap rounded edge";cap.transform.SetParent(root.transform,false);cap.transform.position=center+path.Right(s)*.03f+path.Up(s)*.18f;cap.transform.rotation=path.Frame(s);cap.transform.localScale=new Vector3(1.05f,.18f,.16f);cap.GetComponent<Renderer>().sharedMaterial=Unlit("Fibrous cap lime",new Color(.76f,.90f,.28f));UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());return root;
        }
        static GameObject CreateLayerCutaway(Transform parent,ArteryJourneyPath path,float s)
        {
            var root=new GameObject("Scene 04 — integrated arterial wall layers");root.transform.SetParent(parent,false);var center=path.Offset(s,path.Radius(s)-.18f,0);var specs=new[]{("Endothelium cyan",new Color(.20f,.92f,.82f),.10f),("Intima coral",new Color(.94f,.30f,.28f),.18f),("Media amber",new Color(1f,.55f,.18f),.28f),("Adventitia cream",new Color(1f,.84f,.54f),.38f)};foreach(var spec in specs){var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);part.name=spec.Item1;part.transform.SetParent(root.transform,false);part.transform.position=center+path.Right(s)*spec.Item3;part.transform.rotation=path.Frame(s);part.transform.localScale=new Vector3(.70f,.52f,.06f);part.GetComponent<Renderer>().sharedMaterial=Unlit(spec.Item1,spec.Item2);UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());}return root;
        }
        static GameObject CreateWoundAccent(Transform parent,ArteryJourneyPath path,float s)
        {
            var root=new GameObject("Scene 11 — diagnostic wound integrated edge");root.transform.SetParent(parent,false);var center=path.Offset(s,path.Radius(s)-.08f,0);var line=root.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=28;line.widthMultiplier=.038f;line.material=Unlit("Wound cyan diagnostic edge",new Color(.20f,.98f,.82f));for(int i=0;i<28;i++){float a=i/28f*Mathf.PI*2;var wobble=1f+.08f*Mathf.Sin(a*3f);line.SetPosition(i,center+path.Up(s)*Mathf.Cos(a)*.72f*wobble+path.Forward(s)*Mathf.Sin(a)*.52f*wobble);}return root;
        }
        static GameObject CreateClotAccent(Transform parent,ArteryJourneyPath path,float s)
        {
            var root=new GameObject("Scene 12 — wall-attached haemostasis accent");root.transform.SetParent(parent,false);var center=path.Offset(s,path.Radius(s)-.16f,0);for(int i=0;i<5;i++){var blob=GameObject.CreatePrimitive(PrimitiveType.Sphere);blob.name="Platelet warm cream accent";blob.transform.SetParent(root.transform,false);blob.transform.position=center+path.Up(s)*((i-2)*.18f)+path.Forward(s)*Mathf.Sin(i*1.7f)*.13f;blob.transform.localScale=Vector3.one*.16f;blob.GetComponent<Renderer>().sharedMaterial=Unlit("Platelet cream",new Color(1f,.76f,.36f));UnityEngine.Object.DestroyImmediate(blob.GetComponent<Collider>());}return root;
        }
        static GameObject CreateFinalFocusAccent(Transform parent,ArteryJourneyPath path,float s)
        {
            var root=new GameObject("Scene 15 — final immune focus halo");root.transform.SetParent(parent,false);var center=path.Offset(s,0,0);var line=root.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=36;line.widthMultiplier=.034f;line.material=Unlit("Final focus violet cyan",new Color(.40f,.80f,1f));for(int i=0;i<36;i++){float a=i/36f*Mathf.PI*2;line.SetPosition(i,center+path.Right(s)*Mathf.Cos(a)*1.05f+path.Up(s)*Mathf.Sin(a)*1.05f);}return root;
        }
        public static GameObject CreateLaboratoryRoom(Transform parent,ArteryJourneyPath path)
        {
            var room=new GameObject("Bright educational laboratory — return and free research");room.transform.SetParent(parent,false);room.transform.position=path.Centre(path.Length-8)+path.Up(path.Length-8)*2.2f+path.Right(path.Length-8)*5f;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Cream laboratory platform";floor.transform.SetParent(room.transform,false);floor.transform.localPosition=new Vector3(0,-.15f,0);floor.transform.localScale=new Vector3(7,.25f,6);floor.GetComponent<Renderer>().sharedMaterial=Unlit("Cream lab platform",new Color(1f,.84f,.58f));UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            foreach(var p in new[]{new Vector3(-3,1.5f,2.3f),new Vector3(3,1.5f,2.3f)}){var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name="Amber rounded lab wall panel";panel.transform.SetParent(room.transform,false);panel.transform.localPosition=p;panel.transform.localScale=new Vector3(.18f,3.2f,.35f);panel.GetComponent<Renderer>().sharedMaterial=Unlit("Warm amber panel",new Color(1f,.45f,.16f));UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());}
            var hologram=GameObject.CreatePrimitive(PrimitiveType.Cube);hologram.name="Cyan holographic report screen";hologram.transform.SetParent(room.transform,false);hologram.transform.localPosition=new Vector3(0,1.8f,2.0f);hologram.transform.localScale=new Vector3(3.8f,1.8f,.04f);hologram.GetComponent<Renderer>().sharedMaterial=Unlit("Cyan holographic panel",new Color(.08f,.78f,.82f));UnityEngine.Object.DestroyImmediate(hologram.GetComponent<Collider>());return room;
        }
        static void ConfigureReferenceLights(Scene scene)
        {
            foreach(var light in scene.GetRootGameObjects())foreach(var l in light.GetComponentsInChildren<Light>(true))l.shadows=LightShadows.None;
            var key=GameObject.Find("REFERENCE warm directional key")??new GameObject("REFERENCE warm directional key");var directional=key.GetComponent<Light>()??key.AddComponent<Light>();directional.type=LightType.Directional;directional.color=new Color(1f,.55f,.30f);directional.intensity=1.25f;directional.transform.rotation=Quaternion.Euler(32,-28,0);directional.shadows=LightShadows.None;
        }
        static Material Unlit(string name,Color color){var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name};mat.SetColor("_BaseColor",color);return mat;}
    }
}

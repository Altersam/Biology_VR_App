using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArcadeVisualPassBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/ArcadeVisualPass";
        const string Reports=JourneyGeometry.Root+"/Reports/Polish/ArcadeVisualPass";
        [MenuItem("Biology VR/Save Arcade Left Panel Placement")]
        public static void SavePanelPlacement()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var screens=UnityEngine.Object.FindFirstObjectByType<JourneyHolographicScreens>();
            string backup=Reports+"/Before_Left_Panel_Adjustment.unity";if(!File.Exists(backup))AssetDatabase.CopyAsset(screens.gameObject.scene.path,backup);
            screens.theoryRoot.anchoredPosition3D=new Vector3(-.43f,.04f,1.25f);screens.theoryRoot.localPosition=new Vector3(-.43f,.04f,1.25f);screens.theoryRoot.localRotation=Quaternion.LookRotation(screens.theoryRoot.localPosition,Vector3.up);
            EditorUtility.SetDirty(screens.theoryRoot);EditorUtility.SetDirty(screens);EditorSceneManager.MarkSceneDirty(screens.gameObject.scene);EditorSceneManager.SaveScene(screens.gameObject.scene);
        }
        [MenuItem("Biology VR/Install Readable Arcade Visuals And Steady Blood")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);
            EditorSceneManager.SaveScene(world.gameObject.scene);if(!File.Exists(Reports+"/Before_Arcade_Visuals.unity"))AssetDatabase.CopyAsset(world.gameObject.scene.path,Reports+"/Before_Arcade_Visuals.unity");
            var shader=Shader.Find("BiologyVR/Arcade Opaque Cell");if(!shader)throw new InvalidOperationException("Opaque cell shader missing");var material=new Material(shader){name="Opaque arcade cell palette"};material.SetColor("_BaseColor",Color.white);JourneyGeometry.Save(material,Folder+"/OpaqueCells.mat");material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/OpaqueCells.mat");
            var oxygen=Shape("Oxygen / two atoms",new[]{new Atom(new Vector3(-.32f,0,0),.30f,new Color(.96f,.18f,.22f)),new Atom(new Vector3(.32f,0,0),.30f,new Color(.96f,.18f,.22f))});
            var carbon=Shape("Carbon dioxide / three atoms",new[]{new Atom(new Vector3(-.48f,0,0),.28f,new Color(.96f,.18f,.22f)),new Atom(Vector3.zero,.25f,new Color(.22f,.26f,.32f)),new Atom(new Vector3(.48f,0,0),.28f,new Color(.96f,.18f,.22f))});
            var phag=Shape("Opaque phagocyte",new[]{new Atom(Vector3.zero,.46f,new Color(.70f,.84f,.76f)),new Atom(new Vector3(-.11f,.02f,-.37f),.14f,new Color(.35f,.23f,.55f)),new Atom(new Vector3(.07f,.08f,-.38f),.13f,new Color(.40f,.25f,.60f)),new Atom(new Vector3(.13f,-.09f,-.35f),.12f,new Color(.35f,.23f,.55f))});
            var fragment=Shape("Viral shell fragment",new[]{new Atom(Vector3.zero,.5f,new Color(.48f,.32f,.78f))},6,4);
            Save(oxygen,"Oxygen");Save(carbon,"CarbonDioxide");Save(phag,"Phagocyte");Save(fragment,"VirusFragment");
            var payload=world.freeTools.payload;payload.gasMesh=Load("Oxygen");payload.oxygenMaterial=payload.co2Material=material;
            // Source CO2 selects its distinct three-atom mesh; the interaction topic matches it.
            payload.co2Mesh=Load("CarbonDioxide");
            foreach(var topic in new[]{payload.oxygen,payload.co2}){var visual=topic.transform.Find("Flow bonded topic");visual.GetComponent<MeshFilter>().sharedMesh=topic==payload.oxygen?payload.gasMesh:payload.co2Mesh;visual.GetComponent<Renderer>().sharedMaterial=material;visual.localScale=Vector3.one*.13f;EditorUtility.SetDirty(topic);}
            var stream=world.bloodEntities.stream;stream.uniformCirculation=true;var species=stream.palette.First(p=>p.id=="phagocyte");species.mesh=Load("Phagocyte");species.materials=new[]{material};
            foreach(var cell in world.immunity.phagocytes){foreach(var limb in cell.pseudopodia)limb.GetComponent<Renderer>().sharedMaterial=material;EditorUtility.SetDirty(cell);}
            if(world.phagocyte){foreach(var renderer in world.phagocyte.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=material;var filter=world.phagocyte.GetComponent<MeshFilter>();if(filter)filter.sharedMesh=Load("Phagocyte");}
            var shatter=world.GetComponent<JourneyVirusShatter>();if(!shatter)shatter=world.gameObject.AddComponent<JourneyVirusShatter>();var old=world.transform.Find("Viral shell fragments");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var parent=new GameObject("Viral shell fragments");parent.transform.SetParent(world.transform,false);shatter.pieces=new Transform[36];for(int i=0;i<36;i++){var go=new GameObject("Pooled viral fragment "+i,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent.transform,false);go.GetComponent<MeshFilter>().sharedMesh=Load("VirusFragment");go.GetComponent<Renderer>().sharedMaterial=material;go.SetActive(false);shatter.pieces[i]=go.transform;}
            world.immunity.shatter=shatter;foreach(var virus in world.immunity.viruses){virus.evasive=true;EditorUtility.SetDirty(virus);}
            var screens=world.mission.GetComponent<JourneyHolographicScreens>();screens.theoryRoot.localPosition=new Vector3(-.43f,.04f,1.25f);screens.theoryRoot.localRotation=Quaternion.LookRotation(screens.theoryRoot.localPosition,Vector3.up);
            var hud=world.GetComponent<JourneyArcadeHud>();if(!hud)hud=world.gameObject.AddComponent<JourneyArcadeHud>();hud.world=world;hud.screens=screens;
            screens.shortTheory.fontSize=29;screens.taskText.fontSize=32;screens.theoryTitle.fontSize=32;foreach(var text in screens.theoryRoot.GetComponentsInChildren<Text>(true)){text.fontStyle=FontStyle.Bold;text.verticalOverflow=VerticalWrapMode.Truncate;}
            foreach(var icon in screens.vitalsRoot.GetComponentsInChildren<Text>(true).Where(t=>t.name=="Small metric icon"))icon.gameObject.SetActive(false);
            screens.vitalsRoot.GetComponent<Canvas>().sortingOrder=screens.theoryRoot.GetComponent<Canvas>().sortingOrder;
            foreach(var metric in screens.vitalValues){var outline=metric.GetComponent<Outline>();if(outline)outline.effectDistance=new Vector2(.25f,-.25f);}
            var label=screens.theoryRoot.GetComponentsInChildren<Text>(true).First(t=>t.name=="Section label");label.text="СЛЕДУЮЩЕЕ ДЕЙСТВИЕ";label.fontSize=21;
            var formulaLabel=screens.theoryRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Illustration label");if(formulaLabel)formulaLabel.text="ПОДСКАЗКА / СОСТОЯНИЕ";
            var footer=screens.theoryRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Footer reading hint");if(footer)footer.text="Подробнее — только по желанию";
            foreach(var obj in new UnityEngine.Object[]{world,payload,stream,world.immunity,shatter,screens,hud})EditorUtility.SetDirty(obj);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,leftPanel=screens.theoryRoot.localPosition.ToString(),gas="smooth 2/3-atom molecules",phagocytes="opaque vertex-colored mesh / no external texture",blood="prefix-balanced 48m local circuit / timed hidden returns",viralMotion="three-axis bounded evasive motion",fragmentPool=36},Formatting.Indented));
        }
        readonly struct Atom{public readonly Vector3 centre;public readonly float radius;public readonly Color color;public Atom(Vector3 c,float r,Color tint){centre=c;radius=r;color=tint;}}
        static Mesh Shape(string name,Atom[] atoms,int segments=12,int rings=8)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            foreach(var atom in atoms){int start=vertices.Count;for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++){float a=x*Mathf.PI*2/segments,b=y*Mathf.PI/rings;var normal=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));vertices.Add(atom.centre+normal*atom.radius);normals.Add(normal);colors.Add(atom.color);}for(int y=0;y<rings;y++)for(int x=0;x<segments;x++){int a=start+y*(segments+1)+x,b=a+segments+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}}
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        static void Save(Mesh mesh,string name)=>JourneyGeometry.Save(mesh,Folder+"/"+name+".asset");
        static Mesh Load(string name)=>AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");
    }
}

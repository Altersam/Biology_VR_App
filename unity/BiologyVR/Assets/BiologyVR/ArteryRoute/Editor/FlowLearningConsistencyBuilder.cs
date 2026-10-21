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
    public static class FlowLearningConsistencyBuilder
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/FlowLearningConsistency";
        public const string Reports=JourneyGeometry.Root+"/Reports/Polish/FlowLearningConsistency";
        [MenuItem("Biology VR/Apply Flow Learning And Controller Consistency")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);
            EditorSceneManager.SaveScene(w.gameObject.scene);if(!File.Exists(Reports+"/Before_Consistency.unity"))AssetDatabase.CopyAsset(w.gameObject.scene.path,Reports+"/Before_Consistency.unity");
            var mesh=Heme();JourneyGeometry.Save(mesh,Folder+"/HemeWithIron.asset");mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/HemeWithIron.asset");
            var payload=w.freeTools.payload;payload.ironMesh=mesh;payload.ironMaterial=payload.oxygenMaterial;
            var iron=payload.heme.transform.Find("Flow bonded topic");iron.GetComponent<MeshFilter>().sharedMesh=mesh;iron.GetComponent<Renderer>().sharedMaterial=payload.ironMaterial;iron.localScale=Vector3.one*.18f;
            int centered=0;
            foreach(var item in w.bloodEntities.stream.palette)
            {
                if(item.mesh.bounds.center.sqrMagnitude<.000001f)continue;
                var copy=UnityEngine.Object.Instantiate(item.mesh);var centre=copy.bounds.center;copy.vertices=copy.vertices.Select(v=>v-centre).ToArray();copy.RecalculateBounds();copy.name="Centred "+item.id;
                string path=Folder+"/Centred_"+item.id+".asset";JourneyGeometry.Save(copy,path);item.mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);centered++;
            }
            var screens=w.mission.GetComponent<JourneyHolographicScreens>();
            screens.theoryRoot.localPosition=new Vector3(-.86f,-.08f,1.6f);screens.vitalsRoot.localPosition=new Vector3(.95f,.38f,1.6f);
            foreach(var panel in new[]{screens.theoryRoot,screens.vitalsRoot})panel.localRotation=Quaternion.LookRotation(panel.localPosition,Vector3.up);
            var task=screens.taskText.transform.parent as RectTransform;task.SetParent(screens.theoryRoot,false);task.anchorMin=task.anchorMax=new Vector2(0,1);task.pivot=new Vector2(0,1);task.anchoredPosition=new Vector2(28,-506);task.sizeDelta=new Vector2(584,192);screens.taskRoot=task;
            var viewport=screens.theoryScroll.viewport;viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=new Vector2(28,320);viewport.offsetMax=new Vector2(-44,-174);
            if(screens.theoryScroll.verticalScrollbar){var bar=screens.theoryScroll.verticalScrollbar.transform as RectTransform;bar.offsetMin=new Vector2(bar.offsetMin.x,320);bar.offsetMax=new Vector2(bar.offsetMax.x,-174);EditorUtility.SetDirty(bar);}
            screens.theoryText.fontSize=26;screens.theoryText.horizontalOverflow=HorizontalWrapMode.Wrap;screens.theoryText.verticalOverflow=VerticalWrapMode.Overflow;screens.theoryText.supportRichText=true;
            screens.taskText.fontSize=24;screens.taskHint.fontSize=18;screens.shortTheory.fontSize=26;
            foreach(var label in screens.theoryRoot.GetComponentsInChildren<Text>(true))
            {if(label.name=="Section label")label.text="УЧЕБНАЯ ИНФОРМАЦИЯ";if(label.name=="Footer reading hint")label.text="Сканирование открывает теорию · колесо / правый стик";EditorUtility.SetDirty(label);}
            var rig=w.mission.GetComponent<JourneyInstrumentRig>();
            foreach(var visual in new[]{rig.scannerVR,rig.bioVR}){visual.transform.localRotation=Quaternion.identity;PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);}
            foreach(var obj in new UnityEngine.Object[]{payload,w.bloodEntities.stream,iron.GetComponent<MeshFilter>(),iron.GetComponent<Renderer>(),screens,screens.theoryRoot,screens.vitalsRoot,task,viewport})EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,centeredMeshes=centered,hemeVertices=mesh.vertexCount,fullTheoryAfterScan=true,pinnedTask=true,uniformScreenPlacement=true,controllerAim="independent straight rays; one capture tether only",existingPool=w.bloodEntities.stream.flow.cells.Length},Formatting.Indented));
        }
        static Mesh Heme()
        {
            var v=new List<Vector3>();var n=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
            void Sphere(Vector3 centre,float radius,Color color)
            {int start=v.Count;const int rows=8,cols=12;for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){float a=x*Mathf.PI*2/cols,b=y*Mathf.PI/rows;var normal=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));v.Add(centre+normal*radius);n.Add(normal);c.Add(color);}for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){int a=start+y*(cols+1)+x,b=a+cols+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}}
            for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;Sphere(new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.38f,.055f,new Color(.67f,.25f,.24f));}
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Sphere(new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.22f,.065f,new Color(.35f,.56f,.76f));}
            Sphere(Vector3.zero,.13f,new Color(1,.72f,.22f));
            var mesh=new Mesh{name="Heme porphyrin ring with central Fe2 ion"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateBounds();return mesh;
        }
    }
}

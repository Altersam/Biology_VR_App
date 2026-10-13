using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class PlayerFeedbackReworkBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/PlayerFeedbackRework";
        const string Reports=JourneyGeometry.Root+"/Reports/Polish/PlayerFeedbackRework";
        [MenuItem("Biology VR/Install Player Feedback Gameplay Rework")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);
            EditorSceneManager.SaveScene(w.gameObject.scene);if(!File.Exists(Reports+"/Before_Feedback_Rework.unity"))AssetDatabase.CopyAsset(w.gameObject.scene.path,Reports+"/Before_Feedback_Rework.unity");
            var tool=w.GetComponent<JourneyFreeBioTool>();if(!tool)tool=w.gameObject.AddComponent<JourneyFreeBioTool>();tool.world=w;w.freeTools=tool;
            var payload=w.GetComponent<JourneyBloodPayload>();if(!payload)payload=w.gameObject.AddComponent<JourneyBloodPayload>();payload.world=w;tool.payload=payload;
            var mesh=Octa();JourneyGeometry.Save(mesh,Folder+"/LightCargo.asset");mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/LightCargo.asset");
            payload.gasMesh=payload.ironMesh=mesh;payload.oxygenMaterial=Material("Bound O2",new Color(.15f,.70f,1));payload.co2Material=Material("Bound CO2",new Color(.5f,.23f,.18f));payload.ironMaterial=Material("Bound iron",new Color(1,.69f,.12f));
            var stream=w.bloodEntities.stream;
            var palette=stream.palette.Where(p=>p.id!="erythrocyte-O2"&&p.id!="erythrocyte-CO2"&&p.id!="hemoglobin").ToList();
            var rbc=palette.First(p=>p.id=="erythrocyte");rbc.weight=24;
            foreach(var id in new[]{"erythrocyte-O2","erythrocyte-CO2"})palette.Add(new JourneyContinuousBloodstream.VisualSpecies{id=id,mesh=rbc.mesh,materials=rbc.materials,weight=22,minDiameter=rbc.minDiameter,maxDiameter=rbc.maxDiameter});
            palette.First(p=>p.id=="phagocyte").weight=9;
            var hb=w.Find("hemoglobin");var hbFilter=hb.GetComponentsInChildren<MeshFilter>(true).First();
            var hbMesh=UnityEngine.Object.Instantiate(hbFilter.sharedMesh);var bounds=hbMesh.bounds;float extent=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));hbMesh.vertices=hbMesh.vertices.Select(v=>(v-bounds.center)/extent).ToArray();hbMesh.RecalculateBounds();JourneyGeometry.Save(hbMesh,Folder+"/FlowHb.asset");
            palette.Add(new JourneyContinuousBloodstream.VisualSpecies{id="hemoglobin",mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/FlowHb.asset"),materials=hbFilter.GetComponent<Renderer>().sharedMaterials,weight=4,minDiameter=.25f,maxDiameter=.33f});stream.palette=palette.ToArray();
            var hbSlot=w.bloodEntities.slots.FirstOrDefault(s=>s.type=="hemoglobin");
            if(hbSlot==null)
            {
                var old=hb.transform.Find("Flow Hb body");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var renderer=Visual(hb.transform,"Flow Hb body",palette.Last().mesh,palette.Last().materials[0]);
                var grab=hb.GetComponent<XRGrabInteractable>();var contact=hb.GetComponent<SphereCollider>();
                hbSlot=new JourneyBloodInteractiveSlot{type="hemoglobin",target=hb,body=renderer.GetComponent<MeshFilter>(),contact=contact,grab=grab,originalScale=hb.transform.localScale,legacyRenderers=hb.GetComponentsInChildren<Renderer>(true).Where(r=>r!=renderer).ToArray()};hbSlot.legacyVisibility=hbSlot.legacyRenderers.Select(r=>r.enabled).ToArray();
                w.bloodEntities.slots=w.bloodEntities.slots.Append(hbSlot).ToArray();
            }
            foreach(var slot in w.bloodEntities.slots){slot.grab.interactionLayers=1<<28;EditorUtility.SetDirty(slot.grab);}
            foreach(var grab in w.GetComponentsInChildren<XRGrabInteractable>(true)){if(!w.bloodEntities.slots.Any(s=>s.grab==grab))grab.interactionLayers=grab.GetComponent<JourneyTarget>()?1<<28:grab.interactionLayers.value|(1<<28);EditorUtility.SetDirty(grab);}
            var oldCargo=w.transform.Find("Flow bound cargo");if(oldCargo)UnityEngine.Object.DestroyImmediate(oldCargo.gameObject);
            var cargo=new GameObject("Flow bound cargo");cargo.transform.SetParent(w.transform,false);payload.backgroundCargo=new Transform[stream.flow.cells.Length];
            for(int i=0;i<payload.backgroundCargo.Length;i++){var previous=stream.flow.cells[i].Find("Bound transport cargo");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);var marker=Visual(stream.flow.cells[i],"Bound transport cargo",mesh,payload.oxygenMaterial);marker.transform.localScale=Vector3.one*.2f;marker.gameObject.SetActive(false);payload.backgroundCargo[i]=marker.transform;}
            payload.oxygen=w.Find("oxygen");payload.co2=w.Find("co2");payload.heme=w.Find("heme");
            foreach(var target in new[]{payload.oxygen,payload.co2,payload.heme})
            {
                var old=target.transform.Find("Flow bonded topic");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach(var surface in target.GetComponentsInChildren<Renderer>(true))surface.enabled=false;
                var renderer=Visual(target.transform,"Flow bonded topic",mesh,target==payload.oxygen?payload.oxygenMaterial:target==payload.co2?payload.co2Material:payload.ironMaterial);renderer.transform.localScale=Vector3.one*.12f;
                target.transform.localScale=Vector3.one;var contact=target.GetComponent<SphereCollider>();contact.center=Vector3.zero;contact.radius=.09f;
                var bond=target.GetComponent<JourneyFlowBond>();if(!bond)bond=target.gameObject.AddComponent<JourneyFlowBond>();
                var grab=target.GetComponent<XRGrabInteractable>();if(grab)grab.enabled=false;
                var oldFollow=target.GetComponent<JourneyBloodCargo>();if(oldFollow)oldFollow.enabled=false;target.gameObject.SetActive(false);
            }
            w.analysis.Close();w.analysis.enabled=false;
            foreach(var probe in w.hemodynamics.probes)probe.transform.SetParent(w.transform,true);w.hemodynamics.toneContact.transform.SetParent(w.transform,true);
            foreach(var surface in w.hemodynamics.probes.SelectMany(p=>p.GetComponentsInChildren<Renderer>(true)).Concat(w.hemodynamics.toneContact.GetComponentsInChildren<Renderer>(true)))surface.enabled=false;
            foreach(var grab in w.embolusWave.targets.Select(t=>t.GetComponent<XRGrabInteractable>()).Where(g=>g))grab.enabled=false;
            var screens=w.mission.GetComponent<JourneyHolographicScreens>();
            screens.theoryRoot.localPosition=new Vector3(-.57f,.06f,1.35f);screens.vitalsRoot.localPosition=new Vector3(.62f,.32f,1.38f);
            screens.theoryRoot.localRotation=Quaternion.LookRotation(screens.theoryRoot.localPosition,Vector3.up);screens.vitalsRoot.localRotation=Quaternion.LookRotation(screens.vitalsRoot.localPosition,Vector3.up);
            foreach(var panel in new[]{screens.theoryRoot,screens.vitalsRoot})
            {
                var scale=panel.GetComponent<CanvasScaler>();scale.dynamicPixelsPerUnit=5;
                foreach(var text in panel.GetComponentsInChildren<Text>(true)){text.fontStyle=FontStyle.Bold;text.resizeTextForBestFit=false;text.color=new Color(.94f,1,1,1);text.fontSize=Mathf.Max(text.fontSize,22);text.verticalOverflow=VerticalWrapMode.Overflow;var outline=text.GetComponent<Outline>();if(!outline)outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.015f,.035f,.05f,.85f);outline.effectDistance=new Vector2(.6f,-.6f);}
                foreach(var graphic in panel.GetComponentsInChildren<JourneyRoundedGraphic>(true))if(graphic.name.Contains("glass"))graphic.color=new Color(.012f,.045f,.07f,.86f);
            }
            screens.shortTheory.fontSize=30;screens.taskText.fontSize=29;screens.taskHint.fontSize=24;
            screens.theoryTitle.fontSize=32;screens.theoryTitle.rectTransform.offsetMin=new Vector2(28,-132);screens.theoryTitle.rectTransform.offsetMax=new Vector2(-28,-50);
            screens.pageLabel.rectTransform.offsetMin=new Vector2(28,-163);screens.pageLabel.rectTransform.offsetMax=new Vector2(-28,-136);
            screens.illustrationText.fontSize=26;screens.illustrationText.rectTransform.offsetMin=new Vector2(18,-112);screens.illustrationText.rectTransform.offsetMax=new Vector2(-18,-42);
            screens.monitorStatus.rectTransform.offsetMin=new Vector2(370,-64);screens.monitorStatus.rectTransform.offsetMax=new Vector2(-20,-14);
            foreach(var value in screens.vitalValues)value.fontSize=48;
            var telemetry=w.GetComponent<JourneyLiveTelemetry>();if(!telemetry)telemetry=w.gameObject.AddComponent<JourneyLiveTelemetry>();telemetry.world=w;telemetry.screens=screens;
            var prior=screens.vitalsRoot.Find("Context assistance");if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);
            var go=new GameObject("Context assistance",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(screens.vitalsRoot,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-56);rect.sizeDelta=new Vector2(720,140);
            var help=go.GetComponent<Text>();help.font=screens.taskText.font;help.fontSize=28;help.fontStyle=FontStyle.Bold;help.color=new Color(1,.85f,.45f);help.alignment=TextAnchor.UpperCenter;help.raycastTarget=false;help.material=screens.taskText.material;telemetry.assistance=help;
            var oldMode=screens.vitalsRoot.Find("Current BioTool mode");if(oldMode)UnityEngine.Object.DestroyImmediate(oldMode.gameObject);
            var modeGo=new GameObject("Current BioTool mode",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));modeGo.transform.SetParent(screens.vitalsRoot,false);var modeRect=modeGo.GetComponent<RectTransform>();modeRect.anchorMin=modeRect.anchorMax=new Vector2(.5f,0);modeRect.anchoredPosition=new Vector2(0,-24);modeRect.sizeDelta=new Vector2(700,38);var modeText=modeGo.GetComponent<Text>();modeText.font=help.font;modeText.fontSize=26;modeText.fontStyle=FontStyle.Bold;modeText.color=new Color(.5f,1,1);modeText.alignment=TextAnchor.MiddleCenter;modeText.material=help.material;modeText.raycastTarget=false;telemetry.modeLabel=modeText;
            var oldAlarm=screens.vitalsRoot.Find("Critical alarm wash");if(oldAlarm)UnityEngine.Object.DestroyImmediate(oldAlarm.gameObject);var alarmGo=new GameObject("Critical alarm wash",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));alarmGo.transform.SetParent(screens.vitalsRoot,false);alarmGo.transform.SetAsFirstSibling();var alarmRect=alarmGo.GetComponent<RectTransform>();alarmRect.anchorMin=Vector2.zero;alarmRect.anchorMax=Vector2.one;alarmRect.offsetMin=alarmRect.offsetMax=Vector2.zero;var alarmImage=alarmGo.GetComponent<Image>();alarmImage.raycastTarget=false;alarmImage.material=help.material;telemetry.alarm=alarmImage;
            var recovery=w.transform.Find("Recovery helper phagocyte");if(!recovery){var agent=UnityEngine.Object.Instantiate(w.bloodEntities.slots.First(s=>s.type=="phagocyte").legacyRenderers[0].gameObject,w.transform);agent.name="Recovery helper phagocyte";agent.transform.localScale=Vector3.one*.24f;recovery=agent.transform;agent.SetActive(false);}w.phagocyte=recovery;
            EditorUtility.SetDirty(w);EditorUtility.SetDirty(tool);EditorUtility.SetDirty(payload);EditorUtility.SetDirty(stream);EditorUtility.SetDirty(w.bloodEntities);EditorUtility.SetDirty(w.analysis);EditorUtility.SetDirty(w.hemodynamics);EditorUtility.SetDirty(telemetry);EditorUtility.SetDirty(screens);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,background=360,proxies=w.bloodEntities.slots.Length,gasBoundToRbc=true,ironBoundToHb=true,gripForFlowDisabled=true,globalModes=5,screenRaster=5,helpAfterSeconds=60},Formatting.Indented));
        }
        static Material Material(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.name=name;m.color=color;JourneyGeometry.Save(m,Folder+"/"+name.Replace(" ","_")+".mat");return AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name.Replace(" ","_")+".mat");}
        static Mesh Octa(){var m=new Mesh{name="Light bonded cargo"};m.vertices=new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back};m.triangles=new[]{0,4,3,0,3,5,0,5,2,0,2,4,1,3,4,1,5,3,1,2,5,1,4,2};m.RecalculateNormals();m.RecalculateBounds();return m;}
        static Renderer Visual(Transform parent,string name,Mesh mesh,Material material){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return r;}
    }
}

using System;
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
    public static class FuturisticHolographicScreensBuilder
    {
        const string Reports=JourneyGeometry.Root+"/Reports/Polish/FuturisticHolograms";
        [MenuItem("Biology VR/Polish Futuristic Holograms And VR Typography")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var screens=UnityEngine.Object.FindFirstObjectByType<JourneyHolographicScreens>();if(!screens||!screens.theoryRoot||!screens.vitalsRoot)throw new InvalidOperationException("Existing clean screens required");
            Directory.CreateDirectory(Reports);EditorSceneManager.SaveScene(screens.gameObject.scene);
            string backup=Reports+"/Before_Futuristic_Holograms.unity";if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(screens.gameObject.scene.path,backup))throw new IOException("Screen backup failed");
            screens.theoryRoot.localPosition=new Vector3(-.56f,.025f,1.78f);
            screens.vitalsRoot.localPosition=new Vector3(.67f,.48f,1.80f);
            screens.theoryRoot.localRotation=Quaternion.Euler(0,7,0);screens.vitalsRoot.localRotation=Quaternion.Euler(0,-10,0);
            foreach(var panel in new[]{screens.theoryRoot,screens.vitalsRoot})
            {
                var scaler=panel.GetComponent<CanvasScaler>();if(!scaler)scaler=panel.gameObject.AddComponent<CanvasScaler>();scaler.dynamicPixelsPerUnit=2.5f;scaler.referencePixelsPerUnit=100;EditorUtility.SetDirty(scaler);
                foreach(var text in panel.GetComponentsInChildren<Text>(true)){text.fontStyle=FontStyle.Bold;text.color=text.color.a<.01f?text.color:new Color(text.color.r,text.color.g,text.color.b,1);EditorUtility.SetDirty(text);}
                foreach(var graphic in panel.GetComponentsInChildren<JourneyRoundedGraphic>(true))
                {
                    if(graphic.name=="Dark transparent glass")graphic.color=new Color(.018f,.065f,.11f,.38f);
                    else if(graphic.name=="Thin emissive cyan outline"){graphic.color=new Color(.35f,.9f,1,.36f);graphic.borderWidth=1.2f;graphic.radius=8;}
                    else if(graphic.name=="Soft edge halo"){graphic.color=new Color(.30f,.85f,1,.10f);graphic.borderWidth=4;graphic.radius=10;}
                    else if(graphic.name=="Content glass")graphic.color=new Color(.022f,.10f,.15f,graphic.transform.parent.name=="Task slot"?.32f:.20f);
                    else if(graphic.name=="Quiet content edge"){graphic.color=new Color(.32f,.88f,1,.34f);graphic.radius=7;}
                    EditorUtility.SetDirty(graphic);
                }
                var old=panel.Find("Projection frame brackets");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var go=new GameObject("Projection frame brackets",typeof(RectTransform),typeof(CanvasRenderer),typeof(JourneyHolographicFrameGraphic));go.transform.SetParent(panel,false);var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var frame=go.GetComponent<JourneyHolographicFrameGraphic>();frame.color=new Color(.38f,.92f,1,.9f);frame.raycastTarget=false;frame.material=panel.GetComponentInChildren<Graphic>(true).material;
                EditorUtility.SetDirty(panel);
            }
            var footer=screens.theoryRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Footer reading hint");if(footer)footer.fontSize=19;
            screens.taskHint.fontSize=21;screens.pageLabel.fontSize=19;
            if(screens.monitorStatus){screens.monitorStatus.text="●  BIO / TELEMETRY";screens.monitorStatus.fontSize=18;}
            SaveTemplate(screens.theoryRoot,"CleanTheoryContainer");SaveTemplate(screens.vitalsRoot,"CleanVitalsContainer");
            EditorUtility.SetDirty(screens);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(screens.gameObject.scene);EditorSceneManager.SaveScene(screens.gameObject.scene);
            var theory=screens.theoryRoot.localPosition;var vitals=screens.vitalsRoot.localPosition;
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,style="Futuristic cyan projection rails, bevelled brackets and dark blue holographic glass",textWeight="Inter Bold",fontRasterScale=2.5f,theoryPosition=new[]{theory.x,theory.y,theory.z},vitalsPosition=new[]{vitals.x,vitals.y,vitals.z},raycastsAndCallbacksPreserved=true,heavyEffectsAdded=false,hardwareVerified=false},Formatting.Indented));
        }
        static void SaveTemplate(RectTransform panel,string name)
        {
            var clone=UnityEngine.Object.Instantiate(panel.gameObject);clone.name=name;clone.transform.SetParent(null,false);clone.GetComponent<Canvas>().worldCamera=null;
            foreach(var button in clone.GetComponentsInChildren<Button>(true))button.onClick=new Button.ButtonClickedEvent();
            PrefabUtility.SaveAsPrefabAsset(clone,JourneyGeometry.Root+"/Generated/HolographicScreens/"+name+".prefab");UnityEngine.Object.DestroyImmediate(clone);
        }
    }
}

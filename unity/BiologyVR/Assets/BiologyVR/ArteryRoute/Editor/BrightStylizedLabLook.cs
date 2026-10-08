using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class BrightStylizedLabLook
    {
        [MenuItem("Biology VR/Apply Bright Stylized Retro-Futuristic Lab Look")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            var scene=SceneManager.GetActiveScene();
            RenderSettings.skybox=null;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.72f,.78f,.68f);
            RenderSettings.ambientIntensity=1.10f;
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.72f,.88f,.78f);
            RenderSettings.fogDensity=.006f;
            int materialCount=0,lightCount=0,uiCount=0;
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach(var material in renderer.sharedMaterials)
                    if(material&&ApplyMaterial(material))materialCount++;
                foreach(var image in root.GetComponentsInChildren<Image>(true))
                {
                    if(image.name=="Background")image.color=new Color(.035f,.22f,.22f,.94f);
                    else if(image.GetComponent<Button>())image.color=new Color(.10f,.62f,.52f,.98f);
                    uiCount++;
                }
                foreach(var text in root.GetComponentsInChildren<Text>(true))text.color=new Color(1f,.93f,.70f,1);
                foreach(var light in root.GetComponentsInChildren<Light>(true))
                {
                    light.shadows=LightShadows.None;
                    bool cool=light.name.IndexOf("blue",StringComparison.OrdinalIgnoreCase)>=0||light.name.IndexOf("rim",StringComparison.OrdinalIgnoreCase)>=0||light.name.IndexOf("cool",StringComparison.OrdinalIgnoreCase)>=0;
                    light.color=cool?new Color(.20f,.88f,.90f):new Color(1f,.55f,.20f);
                    light.intensity=cool?1.4f:1.7f;lightCount++;
                }
                foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.backgroundColor=new Color(.72f,.84f,.72f);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log($"Bright stylized lab look applied: materials={materialCount}, lights={lightCount}, ui={uiCount}");
        }
        static bool ApplyMaterial(Material material)
        {
            string shader=material.shader?material.shader.name:"";
            if(shader=="BiologyVR/Cellular Vessel")
            {
                if(material.HasProperty("_StyleTint"))material.SetColor("_StyleTint",new Color(.92f,.22f,.12f,1));
                if(material.HasProperty("_StyleAccent"))material.SetColor("_StyleAccent",new Color(.05f,.90f,.78f,1));
                if(material.HasProperty("_Wetness"))material.SetFloat("_Wetness",.30f);
                return true;
            }
            if(shader=="BiologyVR/Hybrid Artery")
            {
                material.SetColor("_BaseColor",new Color(1f,.42f,.18f,1));
                if(material.HasProperty("_StyleAccent"))material.SetColor("_StyleAccent",new Color(.05f,.90f,.78f,1));
                material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.20f);material.SetFloat("_CellSeams",.75f);
                return true;
            }
            if(shader=="BiologyVR/Biological Surface")
            {
                Color color=new Color(1f,.45f,.30f,1),accent=new Color(.05f,.90f,.78f,1);
                string n=material.name;
                if(Has(n,"RBC"))color=new Color(.82f,.055f,.025f,1);
                else if(Has(n,"CellPink"))color=new Color(1f,.30f,.22f,1);
                else if(Has(n,"Nucleus","Violet","Ink"))color=new Color(.48f,.28f,.85f,1);
                else if(Has(n,"Fibrin","Collagen","Lipid"))color=Has(n,"Lipid")?new Color(1f,.58f,.12f,1):new Color(.72f,.92f,.26f,1);
                else if(Has(n,"Cyan","Mint","Blue")){color=new Color(.12f,.78f,.82f,1);accent=new Color(.86f,1f,.35f,1);}
                else if(Has(n,"Pearl","Cream","White"))color=new Color(1f,.92f,.67f,1);
                else if(Has(n,"Orange"))color=new Color(1f,.52f,.14f,1);
                material.SetColor("_BaseColor",color);material.SetColor("_StyleAccent",accent);material.SetFloat("_Smoothness",.24f);material.SetFloat("_Transmission",.08f);
                return true;
            }
            return false;
        }
        static bool Has(string text,params string[] values)
        {
            foreach(var value in values)if(text.IndexOf(value,StringComparison.OrdinalIgnoreCase)>=0)return true;
            return false;
        }
    }
}

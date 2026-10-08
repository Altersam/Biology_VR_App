using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryFinalFocusBuilder
    {
        public static JourneyFinalFocus Apply(JourneyWorld world)
        {
            var target=world.targets.First(t=>t.targetId=="infected-cell");
            foreach(var r in target.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            foreach(Transform child in target.transform.Cast<Transform>().ToArray())if(child.name.StartsWith("Final focus"))UnityEngine.Object.DestroyImmediate(child.gameObject);
            var root=new GameObject("Final focus — infected cell composite");root.transform.SetParent(target.transform,false);root.transform.localPosition=Vector3.zero;root.transform.localRotation=Quaternion.identity;root.transform.localScale=Vector3.one;
            var membrane=Sphere(root.transform,"Final focus infected membrane",new Color(.93f,.34f,.44f),new Vector3(.92f,.72f,.82f));
            var nucleus=Sphere(root.transform,"Final focus nucleus",new Color(.47f,.22f,.72f),Vector3.one*.44f);nucleus.localPosition=new Vector3(0,0,.10f);
            var virus=AssetDatabase.LoadAssetAtPath<Mesh>(ArteryScenePolish.Folder+"/Normalized_POLISH_Virus_LOD2.asset");
            var material=Lit("FinalFocus_Virion",new Color(.57f,.19f,.72f),.26f);
            var buds=new Renderer[8];for(int i=0;i<buds.Length;i++)buds[i]=Model(root.transform,"Final focus budding virion "+i,virus,material,.16f);
            var particles=new Renderer[9];for(int i=0;i<particles.Length;i++)particles[i]=Model(root.transform,"Final focus internal virion "+i,virus,material,.07f);
            var focus=root.AddComponent<JourneyFinalFocus>();focus.GetType().GetField("mission",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(focus,world.mission);
            focus.GetType().GetField("membrane",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(focus,membrane.GetComponent<Renderer>());
            focus.GetType().GetField("nucleus",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(focus,nucleus.GetComponent<Renderer>());
            focus.GetType().GetField("buds",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(focus,buds);
            focus.GetType().GetField("particles",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(focus,particles);
            return focus;
        }
        static Transform Sphere(Transform parent,string name,Color color,Vector3 scale)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);go.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=Lit(name,color,.36f);return go.transform;}
        static Renderer Model(Transform parent,string name,Mesh mesh,Material material,float scale)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*scale;var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return r;}
        static Material Lit(string name,Color color,float smooth)
        {var shader=Shader.Find("Universal Render Pipeline/Lit");var m=new Material(shader){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);m.SetColor("_EmissionColor",Color.black);ArteryScenePolish.SavePublic(m,name+".mat");return AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/"+name+".mat");}
    }
}

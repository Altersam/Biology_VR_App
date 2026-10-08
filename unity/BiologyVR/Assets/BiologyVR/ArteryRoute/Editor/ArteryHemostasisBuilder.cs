using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryHemostasisBuilder
    {
        public static void Apply(JourneyWorld w,Transform controller)
        {
            var path=w.mover.path;float s=path.Anchor(11)+5;
            Quaternion frame=Quaternion.LookRotation(-path.Right(s),path.Forward(s));
            foreach(var surface in w.clot.GetComponentsInChildren<Renderer>(true))surface.enabled=false;
            foreach(var surface in w.fibrin.GetComponentsInChildren<Renderer>(true))surface.enabled=false;
            w.clot.SetPositionAndRotation(w.woundSite.position-path.Right(s)*.22f,frame);w.clot.localScale=Vector3.one;
            w.fibrin.transform.SetPositionAndRotation(w.woundSite.position-path.Right(s)*.32f,frame);w.fibrin.transform.localScale=Vector3.one;
            var violet=ArteryEpisodeViewsBuilder.Lit("ActivatedPlatelet_SoftViolet",new Color(.74f,.54f,.82f),.36f);
            var granules=AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Amber_Lipid.mat");
            var resting=AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Peach_Platelet.mat");
            w.art.restingPlateletMaterials=new[]{resting,granules};w.art.activatedPlateletMaterials=new[]{violet,granules};
            var instances=new[]{new List<CombineInstance>(),new List<CombineInstance>()};
            var mesh=w.art.activatedPlateletMesh;
            float extent=Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
            for(int i=0;i<15;i++)
            {
                float a=i*2.399963f,r=.12f+.57f*Mathf.Sqrt(i/14f);
                var point=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r*1.20f,.03f+.08f*Mathf.Sin(i*2.1f));
                float size=.29f+(i%3)*.027f;
                Quaternion q=Quaternion.Euler(75+(i%3)*11,i*41,i*23);
                var matrix=Matrix4x4.TRS(point,q,Vector3.one*(size/extent))*Matrix4x4.Translate(-mesh.bounds.center);
                for(int sub=0;sub<2;sub++)instances[sub].Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,transform=matrix});
            }
            var plug=new Renderer[2];for(int sub=0;sub<2;sub++)plug[sub]=Combined(w.clot,"Teaching platelet plug "+sub,instances[sub],sub==0?violet:granules);
            var library=AssetDatabase.LoadAssetAtPath<GameObject>(BuildArteryVrScene.Root+"/Imported/Visual/HybridArteryVisual.prefab");
            var strand=library.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="AB_S12_Fibrin_Vertical_0").sharedMesh;
            var net=Network(strand);string netPath=ArteryScenePolish.Folder+"/Teaching_Fibrin_Network.asset";JourneyGeometry.Save(net,netPath);
            var ivory=ArteryEpisodeViewsBuilder.Lit("Fibrin_PearlIvory",new Color(.97f,.90f,.85f),.31f);
            var go=new GameObject("Teaching fibrin stabilisation network");go.transform.SetParent(w.fibrin.transform,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(netPath);
            var network=go.AddComponent<MeshRenderer>();network.sharedMaterial=ivory;network.shadowCastingMode=ShadowCastingMode.Off;network.receiveShadows=false;
            var label=ArteryAnnotations.Label(controller,"Hemostasis phase label","Гемостаз");
            var view=controller.gameObject.AddComponent<JourneyHemostasisView>();view.Configure(w,plug,network,label);
        }
        static Renderer Combined(Transform parent,string name,List<CombineInstance> source,Material material)
        {
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(source.ToArray(),true,true);mesh.RecalculateBounds();
            string path=ArteryScenePolish.Folder+"/"+name.Replace(" ","_")+".asset";JourneyGeometry.Save(mesh,path);
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return r;
        }
        static Mesh Network(Mesh source)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var faces=new List<int>();var original=source.vertices;var size=source.bounds.size;var centre=source.bounds.center;
            int length=size.x>size.y?(size.x>size.z?0:2):(size.y>size.z?1:2),cross=(length+1)%3,depth=(length+2)%3;
            for(int strand=0;strand<26;strand++)
            {
                int first=vertices.Count;float angle=strand<13?.18f:1.68f;float across=(strand%13-6)*.12f;
                var tangent=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);var lateral=new Vector3(-tangent.y,tangent.x,0);
                foreach(var vertex in original)
                {
                    var d=vertex-centre;float t=d[length]/Mathf.Max(.001f,size[length]);
                    float normal=d[cross]/Mathf.Max(.001f,size[cross]),z=d[depth]/Mathf.Max(.001f,size[depth]);
                    float reach=Mathf.Sqrt(Mathf.Max(.05f,1-across*across/.85f));
                    var p=tangent*(t*1.88f*reach)+lateral*(across+normal*.027f+.05f*Mathf.Sin(t*7+strand));
                    p.z=.055f+.18f*Mathf.Max(0,1-p.x*p.x-p.y*p.y)+z*.024f;
                    vertices.Add(p);uv.Add(new Vector2(t+.5f,strand/26f));
                }
                foreach(int index in source.triangles)faces.Add(first+index);
            }
            var mesh=new Mesh{name="Curved fibrin mesh network",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}

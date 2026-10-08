using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    // Scene-local material variants: the imported asset library remains reusable.
    public static class BiologicalLookDev
    {
        const string Folder=JourneyGeometry.Root+"/Generated/LookDev";
        public static void Apply(GameObject[] roots)
        {
            ArteryGlbImporter.Folder(Folder);
            var shader=Shader.Find("BiologyVR/Biological Surface");
            if(!shader)throw new InvalidOperationException("Biological Surface shader missing");
            var detail=Microtexture();
            var cache=new Dictionary<Material,Material>();
            foreach(var root in roots.Where(r=>r))foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    var source=slots[i];if(!source||source.shader.name!="BiologyVR/Hybrid Artery")continue;
                    if(!cache.TryGetValue(source,out var replacement))
                    {
                        replacement=new Material(shader){name=source.name+"_Membrane",enableInstancing=true};
                        var color=source.GetColor("_BaseColor");color.a=1;
                        bool red=source.name.Contains("RBC"),fibres=source.name.Contains("Fibrin"),lipid=source.name.Contains("Lipid"),pearl=source.name.Contains("Pearl");
                        replacement.SetColor("_BaseColor",color);replacement.SetTexture("_DetailMap",detail);
                        replacement.SetFloat("_Smoothness",red?.58f:fibres?.34f:lipid?.5f:pearl?.42f:.46f);
                        replacement.SetFloat("_DetailScale",fibres?9:pearl?6:4);
                        replacement.SetFloat("_DetailStrength",red?.0018f:fibres?.0028f:pearl?.0016f:.0012f);
                        replacement.SetFloat("_Transmission",red?.16f:pearl?.20f:.08f);
                        string path=Folder+"/"+replacement.name+".mat";JourneyGeometry.Save(replacement,path);replacement=AssetDatabase.LoadAssetAtPath<Material>(path);cache[source]=replacement;
                    }
                    slots[i]=replacement;
                }
                renderer.sharedMaterials=slots;
            }
        }
        [MenuItem("Biology VR/Refresh Narrative Surface Detail")]
        public static void RefreshNarrativeSurfaceDetail()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            const string scenePath=JourneyGeometry.Root+"/Scenes/ArteryNarrativeVR.unity";
            var active=SceneManager.GetActiveScene();
            if(active.path!=scenePath)EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
            var detail=Microtexture();int changed=0;
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach(var material in renderer.sharedMaterials)
                {
                    if(!material||material.shader==null||material.shader.name!="BiologyVR/Biological Surface")continue;
                    material.SetTexture("_DetailMap",detail);
                    bool red=material.name.IndexOf("RBC",StringComparison.OrdinalIgnoreCase)>=0;
                    bool fibres=material.name.IndexOf("Fibrin",StringComparison.OrdinalIgnoreCase)>=0||material.name.IndexOf("Collagen",StringComparison.OrdinalIgnoreCase)>=0;
                    bool pearl=material.name.IndexOf("Pearl",StringComparison.OrdinalIgnoreCase)>=0;
                    material.SetFloat("_DetailStrength",red?.0018f:fibres?.0028f:pearl?.0016f:.0012f);
                    EditorUtility.SetDirty(material);changed++;
                }
            }
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log($"Narrative surface detail refreshed: {changed} materials");
        }
        static Texture2D Microtexture()
        {
            const int size=1024;var tex=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name="MembraneMicrotexture",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            var data=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                float broad=PeriodicNoise(u,v,7),meso=PeriodicNoise(u,v,21),micro=PeriodicNoise(u,v,67);
                float lipid=PeriodicNoise(u+.17f,v-.11f,13);
                float elongated=.5f+.5f*Mathf.Sin((u*26f+Mathf.Sin(v*8f*2f*Mathf.PI)*.34f)*2f*Mathf.PI);
                float ridge=1f-Mathf.Abs(elongated-.5f)*2f;
                float pores=Mathf.SmoothStep(.32f,.78f,meso)*(.5f+.5f*micro);
                float height=.46f+broad*.10f+meso*.055f+micro*.025f+ridge*.018f-pores*.018f;
                float rough=.34f+micro*.22f+(1f-ridge)*.14f+lipid*.06f;
                data[y*size+x]=new Color(Mathf.Clamp01(height),Mathf.Clamp01(rough),Mathf.Clamp01(.35f+ridge*.55f),1);
            }
            tex.SetPixels32(data);tex.Apply();string path=Folder+"/MembraneMicrotexture.asset";JourneyGeometry.Save(tex,path);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static float PeriodicNoise(float u,float v,int period)
        {
            float x=u*period,y=v*period;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float tx=x-ix,ty=y-iy;tx=tx*tx*(3-2*tx);ty=ty*ty*(3-2*ty);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix,iy,period),Hash(ix+1,iy,period),tx),Mathf.Lerp(Hash(ix,iy+1,period),Hash(ix+1,iy+1,period),tx),ty);
        }
        static float Hash(int x,int y,int p){unchecked{uint h=(uint)((x%p+p)%p)*374761393u+(uint)((y%p+p)%p)*668265263u;h=(h^(h>>13))*1274126177u;return (h&65535)/65535f;}}
        public static Mesh TissuePatch(ArteryJourneyPath path,float centre,float radiusOffset,float halfLength,float halfAngle)
        {
            const int rows=24,cols=20;var origin=path.Centre(centre);var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var centres=new List<Vector3>();var triangles=new List<int>();
            for(int j=0;j<=rows;j++)for(int i=0;i<=cols;i++)
            {
                float s=centre+Mathf.Lerp(-halfLength,halfLength,j/(float)rows),a=Mathf.Lerp(-halfAngle,halfAngle,i/(float)cols);
                var radial=path.Right(s)*Mathf.Cos(a)+path.Up(s)*Mathf.Sin(a);var c=path.Centre(s);
                vertices.Add(c+radial*(path.Radius(s)+radiusOffset)-origin);normals.Add(-radial);uvs.Add(new Vector2(i/(float)cols,j/(float)rows));centres.Add(c-origin);
                if(j<rows&&i<cols){int k=j*(cols+1)+i;triangles.AddRange(new[]{k,k+cols+1,k+cols+2,k,k+cols+2,k+1});}
            }
            var mesh=new Mesh{name="WALL_A_CurvedTissue"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetUVs(1,centres);mesh.SetTriangles(triangles,0);mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        public static GameObject Patch(string name,ArteryJourneyPath path,float offset,Transform parent,Material material)
        {
            string meshPath=Folder+"/"+name+".asset";ArteryGlbImporter.Folder(Folder);JourneyGeometry.Save(TissuePatch(path,path.Anchor(11)+5,offset,1.7f,.40f),meshPath);
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=path.Centre(path.Anchor(11)+5);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        public static void FitAssembly(Transform root,Vector3 centre,float diameter)
        {
            var renderers=root.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float factor=diameter/Mathf.Max(.01f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
            foreach(Transform child in root){child.position=centre+(child.position-bounds.center)*factor;child.localScale*=factor;}
        }
    }
}

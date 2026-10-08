using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>One cylindrical wall site, four shells behind each other, and baked unfolding poses.</summary>
    public static class ArteryLayerInspectionBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/Polish";
        const string PatchName="Scene04_WallLayerInspection_Patch";
        static readonly string[] Ids={"endothelium","intima","media","adventitia"};
        const int Rows=24,Cols=20;
        public const float HalfLength=1.22f,HalfAngle=.40f;

        [MenuItem("Biology VR/Apply Scene04 Layer Inspection")]
        public static void ApplyCurrentScene()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();
            var root=GameObject.Find("POLISH — anatomy, chunks and materials");
            Apply(w,w.art,root.transform,AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Coral_Endothelium.mat"),null);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        public static JourneyLayerInspection Apply(JourneyWorld world,JourneyArtComposition art,Transform parent,Material wallMaterial,Material[] layerMaterials)
        {
            if(!world||!art||!parent)throw new InvalidOperationException("Configured narrative world required");
            var old=parent.Find(PatchName);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var path=world.mover.path;float s=path.Anchor(4)+3;
            var patch=new GameObject(PatchName);patch.transform.SetParent(parent,false);patch.transform.SetPositionAndRotation(path.Centre(s),path.Frame(s));
            var targets=new JourneyTarget[4];var renderers=new Renderer[4];var contacts=new Collider[4];var filters=new MeshFilter[4];var closed=new Mesh[4];var opened=new Mesh[4];
            for(int kind=0;kind<4;kind++)
            {
                var target=world.targets.First(t=>t&&t.targetId==Ids[kind]);targets[kind]=target;
                foreach(var r in target.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                foreach(var c in target.GetComponentsInChildren<Collider>(true))c.enabled=false;
                foreach(Transform child in target.transform.Cast<Transform>().ToArray())if(child.name.StartsWith("Scene04_Band_"))UnityEngine.Object.DestroyImmediate(child.gameObject);
                target.transform.SetPositionAndRotation(patch.transform.position,patch.transform.rotation);target.transform.localScale=Vector3.one;
                string name="Scene04_"+Ids[kind];
                ArteryTissueTextures.Bake(name,kind);
                var material=new Material(wallMaterial){name=name+"_Tissue",enableInstancing=true};
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+name+"_Base.png"));
                material.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+name+"_Normal.png"));
                material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);
                material.SetFloat("_AllowWallCutaway",0);material.SetFloat("_PulseAmplitude",0);material.SetFloat("_NormalStrength",.70f);material.SetFloat("_Smoothness",.31f);
                material.SetColor("_BaseColor",Color.white);material.SetColor("_EmissionColor",Color.black);
                string materialPath=Folder+"/"+material.name+".mat";JourneyGeometry.Save(material,materialPath);material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                closed[kind]=SaveMesh(BuildShell(path,s,kind,false,patch.transform),name+"_Closed");
                opened[kind]=SaveMesh(BuildShell(path,s,kind,true,patch.transform),name+"_Opened");
                var go=new GameObject("Scene04_Band_"+Ids[kind]);go.transform.SetParent(target.transform,false);go.layer=target.gameObject.layer;
                filters[kind]=go.AddComponent<MeshFilter>();filters[kind].sharedMesh=closed[kind];
                var surface=go.AddComponent<MeshRenderer>();surface.sharedMaterial=material;surface.shadowCastingMode=ShadowCastingMode.Off;surface.receiveShadows=false;renderers[kind]=surface;
                var contact=go.AddComponent<MeshCollider>();contact.sharedMesh=closed[kind];contacts[kind]=contact;
                var grab=target.GetComponent<XRGrabInteractable>();if(grab){grab.colliders.Clear();grab.colliders.Add(contact);grab.enabled=false;}
                target.SetHomePose();
            }
            var activation=world.targets.First(t=>t.targetId=="layers");
            foreach(var r in activation.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            foreach(var c in activation.GetComponentsInChildren<Collider>(true))c.enabled=false;
            activation.transform.SetPositionAndRotation(patch.transform.position,patch.transform.rotation);activation.transform.localScale=Vector3.one;
            var activationObject=new GameObject("Scene04_Layers_ActivationCollider");activationObject.transform.SetParent(activation.transform,false);activationObject.layer=activation.gameObject.layer;
            var box=activationObject.AddComponent<BoxCollider>();box.center=new Vector3(path.Radius(s)-.12f,0,0);box.size=new Vector3(.12f,1.0f,1.65f);activation.SetHomePose();
            var tube=GameObject.Find("Continuous Main Artery");var collider=tube.GetComponent<MeshCollider>();
            var window=WindowCollider(collider,path,s);
            var inspection=parent.GetComponent<JourneyLayerInspection>();if(!inspection)inspection=parent.gameObject.AddComponent<JourneyLayerInspection>();
            RemoveBindings(art);
            if(art.layerCutaway)art.layerCutaway.SetActive(false);art.layerCutaway=patch;
            inspection.Configure(world,patch.transform,targets,activation,renderers,contacts,box,filters,closed,opened,collider,window);
            var labels=new UnityEngine.UI.Text[4];string[] captions={"Эндотелий","Интима","Медиа","Адвентиция"};
            for(int i=0;i<4;i++)labels[i]=ArteryAnnotations.Label(patch.transform,"Wall label "+Ids[i],captions[i]);inspection.SetLabels(labels);
            AssetDatabase.SaveAssets();EditorUtility.SetDirty(inspection);return inspection;
        }
        static Mesh SaveMesh(Mesh mesh,string name)
        {mesh.name=name;JourneyGeometry.Save(mesh,Folder+"/"+name+".asset");return AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");}
        static Mesh BuildShell(ArteryJourneyPath path,float station,int kind,bool open,Transform patch)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var centres=new List<Vector3>();var triangles=new List<int>();
            float[] depth={-.035f,.065f,.19f,.37f};float[] thickness={.028f,.07f,.11f,.15f};
            float[] foldMin={.76f,.48f,.20f,0};float[] foldMax={1f,.76f,.48f,1f};
            int stride=Cols+1,grid=stride*(Rows+1);
            for(int back=0;back<2;back++)for(int j=0;j<=Rows;j++)for(int i=0;i<=Cols;i++)
            {
                float u=i/(float)Cols,v=j/(float)Rows;
                float mapped=open&&kind<3?Mathf.Lerp(foldMin[kind],foldMax[kind],v):v;
                float distance=station+(mapped*2-1)*HalfLength;
                float angle=(u*2-1)*HalfAngle*(1-.035f*Mathf.Pow(Mathf.Abs(v*2-1),8));
                float fold=open&&kind<3?.10f*Mathf.Sin(v*Mathf.PI):0;
                float relief=kind==0?.012f*(.5f+.5f*Mathf.Cos(u*Mathf.PI*16)*Mathf.Sin(v*Mathf.PI*10)):kind==2?.014f*Mathf.Sin(u*Mathf.PI*14+v*2):.004f;
                var radial=path.Right(distance)*Mathf.Cos(angle)+path.Up(distance)*Mathf.Sin(angle);
                var centre=path.Centre(distance);
                vertices.Add(patch.InverseTransformPoint(centre+radial*(path.Radius(distance)+depth[kind]+back*thickness[kind]-fold-relief)));
                uv.Add(new Vector2(u,v));centres.Add(patch.InverseTransformPoint(centre));
                if(i<Cols&&j<Rows)
                {
                    int a=back*grid+j*stride+i,b=a+1,c=a+stride,d=c+1;
                    if(back==0)triangles.AddRange(new[]{a,c,d,a,d,b});else triangles.AddRange(new[]{a,d,c,a,b,d});
                }
            }
            void Edge(int a,int b){triangles.AddRange(new[]{a,b,a+grid,b,b+grid,a+grid});}
            for(int i=0;i<Cols;i++)Edge(i,i+1);
            for(int j=0;j<Rows;j++)Edge(j*stride+Cols,(j+1)*stride+Cols);
            for(int i=Cols;i>0;i--)Edge(Rows*stride+i,Rows*stride+i-1);
            for(int j=Rows;j>0;j--)Edge(j*stride,(j-1)*stride);
            AddSourceFibres(kind,open,path,station,patch,vertices,uv,centres,triangles);
            var mesh=new Mesh{indexFormat=IndexFormat.UInt16};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,centres);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        static void AddSourceFibres(int kind,bool open,ArteryJourneyPath path,float station,Transform patch,List<Vector3> vertices,List<Vector2> uv,List<Vector3> centres,List<int> triangles)
        {
            if(kind<2)return;
            var library=AssetDatabase.LoadAssetAtPath<GameObject>(BuildArteryVrScene.Root+"/Imported/Visual/HybridArteryVisual.prefab");
            var source=library.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name==(kind==2?"HD_S04_Muscle_Fiber_0":"HD_S04_Collagen_Fiber_0"));
            if(!source||!source.sharedMesh)return;
            var mesh=source.sharedMesh;var size=mesh.bounds.size;var centre=mesh.bounds.center;
            int longest=size.x>size.y?(size.x>size.z?0:2):(size.y>size.z?1:2),second=(longest+1)%3,third=(longest+2)%3;
            int count=kind==2?7:10;float depth=kind==2?.19f:.37f;
            for(int fibre=0;fibre<count;fibre++)
            {
                int start=vertices.Count;
                foreach(var vertex in mesh.vertices)
                {
                    var q=vertex-centre;float length=q[longest]/Mathf.Max(.001f,size[longest])+.5f;
                    float across=q[second]/Mathf.Max(.001f,size[second]),normal=q[third]/Mathf.Max(.001f,size[third]);
                    float a=.08f+length*.84f,b=(fibre+.5f)/count+across*(kind==2?.052f:.021f);
                    if(kind==3){b+=.10f*Mathf.Sin(length*Mathf.PI*2+fibre*.8f);if(fibre%2==0){float swap=a;a=b;b=swap;}}
                    a=Mathf.Clamp01(a);b=Mathf.Clamp01(b);float originalB=b;
                    if(open&&kind==2)b=Mathf.Lerp(.20f,.48f,b);
                    float s=station+(b*2-1)*HalfLength,angle=(a*2-1)*HalfAngle;
                    var radial=path.Right(s)*Mathf.Cos(angle)+path.Up(s)*Mathf.Sin(angle);
                    float curl=open&&kind==2?.10f*Mathf.Sin(originalB*Mathf.PI):0;
                    vertices.Add(patch.InverseTransformPoint(path.Centre(s)+radial*(path.Radius(s)+depth-.025f+normal*.034f-curl)));
                    uv.Add(new Vector2(a,originalB));centres.Add(patch.InverseTransformPoint(path.Centre(s)));
                }
                foreach(int index in mesh.triangles)triangles.Add(start+index);
            }
        }
        static Mesh WindowCollider(MeshCollider collider,ArteryJourneyPath path,float s)
        {
            var source=collider.sharedMesh;var vertices=source.vertices;var original=source.triangles;var indices=new List<int>(original.Length);
            var origin=path.Offset(s,path.Radius(s),0);var up=path.Up(s);var forward=path.Forward(s);var right=path.Right(s);
            for(int i=0;i<original.Length;i+=3)
            {
                var centre=collider.transform.TransformPoint((vertices[original[i]]+vertices[original[i+1]]+vertices[original[i+2]])/3)-origin;
                bool inside=Mathf.Abs(Vector3.Dot(centre,right))<.7f&&Mathf.Abs(Vector3.Dot(centre,up))<path.Radius(s)*HalfAngle+.15f&&Mathf.Abs(Vector3.Dot(centre,forward))<HalfLength+.20f;
                if(!inside){indices.Add(original[i]);indices.Add(original[i+1]);indices.Add(original[i+2]);}
            }
            var mesh=UnityEngine.Object.Instantiate(source);mesh.triangles=indices.ToArray();return SaveMesh(mesh,"Scene04_InspectionWallWindow");
        }
        static void RemoveBindings(JourneyArtComposition art)
        {
            var ids=new List<string>();var anchors=new List<Transform>();
            for(int i=0;i<art.attachmentIds.Length;i++)if(!Ids.Contains(art.attachmentIds[i])){ids.Add(art.attachmentIds[i]);anchors.Add(art.wallAttachments[i]);}
            art.attachmentIds=ids.ToArray();art.wallAttachments=anchors.ToArray();
        }
    }
}

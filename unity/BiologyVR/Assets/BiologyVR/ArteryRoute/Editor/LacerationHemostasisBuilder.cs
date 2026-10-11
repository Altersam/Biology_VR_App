using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class LacerationHemostasisBuilder
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/LacerationHemostasis";
        public const string Reports=JourneyGeometry.Root+"/Reports/LacerationHemostasis";
        [MenuItem("Biology VR/Install Laceration And Eleven Flow Platelets")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();var puzzle=world.hemostasisPuzzle;if(!puzzle||!world.woundState)throw new InvalidOperationException("Existing WALL_A controllers required");
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports+"/Backup");EditorSceneManager.SaveScene(world.gameObject.scene);
            string backup=Reports+"/Backup/ArteryNarrativeVR_before_laceration.unity";if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Wound backup failed");
            float arc=world.mover.path.Anchor(11)+5;var path=world.mover.path;var outward=path.Right(arc);var up=path.Up(arc);var forward=path.Forward(arc);
            var location=world.locationRoots[path.locationIds[11-3]].transform;
            if(world.woundState.LegacyMatrix)world.woundState.LegacyMatrix.enabled=false;
            if(world.woundState.LegacyCollagen)world.woundState.LegacyCollagen.SetActive(false);
            var old=location.Find("WALL_A — jagged longitudinal laceration");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("WALL_A — jagged longitudinal laceration");root.transform.SetParent(location,false);
            var wall=world.art.wallChunks.Where(r=>r&&r.sharedMaterial).OrderBy(r=>(r.bounds.ClosestPoint(world.woundSite.position)-world.woundSite.position).sqrMagnitude).First();
            var skin=new Material(wall.sharedMaterial){name="Laceration adjoining endothelium"};skin.SetFloat("_PulseAmplitude",0);skin.SetFloat("_Tone",1);skin.SetFloat("_AllowWallCutaway",0);SaveMaterial(skin,"EndothelialCover.mat");skin=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/EndothelialCover.mat");
            Material red=Lit("Torn moist tissue",new Color(.46f,.085f,.065f),.22f),deep=Lit("Deep red tear channel",new Color(.29f,.045f,.045f),.3f);
            Mesh annulus=RingMesh(path,arc,root.transform,false),depth=RingMesh(path,arc,root.transform,true),seal=SealMesh(path,arc,root.transform);
            var cover=Visual(root.transform,"Cover old opening except jagged scratch",annulus,skin);
            var interior=Visual(root.transform,"Actual torn depth surfaces",depth,deep);
            var oldPatch=world.repairPatch.GetComponent<MeshFilter>();if(oldPatch){JourneyGeometry.Save(seal,Folder+"/ScratchHealingSeal.asset");oldPatch.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/ScratchHealingSeal.asset");world.repairPatch.transform.SetPositionAndRotation(root.transform.position,root.transform.rotation);world.repairPatch.transform.localScale=root.transform.lossyScale;world.repairPatch.GetComponent<Renderer>().sharedMaterial=skin;EditorUtility.SetDirty(oldPatch);EditorUtility.SetDirty(world.repairPatch);}
            var sphereObject=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=sphereObject.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sphereObject);
            var flaps=new Transform[20];
            for(int i=0;i<flaps.Length;i++)
            {float x=-1.1f+(i/2)*.24f,y=(i%2==0?1:-1)*(.16f+.035f*Mathf.Sin(i*2.17f));var go=Visual(root.transform,"Ragged lifted tissue edge "+i,sphere,red);go.transform.SetPositionAndRotation(Surface(path,arc,x,y,-.023f),Quaternion.LookRotation(-outward,up)*Quaternion.Euler(i%2==0?15:-15,0,i*13));go.transform.localScale=new Vector3(.19f,.055f,.12f);flaps[i]=go.transform;}
            var scaffold=new GameObject("Exposed fine collagen in scratch");scaffold.transform.SetParent(root.transform,false);var collagen=Lit("Exposed collagen cream",new Color(.88f,.72f,.56f),.15f);
            for(int i=0;i<9;i++){var go=Visual(scaffold.transform,"Exposed collagen fibre "+i,sphere,collagen);go.transform.SetPositionAndRotation(world.woundSite.position-outward*.02f+forward*((i-4)*.23f)+up*.03f,Quaternion.LookRotation(forward,up));go.transform.localScale=new Vector3(.02f,.02f,.27f);}
            world.woundState.ReplaceTearSurfaces(interior.GetComponent<Renderer>(),scaffold);
            world.woundState.splitMainFlow=world.GetComponent<JourneyContinuousBloodstream>()!=null;
            var bloodMat=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Crimson outward blood streak"};bloodMat.SetColor("_BaseColor",new Color(.55f,.025f,.045f));SaveMaterial(bloodMat,"BloodStreak.mat");bloodMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/BloodStreak.mat");
            var view=world.GetComponent<JourneyLacerationView>();if(!view)view=world.gameObject.AddComponent<JourneyLacerationView>();view.world=world;view.visualRoot=root;view.edgeFlaps=flaps;view.scar=interior.GetComponent<Renderer>();view.jets=new LineRenderer[6];
            for(int i=0;i<view.jets.Length;i++){var go=new GameObject("Outward scratch jet "+i,typeof(LineRenderer));go.transform.SetParent(root.transform,false);var line=go.GetComponent<LineRenderer>();line.sharedMaterial=bloodMat;line.useWorldSpace=true;line.positionCount=2;line.shadowCastingMode=ShadowCastingMode.Off;line.enabled=false;view.jets[i]=line;}
            view.label=ArteryAnnotations.Label(root.transform,"Laceration status","Разрыв стенки");view.label.fontSize=16;view.label.transform.localScale=Vector3.one*.0017f;
            var pieces=puzzle.platelets.ToList();
            for(int i=pieces.Count;i<14;i++){var go=UnityEngine.Object.Instantiate(pieces[0].gameObject,pieces[0].transform.parent);go.name="Flow-caught platelet "+i;var piece=go.GetComponent<JourneyHemostasisPiece>();piece.puzzle=puzzle;piece.index=i;go.SetActive(false);pieces.Add(piece);}
            puzzle.platelets=pieces.Take(14).ToArray();puzzle.flowLayering=true;world.platelets=puzzle.platelets.Select(p=>p.gameObject).ToArray();
            foreach(var piece in puzzle.platelets){piece.gameObject.name=piece.index<11?"Flow-caught platelet "+piece.index:"Reserve balance platelet "+piece.index;piece.GetComponent<MeshFilter>().sharedMesh=puzzle.resting;piece.GetComponent<Renderer>().sharedMaterials=puzzle.restingMaterials;piece.transform.localScale=Vector3.one;var contact=piece.GetComponent<SphereCollider>();contact.center=Vector3.zero;contact.radius=.17f;piece.Grab.throwOnDetach=false;piece.Grab.smoothPosition=true;piece.Grab.smoothPositionAmount=8;piece.SetPlaced(false);piece.gameObject.SetActive(false);EditorUtility.SetDirty(piece);EditorUtility.SetDirty(piece.Grab);}
            var activation=BuildActivation(world,puzzle,location,sphere);puzzle.activationPuzzle=activation;
            BuildScratchFibrin(world,puzzle,forward,up,-outward);
            var input=world.mission.GetComponent<JourneyInput>();var field=input.GetComponent<JourneyPlateletCapture>();if(!field)field=input.gameObject.AddComponent<JourneyPlateletCapture>();field.input=input;field.puzzle=puzzle;input.plateletCapture=field;EditorUtility.SetDirty(field);EditorUtility.SetDirty(input);
            EditorUtility.SetDirty(puzzle);EditorUtility.SetDirty(world);EditorUtility.SetDirty(world.woundState);EditorUtility.SetDirty(view);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,existingWoundSite=world.woundSite.name,conditionalPrimaryPlugProgress=11,reservePlatelets=3,jaggedScratch=true,tearDepth=true,mainFlowSplit=true,leakSelectionAtFullInjury=.25f,independentLeakEmitterDisabled=true,capture="held BioTool field → automatic adhesion near damaged subendothelium",stabilization="platelet activation → GPIIb/IIIa + fibrinogen aggregation → thrombin/fibrin",EDTA="calcium chelation blocks coagulation",gradualFibrinSeconds=6.25f,physicalTokens=activation.tokens.Length,backgroundPool=world.GetComponent<JourneyBloodFlow>().cells.Length,worldTargets=world.targets.Length,hardwareVerified=false},Formatting.Indented));
        }
        static JourneyPlateletActivation BuildActivation(JourneyWorld world,JourneyHemostasisPuzzle puzzle,Transform location,Mesh sphere)
        {
            var old=location.Find("Platelet activation biochemical board");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var activation=puzzle.GetComponent<JourneyPlateletActivation>();if(!activation)activation=puzzle.gameObject.AddComponent<JourneyPlateletActivation>();activation.puzzle=puzzle;
            var root=new GameObject("Platelet activation biochemical board");root.transform.SetParent(location,false);float s=world.mover.path.Anchor(11)+5;root.transform.SetPositionAndRotation(world.woundSite.position-world.mover.path.Right(s)*1.1f+world.mover.path.Up(s)*1.15f-world.mover.path.Forward(s)*.55f,Quaternion.LookRotation(world.mover.path.Right(s),world.mover.path.Up(s)));activation.panel=root;
            var canvasGo=new GameObject("Biochemical instructions",typeof(RectTransform),typeof(Canvas));canvasGo.transform.SetParent(root.transform,false);var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=world.mover.viewCamera;var rect=(RectTransform)canvasGo.transform;rect.sizeDelta=new Vector2(600,190);rect.localScale=Vector3.one*.002f;rect.localPosition=new Vector3(0,.29f,0);
            var glass=new GameObject("Airy biochemical glass",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));glass.transform.SetParent(canvasGo.transform,false);var image=glass.GetComponent<Image>();image.color=HolographicStyle.Glass(.28f);image.raycastTarget=false;var gr=(RectTransform)glass.transform;gr.anchorMin=Vector2.zero;gr.anchorMax=Vector2.one;gr.offsetMin=gr.offsetMax=Vector2.zero;
            activation.caption=ArteryAnnotations.Label(canvasGo.transform,"Activation sequence instructions","Активация → агрегация → фибрин");activation.caption.fontSize=24;activation.caption.rectTransform.sizeDelta=new Vector2(570,170);activation.caption.transform.localScale=Vector3.one;activation.caption.transform.localPosition=Vector3.zero;activation.caption.raycastTarget=false;
            Material cyan=Lit("Activation slot cyan",HolographicStyle.Cyan*.55f,.15f),amber=Lit("Activation token amber",HolographicStyle.Amber,.25f);
            activation.slots=new Transform[3];activation.slotSurfaces=new Renderer[3];
            for(int i=0;i<3;i++){var go=Visual(root.transform,"Biochemical slot "+i,sphere,cyan);go.transform.localPosition=new Vector3((i-1)*.34f,0,0);go.transform.localScale=new Vector3(.25f,.25f,.025f);activation.slots[i]=go.transform;activation.slotSurfaces[i]=go.GetComponent<Renderer>();}
            activation.tokens=new JourneyPlateletActivationToken[4];
            for(int i=0;i<4;i++)
            {
                var go=new GameObject(new[]{"ADP platelet activation card","GPIIb IIIa plus fibrinogen aggregation card","Thrombin to fibrin stabilization card","EDTA calcium chelator distractor"}[i]);go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3((i-1.5f)*.29f,-.38f,-.08f);go.layer=LayerMask.NameToLayer("JourneyTarget");
                var visual=Visual(go.transform,"Factor chip",sphere,i==3?cyan:amber);visual.transform.localScale=new Vector3(.20f,.16f,.055f);
                var body=go.AddComponent<Rigidbody>();body.useGravity=false;body.isKinematic=true;var contact=go.AddComponent<SphereCollider>();contact.radius=.13f;
                var grab=go.AddComponent<XRGrabInteractable>();grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.throwOnDetach=false;grab.useDynamicAttach=true;grab.colliders.Add(contact);
                var token=go.AddComponent<JourneyPlateletActivationToken>();token.owner=activation;token.code=i;token.contact=contact;token.grab=grab;token.home=go.transform.localPosition;activation.tokens[i]=token;
                var text=ArteryAnnotations.Label(go.transform,"Factor label",new[]{"ADP\nАКТИВАЦИЯ","GPIIb/IIIa +\nФИБРИНОГЕН","ТРОМБИН →\nФИБРИН","EDTA\nСВЯЗЫВАЕТ Ca²⁺"}[i]);text.fontSize=18;text.rectTransform.sizeDelta=new Vector2(210,65);text.transform.localScale=Vector3.one*.0013f;text.transform.localPosition=new Vector3(0,.13f,-.03f);
            }
            root.SetActive(false);EditorUtility.SetDirty(activation);return activation;
        }
        static Vector3 Surface(ArteryJourneyPath path,float arc,float along,float across,float inward)
        {float s=arc+along;float angle=across/path.Radius(s);return path.Centre(s)+(path.Right(s)*Mathf.Cos(angle)+path.Up(s)*Mathf.Sin(angle))*(path.Radius(s)+inward);}
        static void BuildScratchFibrin(JourneyWorld world,JourneyHemostasisPuzzle puzzle,Vector3 forward,Vector3 up,Vector3 normal)
        {
            var network=world.fibrin.transform.Find("Teaching fibrin stabilisation network");if(!network)throw new InvalidOperationException("Existing fibrin view required");
            var vertices=new List<Vector3>();var triangles=new List<int>();
            // Small opaque tubes follow the longitudinal plug instead of the old round teaching web.
            for(int row=0;row<4;row++)for(int segment=0;segment<8;segment++)
            {
                float a=-1.12f+segment*.28f,b=a+.28f,y=(row-1.5f)*.105f;
                FibrinTube(vertices,triangles,new Vector3(a,y,.015f*Mathf.Sin(segment+row)),new Vector3(b,y,.015f*Mathf.Sin(segment+row+1)),.007f);
            }
            for(int cross=0;cross<9;cross++){float x=-1.12f+cross*.28f;FibrinTube(vertices,triangles,new Vector3(x,-.18f,0),new Vector3(x+.07f,.18f,.02f),.007f);}
            var mesh=new Mesh{name="Longitudinal primary plug fibrin lattice"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();JourneyGeometry.Save(mesh,Folder+"/ScratchFibrinLattice.asset");
            network.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/ScratchFibrinLattice.asset");
            world.fibrin.transform.SetPositionAndRotation(world.woundSite.position+normal*.10f,Quaternion.LookRotation(normal,up));world.fibrin.transform.localScale=Vector3.one;
            // Local X follows vessel length; retain the same controller-owned network renderer.
            network.localPosition=Vector3.forward*.23f;network.localRotation=Quaternion.identity;network.localScale=Vector3.one;
            for(int i=0;i<3;i++)
            {
                puzzle.starts[i].position=world.woundSite.position+normal*.32f-forward*1.02f+up*((i-1)*.14f);
                puzzle.ends[i].position=world.woundSite.position+normal*.32f+forward*1.02f+up*((1-i)*.14f);
                puzzle.starts[i].localScale=Vector3.one*.035f;puzzle.ends[i].localScale=Vector3.one*.035f;puzzle.strands[i].transform.localScale=Vector3.one*.055f;
            }
            EditorUtility.SetDirty(network.GetComponent<MeshFilter>());
        }
        static void FibrinTube(List<Vector3> vertices,List<int> triangles,Vector3 a,Vector3 b,float radius)
        {
            int first=vertices.Count;var direction=(b-a).normalized;var x=Vector3.Cross(direction,Vector3.forward).normalized;var y=Vector3.Cross(direction,x).normalized;
            for(int end=0;end<2;end++)for(int side=0;side<5;side++){float angle=side*Mathf.PI*2/5;vertices.Add((end==0?a:b)+(x*Mathf.Cos(angle)+y*Mathf.Sin(angle))*radius);}
            for(int side=0;side<5;side++){int next=(side+1)%5;triangles.AddRange(new[]{first+side,first+next,first+side+5,first+next,first+next+5,first+side+5});}
        }
        static Mesh RingMesh(ArteryJourneyPath path,float arc,Transform parent,bool depth)
        {
            const int count=64;var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var centers=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=count;i++)
            {float a=i*Mathf.PI*2/count;float along=Mathf.Cos(a)*1.24f,across=Mathf.Sin(a)*(.14f+.045f*Mathf.Sin(i*1.73f));
                for(int edge=0;edge<2;edge++)
                {float x=edge==0&&!depth?Mathf.Cos(a)*1.55f:along,y=edge==0&&!depth?Mathf.Sin(a)*1.16f:across,s=arc+x;var point=Surface(path,arc,x,y,edge==0?(depth?.18f:-.012f):-.016f);float angle=y/path.Radius(s);vertices.Add(parent.InverseTransformPoint(point));normals.Add(parent.InverseTransformDirection(-(point-path.Centre(s)).normalized));uv.Add(new Vector2(angle*path.Radius(s)/3.5f,s/5.6f));centers.Add(parent.InverseTransformPoint(path.Centre(s)));}
            }
            for(int i=0;i<count;i++){int a=i*2;triangles.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
            var mesh=new Mesh{name=depth?"Jagged scratch depth walls":"Endothelial old-hole cover with narrow tear"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,centers);mesh.SetTriangles(triangles,0);if(depth)mesh.RecalculateNormals();else mesh.SetNormals(normals);mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        static Mesh SealMesh(ArteryJourneyPath path,float arc,Transform parent)
        {var v=new List<Vector3>();var uv=new List<Vector2>();var centers=new List<Vector3>();var faces=new List<int>();for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;v.Add(parent.InverseTransformPoint(Surface(path,arc,Mathf.Cos(a)*1.25f,Mathf.Sin(a)*.21f,-.024f)));uv.Add(new Vector2(.5f+.5f*Mathf.Cos(a),.5f+.5f*Mathf.Sin(a)));centers.Add(parent.InverseTransformPoint(path.Centre(arc)));}v.Add(parent.InverseTransformPoint(Surface(path,arc,0,0,-.024f)));uv.Add(Vector2.one*.5f);centers.Add(parent.InverseTransformPoint(path.Centre(arc)));for(int i=0;i<32;i++)faces.AddRange(new[]{33,i,i+1});var mesh=new Mesh{name="Scratch closure surface"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetUVs(1,centers);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;}
        static Material Lit(string name,Color color,float smoothness){var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smoothness);SaveMaterial(mat,name.Replace(" ","_")+".mat");return AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name.Replace(" ","_")+".mat");}
        static void SaveMaterial(Material mat,string name){JourneyGeometry.Save(mat,Folder+"/"+name);}
        static GameObject Visual(Transform parent,string name,Mesh mesh,Material mat){string path=Folder+"/"+mesh.name.Replace(" ","_")+".asset";if(!AssetDatabase.Contains(mesh)){JourneyGeometry.Save(mesh,path);mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);}var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go;}
    }
}

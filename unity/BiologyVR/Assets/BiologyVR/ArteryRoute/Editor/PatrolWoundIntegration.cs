using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>In-place authoring and build validation of the existing episode components.</summary>
    public sealed class PatrolWoundIntegration : IProcessSceneWithReport
    {
        public const string Reports=JourneyGeometry.Root+"/Reports/Polish/PatrolWoundIntegration";
        const string Materials=JourneyGeometry.Root+"/Generated/ImmunePatrol";
        public int callbackOrder=>100;

        [MenuItem("Biology VR/Integrate Patrol And Playable Wound In Current Scene")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Validate(world);
            if(world.gameObject.scene.path!=UniversalMobileBuild.ScenePath)throw new InvalidOperationException("Open the actual player build scene first");
            Directory.CreateDirectory(Reports);Directory.CreateDirectory(Materials);
            EditorSceneManager.SaveScene(world.gameObject.scene);
            string backup=Reports+"/Before_Integration.unity";
            if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Scene checkpoint failed");
            var patrol=world.immunePatrol;var path=world.mover.path;float station=patrol.Station;
            var point=path.Offset(station,path.Radius(station)-.06f,0);
            patrol.alteredWall.transform.position=point;
            foreach(var target in new[]{patrol.wallTarget,patrol.revealTarget})
            {target.transform.position=point;target.GetComponent<SphereCollider>().radius=.55f;target.SetHomePose();EditorUtility.SetDirty(target);EditorUtility.SetDirty(target.GetComponent<Collider>());}
            int[] counts={3,4,3,5,4,5};
            var tissue=Lit("Patrol tissue normal",new Color(.76f,.44f,.47f));
            var fragments=Lit("Patrol soft apoptotic fragments",new Color(.65f,.48f,.63f));
            for(int i=0;i<patrol.focuses.Length;i++)
            {
                var focus=patrol.focuses[i];float s=station+(i%3-1)*.78f;float y=i<3?-.62f:.62f;
                focus.responders=counts[i];focus.surfacePoint.position=path.Offset(s,path.Radius(s),y);
                focus.transform.SetPositionAndRotation(focus.surfacePoint.position+path.Right(s)*(.12f+(i%2)*.035f),path.Frame(s));
                focus.target.SetHomePose();focus.target.GetComponent<SphereCollider>().radius=.30f;
                for(int cell=0;cell<focus.cells.Length;cell++)
                {focus.cells[cell].transform.localPosition=new Vector3(0,(cell%2-.5f)*.18f,(cell/2-.5f)*.20f);focus.cells[cell].transform.localScale=new Vector3(.17f,.22f,.18f)*(i==5?1.25f:1);}
                focus.initialScales=focus.cells.Select(r=>r.transform.localScale).ToArray();
                focus.fragments=new Renderer[focus.cells.Length*3];
                for(int n=0;n<focus.fragments.Length;n++)
                {var part=Visual(focus.transform,"Apoptosis fragment "+n,focus.cells[0].GetComponent<MeshFilter>().sharedMesh,fragments);part.enabled=false;part.transform.localScale=Vector3.one*.045f;focus.fragments[n]=part;}
                var bed=Visual(patrol.root.transform,"Normal tissue bed "+i,focus.cells[0].GetComponent<MeshFilter>().sharedMesh,tissue);
                bed.transform.SetPositionAndRotation(focus.surfacePoint.position+path.Right(s)*.58f,path.Frame(s));bed.transform.localScale=new Vector3(.10f,.46f,.48f);
                EditorUtility.SetDirty(focus);EditorUtility.SetDirty(focus.target);EditorUtility.SetDirty(focus.target.GetComponent<Collider>());
            }
            // The existing cutaway owns visibility. Beds stay behind its external layer.
            foreach(var target in new[]{world.Find("wound"),world.Find("leak"),world.Find("pressure-low")})
            {var contact=target.GetComponent<SphereCollider>();contact.center=Vector3.zero;contact.radius=.42f/Mathf.Max(.001f,target.transform.lossyScale.x);EditorUtility.SetDirty(contact);}
            world.mission.SceneNumber=3;world.bloodEntities.stream.uniformCirculation=true;
            foreach(var item in new UnityEngine.Object[]{world,world.mission,patrol,patrol.alteredWall,world.plateletLaunch})EditorUtility.SetDirty(item);
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,version=BiologyReleaseVersion.Version,scene=world.gameObject.scene.path,focuses=6,counts,maximumResponders=5,reused="JourneyMission / JourneyMover / BloodFlow pool / Reverse / Scanner / LayerInspection / HolographicScreens / HemostasisPuzzle",woundContact="actual visible tear plane; automatic free slot selection",cameraForced=false},Formatting.Indented));
        }
        static Material Lit(string name,Color color)
        {string path=Materials+"/"+name.Replace(' ','_')+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;}
        static Renderer Visual(Transform parent,string name,Mesh mesh,Material material)
        {var existing=parent.Find(name);var go=existing?existing.gameObject:new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));if(!existing)go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return renderer;}
        [MenuItem("Biology VR/Refine Existing Wound Readability And Interaction")]
        public static void RefineWound()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Validate(world);var view=world.GetComponent<JourneyLacerationView>();
            Directory.CreateDirectory(Reports);EditorSceneManager.SaveScene(world.gameObject.scene);string backup=Reports+"/Before_WoundReadability.unity";if(!File.Exists(backup))AssetDatabase.CopyAsset(world.gameObject.scene.path,backup);
            var root=view.visualRoot.transform;var rim=root.Find("Wound interaction contour");
            if(!rim){var go=new GameObject("Wound interaction contour",typeof(LineRenderer));go.transform.SetParent(root,false);rim=go.transform;}
            view.interactionRim=rim.GetComponent<LineRenderer>();view.interactionRim.useWorldSpace=true;view.interactionRim.loop=true;view.interactionRim.positionCount=48;view.interactionRim.widthMultiplier=.009f;view.interactionRim.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.interactionRim.enabled=false;
            view.rimIdle=SignalMaterial("Wound contour warm ivory",new Color(.94f,.77f,.49f));view.rimReady=SignalMaterial("Wound contour ready mint",new Color(.38f,.85f,.72f));view.rimSuccess=SignalMaterial("Wound contour adhesion",new Color(.76f,.97f,.73f));view.interactionRim.sharedMaterial=view.rimIdle;
            var hint=root.Find("Platelet contextual action");view.plateletHint=hint?hint.GetComponent<UnityEngine.UI.Text>():ArteryAnnotations.Label(root,"Platelet contextual action","ТРОМБОЦИТ");
            foreach(var text in new[]{view.label,view.plateletHint,world.hemostasisPuzzle.label})
            {text.fontSize=28;text.fontStyle=FontStyle.Bold;text.rectTransform.sizeDelta=new Vector2(850,100);text.transform.localScale=Vector3.one*.0035f;text.raycastTarget=false;text.color=new Color(.98f,.92f,.78f);EditorUtility.SetDirty(text);}
            view.plateletHint.fontSize=20;view.plateletHint.transform.localScale=Vector3.one*.0012f;view.plateletHint.gameObject.SetActive(false);
            var active=Lit("Wound activated platelet warm amber",new Color(.95f,.69f,.31f));world.hemostasisPuzzle.activatedMaterials=new[]{active};
            foreach(var platelet in world.hemostasisPuzzle.platelets){platelet.transform.localScale=Vector3.one*1.5f;EditorUtility.SetDirty(platelet.transform);}
            foreach(var edge in view.edgeFlaps){var renderer=edge.GetComponent<Renderer>();renderer.sharedMaterial=Lit("Wound soft lifted edge",new Color(.69f,.29f,.28f));EditorUtility.SetDirty(renderer);}
            foreach(var obj in new UnityEngine.Object[]{view,view.interactionRim,world.hemostasisPuzzle})EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/WoundReadability.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,scene=world.gameObject.scene.path,existingWound=world.woundSite.name,closerStationOffset=3.8f,diagnosticPressure=92,contextualSteps="capture / Scanner / Reverse / aim / release",visibleContact="warm contour, mint on valid aim, adhesion flash",platelets="existing pooled cells; existing meshes face lumen",newUiSystem=false},Formatting.Indented));
        }
        static Material SignalMaterial(string name,Color color)
        {string path=Materials+"/"+name.Replace(' ','_')+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;}
        public static void Validate(JourneyWorld world)
        {
            if(!world||!world.immunePatrol||!world.plateletLaunch||!world.layerInspection||!world.bloodEntities||!world.freeTools||!world.hemostasisPuzzle)throw new BuildFailedException("Actual artery scene must include installed patrol, cutaway, blood pool and platelet wound layer");
            var p=world.immunePatrol;
            if(p.focuses==null||p.focuses.Length!=6||p.focuses.Any(f=>!f||!f.target||!f.surfacePoint)||p.responderBodies.Length!=5||!p.wallTarget||!p.revealTarget||!p.colliderWindow)throw new BuildFailedException("Incomplete six-focus patrol references in build scene");
            if(!world.hemostasisPuzzle.flowLayering||world.hemostasisPuzzle.RequiredPlatelets!=11||!world.woundSite)throw new BuildFailedException("Existing eleven-platelet wound layer is incomplete");
        }
        public void OnProcessScene(Scene scene,BuildReport report)
        {
            if(report==null||scene.path!=UniversalMobileBuild.ScenePath)return;
            var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<JourneyWorld>(true)).FirstOrDefault();Validate(world);
            if(world.immunePatrol.focuses.Any(f=>f.fragments==null||f.fragments.Length==0))throw new BuildFailedException("Apply Patrol/Wound integration before building this player scene");
            var wound=world.GetComponent<JourneyLacerationView>();if(!wound||!wound.interactionRim||!wound.plateletHint)throw new BuildFailedException("Apply the existing wound readability refinement before building");
            Directory.CreateDirectory(Reports);
            File.WriteAllText(Reports+"/BuiltScene.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,version=PlayerSettings.bundleVersion,scene=scene.path,sceneGuid=AssetDatabase.AssetPathToGUID(scene.path),focuses=world.immunePatrol.focuses.Select(f=>new{f.name,f.responders}).ToArray(),platelets=world.hemostasisPuzzle.RequiredPlatelets,passed=true},Formatting.Indented));
        }
    }
}

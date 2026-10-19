using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ImmunePatrolBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/ImmunePatrol";
        const string Reports=JourneyGeometry.Root+"/Reports/Polish/ImmunePatrol";
        [MenuItem("Biology VR/Install Immune Patrol Scanner Activation And Platelet Reverse")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);EditorSceneManager.SaveScene(world.gameObject.scene);
            if(!File.Exists(Reports+"/Before_ImmunePatrol.unity"))AssetDatabase.CopyAsset(world.gameObject.scene.path,Reports+"/Before_ImmunePatrol.unity");
            var sphereGo=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=sphereGo.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sphereGo);
            var platelets=world.GetComponent<JourneyPlateletLaunch>();if(!platelets)platelets=world.gameObject.AddComponent<JourneyPlateletLaunch>();world.plateletLaunch=platelets;platelets.world=world;
            var oldLine=world.transform.Find("Platelet reverse trajectory");if(oldLine)UnityEngine.Object.DestroyImmediate(oldLine.gameObject);platelets.preview=Line(world.transform,"Platelet reverse trajectory",world.immunity.trajectory.sharedMaterial,.004f);
            var rig=world.mission.GetComponent<JourneyInstrumentRig>();var plasma=Material("Plasma impulse",new Color(.18f,.8f,1,1));var returning=Material("Scanner return activation",new Color(.55f,1,.88f,1));
            foreach(var name in new[]{"Plasma shot orb","Scanner inbound activation orb","Scanner inbound activation trail"}){var old=rig.transform.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            rig.plasmaOrb=Visual(rig.transform,"Plasma shot orb",sphere,plasma).transform;rig.plasmaOrb.gameObject.SetActive(false);rig.scannerActivationOrb=Visual(rig.transform,"Scanner inbound activation orb",sphere,returning).transform;rig.scannerActivationOrb.gameObject.SetActive(false);rig.scannerActivationTrail=Line(rig.transform,"Scanner inbound activation trail",returning,.012f);
            foreach(var virus in world.immunity.viruses)
            {
                var source=AssetDatabase.LoadAssetAtPath<Mesh>(JourneyGeometry.Root+"/Generated/ArcadeVisualPass/Phagocyte.asset");if(!source)throw new InvalidOperationException("Existing opaque source mesh missing");
                var old=virus.transform.Find("Damage-readable spikes");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var spikeRoot=new GameObject("Damage-readable spikes");spikeRoot.transform.SetParent(virus.transform,false);
                virus.body.GetComponent<MeshFilter>().sharedMesh=sphere;virus.body.sharedMaterial=Material("Virus core",new Color(.44f,.28f,.67f,1));virus.spikeGroups=new Transform[3];
                for(int group=0;group<3;group++){var g=new GameObject("Spikes / third "+(group+1));g.transform.SetParent(spikeRoot.transform,false);virus.spikeGroups[group]=g.transform;for(int n=0;n<6;n++){int i=group+n*3;float y=1-2*(i+.5f)/18f,r=Mathf.Sqrt(1-y*y),a=i*2.399963f;var normal=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);var spike=Visual(g.transform,"Viral surface spike "+i,sphere,Material("Viral spikes",new Color(.88f,.27f,.25f,1)));spike.transform.localPosition=normal*.5f;spike.transform.localRotation=Quaternion.FromToRotation(Vector3.up,normal);spike.transform.localScale=new Vector3(.11f,.32f,.11f);}}
                EditorUtility.SetDirty(virus);EditorUtility.SetDirty(virus.body.GetComponent<MeshFilter>());
            }
            var patrol=world.GetComponent<JourneyImmunePatrol>();if(!patrol)patrol=world.gameObject.AddComponent<JourneyImmunePatrol>();world.immunePatrol=patrol;patrol.world=world;
            var prior=world.transform.Find("Immune patrol tissue");if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);var root=new GameObject("Immune patrol tissue");root.transform.SetParent(world.transform,false);patrol.root=root;
            float station=world.mover.path.Anchor(10)+3.2f;var path=world.mover.path;var point=path.Offset(station,path.Radius(station)-.04f,0);
            patrol.wallTarget=Target(root.transform,"patrol-wall","Изменённый участок",world);patrol.wallTarget.transform.position=point;patrol.wallTarget.GetComponent<SphereCollider>().radius=.48f;
            patrol.alteredWall=Visual(root.transform,"Subtle changed wall patch",sphere,Material("Patrol changed warm tissue",new Color(.68f,.34f,.36f,1)));patrol.alteredWall.transform.SetPositionAndRotation(point,path.Frame(station));patrol.alteredWall.transform.localScale=new Vector3(.07f,.7f,1.1f);
            patrol.revealTarget=Target(root.transform,"patrol-reveal","Просмотреть глубже",world);patrol.revealTarget.transform.position=point;patrol.revealTarget.GetComponent<SphereCollider>().radius=.48f;
            patrol.colliderWindow=ArteryLayerInspectionBuilder.WindowColliderAt(GameObject.Find("Continuous Main Artery").GetComponent<MeshCollider>(),path,station,"Scene10_ImmunePatrolWindow");
            patrol.focuses=new JourneyImmuneFocus[6];int[] counts={3,4,3,5,4,5};var targets=new System.Collections.Generic.List<JourneyTarget>{patrol.wallTarget,patrol.revealTarget};var cold=Material("Patrol abnormal cells",new Color(.55f,.30f,.49f,1));
            for(int i=0;i<6;i++)
            {
                var target=Target(root.transform,"patrol-focus-"+(i+1).ToString("00"),"Иммунный очаг "+(i+1),world);target.name="ImmuneFocus_"+(i+1).ToString("00");target.GetComponent<SphereCollider>().radius=.30f;float distance=station+(i%3-1)*.85f;float y=i<3?-.62f:.62f;
                var surface=path.Offset(distance,path.Radius(distance),y);target.transform.position=surface+path.Right(distance)*(.36f+(i%2)*.08f);var focus=target.gameObject.AddComponent<JourneyImmuneFocus>();focus.index=i;focus.responders=counts[i];focus.target=target;focus.surfacePoint=new GameObject("Immune approach wall "+i).transform;focus.surfacePoint.SetParent(root.transform,false);focus.surfacePoint.position=surface;focus.cells=new Renderer[i==4?4:3];
                for(int n=0;n<focus.cells.Length;n++){var cell=Visual(target.transform,"Abnormal cluster cell "+n,sphere,cold);cell.transform.localPosition=new Vector3((n%2-.5f)*.18f,(n/2-.5f)*.16f,0);cell.transform.localScale=new Vector3(.17f,.22f,.18f)*(i==5?1.25f:1);focus.cells[n]=cell;}
                focus.initialScales=focus.cells.Select(c=>c.transform.localScale).ToArray();patrol.focuses[i]=focus;targets.Add(target);EditorUtility.SetDirty(focus);
            }
            var responses=world.transform.Find("Immune patrol responder pool");if(responses)UnityEngine.Object.DestroyImmediate(responses.gameObject);var actors=new GameObject("Immune patrol responder pool");actors.transform.SetParent(world.transform,false);patrol.responderBodies=new MeshFilter[5];var white=Material("Patrol immune responder",new Color(.93f,.93f,1,1));
            for(int i=0;i<5;i++){var cell=Visual(actors.transform,"Pooled immune responder "+i,world.bloodEntities.stream.palette.First(s=>s.id=="leukocyte").mesh,white);patrol.responderBodies[i]=cell.GetComponent<MeshFilter>();cell.gameObject.SetActive(false);}
            var signal=world.transform.Find("Immune patrol reverse signal");if(signal)UnityEngine.Object.DestroyImmediate(signal.gameObject);patrol.signal=Line(world.transform,"Immune patrol reverse signal",rig.captureTether.sharedMaterial,.003f);
            world.targets=world.targets.Where(t=>t&&!t.targetId.StartsWith("patrol-")).Concat(targets).ToArray();root.SetActive(false);
            foreach(var p in world.hemostasisPuzzle.platelets){if(!p.Placed)p.gameObject.SetActive(false);}world.bloodEntities.stream.uniformCirculation=true;
            foreach(var obj in new UnityEngine.Object[]{world,platelets,patrol,rig})EditorUtility.SetDirty(obj);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,episode=10,focuses=6,respondersPerFocus=counts,responderPool=5,reuse="Mission/Mover/BloodFlow/WBC mesh/LayerInspection/UI/Reverse/HemostasisPuzzle",scannerActivation=true,tutorialVirusHits=1,lateVirusHits=3,spikeGroups=3,platelets="actual blood-pool leases / launch to original wound",cameraForced=false},Formatting.Indented));
        }
        static JourneyTarget Target(Transform parent,string id,string label,JourneyWorld world){var go=new GameObject(id,typeof(SphereCollider));go.layer=LayerMask.NameToLayer("JourneyTarget");go.transform.SetParent(parent,false);var target=go.AddComponent<JourneyTarget>();target.targetId=id;target.label=label;target.mission=world.mission;return target;}
        static Renderer Visual(Transform parent,string name,Mesh mesh,Material material){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return r;}
        static LineRenderer Line(Transform parent,string name,Material material,float width){var go=new GameObject(name,typeof(LineRenderer));go.transform.SetParent(parent,false);var line=go.GetComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.widthMultiplier=width;line.enabled=false;return line;}
        static Material Material(string name,Color color){string path=Folder+"/"+name.Replace(" ","_")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Simple Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Surface",0);JourneyGeometry.Save(m,path);return AssetDatabase.LoadAssetAtPath<Material>(path);}
    }
}

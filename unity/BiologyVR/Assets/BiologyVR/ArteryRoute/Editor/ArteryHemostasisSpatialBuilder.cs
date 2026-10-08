using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryHemostasisSpatialBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/HemostasisSpatialV8";
        [MenuItem("Biology VR/Build Spatial Hemostasis On Existing Wound")]
        public static void Current()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Apply(w,GameObject.Find("POLISH — anatomy, chunks and materials").transform);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
        }
        static Mesh Normalize(Mesh source,string name)
        {
            var mesh=UnityEngine.Object.Instantiate(source);mesh.name=name;var vertices=mesh.vertices;var b=mesh.bounds;float size=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
            for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-b.center)*(.27f/size);mesh.vertices=vertices;mesh.RecalculateBounds();
            JourneyGeometry.Save(mesh,Folder+"/"+name+".asset");return AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");
        }
        public static void Apply(JourneyWorld w,Transform parent)
        {
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            ConfigureRecovery(w,parent);
            ConfigureFinal(w,parent);
            var old=parent.GetComponent<JourneyHemostasisPuzzle>();if(old)
            {
                w.hemostasisPuzzle=old;
                if(old.platelets.Length==6)
                {
                    var pieces=new JourneyHemostasisPiece[9];Array.Copy(old.platelets,pieces,6);var objects=new GameObject[9];Array.Copy(w.platelets,objects,6);
                    for(int i=6;i<9;i++){objects[i]=UnityEngine.Object.Instantiate(w.platelets[0],w.platelets[0].transform.parent);objects[i].name="Reserve hand platelet "+i;pieces[i]=objects[i].GetComponent<JourneyHemostasisPiece>();pieces[i].index=i;pieces[i].puzzle=old;objects[i].SetActive(false);}
                    old.platelets=pieces;w.platelets=objects;
                }
                foreach(string id in new[]{"clot","open-flow"}){var t=w.Find(id);var c=t.GetComponent<SphereCollider>();if(c){c.center=Vector3.zero;c.radius=.24f/Mathf.Max(.001f,Mathf.Abs(t.transform.lossyScale.x));}}
                EditorUtility.SetDirty(old);EditorUtility.SetDirty(w);return;
            }
            var puzzle=parent.gameObject.AddComponent<JourneyHemostasisPuzzle>();puzzle.world=w;w.hemostasisPuzzle=puzzle;
            puzzle.resting=Normalize(w.art.restingPlateletMesh,"Resting_HandPlatelet");puzzle.activated=Normalize(w.art.activatedPlateletMesh,"Activated_HandPlatelet");
            puzzle.restingMaterials=w.art.restingPlateletMaterials;puzzle.activatedMaterials=w.art.activatedPlateletMaterials;
            if(w.platelets.Length==6)
            {var array=new GameObject[9];Array.Copy(w.platelets,array,6);for(int i=6;i<9;i++){array[i]=UnityEngine.Object.Instantiate(w.platelets[0],w.platelets[0].transform.parent);array[i].name="Reserve hand platelet "+i;}w.platelets=array;}
            puzzle.platelets=new JourneyHemostasisPiece[w.platelets.Length];
            for(int i=0;i<w.platelets.Length;i++)
            {
                var go=w.platelets[i];go.transform.localScale=Vector3.one;go.GetComponent<MeshFilter>().sharedMesh=puzzle.resting;go.GetComponent<Renderer>().sharedMaterials=puzzle.restingMaterials;
                var collider=go.GetComponent<SphereCollider>();if(!collider)collider=go.AddComponent<SphereCollider>();collider.radius=.16f;collider.center=Vector3.zero;
                go.layer=LayerMask.NameToLayer("JourneyTarget");SetupGrab(go,collider);
                var item=go.AddComponent<JourneyHemostasisPiece>();item.puzzle=puzzle;item.index=i;puzzle.platelets[i]=item;go.SetActive(false);
            }
            var root=new GameObject("Spatial fibrin anchors — same WALL_A");root.transform.SetParent(w.locationRoots[w.mover.path.locationIds[11-3]].transform,false);
            float s=w.mover.path.Anchor(11)+5;var normal=-w.mover.path.Right(s);var up=w.mover.path.Up(s);var forward=w.mover.path.Forward(s);
            puzzle.starts=new Transform[3];puzzle.ends=new Transform[3];puzzle.strands=new JourneyHemostasisPiece[3];puzzle.fibres=new LineRenderer[3];
            var ivory=AssetDatabase.LoadAssetAtPath<Material>(ArteryVisualV8.Folder+"/Fibrin_PearlIvory_Visual_V2.mat")??AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Fibrin_PearlIvory.mat");
            for(int i=0;i<3;i++)
            {
                puzzle.starts[i]=Marker(root.transform,"Fibrin start "+i,w.woundSite.position+normal*.32f-forward*.62f+up*((i-1)*.48f),ivory,.08f,false).transform;
                puzzle.ends[i]=Marker(root.transform,"Fibrin end "+i,w.woundSite.position+normal*.32f+forward*.62f+up*((1-i)*.48f),ivory,.11f,false).transform;
                var go=Marker(root.transform,"Physical fibrin end handle "+i,puzzle.starts[i].position,ivory,.12f,true);
                var sphere=go.GetComponent<SphereCollider>();sphere.radius=1.1f;
                SetupGrab(go,sphere);var item=go.AddComponent<JourneyHemostasisPiece>();item.puzzle=puzzle;item.index=i;item.strand=true;puzzle.strands[i]=item;
                var line=ArteryEpisodeViewsBuilder.Ring(root.transform,"Physical fibrin strand "+i,.1f,new Color(1,.87f,.65f),false);line.useWorldSpace=true;line.loop=false;line.positionCount=2;line.widthMultiplier=.013f;line.enabled=false;puzzle.fibres[i]=line;
                go.SetActive(false);puzzle.starts[i].gameObject.SetActive(false);puzzle.ends[i].gameObject.SetActive(false);
            }
            var factor=Marker(root.transform,"Available factor — calcium",w.woundSite.position+normal*.8f+up*.9f+forward*.7f,ivory,.20f,true);
            var target=factor.AddComponent<JourneyTarget>();target.targetId="factor-calcium";target.label="Доступный фактор Ca²⁺";target.mission=w.mission;puzzle.wrongFactor=target;factor.SetActive(false);
            puzzle.label=ArteryAnnotations.Label(parent,"Physical hemostasis instruction","Тромбоциты и фибрин • работа руками");
            var thrombin=w.Find("thrombin");var thrombinContact=thrombin.GetComponent<SphereCollider>();
            if(thrombinContact){thrombinContact.center=Vector3.zero;thrombinContact.radius=.18f/Mathf.Max(.001f,Mathf.Abs(thrombin.transform.lossyScale.x));}
            foreach(string id in new[]{"clot","open-flow"}){var t=w.Find(id);var c=t.GetComponent<SphereCollider>();if(c){c.center=Vector3.zero;c.radius=.24f/Mathf.Max(.001f,Mathf.Abs(t.transform.lossyScale.x));}}
            EditorUtility.SetDirty(w);EditorUtility.SetDirty(puzzle);
        }
        static void ConfigureRecovery(JourneyWorld w,Transform parent)
        {
            var targets=new JourneyTarget[w.debris.Length];
            var mesh=Normalize(w.debris[0].GetComponent<MeshFilter>().sharedMesh,"Recovery_Debris_Mesh");
            for(int i=0;i<targets.Length;i++)
            {
                var go=w.debris[i];go.GetComponent<MeshFilter>().sharedMesh=mesh;go.transform.localScale=Vector3.one;go.layer=LayerMask.NameToLayer("JourneyTarget");
                var target=go.GetComponent<JourneyTarget>();if(!target)target=go.AddComponent<JourneyTarget>();target.targetId="debris";target.label="Debris "+(i+1);target.mission=w.mission;targets[i]=target;
                var sphere=go.GetComponent<SphereCollider>();if(!sphere)sphere=go.AddComponent<SphereCollider>();sphere.center=Vector3.zero;sphere.radius=.18f;go.SetActive(false);
            }
            w.recoveryDebris=targets;
            var list=new System.Collections.Generic.List<JourneyTarget>(w.targets);
            foreach(string id in new[]{"cleanup-mode","plasmin"})
            {
                var target=list.Find(t=>t&&t.targetId==id);if(target)continue;
                var material=AssetDatabase.LoadAssetAtPath<Material>(ArteryVisualV8.Folder+"/Cyan_StudyAccent_Visual_V2.mat")??AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Cyan_StudyAccent.mat");
                var go=Marker(w.locationRoots[w.mover.path.locationIds[14-3]].transform,"Recovery factor — "+id,w.woundSite.position,material,.22f,true);
                target=go.AddComponent<JourneyTarget>();target.targetId=id;target.label=id=="plasmin"?"Плазмин • фибринолиз":"Поле помощи фагоциту";target.mission=w.mission;list.Add(target);go.SetActive(false);
            }
            w.targets=list.ToArray();EditorUtility.SetDirty(w);
        }
        static void ConfigureFinal(JourneyWorld w,Transform parent)
        {
            foreach(string id in new[]{"infected-cell","epitope","antibody-A","antibody-B","antibody-C","neutralized","t-cell"})
            {
                var target=w.Find(id);var contact=target.GetComponent<SphereCollider>();
                if(contact){contact.center=Vector3.zero;contact.radius=(id=="infected-cell"?.42f:id=="epitope"?.14f:.20f)/Mathf.Max(.001f,Mathf.Abs(target.transform.lossyScale.x));}
            }
            var antigen=w.Find("epitope");
            if(!antigen.transform.Find("Final antigen binding site"))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(ArteryVisualV8.Folder+"/Cyan_StudyAccent_Visual_V2.mat")??AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Cyan_StudyAccent.mat");
                var go=Marker(antigen.transform,"Final antigen binding site",antigen.transform.position,material,.12f/Mathf.Max(.001f,antigen.transform.lossyScale.x),false);go.transform.localPosition=Vector3.zero;
            }
            var infected=w.Find("infected-cell");var audio=infected.GetComponent<AudioSource>();if(!audio)audio=infected.gameObject.AddComponent<AudioSource>();
            audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(ArteryScenePolish.Folder+"/ToolModeClick.wav");audio.loop=false;audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.08f;audio.minDistance=.5f;audio.maxDistance=10;
            w.residualTargets=new JourneyTarget[w.remainingComplexes.Length];
            for(int i=0;i<w.remainingComplexes.Length;i++)
            {
                var go=w.remainingComplexes[i];var target=go.GetComponent<JourneyTarget>();if(!target)target=go.AddComponent<JourneyTarget>();target.targetId="remaining";target.label="Остаточный комплекс "+(i+1);target.mission=w.mission;
                var filter=go.GetComponent<MeshFilter>();if(filter){filter.sharedMesh=Normalize(filter.sharedMesh,"Residual_Complex_Mesh");go.transform.localScale=Vector3.one;}
                var contact=go.GetComponent<SphereCollider>();if(!contact)contact=go.AddComponent<SphereCollider>();contact.center=Vector3.zero;contact.radius=.20f/Mathf.Max(.001f,Mathf.Abs(go.transform.lossyScale.x));go.layer=LayerMask.NameToLayer("JourneyTarget");w.residualTargets[i]=target;
            }
            var list=new System.Collections.Generic.List<JourneyTarget>(w.targets);
            var ids=new[]{"check-pressure","check-temperature","check-flow","check-wall","check-virus","check-hemostasis"};
            var captions=new[]{"АД • измерение","Температура • измерение","Поток • измерение","Стенка • Scanner","Вирусная нагрузка • Scanner","Гемостаз • контроль"};
            for(int i=0;i<ids.Length;i++)
            {
                var t=list.Find(x=>x&&x.targetId==ids[i]);if(t)continue;
                var material=AssetDatabase.LoadAssetAtPath<Material>(ArteryVisualV8.Folder+"/Cyan_StudyAccent_Visual_V2.mat")??AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Cyan_StudyAccent.mat");
                var go=Marker(w.locationRoots[w.mover.path.locationIds[16-3]].transform,"Homeostasis sensor — "+ids[i],w.woundSite.position,material,.24f,true);
                t=go.AddComponent<JourneyTarget>();t.targetId=ids[i];t.label=captions[i];t.mission=w.mission;list.Add(t);go.SetActive(false);
            }
            w.targets=list.ToArray();
            EditorUtility.SetDirty(w);
        }
        static GameObject Marker(Transform parent,string name,Vector3 point,Material material,float size,bool contact)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,true);go.transform.position=point;go.transform.localScale=Vector3.one*size;
            go.GetComponent<Renderer>().sharedMaterial=material;go.layer=LayerMask.NameToLayer("JourneyTarget");if(!contact)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static void SetupGrab(GameObject go,Collider contact)
        {
            var rb=go.GetComponent<Rigidbody>();if(!rb)rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
            var grab=go.GetComponent<XRGrabInteractable>();if(!grab)grab=go.AddComponent<XRGrabInteractable>();grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.useDynamicAttach=true;grab.throwOnDetach=false;grab.colliders.Clear();grab.colliders.Add(contact);
        }
    }
}

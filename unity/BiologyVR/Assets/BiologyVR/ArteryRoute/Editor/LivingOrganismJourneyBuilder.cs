using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
    public static class LivingOrganismJourneyBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/LivingOrganismJourney";
        public const string Reports=JourneyGeometry.Root+"/Reports/Polish/LivingOrganismJourney";
        [MenuItem("Biology VR/Install Connected Living Blood And Innate Immunity")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Reports);EditorSceneManager.SaveScene(world.gameObject.scene);
            string backup=Reports+"/Before_Connected_Living_Organism.unity";if(!File.Exists(backup)&&!AssetDatabase.CopyAsset(world.gameObject.scene.path,backup))throw new IOException("Connected journey backup failed");
            var stream=world.GetComponent<JourneyContinuousBloodstream>();var bridge=world.GetComponent<JourneyBloodInteractionPool>();if(!bridge)bridge=world.gameObject.AddComponent<JourneyBloodInteractionPool>();bridge.world=world;bridge.stream=stream;world.bloodEntities=bridge;
            var sphereGo=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphere=sphereGo.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sphereGo);
            var phagFilter=world.phagocyte.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.sharedMesh.vertexCount>0&&!f.name.StartsWith("Promoted blood")&&!f.name.StartsWith("Pooled pseudopodium")).OrderBy(f=>f.sharedMesh.vertexCount).First();
            var phagMesh=Normalize(phagFilter.sharedMesh.vertexCount<12000?phagFilter.sharedMesh:stream.palette.First(p=>p.id=="leukocyte").mesh,"Flow phagocyte");var phagMaterials=phagFilter.GetComponent<Renderer>().sharedMaterials;
            var palette=stream.palette.Where(p=>p.id!="erythrocyte-O2"&&p.id!="erythrocyte-CO2"&&p.id!="phagocyte").ToList();
            foreach(var p in palette){if(p.id=="erythrocyte")p.weight=70;if(p.id=="leukocyte"){p.weight=3;p.minDiameter=.28f;p.maxDiameter=.38f;}}
            palette.Add(new JourneyContinuousBloodstream.VisualSpecies{id="phagocyte",mesh=phagMesh,materials=phagMaterials,weight=1,minDiameter=.34f,maxDiameter=.44f});stream.palette=palette.ToArray();EditorUtility.SetDirty(stream);
            var slotList=new List<JourneyBloodInteractiveSlot>();var phags=new List<JourneyPhagocyteEntity>();
            foreach(string type in new[]{"rbc","leukocyte","platelet","plasma","phagocyte"})
            {
                var target=world.Find(type);if(!target)throw new InvalidOperationException("Existing target missing: "+type);
                slotList.Add(BuildSlot(target,type,sphere,world,phags));
            }
            var oldAliases=world.transform.Find("Additional pooled phagocyte proxies");if(oldAliases)UnityEngine.Object.DestroyImmediate(oldAliases.gameObject);
            var aliases=new GameObject("Additional pooled phagocyte proxies");aliases.transform.SetParent(world.transform,false);
            for(int i=0;i<2;i++){var go=new GameObject("Interactive phagocyte slot "+(i+1));go.transform.SetParent(aliases.transform,false);var target=go.AddComponent<JourneyTarget>();target.targetId="phagocyte";target.label="Фагоцит";target.mission=world.mission;slotList.Add(BuildSlot(target,"phagocyte",sphere,world,phags));}
            bridge.slots=slotList.ToArray();EditorUtility.SetDirty(bridge);
            AddPhagocyteTheory(world);
            BuildAnalysis(world,sphere);
            BuildImmunity(world,sphere,phags.ToArray());
            foreach(var item in world.hemostasisPuzzle.platelets){var state=item.GetComponent<JourneyPlateletEntity>();if(!state)state=item.gameObject.AddComponent<JourneyPlateletEntity>();state.body=item.GetComponent<MeshFilter>();state.resting=world.hemostasisPuzzle.resting;state.activated=world.hemostasisPuzzle.activated;EditorUtility.SetDirty(state);}
            EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(Reports+"/Authoring.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,bloodPool=stream.flow.cells.Length,interactiveProxyLimit=bridge.slots.Length,phagocyteProxyLimit=phags.Count,virusPool=world.immunity.viruses.Length,firstStageRequired=world.research.RequiredCount,nestedMolecules=true,ambientLargeGasObjects=false,tutorial="Scene08 / one virus / one successful cell launch and engulf",waves="Scene09 and Scene15 / same cell manipulation / existing ViralLoad",aimOriginsPreserved=true,InputActionsPreserved=true,hardwareVerified=false},Formatting.Indented));
        }
        static JourneyBloodInteractiveSlot BuildSlot(JourneyTarget target,string type,Mesh sphere,JourneyWorld world,List<JourneyPhagocyteEntity> phags)
        {
            var old=target.transform.Find("Promoted blood body");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var limb in target.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==target.transform&&t.name.StartsWith("Pooled pseudopodium")).ToArray())UnityEngine.Object.DestroyImmediate(limb.gameObject);
            var renderers=target.GetComponentsInChildren<Renderer>(true);var visible=renderers.Select(r=>r.enabled).ToArray();var body=Visual(target.transform,"Promoted blood body",sphere,renderers.Length>0?renderers[0].sharedMaterials:new[]{world.art.restingPlateletMaterials[0]});body.SetActive(false);
            var contact=target.GetComponent<SphereCollider>();if(!contact)contact=target.gameObject.AddComponent<SphereCollider>();contact.center=Vector3.zero;contact.radius=.2f;
            var rb=target.GetComponent<Rigidbody>();if(!rb)rb=target.gameObject.AddComponent<Rigidbody>();rb.useGravity=false;rb.isKinematic=true;
            var grab=target.GetComponent<XRGrabInteractable>();if(!grab)grab=target.gameObject.AddComponent<XRGrabInteractable>();grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.throwOnDetach=false;grab.useDynamicAttach=true;grab.colliders.Clear();grab.colliders.Add(contact);target.gameObject.layer=LayerMask.NameToLayer("JourneyTarget");
            var slot=new JourneyBloodInteractiveSlot{target=target,type=type,body=body.GetComponent<MeshFilter>(),contact=contact,grab=grab,originalScale=target.transform.localScale,legacyRenderers=renderers,legacyVisibility=visible};
            if(type=="phagocyte")
            {
                var phag=target.GetComponent<JourneyPhagocyteEntity>();if(!phag)phag=target.gameObject.AddComponent<JourneyPhagocyteEntity>();phag.target=target;phag.body=body.GetComponent<Renderer>();phag.pseudopodia=new Transform[6];
                for(int i=0;i<6;i++){var limb=Visual(target.transform,"Pooled pseudopodium "+i,sphere,body.GetComponent<Renderer>().sharedMaterials);limb.SetActive(false);phag.pseudopodia[i]=limb.transform;}phags.Add(phag);EditorUtility.SetDirty(phag);
            }
            if(type=="platelet"){var state=target.GetComponent<JourneyPlateletEntity>();if(!state)state=target.gameObject.AddComponent<JourneyPlateletEntity>();state.body=slot.body;state.resting=world.hemostasisPuzzle.resting;state.activated=world.hemostasisPuzzle.activated;EditorUtility.SetDirty(state);}
            EditorUtility.SetDirty(target);EditorUtility.SetDirty(contact);EditorUtility.SetDirty(grab);return slot;
        }
        static void AddPhagocyteTheory(JourneyWorld world)
        {
            var data=ScriptableObject.CreateInstance<JourneyResearchTheory>();data.id="phagocyte";data.displayName="Фагоцит";data.requiredForCompletion=true;data.professorComment="Эта клетка относится к лейкоцитам и умеет поглощать чужеродные частицы. Запомни её свойства.";
            data.pages=new[]{Page("Связь с лейкоцитами","• Фагоциты — иммунные клетки с выраженной фагоцитарной активностью.\n• Они относятся к лейкоцитам; это не отдельный от них класс.\n• Здесь показан игровой архетип такой клетки.","ЛЕЙКОЦИТЫ → ФАГОЦИТАРНАЯ АКТИВНОСТЬ"),Page("Фагоцитоз","• Клетка распознаёт чужеродный материал.\n• Мембрана образует псевдоподии и окружает частицу.\n• После поглощения материал разрушается внутри.","РАСПОЗНАТЬ → ОКРУЖИТЬ → ПОГЛОТИТЬ"),Page("Контекст защиты","• Это один из механизмов врождённой защиты.\n• Не все лейкоциты одинаково фагоцитируют.\n• Противовирусный иммунитет включает и другие механизмы.","ВРОЖДЁННАЯ И АДАПТИВНАЯ ЗАЩИТА")};
            JourneyGeometry.Save(data,Folder+"/PhagocyteTheory.asset");var target=world.Find("phagocyte");var research=target.GetComponent<JourneyResearchableObject>();if(!research)research=target.gameObject.AddComponent<JourneyResearchableObject>();research.target=target;research.manager=world.research;research.theory=AssetDatabase.LoadAssetAtPath<JourneyResearchTheory>(Folder+"/PhagocyteTheory.asset");research.ResetRun();world.research.objects=world.research.objects.Where(o=>o&&o.theory.id!="phagocyte").Append(research).ToArray();EditorUtility.SetDirty(research);EditorUtility.SetDirty(world.research);
            var leu=world.research.Find("leukocyte").theory;leu.pages[1].text="• Лейкоциты включают различные иммунные клетки.\n• Часть обладает выраженной фагоцитарной активностью.\n• Лимфоциты участвуют в специфическом ответе.\n• Клетки координируют воспаление.";EditorUtility.SetDirty(leu);
        }
        static void BuildAnalysis(JourneyWorld world,Mesh sphere)
        {
            var analysis=world.GetComponent<JourneyErythrocyteAnalysis>();if(!analysis)analysis=world.gameObject.AddComponent<JourneyErythrocyteAnalysis>();analysis.world=world;world.analysis=analysis;
            var old=world.transform.Find("Nested erythrocyte analysis");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var root=new GameObject("Nested erythrocyte analysis");root.transform.SetParent(world.transform,false);analysis.diagram=root;
            analysis.molecules=new[]{world.Find("hemoglobin"),world.Find("heme"),world.Find("oxygen"),world.Find("co2")};
            foreach(var target in analysis.molecules){var cargo=target.GetComponent<JourneyBloodCargo>();if(cargo){cargo.enabled=false;EditorUtility.SetDirty(cargo);}target.gameObject.SetActive(false);}
            var hb=world.Find("hemoglobin").GetComponentsInChildren<MeshFilter>(true).First();analysis.hemoglobinCopies=new MeshRenderer[4];
            for(int i=0;i<4;i++){var copy=Visual(root.transform,"Hb content molecule "+i,hb.sharedMesh,hb.GetComponent<Renderer>().sharedMaterials);copy.transform.localPosition=new Vector3((i%2-.5f)*.22f,.06f+(i/2)*.14f,.05f);copy.transform.localScale=Vector3.one*.07f;analysis.hemoglobinCopies[i]=copy.GetComponent<MeshRenderer>();}
            analysis.caption=ArteryAnnotations.Label(root.transform,"Nested content caption","");analysis.caption.fontStyle=FontStyle.Bold;analysis.caption.transform.localPosition=new Vector3(0,.67f,0);root.SetActive(false);EditorUtility.SetDirty(analysis);
        }
        static void BuildImmunity(JourneyWorld world,Mesh sphere,JourneyPhagocyteEntity[] cells)
        {
            var controller=world.GetComponent<JourneyInnateImmunity>();if(!controller)controller=world.gameObject.AddComponent<JourneyInnateImmunity>();controller.world=world;world.immunity=controller;controller.phagocytes=cells;
            var old=world.transform.Find("Pooled innate virus population");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var root=new GameObject("Pooled innate virus population");root.transform.SetParent(world.transform,false);
            var virusMesh=AssetDatabase.LoadAssetAtPath<Mesh>(ArteryScenePolish.Folder+"/Normalized_POLISH_Virus_LOD2.asset");if(!virusMesh)throw new InvalidOperationException("Existing normalized virus mesh required");var material=world.virusPrefab.GetComponentsInChildren<Renderer>(true).First().sharedMaterials;
            controller.viruses=new JourneyImmuneVirus[8];
            for(int i=0;i<8;i++)
            {
                var go=Visual(root.transform,"Pooled flow virion "+i,virusMesh,material);go.layer=LayerMask.NameToLayer("JourneyTarget");go.transform.localScale=Vector3.one*.45f;
                var collider=go.AddComponent<SphereCollider>();collider.radius=.5f;collider.isTrigger=true;var target=go.AddComponent<JourneyTarget>();target.targetId=i==0?"virus-study":"incoming-virus";target.label="Вирусная частица";target.mission=world.mission;
                var virus=go.AddComponent<JourneyImmuneVirus>();virus.target=target;virus.contact=collider;virus.body=go.GetComponent<Renderer>();virus.initialScale=go.transform.localScale;controller.viruses[i]=virus;go.SetActive(false);
            }
            var line=new GameObject("Cell launch trajectory preview",typeof(LineRenderer));line.transform.SetParent(root.transform,false);controller.trajectory=line.GetComponent<LineRenderer>();controller.trajectory.useWorldSpace=true;controller.trajectory.widthMultiplier=.003f;controller.trajectory.sharedMaterial=world.mission.GetComponent<JourneyInstrumentRig>().captureTether.sharedMaterial;controller.trajectory.shadowCastingMode=ShadowCastingMode.Off;controller.trajectory.enabled=false;
            controller.status=ArteryAnnotations.Label(root.transform,"Innate process status","");controller.status.fontStyle=FontStyle.Bold;controller.status.rectTransform.sizeDelta=new Vector2(520,90);controller.status.gameObject.SetActive(false);EditorUtility.SetDirty(controller);
        }
        static JourneyResearchPage Page(string heading,string text,string link)=>new JourneyResearchPage{heading=heading,text=text,visualConnection=link};
        static Mesh Normalize(Mesh original,string name){var mesh=UnityEngine.Object.Instantiate(original);var bounds=mesh.bounds;float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));mesh.vertices=mesh.vertices.Select(v=>(v-bounds.center)/Mathf.Max(.001f,size)).ToArray();mesh.name=name;mesh.RecalculateBounds();JourneyGeometry.Save(mesh,Folder+"/"+name.Replace(" ","_")+".asset");return AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name.Replace(" ","_")+".asset");}
        static GameObject Visual(Transform parent,string name,Mesh mesh,Material[] materials){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;return go;}
    }
}

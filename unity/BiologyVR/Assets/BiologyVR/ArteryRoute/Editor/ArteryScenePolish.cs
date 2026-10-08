// RECOVERY: reconstructed from the scene-polish implementation authored in this session.
// Build/capture was interrupted by NTFS errors on Z:. Requires revalidation after project recovery.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryScenePolish
    {
        public const string Folder=JourneyGeometry.Root+"/Generated/Polish";
        public const string ScenePath=JourneyGeometry.Root+"/Scenes/ArteryNarrativeVR_VisualRework.unity";
        const string Imported=JourneyGeometry.Root+"/Generated/VisualRework/BiologyVR_Cell_LODs_Polished.fbx";
        const string ImportedVirus=JourneyGeometry.Root+"/Generated/VisualRework/BiologyVR_Virus_LODs_Polished.fbx";
        const string ImportedFlow=JourneyGeometry.Root+"/Generated/VisualRework/BiologyVR_FlowModel_Polished.fbx";
        static Dictionary<string,Mesh> importedMeshes;
        static Dictionary<Material,Material> variants;
        static Material wall,ruby,pearl,wbcGranules,cream,lipid,cap,cyan;

        [MenuItem("Biology VR/Polish VisualRework Scene")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            if(!File.Exists(BuildNarrativeArtery.ScenePath))throw new IOException("Restore the original narrative scene before applying polish");
            var active=SceneManager.GetActiveScene();if(active.isDirty)EditorSceneManager.SaveScene(active);
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(JourneyGeometry.Root+"/Reports/Polish");
            var source=EditorSceneManager.OpenScene(BuildNarrativeArtery.ScenePath);
            EditorSceneManager.SaveScene(source,ScenePath,true);EditorSceneManager.OpenScene(ScenePath);
            try
            {
                var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();if(!w)throw new InvalidOperationException("Narrative world missing");
                var scene=SceneManager.GetActiveScene();var root=new GameObject("POLISH — anatomy, chunks and materials");
                var authoringLibrary=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="Source asset templates (hidden)");
                if(authoringLibrary)UnityEngine.Object.DestroyImmediate(authoringLibrary);
                var art=root.AddComponent<JourneyArtComposition>();w.art=art;art.world=w;variants=new Dictionary<Material,Material>();importedMeshes=new Dictionary<string,Mesh>();
                if(!w.labRoom)w.labRoom=ArteryReferenceVisualOverhaul.CreateLaboratoryRoom(root.transform,w.mover.path);
                w.labRoom.transform.position=new Vector3(-80,0,-40);
                CreateMaterials();
                ArteryExtendedRoute.Apply(w);
                foreach(var go in scene.GetRootGameObjects())foreach(var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++)if(slots[i])slots[i]=Variant(slots[i]);
                    r.sharedMaterials=slots;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                }
                ConfigureWalls(w,art,root.transform);ConfigureModels(w);ConfigureAnatomy(w,art,root.transform);
                var presentation=root.AddComponent<JourneyArteryPresentation>();presentation.world=w;presentation.wall=art.wallChunks;
                w.plaqueState=ArteryPlaqueStateBuilder.Apply(w,art,root.transform,lipid,cap);
                w.plaqueDetail=ArteryPlaqueDetailBuilder.Apply(w,root.transform,w.plaqueState);
                ArteryFinalFocusBuilder.Apply(w);
                ConfigureFlow(w);ArteryFlowDiversity.Apply(w);ArteryBlenderPlasmaImporter.Apply(w);ConfigureInputs(w);CleanClutter(w);ConfigureLight(w);
                ArteryBranchFlowBuilder.Apply(w,root.transform,wall);
                ArteryEpisodeViewsBuilder.Apply(w,root.transform);
                if(!w.mover.GetComponent<JourneyDesktopPresentation>())w.mover.gameObject.AddComponent<JourneyDesktopPresentation>();
                w.layerInspection=ArteryLayerInspectionBuilder.Apply(w,art,root.transform,wall,new[]{cap,cream,pearl});
                ConfigureWound(w,root.transform);
                ArteryHemostasisBuilder.Apply(w,root.transform);
                ArteryHemostasisSpatialBuilder.Apply(w,root.transform);
                var hud=w.mission.GetComponent<JourneyHud>();if(hud)JourneyHudOverhaul.Apply(hud);
                RepairSerializedUi();
                if(w.mover.teachingOverlay)w.mover.teachingOverlay.gameObject.SetActive(false);
                if(File.Exists(ArteryVisualV8.Folder+"/TextureBake.json"))ArteryVisualV8.Apply();
                if(File.Exists(ArteryCartoonV3.Folder+"/TextureBake.json"))ArteryCartoonV3.Apply();
                if(File.Exists(ArteryCartoonV3.ConceptFolder+"/TextureBake.json"))ArteryCartoonV3.ApplyConcept();
                ArteryStartupReview.Apply();
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                var buildScenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();buildScenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=buildScenes.ToArray();
                File.WriteAllText(JourneyGeometry.Root+"/Reports/Polish/Build.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),scene=ScenePath,materials=variants.Count,wallChunks=art.wallChunks.Length,plaqueZones=w.plaqueState.Zones.Length,layerInspection=root.GetComponent<JourneyLayerInspection>()!=null,leakPool=w.woundState.Cells.Length,blenderRbcLODs=true,scannerAction=w.mission.GetComponent<JourneyInput>().scanAction!=null,hardwareVRTested=false},Formatting.Indented));
                string previousError=JourneyGeometry.Root+"/Reports/Polish/BuildError.txt";if(File.Exists(previousError))File.Delete(previousError);
            }
            catch(Exception e){File.WriteAllText(JourneyGeometry.Root+"/Reports/Polish/BuildError.txt",e.ToString());throw;}
        }
        static void Save(UnityEngine.Object asset,string name){JourneyGeometry.Save(asset,Folder+"/"+name);}
        public static void SavePublic(UnityEngine.Object asset,string name){Save(asset,name);}
        static Material Lit(string name,Color color,float smooth=.3f)
        {
            var shader=Shader.Find("BiologyVR/Soft Inked Cell")??Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new InvalidOperationException("Cell shader missing");
            var m=new Material(shader){name=name,enableInstancing=true};
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);
            m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);
            Save(m,name+".mat");return AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name+".mat");
        }
        static void CreateMaterials()
        {
            ruby=Lit("Ruby_RBC",new Color(.72f,.036f,.060f),.40f);pearl=Lit("Lavender_WhiteCell",new Color(.82f,.77f,.94f),.29f);
            wbcGranules=Lit("Soft_Lavender_Granules",new Color(.68f,.58f,.80f),.28f);
            cream=Lit("Peach_Platelet",new Color(.96f,.67f,.37f),.31f);lipid=Lit("Amber_Lipid",new Color(.92f,.48f,.12f),.32f);
            cap=Lit("Salmon_FibrousCap",new Color(.94f,.51f,.47f),.34f);cyan=Lit("Cyan_StudyAccent",new Color(.06f,.72f,.90f),.38f);
            CreateWallTextures();var shader=Shader.Find("BiologyVR/Soft Mobile Tissue");if(!shader)throw new InvalidOperationException("Soft tissue shader missing");wall=new Material(shader){name="Coral_Endothelium",enableInstancing=true};
            wall.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Endothelium_Base.png"));
            wall.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Endothelium_Normal.png"));
            wall.SetFloat("_Smoothness",.34f);wall.SetFloat("_NormalStrength",.40f);wall.SetFloat("_Reveal",1);
            wall.SetFloat("_AllowWallCutaway",1);
            Save(wall,"Coral_Endothelium.mat");wall=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Coral_Endothelium.mat");
        }
        static void CreateWallTextures()
        {
            ArteryTissueTextures.Bake("Endothelium",0,1024);
        }
        static Material Variant(Material source)
        {
            if(!source||!source.shader)return source;
            if(source==wall||source==ruby||source==pearl||source==cream||source==cap||source==lipid||source==cyan)return source;
            if(variants.TryGetValue(source,out var mat)&&mat)return mat;
            if(source.shader.name=="BiologyVR/Cellular Vessel"||source.shader.name=="BiologyVR/Soft Mobile Tissue")return wall;
            if(!source.HasProperty("_BaseColor"))return source;string n=source.name;var color=source.GetColor("_BaseColor");
            if(n.IndexOf("Bubble",StringComparison.OrdinalIgnoreCase)>=0)
            {
                mat=Lit("Gas_Bubble_OnlyTransparentProp",new Color(.32f,.75f,.88f,.22f),.65f);
                mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=(int)RenderQueue.Transparent;EditorUtility.SetDirty(mat);
            }
            else if(n.IndexOf("RBC",StringComparison.OrdinalIgnoreCase)>=0)mat=ruby;
            else if(n.Contains("Pearl")||n.Contains("White"))mat=pearl;
            else if(n.Contains("Lipid"))mat=lipid;
            else if(n.Contains("Fibrin"))mat=Lit("Fibrin_Ivory",new Color(.98f,.85f,.60f),.28f);
            else{if(n.Contains("Gold"))color=new Color(.98f,.62f,.18f);if(n.Contains("Nucleus")||n.Contains("Violet"))color=new Color(.52f,.29f,.74f);mat=Lit("Soft_"+n.Replace(" ","_"),color,.32f);}
            variants[source]=mat;return mat;
        }
        static void ConfigureWalls(JourneyWorld w,JourneyArtComposition art,Transform root)
        {
            var tube=GameObject.Find("Continuous Main Artery");if(!tube)throw new InvalidOperationException("Main artery missing");
            var mesh=tube.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var ns=mesh.normals;var uv=mesh.uv;var centers=new List<Vector3>();mesh.GetUVs(1,centers);
            var tri=mesh.triangles;var groups=new Dictionary<int,List<int>>();
            for(int i=0;i<tri.Length;i+=3)
            {
                int key=Mathf.FloorToInt((uv[tri[i]].y+uv[tri[i+1]].y+uv[tri[i+2]].y)/3*5.6f/12);
                if(!groups.TryGetValue(key,out var g))groups[key]=g=new List<int>();g.Add(tri[i]);g.Add(tri[i+1]);g.Add(tri[i+2]);
            }
            var renderers=new List<Renderer>();var distances=new List<float>();
            foreach(var pair in groups.OrderBy(p=>p.Key))
            {
                var lookup=new Dictionary<int,int>();var vs=new List<Vector3>();var normals=new List<Vector3>();var tex=new List<Vector2>();var cs=new List<Vector3>();var faces=new List<int>();
                foreach(int old in pair.Value)
                {
                    if(!lookup.TryGetValue(old,out int idx))
                    {
                        idx=vs.Count;lookup[old]=idx;
                        var matrix=root.worldToLocalMatrix*tube.transform.localToWorldMatrix;
                        vs.Add(matrix.MultiplyPoint3x4(vertices[old]));normals.Add(matrix.inverse.transpose.MultiplyVector(ns[old]).normalized);tex.Add(uv[old]);cs.Add(matrix.MultiplyPoint3x4(centers.Count>old?centers[old]:Vector3.zero));
                    }
                    faces.Add(idx);
                }
                string label="Artery visibility chunk "+pair.Key;
                var chunk=new Mesh{name=label};chunk.SetVertices(vs);chunk.SetNormals(normals);chunk.SetUVs(0,tex);chunk.SetUVs(1,cs);chunk.SetTriangles(faces,0);chunk.RecalculateTangents();chunk.RecalculateBounds();Save(chunk,"WallChunk_"+pair.Key+".asset");
                var go=new GameObject(label);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/WallChunk_"+pair.Key+".asset");var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=wall;r.shadowCastingMode=ShadowCastingMode.Off;renderers.Add(r);distances.Add(pair.Key*12+6);
            }
            tube.GetComponent<Renderer>().enabled=false;art.wallChunks=renderers.ToArray();art.chunkDistances=distances.ToArray();w.toneWall=art.wallChunks;
            foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var r in go.GetComponentsInChildren<Renderer>(true))if(r.name.StartsWith("Organic side branch"))r.sharedMaterial=wall;
            art.branchRenderers=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Renderer>(true)).Where(r=>r.name.StartsWith("Organic side branch")).ToArray();
            if(w.repairPatch)
            {
                float s=w.mover.path.Anchor(11)+5;
                var repair=new Material(wall){name="Recovery_Membrane"};repair.SetFloat("_PulseAmplitude",0);repair.SetFloat("_Reveal",0);
                repair.SetTextureScale("_BaseMap",new Vector2(.80f*w.mover.path.Radius(s)/3.5f,3.4f/5.6f));
                repair.SetTextureOffset("_BaseMap",new Vector2(-.40f*w.mover.path.Radius(s)/3.5f,(s-1.7f)/5.6f));
                Save(repair,"Recovery_Membrane.mat");w.repairPatch.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Recovery_Membrane.mat");
            }
        }
        static Mesh MeshFromBlender(string name)
        {
            if(importedMeshes.TryGetValue(name,out var cached)&&cached)return cached;
            bool virus=name.StartsWith("POLISH_Virus"),flow=name=="POLISH_FlowModel";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(flow?ImportedFlow:virus?ImportedVirus:Imported);if(!prefab)throw new InvalidOperationException("Blender FBX not imported");
            var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
            var filter=filters.FirstOrDefault(f=>f.name==name);
            // Unity collapses a single-object FBX to the file-named prefab root.
            if(!filter&&flow&&filters.Length==1)filter=filters[0];
            if(!filter)throw new InvalidOperationException("Missing Blender mesh "+name);
            int expected=virus?0:(flow||name.StartsWith("POLISH_RBC_"))?1:2;
            if(expected>0&&filter.sharedMesh.subMeshCount!=expected)throw new InvalidOperationException(name+" material slot count mismatch");
            var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Normalized_"+name;
            var matrix=prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            if(flow)
            {
                var size=filter.sharedMesh.bounds.size;
                if(size.y>size.z&&size.y>size.x)matrix=Matrix4x4.Rotate(Quaternion.FromToRotation(Vector3.up,Vector3.forward))*matrix;
                else if(size.x>size.z&&size.x>size.y)matrix=Matrix4x4.Rotate(Quaternion.FromToRotation(Vector3.right,Vector3.forward))*matrix;
            }
            var vs=mesh.vertices;var ns=mesh.normals;
            for(int i=0;i<vs.Length;i++){vs[i]=matrix.MultiplyPoint3x4(vs[i]);if(ns.Length>i)ns[i]=matrix.inverse.transpose.MultiplyVector(ns[i]).normalized;}
            mesh.vertices=vs;mesh.normals=ns;mesh.RecalculateBounds();mesh.RecalculateTangents();Save(mesh,mesh.name+".asset");
            return importedMeshes[name]=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Normalized_"+name+".asset");
        }
        static GameObject Visual(string name,Transform parent,Mesh mesh,Material[] mats,float diameter)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=mats;
            float factor=diameter/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));go.transform.localScale=Vector3.one*factor;go.transform.localPosition=-mesh.bounds.center*factor;r.shadowCastingMode=ShadowCastingMode.Off;return go;
        }
        static void ConfigureModels(JourneyWorld w)
        {
            ConfigureTargetLOD(w.targets.First(t=>t.targetId=="rbc"),"POLISH_RBC_LOD",new[]{ruby},.25f);
            ConfigureTargetLOD(w.targets.First(t=>t.targetId=="leukocyte"),"POLISH_WBC_LOD",new[]{pearl,wbcGranules},.30f);
            ConfigureTargetLOD(w.targets.First(t=>t.targetId=="platelet"),"POLISH_Platelet_LOD",new[]{cream,lipid},.10f);
            if(w.flowModel)
            {
                foreach(var r in w.flowModel.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                w.flowModel.localScale=Vector3.one;
                var modelMaterial=new Material(cyan){name="Opaque_Cyan_EducationalTube"};modelMaterial.SetFloat("_Cull",0);modelMaterial.SetFloat("_Smoothness",.25f);Save(modelMaterial,"Opaque_Cyan_EducationalTube.mat");
                Visual("Educational artery model",w.flowModel,MeshFromBlender("POLISH_FlowModel"),new[]{AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Opaque_Cyan_EducationalTube.mat")},1.1f);
                var view=w.flowModel.gameObject.AddComponent<JourneyFlowModelView>();view.mission=w.mission;view.cells=new Transform[6];
                for(int i=0;i<6;i++)view.cells[i]=Visual("Model blood cell "+i,w.flowModel,MeshFromBlender("POLISH_RBC_LOD2"),new[]{ruby},.065f).transform;
                var sphere=w.flowModel.GetComponent<SphereCollider>();if(sphere){sphere.center=Vector3.zero;sphere.radius=.6f;}
            }
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var old=filter.sharedMesh;if(!old||old.name.IndexOf("Virus",StringComparison.OrdinalIgnoreCase)<0)continue;
                bool cutaway=old.name.IndexOf("Cutaway",StringComparison.OrdinalIgnoreCase)>=0;
                var mesh=MeshFromBlender(cutaway?"POLISH_VirusCutaway_LOD0":"POLISH_Virus_LOD2");
                float size=Mathf.Max(old.bounds.size.x,Mathf.Max(old.bounds.size.y,old.bounds.size.z));
                float ratio=size/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
                var t=filter.transform;t.localPosition+=t.localRotation*Vector3.Scale(t.localScale,old.bounds.center-mesh.bounds.center*ratio);t.localScale*=ratio;filter.sharedMesh=mesh;
            }
            foreach(var spec in new[]{("hemoglobin",.32f),("heme",.13f),("oxygen",.07f),("co2",.11f),("plasma",.10f),
                ("virus-study",.36f),("genome",.36f),("capsid",.36f),("epitope",.36f),("embolus",.38f),
                ("phagocyte",.44f),("t-cell",.44f),("infected-cell",.85f),("neutralized",.19f),
                ("antibody-A",.18f),("antibody-B",.18f),("antibody-C",.18f),("thrombin",.12f),("debris",.14f)})
            {
                var target=w.targets.FirstOrDefault(x=>x.targetId==spec.Item1);if(target)FitTargetVisual(target,spec.Item2);
            }
        }
        static void FitTargetVisual(JourneyTarget target,float diameter)
        {
            var renderers=target.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float factor=diameter/Mathf.Max(.001f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
            foreach(Transform child in target.transform)
            {child.position=target.transform.position+(child.position-bounds.center)*factor;child.localScale*=factor;}
            var collider=target.GetComponent<SphereCollider>();if(collider)
            {float scale=Mathf.Max(.001f,Mathf.Abs(target.transform.lossyScale.x));collider.center=Vector3.zero;collider.radius=diameter*.52f/scale;}
        }
        static void ConfigureTargetLOD(JourneyTarget target,string family,Material[] materials,float worldDiameter)
        {
            foreach(var r in target.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var baseMesh=MeshFromBlender(family+"0");float parentScale=Mathf.Max(Mathf.Abs(target.transform.lossyScale.x),Mathf.Abs(target.transform.lossyScale.y),Mathf.Abs(target.transform.lossyScale.z));
            float scale=worldDiameter/Mathf.Max(.001f,parentScale)/Mathf.Max(baseMesh.bounds.size.x,Mathf.Max(baseMesh.bounds.size.y,baseMesh.bounds.size.z));
            var lods=new LOD[3];
            for(int i=0;i<3;i++)
            {
                var mesh=MeshFromBlender(family+i);var v=Visual("Blender visual "+family+i,target.transform,mesh,materials,worldDiameter);
                v.transform.localScale=Vector3.one*scale;v.transform.localPosition=-baseMesh.bounds.center*scale;
                lods[i]=new LOD(i==0?.18f:i==1?.055f:.008f,new[]{v.GetComponent<Renderer>()});
            }
            var group=target.GetComponent<LODGroup>();if(!group)group=target.gameObject.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
            var collider=target.GetComponent<SphereCollider>();
            if(collider){collider.center=Vector3.zero;collider.radius=worldDiameter*.52f/Mathf.Max(parentScale,.001f);}
        }
        static void ConfigureFlow(JourneyWorld w)
        {
            var f=w.GetComponent<JourneyBloodFlow>();if(!f)return;f.composedFlow=true;f.mobileCellLimit=180;f.flowSpan=160;f.flowBehind=74;
            var profile=w.mover.GetComponent<QuestPicoPerformanceProfile>();if(profile)profile.mobileBloodCells=180;
            var list=f.cells.ToList();for(int i=list.Count;i<360;i++){var go=new GameObject("Persistent blood flow element "+i);go.transform.SetParent(f.cells[0].parent,false);go.AddComponent<MeshFilter>();go.AddComponent<MeshRenderer>();list.Add(go.transform);}f.cells=list.ToArray();f.offsets=new float[360];f.lanesX=new float[360];f.lanesY=new float[360];f.species=new string[360];
            var near=MeshFromBlender("POLISH_RBC_LOD1");var far=MeshFromBlender("POLISH_RBC_LOD2");
            for(int i=0;i<f.cells.Length;i++)
            {
                var c=f.cells[i];if(!c)continue;f.offsets[i]=i*48f/f.cells.Length;float angle=i*2.39996f,radial=1.15f+(i%5)*.33f;f.lanesX[i]=Mathf.Cos(angle)*radial;f.lanesY[i]=Mathf.Sin(angle)*radial;
                var filter=c.GetComponent<MeshFilter>();var r=c.GetComponent<Renderer>();float diameter;
                if(i%25==0){filter.sharedMesh=MeshFromBlender("POLISH_WBC_LOD2");r.sharedMaterials=new[]{pearl,wbcGranules};diameter=.42f;f.species[i]="leukocyte";}
                else if(i%7==0){filter.sharedMesh=MeshFromBlender("POLISH_Platelet_LOD2");r.sharedMaterials=new[]{cream,lipid};diameter=.14f;f.species[i]="platelet";}
                else{filter.sharedMesh=i%4==0?near:far;r.sharedMaterial=ruby;diameter=.27f+(i%4)*.035f;f.species[i]="erythrocyte";var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.72f,.036f,.06f)*(.92f+(i%7)*.022f));r.SetPropertyBlock(block);}
                float scale=diameter/Mathf.Max(filter.sharedMesh.bounds.size.x,Mathf.Max(filter.sharedMesh.bounds.size.y,filter.sharedMesh.bounds.size.z));c.localScale=Vector3.one*scale;
            }
        }
        static void ConfigureInputs(JourneyWorld w)
        {
            ConfigureVirusGrab(w);
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var list=settings.FindProperty("layers");int layer=LayerMask.NameToLayer("JourneyTarget");
            if(layer<0)for(int i=8;i<32;i++)if(string.IsNullOrEmpty(list.GetArrayElementAtIndex(i).stringValue)){list.GetArrayElementAtIndex(i).stringValue="JourneyTarget";layer=i;break;}
            if(layer<0)throw new InvalidOperationException("No free target layer");settings.ApplyModifiedProperties();
            foreach(var t in w.targets){foreach(var collider in t.GetComponentsInChildren<Collider>(true))collider.gameObject.layer=layer;t.gameObject.layer=layer;var grab=t.GetComponent<XRGrabInteractable>();if(grab){grab.throwOnDetach=false;grab.selectMode=UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Multiple;grab.colliders.Clear();grab.colliders.AddRange(t.GetComponentsInChildren<Collider>(true));}}
            var input=w.mission.GetComponent<JourneyInput>();input.targetMask=1<<layer;
            ConfigureInteractionCasting(w,layer);
            var actions=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions");
            foreach(var spec in new[]{("Scan","XRI Left Interaction/Activate"),("Tool","XRI Right Interaction/Activate")})
            {string path=Folder+"/"+spec.Item1+"Action.asset";var reference=AssetDatabase.LoadAssetAtPath<InputActionReference>(path);if(!reference){reference=InputActionReference.Create(actions.FindAction(spec.Item2,true));AssetDatabase.CreateAsset(reference,path);}if(spec.Item1=="Scan")input.scanAction=reference;else input.toolAction=reference;}
            EditorUtility.SetDirty(input);
            AttachTool(input,true);AttachTool(input,false);
        }
        static void ConfigureInteractionCasting(JourneyWorld w,int layer)
        {
            int mask=1<<layer;
            foreach(var interactor in w.mover.origin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true))
            {
                if(interactor.nearInteractionCaster is UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.SphereInteractionCaster near)
                {Undo.RecordObject(near,"Include biological targets in near casting");near.physicsLayerMask=near.physicsLayerMask.value|mask;EditorUtility.SetDirty(near);}
                if(interactor.farInteractionCaster is UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.CurveInteractionCaster far)
                {Undo.RecordObject(far,"Include biological targets in far casting");far.raycastMask=far.raycastMask.value|mask;EditorUtility.SetDirty(far);}
            }
        }
        static void ConfigureVirusGrab(JourneyWorld w)
        {
            var target=w.targets.FirstOrDefault(t=>t&&t.targetId=="virus-study");if(!target)return;
            var body=target.GetComponent<Rigidbody>();if(!body)body=target.gameObject.AddComponent<Rigidbody>();
            body.isKinematic=true;body.useGravity=false;
            var grab=target.GetComponent<XRGrabInteractable>();if(!grab)grab=target.gameObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach=false;grab.useDynamicAttach=true;
            grab.movementType=UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Kinematic;
            grab.selectMode=UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Multiple;
            grab.colliders.Clear();grab.colliders.AddRange(target.GetComponentsInChildren<Collider>(true));
            EditorUtility.SetDirty(target);EditorUtility.SetDirty(grab);EditorUtility.SetDirty(body);
        }
        [MenuItem("Biology VR/Repair XR Target Casting")]
        public static void RepairInteractionCasting()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Repair the authored scene in Edit Mode");
            var w=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();int layer=LayerMask.NameToLayer("JourneyTarget");
            if(!w||!w.mover||!w.mover.origin||layer<0)throw new InvalidOperationException("Open the authored artery journey scene with JourneyTarget layer");
            ConfigureInteractionCasting(w,layer);
            ConfigureVirusGrab(w);
            EditorSceneManager.MarkSceneDirty(w.gameObject.scene);
            EditorSceneManager.SaveScene(w.gameObject.scene);
        }
        static void AttachTool(JourneyInput input,bool scanner)
        {
            var controller=scanner?input.scanner:input.bioTool;if(!controller)return;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(BuildArteryVrScene.Root+"/Imported/Visual/HybridArteryVisual.prefab");
            if(!source)return;
            string templateName=scanner?"AB_Preview_Scanner":"AB_Preview_BioTool";
            var template=source.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name==templateName);
            if(!template)return;
            var go=new GameObject(scanner?"Scientific scanner — controller visual":"BioTool — controller visual");go.transform.SetParent(controller,false);
            go.transform.localPosition=new Vector3(0,.025f,.055f);go.transform.localRotation=Quaternion.Euler(0,180,0);go.transform.localScale=Vector3.one*.45f;
            go.AddComponent<MeshFilter>().sharedMesh=template.sharedMesh;var renderer=go.AddComponent<MeshRenderer>();var original=template.GetComponent<Renderer>();
            renderer.sharedMaterials=original.sharedMaterials.Select(Variant).ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;
            var aim=new GameObject(scanner?"Scanner aim":"BioTool aim").transform;aim.SetParent(controller,false);aim.localPosition=new Vector3(0,.025f,.17f);
            if(scanner)input.scanner=aim;else input.bioTool=aim;
        }
        static void CleanClutter(JourneyWorld w)
        {
            var old=GameObject.Find("REFERENCE LOOK — embedded anatomy and station accents");if(old){foreach(Transform child in old.transform)if(!w.labRoom||child.gameObject!=w.labRoom)child.gameObject.SetActive(false);var control=old.GetComponent<ArteryReferenceVisualController>();if(control)control.enabled=false;}
            foreach(var root in w.locationRoots)foreach(var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if(r.GetComponentInParent<JourneyTarget>())continue;
                if((w.clot&&r.transform.IsChildOf(w.clot))||(w.fibrin&&r.transform.IsChildOf(w.fibrin.transform)))continue;
                bool legacyDeck=r.name.StartsWith("AB_S")||r.name.StartsWith("HD_S");
                legacyDeck|=r.name.Contains("S12_Approaching")||r.name.Contains("S14_Macrophage")||r.name.Contains("S14_Neutrophil")||r.name.Contains("S14_Healing")||r.name.Contains("S11_Leak");
                bool background=r.name.Contains("RBC_Flow")||r.name.Contains("Rare_Leukocyte")||r.name.Contains("Resting_Platelet")||r.name.Contains("PlasmaDust")||r.name.Contains("Plasma_Solute");
                if(legacyDeck||background)r.gameObject.SetActive(false);
            }
            foreach(var id in new[]{"layers","flow","small-radius","large-radius","pressure","normal-flow","plaque-flow","pulse-mode","attract-mode","immune-mode","pressure-low","pressure-stable","pressure-return","tone-mode","tone","insufficient","excessive","optimal","open-flow","remaining","vitals","homeostasis"})
            {var t=w.targets.FirstOrDefault(x=>x.targetId==id);if(t)foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
        }
        static void ConfigureLight(JourneyWorld w)
        {
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))light.enabled=false;
            var key=new GameObject("Polish — soft warm key").AddComponent<Light>();key.type=LightType.Directional;key.color=new Color(1,.87f,.79f);key.intensity=1;key.shadows=LightShadows.None;key.transform.SetParent(w.mover.origin.transform,false);key.transform.localRotation=Quaternion.Euler(36,24,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.36f,.30f,.34f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=18;RenderSettings.fogEndDistance=54;RenderSettings.fogColor=new Color(.62f,.32f,.36f);
            var camera=w.mover.viewCamera;camera.nearClipPlane=.055f;camera.farClipPlane=65;camera.backgroundColor=RenderSettings.fogColor;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        }
        static void RepairSerializedUi()
        {
            foreach(var graphic in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
                if(graphic&&!graphic.GetComponent<CanvasRenderer>())graphic.gameObject.AddComponent<CanvasRenderer>();
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if(canvas.gameObject.name=="CONTINUE"||canvas.gameObject.name=="Progress fill")
                {if(!canvas.GetComponent<CanvasRenderer>())canvas.gameObject.AddComponent<CanvasRenderer>();}
            }
        }
        static Transform Anchor(Transform parent,JourneyWorld w,string name,float s,float angle,float offset)
        {
            var path=w.mover.path;var go=new GameObject(name);go.transform.SetParent(parent,false);var radial=path.Right(s)*Mathf.Cos(angle)+path.Up(s)*Mathf.Sin(angle);go.transform.SetPositionAndRotation(path.Centre(s)+radial*(path.Radius(s)+offset),Quaternion.LookRotation(-radial,path.Forward(s)));return go.transform;
        }
        static Mesh CurvedPatch(JourneyWorld w,float s,float halfLength,float halfAngle,float radialOffset,float bulge=0)
        {
            var path=w.mover.path;const int rows=20,cols=18;var vs=new List<Vector3>();var tex=new List<Vector2>();var triangles=new List<int>();
            for(int j=0;j<=rows;j++)for(int i=0;i<=cols;i++)
            {
                float u=i/(float)cols,v=j/(float)rows,x=u*2-1,y=v*2-1;
                float dx=x*Mathf.Sqrt(1-y*y*.5f),dy=y*Mathf.Sqrt(1-x*x*.5f);
                float d=s+dy*halfLength,a=dx*halfAngle;
                float bump=bulge*Mathf.Pow(Mathf.Max(0,1-dx*dx-dy*dy),.8f);
                var radial=path.Right(d)*Mathf.Cos(a)+path.Up(d)*Mathf.Sin(a);vs.Add(path.Centre(d)+radial*(path.Radius(d)+radialOffset-bump));tex.Add(new Vector2(dx*.5f+.5f,dy*.5f+.5f));
                if(i<cols&&j<rows){int k=j*(cols+1)+i;triangles.AddRange(new[]{k,k+cols+1,k+cols+2,k,k+cols+2,k+1});}
            }
            var mesh=new Mesh();mesh.SetVertices(vs);mesh.SetUVs(0,tex);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();return mesh;
        }
        static GameObject Patch(JourneyWorld w,Transform parent,string name,float s,float length,float angle,float offset,Material material,float bulge=0)
        {
            var mesh=CurvedPatch(w,s,length,angle,offset,bulge);mesh.name=name;
            var vertices=mesh.vertices;var centers=new List<Vector3>();var uv=mesh.uv;
            for(int i=0;i<vertices.Length;i++){vertices[i]=parent.InverseTransformPoint(vertices[i]);centers.Add(parent.InverseTransformPoint(w.mover.path.Centre(s+(uv[i].y-.5f)*2*length)));}
            mesh.vertices=vertices;mesh.SetUVs(1,centers);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            Save(mesh,name+".asset");var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        static void ConfigureAnatomy(JourneyWorld w,JourneyArtComposition art,Transform root)
        {
            var anchors=new List<Transform>();var ids=new List<string>();void Bind(string id,Transform a){ids.Add(id);anchors.Add(a);}
            var path=w.mover.path;float s=path.Anchor(6)+4;var plaque=w.targets.First(t=>t.targetId=="plaque");foreach(var r in plaque.GetComponentsInChildren<Renderer>(true))r.enabled=false;plaque.transform.localScale=Vector3.one;
            var pr=new GameObject("Embedded plaque wall profile");pr.transform.SetParent(w.locationRoots[2].transform,false);
            var embeddedCap=new Material(wall){name="Plaque_EmbeddedCap"};embeddedCap.SetColor("_PatchColor",new Color(.94f,.58f,.46f));embeddedCap.SetFloat("_PatchMask",1);
            embeddedCap.SetTextureScale("_BaseMap",new Vector2(.9f*path.Radius(s)/3.5f,3.7f/5.6f));embeddedCap.SetTextureOffset("_BaseMap",new Vector2(-.45f*path.Radius(s)/3.5f,(s-1.85f)/5.6f));Save(embeddedCap,"Plaque_EmbeddedCap.mat");
            Patch(w,pr.transform,"Plaque_Cap",s,1.85f,.45f,-.012f,AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Plaque_EmbeddedCap.mat"),.75f);
            var embeddedCore=new Material(wall){name="Plaque_EmbeddedCore"};embeddedCore.SetColor("_PatchColor",new Color(.96f,.55f,.19f));embeddedCore.SetFloat("_PatchMask",1);Save(embeddedCore,"Plaque_EmbeddedCore.mat");
            Patch(w,pr.transform,"Plaque_Exposed_Core",s-.3f,.85f,.22f,-.45f,AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Plaque_EmbeddedCore.mat"),.36f);
            var sourceLibrary=AssetDatabase.LoadAssetAtPath<GameObject>(BuildArteryVrScene.Root+"/Imported/Visual/HybridArteryVisual.prefab");
            var foam=sourceLibrary?sourceLibrary.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name.Contains("S06_FoamCell")):null;
            if(foam&&foam.GetComponent<Renderer>())
            {
                var foamMaterials=foam.GetComponent<Renderer>().sharedMaterials.Select(Variant).ToArray();
                for(int i=0;i<3;i++)
                {
                    float d=s-.70f+i*.38f,a=(i-1)*.09f;var radial=path.Right(d)*Mathf.Cos(a)+path.Up(d)*Mathf.Sin(a);
                    var cell=Visual("Embedded foam cell "+i,pr.transform,foam.sharedMesh,foamMaterials,.17f);
                    cell.transform.position=path.Centre(d)+radial*(path.Radius(d)-.85f)-cell.transform.TransformVector(foam.sharedMesh.bounds.center);
                }
            }
            Bind("plaque",Anchor(root,w,"Plaque scanner surface",s,0,-.75f));Bind("cap",Anchor(root,w,"Cap tool contact",s+.8f,0,-.65f));Bind("lipid",Anchor(root,w,"Lipid target contact",s-.3f,0,-.85f));
            var layer=new GameObject("Integrated arterial wall teaching cutaway");layer.transform.SetParent(w.locationRoots[0].transform,false);art.layerCutaway=layer;s=path.Anchor(4)+3;
            foreach(var spec in new[]{("endothelium",wall,1.65f,.44f,-.012f),("intima",cap,1.42f,.37f,-.06f),("media",cream,1.18f,.29f,-.11f),("adventitia",pearl,.92f,.21f,-.16f)})
            {Patch(w,layer.transform,"WallLayer_"+spec.Item1,s,spec.Item3,spec.Item4,spec.Item5,spec.Item2);Bind(spec.Item1,Anchor(root,w,"Scan wall "+spec.Item1,s+spec.Item3-.20f,spec.Item4-.06f,spec.Item5-.025f));var t=w.targets.First(x=>x.targetId==spec.Item1);foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
            var compare=new GameObject("Scene03 activated platelet comparison");compare.transform.SetParent(w.locationRoots[0].transform,false);Visual("Activated platelet",compare.transform,MeshFromBlender("POLISH_ActivatedPlatelet_LOD0"),new[]{cream,lipid},.23f);compare.transform.position=path.Offset(path.Anchor(3)+1.2f,.8f,-.18f);art.plateletComparison=compare;
            art.attachmentIds=ids.ToArray();art.wallAttachments=anchors.ToArray();if(w.clot)w.clot.position=w.woundSite.position-path.Right(path.Anchor(11)+5)*.25f;if(w.fibrin&&w.clot)w.fibrin.transform.position=w.clot.position-path.Right(path.Anchor(11)+5)*.08f;
            art.restingPlateletMesh=MeshFromBlender("POLISH_Platelet_LOD1");art.activatedPlateletMesh=MeshFromBlender("POLISH_ActivatedPlatelet_LOD1");
            foreach(var p in w.platelets)if(p){var filter=p.GetComponent<MeshFilter>();if(filter)filter.sharedMesh=art.restingPlateletMesh;var r=p.GetComponent<Renderer>();if(r)r.sharedMaterials=new[]{cream,lipid};}
            if(w.clot)foreach(var r in w.clot.GetComponentsInChildren<Renderer>(true))
            {
                if(r.name.IndexOf("Platelet",StringComparison.OrdinalIgnoreCase)<0)continue;
                var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=i==0?cream:lipid;r.sharedMaterials=slots;
            }
            var backing=GameObject.Find("WALL_A_Subendothelial_Matrix");
            if(backing&&backing.GetComponent<Renderer>())
            {
                float woundS=path.Anchor(11)+5;
                var matrix=new Material(wall){name="Wound_Subendothelial_Tissue"};matrix.SetFloat("_PulseAmplitude",0);matrix.SetFloat("_PatchMask",1);matrix.SetColor("_PatchColor",new Color(.72f,.34f,.31f));
                matrix.SetTextureScale("_BaseMap",new Vector2(.80f*path.Radius(woundS)/3.5f,3.4f/5.6f));matrix.SetTextureOffset("_BaseMap",new Vector2(-.40f*path.Radius(woundS)/3.5f,(woundS-1.7f)/5.6f));
                Save(matrix,"Wound_Subendothelial_Tissue.mat");backing.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Wound_Subendothelial_Tissue.mat");
            }
            foreach(var id in new[]{"fibrin","clot","healed-wall","repair"})
            {var t=w.targets.FirstOrDefault(x=>x.targetId==id);if(t)foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
        }
        static void ConfigureWound(JourneyWorld w,Transform parent)
        {
            var pool=new Transform[9];var mesh=MeshFromBlender("POLISH_RBC_LOD2");
            for(int i=0;i<pool.Length;i++)pool[i]=Visual("WALL_A outward blood cell "+i,parent,mesh,new[]{ruby},.17f+(i%3)*.012f).transform;
            var backing=GameObject.Find("WALL_A_Subendothelial_Matrix");
            var collagen=GameObject.Find("WALL_A exposed collagen scaffold");
            var state=parent.gameObject.AddComponent<JourneyWoundState>();w.woundState=state;
            var diagnostics=new[]{"wound","leak","repair","healed-wall","clot","fibrin"}.Select(id=>w.targets.First(t=>t.targetId==id)).ToArray();
            foreach(var r in diagnostics.First(t=>t.targetId=="leak").GetComponentsInChildren<Renderer>(true))r.enabled=false;
            state.Configure(w,pool,backing?backing.GetComponent<Renderer>():null,collagen,diagnostics);
        }
        static void CombineAssembly(Transform root,string name)
        {
            if(!root)return;
            var groups=new Dictionary<Material,List<CombineInstance>>();var oldRenderers=new List<Renderer>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var r=f.GetComponent<Renderer>();if(!r||!r.enabled||!f.sharedMesh)continue;
                var slots=r.sharedMaterials;
                for(int i=0;i<f.sharedMesh.subMeshCount&&i<slots.Length;i++)
                {
                    var mat=slots[i];if(!mat)continue;
                    if(!groups.TryGetValue(mat,out var list))groups[mat]=list=new List<CombineInstance>();
                    list.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});
                }
                oldRenderers.Add(r);
            }
            int index=0;
            foreach(var group in groups)
            {
                var mesh=new Mesh{name=name+"_"+index,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,true);mesh.RecalculateBounds();
                string file=name+"_"+index+".asset";Save(mesh,file);
                var go=new GameObject(name+" combined "+index++);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+file);var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=group.Key;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            }
            foreach(var r in oldRenderers)r.enabled=false;
        }
    }
}

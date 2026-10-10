using System;
using System.Collections.Generic;
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
    public static class LivingBloodstreamBuilder
    {
        const string Folder=JourneyGeometry.Root+"/Generated/LivingBloodstream";
        [MenuItem("Biology VR/Stage 2 Compose Living Background Bloodstream")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();var flow=world.GetComponent<JourneyBloodFlow>();
            if(flow.cells.Length!=360)throw new InvalidOperationException("Expected existing 360-object pool");
            foreach(var cell in flow.cells)
                if(cell.GetComponentsInChildren<Collider>(true).Length>0||cell.GetComponentsInChildren<Rigidbody>(true).Length>0||cell.GetComponentsInChildren<JourneyTarget>(true).Length>0)throw new InvalidOperationException("Background pool contains gameplay/physics: "+cell.name);
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(BiomedicalLivingPass.Reports+"/Stage2");string before=ArteryVisualV8.GeometrySignature(world);
            var palette=new Dictionary<string,(Mesh mesh,Material[] mats)>();
            foreach(var name in new[]{"erythrocyte","leukocyte","platelet","albumin","globulin","lipoprotein","fibrinogen","plasma-ion"})
            {
                int index=Enumerable.Range(0,flow.cells.Length).Where(i=>flow.species[i]==name&&!flow.cells[i].GetComponent<MeshFilter>().sharedMesh.name.Contains("triplet")).DefaultIfEmpty(-1).First();if(index<0)throw new InvalidOperationException("Existing source species missing: "+name);
                palette[name]=(flow.cells[index].GetComponent<MeshFilter>().sharedMesh,flow.cells[index].GetComponent<Renderer>().sharedMaterials);
            }
            var groups=PlateletGroups(palette["platelet"].mesh);var counts=new Dictionary<string,int>();
            var reserved=new HashSet<int>(flow.interactiveCells.Select(p=>p.poolIndex));int serial=0;
            foreach(var cell in flow.cells)
            {
                int i=Array.IndexOf(flow.cells,cell);if(reserved.Contains(i))continue;
                int lane=serial++%100;
                string name=lane<70?"erythrocyte":lane<74?"leukocyte":lane<86?"platelet":new[]{"albumin","globulin","lipoprotein","fibrinogen","plasma-ion"}[i%5];
                var source=palette[name];var filter=cell.GetComponent<MeshFilter>();var renderer=cell.GetComponent<MeshRenderer>();
                filter.sharedMesh=name=="platelet"&&(i%3==0)?groups:source.mesh;renderer.sharedMaterials=source.mats;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                float size=name=="erythrocyte"?.18f+(i*7%100)/100f*.12f:name=="leukocyte"?.18f+(i%4)*.025f:name=="platelet"?(filter.sharedMesh==groups?.19f:.085f+(i%3)*.018f):.035f+(i%4)*.015f;
                float extent=Mathf.Max(filter.sharedMesh.bounds.size.x,Mathf.Max(filter.sharedMesh.bounds.size.y,filter.sharedMesh.bounds.size.z));cell.localScale=Vector3.one*(size/Mathf.Max(.001f,extent));
                flow.species[i]=name;foreach(var mat in source.mats)if(mat){mat.enableInstancing=true;EditorUtility.SetDirty(mat);}
                counts[name]=counts.TryGetValue(name,out int amount)?amount+1:1;
                EditorUtility.SetDirty(cell);EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(filter);
            }
            flow.layeredStudyBackground=true;EditorUtility.SetDirty(flow);
            var continuous=flow.GetComponent<JourneyContinuousBloodstream>();if(!continuous)continuous=flow.gameObject.AddComponent<JourneyContinuousBloodstream>();continuous.flow=flow;
            var visuals=new List<JourneyContinuousBloodstream.VisualSpecies>();
            foreach(var entry in new[]{(id:"erythrocyte",weight:54,min:.18f,max:.30f),(id:"leukocyte",weight:4,min:.18f,max:.28f),(id:"platelet",weight:8,min:.07f,max:.12f),(id:"albumin",weight:2,min:.035f,max:.065f),(id:"globulin",weight:3,min:.045f,max:.075f),(id:"lipoprotein",weight:3,min:.045f,max:.075f),(id:"fibrinogen",weight:3,min:.04f,max:.08f),(id:"plasma-ion",weight:3,min:.025f,max:.045f)})
            {var visual=palette[entry.id];visuals.Add(new JourneyContinuousBloodstream.VisualSpecies{id=entry.id,mesh=visual.mesh,materials=visual.mats,weight=entry.weight,minDiameter=entry.min,maxDiameter=entry.max});}
            visuals.Add(new JourneyContinuousBloodstream.VisualSpecies{id="platelet",mesh=groups,materials=palette["platelet"].mats,weight=4,minDiameter=.16f,maxDiameter=.22f});
            foreach(var gas in new[]{(id:"erythrocyte-O2",mesh:"Erythrocyte_Oxygen_TeachingMarker",material:"Flow_Oxygen_Cyan",weight:10),(id:"erythrocyte-CO2",mesh:"Erythrocyte_CO2_TeachingMarker",material:"Flow_CO2_Pearl",weight:6)})
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(ArteryScenePolish.Folder+"/"+gas.mesh+".asset");var material=AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/"+gas.material+".mat");
                if(!mesh||!material)throw new InvalidOperationException("Existing gas carrier background assets missing");
                material.enableInstancing=true;EditorUtility.SetDirty(material);
                visuals.Add(new JourneyContinuousBloodstream.VisualSpecies{id=gas.id,mesh=mesh,materials=new[]{palette["erythrocyte"].mats[0],material},weight=gas.weight,minDiameter=.18f,maxDiameter=.30f});
            }
            continuous.palette=visuals.ToArray();flow.continuousBackground=continuous;EditorUtility.SetDirty(continuous);EditorUtility.SetDirty(flow);
            var input=world.mission.GetComponent<JourneyInput>();input.hoverFeedback=true;EditorUtility.SetDirty(input);
            foreach(var pickup in flow.interactiveCells){pickup.target.hoverEmission=true;pickup.target.hoverEmissionColor=new Color(.025f,.11f,.14f);EditorUtility.SetDirty(pickup.target);}
            string after=ArteryVisualV8.GeometrySignature(world);if(before!=after)throw new InvalidOperationException("Background authoring changed biological interactions");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);
            File.WriteAllText(BiomedicalLivingPass.Reports+"/Stage2/Background.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,geometryPreserved=true,geometryBefore=before,geometryAfter=after,pool=flow.cells.Length,interactiveSlots=reserved.Count,backgroundVisualSlots=360-reserved.Count,initialAuthoringSpecies=counts,continuousRandomPalette=visuals.Select(v=>new{v.id,v.weight,v.minDiameter,v.maxDiameter}),physicsAdded=0,poolGrowth=0,circulation="unlimited / local upstream-downstream + persistent route and all branches",nearCellsNeverRecentered=true,randomSpeciesOnReuse=true,rareWhiteCells=true,plateletGroups=true,hardwareVerified=false},Formatting.Indented));
        }
        static Mesh PlateletGroups(Mesh mesh)
        {
            float size=Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
            var combines=new List<CombineInstance>();
            for(int i=0;i<3;i++)combines.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3((i-1)*size*.7f,(i==1?.25f:-.15f)*size,i*size*.20f),Quaternion.Euler(i*23,i*41,i*19),Vector3.one*(.70f+i*.08f))});
            var merged=new Mesh{name="Background platelet triplet — no interaction"};merged.CombineMeshes(combines.ToArray(),true,true);merged.RecalculateBounds();string path=Folder+"/PlateletTriplet.asset";JourneyGeometry.Save(merged,path);return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
    }
}

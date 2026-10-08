using System.Linq;
using UnityEditor;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryFlowDiversity
    {
        public static void Apply(JourneyWorld w)
        {
            var flow=w.GetComponent<JourneyBloodFlow>();string folder=ArteryScenePolish.Folder;
            var rbc=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Normalized_POLISH_RBC_LOD2.asset");
            var body=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Ruby_RBC.mat");
            var oxygen=Merge(w.targets.First(t=>t.targetId=="oxygen"));var carbon=Merge(w.targets.First(t=>t.targetId=="co2"));
            var o2=WithMarker(rbc,oxygen,"Erythrocyte_Oxygen_TeachingMarker");var co2=WithMarker(rbc,carbon,"Erythrocyte_CO2_TeachingMarker");
            var oxygenColor=ArteryEpisodeViewsBuilder.Lit("Flow_Oxygen_Cyan",new Color(.30f,.88f,.96f),.35f);
            var carbonColor=ArteryEpisodeViewsBuilder.Lit("Flow_CO2_Pearl",new Color(.88f,.89f,.96f),.35f);
            var ionColor=ArteryEpisodeViewsBuilder.Lit("Plasma_Ion_Turquoise",new Color(.32f,.84f,.78f),.26f);
            var proteinColor=ArteryEpisodeViewsBuilder.Lit("Plasma_Protein_Ivory",new Color(.91f,.79f,.60f),.28f);
            var plasma=Merge(w.targets.First(t=>t.targetId=="plasma"));
            flow.teachingLegend=ArteryAnnotations.Label(w.mover.viewCamera.transform,"Flow teaching scale legend","Поток • молекулы и ионы условно увеличены\nO₂ — гем; CO₂ — другие формы переноса");
            flow.teachingLegend.fontSize=14;flow.teachingLegend.rectTransform.sizeDelta=new Vector2(490,60);flow.teachingLegend.transform.localPosition=new Vector3(-.55f,.53f,1.9f);flow.teachingLegend.transform.localRotation=Quaternion.identity;
            for(int i=0;i<flow.cells.Length;i++)
            {
                var cell=flow.cells[i];var filter=cell.GetComponent<MeshFilter>();var renderer=cell.GetComponent<Renderer>();
                if(flow.species[i]!="erythrocyte")continue;
                if(i%11==3){filter.sharedMesh=oxygen;renderer.sharedMaterial=ionColor;SetSize(cell,oxygen,.055f);flow.species[i]="plasma-ion";}
                else if(i%17==4){filter.sharedMesh=plasma;renderer.sharedMaterial=proteinColor;SetSize(cell,plasma,.09f);flow.species[i]="plasma-protein";}
                else if(i%5==2){filter.sharedMesh=o2;renderer.sharedMaterials=new[]{body,oxygenColor};flow.species[i]="erythrocyte-O2";}
                else if(i%19==5){filter.sharedMesh=co2;renderer.sharedMaterials=new[]{body,carbonColor};flow.species[i]="erythrocyte-CO2";}
            }
        }
        static void SetSize(Transform cell,Mesh mesh,float size){float scale=size/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));cell.localScale=Vector3.one*scale;}
        static Mesh Merge(JourneyTarget target)
        {
            var list=new System.Collections.Generic.List<CombineInstance>();
            foreach(var f in target.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh)
            for(int s=0;s<f.sharedMesh.subMeshCount;s++)list.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=s,transform=target.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
            var mesh=new Mesh{name="Flow glyph "+target.targetId};mesh.CombineMeshes(list.ToArray(),true,true);mesh.RecalculateBounds();
            var vertices=mesh.vertices;float size=Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));var centre=mesh.bounds.center;
            for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-centre)/Mathf.Max(.001f,size);mesh.vertices=vertices;mesh.RecalculateBounds();
            string path=ArteryScenePolish.Folder+"/FlowGlyph_"+target.targetId+".asset";JourneyGeometry.Save(mesh,path);return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
        static Mesh WithMarker(Mesh body,Mesh marker,string name)
        {
            float size=Mathf.Max(body.bounds.size.x,Mathf.Max(body.bounds.size.y,body.bounds.size.z));
            var mesh=new Mesh{name=name};mesh.CombineMeshes(new[]{new CombineInstance{mesh=body,subMeshIndex=0,transform=Matrix4x4.identity},new CombineInstance{mesh=marker,subMeshIndex=0,transform=Matrix4x4.TRS(body.bounds.center+Vector3.up*(body.bounds.extents.y+size*.035f),Quaternion.identity,Vector3.one*(size*.25f))}},false,true);
            mesh.RecalculateBounds();string path=ArteryScenePolish.Folder+"/"+name+".asset";JourneyGeometry.Save(mesh,path);return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
    }
}

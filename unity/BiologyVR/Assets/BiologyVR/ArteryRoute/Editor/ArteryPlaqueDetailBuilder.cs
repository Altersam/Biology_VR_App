using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryPlaqueDetailBuilder
    {
        public static JourneyPlaqueDetail Apply(JourneyWorld world,Transform parent,JourneyPlaqueState state)
        {
            var cap=world.locationRoots[world.mover.path.locationIds[3]].GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="Plaque_Cap");
            var core=cap.transform.parent.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="Plaque_Exposed_Core");
            foreach(var old in cap.transform.parent.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Embedded foam cell")))old.gameObject.SetActive(false);
            var root=new GameObject("Plaque detailed tissue composition");root.transform.SetParent(cap.transform.parent,false);
            var library=AssetDatabase.LoadAssetAtPath<GameObject>(BuildArteryVrScene.Root+"/Imported/Visual/HybridArteryVisual.prefab");
            var source=library.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="HD_S06_Lipid_Droplet_0");
            var droplet=source.sharedMesh;
            var gold=AssetDatabase.LoadAssetAtPath<Material>(ArteryPlaqueStateBuilder.Folder+"/Scene06_SourceLipid_Gold.mat");
            var cell=AssetDatabase.LoadAssetAtPath<Mesh>(ArteryScenePolish.Folder+"/Normalized_POLISH_WBC_LOD2.asset");
            var cellMaterial=AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Lavender_WhiteCell.mat");
            var granules=AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Soft_Lavender_Granules.mat");
            ArteryTissueTextures.Bake("Plaque_Collagen",1);
            var capMaterial=new Material(cap.GetComponent<Renderer>().sharedMaterial){name="Plaque_DetailedFibrousCap",enableInstancing=true};
            capMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(ArteryScenePolish.Folder+"/Plaque_Collagen_Base.png"));
            capMaterial.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(ArteryScenePolish.Folder+"/Plaque_Collagen_Normal.png"));
            capMaterial.SetTextureScale("_BaseMap",Vector2.one);capMaterial.SetTextureOffset("_BaseMap",Vector2.zero);capMaterial.SetFloat("_NormalStrength",.60f);capMaterial.SetFloat("_AllowWallCutaway",0);
            capMaterial.SetColor("_PatchColor",new Color(.96f,.65f,.62f));JourneyGeometry.Save(capMaterial,ArteryScenePolish.Folder+"/Plaque_DetailedFibrousCap.mat");
            cap.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Plaque_DetailedFibrousCap.mat");
            var droplets=new List<Transform>();var sectors=new List<int>();
            for(int row=0;row<5;row++)for(int col=0;col<7;col++)
            {
                float u=.26f+col*.075f+.013f*Mathf.Sin(row*2.1f+col),v=.28f+row*.095f+.015f*Mathf.Cos(col*1.9f);
                if(((u-.5f)*2.5f)*((u-.5f)*2.5f)+((v-.5f)*3)*((v-.5f)*3)>.85f)continue;
                Vector3 surface=Sample(cap,u,v);var radial=Radial(world,surface,v);
                var visual=Visual("Embedded cholesterol lobule "+droplets.Count,root.transform,droplet,new[]{gold},.14f+.06f*Mathf.Repeat(Mathf.Sin(col*17+row*31)*45,1));
                visual.position=surface-radial*.11f-visual.TransformVector(droplet.bounds.center);
                visual.rotation=Quaternion.Euler(row*37,col*31,row*col*13);
                droplets.Add(visual);int closest=0;float distance=float.MaxValue;
                for(int i=0;i<3;i++){float d=(state.Zones[i].transform.position-surface).sqrMagnitude;if(d<distance){distance=d;closest=i;}}
                sectors.Add(closest);
            }
            var foam=new Transform[4];var foamUV=new[]{new Vector2(.29f,.34f),new Vector2(.73f,.42f),new Vector2(.34f,.67f),new Vector2(.63f,.68f)};
            for(int i=0;i<foam.Length;i++)
            {
                var position=Sample(cap,foamUV[i].x,foamUV[i].y);var radial=Radial(world,position,foamUV[i].y);
                foam[i]=Visual("Lipid laden macrophage "+i,root.transform,cell,new[]{cellMaterial,granules},.25f);
                foam[i].position=position-radial*.12f-foam[i].TransformVector(cell.bounds.center);
                for(int j=0;j<5;j++)
                {
                    var lipid=Visual("Intracellular lipid "+j,foam[i],droplet,new[]{gold},.055f);
                    float angle=j*Mathf.PI*2/5;lipid.position=foam[i].position+world.mover.path.Up(world.mover.path.Anchor(6)+4)*Mathf.Cos(angle)*.08f+world.mover.path.Forward(world.mover.path.Anchor(6)+4)*Mathf.Sin(angle)*.08f-radial*.085f;
                }
            }
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            for(int fibre=0;fibre<12;fibre++)
            {
                int start=vertices.Count;
                for(int segment=0;segment<=12;segment++)
                {
                    float u=.10f+segment/12f*.80f,v=.15f+fibre*.030f+.009f*Mathf.Sin(segment*.7f+fibre);
                    for(int side=0;side<2;side++)
                    {
                        Vector3 p=Sample(cap,u,v+(side==0?-.0035f:.0035f));var radial=Radial(world,p,v);
                        vertices.Add(root.transform.InverseTransformPoint(p-radial*.018f));normals.Add(root.transform.InverseTransformDirection(-radial));uv.Add(new Vector2(u,v));
                    }
                    if(segment<12){int a=start+segment*2;indices.AddRange(new[]{a,a+2,a+3,a,a+3,a+1});}
                }
            }
            var mesh=new Mesh{name="Plaque collagen ribbon bundle"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateTangents();mesh.RecalculateBounds();
            string meshPath=ArteryScenePolish.Folder+"/Plaque_Collagen_Ribbons.asset";JourneyGeometry.Save(mesh,meshPath);
            var fibreObject=new GameObject("Fibrous cap collagen bundles");fibreObject.transform.SetParent(root.transform,false);fibreObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var fibreSurface=fibreObject.AddComponent<MeshRenderer>();fibreSurface.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(ArteryScenePolish.Folder+"/Fibrin_Ivory.mat")??cellMaterial;fibreSurface.shadowCastingMode=ShadowCastingMode.Off;
            var labels=new[]{ArteryAnnotations.Label(root.transform,"Plaque cap label","Фиброзная покрышка"),ArteryAnnotations.Label(root.transform,"Plaque lipid label","Липидное ядро"),ArteryAnnotations.Label(root.transform,"Plaque foam label","Пенистые клетки")};
            float station=world.mover.path.Anchor(6)+4;
            labels[0].transform.position=Sample(cap,.5f,.2f)-world.mover.path.Right(station)*.16f;
            labels[1].transform.position=Sample(cap,.5f,.55f)-world.mover.path.Right(station)*.35f;
            labels[2].transform.position=Sample(cap,.72f,.7f)-world.mover.path.Right(station)*.25f;
            var detail=parent.gameObject.AddComponent<JourneyPlaqueDetail>();detail.Configure(world,state,root,cap.GetComponent<Renderer>(),droplets.ToArray(),sectors.ToArray(),foam,fibreObject.transform,labels);
            return detail;
        }
        static Transform Visual(string name,Transform parent,Mesh mesh,Material[] materials,float size)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=materials;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            float factor=size/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));go.transform.localScale=Vector3.one*factor;return go.transform;
        }
        static Vector3 Radial(JourneyWorld w,Vector3 point,float v)
        {return (point-w.mover.path.Centre(w.mover.path.Anchor(6)+4+(v-.5f)*3.7f)).normalized;}
        static Vector3 Sample(MeshFilter filter,float u,float v)
        {
            var mesh=filter.sharedMesh;var uv=mesh.uv;var positions=mesh.vertices;var triangles=mesh.triangles;var point=new Vector2(u,v);
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var x=uv[b]-uv[a];var y=uv[c]-uv[a];var p=point-uv[a];float det=x.x*y.y-x.y*y.x;if(Mathf.Abs(det)<.000001f)continue;
                float wb=(p.x*y.y-p.y*y.x)/det,wc=(x.x*p.y-x.y*p.x)/det,wa=1-wb-wc;
                if(wa<0||wb<0||wc<0)continue;return filter.transform.TransformPoint(positions[a]*wa+positions[b]*wb+positions[c]*wc);
            }
            return filter.transform.TransformPoint(mesh.bounds.center);
        }
    }
}

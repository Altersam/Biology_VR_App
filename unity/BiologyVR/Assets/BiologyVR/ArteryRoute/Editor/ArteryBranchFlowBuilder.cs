using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryBranchFlowBuilder
    {
        public static void Apply(JourneyWorld w,Transform parent,Material wall)
        {
            var flow=w.GetComponent<JourneyBloodFlow>();var main=w.mover.path;var sources=JourneyGeometry.Branches(main);
            flow.branchPaths=new ArteryJourneyPath[sources.Length];flow.branchEntries=new[]{main.Anchor(7)-11,main.Anchor(10)-11,main.Anchor(15)-16};
            for(int branch=0;branch<sources.Length;branch++)
            {
                var source=sources[branch];var last=source.points.Last();var f=source.Forward(source.Length);var r=source.Right(source.Length);var u=source.Up(source.Length);
                var tail=JourneyGeometry.Sample(new[]{last-f*7,last,last+f*9,last+f*27+r*12,last+f*50+r*4+u*3,last+f*83-r*9+u*3},24);
                var p=ScriptableObject.CreateInstance<ArteryJourneyPath>();p.points=source.points.Concat(tail.points.Skip(25)).ToArray();int count=p.points.Length;
                p.distances=new float[count];p.rights=new Vector3[count];p.ups=new Vector3[count];p.radii=new float[count];
                for(int i=0;i<count;i++)
                {
                    if(i>0)p.distances[i]=p.distances[i-1]+Vector3.Distance(p.points[i],p.points[i-1]);
                    if(i<source.points.Length){p.rights[i]=source.rights[i];p.ups[i]=source.ups[i];p.radii[i]=source.radii[i];}
                    else{int j=i-source.points.Length+25;p.rights[i]=tail.rights[j];p.ups[i]=tail.ups[j];p.radii[i]=2.08f;}
                }
                string prefix=ArteryScenePolish.Folder+"/FlowBranch_"+branch;p.name="Flow branch "+branch;JourneyGeometry.Save(p,prefix+".asset");p=AssetDatabase.LoadAssetAtPath<ArteryJourneyPath>(prefix+".asset");flow.branchPaths[branch]=p;
                var mesh=JourneyGeometry.Tube(p,null,true,main);JourneyGeometry.Save(mesh,prefix+"_Lumen.asset");mesh=AssetDatabase.LoadAssetAtPath<Mesh>(prefix+"_Lumen.asset");
                var old=GameObject.Find("Organic side branch "+(branch+1));if(old){old.GetComponent<Renderer>().enabled=false;old.GetComponent<MeshCollider>().sharedMesh=mesh;}
                var vertices=mesh.vertices;var uv=mesh.uv;var normals=mesh.normals;var centres=new List<Vector3>();mesh.GetUVs(1,centres);var indices=mesh.triangles;
                var groups=new Dictionary<int,List<int>>();
                for(int i=0;i<indices.Length;i+=3){int key=Mathf.FloorToInt((uv[indices[i]].y+uv[indices[i+1]].y+uv[indices[i+2]].y)/3*5.6f/8);if(!groups.TryGetValue(key,out var list))groups[key]=list=new List<int>();list.Add(indices[i]);list.Add(indices[i+1]);list.Add(indices[i+2]);}
                foreach(var group in groups)
                {
                    var map=new Dictionary<int,int>();var v=new List<Vector3>();var n=new List<Vector3>();var tex=new List<Vector2>();var c=new List<Vector3>();var faces=new List<int>();
                    foreach(int index in group.Value){if(!map.TryGetValue(index,out int id)){id=v.Count;map[index]=id;v.Add(vertices[index]);n.Add(normals[index]);tex.Add(uv[index]);c.Add(centres[index]);}faces.Add(id);}
                    var chunk=new Mesh{name="Branch flow visibility "+branch+" / "+group.Key};chunk.SetVertices(v);chunk.SetNormals(n);chunk.SetUVs(0,tex);chunk.SetUVs(1,c);chunk.SetTriangles(faces,0);chunk.RecalculateTangents();chunk.RecalculateBounds();
                    string asset=prefix+"_Chunk_"+group.Key+".asset",name=chunk.name;JourneyGeometry.Save(chunk,asset);
                    var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(asset);var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=wall;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                }
            }
            w.art.branchRenderers=parent.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Branch flow visibility")).Cast<Renderer>().ToArray();
        }
    }
}

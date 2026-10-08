using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>Offline importer for this project's uncompressed, texture-free Blender GLBs.
    /// It preserves individual nodes, shared meshes, submeshes, UVs, materials and extras.
    /// glTF right-handed Y-up is converted by reflecting Z and triangle winding.</summary>
    public sealed class ArteryGlbImporter
    {
        JObject document;
        byte[] binary;
        Mesh[] meshes;
        Material[] materials;
        int[][] primitiveMaterials;
        string assetFolder;
        public int MeshCount => meshes.Length;
        public int NodeCount => document["nodes"]?.Count() ?? 0;

        public static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');
            Folder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static string Safe(string name)
        {
            foreach(char c in Path.GetInvalidFileNameChars()) name=name.Replace(c,'_');
            return name;
        }
        public GameObject Import(string filename,string outputFolder,bool colliderOnly=false)
        {
            assetFolder=outputFolder; Folder(outputFolder); Folder(outputFolder+"/Meshes"); Folder(outputFolder+"/Materials");
            using(var stream=File.OpenRead(filename)) using(var reader=new BinaryReader(stream))
            {
                if(reader.ReadUInt32()!=0x46546C67 || reader.ReadUInt32()!=2) throw new InvalidDataException("Expected GLB 2.0");
                uint length=reader.ReadUInt32();
                while(stream.Position<length)
                {
                    int size=(int)reader.ReadUInt32(); uint type=reader.ReadUInt32(); byte[] bytes=reader.ReadBytes(size);
                    if(bytes.Length!=size) throw new EndOfStreamException();
                    if(type==0x4E4F534A) document=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
                    else if(type==0x004E4942) binary=bytes;
                }
            }
            if(document==null || binary==null) throw new InvalidDataException("Missing JSON/binary chunk");
            if(document["extensionsRequired"] is JArray required && required.Count>0) throw new InvalidDataException("Compressed GLB not supported by the offline importer");
            ReadMaterials(); ReadMeshes();
            var root=new GameObject(colliderOnly?"ArteryPhysics":"HybridArteryVisual");
            var nodes=(JArray)document["nodes"]; var objects=new GameObject[nodes.Count];
            for(int i=0;i<nodes.Count;i++)
            {
                var n=(JObject)nodes[i]; var go=new GameObject(n.Value<string>("name")??$"Node_{i}"); objects[i]=go;
                go.transform.SetParent(root.transform,false); ApplyTransform(go.transform,n);
                if(n["mesh"]!=null)
                {
                    int index=n.Value<int>("mesh"); var mesh=meshes[index];
                    if(colliderOnly) { var mc=go.AddComponent<MeshCollider>(); mc.sharedMesh=mesh; mc.convex=false; }
                    else
                    {
                        go.AddComponent<MeshFilter>().sharedMesh=mesh;
                        var renderer=go.AddComponent<MeshRenderer>();
                        var slots=primitiveMaterials[index]; var chosen=new Material[slots.Length];
                        for(int j=0;j<slots.Length;j++) chosen[j]=materials[Mathf.Clamp(slots[j],0,materials.Length-1)];
                        renderer.sharedMaterials=chosen;
                        if(go.name.Contains("Label") || go.name.Contains("_Title_") || go.name.Contains("_Lesson_") || go.name.Contains("_HUD_") || go.name.Contains("Guide_Tag") || go.name.Contains("_Border") || go.name.StartsWith("AB_Flow_Guide") || go.name.StartsWith("AB_Flow_Arrow") || go.name.Contains("_Dash_") || go.name.Contains("_Halo"))
                        {
                            var unlit=new Material[chosen.Length];
                            for(int j=0;j<chosen.Length;j++) unlit[j]=Unlit(chosen[j]);
                            renderer.sharedMaterials=unlit; renderer.shadowCastingMode=ShadowCastingMode.Off;
                        }
                        if(go.name.StartsWith("AB_Artery_"))
                        {
                            foreach(var ma in chosen) { ma.SetFloat("_CellSeams",1); ma.SetFloat("_PulseAmplitude",.03f); }
                            renderer.shadowCastingMode=ShadowCastingMode.Off;
                        }
                    }
                }
                ConfigureExtras(go,n["extras"] as JObject);
            }
            for(int i=0;i<nodes.Count;i++)
                if(nodes[i]["children"] is JArray children)
                    foreach(int child in children) objects[child].transform.SetParent(objects[i].transform,false);
            if(!colliderOnly)
            {
                var groups=new Dictionary<int,Transform>();
                for(int id=3;id<=16;id++) { var g=new GameObject($"Episode_{id:00}"); g.transform.SetParent(root.transform,false); groups[id]=g.transform; }
                foreach(var go in objects)
                {
                    if(go.transform.parent!=root.transform) continue;
                    var tag=go.GetComponent<ArteryNode>();
                    if(tag!=null && tag.sceneId>=3 && tag.sceneId<=16 && !go.name.StartsWith("AB_Artery_")) go.transform.SetParent(groups[tag.sceneId],true);
                }
                // Blender view-models and volume containers must not replace XR controllers/URP fog.
                foreach(var go in objects)
                    if(go.name.Contains("Preview_Hands") || go.name.Contains("Preview_Glove") || go.name.Contains("Preview_Scanner") || go.name.Contains("Preview_BioTool") || go.name.Contains("Plasma_Haze")) go.SetActive(false);
                foreach(var go in objects)
                    if(go.name.Contains("_Lesson_") || go.name.Contains("_Title_") || go.name.Contains("_HUD_")) go.SetActive(false);
            }
            AssetDatabase.SaveAssets();
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,outputFolder+"/"+root.name+".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        void ConfigureExtras(GameObject go,JObject extras)
        {
            var node=go.AddComponent<ArteryNode>();
            node.presetId=extras?.Value<string>("preset_id")??"";
            node.sceneId=extras?.Value<int?>("scene_id")??0;
            if(node.sceneId==0)
            {
                int s=go.name.IndexOf("_S",StringComparison.Ordinal);
                if(s>=0 && go.name.Length>=s+4 && int.TryParse(go.name.Substring(s+2,2),out int id)) node.sceneId=id;
            }
            if(extras?.Value<bool?>("flow_preview")==true && !go.name.Contains("Study"))
            {
                var flow=go.AddComponent<BloodCellFollower>();
                flow.startY=extras.Value<float?>("route_y")??extras.Value<float?>("path_parameter_y")??(node.sceneId-3)*24;
                flow.minimumY=extras.Value<float?>("flow_min_y")??(node.sceneId-3)*24-8;
                flow.maximumY=extras.Value<float?>("flow_max_y")??(node.sceneId-3)*24+16;
                flow.laneX=extras.Value<float?>("lane_x")??0; flow.laneZ=extras.Value<float?>("lane_z")??0;
                flow.speed=node.presetId=="erythrocyte"?2.0f:node.presetId=="small-virus"?.8f:1.1f;
            }
        }
        Material Unlit(Material original)
        {
            string path=assetFolder+"/Materials/"+Safe(original.name)+"_Unlit.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing!=null) return existing;
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name=original.name+"_Unlit" };
            material.SetColor("_BaseColor",original.GetColor("_BaseColor")); material.SetFloat("_Cull",0);
            AssetDatabase.CreateAsset(material,path); return material;
        }
        void ReadMaterials()
        {
            var definitions=document["materials"] as JArray ?? new JArray(); materials=new Material[Math.Max(1,definitions.Count)];
            for(int i=0;i<materials.Length;i++)
            {
                var d=i<definitions.Count?(JObject)definitions[i]:new JObject(); string name=d.Value<string>("name")??$"Material_{i}";
                string path=assetFolder+"/Materials/"+Safe(name)+".mat";
                var existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing!=null) {materials[i]=existing;continue;}
                var pbr=d["pbrMetallicRoughness"] as JObject; Color color=Color.white;
                if(pbr?["baseColorFactor"] is JArray c) color=new Color((float)c[0],(float)c[1],(float)c[2],(float)c[3]);
                bool transparent=d.Value<string>("alphaMode")=="BLEND";
                var shader=Shader.Find(transparent?"Universal Render Pipeline/Lit":"BiologyVR/Hybrid Artery");
                if(shader==null) shader=Shader.Find("Universal Render Pipeline/Lit");
                var m=new Material(shader) { name=name, enableInstancing=true };
                m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.42f); m.SetFloat("_Metallic",pbr?.Value<float?>("metallicFactor")??0);
                m.SetFloat("_Cull",d.Value<bool?>("doubleSided")==true?0:2);
                m.SetFloat("_CellSeams",0); m.SetFloat("_PulseAmplitude",0);
                if(transparent)
                {
                    m.SetFloat("_Surface",1); m.SetFloat("_Blend",0); m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite",0); m.SetOverrideTag("RenderType","Transparent");
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue=3000;
                }
                AssetDatabase.CreateAsset(m,path); materials[i]=m;
            }
        }
        void ReadMeshes()
        {
            var definitions=(JArray)document["meshes"]; meshes=new Mesh[definitions.Count]; primitiveMaterials=new int[definitions.Count][];
            for(int id=0;id<definitions.Count;id++)
            {
                var d=(JObject)definitions[id]; var primitives=(JArray)d["primitives"];
                var positions=new List<Vector3>(); var normals=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int[]>(); var slots=new List<int>();
                foreach(JObject primitive in primitives)
                {
                    if((primitive.Value<int?>("mode")??4)!=4) throw new InvalidDataException("Expected triangles");
                    var attributes=(JObject)primitive["attributes"]; int positionAccessor=(int)attributes["POSITION"];
                    float[][] coords=FloatAccessor(positionAccessor); int start=positions.Count;
                    float[][] ns=attributes["NORMAL"]!=null?FloatAccessor((int)attributes["NORMAL"]):null;
                    float[][] tx=attributes["TEXCOORD_0"]!=null?FloatAccessor((int)attributes["TEXCOORD_0"]):null;
                    for(int k=0;k<coords.Length;k++)
                    {
                        positions.Add(new Vector3(coords[k][0],coords[k][1],-coords[k][2]));
                        normals.Add(ns!=null?new Vector3(ns[k][0],ns[k][1],-ns[k][2]):Vector3.up);
                        uv.Add(tx!=null?new Vector2(tx[k][0],1-tx[k][1]):Vector2.zero);
                    }
                    int[] indices=primitive["indices"]!=null?IndexAccessor((int)primitive["indices"]):Sequence(coords.Length);
                    for(int k=0;k<indices.Length;k+=3) { int a=indices[k]; indices[k]=start+indices[k+2]; indices[k+1]+=start; indices[k+2]=start+a; }
                    triangles.Add(indices); slots.Add(primitive.Value<int?>("material")??0);
                }
                string meshName=d.Value<string>("name")??$"Mesh_{id}";
                var mesh=new Mesh { name=meshName, indexFormat=positions.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
                mesh.SetVertices(positions); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.subMeshCount=triangles.Count;
                for(int p=0;p<triangles.Count;p++) mesh.SetTriangles(triangles[p],p,false);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                string path=assetFolder+"/Meshes/"+id.ToString("D4")+"_"+Safe(meshName)+".asset";
                var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(previous!=null) { UnityEngine.Object.DestroyImmediate(mesh); mesh=previous; }
                else AssetDatabase.CreateAsset(mesh,path);
                meshes[id]=mesh; primitiveMaterials[id]=slots.ToArray();
            }
        }
        static int[] Sequence(int count) {var a=new int[count];for(int i=0;i<count;i++)a[i]=i;return a;}
        float[][] FloatAccessor(int index)
        {
            var a=(JObject)document["accessors"][index]; var view=(JObject)document["bufferViews"][(int)a["bufferView"]];
            if(a.Value<int>("componentType")!=5126) throw new InvalidDataException("Expected float accessor");
            int width=a.Value<string>("type") switch {"VEC2"=>2,"VEC3"=>3,"VEC4"=>4,"SCALAR"=>1,_=>throw new InvalidDataException("Accessor shape")};
            int count=a.Value<int>("count"),offset=(view.Value<int?>("byteOffset")??0)+(a.Value<int?>("byteOffset")??0),stride=view.Value<int?>("byteStride")??width*4;
            var result=new float[count][];
            for(int i=0;i<count;i++){result[i]=new float[width];for(int k=0;k<width;k++)result[i][k]=BitConverter.ToSingle(binary,offset+i*stride+k*4);}
            return result;
        }
        int[] IndexAccessor(int index)
        {
            var a=(JObject)document["accessors"][index]; var view=(JObject)document["bufferViews"][(int)a["bufferView"]];
            int type=a.Value<int>("componentType"),bytes=type==5125?4:type==5123?2:type==5121?1:throw new InvalidDataException("Index type");
            int count=a.Value<int>("count"),offset=(view.Value<int?>("byteOffset")??0)+(a.Value<int?>("byteOffset")??0),stride=view.Value<int?>("byteStride")??bytes;
            var result=new int[count];for(int i=0;i<count;i++){int p=offset+i*stride;result[i]=bytes==4?checked((int)BitConverter.ToUInt32(binary,p)):bytes==2?BitConverter.ToUInt16(binary,p):binary[p];}return result;
        }
        static void ApplyTransform(Transform t,JObject node)
        {
            if(node["translation"] is JArray p) t.localPosition=new Vector3((float)p[0],(float)p[1],-(float)p[2]);
            if(node["rotation"] is JArray q) t.localRotation=new Quaternion(-(float)q[0],-(float)q[1],(float)q[2],(float)q[3]);
            if(node["scale"] is JArray s) t.localScale=new Vector3((float)s[0],(float)s[1],(float)s[2]);
            if(node["matrix"] is JArray a)
            {
                var m=new Matrix4x4(); for(int r=0;r<4;r++)for(int c=0;c<4;c++)m[r,c]=(float)a[c*4+r]*(r==2?-1:1)*(c==2?-1:1);
                t.localPosition=m.GetColumn(3); t.localRotation=Quaternion.LookRotation(m.GetColumn(2),m.GetColumn(1)); t.localScale=new Vector3(m.GetColumn(0).magnitude,m.GetColumn(1).magnitude,m.GetColumn(2).magnitude);
            }
        }
    }
}

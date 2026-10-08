using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryBlenderPlasmaImporter
    {
        public static void Apply(JourneyWorld world)
        {
            string folder=ArteryScenePolish.Folder,path=folder+"/Blender_Plasma_Details.json";
            if(!File.Exists(path))throw new FileNotFoundException("Blender plasma geometry export required",path);
            var data=JObject.Parse(File.ReadAllText(path));var meshes=new Dictionary<string,Mesh>();
            foreach(var model in data["models"])
            {
                string name=model.Value<string>("name");var vertices=model["vertices"].Select(v=>new Vector3((float)v[0],(float)v[1],(float)v[2])).ToArray();
                var uv=model["uv"].Select(v=>new Vector2((float)v[0],(float)v[1])).ToArray();var indices=model["indices"].Select(i=>(int)i).ToArray();
                for(int i=0;i<indices.Length;i+=3){int a=indices[i];indices[i]=indices[i+2];indices[i+2]=a;}
                var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();
                var normals=mesh.normals;var averages=new Dictionary<Vector3,Vector3>();
                for(int i=0;i<vertices.Length;i++){averages.TryGetValue(vertices[i],out var n);averages[vertices[i]]=n+normals[i];}
                for(int i=0;i<normals.Length;i++)normals[i]=averages[vertices[i]].normalized;
                mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();string asset=folder+"/"+name+".asset";JourneyGeometry.Save(mesh,asset);meshes[name]=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
            }
            var materials=new[]{ArteryEpisodeViewsBuilder.Lit("Plasma_Albumin_SoftGold",new Color(.95f,.76f,.40f),.33f),ArteryEpisodeViewsBuilder.Lit("Plasma_Globulin_Lavender",new Color(.71f,.67f,.93f),.32f),ArteryEpisodeViewsBuilder.Lit("Plasma_Fibrinogen_Pearl",new Color(.94f,.84f,.69f),.30f),ArteryEpisodeViewsBuilder.Lit("Plasma_Lipoprotein_Peach",new Color(.99f,.59f,.22f),.33f)};
            string[] names={"PLASMA_Albumin","PLASMA_Globulin","PLASMA_Fibrinogen","PLASMA_Lipoprotein"};
            var flow=world.GetComponent<JourneyBloodFlow>();int variant=0;
            for(int i=0;i<flow.cells.Length;i++)
            {
                if(flow.species[i]!="plasma-protein"&&!(flow.species[i]=="erythrocyte"&&i%23==6))continue;
                int type=variant++%4;var cell=flow.cells[i];var mesh=meshes[names[type]];cell.GetComponent<MeshFilter>().sharedMesh=mesh;cell.GetComponent<Renderer>().sharedMaterial=materials[type];
                float size=type==3?.095f:.075f,scale=size/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));cell.localScale=Vector3.one*scale;
                flow.species[i]=type==3?"lipoprotein":type==0?"albumin":type==1?"globulin":"fibrinogen";
            }
            // Shuffle complete authored entries rather than reassigning identities on camera turns.
            var rng=new System.Random(61957);
            for(int i=flow.cells.Length-1;i>0;i--)
            {
                int j=rng.Next(i+1);var cell=flow.cells[i];flow.cells[i]=flow.cells[j];flow.cells[j]=cell;
                var kind=flow.species[i];flow.species[i]=flow.species[j];flow.species[j]=kind;
            }
            flow.randomizedFlow=true;
        }
    }
}

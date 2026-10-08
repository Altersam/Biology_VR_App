using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class JourneyGeometry
    {
        public const string Root="Assets/BiologyVR/ArteryJourney";
        public static ArteryJourneyPath BuildPath()
        {
            var controls=new[]{
                new Vector3(-2,0,-9),new Vector3(0,0,0),new Vector3(1,0,10),
                new Vector3(4,-1,17),new Vector3(10,-7,24),new Vector3(13,-12,33),new Vector3(12,-12,40),
                new Vector3(15,-8,49),new Vector3(21,0,54),new Vector3(30,7,57),new Vector3(37,9,63),
                new Vector3(43,11,70),new Vector3(40,8,80),new Vector3(30,4,83),new Vector3(23,3,93),
                new Vector3(18,2,105),new Vector3(22,7,112),new Vector3(32,12,116),new Vector3(42,14,124),
                new Vector3(49,18,128),new Vector3(56,20,124),new Vector3(61,13,121),new Vector3(68,7,127),new Vector3(68,4,138),
                new Vector3(70,2,149),new Vector3(77,2,157),new Vector3(84,2,166),new Vector3(91,2,177),
                new Vector3(97,1,184),new Vector3(106,-4,191),new Vector3(106,-6,202),new Vector3(98,-2,210),
                 new Vector3(100,8,219),new Vector3(110,15,224),new Vector3(119,16,233),new Vector3(124,17,242),new Vector3(130,18,252),
                 new Vector3(139,17,276),new Vector3(143,12,298),new Vector3(137,5,318)
            };
            var path=Sample(controls,32);
            int[] anchors={1,2,6,10,14,18,18,23,27,27,27,27,34,34,35};
            path.sceneDistances=anchors.Select(a=>path.distances[a*32]).ToArray();
            path.locationIds=new[]{0,0,1,2,3,4,4,5,6,6,6,6,7,7,8};
            for(int i=0;i<path.points.Length;i++)
            {
                float s=path.distances[i];float r=3.50f+.075f*Mathf.Sin(s*.13f)+.035f*Mathf.Sin(s*.29f);
                foreach(int scene in new[]{3,6,7,8,11,15})r+=.42f*Mathf.Exp(-Mathf.Pow((s-path.Anchor(scene)-4)/7,2));
                path.radii[i]=r;
            }
            path.name="Spatial artery — shared narrative locations";
            ArteryGlbImporter.Folder(Root+"/Generated");Save(path,Root+"/Generated/SpatialRoute.asset");return AssetDatabase.LoadAssetAtPath<ArteryJourneyPath>(Root+"/Generated/SpatialRoute.asset");
        }
        public static ArteryJourneyPath Sample(Vector3[] p,int resolution)
        {
            var route=ScriptableObject.CreateInstance<ArteryJourneyPath>();var points=new List<Vector3>();
            for(int segment=0;segment<p.Length-1;segment++)for(int j=0;j<resolution;j++)
            {
                float t=j/(float)resolution;var a=p[Mathf.Max(0,segment-1)];var b=p[segment];var c=p[segment+1];var d=p[Mathf.Min(p.Length-1,segment+2)];
                points.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
            }
            points.Add(p[p.Length-1]);route.points=points.ToArray();route.distances=new float[points.Count];route.rights=new Vector3[points.Count];route.ups=new Vector3[points.Count];route.radii=Enumerable.Repeat(3.5f,points.Count).ToArray();
            var previousForward=(points[1]-points[0]).normalized;var right=Vector3.Cross(Vector3.up,previousForward).normalized;
            for(int i=0;i<points.Count;i++)
            {
                if(i>0)route.distances[i]=route.distances[i-1]+Vector3.Distance(points[i],points[i-1]);
                var forward=(points[Mathf.Min(points.Count-1,i+1)]-points[Mathf.Max(0,i-1)]).normalized;
                right=Quaternion.FromToRotation(previousForward,forward)*right;right=(right-forward*Vector3.Dot(right,forward)).normalized;
                route.rights[i]=right;route.ups[i]=Vector3.Cross(forward,right).normalized;previousForward=forward;
            }
            return route;
        }
        static float DistanceToSegments(Vector3 p,ArteryJourneyPath route)
        {
            float best=float.MaxValue;
            for(int i=0;i<route.points.Length-1;i+=3)
            {
                var a=route.points[i];var b=route.points[Mathf.Min(route.points.Length-1,i+3)];var v=b-a;
                float t=Mathf.Clamp01(Vector3.Dot(p-a,v)/Mathf.Max(.0001f,v.sqrMagnitude));best=Mathf.Min(best,(p-a-v*t).magnitude);
            }
            return best;
        }
        public static Mesh Tube(ArteryJourneyPath path,ArteryJourneyPath[] openings=null,bool sideBranch=false,ArteryJourneyPath main=null)
        {
             const int sides=64;var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var centres=new List<Vector3>();var indices=new List<int>();
             int rows=Mathf.CeilToInt(path.Length/.32f);
            for(int j=0;j<=rows;j++)
            {
                float s=path.Length*j/rows;var c=path.Centre(s);float r=path.Radius(s);
                for(int i=0;i<=sides;i++)
                {
                    float a=Mathf.PI*2*i/sides;var n=path.Right(s)*Mathf.Cos(a)+path.Up(s)*Mathf.Sin(a);
                    vertices.Add(c+n*r);normals.Add(-n);uv.Add(new Vector2(r*a/3.5f,s/5.6f));centres.Add(c);
                }
            }
            for(int j=0;j<rows;j++)for(int i=0;i<sides;i++)
            {
                int a=j*(sides+1)+i,b=a+1,c=a+sides+1,d=c+1;var midpoint=(vertices[a]+vertices[b]+vertices[c]+vertices[d])*.25f;
                ArteryJourneyPath intersection=null;float intersectionRadius=0;
                if(openings!=null)foreach(var branch in openings)if(DistanceToSegments(midpoint,branch)<2.48f){intersection=branch;intersectionRadius=2.08f;break;}
                if(sideBranch&&main!=null&&DistanceToSegments(midpoint,main)<path.Radius(0)+2.2f){intersection=main;intersectionRadius=main.Radius(main.Anchor(7));}
                float s=path.Length*(j+.5f)/rows;
                  if(intersection!=null)
                  {
                      ClipBranchTriangle(a,c,d,intersection,intersectionRadius,vertices,normals,uv,centres,indices);
                      ClipBranchTriangle(a,d,b,intersection,intersectionRadius,vertices,normals,uv,centres,indices);continue;
                  }
                 if(!sideBranch&&Mathf.Abs(s-path.Anchor(11)-5)<1.5f&&(i<4||i>sides-5))
                 {
                     ClipWoundTriangle(a,c,d,rows,sides,path,vertices,normals,uv,centres,indices);
                     ClipWoundTriangle(a,d,b,rows,sides,path,vertices,normals,uv,centres,indices);
                     continue;
                 }
                 indices.AddRange(new[]{a,c,d,a,d,b});
            }
            if(sideBranch)
            {
                int last=rows*(sides+1);int centre=vertices.Count;vertices.Add(path.Centre(path.Length));normals.Add(-path.Forward(path.Length));uv.Add(Vector2.one);centres.Add(path.Centre(path.Length));
                for(int i=0;i<sides;i++)indices.AddRange(new[]{centre,last+i+1,last+i});
            }
            var mesh=new Mesh {name=sideBranch?"Organic branch lumen":"Spatial artery lumen",indexFormat=IndexFormat.UInt32};
             mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetUVs(1,centres);mesh.SetTriangles(indices,0);mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
         }
         struct WoundVertex {public int index;public Vector2 chart;}
         static void ClipBranchTriangle(int a,int b,int c,ArteryJourneyPath opening,float radius,List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<Vector3> centres,List<int> indices)
         {
             var input=new[]{a,b,c};var polygon=new List<int>(4);
             for(int i=0;i<3;i++)
             {
                 int start=input[(i+2)%3],end=input[i];bool outsideStart=DistanceToSegments(vertices[start],opening)>=radius,outsideEnd=DistanceToSegments(vertices[end],opening)>=radius;
                 if(outsideStart!=outsideEnd)
                 {
                     float low=0,high=1;for(int iteration=0;iteration<10;iteration++){float middle=(low+high)*.5f;bool outside=DistanceToSegments(Vector3.Lerp(vertices[start],vertices[end],middle),opening)>=radius;if(outside==outsideStart)low=middle;else high=middle;}
                     float t=(low+high)*.5f;int index=vertices.Count;vertices.Add(Vector3.Lerp(vertices[start],vertices[end],t));normals.Add(Vector3.Lerp(normals[start],normals[end],t).normalized);uv.Add(Vector2.Lerp(uv[start],uv[end],t));centres.Add(Vector3.Lerp(centres[start],centres[end],t));polygon.Add(index);
                 }
                 if(outsideEnd)polygon.Add(end);
             }
             for(int i=1;i+1<polygon.Count;i++)indices.AddRange(new[]{polygon[0],polygon[i],polygon[i+1]});
         }
         static void ClipWoundTriangle(int a,int b,int c,int rows,int sides,ArteryJourneyPath path,List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<Vector3> centres,List<int> indices)
         {
             var input=new[]{WoundCoordinate(a,rows,sides,path),WoundCoordinate(b,rows,sides,path),WoundCoordinate(c,rows,sides,path)};
             var polygon=new List<WoundVertex>(4);
             for(int i=0;i<3;i++)
             {
                 var start=input[(i+2)%3];var end=input[i];bool insideStart=start.chart.sqrMagnitude>=1,insideEnd=end.chart.sqrMagnitude>=1;
                 if(insideStart!=insideEnd)
                 {
                     var delta=end.chart-start.chart;float aa=delta.sqrMagnitude,bb=2*Vector2.Dot(start.chart,delta),cc=start.chart.sqrMagnitude-1;
                     float disc=Mathf.Sqrt(Mathf.Max(0,bb*bb-4*aa*cc));float t1=(-bb-disc)/(2*Mathf.Max(.00001f,aa)),t2=(-bb+disc)/(2*Mathf.Max(.00001f,aa));float t=Mathf.Clamp01(t1>=0&&t1<=1?t1:t2);
                     int k=vertices.Count;vertices.Add(Vector3.Lerp(vertices[start.index],vertices[end.index],t));normals.Add(Vector3.Lerp(normals[start.index],normals[end.index],t).normalized);uv.Add(Vector2.Lerp(uv[start.index],uv[end.index],t));centres.Add(Vector3.Lerp(centres[start.index],centres[end.index],t));
                     polygon.Add(new WoundVertex{index=k,chart=Vector2.Lerp(start.chart,end.chart,t)});
                 }
                 if(insideEnd)polygon.Add(end);
             }
             for(int i=1;i+1<polygon.Count;i++)indices.AddRange(new[]{polygon[0].index,polygon[i].index,polygon[i+1].index});
         }
         static WoundVertex WoundCoordinate(int index,int rows,int sides,ArteryJourneyPath path)
         {
             float distance=path.Length*(index/(sides+1))/(float)rows;float angle=2*Mathf.PI*(index%(sides+1))/sides;if(angle>Mathf.PI)angle-=2*Mathf.PI;
             return new WoundVertex{index=index,chart=new Vector2((distance-path.Anchor(11)-5)/1.15f,angle/.28f)};
         }
        public static ArteryJourneyPath[] Branches(ArteryJourneyPath main)
        {
            var list=new List<ArteryJourneyPath>();
            foreach(var spec in new[]{(scene:7,delta:-11f,x:1f,y:1f),(scene:10,delta:-11f,x:-1f,y:-1f),(scene:15,delta:-16f,x:1f,y:-.65f)})
            {
                float s=main.Anchor(spec.scene)+spec.delta;var c=main.Centre(s);var u=main.Right(s);var v=main.Up(s);var t=main.Forward(s);
                var branch=Sample(new[]{c,c+u*spec.x*3+v*spec.y*1.5f+t*1.2f,c+u*spec.x*8+v*spec.y*5+t*4,c+u*spec.x*15+v*spec.y*9+t*9},24);
                for(int i=0;i<branch.radii.Length;i++)branch.radii[i]=2.08f+.10f*Mathf.Sin(i*.04f);list.Add(branch);
            }
            return list.ToArray();
        }
        public static Material VesselMaterial()
        {
             const int size=2048;var albedo=new Texture2D(size,size,TextureFormat.RGBA32,true,false){name="Endothelium stretched flat cells",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
             var normal=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name="Endothelium gentle membrane normal",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            float[] heights=new float[size*size];Color[] colors=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                 float px=x/(float)size*12,py=y/(float)size*4;float best=100,next=100;Vector2 near=Vector2.zero;
                int ix=Mathf.FloorToInt(px),iy=Mathf.FloorToInt(py);
                for(int a=-1;a<=1;a++)for(int b=-1;b<=1;b++)
                {
                     int cx=ix+a,cy=iy+b;float sx=cx+.5f+.24f*Mathf.Sin((cx%12+12)%12*4.71f+(cy%4+4)%4*1.83f);float sy=cy+.5f+.14f*Mathf.Cos((cx%12+12)%12*1.9f+(cy%4+4)%4*3.37f);
                    var delta=new Vector2(px-sx,py-sy);float d=delta.sqrMagnitude;
                    if(d<best){next=best;best=d;near=delta;}else if(d<next)next=d;
                }
                 float border=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((next-best)/.075f));float nucleus=Mathf.Exp(-(near.x*near.x/.032f+near.y*near.y/.065f));
                 float membrane=Mathf.PerlinNoise(px*7.3f,py*9.1f);
                 heights[y*size+x]=.003f*(1-border)+.009f*nucleus+.0015f*membrane;
                 float variation=.94f+.065f*Mathf.PerlinNoise(px*1.7f,py*2.8f);
                 colors[y*size+x]=new Color(.61f,.18f,.235f)*variation*(1-border*.035f);
                 colors[y*size+x]=Color.Lerp(colors[y*size+x],new Color(.46f,.10f,.20f),nucleus*.55f)*(1+(membrane-.5f)*.024f);
            }
            Color[] ns=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                 float dx=heights[y*size+(x+1)%size]-heights[y*size+(x+size-1)%size];float dy=heights[((y+1)%size)*size+x]-heights[((y+size-1)%size)*size+x];var n=new Vector3(-dx*48,-dy*32,1).normalized;ns[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            albedo.SetPixels(colors);albedo.Apply();normal.SetPixels(ns);normal.Apply();
            Save(albedo,Root+"/Generated/EndotheliumColor.asset");Save(normal,Root+"/Generated/EndotheliumNormal.asset");
             var m=new Material(Shader.Find("BiologyVR/Cellular Vessel")){name="Cellular organic coral lumen"};m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Generated/EndotheliumColor.asset"));m.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Generated/EndotheliumNormal.asset"));m.SetFloat("_PulseAmplitude",.018f);m.SetFloat("_Wetness",.72f);Save(m,Root+"/Generated/CellularVessel.mat");return AssetDatabase.LoadAssetAtPath<Material>(Root+"/Generated/CellularVessel.mat");
        }
        public static void Save(UnityEngine.Object value,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if(existing==value){EditorUtility.SetDirty(existing);return;}
             if(existing)
             {
                  if(existing is Mesh mesh&&value is Mesh source)
                  {
                      // CopySerialized updates serialized mesh data/bounds but can leave
                      // existing native GPU buffers stale. Rebuild through the Mesh API,
                      // retaining the asset GUID used by scenes, colliders and LODs.
                      mesh.Clear(false);mesh.indexFormat=source.indexFormat;mesh.name=source.name;
                      mesh.vertices=source.vertices;mesh.normals=source.normals;mesh.tangents=source.tangents;
                      if(source.colors32.Length>0)mesh.colors32=source.colors32;
                      if(source.boneWeights.Length>0){mesh.bindposes=source.bindposes;mesh.boneWeights=source.boneWeights;}
                      for(int channel=0;channel<8;channel++)
                      {
                          var attribute=(VertexAttribute)((int)VertexAttribute.TexCoord0+channel);
                          if(!source.HasVertexAttribute(attribute))continue;
                          int dimension=source.GetVertexAttributeDimension(attribute);
                          if(dimension==2){var uv=new List<Vector2>();source.GetUVs(channel,uv);mesh.SetUVs(channel,uv);}
                          else if(dimension==3){var uv=new List<Vector3>();source.GetUVs(channel,uv);mesh.SetUVs(channel,uv);}
                          else{var uv=new List<Vector4>();source.GetUVs(channel,uv);mesh.SetUVs(channel,uv);}
                      }
                      mesh.subMeshCount=source.subMeshCount;
                      for(int i=0;i<source.subMeshCount;i++)mesh.SetIndices(source.GetIndices(i),source.GetTopology(i),i,false);
                      mesh.RecalculateBounds();mesh.UploadMeshData(false);
                  }
                  else EditorUtility.CopySerialized(value,existing);
                 if(existing is Texture2D texture)texture.Apply();
                 EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(value);
             }
             else AssetDatabase.CreateAsset(value,path);
        }
    }
}

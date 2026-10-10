using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>Actual lightweight meshes, in existing emitter-local coordinates; no physics components.</summary>
    public static class BiomedicalDeviceVisualBuilder
    {
        static readonly Color Cream=new Color(.86f,.88f,.83f,0),Dark=new Color(.095f,.14f,.17f,0),Cyan=new Color(.28f,.83f,.94f,.35f),Amber=new Color(.95f,.65f,.22f,.22f);
        public static GameObject Build(string kind)
        {
            string folder=ActiveInstrumentBuilder.Folder+"/"+kind;
            var shader=Shader.Find("BiologyVR/Biomedical Device");if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Biomedical device shader failed");
            var mat=new Material(shader){name="BIO XR — shared opaque medical palette",enableInstancing=true};JourneyGeometry.Save(mat,ActiveInstrumentBuilder.Folder+"/BiomedicalPalette.mat");mat=AssetDatabase.LoadAssetAtPath<Material>(ActiveInstrumentBuilder.Folder+"/BiomedicalPalette.mat");
            var root=new GameObject("Active "+kind+" — biomedical instrument");var view=root.AddComponent<JourneyInstrumentVisual>();view.scanner=kind=="Scanner";view.vertexPalette=true;
            var surfaces=new List<Renderer>();var body=new Draft();
            if(view.scanner)
            {
                body.Ellipsoid(new Vector3(0,.003f,-.112f),new Vector3(.102f,.067f,.158f),Cream);
                body.Ellipsoid(new Vector3(0,-.087f,-.108f),new Vector3(.046f,.132f,.048f),Dark);
                body.Ellipsoid(new Vector3(0,-.043f,-.123f),new Vector3(.064f,.044f,.049f),Cream);
                body.Ellipsoid(new Vector3(0,.031f,-.108f),new Vector3(.075f,.023f,.106f),Dark);
                body.Box(new Vector3(0,.043f,-.124f),new Vector3(.046f,.004f,.042f),Cyan);
                for(int i=0;i<4;i++)body.Box(new Vector3(0,-.062f-i*.018f,-.132f),new Vector3(.033f,.004f,.008f),new Color(.22f,.27f,.29f,0));
                body.Arc(new Vector3(0,.003f,-.181f),.027f,.031f,.008f,360,Dark);
                for(int side=-1;side<=1;side+=2)
                {body.Box(new Vector3(side*.047f,.007f,-.096f),new Vector3(.003f,.020f,.045f),Dark);body.Box(new Vector3(side*.049f,.007f,-.095f),new Vector3(.002f,.003f,.029f),Cyan);}
                body.Box(new Vector3(.034f,.026f,-.075f),new Vector3(.008f,.006f,.012f),Amber);
                body.Arc(new Vector3(0,0,-.018f),.044f,.052f,.008f,360,Cream);
                body.Ellipsoid(new Vector3(0,0,-.026f),new Vector3(.038f,.038f,.032f),Dark);
                var rotor=new Draft();rotor.Arc(Vector3.zero,.051f,.064f,.010f,270,Dark);rotor.Arc(new Vector3(0,0,.006f),.052f,.056f,.003f,270,Cyan);
                view.rotor=Part(root.transform,"Rotor",rotor,folder,mat,surfaces).transform;
                view.prongs=Array.Empty<Transform>();
            }
            else
            {
                body.Ellipsoid(new Vector3(0,.012f,-.135f),new Vector3(.128f,.092f,.186f),Cream);
                body.Ellipsoid(new Vector3(0,-.082f,-.122f),new Vector3(.059f,.118f,.060f),Dark);
                body.Ellipsoid(new Vector3(0,-.039f,-.145f),new Vector3(.088f,.046f,.070f),Cream);
                body.Arc(new Vector3(0,0,-.022f),.036f,.057f,.016f,360,Dark);
                body.Arc(new Vector3(0,0,-.011f),.038f,.043f,.004f,360,Cyan);
                body.Ellipsoid(new Vector3(0,.050f,-.122f),new Vector3(.085f,.024f,.115f),Dark);
                body.Box(new Vector3(0,.062f,-.132f),new Vector3(.051f,.003f,.046f),Cyan);
                body.Arc(new Vector3(0,.012f,-.211f),.025f,.037f,.016f,360,Dark);
                body.Arc(new Vector3(0,.012f,-.220f),.025f,.031f,.003f,360,Amber);
                for(int i=0;i<4;i++)body.Box(new Vector3(0,-.062f-i*.017f,-.151f),new Vector3(.038f,.004f,.008f),new Color(.22f,.27f,.29f,0));
                for(int side=-1;side<=1;side+=2)
                {body.Box(new Vector3(side*.061f,.020f,-.130f),new Vector3(.003f,.025f,.045f),Dark);body.Box(new Vector3(side*.063f,.016f,-.133f),new Vector3(.002f,.004f,.026f),Cyan);}
                for(int i=0;i<3;i++)body.Box(new Vector3(.065f,.013f+i*.012f,-.114f),new Vector3(.003f,.006f,.026f),i==2?Amber:Dark);
                view.prongs=new Transform[4];
                for(int i=0;i<4;i++)
                {
                    var petal=new Draft();petal.Ellipsoid(new Vector3(0,0,.027f),new Vector3(.021f,.021f,.061f),Dark,6,12);
                    petal.Ellipsoid(new Vector3(0,-.005f,.055f),new Vector3(.026f,.022f,.030f),Cream,6,12);
                    petal.Box(new Vector3(0,.011f,.032f),new Vector3(.009f,.003f,.038f),Cyan);
                    var part=Part(root.transform,"Prong"+i,petal,folder,mat,surfaces);part.transform.localPosition=Quaternion.AngleAxis(i*90,Vector3.forward)*new Vector3(0,.044f,.016f);part.transform.localRotation=Quaternion.AngleAxis(i*90,Vector3.forward);view.prongs[i]=part.transform;
                }
            }
            Part(root.transform,"Body",body,folder,mat,surfaces);view.surfaces=surfaces.ToArray();
            view.muzzle=new GameObject("Active muzzle").transform;view.muzzle.SetParent(root.transform,false);
            view.modeCaption=ArteryAnnotations.Label(root.transform,"Active instrument mode",view.scanner?"BIO / SCANNER":"BIO / TOOL");view.modeCaption.fontSize=16;view.modeCaption.rectTransform.sizeDelta=new Vector2(370,54);view.modeCaption.transform.localScale=Vector3.one*.0006f;view.modeCaption.transform.localPosition=new Vector3(0,.085f,-.08f);
            var signal=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="BIO XR — cyan signal"};signal.SetColor("_BaseColor",HolographicStyle.SoftGlow*.8f);JourneyGeometry.Save(signal,folder+"/ModeGlyph.mat");signal=AssetDatabase.LoadAssetAtPath<Material>(folder+"/ModeGlyph.mat");
            view.modeGlyph=Line(root.transform,"Active small mode glyph",signal,true);view.modeGlyph.transform.localPosition=new Vector3(0,.055f,-.06f);view.modeGlyph.widthMultiplier=.0012f;
            view.activeRim=Line(view.scanner?view.rotor:root.transform,"Active working front signal",signal,!view.scanner);view.activeRim.transform.localPosition=new Vector3(0,0,view.scanner?.008f:.042f);view.activeRim.positionCount=33;
            for(int i=0;i<33;i++){float a=(view.scanner?(-135+i*270f/32):i*360f/33)*Mathf.Deg2Rad;float radius=view.scanner?.054f:.037f;view.activeRim.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/Active"+kind+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static GameObject Part(Transform parent,string name,Draft draft,string folder,Material mat,List<Renderer> surfaces)
        {var mesh=draft.Mesh(name);JourneyGeometry.Save(mesh,folder+"/"+(parent.name.Contains("Scanner")?"Scanner":"BioTool")+"_"+name+".asset");var go=new GameObject("Active "+name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/"+(parent.name.Contains("Scanner")?"Scanner":"BioTool")+"_"+name+".asset");var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;surfaces.Add(renderer);return go;}
        static LineRenderer Line(Transform parent,string name,Material mat,bool loop)
        {var go=new GameObject(name,typeof(LineRenderer));go.transform.SetParent(parent,false);var line=go.GetComponent<LineRenderer>();line.sharedMaterial=mat;line.useWorldSpace=false;line.loop=loop;line.enabled=false;line.shadowCastingMode=ShadowCastingMode.Off;return line;}
        sealed class Draft
        {
            readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();readonly List<Color> colors=new List<Color>();readonly List<int> triangles=new List<int>();
            void Vertex(Vector3 v,Vector3 n,Color c){vertices.Add(v);normals.Add(n);colors.Add(c);}
            void Tri(int a,int b,int c){if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normals[a]+normals[b]+normals[c])<0){int t=b;b=c;c=t;}triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            public void Ellipsoid(Vector3 center,Vector3 size,Color color,int rings=8,int sides=16)
            {int start=vertices.Count;Vector3 half=size*.5f;for(int r=0;r<=rings;r++)for(int s=0;s<=sides;s++){float theta=r*Mathf.PI/rings,phi=s*Mathf.PI*2/sides;var n=new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Cos(theta),Mathf.Sin(theta)*Mathf.Sin(phi));Vertex(center+Vector3.Scale(n,half),new Vector3(n.x/half.x,n.y/half.y,n.z/half.z).normalized,color);}for(int r=0;r<rings;r++)for(int s=0;s<sides;s++){int a=start+r*(sides+1)+s,b=a+sides+1;Tri(a,b,a+1);Tri(a+1,b,b+1);}}
            public void Box(Vector3 center,Vector3 size,Color color)
            {var half=size*.5f;for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2){var n=Vector3.zero;n[axis]=sign;int a=(axis+1)%3,b=(axis+2)%3,start=vertices.Count;foreach(var p in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)}){var v=center;v[axis]+=half[axis]*sign;v[a]+=half[a]*p.x;v[b]+=half[b]*p.y;Vertex(v,n,color);}Tri(start,start+1,start+2);Tri(start,start+2,start+3);}}
            public void Arc(Vector3 center,float inner,float outer,float depth,float degrees,Color color)
            {const int steps=24;for(int surface=0;surface<4;surface++){int start=vertices.Count;for(int i=0;i<=steps;i++){float a=(-degrees*.5f+i*degrees/steps)*Mathf.Deg2Rad;var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);for(int side=0;side<2;side++){float radius=surface<2?(side==0?inner:outer):(surface==2?inner:outer);float z=surface<2?(surface==0?-depth*.5f:depth*.5f):(side==0?-depth*.5f:depth*.5f);var n=surface<2?(surface==0?Vector3.back:Vector3.forward):radial*(surface==2?-1:1);Vertex(center+radial*radius+Vector3.forward*z,n,color);}}for(int i=0;i<steps;i++){int a=start+i*2;Tri(a,a+1,a+2);Tri(a+1,a+3,a+2);}}}
            public Mesh Mesh(string name){var mesh=new Mesh{name="Biomedical "+name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;}
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>Small baked tissue atlases; all pattern generation happens in the Editor.</summary>
    public static class ArteryTissueTextures
    {
        public static void Bake(string name,int kind,int size=512)
        {
            string folder=ArteryScenePolish.Folder;
            var colors=new Color[size*size];var heights=new float[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size,h;
                Color c;
                if(kind==0)
                {
                    float px=u*8+.13f*Mathf.Sin(v*Mathf.PI*4),py=v*5+.11f*Mathf.Sin(u*Mathf.PI*4),best=99,second=99,seed=0;Vector2 centre=Vector2.zero;
                    int ix=Mathf.FloorToInt(px),iy=Mathf.FloorToInt(py);
                    for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++)
                    {
                        float k=Hash(ix+i,iy+j,8,5);
                        var d=new Vector2((px-ix-i-.5f-(k-.5f)*.44f)*.75f,py-iy-j-.5f-(Hash(ix+i+3,iy+j,8,5)-.5f)*.30f);
                        float ds=d.sqrMagnitude;
                        if(ds<best){second=best;best=ds;centre=d;seed=k;}else if(ds<second)second=ds;
                    }
                    float seam=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((second-best)/.045f));
                    float dome=Mathf.Exp(-best*3.2f),nucleus=Mathf.Exp(-centre.x*centre.x/.017f-centre.y*centre.y/.047f);
                    c=Color.Lerp(new Color(.83f,.38f,.43f),new Color(.96f,.53f,.53f),dome*.45f+seed*.22f);
                    c*=1-seam*.014f;c=Color.Lerp(c,new Color(.65f,.39f,.57f),nucleus*.29f);
                    h=dome*.009f-seam*.0006f+nucleus*.0018f;
                }
                else if(kind==1)
                {
                    float fibre=Mathf.Pow(.5f+.5f*Mathf.Sin((u*11f+Mathf.Sin(v*Mathf.PI*4)*.23f)*Mathf.PI*2),8);
                    float cross=Mathf.Pow(.5f+.5f*Mathf.Sin((v*7f+u*3f)*Mathf.PI*2),12);
                    c=Color.Lerp(new Color(.84f,.47f,.51f),new Color(.98f,.72f,.70f),fibre*.32f+cross*.15f);
                    h=fibre*.013f+cross*.004f;
                }
                else if(kind==2)
                {
                    float spindle=.5f+.5f*Mathf.Sin((u*16f+Mathf.Sin(v*Mathf.PI*4)*.25f)*Mathf.PI*2);
                    float muscle=Mathf.Pow(spindle,.42f),strand=Mathf.Pow(spindle,18);
                    float nuclei=Mathf.Pow(.5f+.5f*Mathf.Sin(v*Mathf.PI*12),20)*Mathf.Pow(spindle,12);
                    c=Color.Lerp(new Color(.72f,.30f,.38f),new Color(.96f,.53f,.56f),muscle*.75f);
                    c=Color.Lerp(c,new Color(.61f,.34f,.54f),nuclei*.30f);h=muscle*.009f+strand*.003f;
                }
                else
                {
                    float a=Mathf.Pow(.5f+.5f*Mathf.Sin((u*9+v*4+Mathf.Sin(v*Mathf.PI*4)*.14f)*Mathf.PI*2),16);
                    float b=Mathf.Pow(.5f+.5f*Mathf.Sin((v*8-u*3+Mathf.Sin(u*Mathf.PI*2)*.16f)*Mathf.PI*2),16);
                    c=Color.Lerp(new Color(.82f,.71f,.75f),new Color(.98f,.89f,.84f),Mathf.Clamp01(.31f+a*.28f+b*.28f));
                    h=a*.007f+b*.007f;
                }
                colors[y*size+x]=c;heights[y*size+x]=h;
            }
            var normals=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=heights[y*size+(x+1)%size]-heights[y*size+(x+size-1)%size];
                float dy=heights[((y+1)%size)*size+x]-heights[((y+size-1)%size)*size+x];
                var n=new Vector3(-dx*size*.55f,-dy*size*.55f,1).normalized;
                normals[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            Save(folder+"/"+name+"_Base.png",colors,size,false);
            Save(folder+"/"+name+"_Normal.png",normals,size,true);
        }
        static float Hash(int x,int y,int w,int h)
        {x=(x%w+w)%w;y=(y%h+h)%h;return Mathf.Repeat(Mathf.Sin(x*127.1f+y*311.7f)*4375.854f,1);}
        static void Save(string path,Color[] pixels,int size,bool linear)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,linear);texture.SetPixels(pixels);texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=!linear;importer.mipmapEnabled=true;
            importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;
            var mobile=importer.GetPlatformTextureSettings("Android");mobile.overridden=true;mobile.maxTextureSize=size;mobile.format=TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(mobile);importer.SaveAndReimport();
        }
    }
}

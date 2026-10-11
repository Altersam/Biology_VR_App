using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Texture-free projection rails and bevelled corner brackets; no overlay over text.</summary>
    public sealed class JourneyHolographicFrameGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=GetPixelAdjustedRect();if(rect.width<40||rect.height<40)return;
            float left=rect.xMin+7,right=rect.xMax-7,bottom=rect.yMin+7,top=rect.yMax-7;
            var corners=new[]{new Vector2(left+22,top),new Vector2(right-22,top),new Vector2(right,top-22),new Vector2(right,bottom+22),new Vector2(right-22,bottom),new Vector2(left+22,bottom),new Vector2(left,bottom+22),new Vector2(left,top-22)};
            for(int i=0;i<corners.Length;i++)Line(mesh,corners[i],corners[(i+1)%corners.Length],1.5f,Alpha(.32f));
            foreach(float x in new[]{left,right})foreach(float y in new[]{bottom,top})
            {
                float sx=x==left?1:-1,sy=y==bottom?1:-1;
                Line(mesh,new Vector2(x,y+sy*44),new Vector2(x,y+sy*22),2.7f,color);
                Line(mesh,new Vector2(x,y+sy*22),new Vector2(x+sx*22,y),2.7f,color);
                Line(mesh,new Vector2(x+sx*22,y),new Vector2(x+sx*65,y),2.7f,color);
            }
            for(int i=0;i<11;i++)
            {
                float y=Mathf.Lerp(bottom+62,top-62,i/10f);float reach=i%5==0?10:5;
                Line(mesh,new Vector2(left-2,y),new Vector2(left+reach,y),1.2f,Alpha(.45f));
                Line(mesh,new Vector2(right+2,y),new Vector2(right-reach,y),1.2f,Alpha(.45f));
            }
            Line(mesh,new Vector2(left+82,top-11),new Vector2(right-82,top-11),1,Alpha(.18f));
            Line(mesh,new Vector2(left+82,bottom+11),new Vector2(right-82,bottom+11),1,Alpha(.18f));
        }
        Color Alpha(float amount)=>new Color(color.r,color.g,color.b,color.a*amount);
        static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
        {
            int index=mesh.currentVertCount;var delta=(b-a).normalized;var side=new Vector2(-delta.y,delta.x)*width*.5f;
            mesh.AddVert(a-side,tint,Vector2.zero);mesh.AddVert(a+side,tint,Vector2.zero);mesh.AddVert(b+side,tint,Vector2.zero);mesh.AddVert(b-side,tint,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);
        }
    }
}

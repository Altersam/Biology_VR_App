using System;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    [CreateAssetMenu(menuName="Biology VR/Spatial artery path")]
    public sealed class ArteryJourneyPath : ScriptableObject
    {
        public Vector3[] points,rights,ups;
        public float[] distances,radii;
        // Explicit story anchors. Duplicates mean a new scene at the SAME physical site.
        public float[] sceneDistances=new float[15];
        public int[] locationIds={0,0,1,2,3,4,4,5,6,6,6,6,7,7,8};
        public float Length=>distances[distances.Length-1];
        public int Index(float s)
        {
            int i=Array.BinarySearch(distances,Mathf.Clamp(s,0,Length));
            if(i<0)i=~i-1;return Mathf.Clamp(i,0,points.Length-2);
        }
        float T(int i,float s)=>Mathf.Clamp01((s-distances[i])/Mathf.Max(.0001f,distances[i+1]-distances[i]));
        public Vector3 Centre(float s){int i=Index(s);return Vector3.Lerp(points[i],points[i+1],T(i,s));}
        public Vector3 Forward(float s){int i=Index(s);return (points[Mathf.Min(i+2,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;}
        public Vector3 Right(float s){int i=Index(s);return Vector3.Lerp(rights[i],rights[i+1],T(i,s)).normalized;}
        public Vector3 Up(float s){int i=Index(s);return Vector3.Lerp(ups[i],ups[i+1],T(i,s)).normalized;}
        public Vector3 Offset(float s,float x,float z)=>Centre(s)+Right(s)*x+Up(s)*z;
        public Quaternion Frame(float s)=>Quaternion.LookRotation(Forward(s),Up(s));
        public float Radius(float s){int i=Index(s);return Mathf.Lerp(radii[i],radii[i+1],T(i,s));}
        public float Anchor(int scene)=>sceneDistances[Mathf.Clamp(scene-3,0,sceneDistances.Length-1)];
    }
}

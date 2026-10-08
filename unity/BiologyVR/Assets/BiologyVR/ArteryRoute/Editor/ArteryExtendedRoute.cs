using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryExtendedRoute
    {
        /// <summary>Keep every narrative position/frame; append long bent approach/departure passages.</summary>
        public static void Apply(JourneyWorld world)
        {
            var source=world.mover.path;var first=source.points[0];var last=source.points[source.points.Length-1];
            var f=source.Forward(0);var r=source.Right(0);var u=source.Up(0);
            var lead=JourneyGeometry.Sample(new[]{first-f*96-r*17-u*2,first-f*75-r*4,first-f*52+r*14+u*2,first-f*32-r*9+u*2,first-f*17-r*9,first-f*6,first,first+f*8},24);
            int prefixCount=(8-2)*24;
            var tailF=source.Forward(source.Length);var tailR=source.Right(source.Length);var tailU=source.Up(source.Length);
            var tail=JourneyGeometry.Sample(new[]{last-tailF*7,last,last+tailF*9,last+tailF*26+tailR*13,last+tailF*47-tailR*5+tailU*4,last+tailF*75-tailR*21+tailU*4,last+tailF*105-tailR*16},24);
            var points=new List<Vector3>();points.AddRange(lead.points.Take(prefixCount));points.AddRange(source.points);points.AddRange(tail.points.Skip(25));
            var path=ScriptableObject.CreateInstance<ArteryJourneyPath>();path.name="Extended artery — hidden bent ends";
            path.points=points.ToArray();int count=points.Count;path.distances=new float[count];path.rights=new Vector3[count];path.ups=new Vector3[count];path.radii=new float[count];
            for(int i=0;i<count;i++)
            {
                if(i>0)path.distances[i]=path.distances[i-1]+Vector3.Distance(points[i],points[i-1]);
                if(i<prefixCount){path.rights[i]=lead.rights[i];path.ups[i]=lead.ups[i];path.radii[i]=source.radii[0];}
                else if(i<prefixCount+source.points.Length)
                {int old=i-prefixCount;path.rights[i]=source.rights[old];path.ups[i]=source.ups[old];path.radii[i]=source.radii[old];}
                else{int index=i-prefixCount-source.points.Length+25;path.rights[i]=tail.rights[index];path.ups[i]=tail.ups[index];path.radii[i]=source.radii[source.radii.Length-1];}
            }
            // Align the last approach frames with the existing vessel's first ring.
            for(int i=prefixCount-24;i<prefixCount;i++)
            {
                float t=(i-prefixCount+24)/24f;var forward=path.Forward(path.distances[i]);
                var right=Vector3.ProjectOnPlane(Vector3.Slerp(lead.rights[i],source.rights[0],t),forward).normalized;
                path.rights[i]=right;path.ups[i]=Vector3.Cross(forward,right).normalized;
            }
            float offset=path.distances[prefixCount];path.sceneDistances=source.sceneDistances.Select(s=>s+offset).ToArray();path.locationIds=(int[])source.locationIds.Clone();
            string asset=ArteryScenePolish.Folder+"/ExtendedSpatialRoute.asset";JourneyGeometry.Save(path,asset);path=AssetDatabase.LoadAssetAtPath<ArteryJourneyPath>(asset);
            world.mover.path=path;var flow=world.GetComponent<JourneyBloodFlow>();if(flow)flow.path=path;
            var tube=GameObject.Find("Continuous Main Artery");var mesh=JourneyGeometry.Tube(path,JourneyGeometry.Branches(path));
            string meshPath=ArteryScenePolish.Folder+"/ExtendedMainArtery.asset";JourneyGeometry.Save(mesh,meshPath);
            mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);tube.GetComponent<MeshFilter>().sharedMesh=mesh;tube.GetComponent<MeshCollider>().sharedMesh=mesh;
        }
    }
}

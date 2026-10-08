using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryFlowContinuityValidation
    {
        [MenuItem("Biology VR/Validate Two Way Blood Flow")]
        public static void Run()
        {if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");EditorCoroutineUtility.StartCoroutineOwnerless(Check());}
        static IEnumerator Check()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var w=m.world;var camera=m.mover.viewCamera;
            var flow=w.GetComponent<JourneyBloodFlow>();var checks=new Dictionary<string,bool>();var errors=new List<string>();var details=new List<object>();bool done=false;
            var rotation=camera.transform.rotation;
            string folder="Flow_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(ArteryPolishReview.Reports+"/"+folder);var captures=new List<string>();
            string Capture(string name){string relative=folder+"/"+name;ArteryWallInteractionValidation.Capture(w,relative);string file=ArteryPolishReview.Reports+"/"+relative;captures.Add(file);return file;}
            void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
            Application.logMessageReceived+=Log;
            try
            {
                m.Enter(3,false);yield return new EditorWaitForSeconds(.3f);rotation=camera.transform.rotation;
                var forward=m.mover.path.Forward(m.mover.Distance);
                var behind=flow.cells.Where(c=>c&&c.GetComponent<Renderer>().enabled&&Vector3.Dot(c.position-camera.transform.position,forward)<-.5f&&(c.position-camera.transform.position).sqrMagnitude<625).ToArray();
                checks["cellsAlreadyMovingBehindPlayer"]=behind.Length>2;
                var positions=behind.Select(c=>c.position).ToArray();
                camera.transform.rotation=Quaternion.LookRotation(-forward,m.mover.path.Up(m.mover.Distance));
                yield return new EditorWaitForSeconds(.25f);
                checks["headTurnDoesNotTeleportCells"]=behind.Where((c,i)=>Vector3.Distance(c.position,positions[i])>.002f&&Vector3.Distance(c.position,positions[i])<1f).Count()==behind.Length;
                checks["backwardCellsStayVisible"]=behind.All(c=>c.GetComponent<Renderer>().enabled);
                var planes=GeometryUtility.CalculateFrustumPlanes(camera);
                details.Add(new{phase="backward",camera=camera.transform.position.ToString(),direction=camera.transform.forward.ToString(),distance=m.mover.Distance,inspection=Shader.GetGlobalFloat("_InspectionReveal"),walls=w.art.wallChunks.Where(r=>r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds)).Select(r=>new{name=r.name,centre=r.bounds.center.ToString(),vertices=r.GetComponent<MeshFilter>().sharedMesh.vertexCount}).ToArray()});
                string backImage=Capture("Artery_Start_Backward_Continuous.png");
                checks["backwardImageContainsSurroundingWall"]=HasWallCoverage(backImage);
                var saved=w.art.wallChunks.Select(r=>r.sharedMaterial).ToArray();var diagnostic=new Material(Shader.Find("Universal Render Pipeline/Lit"));diagnostic.SetColor("_BaseColor",new Color(.9f,.45f,.5f));diagnostic.SetFloat("_Cull",0);
                try{foreach(var surface in w.art.wallChunks)surface.sharedMaterial=diagnostic;Capture("Artery_Backward_Geometry_Diagnostic.png");}
                finally{for(int i=0;i<saved.Length;i++)w.art.wallChunks[i].sharedMaterial=saved[i];UnityEngine.Object.DestroyImmediate(diagnostic);}
                camera.transform.rotation=rotation;Capture("Artery_Start_Forward_Continuous.png");
                var path=m.mover.path;
                checks["flowWrapOutsideBothViewDirections"]=flow.flowBehind>camera.farClipPlane&&flow.flowSpan-flow.flowBehind>camera.farClipPlane;
                checks["entryEndpointBeyondFarClip"]=Vector3.Distance(path.points[0],camera.transform.position)>camera.farClipPlane;
                checks["startHasLongApproach"]=path.Anchor(3)>flow.flowBehind+15;
                m.Enter(16,false);yield return new EditorWaitForSeconds(.2f);
                checks["finalEndpointBeyondFarClip"]=Vector3.Distance(path.points[path.points.Length-1],camera.transform.position)>camera.farClipPlane;
                checks["finishHasLongDeparture"]=path.Length-path.Anchor(16)>100;
                rotation=camera.transform.rotation;camera.transform.rotation=Quaternion.LookRotation(-path.Forward(m.mover.Distance),path.Up(m.mover.Distance));
                Capture("Artery_Finish_Backward_Continuous.png");camera.transform.rotation=rotation;
                Capture("Artery_Finish_Forward_Continuous.png");
                done=true;
            }
            finally
            {
                Application.logMessageReceived-=Log;checks["completed"]=done;checks["noErrors"]=errors.Count==0;
                File.WriteAllText(ArteryPolishReview.Reports+"/FlowContinuity.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),passed=checks.Values.All(v=>v),checks,captures,details,errors,hardwareVRTested=false,method="Play Mode 180-degree camera rotation, persistent cell displacement, endpoint margins and wall coverage in the actual camera image"},Formatting.Indented));
                camera.transform.rotation=rotation;m.Enter(3,false);
            }
        }
        static bool HasWallCoverage(string path)
        {
            var image=new Texture2D(2,2,TextureFormat.RGB24,false);image.LoadImage(File.ReadAllBytes(path));
            try
            {
                var colors=new Dictionary<int,int>();int count=0;
                void Sample(int x,int y){Color32 c=image.GetPixel(x,y);int key=((c.r>>2)<<12)|((c.g>>2)<<6)|(c.b>>2);colors.TryGetValue(key,out int old);colors[key]=old+1;count++;}
                for(int i=0;i<64;i++){int x=8+i*(image.width-16)/64,y=8+i*(image.height-16)/64;Sample(x,8);Sample(x,image.height-9);Sample(8,y);Sample(image.width-9,y);}
                return colors.Values.Max()/(float)count<.70f;
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}

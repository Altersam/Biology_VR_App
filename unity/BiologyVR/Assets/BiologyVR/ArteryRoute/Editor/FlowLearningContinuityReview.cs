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
    public static class FlowLearningContinuityReview
    {
        static bool running;static readonly List<string> checks=new List<string>();
        static void Report()=>File.WriteAllText(FlowLearningConsistencyBuilder.Reports+"/Continuity.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,running,passed=!running&&checks.Contains("PASS: All arterial stations retain flow, wall and consistent screens")&&!checks.Any(c=>c.StartsWith("BLOCKED")),scope="180-cell budget; explicit station entry/travel fixtures and forward/backward views at timeScale 3; not a mission playthrough",checks},Formatting.Indented));
        [MenuItem("Biology VR/Test Flow Learning Continuity All Stations")]
        public static void Run(){if(!EditorApplication.isPlaying||running)throw new InvalidOperationException("Fresh Play Mode required");Directory.CreateDirectory(FlowLearningConsistencyBuilder.Reports);running=true;checks.Clear();EditorCoroutineUtility.StartCoroutineOwnerless(Guard());}
        static void Check(bool value,string message){checks.Add((value?"PASS: ":"BLOCKED: ")+message);Report();if(!value)throw new InvalidOperationException(message);}
        static IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Test());try{while(stack.Count>0){var current=stack.Peek();bool moved;try{moved=current.MoveNext();}catch(Exception e){checks.Add("BLOCKED: "+e);yield break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current.Current is IEnumerator nested)stack.Push(nested);else yield return current.Current;}}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();running=false;Report();}}
        static IEnumerator Test()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var w=m.world;var flow=w.bloodEntities.stream;var camera=m.mover.viewCamera;var screens=m.GetComponent<JourneyHolographicScreens>();float scale=Time.timeScale;
            w.GetComponent<JourneyBloodFlow>().SetVisibleCellLimit(180);var panel=screens.theoryRoot.localPosition;var monitor=screens.vitalsRoot.localPosition;
            try
            {
                Time.timeScale=3;
                Check(w.freeTools.payload.ironMesh.vertexCount>500,"Heme/iron is a rounded molecular model, not an octahedron");
                Check(flow.palette.All(p=>p.mesh.bounds.center.sqrMagnitude<.0001f),"Every moving species is centred on its rotation pivot");
                for(int scene=3;scene<=16;scene++)
                {
                    m.Enter(scene,scene!=3);int travelMin=int.MaxValue;
                    while(!m.Ready){Look(m,true);travelMin=Mathf.Min(travelMin,flow.MainCellsBetween(m.mover.Distance-16,m.mover.Distance+16).Count());yield return null;}
                    yield return Wait(1);int min=int.MaxValue,forwardMin=int.MaxValue,backwardMin=int.MaxValue;float start=Time.time;
                    while(Time.time-start<32)
                    {
                        bool forward=(int)((Time.time-start)/8)%2==0;Look(m,forward);yield return Wait(.25f);
                        min=Mathf.Min(min,flow.MainCellsBetween(m.mover.Distance-16,m.mover.Distance+16).Count());
                        int count=forward?flow.MainCellsBetween(m.mover.Distance+2,m.mover.Distance+16).Count():flow.MainCellsBetween(m.mover.Distance-16,m.mover.Distance-2).Count();
                        if(forward)forwardMin=Mathf.Min(forwardMin,count);else backwardMin=Mathf.Min(backwardMin,count);
                    }
                    checks.Add($"Station {scene:00}: nearMin={min}, forwardMin={forwardMin}, backwardMin={backwardMin}, travelMin={travelMin}, returns={flow.PoolReturns}");
                    Check(min>=35&&forwardMin>=8&&backwardMin>=8,$"Station {scene:00} has no empty near/forward/backward bloodstream");
                    Check(w.art.wallChunks.Count(r=>r&&r.enabled&&r.gameObject.activeInHierarchy)>0,$"Station {scene:00} retains rendered arterial wall");
                    Check(Vector3.Distance(panel,screens.theoryRoot.localPosition)<.001f&&Vector3.Distance(monitor,screens.vitalsRoot.localPosition)<.001f,$"Station {scene:00} keeps identical screen positions");
                    Check(!string.IsNullOrWhiteSpace(screens.theoryText.text)&&screens.taskRoot.gameObject.activeInHierarchy,$"Station {scene:00} supplies educational theory and a separate task block");
                    if(scene==3||scene==10||scene==12||scene==16){Look(m,true);yield return Wait(.2f);ArteryPolishReview.Capture(camera,$"FlowLearningConsistency/Station_{scene:00}.png");}
                }
                Check(true,"All arterial stations retain flow, wall and consistent screens");
            }
            finally{Time.timeScale=scale;}
        }
        static void Look(JourneyMission m,bool forward){var camera=m.mover.viewCamera.transform;var direction=m.mover.path.Forward(m.mover.Distance)*(forward?1:-1);camera.rotation=Quaternion.LookRotation(direction,m.mover.path.Up(m.mover.Distance));}
        static IEnumerator Wait(float seconds){float start=Time.time;double end=EditorApplication.timeSinceStartup+30;while(Time.time-start<seconds){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>end)throw new InvalidOperationException("Continuity player loop stalled");yield return null;}}
    }
}

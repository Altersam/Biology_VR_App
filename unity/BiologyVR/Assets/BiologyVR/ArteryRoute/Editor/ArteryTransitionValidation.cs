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
    public static class ArteryTransitionValidation
    {
        [MenuItem("Biology VR/Validate Arterial Transitions")]
        public static void Run(){if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");EditorCoroutineUtility.StartCoroutineOwnerless(Check());}
        static IEnumerator Check()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var w=m.world;var checks=new Dictionary<string,bool>();var errors=new List<string>();bool done=false;
            void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
            Application.logMessageReceived+=Log;
            try
            {
                m.Enter(3,false);yield return null;var flow=w.GetComponent<JourneyBloodFlow>();var before=flow.cells[10].position;
                m.Enter(4,true);checks["transitionStartsWithoutSnap"]=m.mover.Moving&&m.mover.TransitionProgress<.1f;
                checks["oldAndNextLocationsPrewarmed"]=w.mover.path.locationIds[0]==w.mover.path.locationIds[1]||w.locationRoots.Count(r=>r&&r.activeSelf)>=2;
                yield return new EditorWaitForSeconds(.35f);checks["flowContinuesDuringTransition"]=Vector3.Distance(before,flow.cells[10].position)>.01f;
                checks["hudFadesDuringTransition"]=m.GetComponent<JourneyHud>().panelGroup&&m.GetComponent<JourneyHud>().panelGroup.alpha<.99f;
                yield return new EditorWaitForSeconds(6.2f);checks["transitionArrivesSmoothly"]=!m.mover.Moving&&m.mover.TransitionProgress>.99f;
                checks["onlyArrivedLocationRemains"]=w.locationRoots.Count(r=>r&&r.activeSelf)==1;
                m.Enter(11,false);yield return null;m.Enter(12,true);checks["sameAnchorStillGetsCorridor"]=m.mover.Moving;yield return new EditorWaitForSeconds(2.2f);checks["sameAnchorCorridorCompletes"]=!m.mover.Moving;
                done=true;
            }
            finally
            {Application.logMessageReceived-=Log;checks["completed"]=done;checks["noErrors"]=errors.Count==0;File.WriteAllText(ArteryPolishReview.Reports+"/TransitionV3.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),passed=checks.Values.All(v=>v),checks,errors,hardwareVRTested=false,method="Play Mode transition timing, root prewarm, HUD fade, continuous flow and same-anchor corridor"},Formatting.Indented));m.Enter(3,false);}
        }
    }
}

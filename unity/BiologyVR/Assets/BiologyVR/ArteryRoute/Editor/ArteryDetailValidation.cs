using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryDetailValidation
    {
        public static string LastResult {get;private set;}="Not run";
        [MenuItem("Biology VR/Validate Artery Detail Pass")]
        public static void Run(){if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");LastResult="Running";EditorCoroutineUtility.StartCoroutineOwnerless(Check());}
        static IEnumerator Check()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();if(!m){LastResult="FAIL: mission missing";yield break;}
            var w=m.world;var results=new Dictionary<string,bool>();
            var errors=new List<string>();bool reported=false;
            void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
            Application.logMessageReceived+=Log;
            try
            {
            var cache=(Dictionary<string,JourneyTarget>)typeof(JourneyWorld).GetField("map",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(w);
            cache.Clear();w.enabled=false;w.enabled=true;
            results["serializedTargetsSurviveLostRuntimeCache"]=w.Find("platelet")&&w.Find("rbc")&&w.Find("plaque");
            m.Enter(3,false);m.mover.TravelTo(3,true);yield return null;
            var flow=UnityEngine.Object.FindFirstObjectByType<JourneyBloodFlow>();var before=flow.cells[10].position;
            yield return new EditorWaitForSeconds(.35f);results["continuousBloodFlow"]=Vector3.Distance(before,flow.cells[10].position)>.01f;
            int scene=m.SceneNumber;m.Continue();results["unfinishedStageBlocksContinue"]=m.SceneNumber==scene;
            m.Enter(12,false);m.mover.TravelTo(12,true);
            results["sharedPlateletVisible"]=w.Find("platelet").gameObject.activeInHierarchy;
            for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"platelet");m.Accept(StudyAction.Activate,"platelet");for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"platelet");m.Accept(StudyAction.Activate,"thrombin");m.Accept(StudyAction.Activate,"fibrin");
            results["sixPlateletsAndFibrin"]=m.Complete&&w.AttachedPlatelets==6&&w.fibrin.activeInHierarchy;
            m.Enter(13,false);m.mover.TravelTo(13,true);m.Accept(StudyAction.Scan,"clot");m.SetBalance(.18f);m.SetBalance(.95f);m.SetBalance(.62f);m.Accept(StudyAction.Measure,"open-flow");results["clotBalance"]=m.Complete;
            m.Enter(14,false);m.mover.TravelTo(14,true);results["phagocyteVisibleAtWound"]=w.phagocyte.gameObject.activeInHierarchy;
            for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"debris");m.Accept(StudyAction.Activate,"repair");results["earlyHealingScanRejected"]=!m.Accept(StudyAction.Scan,"healed-wall");
            yield return new EditorWaitForSeconds(3.8f);m.Accept(StudyAction.Scan,"healed-wall");results["healingCompletesAfterAnimation"]=m.Complete;
            results["oneWoundAnchor"]=Mathf.Abs(m.mover.path.Anchor(11)-m.mover.path.Anchor(14))<.01f;
            m.Enter(15,false);m.mover.TravelTo(15,true);
            yield return new EditorWaitForSeconds(6.5f);
            for(int i=0;i<4;i++)m.Accept(StudyAction.Mark,"incoming-virus");m.Accept(StudyAction.Scan,"epitope");m.Accept(StudyAction.Bind,"antibody-A");m.Accept(StudyAction.Direct,"neutralized");m.Accept(StudyAction.Scan,"infected-cell");m.Accept(StudyAction.Direct,"t-cell");
            results["earlyFinalCleanupRejected"]=!m.Accept(StudyAction.Direct,"remaining");
            yield return new EditorWaitForSeconds(3.1f);
            for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"remaining");
            results["victoryWaitsForFinalPhagocytosis"]=!m.Victory;
            yield return new EditorWaitForSeconds(1.4f);
            results["victoryAfterSourceRemoval"]=m.Victory&&w.Victory&&!w.infected.gameObject.activeInHierarchy&&m.ViralLoad==0;
              m.Continue();results["routeContinuesToRecovery"]=m.SceneNumber==16;
               m.Accept(StudyAction.Measure,"vitals");m.Accept(StudyAction.Scan,"homeostasis");
               results["homeostasisMeasurementStage"]=m.Complete&&m.PressureSystolic==120&&Mathf.Abs(m.Temperature-36.8f)<.01f;
               m.Continue();results["routeReturnsToLaboratory"]=m.SceneNumber==17;
              results["reportWaitsForReturnAnimation"]=m.world.Returning;
              yield return new EditorWaitForSeconds(5.8f);
              m.Continue();results["freeResearchReportUnlocked"]=m.SceneNumber==18&&m.FreeResearchUnlocked;
              results["playerCameraActuallyInLaboratory"]=m.world.labRoom&&Vector3.Distance(m.mover.viewCamera.transform.position,m.world.labRoom.transform.position+new Vector3(0,1.6f,-2))<.1f;
            results["noRuntimeErrors"]=errors.Count==0;
            bool passed=true;foreach(var value in results.Values)passed&=value;
            LastResult=JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,passed,checks=results,errors,hardwareVRTested=false,method="Programmatic Play Mode actions with animation waits"},Formatting.Indented);
            File.WriteAllText(JourneyGeometry.Root+"/Reports/DetailValidation.json",LastResult);
            reported=true;
            }
            finally
            {
                Application.logMessageReceived-=Log;
                if(!reported){results["validationCompleted"]=false;File.WriteAllText(JourneyGeometry.Root+"/Reports/DetailValidation.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),passed=false,checks=results,errors,aborted=true},Formatting.Indented));}
                m.Enter(3,false);m.mover.TravelTo(3,true);
            }
        }
    }
}

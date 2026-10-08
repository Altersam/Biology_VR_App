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
    public static class ArteryWoundStateValidation
    {
        [MenuItem("Biology VR/Validate Directed Wound Flow")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
            EditorCoroutineUtility.StartCoroutineOwnerless(Check());
        }
        static IEnumerator Check()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var w=m.world;
            var checks=new Dictionary<string,bool>();var errors=new List<string>();bool completed=false;
            void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
            Application.logMessageReceived+=Log;
            try
            {
                var state=w.woundState;
                checks["nineReusableMeshCells"]=state&&state.Cells.Length==9&&state.Cells.All(c=>c&&c.GetComponent<MeshFilter>()&&!c.GetComponent<Rigidbody>()&&!c.GetComponent<Collider>());
                checks["oneSharedBloodCellMesh"]=state.Cells.Select(c=>c.GetComponent<MeshFilter>().sharedMesh).Distinct().Count()==1;
                m.Enter(11,false);yield return new EditorWaitForSeconds(.15f);
                checks["directedLeakVisibleOnEntry"]=state.ActiveCellCount>0&&state.LeakStrength>.99f;
                var cell=state.Cells[0];var before=cell.position;
                yield return new EditorWaitForSeconds(.15f);
                checks["actualCellMovesOutward"]=Vector3.Dot(cell.position-before,state.Outward)>.01f;
                checks["pathCrossesDefectOutward"]=Enumerable.Range(0,9).All(i=>Vector3.Dot(state.SamplePath(i,0)-w.woundSite.position,state.Outward)<-.8f&&Vector3.Dot(state.SamplePath(i,1)-w.woundSite.position,state.Outward)>1f);
                checks["leakMeasurementCannotReplaceWoundScan"]=!m.Accept(StudyAction.Measure,"leak")&&m.StepIndex==0;
                checks["woundScanAccepted"]=m.Accept(StudyAction.Scan,"wound");
                checks["leakMeasureAccepted"]=m.Accept(StudyAction.Measure,"leak");
                checks["pressureMeasureAccepted"]=m.Accept(StudyAction.Measure,"pressure-low")&&m.Complete;
                ArteryWallInteractionValidation.Capture(w,"Scene11_Directed_Leak.png");

                m.Enter(12,false);yield return new EditorWaitForSeconds(.15f);
                checks["hemostasisEntryResetsGrowth"]=w.AttachedPlatelets==0&&w.FibrinGrowth==0&&w.RepairGrowth==0&&state.LeakStrength>.99f;
                for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"platelet");
                yield return new EditorWaitForSeconds(.9f);
                checks["adhesionReducesLeak"]=w.AttachedPlatelets==3&&state.LeakStrength>.15f&&state.LeakStrength<.8f;
                checks["restingPlateletsBeforeActivation"]=w.platelets.Take(3).All(p=>p.GetComponent<MeshFilter>().sharedMesh==w.art.restingPlateletMesh);
                ArteryWallInteractionValidation.Capture(w,"Scene12_Adhesion.png");
                m.Accept(StudyAction.Activate,"platelet");
                checks["attachedPlateletsActivate"]=w.platelets.Take(3).All(p=>p.GetComponent<MeshFilter>().sharedMesh==w.art.activatedPlateletMesh);
                for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"platelet");
                m.Accept(StudyAction.Activate,"thrombin");m.Accept(StudyAction.Activate,"fibrin");
                checks["fibrinStartsSmall"]=w.fibrin.activeInHierarchy&&w.FibrinGrowth>0&&w.FibrinGrowth<.1f;
                yield return new EditorWaitForSeconds(3.5f);
                checks["fibrinGrowsOverPlug"]=w.AttachedPlatelets==6&&w.FibrinGrowth>.99f&&w.fibrin.transform.localScale.x>.99f;
                checks["completedHemostasisStopsLeak"]=state.LeakStrength<.001f&&state.ActiveCellCount==0;
                ArteryWallInteractionValidation.Capture(w,"Scene12_Sealed_Defect.png");

                m.Enter(13,false);m.Accept(StudyAction.Scan,"clot");m.SetBalance(.18f);
                yield return new EditorWaitForSeconds(.7f);
                checks["insufficientPlugRestartsLeak"]=state.LeakStrength>.7f&&state.ActiveCellCount>0;
                m.SetBalance(.95f);checks["excessClotShowsOcclusion"]=w.occlusion.activeInHierarchy;
                m.SetBalance(.62f);m.Accept(StudyAction.Measure,"open-flow");
                yield return new EditorWaitForSeconds(2.1f);
                checks["optimalPlugStopsLeakAndOpensFlow"]=m.Complete&&state.ActiveCellCount==0&&!w.occlusion.activeInHierarchy;

                m.Enter(14,false);for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"debris");m.Accept(StudyAction.Activate,"repair");
                checks["earlyHealingScanRejected"]=!m.Accept(StudyAction.Scan,"healed-wall");
                yield return new EditorWaitForSeconds(1.6f);
                checks["repairIsProgressive"]=w.RepairGrowth>.25f&&w.RepairGrowth<.9f;
                ArteryWallInteractionValidation.Capture(w,"Scene14_Healing_In_Progress.png");
                yield return new EditorWaitForSeconds(2.1f);
                checks["healingFinishesAtSameDefect"]=m.Accept(StudyAction.Scan,"healed-wall")&&m.Complete&&w.RepairGrowth>.99f&&state.ActiveCellCount==0;
                var backing=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.name=="WALL_A_Subendothelial_Matrix");
                checks["exposedMatrixHiddenAfterHealing"]=!backing.enabled;
                checks["clotAndFibrinWithdrawAfterHealing"]=!w.clot.gameObject.activeInHierarchy&&!w.fibrin.activeInHierarchy&&w.platelets.All(p=>!p.activeInHierarchy);
                ArteryWallInteractionValidation.Capture(w,"Scene14_Healed_Wall.png");

                m.Enter(11,false);yield return new EditorWaitForSeconds(.2f);
                checks["woundReentryRestoresDiagnosis"]=w.RepairGrowth==0&&w.FibrinGrowth==0&&w.AttachedPlatelets==0&&state.LeakStrength>.99f&&state.ActiveCellCount>0&&backing.enabled;
                m.Enter(3,false);yield return new EditorWaitForSeconds(.2f);
                checks["leakPoolHiddenOutsideWound"]=state.ActiveCellCount==0&&state.Cells.All(c=>!c.GetComponent<Renderer>().enabled)&&!backing.enabled;
                completed=true;
            }
            finally
            {
                Application.logMessageReceived-=Log;checks["validationCompleted"]=completed;checks["noRuntimeErrors"]=errors.Count==0;
                Directory.CreateDirectory(ArteryPolishReview.Reports);
                File.WriteAllText(ArteryPolishReview.Reports+"/WoundStates.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),passed=checks.Values.All(v=>v),checks,errors,hardwareVRTested=false,method="Play Mode mesh-pool motion and timed clot/healing transitions using actual mission APIs"},Formatting.Indented));
                m.Enter(3,false);
            }
        }
    }
}

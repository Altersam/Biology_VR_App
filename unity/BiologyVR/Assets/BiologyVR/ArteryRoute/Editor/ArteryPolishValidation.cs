using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryPolishValidation
    {
        static readonly HashSet<string> errors=new HashSet<string>();
        static void OnLog(string condition,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(condition);}
        [MenuItem("Biology VR/Validate Polished Scene03")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
            EditorCoroutineUtility.StartCoroutineOwnerless(Check());
        }
        static IEnumerator Check()
        {
            errors.Clear();Application.logMessageReceived+=OnLog;
            var checks=new Dictionary<string,bool>();
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var w=m.world;var h=m.GetComponent<JourneyHud>();var input=m.GetComponent<JourneyInput>();
            bool reported=false;
            try
            {
                m.Enter(3,false);yield return new EditorWaitForSeconds(.25f);
                checks["serializedHudComplete"]=h.panelRoot&&h.targetMarker&&h.inputGuard;
                checks["allGraphicsHaveCanvasRenderer"]=UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include,FindObjectsSortMode.None).All(g=>g.GetComponent<CanvasRenderer>());
                checks["independentScannerToolActions"]=input.scanAction&&input.toolAction&&input.scanAction.action!=input.toolAction.action;
                checks["controllerToolsHaveAimAnchors"]=input.scanner&&input.bioTool&&input.scanner.name=="Scanner aim"&&input.bioTool.name=="BioTool aim";
                checks["initialHeroOnly"]=w.Find("rbc").gameObject.activeInHierarchy&&!w.Find("hemoglobin").gameObject.activeInHierarchy&&!w.Find("leukocyte").gameObject.activeInHierarchy;
                var r=w.Find("rbc");float scale=r.transform.localScale.x;
                r.KeyboardGrab();checks["grabAdvancesOnlyGrab"]=m.StepIndex==1&&r.grabbed;
                h.ScanFocused();checks["scannerAdvancesOnlyScan"]=m.StepIndex==2;
                r.Enlarge();checks["enlargeRevealsMolecules"]=m.StepIndex==3&&w.Find("hemoglobin").gameObject.activeInHierarchy;
                r.KeyboardGrab();yield return new EditorWaitForSeconds(.9f);
                checks["releasePreservesEnlargement"]=!r.grabbed&&r.transform.localScale.x>=scale*1.9f;
                h.ScanFocused();h.ScanFocused();w.Primary();w.Primary();
                checks["oxygenDetachBindSequence"]=m.StepIndex==7;
                h.ScanFocused();checks["wbcVisibleOnCorrectStep"]=m.StepIndex==8&&w.Find("leukocyte").gameObject.activeInHierarchy;
                h.ScanFocused();checks["restingPlateletVisible"]=m.StepIndex==9&&w.Find("platelet").gameObject.activeInHierarchy;
                h.ScanFocused();checks["activatedPlateletVisibleBeforeComparison"]=m.StepIndex==10&&w.art&&w.art.plateletComparison&&w.art.plateletComparison.activeInHierarchy;
                w.Primary();h.ScanFocused();checks["allScene03GoalsComplete"]=m.Complete&&m.StepIndex==12;
                m.Enter(4,false);yield return null;
                var endo=w.Find("endothelium");float s=m.mover.path.Anchor(4)+3;
                var band=endo.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name=="Scene04_Band_endothelium");
                checks["endotheliumAnchoredToWall"]=band&&band.enabled&&Vector3.Distance(band.bounds.center,m.mover.path.Centre(s))>2.8f;
                m.Enter(6,false);yield return null;s=m.mover.path.Anchor(6)+4;
                checks["plaqueAnchoredToWall"]=Vector3.Distance(w.Find("plaque").transform.position,m.mover.path.Centre(s))>2.8f;
                m.Enter(5,false);yield return null;w.Primary();
                var demo=w.flowModel.GetComponent<JourneyFlowModelView>();
                checks["flowModelHasSixSharedCells"]=demo&&demo.cells.Length==6&&w.flowModel.gameObject.activeInHierarchy;
                w.Primary();m.SetRadius(.65f);m.SetRadius(1.35f);m.SetPressure(155);w.Primary();
                checks["flowExperimentCompletesAndResets"]=m.Complete&&Mathf.Abs(m.ModelRadius-1)<.001f&&Mathf.Abs(m.ModelPressure-120)<.001f;
                yield return new EditorWaitForSeconds(.6f);
                checks["noErrorsDuringValidation"]=errors.Count==0;
                Directory.CreateDirectory(ArteryPolishReview.Reports);
                File.WriteAllText(ArteryPolishReview.Reports+"/Mechanics.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,passed=checks.Values.All(v=>v),checks,errors=errors.ToArray(),method="Play Mode using actual desktop grab/release and focused UI APIs; no headset simulation",hardwareVRTested=false},Formatting.Indented));reported=true;
            }
            finally
            {
                if(!reported){checks["validationCompleted"]=false;File.WriteAllText(ArteryPolishReview.Reports+"/Mechanics.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),passed=false,checks,errors=errors.ToArray(),aborted=true},Formatting.Indented));}
                Application.logMessageReceived-=OnLog;m.Enter(3,false);h.CloseBriefing();
            }
        }
    }
}

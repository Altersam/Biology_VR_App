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
    public static class ArteryEpisodeViewsValidation
    {
        [MenuItem("Biology VR/Validate Arterial Episode Views")]
        public static void Run(){if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");EditorCoroutineUtility.StartCoroutineOwnerless(Check());}
        static IEnumerator Check()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var w=m.world;
            var checks=new Dictionary<string,bool>();var errors=new List<string>();var captures=new List<string>();bool done=false;
            string folder="EpisodeViews_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(ArteryPolishReview.Reports+"/"+folder);
            void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
            void Capture(string name){ArteryWallInteractionValidation.Capture(w,folder+"/"+name);captures.Add(ArteryPolishReview.Reports+"/"+folder+"/"+name);}
            Application.logMessageReceived+=Log;
            try
            {
                m.Enter(7,false);yield return new EditorWaitForSeconds(.35f);
                var before=w.bubble.position;yield return new EditorWaitForSeconds(.35f);
                checks["embolusMovesInFlow"]=Vector3.Distance(before,w.bubble.position)>.03f;
                checks["bubbleUsesTransparentPearlShell"]=w.bubble.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled&&r.sharedMaterial&&r.sharedMaterial.shader.name=="BiologyVR/Mobile Gas Bubble");
                var bubblePoint=w.mover.viewCamera.WorldToViewportPoint(w.bubble.position);checks["embolusInsidePlayerView"]=bubblePoint.z>0&&bubblePoint.x>.02f&&bubblePoint.x<.98f&&bubblePoint.y>.02f&&bubblePoint.y<.98f;
                checks["stabilisationFieldHasTrigger"]=w.trap&&w.trap.GetComponent<Collider>().isTrigger;
                Capture("07_Moving_Embolus.png");
                checks["captureSequenceAccepted"]=m.Accept(StudyAction.Scan,"embolus")&&m.Accept(StudyAction.Activate,"attract-mode")&&m.Accept(StudyAction.Capture,"embolus");
                yield return new EditorWaitForSeconds(.65f);checks["captureWithoutHeldInputDoesNotAutoDeposit"]=!m.Complete;Capture("07_Capture_API_Regression.png");
                if(m.Current?.action==StudyAction.Capture)m.Accept(StudyAction.Capture,"embolus");
                checks["remoteDepositApiCannotCompleteTrapPuzzle"]=!m.Accept(StudyAction.Deposit,"embolus")&&!m.Complete&&w.bubble.gameObject.activeInHierarchy;
                // Real held-input transport is verified by ArteryXRInputPlaytest.
                // An API/camera regression must not label this as physical deposit.
                m.Enter(7,false);yield return new EditorWaitForSeconds(.2f);checks["embolusReentryResetsCapture"]=!w.EmbolusCaptured&&w.bubble.gameObject.activeInHierarchy;

                m.Enter(8,false);yield return new EditorWaitForSeconds(.35f);
                var inspection=w.virusInspection;checks["persistentVirusControllerPresent"]=inspection;
                var viewport=w.mover.viewCamera.WorldToViewportPoint(inspection.FocusPoint);
                checks["virusInsidePlayerView"]=viewport.z>0&&viewport.x>.05f&&viewport.x<.95f&&viewport.y>.05f&&viewport.y<.95f;
                Capture("08_Intact_Virus.png");w.Primary();w.Primary();yield return new EditorWaitForSeconds(.75f);
                checks["virusEnlargementPersists"]=m.StepIndex==2&&inspection.Enlarged;
                Capture("08_Genome_Study.png");w.Primary();Capture("08_Capsid_Study.png");w.Primary();Capture("08_Epitope_Study.png");w.Primary();
                checks["allVirusPartsStudied"]=m.Complete;yield return new EditorWaitForSeconds(.75f);Capture("08_Released_Virus.png");
                m.Enter(9,false);w.Primary();yield return new EditorWaitForSeconds(4.0f);
                checks["waveTargetsUseInstrumentLayer"]=w.IncomingViruses.Count>=2&&w.IncomingViruses.All(v=>v.layer==LayerMask.NameToLayer("JourneyTarget"));
                if(w.IncomingViruses.Count>0)
                {var chosen=w.IncomingViruses[w.IncomingViruses.Count-1];w.Primary(chosen.GetComponent<JourneyTarget>());checks["pointedVirusIsMarked"]=!w.IncomingViruses.Contains(chosen)&&m.Progress==1;}
                else checks["pointedVirusIsMarked"]=false;
                checks["instrumentFeedbackConfirmsActions"]=m.GetComponent<JourneyToolFeedback>().ConfirmedActions>0;
                Capture("09_Targeted_Viral_Wave.png");

                m.Enter(12,false);yield return new EditorWaitForSeconds(.25f);Capture("12_Resting_Platelets.png");
                for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"platelet");yield return new EditorWaitForSeconds(.9f);Capture("12_Adhesion.png");
                m.Accept(StudyAction.Activate,"platelet");yield return new EditorWaitForSeconds(.15f);Capture("12_Activation.png");
                for(int i=0;i<3;i++)m.Accept(StudyAction.Direct,"platelet");yield return new EditorWaitForSeconds(.9f);Capture("12_Aggregation.png");
                m.Accept(StudyAction.Activate,"thrombin");m.Accept(StudyAction.Activate,"fibrin");yield return new EditorWaitForSeconds(2.8f);Capture("12_Fibrin_Stabilisation.png");
                var teaching=w.fibrin.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="Teaching fibrin stabilisation network");
                checks["lightweightFibrinGeometry"]=teaching.sharedMesh.triangles.Length/3<15000;
                checks["hemostasisCompleted"]=m.Complete&&w.AttachedPlatelets==6&&w.FibrinGrowth>.99f;
                m.Enter(13,false);m.Accept(StudyAction.Scan,"clot");m.SetBalance(.18f);Capture("13_Insufficient.png");m.SetBalance(.95f);Capture("13_Excessive.png");m.SetBalance(.62f);Capture("13_Optimal.png");
                m.Enter(10,false);w.Primary();w.Primary();m.SetRadius(.82f);yield return new EditorWaitForSeconds(.2f);
                var property=new MaterialPropertyBlock();w.toneWall[0].GetPropertyBlock(property);checks["presentationPreservesVascularTone"]=Mathf.Abs(property.GetFloat("_Tone")-.82f)<.001f;
                done=true;
            }
            finally
            {
                Application.logMessageReceived-=Log;checks["completed"]=done;checks["noRuntimeErrors"]=errors.Count==0;
                File.WriteAllText(ArteryPolishReview.Reports+"/EpisodeViews.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),passed=checks.Values.All(v=>v),checks,captures,errors,hardwareVRTested=false,method="Timed Play Mode episode states, selected-virus identity and real model/collider checks"},Formatting.Indented));m.Enter(3,false);
            }
        }
    }
}

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
    public static class ArteryWallInteractionValidation
    {
        [MenuItem("Biology VR/Validate Wall Interactions")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
            EditorCoroutineUtility.StartCoroutineOwnerless(Check());
        }

        static IEnumerator Check()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();
            var w=m.world;var checks=new Dictionary<string,bool>();var errors=new List<string>();
            var details=new List<object>();bool completed=false;
            void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
            Application.logMessageReceived+=Log;
            try
            {
                var inspection=UnityEngine.Object.FindFirstObjectByType<JourneyLayerInspection>(FindObjectsInactive.Include);
                checks["layerControllerSerialized"]=inspection&&inspection.BandTargets.Length==4;
                checks["plaqueControllerSerialized"]=w.plaqueState&&w.plaqueState.Zones.Length==3;
                m.Enter(4,false);yield return new EditorWaitForSeconds(.3f);
                var patch=w.art.layerCutaway;
                checks["layerPatchFoundIncludingInactive"]=patch&&patch.name=="Scene04_WallLayerInspection_Patch"&&patch.activeInHierarchy;
                var bands=inspection.BandTargets.Select(t=>t.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name.StartsWith("Scene04_Band_"))).ToArray();
                details.Add(new{phase="initial",scene=m.SceneNumber,station=w.mover.path.Anchor(4)+3,centre=w.mover.path.Centre(w.mover.path.Anchor(4)+3).ToString(),bands=bands.Select(r=>new{name=r.name,rendererEnabled=r.enabled,activeSelf=r.gameObject.activeSelf,activeHierarchy=r.gameObject.activeInHierarchy,targetActive=r.transform.parent.gameObject.activeSelf,targetParent=r.transform.parent.parent.name,parentActive=r.transform.parent.parent.gameObject.activeInHierarchy,colliderEnabled=r.GetComponent<Collider>().enabled}).ToArray()});
                checks["onlyEndotheliumBeforeActivation"]=bands[0].gameObject.activeInHierarchy&&bands.Skip(1).All(r=>!r.gameObject.activeInHierarchy);
                checks["endotheliumRaycast"]=Hit(bands[0].bounds.center,w,4,inspection.BandTargets[0]);
                m.Accept(StudyAction.Scan,"endothelium");
                checks["scanDoesNotRevealLayers"]=bands.Skip(1).All(r=>!r.gameObject.activeInHierarchy);
                m.Accept(StudyAction.Activate,"layers");checks["earlyLayerScanRejected"]=!m.Accept(StudyAction.Scan,"intima");yield return new EditorWaitForSeconds(1.05f);
                checks["activationRevealsFourBands"]=bands.All(r=>r.enabled&&r.gameObject.activeInHierarchy);
                details.Add(new{cameraPosition=w.mover.viewCamera.transform.position.ToString(),fieldOfView=w.mover.viewCamera.fieldOfView,aspect=w.mover.viewCamera.aspect,viewportCentres=bands.Select(r=>w.mover.viewCamera.WorldToViewportPoint(r.bounds.center).ToString()).ToArray()});
                details.Add(new{phase="revealed",bands=bands.Select(r=>new{name=r.name,rendererEnabled=r.enabled,activeSelf=r.gameObject.activeSelf,activeHierarchy=r.gameObject.activeInHierarchy,targetActive=r.transform.parent.gameObject.activeSelf,targetParent=r.transform.parent.parent.name,parentActive=r.transform.parent.parent.gameObject.activeInHierarchy,colliderEnabled=r.GetComponent<Collider>().enabled}).ToArray()});
                Capture(w,"Scene04_Layers_Revealed.png");Capture(w,"Scene04_Verified_Layer_Patch.png");
                string[] ids={"intima","media","adventitia"};
                for(int i=0;i<ids.Length;i++)
                {
                    var target=inspection.BandTargets[i+1];
                    checks[ids[i]+"Raycast"]=Hit(bands[i+1].bounds.center,w,4,target);
                    checks[ids[i]+"ScanAccepted"]=m.Accept(StudyAction.Scan,ids[i]);
                    yield return new EditorWaitForSeconds(1.05f);
                }
                checks["layerSequenceCompletes"]=m.Complete&&m.StepIndex==5;
                Capture(w,"Scene04_All_Shells_Opened.png");
                details.Add(new{layerPatch=patch.name,patchActive=patch.activeInHierarchy,bands=bands.Select(r=>new{name=r.name,centre=r.bounds.center.ToString(),triangles=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3}).ToArray()});

                m.Enter(6,false);yield return new EditorWaitForSeconds(.3f);
                var state=w.plaqueState;
                checks["plaqueContactsInitiallyDisabled"]=state.Zones.All(z=>!z.Contact.enabled);
                w.Primary();w.Primary();checks["plaqueFormationBlocksEarlyToolActivation"]=!m.Accept(StudyAction.Activate,"pulse-mode");
                yield return new EditorWaitForSeconds(2.65f);w.Primary();yield return new EditorWaitForSeconds(.7f);
                checks["detailedPlaqueCompositionReady"]=w.plaqueDetail&&w.plaqueDetail.Ready&&w.plaqueDetail.LipidLobules>=25;
                checks["threeZonesRevealed"]=state.IntroReveal>.99f&&state.Zones.All(z=>z.Contact.enabled&&z.VisualRenderer.enabled);
                Capture(w,"Scene06_Three_Lipid_Zones.png");
                var filter=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(f=>f.name=="Plaque_Cap");
                var source=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(c=>c.name=="Plaque_Cap").sharedMesh;
                var before=filter.sharedMesh.vertices;
                for(int i=0;i<3;i++)checks["lipidZone"+i+"Raycast"]=Hit(state.Zones[i].transform.position,w,6,state.Zones[i].Target);
                w.Primary(state.Zones[1].Target);
                checks["selectedZoneCountsOnce"]=state.ProcessedCount==1&&m.Progress==1&&state.IsZoneProcessed(1)&&!state.Zones[1].Contact.enabled;
                w.Primary(state.Zones[1].Target);
                checks["processedZoneRejected"]=state.ProcessedCount==1&&m.Progress==1;
                // Change notifications and a wrong-target action cannot count another pulse.
                m.FeedbackMessage("Validation: selection remains explicit");m.Accept(StudyAction.Pulse,"plaque");
                checks["notificationsDoNotProcessZones"]=state.ProcessedCount==1;
                w.Primary(state.Zones[0].Target);w.Primary(state.Zones[2].Target);
                yield return new EditorWaitForSeconds(.8f);
                checks["threeDistinctZonesComplete"]=state.ProcessedCount==3&&m.Complete&&state.PathologyFraction==0;
                checks["capDeformsWithoutMutatingAsset"]=filter.sharedMesh!=source&&before.Where((v,i)=>(v-filter.sharedMesh.vertices[i]).sqrMagnitude>.000001f).Any()&&source.vertices.SequenceEqual(before);
                checks["processedContactsDisabled"]=state.Zones.All(z=>!z.Contact.enabled);
                Capture(w,"Scene06_After_Three_Pulses.png");

                m.Enter(6,false);yield return new EditorWaitForSeconds(.2f);
                checks["plaqueReentryResets"]=state.ProcessedCount==0&&state.CapHits==0&&filter.sharedMesh.vertices.SequenceEqual(before);
                w.Primary();w.Primary();yield return new EditorWaitForSeconds(2.65f);w.Primary();
                m.Accept(StudyAction.Pulse,"cap");
                checks["capHitWarnsWithoutProgress"]=state.CapHits==1&&m.WrongCapHits==1&&m.Progress==0;
                m.Accept(StudyAction.Pulse,"cap");m.Accept(StudyAction.Pulse,"cap");
                checks["threeCapHitsResetCheckpoint"]=m.SceneNumber==6&&m.StepIndex==0&&m.WrongCapHits==0&&state.ProcessedCount==0&&state.CapHits==0;
                w.Primary();w.Primary();yield return new EditorWaitForSeconds(2.65f);w.Primary();
                for(int i=0;i<3;i++)checks["logicalPulse"+i+"Accepted"]=m.Accept(StudyAction.Pulse,"lipid");
                checks["focusedLogicalFallbackUsesDistinctZones"]=m.Complete&&state.ProcessedCount==3;
                completed=true;
            }
            finally
            {
                Application.logMessageReceived-=Log;
                checks["validationCompleted"]=completed;checks["noRuntimeErrors"]=errors.Count==0;
                Directory.CreateDirectory(ArteryPolishReview.Reports);
                File.WriteAllText(ArteryPolishReview.Reports+"/WallInteractions.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),passed=checks.Values.All(v=>v),checks,details,errors,hardwareVRTested=false,method="Unity Play Mode real collider rays and mission/world APIs; no headset"},Formatting.Indented));
                m.Enter(3,false);
            }
        }

        static bool Hit(Vector3 point,JourneyWorld w,int scene,JourneyTarget expected)
        {
            Physics.SyncTransforms();
            float station=w.mover.path.Anchor(scene)+(scene==4?3:4);
            Vector3 start=w.mover.path.Centre(station),direction=point-start;
            return Physics.Raycast(start,direction.normalized,out var hit,direction.magnitude+.2f,~0,QueryTriggerInteraction.Collide)
                &&hit.collider.GetComponentInParent<JourneyTarget>()==expected;
        }

        internal static void Capture(JourneyWorld w,string name)
        {
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var enabled=canvases.Select(c=>c.enabled).ToArray();
            try{foreach(var c in canvases)c.enabled=false;ArteryPolishReview.Capture(w.mover.viewCamera,name);}
            finally{for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=enabled[i];}
        }
    }
}

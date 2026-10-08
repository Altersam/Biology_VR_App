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
    public static class ArteryDiverseFlowValidation
    {
        [MenuItem("Biology VR/Validate Diverse Branch Flow")]
        public static void Run(){if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");EditorCoroutineUtility.StartCoroutineOwnerless(Check());}
        static IEnumerator Check()
        {
            var m=UnityEngine.Object.FindFirstObjectByType<JourneyMission>();var w=m.world;var flow=w.GetComponent<JourneyBloodFlow>();var checks=new Dictionary<string,bool>();
            var errors=new List<string>();bool complete=false;string folder="DiverseFlow_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(ArteryPolishReview.Reports+"/"+folder);
            void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
            Application.logMessageReceived+=Log;
            try
            {
                m.Enter(3,false);yield return new EditorWaitForSeconds(.3f);
                checks["densePersistentPool"]=flow.cells.Length==360&&flow.mobileCellLimit>=180;
                var types=flow.species.Distinct().ToArray();checks["sevenBiologicalVisualTypes"]=types.Length>=7;
                checks["oxygenAndCarbonMarkers"]=types.Contains("erythrocyte-O2")&&types.Contains("erythrocyte-CO2");
                checks["plasmaProteinsAndIons"]=types.Contains("plasma-ion")&&types.Any(t=>t=="plasma-protein"||t=="albumin");
                checks["blenderProteinVariety"]=types.Contains("albumin")&&types.Contains("globulin")&&types.Contains("fibrinogen")&&types.Contains("lipoprotein");
                checks["randomizedDistribution"]=flow.randomizedFlow&&flow.lanesX.Select(x=>Mathf.RoundToInt(x*1000)).Distinct().Count()>flow.cells.Length/2;
                checks["threePhysicalBranches"]=flow.branchPaths.Length==3&&flow.branchEntries.Length==3;
                checks["branchesExtendBeyondView"]=flow.branchPaths.All(p=>p.Length>80);
                checks["massCellsNoRigidbodies"]=flow.cells.All(c=>!c.GetComponent<Rigidbody>()&&!c.GetComponent<Collider>());
                checks["softCelMaterialUsed"]=flow.cells.Any(c=>c.GetComponent<Renderer>().sharedMaterials.Any(mat=>mat&&mat.shader.name=="BiologyVR/Soft Inked Cell"));
                ArteryWallInteractionValidation.Capture(w,folder+"/03_Dense_Blood_And_Plasma.png");
                m.Enter(7,false);int maximum=0;
                for(int i=0;i<28;i++){yield return new EditorWaitForSeconds(.25f);maximum=Mathf.Max(maximum,flow.BranchRoutedCells);}
                checks["cellsActuallyDivertIntoBranch"]=maximum>0;
                var camera=w.mover.viewCamera;var position=camera.transform.position;var rotation=camera.transform.rotation;
                try
                {
                    float entry=flow.branchEntries[0];camera.transform.position=flow.path.Offset(entry-3,-.5f,.2f);camera.transform.LookAt(flow.branchPaths[0].Centre(5));
                    ArteryWallInteractionValidation.Capture(w,folder+"/07_Branch_Flow.png");
                }
                finally{camera.transform.SetPositionAndRotation(position,rotation);}
                complete=true;
            }
            finally
            {
                Application.logMessageReceived-=Log;checks["completed"]=complete;checks["noErrors"]=errors.Count==0;
                File.WriteAllText(ArteryPolishReview.Reports+"/DiverseFlow.json",JsonConvert.SerializeObject(new{generatedAtUtc=DateTime.UtcNow.ToString("O"),passed=checks.Values.All(v=>v),checks,counts=flow.species.GroupBy(s=>s).ToDictionary(g=>g.Key,g=>g.Count()),captures=folder,errors,hardwareVRTested=false,method="Persistent mesh pool, typed teaching markers and actual branch-routing state observed over time"},Formatting.Indented));m.Enter(3,false);
            }
        }
    }
}

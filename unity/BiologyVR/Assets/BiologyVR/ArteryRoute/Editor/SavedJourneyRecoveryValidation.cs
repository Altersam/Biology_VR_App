using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class SavedJourneyRecoveryValidation
    {
        [MenuItem("Biology VR/Verify Saved Journey After Restart")]
        public static void Verify()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            string folder=Path.Combine(Application.dataPath,"BiologyVR/ArteryJourney/Reports/SessionRecovery");Directory.CreateDirectory(folder);
            try{VerifyCore(folder);}
            catch(Exception error){File.WriteAllText(folder+"/SavedScene_Failure.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,passed=false,reason=error.ToString()},Formatting.Indented));throw;}
        }
        static void VerifyCore(string folder)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var world=UnityEngine.Object.FindFirstObjectByType<JourneyWorld>();if(!world||world.gameObject.scene.path!=ArteryScenePolish.ScenePath)throw new InvalidOperationException("Current master scene required");
            var scene=world.gameObject.scene;var input=world.mission.GetComponent<JourneyInput>();var screens=world.mission.GetComponent<JourneyHolographicScreens>();var rig=input.GetComponent<JourneyInstrumentRig>();var flow=world.GetComponent<JourneyBloodFlow>();var stream=world.GetComponent<JourneyContinuousBloodstream>();var puzzle=world.hemostasisPuzzle;
            var missingScripts=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.GetComponents<Component>().Any(c=>!c)).Select(t=>t.name).ToArray();
            var deps=AssetDatabase.GetDependencies(scene.path,true);var missingDeps=deps.Where(p=>p.StartsWith("Assets/")&&!File.Exists(p)).ToArray();
            bool wound=puzzle&&puzzle.flowLayering&&puzzle.RequiredPlatelets==11&&puzzle.platelets.Length==14&&puzzle.platelets.All(p=>p)&&puzzle.activationPuzzle&&puzzle.activationPuzzle.tokens.Length==4&&puzzle.activationPuzzle.tokens.All(t=>t)&&world.woundState.splitMainFlow&&input.plateletCapture&&input.plateletCapture.puzzle==puzzle;
            bool blood=flow.cells.Length==360&&flow.cells.All(c=>c)&&flow.interactiveCells.Length==5&&flow.continuousBackground==stream&&stream.palette.Length==10&&stream.palette.All(p=>p.mesh&&p.materials.All(m=>m))&&stream.palette.Any(p=>p.id=="phagocyte")&&!stream.palette.Any(p=>p.id=="erythrocyte-O2"||p.id=="erythrocyte-CO2");
            bool ui=screens&&screens.theoryRoot&&screens.vitalsRoot&&screens.theoryScroll.content&&screens.theoryScroll.viewport&&screens.vitalValues.Length==5&&screens.vitalValues.All(t=>t)&&screens.theoryRoot.GetComponent<CanvasScaler>().dynamicPixelsPerUnit==2.5f&&screens.vitalsRoot.GetComponent<CanvasScaler>().dynamicPixelsPerUnit==2.5f&&screens.theoryRoot.GetComponentsInChildren<Text>(true).All(t=>t.font&&t.fontStyle==FontStyle.Bold)&&screens.vitalsRoot.GetComponentsInChildren<Text>(true).All(t=>t.font&&t.fontStyle==FontStyle.Bold)&&Vector3.Distance(screens.theoryRoot.localPosition,new Vector3(-.56f,.025f,1.78f))<.001f&&Vector3.Distance(screens.vitalsRoot.localPosition,new Vector3(.67f,.48f,1.80f))<.001f;
            bool xr=rig&&rig.scannerVR&&rig.bioVR&&rig.scannerPC&&rig.bioPC&&rig.scannerBeam&&rig.captureTether&&rig.captureRays.Length==4&&input.scanAction&&input.toolAction&&input.scanAction.action!=input.toolAction.action&&world.mission.GetComponent<JourneyHud>().inputGuard;
            var experiment=world.hemodynamics;bool hemodynamics=experiment&&experiment.world==world&&experiment.probes.Length==9&&experiment.probes.All(p=>p&&world.targets.Contains(p))&&experiment.toneContact&&world.targets.Contains(experiment.toneContact)&&experiment.cards.Length==3&&experiment.cards.All(c=>c&&c.owner==experiment&&c.grab)&&experiment.tracers.Length==7&&experiment.tracers.All(t=>t)&&experiment.streamlines.Length==8&&experiment.streamlines.All(l=>l)&&experiment.display&&experiment.display.values.Length==3&&experiment.display.values.All(t=>t)&&experiment.display.compactTheory&&experiment.display.compactVitals;
            var research=world.research;bool freeResearch=research&&research.world==world&&research.objects.Length==9&&research.RequiredCount==5&&research.objects.All(o=>o&&o.target&&o.theory&&o.manager==research&&world.targets.Contains(o.target)&&o.theory.id==o.target.targetId&&o.theory.pages.Length>=2&&o.theory.pages.All(p=>!string.IsNullOrEmpty(p.text)))&&research.hoverLabel;
            var bridge=world.bloodEntities;var analysis=world.analysis;var immunity=world.immunity;
            bool connected=bridge&&bridge.world==world&&bridge.stream==stream&&bridge.slots.Length==7&&bridge.slots.All(s=>s.target&&s.body&&s.contact&&s.grab&&s.index==-1)&&bridge.slots.Count(s=>s.type=="phagocyte")==3&&analysis&&analysis.world==world&&analysis.diagram&&analysis.molecules.Length==4&&analysis.molecules.All(t=>t&&(!t.GetComponent<JourneyBloodCargo>()||!t.GetComponent<JourneyBloodCargo>().enabled))&&analysis.hemoglobinCopies.Length==4&&analysis.hemoglobinCopies.All(t=>t)&&immunity&&immunity.world==world&&immunity.trajectory&&immunity.status&&immunity.phagocytes.Length==3&&immunity.phagocytes.All(p=>p&&p.target&&p.body&&p.pseudopodia.Length==6&&p.pseudopodia.All(t=>t))&&immunity.viruses.Length==8&&immunity.viruses.All(v=>v&&v.target&&v.body&&v.contact);
            bool passed=missingScripts.Length==0&&missingDeps.Length==0&&wound&&blood&&ui&&xr&&hemodynamics&&freeResearch&&connected;
            if(passed){AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);}
            File.WriteAllText(folder+"/SavedScene.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,passed,scene=scene.path,missingScripts,missingDeps,sceneDependencyCount=deps.Length,woundReferences=wound,bloodReferences=blood,holographicUiReferences=ui,xrInstrumentReferences=xr,hemodynamicsReferences=hemodynamics,freeBloodResearchReferences=freeResearch,connectedOrganismReferences=connected,targets=world.targets.Length,bloodPool=flow.cells.Length,scope="Saved master scene and serialized gameplay, screen, font and instrument references; runtime reports separate"},Formatting.Indented));
            if(!passed)throw new InvalidOperationException("Saved-scene verification failed; inspect "+folder+"/SavedScene.json");
            Debug.Log("Saved journey references PASS after restart");
        }
    }
}

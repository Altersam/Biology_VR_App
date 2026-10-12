"""Verify persisted source/scene and keep a SHA-checked checkpoint on Z:."""
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parent.parent
if root.drive.upper() != 'Z:':
    raise SystemExit('Session checkpoint must stay on Z:')
runtime = root / 'Assets/BiologyVR/ArteryRoute/Runtime/Journey'
editor = root / 'Assets/BiologyVR/ArteryRoute/Editor'
scene = root / 'Assets/BiologyVR/ArteryJourney/Scenes/ArteryNarrativeVR_VisualRework.unity'
expected = {
    runtime / 'JourneyContinuousBloodstream.cs': ['RefillCoverage', 'CountDensity(limit)', 'float advance=dt*speed[i];'],
    runtime / 'JourneyBloodFlow.cs': ['public int VisibleCellLimit'],
    runtime / 'JourneyPlateletCapture.cs': ['TryAdhereFromField', 'CaptureProgress'],
    runtime / 'JourneyHemostasisPuzzle.cs': ['flowLayering?11:6', 'StabilizationComplete', 'ProcessingActivation'],
    runtime / 'JourneyPlateletActivation.cs': ['EDTA', 'FinishActivationPuzzle'],
    runtime / 'JourneyInput.cs': ['RawScannerPressed', 'RawToolPressed', 'SetUiBlocking', 'GuideEndpoint'],
    runtime / 'JourneyUiInputGuard.cs': ['scanWaitForRelease', 'toolWaitForRelease', 'GuideEndpoint'],
    runtime / 'JourneyInstrumentRig.cs': ['Persistent Scanner aim guide', 'Persistent BioTool aim guide', 'ModeChanged'],
    runtime / 'JourneyInstrumentVisual.cs': ['BeginModeTransition', 'ActivationEvents'],
    runtime / 'BioToolModeController.cs': ['rig.ModeChanged()'],
    editor / 'ArteryXRInputPlaytest.cs': ['ControllerLifecycle', 'CapturePlateletWithField', 'position-controllerRotation*offset'],
    editor / 'ArteryFlowPickupPlaytest.cs': ['TestPersistentAllScenes', 'NativeCapturePlatelet'],
    editor / 'LacerationHemostasisBuilder.cs': ['BuildScratchFibrin'],
    editor / 'FuturisticHolographicScreensBuilder.cs': ['dynamicPixelsPerUnit=2.5f', 'new Vector3(.67f,.48f,1.80f)'],
    editor / 'SavedJourneyRecoveryValidation.cs': ['Verify Saved Journey After Restart', 'sceneDependencyCount'],
    runtime / 'JourneyHolographicFrameGraphic.cs': ['projection rails', 'OnPopulateMesh'],
    runtime / 'JourneyHemodynamicsExperiment.cs': ['ResistanceRatio', 'ProcessingHypothesis', 'CacheGraphics'],
    runtime / 'JourneyHemodynamicsCard.cs': ['ResolveCard', 'Released'],
    runtime / 'JourneyHemodynamicsInterface.cs': ['compactTheory', 'ГОТОВО'],
    editor / 'HemodynamicsExperimentBuilder.cs': ['Install In Vessel Hemodynamics Experiment', 'ProjectionMaterial'],
    editor / 'BiologyReleaseVersion.cs': ['"0.6.0"', 'AndroidCode=7'],
    runtime / 'JourneyResearchTheory.cs': ['ScriptableObject', 'requiredForCompletion'],
    runtime / 'JourneyResearchableObject.cs': ['NotInvestigated,Investigating,Investigated'],
    runtime / 'JourneyResearchManager.cs': ['RequiredInvestigated', 'CompleteResearch', 'Capture()'],
    editor / 'FreeBloodResearchBuilder.cs': ['Install Free First Blood Research', 'Гем и железо Fe²⁺'],
    runtime / 'JourneyBloodInteractionPool.cs': ['Promote(Ray', 'HasPhagocyteCapacity', 'CancelInteractableSelection'],
    runtime / 'JourneyErythrocyteAnalysis.cs': ['Subject', 'Available(string id)', 'Close()'],
    runtime / 'JourneyPhagocyteEntity.cs': ['RemainingCapacity', 'Phagocytosis,Recovering'],
    runtime / 'JourneyImmuneVirus.cs': ['BeingPhagocytosed', 'Generation'],
    runtime / 'JourneyInnateImmunity.cs': ['mission.Progress+LiveCount', 'world.mission.ViralLoad<=0', 'Predict(Vector3', 'TickEngulf'],
    runtime / 'JourneyPlateletEntity.cs': ['Flowing,Inactive,Activated,Adhered,Aggregated'],
    editor / 'LivingOrganismJourneyBuilder.cs': ['Install Connected Living Blood And Innate Immunity', 'controller.viruses=new JourneyImmuneVirus[8]'],
    scene: ['flowLayering: 1', 'splitMainFlow: 1', 'Flow-caught platelet 10', 'Reserve balance platelet 13', 'plateletCapture: {fileID: 1035892481}', 'm_DynamicPixelsPerUnit: 2.5', 'Projection frame brackets'],
}
for path, markers in expected.items():
    content = path.read_bytes()
    if b'\0' in content:
        raise SystemExit(f'Unexpected null bytes: {path}')
    text = content.decode('utf-8-sig')
    for marker in markers:
        if marker not in text:
            raise SystemExit(f'Missing persisted change {marker!r}: {path}')

files = set(expected)
files.update(runtime.glob('Journey*.cs'))
files.update(root.glob('*_RU.md'))
files.add(root / 'README.md')
files.update((root / 'Tools').glob('*laceration*'))
files.update((root / 'Tools').glob('*persistent_blood*'))
files.update((root / 'Tools').glob('*controller_lifecycle*'))
files.update((root / 'Tools').glob('*futuristic_holograms*'))
files.update((root / 'Tools').glob('*futuristic_hologram*'))
files.update((root / 'Tools').glob('*hemodynamics*'))
files.update((root / 'Tools').glob('*free_blood_research*'))
files.update((root / 'Tools').glob('*free_research*'))
files.update((root / 'Tools').glob('*living_organism*'))
files.add(root / 'Tools/saved_journey_verify_ws.json')
files.add(Path(__file__).resolve())
files.update((root / 'Assets/BiologyVR/ArteryJourney/Generated/HolographicScreens').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Generated/HemodynamicsExperiment').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Generated/FreeBloodResearch').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Generated/LivingOrganismJourney').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/LivingOrganismJourney').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/FreeBloodResearch').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/HemodynamicsExperiment').rglob('*'))
files.add(root / 'Assets/BiologyVR/ArteryRoute/Shaders/ArteryVisualV2.shader')
files.update((root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/FuturisticHolograms').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Generated/LacerationHemostasis').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Reports/LacerationHemostasis').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/XRControllers').rglob('*'))
files.update((root / 'Assets/BiologyVR/ArteryJourney/Reports/SessionRecovery').rglob('*'))
for name in ['Connected_Organism_PC.json', 'Connected_Wave_PC.json', 'Connected_Organism_Progress.json', 'Connected_Research_Orders_PC.json', 'Connected_Research_Orders_Progress.json', 'Free_Blood_Research_PC_Input.json', 'Hemodynamics_PC_Input.json', 'Laceration_PC_Input.json', 'Plaque_Challenge_PC.json', 'All_Scenes_Persistent_Blood.json', 'Mobile_All_Scenes_Persistent_Blood.json']:
    files.add(root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/FlowPickup' / name)
files.add(root / 'Assets/BiologyVR/ArteryJourney/Reports/Polish/VR_INPUT_PLAYTHROUGH_V7.md')
for path in list(files):
    meta = Path(str(path) + '.meta')
    if meta.is_file():
        files.add(meta)
stamp = datetime.now(timezone.utc)
dest = root / 'RecoveryCheckpoints' / stamp.strftime('%Y%m%d_%H%M%S')
dest.mkdir(parents=True, exist_ok=False)
rows = []
for path in sorted(p for p in files if p.is_file()):
    rel = path.relative_to(root)
    target = dest / rel
    target.parent.mkdir(parents=True, exist_ok=True)
    source_hash = hashlib.sha256(path.read_bytes()).hexdigest()
    shutil.copy2(path, target)
    if source_hash != hashlib.sha256(target.read_bytes()).hexdigest():
        raise SystemExit(f'Checkpoint hash mismatch: {rel}')
    rows.append({'path': rel.as_posix(), 'bytes': path.stat().st_size, 'sha256': source_hash})
report = {'utc': stamp.isoformat(), 'passed': True, 'checkpoint': str(dest),
          'scope': 'Persisted critical code, connected living blood/immunity, master scene, wound assets, docs and available reports; runtime verification is separate',
          'files': rows}
(dest / 'Manifest.json').write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
(root / 'SESSION_CHECKPOINT_VERIFICATION.json').write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
print(f'Persisted changes PASS; SHA-verified checkpoint: {dest}; files={len(rows)}')

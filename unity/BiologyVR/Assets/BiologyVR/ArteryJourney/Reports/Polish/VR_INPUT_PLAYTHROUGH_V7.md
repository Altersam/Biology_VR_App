# V8 Sequential XR Input Playthrough

Generated UTC: 2026-10-08T12:53:48.7975642Z

Status: FULL SIMULATOR PASS

## Scene03→16 and VERY FAR

XR Input System simulator layouts; controller states queued into actual action bindings and XRI. Not a physical headset test.

PASS: Existing Scanner/BioTool actions enabled

Diagnostic: step=1 ready=True RBC selected=True grabbed=True runtime right grip=1 button=True deviceEnabled=True focused=False controller=(0.45, -0.14, 2.51) RBC=(0.43, -0.15, 2.58) bounds=Center: (0.43, -0.15, 2.58), Extents: (0.13, 0.13, 0.13) collider=True grab=True

RBC target enabled=True mission matches=True selecting=Near-Far Interactor

RBC eventsAttached=True

RBC grab=Research target — rbc (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)

RBC disabling=False

RBC lastAcceptedGrabId=rbc

Interactor Gaze Interactor XRGazeInteractor enabled=False hover=False selected=False targets= selectActive=False pos=(0.00, 0.15, 0.00)

Interactor Poke Interactor XRPokeInteractor enabled=True hover=False selected=False targets= selectActive=True pos=(0.03, -0.14, 2.51)

Interactor Near-Far Interactor NearFarInteractor enabled=True hover=False selected=False targets= selectActive=False pos=(0.03, -0.14, 2.51)

Interactor Teleport Interactor XRRayInteractor enabled=False hover=False selected=False targets= selectActive=False pos=(0.03, -0.16, 2.47)

Interactor Poke Interactor XRPokeInteractor enabled=True hover=False selected=False targets= selectActive=True pos=(0.45, -0.14, 2.51)

Interactor Near-Far Interactor NearFarInteractor enabled=True hover=False selected=True targets=Research target — rbc selectActive=True pos=(0.45, -0.14, 2.51)

Interactor Teleport Interactor XRRayInteractor enabled=False hover=False selected=False targets= selectActive=False pos=(0.45, -0.16, 2.47)

Interactor Climb Teleport ClimbTeleportInteractor enabled=True hover=False selected=False targets= selectActive=False pos=(0.00, -1.21, 0.00)

Interactor Desktop mouse XRI hand JourneyDesktopInteractor enabled=True hover=False selected=False targets= selectActive=False pos=(-0.95, 0.75, 1.00)

PASS: Right grip actually selects RBC and advances Grab

PASS: Early Scanner release does not complete scan

PASS: Held real Scanner trigger completes raycast scan

PASS: Both grips select the same RBC

PASS: Separating simulated hands triggers educational enlargement

PASS: Bringing both hands together reduces scale without another objective

PASS: Releasing one hand leaves object in other hand

PASS: Scanning the wrong visible target does not complete Hb objective

PASS: Scanner objective expected for hemoglobin

Contact hemoglobin: Research target — hemoglobin / SphereCollider layerInspector=True

Scan hemoglobin: step=4 enabled=True lock=hemoglobin progress=1 blocker=none aim=(-0.26, 0.05, 0.20) target=(0.55, -0.19, 1.66) contact=(0.55, -0.19, 1.66)

PASS: Scanner completed hemoglobin

PASS: Scanner objective expected for heme

Contact heme: Research target — heme / SphereCollider layerInspector=True

Scan heme: step=5 enabled=True lock=heme progress=1 blocker=none aim=(-0.22, 0.08, 0.16) target=(1.09, 0.11, 1.29) contact=(1.09, 0.11, 1.29)

PASS: Scanner completed heme

PASS: Hb and heme actually scanned through device trigger

PASS: Physically gripping oxygen detaches it

PASS: Placing and releasing O2 near heme binds it

PASS: Scanner objective expected for co2

Contact co2: Research target — co2 / SphereCollider layerInspector=True

Scan co2: step=8 enabled=True lock=plasma progress=1 blocker=none aim=(-0.24, 0.04, 0.19) target=(0.68, -0.32, 1.44) contact=(0.68, -0.32, 1.44)

PASS: Scanner completed co2

PASS: WBC actually travels in blood flow before XR grip catch

PASS: Right XR grip catches travelling WBC

PASS: Scanner objective expected for leukocyte

Contact leukocyte: Research target — leukocyte / SphereCollider layerInspector=True

Scan leukocyte: step=9 enabled=True lock=leukocyte progress=1 blocker=none aim=(-0.34, 0.06, 0.22) target=(-0.07, -0.41, 5.57) contact=(-0.07, -0.41, 5.57)

PASS: Scanner completed leukocyte

PASS: WBC held scan changes goal to platelet without losing hold

PASS: XR release removes WBC hold without an abrupt home teleport

PASS: Released XR WBC rejoins actual pool transform and continues downstream

PASS: Scanner objective expected for platelet

PASS: Moving scanner objective expected for platelet

Tracked scan platelet: locked=leukocyte progress=0

PASS: Scanner completed moving target platelet

PASS: Gas and formed elements scanned through actual Scanner action

PASS: BioTool comparison requires actual ray and trigger

PASS: Scanner objective expected for plasma

Contact plasma: Research target — plasma / SphereCollider layerInspector=True

Scan plasma: step=12 enabled=True lock=plasma progress=1 blocker=none aim=(-0.24, 0.04, 0.19) target=(0.92, -0.35, 1.78) contact=(0.92, -0.35, 1.78)

PASS: Scanner completed plasma

PASS: All Scene03 objectives completed without Accept/Primary/debug calls

PASS: Actual right secondary-button InputAction starts completed 03→04 travel

PASS: Completed Scene03 location stays active during transition

PASS: 03→04 smooth travel arrives without debug enter/force-next

PASS: Origin and tracked head rotation preserved across travel

PASS: Same live Scanner/BioTool aim instances persist across travel

PASS: Observed nearby blood cell continues in world without travelling-anchor jump

PASS: Scanner objective expected for endothelium

Contact endothelium: Scene04_Band_endothelium / MeshCollider layerInspector=True

PASS: Scanner uses actual wall shell contact for endothelium

Scan endothelium: step=1 enabled=True lock=layers progress=1 blocker=none aim=(0.82, 0.07, 10.12) target=(1.86, -0.08, 12.87) contact=(1.86, -0.08, 12.87)

PASS: Scanner completed endothelium

PASS: Real BioTool trigger activates wall layer unfolding

PASS: Scanner objective expected for intima

Contact intima: Scene04_Band_intima / MeshCollider layerInspector=True

PASS: Scanner uses actual wall shell contact for intima

Scan intima: step=3 enabled=True lock=media progress=1 blocker=none aim=(0.82, 0.07, 10.12) target=(1.86, -0.08, 12.87) contact=(1.86, -0.08, 12.87)

PASS: Scanner completed intima

PASS: Scanner objective expected for media

Contact media: Scene04_Band_media / MeshCollider layerInspector=True

PASS: Scanner uses actual wall shell contact for media

Scan media: step=4 enabled=True lock=adventitia progress=1 blocker=none aim=(0.82, 0.07, 10.11) target=(1.86, -0.08, 12.87) contact=(1.86, -0.08, 12.87)

PASS: Scanner completed media

PASS: Scanner objective expected for adventitia

Contact adventitia: Scene04_Band_adventitia / MeshCollider layerInspector=True

PASS: Scanner uses actual wall shell contact for adventitia

Scan adventitia: step=5 enabled=True lock=none progress=1 blocker=none aim=(0.82, 0.07, 10.11) target=(1.86, -0.08, 12.87) contact=(1.86, -0.08, 12.87)

PASS: Scanner completed adventitia

PASS: All Scene04 wall shells scanned through real Scanner actions

PASS: Vitals remain on compact HUD outside optional experiment module

PASS: Real right secondary-button continues completed Scene04

PASS: Transition 04→05 arrives through normal travel

PASS: BioTool objective expected for flow

PASS: BioTool target exists: flow

PASS: BioTool target contact enabled: flow

Tool flow: locked=flow charge=0 radius=1 pressure=120 aim=(12.14, -11.85, 39.76) contact=(11.75, -10.70, 41.58)

PASS: BioTool completed flow

PASS: BioTool objective expected for flow-model

PASS: BioTool target exists: flow-model

PASS: BioTool target contact enabled: flow-model

Tool flow-model: locked=flow-model charge=0 radius=1 pressure=120 aim=(12.19, -11.86, 39.77) contact=(12.48, -10.97, 41.51)

PASS: BioTool completed flow-model

PASS: Scene05 flow measurement and holographic model activation use BioTool trigger

PASS: BioTool objective expected for small-radius

PASS: BioTool target exists: small-radius

PASS: BioTool target contact enabled: small-radius

Tool small-radius: locked=small-radius charge=0 radius=0,65 pressure=120 aim=(12.24, -11.88, 39.76) contact=(13.20, -11.24, 41.43)

PASS: BioTool completed small-radius

PASS: BioTool objective expected for large-radius

PASS: BioTool target exists: large-radius

PASS: BioTool target contact enabled: large-radius

Tool large-radius: locked=large-radius charge=0 radius=1,35 pressure=120 aim=(12.12, -11.90, 39.78) contact=(11.49, -11.48, 41.87)

PASS: BioTool completed large-radius

PASS: BioTool objective expected for pressure

PASS: BioTool target exists: pressure

PASS: BioTool target contact enabled: pressure

Tool pressure: locked=pressure charge=0 radius=1,35 pressure=155 aim=(12.17, -11.92, 39.80) contact=(12.22, -11.75, 41.79)

PASS: BioTool completed pressure

PASS: BioTool objective expected for normal-flow

PASS: BioTool target exists: normal-flow

PASS: BioTool target contact enabled: normal-flow

Tool normal-flow: locked=normal-flow charge=0 radius=1 pressure=120 aim=(12.22, -11.94, 39.79) contact=(12.95, -12.02, 41.72)

PASS: BioTool completed normal-flow

PASS: Scene05 radius/pressure experiment completes and returns to normal flow

PASS: Real right secondary-button continues completed Scene05

PASS: Transition 05→06 arrives through normal travel

PASS: BioTool objective expected for plaque-flow

PASS: BioTool target exists: plaque-flow

PASS: BioTool target contact enabled: plaque-flow

Tool plaque-flow: locked=none charge=0 radius=1 pressure=120 aim=(37.25, 9.04, 62.73) contact=(37.96, 9.17, 64.19)

PASS: BioTool completed plaque-flow

PASS: Scanner objective expected for plaque

Contact plaque: Research target — plaque / SphereCollider layerInspector=True

Scan plaque: step=2 enabled=True lock=none progress=1 blocker=none aim=(37.10, 8.98, 63.27) target=(41.78, 7.73, 64.43) contact=(41.78, 7.73, 64.43)

PASS: Scanner completed plaque

PASS: Scene06 plaque formation completes before pulse mode

PASS: BioTool objective expected for pulse-mode

PASS: BioTool target exists: pulse-mode

PASS: BioTool target contact enabled: pulse-mode

Tool pulse-mode: locked=none charge=0 radius=1 pressure=120 aim=(37.28, 9.01, 62.72) contact=(38.19, 8.88, 64.05)

PASS: BioTool completed pulse-mode

PASS: Scene06 plaque inspection and pulse mode use real instruments

PASS: Lipid zone contact enabled: 0

Pulse zone 0: locked=none selected=1 progress=1 step=3 contact=(39.81, 9.03, 64.82) aim=(37.31, 9.03, 62.69)

PASS: BioTool processed lipid zone 0

PASS: Lipid zone contact enabled: 1

Pulse zone 1: locked=none selected=2 progress=2 step=3 contact=(40.30, 9.36, 64.92) aim=(37.31, 9.04, 62.68)

PASS: BioTool processed lipid zone 1

PASS: Lipid zone contact enabled: 2

Pulse zone 2: locked=none selected=-1 progress=0 step=4 contact=(40.63, 9.29, 65.44) aim=(37.31, 9.04, 62.68)

PASS: BioTool processed lipid zone 2

PASS: Three distinct lipid zones pulse without cap hits through BioTool

PASS: Real right secondary-button continues completed Scene06

PASS: Transition 06→07 arrives through normal travel

PASS: Completed plaque retains root, treated zones, mesh instance/vertices and pose across 06→07

PASS: Completed plaque remains visible with dormant lipid contacts

PASS: Head/origin rotation and instrument instances persist across 06→07

PASS: Moving scanner objective expected for embolus

Tracked scan embolus: locked=none progress=0

PASS: Scanner completed moving target embolus

PASS: BioTool objective expected for attract-mode

PASS: BioTool target exists: attract-mode

PASS: BioTool target contact enabled: attract-mode

Tool attract-mode: locked=attract-mode charge=0 radius=1 pressure=120 aim=(23.12, 2.97, 92.87) contact=(22.06, 2.71, 94.17)

PASS: BioTool completed attract-mode

PASS: Releasing BioTool before charge finishes does not capture embolus

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(23.15, 2.98, 92.89) target=(20.81, 2.35, 97.59)

PASS: BioTool captured moving target embolus

PASS: Held BioTool trigger captures moving embolus after charge

PASS: Capture outside trap does not auto-deposit

PASS: Releasing trigger breaks attraction and retries only capture goal

PASS: Release preserves nearby bubble pose and completed scanning

PASS: Released embolus drifts again with the flow

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(23.15, 2.72, 93.22) target=(21.43, 2.51, 96.35)

PASS: BioTool captured moving target embolus

PASS: Released embolus can be re-captured through trigger input

Transport result: bubble=(22.33, 1.93, 94.67) trap=(22.33, 1.92, 94.66) stability=1

PASS: Only sustained in-trap placement completes geometric deposit

PASS: Held field transports and stabilizes embolus physically inside trap

PASS: Real right secondary-button continues completed Scene07

PASS: Transition 07→08 arrives through normal travel

PASS: Scanner objective expected for virus-study

Contact virus-study: Research target — virus-study / SphereCollider layerInspector=True

Scan virus-study: step=1 enabled=True lock=virus-study progress=1 blocker=none aim=(41.94, 14.25, 124.00) target=(43.45, 14.36, 124.96) contact=(43.45, 14.36, 124.96)

PASS: Scanner completed virus-study

PASS: Study virus has authored XRI grab support

PASS: BioTool click cannot replace two-hand virus enlargement

PASS: Both real grips select the same study virus

PASS: Two-hand separation enlarges the persistent intact/cutaway model

PASS: Virus visual follows its held XRI target

PASS: One released virus grip keeps the other grip selected

PASS: Educational virus scale persists after releasing both hands

PASS: Scanner objective expected for genome

Contact genome: Research target — genome / SphereCollider layerInspector=True

Scan genome: step=3 enabled=True lock=capsid progress=1 blocker=none aim=(42.17, 14.34, 124.01) target=(43.49, 14.31, 124.99) contact=(43.49, 14.31, 124.99)

PASS: Scanner completed genome

PASS: Scanner objective expected for capsid

Contact capsid: Research target — capsid / SphereCollider layerInspector=True

Scan capsid: step=4 enabled=True lock=epitope progress=1 blocker=none aim=(42.17, 14.35, 124.01) target=(43.45, 14.36, 124.96) contact=(43.45, 14.36, 124.96)

PASS: Scanner completed capsid

PASS: Scanner objective expected for epitope

Contact epitope: Research target — epitope / SphereCollider layerInspector=True

Scan epitope: step=5 enabled=True lock=none progress=1 blocker=none aim=(42.17, 14.36, 124.00) target=(43.64, 14.19, 125.05) contact=(43.64, 14.19, 125.05)

PASS: Scanner completed epitope

PASS: All Scene08 parts scanned without tool enlargement/debug completion

PASS: Real right secondary-button continues completed Scene08

PASS: Same-site transition 08→09 arrives through normal travel

PASS: BioTool objective expected for immune-mode

PASS: BioTool target exists: immune-mode

PASS: BioTool target contact enabled: immune-mode

Tool immune-mode: locked=immune-mode charge=0 radius=1 pressure=120 aim=(42.14, 13.77, 124.07) contact=(42.86, 14.86, 124.98)

PASS: BioTool completed immune-mode

PASS: Charging BioTool at the phagocyte cannot mark an unrelated incoming virion

PASS: Incoming virion has active target contact: 0

PASS: Charged BioTool marks the aimed moving virion: 0

PASS: Incoming virion has active target contact: 1

PASS: Charged BioTool marks the aimed moving virion: 1

PASS: Incoming virion has active target contact: 2

PASS: Charged BioTool marks the aimed moving virion: 2

PASS: Incoming virion has active target contact: 3

PASS: Charged BioTool marks the aimed moving virion: 3

PASS: Incoming virion has active target contact: 4

PASS: Charged BioTool marks the aimed moving virion: 4

PASS: Incoming virion has active target contact: 5

PASS: Charged BioTool marks the aimed moving virion: 5

PASS: Incoming virion has active target contact: 6

PASS: Charged BioTool marks the aimed moving virion: 6

PASS: Incoming virion has active target contact: 7

PASS: Charged BioTool marks the aimed moving virion: 7

PASS: Eight individual incoming virions marked through actual charged trigger input

PASS: BioTool objective expected for phagocyte

PASS: BioTool target exists: phagocyte

PASS: BioTool target contact enabled: phagocyte

Tool phagocyte: locked=phagocyte charge=0 radius=1 pressure=120 aim=(42.15, 13.74, 124.08) contact=(42.96, 14.49, 125.08)

PASS: BioTool completed phagocyte

PASS: Scene09 handoff completes only after eight actual phagocyte ingestions

PASS: Scene09 phagocyte stays in its current episode instead of moving to the future wound

PASS: Real right secondary-button continues completed Scene09

PASS: Transition 09→10 arrives through normal travel

PASS: BioTool objective expected for pressure-low

PASS: BioTool target exists: pressure-low

PASS: BioTool target contact enabled: pressure-low

Tool pressure-low: locked=pressure-low charge=0 radius=1 pressure=120 aim=(68.00, 3.47, 138.21) contact=(68.81, 3.02, 139.57)

PASS: BioTool completed pressure-low

PASS: BioTool objective expected for tone-mode

PASS: BioTool target exists: tone-mode

PASS: BioTool target contact enabled: tone-mode

Tool tone-mode: locked=tone-mode charge=0 radius=1 pressure=120 aim=(67.93, 3.50, 138.25) contact=(68.03, 3.34, 139.70)

PASS: BioTool completed tone-mode

PASS: Scene10 pressure loss measured before opening vascular control

PASS: Holding/clicking tone without hand movement cannot auto-set radius

PASS: Tone control locks to a correctly aimed held trigger

PASS: Excessive manual constriction warns but cannot complete moderate-tone goal

PASS: Passing into safe tone range still requires a sustained steady hold

PASS: Correcting hand position and settling finishes tone through InputActions

PASS: Manual BioTool motion and settling produce moderate constriction and temporary stabilization

PASS: BioTool objective expected for pressure-stable

PASS: BioTool target exists: pressure-stable

PASS: BioTool target contact enabled: pressure-stable

Tool pressure-stable: locked=pressure-stable charge=0 radius=0,8017082 pressure=120 aim=(67.76, 4.06, 138.05) contact=(68.76, 3.89, 139.76)

PASS: BioTool completed pressure-stable

PASS: Early repeat measurement cannot complete the pressure-drop goal

PASS: BioTool objective expected for pressure-return

PASS: BioTool target exists: pressure-return

PASS: BioTool target contact enabled: pressure-return

Tool pressure-return: locked=pressure-return charge=0 radius=0,8017082 pressure=120 aim=(67.69, 4.09, 138.07) contact=(67.94, 4.23, 139.90)

PASS: BioTool completed pressure-return

PASS: Repeated real measurement detects delayed pressure fall despite vascular tone

PASS: Real right secondary-button continues completed Scene10

PASS: Transition 10→11 arrives through normal travel

PASS: Treated tone root and shader radius persist on entering wound diagnosis

PASS: 10→11 preserves head/origin rotation and original instrument instances

PASS: Scene11 starts with actual pooled outward blood leakage

PASS: Scanner objective expected for wound

Contact wound: Research target — wound / SphereCollider layerInspector=True

Scan wound: step=1 enabled=True lock=leak progress=1 blocker=none aim=(91.31, 1.83, 177.15) target=(91.35, 2.99, 183.40) contact=(91.35, 2.99, 183.40)

PASS: Scanner completed wound

PASS: BioTool objective expected for leak

PASS: BioTool target exists: leak

PASS: BioTool target contact enabled: leak

Tool leak: locked=none charge=0 radius=0,8017082 pressure=120 aim=(90.69, 2.08, 177.26) contact=(91.35, 2.99, 183.40)

PASS: BioTool completed leak

PASS: BioTool objective expected for pressure-low

PASS: BioTool target exists: pressure-low

PASS: BioTool target contact enabled: pressure-low

Tool pressure-low: locked=pressure-low charge=0 radius=0,8017082 pressure=120 aim=(90.78, 2.08, 177.23) contact=(91.66, 2.36, 178.39)

PASS: BioTool completed pressure-low

PASS: Scene11 Scanner/measurement sequence identifies wound, leak and pressure loss

PASS: Diagnosed wound remains in the same physical location and keeps leaking until treated

PASS: Scene12 continues same wound and leak instances from Scene11

PASS: Six physical platelet XRI pieces authored at existing wound

PASS: Real right grip selects Controllable platelet 0

Released Controllable platelet 0 position=(92.47, 2.56, 182.85) intended=(92.47, 2.56, 182.85) accepted=False

PASS: Wrong physical platelet placement stays unaccepted and can be re-grabbed

PASS: Real right grip selects Controllable platelet 0

Released Controllable platelet 0 position=(91.70, 2.91, 183.55) intended=(91.70, 2.91, 183.55) accepted=True

PASS: First actual platelet release adheres to wound

PASS: BioTool objective expected for platelet

PASS: BioTool target exists: platelet

PASS: BioTool target contact enabled: platelet

Tool platelet: locked=platelet charge=0 radius=0,8017082 pressure=120 aim=(90.67, 2.05, 177.18) contact=(91.70, 2.91, 183.55)

PASS: BioTool completed platelet

PASS: First attached platelet changes into activated pseudopod-like mesh

PASS: Real right grip selects Controllable platelet 1

Released Controllable platelet 1 position=(91.53, 2.63, 183.44) intended=(91.53, 2.63, 183.44) accepted=True

PASS: Real right grip selects Controllable platelet 2

Released Controllable platelet 2 position=(91.28, 2.68, 183.21) intended=(91.28, 2.68, 183.21) accepted=True

PASS: Real right grip selects Controllable platelet 3

Released Controllable platelet 3 position=(91.28, 2.95, 183.01) intended=(91.28, 2.95, 183.01) accepted=True

PASS: Real right grip selects Controllable platelet 4

Released Controllable platelet 4 position=(91.45, 3.23, 183.12) intended=(91.45, 3.23, 183.12) accepted=True

PASS: Real right grip selects Controllable platelet 5

Released Controllable platelet 5 position=(91.70, 3.19, 183.35) intended=(91.70, 3.19, 183.35) accepted=True

PASS: Five more hand-placed platelets form spatial plug and reduce real leak

PASS: Wrong available factor scan cannot substitute thrombin identification

PASS: Scanner objective expected for thrombin

Contact thrombin: Research target — thrombin / SphereCollider layerInspector=True

Scan thrombin: step=4 enabled=True lock=thrombin progress=1 blocker=none aim=(91.32, 1.83, 177.17) target=(91.69, 1.87, 182.99) contact=(91.69, 1.87, 182.99)

PASS: Scanner completed thrombin

PASS: BioTool objective expected for thrombin

PASS: BioTool target exists: thrombin

PASS: BioTool target contact enabled: thrombin

Tool thrombin: locked=thrombin charge=0 radius=0,8017082 pressure=120 aim=(90.67, 2.02, 177.18) contact=(91.69, 1.87, 182.99)

PASS: BioTool completed thrombin

PASS: Thrombin reaction generates physical fibrin strand endpoints

PASS: Real right grip selects Physical fibrin end handle 0

Released Physical fibrin end handle 0 position=(91.31, 3.40, 182.66) intended=(91.31, 3.40, 182.66) accepted=False

PASS: Releasing fibrin at its starting anchor does not complete a connection

PASS: Real right grip selects Physical fibrin end handle 0

Released Physical fibrin end handle 0 position=(91.84, 2.38, 183.74) intended=(91.84, 2.38, 183.74) accepted=True

PASS: Real right grip selects Physical fibrin end handle 1

Released Physical fibrin end handle 1 position=(91.98, 2.84, 183.67) intended=(91.98, 2.84, 183.67) accepted=True

PASS: Real right grip selects Physical fibrin end handle 2

Released Physical fibrin end handle 2 position=(92.12, 3.29, 183.60) intended=(92.12, 3.29, 183.60) accepted=True

PASS: Scene12 requires three spatial fibrin connections and closes same leaking wound

PASS: Scene13 inherits same six-platelet plug and three fibrin links without reset

PASS: Hand-placed platelet transforms persist across 12→13

PASS: Scanner objective expected for clot

Contact clot: Research target — clot / SphereCollider layerInspector=True

Scan clot: step=1 enabled=True lock=platelet progress=1 blocker=none aim=(91.31, 1.86, 177.17) target=(91.35, 2.99, 183.40) contact=(91.35, 2.99, 183.40)

PASS: Scanner completed clot

PASS: Real right grip selects Controllable platelet 0

Released Controllable platelet 0 position=(92.54, 2.53, 182.78) intended=(92.54, 2.53, 182.78) accepted=False

PASS: Real right grip selects Controllable platelet 1

Released Controllable platelet 1 position=(92.37, 2.26, 182.67) intended=(92.37, 2.26, 182.67) accepted=False

PASS: Real right grip selects Controllable platelet 2

Released Controllable platelet 2 position=(92.12, 2.30, 182.44) intended=(92.12, 2.30, 182.44) accepted=False

PASS: Real right grip selects Controllable platelet 3

Released Controllable platelet 3 position=(92.13, 2.58, 182.24) intended=(92.13, 2.58, 182.24) accepted=False

PASS: Real right grip selects Physical fibrin end handle 0

Released Physical fibrin end handle 0 position=(91.31, 3.40, 182.66) intended=(91.31, 3.40, 182.66) accepted=False

PASS: Real right grip selects Physical fibrin end handle 1

Released Physical fibrin end handle 1 position=(91.17, 2.95, 182.73) intended=(91.17, 2.95, 182.73) accepted=False

PASS: Real right grip selects Physical fibrin end handle 2

Released Physical fibrin end handle 2 position=(91.04, 2.49, 182.80) intended=(91.04, 2.49, 182.80) accepted=False

PASS: Removing real platelets/fibrin produces UNDER with renewed blood leak

PASS: Real right grip selects Controllable platelet 0

Released Controllable platelet 0 position=(91.70, 2.91, 183.55) intended=(91.70, 2.91, 183.55) accepted=True

PASS: Real right grip selects Controllable platelet 1

Released Controllable platelet 1 position=(91.53, 2.63, 183.44) intended=(91.53, 2.63, 183.44) accepted=True

PASS: Real right grip selects Controllable platelet 2

Released Controllable platelet 2 position=(91.28, 2.68, 183.21) intended=(91.28, 2.68, 183.21) accepted=True

PASS: Real right grip selects Controllable platelet 3

Released Controllable platelet 3 position=(91.28, 2.95, 183.01) intended=(91.28, 2.95, 183.01) accepted=True

PASS: Real right grip selects Physical fibrin end handle 0

Released Physical fibrin end handle 0 position=(91.84, 2.38, 183.74) intended=(91.84, 2.38, 183.74) accepted=True

PASS: Real right grip selects Physical fibrin end handle 1

Released Physical fibrin end handle 1 position=(91.98, 2.84, 183.67) intended=(91.98, 2.84, 183.67) accepted=True

PASS: Real right grip selects Physical fibrin end handle 2

Released Physical fibrin end handle 2 position=(92.12, 3.29, 183.60) intended=(92.12, 3.29, 183.60) accepted=True

PASS: Real right grip selects Reserve hand platelet 6

Released Reserve hand platelet 6 position=(91.85, 2.89, 183.72) intended=(91.85, 2.89, 183.72) accepted=True

PASS: Real right grip selects Reserve hand platelet 7

Released Reserve hand platelet 7 position=(91.55, 2.43, 183.55) intended=(91.55, 2.43, 183.55) accepted=True

PASS: Real right grip selects Reserve hand platelet 8

Released Reserve hand platelet 8 position=(91.14, 2.50, 183.15) intended=(91.14, 2.50, 183.15) accepted=True

PASS: Adding reserve platelets by hand produces OVER obstruction and slowed flow

PASS: Real right grip selects Reserve hand platelet 6

Released Reserve hand platelet 6 position=(92.76, 2.48, 182.89) intended=(92.76, 2.48, 182.89) accepted=False

PASS: Real right grip selects Reserve hand platelet 7

Released Reserve hand platelet 7 position=(92.46, 2.03, 182.72) intended=(92.46, 2.03, 182.72) accepted=False

PASS: Real right grip selects Reserve hand platelet 8

Released Reserve hand platelet 8 position=(92.06, 2.09, 182.32) intended=(92.06, 2.09, 182.32) accepted=False

PASS: Physical correction returns to OPTIMAL with open lumen

PASS: BioTool objective expected for open-flow

PASS: BioTool target exists: open-flow

PASS: BioTool target contact enabled: open-flow

Tool open-flow: locked=open-flow charge=0 radius=0,8017082 pressure=120 aim=(90.77, 2.07, 177.13) contact=(92.28, 2.53, 178.41)

PASS: BioTool completed open-flow

PASS: Scene13 balance completed by actual piece removal/replacement without slider

PASS: Scene14 inherits optimal same-wound plug and network without resetting them

PASS: Scene13 platelet positions are retained at recovery entry

PASS: Scanner identifies actual debris Cellular debris 0

PASS: Scanner identifies actual debris Cellular debris 1

PASS: Scanner identifies actual debris Cellular debris 2

PASS: Scanner identifies three distinct debris without prematurely deleting them

PASS: BioTool objective expected for cleanup-mode

PASS: BioTool target exists: cleanup-mode

PASS: BioTool target contact enabled: cleanup-mode

Tool cleanup-mode: locked=cleanup-mode charge=0 radius=0,8017082 pressure=120 aim=(90.67, 2.03, 177.18) contact=(91.43, 2.01, 182.64)

PASS: BioTool completed cleanup-mode

PASS: BioTool assists actual phagocytosis of Cellular debris 0

PASS: BioTool assists actual phagocytosis of Cellular debris 1

PASS: BioTool assists actual phagocytosis of Cellular debris 2

PASS: Three selected debris physically engulfed before progressing to fibrinolysis

PASS: Scanner objective expected for plasmin

Contact plasmin: Recovery factor — plasmin / SphereCollider layerInspector=True

Scan plasmin: step=4 enabled=True lock=plasmin progress=1 blocker=none aim=(91.33, 1.83, 177.17) target=(92.01, 1.93, 183.32) contact=(92.01, 1.93, 183.32)

PASS: Scanner completed plasmin

PASS: BioTool objective expected for plasmin

PASS: BioTool target exists: plasmin

PASS: BioTool target contact enabled: plasmin

Tool plasmin: locked=plasmin charge=0 radius=0,8017082 pressure=120 aim=(90.68, 2.03, 177.17) contact=(92.01, 1.93, 183.32)

PASS: BioTool completed plasmin

PASS: Player-activated plasmin progressively removes the existing fibrin network

PASS: BioTool objective expected for repair

PASS: BioTool target exists: repair

PASS: BioTool target contact enabled: repair

Tool repair: locked=healed-wall charge=0 radius=0,8017082 pressure=120 aim=(90.66, 2.05, 177.18) contact=(91.35, 2.99, 183.40)

PASS: BioTool completed repair

PASS: Early Scanner cannot claim fully restored endothelium during repair

PASS: Scanner objective expected for healed-wall

Contact healed-wall: Research target — healed-wall / SphereCollider layerInspector=True

Scan healed-wall: step=7 enabled=True lock=none progress=1 blocker=none aim=(91.31, 1.86, 177.17) target=(91.35, 2.99, 183.40) contact=(91.35, 2.99, 183.40)

PASS: Scanner completed healed-wall

PASS: Actual recovery process closes and scans the same original wound

PASS: Final-focus travel preserves repaired original wound and wall instance

PASS: 14→15 preserves origin/gaze and original tools

PASS: Scanner objective expected for infected-cell

Contact infected-cell: Research target — infected-cell / SphereCollider layerInspector=True

Scan infected-cell: step=1 enabled=True lock=infected-cell progress=1 blocker=none aim=(119.07, 15.79, 233.31) target=(121.29, 17.71, 235.89) contact=(121.29, 17.71, 235.89)

PASS: Scanner completed infected-cell

PASS: Final exam starts by finding/scanning an actual infected cell before wave actions

PASS: Wrong final biological target cannot substitute an incoming virion

PASS: Real pulse/mark in timing window handles final virion 0

PASS: Real pulse/mark in timing window handles final virion 1

PASS: Real pulse/mark in timing window handles final virion 2

PASS: Real pulse/mark in timing window handles final virion 3

PASS: Final timed wave includes real shield pulses followed by four charged marks

PASS: Scanner objective expected for epitope

Contact epitope: Research target — epitope / SphereCollider layerInspector=True

Scan epitope: step=3 enabled=True lock=epitope progress=1 blocker=none aim=(119.07, 15.79, 233.31) target=(121.05, 17.54, 235.62) contact=(121.05, 17.54, 235.62)

PASS: Scanner completed epitope

PASS: Real grip selects antibody-B

PASS: Physically placed wrong antibody is rejected without advancing final exam

PASS: Real grip selects antibody-A

PASS: Complementary hand-placed antibody binds actual antigen site

PASS: BioTool objective expected for neutralized

PASS: BioTool target exists: neutralized

PASS: BioTool target contact enabled: neutralized

Tool neutralized: locked=infected-cell charge=0 radius=0,8017082 pressure=120 aim=(119.00, 16.38, 233.02) contact=(121.05, 17.54, 235.62)

PASS: BioTool completed neutralized

PASS: BioTool objective expected for t-cell

PASS: BioTool target exists: t-cell

PASS: BioTool target contact enabled: t-cell

Tool t-cell: locked=none charge=0 radius=0,8017082 pressure=120 aim=(119.04, 16.28, 232.99) contact=(121.20, 16.98, 235.38)

PASS: BioTool completed t-cell

PASS: Actual selected residual complex cleared: Residual neutralized complex 0

PASS: Actual selected residual complex cleared: Residual neutralized complex 1

PASS: Actual selected residual complex cleared: Residual neutralized complex 2

PASS: Actual final immune actions eliminate source and three residual complexes before victory

PASS: Homeostasis requires new real measurements instead of auto-completing on entry

PASS: BioTool objective expected for check-pressure

PASS: BioTool target exists: check-pressure

PASS: BioTool target contact enabled: check-pressure

Tool check-pressure: locked=check-pressure charge=0 radius=0,8017082 pressure=120 aim=(118.95, 16.27, 233.04) contact=(119.59, 15.61, 234.81)

PASS: BioTool completed check-pressure

PASS: BioTool objective expected for check-temperature

PASS: BioTool target exists: check-temperature

PASS: BioTool target contact enabled: check-temperature

Tool check-temperature: locked=check-temperature charge=0 radius=0,8017082 pressure=120 aim=(118.97, 16.32, 233.04) contact=(119.66, 16.25, 234.70)

PASS: BioTool completed check-temperature

PASS: BioTool objective expected for check-flow

PASS: BioTool target exists: check-flow

PASS: BioTool target contact enabled: check-flow

Tool check-flow: locked=check-flow charge=0 radius=0,8017082 pressure=120 aim=(118.97, 16.37, 233.03) contact=(119.73, 16.88, 234.58)

PASS: BioTool completed check-flow

PASS: Scanner objective expected for check-wall

Contact check-wall: Homeostasis sensor — check-wall / SphereCollider layerInspector=True

Scan check-wall: step=4 enabled=True lock=check-wall progress=1 blocker=none aim=(119.10, 15.68, 233.33) target=(120.17, 15.48, 234.45) contact=(120.17, 15.48, 234.45)

PASS: Scanner completed check-wall

PASS: Scanner objective expected for check-virus

Contact check-virus: Homeostasis sensor — check-virus / SphereCollider layerInspector=True

Scan check-virus: step=5 enabled=True lock=check-virus progress=1 blocker=none aim=(119.10, 15.75, 233.31) target=(120.24, 16.12, 234.33) contact=(120.24, 16.12, 234.33)

PASS: Scanner completed check-virus

PASS: BioTool objective expected for check-hemostasis

PASS: BioTool target exists: check-hemostasis

PASS: BioTool target contact enabled: check-hemostasis

Tool check-hemostasis: locked=check-hemostasis charge=0 radius=0,8017082 pressure=120 aim=(119.03, 16.36, 233.01) contact=(120.32, 16.75, 234.22)

PASS: BioTool completed check-hemostasis

PASS: Six actual final measurements produce mission result and grades

PASS: VERY FAR tracked-device excursion hides old plaque renderers

PASS: VERY FAR tracked-device excursion culls recovered wound location

PASS: Plaque re-entry restores exact treated mesh, pose and processed zones

PASS: Recovered wound re-entry retains repaired wall instance/state and no leak

PASS: VERY FAR excursions do not reset XR Origin heading

VERY FAR method: tracked HMD state excursions along existing path after final measurements; no scene reset, no mission API success. This does not verify physical-headset comfort or joystick navigation.

## Scope

Only completed PASS sequences above establish verification for this run. No debug enter/force-next/mission Accept is called by this driver. Scene05 retains ray-target parameter controls. Scanner/BioTool are controller-mounted visuals, not physically grabbable instruments. Physical Quest/Pico, thermal/FPS, pooling/difficulty overhaul and joystick comfort remain unverified. VERY FAR uses tracked HMD state excursions, not a mission reset. Scenes12–16 are verified only when their actual hand/tool/measurement sequences above pass.

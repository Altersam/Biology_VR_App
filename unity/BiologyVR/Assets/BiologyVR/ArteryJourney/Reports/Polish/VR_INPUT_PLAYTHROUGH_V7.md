# V8 Sequential XR Input Playthrough

Generated UTC: 2026-10-10T20:47:43.5412549Z

Status: FULL SIMULATOR PASS

## Scene03→16 and VERY FAR

XR Input System simulator layouts; controller states queued into actual action bindings and XRI. Not a physical headset test.

PASS: Tracked view opens main theory without the old side-panel overlay

PASS: Actual right XR thumbstick scrolls the aimed holographic theory panel

PASS: Existing Scanner/BioTool actions enabled

PASS: Actual right A selects Capture without Continue or mission success

PASS: Actual right A selects Tonus without Continue or mission success

PASS: Actual right A selects Bio without Continue or mission success

PASS: Actual right A selects Impulse without Continue or mission success

PASS: Actual right B selects previous mode without Continue: Bio

PASS: Actual right B selects previous mode without Continue: Tonus

PASS: Actual right B selects previous mode without Continue: Capture

PASS: Actual right B selects previous mode without Continue: Impulse

PASS: Tracked input displays only VR tool views

PASS: Biomedical tool views add no physical contacts

Installed device mesh triangles: Scanner=2168; BioTool=3136

Diagnostic: step=1 ready=True RBC selected=True grabbed=True runtime right grip=1 button=True deviceEnabled=True focused=False controller=(0.59, -0.12, 5.11) RBC=(0.58, -0.12, 5.24) bounds=Center: (0.58, -0.12, 5.24), Extents: (0.13, 0.13, 0.13) collider=True grab=True

RBC target enabled=True mission matches=True selecting=Near-Far Interactor

RBC eventsAttached=True

RBC grab=Research target — rbc (UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)

RBC disabling=False

RBC lastAcceptedGrabId=rbc

Interactor Gaze Interactor XRGazeInteractor enabled=False hover=False selected=False targets= selectActive=False pos=(0.00, 0.15, 0.00)

Interactor Poke Interactor XRPokeInteractor enabled=True hover=False selected=False targets= selectActive=True pos=(0.17, -0.12, 5.11)

Interactor Near-Far Interactor NearFarInteractor enabled=True hover=False selected=False targets= selectActive=False pos=(0.17, -0.12, 5.11)

Interactor Teleport Interactor XRRayInteractor enabled=False hover=False selected=False targets= selectActive=False pos=(0.17, -0.14, 5.08)

Interactor Poke Interactor XRPokeInteractor enabled=True hover=False selected=False targets= selectActive=True pos=(0.59, -0.12, 5.11)

Interactor Near-Far Interactor NearFarInteractor enabled=True hover=False selected=True targets=Research target — rbc selectActive=True pos=(0.59, -0.12, 5.11)

Interactor Teleport Interactor XRRayInteractor enabled=False hover=False selected=False targets= selectActive=False pos=(0.59, -0.14, 5.08)

Interactor Climb Teleport ClimbTeleportInteractor enabled=True hover=False selected=False targets= selectActive=False pos=(0.00, -1.21, 0.00)

Interactor Desktop mouse XRI hand JourneyDesktopInteractor enabled=True hover=False selected=False targets= selectActive=False pos=(-0.95, -0.44, 1.00)

PASS: Right grip actually selects RBC and advances Grab

PASS: Early Scanner release does not complete scan

PASS: Held real Scanner trigger completes raycast scan

PASS: Both grips select the same RBC

PASS: Separating simulated hands triggers educational enlargement

PASS: Bringing both hands together reduces scale without another objective

PASS: Releasing one hand leaves object in other hand

PASS: Scanning the wrong visible target does not complete Hb objective

PASS: RBC re-extracted from flow for Hb/heme study

PASS: Scanner objective expected for hemoglobin

Contact hemoglobin: Research target — hemoglobin / SphereCollider layerInspector=True

Scan hemoglobin: step=4 enabled=True lock=hemoglobin progress=1 blocker=none aim=(-0.17, 0.00, 0.65) target=(0.28, 0.04, 6.54) contact=(0.28, 0.04, 6.54)

PASS: Scanner completed hemoglobin

PASS: Scanner objective expected for heme

Contact heme: Research target — heme / SphereCollider layerInspector=True

Scan heme: step=5 enabled=True lock=heme progress=1 blocker=none aim=(-0.16, 0.00, 0.65) target=(0.60, 0.18, 6.49) contact=(0.60, 0.18, 6.49)

PASS: Scanner completed heme

PASS: Hb and heme actually scanned through device trigger

PASS: Physically gripping oxygen detaches it

PASS: Placing and releasing O2 near heme binds it

PASS: Scanner objective expected for co2

PASS: Actual XR grip extracts co2 from blood before scanning

Contact co2: Research target — co2 / SphereCollider layerInspector=True

Scan co2: step=8 enabled=True lock=co2 progress=1 blocker=none aim=(-0.16, 0.00, 0.65) target=(0.89, 0.51, 10.92) contact=(0.89, 0.51, 10.92)

PASS: Scanner completed co2

PASS: Extracted co2 returns smoothly into moving blood pool

PASS: WBC actually travels in blood flow before XR grip catch

PASS: Right XR grip catches travelling WBC

PASS: Scanner objective expected for leukocyte

Contact leukocyte: Research target — leukocyte / SphereCollider layerInspector=True

Scan leukocyte: step=9 enabled=True lock=leukocyte progress=1 blocker=none aim=(-0.17, -0.01, 0.65) target=(0.83, -0.44, 11.56) contact=(0.83, -0.44, 11.56)

PASS: Scanner completed leukocyte

PASS: WBC held scan changes goal to platelet without losing hold

PASS: XR release removes WBC hold without an abrupt home teleport

PASS: Released XR WBC rejoins actual pool transform and continues downstream

PASS: Scanner objective expected for platelet

PASS: Actual XR grip extracts platelet from blood before scanning

Contact platelet: Research target — platelet / SphereCollider layerInspector=True

Scan platelet: step=10 enabled=True lock=platelet progress=1 blocker=none aim=(-0.14, 0.00, 0.65) target=(2.47, 0.24, 12.02) contact=(2.47, 0.24, 12.02)

PASS: Scanner completed platelet

PASS: Extracted platelet returns smoothly into moving blood pool

PASS: Gas and formed elements scanned through actual Scanner action

PASS: BioTool comparison requires actual ray and trigger

PASS: Scanner objective expected for plasma

PASS: Actual XR grip extracts plasma from blood before scanning

Contact plasma: Research target — plasma / SphereCollider layerInspector=True

Scan plasma: step=12 enabled=True lock=plasma progress=1 blocker=none aim=(-0.14, -0.01, 0.65) target=(3.25, -0.46, 13.30) contact=(3.25, -0.46, 13.30)

PASS: Scanner completed plasma

PASS: Extracted plasma returns smoothly into moving blood pool

PASS: All Scene03 objectives completed without Accept/Primary/debug calls

PASS: Actual left Y InputAction starts completed 03→04 travel

PASS: Completed Scene03 location stays active during transition

PASS: 03→04 smooth travel arrives without debug enter/force-next

PASS: Origin and tracked head rotation preserved across travel

PASS: Same live Scanner/BioTool aim instances persist across travel

PASS: Observed nearby blood cell continues in world without travelling-anchor jump

PASS: Scanner objective expected for endothelium

Contact endothelium: Scene04_Band_endothelium / MeshCollider layerInspector=True

PASS: Scanner uses actual wall shell contact for endothelium

Scan endothelium: step=1 enabled=True lock=layers progress=1 blocker=none aim=(0.98, -0.01, 10.54) target=(1.86, -0.08, 12.87) contact=(1.86, -0.08, 12.87)

PASS: Scanner completed endothelium

PASS: Real BioTool trigger activates wall layer unfolding

PASS: Scanner objective expected for intima

Contact intima: Scene04_Band_intima / MeshCollider layerInspector=True

PASS: Scanner uses actual wall shell contact for intima

Scan intima: step=3 enabled=True lock=media progress=1 blocker=none aim=(0.99, -0.01, 10.53) target=(1.86, -0.08, 12.87) contact=(1.86, -0.08, 12.87)

PASS: Scanner completed intima

PASS: Scanner objective expected for media

Contact media: Scene04_Band_media / MeshCollider layerInspector=True

PASS: Scanner uses actual wall shell contact for media

Scan media: step=4 enabled=True lock=adventitia progress=1 blocker=none aim=(0.99, -0.01, 10.53) target=(1.86, -0.08, 12.87) contact=(1.86, -0.08, 12.87)

PASS: Scanner completed media

PASS: Scanner objective expected for adventitia

Contact adventitia: Scene04_Band_adventitia / MeshCollider layerInspector=True

PASS: Scanner uses actual wall shell contact for adventitia

Scan adventitia: step=5 enabled=True lock=none progress=1 blocker=none aim=(0.99, -0.01, 10.53) target=(1.86, -0.08, 12.87) contact=(1.86, -0.08, 12.87)

PASS: Scanner completed adventitia

PASS: All Scene04 wall shells scanned through real Scanner actions

PASS: Vitals remain on compact HUD outside optional experiment module

PASS: Real left Y continues completed Scene04

PASS: Transition 04→05 arrives through normal travel

PASS: BioTool objective expected for flow

PASS: BioTool target exists: flow

PASS: BioTool target contact enabled: flow

Tool flow: locked=flow charge=0 radius=1 pressure=120 aim=(12.50, -11.94, 39.99) contact=(11.75, -10.70, 41.58)

PASS: BioTool completed flow

PASS: BioTool objective expected for flow-model

PASS: BioTool target exists: flow-model

PASS: BioTool target contact enabled: flow-model

Tool flow-model: locked=flow-model charge=0 radius=1 pressure=120 aim=(12.54, -11.95, 40.00) contact=(12.48, -10.97, 41.51)

PASS: BioTool completed flow-model

PASS: Scene05 flow measurement and holographic model activation use BioTool trigger

PASS: BioTool objective expected for small-radius

PASS: BioTool target exists: small-radius

PASS: BioTool target contact enabled: small-radius

Tool small-radius: locked=small-radius charge=0 radius=0,65 pressure=120 aim=(12.60, -11.97, 40.01) contact=(13.20, -11.24, 41.43)

PASS: BioTool completed small-radius

PASS: BioTool objective expected for large-radius

PASS: BioTool target exists: large-radius

PASS: BioTool target contact enabled: large-radius

Tool large-radius: locked=large-radius charge=0 radius=1,35 pressure=120 aim=(12.47, -12.00, 40.02) contact=(11.49, -11.48, 41.87)

PASS: BioTool completed large-radius

PASS: BioTool objective expected for pressure

PASS: BioTool target exists: pressure

PASS: BioTool target contact enabled: pressure

Tool pressure: locked=pressure charge=0 radius=1,35 pressure=155 aim=(12.52, -12.01, 40.04) contact=(12.22, -11.75, 41.79)

PASS: BioTool completed pressure

PASS: BioTool objective expected for normal-flow

PASS: BioTool target exists: normal-flow

PASS: BioTool target contact enabled: normal-flow

Tool normal-flow: locked=normal-flow charge=0 radius=1 pressure=120 aim=(12.58, -12.03, 40.04) contact=(12.95, -12.02, 41.72)

PASS: BioTool completed normal-flow

PASS: Scene05 radius/pressure experiment completes and returns to normal flow

PASS: Real left Y continues completed Scene05

PASS: Transition 05→06 arrives through normal travel

PASS: BioTool objective expected for plaque-flow

PASS: BioTool target exists: plaque-flow

PASS: BioTool target contact enabled: plaque-flow

Tool plaque-flow: locked=plaque charge=0 radius=1 pressure=120 aim=(37.61, 8.95, 62.98) contact=(37.96, 9.17, 64.19)

PASS: BioTool completed plaque-flow

PASS: Scanner objective expected for plaque

Contact plaque: Actual growing plaque diagnosis contact / BoxCollider layerInspector=True

Scan plaque: step=2 enabled=True lock=none progress=1 blocker=none aim=(37.52, 8.96, 63.39) target=(41.78, 7.73, 64.43) contact=(41.07, 9.96, 66.17)

PASS: Scanner completed plaque

PASS: Scene06 plaque formation completes before pulse mode

PASS: BioTool objective expected for pulse-mode

PASS: BioTool target exists: pulse-mode

PASS: BioTool target contact enabled: pulse-mode

Tool pulse-mode: locked=none charge=0 radius=1 pressure=120 aim=(37.64, 8.92, 62.97) contact=(38.19, 8.88, 64.05)

PASS: BioTool completed pulse-mode

PASS: Scene06 plaque inspection and pulse mode use real instruments

Plaque XR diagnostic: before=2 destroyed=45 accepted=45 elapsed=9,941557 lock=none obstruction=none

PASS: Actual right trigger fragments the original pile plus newly arriving lipids before occlusion

PASS: Distinct real lipid contacts are fully treated through BioTool input

PASS: Real left Y continues completed Scene06

PASS: Transition 06→07 arrives through normal travel

PASS: Completed plaque retains root, treatment state, mesh instance/vertices and pose across 06→07

PASS: Completed plaque retains restored wall and dormant lipid contacts

PASS: Head/origin rotation and instrument instances persist across 06→07

PASS: Moving scanner objective expected for embolus

Tracked scan embolus: locked=none progress=0

PASS: Scanner completed moving target embolus

PASS: BioTool objective expected for attract-mode

PASS: BioTool target exists: attract-mode

PASS: BioTool target contact enabled: attract-mode

Tool attract-mode: locked=none charge=0 radius=1 pressure=120 aim=(23.36, 2.85, 93.43) contact=(22.06, 2.71, 94.17)

PASS: BioTool completed attract-mode

PASS: Releasing BioTool before charge finishes does not capture embolus

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(23.43, 2.85, 93.51) target=(21.51, 2.37, 96.71)

PASS: BioTool captured moving target embolus

PASS: Held BioTool trigger captures moving embolus after charge

PASS: Capture outside trap does not auto-deposit

PASS: Releasing trigger breaks attraction and retries only capture goal

PASS: Release preserves nearby bubble pose and completed scanning

PASS: Released embolus drifts again with the flow

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(22.89, 2.69, 93.58) target=(21.85, 2.45, 96.16)

PASS: BioTool captured moving target embolus

PASS: Released embolus can be re-captured through trigger input

Transport result: bubble=(20.79, 2.93, 97.93) trap=(22.33, 1.92, 94.66) stability=1

PASS: Only sustained in-trap placement completes geometric deposit

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(22.88, 2.71, 93.58) target=(20.72, 2.90, 98.05)

PASS: BioTool captured moving target embolus

Transport result: bubble=(19.72, 1.97, 98.79) trap=(22.33, 1.92, 94.66) stability=1

PASS: Only sustained in-trap placement completes geometric deposit

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(22.86, 2.68, 93.57) target=(19.65, 1.94, 98.93)

PASS: BioTool captured moving target embolus

Transport result: bubble=(20.17, 2.01, 100.87) trap=(22.33, 1.92, 94.66) stability=1

PASS: Only sustained in-trap placement completes geometric deposit

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(22.89, 2.69, 93.58) target=(20.09, 1.99, 101.02)

PASS: BioTool captured moving target embolus

Transport result: bubble=(18.24, 2.62, 102.22) trap=(22.33, 1.92, 94.66) stability=1

PASS: Only sustained in-trap placement completes geometric deposit

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(22.87, 2.70, 93.57) target=(18.18, 2.62, 102.40)

PASS: BioTool captured moving target embolus

Transport result: bubble=(18.29, 1.11, 104.63) trap=(22.33, 1.92, 94.66) stability=1

PASS: Only sustained in-trap placement completes geometric deposit

PASS: Moving BioTool capture objective expected for embolus

Tracked capture embolus: locked=embolus charge=1 blocker=none input enabled=True aim=(22.89, 2.68, 93.58) target=(18.29, 1.15, 104.82)

PASS: BioTool captured moving target embolus

Transport result: bubble=(22.32, 1.91, 94.68) trap=(22.33, 1.92, 94.66) stability=1

PASS: Only sustained in-trap placement completes geometric deposit

PASS: Held field physically captures and deposits every gas embolus inside trap

PASS: Real left Y continues completed Scene07

PASS: Transition 07→08 arrives through normal travel

PASS: Scanner objective expected for virus-study

Contact virus-study: Research target — virus-study / SphereCollider layerInspector=True

Scan virus-study: step=1 enabled=True lock=virus-study progress=1 blocker=none aim=(41.78, 13.92, 124.25) target=(43.45, 14.36, 124.96) contact=(43.45, 14.36, 124.96)

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

Scan genome: step=3 enabled=True lock=capsid progress=1 blocker=none aim=(42.48, 14.30, 124.34) target=(43.51, 14.38, 125.00) contact=(43.51, 14.38, 125.00)

PASS: Scanner completed genome

PASS: Scanner objective expected for capsid

Contact capsid: Research target — capsid / SphereCollider layerInspector=True

Scan capsid: step=4 enabled=True lock=none progress=1 blocker=none aim=(42.48, 14.29, 124.34) target=(43.45, 14.36, 124.96) contact=(43.45, 14.36, 124.96)

PASS: Scanner completed capsid

PASS: Scanner objective expected for epitope

Contact epitope: Research target — epitope / SphereCollider layerInspector=True

Scan epitope: step=5 enabled=True lock=none progress=1 blocker=none aim=(42.47, 14.33, 124.33) target=(43.64, 14.19, 125.05) contact=(43.64, 14.19, 125.05)

PASS: Scanner completed epitope

PASS: All Scene08 parts scanned without tool enlargement/debug completion

PASS: Real left Y continues completed Scene08

PASS: Same-site transition 08→09 arrives through normal travel

PASS: BioTool objective expected for immune-mode

PASS: BioTool target exists: immune-mode

PASS: BioTool target contact enabled: immune-mode

Tool immune-mode: locked=immune-mode charge=0 radius=1 pressure=120 aim=(42.42, 13.93, 124.39) contact=(42.86, 14.86, 124.98)

PASS: BioTool completed immune-mode

PASS: Charging BioTool at the phagocyte cannot mark an unrelated incoming virion

PASS: Rapid immune assault starts with a six-target burst

PASS: Incoming virion has active target contact: 0

PASS: Rapid BioTool hit marks the aimed moving virion: 0

PASS: Incoming virion has active target contact: 1

PASS: Rapid BioTool hit marks the aimed moving virion: 1

PASS: Incoming virion has active target contact: 2

PASS: Rapid BioTool hit marks the aimed moving virion: 2

PASS: Incoming virion has active target contact: 3

PASS: Rapid BioTool hit marks the aimed moving virion: 3

PASS: Incoming virion has active target contact: 4

PASS: Rapid BioTool hit marks the aimed moving virion: 4

PASS: Incoming virion has active target contact: 5

PASS: Rapid BioTool hit marks the aimed moving virion: 5

PASS: Incoming virion has active target contact: 6

PASS: Rapid BioTool hit marks the aimed moving virion: 6

PASS: Incoming virion has active target contact: 7

PASS: Rapid BioTool hit marks the aimed moving virion: 7

PASS: Incoming virion has active target contact: 8

PASS: Rapid BioTool hit marks the aimed moving virion: 8

PASS: Incoming virion has active target contact: 9

PASS: Rapid BioTool hit marks the aimed moving virion: 9

PASS: Incoming virion has active target contact: 10

PASS: Rapid BioTool hit marks the aimed moving virion: 10

PASS: Incoming virion has active target contact: 11

PASS: Rapid BioTool hit marks the aimed moving virion: 11

PASS: Incoming virion has active target contact: 12

PASS: Rapid BioTool hit marks the aimed moving virion: 12

PASS: Incoming virion has active target contact: 13

PASS: Rapid BioTool hit marks the aimed moving virion: 13

PASS: Incoming virion has active target contact: 14

PASS: Rapid BioTool hit marks the aimed moving virion: 14

PASS: Incoming virion has active target contact: 15

PASS: Rapid BioTool hit marks the aimed moving virion: 15

PASS: Incoming virion has active target contact: 16

PASS: Rapid BioTool hit marks the aimed moving virion: 16

PASS: Incoming virion has active target contact: 17

PASS: Rapid BioTool hit marks the aimed moving virion: 17

PASS: Incoming virion has active target contact: 18

PASS: Rapid BioTool hit marks the aimed moving virion: 18

PASS: Incoming virion has active target contact: 19

PASS: Rapid BioTool hit marks the aimed moving virion: 19

PASS: Incoming virion has active target contact: 20

PASS: Rapid BioTool hit marks the aimed moving virion: 20

PASS: Incoming virion has active target contact: 21

PASS: Rapid BioTool hit marks the aimed moving virion: 21

PASS: Incoming virion has active target contact: 22

PASS: Rapid BioTool hit marks the aimed moving virion: 22

PASS: Incoming virion has active target contact: 23

PASS: Rapid BioTool hit marks the aimed moving virion: 23

PASS: Twenty-four individual incoming virions hit through actual rapid-fire trigger input

PASS: BioTool objective expected for phagocyte

PASS: BioTool target exists: phagocyte

PASS: BioTool target contact enabled: phagocyte

Tool phagocyte: locked=phagocyte charge=0 radius=1 pressure=120 aim=(42.44, 13.89, 124.41) contact=(42.96, 14.49, 125.08)

PASS: BioTool completed phagocyte

PASS: Scene09 handoff completes only after all twenty-four actual phagocyte ingestions

PASS: Scene09 phagocyte stays in its current episode instead of moving to the future wound

PASS: Real left Y continues completed Scene09

PASS: Transition 09→10 arrives through normal travel

PASS: BioTool objective expected for pressure-low

PASS: BioTool target exists: pressure-low

PASS: BioTool target contact enabled: pressure-low

Tool pressure-low: locked=pressure-low charge=0 radius=1 pressure=120 aim=(68.28, 3.58, 138.54) contact=(68.81, 3.02, 139.57)

PASS: BioTool completed pressure-low

PASS: BioTool objective expected for tone-mode

PASS: BioTool target exists: tone-mode

PASS: BioTool target contact enabled: tone-mode

Tool tone-mode: locked=tone-mode charge=0 radius=1 pressure=120 aim=(68.19, 3.61, 138.58) contact=(68.03, 3.34, 139.70)

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

Tool pressure-stable: locked=pressure-stable charge=0 radius=0,8003413 pressure=120 aim=(67.74, 4.04, 138.50) contact=(68.76, 3.89, 139.76)

PASS: BioTool completed pressure-stable

PASS: Early repeat measurement cannot complete the pressure-drop goal

PASS: BioTool objective expected for pressure-return

PASS: BioTool target exists: pressure-return

PASS: BioTool target contact enabled: pressure-return

Tool pressure-return: locked=pressure-return charge=0 radius=0,8003413 pressure=120 aim=(67.66, 4.08, 138.54) contact=(67.94, 4.23, 139.90)

PASS: BioTool completed pressure-return

PASS: Repeated real measurement detects delayed pressure fall despite vascular tone

PASS: Real left Y continues completed Scene10

PASS: Transition 10→11 arrives through normal travel

PASS: Treated tone root and shader radius persist on entering wound diagnosis

PASS: 10→11 preserves head/origin rotation and original instrument instances

PASS: Scene11 starts with actual pooled outward blood leakage

PASS: Scanner objective expected for wound

Contact wound: Research target — wound / SphereCollider layerInspector=True

Scan wound: step=1 enabled=True lock=leak progress=1 blocker=none aim=(91.29, 1.86, 177.62) target=(91.35, 2.99, 183.40) contact=(91.35, 2.99, 183.40)

PASS: Scanner completed wound

PASS: BioTool objective expected for leak

PASS: BioTool target exists: leak

PASS: BioTool target contact enabled: leak

Tool leak: locked=none charge=0 radius=0,8003413 pressure=120 aim=(90.85, 2.04, 177.70) contact=(91.35, 2.99, 183.40)

PASS: BioTool completed leak

PASS: BioTool objective expected for pressure-low

PASS: BioTool target exists: pressure-low

PASS: BioTool target contact enabled: pressure-low

Tool pressure-low: locked=pressure-low charge=0 radius=0,8003413 pressure=120 aim=(90.96, 2.06, 177.65) contact=(91.66, 2.36, 178.39)

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

Tool platelet: locked=platelet charge=0 radius=0,8003413 pressure=120 aim=(90.81, 2.17, 177.60) contact=(91.70, 2.91, 183.55)

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

Scan thrombin: step=4 enabled=True lock=thrombin progress=1 blocker=none aim=(91.28, 2.01, 177.60) target=(91.69, 1.87, 182.99) contact=(91.69, 1.87, 182.99)

PASS: Scanner completed thrombin

PASS: BioTool objective expected for thrombin

PASS: BioTool target exists: thrombin

PASS: BioTool target contact enabled: thrombin

Tool thrombin: locked=thrombin charge=0 radius=0,8003413 pressure=120 aim=(90.82, 2.14, 177.60) contact=(91.69, 1.87, 182.99)

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

Scan clot: step=1 enabled=True lock=platelet progress=1 blocker=none aim=(91.27, 2.04, 177.60) target=(91.35, 2.99, 183.40) contact=(91.35, 2.99, 183.40)

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

Tool open-flow: locked=open-flow charge=0 radius=0,8003413 pressure=120 aim=(90.93, 2.19, 177.53) contact=(92.28, 2.53, 178.41)

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

Tool cleanup-mode: locked=cleanup-mode charge=0 radius=0,8003413 pressure=120 aim=(90.81, 2.15, 177.60) contact=(91.43, 2.01, 182.64)

PASS: BioTool completed cleanup-mode

PASS: BioTool assists actual phagocytosis of Cellular debris 0

PASS: BioTool assists actual phagocytosis of Cellular debris 1

PASS: BioTool assists actual phagocytosis of Cellular debris 2

PASS: Three selected debris physically engulfed before progressing to fibrinolysis

PASS: Scanner objective expected for plasmin

Contact plasmin: Recovery factor — plasmin / SphereCollider layerInspector=True

Scan plasmin: step=4 enabled=True lock=plasmin progress=1 blocker=none aim=(91.29, 2.01, 177.60) target=(92.01, 1.93, 183.32) contact=(92.01, 1.93, 183.32)

PASS: Scanner completed plasmin

PASS: BioTool objective expected for plasmin

PASS: BioTool target exists: plasmin

PASS: BioTool target contact enabled: plasmin

Tool plasmin: locked=plasmin charge=0 radius=0,8003413 pressure=120 aim=(90.82, 2.15, 177.60) contact=(92.01, 1.93, 183.32)

PASS: BioTool completed plasmin

PASS: Player-activated plasmin progressively removes the existing fibrin network

PASS: BioTool objective expected for repair

PASS: BioTool target exists: repair

PASS: BioTool target contact enabled: repair

Tool repair: locked=healed-wall charge=0 radius=0,8003413 pressure=120 aim=(90.80, 2.18, 177.61) contact=(91.35, 2.99, 183.40)

PASS: BioTool completed repair

PASS: Early Scanner cannot claim fully restored endothelium during repair

PASS: Scanner objective expected for healed-wall

Contact healed-wall: Research target — healed-wall / SphereCollider layerInspector=True

Scan healed-wall: step=7 enabled=True lock=none progress=1 blocker=none aim=(91.27, 2.04, 177.60) target=(91.35, 2.99, 183.40) contact=(91.35, 2.99, 183.40)

PASS: Scanner completed healed-wall

PASS: Actual recovery process closes and scans the same original wound

PASS: Final-focus travel preserves repaired original wound and wall instance

PASS: 14→15 preserves origin/gaze and original tools

PASS: Scanner objective expected for infected-cell

Contact infected-cell: Research target — infected-cell / SphereCollider layerInspector=True

Scan infected-cell: step=1 enabled=True lock=infected-cell progress=1 blocker=none aim=(119.39, 16.05, 233.54) target=(121.29, 17.71, 235.89) contact=(121.29, 17.71, 235.89)

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

Scan epitope: step=3 enabled=True lock=epitope progress=1 blocker=none aim=(119.39, 16.05, 233.54) target=(121.05, 17.54, 235.62) contact=(121.05, 17.54, 235.62)

PASS: Scanner completed epitope

PASS: Real grip selects antibody-B

PASS: Physically placed wrong antibody is rejected without advancing final exam

PASS: Real grip selects antibody-A

PASS: Complementary hand-placed antibody binds actual antigen site

PASS: BioTool objective expected for neutralized

PASS: BioTool target exists: neutralized

PASS: BioTool target contact enabled: neutralized

Tool neutralized: locked=infected-cell charge=0 radius=0,8003413 pressure=120 aim=(119.34, 16.47, 233.33) contact=(121.05, 17.54, 235.62)

PASS: BioTool completed neutralized

PASS: BioTool objective expected for t-cell

PASS: BioTool target exists: t-cell

PASS: BioTool target contact enabled: t-cell

Tool t-cell: locked=none charge=0 radius=0,8003413 pressure=120 aim=(119.38, 16.35, 233.30) contact=(121.21, 16.99, 235.39)

PASS: BioTool completed t-cell

PASS: Actual selected residual complex cleared: Residual neutralized complex 0

PASS: Actual selected residual complex cleared: Residual neutralized complex 1

PASS: Actual selected residual complex cleared: Residual neutralized complex 2

PASS: Actual final immune actions eliminate source and three residual complexes before victory

PASS: Homeostasis requires new real measurements instead of auto-completing on entry

PASS: BioTool objective expected for check-pressure

PASS: BioTool target exists: check-pressure

PASS: BioTool target contact enabled: check-pressure

Tool check-pressure: locked=check-pressure charge=0 radius=0,8003413 pressure=120 aim=(119.27, 16.34, 233.34) contact=(119.59, 15.61, 234.81)

PASS: BioTool completed check-pressure

PASS: BioTool objective expected for check-temperature

PASS: BioTool target exists: check-temperature

PASS: BioTool target contact enabled: check-temperature

Tool check-temperature: locked=check-temperature charge=0 radius=0,8003413 pressure=120 aim=(119.29, 16.39, 233.36) contact=(119.66, 16.25, 234.70)

PASS: BioTool completed check-temperature

PASS: BioTool objective expected for check-flow

PASS: BioTool target exists: check-flow

PASS: BioTool target contact enabled: check-flow

Tool check-flow: locked=check-flow charge=0 radius=0,8003413 pressure=120 aim=(119.29, 16.46, 233.35) contact=(119.73, 16.88, 234.58)

PASS: BioTool completed check-flow

PASS: Scanner objective expected for check-wall

Contact check-wall: Homeostasis sensor — check-wall / SphereCollider layerInspector=True

Scan check-wall: step=4 enabled=True lock=check-wall progress=1 blocker=none aim=(119.40, 15.91, 233.55) target=(120.17, 15.48, 234.45) contact=(120.17, 15.48, 234.45)

PASS: Scanner completed check-wall

PASS: Scanner objective expected for check-virus

Contact check-virus: Homeostasis sensor — check-virus / SphereCollider layerInspector=True

Scan check-virus: step=5 enabled=True lock=check-virus progress=1 blocker=none aim=(119.42, 15.98, 233.54) target=(120.24, 16.12, 234.33) contact=(120.24, 16.12, 234.33)

PASS: Scanner completed check-virus

PASS: BioTool objective expected for check-hemostasis

PASS: BioTool target exists: check-hemostasis

PASS: BioTool target contact enabled: check-hemostasis

Tool check-hemostasis: locked=check-hemostasis charge=0 radius=0,8003413 pressure=120 aim=(119.37, 16.45, 233.32) contact=(120.32, 16.75, 234.22)

PASS: BioTool completed check-hemostasis

PASS: Six actual final measurements produce mission result and grades

PASS: VERY FAR tracked-device excursion hides old plaque renderers

PASS: VERY FAR tracked-device excursion culls recovered wound location

PASS: Plaque re-entry restores exact treated mesh, pose and treatment state

PASS: Recovered wound re-entry retains repaired wall instance/state and no leak

PASS: VERY FAR excursions do not reset XR Origin heading

VERY FAR method: tracked HMD state excursions along existing path after final measurements; no scene reset, no mission API success. This does not verify physical-headset comfort or joystick navigation.

## Scope

Only completed PASS sequences above establish verification for this run. No debug enter/force-next/mission Accept is called by this driver. Scene05 retains ray-target parameter controls. Scanner/BioTool are controller-mounted visuals, not physically grabbable instruments. Physical Quest/Pico, thermal/FPS, pooling/difficulty overhaul and joystick comfort remain unverified. VERY FAR uses tracked HMD state excursions, not a mission reset. Scenes12–16 are verified only when their actual hand/tool/measurement sequences above pass.

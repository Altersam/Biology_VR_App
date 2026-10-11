# XR controller lifecycle

UTC: 2026-10-11T13:32:44.3218172Z

Status: PASS

XR Input System simulator layouts; controller states queued into actual action bindings and XRI. Not a physical headset test.

PASS: Both XR aiming guides persist while triggers are released

PASS: Actual A switches mode with an explicit animation: Capture

PASS: Working crown changes pose during mode selection: Capture

PASS: Trigger animates selected mode even without an eligible target: Capture

PASS: BioTool aiming guide persists after release: Capture

PASS: Actual A switches mode with an explicit animation: Tonus

PASS: Working crown changes pose during mode selection: Tonus

PASS: Trigger animates selected mode even without an eligible target: Tonus

PASS: Area field remains visible on a miss: Tonus

PASS: BioTool aiming guide persists after release: Tonus

PASS: Actual A switches mode with an explicit animation: Bio

PASS: Working crown changes pose during mode selection: Bio

PASS: Trigger animates selected mode even without an eligible target: Bio

PASS: Area field remains visible on a miss: Bio

PASS: BioTool aiming guide persists after release: Bio

PASS: Actual A switches mode with an explicit animation: Impulse

PASS: Working crown changes pose during mode selection: Impulse

PASS: Trigger animates selected mode even without an eligible target: Impulse

PASS: BioTool aiming guide persists after release: Impulse

PASS: Scanner trigger starts animation and beam even before target lock

PASS: Idle UI hover does not disable the input component

PASS: UI consumes only its own pressed trigger while keeping input and guides alive

PASS: Scanner hand continues working while BioTool hand interacts with UI

PASS: Dragging the held UI trigger off-panel does not fire a biological action

PASS: Release and re-press restores tool immediately after UI interaction

PASS: Input enable cycle restores live trigger polling without stale hold state

PASS: Controller visual/UI checks never complete an unrelated biological goal

PASS: Controller lifecycle input regression complete
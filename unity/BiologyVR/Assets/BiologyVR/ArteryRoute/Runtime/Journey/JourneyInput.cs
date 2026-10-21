using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>
    /// Callback-driven scanner/BioTool input with the existing desktop and HUD APIs.
    /// The scene's InputActionManager owns action enablement. Disabling this component
    /// unsubscribes without disabling actions that are shared with XRI/UI.
    /// </summary>
    public sealed class JourneyInput : MonoBehaviour
    {
        public JourneyMission mission;
        public JourneyWorld world;
        public JourneyMover mover;
        public Transform scanner, bioTool;

        [Tooltip("Scanner Button action. Main binds an InputActionReference from its action asset.")]
        public InputActionReference scanAction;
        [Tooltip("BioTool Button action. Must be a different action from scanAction.")]
        public InputActionReference toolAction;

        [Tooltip("Target collider layers only. Main binds the JourneyTarget layer; zero selects nothing.")]
        public LayerMask targetMask;
        [Min(.01f)] public float maxAimDistance = 35f;
        [Min(0f)] public float sphereAssistRadius = .045f;
        public bool useSphereAssist = true;

        [Tooltip("Enable together with JourneyTarget.hoverEmission for optional hover feedback.")]
        public bool hoverFeedback;
        [Header("Instrument timing")]
        [Min(.1f)] public float scanDuration=.75f;
        [Min(.1f)] public float impulseChargeDuration=.24f;
        [Tooltip("Use assigned aim transforms in automated controller emulation as well as XR.")]
        public bool useExplicitAimSources;
        public float ScanProgress{get;private set;}
        public float ChargeProgress{get;private set;}
        public JourneyTarget LockedTarget{get;private set;}
        public Collider AimObstruction{get;private set;}
        public Collider LockedContact{get;private set;}
        public JourneyPlateletCapture plateletCapture;
        public bool AdjustingTone{get;private set;}
        public float ToneStability{get;private set;}
        Vector3 toneHandStart,toneAxis;
        float toneRadiusStart,tonePreviousRadius;
        bool toneLimitWarned;
        JourneyTarget scanTarget,chargeTarget;
        bool scanConsumed,toolConsumed,previousPhysicalTool;
        bool desktopFieldActive;
        public float DesktopFieldDistance{get;private set;}=JourneyWorld.EmbolusHoldDistance;
        JourneyToolFeedback instrumentFeedback;
        JourneyDesktopControls desktopControls;
        JourneyUiInputGuard uiGuard;
        float desktopToneOffset;
        public JourneyDesktopControls DesktopControls=>desktopControls;
        public bool ScannerHeld{get;private set;}
        public bool ToolHeld{get;private set;}
        public bool ScannerBlockedByUi{get;private set;}
        public bool ToolBlockedByUi{get;private set;}
        public bool RawScannerPressed=>ReadButton(boundScanAction,XRNode.LeftHand)||(Keyboard.current!=null&&Keyboard.current.fKey.isPressed);
        public bool RawToolPressed=>ReadButton(boundToolAction,XRNode.RightHand)||(Keyboard.current!=null&&Keyboard.current.eKey.isPressed);
        BioToolModeController bioModes;
        float nextAssaultShot;
        float nextPlaqueShot;
        float deepScanAge;
        Transform leftHand,rightHand;
        public const float AssaultFireInterval=.18f;
        void Awake()
        {
            if(!Application.isMobilePlatform){desktopControls=GetComponent<JourneyDesktopControls>();if(!desktopControls)desktopControls=gameObject.AddComponent<JourneyDesktopControls>();desktopControls.input=this;}
        }

        const int RaycastCapacity = 64;
        const float ObstructionEpsilon = .002f;
        readonly RaycastHit[] targetHits = new RaycastHit[RaycastCapacity];
        readonly RaycastHit[] assistHits = new RaycastHit[RaycastCapacity];
        readonly RaycastHit[] obstructionHits = new RaycastHit[RaycastCapacity];
        readonly RaycastHit[] guideHits = new RaycastHit[RaycastCapacity];

        InputAction boundScanAction;
        InputAction boundToolAction;
        JourneyTarget hoveredTarget;
        bool pendingScan, pendingTool;
        bool scanHeld, toolHeld;

        void OnEnable()
        {
            BindActions();
        }

        void OnDisable()
        {
            UnbindActions();
            SetHovered(null);
            CancelInstrumentProgress();
        }

        void OnDestroy()
        {
            UnbindActions();
        }

        void Update()
        {
            // Main may populate references after AddComponent or replace its runtime asset.
            EnsureActionBindings();

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.gKey.wasPressedThisFrame&&!world.freeTools)
                {
                    if(desktopControls&&desktopControls.ReleaseMouseGrab())return;
                    if(world!=null)
                    {
                        JourneyTarget held=null;foreach(var item in world.targets)if(item&&item.DesktopHeld){held=item;break;}
                        // Release the actual held object even after scanning changed
                        // the current goal to another species. Never strand the old WBC.
                        if(held)held.KeyboardGrab();
                        else
                        {
                            var target=Aim(AimSource(bioTool));
                            if(target&&target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>())target.KeyboardGrab();
                            else mission.FeedbackMessage("Наведи указатель на объект: ЛКМ удерживать — взять/перенести, колесо — глубина. G — взять/отпустить клетку.");
                        }
                    }
                }
                if (keyboard.zKey.wasPressedThisFrame)
                {
                    var goal = CurrentGoal();
                    if(mission.FreeBloodResearch){var target=Aim(AimSource(bioTool));if(!target&&world.research.Focused)target=world.research.Focused.target;if(target)target.Enlarge();}
                    else if (world != null) world.Find(goal != null ? goal.target : "rbc")?.Enlarge();
                }
                if (keyboard.qKey.wasPressedThisFrame && mission != null)
                {
                    if (mission.SceneNumber == 13) mission.SetBalance(.18f);
                    else mission.SetRadius(.65f);
                }
                if (keyboard.rKey.wasPressedThisFrame && mission != null)
                {
                    if (mission.SceneNumber == 13) mission.SetBalance(.95f);
                    else mission.SetRadius(1.35f);
                }
            }

            if (hoverFeedback)
            {
                var target = Aim(AimSource(scanner));
                if (!target) target = Aim(AimSource(bioTool));
                SetHovered(target);
            }
            else SetHovered(null);
        }

        void LateUpdate()
        {
            // InputSystem callbacks may run before MonoBehaviour.Update. Queue them
            // until the existing JourneyUiInputGuard has had a chance to disable us.
            // OnDisable clears the queue; public HUD methods remain callable while disabled.
            pendingScan = pendingTool = false;
            if (!isActiveAndEnabled) return;
            if(!world.freeTools&&world.bloodEntities&&world.bloodEntities.Active&&(useExplicitAimSources||mover.UsingTrackedInput||XRSettings.isDeviceActive))
            {
                if(!leftHand||!rightHand)foreach(var hand in mover.origin.GetComponentsInChildren<Transform>(true)){if(hand.name=="Left Controller")leftHand=hand;else if(hand.name=="Right Controller")rightHand=hand;}
                int before=world.bloodEntities.Acquisitions;
                // Register the bounded near-hover proxy before XRI's grip edge.
                // Proximity alone does not begin or complete journal research.
                if(leftHand)world.bloodEntities.PromoteNear(leftHand.position);
                if(rightHand)world.bloodEntities.PromoteNear(rightHand.position);
                if(world.bloodEntities.Acquisitions!=before)Physics.SyncTransforms();
            }
            // Poll live actions as well as subscribing: UI/focus cancellations or
            // enable cycles must not leave a cached held/released state stranded.
            if(!uiGuard){var hud=GetComponent<JourneyHud>();if(hud)uiGuard=hud.inputGuard;}
            if(uiGuard)uiGuard.RefreshBlocking();
            bool scan=RawScannerPressed&&!ScannerBlockedByUi;
            bool tool=RawToolPressed&&!ToolBlockedByUi;
            UpdateHeldInstruments(Time.deltaTime,scan,tool);
        }
        static bool ReadButton(InputAction action,XRNode node)
        {
            if(action!=null&&action.enabled&&action.IsPressed())return true;
            if(!XRSettings.isDeviceActive)return false;var device=InputDevices.GetDeviceAtXRNode(node);if(!device.isValid)return false;
            if(device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out bool pressed)&&pressed)return true;
            return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger,out float value)&&value>=.5f;
        }
        public void SetUiBlocking(bool scannerBlocked,bool toolBlocked)
        {
            if(scannerBlocked&&!ScannerBlockedByUi){ScanProgress=0;scanTarget=null;scanConsumed=false;ScannerHeld=false;}
            if(toolBlocked&&!ToolBlockedByUi){ChargeProgress=0;chargeTarget=null;toolConsumed=false;ToolHeld=false;AdjustingTone=false;ToneStability=0;if(world&&world.EmbolusCaptured)world.ReleaseEmbolus();if(world&&world.freeTools)world.freeTools.Release();if(world&&world.immunity)world.immunity.CancelHold();if(plateletCapture)plateletCapture.Release();}
            ScannerBlockedByUi=scannerBlocked;ToolBlockedByUi=toolBlocked;
        }
        public Vector3 GuideEndpoint(Transform source,out bool targetContact)
        {
            targetContact=false;if(!source)return Vector3.zero;var ray=new Ray(source.position,source.forward);float distance=Mathf.Min(maxAimDistance,12f);
            int count=Physics.RaycastNonAlloc(ray,guideHits,distance,~0,QueryTriggerInteraction.Collide);SortByDistance(guideHits,count);
            for(int i=0;i<count;i++)
            {
                var collider=guideHits[i].collider;if(!collider||mover&&mover.origin&&collider.transform.IsChildOf(mover.origin.transform))continue;
                var target=TargetFrom(collider);bool physical=collider.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                if(TargetingTissue&&world.bloodEntities.Owns(target))continue;
                if(collider.isTrigger&&!IsUsableTarget(target)&&!physical)continue;
                targetContact=IsUsableTarget(target)||physical;return guideHits[i].point;
            }
            return ray.GetPoint(distance);
        }
        public void CancelInstrumentProgress()
        {ScannerHeld=ToolHeld=false;ScanProgress=ChargeProgress=0;scanTarget=chargeTarget=LockedTarget=null;LockedContact=null;scanConsumed=toolConsumed=previousPhysicalTool=desktopFieldActive=false;AdjustingTone=false;ToneStability=0;if(world)world.ReleaseEmbolus();if(world&&world.freeTools)world.freeTools.Release();if(world&&world.immunity)world.immunity.CancelHold();if(plateletCapture)plateletCapture.Release();}

        /// <summary>Shared controller/keyboard hold path. Uses real scene raycasts; no focused-target fallback.</summary>
        public void UpdateHeldInstruments(float deltaTime,bool scanning,bool firing)
        {
            ScannerHeld=scanning;ToolHeld=firing;
            if(!bioModes)bioModes=GetComponent<BioToolModeController>();
            if(!instrumentFeedback)instrumentFeedback=GetComponent<JourneyToolFeedback>();
            if(!scanning){ScanProgress=0;scanTarget=null;scanConsumed=false;}
            if(!firing){ChargeProgress=0;chargeTarget=null;toolConsumed=false;}
            if(!mission||mission.Goals==null||!mission.Ready){ScanProgress=ChargeProgress=0;previousPhysicalTool=firing;return;}
            var goal=CurrentGoal();
            var source=AimSource(scanning?scanner:bioTool);
            var hit=Aim(source);LockedTarget=hit;
            if(world.immunePatrol&&world.immunePatrol.Handle(deltaTime,scanning,firing,hit,AimSource(bioTool),bioModes.CurrentMode)){previousPhysicalTool=firing;return;}
            if(!scanning&&world.plateletLaunch&&mission.SceneNumber==13&&bioModes.CurrentMode==BioToolMode.Capture&&plateletCapture&&plateletCapture.UpdateField(deltaTime,firing,AimSource(bioTool),LockedContact,desktopControls&&desktopControls.Active)){previousPhysicalTool=firing;return;}
            if(world.bloodEntities&&hit)world.bloodEntities.Touch(hit);
            if(world.freeTools&&world.freeTools.Handle(deltaTime,scanning,firing,hit,AimSource(bioTool),desktopControls&&desktopControls.Active,bioModes.CurrentMode)){ChargeProgress=world.freeTools.ReverseCharge;previousPhysicalTool=firing;return;}
            if(world.freeTools&&world.freeTools.ScanFlow(deltaTime,scanning,hit)){previousPhysicalTool=firing;return;}
            if(world.immunity&&world.immunity.HandleInput(deltaTime,scanning,firing&&bioModes.CurrentMode==BioToolMode.Capture,hit,AimSource(bioTool),desktopControls&&desktopControls.Active))
            {ScanProgress=world.immunity.ScanProgress;ChargeProgress=world.immunity.ChargeProgress;if(instrumentFeedback)instrumentFeedback.PresentLock(hit,ScanProgress,hit,scanning,source);previousPhysicalTool=firing;return;}
            if(mission.FreeBloodResearch)
            {
                bool correct=world.research.CanInvestigate(hit);
                if(correct)world.research.SelectInstance(hit);
                if(!scanning)deepScanAge=0;
                if(!world.freeTools&&scanning&&scanConsumed&&correct&&hit.targetId=="rbc"&&world.analysis&&!world.analysis.Opened){deepScanAge+=deltaTime;if(deepScanAge>.65f)world.analysis.Open(hit);}
                if(scanning&&scanConsumed&&correct&&hit!=scanTarget){scanConsumed=false;ScanProgress=0;scanTarget=null;}
                if(scanning&&!scanConsumed)
                {
                    if(!correct){ScanProgress=0;scanTarget=null;}
                    else
                    {
                        if(scanTarget!=hit){scanTarget=hit;ScanProgress=0;mission.Accept(StudyAction.Measure,hit.targetId);}
                        ScanProgress=Mathf.Clamp01(ScanProgress+Mathf.Max(0,deltaTime)/scanDuration);
                        if(ScanProgress>=1){scanConsumed=true;mission.Accept(StudyAction.Scan,hit.targetId);var phag=hit.GetComponent<JourneyPhagocyteEntity>();if(phag)phag.Studied=true;if(world.plateletLaunch&&world.freeTools.Held==hit){if(phag)phag.SetState(JourneyPhagocyteState.Activated);var platelet=hit.GetComponent<JourneyPlateletEntity>();if(platelet)platelet.ActivateByScanner();if(phag||platelet)GetComponent<JourneyInstrumentRig>().ScannerActivated(hit.transform.position);}}
                    }
                }
                else if(!world.freeTools&&firing&&!toolConsumed&&!previousPhysicalTool&&correct){toolConsumed=true;world.Primary(hit);}
                if(instrumentFeedback)instrumentFeedback.PresentLock(correct?hit:null,scanning?ScanProgress:0,correct,scanning,source);
                previousPhysicalTool=firing;return;
            }
            if(world.hemodynamics)world.hemodynamics.ObserveScanner(scanning,hit);
            if(bioModes&&firing&&!scanning&&!bioModes.AllowsCurrentGoal())
            {
                ChargeProgress=0;
                if(!previousPhysicalTool)mission.FeedbackMessage("Выбери нужный режим: A вперёд / B назад; на ПК C.");
                previousPhysicalTool=firing;return;
            }
            if(plateletCapture&&plateletCapture.UpdateField(deltaTime,firing&&!scanning,AimSource(bioTool),LockedContact,desktopControls&&desktopControls.Active))
            {ChargeProgress=plateletCapture.CaptureProgress;if(plateletCapture.JustAdhered)toolConsumed=true;previousPhysicalTool=firing;return;}
            if(world&&world.EmbolusCaptured)
            {
                var fieldSource=AimSource(bioTool);
                bool desktop=mover&&!mover.UsingTrackedInput&&!XRSettings.isDeviceActive&&!useExplicitAimSources;
                if(desktop)
                {
                    if(!desktopFieldActive)DesktopFieldDistance=Mathf.Clamp(Vector3.Distance(fieldSource.position,world.bubble.position),.45f,6.5f);
                    desktopFieldActive=true;
                    if(firing)DesktopFieldDistance=Mathf.Clamp(DesktopFieldDistance+JourneyDesktopControls.WheelSteps*JourneyDesktopControls.DepthPerWheelStep,.45f,6.5f);
                }
                else desktopFieldActive=false;
                if(world.freeTools&&firing&&hit!=world.Find("embolus"))world.PauseEmbolusAttraction(deltaTime);
                else world.UpdateEmbolusField(deltaTime,fieldSource,firing,desktop?DesktopFieldDistance:JourneyWorld.EmbolusHoldDistance);
                if(instrumentFeedback)instrumentFeedback.PresentLock(firing?world.Find("embolus"):null,world.EmbolusStability,firing,false,AimSource(bioTool));
                previousPhysicalTool=firing;return;
            }
            desktopFieldActive=false;
            if(world.hemodynamics&&(!world.freeTools||bioModes.CurrentMode==BioToolMode.Tonus)&&world.hemodynamics.Control(deltaTime,firing&&!scanning,AimSource(bioTool),hit,desktopControls&&desktopControls.Active))
            {AdjustingTone=world.hemodynamics.Adjusting;ToneStability=world.hemodynamics.Stability;ChargeProgress=ToneStability;previousPhysicalTool=firing;return;}
            if(mission.SceneNumber==10&&goal!=null&&goal.action==StudyAction.Radius&&goal.target=="tone")
            {
                UpdateManualTone(deltaTime,firing,scanning,hit);
                previousPhysicalTool=firing;return;
            }
            if(AdjustingTone){AdjustingTone=false;ToneStability=0;}
            if(scanning&&!scanConsumed)
            {
                bool correct=hit&&(goal!=null&&(goal.action==StudyAction.Scan||world.plateletLaunch&&(goal.action==StudyAction.Activate||goal.action==StudyAction.Measure))&&goal.target==hit.targetId||goal==null&&mission.FreeResearchUnlocked)&&world.CanInspectBlood(hit);
                if(!correct){ScanProgress=0;scanTarget=null;}
                else
                {
                    if(scanTarget!=hit){scanTarget=hit;ScanProgress=0;}
                    ScanProgress=Mathf.Clamp01(ScanProgress+Mathf.Max(0,deltaTime)/scanDuration);
                    if(ScanProgress>=1)
                    {
                        scanConsumed=true;
                        if(goal==null)mission.FreeInspect(hit.targetId);
                        else if(mission.SceneNumber==14&&hit.targetId=="debris"&&world.recoveryDebris!=null)world.ScanRecoveryDebris(hit);
                        else{mission.Accept(goal.action,hit.targetId);if(world.plateletLaunch&&(goal.action==StudyAction.Activate||goal.action==StudyAction.Measure))GetComponent<JourneyInstrumentRig>().ScannerActivated(hit.transform.position);}
                    }
                }
                if(instrumentFeedback)instrumentFeedback.PresentLock(hit,ScanProgress,correct,true,source);
            }
            else if(firing&&goal!=null&&mission.SceneNumber==6&&goal.action==StudyAction.Pulse&&world.plaqueChallenge)
            {
                if(Time.time>=nextPlaqueShot&&!world.plaqueChallenge.Failed)
                {
                    nextPlaqueShot=Time.time+.22f;int before=world.plaqueChallenge.DestroyedCount;
                    if(hit&&hit.GetComponent<JourneyPlaqueLipid>())world.Primary(hit);
                    if(world.plaqueChallenge.DestroyedCount==before)
                    {var rig=GetComponent<JourneyInstrumentRig>();if(rig)rig.FirePulse(hit?hit.transform.position:source.position+source.forward*8);}
                }
                ChargeProgress=1;
                if(instrumentFeedback)instrumentFeedback.PresentLock(hit,0,hit&&hit.GetComponent<JourneyPlaqueLipid>(),false,source);
            }
            else if(firing&&goal!=null&&mission.SceneNumber==9&&goal.action==StudyAction.Mark)
            {
                if(Time.time>=nextAssaultShot)
                {
                    nextAssaultShot=Time.time+AssaultFireInterval;
                    int before=world.MarkedViruses;
                    if(hit&&hit.targetId=="incoming-virus")world.Primary(hit);
                    bool accepted=world.MarkedViruses>before;world.RecordAssaultShot(accepted);
                    if(mission.Current?.action==StudyAction.Mark)mission.FeedbackMessage(accepted?$"Попадание • серия {world.AssaultCombo} • {world.MarkedViruses}/{JourneyMission.AssaultVirusCount}":"Мимо • переведи прицел на движущийся вирион");
                    if(!accepted)
                    {
                        var rig=GetComponent<JourneyInstrumentRig>();
                        Vector3 end=hit?hit.transform.position:AimObstruction?AimObstruction.bounds.center:source.position+source.forward*12;
                        if(rig)rig.FirePulse(end);
                        if(instrumentFeedback&&instrumentFeedback.audioSource&&instrumentFeedback.pulseSound)instrumentFeedback.audioSource.PlayOneShot(instrumentFeedback.pulseSound,.20f);
                    }
                }
                ChargeProgress=1;
                if(instrumentFeedback)instrumentFeedback.PresentLock(hit,0,hit&&hit.targetId=="incoming-virus",false,source);
            }
            else if(firing&&!toolConsumed&&goal!=null)
            {
                bool charged=goal.action==StudyAction.Pulse||goal.action==StudyAction.Mark||goal.action==StudyAction.Capture;
                bool correct=hit&&(hit.targetId==goal.target||(goal.action==StudyAction.Pulse&&hit.targetId=="cap"));
                if(charged)
                {
                    if(!correct){ChargeProgress=0;chargeTarget=null;}
                    else
                    {
                        if(chargeTarget!=hit){chargeTarget=hit;ChargeProgress=0;}
                        ChargeProgress=Mathf.Clamp01(ChargeProgress+Mathf.Max(0,deltaTime)/impulseChargeDuration);
                        if(ChargeProgress>=1){toolConsumed=true;world.Primary(hit);}
                    }
                    if(instrumentFeedback)instrumentFeedback.PresentLock(hit,ChargeProgress,correct,false,source);
                }
                else if(!previousPhysicalTool&&goal.action!=StudyAction.Scan)
                {
                    toolConsumed=true;
                    if(goal.action==StudyAction.Enlarge)mission.FeedbackMessage("Увеличение: захвати учебную модель двумя руками и разведи их.");
                    else if(hit)world.Primary(hit);
                    else mission.FeedbackMessage("Наведи BioTool на цель: действие без попадания не засчитывается.");
                }
            }
            else if(instrumentFeedback)instrumentFeedback.PresentLock(null,0,false,false,source);
            previousPhysicalTool=firing;
        }
        void UpdateManualTone(float deltaTime,bool firing,bool scanning,JourneyTarget hit)
        {
            var source=AimSource(bioTool);
            if(!firing||scanning||!source){AdjustingTone=false;ToneStability=0;return;}
            if(!AdjustingTone)
            {
                // A missed press cannot become a grab by sweeping the held ray.
                if(previousPhysicalTool||!hit||hit.targetId!="tone")return;
                AdjustingTone=true;ToneStability=0;toneLimitWarned=false;
                toneHandStart=mover.origin.transform.InverseTransformPoint(source.position);
                toneAxis=mover.origin.transform.InverseTransformDirection(mover.path.Up(mover.Distance)).normalized;
                toneRadiusStart=tonePreviousRadius=mission.ModelRadius;
                desktopToneOffset=0;
                mission.FeedbackMessage(desktopControls&&desktopControls.Active?"Удерживай E над регулятором; колесо — настройка тонуса. Удержи умеренное сужение.":"Удерживай Trigger и опускай BioTool: умеренно сузь участок, затем удержи настройку.");
            }
            float dt=Mathf.Max(.0001f,deltaTime);
            var local=mover.origin.transform.InverseTransformPoint(source.position);
            float radius=Mathf.Clamp(toneRadiusStart+Vector3.Dot(local-toneHandStart,toneAxis)*.9f,.55f,1.15f);
            if(desktopControls&&desktopControls.Active)
            {desktopToneOffset+=JourneyDesktopControls.WheelSteps*.025f;radius=Mathf.Clamp(toneRadiusStart+desktopToneOffset,.55f,1.15f);}
            float rate=Mathf.Abs(radius-tonePreviousRadius)/dt;tonePreviousRadius=radius;
            mission.UpdateToneFromHand(radius);
            bool safe=radius>=.75f&&radius<=.88f;
            ToneStability=safe&&rate<.12f?Mathf.Clamp01(ToneStability+dt/.8f):0;
            if(radius<.68f&&!toneLimitWarned){toneLimitWarned=true;mission.WarnExcessiveTone();if(instrumentFeedback)instrumentFeedback.Warn(false);}
            var target=world.Find("tone");LockedTarget=target;
            if(instrumentFeedback)instrumentFeedback.PresentLock(target,ToneStability,safe,false,source);
            if(ToneStability>=1)
            {toolConsumed=true;AdjustingTone=false;mission.Accept(StudyAction.Radius,"tone");}
        }

        StudyGoal CurrentGoal()
        {
            // JourneyMission.Current is not safe until its Goals list is initialized.
            return mission != null && mission.Goals != null ? mission.Current : null;
        }

        void BindActions()
        {
            UnbindActions();
            boundScanAction = scanAction != null ? scanAction.action : null;
            boundToolAction = toolAction != null ? toolAction.action : null;
            // Misbinding the same action must not dispatch two journey operations.
            if (boundToolAction == boundScanAction) boundToolAction = null;

            if (boundScanAction != null)
            {
                scanHeld = boundScanAction.IsPressed();
                boundScanAction.performed += OnScanPerformed;
                boundScanAction.canceled += OnScanCanceled;
            }
            if (boundToolAction != null)
            {
                toolHeld = boundToolAction.IsPressed();
                boundToolAction.performed += OnToolPerformed;
                boundToolAction.canceled += OnToolCanceled;
            }
        }

        void EnsureActionBindings()
        {
            var scan = scanAction != null ? scanAction.action : null;
            var tool = toolAction != null ? toolAction.action : null;
            if (tool == scan) tool = null;
            if (scan != boundScanAction || tool != boundToolAction) BindActions();
        }

        void UnbindActions()
        {
            if (boundScanAction != null)
            {
                boundScanAction.performed -= OnScanPerformed;
                boundScanAction.canceled -= OnScanCanceled;
            }
            if (boundToolAction != null)
            {
                boundToolAction.performed -= OnToolPerformed;
                boundToolAction.canceled -= OnToolCanceled;
            }
            boundScanAction = null;
            boundToolAction = null;
            pendingScan = pendingTool = scanHeld = toolHeld = false;
        }

        void OnScanPerformed(InputAction.CallbackContext context)
        {
            if (!isActiveAndEnabled || scanHeld || !context.ReadValueAsButton()) return;
            scanHeld = true;
            pendingScan = true;
        }

        void OnScanCanceled(InputAction.CallbackContext context)
        {
            scanHeld = false;
        }

        void OnToolPerformed(InputAction.CallbackContext context)
        {
            if (!isActiveAndEnabled || toolHeld || !context.ReadValueAsButton()) return;
            toolHeld = true;
            pendingTool = true;
        }

        void OnToolCanceled(InputAction.CallbackContext context)
        {
            toolHeld = false;
        }

        Transform AimSource(Transform preferred)
        {
            if (XRSettings.isDeviceActive||useExplicitAimSources||(mover&&mover.UsingTrackedInput)) return preferred;
            if(desktopControls&&desktopControls.Active)return desktopControls.AimTransform;
            return mover != null && mover.viewCamera ? mover.viewCamera.transform : preferred;
        }

        bool TargetingTissue
        {
            get
            {
                if(!world||!world.freeTools||world.freeTools.Held||!mission||mission.Goals==null||mission.FreeBloodResearch)return false;
                var goal=CurrentGoal();if(goal==null)return false;
                if(RawScannerPressed)return (goal.action==StudyAction.Scan||goal.action==StudyAction.Measure||goal.action==StudyAction.Activate)&&world.research.Find(goal.target)==null;
                return RawToolPressed&&bioModes&&bioModes.CurrentMode==BioToolMode.Reverse&&goal.action==StudyAction.Direct&&goal.target!="immune-engulf"&&goal.target!="platelet";
            }
        }
        JourneyTarget Aim(Transform source)
        {
            AimObstruction=null;LockedContact=null;
            if (!source || targetMask.value == 0) return null;

            var ray = new Ray(source.position, source.forward);
            float distance = Mathf.Max(.01f, maxAimDistance);
            if(world.freeTools&&RawScannerPressed&&world.freeTools.Held)
            {
                var held=world.freeTools.Held;var heldCollider=held.GetComponent<Collider>();float bodyAcross=Vector3.Cross(heldCollider.bounds.center-ray.origin,ray.direction).sqrMagnitude;JourneyTarget nearest=heldCollider.Raycast(ray,out var bodyHit,12)?held:null;float best=nearest?bodyAcross:float.MaxValue;
                foreach(var topic in new[]{world.freeTools.payload.oxygen,world.freeTools.payload.co2,world.freeTools.payload.heme})
                {var c=topic.GetComponent<Collider>();if(world.freeTools.payload.Available(topic)&&c.Raycast(ray,out var bondHit,12)){float across=Vector3.Cross(c.bounds.center-ray.origin,ray.direction).sqrMagnitude;if(across<best){best=across;nearest=topic;}}}
                if(nearest){LockedContact=nearest.GetComponent<Collider>();return nearest;}
            }
            // Local analysis owns its interaction space. A momentary miss between
            // small content models must not promote a distant cell and close it.
            float contentsDistance=0;bool contents=world.analysis&&world.analysis.RayThroughContents(ray,out contentsDistance);
            if(contents)distance=Mathf.Min(distance,contentsDistance);
            else if(!TargetingTissue&&world.bloodEntities&&(RawScannerPressed||RawToolPressed||!world.freeTools&&(GripPressed()||Mouse.current!=null&&Mouse.current.leftButton.isPressed))){world.bloodEntities.Promote(ray,Mathf.Min(maxAimDistance,12));Physics.SyncTransforms();}
            int count = Physics.RaycastNonAlloc(ray, targetHits, distance, targetMask.value, QueryTriggerInteraction.Collide);
            // NonAlloc does not promise the nearest hits if its buffer is full.
            // Fail closed instead of aiming through an omitted nearer target.
            if (count == targetHits.Length) return null;
            SortByDistance(targetHits, count);
            for (int i = 0; i < count; i++)
            {
                var target = TargetFrom(targetHits[i].collider);
                if(TargetingTissue&&world.bloodEntities.Owns(target))continue;
                if(world.freeTools&&targetHits[i].collider.GetComponentInParent<JourneyHemostasisPiece>()&&target){LockedContact=targetHits[i].collider;return target;}
                if (!IsUsableTarget(target)) continue;
                if (!Obstructed(ray, targetHits[i].distance, target, 0f, source)){LockedContact=targetHits[i].collider;return target;}
            }

            // BioTool actions are precision operations: never turn a nearby cap or
            // other collider into a successful target through scanner-style assist.
            // Scanner keeps the small assist window for readable hold-to-lock scans.
            if (!useSphereAssist || sphereAssistRadius <= 0f || source != scanner) return null;
            count = Physics.SphereCastNonAlloc(ray, sphereAssistRadius, assistHits, distance, targetMask.value, QueryTriggerInteraction.Collide);
            if (count == assistHits.Length) return null;
            SortByDistance(assistHits, count);
            for (int i = 0; i < count; i++)
            {
                var target = TargetFrom(assistHits[i].collider);
                if (!IsUsableTarget(target)) continue;
                if (!Obstructed(ray, assistHits[i].distance, target, sphereAssistRadius, source)){LockedContact=assistHits[i].collider;return target;}
            }
            return null;
        }

        static JourneyTarget TargetFrom(Collider collider)
        {
            if(!collider)return null;
            var target=collider.GetComponentInParent<JourneyTarget>();if(target)return target;
            // Scene12 physical hand pieces are the visible interaction surface for
            // the existing semantic target. They do not use JourneyTarget's home
            // return/grab progression: placement belongs to the wound puzzle.
            var piece=collider.GetComponentInParent<JourneyHemostasisPiece>();
            if(piece&&piece.puzzle)
            {
                var m=piece.puzzle.world.mission;
                if(m.SceneNumber==13&&m.Current?.action==StudyAction.Scan&&m.Current.target=="clot")return piece.puzzle.world.Find("clot");
                return piece.puzzle.world.Find(piece.strand?"fibrin":"platelet");
            }
            return null;
        }
        static bool GripPressed()
        {
            foreach(var device in InputSystem.devices)if(device.enabled&&device.layout.Contains("Controller")){var grip=device.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("gripButton");if(grip!=null&&grip.isPressed)return true;}
            return false;
        }

        static bool IsUsableTarget(JourneyTarget target)
        {
            return target && target.isActiveAndEnabled && target.gameObject.activeInHierarchy;
        }

        bool Obstructed(Ray ray, float targetDistance, JourneyTarget target, float radius, Transform source)
        {
            float distance = Mathf.Max(.01f, targetDistance + ObstructionEpsilon);
            int count = radius > 0f
                ? Physics.SphereCastNonAlloc(ray, radius, obstructionHits, distance, ~0, QueryTriggerInteraction.Collide)
                : Physics.RaycastNonAlloc(ray, obstructionHits, distance, ~0, QueryTriggerInteraction.Collide);
            if (count == obstructionHits.Length) return true;
            SortByDistance(obstructionHits, count);
            for (int i = 0; i < count; i++)
            {
                var collider = obstructionHits[i].collider;
                if (!collider || obstructionHits[i].distance >= targetDistance - ObstructionEpsilon) continue;
                if (source && collider.transform.IsChildOf(source)) continue;
                // The player's body and controller colliders are not biological
                // obstacles. A hand-held emitter can start inside the rig capsule.
                if(mover&&mover.origin&&collider.transform.IsChildOf(mover.origin.transform))continue;
                if (TargetFrom(collider) == target) continue;
                if(TargetingTissue&&world.bloodEntities.Owns(TargetFrom(collider)))continue;
                // Sensor volumes (e.g. the transparent embolus trap) are not solid
                // line-of-sight blockers. Other target triggers still block normally.
                if(collider.isTrigger&&!TargetFrom(collider))continue;
                AimObstruction=collider;
                return true;
            }
            return false;
        }

        static void SortByDistance(RaycastHit[] hits, int count)
        {
            for (int i = 1; i < count; i++)
            {
                var value = hits[i];
                int j = i - 1;
                while (j >= 0 && hits[j].distance > value.distance)
                {
                    hits[j + 1] = hits[j];
                    j--;
                }
                hits[j + 1] = value;
            }
        }

        void SetHovered(JourneyTarget target)
        {
            if(world&&world.research)world.research.SetHovered(target);
            if (hoveredTarget == target) return;
            if (hoveredTarget) hoveredTarget.SetHovered(false);
            hoveredTarget = target;
            if (hoveredTarget) hoveredTarget.SetHovered(true);
        }

        public void Scan()
        {
            if (mission == null || mission.Goals == null) return;
            var goal = CurrentGoal();
            var target = Aim(AimSource(scanner));
            // Retain the explicit desktop focus fallback; XR always requires pointing.
            if (!target && !mission.FreeBloodResearch&&!XRSettings.isDeviceActive && world != null)
                target = world.Find(goal != null ? goal.target : (mission.FreeResearchUnlocked ? "rbc" : ""));

            if (!IsUsableTarget(target))
            {
                mission.FeedbackMessage("Наведи сканер на подсвеченную модель.");
                return;
            }
            if (goal == null && mission.FreeResearchUnlocked) mission.FreeInspect(target.targetId);
            else mission.Accept(StudyAction.Scan, target.targetId);
        }

        public void Primary()
        {
            if (mission == null || mission.Goals == null || world == null) return;
            if(mission.FreeBloodResearch){var researchTarget=Aim(AimSource(bioTool));if(researchTarget)world.Primary(researchTarget);return;}
            var goal = CurrentGoal();
            // World.Primary supports Scan for the generic HUD/validation API. The
            // instrument-specific BioTool must not invoke that scanner route.
            if (goal != null && goal.action == StudyAction.Scan)
            {
                mission.FeedbackMessage("Для сканирования используй сканер (F / левый Trigger).");
                return;
            }
            if (goal == null) return;

            var target = Aim(AimSource(bioTool));
            if (!target && !XRSettings.isDeviceActive) target = world.Find(goal.target);
            if(IsUsableTarget(target)&&world.flowModel&&target.transform==world.flowModel&&mission.Current!=null&&
                (mission.Current.action==StudyAction.Radius||mission.Current.action==StudyAction.Pressure))
                world.Primary(world.Find(mission.Current.target));
            else if (IsUsableTarget(target)) world.Primary(target);
            else mission.FeedbackMessage("Наведи BioTool на цель или выбери действие на панели.");
        }

        public void FocusedAction()
        {
            // Intentionally remains callable by HUD buttons even when the UI guard
            // has disabled this component's automatic instrument input.
            if (world == null || mission == null || mission.Goals == null) return;
            var goal = CurrentGoal();
            world.Primary(goal != null ? world.Find(goal.target) : null);
        }
    }
}

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
        float desktopToneOffset;
        public JourneyDesktopControls DesktopControls=>desktopControls;
        void Awake()
        {
            if(!Application.isMobilePlatform){desktopControls=GetComponent<JourneyDesktopControls>();if(!desktopControls)desktopControls=gameObject.AddComponent<JourneyDesktopControls>();desktopControls.input=this;}
        }

        const int RaycastCapacity = 64;
        const float ObstructionEpsilon = .002f;
        readonly RaycastHit[] targetHits = new RaycastHit[RaycastCapacity];
        readonly RaycastHit[] assistHits = new RaycastHit[RaycastCapacity];
        readonly RaycastHit[] obstructionHits = new RaycastHit[RaycastCapacity];

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
                if (keyboard.gKey.wasPressedThisFrame)
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
                    if (world != null) world.Find(goal != null ? goal.target : "rbc")?.Enlarge();
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
            var keyboard=Keyboard.current;
            bool scan=scanHeld||(keyboard!=null&&keyboard.fKey.isPressed);
            bool tool=toolHeld||(keyboard!=null&&keyboard.eKey.isPressed);
            UpdateHeldInstruments(Time.deltaTime,scan,tool);
        }
        public void CancelInstrumentProgress()
        {ScanProgress=ChargeProgress=0;scanTarget=chargeTarget=LockedTarget=null;scanConsumed=toolConsumed=previousPhysicalTool=desktopFieldActive=false;AdjustingTone=false;ToneStability=0;if(world)world.ReleaseEmbolus();}

        /// <summary>Shared controller/keyboard hold path. Uses real scene raycasts; no focused-target fallback.</summary>
        public void UpdateHeldInstruments(float deltaTime,bool scanning,bool firing)
        {
            if(!instrumentFeedback)instrumentFeedback=GetComponent<JourneyToolFeedback>();
            if(!scanning){ScanProgress=0;scanTarget=null;scanConsumed=false;}
            if(!firing){ChargeProgress=0;chargeTarget=null;toolConsumed=false;}
            if(!mission||mission.Goals==null||!mission.Ready){ScanProgress=ChargeProgress=0;previousPhysicalTool=firing;return;}
            var goal=CurrentGoal();
            var source=AimSource(scanning?scanner:bioTool);
            var hit=Aim(source);LockedTarget=hit;
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
                world.UpdateEmbolusField(deltaTime,fieldSource,firing,desktop?DesktopFieldDistance:JourneyWorld.EmbolusHoldDistance);
                if(instrumentFeedback)instrumentFeedback.PresentLock(firing?world.Find("embolus"):null,world.EmbolusStability,firing,false,AimSource(bioTool));
                previousPhysicalTool=firing;return;
            }
            desktopFieldActive=false;
            if(mission.SceneNumber==10&&goal!=null&&goal.action==StudyAction.Radius&&goal.target=="tone")
            {
                UpdateManualTone(deltaTime,firing,scanning,hit);
                previousPhysicalTool=firing;return;
            }
            if(AdjustingTone){AdjustingTone=false;ToneStability=0;}
            if(scanning&&!scanConsumed)
            {
                bool correct=hit&&(goal!=null&&goal.action==StudyAction.Scan&&goal.target==hit.targetId||goal==null&&mission.FreeResearchUnlocked);
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
                        else mission.Accept(StudyAction.Scan,hit.targetId);
                    }
                }
                if(instrumentFeedback)instrumentFeedback.PresentLock(hit,ScanProgress,correct,true,source);
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

        JourneyTarget Aim(Transform source)
        {
            AimObstruction=null;
            if (!source || targetMask.value == 0) return null;

            var ray = new Ray(source.position, source.forward);
            float distance = Mathf.Max(.01f, maxAimDistance);
            int count = Physics.RaycastNonAlloc(ray, targetHits, distance, targetMask.value, QueryTriggerInteraction.Collide);
            // NonAlloc does not promise the nearest hits if its buffer is full.
            // Fail closed instead of aiming through an omitted nearer target.
            if (count == targetHits.Length) return null;
            SortByDistance(targetHits, count);
            for (int i = 0; i < count; i++)
            {
                var target = TargetFrom(targetHits[i].collider);
                if (!IsUsableTarget(target)) continue;
                if (!Obstructed(ray, targetHits[i].distance, target, 0f, source)) return target;
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
                if (!Obstructed(ray, assistHits[i].distance, target, sphereAssistRadius, source)) return target;
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
            if (!target && !XRSettings.isDeviceActive && world != null)
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

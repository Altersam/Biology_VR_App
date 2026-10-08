using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyTarget : MonoBehaviour
    {
        public string targetId, label;
        public JourneyMission mission;
        public bool grabbed;

        [Tooltip("Optional hover emission override; enable with JourneyInput.hoverFeedback.")]
        public bool hoverEmission;
        public Color hoverEmissionColor = new Color(.02f, .24f, .42f, 1f);

        const float RestoreDuration = .35f;
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        Vector3 originalScale;
        Transform homeParent;
        Vector3 homeLocalPosition;
        Quaternion homeLocalRotation;
        Vector3 homeWorldPosition;
        Quaternion homeWorldRotation;
        bool homeInitialized;
        float twoHandStart;
        Vector3 twoHandScaleStart;
        bool enlargedForHome;
        bool keyboardHeld;
        bool restorePending;
        bool eventsAttached;
        bool rebindGrabEvents;
        bool disabling;
        bool hovered;
        string lastAcceptedGrabId;
        XRGrabInteractable grab;
        Rigidbody body;
        Coroutine restoreRoutine;
        JourneyFlowPickup flowPickup;
        Vector3 desktopHoldVelocity;
        float desktopHoldDistance=1.05f;
        Renderer[] hoverRenderers;
        bool[] supportsEmission;
        Color[] savedEmission;
        MaterialPropertyBlock hoverBlock;

        public Vector3 HomeScale => originalScale;
        public bool ReturningHome=>restoreRoutine!=null||(flowPickup&&flowPickup.Rejoining);
        public bool DesktopHeld=>keyboardHeld;
        public void SetPresentationPose(Vector3 position,Quaternion rotation)
        {
            if(grabbed||keyboardHeld||(grab&&grab.isSelected))return;
            StopRestore();homeWorldPosition=position;homeWorldRotation=rotation;
            homeLocalPosition=homeParent?homeParent.InverseTransformPoint(position):position;
            homeLocalRotation=homeParent?Quaternion.Inverse(homeParent.rotation)*rotation:rotation;
            BeginRestore();
        }
        Vector3 DisplayHomeScale => NonZeroScale(originalScale) * (enlargedForHome ? 2f : 1f);

        void Awake()
        {
            SetHomePose();
            CacheHoverRenderers();
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            flowPickup=GetComponent<JourneyFlowPickup>();
        }

        void OnEnable()
        {
            disabling = false;
            // World.ShowScene stages inactive targets before reactivating them.
            SetHomePose();
            AttachGrabEvents();
            rebindGrabEvents=true;
        }

        void Start()
        {
            // Also covers AddComponent<XRGrabInteractable> after our first OnEnable.
            AttachGrabEvents();
        }

        void OnDisable()
        {
            disabling = true;
            SetHovered(false);
            StopRestore();
            if (grab && grab.isSelected && grab.interactionManager)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            DetachGrabEvents();
            // Unity is inside GameObject.SetActive(false) here. Reparenting from
            // OnDisable triggers "already being activated or deactivated" and can
            // corrupt the next scene's staging pose. The owning scene stages the
            // target before re-enabling it; just clear transient interaction state.
            if (homeInitialized)
            {
                restorePending=false;keyboardHeld=grabbed=false;lastAcceptedGrabId=null;enlargedForHome=false;ClearVelocity();
            }
        }

        void OnDestroy()
        {
            DetachGrabEvents();
            StopRestore();
        }

        void AttachGrabEvents()
        {
            if (!grab) grab = GetComponent<XRGrabInteractable>();
            if (!body) body = GetComponent<Rigidbody>();
            if (!grab) return;
            // Controller-operated XRI grabbing, with no release throw.
            grab.throwOnDetach = false;
            // Runtime UnityEvent listeners are not retained by editor script reloads,
            // while the private bookkeeping flag can be restored by hot reload.
            // Rebind idempotently whenever this target becomes active.
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
            eventsAttached = true;
        }

        void DetachGrabEvents()
        {
            if (grab && eventsAttached)
            {
                grab.selectEntered.RemoveListener(OnSelectEntered);
                grab.selectExited.RemoveListener(OnSelectExited);
            }
            eventsAttached = false;
        }

        /// <summary>
        /// Capture the staged transform and base scale for a new stage. Call after
        /// positioning an active target; inactive staging is captured in OnEnable.
        /// </summary>
        public void SetHomePose()
        {
            if (grabbed || keyboardHeld || (grab && grab.isSelected)) return;
            StopRestore();
            originalScale = NonZeroScale(transform.localScale);
            transform.localScale = originalScale;
            CaptureHomePosition();
            enlargedForHome = false;
            lastAcceptedGrabId = null;
        }

        void CaptureHomePosition()
        {
            homeParent = transform.parent;
            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;
            homeWorldPosition = transform.position;
            homeWorldRotation = transform.rotation;
            homeInitialized = true;
        }

        /// <summary>Immediately restore the staged pose/base scale for reset or restaging.</summary>
        public void ResetHomePose()
        {
            StopRestore();
            if (!homeInitialized) SetHomePose();
            restorePending = false;
            keyboardHeld = grabbed = false;
            lastAcceptedGrabId = null;
            if (grab && grab.isSelected && grab.interactionManager)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            StopRestore();
            enlargedForHome = false;
            ApplyHomePose();
        }

        void ApplyHomePose()
        {
            transform.SetParent(homeParent, false);
            if (homeParent)
            {
                transform.localPosition = homeLocalPosition;
                transform.localRotation = homeLocalRotation;
            }
            else transform.SetPositionAndRotation(homeWorldPosition, homeWorldRotation);
            transform.localScale = DisplayHomeScale;
            ClearVelocity();
        }

        static Vector3 NonZeroScale(Vector3 scale)
        {
            return new Vector3(NonZeroComponent(scale.x), NonZeroComponent(scale.y), NonZeroComponent(scale.z));
        }

        static float NonZeroComponent(float value)
        {
            const float minimum = .0001f;
            if (float.IsNaN(value) || float.IsInfinity(value)) return minimum;
            return Mathf.Abs(value) < minimum ? (value < 0f ? -minimum : minimum) : value;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {BeginXriSelection(true);}

        void BeginXriSelection(bool captureCurrentHome)
        {
            if (!isActiveAndEnabled) return;
            // A second selecting controller is not another educational grab.
            if (grab && grab.interactorsSelecting.Count > 1 && grabbed) return;
            if (captureCurrentHome && !keyboardHeld && restoreRoutine == null) CaptureHomePosition();
            StopRestore();
            keyboardHeld = false;
            restorePending = true;
            grabbed = CanBeginGrab();
            if(grabbed&&mission.Current!=null&&targetId=="oxygen"&&mission.Current.action==StudyAction.Detach)mission.Accept(StudyAction.Detach,"oxygen");
            if(!grabbed&&grab&&grab.isSelected&&grab.interactionManager)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            twoHandStart=0;
            if (grab && grab.isSelected) return;
            grabbed = false;
            if(!disabling&&targetId.StartsWith("antibody")&&mission&&mission.SceneNumber==15&&mission.world.PlaceAntibody(this))
            {restorePending=false;return;}
            if(!disabling&&targetId=="oxygen"&&mission&&mission.Goals!=null&&mission.Current!=null&&mission.Current.action==StudyAction.Bind)
            {
                var heme=mission.world.Find("heme");
                if(heme&&Vector3.Distance(transform.position,heme.transform.position)<.16f)mission.Accept(StudyAction.Bind,"oxygen");
            }
            if (!restorePending) return;
            restorePending = false;
            if(disabling)return;
            if (!gameObject.activeInHierarchy) ApplyHomePose();
            else BeginRestore();
        }
        void Update()
        {
            // Unity/XRI may reconstruct the UnityEvent instance after our
            // OnEnable/Start during a script reload. Rebind after initialization,
            // and reconcile only an actual XRI selection if its event was lost.
            if(rebindGrabEvents){AttachGrabEvents();rebindGrabEvents=false;}
            if(grab&&!keyboardHeld&&!disabling)
            {
                if(grab.isSelected&&!grabbed){AttachGrabEvents();BeginXriSelection(false);}
                else if(!grab.isSelected&&grabbed)OnSelectExited(null);
            }
            if(keyboardHeld&&UnityEngine.InputSystem.Mouse.current!=null)
            {
                float scroll=JourneyDesktopControls.WheelSteps;
                if(Mathf.Abs(scroll)>.001f)desktopHoldDistance=Mathf.Clamp(desktopHoldDistance+scroll*JourneyDesktopControls.DepthPerWheelStep,.45f,1.85f);
            }
            if(keyboardHeld)
            {
                var controls=mission?mission.GetComponent<JourneyInput>()?.DesktopControls:null;
                var destination=new Vector3(0,-.02f,desktopHoldDistance);
                if(controls&&controls.Active){var ray=controls.PointerRay();destination=transform.parent.InverseTransformPoint(mission.mover.viewCamera.transform.position+ray.direction*desktopHoldDistance);}
                transform.localPosition=Vector3.SmoothDamp(transform.localPosition,destination,ref desktopHoldVelocity,.14f,4,Time.deltaTime);transform.localRotation=Quaternion.Slerp(transform.localRotation,Quaternion.identity,1-Mathf.Exp(-Time.deltaTime*12));
            }
            if(!grab||!mission||mission.Goals==null||grab.interactorsSelecting.Count<2){twoHandStart=0;return;}
            bool educational=lastAcceptedGrabId==targetId||enlargedForHome||(mission.Current!=null&&mission.Current.target==targetId&&mission.Current.action==StudyAction.Enlarge);
            if(!educational)return;
            float distance=Vector3.Distance(grab.interactorsSelecting[0].transform.position,grab.interactorsSelecting[1].transform.position);
            if(twoHandStart<=0){twoHandStart=Mathf.Max(.06f,distance);twoHandScaleStart=transform.localScale;}
            float factor=Mathf.Clamp(distance/twoHandStart,.7f,2.4f);transform.localScale=twoHandScaleStart*factor;
            if(!enlargedForHome&&distance>twoHandStart*1.25f&&distance-twoHandStart>.10f&&mission.Current!=null&&mission.Current.action==StudyAction.Enlarge)Enlarge();
        }

        bool CanBeginGrab()
        {
            if (mission == null || mission.Goals == null) return false;
            var current = mission.Current;
            if(mission.SceneNumber==15&&current?.action==StudyAction.Bind&&targetId.StartsWith("antibody"))return mission.Ready;
            if (current != null && current.action == StudyAction.Grab)
            {
                // Reject before desktop parenting; never advance a wrong target.
                if (current.target != targetId) return false;
                if (!mission.Accept(StudyAction.Grab, targetId)) return false;
                lastAcceptedGrabId = targetId;
                return true;
            }

            // Regrabbing an already taught object is inspection only, without Accept.
            return mission.Ready && (mission.FreeResearchUnlocked || lastAcceptedGrabId == targetId || (flowPickup&&flowPickup.InStudyFlow) || (current!=null&&current.target==targetId));
        }

        public void KeyboardGrab()
        {
            if (keyboardHeld)
            {
                keyboardHeld = grabbed = false;
                if(targetId=="oxygen"&&mission?.Current?.action==StudyAction.Bind)
                {var heme=mission.world.Find("heme");if(heme&&Vector3.Distance(transform.position,heme.transform.position)<.16f)mission.Accept(StudyAction.Bind,"oxygen");}
                BeginRestore();
                return;
            }
            if (!isActiveAndEnabled || (grab && grab.isSelected)) return;
            var camera = mission != null && mission.mover != null ? mission.mover.viewCamera : null;
            if (!camera) return;
            if (restoreRoutine == null) CaptureHomePosition();
            if (!CanBeginGrab()) return;
            if (!isActiveAndEnabled) return;

            StopRestore();
            ClearVelocity();
            keyboardHeld = grabbed = true;
            desktopHoldVelocity=Vector3.zero;desktopHoldDistance=1.05f;
            if(targetId=="oxygen"&&mission?.Current?.action==StudyAction.Detach)mission.Accept(StudyAction.Detach,"oxygen");
            transform.SetParent(camera.transform, true);
            // Desktop aim is the camera centre. An object fixed off-axis relative
            // to that same camera could never be scanned by looking at it.
            // Preserve the catch pose and ease into the camera-space hand socket.
        }

        public void Enlarge()
        {
            if (enlargedForHome || mission == null || mission.Goals == null) return;
            if (!mission.Accept(StudyAction.Enlarge, targetId)) return;
            bool wasRestoring = restoreRoutine != null;
            StopRestore();
            originalScale = NonZeroScale(originalScale);
            enlargedForHome = true;
            // Display scale is defined in the staged parent, not in a possibly scaled controller.
            var desiredWorld = homeParent ? Vector3.Scale(homeParent.lossyScale, DisplayHomeScale) : DisplayHomeScale;
            var parentScale = transform.parent ? NonZeroScale(transform.parent.lossyScale) : Vector3.one;
            transform.localScale = new Vector3(desiredWorld.x / parentScale.x, desiredWorld.y / parentScale.y, desiredWorld.z / parentScale.z);
            if(wasRestoring && !grabbed && !keyboardHeld) BeginRestore();
        }

        void BeginRestore()
        {
            if (!homeInitialized) return;
            StopRestore();
            if(!flowPickup)flowPickup=GetComponent<JourneyFlowPickup>();
            if(flowPickup&&flowPickup.InStudyFlow)
            {enlargedForHome=false;flowPickup.StartReturn(homeParent,NonZeroScale(originalScale));return;}
            if (!gameObject.activeInHierarchy)
            {
                ApplyHomePose();
                return;
            }
            restoreRoutine = StartCoroutine(RestoreHomePose());
        }

        IEnumerator RestoreHomePose()
        {
            // XRI applies its detach/rigidbody restoration after selectExited.
            // Start moving on the next frame so its final detach does not overwrite us.
            yield return null;
            if (!this || (grab && grab.isSelected))
            {
                restoreRoutine = null;
                yield break;
            }
            ClearVelocity();
            // Interpolate scale in the home's local space, not a controller/camera's.
            transform.SetParent(homeParent, true);
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 startScale = NonZeroScale(transform.localScale);

            for (float elapsed = 0f; elapsed < RestoreDuration; elapsed += Time.deltaTime)
            {
                if (!this || (grab && grab.isSelected))
                {
                    restoreRoutine = null;
                    yield break;
                }
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / RestoreDuration));
                Vector3 targetPosition = homeParent ? homeParent.TransformPoint(homeLocalPosition) : homeWorldPosition;
                Quaternion targetRotation = homeParent ? homeParent.rotation * homeLocalRotation : homeWorldRotation;
                transform.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));
                transform.localScale = Vector3.Lerp(startScale, DisplayHomeScale, t);
                ClearVelocity();
                yield return null;
            }
            if (this) ApplyHomePose();
            restoreRoutine = null;
        }

        void ClearVelocity()
        {
            if (!body || body.isKinematic) return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        void StopRestore()
        {
            if (restoreRoutine == null) return;
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }

        void CacheHoverRenderers()
        {
            hoverRenderers = GetComponentsInChildren<Renderer>(true);
            supportsEmission = new bool[hoverRenderers.Length];
            savedEmission = new Color[hoverRenderers.Length];
            hoverBlock = new MaterialPropertyBlock();
            for (int i = 0; i < hoverRenderers.Length; i++)
            {
                var materials = hoverRenderers[i].sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                {
                    if (materials[j] && materials[j].HasProperty(EmissionColorId))
                    {
                        supportsEmission[i] = true;
                        break;
                    }
                }
            }
        }

        /// <summary>Optional hover property override; allocates only during initial caching.</summary>
        public void SetHovered(bool value)
        {
            if (value == hovered || (value && !hoverEmission) || hoverRenderers == null) return;
            hovered = value;
            for (int i = 0; i < hoverRenderers.Length; i++)
            {
                var renderer = hoverRenderers[i];
                if (!renderer || !supportsEmission[i]) continue;
                renderer.GetPropertyBlock(hoverBlock);
                if (value) savedEmission[i] = hoverBlock.GetColor(EmissionColorId);
                hoverBlock.SetColor(EmissionColorId, value ? hoverEmissionColor : savedEmission[i]);
                renderer.SetPropertyBlock(hoverBlock);
            }
        }
    }
}

using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace BiologyVR.V2
{
    /// <summary>
    /// Player rig: XR Origin with head and two tracked hands; on desktop the same hands are
    /// placed in front of the camera and mouse-look turns the body. Owns the comfort vignette
    /// and full-screen fades (a quad in front of the eyes, works on PICO without post-processing).
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class Bvr2Rig : MonoBehaviour
    {
        public XROrigin origin;
        public Transform cameraOffset;
        public Camera head;
        public Transform leftHand, rightHand;
        public TrackedPoseDriver headDriver, leftDriver, rightDriver;
        public Renderer screenQuad;
        public Bvr2Input input;
        [Tooltip("Desktop eye height above the body root (m).")]
        public float desktopEyeHeight = 0f;
        public float mouseSensitivity = 0.11f;
        [Header("Comfort")]
        public bool comfortVignette = true;
        public int snapDegrees = 30;

        public bool IsXR { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public Transform LeftAim => leftHand;
        public Transform RightAim => rightHand;

        float vignette, fadeAlpha, fadeTarget; Color fadeColor = Color.black; float fadeSpeed = 2f;
        MaterialPropertyBlock block;

        public void Build(Material screenMaterial)
        {
            if (!origin)
            {
                var o = new GameObject("XR Origin"); o.transform.SetParent(transform, false);
                origin = o.AddComponent<XROrigin>();
            }
            origin.transform.localPosition = new Vector3(0, -1.55f, 0);
            if (!cameraOffset) { cameraOffset = new GameObject("Camera Offset").transform; cameraOffset.SetParent(origin.transform, false); }
            if (!head)
            {
                var c = new GameObject("Main Camera"); c.tag = "MainCamera"; c.transform.SetParent(cameraOffset, false); c.transform.localPosition = new Vector3(0, 1.55f, 0);
                head = c.AddComponent<Camera>(); c.AddComponent<AudioListener>();
            }
            head.nearClipPlane = 0.03f; head.farClipPlane = 160f; head.clearFlags = CameraClearFlags.SolidColor; head.backgroundColor = Bvr2Style.Lumen; head.allowHDR = false;
            origin.Camera = head; origin.CameraFloorOffsetObject = cameraOffset.gameObject; origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            if (!headDriver) headDriver = head.gameObject.AddComponent<TrackedPoseDriver>();
            headDriver.ignoreTrackingState = true;
            headDriver.positionInput = new InputActionProperty(new InputAction("HMD position", binding: "<XRHMD>/centerEyePosition"));
            headDriver.rotationInput = new InputActionProperty(new InputAction("HMD rotation", binding: "<XRHMD>/centerEyeRotation"));
            if (!leftHand) { leftHand = new GameObject("Left Hand").transform; leftHand.SetParent(cameraOffset, false); }
            if (!rightHand) { rightHand = new GameObject("Right Hand").transform; rightHand.SetParent(cameraOffset, false); }
            leftDriver = HandDriver(leftHand, "LeftHand", leftDriver); rightDriver = HandDriver(rightHand, "RightHand", rightDriver);
            if (!screenQuad)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Comfort screen";
                DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(head.transform, false); q.transform.localPosition = new Vector3(0, 0, 0.06f); q.transform.localScale = new Vector3(0.16f, 0.16f, 1);
                screenQuad = q.GetComponent<Renderer>();
            }
            if (screenMaterial) screenQuad.sharedMaterial = screenMaterial;
            screenQuad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static TrackedPoseDriver HandDriver(Transform hand, string side, TrackedPoseDriver existing)
        {
            var d = existing ? existing : hand.gameObject.AddComponent<TrackedPoseDriver>();
            d.ignoreTrackingState = true;
            d.positionInput = new InputActionProperty(new InputAction(side + " aim position", binding: $"<XRController>{{{side}}}/pointerPosition"));
            d.rotationInput = new InputActionProperty(new InputAction(side + " aim rotation", binding: $"<XRController>{{{side}}}/pointerRotation"));
            return d;
        }

        void Awake() { block = new MaterialPropertyBlock(); if (!head) Build(null); }

        void OnEnable()
        {
            foreach (var d in new[] { headDriver, leftDriver, rightDriver })
                if (d) { d.positionInput.action?.Enable(); d.rotationInput.action?.Enable(); }
        }

        void Start() { ApplyMode(XRSettings.isDeviceActive); Yaw = transform.eulerAngles.y; }

        void ApplyMode(bool xr)
        {
            IsXR = xr;
            foreach (var d in new[] { headDriver, leftDriver, rightDriver }) if (d) d.enabled = xr;
            if (!xr)
            {
                head.transform.localPosition = new Vector3(0, 1.55f + desktopEyeHeight, 0);
                head.fieldOfView = 72f;
            }
        }

        void Update()
        {
            if (XRSettings.isDeviceActive != IsXR) ApplyMode(XRSettings.isDeviceActive);
            if (!IsXR) DesktopLook();
            else if (input && input.SnapTurn != 0) SnapTurn(input.SnapTurn * snapDegrees);
        }

        void DesktopLook()
        {
            var mouse = Mouse.current;
            if (mouse != null && Application.isFocused && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame) && Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
            bool look = mouse != null && (Cursor.lockState == CursorLockMode.Locked || mouse.middleButton.isPressed);
            if (look && input) { Yaw += input.Look.x * mouseSensitivity; Pitch = Mathf.Clamp(Pitch - input.Look.y * mouseSensitivity, -80, 80); }
            transform.rotation = Quaternion.Euler(0, Yaw, 0);
            head.transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
            // Hands: tools held low in front of the camera, aimed at the screen centre.
            var cam = head.transform; Vector3 target = cam.position + cam.forward * 12f;
            leftHand.position = cam.position + cam.rotation * new Vector3(-0.26f, -0.27f, 0.58f);
            rightHand.position = cam.position + cam.rotation * new Vector3(0.27f, -0.28f, 0.58f);
            leftHand.rotation = Quaternion.LookRotation(target - leftHand.position, cam.up);
            rightHand.rotation = Quaternion.LookRotation(target - rightHand.position, cam.up);
        }

        /// <summary>Desktop helper for scripts and tests: aim the view at a world point.</summary>
        public void LookAt(Vector3 world)
        {
            var d = world - head.transform.position;
            if (d.sqrMagnitude < 1e-4f) return;
            if (IsXR) { var flat = Vector3.ProjectOnPlane(d, Vector3.up); if (flat.sqrMagnitude > 1e-4f) SnapTurn(Vector3.SignedAngle(Vector3.ProjectOnPlane(head.transform.forward, Vector3.up), flat, Vector3.up)); return; }
            var rot = Quaternion.LookRotation(d); var e = rot.eulerAngles;
            Yaw = e.y; Pitch = Mathf.Clamp(Mathf.DeltaAngle(0, e.x), -80, 80);
            transform.rotation = Quaternion.Euler(0, Yaw, 0); head.transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
        }

        public void SnapTurn(float degrees)
        {
            // Rotate the whole body around the head so the view does not swing sideways.
            Vector3 pivot = head.transform.position;
            transform.RotateAround(pivot, Vector3.up, degrees);
            Yaw = transform.eulerAngles.y;
            Fade(Color.black, 0.18f, 14f);
        }

        // ----- comfort screen --------------------------------------------------------------
        public void SetVignette(float amount) { vignette = comfortVignette ? Mathf.Clamp01(amount) : 0; }
        /// <summary>A coloured vignette for special views (synchronisation membrane). amount 0 = off.</summary>
        public void SetOverlay(Color color, float amount) { overlayColor = color; overlay = Mathf.Clamp01(amount); }
        Color overlayColor; float overlay;
        /// <summary>Flash or fade the view. alpha 0 clears.</summary>
        public void Fade(Color color, float alpha, float speed = 2f) { fadeColor = color; fadeTarget = alpha; fadeSpeed = speed; if (speed > 10) fadeAlpha = alpha; }
        public void FadeTo(Color color, float alpha, float speed) { fadeColor = color; fadeTarget = alpha; fadeSpeed = speed; }
        public float FadeAlpha => fadeAlpha;

        void LateUpdate()
        {
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, fadeTarget, Time.unscaledDeltaTime * fadeSpeed);
            if (fadeTarget > 0 && fadeSpeed > 10 && Mathf.Approximately(fadeAlpha, fadeTarget)) fadeTarget = 0; // snap flashes fall back
            if (!screenQuad) return;
            block.SetColor("_Color", new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeAlpha));
            float vg = Mathf.Max(vignette, overlay);
            block.SetVector("_Vignette", new Vector4(vg * 0.85f, Mathf.Lerp(0.95f, 0.38f, vg), 1.0f, 0));
            block.SetColor("_VignetteColor", overlay > vignette ? overlayColor : new Color(0.03f, 0f, 0.01f, 1f));
            screenQuad.SetPropertyBlock(block);
            screenQuad.enabled = fadeAlpha > 0.002f || vg > 0.01f;
        }
    }
}

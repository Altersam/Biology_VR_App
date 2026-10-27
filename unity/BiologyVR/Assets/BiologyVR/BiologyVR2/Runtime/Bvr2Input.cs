using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace BiologyVR.V2
{
    /// <summary>
    /// One place that turns devices into game intents. Same intents for PICO / Quest controllers
    /// and for keyboard + mouse. Tests and the autopilot can inject intents through <see cref="Virtual"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class Bvr2Input : MonoBehaviour
    {
        public struct VirtualInput
        {
            public bool active;
            public Vector2 move; public float vertical; public bool boost, scan, tool, gripL, gripR, anchor, menu, ability; public float field;
        }
        public VirtualInput Virtual;

        public bool IsXR => XRSettings.isDeviceActive;
        public Vector2 Move { get; private set; }
        public float Vertical { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Boost { get; private set; }
        public bool BoostDown { get; private set; }
        public bool Scan { get; private set; }
        public bool ScanDown { get; private set; }
        public bool Tool { get; private set; }
        public bool ToolDown { get; private set; }
        public bool ToolUp { get; private set; }
        public bool GripL { get; private set; }
        public bool GripR { get; private set; }
        public bool AnchorDown { get; private set; }
        public bool MenuDown { get; private set; }
        /// <summary>Switch BioTool ability: B on the right controller, Q on the keyboard.</summary>
        public bool AbilityDown { get; private set; }
        /// <summary>Desktop vascular-field change per frame (wheel / Z X keys), −1..1 scaled.</summary>
        public float FieldDelta { get; private set; }
        public int SnapTurn { get; private set; }

        InputAction move, stickR, scan, tool, gripL, gripR, boostA, boostX, menu, menuY, ability;
        bool snapArmed = true, prevBoost, prevScan, prevTool, prevGripL, prevGripR, prevMenu, prevAnchorV, prevAbility;

        void OnEnable()
        {
            move = new InputAction("move", InputActionType.Value, "<XRController>{LeftHand}/{Primary2DAxis}");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            stickR = new InputAction("stickR", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}");
            scan = Button("<XRController>{LeftHand}/{TriggerButton}", "<Mouse>/rightButton", "<Keyboard>/f");
            tool = Button("<XRController>{RightHand}/{TriggerButton}", "<Mouse>/leftButton");
            gripL = Button("<XRController>{LeftHand}/{GripButton}", "<Keyboard>/e");
            gripR = Button("<XRController>{RightHand}/{GripButton}");
            boostA = Button("<XRController>{RightHand}/{PrimaryButton}", "<Keyboard>/leftShift");
            boostX = Button("<XRController>{LeftHand}/{PrimaryButton}");
            menu = Button("<XRController>{LeftHand}/{MenuButton}", "<Keyboard>/tab");
            menuY = Button("<XRController>{LeftHand}/{SecondaryButton}");
            ability = Button("<XRController>{RightHand}/{SecondaryButton}", "<Keyboard>/q");
            foreach (var a in new[] { move, stickR, scan, tool, gripL, gripR, boostA, boostX, menu, menuY, ability }) a.Enable();
        }

        static InputAction Button(params string[] paths)
        {
            var a = new InputAction(type: InputActionType.Button);
            foreach (var p in paths) a.AddBinding(p);
            return a;
        }

        void OnDisable() { foreach (var a in new[] { move, stickR, scan, tool, gripL, gripR, boostA, boostX, menu, menuY, ability }) { a?.Disable(); a?.Dispose(); } }

        void Update()
        {
            var kb = Keyboard.current; var mouse = Mouse.current;
            Vector2 m = move.ReadValue<Vector2>();
            Vector2 r = stickR.ReadValue<Vector2>();
            float vert = Mathf.Abs(r.y) > 0.25f ? r.y : 0f;
            if (kb != null) { if (kb.spaceKey.isPressed) vert += 1; if (kb.leftCtrlKey.isPressed || kb.cKey.isPressed) vert -= 1; }
            bool boost = boostA.IsPressed() || boostX.IsPressed();
            bool sc = scan.IsPressed(), tl = tool.IsPressed(), gl = gripL.IsPressed(), gr = gripR.IsPressed(), mn = menu.IsPressed() || menuY.IsPressed();
            float field = 0;
            if (mouse != null) field += Mathf.Clamp(mouse.scroll.ReadValue().y / 120f, -1, 1) * 0.06f;
            if (kb != null) { if (kb.zKey.isPressed) field -= Time.unscaledDeltaTime * 0.5f; if (kb.xKey.isPressed) field += Time.unscaledDeltaTime * 0.5f; }
            Look = mouse != null && !IsXR ? mouse.delta.ReadValue() : Vector2.zero;
            bool anchorV = false, ab = ability.IsPressed();
            if (Virtual.active)
            {
                m += Virtual.move; vert += Virtual.vertical; boost |= Virtual.boost; sc |= Virtual.scan; tl |= Virtual.tool;
                gl |= Virtual.gripL; gr |= Virtual.gripR; mn |= Virtual.menu; field += Virtual.field; anchorV = Virtual.anchor; ab |= Virtual.ability;
            }
            Move = Vector2.ClampMagnitude(m, 1f); Vertical = Mathf.Clamp(vert, -1, 1);
            Boost = boost; BoostDown = boost && !prevBoost; prevBoost = boost;
            Scan = sc; ScanDown = sc && !prevScan; prevScan = sc;
            ToolDown = tl && !prevTool; ToolUp = !tl && prevTool; Tool = tl; prevTool = tl;
            // Anchor: grip press in VR (either hand alone), E on desktop, or virtual.
            bool glDown = gl && !prevGripL, grDown = gr && !prevGripR;
            AnchorDown = (glDown && !gr) || (grDown && !gl) || (anchorV && !prevAnchorV);
            prevAnchorV = anchorV; GripL = gl; GripR = gr; prevGripL = gl; prevGripR = gr;
            MenuDown = mn && !prevMenu; prevMenu = mn;
            AbilityDown = ab && !prevAbility; prevAbility = ab;
            FieldDelta = field;
            SnapTurn = 0;
            if (IsXR)
            {
                if (Mathf.Abs(r.x) > 0.7f && snapArmed) { SnapTurn = r.x > 0 ? 1 : -1; snapArmed = false; }
                if (Mathf.Abs(r.x) < 0.3f) snapArmed = true;
            }
        }
    }
}

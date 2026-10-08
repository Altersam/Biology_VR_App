// RECOVERY: Reconstructed from this session's captured source and patches; Z: source read failed.
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>
    /// JourneyInput polls raw XR triggers, independently of XR UI Press. Suspend just that poller
    /// while interacting with this HUD. Button methods still work on a disabled MonoBehaviour.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class JourneyUiInputGuard : MonoBehaviour
    {
        public JourneyHud hud;
        public RectTransform[] surfaces;
        public XRRayInteractor[] rays;
        public NearFarInteractor[] nearFarRays;
        bool suspended, waitForRelease;
        bool restoreInput;
        int releaseFrame;

        void Update()
        {
            if (!hud || !hud.input) { Restore(); return; }
            bool hover = XRSettings.isDeviceActive && IsOverUi();
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.triggerButton, out bool left);
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.triggerButton, out bool right);
            if (hover)
            {
                if (left || right) waitForRelease = true;
                releaseFrame = Time.frameCount + 1;
                Suspend();
            }
            else if (suspended)
            {
                if (left || right) { waitForRelease = true; releaseFrame = Time.frameCount + 1; }
                else if (waitForRelease) { waitForRelease = false; releaseFrame = Time.frameCount + 1; }
                else if (Time.frameCount > releaseFrame) Restore();
            }
        }
        bool IsOverUi()
        {
            if (hud.panelGroup && !hud.panelGroup.blocksRaycasts) return false;
            // Last XR UI result also handles curved rays and NearFarInteractor implementations.
            if (rays != null) foreach (var ray in rays)
            {
                if (!ray || !ray.isActiveAndEnabled) continue;
                if (ray.TryGetCurrentUIRaycastResult(out var hit) && Owns(hit.gameObject)) return true;
                // Check this frame's straight ray before the EventSystem update, avoiding a one-frame fire leak.
                var origin = ray.rayOriginTransform ? ray.rayOriginTransform : ray.transform;
                if (Intersects(origin)) return true;
            }
            if (nearFarRays != null) foreach (var ray in nearFarRays)
            {
                if (!ray || !ray.isActiveAndEnabled) continue;
                if (ray.TryGetCurrentUIRaycastResult(out var hit) && Owns(hit.gameObject)) return true;
            }
            // Raw tool aim is the same source used by JourneyInput, and covers first-frame hover.
            return Intersects(hud.input.scanner) || Intersects(hud.input.bioTool);
        }
        bool Owns(GameObject hit)
        {
            if (!hit || surfaces == null) return false;
            foreach (var surface in surfaces)
                if (surface && surface.gameObject.activeInHierarchy && hit.transform.IsChildOf(surface)) return true;
            return false;
        }
        bool Intersects(Transform origin)
        {
            if (!origin || surfaces == null) return false;
            Ray ray = new Ray(origin.position, origin.forward);
            foreach (var surface in surfaces)
            {
                if (!surface || !surface.gameObject.activeInHierarchy) continue;
                Plane plane = new Plane(surface.forward, surface.position);
                if (!plane.Raycast(ray, out float distance) || distance < 0 || distance > 35f) continue;
                Vector3 local = surface.InverseTransformPoint(ray.GetPoint(distance));
                if (surface.rect.Contains(new Vector2(local.x, local.y))) return true;
            }
            return false;
        }
        void Suspend()
        {
            if (suspended || !hud.input.enabled) return;
            restoreInput = true;
            hud.input.enabled = false;
            suspended = true;
        }
        void Restore()
        {
            if (suspended && restoreInput && hud && hud.input) hud.input.enabled = true;
            restoreInput = false;
            suspended = waitForRelease = false;
        }
        void OnDisable() { Restore(); }
    }
}

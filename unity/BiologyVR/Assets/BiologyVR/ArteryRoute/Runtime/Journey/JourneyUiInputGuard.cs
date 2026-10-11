// RECOVERY: Reconstructed from this session's captured source and patches; Z: source read failed.
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>
    /// Consume a UI press only for its own hand until release; keep live input and guides enabled.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class JourneyUiInputGuard : MonoBehaviour
    {
        public JourneyHud hud;
        public RectTransform[] surfaces;
        public XRRayInteractor[] rays;
        public NearFarInteractor[] nearFarRays;
        bool scanWaitForRelease,toolWaitForRelease;
        bool previousScan,previousTool;

        // JourneyInput invokes this in LateUpdate, after tracked hand poses and
        // UI layout. Early Update could latch a new press against LAST frame's
        // panel aim and keep a correctly pointed instrument blocked until release.
        public void RefreshBlocking()
        {
            if(!isActiveAndEnabled||!hud||!hud.input)return;
            var input=hud.input;bool tracked=XRSettings.isDeviceActive||input.useExplicitAimSources||input.mover&&input.mover.UsingTrackedInput;
            if(!tracked){Restore();return;}
            bool scan=input.RawScannerPressed,tool=input.RawToolPressed;
            if(!scan)scanWaitForRelease=false;if(!tool)toolWaitForRelease=false;
            if(scan&&!previousScan&&Intersects(input.scanner))scanWaitForRelease=true;
            if(tool&&!previousTool&&Intersects(input.bioTool))toolWaitForRelease=true;
            previousScan=scan;previousTool=tool;
            bool scannerBlocked=scan&&scanWaitForRelease,bioBlocked=tool&&toolWaitForRelease;
            bool newlyBlocked=scannerBlocked&&!input.ScannerBlockedByUi||bioBlocked&&!input.ToolBlockedByUi;
            input.SetUiBlocking(scannerBlocked,bioBlocked);
            if(newlyBlocked)input.mission.FeedbackMessage("Нажатие используется панелью. Отпусти Trigger, затем нажми снова для работы прибором.");
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
                if (VisibleSurface(surface) && hit.transform.IsChildOf(surface)) return true;
            return false;
        }
        bool Intersects(Transform origin)
        {
            if (!origin || surfaces == null) return false;
            Ray ray = new Ray(origin.position, origin.forward);
            foreach (var surface in surfaces)
            {
                if (!VisibleSurface(surface)) continue;
                Plane plane = new Plane(surface.forward, surface.position);
                if (!plane.Raycast(ray, out float distance) || distance < 0 || distance > 35f) continue;
                Vector3 local = surface.InverseTransformPoint(ray.GetPoint(distance));
                if (surface.rect.Contains(new Vector2(local.x, local.y)))
                {
                    // A held biological object in front of the hologram owns this
                    // aim. The infinite UI plane must not swallow its scan trigger.
                    var endpoint=hud.input.GuideEndpoint(origin,out bool contact);
                    if(contact&&Vector3.Distance(origin.position,endpoint)<distance-.02f)continue;
                    return true;
                }
            }
            return false;
        }
        static bool VisibleSurface(RectTransform surface)
        {
            if(!surface||!surface.gameObject.activeInHierarchy)return false;
            var canvas=surface.GetComponentInParent<Canvas>();if(canvas&&!canvas.isActiveAndEnabled)return false;
            foreach(var group in surface.GetComponentsInParent<CanvasGroup>())if(!group.blocksRaycasts||group.alpha<=.01f)return false;
            return true;
        }
        void Restore()
        {
            if(hud&&hud.input)hud.input.SetUiBlocking(false,false);
            scanWaitForRelease=toolWaitForRelease=previousScan=previousTool=false;
        }
        void OnDisable() { Restore(); }
    }
}

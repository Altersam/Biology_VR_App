// RECOVERY: Reconstructed from this session's captured source and patches; Z: source read failed.
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>A purely visual world-space marker. No collider, raycaster or camera-position anchor.</summary>
    public sealed class JourneyTargetMarker : MonoBehaviour
    {
        public JourneyHud hud;
        public RectTransform visual;
        public CanvasGroup group;
        public Text caption;
        JourneyTarget cachedTarget;
        Renderer[] renderers;
        Collider[] colliders;

        void LateUpdate()
        {
            if (!visual || !hud || !hud.mission || hud.mission.Goals == null) { Hide(); return; }
            var mission = hud.mission;
            var current = mission.Current;
            var target = current != null && mission.world ? mission.world.Find(current.target) : null;
            var camera = mission.mover ? mission.mover.viewCamera : null;
            if (!target || !target.gameObject.activeInHierarchy || !camera || !mission.Ready) { Hide(); return; }
            if (cachedTarget != target)
            {
                cachedTarget = target;
                renderers = target.GetComponentsInChildren<Renderer>(true);
                colliders = target.GetComponentsInChildren<Collider>(true);
                if (caption) caption.text = JourneyHud.TargetName(target.targetId);
            }
            Vector3 center = target.transform.position;
            Bounds bounds = new Bounds(center, Vector3.zero);
            bool hasBounds = false;
            foreach (var renderer in renderers)
            {
                if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if(!hasBounds)foreach(var collider in colliders)
            {
                if(!collider||!collider.enabled||!collider.gameObject.activeInHierarchy)continue;
                if(!hasBounds){bounds=collider.bounds;hasBounds=true;}
                else bounds.Encapsulate(collider.bounds);
            }
            if (hasBounds) center = bounds.center;
            float distance = Vector3.Distance(camera.transform.position, center);
            if (Vector3.Dot(camera.transform.forward, center - camera.transform.position) <= 0 || distance < .15f)
            { Hide(); return; }
            // Billboard orientation follows the viewer; position always follows the actual world target.
            // Offset toward the viewer prevents the ring being buried inside the target's rendered surface.
            Vector3 towardCamera = (camera.transform.position - center).normalized;
            float offset = hasBounds ? Mathf.Min(bounds.extents.magnitude + .035f, distance * .45f) : .035f;
            // The marker canvas already has a world-space scale assigned by the authoring
            // helper. Position and billboard the root at the real target bounds; scaling the
            // child by a second pixel-to-world factor made the ring microscopic.
            transform.SetPositionAndRotation(center + towardCamera * offset, camera.transform.rotation);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            if (group) { group.alpha = 1; group.interactable = group.blocksRaycasts = false; }
        }
        void Hide() { if (group) group.alpha = 0; }
        void OnDisable() { Hide(); }
    }
}

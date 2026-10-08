using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Identity on the same GameObject as a lipid proxy target and its collider.</summary>
    [DisallowMultipleComponent]
    public sealed class JourneyPlaqueZone : MonoBehaviour
    {
        [SerializeField] JourneyPlaqueState owner;
        [SerializeField] int zoneIndex;
        [SerializeField] JourneyTarget target;
        [SerializeField] Collider contact;
        [SerializeField] Transform visual;
        [SerializeField] Renderer visualRenderer;
        [SerializeField] Vector3 visualScale;

        public JourneyPlaqueState Owner => owner;
        public int ZoneIndex => zoneIndex;
        public JourneyTarget Target => target;
        public Collider Contact => contact;
        public Transform Visual => visual;
        public Renderer VisualRenderer => visualRenderer;
        public Vector3 VisualScale => visualScale;
        public bool Processed => owner && owner.IsZoneProcessed(zoneIndex);

        public void Configure(JourneyPlaqueState state, int index, JourneyTarget proxy,
            Collider collider, Transform model, Renderer renderer)
        {
            owner = state;
            zoneIndex = index;
            target = proxy;
            contact = collider;
            visual = model;
            visualRenderer = renderer;
            visualScale = model ? model.localScale : Vector3.one;
        }
    }
}

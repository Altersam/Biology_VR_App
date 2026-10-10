using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyPlaqueLipid:MonoBehaviour
    {
        public JourneyPlaqueChallenge owner;
        public int index;
        public JourneyTarget target;
        public Collider contact;
        public Renderer surface;
    }
}

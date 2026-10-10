using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Magnified Hb/heme/O2 travel with their carrier, rather than in a static teaching dock.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class JourneyBloodCargo:MonoBehaviour
    {
        public JourneyTarget target,carrier;
        public Vector3 displayOffset;
        public bool detached;
        Vector3 velocity;
        public bool Extracted=>target&&(target.grabbed||target.DesktopHeld)||carrier&&(carrier.grabbed||carrier.DesktopHeld||carrier.GetComponent<JourneyBloodCargo>()?.Extracted==true);
        void LateUpdate()
        {
            if(!target||!carrier||!target.mission||target.mission.SceneNumber!=3||target.grabbed||target.DesktopHeld)return;
            var camera=target.mission.mover.viewCamera.transform;
            var offset=displayOffset+(detached?Vector3.right*.25f:Vector3.zero);
            var destination=carrier.transform.position+camera.rotation*offset;
            transform.position=Vector3.SmoothDamp(transform.position,destination,ref velocity,.10f,8,Time.deltaTime);
            transform.rotation=Quaternion.Slerp(transform.rotation,camera.rotation,1-Mathf.Exp(-Time.deltaTime*10));
        }
    }
}

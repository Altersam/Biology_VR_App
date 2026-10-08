using UnityEngine;
namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Visual-only atmospheric grading; no movement, input, targets or biology state.</summary>
    [DefaultExecutionOrder(3000)]
    public sealed class ArteryVisualEnvironmentV8:MonoBehaviour
    {
        public JourneyWorld world;
        void LateUpdate()
        {
            if(!world||!world.mission||world.mission.SceneNumber<3||world.mission.SceneNumber>16)return;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(.80f,.71f,.66f);RenderSettings.fogStartDistance=35;RenderSettings.fogEndDistance=85;
            world.mover.viewCamera.backgroundColor=RenderSettings.fogColor;
            world.ApplyLateLocationCulling();
            if(world.art&&world.art.wallChunks!=null)
            {
                var point=world.mover.viewCamera.transform.position;float range=world.mover.viewCamera.farClipPlane+18;
                foreach(var renderer in world.art.wallChunks)if(renderer)renderer.enabled=(renderer.bounds.ClosestPoint(point)-point).sqrMagnitude<range*range;
            }
        }
    }
}

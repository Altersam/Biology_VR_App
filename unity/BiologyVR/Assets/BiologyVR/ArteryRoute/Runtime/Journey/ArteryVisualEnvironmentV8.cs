using UnityEngine;
namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Visual-only atmospheric grading; no movement, input, targets or biology state.</summary>
    [DefaultExecutionOrder(3000)]
    public sealed class ArteryVisualEnvironmentV8:MonoBehaviour
    {
        public JourneyWorld world;
        public Color fogColor=new Color(.80f,.71f,.66f);
        public float fogStart=35,fogEnd=85;
        public bool localPathologyTint;
        MaterialPropertyBlock visualBlock;
        void LateUpdate()
        {
            if(!world||!world.mission||world.mission.SceneNumber<3||world.mission.SceneNumber>16)return;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=fogColor;RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;
            world.mover.viewCamera.backgroundColor=RenderSettings.fogColor;
            world.ApplyLateLocationCulling();
            if(world.art&&world.art.wallChunks!=null)
            {
                var point=world.mover.viewCamera.transform.position;float range=world.mover.viewCamera.farClipPlane+18;
                foreach(var renderer in world.art.wallChunks)if(renderer)renderer.enabled=(renderer.bounds.ClosestPoint(point)-point).sqrMagnitude<range*range;
                if(localPathologyTint)
                {
                    if(visualBlock==null)visualBlock=new MaterialPropertyBlock();
                    var path=world.mover.path;
                    for(int i=0;i<world.art.wallChunks.Length;i++)
                    {
                        var renderer=world.art.wallChunks[i];if(!renderer)continue;
                        float arc=world.art.chunkDistances[i];
                        float plaque=world.plaqueState?world.plaqueState.PathologyFraction:0;
                        float plaqueWeight=Mathf.Exp(-Mathf.Pow((arc-path.Anchor(6)-4)/8,2))*plaque;
                        float woundWeight=world.mission.SceneNumber>=11?Mathf.Exp(-Mathf.Pow((arc-path.Anchor(11)-5)/7,2))*(1-world.RepairGrowth):0;
                        Color tint=Color.Lerp(Color.white,new Color(.97f,.89f,.90f),Mathf.Max(plaqueWeight,woundWeight));
                        renderer.GetPropertyBlock(visualBlock);visualBlock.SetColor("_ZoneTint",tint);renderer.SetPropertyBlock(visualBlock);
                    }
                }
            }
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute
{
    /// <summary>
    /// Lightweight runtime profile shared by Meta Quest and Pico 4 Enterprise.
    /// The scene remains the same lesson; only mobile-safe rendering limits change.
    /// </summary>
    public sealed class QuestPicoPerformanceProfile : MonoBehaviour
    {
        public int mobileBloodCells=48;
        public bool disablePostProcessing=true;
        public bool disableRealtimeShadows=true;
        void Start()
        {
            if(!Application.isMobilePlatform)return;
            var camera=Camera.main;
            if(camera)
            {
                var data=camera.GetUniversalAdditionalCameraData();
                if(disablePostProcessing)data.renderPostProcessing=false;
            }
            QualitySettings.vSyncCount=0;
            QualitySettings.antiAliasing=2;
            var flow=FindFirstObjectByType<JourneyBloodFlow>();
            if(flow)flow.SetVisibleCellLimit(mobileBloodCells);
            foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if(disableRealtimeShadows)light.shadows=LightShadows.None;
                if(light.type==LightType.Point||light.type==LightType.Spot)light.intensity*=.55f;
            }
            foreach(var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
        }
    }
}

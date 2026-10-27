using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace BiologyVR.V2
{
    /// <summary>
    /// Safety net for standalone headsets (PICO / Quest builds only): if the frame rate stays
    /// clearly below the display rate, the render scale steps down 1.0 → 0.88 → 0.78.
    /// It never steps back up within a session, so the image does not pulse.
    /// Added by <see cref="Bvr2Game"/> at runtime; does nothing in the editor or on desktop.
    /// </summary>
    public sealed class Bvr2MobileQuality : MonoBehaviour
    {
        static readonly float[] Steps = { 1f, 0.88f, 0.78f };
        int step; float acc, settle = 8f; int frames;

        public static void Attach(GameObject host)
        {
            if (Application.isEditor || !Application.isMobilePlatform) return;
            if (!host.GetComponent<Bvr2MobileQuality>()) host.AddComponent<Bvr2MobileQuality>();
        }

        static UniversalRenderPipelineAsset Asset => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        void Start()
        {
            // no realtime shadows on the headset: the scene is lit in the shaders anyway
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) l.shadows = LightShadows.None;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (settle > 0) { settle -= dt; acc = 0; frames = 0; return; } // skip loading spikes
            acc += dt; frames++;
            if (acc < 4f) return;
            float fps = frames / acc; acc = 0; frames = 0;
            float display = XRDevice.refreshRate > 30f ? XRDevice.refreshRate : 72f;
            var asset = Asset;
            if (asset && fps < display * 0.86f && step < Steps.Length - 1)
            {
                step++; asset.renderScale = Steps[step]; settle = 3f;
                Debug.Log($"[BVR2] {fps:F0} fps at {display:F0} Hz — render scale {Steps[step]:F2}");
            }
        }
    }
}

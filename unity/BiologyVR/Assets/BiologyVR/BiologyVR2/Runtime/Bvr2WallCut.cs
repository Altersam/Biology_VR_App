using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// The wall-layer cut in the real artery wall (chapter 1): placed at the vessel centre of the study point
    /// (Z = downstream, X = towards the window) and scaled across the vessel like the endothelium's vertex shader —
    /// zone narrowing × the travelling pulse bulge — so the layers stay flush with the wall around the window.
    /// </summary>
    public sealed class Bvr2WallCut : MonoBehaviour
    {
        public Bvr2Vessel vessel; public Bvr2Organism organism;
        float s;

        public void Place(Bvr2Vessel v, Bvr2Organism o, float sAt, float angleDeg)
        {
            vessel = v; organism = o; s = sAt;
            v.Frame(s, out var c, out var t, out _, out var b);
            transform.position = c;
            transform.rotation = Quaternion.LookRotation(t, b) * Quaternion.AngleAxis(angleDeg, Vector3.forward);
            Update();
        }

        void Update()
        {
            if (!vessel) return;
            float phase = organism ? organism.HeartPhase : Time.time * 1.2f;
            float k = vessel.ZoneScale(s) * (1f + 0.035f * Bvr2Math.PulseShape(phase - s / 70f)) * vessel.BaseRadius(s) / Mathf.Max(vessel.baseRadius, 0.01f);
            transform.localScale = new Vector3(k, k, 1f);
        }
    }
}

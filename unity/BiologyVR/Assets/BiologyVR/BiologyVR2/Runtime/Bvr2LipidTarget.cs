using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// A lipid droplet of the plaque core showing through a thin spot of the fibrous cap.
    /// Between pulse waves the "window" is open (core glows, ring wide); while the pulse wave
    /// passes, the stretched cap closes over it and a shot would hit the cap instead.
    /// </summary>
    public sealed class Bvr2LipidTarget : Bvr2ImpulseTarget
    {
        public Bvr2Organism organism;
        public Transform core, cap, ring;
        public Renderer coreRenderer, ringRenderer;
        public float s;                    // arc length of the target (travelling pulse)
        public float angleDeg;             // where on the plaque it sits (re-seated when the plaque shrinks)
        [Tooltip("Pulse shape above this value = window closed.")]
        public float closedAbove = 0.12f;
        bool alive = true, open;
        public override bool Alive => alive;
        public override bool Open => open;
        public override bool OpenWithGrace => open || WindowOpen(organism, s, 0.22f);
        /// <summary>Open, but the next pulse wave is about to arrive (ring turns yellow).</summary>
        public bool Closing { get; private set; }
        public event Action<Bvr2LipidTarget> OnMelted;

        float openK, melt; Vector3 coreScale, capScale, ringScale; MaterialPropertyBlock mpb;


        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            if (core) coreScale = core.localScale; if (cap) capScale = cap.localScale; if (ring) ringScale = ring.localScale;
        }

        /// <summary>Back to the initial state (chapter restart).</summary>
        public void ResetTarget()
        {
            alive = true; melt = 0; gameObject.SetActive(true);
            if (core) core.localScale = coreScale; if (ring) ring.gameObject.SetActive(true);
        }

        /// <summary>Window state from the travelling pulse wave at this target, optionally a little in the future.</summary>
        public static bool WindowOpen(Bvr2Organism o, float s, float closedAbove = 0.12f, float aheadSeconds = 0f)
        {
            if (!o) return true;
            return Bvr2Math.PulseShape(o.HeartPhase + o.heartRate / 60f * aheadSeconds - s / 70f) < closedAbove;
        }

        /// <summary>Position in the local pulse cycle, 0 = wave front arrives.</summary>
        public static float WindowPhase(Bvr2Organism o, float s) => o ? Mathf.Repeat(o.HeartPhase - s / 70f, 1f) : 0.5f;

        public override void Hit(bool isOpen)
        {
            if (!alive) return;
            if (isOpen) { alive = false; melt = 0.001f; OnMelted?.Invoke(this); }
        }

        void Update()
        {
            if (melt > 0)
            {
                melt += Time.deltaTime / 0.45f;
                if (core) core.localScale = coreScale * Mathf.Max(0.01f, 1f - melt);
                if (cap) cap.localScale = capScale * Mathf.Max(0.01f, 1f - melt);
                if (ring) ring.gameObject.SetActive(false);
                if (melt >= 1f) { melt = 0; gameObject.SetActive(false); }
                return;
            }
            open = WindowOpen(organism, s, closedAbove);
            Closing = open && WindowPhase(organism, s) > 0.82f;
            openK = Mathf.MoveTowards(openK, open ? 1f : 0f, Time.deltaTime * (open ? 7f : 14f));
            // closed: the cap shell covers the droplet; open: it pulls back and the droplet glows
            if (cap) cap.localScale = capScale * Mathf.Lerp(1f, 0.55f, openK);
            if (core) core.localScale = coreScale * (0.92f + 0.08f * openK + 0.04f * Mathf.Sin(Time.time * 7f + s));
            if (ring) ring.localScale = ringScale * Mathf.Lerp(0.7f, 1.15f, openK);
            if (coreRenderer) { mpb.Clear(); mpb.SetColor("_Emission", new Color(1f, 0.7f, 0.15f) * Mathf.Lerp(0.08f, 0.75f, openK)); coreRenderer.SetPropertyBlock(mpb); }
            if (ringRenderer)
            {
                var oc = Closing ? new Color(1f, 0.85f, 0.25f, 0.95f) : new Color(0.5f, 1f, 0.55f, 0.95f); // yellow: the wave is coming
                mpb.Clear(); mpb.SetColor("_Color", Color.Lerp(new Color(1f, 0.3f, 0.15f, 0.25f), oc, openK)); ringRenderer.SetPropertyBlock(mpb);
            }
        }
    }
}

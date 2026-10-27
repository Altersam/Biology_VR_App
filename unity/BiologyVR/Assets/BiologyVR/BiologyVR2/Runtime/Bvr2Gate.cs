using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>Energy ring in the lumen. Swim through it: shock ring, sparks, chime. Used for the
    /// movement tutorial and the race through the spasm.</summary>
    public sealed class Bvr2Gate : MonoBehaviour
    {
        public Renderer ring, inner;
        public float radius = 1.7f;
        public float s;
        public bool passed, isNext;
        public event Action<Bvr2Gate> OnPassed;
        Vector3 lastLocal; bool hasLast; MaterialPropertyBlock mpb; float passedAt = -1;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        public void ForcePass() { if (passed) return; passed = true; passedAt = Time.time; OnPassed?.Invoke(this); }

        public void ResetGate() { passed = false; passedAt = -1; hasLast = false; gameObject.SetActive(true); transform.localScale = Vector3.one * radius; }

        public void Track(Vector3 head)
        {
            if (passed) return;
            var local = transform.InverseTransformPoint(head);
            if (hasLast && Mathf.Sign(local.z) != Mathf.Sign(lastLocal.z) && lastLocal.z < 0)
            {
                Vector2 a = new Vector2(local.x, local.y), b = new Vector2(lastLocal.x, lastLocal.y);
                if (((a + b) * 0.5f).magnitude < 1.15f) { passed = true; passedAt = Time.time; OnPassed?.Invoke(this); }
            }
            lastLocal = local; hasLast = true;
        }

        void Update()
        {
            if (mpb == null) return;
            float now = Time.time;
            Color c;
            if (passed)
            {
                float t = (now - passedAt) / 0.6f;
                transform.localScale = Vector3.one * radius * (1f + t * 0.8f);
                c = Bvr2Style.Good; c.a = Mathf.Clamp01(1f - t);
                if (t >= 1f) { gameObject.SetActive(false); return; }
            }
            else if (isNext) { c = Bvr2Style.Knowledge; c.a = 0.75f + 0.25f * Mathf.Sin(now * 6f); transform.localScale = Vector3.one * radius * (1f + 0.04f * Mathf.Sin(now * 6f)); }
            else { c = Bvr2Style.Ally; c.a = 0.35f; }
            mpb.SetColor("_Color", c);
            if (ring) ring.SetPropertyBlock(mpb);
            if (inner) { var ci = c; ci.a *= 0.18f; mpb.SetColor("_Color", ci); inner.SetPropertyBlock(mpb); }
        }
    }
}

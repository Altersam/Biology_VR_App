using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// BioTool ability "Импульс": a short, precise bolt. It dissolves lipid where it lands; a hit on the
    /// fibrous cap of a plaque damages the cap. The bolt flies for a moment, so the shot has to be timed
    /// to the heartbeat (lipid windows close while the pulse wave stretches the cap).
    /// </summary>
    public sealed class Bvr2Impulse : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Vessel vessel; public Bvr2Fx fx; public Bvr2Audio audioDirector;
        public Transform tip;
        public bool active;
        public float range = 30f, cooldown = 0.32f, boltSpeed = 90f, aimAssistDeg = 3.2f;
        public Mesh boltMesh; public Material boltMaterial;

        /// <summary>Every target that can be hit (lipid droplets register themselves).</summary>
        public static readonly List<Bvr2ImpulseTarget> Targets = new List<Bvr2ImpulseTarget>();
        public event Action OnFire;
        /// <summary>A target was reached: (target, window open at the moment of impact).</summary>
        public event Action<Bvr2ImpulseTarget, bool> OnHitTarget;
        /// <summary>The wall or a plaque was hit: (point, plaque inset at that point in metres).</summary>
        public event Action<Vector3, float> OnHitWall;

        float nextShot; MaterialPropertyBlock mpb;
        public int Shots { get; private set; }

        void Awake() { mpb = new MaterialPropertyBlock(); }

        void Update()
        {
            if (!active || !input || !rig || !rig.rightHand) return;
            if (input.ToolDown && Time.time >= nextShot) Fire();
        }

        /// <summary>Fire along the BioTool (also used by tests with an explicit direction).</summary>
        public void Fire(Vector3? direction = null)
        {
            var hand = rig.rightHand; Vector3 origin = tip ? tip.position : hand.position;
            Vector3 dir = direction ?? hand.forward;
            nextShot = Time.time + cooldown; Shots++;
            // aim assist toward the target nearest to the aim line (in front of the wall)
            Bvr2ImpulseTarget best = null; float bestScore = 1f;
            float wallDist = range; Vector3 wallHit = origin + dir * range; float inset = 0;
            if (vessel && vessel.RaycastWall(new Ray(origin, dir), range, out var h, out _, out var ins)) { wallHit = h; wallDist = Vector3.Distance(origin, h); inset = ins; }
            foreach (var t in Targets)
            {
                if (!t || !t.Alive) continue;
                Vector3 to = t.transform.position - origin; float d = to.magnitude; if (d > range || d < 0.2f) continue;
                if (d > wallDist + t.radius + 0.8f) continue; // hidden behind the wall
                float allowed = aimAssistDeg + Mathf.Atan2(t.radius, d) * Mathf.Rad2Deg;
                float score = Vector3.Angle(dir, to) / allowed;
                if (score < bestScore) { bestScore = score; best = t; }
            }
            Vector3 end = best ? best.transform.position : wallHit;
            StartCoroutine(Fly(origin, end, best, inset));
            fx?.Burst(origin + dir * 0.05f, new Color(1f, 0.85f, 0.4f), 8, 1.2f, 0.025f, 0.25f, dir * 2f);
            audioDirector?.Play("BVR2_Impulse", 0.8f, UnityEngine.Random.Range(0.95f, 1.08f));
            Bvr2Haptics.Pulse(false, 0.35f, 0.05f);
            OnFire?.Invoke();
        }

        IEnumerator Fly(Vector3 from, Vector3 to, Bvr2ImpulseTarget target, float inset)
        {
            float dist = Vector3.Distance(from, to), t = 0, dur = Mathf.Max(0.05f, dist / boltSpeed);
            Transform bolt = null;
            if (boltMesh && boltMaterial)
            {
                bolt = new GameObject("Impulse Bolt").transform;
                bolt.gameObject.AddComponent<MeshFilter>().sharedMesh = boltMesh;
                var r = bolt.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = boltMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mpb.SetColor("_Color", new Color(1f, 0.82f, 0.35f)); r.SetPropertyBlock(mpb);
                bolt.localScale = new Vector3(0.05f, 0.05f, 0.45f);
                bolt.rotation = Quaternion.LookRotation(to - from);
            }
            var beam = fx ? fx.GetBeam() : null;
            while (t < dur)
            {
                t += Time.deltaTime; float k = Mathf.Clamp01(t / dur);
                Vector3 p = Vector3.Lerp(from, to, k);
                if (bolt) bolt.position = p;
                if (beam != null) Bvr2Fx.ShapeBeam(beam, Vector3.Lerp(from, to, Mathf.Max(0, k - 0.25f)), (to - from).normalized, p, new Color(1f, 0.8f, 0.35f, 0.8f), 0.02f, 0f);
                yield return null;
            }
            if (bolt) Destroy(bolt.gameObject);
            if (beam != null) fx.ReleaseBeam(beam);
            if (target && target.Alive)
            {
                // a small grace: the window still counts as open while the wave is only starting to stretch the cap
                bool open = target.OpenWithGrace;
                target.Hit(open);
                OnHitTarget?.Invoke(target, open);
            }
            else
            {
                fx?.Burst(to, inset > 0.3f ? new Color(1f, 0.9f, 0.75f) : new Color(1f, 0.5f, 0.45f), 14, 1.6f, 0.035f, 0.45f);
                OnHitWall?.Invoke(to, inset);
            }
        }
    }
}

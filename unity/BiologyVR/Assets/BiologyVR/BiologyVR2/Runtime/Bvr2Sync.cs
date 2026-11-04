using System;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// "Синхронизация" — the hands become immune cells. As a phagocyte (chapters 3 and 5) each hand holds a
    /// macrophage: swing it through virions to engulf them (a mini slasher). A fast swing is a "slash": a bigger
    /// burst, a whoosh and +1 combo; a slow touch still engulfs. Viruses tagged by antibodies (opsonised) are
    /// caught from almost twice as far — the biology of opsonisation as a feel. A cell holds <see cref="stomach"/>
    /// virions, then digests for a moment (it glows and cannot eat), so both hands get used. Armoured "hedgehogs"
    /// bounce off until specific antibodies neutralise them. As a T-killer (chapter 5) the hands only glow and
    /// the chapter handles touching infected cells. The view is seen through a light membrane; tools are put away.
    /// </summary>
    public sealed class Bvr2Sync : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Immune immune; public Bvr2Fx fx; public Bvr2Audio audioDirector; public Bvr2Swimmer swimmer;
        public GameObject[] hideWhileSynced;
        public Mesh blobMesh; public Material blobMaterial; public Material trailMaterial;
        [Header("Phagocyte hands")]
        public float cellRadius = 0.085f, reachHand = 0.45f, reachHead = 0.5f, taggedReach = 1.8f, slashSpeed = 1.6f, comboWindow = 1.8f;
        public int stomach = 8; public float digestSeconds = 1.3f;
        [Header("Reach assist (04.11)")]
        [Tooltip("Virions this close in front of a hand are drawn to it (VR / PC).")] public float assistRange = 1.5f, assistRangeDesktop = 2.6f;
        public float assistSpeed = 2.8f;
        [Tooltip("While synchronised the player may come this close to the wall (virions settle there).")] public float wallMarginSynced = 0.5f;
        float wallMarginBefore = -1f;
        [Tooltip("Chapter 5: the player becomes a T-killer — faster, kills infected cells (the chapter handles the touch), does not eat virions.")]
        public bool tKiller;
        public bool Active { get; private set; }
        public bool Endless { get; private set; }
        public float TimeLeft { get; private set; }
        public int Eaten { get; private set; }
        public int Slashes { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Bounces { get; private set; }
        /// <summary>Test probe: are the living virions where a player can get at them? A virion is out of reach when it
        /// lies outside the swim region or deeper in the wall than the closest swim point plus an arm and the hand reach.</summary>
        public string ReachReport
        {
            get
            {
                if (!immune || !swimmer || !swimmer.vessel) return "";
                var vessel = swimmer.vessel; int alive = 0, outside = 0, deep = 0; float worst = 0f;
                bool region = swimmer.regionMax > swimmer.regionMin;
                foreach (var v in immune.viruses)
                {
                    if (!v || !v.gameObject.activeSelf || !v.Alive) continue;
                    alive++;
                    float sv = vessel.Locate(v.transform.position, ref probeHint, out var radial);
                    if (region && (sv < swimmer.regionMin - 0.6f || sv > swimmer.regionMax + 0.6f)) outside++;
                    float limit = vessel.FreeRadius(sv, vessel.AngleOf(sv, radial)) - swimmer.wallMargin;
                    float gap = radial.magnitude - limit;
                    if (gap > 0.6f + reachHand) deep++;
                    worst = Mathf.Max(worst, gap);
                }
                return $"alive {alive} · outside region {outside} · beyond arm {deep} · worst {worst:F2} m · margin {swimmer.wallMargin:F2}";
            }
        }
        int probeHint = -1;
        /// <summary>A virion was engulfed: (virus, it was a fast slash, by the left hand).</summary>
        public event Action<Bvr2Virus, bool, bool> OnEat;
        public event Action<Bvr2Virus> OnBounce;
        public event Action<int> OnComboMilestone;

        sealed class Hand
        {
            public Transform t, blob; public Renderer r; public TrailRenderer trail; public bool left;
            public Vector3 prev, vel; public int fill; public float digest; public bool hasPrev;
        }
        readonly Hand[] hands = new Hand[2];
        readonly Dictionary<Bvr2Virus, float> bounced = new Dictionary<Bvr2Virus, float>();
        MaterialPropertyBlock mpb; float maxSwimBefore, lastEat = -10f;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        Hand MakeHand(Transform parent, bool left)
        {
            var go = new GameObject(left ? "Hand Cell L" : "Hand Cell R"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = blobMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = blobMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; go.transform.localPosition = new Vector3(0, 0, 0.09f); go.transform.localScale = Vector3.one * cellRadius * 2f;
            TrailRenderer trail = null;
            if (trailMaterial)
            {
                trail = go.AddComponent<TrailRenderer>(); trail.sharedMaterial = trailMaterial; trail.time = 0.22f; trail.minVertexDistance = 0.02f;
                trail.widthMultiplier = cellRadius * 2.2f; trail.widthCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
                var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0f, 1) });
                trail.colorGradient = g; trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.emitting = false;
            }
            go.SetActive(false);
            return new Hand { t = parent, blob = go.transform, r = r, trail = trail, left = left };
        }

        /// <summary>Starts the synchronisation; seconds ≤ 0 = until <see cref="End"/> (the chapter decides).</summary>
        public void Begin(float seconds)
        {
            if (hands[0] == null) { hands[0] = MakeHand(rig.leftHand, true); hands[1] = MakeHand(rig.rightHand, false); }
            Active = true; Endless = seconds <= 0; TimeLeft = Endless ? 0 : seconds; Eaten = 0; Slashes = 0; Combo = 0; BestCombo = 0; Bounces = 0; lastEat = -10f; bounced.Clear();
            foreach (var g in hideWhileSynced) if (g) g.SetActive(false);
            foreach (var h in hands) { h.blob.gameObject.SetActive(true); h.fill = 0; h.digest = 0; h.hasPrev = false; if (h.trail) { h.trail.Clear(); h.trail.emitting = false; } }
            if (swimmer) { maxSwimBefore = swimmer.maxSwim; swimmer.maxSwim *= tKiller ? 1.5f : 0.95f; wallMarginBefore = swimmer.wallMargin; swimmer.wallMargin = Mathf.Min(swimmer.wallMargin, wallMarginSynced); }
            rig.Fade(Color.white, 0.9f, 50f); rig.FadeTo(Color.white, 0f, 1.2f);
            audioDirector?.Play("BVR2_Sync", 1f);
        }

        public void End()
        {
            if (!Active) return;
            Active = false; TimeLeft = 0; Endless = false;
            foreach (var g in hideWhileSynced) if (g) g.SetActive(true);
            foreach (var h in hands) if (h != null) { if (h.trail) { h.trail.emitting = false; h.trail.Clear(); } h.blob.gameObject.SetActive(false); }
            if (swimmer && maxSwimBefore > 0) swimmer.maxSwim = maxSwimBefore;
            if (swimmer && wallMarginBefore > 0) { swimmer.wallMargin = wallMarginBefore; wallMarginBefore = -1f; }
            rig.SetOverlay(Color.clear, 0f);
            rig.Fade(Color.white, 0.8f, 50f); rig.FadeTo(Color.white, 0f, 1.5f);
        }

        /// <summary>Tests / autopilot: engulf this virion as if a hand swung through it.</summary>
        public bool ForceEat(Bvr2Virus v, bool slash = true)
        {
            if (!Active || tKiller || hands[1] == null || !v || !v.Alive || v.Engulfer) return false;
            if (v.armored && !v.Neutralized) { Bounce(v, hands[1]); return false; }
            Eat(v, hands[1], slash); return true;
        }

        void Update()
        {
            if (!Active) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            if (!Endless) TimeLeft -= dt;
            float t = Time.time;
            // a light membrane around the view (the battlefield must stay readable)
            rig.SetOverlay(tKiller ? new Color(0.55f, 0.65f, 1f, 1f) : new Color(0.85f, 0.92f, 0.98f, 1f), (tKiller ? 0.45f : 0.3f) + 0.06f * Mathf.Sin(t * 2.2f));
            foreach (var h in hands)
            {
                Vector3 p = h.blob.position;
                if (h.hasPrev) h.vel = Vector3.Lerp(h.vel, (p - h.prev) / dt, 1f - Mathf.Exp(-dt * 25f)); else h.vel = Vector3.zero;
                float speed = h.vel.magnitude;
                if (h.digest > 0) { h.digest -= dt; if (h.digest <= 0) h.fill = 0; }
                float full = Mathf.Clamp01(h.fill / (float)Mathf.Max(1, stomach));
                float breathe = 1f + 0.12f * Mathf.Sin(t * 5f + (h.left ? 0 : 1.7f));
                bool xr = UnityEngine.XR.XRSettings.isDeviceActive; // desktop hands sit right under the camera: smaller, a bit further out
                h.blob.localPosition = new Vector3(0, 0, xr ? 0.09f : 0.22f);
                h.blob.localScale = Vector3.one * cellRadius * 2f * breathe * (1f + 0.35f * full) * (xr ? 1f : 0.6f);
                mpb.Clear();
                mpb.SetVector("_Inst", new Vector4(0.6f, tKiller ? 0.6f : (h.digest > 0 ? 0.9f : 0.15f + 0.4f * full), 0.8f, 0.3f));
                if (tKiller) mpb.SetColor("_BaseColor", new Color(0.7f, 0.78f, 1f));
                else if (h.digest > 0) mpb.SetColor("_BaseColor", new Color(1f, 0.92f, 0.7f));
                h.r.SetPropertyBlock(mpb);
                if (h.trail)
                {
                    h.trail.emitting = speed > 0.9f;
                    var g = h.trail.colorGradient; var c = tKiller ? new Color(0.7f, 0.8f, 1f) : speed > slashSpeed ? new Color(1f, 0.9f, 0.5f) : new Color(0.55f, 0.95f, 1f);
                    g.SetKeys(new[] { new GradientColorKey(c, 0), new GradientColorKey(c, 1) }, g.alphaKeys); h.trail.colorGradient = g;
                }
            }
            if (!tKiller) { Assist(dt); Engulf(); }
            foreach (var h in hands) { h.prev = h.blob.position; h.hasPrev = true; }
            if (Combo > 0 && Time.time - lastEat > comboWindow) Combo = 0;
            if (!Endless && TimeLeft <= 0) End();
        }

        /// <summary>
        /// Reach assist: a virion close in front of a free hand is drawn to it (the phagocyte's pseudopodia), and one
        /// settling on the wall is pulled off it — the player does not have to hit it to the centimetre or squeeze
        /// against the wall. Bare armoured hedgehogs are not drawn (they must be tagged first).
        /// </summary>
        void Assist(float dt)
        {
            bool xr = UnityEngine.XR.XRSettings.isDeviceActive;
            float range = xr ? assistRange : assistRangeDesktop;
            if (range <= 0f) return;
            var head = rig.head.transform;
            foreach (var v in immune.viruses)
            {
                if (!v || !v.gameObject.activeSelf || !v.Alive || v.Engulfer || v.debris || (v.armored && !v.Neutralized)) continue;
                Vector3 p = v.transform.position;
                if (Vector3.Angle(head.forward, p - head.position) > 80f) continue; // only what the player is facing
                Hand best = null; float bd = range * (v.Current == Bvr2Virus.State.Infecting ? 1.3f : 1f);
                foreach (var h in hands) { if (h == null || h.digest > 0) continue; float d = Vector3.Distance(p, h.blob.position); if (d < bd) { bd = d; best = h; } }
                if (best == null) continue;
                float k = 1f - Mathf.Clamp01(bd / range);
                v.PullToward(best.blob.position, assistSpeed * dt * (0.35f + 0.65f * k));
            }
        }

        void Engulf()
        {
            Vector3 head = rig.head.transform.position;
            bool xrNow = UnityEngine.XR.XRSettings.isDeviceActive;
            var list = immune.viruses;
            for (int i = 0; i < list.Count; i++)
            {
                var v = list[i];
                if (!v || !v.gameObject.activeSelf || !v.Alive || v.Engulfer || v.debris) continue;
                Vector3 p = v.transform.position;
                Hand by = null; float best = float.MaxValue;
                foreach (var h in hands)
                {
                    if (h.digest > 0) continue;
                    // swept test along the hand's path this frame: a fast swing cannot pass through a virion
                    Vector3 a = h.hasPrev ? h.prev : h.blob.position, b = h.blob.position;
                    float d = DistanceToSegment(p, a, b);
                    float reach = (reachHand + v.radius * 0.5f) * (v.Tagged ? taggedReach : 1f) * (v.Current == Bvr2Virus.State.Infecting ? 1.4f : 1f);
                    if (d < reach && d < best) { best = d; by = h; }
                }
                float rh = xrNow ? reachHead : Mathf.Max(reachHead, 0.9f); // PC: the hands sit under the camera
                if (by == null && (p - head).sqrMagnitude < rh * rh) by = hands[1].digest > 0 ? (hands[0].digest > 0 ? null : hands[0]) : hands[1];
                if (by == null) continue;
                if (v.armored && !v.Neutralized) { Bounce(v, by); continue; }
                Eat(v, by, by.vel.magnitude > slashSpeed);
            }
        }

        void Eat(Bvr2Virus v, Hand h, bool slash)
        {
            v.Absorb(h.blob); Eaten++; h.fill++;
            if (slash) Slashes++;
            Combo = Time.time - lastEat < comboWindow ? Combo + 1 : 1; lastEat = Time.time;
            if (Combo > BestCombo) BestCombo = Combo;
            var col = v.Tagged ? Bvr2Style.Knowledge : Bvr2Style.Ally;
            fx?.Burst(v.transform.position, col, slash ? 26 : 12, slash ? 3.2f : 1.6f, 0.05f, 0.5f);
            if (slash) fx?.Shock(v.transform.position, h.vel.sqrMagnitude > 1e-4f ? h.vel.normalized : Vector3.up, col, 0.1f, 1.1f, 0.35f);
            audioDirector?.Play(slash ? "BVR2_Swish" : "BVR2_Gulp", slash ? 0.85f : 0.8f, UnityEngine.Random.Range(0.92f, 1.12f) + Mathf.Min(Combo, 10) * 0.02f);
            Bvr2Haptics.Pulse(h.left, slash ? 0.75f : 0.45f, slash ? 0.1f : 0.06f);
            if (h.fill >= stomach) { h.digest = digestSeconds; audioDirector?.Play("BVR2_Gulp", 0.9f, 0.7f); Bvr2Haptics.Pulse(h.left, 0.3f, 0.3f); }
            if (Combo > 0 && Combo % 5 == 0) { audioDirector?.Play("BVR2_Combo", 0.8f, 1f + Mathf.Min(Combo, 20) * 0.01f); OnComboMilestone?.Invoke(Combo); }
            OnEat?.Invoke(v, slash, h.left);
        }

        void Bounce(Bvr2Virus v, Hand h)
        {
            if (bounced.TryGetValue(v, out var at) && Time.time - at < 0.6f) return;
            bounced[v] = Time.time; Bounces++;
            Vector3 dir = v.transform.position - h.blob.position; if (dir.sqrMagnitude < 1e-4f) dir = h.vel; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.up;
            v.Knock(dir.normalized * 1.4f);
            fx?.Burst(v.transform.position, new Color(1f, 0.85f, 0.4f), 14, 2.6f, 0.03f, 0.35f);
            audioDirector?.Play("BVR2_Clang", 0.8f, UnityEngine.Random.Range(0.9f, 1.1f));
            Bvr2Haptics.Pulse(h.left, 0.9f, 0.08f);
            OnBounce?.Invoke(v);
        }

        static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a; float l = ab.sqrMagnitude;
            if (l < 1e-8f) return Vector3.Distance(p, a);
            float k = Mathf.Clamp01(Vector3.Dot(p - a, ab) / l);
            return Vector3.Distance(p, a + ab * k);
        }
    }
}

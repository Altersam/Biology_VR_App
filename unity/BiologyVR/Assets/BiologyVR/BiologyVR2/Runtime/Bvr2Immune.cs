using System;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// The immune battlefield: viruses (pooled), infected spots on the wall (they release new viruses —
    /// a virus multiplies only inside a cell), ally phagocytes and the player's Signal. Feeds the organism:
    /// infections raise the viral load, the viral load raises the temperature (fever).
    /// </summary>
    public sealed class Bvr2Immune : MonoBehaviour
    {
        public Bvr2Vessel vessel; public Bvr2Organism organism; public Bvr2Fx fx; public Bvr2Audio audioDirector; public Bvr2Rig rig;
        public Bvr2Virus virusTemplate, debrisTemplate, hedgehogTemplate; public Transform spotTemplate;
        public List<Bvr2Phagocyte> phagocytes = new List<Bvr2Phagocyte>();
        [Header("Rules")]
        public bool replicate = true;
        public int maxAlive = 40;
        public float replicateAfter = 7f, loadPerInfection = 0.05f, loadDecay = 0.008f;

        public readonly List<Bvr2Virus> viruses = new List<Bvr2Virus>();
        readonly List<Spot> spots = new List<Spot>();
        class Spot { public Transform t; public float s, angle, releaseAt; public int left; public Renderer r; }

        public event Action<Bvr2Virus> OnTagged, OnLanded, OnEscaped;
        public event Action<Bvr2Virus, bool> OnKilled;            // virus, by the player directly
        public event Action<Vector3> OnInfected;
        public event Action<Vector3, bool> OnSignal;               // point, on the beat

        public int Alive { get { int n = 0; foreach (var v in viruses) if (v.gameObject.activeSelf && v.Alive) n++; return n; } }
        public int PendingReleases { get { int n = 0; foreach (var s in spots) n += s.left; return n; } }
        public int Killed { get; private set; }
        public int KilledTagged { get; private set; }
        public int Infections { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public float LastBeat { get; private set; } = -10;
        float lastComboAt;

        void OnEnable() { if (organism) organism.OnBeat += Beat; }
        void OnDisable() { if (organism) organism.OnBeat -= Beat; }
        void Beat() { LastBeat = Time.unscaledTime; }

        /// <summary>Within ±0.2 s of a heartbeat: the rhythm bonus window.</summary>
        public bool OnTheBeat
        {
            get
            {
                if (!organism) return false;
                float period = 60f / Mathf.Max(organism.heartRate, 30f), since = Time.unscaledTime - LastBeat;
                return since < 0.2f || period - since < 0.12f;
            }
        }

        public Bvr2Virus Spawn(float s, float angle, float r01, float seek = 1f, float infectSeconds = 6f, bool armored = false)
        {
            Bvr2Virus v = null;
            if (armored && !hedgehogTemplate) armored = false;
            foreach (var x in viruses) if (!x.gameObject.activeSelf && !x.debris && x.armored == armored) { v = x; break; }
            if (!v)
            {
                int live = 0; foreach (var x in viruses) if (x.gameObject.activeSelf) live++;
                if (live >= maxAlive) return null;
                v = Instantiate(armored ? hedgehogTemplate : virusTemplate, transform); v.armored = armored; v.name = (armored ? "Hedgehog " : "Virus ") + viruses.Count; viruses.Add(v);
            }
            v.Init(this, s, angle, r01, seek, infectSeconds);
            return v;
        }

        /// <summary>Debris of dead cells at a wound (chapter 4): phagocytes clear it after a Signal.</summary>
        public Bvr2Virus SpawnDebris(Vector3 position)
        {
            if (!debrisTemplate) return null;
            Bvr2Virus v = null;
            foreach (var x in viruses) if (!x.gameObject.activeSelf && x.debris) { v = x; break; }
            if (!v) { v = Instantiate(debrisTemplate, transform); v.debris = true; v.name = "Debris " + viruses.Count; viruses.Add(v); }
            v.InitAnchored(this, position); v.fadeAfter = debrisFade;
            return v;
        }

        public int AliveViruses { get { int n = 0; foreach (var v in viruses) if (!v.debris && v.gameObject.activeSelf && v.Alive) n++; return n; } }
        /// <summary>Debris left by apoptosis fades on its own after this many seconds (0 = never).</summary>
        public float debrisFade;
        public int AliveDebris { get { int n = 0; foreach (var v in viruses) if (v.debris && v.gameObject.activeSelf && v.Alive) n++; return n; } }

        public Bvr2Virus NearestVirus(Vector3 p, float radius, bool taggedOnly, bool includeDebris = true, bool forPhagocyte = false)
        {
            Bvr2Virus best = null; float bd = radius * radius;
            foreach (var v in viruses)
            {
                if (!v.gameObject.activeSelf || !v.Alive || v.Engulfer) continue;
                if (v.debris && !includeDebris) continue;
                if (forPhagocyte && v.armored && !v.Tagged) continue; // a phagocyte cannot get a grip on bare armour
                if (taggedOnly && !v.Tagged) continue;
                float d = (v.transform.position - p).sqrMagnitude; if (d < bd) { bd = d; best = v; }
            }
            return best;
        }

        /// <summary>Player's Signal: ally phagocytes swim to the point (chemotaxis).</summary>
        public void Signal(Vector3 point, bool onBeat)
        {
            foreach (var p in phagocytes) if (p && p.gameObject.activeSelf) p.Signal(point, onBeat);
            fx?.Shock(point, rig ? rig.head.transform.position - point : Vector3.up, onBeat ? Bvr2Style.Knowledge : Bvr2Style.Ally, 0.2f, onBeat ? 3.2f : 2.2f, 0.6f);
            fx?.Burst(point, onBeat ? Bvr2Style.Knowledge : Bvr2Style.Ally, onBeat ? 30 : 16, 1.6f, 0.05f, 0.7f);
            audioDirector?.Play("BVR2_Signal", 0.8f, onBeat ? 1.15f : 1f);
            OnSignal?.Invoke(point, onBeat);
        }

        /// <summary>Bring the ally phagocytes in (from upstream) around the player.</summary>
        public void Deploy(int count, Transform head)
        {
            // formation: to the sides and a little below, far enough not to block the view
            Vector3[] slots = { new(-4.6f, -1.6f, 7f), new(4.6f, -1.3f, 7.5f), new(-2.6f, 1.9f, 10.5f), new(2.8f, -2.4f, 11f), new(0f, 2.4f, 13f) };
            for (int i = 0; i < phagocytes.Count; i++)
            {
                var p = phagocytes[i]; if (!p) continue;
                if (i >= count) { p.gameObject.SetActive(false); continue; }
                p.transform.position = head.position - head.forward * (10f + i * 2f) + UnityEngine.Random.insideUnitSphere * 1.5f;
                p.Init(this, head, slots[i % slots.Length]);
            }
        }

        public void ResetAll()
        {
            foreach (var v in viruses) if (v) v.gameObject.SetActive(false);
            foreach (var s in spots) if (s.t) Destroy(s.t.gameObject);
            spots.Clear();
            foreach (var p in phagocytes) if (p) p.gameObject.SetActive(false);
            Killed = KilledTagged = Infections = Combo = BestCombo = 0;
            if (organism) organism.viralLoad = 0;
        }

        public void DismissPhagocytes() { foreach (var p in phagocytes) if (p) p.gameObject.SetActive(false); }

        /// <summary>Remove remaining infected spots (the professor's medicine / end of a chapter).</summary>
        public void ClearSpots() { foreach (var s in spots) if (s.t) Destroy(s.t.gameObject); spots.Clear(); }

        /// <summary>Phagocytes also clear cell debris on their own (chapter 5: remains of apoptosis).</summary>
        public bool autoEatDebris;
        public int SpotCount => spots.Count;
        public event Action<Bvr2Virus> OnGlance;
        public event Action<Vector3> OnSpotKilled;
        float lastGlance;

        /// <summary>Ordinary antibodies slide off an armoured virion (rate-limited notification).</summary>
        public void Glance(Bvr2Virus v)
        {
            if (Time.time - lastGlance < 0.25f) return; lastGlance = Time.time;
            fx?.Burst(v.transform.position, new Color(0.8f, 0.85f, 1f), 6, 2.2f, 0.025f, 0.3f);
            audioDirector?.PlayAt("BVR2_Glance", v.transform.position, 0.5f);
            OnGlance?.Invoke(v);
        }

        public Vector3? NearestSpot(Vector3 p, float radius)
        {
            Vector3? best = null; float bd = radius * radius;
            foreach (var sp in spots) if (sp.t) { float d = (sp.t.position - p).sqrMagnitude; if (d < bd) { bd = d; best = sp.t.position; } }
            return best;
        }

        /// <summary>A T-killer destroys an infected wall spot (apoptosis: it shrinks and falls apart into vesicles).</summary>
        public bool KillSpotNear(Vector3 p, float radius)
        {
            for (int i = 0; i < spots.Count; i++)
            {
                var sp = spots[i]; if (!sp.t || (sp.t.position - p).sqrMagnitude > radius * radius) continue;
                spots.RemoveAt(i);
                if (organism) organism.viralLoad = Mathf.Max(0, organism.viralLoad - loadPerInfection);
                StartCoroutine(Apoptose(sp.t));
                OnSpotKilled?.Invoke(sp.t.position);
                return true;
            }
            return false;
        }

        System.Collections.IEnumerator Apoptose(Transform t)
        {
            Vector3 p = t.position; Vector3 s0 = t.localScale;
            audioDirector?.PlayAt("BVR2_Apoptosis", p, 0.8f);
            for (float k = 0; k < 1f && t; k += Bvr2Time.Delta / 0.7f) { t.localScale = s0 * Mathf.Lerp(1f, 0.35f, k) * (1f + 0.1f * Mathf.Sin(k * 40f)); yield return null; }
            if (t) Destroy(t.gameObject);
            fx?.Burst(p, new Color(0.95f, 0.85f, 1f), 18, 1f, 0.07f, 0.9f);
            for (int i = 0; i < 2; i++) SpawnDebris(p + UnityEngine.Random.insideUnitSphere * 0.4f);
        }

        // ---- notifications from viruses ------------------------------------------------------
        public void NotifyTagged(Bvr2Virus v)
        {
            fx?.Burst(v.transform.position, Bvr2Style.Ally, 12, 1f, 0.03f, 0.4f);
            audioDirector?.PlayAt("SFX_Capture_Lock", v.transform.position, 0.5f);
            OnTagged?.Invoke(v);
        }

        public void NotifyKilled(Bvr2Virus v, bool byPlayer)
        {
            Killed++; if (v.Tagged) KilledTagged++;
            // combo: kills that follow each other quickly
            if (Time.time - lastComboAt < 3f) Combo++; else Combo = 1;
            lastComboAt = Time.time; BestCombo = Mathf.Max(BestCombo, Combo);
            fx?.Burst(v.transform.position, new Color(0.7f, 1f, 0.35f), 20, 1.8f, 0.04f, 0.5f);
            OnKilled?.Invoke(v, byPlayer);
        }

        public void NotifyLanded(Bvr2Virus v) { audioDirector?.PlayAt("BVR2_Infect", v.transform.position, 0.6f); OnLanded?.Invoke(v); }
        public void NotifyEscaped(Bvr2Virus v) { OnEscaped?.Invoke(v); }

        public void NotifyInfected(Bvr2Virus v, float s, float angle)
        {
            Infections++;
            if (organism) organism.viralLoad = Mathf.Clamp01(organism.viralLoad + loadPerInfection);
            Vector3 p = vessel.SurfacePoint(s, angle, 0.05f);
            fx?.Burst(p, new Color(0.6f, 1f, 0.3f), 18, 1.2f, 0.05f, 0.6f);
            if (spotTemplate)
            {
                var t = Instantiate(spotTemplate, transform); t.gameObject.SetActive(true);
                t.position = p; t.rotation = Quaternion.LookRotation(vessel.Centre(s) - p);
                spots.Add(new Spot { t = t, s = s, angle = angle, releaseAt = Time.time + replicateAfter, left = replicate ? 2 : 0, r = t.GetComponentInChildren<Renderer>() });
            }
            OnInfected?.Invoke(p);
        }

        void Update()
        {
            float dt = Bvr2Time.Delta;
            if (organism && organism.viralLoad > 0 && spots.Count == 0) organism.viralLoad = Mathf.Max(0, organism.viralLoad - loadDecay * dt);
            float now = Time.time;
            foreach (var sp in spots)
            {
                if (!sp.t) continue;
                float k = Mathf.Clamp01(1f - (sp.releaseAt - now) / replicateAfter);
                sp.t.localScale = Vector3.one * (0.7f + 0.6f * k) * (1f + 0.08f * Mathf.Sin(now * 6f));
                if (sp.left > 0 && now >= sp.releaseAt)
                {
                    // the infected cell releases new viruses
                    sp.left--; sp.releaseAt = now + 1.2f;
                    var v = Spawn(sp.s + UnityEngine.Random.Range(-0.5f, 0.5f), sp.angle + UnityEngine.Random.Range(-10f, 10f), 0.75f, 1.1f);
                    if (v) fx?.Burst(v.transform.position, new Color(0.6f, 1f, 0.3f), 14, 1.6f, 0.04f, 0.5f);
                }
            }
            if (Combo > 0 && now - lastComboAt > 3f) Combo = 0;
        }
    }
}

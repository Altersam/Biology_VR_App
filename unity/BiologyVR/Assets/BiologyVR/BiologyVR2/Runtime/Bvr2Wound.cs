using System;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// A tear in the artery wall (chapter 4). The wall shader cuts a lens-shaped slit with a raw rim;
    /// inside lie the exposed collagen fibres. Blood leaks out: the organism loses volume, cells are dragged
    /// into the gap, the player is pulled toward it. Hemostasis happens on nine collagen patches in the real
    /// order: a "glue" factor painted on collagen → platelets stick (adhesion), change shape and release
    /// signals (activation) → neighbours join (aggregation) → fibrin stitches the loose plug.
    /// Rim patches must hold before the centre ones, otherwise the stream rips the plug away.
    /// </summary>
    public sealed class Bvr2Wound : MonoBehaviour
    {
        public Bvr2Vessel vessel; public Bvr2Environment env; public Bvr2Organism organism; public Bvr2Blood blood; public Bvr2Fx fx; public Bvr2Audio audioDirector; public Bvr2Swimmer swimmer;
        public float s = 1800f, angleDeg = 120f, halfLength = 2.6f, halfAngleDeg = 15f;
        [Tooltip("Litres per minute lost with the tear wide open and the vessel relaxed.")] public float maxLeak = 2f;
        [Tooltip("Scenario multiplier (lower while the professor explains).")] public float leakScale = 1f;
        public bool pullPlayer;
        public Mesh patchMesh, plateletMesh, ringMesh, redMesh;
        public Material patchMaterial, plateletMaterial, ringMaterial, fibrinMaterial, collagenMaterial, redMaterial;

        public sealed class Patch
        {
            public Transform t, ring; public Renderer r, ringR; public int index, slot; public float side; // side -1 left rim, 0 centre, +1 right rim
            public float paint, platelets, weight, tornAt = -10; public bool sealedFlag;
            public readonly List<Transform> plates = new List<Transform>();
            public bool Sealed => sealedFlag;
            public bool IsCentre => side == 0;
        }
        public readonly List<Patch> patches = new List<Patch>();
        public readonly List<Bvr2StrayClot> strays = new List<Bvr2StrayClot>();
        static readonly float[] SlotDs = { -0.55f, 0f, 0.55f };

        public float Open01 { get; private set; } = 1f;     // healing closes the slit
        public float FibrinProgress { get; set; }           // 0..1 needed, above ~1.25 = too much
        public bool Active { get; private set; }
        public int SealedCount { get { int n = 0; foreach (var p in patches) if (p.Sealed) n++; return n; } }
        public int AliveStrays { get { int n = 0; foreach (var c in strays) if (c && c.Alive) n++; return n; } }
        /// <summary>How much the stray clots narrow the lumen (1 = free).</summary>
        public float StrayConductance { get { float k = 1f; foreach (var c in strays) if (c && c.gameObject.activeSelf) k *= 1f - 0.14f * c.Growth * (1f - c.Dissolve); return k; } }
        public event Action<Patch> OnSealed, OnTorn;
        public event Action OnFirstAdhesion;
        bool firstAdhesion; float healK; MaterialPropertyBlock mpb; Transform collagen;
        readonly List<LineRenderer> fibrin = new List<LineRenderer>();
        readonly List<Transform> jets = new List<Transform>(); float jetPhase;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        /// <summary>Point on the tear: ds -1..1 along it, side -1 (left rim) .. 0 (centre, in the gap) .. +1 (right rim).</summary>
        public Vector3 PointOn(float ds, float side, float lift = 0f)
        {
            float cds = Mathf.Clamp(ds, -1f, 1f);
            float lens = Mathf.Sqrt(Mathf.Max(0.02f, 1f - cds * cds));
            float a = angleDeg + side * halfAngleDeg * lens * 1.08f * Mathf.Max(Open01, 0.05f);
            float ss = s + ds * halfLength * Mathf.Lerp(0.25f, 1f, Open01);
            float r = vessel.Radius(ss) + (side == 0 ? 0.3f : 0f) - lift;
            return vessel.PointAt(ss, r, a);
        }

        public Vector3 Centre => PointOn(0, 0, 0.3f);
        public Vector3 Inward => (vessel.Centre(s) - vessel.PointAt(s, vessel.Radius(s), angleDeg)).normalized;

        /// <summary>Tear coordinates of a world point on the wall: ds along, side across (±1 = rim).</summary>
        public void ToTear(Vector3 world, out float ds, out float side)
        {
            int hint = -1; float ws = vessel.Locate(world, ref hint, out var radial);
            ds = (ws - s) / halfLength;
            float a = vessel.AngleOf(ws, radial);
            float cds = Mathf.Clamp01(Mathf.Abs(ds));
            float lens = Mathf.Sqrt(Mathf.Max(0.02f, 1f - cds * cds));
            side = Mathf.DeltaAngle(angleDeg, a) / (halfAngleDeg * lens * 1.08f * Mathf.Max(Open01, 0.05f));
        }

        public void Build()
        {
            if (patches.Count > 0) return;
            int idx = 0;
            for (int k = 0; k < SlotDs.Length; k++)
                foreach (int side in new[] { -1, 0, 1 })
                {
                    var go = new GameObject($"Collagen Patch {k}{(side < 0 ? "L" : side > 0 ? "R" : "C")}"); go.transform.SetParent(transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = patchMesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = patchMaterial;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var ring = new GameObject("Ring"); ring.transform.SetParent(transform, false);
                    ring.AddComponent<MeshFilter>().sharedMesh = ringMesh; var rr = ring.AddComponent<MeshRenderer>(); rr.sharedMaterial = ringMaterial; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    patches.Add(new Patch { t = go.transform, r = mr, ring = ring.transform, ringR = rr, index = idx++, side = side, slot = k, weight = side == 0 ? 0.18f : 0.075f });
                }
            BuildCollagen();
            for (int i = 0; i < 6; i++)
            {
                var j = new GameObject("Leak Cell"); j.transform.SetParent(transform, false);
                j.AddComponent<MeshFilter>().sharedMesh = redMesh; var r = j.AddComponent<MeshRenderer>(); r.sharedMaterial = redMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                j.transform.localScale = Vector3.one * 0.4f; jets.Add(j.transform);
            }
        }

        /// <summary>Wavy collagen fibres stretched along the floor of the tear.</summary>
        void BuildCollagen()
        {
            collagen = new GameObject("Exposed Collagen").transform; collagen.SetParent(transform, false);
            var rnd = new System.Random(4);
            for (int i = 0; i < 11; i++)
            {
                var go = new GameObject("Fibre " + i); go.transform.SetParent(collagen, false);
                var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = collagenMaterial ? collagenMaterial : fibrinMaterial; lr.useWorldSpace = true;
                lr.positionCount = 14; lr.widthMultiplier = 0.07f + (float)rnd.NextDouble() * 0.05f; lr.numCapVertices = 2;
                lr.startColor = lr.endColor = Color.Lerp(new Color(1f, 0.93f, 0.8f), new Color(0.95f, 0.75f, 0.6f), (float)rnd.NextDouble());
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fibres.Add(new Fibre { lr = lr, side = (float)rnd.NextDouble() * 1.6f - 0.8f, phase = (float)rnd.NextDouble() * 10f, depth = 0.25f + (float)rnd.NextDouble() * 0.35f });
            }
        }

        public void Show(bool on)
        {
            Build();
            Active = on; gameObject.SetActive(true);
            env.windowOn = on; env.windowSlit = on; env.windowRagged = on ? 0.8f : 0f; env.windowEdge = on ? new Color(1f, 0.42f, 0.22f, 1f) : Color.clear;
            if (collagen) collagen.gameObject.SetActive(on);
            foreach (var p in patches) { p.t.gameObject.SetActive(on); p.ring.gameObject.SetActive(on); }
            foreach (var j in jets) j.gameObject.SetActive(on);
            if (!on) { blood.ClearSink(); organism.leakRate = 0; if (swimmer) swimmer.externalPull = Vector3.zero; foreach (var c in strays) if (c) c.gameObject.SetActive(false); }
        }

        public void ResetWound()
        {
            Open01 = 1f; healK = 0; FibrinProgress = 0; firstAdhesion = false; leakScale = 1f; pullPlayer = false;
            foreach (var p in patches) ResetPatch(p);
            foreach (var l in fibrin) if (l) Destroy(l.gameObject);
            fibrin.Clear();
            foreach (var c in strays) if (c) c.gameObject.SetActive(false);
        }

        public void ResetPatches() { foreach (var p in patches) ResetPatch(p); }
        public void ClearFibrin() { FibrinProgress = 0; foreach (var l in fibrin) if (l) Destroy(l.gameObject); fibrin.Clear(); }

        void ResetPatch(Patch p)
        {
            p.paint = 0; p.platelets = 0; p.sealedFlag = false; p.tornAt = -10;
            foreach (var x in p.plates) if (x) Destroy(x.gameObject);
            p.plates.Clear();
        }

        /// <summary>0 = sealed, 1 = wide open. A sealed but un-stitched plug still seeps.</summary>
        public float Leak
        {
            get
            {
                float open = 0, sealedW = 0;
                foreach (var p in patches) if (p.Sealed) sealedW += p.weight; else open += p.weight;
                float seep = sealedW * 0.22f * (1f - Mathf.Clamp01(FibrinProgress));
                return Mathf.Clamp01(open + seep) * Open01;
            }
        }

        public Patch NearestPatch(Ray ray, float maxDist, float coneDeg)
        {
            Patch best = null; float bestA = coneDeg;
            foreach (var p in patches)
            {
                if (!p.t.gameObject.activeSelf || p.Sealed) continue;
                Vector3 to = p.t.position - ray.origin; if (to.magnitude > maxDist) continue;
                float a = Vector3.Angle(ray.direction, to) - Mathf.Atan2(0.5f, to.magnitude) * Mathf.Rad2Deg;
                if (a < bestA) { bestA = a; best = p; }
            }
            return best;
        }

        public bool RimsHold(Patch centre)
        {
            foreach (var p in patches) if (!p.IsCentre && p.slot == centre.slot && !p.Sealed) return false;
            return true;
        }

        /// <summary>The Factor beam paints the glue factor on a patch.</summary>
        public void Paint(Patch p, float amount) { if (p != null && !p.Sealed) p.paint = Mathf.Clamp01(p.paint + amount); }

        void Update()
        {
            if (!Active) return;
            float dt = Bvr2Time.Delta, now = Time.time;
            Vector3 tangent = vessel.Tangent(s);
            foreach (var p in patches)
            {
                Vector3 pos = PointOn(SlotDs[p.slot], p.side, p.IsCentre ? 0.05f : 0.06f);
                Vector3 inward = (vessel.Centre(s + SlotDs[p.slot] * halfLength) - pos).normalized;
                p.t.position = pos; p.t.rotation = Quaternion.LookRotation(tangent, inward);
                // the sphere mesh has radius 1: rim patches ≈ 0.9 m across, centre ones ≈ 1.2 m
                float sc = (p.IsCentre ? 0.6f : 0.45f) * Mathf.Lerp(1f, 0.35f, healK);
                p.t.localScale = new Vector3(sc, 0.22f * sc, sc);
                p.ring.position = pos + inward * 0.1f; p.ring.rotation = Quaternion.LookRotation(inward, tangent);
                p.ring.localScale = Vector3.one * sc * 1.25f * (p.Sealed ? 1f : 1f + 0.06f * Mathf.Sin(now * 4f + p.index));
            }
            foreach (var p in patches)
            {
                if (!p.Sealed && p.paint >= 1f)
                {
                    if (!firstAdhesion) { firstAdhesion = true; OnFirstAdhesion?.Invoke(); }
                    float rate = NeighbourSealed(p) ? 1.5f : 0.85f;   // signals (ADP) from sealed neighbours speed it up
                    p.platelets = Mathf.Min(1f, p.platelets + dt * rate);
                    while (p.plates.Count < Mathf.CeilToInt(p.platelets * 10f)) AddPlatelet(p);
                    if (p.platelets >= 1f)
                    {
                        if (p.IsCentre && !RimsHold(p))
                        {
                            // the stream tears away a plug that has nothing to hold on to
                            Vector3 at = p.t.position;
                            ResetPatch(p); p.tornAt = now;
                            fx?.Burst(at, new Color(1f, 0.8f, 0.4f), 34, 3.2f, 0.05f, 0.8f, Inward * 3f + tangent * 4f);
                            audioDirector?.PlayAt("BVR2_Crack", at, 0.8f);
                            OnTorn?.Invoke(p);
                        }
                        else
                        {
                            p.sealedFlag = true;
                            fx?.Shock(p.t.position + Inward * 0.2f, Inward, new Color(1f, 0.85f, 0.4f), 0.2f, 2.4f, 0.6f);
                            audioDirector?.PlayAt("BVR2_Stick", p.t.position, 0.9f);
                            OnSealed?.Invoke(p);
                        }
                    }
                }
                // ring colour: glue painted (blue), no support yet for a centre patch (orange), sealed (gold)
                Color c = p.Sealed ? new Color(1f, 0.8f, 0.35f, 0.75f)
                        : p.IsCentre && !RimsHold(p) ? Color.Lerp(new Color(1f, 0.5f, 0.2f, 0.55f), new Color(0.45f, 0.8f, 1f, 0.9f), p.paint)
                        : Color.Lerp(new Color(0.55f, 0.95f, 1f, 0.6f), new Color(0.45f, 0.8f, 1f, 1f), p.paint);
                if (now - p.tornAt < 1.2f) c = Color.Lerp(Bvr2Style.Danger, c, (now - p.tornAt) / 1.2f);
                mpb.Clear(); mpb.SetColor("_Color", c); p.ringR.SetPropertyBlock(mpb);
                Color body = p.Sealed ? new Color(1f, 0.82f, 0.55f) : Color.Lerp(new Color(0.95f, 0.88f, 0.78f), new Color(0.5f, 0.78f, 1f), p.paint);
                mpb.Clear(); mpb.SetColor("_BaseColor", body); mpb.SetColor("_Emission", body * (0.08f + 0.3f * p.paint)); p.r.SetPropertyBlock(mpb);
            }
            // platelets drift in from the stream and flatten on their patch
            foreach (var p in patches)
                for (int i = 0; i < p.plates.Count; i++)
                {
                    var x = p.plates[i]; if (!x) continue;
                    x.localPosition = Vector3.Lerp(x.localPosition, PlateHome(i), dt * 3.5f);
                    x.localScale = Vector3.Lerp(x.localScale, new Vector3(0.5f, 1.4f, 0.5f) * (p.Sealed ? 1.15f : 1f), dt * 2f);
                }
            // the leak: volume loss, cells dragged into the gap, the player pulled toward it
            float leak = Leak;
            organism.leakRate = maxLeak * leak * leakScale * Mathf.Clamp(organism.fieldConductance, 0.15f, 1.5f);
            if (leak > 0.01f) blood.SetSink(Centre, 7f, 16f * leak); else blood.ClearSink();
            if (swimmer)
            {
                Vector3 to = Centre - swimmer.HeadPosition; float d = to.magnitude;
                swimmer.externalPull = pullPlayer && d < 30f && d > 2.5f ? to / d * 1.4f * leak * Mathf.Clamp01((30f - d) / 18f) : Vector3.zero;
            }
            jetPhase += dt * (0.8f + 1.6f * leak);
            for (int i = 0; i < jets.Count; i++)
            {
                float k = Mathf.Repeat(jetPhase + i / (float)jets.Count, 1f);
                float ds = Mathf.Sin(i * 2.3f) * 0.6f;
                jets[i].gameObject.SetActive(leak > 0.04f);
                jets[i].position = Vector3.Lerp(PointOn(ds, 0, 1.6f) - tangent * 1.5f, PointOn(ds * 0.5f, 0, -0.9f), k * k);
                jets[i].rotation = Quaternion.Euler(i * 40f + jetPhase * 200f, i * 70f, 0);
                jets[i].localScale = Vector3.one * 0.38f * (1f - k * 0.6f);
            }
            env.windowS = s; env.windowAngle01 = Mathf.Repeat(angleDeg, 360f) / 360f;
            env.windowHalfLength = halfLength * Mathf.Lerp(0.25f, 1f, Open01);
            env.windowHalfAngle01 = halfAngleDeg / 360f * Mathf.Max(Open01, 0.001f);
            env.windowOn = Open01 > 0.01f;
            organism.lesionConductance = Mathf.Lerp(organism.lesionConductance, Mathf.Clamp(StrayConductance, 0.3f, 1f), dt * 2f);
        }

        bool NeighbourSealed(Patch p)
        {
            foreach (var q in patches)
                if (q != p && q.Sealed && ((q.slot == p.slot) || (Mathf.Abs(q.slot - p.slot) == 1 && q.side == p.side))) return true;
            return false;
        }

        static Vector3 PlateHome(int i) { float a = i * 2.39996f; float r = Mathf.Min(0.15f + 0.08f * i, 0.8f); return new Vector3(Mathf.Cos(a) * r, 1.1f, Mathf.Sin(a) * r); }

        void AddPlatelet(Patch p)
        {
            var go = new GameObject("Platelet"); go.transform.SetParent(p.t, false);
            go.AddComponent<MeshFilter>().sharedMesh = plateletMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = plateletMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = new MaterialPropertyBlock(); b.SetVector("_Inst", new Vector4(UnityEngine.Random.value, 0.4f, 1f, UnityEngine.Random.value)); r.SetPropertyBlock(b);
            // it arrives from the stream above the patch
            go.transform.position = p.t.position + Inward * 2.5f + UnityEngine.Random.insideUnitSphere * 0.8f - vessel.Tangent(s) * 2f;
            go.transform.localScale = Vector3.one * 0.8f; go.transform.localRotation = UnityEngine.Random.rotation;
            p.plates.Add(go.transform);
        }

        /// <summary>Lay one fibrin thread across the plug between two points along the tear (stitching).</summary>
        public void AddFibrin(float ds0, float ds1)
        {
            if (!fibrinMaterial) return;
            var go = new GameObject("Fibrin Thread"); go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = fibrinMaterial; lr.positionCount = 5; lr.useWorldSpace = true;
            lr.widthMultiplier = 0.045f; lr.numCapVertices = 2; lr.startColor = lr.endColor = new Color(1f, 0.95f, 0.75f, 1f); lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ds0 = Mathf.Clamp(ds0, -0.9f, 0.9f); ds1 = Mathf.Clamp(ds1, -0.9f, 0.9f);
            for (int i = 0; i < 5; i++)
            {
                float k = i / 4f, side = Mathf.Lerp(-1.15f, 1.15f, k), ds = Mathf.Lerp(ds0, ds1, k);
                lr.SetPosition(i, PointOn(ds, side, 0.18f + Mathf.Sin(k * Mathf.PI) * 0.3f));
            }
            fibrin.Add(lr);
        }

        public int FibrinThreads => fibrin.Count;

        /// <summary>A clot grows on healthy wall (glue in the wrong place) or into the lumen (too much fibrin).</summary>
        public Bvr2StrayClot SpawnStray(Vector3 wallPoint, float size = 1f)
        {
            Bvr2StrayClot c = null;
            foreach (var x in strays) if (x && !x.gameObject.activeSelf) { c = x; break; }
            if (!c)
            {
                var go = new GameObject("Stray Clot " + strays.Count); go.transform.SetParent(transform, false);
                c = go.AddComponent<Bvr2StrayClot>();
                c.Build(redMesh, plateletMesh, redMaterial, plateletMaterial, fibrinMaterial);
                c.SetMarker(ringMesh, ringMaterial);
                strays.Add(c);
            }
            int hint = -1; vessel.Locate(wallPoint, ref hint, out var radial);
            Vector3 inward = radial.sqrMagnitude > 0.001f ? -radial.normalized : Inward;
            c.Place(wallPoint, inward, size, fx, audioDirector);
            return c;
        }

        /// <summary>Healing: the slit closes like a zip, plasmin dissolves the fibrin.</summary>
        public void Heal(float k)
        {
            healK = Mathf.Clamp01(k);
            Open01 = 1f - healK;
            foreach (var l in fibrin) if (l) { var c = new Color(1f, 0.95f, 0.75f, 1f - healK); l.startColor = l.endColor = c; l.widthMultiplier = 0.045f * (1f - healK * 0.7f); }
            if (healK >= 1f)
            {
                foreach (var l in fibrin) if (l) Destroy(l.gameObject);
                fibrin.Clear();
                foreach (var p in patches) { p.t.gameObject.SetActive(false); p.ring.gameObject.SetActive(false); }
                if (collagen) collagen.gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (!Active || !collagen || !collagen.gameObject.activeSelf) return;
            float time = Time.time * 0.6f;
            foreach (var f in fibres)
            {
                for (int i = 0; i < f.lr.positionCount; i++)
                {
                    float k = i / (f.lr.positionCount - 1f), ds = Mathf.Lerp(-0.95f, 0.95f, k);
                    float wave = Mathf.Sin(k * 9f + f.phase) * 0.18f + Mathf.Sin(time + f.phase + k * 4f) * 0.05f;
                    f.lr.SetPosition(i, PointOn(ds, (f.side + wave) * 0.9f, -f.depth));
                }
            }
        }

        struct Fibre { public LineRenderer lr; public float side, phase, depth; }
        readonly List<Fibre> fibres = new List<Fibre>();
    }
}

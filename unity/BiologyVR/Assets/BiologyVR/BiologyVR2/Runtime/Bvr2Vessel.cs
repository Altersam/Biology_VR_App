using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// The artery as a spline with a radius profile. Everything that lives in the lumen
    /// (player, cells, gates, flow) uses the same arc-length coordinate s (metres).
    /// Dynamic constriction zones (vascular field, spasm) are mirrored in the wall shader.
    /// </summary>
    public sealed class Bvr2Vessel : MonoBehaviour
    {
        [Tooltip("Control points of the centre line (world space).")]
        public List<Vector3> controlPoints = new List<Vector3>();
        public float baseRadius = 6f;
        [Tooltip("Radius multiplier keys: x = s (m), y = factor.")]
        public List<Vector2> radiusKeys = new List<Vector2>();
        public float sampleStep = 0.5f;

        [System.Serializable]
        public struct Zone { public float centre, halfWidth, factor; public bool active; }
        public readonly Zone[] zones = new Zone[4];

        /// <summary>Atherosclerotic plaque: a smooth bump growing out of one side of the wall (chapter 2).
        /// The same shape is applied in the wall shader (BVR2_Common.hlsl, B2PlaqueInset).</summary>
        [System.Serializable]
        public struct Plaque
        {
            public bool active;
            public float s, halfLength, angleDeg, halfAngleDeg, height;
            [Range(0, 1)] public float rupture, reveal;
        }
        public Plaque[] plaques = new Plaque[2];

        Vector3[] pos, tan, nrm, bin;
        float length;
        public float Length { get { Ensure(); return length; } }
        public int SampleCount { get { Ensure(); return pos.Length; } }

        void Awake() { Ensure(); }

        public void Ensure() { if (pos == null || pos.Length < 2) Rebuild(); }

        /// <summary>Re-sample the centre line by arc length with parallel-transport frames.</summary>
        public void Rebuild()
        {
            var dense = new List<Vector3>();
            int n = controlPoints.Count;
            if (n < 2) { controlPoints = new List<Vector3> { Vector3.zero, Vector3.forward * 100 }; n = 2; }
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 p0 = controlPoints[Mathf.Max(i - 1, 0)], p1 = controlPoints[i], p2 = controlPoints[i + 1], p3 = controlPoints[Mathf.Min(i + 2, n - 1)];
                int steps = Mathf.Max(8, Mathf.CeilToInt(Vector3.Distance(p1, p2) / 0.25f));
                for (int k = 0; k < steps; k++) dense.Add(CatmullRom(p0, p1, p2, p3, k / (float)steps));
            }
            dense.Add(controlPoints[n - 1]);
            var cum = new float[dense.Count];
            for (int i = 1; i < dense.Count; i++) cum[i] = cum[i - 1] + Vector3.Distance(dense[i - 1], dense[i]);
            length = cum[cum.Length - 1];
            int count = Mathf.FloorToInt(length / sampleStep) + 1;
            pos = new Vector3[count]; tan = new Vector3[count]; nrm = new Vector3[count]; bin = new Vector3[count];
            int j = 0;
            for (int i = 0; i < count; i++)
            {
                float s = i * sampleStep;
                while (j < cum.Length - 2 && cum[j + 1] < s) j++;
                float t = Mathf.InverseLerp(cum[j], cum[j + 1], s);
                pos[i] = Vector3.Lerp(dense[j], dense[j + 1], t);
            }
            for (int i = 0; i < count; i++) tan[i] = (pos[Mathf.Min(i + 1, count - 1)] - pos[Mathf.Max(i - 1, 0)]).normalized;
            // Parallel transport keeps the frame from twisting along the tube.
            Vector3 up = Mathf.Abs(Vector3.Dot(tan[0], Vector3.up)) > 0.9f ? Vector3.right : Vector3.up;
            nrm[0] = Vector3.Cross(Vector3.Cross(tan[0], up), tan[0]).normalized;
            for (int i = 1; i < count; i++)
            {
                var q = Quaternion.FromToRotation(tan[i - 1], tan[i]);
                nrm[i] = (q * nrm[i - 1]); nrm[i] = (nrm[i] - tan[i] * Vector3.Dot(nrm[i], tan[i])).normalized;
            }
            for (int i = 0; i < count; i++) bin[i] = Vector3.Cross(tan[i], nrm[i]);
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        public void Frame(float s, out Vector3 centre, out Vector3 tangent, out Vector3 normal, out Vector3 binormal)
        {
            Ensure();
            float f = Mathf.Clamp(s / sampleStep, 0, pos.Length - 1.001f);
            int i = (int)f; float t = f - i;
            centre = Vector3.Lerp(pos[i], pos[i + 1], t);
            tangent = Vector3.Lerp(tan[i], tan[i + 1], t).normalized;
            normal = Vector3.Lerp(nrm[i], nrm[i + 1], t).normalized;
            binormal = Vector3.Lerp(bin[i], bin[i + 1], t).normalized;
        }

        public Vector3 Centre(float s) { Frame(s, out var c, out _, out _, out _); return c; }
        public Vector3 Tangent(float s) { Frame(s, out _, out var t, out _, out _); return t; }

        /// <summary>World point at arc length s, radial distance r and angle (degrees, 0 = frame normal).</summary>
        public Vector3 PointAt(float s, float r, float angleDeg)
        {
            Frame(s, out var c, out _, out var n, out var b);
            float a = angleDeg * Mathf.Deg2Rad;
            return c + (n * Mathf.Cos(a) + b * Mathf.Sin(a)) * r;
        }

        public float BaseRadius(float s)
        {
            float f = 1f;
            if (radiusKeys.Count == 1) f = radiusKeys[0].y;
            else if (radiusKeys.Count > 1)
            {
                if (s <= radiusKeys[0].x) f = radiusKeys[0].y;
                else if (s >= radiusKeys[radiusKeys.Count - 1].x) f = radiusKeys[radiusKeys.Count - 1].y;
                else for (int i = 0; i < radiusKeys.Count - 1; i++)
                    if (s < radiusKeys[i + 1].x) { float t = Mathf.InverseLerp(radiusKeys[i].x, radiusKeys[i + 1].x, s); f = Mathf.Lerp(radiusKeys[i].y, radiusKeys[i + 1].y, Mathf.SmoothStep(0, 1, t)); break; }
            }
            return baseRadius * f;
        }

        public float ZoneScale(float s)
        {
            float k = 1f;
            for (int i = 0; i < zones.Length; i++)
            {
                var z = zones[i]; if (!z.active || z.halfWidth < 0.001f) continue;
                float d = Mathf.Abs(s - z.centre) / z.halfWidth;
                float w = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f, 1f, d));
                k *= Mathf.Lerp(1f, z.factor, w);
            }
            return k;
        }

        /// <summary>Current lumen radius (base profile × dynamic constriction).</summary>
        public float Radius(float s) => BaseRadius(s) * ZoneScale(s);

        static float Bump(float x) { x = Mathf.Clamp01(1f - x * x); return x * x; }

        /// <summary>How far plaques push the wall inwards at (s, angle). Mirrors B2PlaqueInset in the shader.</summary>
        public float Inset(float s, float angleDeg)
        {
            float inset = 0;
            for (int i = 0; i < plaques.Length; i++)
            {
                var p = plaques[i]; if (!p.active || p.height <= 0.001f) continue;
                float ds = (s - p.s) / Mathf.Max(p.halfLength, 0.01f);
                if (ds <= -1f || ds >= 1f) continue;
                float da = Mathf.DeltaAngle(p.angleDeg, angleDeg) / Mathf.Max(p.halfAngleDeg, 0.01f);
                inset += p.height * Bump(ds) * Bump(da) * (1f + 0.08f * Mathf.Sin(ds * 7f + da * 5f));
            }
            return inset;
        }

        /// <summary>Free radius of the lumen in a given direction (wall minus plaques).</summary>
        public float FreeRadius(float s, float angleDeg) => Radius(s) - Inset(s, angleDeg);

        /// <summary>Angle (degrees, 0..360) of a radial vector at s, in the same convention as PointAt.</summary>
        public float AngleOf(float s, Vector3 radial)
        {
            Frame(s, out _, out _, out var n, out var b);
            float a = Mathf.Atan2(Vector3.Dot(radial, b), Vector3.Dot(radial, n)) * Mathf.Rad2Deg;
            return a < 0 ? a + 360f : a;
        }

        /// <summary>Point on the plaque surface (or bare wall) at (s, angle), lifted by <paramref name="lift"/>.</summary>
        public Vector3 SurfacePoint(float s, float angleDeg, float lift = 0) => PointAt(s, FreeRadius(s, angleDeg) - lift, angleDeg);

        /// <summary>March a ray through the lumen until it meets the wall or a plaque. Returns false if nothing within maxDistance.</summary>
        public bool RaycastWall(Ray ray, float maxDistance, out Vector3 hit, out float s, out float plaqueInset)
        {
            int h = -1; hit = ray.origin; s = 0; plaqueInset = 0;
            float step = 0.2f;
            for (float d = 0.2f; d <= maxDistance; d += step)
            {
                Vector3 p = ray.origin + ray.direction * d;
                float sp = Locate(p, ref h, out var radial);
                float ang = AngleOf(sp, radial); float ins = Inset(sp, ang);
                if (radial.magnitude >= Radius(sp) - ins)
                {
                    // refine between the last two samples
                    float lo = d - step, hi = d;
                    for (int k = 0; k < 6; k++)
                    {
                        float mid = (lo + hi) * 0.5f; Vector3 q = ray.origin + ray.direction * mid;
                        float sq = Locate(q, ref h, out var rq); float iq = Inset(sq, AngleOf(sq, rq));
                        if (rq.magnitude >= Radius(sq) - iq) hi = mid; else lo = mid;
                    }
                    hit = ray.origin + ray.direction * hi; s = Locate(hit, ref h, out var rh); plaqueInset = Inset(s, AngleOf(s, rh));
                    return true;
                }
            }
            return false;
        }

        public void SetZone(int index, float centre, float halfWidth, float factor)
        { zones[index] = new Zone { centre = centre, halfWidth = halfWidth, factor = factor, active = true }; }
        public void ClearZone(int index) { zones[index].active = false; }

        /// <summary>Closest centre-line location for a world point. <paramref name="hint"/> speeds up repeated queries.</summary>
        public float Locate(Vector3 world, ref int hint, out Vector3 radial)
        {
            Ensure();
            int best = 0; float bestD = float.MaxValue;
            int from = 0, to = pos.Length - 1;
            if (hint > 0 && hint < pos.Length) { from = Mathf.Max(0, hint - 60); to = Mathf.Min(pos.Length - 1, hint + 60); }
            for (int i = from; i <= to; i++) { float d = (pos[i] - world).sqrMagnitude; if (d < bestD) { bestD = d; best = i; } }
            hint = best;
            int a = Mathf.Max(0, best - 1), b = Mathf.Min(pos.Length - 1, best + 1);
            Vector3 seg = pos[b] - pos[a];
            float t = seg.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(world - pos[a], seg) / seg.sqrMagnitude) : 0;
            float s = (a + t * (b - a)) * sampleStep;
            radial = world - Centre(s);
            radial -= Tangent(s) * Vector3.Dot(radial, Tangent(s));
            return s;
        }

        /// <summary>Blood velocity at a world point: Poiseuille profile, continuity (narrow → faster), pulsatile.</summary>
        public Vector3 FlowAt(float s, float radialDistance, float meanSpeed)
        {
            float R = Mathf.Max(Radius(s), 0.5f);
            float continuity = baseRadius * baseRadius / (R * R);
            float x = Mathf.Clamp01(radialDistance / R);
            float profile = 1.6f * (1f - 0.75f * x * x); // softened parabola, never zero (playability)
            return Tangent(s) * meanSpeed * continuity * profile;
        }

        // ----- mesh ---------------------------------------------------------------------------
        /// <summary>Builds the wall as chunks (for culling). UV0 = (s, angle 0..1); UV1 = centre point; tangent = spline tangent.</summary>
        public List<Mesh> BuildWallMeshes(float ringStep = 0.6f, int segments = 56, float chunkLength = 36f)
        {
            Ensure();
            var meshes = new List<Mesh>();
            float L = length;
            for (float start = 0; start < L - 0.01f; start += chunkLength)
            {
                float end = Mathf.Min(L, start + chunkLength);
                int rings = Mathf.Max(2, Mathf.CeilToInt((end - start) / ringStep) + 1);
                var v = new Vector3[rings * (segments + 1)]; var n = new Vector3[v.Length]; var t = new Vector4[v.Length];
                var uv = new Vector2[v.Length]; var c = new Vector3[v.Length];
                for (int r = 0; r < rings; r++)
                {
                    float s = Mathf.Lerp(start, end, r / (float)(rings - 1));
                    Frame(s, out var centre, out var tg, out var nm, out var bn);
                    float R = BaseRadius(s);
                    for (int k = 0; k <= segments; k++)
                    {
                        float a = k / (float)segments * Mathf.PI * 2f;
                        Vector3 dir = nm * Mathf.Cos(a) + bn * Mathf.Sin(a);
                        int idx = r * (segments + 1) + k;
                        v[idx] = centre + dir * R; n[idx] = dir; t[idx] = new Vector4(tg.x, tg.y, tg.z, 1);
                        uv[idx] = new Vector2(s, k / (float)segments); c[idx] = centre;
                    }
                }
                var tris = new int[(rings - 1) * segments * 6]; int ti = 0;
                for (int r = 0; r < rings - 1; r++)
                    for (int k = 0; k < segments; k++)
                    {
                        int i0 = r * (segments + 1) + k, i1 = i0 + 1, i2 = i0 + segments + 1, i3 = i2 + 1;
                        tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i1; tris[ti++] = i1; tris[ti++] = i2; tris[ti++] = i3;
                    }
                var m = new Mesh { name = $"BVR2_Wall_{Mathf.RoundToInt(start)}" };
                m.indexFormat = v.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                m.vertices = v; m.normals = n; m.tangents = t; m.SetUVs(0, uv); m.SetUVs(1, new List<Vector3>(c)); m.triangles = tris;
                m.RecalculateBounds();
                // Bounds must include the pulse bulge and widening zones.
                var bounds = m.bounds; bounds.Expand(baseRadius * 0.8f); m.bounds = bounds;
                meshes.Add(m);
            }
            return meshes;
        }
    }
}

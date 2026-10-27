using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>Procedural meshes for cells and holograms. Unit sized (radius ≈ 1), thin axis = Y.</summary>
    public static class Bvr2Meshes
    {
        /// <summary>Red blood cell: Evans–Fung biconcave disc.</summary>
        public static Mesh RedCell(int rings = 8, int segments = 20)
        {
            var v = new List<Vector3>(); var tris = new List<int>();
            // top and bottom share the rim ring so normals stay smooth around the edge
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? 1 : -1;
                for (int r = 0; r <= rings; r++)
                {
                    float rr = r / (float)rings; rr = Mathf.Sin(rr * Mathf.PI * 0.5f); // denser at the rim
                    float rr2 = rr * rr;
                    float h = 0.5f * Mathf.Sqrt(Mathf.Max(0, 1 - rr2)) * (0.207f + 2.003f * rr2 - 1.123f * rr2 * rr2);
                    for (int k = 0; k < segments; k++)
                    {
                        float a = k / (float)segments * Mathf.PI * 2;
                        v.Add(new Vector3(Mathf.Cos(a) * rr, sign * h, Mathf.Sin(a) * rr));
                    }
                }
            }
            int perSide = (rings + 1) * segments;
            for (int side = 0; side < 2; side++)
            {
                int o = side * perSide;
                for (int r = 0; r < rings; r++)
                    for (int k = 0; k < segments; k++)
                    {
                        int k1 = (k + 1) % segments;
                        int a = o + r * segments + k, b = o + r * segments + k1, c = o + (r + 1) * segments + k, d = o + (r + 1) * segments + k1;
                        if (side == 0) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(d); tris.Add(c); }
                        else { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); }
                    }
            }
            // weld the two rims (same positions) by merging indices of the bottom rim into the top rim
            for (int i = 0; i < tris.Count; i++)
            {
                int idx = tris[i];
                if (idx >= perSide + rings * segments) tris[i] = idx - perSide; // bottom rim → top rim
            }
            var m = new Mesh { name = "BVR2_RedCell" }; m.SetVertices(v); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        public static Mesh IcoSphere(int subdivisions, float noise = 0f, int seed = 1, float flatten = 1f, string name = "BVR2_Sphere")
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new List<Vector3> { new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0), new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t), new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1) };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
            var f = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            var cache = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
                if (cache.TryGetValue(key, out int idx)) return idx;
                v.Add(((v[a] + v[b]) * 0.5f).normalized); cache[key] = v.Count - 1; return v.Count - 1;
            }
            for (int s = 0; s < subdivisions; s++)
            {
                var nf = new List<int>();
                for (int i = 0; i < f.Count; i += 3)
                {
                    int a = f[i], b = f[i + 1], c = f[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                f = nf;
            }
            if (noise > 0)
                for (int i = 0; i < v.Count; i++)
                {
                    var p = v[i];
                    float n = Mathf.PerlinNoise(p.x * 1.7f + seed * 3.1f, p.y * 1.7f + p.z * 1.3f + seed) * 0.7f + Mathf.PerlinNoise(p.x * 4.1f + seed, p.z * 4.3f - p.y * 2f) * 0.3f;
                    v[i] = p * (1f + (n - 0.5f) * 2f * noise);
                }
            for (int i = 0; i < v.Count; i++) v[i] = new Vector3(v[i].x, v[i].y * flatten, v[i].z);
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(f, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// Virus particle (radius 1): an icosahedral capsid with a crown of spike proteins (antigens).
        /// half = +1 / -1 builds only the upper / lower half (the capsid "opened" by hand), 0 = whole.
        /// </summary>
        public static Mesh Virus(int half = 0, string name = "BVR2_Virus")
        {
            var shell = IcoSphere(2, 0.04f, 5, 1f, "tmp");
            var sv = shell.vertices; var st = shell.triangles;
            var v = new List<Vector3>(); var tris = new List<int>(); var col = new List<Color>();
            bool Keep(Vector3 d) => half == 0 || d.y * half >= -0.02f;
            var map = new Dictionary<int, int>();
            for (int i = 0; i < st.Length; i += 3)
            {
                Vector3 c = (sv[st[i]] + sv[st[i + 1]] + sv[st[i + 2]]) / 3f;
                if (!Keep(c)) continue;
                for (int k = 0; k < 3; k++)
                {
                    int src = st[i + k];
                    if (!map.TryGetValue(src, out int dst)) { dst = v.Count; v.Add(sv[src] * 0.72f); col.Add(new Color(0.8f, 1f, 0.75f)); map[src] = dst; }
                    tris.Add(dst);
                }
            }
            // spikes at the vertices of a subdivided icosahedron
            var spikes = IcoSphere(1, 0, 1, 1, "tmp2").vertices;
            foreach (var d0 in spikes)
            {
                var d = d0.normalized; if (!Keep(d)) continue;
                Vector3 t1 = Vector3.Cross(d, Mathf.Abs(d.y) < 0.9f ? Vector3.up : Vector3.right).normalized, t2 = Vector3.Cross(d, t1);
                int b = v.Count;
                for (int k = 0; k < 4; k++)
                {
                    float a = k * Mathf.PI * 0.5f; Vector3 o = t1 * Mathf.Cos(a) + t2 * Mathf.Sin(a);
                    v.Add(d * 0.7f + o * 0.045f); v.Add(d * 0.98f + o * 0.03f); col.Add(new Color(0.9f, 1f, 0.6f)); col.Add(new Color(0.9f, 1f, 0.6f));
                }
                for (int k = 0; k < 4; k++)
                {
                    int a0 = b + k * 2, a1 = a0 + 1, b0 = b + ((k + 1) % 4) * 2, b1 = b0 + 1;
                    tris.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
                }
                // knob (the part the antibody recognises: epitope)
                int kb = v.Count; float kr = 0.075f; Vector3 kc = d * 1.02f;
                v.Add(kc + d * kr); v.Add(kc - d * kr); v.Add(kc + t1 * kr); v.Add(kc - t1 * kr); v.Add(kc + t2 * kr); v.Add(kc - t2 * kr);
                for (int k = 0; k < 6; k++) col.Add(new Color(1f, 0.92f, 0.45f));
                int[] oct = { 0, 2, 4, 0, 4, 3, 0, 3, 5, 0, 5, 2, 1, 4, 2, 1, 3, 4, 1, 5, 3, 1, 2, 5 };
                foreach (int o in oct) tris.Add(kb + o);
            }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Armoured "hedgehog" virion (radius ≈ 1.2): a dense crown of long sharp spikes over a dark capsid.</summary>
        public static Mesh Hedgehog(string name = "BVR2_Hedgehog")
        {
            var shell = IcoSphere(2, 0.05f, 9, 1f, "tmp");
            var v = new List<Vector3>(); var col = new List<Color>(); var tris = new List<int>();
            foreach (var p in shell.vertices) { v.Add(p * 0.66f); col.Add(new Color(0.62f, 0.45f, 0.85f)); }
            tris.AddRange(shell.triangles);
            foreach (var d0 in IcoSphere(1, 0, 1, 1, "tmp2").vertices)
            {
                var d = d0.normalized;
                Vector3 t1 = Vector3.Cross(d, Mathf.Abs(d.y) < 0.9f ? Vector3.up : Vector3.right).normalized, t2 = Vector3.Cross(d, t1);
                int b = v.Count;
                for (int k = 0; k < 4; k++)
                {
                    float a = k * Mathf.PI * 0.5f;
                    v.Add(d * 0.6f + (t1 * Mathf.Cos(a) + t2 * Mathf.Sin(a)) * 0.09f); col.Add(new Color(0.7f, 0.5f, 0.95f));
                }
                v.Add(d * 1.22f); col.Add(new Color(1f, 0.55f, 0.95f));
                for (int k = 0; k < 4; k++) tris.AddRange(new[] { b + k, b + (k + 1) % 4, b + 4 });
            }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Antibody: a Y of three bars, about 1 unit tall (stem along +Y).</summary>
        public static Mesh Antibody(string name = "BVR2_Antibody")
        {
            var v = new List<Vector3>(); var tris = new List<int>();
            void Bar(Vector3 a, Vector3 b, float w)
            {
                Vector3 d = (b - a).normalized, t1 = Vector3.Cross(d, Vector3.forward).normalized * w, t2 = Vector3.Cross(d, t1).normalized * w;
                int i0 = v.Count;
                foreach (var e in new[] { a, b }) { v.Add(e + t1 + t2); v.Add(e - t1 + t2); v.Add(e - t1 - t2); v.Add(e + t1 - t2); }
                for (int k = 0; k < 4; k++) { int a0 = i0 + k, a1 = i0 + (k + 1) % 4, b0 = a0 + 4, b1 = a1 + 4; tris.AddRange(new[] { a0, b0, a1, a1, b0, b1 }); }
                tris.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3, i0 + 4, i0 + 6, i0 + 5, i0 + 4, i0 + 7, i0 + 6 });
            }
            Bar(Vector3.zero, new Vector3(0, 0.5f, 0), 0.06f);
            Bar(new Vector3(0, 0.5f, 0), new Vector3(-0.32f, 0.95f, 0), 0.055f);
            Bar(new Vector3(0, 0.5f, 0), new Vector3(0.32f, 0.95f, 0), 0.055f);
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        public static Mesh Torus(float major = 1f, float minor = 0.05f, int seg = 48, int side = 8, string name = "BVR2_Torus")
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2; var c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                for (int j = 0; j <= side; j++)
                {
                    float b = j / (float)side * Mathf.PI * 2; var dir = c * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    v.Add(c * major + dir * minor); n.Add(dir); uv.Add(new Vector2(i / (float)seg, j / (float)side));
                }
            }
            for (int i = 0; i < seg; i++) for (int j = 0; j < side; j++)
                {
                    int a = i * (side + 1) + j, b = a + side + 1;
                    tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tris, 0); m.RecalculateBounds(); return m;
        }

        public static Mesh Quad(string name = "BVR2_Quad")
        {
            var m = new Mesh { name = name };
            m.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(-1, 1, 0), new Vector3(1, 1, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.normals = new[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
            m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 2);
            return m;
        }

        /// <summary>Partial cylinder shell (for wall-layer cut-away): angles in degrees, along +Z.</summary>
        public static Mesh Shell(float innerR, float outerR, float length, float a0, float a1, int seg = 24, string name = "BVR2_Shell")
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            void Strip(float r, bool flip)
            {
                int start = v.Count;
                for (int i = 0; i <= seg; i++)
                {
                    float a = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad;
                    var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                    v.Add(d * r + Vector3.forward * -length * 0.5f); v.Add(d * r + Vector3.forward * length * 0.5f);
                    uv.Add(new Vector2(i / (float)seg * (a1 - a0) / 10f, 0)); uv.Add(new Vector2(i / (float)seg * (a1 - a0) / 10f, length));
                }
                for (int i = 0; i < seg; i++)
                {
                    int a = start + i * 2;
                    if (flip) tris.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
                    else tris.AddRange(new[] { a, a + 2, a + 1, a + 2, a + 3, a + 1 });
                }
            }
            Strip(outerR, false); Strip(innerR, true);
            // end caps (cross-section faces) – the "cut" that shows the layer thickness
            foreach (float zz in new[] { -length * 0.5f, length * 0.5f })
            {
                int start = v.Count;
                for (int i = 0; i <= seg; i++)
                {
                    float a = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                    v.Add(d * innerR + Vector3.forward * zz); v.Add(d * outerR + Vector3.forward * zz);
                    uv.Add(new Vector2(i / (float)seg * 4f, 0)); uv.Add(new Vector2(i / (float)seg * 4f, 1));
                }
                for (int i = 0; i < seg; i++)
                {
                    int a = start + i * 2;
                    if (zz > 0) tris.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
                    else tris.AddRange(new[] { a, a + 2, a + 1, a + 2, a + 3, a + 1 });
                }
            }
            // side faces along the length at both arc ends, so the slice is closed from every angle
            foreach (float ae in new[] { a0, a1 })
            {
                int start = v.Count; float a = ae * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v.Add(d * innerR - Vector3.forward * length * 0.5f); v.Add(d * outerR - Vector3.forward * length * 0.5f);
                v.Add(d * innerR + Vector3.forward * length * 0.5f); v.Add(d * outerR + Vector3.forward * length * 0.5f);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, length)); uv.Add(new Vector2(1, length));
                if (ae == a1) tris.AddRange(new[] { start, start + 1, start + 2, start + 2, start + 1, start + 3 });
                else tris.AddRange(new[] { start, start + 2, start + 1, start + 2, start + 3, start + 1 });
            }
            // vertex colour: cut faces (caps and sides) a little darker and redder, like a fresh section
            var cols = new Color[v.Count]; int sideCount = (seg + 1) * 4;
            for (int i = 0; i < cols.Length; i++) cols[i] = i < sideCount ? Color.white : new Color(0.86f, 0.72f, 0.74f, 1f);
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0); m.colors = cols; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        /// <summary>Flat ring (annulus) in the XY plane facing -Z, uv.x along the ring.</summary>
        public static Mesh Ring(float inner, float outer, float fromDeg = 0, float toDeg = 360, int seg = 64, string name = "BVR2_Ring")
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)seg) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v.Add(d * inner); v.Add(d * outer); uv.Add(new Vector2(i / (float)seg, 0)); uv.Add(new Vector2(i / (float)seg, 1)); n.Add(-Vector3.forward); n.Add(-Vector3.forward);
            }
            for (int i = 0; i < seg; i++) { int a = i * 2; tris.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 }); }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetUVs(0, uv); m.SetNormals(n); m.SetTriangles(tris, 0); m.RecalculateBounds(); return m;
        }

        /// <summary>Open cone along +Z (scanner beam): apex at origin, base radius 1 at z = 1.</summary>
        public static Mesh Cone(int seg = 24, string name = "BVR2_Cone")
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v.Add(Vector3.zero); v.Add(d + Vector3.forward); uv.Add(new Vector2(i / (float)seg, 0)); uv.Add(new Vector2(i / (float)seg, 1));
            }
            for (int i = 0; i < seg; i++) { int a = i * 2; tris.AddRange(new[] { a, a + 1, a + 3 }); }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }
}

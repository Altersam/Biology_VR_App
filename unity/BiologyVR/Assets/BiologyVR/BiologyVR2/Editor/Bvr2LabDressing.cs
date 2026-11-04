using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// Dressing of the prologue lab after the concept art (Сцены/1 сцена. Лаборатория): the SecVarLab room is
    /// a bare sci-fi shell, the concept is a warm, lived-in biology lab. This builds, procedurally and
    /// deterministically, a warm palette for the room's big surfaces, eight coloured lamp pools (pendants,
    /// desk lamp, glowing jars, screens), wall shelving full of specimen jars, books, flasks and plants, big
    /// potted plants, hanging lamps, neon trims, an information screen and wall posters, bench clutter (test
    /// tubes, sample containers, petri dishes, a large teaching model of an erythrocyte) and a slowly turning
    /// DNA hologram. Props are placed with raycasts against the room (temporary colliders) so they stand on
    /// the floor, hug free walls and never cut through existing geometry. Everything is merged into a few
    /// meshes (vertex-coloured toon / emissive / glass) — a handful of draw calls, ~30k triangles.
    /// </summary>
    public static class Bvr2LabDressing
    {
        const string Dir = Bvr2SceneBuilder.Root + "/Generated/Lab/Dressing";
        static readonly Vector3 MainRoom = new Vector3(-0.6f, 0, -0.5f); // Bvr2SceneBuilder.SecVarMainRoom

        // ------------------------------------------------------------------ entry
        public static string Build(Transform labRoot, GameObject room, Font font)
        {
            Directory.CreateDirectory(Dir);
            var log = new List<string>();
            Repaint(log);
            var holder = new GameObject("Dressing").transform; holder.SetParent(labRoot, false);
            var cols = AddColliders(room);
            try
            {
                var k = new Kit(labRoot);
                Furnish(k, labRoot, holder, font, log);
                k.Emit(holder, log);
                if (k.why.Count > 0) Debug.Log("[BVR2] Lab dressing blocked: " + string.Join(" | ", k.why));
                Lights(labRoot, room, holder, log);
            }
            finally { foreach (var c in cols) if (c) UnityEngine.Object.DestroyImmediate(c); }
            string msg = "[BVR2] Lab dressing: " + string.Join("; ", log);
            Debug.Log(msg); return msg;
        }

        // ------------------------------------------------------------------ room palette
        /// <summary>The big surfaces get warm ramps (cream walls, honey-wood floor, copper decor panels); metals,
        /// screens and glass keep their colours — the concept's warm room with cyan accents.</summary>
        static void Repaint(List<string> log)
        {
            int n = 0;
            void Ramp(string mat, Color shade, Color mid, Color lit, Color? baseCol = null)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Bvr2SceneBuilder.Root + "/Generated/Lab/Materials/" + mat + ".mat");
                if (!m) return;
                m.SetTexture("_Ramp", RampTex("Room_" + mat, shade, mid, lit));
                m.SetColor("_BaseColor", baseCol ?? Color.white); m.SetFloat("_VertexTint", 0);
                EditorUtility.SetDirty(m); n++;
            }
            Ramp("Walls", new Color(0.46f, 0.34f, 0.28f), new Color(0.76f, 0.62f, 0.5f), new Color(0.9f, 0.79f, 0.66f));
            Wainscot("Walls", 0.95f, new Color(0.62f, 0.38f, 0.22f));
            Wainscot("KB3D_CPI_MetalPanelLBeigeTrimA", 0.95f, new Color(0.62f, 0.38f, 0.22f));
            Ramp("Floor", new Color(0.36f, 0.22f, 0.13f), new Color(0.66f, 0.44f, 0.27f), new Color(0.82f, 0.60f, 0.38f));
            Ramp("KB3D_CPI_ConcreteFloorDGray", new Color(0.36f, 0.22f, 0.13f), new Color(0.62f, 0.42f, 0.26f), new Color(0.78f, 0.56f, 0.36f));
            Ramp("KB3D_CPI_MetalPanelsDecor", new Color(0.40f, 0.20f, 0.12f), new Color(0.80f, 0.46f, 0.26f), new Color(1f, 0.72f, 0.46f));
            Ramp("KB3D_CPI_MetalPanelLBeigeTrimA", new Color(0.46f, 0.35f, 0.27f), new Color(0.78f, 0.66f, 0.52f), new Color(0.92f, 0.83f, 0.7f));
            Ramp("KB3D_CPI_ConcreteWhiteTrim", new Color(0.62f, 0.52f, 0.44f), new Color(0.93f, 0.86f, 0.76f), new Color(1f, 0.97f, 0.9f));
            Ramp("KB3D_CPI_MetalPanelWhiteTrimB", new Color(0.62f, 0.52f, 0.44f), new Color(0.93f, 0.86f, 0.76f), new Color(1f, 0.97f, 0.9f));
            Ramp("KB3D_CPI_WhiteMetalTrimA", new Color(0.55f, 0.47f, 0.42f), new Color(0.86f, 0.8f, 0.72f), new Color(0.97f, 0.93f, 0.87f));
            Ramp("KB3D_CPI_PlyWoodTrim", new Color(0.40f, 0.24f, 0.13f), new Color(0.74f, 0.50f, 0.30f), new Color(0.92f, 0.70f, 0.46f));
            Ramp("KB3D_CPI_TrashAtlas", new Color(0.30f, 0.34f, 0.40f), new Color(0.55f, 0.68f, 0.80f), new Color(0.80f, 0.90f, 1f));
            Ramp("KB3D_CPI_DetailMaterialApartmentAtlas", new Color(0.42f, 0.30f, 0.26f), new Color(0.80f, 0.62f, 0.48f), new Color(0.98f, 0.84f, 0.68f));
            Ramp("KB3D_CPI_GlassBottleTrim", new Color(0.12f, 0.42f, 0.36f), new Color(0.26f, 0.76f, 0.62f), new Color(0.6f, 1f, 0.86f));
            Ramp("KB3D_CPI_LeatherTrimA", new Color(0.30f, 0.12f, 0.08f), new Color(0.62f, 0.28f, 0.16f), new Color(0.86f, 0.46f, 0.28f));
            log.Add($"repainted {n} room materials");
            void Wainscot(string mat, float height, Color tint)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Bvr2SceneBuilder.Root + "/Generated/Lab/Materials/" + mat + ".mat");
                if (!m) return; m.SetVector("_Wainscot", new Vector4(height, tint.r, tint.g, tint.b)); EditorUtility.SetDirty(m);
            }
        }

        // ------------------------------------------------------------------ physics helpers (room only)
        static List<Collider> AddColliders(GameObject room)
        {
            var list = new List<Collider>();
            foreach (var mf in room.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh || !mf.gameObject.activeInHierarchy) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; list.Add(mc);
            }
            Physics.SyncTransforms(); return list;
        }

        sealed class Kit
        {
            public readonly Transform lab;
            public readonly Group toon = new Group("Toon"), glow = new Group("Glow"), glass = new Group("Glass");
            public readonly Dictionary<string, Group> textured = new Dictionary<string, Group>();
            public Kit(Transform lab) { this.lab = lab; }
            public Vector3 W(Vector3 local) => lab.TransformPoint(local);
            public Vector3 L(Vector3 world) => lab.InverseTransformPoint(world);
            /// <summary>Distance from a lab-local point along a lab-local direction to the room, or max.</summary>
            public float Cast(Vector3 from, Vector3 dir, float max = 8f)
            {
                return Physics.Raycast(W(from), lab.TransformDirection(dir).normalized, out var hit, max * lab.lossyScale.y) ? hit.distance / lab.lossyScale.y : max;
            }
            /// <summary>True when a lab-local box is free of room geometry.</summary>
            public readonly List<string> why = new List<string>();
            public bool Free(Vector3 centre, Vector3 size, Quaternion rot)
            {
                var hits = Physics.OverlapBox(W(centre), size * 0.5f * lab.lossyScale.y, lab.rotation * rot);
                if (hits.Length == 0) return true;
                if (why.Count < 40) why.Add($"({centre.x:F2},{centre.y:F2},{centre.z:F2})→{string.Join("/", System.Linq.Enumerable.Select(hits, h => h.name))}");
                return false;
            }
            public Group Tex(string key) { if (!textured.TryGetValue(key, out var g)) textured[key] = g = new Group(key); return g; }

            public void Emit(Transform holder, List<string> log)
            {
                int tris = 0;
                tris += Write(toon, holder, Mats.Toon); tris += Write(glow, holder, Mats.Glow); tris += Write(glass, holder, Mats.Glass);
                foreach (var kv in textured) tris += Write(kv.Value, holder, Mats.Textured(kv.Key));
                log.Add($"props {tris} tris in {3 + textured.Count} meshes [" + string.Join("; ", log2) + "]");
            }

            int Write(Group g, Transform holder, Material mat)
            {
                if (g.t.Count == 0 || !mat) return 0;
                var mesh = StoreMesh("LabDress_" + g.name, g);
                var go = new GameObject(g.name); go.transform.SetParent(holder, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                log2.Add($"{g.name}: {g.v.Count} v, {g.t.Count / 3} t, bounds {mesh.bounds.center:F2} {mesh.bounds.size:F2}");
                return g.t.Count / 3;
            }
            public readonly List<string> log2 = new List<string>();
        }

        // ------------------------------------------------------------------ mesh accumulation
        sealed class Group
        {
            public readonly string name;
            public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
            public readonly List<Color> c = new List<Color>(); public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> t = new List<int>();
            public Group(string name) { this.name = name; }
            int V(Vector3 p, Vector3 nn, Color col, Vector2 u) { v.Add(p); n.Add(nn); c.Add(col); uv.Add(u); return v.Count - 1; }
            void Tri(int a, int b, int d)
            {
                var g = Vector3.Cross(v[b] - v[a], v[d] - v[a]);
                if (Vector3.Dot(g, n[a] + n[b] + n[d]) < 0) { t.Add(a); t.Add(d); t.Add(b); } else { t.Add(a); t.Add(b); t.Add(d); }
            }
            static Vector3 N(Matrix4x4 m, Vector3 local) => m.inverse.transpose.MultiplyVector(local).normalized;

            /// <summary>Unit cube (−0.5..0.5) through m.</summary>
            public void Box(Matrix4x4 m, Color col)
            {
                var it = m.inverse.transpose;
                for (int f = 0; f < 6; f++)
                {
                    Vector3 nl = f switch { 0 => Vector3.right, 1 => Vector3.left, 2 => Vector3.up, 3 => Vector3.down, 4 => Vector3.forward, _ => Vector3.back };
                    Vector3 a = Mathf.Abs(nl.y) > 0.5f ? Vector3.right : Vector3.up, b = Vector3.Cross(nl, a);
                    var nw = it.MultiplyVector(nl).normalized;
                    int i0 = V(m.MultiplyPoint3x4(nl * 0.5f - a * 0.5f - b * 0.5f), nw, col, new Vector2(0, 0));
                    int i1 = V(m.MultiplyPoint3x4(nl * 0.5f + a * 0.5f - b * 0.5f), nw, col, new Vector2(1, 0));
                    int i2 = V(m.MultiplyPoint3x4(nl * 0.5f + a * 0.5f + b * 0.5f), nw, col, new Vector2(1, 1));
                    int i3 = V(m.MultiplyPoint3x4(nl * 0.5f - a * 0.5f + b * 0.5f), nw, col, new Vector2(0, 1));
                    Tri(i0, i1, i2); Tri(i0, i2, i3);
                }
            }

            /// <summary>Unit frustum along y (−0.5..0.5), bottom radius r0, top r1 (unit = 0.5 = full width).</summary>
            public void Cyl(Matrix4x4 m, Color col, int seg = 12, float r0 = 0.5f, float r1 = 0.5f, bool capTop = true, bool capBottom = true, Color? topCol = null)
            {
                float slope = (r0 - r1); // outward normal tilt per unit height
                int start = v.Count;
                for (int i = 0; i <= seg; i++)
                {
                    float a = i * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    var nl = (d + Vector3.up * slope).normalized;
                    V(m.MultiplyPoint3x4(d * r0 + Vector3.down * 0.5f), N(m, nl), col, new Vector2(i / (float)seg, 0));
                    V(m.MultiplyPoint3x4(d * r1 + Vector3.up * 0.5f), N(m, nl), col, new Vector2(i / (float)seg, 1));
                }
                for (int i = 0; i < seg; i++) { int a = start + i * 2; Tri(a, a + 1, a + 3); Tri(a, a + 3, a + 2); }
                if (capTop && r1 > 0.001f) Disc(m, col, seg, r1, 0.5f, Vector3.up, topCol ?? col);
                if (capBottom && r0 > 0.001f) Disc(m, col, seg, r0, -0.5f, Vector3.down, col);
            }
            void Disc(Matrix4x4 m, Color col, int seg, float r, float y, Vector3 nl, Color c2)
            {
                var nw = N(m, nl); int centre = V(m.MultiplyPoint3x4(new Vector3(0, y, 0)), nw, c2, new Vector2(0.5f, 0.5f)); int first = v.Count;
                for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2f / seg; V(m.MultiplyPoint3x4(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)), nw, c2, new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f)); }
                for (int i = 0; i < seg; i++) Tri(centre, first + i, first + i + 1);
            }

            /// <summary>Unit sphere (radius 0.5) through m.</summary>
            public void Sphere(Matrix4x4 m, Color col, int lat = 6, int lon = 10)
            {
                int start = v.Count;
                for (int y = 0; y <= lat; y++)
                {
                    float th = y * Mathf.PI / lat;
                    for (int x = 0; x <= lon; x++)
                    {
                        float ph = x * Mathf.PI * 2f / lon;
                        var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                        V(m.MultiplyPoint3x4(d * 0.5f), N(m, d), col, new Vector2(x / (float)lon, y / (float)lat));
                    }
                }
                for (int y = 0; y < lat; y++)
                    for (int x = 0; x < lon; x++)
                    {
                        int a = start + y * (lon + 1) + x, b = a + lon + 1;
                        if (y > 0) Tri(a, b, a + 1);
                        if (y < lat - 1) Tri(a + 1, b, b + 1);
                    }
            }

            /// <summary>Unit quad in the local XY plane facing +Z (uv 0..1).</summary>
            public void Quad(Matrix4x4 m, Color col)
            {
                var nw = N(m, Vector3.forward);
                int a = V(m.MultiplyPoint3x4(new Vector3(-0.5f, -0.5f, 0)), nw, col, new Vector2(1, 0));
                int b = V(m.MultiplyPoint3x4(new Vector3(0.5f, -0.5f, 0)), nw, col, new Vector2(0, 0));
                int c2 = V(m.MultiplyPoint3x4(new Vector3(0.5f, 0.5f, 0)), nw, col, new Vector2(0, 1));
                int d = V(m.MultiplyPoint3x4(new Vector3(-0.5f, 0.5f, 0)), nw, col, new Vector2(1, 1));
                Tri(a, b, c2); Tri(a, c2, d);
            }
        }

        /// <summary>Writes a group into a mesh asset, updating an existing asset in place (CopySerialized would keep
        /// the old GPU buffers until the next domain reload).</summary>
        static Mesh StoreMesh(string name, Group g)
        {
            string path = $"{Dir}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = !mesh;
            if (fresh) mesh = new Mesh { name = name };
            mesh.Clear(); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(g.v); mesh.SetNormals(g.n); mesh.SetColors(g.c); mesh.SetUVs(0, g.uv); mesh.SetTriangles(g.t, 0);
            mesh.RecalculateBounds();
            if (fresh) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Matrix4x4 M(Vector3 p, Quaternion r, Vector3 s) => Matrix4x4.TRS(p, r, s);
        static Matrix4x4 M(Vector3 p, Vector3 s) => Matrix4x4.TRS(p, Quaternion.identity, s);

        /// <summary>A local frame: origin on a wall/floor, yaw = direction the front faces (into the room).</summary>
        readonly struct Frame
        {
            public readonly Vector3 o; public readonly Quaternion r;
            public Frame(Vector3 origin, float yaw) { o = origin; r = Quaternion.Euler(0, yaw, 0); }
            public Vector3 P(float x, float y, float z) => o + r * new Vector3(x, y, z);
            public Matrix4x4 M(float x, float y, float z, Vector3 size, Vector3 euler = default) => Matrix4x4.TRS(P(x, y, z), r * Quaternion.Euler(euler), size);
        }

        // ------------------------------------------------------------------ palette
        static readonly Color Wood = new Color(0.62f, 0.38f, 0.22f), WoodDark = new Color(0.36f, 0.21f, 0.12f), WoodLight = new Color(0.82f, 0.58f, 0.36f);
        static readonly Color Brass = new Color(0.86f, 0.62f, 0.30f), Steel = new Color(0.72f, 0.76f, 0.80f), Charcoal = new Color(0.20f, 0.22f, 0.25f);
        static readonly Color Leaf = new Color(0.30f, 0.66f, 0.30f), LeafLight = new Color(0.52f, 0.82f, 0.36f), LeafDark = new Color(0.16f, 0.44f, 0.24f);
        static readonly Color Terracotta = new Color(0.82f, 0.42f, 0.24f), Paper = new Color(0.96f, 0.92f, 0.82f);
        static readonly Color[] Liquids = { new Color(0.2f, 0.95f, 1f), new Color(0.55f, 1f, 0.35f), new Color(1f, 0.35f, 0.75f), new Color(1f, 0.72f, 0.2f), new Color(0.55f, 0.45f, 1f), new Color(1f, 0.32f, 0.28f) };
        static readonly Color[] Books = { new Color(0.75f, 0.2f, 0.18f), new Color(0.15f, 0.4f, 0.65f), new Color(0.2f, 0.55f, 0.35f), new Color(0.9f, 0.7f, 0.25f), new Color(0.5f, 0.25f, 0.55f), new Color(0.9f, 0.5f, 0.2f), new Color(0.85f, 0.85f, 0.8f), new Color(0.25f, 0.25f, 0.3f) };

        // ------------------------------------------------------------------ the layout
        static void Furnish(Kit k, Transform lab, Transform holder, Font font, List<string> log)
        {
            var rnd = new Random(1907);
            int shelves = 0, plants = 0, pendants = 0;

            // 1) left wall: two shelving units in front of the bare panel (between the white corner block and
            //    the lit pillar), hugging the actual wall
            foreach (var (zc, w) in new[] { (-1.55f, 1.0f), (-0.55f, 0.9f) })
            {
                float wall = -1.2f - k.Cast(new Vector3(-1.2f, 1.2f, zc), Vector3.left, 4f);
                if (wall < -3.4f || wall > -2.5f) { log.Add($"shelf z{zc}: wall at {wall:F2}, skipped"); continue; }
                bool ok = false;
                for (float d = 0.01f; d <= 0.26f && !ok; d += 0.025f)
                {
                    var f = new Frame(new Vector3(wall + d, 0, zc), 90f);
                    if (!k.Free(f.P(0, 1.18f, 0.2f), new Vector3(w - 0.08f, 1.96f, 0.3f), f.r)) continue;
                    int before = k.toon.v.Count; Shelf(k, f, w, 2.25f, 0.36f, 5, rnd); shelves++; ok = true;
                    log.Add($"shelf verts {k.toon.v.Count - before} at {f.o:F2}");
                    log.Add($"shelf z{zc}: wall {wall:F2} + {d:F2}");
                }
                if (!ok) log.Add($"shelf z{zc}: blocked");
            }
            // 2) the right corner by the lounge: a low cabinet with jars and a plant on it if free
            {
                float wall = 0.9f + k.Cast(new Vector3(0.9f, 1.2f, -3.0f), Vector3.right, 3f);
                var f = new Frame(new Vector3(Mathf.Min(wall, 2.3f) - 0.01f, 0, -3.0f), 270f);
                if (wall < 2.6f && k.Free(f.P(0, 0.5f, 0.2f), new Vector3(0.8f, 0.9f, 0.3f), f.r)) { Shelf(k, f, 0.85f, 1.05f, 0.34f, 2, rnd); shelves++; }
            }
            // 3) big potted plants on free floor spots near the walls and corners
            foreach (var (x0, z0, s) in new[] { (-2.55f, -2.6f, 1.15f), (-2.55f, 0.2f, 0.9f), (1.6f, -3.05f, 1.1f), (-0.75f, -3.2f, 0.85f), (1.65f, 1.45f, 0.95f), (-6.1f, 2.85f, 0.85f), (-6.1f, 1.2f, 0.75f), (0.2f, -3.25f, 0.8f), (-2.5f, -0.75f, 0.75f) })
            {
                var centre = x0 < -3.2f ? new Vector3(-4.9f, 0, 2.0f) : new Vector3(-0.3f, 0, -0.8f);
                var inward = (centre - new Vector3(x0, 0, z0)).normalized;
                for (float t = 0f; t <= 0.6f; t += 0.06f)
                {
                    float x = x0 + inward.x * t, z = z0 + inward.z * t;
                    float floor = 1.5f - k.Cast(new Vector3(x, 1.5f, z), Vector3.down, 2f);
                    if (floor > 0.12f || floor < -0.1f) continue;
                    if (!k.Free(new Vector3(x, floor + 0.5f * s + 0.03f, z), new Vector3(0.38f, 0.9f, 0.38f) * s, Quaternion.identity)) continue;
                    Plant(k, new Vector3(x, floor, z), s, rnd); plants++; break;
                }
            }
            // 4) pendant lamps over the room (warm, as in the concept) — under the actual ceiling
            var pend = new List<Vector3>();
            foreach (var (x, z) in new[] { (-1.9f, -1.7f), (0.85f, -1.45f), (-1.95f, 0.95f), (0.85f, 0.95f), (-1.6f, 2.0f) })
            {
                float ceil = 1.6f + k.Cast(new Vector3(x, 1.6f, z), Vector3.up, 3f);
                if (ceil > 4.2f || ceil < 2.45f) continue;
                float bulbY = Mathf.Min(ceil - 0.45f, 2.55f);
                Pendant(k, new Vector3(x, bulbY, z), ceil); pend.Add(new Vector3(x, bulbY - 0.08f, z)); pendants++;
            }
            PendantLamps = pend;
            // 5) bench clutter on the teal workbench (left part; the instruments float at its right end)
            Bench(k, rnd, log);
            // 6) information screen hanging in the left front, posters on free walls
            InfoScreen(k, lab, holder, font, log);
            Posters(k, lab, holder, font, log);
            // 7) neon trims: cyan along the left wall top, orange under the shelves
            NeonFrames(k, log);
            // 8) DNA hologram over the bench
            Dna(lab, holder);
            // 9) the room's own empty racks and tables get jars, books and plants (found by raycasts)
            int items = Populate(k, new Rect(1.35f, -2.2f, 1.1f, 2.9f), rnd) + Populate(k, new Rect(1.2f, 0.0f, 1.2f, 1.2f), rnd);
            log.Add($"rack items {items}");
            // 10) the right wall: a cork board with notes and a clock; vines over the left shelves
            WallExtras(k, rnd, log);
            Vines(k, rnd);
            log.Add($"shelves {shelves}, plants {plants}, pendants {pendants}");
        }

        public static List<Vector3> PendantLamps = new List<Vector3>();
        /// <summary>Lab-local points for the bubbles (Bvr2HoloFx.LabFx): inside the big sample jars and the test tubes.</summary>
        public static List<Vector3> BubbleSpots = new List<Vector3>(), TubeSpots = new List<Vector3>();

        // ------------------------------------------------------------------ props
        static void Shelf(Kit k, Frame f, float width, float height, float depth, int levels, Random rnd)
        {
            float hw = width / 2f, th = 0.03f;
            // carcass: back panel, sides, top, bottom kick
            k.toon.Box(f.M(0, height / 2f, 0.012f, new Vector3(width, height, 0.02f)), WoodDark);
            k.toon.Box(f.M(-hw + th / 2f, height / 2f, depth / 2f, new Vector3(th, height, depth)), Wood);
            k.toon.Box(f.M(hw - th / 2f, height / 2f, depth / 2f, new Vector3(th, height, depth)), Wood);
            k.toon.Box(f.M(0, height - th / 2f, depth / 2f + 0.01f, new Vector3(width + 0.04f, th, depth + 0.02f)), WoodLight);
            k.toon.Box(f.M(0, 0.05f, depth / 2f, new Vector3(width, 0.1f, depth - 0.02f)), WoodDark);
            float gap = (height - 0.16f) / levels;
            for (int l = 0; l < levels; l++)
            {
                float y = 0.1f + l * gap;
                k.toon.Box(f.M(0, y + th / 2f, depth / 2f, new Vector3(width - 2 * th, th, depth - 0.02f)), WoodLight);
                // a thin brass lip
                k.toon.Box(f.M(0, y + th + 0.006f, depth - 0.012f, new Vector3(width - 2 * th, 0.012f, 0.012f)), Brass);
                FillShelf(k, f, -hw + th + 0.03f, hw - th - 0.03f, y + th, Mathf.Min(gap - 0.06f, 0.42f), depth, rnd, l);
            }
            // LED strip under the top (cyan) and a glow under the first shelf (warm)
            k.glow.Box(f.M(0, height - th - 0.012f, depth - 0.04f, new Vector3(width - 0.1f, 0.012f, 0.015f)), new Color(0.35f, 0.9f, 1f));
        }

        static void FillShelf(Kit k, Frame f, float x0, float x1, float y, float maxH, float depth, Random rnd, int level)
        {
            float x = x0;
            int guard = 0;
            while (x < x1 - 0.06f && guard++ < 40)
            {
                double pick = rnd.NextDouble();
                float room = x1 - x;
                if (pick < 0.34) { float w = Jar(k, f, x, y, depth, maxH, rnd); x += w; }
                else if (pick < 0.62 && room > 0.2f) { float w = BookRow(k, f, x, Mathf.Min(x1, x + 0.18f + (float)rnd.NextDouble() * 0.2f), y, depth, maxH, rnd); x += w; }
                else if (pick < 0.75) { float w = SmallPlant(k, f, x, y, depth, maxH, rnd); x += w; }
                else if (pick < 0.88) { float w = Flask(k, f, x, y, depth, maxH, rnd); x += w; }
                else { float w = Crate(k, f, x, y, depth, maxH, rnd); x += w; }
                x += 0.025f + (float)rnd.NextDouble() * 0.04f;
            }
        }

        static float Jar(Kit k, Frame f, float x, float y, float depth, float maxH, Random rnd)
        {
            float r = 0.045f + (float)rnd.NextDouble() * 0.035f, h = Mathf.Min(maxH, 0.12f + (float)rnd.NextDouble() * 0.16f);
            float cx = x + r, cz = depth * 0.5f + (float)(rnd.NextDouble() - 0.5) * 0.05f;
            var liquid = Liquids[rnd.Next(Liquids.Length)];
            float fill = 0.55f + (float)rnd.NextDouble() * 0.35f;
            k.glow.Cyl(f.M(cx, y + h * fill / 2f, cz, new Vector3(r * 1.8f, h * fill, r * 1.8f)), liquid, 10);
            if (rnd.NextDouble() < 0.5) k.glow.Sphere(f.M(cx, y + h * fill * 0.55f, cz, Vector3.one * r * 0.9f), Color.Lerp(liquid, Color.white, 0.4f), 4, 6); // a specimen inside
            k.glass.Cyl(f.M(cx, y + h / 2f, cz, new Vector3(r * 2f, h, r * 2f)), new Color(0.8f, 0.95f, 1f, 1f), 12, 0.5f, 0.5f, false, false);
            k.toon.Cyl(f.M(cx, y + h + 0.012f, cz, new Vector3(r * 2.1f, 0.024f, r * 2.1f)), rnd.NextDouble() < 0.5 ? Brass : Charcoal, 12);
            return r * 2f;
        }

        static float BookRow(Kit k, Frame f, float x0, float x1, float y, float depth, float maxH, Random rnd)
        {
            float x = x0; int n = 0;
            bool lying = rnd.NextDouble() < 0.25;
            if (lying)
            {
                float hh = 0; int stack = 2 + rnd.Next(4); float w = Mathf.Min(x1 - x0, 0.2f + (float)rnd.NextDouble() * 0.06f);
                for (int i = 0; i < stack && hh < maxH - 0.04f; i++)
                {
                    float t = 0.025f + (float)rnd.NextDouble() * 0.02f, d = depth * (0.6f + (float)rnd.NextDouble() * 0.2f);
                    k.toon.Box(f.M(x0 + w / 2f + (float)(rnd.NextDouble() - 0.5) * 0.02f, y + hh + t / 2f, depth * 0.5f, new Vector3(w * (0.85f + (float)rnd.NextDouble() * 0.15f), t, d), new Vector3(0, (float)(rnd.NextDouble() - 0.5) * 14f, 0)), Books[rnd.Next(Books.Length)]);
                    hh += t;
                }
                return w;
            }
            while (x < x1 && n < 12)
            {
                float t = 0.022f + (float)rnd.NextDouble() * 0.022f, h = Mathf.Min(maxH - 0.02f, 0.16f + (float)rnd.NextDouble() * 0.12f), d = depth * (0.62f + (float)rnd.NextDouble() * 0.2f);
                if (x + t > x1) break;
                float lean = n == 0 && rnd.NextDouble() < 0.3 ? 12f : 0f;
                var col = Books[rnd.Next(Books.Length)];
                k.toon.Box(f.M(x + t / 2f, y + h / 2f, depth - d / 2f - 0.02f, new Vector3(t, h, d), new Vector3(0, 0, lean)), col);
                if (rnd.NextDouble() < 0.6) k.toon.Box(f.M(x + t / 2f, y + h * 0.75f, depth - 0.019f, new Vector3(t * 1.02f, 0.012f, 0.004f)), Brass); // spine band
                x += t + 0.002f; n++;
            }
            return x - x0;
        }

        static float SmallPlant(Kit k, Frame f, float x, float y, float depth, float maxH, Random rnd)
        {
            float r = 0.05f + (float)rnd.NextDouble() * 0.02f; float cx = x + r, cz = depth * 0.5f;
            k.toon.Cyl(f.M(cx, y + 0.045f, cz, new Vector3(r * 2f, 0.09f, r * 2f)), Terracotta, 10, 0.42f, 0.5f);
            int leaves = 5 + rnd.Next(4);
            for (int i = 0; i < leaves; i++)
            {
                float a = i * 360f / leaves + (float)rnd.NextDouble() * 30f, tilt = 25f + (float)rnd.NextDouble() * 35f;
                float len = Mathf.Min(maxH * 0.9f, 0.1f + (float)rnd.NextDouble() * 0.09f);
                var rot = Quaternion.Euler(0, a, 0) * Quaternion.Euler(tilt, 0, 0);
                var c = f.P(cx, y + 0.09f, cz) + f.r * (rot * Vector3.up) * len * 0.5f;
                k.toon.Sphere(Matrix4x4.TRS(c, f.r * rot, new Vector3(len * 0.38f, len, len * 0.12f)), Color.Lerp(Leaf, LeafLight, (float)rnd.NextDouble()), 4, 6);
            }
            return r * 2f + 0.02f;
        }

        static float Flask(Kit k, Frame f, float x, float y, float depth, float maxH, Random rnd)
        {
            float r = 0.05f + (float)rnd.NextDouble() * 0.02f, h = Mathf.Min(maxH - 0.02f, 0.16f); float cx = x + r, cz = depth * 0.5f;
            var liquid = Liquids[rnd.Next(Liquids.Length)];
            k.glow.Cyl(f.M(cx, y + h * 0.2f, cz, new Vector3(r * 1.7f, h * 0.38f, r * 1.7f)), liquid, 10, 0.5f, 0.3f);
            k.glass.Cyl(f.M(cx, y + h * 0.33f, cz, new Vector3(r * 2f, h * 0.66f, r * 2f)), Color.white, 12, 0.5f, 0.16f, false, true);
            k.glass.Cyl(f.M(cx, y + h * 0.83f, cz, new Vector3(r * 0.7f, h * 0.34f, r * 0.7f)), Color.white, 8, 0.5f, 0.5f, false, false);
            return r * 2f;
        }

        static float Crate(Kit k, Frame f, float x, float y, float depth, float maxH, Random rnd)
        {
            float w = 0.12f + (float)rnd.NextDouble() * 0.1f, h = Mathf.Min(maxH - 0.03f, 0.08f + (float)rnd.NextDouble() * 0.1f);
            var c = rnd.NextDouble() < 0.5 ? new Color(0.94f, 0.94f, 0.9f) : Color.Lerp(WoodLight, Paper, 0.4f);
            k.toon.Box(f.M(x + w / 2f, y + h / 2f, depth * 0.52f, new Vector3(w, h, depth * 0.66f)), c);
            k.glow.Box(f.M(x + w * 0.25f, y + h * 0.7f, depth * 0.52f + depth * 0.33f + 0.001f, new Vector3(0.012f, 0.012f, 0.004f)), rnd.NextDouble() < 0.5 ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.6f, 0.2f)); // status LED
            return w;
        }

        static void Plant(Kit k, Vector3 pos, float s, Random rnd)
        {
            float pr = 0.17f * s, ph = 0.34f * s;
            k.toon.Cyl(M(pos + Vector3.up * ph / 2f, new Vector3(pr * 2f, ph, pr * 2f)), Terracotta, 14, 0.4f, 0.5f, true, true, new Color(0.25f, 0.16f, 0.1f));
            k.toon.Cyl(M(pos + Vector3.up * (ph + 0.015f), new Vector3(pr * 2.12f, 0.035f, pr * 2.12f)), Color.Lerp(Terracotta, WoodLight, 0.4f), 14);
            int leaves = 9 + rnd.Next(5);
            for (int i = 0; i < leaves; i++)
            {
                float a = i * 360f / leaves + (float)rnd.NextDouble() * 25f, tilt = 12f + (float)rnd.NextDouble() * 48f;
                float len = (0.38f + (float)rnd.NextDouble() * 0.34f) * s;
                var rot = Quaternion.Euler(0, a, 0) * Quaternion.Euler(tilt, 0, 0);
                var c = pos + Vector3.up * (ph + 0.02f) + rot * Vector3.up * len * 0.5f;
                var col = Color.Lerp(LeafDark, LeafLight, (float)rnd.NextDouble());
                k.toon.Sphere(Matrix4x4.TRS(c, rot, new Vector3(len * 0.34f, len, len * 0.08f)), col, 4, 8);
                k.toon.Cyl(Matrix4x4.TRS(pos + Vector3.up * (ph + 0.02f) + rot * Vector3.up * len * 0.12f, rot, new Vector3(0.012f * s, len * 0.3f, 0.012f * s)), LeafDark, 5, 0.5f, 0.5f, false, false);
            }
        }

        static void Pendant(Kit k, Vector3 bulb, float ceiling)
        {
            float cord = Mathf.Max(0.05f, ceiling - bulb.y - 0.12f);
            k.toon.Cyl(M(new Vector3(bulb.x, ceiling - cord / 2f, bulb.z), new Vector3(0.012f, cord, 0.012f)), Charcoal, 6, 0.5f, 0.5f, false, false);
            k.toon.Cyl(M(new Vector3(bulb.x, ceiling - 0.012f, bulb.z), new Vector3(0.1f, 0.024f, 0.1f)), Brass, 10);
            k.toon.Cyl(M(new Vector3(bulb.x, bulb.y + 0.07f, bulb.z), new Vector3(0.36f, 0.16f, 0.36f)), new Color(0.95f, 0.55f, 0.25f), 16, 0.5f, 0.16f, true, false);
            k.glow.Sphere(M(new Vector3(bulb.x, bulb.y - 0.01f, bulb.z), new Vector3(0.13f, 0.1f, 0.13f)), new Color(1f, 0.82f, 0.5f), 5, 10);
            k.glow.Cyl(M(new Vector3(bulb.x, bulb.y - 0.005f, bulb.z), new Vector3(0.34f, 0.006f, 0.34f)), new Color(1f, 0.7f, 0.35f), 16, 0.5f, 0.5f, false, true); // warm rim
        }

        // ------------------------------------------------------------------ the workbench
        static void Bench(Kit k, Random rnd, List<string> log)
        {
            // items stand on the low teal back table (top ≈ 0.53 m, x −2.25…1.1, z 3.1…3.8, behind the robot-arm table)
            // where it is free; each spot measures the top and checks for clearance
            int placed = 0; BubbleSpots.Clear(); TubeSpots.Clear();
            bool Spot(float x, float z, Vector3 size, out Frame f)
            {
                float top = 1.7f - k.Cast(new Vector3(x, 1.7f, z), Vector3.down, 1.7f);
                f = new Frame(new Vector3(x, top, z), 180f); // front toward the room (−z)
                if (top < 0.4f || top > 1.2f) { k.why.Add($"bench ({x:F2},{z:F2}) top {top:F2}"); return false; }
                if (!k.Free(new Vector3(x, top + size.y / 2f + 0.02f, z), size, Quaternion.identity)) return false;
                placed++; return true;
            }
            // test-tube rack with six coloured tubes
            if (Spot(-2.05f, 3.3f, new Vector3(0.36f, 0.2f, 0.12f), out var f))
            {
                k.toon.Box(f.M(0, 0.02f, 0, new Vector3(0.34f, 0.04f, 0.1f)), WoodLight);
                k.toon.Box(f.M(0, 0.1f, 0, new Vector3(0.34f, 0.018f, 0.1f)), WoodLight);
                for (int i = 0; i < 6; i++)
                {
                    float x = -0.14f + i * 0.056f; var c = Liquids[i % Liquids.Length];
                    k.glow.Cyl(f.M(x, 0.06f, 0, new Vector3(0.026f, 0.1f, 0.026f)), c, 8);
                    TubeSpots.Add(f.P(x, 0.02f, 0));
                    k.glass.Cyl(f.M(x, 0.1f, 0, new Vector3(0.03f, 0.18f, 0.03f)), Color.white, 8, 0.5f, 0.5f, false, true);
                }
            }
            // three big sample containers (red / blue / yellow, as on the concept's table)
            Color[] samples = { new Color(1f, 0.25f, 0.25f), new Color(0.25f, 0.55f, 1f), new Color(1f, 0.85f, 0.25f) };
            for (int i = 0; i < 3; i++)
                if (Spot(-1.95f + i * 0.13f, 3.6f, new Vector3(0.11f, 0.22f, 0.11f), out f))
                {
                    k.glow.Cyl(f.M(0, 0.07f, 0, new Vector3(0.09f, 0.14f, 0.09f)), samples[i], 12);
                    BubbleSpots.Add(f.P(0, 0.012f, 0));
                    k.glass.Cyl(f.M(0, 0.1f, 0, new Vector3(0.105f, 0.2f, 0.105f)), Color.white, 14, 0.5f, 0.5f, false, true);
                    k.toon.Cyl(f.M(0, 0.21f, 0, new Vector3(0.11f, 0.025f, 0.11f)), Steel, 14);
                }
            // petri dishes
            for (int i = 0; i < 3; i++)
                if (Spot(-1.6f + i * 0.1f, 3.25f, new Vector3(0.09f, 0.03f, 0.09f), out f))
                {
                    k.glow.Cyl(f.M(0, 0.008f, 0, new Vector3(0.075f, 0.008f, 0.075f)), Color.Lerp(Liquids[(i + 2) % Liquids.Length], Color.white, 0.3f), 12);
                    k.glass.Cyl(f.M(0, 0.012f, 0, new Vector3(0.09f, 0.022f, 0.09f)), Color.white, 14, 0.5f, 0.5f, true, false);
                }
            // microscope (right end of the bench, or behind the arms)
            foreach (var (x, z) in new[] { (0.75f, 3.4f), (0.55f, 3.55f), (-1.2f, 3.6f) })
                if (Spot(x, z, new Vector3(0.2f, 0.4f, 0.24f), out f))
                {
                    var mf = new Frame(f.o, 180f + 25f);
                    var white = new Color(0.92f, 0.92f, 0.9f);
                    k.toon.Box(mf.M(0, 0.015f, 0, new Vector3(0.16f, 0.03f, 0.2f)), white);
                    k.toon.Box(mf.M(0, 0.15f, 0.07f, new Vector3(0.05f, 0.26f, 0.05f), new Vector3(-12, 0, 0)), white);
                    k.toon.Box(mf.M(0, 0.1f, 0.0f, new Vector3(0.12f, 0.012f, 0.1f)), Charcoal);
                    k.toon.Cyl(mf.M(0, 0.24f, 0.0f, new Vector3(0.04f, 0.16f, 0.04f)), Charcoal, 10);
                    k.toon.Cyl(mf.M(0, 0.33f, -0.04f, new Vector3(0.03f, 0.1f, 0.03f)), Charcoal, 8);
                    k.glow.Cyl(mf.M(0, 0.03f, 0, new Vector3(0.03f, 0.004f, 0.03f)), new Color(1f, 0.95f, 0.8f), 8);
                    break;
                }
            // a large teaching model of an erythrocyte on a stand (the concept's "увеличенная модель эритроцита")
            foreach (var (x, z) in new[] { (0.35f, 3.45f), (-0.9f, 3.55f), (-2.0f, 3.62f) })
                if (Spot(x, z, new Vector3(0.3f, 0.48f, 0.3f), out f))
                {
                    var ef = new Frame(f.o, 180f - 20f);
                    k.toon.Cyl(ef.M(0, 0.015f, 0, new Vector3(0.18f, 0.03f, 0.18f)), Brass, 16);
                    k.toon.Cyl(ef.M(0, 0.12f, 0, new Vector3(0.02f, 0.2f, 0.02f)), Brass, 8, 0.5f, 0.5f, false, false);
                    var rr = ef.r * Quaternion.Euler(70, 0, 0); var red = new Color(0.92f, 0.16f, 0.16f);
                    for (int i = 0; i < 18; i++) // the thick rim (a torus of spheres) and the thin centre
                    {
                        float a = i * Mathf.PI * 2f / 18; var c = new Vector3(Mathf.Cos(a) * 0.1f, 0, Mathf.Sin(a) * 0.1f);
                        k.toon.Sphere(Matrix4x4.TRS(ef.P(0, 0.3f, 0) + rr * c, rr, Vector3.one * 0.075f), red, 5, 8);
                    }
                    k.toon.Cyl(Matrix4x4.TRS(ef.P(0, 0.3f, 0), rr, new Vector3(0.21f, 0.03f, 0.21f)), new Color(0.78f, 0.12f, 0.14f), 18);
                    break;
                }
            // a notebook and a mug
            if (Spot(-1.3f, 3.3f, new Vector3(0.24f, 0.03f, 0.3f), out f))
            {
                k.toon.Box(f.M(0, 0.008f, 0, new Vector3(0.21f, 0.016f, 0.28f), new Vector3(0, 12, 0)), Paper);
                k.toon.Box(f.M(0, 0.018f, 0, new Vector3(0.2f, 0.004f, 0.012f), new Vector3(0, 12, 0)), new Color(0.2f, 0.45f, 0.7f));
            }
            if (Spot(0.9f, 3.25f, new Vector3(0.09f, 0.12f, 0.09f), out f)) k.toon.Cyl(f.M(0, 0.05f, 0, new Vector3(0.075f, 0.1f, 0.075f)), new Color(0.95f, 0.5f, 0.2f), 12);
            log.Add($"bench items {placed}");
        }

        // ------------------------------------------------------------------ populate free horizontal surfaces
        /// <summary>Scans a lab-local floor rectangle with downward rays and puts small props on every free
        /// horizontal surface above 0.35 m that has headroom (the room's empty white racks, side tables).</summary>
        static int Populate(Kit k, Rect area, Random rnd)
        {
            int n = 0; var used = new List<Vector3>();
            for (float x = area.xMin + 0.08f; x < area.xMax; x += 0.16f)
                for (float z = area.yMin + 0.08f; z < area.yMax; z += 0.16f)
                {
                    float y = 2.5f;
                    for (int level = 0; level < 5 && y > 0.4f; level++)
                    {
                        if (!Physics.Raycast(k.W(new Vector3(x, y, z)), -k.lab.up, out var hit, y)) break;
                        float top = k.L(hit.point).y;
                        if (Vector3.Dot(hit.normal, k.lab.up) > 0.95f && top > 0.35f && top < 2.1f)
                        {
                            float head = k.Cast(new Vector3(x, top + 0.01f, z), Vector3.up, 0.6f);
                            var p = new Vector3(x, top, z);
                            bool near = false; foreach (var u in used) if ((u - p).sqrMagnitude < 0.2f * 0.2f) { near = true; break; }
                            if (head > 0.16f && !near && rnd.NextDouble() < 0.55 && k.Free(p + Vector3.up * 0.1f, new Vector3(0.14f, 0.16f, 0.14f), Quaternion.identity))
                            {
                                var f = new Frame(p - new Vector3(0.07f, 0, 0.07f), (float)rnd.NextDouble() * 360f);
                                double pick = rnd.NextDouble(); float maxH = Mathf.Min(head - 0.03f, 0.3f);
                                if (pick < 0.4) Jar(k, f, 0, 0, 0.14f, maxH, rnd);
                                else if (pick < 0.6) SmallPlant(k, f, 0, 0, 0.14f, maxH, rnd);
                                else if (pick < 0.8) Flask(k, f, 0, 0, 0.14f, maxH, rnd);
                                else BookRow(k, f, 0, 0.16f, 0, 0.14f, maxH, rnd);
                                used.Add(p); n++;
                            }
                        }
                        y = top - 0.06f; // look for the next shelf below
                    }
                }
            return n;
        }

        static void WallExtras(Kit k, Random rnd, List<string> log)
        {
            // find the right wall above the counters (x ≈ 2.3) at eye-plus height
            foreach (float z in new[] { -1.2f, -1.9f, -0.6f, -2.5f, -0.1f })
            {
                float d = k.Cast(new Vector3(0.6f, 1.95f, z), Vector3.right, 3f);
                if (d >= 3f) continue;
                var f = new Frame(new Vector3(0.6f + d - 0.012f, 1.95f, z), 270f);
                if (!k.Free(f.P(0, 0, 0.08f), new Vector3(1.0f, 0.62f, 0.05f), f.r)) continue;
                // cork board with pinned notes
                k.toon.Box(f.M(0, 0, 0.012f, new Vector3(1.04f, 0.66f, 0.024f)), WoodDark);
                k.toon.Box(f.M(0, 0, 0.026f, new Vector3(0.96f, 0.58f, 0.008f)), new Color(0.78f, 0.56f, 0.34f));
                Color[] notes = { new Color(1f, 0.93f, 0.45f), new Color(0.6f, 0.9f, 1f), new Color(1f, 0.65f, 0.75f), new Color(0.7f, 1f, 0.6f), Paper };
                for (int i = 0; i < 9; i++)
                {
                    float nx = -0.38f + (i % 3) * 0.29f + (float)(rnd.NextDouble() - 0.5) * 0.08f, ny = 0.17f - (i / 3) * 0.17f + (float)(rnd.NextDouble() - 0.5) * 0.04f;
                    float w = 0.1f + (float)rnd.NextDouble() * 0.06f;
                    k.toon.Box(f.M(nx, ny, 0.032f, new Vector3(w, w * 0.9f, 0.003f), new Vector3(0, 0, (float)(rnd.NextDouble() - 0.5) * 16f)), notes[rnd.Next(notes.Length)]);
                    k.glow.Sphere(f.M(nx, ny + w * 0.38f, 0.036f, Vector3.one * 0.012f), new Color(1f, 0.3f, 0.3f), 3, 5);
                }
                // a round clock above it
                if (k.Free(f.P(0.75f, 0.12f, 0.06f), new Vector3(0.3f, 0.3f, 0.05f), f.r))
                {
                    var cf = new Frame(f.P(0.75f, 0.12f, 0), 270f);
                    k.toon.Cyl(cf.M(0, 0, 0.02f, new Vector3(0.3f, 0.04f, 0.3f), new Vector3(90, 0, 0)), Brass, 20);
                    k.toon.Cyl(cf.M(0, 0, 0.041f, new Vector3(0.26f, 0.004f, 0.26f), new Vector3(90, 0, 0)), Paper, 20);
                    k.toon.Box(cf.M(0, 0.04f, 0.045f, new Vector3(0.012f, 0.09f, 0.004f), new Vector3(0, 0, 20)), Charcoal);
                    k.toon.Box(cf.M(0, 0.025f, 0.047f, new Vector3(0.012f, 0.06f, 0.004f), new Vector3(0, 0, 110)), Charcoal);
                }
                log.Add($"cork board at z {z:F1}");
                return;
            }
            log.Add("cork board: no free wall");
        }

        static void Vines(Kit k, Random rnd)
        {
            // trailing ivy from the tops of the left shelves
            foreach (float z in new[] { -1.85f, -1.2f, -0.85f, -0.3f })
            {
                float top = 2.4f - k.Cast(new Vector3(-2.75f, 2.4f, z), Vector3.down, 1f);
                if (top < 2.0f || top > 2.4f) continue;
                var p = new Vector3(-2.66f, top, z);
                int segs = 6 + rnd.Next(6);
                for (int i = 0; i < segs; i++)
                {
                    var q = p + new Vector3(0.02f * Mathf.Sin(i * 1.3f), -i * 0.07f, 0.03f * Mathf.Cos(i * 0.9f));
                    k.toon.Sphere(Matrix4x4.TRS(q, Quaternion.Euler((float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f, 0), new Vector3(0.06f, 0.035f, 0.012f)), Color.Lerp(Leaf, LeafLight, (float)rnd.NextDouble()), 3, 5);
                    k.toon.Sphere(Matrix4x4.TRS(q + new Vector3(0.025f, -0.02f, 0.02f), Quaternion.Euler((float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f, 0), new Vector3(0.055f, 0.03f, 0.012f)), Color.Lerp(LeafDark, Leaf, (float)rnd.NextDouble()), 3, 5);
                }
            }
        }

        // ------------------------------------------------------------------ screens and posters
        static void InfoScreen(Kit k, Transform lab, Transform holder, Font font, List<string> log)
        {
            // hangs from the ceiling in the left front, turned to the player's start; frame + glowing picture
            var pos = new Vector3(-2.2f, 2.05f, 1.35f);
            var toStart = new Vector3(0.3f, 0, -2.7f) - new Vector3(pos.x, 0, pos.z);
            float yaw = Mathf.Atan2(toStart.x, toStart.z) * Mathf.Rad2Deg; // the front faces the player
            var f = new Frame(pos, yaw);
            float w = 1.5f, h = 0.84f;
            k.toon.Box(f.M(0, 0, -0.03f, new Vector3(w + 0.08f, h + 0.08f, 0.05f)), Charcoal);
            k.glow.Box(f.M(0, -h / 2f - 0.045f, 0.0f, new Vector3(w + 0.06f, 0.012f, 0.012f)), new Color(0.3f, 0.9f, 1f));
            k.glow.Box(f.M(0, h / 2f + 0.045f, 0.0f, new Vector3(w + 0.06f, 0.012f, 0.012f)), new Color(0.3f, 0.9f, 1f));
            float ceil = pos.y + k.Cast(pos + Vector3.up * 0.5f, Vector3.up, 2f) + 0.5f;
            foreach (float x in new[] { -w * 0.35f, w * 0.35f })
                k.toon.Cyl(M(f.P(x, h / 2f + 0.04f + (ceil - pos.y - h / 2f) / 2f, -0.03f), new Vector3(0.014f, Mathf.Max(0.05f, ceil - pos.y - h / 2f - 0.04f), 0.014f)), Charcoal, 6, 0.5f, 0.5f, false, false);
            k.Tex("Info").Quad(f.M(0, 0, 0.001f, new Vector3(w, h, 1)), Color.white);
            Mats.Register("Info", Paint("Info", 1024, 576, InfoPixel));
            var canvas = Bvr2UiEditor.Canvas("Info Screen Text", holder, new Vector2(1500, 840), 0.001f);
            canvas.localPosition = f.P(0, 0, 0.004f); canvas.localRotation = Quaternion.Euler(0, yaw + 180f, 0);
            var t = Bvr2UiEditor.MakeText(canvas, "ИНФОРМАЦИЯ", 64, new Color(0.6f, 0.95f, 1f), font, null); t.alignment = TextAnchor.UpperLeft; t.fontStyle = FontStyle.Bold;
            t.rectTransform.offsetMin = new Vector2(46, 20); t.rectTransform.offsetMax = new Vector2(-40, -26);
            var t2 = Bvr2UiEditor.MakeText(canvas, "клетка · ДНК · кровь", 40, new Color(0.45f, 0.8f, 0.9f), font, null); t2.alignment = TextAnchor.UpperLeft;
            t2.rectTransform.offsetMin = new Vector2(50, 20); t2.rectTransform.offsetMax = new Vector2(-40, -106);
            log.Add("info screen");
        }

        static void Posters(Kit k, Transform lab, Transform holder, Font font, List<string> log)
        {
            int n = 0;
            // (lab-local probe point, direction toward the wall, poster key, title)
            var list = new[] {
                (new Vector3(1.0f, 1.55f, -2.3f), Vector3.back, "PosterBlood", "КЛЕТКИ КРОВИ"),
                (new Vector3(-1.3f, 1.55f, -2.9f), Vector3.back, "PosterArtery", "СТЕНКА АРТЕРИИ"),
                (new Vector3(-1.6f, 1.6f, 0.95f), Vector3.left, "PosterHeart", "КРУГИ КРОВООБРАЩЕНИЯ"),
            };
            foreach (var (probe, dir, key, title) in list)
            {
                float d = k.Cast(probe, dir, 3f);
                if (d >= 3f) continue;
                var at = probe + dir * (d - 0.012f);
                float yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg; // facing back into the room
                var f = new Frame(at, yaw);
                float w = 0.62f, h = 0.86f;
                if (!k.Free(f.P(0, 0, 0.09f), new Vector3(w, h, 0.05f), f.r)) continue; // something stands in front of that wall
                k.toon.Box(f.M(0, 0, 0.004f, new Vector3(w + 0.04f, h + 0.04f, 0.008f)), WoodDark);
                k.Tex(key).Quad(f.M(0, 0, 0.0095f, new Vector3(w, h, 1)), Color.white);
                Mats.Register(key, Paint(key, 512, 720, key == "PosterBlood" ? (Func<float, float, Color>)BloodPixel : key == "PosterArtery" ? ArteryPixel : HeartPixel));
                var canvas = Bvr2UiEditor.Canvas(key + " Title", holder, new Vector2(620, 120), 0.001f);
                canvas.localPosition = f.P(0, h / 2f - 0.065f, 0.012f); canvas.localRotation = Quaternion.Euler(0, yaw + 180f, 0);
                var t = Bvr2UiEditor.MakeText(canvas, title, 46, new Color(0.98f, 0.95f, 0.88f), font, null); t.fontStyle = FontStyle.Bold;
                n++;
            }
            log.Add($"posters {n}");
        }

        static void NeonFrames(Kit k, List<string> log)
        {
            // cyan frame around the door to the capsule bay (left wall, z 1.35..2.45) and a warm strip along the
            // left wall under the ceiling
            float j0 = -1.5f - k.Cast(new Vector3(-1.5f, 1.2f, 1.22f), Vector3.left, 3f) + 0.015f;
            float j1 = -1.5f - k.Cast(new Vector3(-1.5f, 1.2f, 2.58f), Vector3.left, 3f) + 0.015f;
            float open = -1.5f - k.Cast(new Vector3(-1.5f, 1.2f, 1.9f), Vector3.left, 6f);
            if (j0 > -3.6f && j1 > -3.6f && open < Mathf.Min(j0, j1) - 0.4f) // a real opening between two jambs
            {
                var cyan = new Color(0.3f, 0.9f, 1f);
                k.glow.Box(M(new Vector3(j0, 1.12f, 1.3f), new Vector3(0.02f, 2.2f, 0.025f)), cyan);
                k.glow.Box(M(new Vector3(j1, 1.12f, 2.5f), new Vector3(0.02f, 2.2f, 0.025f)), cyan);
                k.glow.Box(M(new Vector3((j0 + j1) / 2f, 2.24f, 1.9f), new Vector3(0.02f, 0.025f, 1.22f)), cyan);
                log.Add("bay door neon");
            }
            float topY = 1.6f + k.Cast(new Vector3(-2.2f, 1.6f, -1.0f), Vector3.up, 3f) - 0.08f;
            float lw = -1.5f - k.Cast(new Vector3(-1.5f, topY - 0.05f, -1.0f), Vector3.left, 3f) + 0.03f;
            if (topY > 2.3f && lw > -3.6f) k.glow.Box(M(new Vector3(lw, topY, -1.05f), new Vector3(0.02f, 0.02f, 2.2f)), new Color(1f, 0.6f, 0.25f));
        }

        static void Dna(Transform lab, Transform holder)
        {
            // a slowly turning DNA hologram above the bench's left end (cheap: 2×18 small spheres + rungs)
            var root = new GameObject("DNA Hologram").transform; root.SetParent(holder, false); root.localPosition = new Vector3(-0.95f, 1.55f, 2.95f);
            var spin = root.gameObject.AddComponent<Bvr2Spin>(); spin.speed = new Vector3(0, 22f, 0); spin.bob = 0f;
            var g = new Group("DNA");
            for (int i = 0; i < 18; i++)
            {
                float y = -0.3f + i * 0.036f, a = i * 0.55f;
                var p1 = new Vector3(Mathf.Cos(a) * 0.09f, y, Mathf.Sin(a) * 0.09f); var p2 = -new Vector3(p1.x, 0, p1.z) + Vector3.up * y;
                g.Sphere(M(p1, Vector3.one * 0.03f), new Color(0.3f, 0.9f, 1f), 3, 6);
                g.Sphere(M(p2, Vector3.one * 0.03f), new Color(1f, 0.35f, 0.8f), 3, 6);
                if (i % 2 == 0) g.Cyl(Matrix4x4.TRS((p1 + p2) / 2f, Quaternion.FromToRotation(Vector3.up, p2 - p1), new Vector3(0.008f, (p2 - p1).magnitude, 0.008f)), new Color(0.6f, 1f, 0.6f), 4, 0.5f, 0.5f, false, false);
            }
            var mesh = StoreMesh("LabDress_DNA", g);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = root.gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = Mats.Glow; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ lamps (Bvr2LabLighting)
        static void Lights(Transform lab, GameObject room, Transform holder, List<string> log)
        {
            var lighting = room.GetComponent<Bvr2LabLighting>(); if (!lighting) return;
            var lamps = new List<(Vector3 p, Color c, float range, float flicker)>();
            foreach (var p in PendantLamps) lamps.Add((p, new Color(0.75f, 0.5f, 0.25f), 3.4f, 0f));
            lamps.Add((new Vector3(-2.35f, 1.55f, -1.05f), new Color(0.22f, 0.36f, 0.5f), 2.2f, 0.12f));  // glowing jars on the left shelves
            lamps.Add((new Vector3(-2.0f, 1.9f, 1.15f), new Color(0.12f, 0.36f, 0.44f), 2.4f, 0.1f));    // info screen
            lamps.Add((new Vector3(-1.2f, 1.6f, 2.6f), new Color(0.4f, 0.3f, 0.5f), 2.4f, 0.08f));       // bench, DNA hologram
            lamps.Add((new Vector3(-4.9f, 1.7f, 2.0f), new Color(0.12f, 0.42f, 0.52f), 3.2f, 0.06f));    // capsule bay
            while (lamps.Count > Bvr2LabLighting.MaxLamps) lamps.RemoveAt(0);
            var parent = new GameObject("Lamps").transform; parent.SetParent(holder, false);
            var ts = new List<Transform>(); var cs = new List<Color>(); var rs = new List<float>(); var fs = new List<float>();
            int i = 0;
            foreach (var (p, c, r, fl) in lamps) { var t = new GameObject("Lamp" + i++).transform; t.SetParent(parent, false); t.localPosition = p; ts.Add(t); cs.Add(c); rs.Add(r); fs.Add(fl); }
            lighting.lamps = ts.ToArray(); lighting.lampColors = cs.ToArray(); lighting.lampRanges = rs.ToArray(); lighting.flicker = fs.ToArray();
            lighting.sunColor = new Color(1f, 0.86f, 0.68f); lighting.sunStrength = 0.92f;
            lighting.ambient = new Color(0.8f, 0.72f, 0.68f); lighting.ambientStrength = 0.46f;
            EditorUtility.SetDirty(lighting);
            log.Add($"lamps {ts.Count}");
        }

        // ------------------------------------------------------------------ materials and textures
        static class Mats
        {
            static readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
            public static void Register(string key, Texture2D t) { tex[key] = t; }
            static Material Get(string name, string shader)
            {
                string path = $"{Dir}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                var sh = Shader.Find(shader);
                if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); } else if (m.shader != sh) m.shader = sh;
                m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
            }
            public static Material Toon
            {
                get
                {
                    var m = Get("LabDress_Toon", "BVR2/LabToon");
                    m.SetColor("_BaseColor", Color.white); m.SetFloat("_VertexTint", 1); m.SetFloat("_Gloss", 0.18f);
                    m.SetTexture("_Ramp", RampTex("Dress_Toon", new Color(0.42f, 0.36f, 0.34f), new Color(0.85f, 0.8f, 0.76f), new Color(1f, 0.98f, 0.95f)));
                    m.SetFloat("_SrcBlend", 1); m.SetFloat("_DstBlend", 0); m.SetFloat("_ZWrite", 1); m.renderQueue = -1; return m;
                }
            }
            public static Material Glow
            {
                get { var m = Get("LabDress_Glow", "BVR2/LabEmissive"); m.SetFloat("_Intensity", 1.25f); m.SetFloat("_VertexTint", 1); m.SetFloat("_Rim", 0.5f); m.SetTexture("_MainTex", Texture2D.whiteTexture); return m; }
            }
            public static Material Glass
            {
                get
                {
                    var m = Get("LabDress_Glass", "BVR2/LabToon");
                    m.SetColor("_BaseColor", new Color(0.82f, 0.96f, 1f, 0.24f)); m.SetFloat("_VertexTint", 1); m.SetFloat("_Gloss", 0.8f);
                    m.SetTexture("_Ramp", RampTex("Dress_Glass", new Color(0.55f, 0.7f, 0.78f), new Color(0.85f, 0.95f, 1f), Color.white));
                    m.SetFloat("_SrcBlend", 5); m.SetFloat("_DstBlend", 10); m.SetFloat("_ZWrite", 0); m.renderQueue = 3000; return m;
                }
            }
            public static Material Textured(string key)
            {
                var m = Get("LabDress_" + key, "BVR2/LabEmissive"); m.SetFloat("_VertexTint", 0); m.SetFloat("_Rim", 0f);
                m.SetFloat("_Intensity", key == "Info" ? 1.15f : 0.95f);
                m.SetVector("_Scroll", key == "Info" ? new Vector4(90, 1.2f, 0.06f, 0) : Vector4.zero);
                if (tex.TryGetValue(key, out var t)) m.SetTexture("_MainTex", t);
                return m;
            }
        }

        static Texture2D RampTex(string name, Color shade, Color mid, Color lit)
        {
            const int W = 64; var t = new Texture2D(W, 1, TextureFormat.RGBA32, false, false);
            for (int x = 0; x < W; x++)
            {
                float u = x / (W - 1f);
                // soft toon: shade until 0.35, blend to mid by 0.5, mid to lit from 0.62 to 0.9
                Color c = u < 0.35f ? shade : u < 0.5f ? Color.Lerp(shade, mid, (u - 0.35f) / 0.15f) : u < 0.62f ? mid : Color.Lerp(mid, lit, Mathf.Clamp01((u - 0.62f) / 0.28f));
                t.SetPixel(x, 0, c);
            }
            string path = $"{Dir}/Ramp_{name}.png";
            File.WriteAllBytes(path, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp.wrapMode != TextureWrapMode.Clamp || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.Uncompressed)
            {
                imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = false; imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed; imp.npotScale = TextureImporterNPOTScale.None; imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D Paint(string name, int w, int h, Func<float, float, Color> pixel)
        {
            var t = new Texture2D(w, h, TextureFormat.RGB24, false, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = pixel((x + 0.5f) / w, (y + 0.5f) / h);
            t.SetPixels(px); t.Apply();
            string path = $"{Dir}/Tex_{name}.png";
            File.WriteAllBytes(path, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp.wrapMode != TextureWrapMode.Clamp || !imp.mipmapEnabled || imp.maxTextureSize != 1024)
            { imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.maxTextureSize = 1024; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ pictures (u, v in 0..1, v up)
        static float Smooth(float d, float aa = 0.002f) => Mathf.Clamp01(0.5f - d / aa);
        static float Ring(float d, float width) => Smooth(Mathf.Abs(d) - width);
        static Color Over(Color under, Color over, float a) => Color.Lerp(under, over, Mathf.Clamp01(a));
        static float Seg(Vector2 p, Vector2 a, Vector2 b) { var pa = p - a; var ba = b - a; float t = Mathf.Clamp01(Vector2.Dot(pa, ba) / ba.sqrMagnitude); return (pa - ba * t).magnitude; }

        static Color InfoPixel(float u, float v)
        {
            var p = new Vector2(u * 16f / 9f, v); // square units
            Color c = Color.Lerp(new Color(0.02f, 0.06f, 0.11f), new Color(0.03f, 0.13f, 0.2f), v);
            float grid = Mathf.Max(Ring(Mathf.Repeat(p.x, 0.0625f) - 0.03125f, 0.0012f), Ring(Mathf.Repeat(p.y, 0.0625f) - 0.03125f, 0.0012f));
            c = Over(c, new Color(0.1f, 0.35f, 0.45f), grid * 0.35f);
            // header bar
            if (v > 0.8f) c = Over(c, new Color(0.05f, 0.2f, 0.28f), 0.7f);
            c = Over(c, new Color(0.3f, 0.9f, 1f), Ring(v - 0.8f, 0.0025f));
            // a cell: membrane, cytoplasm, nucleus, organelles
            var cc = new Vector2(0.48f, 0.4f); var q = p - cc; q.x /= 1.25f; float r = q.magnitude;
            c = Over(c, new Color(0.08f, 0.3f, 0.36f), Smooth(r - 0.27f) * 0.6f);
            c = Over(c, new Color(0.4f, 0.95f, 1f), Ring(r - 0.27f, 0.008f));
            float nr = (p - new Vector2(0.52f, 0.43f)).magnitude;
            c = Over(c, new Color(0.62f, 0.32f, 0.9f), Smooth(nr - 0.085f));
            c = Over(c, new Color(0.85f, 0.6f, 1f), Ring(nr - 0.085f, 0.006f));
            c = Over(c, new Color(0.95f, 0.75f, 1f), Smooth((p - new Vector2(0.54f, 0.45f)).magnitude - 0.025f));
            foreach (var o in new[] { new Vector2(0.33f, 0.36f), new Vector2(0.4f, 0.24f), new Vector2(0.62f, 0.3f), new Vector2(0.66f, 0.47f), new Vector2(0.36f, 0.5f) })
                c = Over(c, new Color(0.4f, 1f, 0.5f), Smooth(new Vector2((p - o).x / 1.8f, (p - o).y).magnitude - 0.018f));
            // DNA helix on the right
            float hx = p.x - 1.38f, phase = v * 22f;
            float s1 = Mathf.Sin(phase) * 0.11f, s2 = -s1;
            if (v > 0.08f && v < 0.74f)
            {
                c = Over(c, new Color(0.3f, 0.9f, 1f), Ring(hx - s1, 0.007f));
                c = Over(c, new Color(1f, 0.35f, 0.8f), Ring(hx - s2, 0.007f));
                if (Mathf.Repeat(phase, Mathf.PI / 3f) < 0.18f && Mathf.Abs(hx) < Mathf.Abs(s1)) c = Over(c, new Color(0.6f, 1f, 0.6f), 0.9f);
            }
            // pulse line at the bottom
            float px = u * 9f; float ecg = Mathf.Repeat(px, 1f); float yv = 0.08f + (ecg > 0.45f && ecg < 0.5f ? (ecg - 0.45f) * 2.4f : ecg >= 0.5f && ecg < 0.55f ? (0.55f - ecg) * 2.4f - 0.02f : 0f);
            if (u > 0.72f) c = Over(c, new Color(0.35f, 1f, 0.5f), Ring(v - yv, 0.004f));
            // "text" lines on the left
            for (int i = 0; i < 4; i++) { float ly = 0.66f - i * 0.05f; if (u > 0.03f && u < 0.03f + 0.1f + (i % 2) * 0.05f) c = Over(c, new Color(0.45f, 0.75f, 0.85f), Ring(v - ly, 0.006f) * 0.8f); }
            // frame glow
            float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
            c = Over(c, new Color(0.3f, 0.9f, 1f), Smooth(edge - 0.006f, 0.004f));
            return c;
        }

        static Color PaperBg(float u, float v)
        {
            Color c = Color.Lerp(new Color(0.93f, 0.88f, 0.76f), new Color(0.98f, 0.95f, 0.86f), v);
            if (v > 0.86f) c = new Color(0.12f, 0.33f, 0.38f);
            float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
            return Over(c, new Color(0.55f, 0.42f, 0.3f), Smooth(edge - 0.012f, 0.006f) * 0.7f);
        }

        static Color BloodPixel(float u, float v)
        {
            var c = PaperBg(u, v); var p = new Vector2(u, v * 720f / 512f);
            // erythrocyte (biconcave: darker centre)
            var e = p - new Vector2(0.32f, 0.86f); float r = e.magnitude;
            c = Over(c, Color.Lerp(new Color(0.9f, 0.18f, 0.16f), new Color(0.65f, 0.08f, 0.1f), Mathf.Clamp01(1f - r / 0.2f) * 0.8f), Smooth(r - 0.2f));
            c = Over(c, new Color(0.5f, 0.05f, 0.06f), Ring(r - 0.2f, 0.006f));
            // second erythrocyte, side view (dumbbell)
            var e2 = p - new Vector2(0.72f, 0.86f); float side = new Vector2(e2.x / 1.0f, e2.y / 0.35f).magnitude - 0.17f - Mathf.Abs(e2.x) * -0.0f;
            float pinch = Mathf.Abs(e2.y) - (0.045f + 0.03f * Mathf.Clamp01(Mathf.Abs(e2.x) / 0.17f));
            c = Over(c, new Color(0.88f, 0.2f, 0.18f), Smooth(Mathf.Max(Mathf.Abs(e2.x) - 0.17f, pinch)));
            // leukocyte with a lobed nucleus
            var l = p - new Vector2(0.34f, 0.42f); float lr = l.magnitude;
            c = Over(c, new Color(0.92f, 0.9f, 0.96f), Smooth(lr - 0.18f));
            c = Over(c, new Color(0.6f, 0.55f, 0.75f), Ring(lr - 0.18f, 0.005f));
            foreach (var o in new[] { new Vector2(-0.06f, 0.03f), new Vector2(0.03f, 0.06f), new Vector2(0.06f, -0.04f) })
                c = Over(c, new Color(0.45f, 0.25f, 0.7f), Smooth((l - o).magnitude - 0.055f));
            // platelets
            foreach (var o in new[] { new Vector2(0.7f, 0.5f), new Vector2(0.8f, 0.4f), new Vector2(0.68f, 0.33f), new Vector2(0.82f, 0.56f) })
            { var d = p - o; c = Over(c, new Color(0.75f, 0.5f, 0.85f), Smooth(new Vector2(d.x / 1.6f, d.y).magnitude - 0.025f)); }
            // caption lines
            for (int i = 0; i < 3; i++) { float y = 0.17f - i * 0.045f; if (u > 0.1f && u < 0.9f - i * 0.12f) c = Over(c, new Color(0.35f, 0.3f, 0.28f), Ring(v - y, 0.005f) * 0.7f); }
            return c;
        }

        static Color ArteryPixel(float u, float v)
        {
            var c = PaperBg(u, v); var p = new Vector2(u, v * 720f / 512f) - new Vector2(0.5f, 0.66f); float r = p.magnitude;
            c = Over(c, new Color(0.95f, 0.8f, 0.45f), Smooth(r - 0.36f));   // adventitia
            c = Over(c, new Color(0.85f, 0.3f, 0.28f), Smooth(r - 0.29f));   // media (muscle)
            for (int i = 0; i < 4; i++) c = Over(c, new Color(0.7f, 0.18f, 0.2f), Ring(r - (0.2f + i * 0.022f), 0.002f) * 0.6f);
            c = Over(c, new Color(1f, 0.72f, 0.72f), Smooth(r - 0.19f));     // intima
            c = Over(c, new Color(0.55f, 0.06f, 0.08f), Smooth(r - 0.165f)); // lumen
            foreach (var o in new[] { new Vector2(-0.06f, 0.04f), new Vector2(0.05f, -0.05f), new Vector2(0.06f, 0.07f), new Vector2(-0.07f, -0.07f) })
                c = Over(c, new Color(0.95f, 0.25f, 0.22f), Smooth((p - o).magnitude - 0.04f));
            for (int i = 0; i < 3; i++) { float y = 0.17f - i * 0.045f; if (u > 0.1f && u < 0.9f - i * 0.1f) c = Over(c, new Color(0.35f, 0.3f, 0.28f), Ring(v - y, 0.005f) * 0.7f); }
            return c;
        }

        static Color HeartPixel(float u, float v)
        {
            var c = PaperBg(u, v); var p = new Vector2(u, v * 720f / 512f);
            // a stylised body loop: red arteries out of the heart, blue veins back
            var heart = new Vector2(0.5f, 0.68f);
            float hd = (p - heart).magnitude;
            var red = new Color(0.86f, 0.16f, 0.16f); var blue = new Color(0.2f, 0.36f, 0.82f);
            c = Over(c, red, Ring(Seg(p, heart, new Vector2(0.5f, 1.05f)), 0.012f));
            c = Over(c, red, Ring(Seg(p, heart, new Vector2(0.2f, 0.98f)), 0.01f));
            c = Over(c, red, Ring(Seg(p, heart, new Vector2(0.8f, 0.98f)), 0.01f));
            c = Over(c, red, Ring(Seg(p, heart, new Vector2(0.35f, 0.3f)), 0.012f));
            c = Over(c, red, Ring(Seg(p, heart, new Vector2(0.65f, 0.3f)), 0.012f));
            c = Over(c, blue, Ring(Seg(p, heart + new Vector2(0.03f, 0), new Vector2(0.58f, 0.28f)), 0.01f));
            c = Over(c, blue, Ring(Seg(p, heart + new Vector2(-0.03f, 0), new Vector2(0.42f, 0.28f)), 0.01f));
            c = Over(c, blue, Ring(Seg(p, heart + new Vector2(0, 0.03f), new Vector2(0.62f, 1.0f)), 0.009f));
            // capillary beds
            foreach (var o in new[] { new Vector2(0.38f, 0.3f), new Vector2(0.62f, 0.3f), new Vector2(0.5f, 1.05f) })
                c = Over(c, new Color(0.6f, 0.25f, 0.55f), Smooth((p - o).magnitude - 0.045f) * 0.8f);
            // the heart
            var hq = p - heart; float lobes = Mathf.Min((hq - new Vector2(-0.04f, 0.02f)).magnitude, (hq - new Vector2(0.04f, 0.02f)).magnitude) - 0.06f;
            float tip = Mathf.Max(Mathf.Abs(hq.x) * 1.1f + hq.y * 0.9f - 0.04f, -hq.y - 0.11f);
            c = Over(c, new Color(0.78f, 0.1f, 0.14f), Smooth(Mathf.Min(lobes, Mathf.Max(tip, hq.y - 0.02f))));
            for (int i = 0; i < 3; i++) { float y = 0.12f - i * 0.04f; if (u > 0.1f && u < 0.9f - i * 0.1f) c = Over(c, new Color(0.35f, 0.3f, 0.28f), Ring(v - y, 0.005f) * 0.7f); }
            return c;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// Prologue showpieces (03.11): the patient hologram (a human figure with the circulatory system, a beating
    /// heart, blood pulses running along the arteries, a projector with a light cone, turning rings and rising
    /// motes, labels), the hand tools (bio-scanner and BioTool models from Art/Holo/Tools.fbx with turning lens
    /// rings and coils) and the lab's small effects (dust in the air, light shafts under the lamps, bubbling
    /// sample jars, the ECG strip on the vitals panel). Assets: Art/Holo/Patient.fbx, Art/Holo/Tools.fbx
    /// (made by Tools/holo/patient.py and tools.py in Blender).
    /// </summary>
    public static class Bvr2HoloFx
    {
        const string Art = Bvr2SceneBuilder.Root + "/Art/Holo";
        const string Dir = Bvr2SceneBuilder.Gen + "/Holo";

        // ------------------------------------------------------------------ assets
        static Material Mat(string name, string shader)
        {
            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{name}.mat";
            var sh = Shader.Find(shader);
            if (!sh) { Debug.LogError("[BVR2] Shader not found: " + shader); sh = Shader.Find("Universal Render Pipeline/Unlit"); }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != sh) m.shader = sh;
            EditorUtility.SetDirty(m); return m;
        }

        /// <summary>Writes a mesh asset, updating an existing one in place (no stale GPU buffers).</summary>
        static Mesh Store(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv0, List<Vector2> uv1, List<Color> c, List<int> t, Bounds? bounds = null)
        {
            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); bool fresh = !mesh;
            if (fresh) mesh = new Mesh { name = name };
            mesh.Clear(); mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(v); if (n != null) mesh.SetNormals(n); if (uv0 != null) mesh.SetUVs(0, uv0); if (uv1 != null) mesh.SetUVs(1, uv1); if (c != null) mesh.SetColors(c);
            mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            if (bounds.HasValue) mesh.bounds = bounds.Value;
            if (fresh) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static GameObject Obj(string name, Mesh mesh, Material mat, Transform parent, Vector3 pos, Vector3 scale, Quaternion? rot = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale; go.transform.localRotation = rot ?? Quaternion.identity;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        static void ConfigureModel(string path)
        {
            if (AssetImporter.GetAtPath(path) is ModelImporter mi)
            {
                bool dirty = mi.materialImportMode != ModelImporterMaterialImportMode.None || mi.animationType != ModelImporterAnimationType.None || mi.importNormals != ModelImporterNormals.Import;
                mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.animationType = ModelImporterAnimationType.None; mi.importAnimation = false;
                mi.importBlendShapes = false; mi.isReadable = false; mi.meshCompression = ModelImporterMeshCompression.Off; mi.importNormals = ModelImporterNormals.Import;
                if (dirty) mi.SaveAndReimport();
            }
        }

        static GameObject Load(string path)
        {
            ConfigureModel(path);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) { Debug.LogWarning("[BVR2] missing " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset); PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            return go;
        }

        // ------------------------------------------------------------------ procedural meshes
        /// <summary>Motes for BVR2/Motes: count quads; base points in a box (rise: on the box floor).</summary>
        public static Mesh Motes(string name, int count, Vector3 centre, Vector3 size, bool rise, Color[] palette, int seed, float maxTravel = 0f, bool disc = false)
        {
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var uv1 = new List<Vector2>(); var c = new List<Color>(); var t = new List<int>();
            Vector2[] corners = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
            for (int i = 0; i < count; i++)
            {
                Vector3 p;
                if (disc) { float a = R() * Mathf.PI * 2f, r = Mathf.Sqrt(R()); p = centre + new Vector3(Mathf.Cos(a) * r * size.x * 0.5f, 0, Mathf.Sin(a) * r * size.z * 0.5f); }
                else p = centre + new Vector3((R() - 0.5f) * size.x, 0, (R() - 0.5f) * size.z);
                p.y = rise ? centre.y - size.y * 0.5f : centre.y + (R() - 0.5f) * size.y;
                var col = palette[rnd.Next(palette.Length)];
                var ph = new Vector2(R(), 0.6f + 0.8f * R());
                int b = v.Count;
                foreach (var k in corners) { v.Add(p); uv.Add(k); uv1.Add(ph); c.Add(col); }
                t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3);
            }
            var bounds = new Bounds(centre + (rise ? Vector3.up * maxTravel * 0.5f : Vector3.zero), size + Vector3.one * 0.5f + Vector3.up * maxTravel);
            return Store(name, v, null, uv, uv1, c, t, bounds);
        }

        /// <summary>Fixed blinking dots (LEDs) at given points; phase = order along the list (a running chase).</summary>
        public static Mesh Leds(string name, IList<Vector3> pts, Color[] palette, float chase)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var uv1 = new List<Vector2>(); var c = new List<Color>(); var t = new List<int>();
            Vector2[] corners = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
            for (int i = 0; i < pts.Count; i++)
            {
                int b = v.Count; var ph = new Vector2(chase > 0 ? i * chase % 1f : (i * 0.618f) % 1f, 1f);
                foreach (var k in corners) { v.Add(pts[i]); uv.Add(k); uv1.Add(ph); c.Add(palette[i % palette.Length]); }
                t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3);
            }
            var bb = new Bounds(pts.Count > 0 ? pts[0] : Vector3.zero, Vector3.one * 0.1f); foreach (var p in pts) bb.Encapsulate(p); bb.Expand(0.2f);
            return Store(name, v, null, uv, uv1, c, t, bb);
        }

        /// <summary>Open frustum along +y from radius r0 (y = 0, UV.y = 0) to r1 (y = h, UV.y = 1).</summary>
        public static Mesh Beam(string name, float r0, float r1, float h, int seg = 32)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int k = 0; k <= 4; k++)
                {
                    float y = k / 4f; v.Add(d * Mathf.Lerp(r0, r1, y) + Vector3.up * h * y); n.Add((d + Vector3.up * (r0 - r1) / h).normalized); uv.Add(new Vector2(i / (float)seg, y));
                }
            }
            for (int i = 0; i < seg; i++)
                for (int k = 0; k < 4; k++) { int a = i * 5 + k, b = a + 5; t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1); }
            return Store(name, v, n, uv, null, null, t);
        }

        /// <summary>Flat ring segments in the XZ plane (projector rings): count arcs with gaps.</summary>
        public static Mesh Arcs(string name, float inner, float outer, int count, float gapDeg, float offsetDeg = 0f, int seg = 64)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float span = 360f / count - gapDeg;
            for (int s = 0; s < count; s++)
            {
                float a0 = offsetDeg + s * 360f / count; int steps = Mathf.Max(2, Mathf.RoundToInt(seg * span / 360f));
                int b = v.Count;
                for (int i = 0; i <= steps; i++)
                {
                    float a = (a0 + span * i / steps) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    v.Add(d * inner); v.Add(d * outer); n.Add(Vector3.up); n.Add(Vector3.up); uv.Add(new Vector2(i / (float)steps, 0)); uv.Add(new Vector2(i / (float)steps, 1));
                }
                for (int i = 0; i < steps; i++) { int a = b + i * 2; t.Add(a); t.Add(a + 1); t.Add(a + 3); t.Add(a); t.Add(a + 3); t.Add(a + 2); }
            }
            return Store(name, v, n, uv, null, null, t);
        }

        static Mesh QuadMesh()
        {
            var v = new List<Vector3> { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            var n = new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            return Store("Holo_Quad", v, n, uv, null, null, new List<int> { 0, 2, 1, 0, 3, 2 });
        }

        // ------------------------------------------------------------------ the patient hologram
        public static string Patient(Bvr2Prelude ch, Transform map, Font font)
        {
            var log = new List<string>();
            var fx = map.gameObject.AddComponent<Bvr2HoloPatient>();
            var model = Figure(map, 1.78f, -1.2f, out var heart, out var entry, out var b);
            if (model)
            {
                fx.heart = heart;
                // the entry point: the old torus marker moves onto the forearm artery
                Vector3 entryPos = entry ? map.InverseTransformPoint(entry.GetComponent<Renderer>().bounds.center) : new Vector3(0.3f, -0.2f, 0.05f);
                if (entry) Object.DestroyImmediate(entry.gameObject);
                var old = map.Find("Entry Marker");
                if (old) { old.localPosition = entryPos; fx.entryMarker = old; }
                log.Add($"figure {b.size.y:F2} m, heart {(heart ? "ok" : "missing")}, entry {entryPos:F2}");
                // labels beside the heart, the entry point and the circulation, with thin leader lines (Bvr2HoloPatient
                // keeps them beside their point as the figure turns)
                var leaderMat = Mat("Holo_Leader", "BVR2/Trail"); leaderMat.SetColor("_Color", new Color(0.75f, 0.97f, 1f, 1f)); leaderMat.SetFloat("_Intensity", 1.8f);
                var dotMat = Mat("Holo_LabelDot", "BVR2/Motes"); dotMat.SetColor("_Color", new Color(1f, 1f, 1f, 1f)); dotMat.SetFloat("_Size", 0.009f); dotMat.SetFloat("_Mode", 2); dotMat.SetVector("_Motion", new Vector4(0, 0.9f, 0, 0)); dotMat.SetFloat("_Intensity", 1.6f);
                var labels = new List<Transform>(); var anchors = new List<Vector3>(); var offsets = new List<Vector2>(); var leaders = new List<LineRenderer>();
                void Add(string text, Vector3 anchor, Vector2 offset, Color col)
                {
                    var l = Label(map, text, anchor, font, col); labels.Add(l); anchors.Add(anchor); offsets.Add(offset);
                    var lg = new GameObject("Leader " + text); lg.transform.SetParent(map, false);
                    var lr = lg.AddComponent<LineRenderer>(); lr.sharedMaterial = leaderMat; lr.positionCount = 2; lr.useWorldSpace = true; lr.widthMultiplier = 0.009f;
                    lr.startColor = new Color(col.r, col.g, col.b, 0.75f); lr.endColor = new Color(col.r, col.g, col.b, 1f);
                    // a blinking dot on the named point (rides with the figure as it turns)
                    Obj("Anchor " + text, Leds("Holo_LabelDot_" + labels.Count, new[] { Vector3.zero }, new[] { col }, 0f), dotMat, map, anchor, Vector3.one);
                    lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; lr.numCapVertices = 2;
                    lr.SetPosition(0, map.TransformPoint(anchor)); lr.SetPosition(1, map.TransformPoint(anchor) + Vector3.right * 0.3f);
                    leaders.Add(lr);
                }
                if (heart) Add("СЕРДЦЕ", map.InverseTransformPoint(heart.position), new Vector2(0.30f, 0.13f), new Color(1f, 0.6f, 0.6f));
                Add("ТОЧКА ВХОДА", entryPos, new Vector2(-0.36f, -0.10f), Bvr2Style.Knowledge);
                // the circulation label points at the aorta arch above the heart
                Vector3 arch = heart ? map.InverseTransformPoint(heart.position) + new Vector3(0f, 0.09f, 0f) : new Vector3(0f, 0.2f, 0f);
                Add("АРТЕРИИ · ВЕНЫ", arch + new Vector3(0f, 0.0f, 0f), new Vector2(-0.32f, 0.22f), new Color(0.7f, 0.9f, 1f));
                fx.labels = labels.ToArray(); fx.labelAnchors = anchors.ToArray(); fx.labelOffsets = offsets.ToArray(); fx.leaders = leaders.ToArray();
                fx.labelHalfSize = new Vector2(420f * 0.0011f * 0.5f, 70f * 0.0011f * 0.5f);
            }
            // projector: the pad rings, a light cone, rising motes and a ring of running LEDs
            fx.rings = Projector(map, new Vector3(0, -1.22f, 0), 1f); fx.ringSpeeds = new[] { 18f, -32f, 6f };
            // the old primitive figure goes
            foreach (var n in new[] { "Body", "Head", "Arm L", "Arm R", "Leg L", "Leg R", "Beam", "Heart", "Aorta", "Artery Arm", "Base" })
            { var t = map.Find(n); if (t) Object.DestroyImmediate(t.gameObject); }
            return "patient hologram: " + string.Join(", ", log);
        }

        /// <summary>The hologram figure (Art/Holo/Patient.fbx) with its materials: upright, <paramref name="height"/> tall,
        /// feet at local y = <paramref name="feetY"/> of the parent, centred over the parent's origin.</summary>
        static GameObject Figure(Transform parent, float height, float feetY, out Transform heart, out Transform entry, out Bounds bounds)
        {
            heart = null; entry = null; bounds = default;
            var model = Load(Art + "/Patient.fbx");
            if (!model) return null;
            model.name = "Figure"; model.transform.SetParent(parent, false);
            var b = Bounds(model);
            if (b.size.z > b.size.y * 1.3f) { model.transform.localRotation = Quaternion.Euler(-90, 0, 0) * model.transform.localRotation; b = Bounds(model); }
            if (b.size.y > 0.01f) model.transform.localScale *= height * parent.lossyScale.y / b.size.y;
            b = Bounds(model);
            var c = parent.InverseTransformPoint(b.center); var lo = parent.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
            model.transform.localPosition += new Vector3(-c.x, feetY - lo.y, -c.z);
            bounds = Bounds(model);
            var body = Mat("Holo_Body", "BVR2/HoloBody"); body.SetColor("_Color", new Color(0.32f, 0.82f, 1f, 1f)); body.SetFloat("_Intensity", 1.25f); body.SetFloat("_Fill", 0.10f);
            var lungs = Mat("Holo_Lungs", "BVR2/HoloBody"); lungs.SetColor("_Color", new Color(0.65f, 0.85f, 1f, 0.55f)); lungs.SetFloat("_Intensity", 0.7f); lungs.SetFloat("_Fill", 0.05f); lungs.SetFloat("_LineAmount", 0.1f); lungs.SetVector("_Band", new Vector4(0.18f, 0.06f, 0.5f, 0));
            var art = Mat("Holo_Arteries", "BVR2/HoloFlow"); art.SetColor("_Color", new Color(1f, 0.22f, 0.28f, 1f)); art.SetColor("_PulseColor", new Color(1f, 0.8f, 0.7f)); art.SetVector("_Pulse", new Vector4(0.55f, 0.25f, 1.8f, 1f));
            var vein = Mat("Holo_Veins", "BVR2/HoloFlow"); vein.SetColor("_Color", new Color(0.3f, 0.45f, 1f, 0.8f)); vein.SetColor("_PulseColor", new Color(0.6f, 0.75f, 1f)); vein.SetVector("_Pulse", new Vector4(0.45f, 0.35f, 0.9f, -1f)); vein.SetFloat("_Intensity", 0.9f);
            var heartMat = Mat("Holo_Heart", "BVR2/Glow"); heartMat.SetColor("_Color", new Color(1f, 0.28f, 0.33f)); heartMat.SetFloat("_Intensity", 1.9f); heartMat.SetFloat("_Core", 0.55f); heartMat.SetFloat("_Fresnel", 1.6f);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (n.StartsWith("PatientBody")) { r.sharedMaterial = body; SetHeight(r, body); }
                else if (n.StartsWith("PatientLungs")) { r.sharedMaterial = lungs; SetHeight(r, lungs); }
                else if (n.StartsWith("PatientArteries")) r.sharedMaterial = art;
                else if (n.StartsWith("PatientVeins")) r.sharedMaterial = vein;
                else if (n.StartsWith("PatientHeart")) { r.sharedMaterial = heartMat; heart = r.transform; }
                else if (n.StartsWith("PatientEntryMark")) entry = r.transform;
            }
            return model;
        }

        /// <summary>A hologram projector pad at <paramref name="pos"/> (scale k): turning ring segments, a light cone,
        /// rising motes and running LEDs. Returns the three rings (for a spinner).</summary>
        static Transform[] Projector(Transform parent, Vector3 pos, float k)
        {
            var glowA = Mat("Holo_Ring", "BVR2/Glow"); glowA.SetColor("_Color", new Color(0.35f, 0.9f, 1f)); glowA.SetFloat("_Intensity", 1.6f); glowA.SetFloat("_Core", 0.85f);
            var glowB = Mat("Holo_RingDim", "BVR2/Glow"); glowB.SetColor("_Color", new Color(0.35f, 0.75f, 1f)); glowB.SetFloat("_Intensity", 0.8f); glowB.SetFloat("_Core", 0.7f);
            var pad = new GameObject("Projector").transform; pad.SetParent(parent, false); pad.localPosition = pos; pad.localScale = Vector3.one * k;
            var r1 = Obj("Ring Outer", Arcs("Holo_Arcs3", 0.43f, 0.47f, 3, 18f), glowA, pad, Vector3.zero, Vector3.one);
            var r2 = Obj("Ring Inner", Arcs("Holo_Arcs5", 0.33f, 0.35f, 5, 26f, 10f), glowB, pad, new Vector3(0, 0.005f, 0), Vector3.one);
            var r3 = Obj("Ring Ticks", Arcs("Holo_Ticks", 0.38f, 0.40f, 36, 6f), glowB, pad, new Vector3(0, 0.003f, 0), Vector3.one);
            var beam = Mat("Holo_Beam", "BVR2/HoloBeam"); beam.SetColor("_Color", new Color(0.35f, 0.85f, 1f, 0.22f)); beam.SetFloat("_Falloff", 1.4f);
            Obj("Light Cone", Beam("Holo_Cone", 0.42f, 0.30f, 2.0f), beam, pad, Vector3.zero, Vector3.one);
            var motes = Mat("Holo_Motes", "BVR2/Motes"); motes.SetColor("_Color", new Color(0.55f, 0.95f, 1f, 0.9f)); motes.SetFloat("_Size", 0.009f); motes.SetFloat("_Mode", 0); motes.SetVector("_Motion", new Vector4(2.0f, 0.22f, 0.03f, 0));
            Obj("Motes", Motes("Holo_PatientMotes", 120, Vector3.zero, new Vector3(0.8f, 0.0f, 0.8f), true, new[] { Color.white, new Color(0.7f, 0.95f, 1f), new Color(1f, 0.6f, 0.6f) }, 5, 2.0f, true), motes, pad, Vector3.zero, Vector3.one);
            var ledPts = new List<Vector3>(); for (int i = 0; i < 24; i++) { float a = i * Mathf.PI * 2f / 24; ledPts.Add(new Vector3(Mathf.Cos(a) * 0.52f, 0.01f, Mathf.Sin(a) * 0.52f)); }
            var leds = Mat("Holo_Leds", "BVR2/Motes"); leds.SetColor("_Color", new Color(0.4f, 0.95f, 1f, 1f)); leds.SetFloat("_Size", 0.011f); leds.SetFloat("_Mode", 2); leds.SetVector("_Motion", new Vector4(0, 0.6f, 0, 0));
            Obj("LED Chase", Leds("Holo_PadLeds", ledPts, new[] { Color.white }, 1f / 24f), leds, pad, Vector3.zero, Vector3.one);
            return new[] { r1.transform, r2.transform, r3.transform };
        }

        // ------------------------------------------------------------------ the main menu stage
        /// <summary>
        /// The title / main menu stage, 1500 m above the artery (04.11): a sky dome, a glowing floor disc, the
        /// hologram human on a projector on the left, the scanner and the BioTool turning over a projector on the
        /// right, two halo rings behind the menu panel and drifting motes. Inactive until the menu opens it.
        /// </summary>
        public static string MenuStage(Bvr2Menu menu, Material device, Material dark, Material glow)
        {
            var root = new GameObject("Menu Stage").transform; root.SetParent(menu.transform, false);
            root.position = new Vector3(0, 1500, 0); root.rotation = Quaternion.identity;
            menu.stage = root;
            var stand = new GameObject("Stand (eyes)").transform; stand.SetParent(root, false); stand.localPosition = new Vector3(0, 1.62f, 0); menu.stand = stand;
            var spot = new GameObject("Panel Spot").transform; spot.SetParent(root, false); spot.localPosition = new Vector3(0, 1.5f, 1.75f); menu.panelSpot = spot;
            var sky = Mat("Menu_Sky", "BVR2/MenuSky"); sky.SetColor("_Grid", new Color(0.3f, 0.85f, 1f, 0.07f));
            Obj("Sky", Dome("Menu_Dome", 30f, 48, 24), sky, root, new Vector3(0, 1.6f, 0), Vector3.one);
            Obj("Floor", Disc("Menu_Disc", 9f, 96), Mat("Menu_Floor", "BVR2/MenuFloor"), root, new Vector3(0, 0.001f, 0), Vector3.one);
            // the hologram human, turning slowly on its projector (left)
            var figPivot = new GameObject("Figure").transform; figPivot.SetParent(root, false); figPivot.localPosition = new Vector3(-1.9f, 0, 3.1f);
            var figSpin = new GameObject("Figure Spin").transform; figSpin.SetParent(figPivot, false);
            var fig = Figure(figSpin, 1.95f, 0.05f, out var heart, out var entry, out var fb);
            if (entry) Object.DestroyImmediate(entry.gameObject);
            var holo = figPivot.gameObject.AddComponent<Bvr2HoloPatient>(); holo.heart = heart;
            holo.rings = Projector(figPivot, new Vector3(0, 0.02f, 0), 1.15f); holo.ringSpeeds = new[] { 18f, -32f, 6f };
            // the tools turning over a projector (right)
            var toolsPivot = new GameObject("Tools").transform; toolsPivot.SetParent(root, false); toolsPivot.localPosition = new Vector3(1.9f, 0, 3.1f);
            var toolPad = Projector(toolsPivot, new Vector3(0, 0.02f, 0), 0.9f);
            var toolsSpin = new GameObject("Tools Spin").transform; toolsSpin.SetParent(toolsPivot, false); toolsSpin.localPosition = new Vector3(0, 1.25f, 0);
            var scH = new GameObject("Scanner").transform; scH.SetParent(toolsSpin, false); scH.localPosition = new Vector3(0, 0.12f, -0.18f); scH.localRotation = Quaternion.Euler(0, 90, 18);
            var btH = new GameObject("BioTool").transform; btH.SetParent(toolsSpin, false); btH.localPosition = new Vector3(0, -0.12f, 0.18f); btH.localRotation = Quaternion.Euler(0, -90, -18);
            bool tools = Tool("Scanner", scH, device, dark, glow, 3.2f) != null & Tool("BioTool", btH, device, dark, glow, 3.2f) != null;
            // halo rings behind the panel (standing upright, turning in their plane)
            var dim = Mat("Holo_RingDim", "BVR2/Glow");
            var haloA = Obj("Halo A", Arcs("Menu_HaloA", 1.16f, 1.19f, 4, 32f), Mat("Holo_Ring", "BVR2/Glow"), root, new Vector3(0, 1.5f, 2.4f), Vector3.one, Quaternion.Euler(-90, 0, 0));
            var haloB = Obj("Halo B", Arcs("Menu_HaloB", 1.27f, 1.285f, 72, 2.5f), dim, root, new Vector3(0, 1.5f, 2.45f), Vector3.one, Quaternion.Euler(-90, 0, 0));
            // motes drifting in the air
            var motes = Mat("Menu_Motes", "BVR2/Motes"); motes.SetColor("_Color", new Color(0.6f, 0.95f, 1f, 0.75f)); motes.SetFloat("_Size", 0.012f); motes.SetFloat("_Mode", 1); motes.SetVector("_Motion", new Vector4(0, 0, 0.35f, 0)); motes.SetFloat("_Intensity", 1.2f);
            Obj("Motes", Motes("Menu_MotesMesh", 320, new Vector3(0, 2.3f, 2.5f), new Vector3(14f, 4.6f, 12f), false, new[] { Color.white, new Color(0.6f, 0.9f, 1f), new Color(1f, 0.45f, 0.5f) }, 77), motes, root, Vector3.zero, Vector3.one);
            menu.spinners = new[] { figSpin, toolsSpin, haloA.transform, haloB.transform };
            menu.spinSpeeds = new[] { 10f, 28f, 7f, -4f };
            var laser = Mat("Menu_Laser", "BVR2/Trail"); laser.SetColor("_Color", new Color(0.6f, 0.95f, 1f, 1f)); laser.SetFloat("_Intensity", 1.6f); menu.laserMaterial = laser;
            root.gameObject.SetActive(false);
            return $"menu stage: figure {(fig ? fb.size.y.ToString("F2") : "missing")} m, tools {(tools ? "ok" : "missing")}";
        }

        /// <summary>Lat-long sphere (the sky dome; the shader draws its inside).</summary>
        static Mesh Dome(string name, float radius, int seg, int rings)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float lat = Mathf.PI * (j / (float)rings - 0.5f);
                for (int i = 0; i <= seg; i++)
                {
                    float lon = 2f * Mathf.PI * i / seg; var d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                    v.Add(d * radius); n.Add(-d); uv.Add(new Vector2(i / (float)seg, j / (float)rings));
                }
            }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++) { int a = j * (seg + 1) + i, b = a + seg + 1; t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1); }
            return Store(name, v, n, uv, null, null, t);
        }

        /// <summary>Flat disc in the XZ plane, UV = position / radius (−1..1).</summary>
        static Mesh Disc(string name, float radius, int seg)
        {
            // concentric rings so the UV stays smooth for the radial patterns
            var w = new List<Vector3>(); var wn = new List<Vector3>(); var wuv = new List<Vector2>(); var wt = new List<int>();
            const int R = 12;
            for (int j = 0; j <= R; j++)
                for (int i = 0; i <= seg; i++)
                {
                    float a = 2f * Mathf.PI * i / seg, rr = j / (float)R; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    w.Add(d * radius * rr); wn.Add(Vector3.up); wuv.Add(new Vector2(d.x, d.z) * rr);
                }
            for (int j = 0; j < R; j++)
                for (int i = 0; i < seg; i++) { int a = j * (seg + 1) + i, b = a + seg + 1; wt.Add(a); wt.Add(a + 1); wt.Add(b); wt.Add(a + 1); wt.Add(b + 1); wt.Add(b); }
            return Store(name, w, wn, wuv, null, null, wt);
        }

        static void SetHeight(Renderer r, Material m)
        {
            // object-space height range of the figure for the scan band (the import may keep z-up meshes)
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) return;
            var up = r.transform.InverseTransformDirection(Vector3.up).normalized;
            var mesh = mf.sharedMesh; var bb = mesh.bounds; float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3((i & 1) == 0 ? bb.min.x : bb.max.x, (i & 2) == 0 ? bb.min.y : bb.max.y, (i & 4) == 0 ? bb.min.z : bb.max.z);
                float d = Vector3.Dot(p, up); lo = Mathf.Min(lo, d); hi = Mathf.Max(hi, d);
            }
            m.SetVector("_Height", new Vector4(lo, hi, 0, 0)); m.SetVector("_Up", new Vector4(up.x, up.y, up.z, 0));
        }

        static Transform Label(Transform parent, string text, Vector3 pos, Font font, Color col)
        {
            var c = Bvr2UiEditor.Canvas("Label " + text, parent, new Vector2(420, 70), 0.0011f);
            c.localPosition = pos;
            var bg = Bvr2UiEditor.MakeImage(c, new Color(0.02f, 0.07f, 0.1f, 0.82f), null, null); // dense enough that the bright lab behind does not wash the text out
            var bar = Bvr2UiEditor.MakeImage(c, col, null, null); var br = bar.rectTransform; br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(0, 1); br.sizeDelta = new Vector2(6, 0); br.anchoredPosition = new Vector2(3, 0); // colour tab on the left
            var t = Bvr2UiEditor.MakeText(c, text, 40, col, font, null); t.alignment = TextAnchor.MiddleCenter;
            return c;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        // ------------------------------------------------------------------ the hand tools
        public sealed class ToolModel
        {
            public Transform root, tip, screen; public Renderer lens, emitter, core; public List<Transform> spinners = new List<Transform>();
            public List<Renderer> accents = new List<Renderer>(), coils = new List<Renderer>(); public Transform emitterT;
        }

        /// <summary>Instantiates one tool ("Scanner" or "BioTool") from Tools.fbx under parent, pointing along +Z.</summary>
        public static ToolModel Tool(string prefix, Transform parent, Material body, Material dark, Material glow, float scale = 1f)
        {
            var src = Load(Art + "/Tools.fbx"); if (!src) return null;
            var glass = Mat("Tool_Glass", "BVR2/Glow"); glass.SetColor("_Color", new Color(0.55f, 0.9f, 1f)); glass.SetFloat("_Intensity", 0.6f); glass.SetFloat("_Core", 0.04f); glass.SetFloat("_Fresnel", 2.6f);
            // the dark parts in a blue-grey gunmetal rather than near-black (they read as holes in the headset otherwise)
            var tdark = Mat("Tool_Dark", dark.shader.name); tdark.CopyPropertiesFromMaterial(dark);
            tdark.SetColor("_BaseColor", new Color(0.24f, 0.27f, 0.33f)); tdark.SetFloat("_Gloss", 0.85f); tdark.SetColor("_Rim", new Color(0.22f, 0.5f, 0.62f));
            dark = tdark;
            var tm = new ToolModel();
            var group = new GameObject(prefix + " Model").transform; group.position = src.transform.position; group.rotation = Quaternion.identity;
            Transform bodyT = null, lensT = null, screenT = null;
            foreach (var t in src.GetComponentsInChildren<Transform>(true))
            {
                if (t == src.transform || !t.name.StartsWith(prefix + "_")) continue;
                if (!t.GetComponent<Renderer>()) continue;
                string n = t.name; string suf = n.Substring(n.LastIndexOf('_') + 1);
                t.SetParent(group, true);
                var r = t.GetComponent<Renderer>();
                r.sharedMaterial = suf == "Body" ? body : suf == "Dark" ? dark : suf == "Glass" ? glass : glow;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (n.Contains("_Body_")) bodyT = t;
                if (n.Contains("_Lens_") || n.Contains("_Emitter_")) lensT = t;
                if (n.Contains("_Screen_")) screenT = t;
                if (n.Contains("_Ring")) tm.spinners.Add(t);
                if (n.Contains("_Coil")) { tm.spinners.Add(t); tm.coils.Add(r); }
                if (n.Contains("_Strip") || n.Contains("_Led") || n.Contains("_BackRing") || n.Contains("_GripLight") || n.Contains("_Bead")) tm.accents.Add(r);
                if (n.Contains("_Lens_")) tm.lens = r;
                if (n.Contains("_Emitter_")) { tm.emitter = r; tm.emitterT = t; }
                if (n.Contains("_Core_")) tm.core = r;
            }
            Object.DestroyImmediate(src);
            if (!bodyT || !lensT) { Debug.LogWarning("[BVR2] tool parts missing for " + prefix); return tm; }
            // orientation: body -> lens is +Z, the screen is up; size: the body's length
            Vector3 c0 = bodyT.GetComponent<Renderer>().bounds.center, f = lensT.GetComponent<Renderer>().bounds.center - c0;
            Vector3 u = screenT ? screenT.GetComponent<Renderer>().bounds.center - c0 : Vector3.up;
            var look = Quaternion.LookRotation(f.normalized, Vector3.ProjectOnPlane(u, f).normalized);
            var fix = Quaternion.Inverse(look);
            float len = Vector3.Dot(bodyT.GetComponent<Renderer>().bounds.size, new Vector3(Mathf.Abs(f.normalized.x), Mathf.Abs(f.normalized.y), Mathf.Abs(f.normalized.z)));
            float want = prefix == "Scanner" ? 0.116f : 0.083f;
            float s = len > 1e-4f ? want / len : 1f;
            group.SetParent(parent, false); group.localPosition = Vector3.zero; group.localRotation = fix; group.localScale = Vector3.one * s * scale;
            tm.root = group;
            // tip just in front of the lens / emitter; the screen anchor above the screen plate
            var tip = new GameObject("Tip").transform; tip.SetParent(parent, false);
            tip.position = lensT.GetComponent<Renderer>().bounds.center + parent.forward * 0.012f * scale; tm.tip = tip;
            if (screenT) { var sa = new GameObject("Screen Anchor").transform; sa.SetParent(parent, false); sa.position = screenT.GetComponent<Renderer>().bounds.center + parent.up * 0.0035f * scale; tm.screen = sa; }
            return tm;
        }

        // ------------------------------------------------------------------ lab effects
        public static string LabFx(Transform lab, IList<Vector3> pendants, IList<Vector3> bubbleSpots, IList<Vector3> tubeSpots)
        {
            var holder = new GameObject("Lab FX").transform; holder.SetParent(lab, false);
            // dust floating in the warm air of the main room
            var dust = Mat("Lab_Dust", "BVR2/Motes"); dust.SetColor("_Color", new Color(1f, 0.86f, 0.62f, 0.55f)); dust.SetFloat("_Size", 0.0065f); dust.SetFloat("_Mode", 1); dust.SetVector("_Motion", new Vector4(0, 0, 0.28f, 0)); dust.SetFloat("_Intensity", 1.1f);
            Obj("Dust", Motes("Lab_DustMotes", 260, new Vector3(-0.3f, 1.45f, -0.8f), new Vector3(4.4f, 2.2f, 5.0f), false, new[] { Color.white, new Color(1f, 0.9f, 0.75f) }, 11), dust, holder, Vector3.zero, Vector3.one);
            // soft light shafts under the pendant lamps
            var shaft = Mat("Lab_Shaft", "BVR2/HoloBeam"); shaft.SetColor("_Color", new Color(1f, 0.8f, 0.52f, 0.11f)); shaft.SetFloat("_Falloff", 1.2f); shaft.SetFloat("_Edge", 1.2f); shaft.SetVector("_Streaks", new Vector4(9, 0.15f, 0.25f, 0));
            int n = 0;
            foreach (var p in pendants)
            {
                float h = Mathf.Max(0.5f, p.y - 0.05f);
                Obj("Light Shaft " + n++, Beam("Lab_Shaft_" + Mathf.RoundToInt(h * 100), 0.13f, 0.6f, h), shaft, holder, p, Vector3.one, Quaternion.Euler(180, 0, 0));
            }
            // bubbles rising in the sample jars and the test tubes on the bench
            var bub = Mat("Lab_Bubbles", "BVR2/Motes"); bub.SetColor("_Color", new Color(1f, 1f, 1f, 0.85f)); bub.SetFloat("_Size", 0.0042f); bub.SetFloat("_Mode", 0); bub.SetVector("_Motion", new Vector4(0.12f, 0.05f, 0.004f, 0));
            int j = 0;
            foreach (var p in bubbleSpots) Obj("Bubbles " + j, Motes("Lab_Bubbles_" + j++, 14, Vector3.zero, new Vector3(0.06f, 0f, 0.06f), true, new[] { Color.white, new Color(0.8f, 1f, 1f) }, 20 + j, 0.12f, true), bub, holder, p, Vector3.one);
            var bubT = Mat("Lab_BubblesTube", "BVR2/Motes"); bubT.SetColor("_Color", new Color(1f, 1f, 1f, 0.8f)); bubT.SetFloat("_Size", 0.0028f); bubT.SetFloat("_Mode", 0); bubT.SetVector("_Motion", new Vector4(0.085f, 0.04f, 0.0015f, 0));
            foreach (var p in tubeSpots) Obj("Tube Bubbles " + j, Motes("Lab_Bubbles_" + j++, 5, Vector3.zero, new Vector3(0.012f, 0f, 0.012f), true, new[] { Color.white }, 40 + j, 0.09f, true), bubT, holder, p, Vector3.one);
            return $"lab fx: dust, {n} light shafts, {bubbleSpots.Count} bubbling jars, {tubeSpots.Count} tubes" + (bubbleSpots.Count > 0 ? $" (first jar at {bubbleSpots[0]:F2})" : "");
        }

        /// <summary>A device frame for a world-space panel: thin bright edges, corner brackets and a divider under the title.</summary>
        public static void PanelFrame(RectTransform canvas, Color col, float dividerFromTop)
        {
            var size = canvas.sizeDelta;
            void Bar(string n, Vector2 aMin, Vector2 aMax, Vector2 sz, Vector2 pos, Color c)
            {
                var img = Bvr2UiEditor.MakeImage(canvas, c, null, null); img.name = n; img.raycastTarget = false; var r = img.rectTransform;
                r.anchorMin = aMin; r.anchorMax = aMax; r.sizeDelta = sz; r.anchoredPosition = pos;
            }
            var edge = new Color(col.r, col.g, col.b, 0.45f);
            Bar("Edge Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 3), new Vector2(0, -1.5f), edge);
            Bar("Edge Bottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 3), new Vector2(0, 1.5f), edge);
            Bar("Edge Left", new Vector2(0, 0), new Vector2(0, 1), new Vector2(3, 0), new Vector2(1.5f, 0), edge);
            Bar("Edge Right", new Vector2(1, 0), new Vector2(1, 1), new Vector2(3, 0), new Vector2(-1.5f, 0), edge);
            const float L = 46f, T = 7f;
            foreach (var c in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) })
            {
                float sx = c.x < 0.5f ? 1f : -1f, sy = c.y < 0.5f ? 1f : -1f;
                Bar("Corner H", c, c, new Vector2(L, T), new Vector2(sx * L * 0.5f, sy * T * 0.5f), col);
                Bar("Corner V", c, c, new Vector2(T, L), new Vector2(sx * T * 0.5f, sy * L * 0.5f), col);
            }
            Bar("Divider", new Vector2(0, 1), new Vector2(1, 1), new Vector2(-60, 2), new Vector2(0, -dividerFromTop), new Color(col.r, col.g, col.b, 0.35f));
        }

        /// <summary>The ECG strip on the vitals panel (a quad in the canvas, bottom part).</summary>
        public static void Ecg(RectTransform canvas, Vector2 sizePx, Vector2 centrePx)
        {
            var ecg = Mat("Lab_Ecg", "BVR2/HoloEcg"); ecg.SetFloat("_Aspect", sizePx.x / Mathf.Max(1f, sizePx.y)); ecg.SetFloat("_Thickness", 0.022f);
            ecg.SetColor("_Color", new Color(0.35f, 1f, 0.55f, 1f)); ecg.SetColor("_Grid", new Color(0.2f, 0.6f, 0.5f, 0.22f));
            // a UI graphic in the panel (drawn after the panel's background in the canvas order, unlike a separate mesh)
            var img = Bvr2UiEditor.MakeImage(canvas, Color.white, null, ecg); img.name = "ECG"; var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.sizeDelta = sizePx; r.anchoredPosition = centrePx;
        }
    }
}
